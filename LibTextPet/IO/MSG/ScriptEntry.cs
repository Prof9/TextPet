using System;

namespace LibTextPet.IO.Msg {
	/// <summary>
	/// A script entry that contains the script's number and offset.
	/// </summary>
	public struct ScriptEntry {
		/// <summary>
		/// The script number of this script entry.
		/// </summary>
		public int ScriptNumber { get; set; }

		/// <summary>
		/// The stream position of this script entry.
		/// </summary>
		public long Position { get; set; }

		private int _size;
		/// <summary>
		/// The fixed size of this script entry, or -1 if there is no fixed size.
		/// </summary>
		public int Size {
			get => this._size;
			set {
				if (value < -1) {
					throw new ArgumentOutOfRangeException(nameof(value), value, "Size cannot be less than -1.");
				}
				this._size = value;
			}
		}

		/// <summary>
		/// Creates a new script entry with the specified script number.
		/// </summary>
		/// <param name="scriptNumber">The script number.</param>
		public ScriptEntry(int scriptNumber) {
			this.ScriptNumber = scriptNumber;
			this.Position = 0;
			this._size = -1;
		}
	}
}
