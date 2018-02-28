using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace LibTextPet.General {
	/// <summary>
	/// Represents a byte masked with a bit pattern.
	/// </summary>
	[ImmutableObject(true)]
	public struct MaskedByte : IEquatable<MaskedByte>, IEquatable<byte> {
		/// <summary>
		/// Gets the base byte value of this masked byte.
		/// </summary>
		public byte Byte { get; }
		/// <summary>
		/// Gets the mask bit pattern of this masked byte.
		/// </summary>
		public byte Mask { get; }

		/// <summary>
		/// Creates a new masked byte with the specified base byte value and mask.
		/// </summary>
		/// <param name="baseValue">The base value.</param>
		/// <param name="mask">The mask.</param>
		public MaskedByte(byte baseValue, byte mask) {
			this.Byte = baseValue;
			this.Mask = mask;
		}

		public override int GetHashCode() {
			// Generates same hash code as byte for mask = 0xFF.
			return this.Byte.GetHashCode() | (~this.Mask << 8);
		}

		public override bool Equals(object obj) {
			if (obj is MaskedByte mb) {
				return this.Equals(mb);
			} else if (obj is byte b) {
				return this.Equals(b);
			} else {
				return false;
			}
		}

		/// <summary>
		/// Checks if this masked byte equals another masked byte. The comparison is
		/// performed by masking the two base bytes with the mask of the other.
		/// NOTE: This tests equality, NOT equivalence!
		/// </summary>
		/// <param name="other">The masked byte to compare to.</param>
		/// <returns>true if the two masked bytes are equal; otherwise, false.</returns>
		public bool Equals(MaskedByte other) {
			return (this.Byte & other.Mask) == (other.Byte & this.Mask);
		}

		/// <summary>
		/// Checks if this masked byte equals a regular byte. The comparison is
		/// performed by masking the regular byte with the mask of this masked byte.
		/// NOTE: This tests equality, NOT equivalence!
		/// </summary>
		/// <param name="other">The byte to compare to.</param>
		/// <returns>true if the two masked bytes are equal; otherwise, false.</returns>
		public bool Equals(byte other) {
			return this.Byte == (other & this.Mask);
		}

		public static bool operator ==(MaskedByte maskedByte1, MaskedByte maskedByte2) {
			return maskedByte1.Equals(maskedByte2);
		}
		public static bool operator !=(MaskedByte maskedByte1, MaskedByte maskedByte2) {
			return !maskedByte1.Equals(maskedByte2);
		}
		public static bool operator ==(MaskedByte maskedByte, byte value) {
			return maskedByte.Equals(value);
		}
		public static bool operator !=(MaskedByte maskedByte, byte value) {
			return !maskedByte.Equals(value);
		}
		public static bool operator ==(byte value, MaskedByte maskedByte) {
			return maskedByte.Equals(value);
		}
		public static bool operator !=(byte value, MaskedByte maskedByte) {
			return !maskedByte.Equals(value);
		}
	}
}
