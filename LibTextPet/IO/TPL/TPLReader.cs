using LibTextPet.Msg;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace LibTextPet.IO.TPL {
	/// <summary>
	/// A TextPet Language reader that reads TextPet Language tokens from an input stream.
	/// </summary>
	/// <typeparam name="T">The type of object read by this reader.</typeparam>
	public abstract class TPLReader<T> : TokenReader<T> {
		/// <summary>
		/// Gets the precompiled regex used to tokenize the input text.
		/// </summary>
		private static Regex PreCompiledTokenizerRegex = null;
		/// <summary>
		/// Gets the regex used to tokenize the input text.
		/// </summary>
		protected static Regex TokenizerRegex {
			get {
				if (PreCompiledTokenizerRegex == null) {
					PreCompiledTokenizerRegex = new Regex(
						// Indented heredoc:
						// """
						// Blah blah
						// """
						@"(?:(?<" + nameof(TPLTokenType.HeredocStart) + @">^[^\S\r]*?"""""")[^\S\r]*\r?\n" +
						@"(?<" + nameof(TPLTokenType.Heredoc) + @">^.*?)\r?\n[^\S\r\n]*" + 
						@"(?<" + nameof(TPLTokenType.HeredocEnd) + @">""""""))" +
						@"|" +
						// Single symbol
						@"(?<" + nameof(TPLTokenType.Symbol) + @">[,=\[\]{}!<>;])" +
						@"|" +
						// Regular string:
						// "Blah blah"
						@"(?:""(?<" + nameof(TPLTokenType.String) + @">[^""\\]*(?:\\.[^""\\]*)*)"")" +
						@"|" +
						// Word
						@"(?<" + nameof(TPLTokenType.Word) + @">[^""\s\r,=\[\]{}!<>;]+)",
						RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.Singleline);
				}
				return PreCompiledTokenizerRegex;
			}
		}

		/// <summary>
		/// Creates a new TextPet Language reader that reads from the specified input stream, using the specified command databases.
		/// </summary>
		/// <param name="stream">The stream to read from.</param>
		/// <param name="databases">The command databases to use.</param>
		protected TPLReader(Stream stream, params CommandDatabase[] databases)
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
					int type = (int)Enum.Parse(typeof(TPLTokenType), TokenizerRegex.GroupNameFromNumber(i));
					yield return new Token(type, match.Groups[i].Value);
				}
			}
			if (!found) {
				throw new ArgumentException("Could not find a matching token type.", nameof(match));
			}
		}
	}
}
