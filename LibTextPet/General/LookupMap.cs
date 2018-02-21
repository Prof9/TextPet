using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LibTextPet.General {
	/// <summary>
	/// A structure that allows looking up values by keys composed of multiple elements.
	/// </summary>
	/// <typeparam name="TKeyElement">The type of the key elements.</typeparam>
	/// <typeparam name="TValue">The type of the values.</typeparam>
	public abstract class LookupMap<TKeyElement, TValue> : ILookupMap<TKeyElement, TValue> {
		public abstract void Add(IList<TKeyElement> key, TValue value);
		public abstract IEnumerable<TValue> Match(IEnumerator<TKeyElement> keyElementEnumerator);
		public abstract int ElementsRead { get; }

		/// <summary>
		/// Finds the first match for the value associated with the key read from the specified key element enumerator.
		/// </summary>
		/// <param name="keyElementEnumerator">The key element enumerator to read from.</param>
		/// <param name="value">When this method returns, the first value that matched, if one did; otherwise, the default value for the value type.</param>
		/// <returns>true if a match was found; otherwise, false.</returns>
		public bool TryMatchFirst(IEnumerator<TKeyElement> keyElementEnumerator, out TValue value) {
			foreach (TValue match in this.Match(keyElementEnumerator)) {
				value = match;
				return true;
			}
			value = default(TValue);
			return false;
		}

		/// <summary>
		/// Finds the last match for the value associated with the key read from the specified key element enumerator.
		/// </summary>
		/// <param name="keyElementEnumerator">The key element enumerator to read from.</param>
		/// <param name="value">When this method returns, the last value that matched, if one did; otherwise, the default value for the value type.</param>
		/// <returns>true if a match was found; otherwise, false.</returns>
		public bool TryMatchLast(IEnumerator<TKeyElement> keyElementEnumerator, out TValue value) {
			bool found = false;
			value = default(TValue);
			foreach (TValue match in this.Match(keyElementEnumerator)) {
				value = match;
				found = true;
			}
			return found;
		}
	}
}
