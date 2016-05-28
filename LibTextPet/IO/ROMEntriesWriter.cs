using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace LibTextPet.IO {
	/// <summary>
	/// A reader that writes ROM entries to an output stream.
	/// </summary>
	public class ROMEntriesWriter : Manager, IWriter<ROMEntry>, IWriter<IEnumerable<ROMEntry>>, IDisposable {
		/// <summary>
		/// Gets the text writer that is used to write text to the output stream.
		/// </summary>
		protected TextWriter TextWriter { get; }

		/// <summary>
		/// Gets or sets a boolean that indicates whether this ROM entries writer should write additional comments explaining the format used for ROM entries.
		/// </summary>
		public bool IncludeFormatComments { get; set; }

		/// <summary>
		/// Gets or sets a boolean that indicates whether this ROM entries writer should write additional comments for the gaps between ROM entries.
		/// </summary>
		public bool IncludeGapComments { get; set; }

		/// <summary>
		/// Gets or sets a boolean that indicates whether this ROM entries writer should write additional comments from overlapping ROM entries.
		/// </summary>
		public bool IncludeOverlapComments { get; set; }

		/// <summary>
		/// Gets or sets a boolean that indicates whether this ROM entries writer should write additional comments containing a number of bytes following the end of each ROM entry.
		/// </summary>
		public bool IncludePostBytesComments { get; set; }

		/// <summary>
		/// Creates a new ROM entry writer that writes to the specified input stream.
		/// </summary>
		/// <param name="stream">The stream to write to.</param>
		public ROMEntriesWriter(Stream stream)
			: base(stream, false, FileAccess.Write) {
			this.TextWriter = new StreamWriter(stream, new UTF8Encoding(false, true));
			this.IncludeFormatComments = false;
			this.IncludeGapComments = false;
			this.IncludeOverlapComments = false;
			this.IncludePostBytesComments = false;
		}

		/// <summary>
		/// Writes the specified ROM entry to the output stream.
		/// </summary>
		/// <param name="obj">The ROM entry to write.</param>
		public void Write(ROMEntry obj) {
			if (obj == null) {
				throw new ArgumentNullException(nameof(obj), "The ROM entry cannot be null.");
			}

			this.Write(new ROMEntry[] { obj });
		}

		/// <summary>
		/// Writes the specified ROM entries to the output stream.
		/// </summary>
		/// <param name="obj">The ROM entries to write.</param>
		public void Write(IEnumerable<ROMEntry> obj) {
			this.Write(obj, null);
		}

		/// <summary>
		/// Writes the specified ROM entries to the output stream, optionally writing some bytes from the ROM to aid with debugging.
		/// </summary>
		/// <param name="obj">The ROM entries to write.</param>
		/// <param name="rom">The ROM that the ROM entries correspond to, or null to not use a ROM.</param>
		public void Write(IEnumerable<ROMEntry> obj, Stream rom) {
			// Check parameters.
			if (obj == null) {
				throw new ArgumentNullException(nameof(obj), "The ROM entries cannot be null.");
			}
			if (rom != null) {
				if (!rom.CanRead) {
					throw new ArgumentException("The ROM does not support reading.");
				}
				if (!rom.CanSeek) {
					throw new ArgumentException("The ROM does not support seeking.");
				}
			}
			
			// Sort the ROM entries by offset ascending.
			IList<ROMEntry> sorted = new LinkedList<ROMEntry>(obj.OrderBy(e => e.Offset)).ToList();

			if (this.IncludeFormatComments) {
				this.TextWriter.WriteLine("// offset:&size=pointer,pointer,...");
				this.TextWriter.WriteLine("// & indicates compressed size");
			}

			for (int i = 0; i < sorted.Count; i++) {
				ROMEntry entry = sorted[i];

				// Write preceeding 'gap'.
				if (this.IncludeGapComments && i > 0 && entry.Offset >= 4) {
					ROMEntry prev = sorted[i - 1];
					int prevEnd = prev.Offset + prev.Size;
					int gap = entry.Offset - prevEnd;

					// With a gap up to 4 bytes it's probably just padding.
					if (gap >= 4) {
						this.TextWriter.Write("// gap: 0x");
						this.TextWriter.Write(gap.ToString("X1", CultureInfo.InvariantCulture));
						this.TextWriter.Write(" bytes at 0x");
						this.TextWriter.WriteLine(prevEnd.ToString("X6", CultureInfo.InvariantCulture));
					}
				}

				if (this.IncludeOverlapComments) {
					// Write any overlapping ROM entries.
					IEnumerable<ROMEntry> overlaps = obj.Where(other => entry != other && entry.Overlaps(other));
					if (overlaps.Any()) {
						this.TextWriter.Write("// overlaps with ");
						this.TextWriter.WriteLine(String.Join(", ", overlaps.Select(e => "0x" + e.Offset.ToString("X6", CultureInfo.InvariantCulture))));
					}
				}

				if (this.IncludePostBytesComments && rom != null) {
					// Write some post-ending bytes.
					byte[] buffer = new byte[0x10];
					rom.Position = entry.Offset + entry.Size;
					int read = rom.Read(buffer, 0, buffer.Length);
					this.TextWriter.Write("// post: ");
					this.TextWriter.WriteLine(String.Join(" ", buffer.Take(read).Select(b => b.ToString("X2", CultureInfo.InvariantCulture))));
				}

				// Write the ROM entry.
				this.TextWriter.Write("0x");
				this.TextWriter.Write(entry.Offset.ToString("X6", CultureInfo.InvariantCulture));
				this.TextWriter.Write(':');
				if (entry.Compressed) {
					this.TextWriter.Write('&');
				}
				this.TextWriter.Write(entry.Size);
				this.TextWriter.Write('=');
				this.TextWriter.Write(String.Join(",", entry.Pointers.Select(e => "0x" + e.ToString("X6", CultureInfo.InvariantCulture))));
				if (entry.Pointers.Any(ptr => (ptr & 0x3) != 0)) {
					this.TextWriter.Write(" // CHECK POINTERS!");
				}
				this.TextWriter.WriteLine();
			}

			this.TextWriter.Flush();
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed", MessageId = "<TextWriter>k__BackingField")]
		protected virtual void Dispose(bool disposing) {
			if (disposing) {
				this.TextWriter.Close();
			}
		}

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}
	}
}
