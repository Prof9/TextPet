using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace LibTextPet.IO {
	/// <summary>
	/// A file index entry for a single text archive inside a larger file.
	/// </summary>
	public class FileIndexEntry {
		/// <summary>
		/// Gets the offset of the text archive in the file.
		/// </summary>
		public int Offset { get; set; }
		/// <summary>
		/// Gets the (compressed) size of the text archive.
		/// </summary>
		public int Size { get; set; }
		/// <summary>
		/// Gets or sets a boolean that indicates whether the text archive is compressed.
		/// </summary>
		public bool Compressed { get; set; }
		/// <summary>
		/// Gets or sets a boolean that indicates whether the text archive contains a size header.
		/// </summary>
		public bool SizeHeader { get; set; }
		/// <summary>
		/// Gets or sets the offsets of the pointers pointing to the text archive.
		/// </summary>
		public ICollection<int> Pointers { get; }

		/// <summary>
		/// Creates a new file index entry with the specified offset.
		/// </summary>
		/// <param name="offset">The offset of the text archive in the file.</param>
		public FileIndexEntry(int offset) 
			: this(offset, 0, false, false, new int[0]) { }

		/// <summary>
		/// Creates a new file index entry with the specified offset, size, compression and pointer offsets.
		/// </summary>
		/// <param name="offset">The offset of the text archive in the file.</param>
		/// <param name="size">The (compressed) size of the text archive.</param>
		/// <param name="compressed">Whether the text archive is compressed.</param>
		/// <param name="sizeHeader">Whether the text archive has a size header.</param>
		/// <param name="pointers">The offsets of the pointers to the text archive.</param>
		public FileIndexEntry(int offset, int size, bool compressed, bool sizeHeader, IEnumerable<int> pointers) {
			if (offset < 0)
				throw new ArgumentOutOfRangeException(nameof(offset), offset, "The file offset cannot be negative.");
			if (size < 0)
				throw new ArgumentOutOfRangeException(nameof(size), size, "The size cannot be negative.");
			if (pointers == null)
				throw new ArgumentNullException(nameof(pointers), "The pointer offsets cannot be null.");

			this.Offset = offset;
			this.Size = size;
			this.Compressed = compressed;
			this.SizeHeader = sizeHeader;
			this.Pointers = pointers.Distinct().ToList();
		}

		/// <summary>
		/// Checks whether this file index entry overlaps another file index entry.
		/// </summary>
		/// <param name="other">The file index entry to compare against.</param>
		/// <returns>true if the file entries overlap; otherwise, false.</returns>
		public bool Overlaps(FileIndexEntry other) {
			if (other == null)
				throw new ArgumentNullException(nameof(other), "The other file index entry cannot be null.");

			return Math.Max(this.Offset, other.Offset) < Math.Min(this.Offset + this.Size, other.Offset + other.Size);
		}

		public override bool Equals(object obj) {
			if (obj == null || GetType() != obj.GetType())
				return false;

			FileIndexEntry entry = (FileIndexEntry)obj;

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

		public static bool operator ==(FileIndexEntry entry1, FileIndexEntry entry2) {
			if (ReferenceEquals(entry1, entry2)) {
				return true;
			} else if (entry1 == null || entry2 == null) {
				return false;
			} else {
				return entry1.Equals(entry2);
			}
		}

		public static bool operator !=(FileIndexEntry token1, FileIndexEntry token2) {
			return !(token1 == token2);
		}
	}
}
