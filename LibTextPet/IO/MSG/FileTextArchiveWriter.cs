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
	/// A writer that writes text archives to a file.
	/// </summary>
	public class FileTextArchiveWriter : FileManager, IWriter<TextArchive> {
		/// <summary>
		/// Gets or sets the current free space offset.
		/// </summary>
		public long FreeSpaceOffset { get; set; }

		/// <summary>
		/// Gets or sets a boolean that indicates this file text archive writer will LZ77 compress text archives.
		/// If set to false, text archives will be LZ77-encoded but not compressed; this is much faster than compressing,
		/// but results in a larger filesize than simply storing the text archive uncompressed.
		/// </summary>
		public bool LZ77Compress { get; set; }

		/// <summary>
		/// Creates a new file text archive writer that writes to the specified output stream.
		/// </summary>
		/// <param name="stream">The stream to write to.</param>
		/// <param name="game">The game info for the output stream.</param>
		/// <param name="fileIndex">The file index to use.</param>
		public FileTextArchiveWriter(Stream stream, GameInfo game, FileIndexEntryCollection fileIndex)
			: base(stream, FileAccess.ReadWrite, game, fileIndex) {
			if (stream == null)
				throw new ArgumentNullException(nameof(stream), "The output stream cannot be null.");

			this.UpdateFileIndex = true;
			this.FreeSpaceOffset = stream.Length;
			this.LZ77Compress = true;
		}

		/// <summary>
		/// Writes the specified text archive to the output stream.
		/// </summary>
		/// <param name="obj">The text archive to write.</param>
		public void Write(TextArchive obj) {
			if (obj == null)
				throw new ArgumentNullException(nameof(obj), "The text archive cannot be null.");

			FileIndexEntry entryOrNull = this.FileIndex.GetEntryForTextArchive(obj);

			if (entryOrNull == null)
				throw new InvalidOperationException("Could not find a matching file index entry for text archive " + obj.Identifier + ".");

			FileIndexEntry entry = (FileIndexEntry)entryOrNull;

			MemoryStream writeStream;
			using (MemoryStream rawStream = new MemoryStream()) {
				if (entry.SizeHeader) {
					// Allocate some space for the size header.
					byte[] buffer = new byte[4];
					rawStream.Write(buffer, 0, buffer.Length);
				}

				new BinaryTextArchiveWriter(rawStream, this.Game.Encoding).Write(obj);

				if (entry.SizeHeader) {
					// Write the size header.
					if (rawStream.Length > 0xFFFFFF) {
						throw new InvalidDataException("The text archive is too large to have a size header.");
					}

					rawStream.Position = 0;
					rawStream.WriteByte(0);
					rawStream.WriteByte((byte)(rawStream.Length & 0xFF));
					rawStream.WriteByte((byte)((rawStream.Length >> 8) & 0xFF));
					rawStream.WriteByte((byte)((rawStream.Length >> 16) & 0xFF));
				}

				// Compress, if necessary.
				// TODO: actual compression lol
				if (entry.Compressed) {
					rawStream.Position = 0;
					writeStream = new MemoryStream(LZ77.GetMaxCompressedSize((int)rawStream.Length));
					if (this.LZ77Compress) {
						LZ77.Compress(rawStream, writeStream, (int)rawStream.Length);
					} else {
						LZ77.Wrap(rawStream, writeStream, (int)rawStream.Length);
					}
				} else {
					writeStream = rawStream;
				}

				// Determine where to write to.
				long offset;
				if (writeStream.Length > entry.Size) {
					// Align to multiple of 4 bytes.
					this.FreeSpaceOffset = (this.FreeSpaceOffset + 3) & ~3;
					offset = this.FreeSpaceOffset;
					this.FreeSpaceOffset += writeStream.Length;
				} else {
					offset = entry.Offset;
				}

				// Update the file index entry and text archive identifier if necessary.
				if (this.UpdateFileIndex) {
					entry.Offset = (int)offset;
					entry.Size = (int)writeStream.Length;
					obj.Identifier = offset.ToString("X6", CultureInfo.InvariantCulture);
				}

				// Expand file, if necessary.
				this.BaseStream.Position = this.BaseStream.Length;
				while (offset > this.BaseStream.Length) {
					this.BaseStream.WriteByte(0xFF);
				}

				// Write the text archive to the file.
				this.BaseStream.Position = offset;
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
