using LibTextPet.General;
using LibTextPet.Msg;
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
		/// Creates a binary command reader that reads from the specified input stream.
		/// </summary>
		/// <param name="stream">The stream to read from.</param>
		/// <param name="database">The command database to use.</param>
		public BinaryCommandReader(Stream stream, CommandDatabase database) 
			: base(stream, true, FileAccess.Read, database) { }

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
			byte[] buffer = new byte[definition.Base.Count];
			if (this.BaseStream.Read(buffer, 0, buffer.Length) < buffer.Length) {
				// Cannot read enough bytes.
				return null;
			}
			IList<byte> bytes = new List<byte>(buffer);

			// Verify base bytes.
			for (int i = 0; i < definition.Base.Count; i++) {
				if ((bytes[i] & definition.Mask[i]) != definition.Base[i]) {
					// Base mismatch; return null.
					return null;
				}
			}

			// Read length.
			long length = 0;
			if (definition.HasData) {
				length = ReadParameterValueFromBytes(bytes, definition.LengthParameter, 0) + definition.DataCountOffset;

				// If length is invalid, the command cannot be read.
				if (length <= 0) {
					return null;
				}

				buffer = new byte[length * definition.TotalDataEntryLength];
				if (this.BaseStream.Read(buffer, 0, buffer.Length) < buffer.Length) {
					// Cannot read enough bytes.
					return null;
				}
				foreach (byte b in buffer) {
					bytes.Add(b);
				}
			}

			// Read parameters.
			foreach (ParameterDefinition parDef in definition.Parameters) {
				long value = ReadParameterValueFromBytes(bytes, parDef, 0);

				// If the parameter value is not in range, the command is invalid.
				if (!cmd.Parameters[parDef.Name].InRange(value)) {
					return null;
				}

				cmd.Parameters[parDef.Name].SetInt64(value);
			}

			// Read data parameters.
			if (definition.HasData) {
				// Create empty data entries.
				for (int i = 0; i < length; i++) {
					cmd.Data.Add(cmd.Data.CreateDefaultEntry());
				}
				
				// Calculate the offsets for the data groups.
				IList<int> dataGroupOffsets = cmd.CalculateDataGroupOffsets();

				// Read all data entries.
				for (int i = 0; i < length; i++) {
					// Read every data parameter in the entry.
					foreach (ParameterDefinition parDef in definition.DataParameters) {
						// Calculate the base offset for this parameter.
						int offset = definition.MinimumLength
							+ dataGroupOffsets[parDef.DataGroup]
							+ i * definition.DataEntryLengths[parDef.DataGroup];

						long value = ReadParameterValueFromBytes(bytes, parDef, offset);

						// If the data parameter value is not in range, the command is invalid.
						if (!cmd.Data[i][parDef.Name].InRange(value)) {
							return null;
						}

                        cmd.Data[i][parDef.Name].SetInt64(ReadParameterValueFromBytes(bytes, parDef, offset));
					}
				}
			}

			// Perform rewind.
			this.BaseStream.Position -= definition.RewindCount;

			return cmd;
		}

		/// <summary>
		/// Reads the value of a parameter with the given definition from the given byte sequence, from the given offset.
		/// </summary>
		/// <param name="bytes">The byte sequence to read from.</param>
		/// <param name="definition">The parameter definition to use.</param>
		/// <param name="offset">The value to add for the parameter byte offset. Normally, this should be zero, except for data parameters.</param>
		/// <returns>The value that was read.</returns>
		protected static long ReadParameterValueFromBytes(IList<byte> bytes, ParameterDefinition definition, int offset) {
			if (bytes == null)
				throw new ArgumentNullException(nameof(bytes), "The byte sequence cannot be null.");
			if (definition == null)
				throw new ArgumentNullException(nameof(definition), "The parameter definition cannot be null.");
			if (offset < 0)
				throw new ArgumentOutOfRangeException(nameof(offset), offset, "The offset cannot be negative.");

			long value = 0;

			// The number of bits to read.
			int bits = definition.Bits;
			// The bit position for writing to the output value.
			int outshift = 0;
			// The bit position for reading from the input bytes.
			int inshift = definition.Shift % 8;
			// The byte position for reading from the input bytes.
			offset += definition.Offset + definition.Shift / 8;

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

			return value + definition.ExtensionBase;
		}
	}
}
