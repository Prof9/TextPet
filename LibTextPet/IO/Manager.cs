using LibTextPet.General;
using LibTextPet.Msg;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;

namespace LibTextPet.IO {
	/// <summary>
	/// An object that reads objects from and/or writes objects to a stream using a command database and encoding.
	/// </summary>
	public abstract class Manager {
		/// <summary>
		/// Gets the base stream that is being read from and/or written to.
		/// </summary>
		public Stream BaseStream { get; }

		/// <summary>
		/// Gets the encoding that is used to parse text.
		/// </summary>
		public Encoding Encoding { get; }

		/// <summary>
		/// Gets the command databases used by this reader.
		/// </summary>
		public ReadOnlyCollection<CommandDatabase> Databases { get; }

		/// <summary>
		/// Creates a manager that reads from and/or writes to the specified stream using UTF-8 encoding.
		/// </summary>
		/// <param name="stream">The stream to read from and/or write to.</param>
		/// <param name="seekable">Whether the stream must be seekable.</param>
		/// <param name="access">The type of access this manager requires.</param>
		protected Manager(Stream stream, bool seekable, FileAccess access)
			: this(stream, seekable, access, null, new CommandDatabase[0]) { }

		/// <summary>
		/// Creates a manager that reads from and/or writes to the specified stream using the specified encoding.
		/// </summary>
		/// <param name="stream">The stream to read from and/or write to.</param>
		/// <param name="seekable">Whether the stream must be seekable.</param>
		/// <param name="access">The type of access this manager requires.</param>
		/// <param name="encoding">The encoding to use, or null to use UTF-8 encoding.</param>
		protected Manager(Stream stream, bool seekable, FileAccess access, Encoding encoding)
			: this(stream, seekable, access, encoding, new CommandDatabase[0]) { }

		/// <summary>
		/// Creates a manager that reads from and/or writes to the specified stream using UTF-8 encoding and the specified command databases.
		/// </summary>
		/// <param name="stream">The stream to read from and/or write to.</param>
		/// <param name="seekable">Whether the stream must be seekable.</param>
		/// <param name="access">The type of access this manager uses.</param>
		/// <param name="databases">The command databases to use.</param>
		protected Manager(Stream stream, bool seekable, FileAccess access, params CommandDatabase[] databases)
			: this(stream, seekable, access, null, databases) { }

		/// <summary>
		/// Creates a manager that reads from and/or writes to the specified stream using the encoding and command databases for the specified game.
		/// </summary>
		/// <param name="stream">The stream to read from and/or write to.</param>
		/// <param name="seekable">Whether the stream must be seekable.</param>
		/// <param name="access">The type of access this manager uses.</param>
		/// <param name="game">The game info to use.</param>
		protected Manager(Stream stream, bool seekable, FileAccess access, GameInfo game)
			: this(stream, seekable, access, VerifyGameLoaded(game).Encoding, VerifyGameLoaded(game).Databases.ToArray()) {
			if (!game.Loaded)
				throw new ArgumentException("");
		}

		/// <summary>
		/// Creates a stream manager that reads from and/or writes to the specified stream using the specified encoding and command databases.
		/// </summary>
		/// <param name="stream">The stream to read from and/or write to.</param>
		/// <param name="seekable">Whether the stream must be seekable.</param>
		/// <param name="access">The type of access this manager requires.</param>
		/// <param name="encoding">The encoding to use, or null to use UTF-8 encoding.</param>
		/// <param name="databases">The command databases to use.</param>
		protected Manager(Stream stream, bool seekable, FileAccess access, Encoding encoding, params CommandDatabase[] databases) {
			if (stream == null)
				throw new ArgumentNullException(nameof(stream), "The stream cannot be null.");
			if (seekable && !stream.CanSeek)
				throw new ArgumentException("The stream does not support seeking.", nameof(stream));
			if (!(access.HasFlag(FileAccess.Read) || access.HasFlag(FileAccess.Write)))
				throw new ArgumentException("File access does not include reading or writing.", nameof(access));
			if (access.HasFlag(FileAccess.Read) && !stream.CanRead)
				throw new ArgumentException("The stream does not support reading.", nameof(stream));
			if (access.HasFlag(FileAccess.Write) && !stream.CanWrite)
				throw new ArgumentException("The stream does not support writing.", nameof(stream));
			if (databases == null)
				throw new ArgumentNullException(nameof(databases), "The command databases cannot be null.");

			foreach (CommandDatabase database in databases) {
				if (database == null) {
					throw new ArgumentException("The command database cannot be null.", nameof(databases));
				}
			}
			
			this.BaseStream = stream;
			this.Encoding = encoding ?? new UTF8Encoding(false, true);
			this.Databases = new ReadOnlyCollection<CommandDatabase>(databases);
		}

		/// <summary>
		/// Finds the command database that matches the specified name, using case-insensitive comparison.
		/// </summary>
		/// <param name="dbName">The database name.</param>
		/// <returns>The command database.</returns>
		protected CommandDatabase FindDatabase(string dbName) {
			foreach (CommandDatabase db in this.Databases) {
				if (db.Name.Equals(dbName, StringComparison.OrdinalIgnoreCase)) {
					return db;
				}
			}
			throw new KeyNotFoundException("No command database found with name \"" + dbName + "\".");
		}

		/// <summary>
		/// Gets a boolean that indicates whether this manager has finished reading the underlying base stream.
		/// </summary>
		public bool AtEnd {
			get {
				return this.BaseStream.Position >= this.BaseStream.Length;
			}
		}

		private static GameInfo VerifyGameLoaded(GameInfo game) {
			if (game == null)
				throw new ArgumentNullException(nameof(game), "The game info cannot be null.");
			if (!game.Loaded)
				throw new ArgumentException("The game plugins have not yet been loaded.", nameof(game));

			return game;
		}
	}
}
