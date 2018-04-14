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
	public struct MaskedByte : IEquatable<MaskedByte> {
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
		public MaskedByte(int baseValue, int mask) {
			this.Byte = (byte)(baseValue & mask);
			this.Mask = (byte)mask;
		}

		public override int GetHashCode() {
			return this.Byte | (this.Mask << 8);
		}

		public override bool Equals(object obj) {
			if (obj is MaskedByte mb) {
				return this.Equals(mb);
			} else {
				return false;
			}
		}

		public bool Equals(MaskedByte other) {
			return this.Byte == other.Byte
				&& this.Mask == other.Mask;
		}

		public static bool operator ==(MaskedByte mb1, MaskedByte mb2) {
			return mb1.Equals(mb2);
		}

		public static bool operator !=(MaskedByte mb1, MaskedByte mb2) {
			return !mb1.Equals(mb2);
		}
	}
}
