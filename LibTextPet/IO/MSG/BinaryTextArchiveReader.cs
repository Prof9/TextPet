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
		private struct ScriptEntry {
			/// <summary>
			/// The script number of this script entry.
			/// </summary>
			public int ScriptNumber;

			/// <summary>
			/// The offset of this script entry.
			/// </summary>
			public int Offset;

			/// <summary>
			/// Creates a new script entry with the specified script number and offset.
			/// </summary>
			/// <param name="scriptNumber">The script number.</param>
			/// <param name="offset">The script offset.</param>
			public ScriptEntry(int scriptNumber, int offset) {
				this.ScriptNumber = scriptNumber;
				this.Offset = offset;
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
			: this(stream, game?.Encoding, game?.Databases.ToArray()) { }

		/// <summary>
		/// Creates a new binary text archive reader that reads from the specified input stream, using the specified encoding and command databases.
		/// </summary>
		/// <param name="stream">The stream to read from.</param>
		/// <param name="encoding">The encoding to use.</param>
		/// <param name="databases">The command databases to use, in order of preference.</param>
		public BinaryTextArchiveReader(Stream stream, CustomFallbackEncoding encoding, params CommandDatabase[] databases)
			: base(stream, true, FileAccess.Read, encoding, databases) {
			this.ScriptReader = new FixedSizeScriptReader(stream, encoding, databases);
			this.IgnorePointerSyncErrors = false;
			this.AutoSortPointers = true;
		}

		/// <summary>
		/// Reads a text archive from the input stream, optionally reading exactly the specified size.
		/// </summary>
		/// <param name="fixedSize">The fixed size of the text archive in bytes, or 0 to read normally.</param>
		/// <returns>The text archive that was read, or null if no text archive could be read.</returns>
		public TextArchive Read(int fixedSize) {
			long start = this.BaseStream.Position;

			// Keep track of script entries.
			List<ScriptEntry> scriptEntries = new List<ScriptEntry>();

			// Keep track of the offset at which the first script starts.
			int firstScriptOffset = int.MaxValue;
			int scriptNum = 0;
			do {
				// Read the next offset.
				int offset = this.ReadOffset();

				// If the offset is invalid, the text archive cannot be read.
				if (offset < 0) {
					return null;
				}

				scriptEntries.Add(new ScriptEntry(scriptNum++, offset));

				// Update the offset of the first script.
				if (offset < firstScriptOffset) {
					firstScriptOffset = offset;
				}
				// Read until the first script is reached.
			} while (this.BaseStream.Position - start < firstScriptOffset);

			// Check if this is a valid text archive.
			if (this.BaseStream.Position - start != firstScriptOffset || firstScriptOffset % 2 != 0) {
				return null;
			}

			// Create the text archive.
			int count = firstScriptOffset / 2;
			// Use the address of the text archive as the ID.
			TextArchive ta = new TextArchive(start.ToString("X6", CultureInfo.InvariantCulture), count);

			// Sort the script entries by offset ascending.
			// OrderBy must be used, as this produces a stable sort.
			if (this.AutoSortPointers) {
				scriptEntries.OrderBy(entry => entry.Offset);
			}

			// Check if the first script pointer is valid.
			if (count > 0 && scriptEntries[0].Offset / 2 != count) {
				return null;
			}

			// Read all scripts.
			bool isUnknownLengthLastScript = false;
			for (int i = 0; i < scriptEntries.Count; i++) {
				ScriptEntry entry = scriptEntries[i];

				// Set the length of the script.
				if (i < count - 1) {
					// Not the last script.
					ScriptEntry next = scriptEntries[i + 1];
					// Set the length to the number of bytes until the next script starts.
					int length = next.Offset - entry.Offset;
					// If the number of bytes is negative, this is an invalid text archive.
					if (length < 0) {
						return null;
					}
					this.ScriptReader.SetFixedLength(length);
				} else if (fixedSize > 0) {
					// Last script.
					// Set the length to the rest of the minimum size.
					int length = fixedSize - entry.Offset;
					if (length < 0) {
						throw new ArgumentException("The size of the text archive exceeds the requested size.", nameof(fixedSize));
					} else {
						// Read to the fixed length.
						this.ScriptReader.SetFixedLength(length);
					}
				} else {
					// Last script.
					// Stop at the first ending script element, or read an empty script if the script is invalid.
					this.ScriptReader.ClearFixedLength();
					isUnknownLengthLastScript = true;
				}

				// Make sure we're at the right position.
				if (this.BaseStream.Position - start != entry.Offset) {
					if (!this.IgnorePointerSyncErrors) {
						throw new InvalidDataException("Text archive reading position is off-sync with script offset. This should not happen in a proper text archive."
							+ "To ignore this error, set the " + nameof(IgnorePointerSyncErrors) + " property to true.");
					} else {
						this.BaseStream.Position = start + entry.Offset;
					}
				}

				// Read the script.
				Script script = this.ScriptReader.Read();

				// Check if the script was valid.
				if (script == null) {
					if (isUnknownLengthLastScript) {
						// Set an empty script with the first database name.
						script = new Script(this.Databases[0].Name);
						this.BaseStream.Position = start + entry.Offset;
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

		/// <summary>
		/// Reads the next script offset from the input stream.
		/// </summary>
		/// <returns>The next script offset, or -1 if no script offset could be read.</returns>
		private int ReadOffset() {
			int lower = this.BaseStream.ReadByte();
			if (lower < 0) {
				return -1;
			}

			int upper = this.BaseStream.ReadByte();
			if (upper < 0) {
				return -1;
			}

			return lower + (upper << 8);
		}
	}
}
