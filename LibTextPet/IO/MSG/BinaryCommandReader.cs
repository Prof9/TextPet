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
		private List<byte> byteSequence;
		private StringBuilder stringBuilder;
		private char[] charBuffer;
		private byte[] byteBuffer;

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
		public BinaryCommandReader(Stream stream, CommandDatabase database, Encoding encoding) 
			: base(stream, true, FileAccess.Read, database) {
			this.TextReader = new ConservativeStreamReader(stream, encoding);
			this.BytesLeft = -1;

			this.byteSequence = new List<byte>();
			this.stringBuilder = new StringBuilder();
			this.charBuffer = new char[this.TextReader.GetMaxCharCount(1)];
			this.byteBuffer = new byte[this.TextReader.GetMaxByteCount(1)];
		}

		/// <summary>
		/// Gets or sets the amount of bytes that this binary command reader is allowed to continue reading. If this value is negative, no limit is imposed.
		/// </summary>
		public long BytesLeft { get; set; }

		protected virtual int ReadNextByte() {
			if (this.BytesLeft == 0) {
				return -1;
			}
			if (this.BytesLeft > 0) {
				this.BytesLeft--;
			}
			return this.BaseStream.ReadByte();
		}

		/// <summary>
		/// Reads the next script command from the input stream.
		/// </summary>
		/// <returns>The next script command read from the input stream, or null if no script command was read.</returns>
		public Command Read() {
			this.byteSequence.Clear();
			IList<CommandDefinition> defs;
			long start = this.BaseStream.Position;
			long bytesLeft = this.BytesLeft;

			// Filter command database to find matching element.
			CommandDefinition matchDef = null;
			do {
				// Cancel if end of stream reached.
				int b = this.ReadNextByte();
				if (b == -1) {
					break;
				}
				this.byteSequence.Add((byte)b);

				// Match current sequence.
				defs = this.Databases[this.Databases.Count - 1].Match(this.byteSequence);
				if (defs.Count == 0) {
					// Cancel if no more matches.
					break;
				}

				// Is there a command that takes priority at at least this length?
				foreach (CommandDefinition def in defs) {
					if (def.PriorityLength > 0 && this.byteSequence.Count >= def.PriorityLength) {
						// Do we already have a priority command?
						if (matchDef == null) {
							matchDef = def;
						} else {
							// We can't have multiple commands taking priority, so abort.
							matchDef = null;
							break;
						}
					}
				}

				// Did we find a non-priority match?
				if ((matchDef == null || matchDef.LookAhead) && defs.Count == 1) {
					matchDef = defs[0];
				}
			} while (matchDef == null || matchDef.LookAhead);

			// Abort if no match found.
			if (matchDef == null) {
				return null;
			}

			// If match found, read it.
			this.BaseStream.Position = start;
			this.BytesLeft = bytesLeft;
			return Read(matchDef);
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

			Dictionary<string, int> labelDict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

			this.byteSequence.Clear();

			int byteCount = definition.Base.Count;
			for (int i = 0; i < byteCount; i++) {
				int b = this.ReadNextByte();
				if (b == -1) {
					// Cannot read enough bytes.
					return null;
				}
				this.byteSequence.Add((byte)b);
			}

			// Verify base bytes.
			for (int i = 0; i < byteCount; i++) {
				if ((this.byteSequence[i] & definition.Base[i].Mask) != definition.Base[i].Byte) {
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
					if (!this.ReadParameterValue(this.byteSequence, labelDict, elemDef.LengthParameterDefinition, out length)) {
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
						while (elemDef.HasMultipleDataEntries && i >= elem.Count) {
							elem.Add(elem.CreateDataEntry());
						}

						foreach (ParameterDefinition parDef in dataParGroup) {
							if (!this.ReadParameter(this.byteSequence, labelDict, elem[i][parDef.Name])) {
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

		private bool ReadParameter(IList<byte> readBytes, IDictionary<string, int> labelDict, Parameter par) {
			if (!this.ReadParameterValue(readBytes, labelDict, par.Definition, out long value)) {
				// Failed to read parameter number.
				return false;
			}

			if (par.IsNumber) {
				par.NumberValue = value;
				return true;
			}

			// Parameter is a string, need to load this next.
			this.stringBuilder.Clear();

			// Use variable length if parameter has a number component.
			int varLength = par.Definition.Bits > 0 ? (int)Math.Min(value, Int32.MaxValue) : Int32.MaxValue;
			int fixLength = par.Definition.StringDefinition.FixedLength > 0 ? par.Definition.StringDefinition.FixedLength : Int32.MaxValue;

			// Use minimum string length.
			int strLen = Math.Min(varLength, fixLength);
			if (strLen == Int32.MaxValue || strLen < 0) {
				// No valid string length available.
				return false;
			}

			// Fast-forward to string offset, if needed.
			int strOffset = 0 + par.Definition.StringDefinition.Offset;
			if (strOffset > readBytes.Count) {
				int byteCount = strOffset - readBytes.Count;
				for (int i = 0; i < byteCount; i++) {
					int b = this.ReadNextByte();
					if (b == -1) {
						// Could not read the required bytes.
						return false;
					}
					readBytes.Add((byte)b);
				}
			}

			// Read the string.
			switch (par.Definition.StringDefinition.Unit) {
			case StringLengthUnit.Char:
				for (int i = 0; i < strLen; i++) {
					// Read the next code point.
					if (!this.TextReader.TryReadSingleCodePoint(this.charBuffer, 0, this.byteBuffer, 0, out int charsUsed, out int bytesUsed)) {
						// Abort on invalid character.
						return false;
					}

					this.stringBuilder.Append(this.charBuffer, 0, charsUsed);
					// Add read bytes to buffer.
					for (int j = 0; j < bytesUsed; j++) {
						readBytes.Add(this.byteBuffer[0]);
					}
				}
				break;
			case StringLengthUnit.Byte:
				// Read the bytes.
				byte[] buffer = new byte[strLen];
				for (int i = 0; i < buffer.Length; i++) {
					int b = this.ReadNextByte();
					if (b == -1) {
						// Could not read the required bytes.
						return false;
					}
					readBytes.Add(buffer[i] = (byte)b);
				}

				// Expand char buffer if necessary.
				int maxCharCount = this.TextReader.Encoding.GetMaxCharCount(strLen);
				if (this.charBuffer == null || this.charBuffer.Length < maxCharCount) {
					this.charBuffer = new char[maxCharCount];
				}

				// Decode the string.
				while (strLen > 0) {
					// Read the next code point.
					if (!this.TextReader.TryReadSingleCodePoint(this.charBuffer, 0, this.byteBuffer, 0, out int charsUsed, out int bytesUsed)) {
						// Abort on invalid character.
						return false;
					}

					this.stringBuilder.Append(this.charBuffer, 0, charsUsed);
					strLen -= bytesUsed;
				}
				
				break;
			}

			par.StringValue = this.stringBuilder.ToString();
			return true;
		}

		/// <summary>
		/// Reads the value of a parameter with the specified definition from the current stream, reading extra bytes if needed.
		/// </summary>
		/// <param name="readBytes">The sequence of bytes that have already been read.</param>
		/// <param name="labelDict">A dictionary containing the offset labels for the current command.</param>
		/// <param name="parDef">The parameter definition to use.</param>
		/// <param name="result">When this method returns, contains the value of the parameter that was read.</param>
		/// <returns>true if the parameter value was read successfully; otherwise, false.</returns>
		private bool ReadParameterValue(IList<byte> readBytes, IDictionary<string, int> labelDict, ParameterDefinition parDef, out long result) {
			if (readBytes == null)
				throw new ArgumentNullException(nameof(readBytes), "The byte sequence cannot be null.");
			if (parDef == null)
				throw new ArgumentNullException(nameof(parDef), "The parameter definition cannot be null.");

			result = 0;

			// Add relative offset.
			int offset;
			switch (parDef.OffsetType) {
			case OffsetType.Start:
				offset = 0;
				break;
			case OffsetType.End:
				offset = readBytes.Count;
				break;
			case OffsetType.Label:
				if (!labelDict.TryGetValue(parDef.RelativeLabel, out offset)) {
					return false;
				}
				break;
			default:
				throw new InvalidDataException("Unrecognized offset type.");
			}
			int bytesNeeded = offset + parDef.Offset + parDef.MinimumByteCount;

			// Add offset to labels.
			labelDict[parDef.Name] = offset + parDef.Offset;

			// Read extra bytes if needed.
			while (readBytes.Count < bytesNeeded) {
				int b = this.ReadNextByte();
				if (b == -1) {
					// Cannot read enough bytes.
					return false;
				}
				readBytes.Add((byte)b);
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