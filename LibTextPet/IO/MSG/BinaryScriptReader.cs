using LibTextPet.General;
using LibTextPet.Msg;
using LibTextPet.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LibTextPet.IO.Msg {
	/// <summary>
	/// A binary script reader that reads a script from an input stream.
	/// </summary>
	public class BinaryScriptReader : ScriptReader<BinaryCommandReader> {
		/// <summary>
		/// Gets the starting position of the last script read or the script currently being read from the input stream.
		/// </summary>
		public long StartPosition { get; private set; }

		/// <summary>
		/// Creates a new binary script reader that reads from the specified input stream, using the specified game info.
		/// </summary>
		/// <param name="stream">The stream to read from.</param>
		/// <param name="game">The game info to use.</param>
		public BinaryScriptReader(Stream stream, GameInfo game)
			: this(stream, game?.Encoding, game?.Databases.ToArray()) { }

		/// <summary>
		/// Creates a new binary script reader that reads from the specified input stream, using the specified encoding and command databases.
		/// </summary>
		/// <param name="stream">The stream to read from.</param>
		/// <param name="encoding">The encoding to use.</param>
		/// <param name="databases">The command databases to use, in order of preference.</param>
		public BinaryScriptReader(Stream stream, Encoding encoding, params CommandDatabase[] databases)
			: base(stream, encoding, CreateCommandReaders(stream, encoding, databases)) { }

		/// <summary>
		/// Reads a script from the current input stream.
		/// </summary>
		/// <returns>The script that was read.</returns>
		public override Script Read() {
			// Save the start position of the current script to keep track of the running length.
			this.StartPosition = this.BaseStream.Position;

			return base.Read();
		}

		/// <summary>
		/// Reads the next fallback element from the input stream.
		/// </summary>
		/// <returns>A fallback script element.</returns>
		protected override IScriptElement ReadFallback() {
			return new ByteElement((byte)this.BaseStream.ReadByte());
		}

		/// <summary>
		/// Creates command readers with the specified command databases that read from the specified input stream.
		/// </summary>
		/// <param name="stream">The stream to read from.</param>
		/// <param name="databases">The command databases to use.</param>
		/// <param name="encoding">The encoding to use.</param>
		/// <returns>The resulting command readers.</returns>
		private static BinaryCommandReader[] CreateCommandReaders(Stream stream, Encoding encoding, params CommandDatabase[] databases) {
			if (databases == null)
				throw new ArgumentNullException(nameof(databases), "The command databases cannot be null.");

			BinaryCommandReader[] readers = new BinaryCommandReader[databases.Length];
			for (int i = 0; i < databases.Length; i++) {
				readers[i] = new BinaryCommandReader(stream, databases[i], encoding);
			}

			return readers;
		}
	}
}
