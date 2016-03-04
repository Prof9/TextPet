using LibTextPet.General;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LibTextPet.IO.Msg {
	/// <summary>
	/// A manager that reads from and/or writes to a ROM.
	/// </summary>
	public class ROMManager : Manager, IDisposable {
		/// <summary>
		/// Gets the binary reader that is used to read bytes from the base stream.
		/// </summary>
		protected BinaryReader BinaryReader { get; }

		/// <summary>
		/// Gets the binary writer that is used to write bytes to the base stream.
		/// </summary>
		protected BinaryWriter BinaryWriter { get; }

		/// <summary>
		/// Gets the game info for the base stream.
		/// </summary>
		protected GameInfo Game { get; }

		/// <summary>
		/// Gets the ROM entries for reading/writing text archives from/to the base stream.
		/// </summary>
		public ROMEntryCollection ROMEntries { get; }

		/// <summary>
		/// Creates a new ROM manager that reads to and/or writes from the specified stream.
		/// </summary>
		/// <param name="stream">The stream to read from or write to.</param>
		/// <param name="access">The type of access this manager requires.</param>
		/// <param name="game">The game info to use.</param>
		/// <param name="romEntries">The ROM entries to use.</param>
		public ROMManager(Stream stream, FileAccess access, GameInfo game, ROMEntryCollection romEntries)
			: base(stream, true, access, game) {
			if (romEntries == null)
				throw new ArgumentNullException(nameof(romEntries), "The ROM entries cannot be null.");

			if (access.HasFlag(FileAccess.Read)) {
				this.BinaryReader = new BinaryReader(stream);
			}
			if (access.HasFlag(FileAccess.Write)) {
				this.BinaryWriter = new BinaryWriter(stream);
			}

			this.Game = game;
			this.ROMEntries = romEntries;
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed", MessageId = "<BinaryReader>k__BackingField")]
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed", MessageId = "<BinaryWriter>k__BackingField")]
		protected virtual void Dispose(bool disposing) {
			if (disposing) {
				this.BinaryReader?.Dispose();
				this.BinaryWriter?.Dispose();
			}
		}

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}
	}
}
