using LibTextPet.General;
using LibTextPet.Msg;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace LibTextPet.IO {
	/// <summary>
	/// A ROM index consisting of ROM entries for text archives, indexed by their ROM offsets.
	/// </summary>
	public class ROMEntryCollection : KeyedCollection<long, ROMEntry> {
		private Regex IdentifierRegex;

		/// <summary>
		/// Creates a new empty ROM index.
		/// </summary>
		public ROMEntryCollection()
			: base() {
			this.IdentifierRegex = new Regex("^[0-9A-Fa-f]+$", RegexOptions.Compiled);
		}

		protected override long GetKeyForItem(ROMEntry item) {
			// long is used instead of int to not overlap with this[int].
			return item.Offset;
		}

		/// <summary>
		/// Gets the ROM entry for the specified text archive, or null if no such ROM entry exists.
		/// </summary>
		/// <param name="identifier">The text archive.</param>
		/// <returns>The corresponding ROM entry, or null if no matching ROM entry was found.</returns>
		public ROMEntry GetEntryForTextArchive(TextArchive textArchive) {
			if (textArchive == null)
				throw new ArgumentNullException(nameof(textArchive), "The text archive cannot be null.");

			return GetEntryForTextArchive(textArchive.Identifier);
		}

		/// <summary>
		/// Gets the ROM entry for the text archive with the specified identifier, or null if no such ROM entry exists.
		/// </summary>
		/// <param name="identifier">The identifier of the text archive.</param>
		/// <returns>The corresponding ROM entry, or null if no matching ROM entry was found.</returns>
		public ROMEntry GetEntryForTextArchive(string identifier) {
			if (identifier == null)
				throw new ArgumentNullException(nameof(identifier), "The text archive identifier cannot be null.");
			if (String.IsNullOrWhiteSpace(identifier))
				throw new ArgumentException("The text archive identifier cannot consist only of whitespace.", nameof(identifier));

			int offset;
			if (this.IdentifierRegex.IsMatch(identifier)) {
				offset = Convert.ToInt32(identifier, 16);
			} else if (!NumberParser.TryParseInt32(identifier, out offset)) {
				// Cannot parse identifier as offset, so no ROM entry available.
				return null;
			}

			if (this.Contains(offset)) {
				return this[(long)offset];
			} else {
				return null;
			}
		}
	}
}
