using LibTextPet.General;
using LibTextPet.Msg;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LibTextPet.IO.TextBox {
	public class TextBoxTextArchiveTemplateReader : TextBoxTemplateReader<TextArchive> {
		protected TokenReader<Script> ScriptReader { get; }

		private bool identifierRead;
		private int scriptNum;
		private List<Token> currentScriptTokens;

		/// <summary>
		/// Creates a new text archive text box template reader that reads from the given input stream.
		/// </summary>
		/// <param name="stream">The stream to read from.</param>
		/// <param name="databases">The command databases to use.</param>
		public TextBoxTextArchiveTemplateReader(Stream stream, params CommandDatabase[] databases)
			: base(stream, databases) {
			if (databases == null)
				throw new ArgumentNullException(nameof(databases), "The command databases cannot be null.");

			this.ScriptReader = new TextBoxScriptTemplateReader(stream, databases);
		}

		protected override TextArchive BeginRead() {
			identifierRead = false;
			scriptNum = -1;
			currentScriptTokens = null;
			return new TextArchive();
		}

		protected override ProcessResult ProcessToken(TextArchive obj, Token token, CommandDatabase db) {
			if (obj == null)
				throw new ArgumentNullException(nameof(obj), "The text archive cannot be null.");

			switch (token.Class) {
				case (int)TextBoxTokenType.Directive:
					DirectiveElement directive = ParseDirective(token);
					switch (directive.DirectiveType) {
						case DirectiveType.TextArchive:
							if (identifierRead) {
								// Start of a new text archive.
								// Finish the current script.
								ReadCurrentScript(obj);
								return ProcessResult.Stop;
							} else {
								// Start of the current text archive.
								if (directive.HasNonemptyValue) {
									obj.Identifier = directive.Value.Trim();
								}
								identifierRead = true;
								return ProcessResult.ConsumeAndContinue;
							}
						case DirectiveType.Script:
							// Start of a new script.
							// Finish the current script.
							ReadCurrentScript(obj);
							if (!directive.HasNonemptyValue) {
								throw new FormatException("Script number is missing.");
							}
							if (!NumberParser.TryParseInt32(directive.Value.Trim(), out scriptNum)) {
								throw new FormatException("Could not parse script number \"" + directive.Value.Trim() + "\".");
							}
							currentScriptTokens = new List<Token>();
							return ProcessResult.ConsumeAndContinue;
					}
					break;
				case (int)TextBoxTokenType.Comment:
					return ProcessResult.ConsumeAndContinue;
			}

			if (currentScriptTokens == null) {
				throw new ArgumentException("Unrecognized token.", nameof(token));
			}

			currentScriptTokens.Add(token);
			return ProcessResult.ConsumeAndContinue;
		}

		protected override TextArchive EndRead(TextArchive obj, CommandDatabase db) {
			ReadCurrentScript(obj);
			return base.EndRead(obj, db);
		}

		/// <summary>
		/// Reads the next script from the currently read script tokens and inserts it into the specified text archive.
		/// </summary>
		/// <param name="ta">The text archive to insert into.</param>
		private void ReadCurrentScript(TextArchive ta) {
			if (currentScriptTokens == null) {
				return;
			}

			if (scriptNum >= ta.Count) {
				ta.Resize(scriptNum + 1);
			}

			ta[scriptNum] = this.ScriptReader.Read(currentScriptTokens)[0];
		}
	}
}
