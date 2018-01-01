using LibTextPet.General;
using LibTextPet.Msg;
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
		/// <summary>
		/// Creates a binary command writer that writes to the specified output stream.
		/// </summary>
		/// <param name="stream">The stream to write to.</param>
		public BinaryCommandWriter(Stream stream)
			: base(stream, true, FileAccess.Write) { }

		/// <summary>
		/// Writes the specified script command to the output stream.
		/// </summary>
		/// <param name="obj">The script command to write.</param>
		public void Write(Command obj) {
			if (obj == null)
				throw new ArgumentNullException(nameof(obj), "The script command cannot be null.");

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
					WriteParameterValueToBytes(elem.Count, bytes, elem.Definition.LengthParameterDefinition);
				}

				foreach (IEnumerable<ParameterDefinition> dataGroup in elem.Definition.DataGroups) {
					// Write the data parameters.
					foreach (ReadOnlyNamedCollection<Parameter> entry in elem) {
						foreach (ParameterDefinition parDef in dataGroup) {
							WriteParameterValueToBytes(entry[parDef.Name], bytes);
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
		/// <param name="par">The parameter to write.</param>
		/// <param name="bytes">The byte sequence to write to.</param>
		protected static void WriteParameterValueToBytes(Parameter par, IList<byte> bytes)
			=> WriteParameterValueToBytes(par.ToInt64(), bytes, par.Definition);

		/// <summary>
		/// Writes the value of a parameter with the specified definition to the specified byte sequence.
		/// </summary>
		/// <param name="value">The value to write.</param>
		/// <param name="bytes">The byte sequence to write to.</param>
		/// <param name="parDef">The parameter definition to use.</param></param>
		protected static void WriteParameterValueToBytes(long value, IList<byte> bytes, ParameterDefinition parDef) {
			if (bytes == null)
				throw new ArgumentNullException(nameof(bytes), "The byte sequence cannot be null.");
			if (parDef == null)
				throw new ArgumentNullException(nameof(parDef), "The parameter definition cannot be null.");
			if (value < parDef.Minimum || value > parDef.Maximum)
				throw new ArgumentOutOfRangeException(nameof(value), value, "The value falls outside the allowed range.");

			// TODO: Add relative offset.
			int offset = 0;
			int bytesNeeded = offset + parDef.Offset + parDef.MinimumByteCount;

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
