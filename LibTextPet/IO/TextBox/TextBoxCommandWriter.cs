using LibTextPet.Msg;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LibTextPet.IO.TextBox {
	public class TextBoxCommandWriter : Manager, IWriter<Command>, IDisposable {
		/// <summary>
		/// Gets the text writer that is used to write text to the underlying stream.
		/// </summary>
		protected TextWriter TextWriter { get; }

		public TextBoxCommandWriter(Stream stream)
			: base(stream, false, FileAccess.Write) {
			this.TextWriter = new StreamWriter(stream, new UTF8Encoding(false, true));
		}

		public void Write(Command obj) {
			if (obj == null)
				throw new ArgumentNullException(nameof(obj), "The command cannot be null.");

			this.TextWriter.Write('<');
			this.TextWriter.Write(obj.Name);
			this.TextWriter.Write('>');
			this.TextWriter.Flush();
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
