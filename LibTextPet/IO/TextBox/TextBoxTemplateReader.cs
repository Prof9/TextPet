using LibTextPet.Msg;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace LibTextPet.IO.TextBox {
	/// <summary>
	/// A text box template reader that reads template tokens from an input stream.
	/// </summary>
	/// <typeparam name="T">The type of object read by this reader.</typeparam>
	public abstract class TextBoxTemplateReader<T> : TokenReader<T> {
		/// <summary>
		/// Gets the precompiled regex used to tokenize the input text.
		/// </summary>
		private static Regex PreCompiledTokenizerRegex = null;
		/// <summary>
		/// Gets the regex used to tokenize the input text.
		/// </summary>
		protected static Regex TokenizerRegex
		{
			get
			{
				if (PreCompiledTokenizerRegex == null) {
					PreCompiledTokenizerRegex = new Regex(
						@"^###(?<" + nameof(TextBoxTokenType.Directive) + @">[^\r\n]*)(\r?\n)?" +
						@"|" +
						@"^##[^#](?<" + nameof(TextBoxTokenType.Comment) + @">[^\r\n]*)(\r?\n)?" +
						@"|" +
						@"<(?<" + nameof(TextBoxTokenType.Command) + @">\\\\|\\>|[^>]+)>(\r?\n(?=##))?" +
						@"|" +
						@"(?<" + nameof(TextBoxTokenType.Text) + @">(\\\\|\\<|[^<])+?)((\r?\n)?(?=##|$(?!\r?\n))|(?=\r?\n)?(?=<))",
						RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.ExplicitCapture);
				}
				return PreCompiledTokenizerRegex;
			}
		}

		/// <summary>
		/// Creates a new text box template reader that reads from the specified stream, using the specified command databases.
		/// </summary>
		/// <param name="stream">The stream to read from.</param>
		/// <param name="databases">The command databases to use, in order of preference.</param>
		protected TextBoxTemplateReader(Stream stream, params CommandDatabase[] databases)
			: base(stream, databases) { }

		/// <summary>
		/// Extracts tokens from the specified full text.
		/// </summary>
		/// <param name="fullText">The full text to extract tokens from.</param>
		/// <returns>The extracted tokens.</returns>
		protected override sealed IEnumerable<Token> Tokenize(string fullText) {
			foreach (Match match in TokenizerRegex.Matches(fullText)) {
				foreach (Token token in CreateTokens(match)) {
					yield return token;
				}
			}
		}

		/// <summary>
		/// Creates a token from the specified tokenizer regex match.
		/// </summary>
		/// <param name="match">The regex match.</param>
		/// <returns>The resulting token.</returns>
		private static IEnumerable<Token> CreateTokens(Match match) {
			bool found = false;
			for (int i = 1; i < match.Groups.Count; i++) {
				if (match.Groups[i].Length > 0) {
					found = true;
					int type = (int)Enum.Parse(typeof(TextBoxTokenType), TokenizerRegex.GroupNameFromNumber(i));
					yield return new Token(type, match.Groups[i].Value);
				}
			}
			if (!found) {
				throw new ArgumentException("Could not find a matching token type.", nameof(match));
			}
		}

		/// <summary>
		/// Parses a directive element from the given token.
		/// </summary>
		/// <param name="token">The token to parse.</param>
		/// <returns>The parsed directive element.</returns>
		protected static DirectiveElement ParseDirective(Token token) {
			if (token == null)
				throw new ArgumentNullException(nameof(token), "The token cannot be null.");
			if (token.Class != (int)TextBoxTokenType.Directive)
				throw new ArgumentException("The token is not a directive.", nameof(token));

			DirectiveType dType;
			string dName;
			string dValue;

			// Extract the directive name and value.
			int valueIndex = token.Value.IndexOf(':');
			if (valueIndex < 0) {
				dName = token.Value;
				dValue = null;
			} else {
				dName = token.Value.Substring(0, valueIndex);
				dValue = token.Value.Substring(valueIndex + 1);
			}

			// Handle special cases for TextBoxSeparator and TextBoxSplit.
			bool isTextBoxSeparator = true;
			bool isTextBoxSplit = true;
			foreach (char c in dName) {
				if (c != '-') {
					isTextBoxSeparator = false;
				}
				if (c != '+') {
					isTextBoxSplit = false;
				}
			}

			// Parse the directive name.
			if (isTextBoxSeparator) {
				dType = DirectiveType.TextBoxSeparator;
			} else if (isTextBoxSplit) {
				dType = DirectiveType.TextBoxSplit;
			} else if (!Enum.TryParse<DirectiveType>(dName, true, out dType)) {
				throw new ArgumentException("Unrecognized directive \"" + dName + "\".", nameof(token));
			}

			return new DirectiveElement(dType, dValue);
		}
	}
}
