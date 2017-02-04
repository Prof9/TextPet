using LibTextPet.Msg;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LibTextPet.IO.TextBox {
	/// <summary>
	/// A text box script writer that writes a script to an output stream.
	/// </summary>
	public class TextBoxScriptWriter : ScriptWriter {
		private bool textBoxActive;
		private string activeMugshot;

		private string _textBoxSeparator;
		public string TextBoxSeparator {
			get {
				return this._textBoxSeparator;
			}
			set {
				if (value == null)
					throw new ArgumentNullException(nameof(value), "The text box separator cannot be null.");

				this._textBoxSeparator = value;
			}
		}

		/// <summary>
		/// Creates a new text box script writer that writes to the specified output stream.
		/// </summary>
		/// <param name="stream">The stream to write to.</param>
		public TextBoxScriptWriter(Stream stream)
			: base(stream, false, false, new UTF8Encoding(false, true), new TextBoxCommandWriter(stream)) {
			this.TextBoxSeparator = Environment.NewLine + "###--------" + Environment.NewLine;
		}

		public override void Write(Script obj) {
			textBoxActive = false;
			activeMugshot = null;

			base.Write(obj);

			// Finish any currently active text box.
			FinishTextBox();
		}

		protected override void WriteText(string value) {
			if (value == null)
				throw new ArgumentNullException(nameof(value), "The text to write cannot be null.");

			// Start text box, if necessary.
			StartTextBox();
			// Write the text to the stream.
			base.WriteText(value.Replace("\\n", Environment.NewLine).Replace("\\", "\\\\").Replace("<", "\\<").Replace(">", "\\>"));
		}

		protected override void WriteCommand(Command command) {
			if (command == null)
				throw new ArgumentNullException(nameof(command), "The command cannot be null.");

			// Change the active mugshot.
			if (command.Definition.MugshotParameterName != null) {
				activeMugshot = command.Parameters[command.Definition.MugshotParameterName].ToString();
			}
			// Hide the active mugshot.
			if (command.Definition.HidesMugshot) {
				activeMugshot = null;
			}

			// Does the command print?
			if (command.Definition.Prints) {
				// Start text box, if necessary.
				StartTextBox();
				// Write the printed command to the stream.
				base.WriteCommand(command);
			} else {
				// Write the end of this text box to the stream.
				FinishTextBox();
			}
		}

		protected override void WriteFallback(IScriptElement element) {
			// Ignore raw byte elements.
			if (element is ByteElement) {
				return;
			}

			base.WriteFallback(element);
		}

		protected void StartTextBox() {
			// Only start new text box if no text box is active.
			if (!textBoxActive) {
				// Print active mugshot.
				if (activeMugshot != null) {
					this.TextWriter.Write("###" + nameof(DirectiveType.Mugshot) + ":");
					this.TextWriter.WriteLine(activeMugshot);
					this.TextWriter.Flush();
				}

				textBoxActive = true;
			}
		}

		protected void FinishTextBox() {
			// Only finish if text box active.
			if (textBoxActive) {
				// Write text box separator.
				this.TextWriter.Write(this.TextBoxSeparator);
				this.TextWriter.Flush();
				textBoxActive = false;
			}
		}
	}
}
