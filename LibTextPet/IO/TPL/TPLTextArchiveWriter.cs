using LibTextPet.Msg;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace LibTextPet.IO.TPL {
	/// <summary>
	/// A TextPet Language text archive writer that writes a text archive to an output stream.
	/// </summary>
	public class TPLTextArchiveWriter : Manager, IWriter<TextArchive>, IDisposable {
		/// <summary>
		/// Gets the script writer that is used to write scripts to the output stream.
		/// </summary>
		protected TPLScriptWriter ScriptWriter { get; }

		/// <summary>
		/// Gets the text writer that is used to write text to the output stream.
		/// </summary>
		protected TextWriter TextWriter { get; }

		/// <summary>
		/// Creates a new TextPet Language text archive writer that writes to the specified output stream.
		/// </summary>
		/// <param name="stream">The stream to write to.</param>
		public TPLTextArchiveWriter(Stream stream)
			: base(stream, false, FileAccess.Write) {
			this.ScriptWriter = new TPLScriptWriter(stream);
			this.TextWriter = new StreamWriter(stream, new UTF8Encoding(false, true));
		}

		/// <summary>
		/// Writes the given text archive to the output stream.
		/// </summary>
		/// <param name="obj">The text archive to write.</param>
		public void Write(TextArchive obj) {
			if (obj == null)
				throw new ArgumentNullException(nameof(obj), "The text archive cannot be null.");

			this.TextWriter.WriteLine(String.Format(CultureInfo.InvariantCulture, "@archive {0}", obj.Identifier));
			this.TextWriter.WriteLine(String.Format(CultureInfo.InvariantCulture, "@size {0}", obj.Count));
			this.TextWriter.WriteLine();
			this.TextWriter.Flush();

			// Output every script.
			for (int i = 0; i < obj.Count; i++) {
				Script script = obj[i];

				// Skip empty scripts.
				if (script.Count <= 0) {
					continue;
				}

				// Write the script.
				this.ScriptWriter.Write(script, i);
			}
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed", MessageId = "<TextWriter>k__BackingField")]
		protected virtual void Dispose(bool disposing) {
			if (disposing) {
				this.TextWriter.Close();
			}
		}

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}
	}
}
