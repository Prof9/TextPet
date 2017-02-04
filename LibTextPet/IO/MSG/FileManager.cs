using LibTextPet.General;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LibTextPet.IO.Msg {
	/// <summary>
	/// A manager that reads from and/or writes to a file.
	/// </summary>
	public class FileManager : Manager, IDisposable {
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
		/// Gets the file index for reading/writing text archives from/to the base stream.
		/// </summary>
		public FileIndexEntryCollection FileIndex { get; }

		/// <summary>
		/// Gets or sets a boolean that indicates whether the currently loaded file index and the identifiers of written text archives will be updated after writing.
		/// </summary>
		public bool UpdateFileIndex { get; set; }

		/// <summary>
		/// Creates a new file index manager that reads to and/or writes from the specified stream.
		/// </summary>
		/// <param name="stream">The stream to read from or write to.</param>
		/// <param name="access">The type of access this manager requires.</param>
		/// <param name="game">The game info to use.</param>
		/// <param name="fileIndex">The file index to use.</param>
		public FileManager(Stream stream, FileAccess access, GameInfo game, FileIndexEntryCollection fileIndex)
			: base(stream, true, access, game) {
			if (fileIndex == null)
				throw new ArgumentNullException(nameof(fileIndex), "The file index cannot be null.");

			if (access.HasFlag(FileAccess.Read)) {
				this.BinaryReader = new BinaryReader(stream);
			}
			if (access.HasFlag(FileAccess.Write)) {
				this.BinaryWriter = new BinaryWriter(stream);
			}

			this.Game = game;
			this.FileIndex = fileIndex;
			this.UpdateFileIndex = true;
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
