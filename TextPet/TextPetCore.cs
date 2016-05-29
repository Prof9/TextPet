using System;
using System.Collections.Generic;
using System.Linq;
using LibTextPet.Plugins;
using LibTextPet.General;
using LibTextPet.IO.Msg;
using System.Collections.ObjectModel;
using System.IO;
using LibTextPet.Msg;
using LibTextPet.IO.TPL;
using System.Globalization;
using TextPet.Events;
using System.ComponentModel;
using LibTextPet.IO;
using LibTextPet.IO.TextBox;
using System.Text;

namespace TextPet {
	/// <summary>
	/// A single instance of TextPet.
	/// </summary>
	public class TextPetCore {
		/// <summary>
		/// Gets the plugin loader that is used to load plugins.
		/// </summary>
		internal PluginLoader PluginLoader { get; }
		/// <summary>
		/// Gets the currently loaded games.
		/// </summary>
		private KeyedCollection<string, GameInfo> Games { get; }
		/// <summary>
		/// Gets or sets the currently loaded text archives.
		/// </summary>
		public IList<TextArchive> TextArchives { get; }
		/// <summary>
		/// Gets the currently active game.
		/// </summary>
		public GameInfo Game { get; private set; }
		/// <summary>
		/// Gets the currently loaded ROM entries.
		/// </summary>
		public ROMEntryCollection ROMEntries { get; private set; }
		/// <summary>
		/// Gets the currently loaded ROM.
		/// </summary>
		public MemoryStream ROM { get; private set; }

		/// <summary>
		/// Initializes a new TextPet instance.
		/// </summary>
		public TextPetCore() {
			this.PluginLoader = new PluginLoader();
			this.Games = new NamedCollection<GameInfo>();

			this.TextArchives = new List<TextArchive>();
			this.ROMEntries = new ROMEntryCollection();
		}

		/// <summary>
		/// Retrieves the game with the specified name and initializes it, if needed.
		/// </summary>
		/// <param name="name">The game name, case insensitive.</param>
		/// <param name="ignoreUnknownChars">If true, unrecognized characters will be skipped; otherwise, an error will be thrown if one is encountered.</param>
		/// <returns>true if the active game was changed; false if the specified game name was not recognized.</returns>
		public bool SetActiveGame(string name, bool ignoreUnknownChars) {
			if (name == null)
				throw new ArgumentNullException(nameof(name), "The game name cannot be null.");

			name = name.Trim();

			if (name.Length <= 0)
				throw new ArgumentException("The game name cannot be empty.", nameof(name));

			if (!this.Games.Contains(name)) {
				return false;
			}

			GameInfo game = this.Games[name];

			// Initialize the game, if needed.
			if (!game.Loaded) {
				game.Load(this.PluginLoader.Plugins);

				GameInitialized?.Invoke(this, new GameInfoEventArgs(game));
			}
			game.Encoding.IgnoreUnknownChars = ignoreUnknownChars;

			this.Game = game;
			return true;
		}

		/// <summary>
		/// Loads all TextPet plugins from the specified path.
		/// </summary>
		/// <param name="path">The path to load from. Can be a file or a folder.</param>
		/// <param name="recursive">Whether the files should be read recursively, in case of a folder read.</param>
		/// <returns>The plugins that were loaded.</returns>
		public void LoadPlugins(string path, bool recursive) {
			ICollection<string> files = GetReadFiles(path, recursive);
			BeginLoadingPlugins?.Invoke(this, new BeginReadWriteEventArgs(files.ToList(), false));

			// Load the plugins.
			List<IPlugin> loadedPlugins = new List<IPlugin>(files.Count);
			foreach (string file in files) {
				foreach (IPlugin plugin in this.PluginLoader.LoadPlugins(file)) {
					loadedPlugins.Add(plugin);
					LoadedPlugin?.Invoke(this, new PluginsEventArgs(file, plugin));

					// Add the plugin to the set of games if it was a game plugin.
					GameInfo game = plugin as GameInfo;
					if (game != null) {
						this.Games.Add(game);
					}
				}
			}

			FinishedLoadingPlugins?.Invoke(this, new PluginsEventArgs(path, loadedPlugins));
		}

		/// <summary>
		/// Loads all ROM entries from the specified path.
		/// </summary>
		/// <param name="path">The path to load from. Can be a file or folder.</param>
		/// <param name="recursive">Whether the files should be read recursively, in case of a folder read.</param>
		public void LoadROMEntries(string path, bool recursive, bool ignoreSize) {
			ICollection<string> files = GetReadFiles(path, recursive);
			BeginLoadingROMEntries?.Invoke(this, new BeginReadWriteEventArgs(files.ToList(), false));

			// Load the ROM entries.
			ROMEntryCollection entries = new ROMEntryCollection();
			foreach (string file in files) {
				ROMEntryCollection newEntries;
				using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read)) {
					IReader<ROMEntryCollection> reader = new ROMEntriesReader(fs);
					newEntries = reader.Read();
				}

				for (int i = 0; i < newEntries.Count; i++) {
					ROMEntry entry = ((Collection<ROMEntry>)newEntries)[i];

					if (entries.Contains(entry.Offset)) {
						throw new ArgumentException("ROM entry with position 0x" + entry.Offset.ToString("X6", CultureInfo.InvariantCulture) + " has already been loaded.", nameof(path));
					}

					if (ignoreSize) {
						entry.Size = 0;
					}
					entries.Add(entry);
				}

				LoadedROMEntries?.Invoke(this, new ROMEntriesEventArgs(newEntries, file));
			}

			this.ROMEntries = entries;
			FinishedLoadingROMEntries?.Invoke(this, new ROMEntriesEventArgs(entries, path));
		}

		/// <summary>
		/// Clears all currently loaded text archives.
		/// </summary>
		public void ClearTextArchives() {
			this.TextArchives.Clear();
			ClearedTextArchives?.Invoke(this, EventArgs.Empty);
		}

		/// <summary>
		/// A delegate that reads text archives from the specified input stream.
		/// </summary>
		/// <param name="stream">The input stream to read from.</param>
		/// <param name="path">The path of the text archive.</param>
		/// <returns>The text archives that were read.</returns>
		internal delegate IList<TextArchive> TextArchiveFileHandler(MemoryStream stream, string path);

		/// <summary>
		/// Reads text archives from the specified path.
		/// </summary>
		/// <param name="path">The path to read from; can be a file or folder.</param>
		/// <param name="recursive">Whether the files should be read recursively, in case of a folder read.</param>
		/// <param name="readDelegate">The read delegate that reads a text archive with the specified name from the specified stream.</param>
		internal void ReadTextArchives(string path, bool recursive, TextArchiveFileHandler readDelegate) {
			if (path == null)
				throw new ArgumentNullException(nameof(path), "The path cannot be null.");
			if (readDelegate == null)
				throw new ArgumentNullException(nameof(readDelegate), "The read delegate cannot be null.");

			VerifyGameInitialized();

			ICollection<string> files = GetReadFiles(path, recursive);

			BeginReadingTextArchives?.Invoke(this, new BeginReadWriteEventArgs(files.ToList(), false));

			List<TextArchive> readTAs = new List<TextArchive>(files.Count);
			foreach (string file in files) {
				// Begin reading the text archive.
				ReadingTextArchive?.Invoke(this, new TextArchivesEventArgs(file));

				IEnumerable<TextArchive> textArchives;
				using (MemoryStream ms = new MemoryStream()) {
					// Read the entire contents of the file into memory.
					using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read)) {
						fs.CopyTo(ms);
					}

					// Read text archives from the read file using the read delegate.
					ms.Position = 0;
					textArchives = readDelegate(ms, file);
				}

				// Finish reading the text archives.
				foreach (TextArchive ta in textArchives) {
					if (ta == null) {
						throw new InvalidDataException("Could not read one or more text archives.");
					}

					readTAs.Add(ta);
					this.TextArchives.Add(ta);
					ReadTextArchive?.Invoke(this, new TextArchivesEventArgs(file, ta));
				}
			}

			FinishedReadingTextArchives?.Invoke(this, new TextArchivesEventArgs(path, readTAs));
		}

		/// <summary>
		/// Reads binary text archives from the specified ROM file using the currently loaded ROM entries.
		/// </summary>
		/// <param name="file">The path to the ROM file.</param>
		/// <param name="updateEntries">If true, the attributes of any currently loaded ROM entries will be updated after successfully reading the corresponding text archive.</param>
		/// /// <param name="searchPointers">If true, the pointers of any currently loaded ROM entries will be updated after successfully reading the corresponding text archive.</param>
		public void ReadTextArchivesROM(string file, bool updateEntries, bool searchPointers) {
			if (file == null)
				throw new ArgumentNullException(nameof(file), "The file path cannot be null.");
			if (!File.Exists(file))
				throw new ArgumentException("The specified ROM file could not be found.");

			VerifyGameInitialized();

			BeginReadingTextArchives?.Invoke(this, new BeginReadWriteEventArgs(file, false, this.ROMEntries.Count));

			LoadROM(file);

			ROMTextArchiveReader reader = new ROMTextArchiveReader(this.ROM, this.Game, this.ROMEntries);
			reader.UpdateROMEntriesAndIdentifiers = updateEntries;
			reader.SearchPointers = searchPointers;

			List<TextArchive> textArchives = new List<TextArchive>(this.ROMEntries.Count);
			foreach (ROMEntry romEntry in this.ROMEntries) {
				ReadingTextArchive?.Invoke(this, new TextArchivesEventArgs(file, romEntry.Offset));

				this.ROM.Position = romEntry.Offset;
				TextArchive ta = reader.Read();

				if (ta == null) {
					throw new InvalidDataException("Could not read text archive at 0x" + romEntry.Offset.ToString("X6", CultureInfo.InvariantCulture) + ".");
				}

				textArchives.Add(ta);
				this.TextArchives.Add(ta);
				ReadTextArchive?.Invoke(this, new TextArchivesEventArgs(file, romEntry.Offset, ta));
			}

			FinishedReadingTextArchives?.Invoke(this, new TextArchivesEventArgs(file, textArchives));
		}

		/// <summary>
		/// Loads the specified ROM file into memory.
		/// </summary>
		/// <param name="file">The path of the ROM file.</param>
		public void LoadROM(string file) {
			this.ROM = new MemoryStream();
			using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read)) {
				fs.CopyTo(this.ROM);
			}
		}

		/// <summary>
		/// Inserts text boxes from patch text archives read the specified path.
		/// </summary>
		/// <param name="path">The path to load from. Can be a file or folder.</param>
		public void InsertTextArchivesTextBoxes(string path, bool recursive) {
			VerifyGameInitialized();
			CommandDatabase[] databases = this.Game.Databases.ToArray();
			IPatcher<TextArchive> patcher = new TextArchiveTextBoxPatcher(databases);

			ReadTextArchives(path, recursive, delegate (MemoryStream ms, string file) {
				TextBoxTextArchiveTemplateReader reader = new TextBoxTextArchiveTemplateReader(ms, databases);
				NamedCollection<TextArchive> patchedTAs = new NamedCollection<TextArchive>();

				// Read all templates.
				List<TextArchive> patchTAs = new List<TextArchive>();
				while (!reader.AtEnd) {
					patchTAs.AddRange(reader.Read());
				}

				foreach (TextArchive patchTA in patchTAs) {
					if (patchedTAs.Contains(patchTA.Identifier)) {
						throw new InvalidOperationException("Text archive " + patchTA.Identifier + " has already been patched.");
					}

					TextArchive patchedTA = null;
					for (int i = 0; i < this.TextArchives.Count; i++) {
						TextArchive baseTA = this.TextArchives[i];
						if (baseTA.Identifier.Equals(patchTA.Identifier, StringComparison.OrdinalIgnoreCase)) {
							if (patchedTA != null) {
								throw new InvalidOperationException("Cannot patch multiple text archives with identifier " + patchTA.Identifier + ".");
							}
							this.TextArchives.RemoveAt(i--);
							patcher.Patch(baseTA, patchTA);
							patchedTA = baseTA;
						}
					}

					if (patchedTA != null) {
						patchedTAs.Add(patchedTA);
					} else {
						throw new InvalidOperationException("Could not find base text archive with identifier " + patchTA.Identifier + ".");
					}
				}
				return patchedTAs;
			});
		}

		/// <summary>
		/// Writes text archives to the specified path.
		/// </summary>
		/// <param name="path">The path to write to; can be a file or folder.</param>
		/// <param name="extension">The extension to use in case the write path points to a folder.</param>
		/// <param name="single">If true, all text archives are written to a single file; otherwise, each text archive is written to a separate file.</param>
		/// <param name="writeDelegate">The write delegate that writes the specified text archive to the specified stream.</param>
		public void WriteTextArchives(string path, string extension, bool single, Action<MemoryStream, TextArchive> writeDelegate) {
			if (path == null)
				throw new ArgumentNullException(nameof(path), "The path cannot be null.");
			if (writeDelegate == null)
				throw new ArgumentNullException(nameof(writeDelegate), "The write delegate cannot be null.");

			VerifyGameInitialized();

			// Are we writing to a file or a folder?
			bool toFolder = single ? false : IsFolderWrite(path);

			// Create the (containing directory).
			if (toFolder) {
				Directory.CreateDirectory(path);
			} else {
				string directory = Path.GetDirectoryName(path);
				if (directory.Length > 0) {
					Directory.CreateDirectory(directory);
				}
			}

			// Determine the output file paths.
			string[] files = new string[this.TextArchives.Count];
			for (int i = 0; i < this.TextArchives.Count; i++) {
				TextArchive ta = this.TextArchives[i];
				string file;

				if (toFolder) {
					file = Path.Combine(path, ta.Identifier);
					if (extension != null && extension.Length > 0) {
						file = file + "." + extension;
					}
				} else {
					file = path;
				}
				files[i] = file;
			}

			BeginWritingTextArchives?.Invoke(this, new BeginReadWriteEventArgs(files, true));

			List<TextArchive> writtenTAs = new List<TextArchive>(this.TextArchives.Count);
			int succeeded = 0;
			for (int i = 0; i < this.TextArchives.Count; i++) {
				TextArchive ta = this.TextArchives[i];
				string file = files[i];

				// Begin writing the text archive.
				WritingTextArchive?.Invoke(this, new TextArchivesEventArgs(file, ta));

				// Append if this is a single file write and we're past the first file, otherwise create separate files for every text archive.
				bool append = !toFolder && i > 0;

				// Write the text archive using the write delegate, then copy it to the file.
				using (MemoryStream ms = new MemoryStream()) {
					writeDelegate(ms, ta);

					using (FileStream fs = new FileStream(file, append ? FileMode.Append : FileMode.Create, FileAccess.Write, FileShare.None)) {
						fs.Position = fs.Length;
						ms.WriteTo(fs);
					}
				}

				// Finish writing the text archive.
				writtenTAs.Add(ta);
				WroteTextArchive?.Invoke(this, new TextArchivesEventArgs(path, ta));
				succeeded++;
			}
			FinishedWritingTextArchives?.Invoke(this, new TextArchivesEventArgs(path, writtenTAs));
		}

		/// <summary>
		/// Writes binary text archives to the specified path.
		/// </summary>
		/// <param name="path">The path to write to; can be a file or folder.</param>
		public void WriteTextArchivesBinary(string path) {
			WriteTextArchives(path, "msg", false, delegate (MemoryStream ms, TextArchive ta) {
				IWriter<TextArchive> writer = new BinaryTextArchiveWriter(ms, this.Game.Encoding);
				writer.Write(ta);
			});
		}

		/// <summary>
		/// Writes TextPet Language text archives to the specified path.
		/// </summary>
		/// <param name="path">The path to write to; can be a file or folder.</param>
		/// <param name="single">If true, all text archives are written to a single file; otherwise, each text archive is written to a separate file.</param>
		public void WriteTextArchivesTPL(string path, bool single) {
			WriteTextArchives(path, "tpl", single, delegate (MemoryStream ms, TextArchive ta) {
				IWriter<TextArchive> writer = new TPLTextArchiveWriter(ms);
				writer.Write(ta);
			});
		}

		/// <summary>
		/// Writes binary text archives into the specified ROM file.
		/// </summary>
		/// <param name="file">The path to the ROM file.</param>
		/// <param name="freeSpaceOffset">The free space offset to use, or -1 to append to the end of the ROM.</param>
		public void WriteTextArchivesROM(string file, long freeSpaceOffset) {
			if (file == null)
				throw new ArgumentNullException(nameof(file), "The file path cannot be null.");

			VerifyGameInitialized();

			Directory.CreateDirectory(Path.GetDirectoryName(file));

			BeginWritingTextArchives?.Invoke(this, new BeginReadWriteEventArgs(file, true, this.TextArchives.Count));

			using (MemoryStream ms = new MemoryStream()) {
				this.ROM.Position = 0;
				this.ROM.CopyTo(ms);

				// Set up the text archive writer.
				ROMTextArchiveWriter writer = new ROMTextArchiveWriter(ms, this.Game, this.ROMEntries);
				writer.UpdateROMEntriesAndIdentifiers = true;
				if (freeSpaceOffset > 0) {
					writer.FreeSpaceOffset = freeSpaceOffset;
				}

				// Write ALL the text archives!
				foreach (TextArchive ta in this.TextArchives) {
					ROMEntry entry = writer.ROMEntries.GetEntryForTextArchive(ta);
					int offset = entry?.Offset ?? -1;

					WritingTextArchive?.Invoke(this, new TextArchivesEventArgs(file, offset, ta));
					writer.Write(ta);
					WroteTextArchive?.Invoke(this, new TextArchivesEventArgs(file, offset, ta));
				}

				this.ROM = ms;

				// Write the ROM.
				ms.Position = 0;
				using (FileStream fs = new FileStream(file, FileMode.Create, FileAccess.Write, FileShare.None)) {
					ms.WriteTo(fs);
				}
			}

			FinishedWritingTextArchives?.Invoke(this, new TextArchivesEventArgs(file, this.TextArchives));
		}

		/// <summary>
		/// Tests text archive reading by reading binary text archives from the specified path, converting them to various other
		/// formats, then verifying that the input and output are the same. Any failing text archives are not loaded.
		/// </summary>
		/// <param name="path">The path to read from.</param>
		/// <param name="recursive">Whether the files should be read recursively, in case of a folder read.</param>
		public void TestTextArchivesIO(string path, bool recursive) {
			VerifyGameInitialized();

			ICollection<string> files = GetReadFiles(path, recursive);
			BeginTestingTextArchives?.Invoke(this, new BeginReadWriteEventArgs(files.ToList(), false));

			List<TextArchive> testedTAs = new List<TextArchive>();
			ReadTextArchives(path, recursive, delegate (MemoryStream ms, string file) {
				TextArchive ta1, ta2;
				byte[] before = ms.ToArray();
				byte[] after;
				
				// Read the text archive from binary.
				// Could be done by re-using the ReadTextArchiveBinary delegate?
				BinaryTextArchiveReader msgReader = new BinaryTextArchiveReader(ms, this.Game);
				msgReader.IgnorePointerSyncErrors = true;
				ta1 = msgReader.Read((int)ms.Length);
				ta1.Identifier = Path.GetFileNameWithoutExtension(file);
				ta2 = ta1;

				// Could just string a bunch of delegates together?
				using (MemoryStream msTemp = new MemoryStream()) {
					// Write the text archive to TPL.
					// Could be done by re-using the WriteTextArchiveTPL delegate?
					TPLTextArchiveWriter tplWriter = new TPLTextArchiveWriter(msTemp);
					tplWriter.Write(ta2);

					// ...and read it back from TPL.
					// Could be done by re-using the ReadTextArchiveTPL delegate?
					msTemp.Position = 0;
					TPLTextArchiveReader tplReader = new TPLTextArchiveReader(msTemp, this.Game.Databases.ToArray());
					ta2 = tplReader.ReadSingle();
				}

				using (MemoryStream msTemp = new MemoryStream()) {
					// Extract the text archive text boxes.
					TextBoxTextArchiveWriter tbWriter = new TextBoxTextArchiveWriter(msTemp);
					tbWriter.Write(ta2);

					// ...and insert them again.
					msTemp.Position = 0;
					TextBoxTextArchiveTemplateReader tbReader = new TextBoxTextArchiveTemplateReader(msTemp, this.Game.Databases.ToArray());
					TextArchive patchTA = tbReader.ReadSingle();
					TextArchiveTextBoxPatcher patcher = new TextArchiveTextBoxPatcher(this.Game.Databases.ToArray());
					patcher.Patch(ta2, patchTA);
				}

				using (MemoryStream msTemp = new MemoryStream()) {
					// ...then write it to binary again.
					// Could be done by re-using the WriteTextArchiveBinary delegate?
					BinaryTextArchiveWriter msgWriter = new BinaryTextArchiveWriter(msTemp, this.Game.Encoding);
					msgWriter.Write(ta2);

					after = msTemp.ToArray();
				}

				bool passed = ByteSequenceEqualityComparer.Instance.Equals(before, after);

				testedTAs.Add(ta1);
				TestedTextArchive?.Invoke(this, new TestEventArgs(path, ta1, ta2, before, after, passed));

				return new TextArchive[] { ta2 };
			});
			FinishedTestingTextArchives?.Invoke(this, new TextArchivesEventArgs(path, testedTAs));
		}

		/// <summary>
		/// Extracts text boxes from text archives and writes them to the specified path.
		/// </summary>
		/// <param name="path">The path to write to; can be a file or folder.</param>
		/// <param name="single">If true, all text archives are written to a single file; otherwise, each text archive is written to a separate file.</param>
		public void ExtractTextBoxes(string path, bool single) {
			WriteTextArchives(path, "txt", single, delegate (MemoryStream ms, TextArchive ta) {
				IWriter<TextArchive> writer = new TextBoxTextArchiveWriter(ms);
				writer.Write(ta);
			});
		}

		/// <summary>
		/// Gets all files that exist in the specified path.
		/// </summary>
		/// <param name="path">The path. If this is a folder, all files in the folder will be returned.</param>
		/// <param name="recursive">Whether the files should be read recursively, in case of a folder read.</param>
		/// <returns>The files.</returns>
		private static ICollection<string> GetReadFiles(string path, bool recursive) {
			if (path == null)
				throw new ArgumentNullException(nameof(path), "The path cannot be null.");

			string absolutePath = Path.Combine(Directory.GetCurrentDirectory(), path);

			if (Directory.Exists(absolutePath)) {
				return Directory.GetFiles(absolutePath, "*", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
			} else if (File.Exists(absolutePath)) {
				return new string[] { absolutePath };
			} else {
				throw new FileNotFoundException("Could not find the specified file or folder.", absolutePath);
			}
		}

		/// <summary>
		/// Checks if a write to the specified path should result in writing to a folder of multiple files or to a single file.
		/// </summary>
		/// <param name="path">The path to write to.</param>
		/// <returns>true if one or more files are to be written to a folder; false if a single file is to be written to.</returns>
		private bool IsFolderWrite(string path) {
			if (path == null)
				throw new ArgumentNullException(nameof(path), "The path cannot be null.");
			if (path.Length <= 0)
				throw new ArgumentNullException(nameof(path), "The path cannot be empty.");

			// If we have multiple text archives, always output to a directory.
			if (this.TextArchives.Count > 1) {
				return true;
			}

			// If the path ends with \, always treat it as a directory.
			if (path[path.Length - 1] == Path.DirectorySeparatorChar) {
				return true;
			}

			// Otherwise, we have a single text archive; output to a file.
			return false;
		}
		 
		/// <summary>
		/// Verifies whether the active game has been initialized, throwing an exception if it has not.
		/// </summary>
		private void VerifyGameInitialized() {
			if (this.Game == null) {
				throw new InvalidOperationException("No active game has been set.");
			}
		}

		public event EventHandler<BeginReadWriteEventArgs> BeginLoadingPlugins;
		public event EventHandler<PluginsEventArgs> LoadedPlugin;
		public event EventHandler<PluginsEventArgs> FinishedLoadingPlugins;
		public event EventHandler<GameInfoEventArgs> GameInitialized;

		public event EventHandler<BeginReadWriteEventArgs> BeginLoadingROMEntries;
		public event EventHandler<ROMEntriesEventArgs> LoadedROMEntries;
		public event EventHandler<ROMEntriesEventArgs> FinishedLoadingROMEntries;

		public event EventHandler<EventArgs> ClearedTextArchives;

		public event EventHandler<BeginReadWriteEventArgs> BeginReadingTextArchives;
		public event EventHandler<TextArchivesEventArgs> ReadingTextArchive;
		public event EventHandler<TextArchivesEventArgs> ReadTextArchive;
		public event EventHandler<TextArchivesEventArgs> FinishedReadingTextArchives;

		public event EventHandler<BeginReadWriteEventArgs> BeginWritingTextArchives;
		public event EventHandler<TextArchivesEventArgs> WritingTextArchive;
		public event EventHandler<TextArchivesEventArgs> WroteTextArchive;
		public event EventHandler<TextArchivesEventArgs> FinishedWritingTextArchives;

		public event EventHandler<BeginReadWriteEventArgs> BeginTestingTextArchives;
		public event EventHandler<TestEventArgs> TestedTextArchive;
		public event EventHandler<TextArchivesEventArgs> FinishedTestingTextArchives;
	}
}
