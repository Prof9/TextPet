using LibTextPet.General;
using LibTextPet.Msg;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace LibTextPet.IO.Msg {
	/// <summary>
	/// A reader that reads text archives from a ROM.
	/// </summary>
	public class ROMTextArchiveReader : ROMManager, IReader<TextArchive> {
		/// <summary>
		/// Gets the text archive reader that is used to read text archives from the input stream.
		/// </summary>
		protected BinaryTextArchiveReader TextArchiveReader { get; }

		/// <summary>
		/// Creates a new ROM text archive reader that reads from the specified input stream and uses the specified game info.
		/// </summary>
		/// <param name="stream">The stream to read from.</param>
		/// <param name="game">The game info.</param>
		public ROMTextArchiveReader(Stream stream, GameInfo game, ROMEntryCollection romEntries)
			: base(stream, FileAccess.Read, game, romEntries) {
			this.TextArchiveReader = new BinaryTextArchiveReader(stream, game);
		}

		/// <summary>
		/// Reads a text archive from the current input stream.
		/// </summary>
		/// <returns></returns>
		public TextArchive Read() {
			if (!this.ROMEntries.Contains((long)this.BaseStream.Position))
				throw new InvalidOperationException("Could not find a matching ROM entry for offset 0x" + this.BaseStream.Position.ToString("X6", CultureInfo.InvariantCulture) + ".");

			ROMEntry entry = this.ROMEntries[(long)this.BaseStream.Position];

			if (entry.Offset < 0 || entry.Offset > this.BaseStream.Length) {
				throw new InvalidOperationException("The ROM offset of the current ROM entry lies outside the range of the current input stream.");
			}
			if (entry.Offset + entry.Size > this.BaseStream.Length) {
				throw new InvalidOperationException("The size of the current ROM entry exceeds the number of bytes left in the current input stream.");
			}

			this.BaseStream.Position = entry.Offset;
			TextArchive ta;
			if (entry.Compressed) {
				using (MemoryStream ms = LZ77.Decompress(this.BaseStream)) {
					ms.Position = 0;
					ta = new BinaryTextArchiveReader(ms, this.Game).Read((int)ms.Length);
				}
			} else {
				ta = this.TextArchiveReader.Read(entry.Size);
			}

			ta.Identifier = entry.Offset.ToString("X6", CultureInfo.InvariantCulture);
			return ta;
		}
	}
}
