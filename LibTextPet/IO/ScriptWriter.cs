using LibTextPet.IO;
using LibTextPet.Msg;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;

namespace LibTextPet.IO {
	/// <summary>
	/// A script writer that writes a script to an output stream.
	/// </summary>
	public abstract class ScriptWriter : Manager, IWriter<Script> {
		/// <summary>
		/// Gets the command writer that is used to write script commands to the output stream.
		/// </summary>
		protected IWriter<Command> CommandWriter { get; set; }

		/// <summary>
		/// Gets a text writer for the current output stream.
		/// </summary>
		protected StreamWriter TextWriter { get; private set; }

		/// <summary>
		/// Creates a script writer that writes to the specified output stream.
		/// </summary>
		/// <param name="stream">The stream to write to.</param>
		/// <param name="seekable">Whether the stream must be seekable.</param>
		/// /// <param name="seekable">Whether the stream must be readable.</param>
		/// <param name="encoding">The encoding to use.</param>
		/// <param name="commandWriter">The command writer to use.</param>
		protected ScriptWriter(Stream stream, bool seekable, bool readable, Encoding encoding, IWriter<Command> commandWriter)
			: base(stream, seekable, readable ? FileAccess.ReadWrite : FileAccess.Write, encoding) {
			this.TextWriter = new StreamWriter(stream, encoding);
			this.CommandWriter = commandWriter;
		}

		/// <summary>
		/// Writes a script to the current output stream.
		/// </summary>
		/// <param name="obj">The script to write.</param>
		public virtual void Write(Script obj) {
			if (obj == null)
				throw new ArgumentNullException(nameof(obj), "The script cannot be null.");

			foreach (IScriptElement element in obj) {
				// Try to write the next element as a command.
				Command command = element as Command;
				if (command != null) {
					WriteCommand(command);
					continue;
				}

				// Try to write the next element as a string.
				TextElement text = element as TextElement;
				if (text != null) {
					WriteText(text.Text);
					continue;
				}

				// Try to process the next element as a directive.
				DirectiveElement directive = element as DirectiveElement;
				if (directive != null) {
					ProcessDirective(directive);
					continue;
				}

				// Could not determine element; try to write as fallback element.
				WriteFallback(element);
			}
		}

		/// <summary>
		/// Writes the given script command to the output stream.
		/// </summary>
		/// <param name="command">The script command to write.</param>
		protected virtual void WriteCommand(Command command) {
			this.CommandWriter.Write(command);
			this.TextWriter.Flush();
		}

		/// <summary>
		/// Writes the given string to the output stream.
		/// </summary>
		/// <param name="value">The string to write.</param>
		protected virtual void WriteText(string value) {
			this.TextWriter.Write(value);
			this.TextWriter.Flush();
		}

		/// <summary>
		/// Process the given directive.
		/// </summary>
		/// <param name="directive">The directive to process.</param>
		protected virtual void ProcessDirective(DirectiveElement directive) { }

		/// <summary>
		/// Writes a fallback element to the output stream. This method is
		/// is called if the script element to be written is neither a script
		/// command nor a string. By default, this method throws an exception.
		/// </summary>
		/// <param name="element">The script element to write.</param>
		protected virtual void WriteFallback(IScriptElement element) {
			throw new NotSupportedException("The standard script writer does not support fallback elements.");
		}
	}
}
