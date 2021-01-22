using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LibTextPet.General;
using LibTextPet.IO.Msg;
using LibTextPet.Msg;

namespace TextPet {
	/// <summary>
	/// A reader that reads text archives from Mega Man Star Force 3 MSG files.
	/// </summary>
	public class MMSF3TextArchiveReader : BinaryTextArchiveReader {
		/// <summary>
		/// Gets or sets a boolean that indicates whether strict header checks will be performed. If false, only the header string (" GSM") will be checked. By default, this is false.
		/// </summary>
		public bool StrictHeaderChecks { get; set; }

		/// <summary>
		/// Gets the base stream that is being read from.
		/// </summary>
		public new Stream BaseStream => this.EncryptedStream;

		/// <summary>
		/// Gets the stream that holds the encrypted text archive.
		/// </summary>
		protected Stream EncryptedStream { get; }
		/// <summary>
		/// Gets the stream that holds the decrypted text archive.
		/// </summary>
		protected Stream DecryptedStream => base.BaseStream;

		/// <summary>
		/// Gets the list of script entries.
		/// </summary>
		private List<ScriptEntry> ScriptEntries { get; }

		public MMSF3TextArchiveReader(Stream stream, GameInfo game)
			: base(new MemoryStream(), game) {
			this.EncryptedStream = stream;
			this.ScriptEntries = new List<ScriptEntry>();
			this.StrictHeaderChecks = false;
		}

		public override TextArchive Read(long fixedSize) {
			// Reset decrypted stream.
			this.DecryptedStream.Position = 0;

			// Read header.
			byte[] headerBuffer = new byte[12];
			if (this.EncryptedStream.Read(headerBuffer, 0, headerBuffer.Length) != headerBuffer.Length) {
				return null;
			}

			// Check header string " GSM".
			if (headerBuffer[0] != 0x20 || headerBuffer[1] != 0x47 ||
				headerBuffer[2] != 0x53 || headerBuffer[3] != 0x4D) {
				return null;
			}
			// Check fixed bytes 00 01.
			if (this.StrictHeaderChecks && (headerBuffer[4] != 0x00 || headerBuffer[5] != 0x01)) {
				return null;
			}

			// Read script count.
			int scriptCount = headerBuffer[6] | (headerBuffer[7] << 8);
			int maxScriptSize = ((headerBuffer[8] | (headerBuffer[9] << 8)) + 1) * 2;

			// Check fixed bytes FF FF.
			if (this.StrictHeaderChecks && (headerBuffer[10] | (headerBuffer[11] << 8)) != 0xFFFF) {
				return null;
			}

			// Read bytes for script entries.
			byte[] scriptEntriesBuffer = new byte[scriptCount * 4];
			if (this.EncryptedStream.Read(scriptEntriesBuffer, 0, scriptEntriesBuffer.Length) != scriptEntriesBuffer.Length) {
				return null;
			}
			int headerEndOffset = (int)this.EncryptedStream.Position;

			// Read script entries.
			this.ScriptEntries.Clear();
			bool encounteredMaxScriptSize = false;
			for (int i = 0; i < scriptCount; i++) {
				ScriptEntry entry = new ScriptEntry() {
					ScriptNumber = i,
					// Adjust position for new stream.
					Position =  (scriptEntriesBuffer[i * 4 + 0] | (scriptEntriesBuffer[i * 4 + 1] << 8)),
					Size     = ((scriptEntriesBuffer[i * 4 + 2] | (scriptEntriesBuffer[i * 4 + 3] << 8)) + 1) * 2
				};
				this.ScriptEntries.Add(entry);

				// Check that position is valid.
				if (entry.Position < 0) {
					return null;
				}
				// Check that script size does not exceed max size set in header.
				if (this.StrictHeaderChecks && entry.Size > maxScriptSize) {
					return null;
				}

				encounteredMaxScriptSize |= entry.Size == maxScriptSize;
			}
			// Check that we actually encountered the max script size set in header.
			if (this.StrictHeaderChecks && !encounteredMaxScriptSize) {
				return null;
			}

			// Decrypt the scripts.
			int scriptsStart = (int)(this.ScriptEntries.Min(entry => entry.Position) - this.EncryptedStream.Position);
			int scriptsEnd   = (int)(this.ScriptEntries.Max(entry => entry.Position + entry.Size) - this.EncryptedStream.Position);
			if (this.StrictHeaderChecks && scriptsStart != 0 || this.EncryptedStream.Position + scriptsEnd != this.EncryptedStream.Length) {
				return null;
			}

			// Decrypt the scripts in one big block instead of going by script entries,
			// otherwise we might re-encrypt parts of a script when there is overlap.
			byte[] scriptsBuffer = new byte[this.EncryptedStream.Length - this.EncryptedStream.Position];
			if (this.EncryptedStream.Read(scriptsBuffer, 0, scriptsBuffer.Length) != scriptsBuffer.Length) {
				return null;
			}
			for (int i = scriptsStart; i < scriptsEnd; i++) {
				scriptsBuffer[i] ^= 0x55;
			}

			// Write to decrypted stream.
			this.DecryptedStream.Write(headerBuffer, 0, headerBuffer.Length);
			this.DecryptedStream.Write(scriptEntriesBuffer, 0, scriptEntriesBuffer.Length);
			this.DecryptedStream.Write(scriptsBuffer, 0, scriptsBuffer.Length);
			this.DecryptedStream.SetLength(this.DecryptedStream.Position);

			// Use original reader.
			this.DecryptedStream.Position = headerBuffer.Length + scriptEntriesBuffer.Length + scriptsStart;
			return base.Read(fixedSize);
		}

		protected override IList<ScriptEntry> ReadScriptEntries(long fixedSize) {
			// We already parsed these, so return them.
			return this.ScriptEntries;
		}
	}
}
