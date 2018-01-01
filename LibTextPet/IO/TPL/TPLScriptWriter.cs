using LibTextPet.Msg;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace LibTextPet.IO.TPL {
	/// <summary>
	/// A TextPet Language script writer that writes a script to an output stream.
	/// </summary>
	public class TPLScriptWriter : ScriptWriter {
		protected int ConsecutiveByteElements { get; set; }

		/// <summary>
		/// Creates a new TextPet Language script writer that writes to the specified output stream.
		/// </summary>
		/// <param name="stream">The stream to write to.</param>
		public TPLScriptWriter(Stream stream)
			: base(stream, false, false, new UTF8Encoding(false, true), new TPLCommandWriter(stream)) {
			((TPLCommandWriter)this.CommandWriter).IndentLevel = 1;
		}

		/// <summary>
		/// Writes a script to the current output stream.
		/// </summary>
		/// <param name="obj">The script to write.</param>
		public override void Write(Script obj) {
			Write(obj, 0);
		}

		/// <summary>
		/// Writes the script with the given script number to the current output stream.
		/// </summary>
		/// <param name="script">The script to write.</param>
		/// <param name="scriptNumber">The script number.</param>
		public void Write(Script script, int scriptNumber) {
			if (script == null)
				throw new ArgumentNullException(nameof(script), "The script cannot be null.");
			if (scriptNumber < 0)
				throw new ArgumentOutOfRangeException(nameof(scriptNumber), scriptNumber, "The script number cannot be negative.");

			this.ConsecutiveByteElements = 0;

			// Write: script 0 dbname {
			this.TextWriter.WriteLine(String.Format(CultureInfo.InvariantCulture, "script {0} {1} {{", scriptNumber, script.DatabaseName));
			this.TextWriter.Flush();

			// Write the script
			base.Write(script);
			this.TextWriter.Flush();

			// Finish pending byte elements.
			if (this.ConsecutiveByteElements > 0) {
				this.TextWriter.WriteLine();
			}

			// Write: }
			this.TextWriter.WriteLine("}");

			this.TextWriter.Flush();
		}

		/// <summary>
		/// Writes the given string to the output stream.
		/// </summary>
		/// <param name="value">The string to write.</param>
		protected override void WriteText(string value) {
			if (value == null)
				throw new ArgumentNullException(nameof(value), "The string to write cannot be null.");

			if (this.ConsecutiveByteElements > 0) {
				this.ConsecutiveByteElements = 0;
				this.TextWriter.WriteLine();
			}

			// Determine whether to write heredoc or regular string.
			if (Regex.IsMatch(value, @"\S\\n\S")) {
				// Write heredoc for strings that contain a line split with text before/after it.
				this.TextWriter.WriteLine("\t\"\"\"");
				foreach (string line in value.Split(new string[] { "\\n" }, StringSplitOptions.None)) {
					// Indent every line.
					this.TextWriter.Write('\t');
					base.WriteText(line);
					this.TextWriter.WriteLine();
				}
				this.TextWriter.WriteLine("\t\"\"\"");
			} else {
				// Write regular string.
				this.TextWriter.Write("\t\"");
				base.WriteText(value.Replace("\"", "\\\""));
				this.TextWriter.WriteLine("\"");
			}
			this.TextWriter.Flush();
		}

		/// <summary>
		/// Writes the given script command to the output stream.
		/// </summary>
		/// <param name="command">The script command to write.</param>
		protected override void WriteCommand(Command command) {
			if (command == null)
				throw new ArgumentNullException(nameof(command), "The script command cannot be null.");

			if (this.ConsecutiveByteElements > 0) {
				this.ConsecutiveByteElements = 0;
				this.TextWriter.WriteLine();
			}

			base.WriteCommand(command);
		}

		/// <summary>
		/// Writes the given fallback element to the output stream.
		/// </summary>
		/// <param name="element">The fallback element to write.</param>
		protected override void WriteFallback(IScriptElement element) {
			if (element == null)
				throw new ArgumentNullException(nameof(element), "The script element cannot be null.");

			if (element is ByteElement byteElem) {
				if (this.ConsecutiveByteElements >= 8) {
					this.ConsecutiveByteElements = 0;
					this.TextWriter.WriteLine();
				}

				if (this.ConsecutiveByteElements == 0) {
					this.TextWriter.Write('\t');
				} else {
					this.TextWriter.Write(' ');
				}

				this.TextWriter.Write("$");
				this.TextWriter.Write(byteElem.Byte.ToString("X2", CultureInfo.InvariantCulture));

				this.ConsecutiveByteElements++;
				return;
			}

			base.WriteFallback(element);
		}

		protected override void ProcessDirective(DirectiveElement directive) {
			if (directive == null)
				throw new ArgumentNullException(nameof(directive), "The directive cannot be null.");

			base.ProcessDirective(directive);

			switch (directive.DirectiveType) {
				case DirectiveType.TextBoxSeparator:
					this.TextWriter.WriteLine("\t#--------");
					break;
				case DirectiveType.TextBoxSplit:
					this.TextWriter.WriteLine("\t#++++++++");
					break;
				case DirectiveType.TextArchive:
				case DirectiveType.Script:
				case DirectiveType.Mugshot:
					break;
				default:
					this.TextWriter.Write("\t#");
					this.TextWriter.Write(directive.Name);
					if (directive.Value != null) {
						this.TextWriter.Write(':');
						this.TextWriter.Write(directive.Value);
					}
					this.TextWriter.WriteLine();
					break;
			}
			this.TextWriter.Flush();
		}
	}
}
