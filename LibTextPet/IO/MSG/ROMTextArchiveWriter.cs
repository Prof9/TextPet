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
	/// A writer that writes text archives to a ROM.
	/// </summary>
	public class ROMTextArchiveWriter : ROMManager, IWriter<TextArchive> {
		/// <summary>
		/// Gets or sets a boolean that indicates whether the currently loaded ROM entries and the identifiers of written text archives will be updated after writing.
		/// </summary>
		public bool UpdateROMEntriesAndIdentifiers { get; set; }

		/// <summary>
		/// Gets or sets the current free space offset.
		/// </summary>
		public long FreeSpaceOffset { get; set; }

		/// <summary>
		/// Creates a new ROM text archive writer that writes to the specified output stream.
		/// </summary>
		/// <param name="stream">The stream to write to.</param>
		/// <param name="game">The game info for the output stream.</param>
		/// <param name="romEntries">The ROM entries of the text archives.</param>
		public ROMTextArchiveWriter(Stream stream, GameInfo game, ROMEntryCollection romEntries)
			: base(stream, FileAccess.ReadWrite, game, romEntries) {
			if (stream == null)
				throw new ArgumentNullException(nameof(stream), "The output stream cannot be null.");

			this.UpdateROMEntriesAndIdentifiers = true;
			this.FreeSpaceOffset = stream.Length;
		}

		/// <summary>
		/// Writes the specified text archive to the output stream.
		/// </summary>
		/// <param name="obj">The text archive to write.</param>
		public void Write(TextArchive obj) {
			if (obj == null)
				throw new ArgumentNullException(nameof(obj), "The text archive cannot be null.");

			ROMEntry? entryOrNull = this.ROMEntries.GetEntryForTextArchive(obj);

			if (entryOrNull == null)
				throw new InvalidOperationException("Could not find a matching ROM entry for text archive " + obj.Identifier + ".");

			ROMEntry entry = (ROMEntry)entryOrNull;

			MemoryStream writeStream;
			using (MemoryStream rawStream = new MemoryStream()) {
				new BinaryTextArchiveWriter(rawStream, this.Game.Encoding).Write(obj);

				// Compress, if necessary.
				// TODO: actual compression lol
				if (entry.Compressed) {
					rawStream.Position = 0;
					writeStream = LZ77.Wrap(rawStream, (int)rawStream.Length);
				} else {
					writeStream = rawStream;
				}

				// Determine where to write to.
				long offset;
				if (writeStream.Length > entry.Size) {
					// Align to multiple of 4 bytes.
					offset = (this.FreeSpaceOffset + 3) & ~3;
					this.FreeSpaceOffset += writeStream.Length;
				} else {
					offset = entry.Offset;
				}

				// Update the ROM entry and text archive identifier if necessary.
				if (this.UpdateROMEntriesAndIdentifiers) {
					entry.Offset = (int)offset;
					entry.Size = (int)writeStream.Length;
					obj.Identifier = offset.ToString("X6", CultureInfo.InvariantCulture);
				}

				// Expand ROM, if necessary.
				this.BaseStream.Position = this.BaseStream.Length;
				while (offset > this.BaseStream.Length) {
					this.BaseStream.WriteByte(0xFF);
				}

				// Write the text archive to the ROM.
				this.BaseStream.Position = offset;
				writeStream.Position = 0;
				writeStream.WriteTo(this.BaseStream);

				// Re-point.
				foreach (int pointerOffset in entry.Pointers) {
					this.BaseStream.Position = pointerOffset;
					uint pointer = this.BinaryReader.ReadUInt32();
					pointer &= unchecked((uint)~0x1FFFFFF);
					pointer |= (uint)(offset & 0x1FFFFFF);
					this.BaseStream.Position = pointerOffset;
					this.BinaryWriter.Write(pointer);
				}
			}
		}
	}
}
