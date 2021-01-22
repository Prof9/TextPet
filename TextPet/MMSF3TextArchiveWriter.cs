using System;
using System.Collections.Generic;
using System.IO;
using LibTextPet.General;
using LibTextPet.IO.Msg;
using LibTextPet.Msg;

namespace TextPet {
	/// <summary>
	/// A writer that writes text archives to Mega Man Star Force 3 MSG files.
	/// </summary>
	public class MMSF3TextArchiveWriter : BinaryTextArchiveWriter {
		/// <summary>
		/// Gets the base stream that is being written to.
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

		public MMSF3TextArchiveWriter(Stream stream, GameInfo game)
			: base(new MemoryStream(), game?.Encoding) {
			this.EncryptedStream = stream;
		}

		public override void Write(TextArchive obj) {
			if (obj == null)
				throw new ArgumentNullException(nameof(obj), "The text archive cannot be null.");

			long start = this.EncryptedStream.Position;

			// Pad the stream where the header and pointers go.
			byte[] buffer = new byte[4];
			for (int i = 0; i < 3 + obj.Count; i++) {
				this.EncryptedStream.Write(buffer, 0, 4);
			}

			// Keep track of the script offsets and sizes.
			ushort[] offsets = new ushort[obj.Count];
			ushort[] sizes   = new ushort[obj.Count];

			// Write all scripts.
			ushort maxScriptSize = 0;
			for (int i = 0; i < obj.Count; i++) {
				// Set the script offset.
				long offset = this.BaseStream.Position - start;
				if (offset > 0xFFFF) {
					throw new ArgumentException("The text archive is too large.", nameof(obj));
				}
				offsets[i] = (ushort)offset;

				// Write the decrypted script.
				this.DecryptedStream.Position = 0;
				Script script = obj[i];
#if !DEBUG
				try {
#endif
					this.ScriptWriter.Write(script);
#if !DEBUG
				} catch (EncoderFallbackException ex) {
					throw new InvalidDataException("Could not encode character " + ex.CharUnknown + " in script " + i + " of text archive " + obj.Identifier + ".", ex);
				}
#endif
				// Pad to minimum size (2)
				while (this.DecryptedStream.Position < 2) {
					this.DecryptedStream.WriteByte(0);
				}

				// Get size of script.
				long scriptEnd  = this.DecryptedStream.Position;
				long scriptSize = (scriptEnd - 1) / 2;
				if (scriptSize > 0xFFFF) {
					throw new ArgumentException("The text archive is too large.", nameof(obj));
				}
				sizes[i] = (ushort)scriptSize;
				if (scriptSize > maxScriptSize) {
					maxScriptSize = (ushort)scriptSize;
				}

				// Encrypt and copy to stream.
				this.DecryptedStream.Position = 0;
				while (this.DecryptedStream.Position < scriptEnd) {
					int b = this.DecryptedStream.ReadByte();
					if (b < -1) {
						throw new IOException("Error reading from decrypted stream.");
					}

					this.EncryptedStream.WriteByte((byte)(b ^ 0x55));
				}
			}

			// Save the end position.
			long end = this.EncryptedStream.Position;

			// Write header.
			this.EncryptedStream.Position = start;
			buffer[0] = 0x20;
			buffer[1] = 0x47;
			buffer[2] = 0x53;
			buffer[3] = 0x4D;
			this.EncryptedStream.Write(buffer, 0, 4);
			buffer[0] = 0x00;
			buffer[1] = 0x01;
			buffer[2] = (byte)(obj.Count >> 0);
			buffer[3] = (byte)(obj.Count >> 8);
			this.EncryptedStream.Write(buffer, 0, 4);
			buffer[0] = (byte)(maxScriptSize >> 0);
			buffer[1] = (byte)(maxScriptSize >> 8);
			buffer[2] = 0xFF;
			buffer[3] = 0xFF;
			this.EncryptedStream.Write(buffer, 0, 4);

			// Write script entries.
			for (int i = 0; i < obj.Count; i++) {
				buffer[0] = (byte)(offsets[i] >> 0);
				buffer[1] = (byte)(offsets[i] >> 8);
				buffer[2] = (byte)(sizes  [i] >> 0);
				buffer[3] = (byte)(sizes  [i] >> 8);
				this.EncryptedStream.Write(buffer, 0, 4);
			}

			// Restore the end position.
			this.EncryptedStream.Position = end;

			// Clear decrypted stream.
			this.DecryptedStream.SetLength(0);
		}
	}
}
