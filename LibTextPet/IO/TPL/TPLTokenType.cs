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
		/// A token that indicates a single symbol.
		/// </summary>
		Symbol,
		/// <summary>
		/// A token that indicates a string.
		/// </summary>
		String,
		/// <summary>
		/// A token that indicates a single word, key or value.
		/// </summary>
		Word
	}
}
