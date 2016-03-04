using LibTextPet.IO;
using LibTextPet.Msg;
using LibTextPet.Text;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;

namespace LibTextPet.IO {
	/// <summary>
	/// A script reader that reads a script from an input stream.
	/// </summary>
	public abstract class ScriptReader<T> : Manager, IReader<Script> where T : SingleManager, IReader<Command> {
		/// <summary>
		/// Gets the last script element that was read from the current input stream.
		/// </summary>
		protected IScriptElement LastElement { get; private set; }

		/// <summary>
		/// Gets the pairs of command database names and command readers that are used to read script commands from the current input stream.
		/// </summary>
		protected ReadOnlyCollection<T> CommandReaders { get; private set; }

		/// <summary>
		/// Gets a text reader for the current input stream.
		/// </summary>
		protected ConservativeStreamReader TextReader { get; private set; }

		/// <summary>
		/// Creates a script reader that reads from the specified input stream.
		/// </summary>
		/// <param name="stream">The stream to read from.</param>
		/// <param name="database">The command database to use.</param>
		/// <param name="encoding">The encoding to use.</param>
		/// <param name="commandReader">The command reader to use.</param>
		protected ScriptReader(Stream stream, CustomFallbackEncoding encoding,
			params T[] commandReaders)
			: base(stream, true, FileAccess.Read, encoding,
				  // Aggregate databases of all command readers.
				  commandReaders
				  .SelectMany<T, CommandDatabase>(a => a.Databases)
				  .Distinct()
				  .ToArray()) {

			this.TextReader = new ConservativeStreamReader(stream, encoding);

			this.CommandReaders = new ReadOnlyCollection<T>(commandReaders);
		}

		/// <summary>
		/// Reads a script from the current input stream.
		/// </summary>
		/// <returns>The script that was read.</returns>
		public virtual Script Read() {
			long start = this.BaseStream.Position;
			Script script = null;

			// Attempt to read the script with every command reader.
			foreach (T commandReader in this.CommandReaders) {
				this.BaseStream.Position = start;
				script = this.Read(commandReader, true);

				// If the script was read properly, stop.
				if (script != null) {
					// Set the command database name for the script.
					script.DatabaseName = commandReader.Database.Name;
                    break;
				}
			}

			// Check if the script was read properly.
			if (script == null)
				throw new InvalidDataException("Could not parse the script.");

			return script;
		}

		/// <summary>
		/// Reads a script from the current input stream using the specified command reader.
		/// </summary>
		/// <param name="commandReader">The command reader to use.</param>
		/// <param name="abortOnFallback">If true, this method returns null instead of a script when an unrecognized script element is encountered.</param>
		/// <returns>The script that was read.</returns>
		private Script Read(IReader<Command> commandReader, bool abortOnFallback) {
			Script script = new Script();

			while (this.HasNext()) {
				if (this.BaseStream.Position >= this.BaseStream.Length) {
					throw new EndOfStreamException();
				}

				IScriptElement elem = this.ReadElement(commandReader, abortOnFallback);
				if (elem == null && abortOnFallback) {
					return null;
				}
				script.Add(elem);
			}

			return script;
		}

		/// <summary>
		/// Reads the next script element from the input stream.
		/// </summary>
		/// <param name="abortOnFallback">If true, this method returns null instead of a fallback element when an unrecognized script element is encountered.</param>
		/// <returns>The element that was read.</returns>
		private IScriptElement ReadElement(IReader<Command> commandReader, bool abortOnFallback) {
			long start = this.BaseStream.Position;

			// Try to read the next element as a command.
			IScriptElement elem = commandReader.Read();

			// Try to read the next element(s) as a string.
			if (elem == null) {
				this.BaseStream.Position = start;
				string s = ReadText();

				if (s != null && s.Length > 0) {
					elem = new TextElement(s);
				}
			}

			// Could not find any element; return as fallback element.
			if (elem == null) {
				this.BaseStream.Position = start;

				if (abortOnFallback) {
					return null;
				} else {
					elem = new ByteElement((byte)this.BaseStream.ReadByte());
				}
			}

			this.LastElement = elem;
			return elem;
		}

		/// <summary>
		/// Reads the next string from the input stream.
		/// </summary>
		/// <returns>The next string read from the input stream, or an empty string if no string exists at the current position in the input stream.</returns>
		protected virtual string ReadText() {
			StringBuilder builder = new StringBuilder();

			// Determine lookahead.
			char[] buffer = new char[this.Encoding.GetMaxCharCount(this.Encoding.GetMaxByteCount(1))];
			while (HasNext()) {
				// Read the next character from the input stream.
				int read = this.TextReader.Read(buffer, 0, 1);

				// Check if next character is unrecognized or end of stream.
				if (read == 0) {
					break;
				}

				// Append character and advance stream.
				builder.Append(buffer, 0, read);
			}

			return builder.ToString();
		}

		/// <summary>
		/// Reads the next fallback element from the input stream. This method
		/// is called if the script element at the current position in the input
		/// stream could not be decoded as a script command or string. By
		/// default, this method throws an exception.
		/// </summary>
		/// <returns>A fallback script element.</returns>
		protected virtual IScriptElement ReadFallback() {
			throw new NotSupportedException("The standard script reader does not support fallback elements.");
		}

		/// <summary>
		/// Checks whether the current input stream has script elements left.
		/// </summary>
		/// <returns>true if there are script elements left; otherwise, false.</returns>
		protected virtual bool HasNext() {
			// Check if the last element ended the script.
			if (this.LastElement != null && this.LastElement.EndsScript) {
				return false;
			}
			return true;
		}
	}
}
