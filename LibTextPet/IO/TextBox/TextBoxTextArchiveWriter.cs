using LibTextPet.Msg;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace LibTextPet.IO.TextBox {
	public class TextBoxTextArchiveWriter : Manager, IWriter<TextArchive>, IDisposable {
		/// <summary>
		/// Gets the script writer that is used to write scripts to the output stream.
		/// </summary>
		protected IWriter<Script> ScriptWriter { get; }

		/// <summary>
		/// Gets the text writer that is used to write text to the output stream.
		/// </summary>
		protected TextWriter TextWriter { get; }

		/// <summary>
		/// Gets or sets a boolean that indicates whether text archive identifiers should be included in the output.
		/// </summary>
		public bool IncludeIdentifiers { get; set; }

		public TextBoxTextArchiveWriter(Stream stream)
			: base(stream, false, FileAccess.Write) {
			this.ScriptWriter = new TextBoxScriptWriter(stream);
			this.TextWriter = new StreamWriter(stream, new UTF8Encoding(false, true));
			this.IncludeIdentifiers = true;
		}

		public void Write(TextArchive obj) {
			if (obj == null)
				throw new ArgumentNullException(nameof(obj), "The text archive cannot be null.");

			if (this.IncludeIdentifiers) {
				this.TextWriter.Write("###" + new DirectiveElement(DirectiveType.TextArchive, obj.Identifier).ToString());
			} else {
				this.TextWriter.Write("###" + new DirectiveElement(DirectiveType.TextArchive).ToString());
			}
			this.TextWriter.WriteLine();
			this.TextWriter.Flush();

			// Output every script.
			for (int i = 0; i < obj.Count; i++) {
				Script script = obj[i];

				// Skip scripts that are not printed.
				bool isEmpty = true;
				foreach (IScriptElement elem in script) {
					Command cmd = elem as Command;
					if ((cmd != null && cmd.Definition.Prints) || elem is TextElement) {
						isEmpty = false;
						break;
					}
				}
				if (isEmpty) {
					continue;
				}
				
				// Write the script number.
				this.TextWriter.Write("###" + new DirectiveElement(DirectiveType.Script, i.ToString(CultureInfo.InvariantCulture)).ToString());
				this.TextWriter.WriteLine();
				this.TextWriter.Flush();

				// Write the script.
				this.ScriptWriter.Write(script);
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
