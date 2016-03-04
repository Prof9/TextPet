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
		/// Creates a new output writer that writes to the specified output stream.
		/// </summary>
		/// <param name="stream">The stream to write to.</param>
		protected IndentedWriter(Stream stream)
			: base(stream, false, FileAccess.Write) {
			this.StreamWriter = new StreamWriter(stream, this.Encoding);
			this.indentLevel = 0;
		}

		/// <summary>
		/// Writes the specified object to the output stream.
		/// </summary>
		/// <param name="obj">The object to write.</param>
		public void Write(T obj) {
			Write(Output(obj));
		}

		/// <summary>
		/// Writes the specified string to the output stream.
		/// </summary>
		/// <param name="value">The string to write.</param>
		protected void Write(string value) {
			this.StreamWriter.Write(value);
			this.StreamWriter.Flush();
		}

		/// <summary>
		/// Outputs the specified object as a string.
		/// </summary>
		/// <param name="obj">The object.</param>
		/// <returns>The resulting string.</returns>
		protected abstract string Output(T obj);
	}
}
