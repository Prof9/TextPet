using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LibTextPet.Msg {
	/// <summary>
	/// An enum that indicates how string length is calculated.
	/// </summary>
	public enum StringLengthUnit {
		/// <summary>
		/// Indicates that string length is calculated from the number of distinct encoded character codes.
		/// </summary>
		Char,
		/// <summary>
		/// Indicates that string length is calculated from the number of bytes.
		/// </summary>
		Byte
	}
}
