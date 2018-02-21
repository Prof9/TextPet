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
	public interface ILookupMap<TKeyElement, TValue> {
		/// <summary>
		/// Gets the amount of key elements that have been read in the current lookup.
		/// </summary>
		int ElementsRead { get; }

		/// <summary>
		/// Adds the specified item to the tree lookup.
		/// </summary>
		/// <param name="key">The key of the item to add.</param>
		/// <param name="value">The value of the item to add.</param>
		void Add(IList<TKeyElement> key, TValue value);
		/// <summary>
		/// Finds all matches for values associated with the key(s) read from the specified key element enumerator.
		/// </summary>
		/// <param name="keyElementEnumerator">The key element enumerator to read from.</param>
		/// <returns>The values that matched, ordered by ascending key length.</returns>
		IEnumerable<TValue> Match(IEnumerator<TKeyElement> keyElementEnumerator);
		/// <summary>
		/// Finds the first match for the value associated with the key read from the specified key element enumerator.
		/// </summary>
		/// <param name="keyElementEnumerator">The key element enumerator to read from.</param>
		/// <param name="value">When this method returns, the first value that matched, if one did; otherwise, the default value for the value type.</param>
		/// <returns>true if a match was found; otherwise, false.</returns>
		bool TryMatchFirst(IEnumerator<TKeyElement> keyElementEnumerator, out TValue value);
		/// <summary>
		/// Finds the last match for the value associated with the key read from the specified key element enumerator.
		/// </summary>
		/// <param name="keyElementEnumerator">The key element enumerator to read from.</param>
		/// <param name="value">When this method returns, the last value that matched, if one did; otherwise, the default value for the value type.</param>
		/// <returns>true if a match was found; otherwise, false.</returns>
		bool TryMatchLast(IEnumerator<TKeyElement> keyElementEnumerator, out TValue value);
	}
}
