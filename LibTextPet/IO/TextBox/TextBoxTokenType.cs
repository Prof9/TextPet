using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LibTextPet.IO.TextBox {
	/// <summary>
	/// The type of text box token.
	/// </summary>
	public enum TextBoxTokenType {
		/// <summary>
		/// A token that indicates a directive.
		/// </summary>
		Directive,
		/// <summary>
		/// A token that indicates a comment.
		/// </summary>
		Comment,
		/// <summary>
		/// A token that indicates a script command.
		/// </summary>
		Command,
		/// <summary>
		/// A token that indicates plain text.
		/// </summary>
		Text,
	}
}
