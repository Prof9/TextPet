using LibTextPet.General;
using LibTextPet.Msg;
using LibTextPet.Text;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace LibTextPet.IO.Msg {
	/// <summary>
	/// A binary text archive reader that reads a text archive from an input stream.
	/// </summary>
	public class BinaryTextArchiveReader : Manager, IReader<TextArchive> {
		/// <summary>
		/// A script entry that contains the script's number and offset.
		/// </summary>
		protected struct ScriptEntry {
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
				set => this._size = value >= -1 ? value : throw new ArgumentOutOfRangeException(nameof(value), value, "Size cannot be less than -1.");
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

		/// <summary>
		/// Gets the script reader that is used to read scripts from the input stream.
		/// </summary>
		public FixedSizeScriptReader ScriptReader { get; private set; }

		/// <summary>
		/// Gets or sets a boolean that indicates whether errors caused by the next script pointer being off-sync with the stream position should be ignored. By default, this is false.
		/// </summary>
		public bool IgnorePointerSyncErrors { get; set; }

		/// <summary>
		/// Gets or sets a boolean that indicates whether script pointers are automatically sorted. If the script pointers are not sorted and a text archive is read where the script pointers are not in ascending order, the text archive is deemed to be invalid. By default, this is true.
		/// </summary>
		public bool AutoSortPointers { get; set; }

		/// <summary>
		/// Creates a new binary text archive reader that reads from the specified input stream, using the specified game info.
		/// </summary>
		/// <param name="stream">The stream to read from.</param>
		/// <param name="game">The game info to use.</param>
		public BinaryTextArchiveReader(Stream stream, GameInfo game)
			: base(stream, true, FileAccess.Read, game?.Encoding, game?.Databases.ToArray()) {
			this.ScriptReader = new FixedSizeScriptReader(stream, game?.Encoding, game?.Databases.ToArray());
			this.IgnorePointerSyncErrors = false;
			this.AutoSortPointers = true;
		}

		/// <summary>
		/// Reads script entries from the input stream.
		/// </summary>
		/// <param name="fixedSize">The fixed size of the text archive in bytes, or 0 to read normally.</param>
		/// <returns>The script entries that were read, or null if the script entries were invalid.</returns>
		protected virtual IList<ScriptEntry> ReadScriptEntries(long fixedSize) {
			long start = this.BaseStream.Position;

			// Keep track of script entries.
			List<ScriptEntry> scriptEntries = new List<ScriptEntry>();

			// Keep track of the offset at which the first script starts.
			int firstScriptOffset = int.MaxValue;
			int scriptNum = 0;
			do {
				// Read the next offset.
				int offset = this.BaseStream.ReadByte() | (this.BaseStream.ReadByte() << 8);

				// If the offset is invalid, the text archive cannot be read.
				if (offset < 0 || (fixedSize > 0 && offset > fixedSize)) {
					return null;
				}

				scriptEntries.Add(new ScriptEntry(scriptNum) {
					Position = offset
				});

				// Update the offset of the first script.
				if (offset < firstScriptOffset) {
					firstScriptOffset = offset;
				}

				scriptNum++;
				// Read until the first script is reached.
			} while (this.BaseStream.Position - start < firstScriptOffset);

			// List and sort script offsets.
			List<int> scriptOffsets = scriptEntries
				.Select(entry => (int)entry.Position)
				.Distinct()
				.ToList();
			scriptOffsets.Sort();
			bool[] scriptOffsetsUsed = new bool[scriptOffsets.Count];

			// Calculate script lengths, resolve offsets.
			for (int i = scriptEntries.Count - 1; i >= 0; i--) {
				ScriptEntry entry = scriptEntries[i];

				int offsetIdx = scriptOffsets.BinarySearch((int)entry.Position);
				if (scriptOffsetsUsed[offsetIdx]) {
					// A script with higher script number already uses this offset.
					entry.Size = 0;
				} else if (offsetIdx == scriptOffsets.Count - 1) {
					// Last script; length may not exceed fixed size.
					entry.Size = fixedSize > 0 ? (int)(fixedSize - entry.Position) : -1;
				} else {
					// Calculate length from next script offset.
					entry.Size = scriptOffsets[offsetIdx + 1] - (int)entry.Position;
				}
				scriptOffsetsUsed[offsetIdx] = true;

				entry.Position += start;
				scriptEntries[i] = entry;
			}

			return scriptEntries;
		}

		/// <summary>
		/// Reads a text archive from the input stream, optionally reading exactly the specified size.
		/// </summary>
		/// <param name="fixedSize">The fixed size of the text archive in bytes, or 0 to read normally.</param>
		/// <returns>The text archive that was read, or null if no text archive could be read.</returns>
		public TextArchive Read(long fixedSize) {
			long start = this.BaseStream.Position;

			// Load script entries.
			IList<ScriptEntry> scriptEntries = this.ReadScriptEntries(fixedSize);
			if (scriptEntries == null) {
				return null;
			}

			// Create the text archive.
			// Use the address of the text archive as the ID.
			TextArchive ta = new TextArchive(start.ToString("X6", CultureInfo.InvariantCulture), scriptEntries.Count);

			// Sort the script entries by offset ascending.
			// OrderBy must be used, as this produces a stable sort.
			// If pointers are not sorted, pointer sync errors may occur when reading scripts.
			if (this.AutoSortPointers) {
				scriptEntries.OrderBy(entry => entry.Position);
			}

			// Read all scripts. Note: i may not correspond with script number if pointers are sorted.
			for (int i = 0; i < scriptEntries.Count; i++) {
				ScriptEntry entry = scriptEntries[i];

				// Set the length of the script.
				if (entry.Size == -1) {
					this.ScriptReader.ClearFixedLength();
				} else {
					this.ScriptReader.SetFixedLength(entry.Size);
				}

				// Make sure we're at the right position.
				if (this.BaseStream.Position != entry.Position) {
					if (!this.IgnorePointerSyncErrors) {
						// Text archive reading position is off-sync with script offset; this should not happen in a proper text archive.
						return null;
					} else {
						this.BaseStream.Position = entry.Position;
					}
				}

				// Read the script.
				Script script = this.ScriptReader.Read();

				// Check if the script was valid.
				if (script == null) {
					// Check if this was the last script and size was unknown.
					// If this is the case, this was likely an empty script.
					if (i == scriptEntries.Count - 1 && entry.Size == -1) {
						// Set an empty script with the first database name.
						script = new Script(this.Databases[0].Name);
						this.BaseStream.Position = entry.Position;
					} else {
						return null;
					}
				}

				ta[entry.ScriptNumber] = script;
			}

			return ta;
		}

		/// <summary>
		/// Reads a text archive from the input stream, and stops reading after the ending element of the last script.
		/// </summary>
		/// <returns>The text archive that was read.</returns>
		public TextArchive Read() {
			return this.Read(0);
		}
	}
}
