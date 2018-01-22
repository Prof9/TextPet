using LibTextPet.Msg;
using LibTextPet.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LibTextPet.IO.Msg {
	/// <summary>
	/// A binary text archive writer that writes a text archive to an output stream.
	/// </summary>
    public class BinaryTextArchiveWriter : Manager, IWriter<TextArchive> {
		/// <summary>
		/// Gets the script writer that is used to write scripts to the output stream.
		/// </summary>
		protected BinaryScriptWriter ScriptWriter { get; private set; }

		/// <summary>
		/// Creates a new binary text archive writer that writes to the specified output stream, using the specified encoding and command databases.
		/// </summary>
		/// <param name="stream">The stream to write to.</param>
		/// <param name="encoding">The encoding to use.</param>
		/// <param name="databases">The command databases to use.</param>
		public BinaryTextArchiveWriter(Stream stream, IgnoreFallbackEncoding encoding)
			: base(stream, true, FileAccess.Write, encoding) {
			this.ScriptWriter = new BinaryScriptWriter(stream, encoding);
		}

		/// <summary>
		/// Writes the specified text archive to the output stream.
		/// </summary>
		/// <param name="obj">The text archive to write.</param>
		public void Write(TextArchive obj) {
			if (obj == null)
				throw new ArgumentNullException(nameof(obj), "The text archive cannot be null.");

			long start = this.BaseStream.Position;

			// Pad the stream where the pointers go.
			byte[] buffer = new byte[2];
			for (int i = 0; i < obj.Count; i++) {
				this.BaseStream.Write(buffer, 0, buffer.Length);
			}

			// Keep track of the script offsets.
			List<long> offsets = new List<long>(obj.Count);
			
			// Write all scripts.
			for (int i = 0; i < obj.Count; i++) {
				// Save the script offset.
				offsets.Add(this.BaseStream.Position - start);

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
			}

			// Save the end position.
			long end = this.BaseStream.Position;

			// Write all offsets.
			this.BaseStream.Position = start;
			foreach (long offset in offsets) {
				if (offset > 0xFFFF) {
					throw new ArgumentException("The text archive is too large.", nameof(obj));
				}
				buffer[0] = (byte)(offset & 0xFF);
				buffer[1] = (byte)((offset >> 8) & 0xFF);

				this.BaseStream.Write(buffer, 0, buffer.Length);
			}

			// Restore the end position.
			this.BaseStream.Position = end;
		}
	}
}
