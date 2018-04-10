using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LibTextPet.IO.TPL {
	/// <summary>
	/// A writer that writes an object to an output stream, optionally intended to a certain level.
	/// </summary>
	public abstract class IndentedWriter<T> : Manager, IWriter<T> {
		/// <summary>
		/// Gets the binary writer that is used to write text to the output stream.
		/// </summary>
		private StreamWriter StreamWriter { get; set; }

		/// <summary>
		/// Gets or sets a boolean that indicates whether the indented writer flattens all written text. If this is enabled, newlines and indents are not written.
		/// </summary>
		public bool Flatten { get; set; }

		/// <summary>
		/// Gets or sets a boolean that indicates whether the indented writer is at the start of a new line. This property is automatically updated as lines are written.
		/// </summary>
		protected bool IsNewLine { get; set; }

		private int indentLevel;
		/// <summary>
		/// Gets or sets the indent level that is used when printing lines.
		/// </summary>
		public int IndentLevel {
			get {
				return this.indentLevel;
			}
			set {
				if (value < 0)
					throw new ArgumentOutOfRangeException(nameof(value), value, "The new indent level cannot be negative.");

				this.indentLevel = value;
			}
		}

		/// <summary>
		/// Gets the string that is output per indent level.
		/// </summary>
		protected string IndentBlock => "\t";

		/// <summary>
		/// Creates a new output writer that writes to the specified output stream.
		/// </summary>
		/// <param name="stream">The stream to write to.</param>
		protected IndentedWriter(Stream stream)
			: base(stream, false, FileAccess.Write) {
			this.StreamWriter = new StreamWriter(stream, this.Encoding);
			this.indentLevel = 0;
			this.IsNewLine = true;
			this.Flatten = false;
		}

		private void WriteIndent() {
			if (this.Flatten) {
				this.StreamWriter.Write(' ');
			} else {
				for (int i = 0; i < this.IndentLevel; i++) {
					this.StreamWriter.Write(this.IndentBlock);
				}
			}
		}

		/// <summary>
		/// Writes the specified string to the output stream.
		/// </summary>
		/// <param name="value">The string to write.</param>
		protected void Write(string value) {
			if (this.IsNewLine) {
				this.WriteIndent();
				this.IsNewLine = false;
			}
			this.StreamWriter.Write(value);
		}

		/// <summary>
		/// Writes a line terminator to the output stream.
		/// </summary>
		protected void WriteLine() {
			if (!this.Flatten) {
				this.StreamWriter.WriteLine();
			}
			this.IsNewLine = true;
		}

		/// <summary>
		/// Writes the specified line to the output stream.
		/// </summary>
		/// <param name="line">The line to write.</param>
		protected void WriteLine(string line) {
			this.Write(line);
			this.WriteLine();
		}

		protected void Flush() => this.StreamWriter.Flush();

		/// <summary>
		/// Writes the specified object to the output stream.
		/// </summary>
		/// <param name="obj">The object to write.</param>
		public abstract void Write(T obj);
	}
}
