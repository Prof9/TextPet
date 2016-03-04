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

			byte[] bytes = new byte[obj.ByteLength];

			// Write the base.
			obj.Definition.Base.CopyTo(bytes, 0);

			// Write the parameters.
			foreach (Parameter par in obj.Parameters) {
				WriteParameterValueToBytes(par.ToInt64(), bytes, par.Definition, 0);
			}

			// Write the length and data parameters, if any.
			if (obj.Definition.HasData) {
				// Write the length parameter.
				WriteParameterValueToBytes(obj.Data.Count - obj.Definition.DataCountOffset,
					bytes, obj.Definition.LengthParameter, 0);

				// Write each data entry.
				IList<int> dataGroupOffsets = obj.DataGroupOffsets;
				for (int i = 0; i < obj.Data.Count; i++) {
					Collection<Parameter> dataEntry = obj.Data[i];

                    foreach (Parameter dataPar in dataEntry) {
						ParameterDefinition def = obj.Definition.DataParameters[dataPar.Name];

						// Calculate the base offset for this parameter.
						int offset = obj.Definition.MinimumLength
							+ dataGroupOffsets[def.DataGroup]
							+ i * obj.Definition.DataEntryLengths[def.DataGroup];

						WriteParameterValueToBytes(dataPar.ToInt64(), bytes, def, offset);
					}
				}
			}

			this.BaseStream.Write(bytes, 0, bytes.Length);
		}

		/// <summary>
		/// Writes the value of a parameter with the given definition to the given byte sequence, from the given offset.
		/// </summary>
		/// <param name="value">The value to write.</param>
		/// <param name="bytes">The byte sequence to write to.</param>
		/// <param name="definition">The parameter definition to use.</param>
		/// <param name="offset">The value to add for the parameter byte offset. Normally, this should be zero, except for data parameters.</param>
		protected static void WriteParameterValueToBytes(long value, IList<byte> bytes, ParameterDefinition definition, int offset) {
			if (bytes == null)
				throw new ArgumentNullException(nameof(bytes), "The byte sequence cannot be null.");
			if (definition == null)
				throw new ArgumentNullException(nameof(definition), "The parameter definition cannot be null.");
			if (value < definition.Minimum || value > definition.Maximum)
				throw new ArgumentOutOfRangeException(nameof(value), value, "The value falls outside the allowed range.");
			if (offset < 0)
				throw new ArgumentOutOfRangeException(nameof(offset), offset, "The offset cannot be negative.");

			// The number of bits to write.
			int bits = definition.Bits;
			// The bit position for writing to the output bytes.
			int outshift = definition.Shift % 8;
			// The byte position for writing to the output bytes.
			offset += definition.Offset + definition.Shift / 8;

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
