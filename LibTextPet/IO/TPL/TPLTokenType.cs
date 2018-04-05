using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LibTextPet.IO.TPL {
	/// <summary>
	/// The type of TPL token.
	/// </summary>
	public enum TPLTokenType {
		/// <summary>
		/// A token that indicates a comment spanning one or more lines.
		/// </summary>
		MultilineComment,
		/// <summary>
		/// A token that indicates a comment that is not terminated.
		/// </summary>
		UnterminatedMultilineComment,
		/// <summary>
		/// A token that indicates a comment spanning the rest of the line.
		/// </summary>
		Comment,
		/// <summary>
		/// A token that indicates the start of a heredoc (here document).
		/// </summary>
		HeredocStart,
		/// <summary>
		/// A token that indicates (the contents of) a heredoc (here document).
		/// </summary>
		Heredoc,
		/// <summary>
		/// A token that indicates the end of a heredoc (here document).
		/// </summary>
		HeredocEnd,
		/// <summary>
		/// A token that indicates a heredoc that is not terminated properly.
		/// </summary>
		UnterminatedHeredoc,
		/// <summary>
		/// A token that indicates a string.
		/// </summary>
		String,
		/// <summary>
		/// A token that indicates a string that is not terminated properly.
		/// </summary>
		UnterminatedString,
		/// <summary>
		/// A token that indicates a single word, key or value.
		/// </summary>
		Word,
		/// <summary>
		/// A token that indicates a single symbol.
		/// </summary>
		Symbol,
		/// <summary>
		/// An unrecognized token.
		/// </summary>
		Unknown
	}
}
