using System;
using System.Collections.Generic;

namespace LibTextPet.General
{
	public class ByteSequenceEqualityComparer : IEqualityComparer<IEnumerable<byte>>
	{
		// Singleton design pattern
		private static ByteSequenceEqualityComparer instance;
		public static ByteSequenceEqualityComparer Instance {
			get {
				if (instance == null) {
					instance = new ByteSequenceEqualityComparer();
				}
				return instance;
			}
		}

		private ByteSequenceEqualityComparer() { }

		/// <summary>
		/// Determines whether the specified byte sequences are equal.
		/// </summary>
		/// <param name="x">The first byte sequence to compare.</param>
		/// <param name="y">The second byte sequence to compare.</param>
		/// <returns>true if the specified byte sequences are equal; otherwise, false.</returns>
		public bool Equals(IEnumerable<byte> x, IEnumerable<byte> y)
		{
			// check if same reference, or both null
			if (x == y)
				return true;

			// check if either is null
			if (x == null || y == null)
				return false;
			
			IEnumerator<byte> xe = x.GetEnumerator();
			IEnumerator<byte> ye = y.GetEnumerator();
			bool xHas, yHas;

			// compare all elements
			while (true) {
				xHas = xe.MoveNext();
				yHas = ye.MoveNext();

				// end reached
				if (!xHas && !yHas)
					break;

				// length differs
				if (xHas != yHas)
					return false;

				// element differs
				if (xe.Current != ye.Current)
					return false;
			}

			// all tests passed
			return true;
		}

		/// <summary>
		/// Returns a hash code for the specified byte sequence;
		/// </summary>
		/// <param name="obj">The byte sequence for which a hash code is to be returned.</param>
		/// <returns>A hash code for the specified byte sequence.</returns>
		public int GetHashCode(IEnumerable<byte> obj)
		{
			if (obj == null)
				throw new ArgumentNullException(nameof(obj), "The byte sequence cannot be null.");

			// compute FNV-1a hash
			int hash = -2128831035;
			unchecked
			{
				foreach (byte b in obj)
					hash ^= b;
					hash *= 16777619;
			}
			return hash;
		}
	}
}
