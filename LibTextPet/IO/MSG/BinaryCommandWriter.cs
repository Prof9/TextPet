using LibTextPet.General;
using LibTextPet.Msg;
using LibTextPet.Text;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;

namespace LibTextPet.IO.Msg {
	/// <summary>
	/// A binary command writer that writes script commands to an output stream.
	/// </summary>
	public class BinaryCommandWriter : Manager, IWriter<Command> {
		protected CharacterCodeEncoder CharCodeEncoder { get; }

		/// <summary>
		/// Creates a binary command writer that writes to the specified output stream.
		/// </summary>
		/// <param name="stream">The stream to write to.</param>
		public BinaryCommandWriter(Stream stream, IgnoreFallbackEncoding encoding)
			: base(stream, true, FileAccess.Write, encoding) {
			this.CharCodeEncoder = new CharacterCodeEncoder(encoding);
		}

		/// <summary>
		/// Writes the specified script command to the output stream.
		/// </summary>
		/// <param name="obj">The script command to write.</param>
		public void Write(Command obj) {
			if (obj == null)
				throw new ArgumentNullException(nameof(obj), "The script command cannot be null.");

			IDictionary<string, int> labelDict = new Dictionary<string, int>();

			// Write the base.
			IList<byte> bytes = new List<byte>();
			foreach (byte b in obj.Definition.Base) {
				bytes.Add(b);
			}

			// Extend to mask length if needed.
			while (bytes.Count < obj.Definition.Mask.Count) {
				bytes.Add(0);
			}

			// Write the elements.
			foreach (CommandElement elem in obj.Elements) {
				// Write the length length.
				if (elem.Definition.HasMultipleDataEntries) {
					WriteParameterValueToBytes(elem.Count, bytes, labelDict, elem.Definition.LengthParameterDefinition);
				}

				foreach (IEnumerable<ParameterDefinition> dataGroup in elem.Definition.DataGroups) {
					// Write the data parameters.
					foreach (ReadOnlyNamedCollection<Parameter> entry in elem) {
						foreach (ParameterDefinition parDef in dataGroup) {
							WriteParameterValueToBytes(labelDict, entry[parDef.Name], bytes);
						}
					}
				}
			}

			// Perform rewind (or fast-forward, though that's probably not something you'd ever want, but hey).
			int writeCount = (int)(bytes.Count - obj.Definition.RewindCount);
			byte[] buffer = bytes.Take(writeCount).ToArray();
			this.BaseStream.Write(buffer, 0, buffer.Length);
		}

		/// <summary>
		/// Writes the value of the specified parameter to the specified byte sequence.
		/// </summary>
		/// <param name="labelDict">A dictionary containing the offset labels for the current command.</param>
		/// <param name="par">The parameter to write.</param>
		/// <param name="bytes">The byte sequence to write to.</param>
		protected void WriteParameterValueToBytes(IDictionary<string, int> labelDict, Parameter par, IList<byte> bytes) {
			if (par == null)
				throw new ArgumentNullException(nameof(par), "The parameter cannot be null.");
			if (bytes == null)
				throw new ArgumentNullException(nameof(bytes), "The byte sequence cannot be null.");

			if (par.IsNumber) {
				WriteParameterValueToBytes(par.NumberValue, bytes, labelDict, par.Definition);
				return;
			}

			// Parameter is a string, need to write this first.
			IList<byte> stringBytes = new List<byte>();
			int charCodeCount = 0;
			int charsEncoded = 0;
			while (charsEncoded < par.StringValue.Length) {
				// Encode the next character code.
				int curCharCodeLength = this.CharCodeEncoder.WriteSingle(par.StringValue, charsEncoded, stringBytes, stringBytes.Count);
				if (curCharCodeLength == 0) {
					throw new InvalidDataException("Could not encode string value \"" + par.StringValue + "\" of parameter \"" + par.Name + "\".");
				}

				charsEncoded += curCharCodeLength;
				charCodeCount += 1;
			}

			// Check the length of the string.
			if (par.Definition.StringDefinition.FixedLength != 0) {
				// Verify that the string matches fixed length.
				switch (par.Definition.StringDefinition.Unit) {
				case StringLengthUnit.Byte:
					if (stringBytes.Count != par.Definition.StringDefinition.FixedLength) {
						throw new InvalidDataException("String value \"" + par.StringValue + "\" of parameter \"" + par.Name + "\" does not match the expected length of " + par.Definition.StringDefinition.FixedLength + " bytes.");
					}
					break;
				case StringLengthUnit.Char:
					if (charCodeCount > par.Definition.StringDefinition.FixedLength) {
						throw new InvalidDataException("String value \"" + par.StringValue + "\" of parameter \"" + par.Name + "\" does not match the expected length of " + par.Definition.StringDefinition.FixedLength + " characters.");
					}
					break;
				}
			} else {
				// Write the variable length of the string.
				switch (par.Definition.StringDefinition.Unit) {
				case StringLengthUnit.Byte:
					WriteParameterValueToBytes(stringBytes.Count, bytes, labelDict, par.Definition);
					break;
				case StringLengthUnit.Char:
					WriteParameterValueToBytes(charCodeCount, bytes, labelDict, par.Definition);
					break;
				}
			}

			// Write the string itself.
			for (int i = 0; i < stringBytes.Count; i++) {
				int pos = par.Definition.StringDefinition.Offset + i;

				while (pos > bytes.Count) {
					bytes.Add(0);
				}
				if (pos == bytes.Count) {
					bytes.Add(stringBytes[i]);
				} else {
					bytes[pos] = stringBytes[i];
				}
			}
		}

		/// <summary>
		/// Writes the value of a parameter with the specified definition to the specified byte sequence.
		/// </summary>
		/// <param name="value">The value to write.</param>
		/// <param name="bytes">The byte sequence to write to.</param>
		/// <param name="labelDict">A dictionary containing the offset labels for the current command.</param>
		/// <param name="parDef">The parameter definition to use.</param></param>
		protected static void WriteParameterValueToBytes(long value, IList<byte> bytes, IDictionary<string, int> labelDict, ParameterDefinition parDef) {
			if (bytes == null)
				throw new ArgumentNullException(nameof(bytes), "The byte sequence cannot be null.");
			if (parDef == null)
				throw new ArgumentNullException(nameof(parDef), "The parameter definition cannot be null.");
			if (value < parDef.Minimum || value > parDef.Maximum)
				throw new ArgumentOutOfRangeException(nameof(value), value, "The value falls outside the allowed range.");

			value -= parDef.Add;

			// Add relative offset.
			int offset;
			switch (parDef.OffsetType) {
			case OffsetType.Start:
				offset = 0;
				break;
			case OffsetType.End:
				offset = bytes.Count;
				break;
			case OffsetType.Label:
				if (!labelDict.TryGetValue(parDef.RelativeLabel, out offset)) {
					throw new InvalidDataException("Unknown label \"" + parDef.RelativeLabel + "\".");
				}
				break;
			default:
				throw new InvalidDataException("Unrecognized offset type.");
			}
			int bytesNeeded = offset + parDef.Offset + parDef.MinimumByteCount;

			// Add offset to labels.
			labelDict[parDef.Name] = offset + parDef.Offset;

			// Add extra bytes if needed.
			while (bytes.Count < bytesNeeded) {
				bytes.Add(0);
			}

			// The number of bits to write.
			int bits = parDef.Bits;
			// The bit position for writing to the output bytes.
			int outshift = parDef.Shift % 8;
			// The byte position for writing to the output bytes.
			offset += parDef.Offset + parDef.Shift / 8;

			// Write entire value.
			while (bits > 0) {
				int next = 8 - outshift;
				next = bits < next ? bits : next;
				int mask = (1 << next) - 1;

				bytes[offset] &= (byte)~(mask << outshift);
				bytes[offset] |= (byte)((value & mask) << outshift);

				bits -= next;
				value >>= next;
				outshift = 0;
				offset += 1;
			}
		}
	}
}
