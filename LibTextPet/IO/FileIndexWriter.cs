using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace LibTextPet.IO {
	/// <summary>
	/// A reader that writes a file index to an output stream.
	/// </summary>
	public class FileIndexWriter : Manager, IWriter<FileIndexEntry>, IWriter<IEnumerable<FileIndexEntry>>, IDisposable {
		/// <summary>
		/// Gets the text writer that is used to write text to the output stream.
		/// </summary>
		protected TextWriter TextWriter { get; }

		/// <summary>
		/// Gets or sets a boolean that indicates whether this file index writer should write additional comments explaining the format used for file index entries.
		/// </summary>
		public bool IncludeFormatComments { get; set; }

		/// <summary>
		/// Gets or sets a boolean that indicates whether this file index writer should write additional comments for the gaps between file index entries.
		/// </summary>
		public bool IncludeGapComments { get; set; }

		/// <summary>
		/// Gets or sets a boolean that indicates whether this file index writer should write additional comments from overlapping file index entries.
		/// </summary>
		public bool IncludeOverlapComments { get; set; }

		/// <summary>
		/// Gets or sets a boolean that indicates whether this file index writer should write additional comments in case a pointer appears to be misaligned.
		/// </summary>
		public bool IncludePointerWarnings { get; set; }

		/// <summary>
		/// Gets or sets a boolean that indicates whether this file index writer should write additional comments containing a number of bytes following the end of each file index entry.
		/// </summary>
		public bool IncludePostBytesComments { get; set; }

		/// <summary>
		/// Gets or sets a value that will be added to the size of all written file index entries.
		/// </summary>
		public int AddSize { get; set; }

		/// <summary>
		/// Gets or sets the byte that will be ignored if a low number of it follows the end of a file index entry.
		/// If set to -1, no bytes will be ignored.
		/// </summary>
		public int ExcludeByte { get; set; }

		/// <summary>
		/// Creates a new file index entry writer that writes to the specified input stream.
		/// </summary>
		/// <param name="stream">The stream to write to.</param>
		public FileIndexWriter(Stream stream)
			: base(stream, false, FileAccess.Write) {
			this.TextWriter = new StreamWriter(stream, new UTF8Encoding(false, true));
			this.IncludeFormatComments = false;
			this.IncludeGapComments = false;
			this.IncludeOverlapComments = false;
			this.IncludePointerWarnings = false;
			this.IncludePostBytesComments = false;
			this.AddSize = 0;
			this.ExcludeByte = -1;
		}

		/// <summary>
		/// Writes the specified file index entry to the output stream.
		/// </summary>
		/// <param name="obj">The file index entry to write.</param>
		public void Write(FileIndexEntry obj) {
			if (obj == null) {
				throw new ArgumentNullException(nameof(obj), "The file index entry cannot be null.");
			}

			this.Write(new FileIndexEntry[] { obj });
		}

		/// <summary>
		/// Writes the specified file index entries to the output stream.
		/// </summary>
		/// <param name="obj">The file index entries to write.</param>
		public void Write(IEnumerable<FileIndexEntry> obj) {
			this.Write(obj, null);
		}

		/// <summary>
		/// Writes the specified file index entries to the output stream, optionally writing some bytes from the file to aid with debugging.
		/// </summary>
		/// <param name="obj">The file index entries to write.</param>
		/// <param name="file">The file that the file index entries correspond to, or null to not use a file.</param>
		public void Write(IEnumerable<FileIndexEntry> obj, Stream file) {
			// Check parameters.
			if (obj == null) {
				throw new ArgumentNullException(nameof(obj), "The file index entries cannot be null.");
			}
			if (file != null) {
				if (!file.CanRead) {
					throw new ArgumentException("The file does not support reading.");
				}
				if (!file.CanSeek) {
					throw new ArgumentException("The file does not support seeking.");
				}
			}
			
			// Sort the file index entries by offset ascending.
			IList<FileIndexEntry> sorted = new LinkedList<FileIndexEntry>(obj.OrderBy(e => e.Offset)).ToList();

			if (this.IncludeFormatComments) {
				this.TextWriter.WriteLine("// offset:&%size=pointer,pointer,...");
				this.TextWriter.WriteLine("// & indicates compressed size, % indicates size header");
			}

			for (int i = 0; i < sorted.Count; i++) {
				FileIndexEntry entry = sorted[i];

				// Write preceeding 'gap'.
				if (this.IncludeGapComments && i > 0 && entry.Offset >= 4) {
					WriteGapComments(sorted, entry);
				}

				if (this.IncludeOverlapComments) {
					// Write any overlapping file index entries.
					WriteOverlapComments(obj, entry);
				}

				// Write the file index entry.
				WriteEntry(entry);

				if (this.IncludePostBytesComments && file != null) {
					WritePostBytes(file, sorted, i, entry);
				}
			}

			this.TextWriter.Flush();
		}

		private void WritePostBytes(Stream file, IList<FileIndexEntry> sorted, int i, FileIndexEntry entry) {
			long startPos = entry.Offset + entry.Size + this.AddSize;

			// Print 16 bytes at most.
			int toPrint = 0x10;

			// Reduce to the number of bytes between this entry and the next entry.
			if (i < sorted.Count - 1) {
				FileIndexEntry next = sorted[i + 1];
				int gap = next.Offset - (entry.Offset + entry.Size + this.AddSize);
				if (gap >= 0 && gap < toPrint) {
					toPrint = gap;
				}
			}

			// Abort if there are no bytes to print.
			if (toPrint <= 0) {
				return;
			}

			// Read the bytes to print.
			byte[] buffer = new byte[toPrint];
			file.Position = startPos;
			int read = file.Read(buffer, 0, buffer.Length);

			// Check if we should exclude these bytes.
			bool exclude = false;

			// Only exclude if there are less than 4 bytes.
			if (read < 4) {
				exclude = true;
				for (int j = 0; j < read; j++) {
					// If any byte differs from the exclusion byte, don't exclude.
					if (buffer[j] != this.ExcludeByte) {
						exclude = false;
						break;
					}
				}
			}

			// Write some post-ending bytes.
			if (read > 0 && !exclude) {
				this.TextWriter.Write("// ");
				this.TextWriter.Write(startPos.ToString("X8", CultureInfo.InvariantCulture));
				this.TextWriter.Write(new string(' ', 8));

				this.TextWriter.WriteLine(String.Join(" ", buffer.Take(read).Select(b => b.ToString("X2", CultureInfo.InvariantCulture))));

				this.TextWriter.WriteLine();
			}
		}

		private void WriteGapComments(IList<FileIndexEntry> sorted, FileIndexEntry entry) {
			int prevEnd = sorted.TakeWhile(e => e.Offset < entry.Offset).Max(e => e.Offset + e.Size + this.AddSize);
			int gap = entry.Offset - prevEnd;

			// With a gap up to 3 bytes it's probably just padding.
			if (gap >= 4) {
				this.TextWriter.Write("// gap: 0x");
				this.TextWriter.Write(gap.ToString("X1", CultureInfo.InvariantCulture));
				this.TextWriter.Write(" bytes at 0x");
				this.TextWriter.WriteLine(prevEnd.ToString("X6", CultureInfo.InvariantCulture));
			}
		}

		private void WriteOverlapComments(IEnumerable<FileIndexEntry> obj, FileIndexEntry entry) {
			IEnumerable<FileIndexEntry> overlaps = obj.Where(other => entry != other && entry.Overlaps(other));
			if (overlaps.Any()) {
				this.TextWriter.Write("// overlaps with ");
				this.TextWriter.WriteLine(String.Join(", ", overlaps.Select(e => "0x" + e.Offset.ToString("X6", CultureInfo.InvariantCulture))));
			}
		}

		private void WriteEntry(FileIndexEntry entry) {
			this.TextWriter.Write("0x");
			this.TextWriter.Write(entry.Offset.ToString("X6", CultureInfo.InvariantCulture));
			this.TextWriter.Write(':');
			if (entry.Compressed) {
				this.TextWriter.Write('&');
			}
			if (entry.SizeHeader) {
				this.TextWriter.Write('%');
			}
			this.TextWriter.Write("0x");
			this.TextWriter.Write((entry.Size + this.AddSize).ToString("X1", CultureInfo.InvariantCulture));
			this.TextWriter.Write('=');
			this.TextWriter.Write(String.Join(",", entry.Pointers.Select(e => "0x" + e.ToString("X6", CultureInfo.InvariantCulture))));
			if (this.IncludePointerWarnings && entry.Pointers.Any(ptr => (ptr & 0x3) != 0)) {
				this.TextWriter.Write(" // CHECK POINTERS!");
			}
			this.TextWriter.WriteLine();
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
