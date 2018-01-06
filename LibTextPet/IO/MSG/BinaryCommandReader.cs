using LibTextPet.General;
using LibTextPet.Msg;
using LibTextPet.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LibTextPet.IO.Msg {
	/// <summary>
	/// A binary command reader that reads script commands from an input stream.
	/// </summary>
	public class BinaryCommandReader : SingleManager, IReader<Command> {
		/// <summary>
		/// Gets a text reader for the current input stream.
		/// </summary>
		protected ConservativeStreamReader TextReader { get; private set; }

		/// <summary>
		/// Creates a binary command reader that reads from the specified input stream.
		/// </summary>
		/// <param name="stream">The stream to read from.</param>
		/// <param name="database">The command database to use.</param>
		/// <param name="encoding">The encoding to use.</param>
		public BinaryCommandReader(Stream stream, CommandDatabase database, CustomFallbackEncoding encoding) 
			: base(stream, true, FileAccess.Read, database) {
			this.TextReader = new ConservativeStreamReader(stream, encoding);
		}

		/// <summary>
		/// Reads the next script command from the input stream.
		/// </summary>
		/// <returns>The next script command read from the input stream, or null if no script command was read.</returns>
		public Command Read() {
			List<byte> sequence = new List<byte>();
			Command cmd = null;
			IList<CommandDefinition> defs;
			long start = this.BaseStream.Position;
			

			// Filter command database to find matching element.
			do {
				// Cancel if end of stream reached.
				int b = this.BaseStream.ReadByte();
				if (b == -1) {
					break;
				}
				sequence.Add((byte)b);

				// Match current sequence.
				defs = this.Databases[this.Databases.Count - 1].Match(sequence);

				// Is there a command that takes priority at at least this length?
				CommandDefinition priorityDef = null;
				foreach (CommandDefinition def in defs) {
					if (def.PriorityLength > 0 && sequence.Count >= def.PriorityLength) {
						// Do we already have a priority command?
						if (priorityDef == null) {
							priorityDef = def;
						} else {
							// We can't have multiple commands taking priority, so abort.
							priorityDef = null;
							break;
						}
					}
				}
				// Did we find a priority match?
				if (priorityDef != null) {
					defs = new CommandDefinition[] { priorityDef };
				}

				// If match found, read it.
				if (defs.Count == 1) {
					this.BaseStream.Position = start;
					cmd = Read(defs[0]);
				}
			} while (defs.Count > 1);

			return cmd;
		}

		/// <summary>
		/// Reads the next script command from the input stream with the given command definition.
		/// </summary>
		/// <param name="definition">The command definition.</param>
		/// <returns>The command read.</returns>
		protected Command Read(CommandDefinition definition) {
			if (definition == null)
				throw new ArgumentNullException(nameof(definition), "The command definition cannot be null.");

			Command cmd = new Command(definition);

			// Read base bytes.
			byte[] buffer = new byte[Math.Max(definition.Base.Count, definition.Mask.Count)];
			if (this.BaseStream.Read(buffer, 0, buffer.Length) < buffer.Length) {
				// Cannot read enough bytes.
				return null;
			}
			IList<byte> bytes = new List<byte>(buffer);

			// Verify base bytes.
			for (int i = 0; i < Math.Min(definition.Base.Count, definition.Mask.Count); i++) {
				if ((bytes[i] & definition.Mask[i]) != definition.Base[i]) {
					// Base mismatch; return null.
					return null;
				}
			}

			// Read elements.
			foreach (CommandElementDefinition elemDef in definition.Elements) {
				CommandElement elem = cmd.Elements[elemDef.Name];

				// Get the number of entries in the element.
				long length = 1;
				if (elemDef.HasMultipleDataEntries) {
					if (!this.ReadParameterValue(bytes, elemDef.LengthParameterDefinition, out length)) {
						// Failed to read parameter.
						return null;
					}
				}

				// If length is invalid, the command cannot be read.
				if (length < 0) {
					return null;
				}

				foreach (IEnumerable<ParameterDefinition> dataParGroup in elemDef.DataGroups) {
					// Read data entries.
					for (int i = 0; i < length; i++) {
						if (elemDef.HasMultipleDataEntries) {
							elem.Add(elem.CreateDataEntry());
						}

						foreach (ParameterDefinition parDef in dataParGroup) {
							if (!this.ReadParameter(bytes, elem[i][parDef.Name])) {
								// Failed to read parameter.
								return null;
							}
						}
					}
				}
			}

			// Perform rewind.
			this.BaseStream.Position -= definition.RewindCount;

			return cmd;
		}

		private bool ReadParameter(IList<byte> readBytes, Parameter par) {
			if (!this.ReadParameterValue(readBytes, par.Definition, out long value)) {
				// Failed to read parameter number.
				return false;
			}

			if (par.IsNumber) {
				par.NumberValue = value;
				return true;
			}

			// Parameter is a string, need to load this next.
			StringBuilder builder = new StringBuilder();
			byte[] buffer;

			// Use variable length if parameter has a number component.
			long varLength = par.Definition.Bits > 0 ? value : Int64.MaxValue;
			long fixLength = par.Definition.StringDefinition.FixedLength > 0 ? par.Definition.StringDefinition.FixedLength : Int64.MaxValue;

			// Use minimum string length.
			long strLen = Math.Min(varLength, fixLength);
			if (strLen == Int64.MaxValue || strLen < 0) {
				// No valid string length available.
				return false;
			}

			// Fast-forward to string offset, if needed.
			int strOffset = 0 + par.Definition.StringDefinition.Offset;
			if (strOffset > readBytes.Count) {
				buffer = new byte[strOffset - readBytes.Count];
				if (this.BaseStream.Read(buffer, 0, buffer.Length) != buffer.Length) {
					// Could not read the required bytes.
					return false;
				}
				foreach (byte b in buffer) {
					readBytes.Add(b);
				}
			}

			// Read the string.
			long strPos = this.BaseStream.Position;
			switch (par.Definition.StringDefinition.Unit) {
			case StringLengthUnit.Char:
				for (int i = 0; i < strLen; i++) {
					// Read the next character.
					IEnumerable<char> nextChar = this.TextReader.Read();
					//if (!nextChar.Any()) {
					//	// Could not read next character.
					//	return false;
					//}

					foreach (char c in nextChar) {
						builder.Append(c);
					}
				}

				// Rewind and add read bytes to buffer.
				buffer = new byte[this.BaseStream.Position - strPos];
				this.BaseStream.Position = strPos;
				if (this.BaseStream.Read(buffer, 0, buffer.Length) != buffer.Length) {
					// Could not read the required bytes the second time. Should never happen...
					return false;
				}
				foreach (byte b in buffer) {
					readBytes.Add(b);
				}
				break;
			case StringLengthUnit.Byte:
				// Read the bytes.
				buffer = new byte[strLen];
				if (this.BaseStream.Read(buffer, 0, buffer.Length) != buffer.Length) {
					// Could not read the required bytes.
					return false;
				}
				foreach (byte b in buffer) {
					readBytes.Add(b);
				}

				// Decode the string.
				string s = this.TextReader.Encoding.GetString(buffer);
				if (s.Contains('\uFFFD')) {
					// Could not properly decode the string.
					return false;
				}
				builder.Append(s);
				break;
			}

			par.StringValue = builder.ToString();
			return true;
		}

		/// <summary>
		/// Reads the value of a parameter with the specified definition from the current stream, reading extra bytes if needed.
		/// </summary>
		/// <param name="readBytes">The sequence of bytes that have already been read.</param>
		/// <param name="parDef">The parameter definition to use.</param>
		/// <param name="result">When this method returns, contains the value of the parameter that was read.</param>
		/// <returns>true if the parameter value was read successfully; otherwise, false.</returns>
		private bool ReadParameterValue(IList<byte> readBytes, ParameterDefinition parDef, out long result) {
			if (readBytes == null)
				throw new ArgumentNullException(nameof(readBytes), "The byte sequence cannot be null.");
			if (parDef == null)
				throw new ArgumentNullException(nameof(parDef), "The parameter definition cannot be null.");

			// TODO: Add relative offset.
			int offset = 0;
			int bytesNeeded = offset + parDef.Offset + parDef.MinimumByteCount;
			
			// Read extra bytes if needed.
			if (readBytes.Count < bytesNeeded) {
				byte[] buffer = new byte[bytesNeeded - readBytes.Count];
				if (this.BaseStream.Read(buffer, 0, buffer.Length) != buffer.Length) {
					// Cannot read enough bytes.
					result = 0;
					return false;
				}
				foreach (byte b in buffer) {
					readBytes.Add(b);
				}
			}

			result = ReadParameterValueFromBytes(readBytes, parDef, offset);
			return true;
		}

		/// <summary>
		/// Reads the value of a parameter with the given definition from the given byte sequence, from the given offset.
		/// </summary>
		/// <param name="bytes">The byte sequence to read from.</param>
		/// <param name="parDef">The parameter definition to use.</param>
		/// <param name="offset">The value to add for the parameter byte offset. Normally, this should be zero, except for data parameters.</param>
		/// <returns>The value that was read.</returns>
		private static long ReadParameterValueFromBytes(IList<byte> bytes, ParameterDefinition parDef, int offset) {
			if (bytes == null)
				throw new ArgumentNullException(nameof(bytes), "The byte sequence cannot be null.");
			if (parDef == null)
				throw new ArgumentNullException(nameof(parDef), "The parameter definition cannot be null.");
			if (offset < 0)
				throw new ArgumentOutOfRangeException(nameof(offset), offset, "The offset cannot be negative.");

			long value = 0;

			// The number of bits to read.
			int bits = parDef.Bits;
			// The bit position for writing to the output value.
			int outshift = 0;
			// The bit position for reading from the input bytes.
			int inshift = parDef.Shift % 8;
			// The byte position for reading from the input bytes.
			offset += parDef.Offset + parDef.Shift / 8;

			// Read entire value.
			int mask = 0xFF;
			while (bits > 0) {
				if (bits < 8) {
					mask = (1 << bits) - 1;
				}

				// Need to cast to long here, otherwise the value may overflow when shifted.
				value += (long)((bytes[offset] >> inshift) & mask) << outshift;

				bits -= 8 - inshift;
				outshift += 8 - inshift;
				inshift = 0;
				offset += 1;
			}

			return value + parDef.Add;
		}
	}
}
