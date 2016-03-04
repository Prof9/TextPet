using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace LibTextPet.General {
	/// <summary>
	/// A read-only collection whose keys are embedded in the values.
	/// </summary>
	public abstract class ReadOnlyKeyedCollection<TKey, TValue> : KeyedCollection<TKey, TValue> {
		private const string exceptionMessage = "This collection is read-only.";

		private bool locked;

		/// <summary>
		/// Constructs a read-only collection containing the given values that uses the specified equality comparer.
		/// </summary>
		/// <param name="values">The values this read-only collection should contain.</param>
		/// <param name="comparer">The equality comparer to use when comparing keys.</param>
		protected ReadOnlyKeyedCollection(IEnumerable<TValue> values, IEqualityComparer<TKey> comparer)
			: base(comparer) {
			if (values == null)
				throw new ArgumentNullException(nameof(values), "The values cannot be null.");

			this.locked = false;

			foreach (TValue value in values) {
				Add(value);
			}

			this.locked = true;
		}

		/// <summary>
		/// Constructs a read-only collection containing the given values.
		/// </summary>
		/// <param name="values">The values this read-only collection should contain.</param>
		protected ReadOnlyKeyedCollection(IEnumerable<TValue> values)
			: this(values, EqualityComparer<TKey>.Default) { }

		protected override void InsertItem(int index, TValue item) {
			if (locked)
				throw new NotSupportedException(exceptionMessage);
			if (item == null)
				throw new ArgumentNullException(nameof(item), "The item cannot be null.");

			base.InsertItem(index, item);
		}

		protected override void ClearItems() {
			throw new NotSupportedException(exceptionMessage);
		}

		protected override void RemoveItem(int index) {
			throw new NotSupportedException(exceptionMessage);
		}

		protected override void SetItem(int index, TValue item) {
			throw new NotSupportedException(exceptionMessage);
		}
	}
}
