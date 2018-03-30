using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LibTextPet.General {
	/// <summary>
	/// An equality comparer for masked bytes that compares (masked) bytes on their common bits.
	/// </summary>
	public class CommonBitsEqualityComparer : IEqualityComparer<MaskedByte> {
		// Singleton design pattern
		private static CommonBitsEqualityComparer _instance;
		public static CommonBitsEqualityComparer Instance => _instance ?? (_instance = new CommonBitsEqualityComparer());

		protected CommonBitsEqualityComparer() { }

		public bool Equals(MaskedByte x, MaskedByte y) {
			return (x.Byte & y.Mask) == (y.Byte & x.Mask);
		}

		public bool Equals(MaskedByte x, byte y) {
			return x.Byte == (y & x.Mask);
		}

		public int GetHashCode(MaskedByte obj) {
			// Not possible to generate a unique hash code as common bits "equality" is not transitive.
			return 0;
		}
	}
}
