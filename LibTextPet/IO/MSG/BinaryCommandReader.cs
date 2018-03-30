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
		private LookupTreePath<MaskedByte, CommandDefinition> cmdDefPath;
		private Dictionary<string, int> labelDict;

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
		public BinaryCommandReader(Stream stream, CommandDatabase database, IgnoreFallbackEncoding encoding) 
			: base(stream, true, FileAccess.Read, database) {
			if (database == null)
				throw new ArgumentNullException(nameof(database), "The command database cannot be null.");

			this.TextReader = new ConservativeStreamReader(stream, encoding);

			this.byteSequence = new List<byte>();
			this.stringBuilder = new StringBuilder();
			this.charBuffer = new char[this.TextReader.GetMaxCharCount(1)];
			this.byteBuffer = new byte[this.TextReader.GetMaxByteCount(1)];
			this.cmdDefPath = database.BytesToCommandLookup.BeginPath();
			this.labelDict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		}

		/// <summary>
		/// Reads the next script command from the input stream. The stream is advanced even if no valid script command was read.
		/// </summary>
		/// <returns>The next script command read from the input stream, or null if no script command was read.</returns>
		public Command Read() {
			this.cmdDefPath.Reset();
			this.byteSequence.Clear();
			long start = this.BaseStream.Position;

			int bytesRead = 0;
			CommandDefinition matchDef = null;
			CommandDefinition priorityDef = null;
			int priorityLength = -1;
			while (true) {
				// Cancel if end of stream reached.
				int b = this.BaseStream.ReadByte();
				if (b == -1) {
					return null;
				}
				this.byteSequence.Add((byte)b);
				bytesRead++;

				// Step on current byte.
				if (!this.cmdDefPath.Step(new MaskedByte(b, 0xFF))) {
					// Stop if no more matches.
					break;
				}

				// Did we reach a potential command?
				if (this.cmdDefPath.AtValue) {
					matchDef = this.cmdDefPath.CurrentValue;

					// Does this command take priority at a lower length than the current priority?
					if (matchDef.PriorityLength > 0 && (priorityLength < 0 || matchDef.PriorityLength < priorityLength)) {
						priorityDef = matchDef;
						priorityLength = matchDef.PriorityLength;
					}
				}

				if (this.cmdDefPath.AtEnd) {
					// Stop if end reached.
					break;
				}
			}

			// Did we find a priority match?
			if (priorityLength >= 0) {
				matchDef = priorityDef;
			}

			// Did we find a match?
			if (matchDef == null) {
				return null;
			}

			// If match found, read it.
			return this.ReadFromDefinition(matchDef);
		}

		/// <summary>
		/// Reads the next script command from the input stream with the given command definition.
		/// </summary>
		/// <param name="definition">The command definition.</param>
		/// <param name="bytesRead">The amount of bytes that were already read.</param>
		/// <returns>The command read.</returns>
		private Command ReadFromDefinition(CommandDefinition definition) {
			if (definition == null)
				throw new ArgumentNullException(nameof(definition), "The command definition cannot be null.");

			Command cmd = new Command(definition);

			this.labelDict.Clear();

			// Read (rest of) base bytes.
			int byteCount = definition.Base.Count;
			while (this.byteSequence.Count < byteCount) {
				int b = this.BaseStream.ReadByte();
				if (b == -1) {
					// Cannot read enough bytes.
					return null;
				}
				this.byteSequence.Add((byte)b);
			}

			// Verify base bytes.
			for (int i = 0; i < byteCount; i++) {
				if (!CommonBitsEqualityComparer.Instance.Equals(definition.Base[i], this.byteSequence[i])) {
					// Base mismatch; return null.
					return null;
				}
			}

			// Rewind if we already read too many bytes.
			if (this.byteSequence.Count > byteCount) {
				int overflow = this.byteSequence.Count - byteCount;
				this.byteSequence.RemoveRange(byteCount, overflow);
				this.BaseStream.Position -= overflow;
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
					int b = this.BaseStream.ReadByte();
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
						break;
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
				if (this.BaseStream.Read(buffer, 0, buffer.Length) != buffer.Length) {
					// Could not read the required bytes.
					return false;
				}
				foreach (byte b in buffer) {
					readBytes.Add(b);
				}

				// Expand char buffer if necessary.
				int maxCharCount = this.TextReader.Encoding.GetMaxCharCount(strLen);
				if (this.charBuffer == null || this.charBuffer.Length < maxCharCount) {
					this.charBuffer = new char[maxCharCount];
				}

				// Decode the string.
				this.TextReader.Encoding.ResetFallbackCount();
				int charCount = this.TextReader.Encoding.GetChars(buffer, 0, strLen, this.charBuffer, 0);

				if (this.TextReader.Encoding.FallbackCount != 0) {
					// Could not properly decode the string.
					return false;
				}
				this.stringBuilder.Append(this.charBuffer, 0, charCount);
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
			if (readBytes.Count < bytesNeeded) {
				byte[] buffer = new byte[bytesNeeded - readBytes.Count];
				if (this.BaseStream.Read(buffer, 0, buffer.Length) != buffer.Length) {
					// Cannot read enough bytes.
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
