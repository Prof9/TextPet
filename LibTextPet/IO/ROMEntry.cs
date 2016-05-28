using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace LibTextPet.IO {
	/// <summary>
	/// A ROM entry for a single text archive.
	/// </summary>
	public struct ROMEntry {
		/// <summary>
		/// Gets the ROM offset of the text archive.
		/// </summary>
		public int Offset { get; set; }
		/// <summary>
		/// Gets the (compressed) size of the text archive.
		/// </summary>
		public int Size { get; set; }
		/// <summary>
		/// Gets a boolean that indicates whether the text archive is compressed.
		/// </summary>
		public bool Compressed { get; }
		/// <summary>
		/// Gets the offsets of the pointers pointing to the text archive.
		/// </summary>
		public ReadOnlyCollection<int> Pointers { get; }

		/// <summary>
		/// Creates a new ROM entry with the specified offset, size, compression and pointer offsets.
		/// </summary>
		/// <param name="offset">The ROM offset of the text archive.</param>
		/// <param name="size">The (compressed) size of the text archive.</param>
		/// <param name="compressed">Whether the text archive is compressed.</param>
		/// <param name="pointers">The offsets of the pointers to the text archive.</param>
		public ROMEntry(int offset, int size, bool compressed, IEnumerable<int> pointers) {
			if (offset < 0)
				throw new ArgumentOutOfRangeException(nameof(offset), offset, "The ROM offset cannot be negative.");
			if (size < 0)
				throw new ArgumentOutOfRangeException(nameof(size), size, "The size cannot be negative.");
			if (pointers == null)
				throw new ArgumentNullException(nameof(pointers), "The pointer offsets cannot be null.");

			this.Offset = offset;
			this.Size = size;
			this.Compressed = compressed;
			this.Pointers = new ReadOnlyCollection<int>(pointers.Distinct().ToList());
		}

		/// <summary>
		/// Checks whether this ROM entry overlaps another ROM entry.
		/// </summary>
		/// <param name="other">The ROM entry to compare against.</param>
		/// <returns>true if the ROM entries overlap; otherwise, false.</returns>
		public bool Overlaps(ROMEntry other) {
			return Math.Max(this.Offset, other.Offset) < Math.Min(this.Offset + this.Size, other.Offset + other.Size);
		}

		public override bool Equals(object obj) {
			if (obj == null || GetType() != obj.GetType())
				return false;

			ROMEntry entry = (ROMEntry)obj;

			if (!(this.Offset == entry.Offset && this.Size == entry.Size && this.Compressed == entry.Compressed)) {
				return false;
			}

			if (this.Pointers.Count != entry.Pointers.Count) {
				return false;
			}

			if (this.Pointers.Union(entry.Pointers).Distinct().Count() != this.Pointers.Count) {
				return false;
			}

			return true;
		}

		public override int GetHashCode() {
			return this.Offset.GetHashCode() ^ this.Size.GetHashCode() ^ this.Compressed.GetHashCode() ^ this.Pointers.GetHashCode();
		}

		public static bool operator ==(ROMEntry entry1, ROMEntry entry2) {
			if (ReferenceEquals(entry1, entry2)) {
				return true;
			} else if (ReferenceEquals(entry1, null) || ReferenceEquals(entry2, null)) {
				return false;
			} else {
				return entry1.Equals(entry2);
			}
		}

		public static bool operator !=(ROMEntry token1, ROMEntry token2) {
			return !(token1 == token2);
		}
	}
}
