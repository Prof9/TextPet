using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LibTextPet.General {
	/// <summary>
	/// A read-only collection of values indexed by their names, case insensitive.
	/// </summary>
	public class ReadOnlyNamedCollection<T> : ReadOnlyKeyedCollection<string, T> where T : INameable {
		/// <summary>
		/// Constructs a new named collection containing the given elements.
		/// </summary>
		/// <param name="elements">The elements this named collection should contain.</param>
		public ReadOnlyNamedCollection(IEnumerable<T> elements)
			: base(elements, StringComparer.OrdinalIgnoreCase) { }

		/// <summary>
		/// Extracts the name from the specified element.
		/// </summary>
		/// <param name="item">The element from which to extract the name.</param>
		/// <returns>The name for the specified element.</returns>
		protected override string GetKeyForItem(T item) {
			if (item == null)
				throw new ArgumentNullException(nameof(item), "The item cannot be null.");

			return item.Name;
		}
	}
}
