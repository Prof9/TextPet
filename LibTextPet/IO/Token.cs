using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LibTextPet.IO {
	/// <summary>
	/// A single token.
	/// </summary>
	public struct Token {
		/// <summary>
		/// Gets the class of token.
		/// </summary>
		public int Class { get; private set; }
		/// <summary>
		/// Gets the token string.
		/// </summary>
		public string Value { get; private set; }

		/// <summary>
		/// Creates a new token with the given type and string.
		/// </summary>
		/// <param name="type">The type of the token.</param>
		/// <param name="value">The token string.</param>
		public Token(int type, string value) {
			if (value == null)
				throw new ArgumentNullException(nameof(value), "The token string cannot be null.");

			this.Class = type;
			this.Value = value;
		}

		public override bool Equals(object obj) {
			if (obj == null || GetType() != obj.GetType())
				return false;

			Token token = (Token)obj;

			return this.Class == token.Class && this.Value == token.Value;
		}

		public override int GetHashCode() {
			return this.Value.GetHashCode() ^ this.Class.GetHashCode();
		}

		public static bool operator ==(Token token1, Token token2) {
			if (ReferenceEquals(token1, token2)) {
				return true;
#pragma warning disable IDE0041 // Use 'is null' check
			} else if (ReferenceEquals(token1, null) || ReferenceEquals(token2, null)) {
#pragma warning restore IDE0041 // Use 'is null' check
				return false;
			} else {
				return token1.Equals(token2);
			}
		}

		public static bool operator !=(Token token1, Token token2) {
			return !(token1 == token2);
		}
    }
}
