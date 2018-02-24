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
		private StringBuilder stringBuilder;
		private char[] charBuffer;
		private byte[] byteBuffer;

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
		/// Gets or sets a boolean that indicates whether this script reader will accept fallback elements when using the most compatible command reader.
		/// If this is set to false, script reading will abort if a fallback element cannot be resolved.
		/// By default, this is set to false.
		/// </summary>
		public bool AcceptMostCompatibleFallback { get; set; }

		/// <summary>
		/// Creates a script reader that reads from the specified input stream.
		/// </summary>
		/// <param name="stream">The stream to read from.</param>
		/// <param name="database">The command database to use.</param>
		/// <param name="encoding">The encoding to use.</param>
		/// <param name="commandReader">The command reader to use.</param>
		protected ScriptReader(Stream stream, IgnoreFallbackEncoding encoding,
			params T[] commandReaders)
			: base(stream, true, FileAccess.Read, encoding,
				// Aggregate databases of all command readers.
				commandReaders
				.SelectMany<T, CommandDatabase>(a => a.Databases)
				.Distinct()
				.ToArray()) {
			this.TextReader = new ConservativeStreamReader(stream, encoding);
			this.CommandReaders = new ReadOnlyCollection<T>(commandReaders);

			this.stringBuilder = new StringBuilder();
			this.charBuffer = new char[this.TextReader.GetMaxCharCount(1)];
			this.byteBuffer = new byte[this.TextReader.GetMaxByteCount(1)];
		}

		/// <summary>
		/// Reads a script from the current input stream.
		/// </summary>
		/// <returns>The script that was read.</returns>
		public virtual Script Read() {
			long start = this.BaseStream.Position;
			Script script = null;
			bool abortOnFallback = true;

			// Attempt to read the script with every command reader.
			for (int i = 0; i < this.CommandReaders.Count; i++) {
				T commandReader = this.CommandReaders[i];
				this.BaseStream.Position = start;

				// Turn off fallback abort for the last command reader, if the option is enabled.
				if (this.AcceptMostCompatibleFallback && i == this.CommandReaders.Count - 1) {
					abortOnFallback = false;
				}
				
				script = this.Read(commandReader, abortOnFallback);

				// If the script was read properly, stop.
				if (script != null) {
					// Set the command database name for the script.
					script.DatabaseName = commandReader.Database.Name;
					break;
				}
			}

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

			this.LastElement = null;
			while (this.HasNext()) {
				if (this.BaseStream.Position >= this.BaseStream.Length) {
					return null;
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
		/// <param name="commandReader">The script command reader to use.</param>
		/// <param name="abortOnFallback">If true, this method returns null instead of a fallback element when an unrecognized script element is encountered.</param>
		/// <returns>The element that was read.</returns>
		private IScriptElement ReadElement(IReader<Command> commandReader, bool abortOnFallback) {
			long start = this.BaseStream.Position;

			// Try to read the next element as a command.
			IScriptElement elem = ReadCommand(commandReader);

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
		/// Reads the next script command from the input stream.
		/// </summary>
		/// <returns>The next script command read from the input stream, or null if no script command exists at the current position in the input stream.</returns>
		protected virtual Command ReadCommand(IReader<Command> commandReader) {
			if (commandReader == null)
				throw new ArgumentNullException(nameof(commandReader), "The command reader cannot be null.");

			return commandReader.Read();
		}

		/// <summary>
		/// Reads the next string from the input stream.
		/// </summary>
		/// <returns>The next string read from the input stream, or an empty string if no string exists at the current position in the input stream.</returns>
		protected virtual string ReadText() {
			this.stringBuilder.Clear();
			
			while (HasNext()) {
				// Read the next code point from the input stream.
				if (!this.TextReader.TryReadSingleCodePoint(this.charBuffer, 0, this.byteBuffer, 0, out int charsUsed, out _)) {
					// Next character was unrecognized or end of stream.
					break;
				}

				this.stringBuilder.Append(this.charBuffer, 0, charsUsed);
			}

			return this.stringBuilder.ToString();
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
