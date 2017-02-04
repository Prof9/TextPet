using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LibTextPet.Msg;
using System.Text.RegularExpressions;
using System.IO;
using LibTextPet.General;

namespace LibTextPet.IO {
	/// <summary>
	/// A reader that reads a file index from an input stream.
	/// </summary>
	public class FileIndexReader : Manager, IReader<FileIndexEntryCollection>, IDisposable {
		/// <summary>
		/// Gets the text reader that is used to read text from the input stream.
		/// </summary>
		protected TextReader TextReader { get; }

		/// <summary>
		/// Gets the regex that is used for removing comments, whitespace and carriage returns.
		/// </summary>
		private Regex StripRegex { get; }
		/// <summary>
		/// Gets the regex that is used for matching a single line.
		/// </summary>
		private Regex EntryRegex { get; }

		/// <summary>
		/// Creates a new file index reader that reads from the specified input stream.
		/// </summary>
		/// <param name="stream">The stream to read from.</param>
		public FileIndexReader(Stream stream)
			: base(stream, false, FileAccess.Read) {
			this.TextReader = new StreamReader(stream);

			this.StripRegex = new Regex(
				@"[^\S\n]+|(//|;|#)[^\r\n]*?(?=\r?$)|/\*(.|\n)*?(\*/|$(?!\r?\n))",
				RegexOptions.Multiline | RegexOptions.Compiled
			);
			this.EntryRegex = new Regex(
				@"^[\dA-Za-z$#]+:&?%?[\dA-Za-z$#]+=([\dA-Za-z$#]+,)*([\dA-Za-z$#]+)?$",
				RegexOptions.Compiled
			);
		}

		/// <summary>
		/// Reads a file index from the input stream.
		/// </summary>
		/// <returns>The file index that was read.</returns>
		public FileIndexEntryCollection Read() {
			string fulltext = this.TextReader.ReadToEnd();
			return Read(fulltext);
		}

		/// <summary>
		/// Reads a file index entries from the specified full text.
		/// </summary>
		/// <param name="fullText">The full text.</param>
		/// <returns>The file index that was read.</returns>
		public FileIndexEntryCollection Read(string fullText) {
			if (fullText == null)
				throw new ArgumentNullException(nameof(fullText), "The full text cannot be null.");

			fullText = this.StripRegex.Replace(fullText, "");
			string[] lines = fullText.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

			FileIndexEntryCollection fileIndex = new FileIndexEntryCollection();

			foreach (string line in lines) {
				fileIndex.Add(ReadEntry(line));
			}

			return fileIndex;
		}

		/// <summary>
		/// Reads a single file index entry from the specified entry string. The entry string must be correctly formatted.
		/// </summary>
		/// <param name="entry">The entry string to read from.</param>
		/// <returns>The file index entry that was read.</returns>
		protected FileIndexEntry ReadEntry(string entry) {
			if (entry == null)
				throw new ArgumentNullException(nameof(entry), "The entry string cannot be null.");
			if (!this.EntryRegex.IsMatch(entry))
				throw new ArgumentException("Could not parse \"" + entry + "\" as a file index entry.", nameof(entry));

			int colonPos = entry.IndexOf(':');
			int equalsPos = entry.IndexOf('=');
			bool compressed = entry.Contains('&');
			bool sizeHeader = entry.Contains('%');

			int sizeStart = colonPos + 1 + (compressed ? 1 : 0) + (sizeHeader ? 1 : 0);

			string offsetString = entry.Substring(0, colonPos);
			string sizeString = entry.Substring(sizeStart, equalsPos - sizeStart);
			string[] pointerStrings = entry.Substring(equalsPos + 1).Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

			int offset = NumberParser.ParseInt32(offsetString);
			int size = NumberParser.ParseInt32(sizeString);
			int[] pointers = new int[pointerStrings.Length];
			for (int i = 0; i < pointers.Length; i++) {
				pointers[i] = NumberParser.ParseInt32(pointerStrings[i]);
			}

			return new FileIndexEntry(offset, size, compressed, sizeHeader, pointers);
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed", MessageId = "<TextReader>k__BackingField")]
		protected virtual void Dispose(bool disposing) {
			if (disposing) {
				this.TextReader.Close();
			}
		}

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}
	}
}
