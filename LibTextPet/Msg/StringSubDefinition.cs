using LibTextPet.General;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LibTextPet.Msg {
	/// <summary>
	/// A definition of a string, to be used as a sub-definition in a command parameter.
	/// </summary>
	public sealed class StringSubDefinition : ICloneable {
		/// <summary>
		/// Gets the relative offset of the string.
		/// </summary>
		public int Offset { get; }
		/// <summary>
		/// Gets the length unit used for determining string length.
		/// </summary>
		public StringLengthUnit Unit { get; }
		/// <summary>
		/// Gets the fixed length of the string. A value of 0 indicates the string has a variable length.
		/// </summary>
		public int FixedLength { get; }

		/// <summary>
		/// Creates a new string sub-definition with the specified properties.
		/// </summary>
		/// <param name="offset">The offset of the string.</param>
		/// <param name="lengthUnit">The length unit to use.</param>
		/// <param name="fixedLength">The fixed length of the string, or 0 to allow a variable length.</param>
		public StringSubDefinition(int offset, StringLengthUnit lengthUnit, int fixedLength) {
			if (fixedLength < 0)
				throw new ArgumentOutOfRangeException(nameof(fixedLength), "The fixed string length cannot be negative.");

			this.Offset = offset;
			this.Unit = lengthUnit;
			this.FixedLength = fixedLength;
		}

		/// <summary>
		/// Creates a new string sub-definition that is a deep clone of the current instance.
		/// </summary>
		/// <returns>A new string sub-definition that is a deep clone of this instance.</returns>
		public StringSubDefinition Clone() {
			return new StringSubDefinition(
				this.Offset,
				this.Unit,
				this.FixedLength
			);
		}

		/// <summary>
		/// Creates a new object that is a deep clone of the current instance.
		/// </summary>
		/// <returns>A new object that is a deep clone of this instance.</returns>
		object ICloneable.Clone() {
			return this.Clone();
		}
	}
}
