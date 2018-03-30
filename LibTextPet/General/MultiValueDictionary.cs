using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace LibTextPet.General {
	/// <summary>
	/// A dictionary where a key can have any number of values.
	/// </summary>
	/// <typeparam name="TKey">The type of the keys.</typeparam>
	/// <typeparam name="TValue">The type of the values.</typeparam>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Multi")]
	public class MultiValueDictionary<TKey, TValue> : IDictionary<TKey, IList<TValue>> {
		private static ReadOnlyCollection<TValue> EmptySet = new ReadOnlyCollection<TValue>(new TValue[0]);

		private Dictionary<TKey, IList<TValue>> BaseDictionary;

		/// <summary>
		/// Creates a new empty multi-valued dictionary.
		/// </summary>
		public MultiValueDictionary() {
			this.BaseDictionary = new Dictionary<TKey, IList<TValue>>();
		}

		public IList<TValue> this[TKey key] {
			get {
				if (this.BaseDictionary.TryGetValue(key, out IList<TValue> list)) {
					return list;
				} else {
					return MultiValueDictionary<TKey, TValue>.EmptySet;
				}
			}
			set {
				throw new NotSupportedException("Multi-valued dictionary does not support setting multiple values at once.");
			}
		}

		/// <summary>
		/// Adds the specified value to the dictionary under the specified key.
		/// </summary>
		/// <param name="key">The key to add a value for.</param>
		public void Add(TKey key, TValue value) {
			if (this.BaseDictionary.TryGetValue(key, out IList<TValue> list)) {
				list.Add(value);
			} else {
				this.BaseDictionary[key] = new List<TValue>() { value };
			}
		}

		public void Add(KeyValuePair<TKey, TValue> item)
			=> this.Add(item.Key, item.Value);

		public ICollection<TKey> Keys => ((IDictionary<TKey, IList<TValue>>)this.BaseDictionary).Keys;

		public ICollection<IList<TValue>> Values => ((IDictionary<TKey, IList<TValue>>)this.BaseDictionary).Values;

		public int Count
			=> ((IDictionary<TKey, IList<TValue>>)this.BaseDictionary).Count;

		public bool IsReadOnly
			=> ((IDictionary<TKey, IList<TValue>>)this.BaseDictionary).IsReadOnly;

		public void Add(TKey key, IList<TValue> value)
			=> ((IDictionary<TKey, IList<TValue>>)this.BaseDictionary).Add(key, value);

		public void Add(KeyValuePair<TKey, IList<TValue>> item)
			=> ((IDictionary<TKey, IList<TValue>>)this.BaseDictionary).Add(item);

		public void Clear() 
			=> ((IDictionary<TKey, IList<TValue>>)this.BaseDictionary).Clear();

		public bool Contains(KeyValuePair<TKey, IList<TValue>> item)
			=> ((IDictionary<TKey, IList<TValue>>)this.BaseDictionary).Contains(item);

		public bool ContainsKey(TKey key)
			=> ((IDictionary<TKey, IList<TValue>>)this.BaseDictionary).ContainsKey(key);

		public void CopyTo(KeyValuePair<TKey, IList<TValue>>[] array, int arrayIndex) {
			((IDictionary<TKey, IList<TValue>>)this.BaseDictionary).CopyTo(array, arrayIndex);
		}

		public IEnumerator<KeyValuePair<TKey, IList<TValue>>> GetEnumerator()
			=> ((IDictionary<TKey, IList<TValue>>)this.BaseDictionary).GetEnumerator();

		public bool Remove(TKey key)
			=> ((IDictionary<TKey, IList<TValue>>)this.BaseDictionary).Remove(key);

		public bool Remove(KeyValuePair<TKey, IList<TValue>> item)
			=> ((IDictionary<TKey, IList<TValue>>)this.BaseDictionary).Remove(item);

		public bool TryGetValue(TKey key, out IList<TValue> value)
			=> ((IDictionary<TKey, IList<TValue>>)this.BaseDictionary).TryGetValue(key, out value);

		IEnumerator IEnumerable.GetEnumerator()
			=> ((IDictionary<TKey, IList<TValue>>)this.BaseDictionary).GetEnumerator();
	}
}
