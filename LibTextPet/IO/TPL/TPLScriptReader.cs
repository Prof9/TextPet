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
	/// A TextPet Language reader that reads scripts from an input stream.
	/// </summary>
	public class TPLScriptReader : TPLReader<Script> {
		/// <summary>
		/// Gets the name of the command database used by this script reader.
		/// </summary>
		public string DatabaseName {
			get {
				return this.Databases[0].Name;
			}
		}

		/// <summary>
		/// Gets the TextPet Language reader that is used to read script commands.
		/// </summary>
		protected TPLCommandReader CommandReader { get; private set; }

		/// <summary>
		/// Creates a new script reader that reads from the specified input stream using the specified command database.
		/// </summary>
		/// <param name="stream">The stream to read from.</param>
		/// <param name="database">The command database to use.</param>
		public TPLScriptReader(Stream stream, CommandDatabase database)
			: base(stream, database) {
			this.CommandReader = new TPLCommandReader(stream, database);
		}

		/// <summary>
		/// Initializes a script to be read from the token enumerator.
		/// </summary>
		/// <returns>The initialized script.</returns>
		protected override Script BeginRead() {
			return new Script();
		}

		/// <summary>
		/// Finalized the script that was read.
		/// </summary>
		/// <param name="obj">The script that was read.</param>
		/// <param name="db">The command database that was used.</param>
		/// <returns>The finalized script.</returns>
		protected override Script EndRead(Script obj, CommandDatabase db) {
			if (obj != null) {
				obj.DatabaseName = this.DatabaseName;
			}

			return base.EndRead(obj, db);
		}

		/// <summary>
		/// Processes the specified token for the specified script.
		/// </summary>
		/// <param name="obj">The script to modify.</param>
		/// <param name="token">The token to process.</param>
		/// <returns>A result value that indicates whether the token was consumed, and whether to continue reading.</returns>
		protected override ProcessResult ProcessToken(Script obj, Token token, CommandDatabase db) {
			if (obj == null)
				throw new ArgumentNullException(nameof(obj), "The script cannot be null.");

			switch (token.Class) {
			case (int)TPLTokenType.Symbol:
				if (token.Value == "}") {
					// Read terminator.
					return ProcessResult.Stop;
				}
				throw new ArgumentException("Unexpected symbol token '" + token.Value + "'.", nameof(token));
			case (int)TPLTokenType.String:
				// Create a new text element.
				obj.Add(new TextElement(
					// Unescape \" and \\.
					Regex.Replace(token.Value, @"\\([\\""])", "$1")
				));
				return ProcessResult.ConsumeAndContinue;
			case (int)TPLTokenType.Heredoc:
				// Create a new text element.
				obj.Add(new TextElement(
					// Escape line breaks to \n.
					Regex.Replace(token.Value, @"\r?\n", @"\n")
				));
				return ProcessResult.ConsumeAndContinue;
			case (int)TPLTokenType.HeredocStart:
				// Read the padding.
				string padding = token.Value.Substring(0, token.Value.IndexOf('"'));
				// Read the actual heredoc.
				string[] lines = Regex.Split(ReadString((int)TPLTokenType.Heredoc), @"\r?\n");
				// Remove the padding from each line.
				for (int i = 0; i < lines.Length; i++) {
					if (!lines[i].StartsWith(padding, StringComparison.Ordinal)) {
						throw new InvalidDataException("The indentation of the heredoc contents do not match the indentation of the heredoc start.");
					}
					lines[i] = lines[i].Substring(padding.Length);
				}
				// Create a new text element.
				obj.Add(new TextElement(
					// Join the heredoc lines with a \n.
					String.Join(@"\n", lines)
				));
				// Read the end of the heredoc.
				ReadToken((int)TPLTokenType.HeredocEnd);
				return ProcessResult.ConsumeAndContinue;
			case (int)TPLTokenType.Word:
				if (token.Value.StartsWith("$", StringComparison.OrdinalIgnoreCase)) {
					// Read raw byte.
					if (token.Value.Length <= 1 || token.Value.Length > 3) {
						throw new ArgumentException("Invalid hexadecimal byte element '" + token.Value + "'.", nameof(token));
					}
					if (!Byte.TryParse(token.Value.Substring(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte byteVal)) {
						throw new ArgumentException("Could not parse hexadecimal byte element '" + token.Value + "'.", nameof(token));
					}
					obj.Add(new ByteElement(byteVal));
					return ProcessResult.ConsumeAndContinue;
				} else {
					// Read a script command.
					obj.Add(this.CommandReader.SubRead(this, db, false));
					if (this.CommandReader.Consumed) {
						return ProcessResult.ConsumeAndContinue;
					} else {
						return ProcessResult.Continue;
					}
				}
			}
			throw new ArgumentException("Unrecognized token.", nameof(token));
		}
	}
}
