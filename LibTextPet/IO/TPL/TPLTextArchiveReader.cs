using LibTextPet.General;
using LibTextPet.Msg;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace LibTextPet.IO.TPL {
	/// <summary>
	/// A TextPet Language reader that reads text archives from an input stream.
	/// </summary>
	public class TPLTextArchiveReader : TPLReader<TextArchive> {
		/// <summary>
		/// Gets the script readers that are used to read scripts.
		/// </summary>
		protected ReadOnlyCollection<TPLScriptReader> ScriptReaders { get; private set; }

		private bool identifierRead;

		/// <summary>
		/// Creates a new TPL text archive reader that reads from the given input stream.
		/// </summary>
		/// <param name="stream">The stream to read from.</param>
		/// <param name="databases">The command databases to use.</param>
		public TPLTextArchiveReader(Stream stream, params CommandDatabase[] databases)
			:base(stream, databases) {
			if (databases == null)
				throw new ArgumentNullException(nameof(databases), "The command databases cannot be null.");

			TPLScriptReader[] scriptReaders = new TPLScriptReader[databases.Length];
			for (int i = 0; i < databases.Length; i++) {
				scriptReaders[i] = new TPLScriptReader(stream, databases[i]);
			}
			this.ScriptReaders = new ReadOnlyCollection<TPLScriptReader>(scriptReaders);
		}

		/// <summary>
		/// Initializes a text archive to be read from the token enumerator.
		/// </summary>
		/// <returns>The initialized text archive.</returns>
		protected override TextArchive BeginRead() {
			identifierRead = false;
			return new TextArchive();
		}

		/// <summary>
		/// Gets the tokenized script reader that matches the specified command database name, using case-insensitive comparison.
		/// </summary>
		/// <param name="dbName">The command database name.</param>
		/// <returns>The command database.</returns>
		private TokenReader<Script> GetScriptReader(string dbName) {
			foreach (TPLScriptReader scriptReader in this.ScriptReaders) {
				if (scriptReader.DatabaseName.Equals(dbName, StringComparison.OrdinalIgnoreCase)) {
					return scriptReader;
				}
			}
			throw new ArgumentException("Unrecognized command database name \"" + dbName + "\".");
		}

		/// <summary>
		/// Processes the specified token for the specified text archive.
		/// </summary>
		/// <param name="obj">The text archive to modify.</param>
		/// <param name="token">The token to modify.</param>
		/// <returns>A result value that indicates whether the token was consumed, and whether to continue reading.</returns>
		protected override ProcessResult ProcessToken(TextArchive obj, Token token, CommandDatabase db) {
			if (obj == null)
				throw new ArgumentNullException(nameof(obj), "The text archive cannot be null.");
			
			if (token.Class == (int)TPLTokenType.Word) {
				switch (token.Value) {
					case "@archive":
						if (identifierRead) {
							// Beginning of a new archive, so our job is done.
							return ProcessResult.Stop;
						} else {
							obj.Identifier = ReadString((int)TPLTokenType.Word);
							identifierRead = true;
							return ProcessResult.ConsumeAndContinue;
						}
					case "@size":
						// Read the size of the text archive.
						obj.Resize((int)ReadNumber((int)TPLTokenType.Word));
						return ProcessResult.ConsumeAndContinue;
					case "script":
						// Read a script.
						ReadScript(obj);
						return ProcessResult.ConsumeAndContinue;
				}
			}
			// Unrecognized token, so stop.
			return ProcessResult.Stop;
		}

		/// <summary>
		/// Reads a script from the token enumerator and inserts it in the specified text archive.
		/// </summary>
		/// <param name="ta">The text archive to modify.</param>
		private void ReadScript(TextArchive ta) {
			string dbName = null;
			int? scriptNum = null;

			// Parse the script number and (optional) command database name.
			string parToken;
			while ((parToken = ReadString()) != "{") {
				// Parse as a number only if the script number was not yet defined.
				int r = 0;
				bool isNumber = scriptNum == null && NumberParser.TryParseInt32(parToken, out r);

				if (isNumber) {
					// If it's parseable as a number, treat it as the script number.
					scriptNum = r;
				} else {
					// Otherwise, treat it as the command database name.
					if (dbName == null) {
						dbName = parToken;
					} else {
						throw new InvalidDataException("Unexpected token; '{' expected.");
					}
				}
			}

			// Do we have the script number?
			if (scriptNum == null) {
				throw new InvalidDataException("No script number specified.");
			}

			// Do we have the command database name?
			if (dbName == null) {
				if (this.Databases.Count != 1) {
					throw new InvalidDataException("Command database is ambiguous; more than one command database loaded.");
				}
			}

			// Read the actual script.
			TokenReader<Script> scriptReader = GetScriptReader(dbName);
			Script script = scriptReader.SubRead<TextArchive>(this, scriptReader.Databases[0], true);

			// Insert the script in the text archive.
			ta[(int)scriptNum] = script;

			// Read the next token if the script reader consumed the last one.
			if (scriptReader.Consumed) {
				// Do not specify a token type, since we need to validate it anyway.
				ReadToken();
			}
			Token terminatorToken = this.Current;
			if (terminatorToken.Class != (int)TPLTokenType.Symbol || terminatorToken.Value != "}") {
				throw new InvalidDataException("Unexpected token; '}' expected.");
			}
		}
	}
}
