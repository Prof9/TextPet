using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace LibTextPet.General {
	/// <summary>
	/// A collection of values indexed by their names, case insensitive.
	/// </summary>
	public class NamedCollection<T> : KeyedCollection<string, T> where T : INameable {
		/// <summary>
		/// Constructs an empty collection.
		/// </summary>
		public NamedCollection()
			: base(StringComparer.OrdinalIgnoreCase) { }

		/// <summary>
		/// Constructs a collection containing the given values.
		/// </summary>
		/// <param name="values">The values this collection should contain.</param>
		protected NamedCollection(IEnumerable<T> values)
			: base(StringComparer.OrdinalIgnoreCase) {
			if (values == null)
				throw new ArgumentNullException(nameof(values), "The values cannot be null.");

			foreach (T value in values) {
				Add(value);
			}
		}

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
