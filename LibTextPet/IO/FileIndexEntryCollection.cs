using LibTextPet.General;
using LibTextPet.Msg;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace LibTextPet.IO {
	/// <summary>
	/// A collection of file index entries for text archives, indexed by their offsets.
	/// </summary>
	public class FileIndexEntryCollection : KeyedCollection<long, FileIndexEntry> {
		private Regex OffsetRegex;

		/// <summary>
		/// Creates a new empty file index.
		/// </summary>
		public FileIndexEntryCollection()
			: base() {
			this.OffsetRegex = new Regex("^[0-9A-Fa-f]{6,8}$", RegexOptions.Compiled);
		}

		protected override long GetKeyForItem(FileIndexEntry item) {
			if (item == null)
				throw new ArgumentNullException(nameof(item), "The item cannot be null.");

			// long is used instead of int to not overlap with this[int].
			return item.Offset;
		}

		/// <summary>
		/// Gets the file index entry for the specified text archive, or null if no such entry exists.
		/// </summary>
		/// <param name="identifier">The text archive.</param>
		/// <returns>The corresponding file index entry, or null if no matching entry was found.</returns>
		public FileIndexEntry GetEntryForTextArchive(TextArchive textArchive) {
			if (textArchive == null)
				throw new ArgumentNullException(nameof(textArchive), "The text archive cannot be null.");

			return GetEntryForTextArchive(textArchive.Identifier);
		}

		/// <summary>
		/// Gets the file index entry for the text archive with the specified identifier, or null if no such entry exists.
		/// </summary>
		/// <param name="identifier">The identifier of the text archive.</param>
		/// <returns>The corresponding file index entry, or null if no matching entry was found.</returns>
		public FileIndexEntry GetEntryForTextArchive(string identifier) {
			if (identifier == null)
				throw new ArgumentNullException(nameof(identifier), "The text archive identifier cannot be null.");
			if (String.IsNullOrWhiteSpace(identifier))
				throw new ArgumentException("The text archive identifier cannot consist only of whitespace.", nameof(identifier));

			bool parsed = false;
			int offset;
			if (this.OffsetRegex.IsMatch(identifier)) {
				parsed = Int32.TryParse(identifier, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out offset);
			} else {
				parsed = NumberParser.TryParseInt32(identifier, out offset);
			}

			if (parsed && this.Contains(offset)) {
				return this[(long)offset];
			} else {
				// Could not parse identifier or no entry available.
				return null;
			}
		}
	}
}
