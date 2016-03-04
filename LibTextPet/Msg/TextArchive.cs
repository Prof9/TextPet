using LibTextPet.General;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace LibTextPet.Msg {
	/// <summary>
	/// A text archive containing scripts.
	/// </summary>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix")]
	public class TextArchive : Collection<Script> {
		private string identifier;
		/// <summary>
		/// Gets or sets the identifier for this text archive. This must be non-null, non-empty and cannot contain any whitespace.
		/// </summary>
		public string Identifier {
			get {
				return this.identifier;
			}
			set {
				if (value == null)
					throw new ArgumentNullException(nameof(value), "The new identifier cannot be null.");
				if (value.Any(x => Char.IsWhiteSpace(x)))
					throw new ArgumentException("The new identifier cannot contain whitespace.", nameof(value));

				this.identifier = value;
			}
		}

		/// <summary>
		/// Creates a new empty text archive with a random identifier.
		/// </summary>
		public TextArchive()
			: this(RandomID(), new List<Script>()) { }

		/// <summary>
		/// Creates a new empty text archive with the specified identifier.
		/// </summary>
		/// <param name="id">The identifier.</param>
		public TextArchive(string id)
			: this(id, new List<Script>()) { }

		/// <summary>
		/// Creates a new text archive with a random identifier, containing the specified scripts.
		/// </summary>
		/// <param name="scripts">The scripts to be contained in the text archive.</param>
		public TextArchive(IList<Script> scripts)
			: this(RandomID(), scripts) { }

		/// <summary>
		/// Creates a new text archive with a random identifier and the specified size.
		/// </summary>
		/// <param name="size">The size of the text archive.</param>
		public TextArchive(int size)
			: this(RandomID(), size) { }

		/// <summary>
		/// Creates a new text archive with the specified identifier and size.
		/// </summary>
		/// <param name="id">The identifier.</param>
		/// <param name="size">The size of the text archive.</param>
		public TextArchive(string id, int size)
			: this(id, new List<Script>()) {
			// Fill the text archive with empty scripts.
			while (this.Count < size) {
				this.Add(new Script());
			}
		}

		/// <summary>
		/// Creates a new text archive with the specified identifier, containing the specified scripts.
		/// </summary>
		/// <param name="id">The identifier.</param>
		/// <param name="scripts">The scripts to be contained in the text archive.</param>
		public TextArchive(string id, IList<Script> scripts)
			: base(scripts) {
			if (id == null)
				throw new ArgumentNullException(nameof(id), "The identifier cannot be null.");
			if (string.IsNullOrWhiteSpace(id))
				throw new ArgumentException("The identifier cannot consist only of whitespace.", nameof(id));

			this.Identifier = id;
		}

		/// <summary>
		/// Generates a random identifier for text archives.
		/// </summary>
		/// <returns>The identifier.</returns>
		private static string RandomID() {
			return Guid.NewGuid().ToString("N").ToUpperInvariant();
		}

		/// <summary>
		/// Resizes this text archive to the specified size. Any scripts past the new size are discarded.
		/// </summary>
		/// <param name="size">The new size of the text archive.</param>
		public void Resize(int size) {
			if (size < 0)
				throw new ArgumentOutOfRangeException(nameof(size), size, "The new size cannot be negative.");
			
			// Size up.
			while (this.Count < size) {
				this.Add(new Script());
			}
			// Size down.
			while (this.Count > size) {
				this.RemoveAt(this.Count - 1);
			}
		}

		/// <summary>
		/// Resizes this text archive to remove trailing empty scripts.
		/// </summary>
		public void Trim() {
			this.Trim(0);
		}

		/// <summary>
		/// Resizes this text archive to remove trailing empty scripts, down to the specified minimum value.
		/// </summary>
		/// <param name="minimum">The minimum new size of this text archive.</param>
		public void Trim(int minimum) {
			if (minimum < 0)
				throw new ArgumentOutOfRangeException(nameof(minimum), minimum, "The minimum size cannot be negative.");
			if (minimum > this.Count)
				throw new ArgumentOutOfRangeException(nameof(minimum), minimum, "The minimum size cannot be greater than the current size.");

			int newSize = this.Count;
			while (this.Count > minimum) {
				if (this[newSize - 1].Count > 0) {
					break;
				}
				newSize--;
			}

			this.Resize(newSize);
		}
	}
}