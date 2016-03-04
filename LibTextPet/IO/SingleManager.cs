using LibTextPet.Msg;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LibTextPet.IO {
	/// <summary>
	/// A stream manager that uses a single command database.
	/// </summary>
	public abstract class SingleManager : Manager {
		/// <summary>
		/// Gets the command database that is used by this manager.
		/// </summary>
		public CommandDatabase Database {
			get {
				return this.Databases[0];
			}
		}

		/// <summary>
		/// Creates a manager that reads from and/or writes to the specified stream using the specified encoding and command database.
		/// </summary>
		/// <param name="stream">The stream to read from and/or write to.</param>
		/// <param name="seekable">Whether the stream must be seekable.</param>
		/// <param name="access">The type of access this manager uses.</param>
		/// <param name="encoding">The encoding to use, or null to use UTF-8 encoding.</param>
		/// <param name="database">The command database to use.</param>
		protected SingleManager(Stream stream, bool seekable, FileAccess access, Encoding encoding, CommandDatabase database)
			: base(stream, seekable, access, encoding, database) { }

		/// <summary>
		/// Creates a manager that reads from and/or writes to the specified stream using UTF-8 encoding and the specified command database.
		/// </summary>
		/// <param name="stream">The stream to read from and/or write to.</param>
		/// <param name="seekable">Whether the stream must be seekable.</param>
		/// <param name="access">The type of access this manager uses.</param>
		/// <param name="database">The command database to use.</param>
		protected SingleManager(Stream stream, bool seekable, FileAccess access, CommandDatabase database)
			: base(stream, seekable, access, database) { }
	}
}
