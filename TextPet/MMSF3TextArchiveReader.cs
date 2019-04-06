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

		public MMSF3TextArchiveReader(MemoryStream stream, GameInfo game)
			: base(stream, game) {
			// Enforce MemoryStream so we don't accidentally write to files when decrypting scripts.
			this.StrictHeaderChecks = false;
		}

		protected override IList<ScriptEntry> ReadScriptEntries(long fixedSize) {
			long start = this.BaseStream.Position;

			// Read header.
			byte[] headerBuffer = new byte[12];
			if (this.BaseStream.Read(headerBuffer, 0, headerBuffer.Length) != headerBuffer.Length) {
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
			if (this.BaseStream.Read(scriptEntriesBuffer, 0, scriptEntriesBuffer.Length) != scriptEntriesBuffer.Length) {
				return null;
			}
			int headerEndOffset = (int)(this.BaseStream.Position - start);

			// Read script entries.
			List<ScriptEntry> scriptEntries = new List<ScriptEntry>(scriptCount);
			bool encounteredMaxScriptSize = false;
			for (int i = 0; i < scriptCount; i++) {
				ScriptEntry entry = new ScriptEntry() {
					ScriptNumber = i,
					// Adjust position for new stream.
					Position = (scriptEntriesBuffer[i * 4 + 0] | (scriptEntriesBuffer[i * 4 + 1] << 8)) - headerEndOffset,
					Size = ((scriptEntriesBuffer[i * 4 + 2] | (scriptEntriesBuffer[i * 4 + 3] << 8)) + 1) * 2
				};
				scriptEntries.Add(entry);

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
			if (!this.DecryptScripts(scriptEntries, headerEndOffset)) {
				return null;
			}

			return scriptEntries;
		}

		private bool DecryptScripts(IList<ScriptEntry> scriptEntries, int headerEndOffset) {
			int scriptsStartOffset = (int)scriptEntries.Min(entry => entry.Position);
			int bufferLength = (int)scriptEntries.Max(entry => entry.Position + entry.Size);

			// Decrypt the scripts in one big block instead of going by script entries,
			// otherwise we might re-encrypt parts of a script when there is overlap.
			byte[] scriptsBuffer = new byte[bufferLength];
			if (this.BaseStream.Read(scriptsBuffer, 0, bufferLength) != bufferLength) {
				return false;
			}
			for (int i = scriptsStartOffset; i < bufferLength; i++) {
				scriptsBuffer[i] ^= 0x55;
			}

			// MemoryStream should be transient, we can just write into it.
			this.BaseStream.SetLength(bufferLength);
			this.BaseStream.Position = 0;
			this.BaseStream.Write(scriptsBuffer, 0, bufferLength);
			this.BaseStream.Position = 0;

			return true;
		}
	}
}
