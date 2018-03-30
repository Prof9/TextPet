using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LibTextPet.General {
	public class LookupTree<TKeyElement, TValue> where TKeyElement : IEquatable<TKeyElement> {
		private IEqualityComparer<TKeyElement> equalityComparer;

		/// <summary>
		/// Gets the root node of the tree.
		/// </summary>
		internal LookupTreeNode<TKeyElement, TValue> RootNode { get; }

		internal LookupTreePath<TKeyElement, TValue> Path { get; set; }

		/// <summary>
		/// Creates an empty lookup tree.
		/// </summary>
		public LookupTree()
			: this(null) { }

		/// <summary>
		/// Creates an empty lookup tree.
		/// </summary>
		/// <param name="equalityComparer">The equality comparer to use for the node keys.</param>
		public LookupTree(IEqualityComparer<TKeyElement> equalityComparer) {
			this.equalityComparer = equalityComparer;

			if (equalityComparer != null) {
				this.RootNode = new LookupTreeNode<TKeyElement, TValue>(equalityComparer);
			} else {
				this.RootNode = new LookupTreeNode<TKeyElement, TValue>();
			}
			this.Path = new LookupTreePath<TKeyElement, TValue>(this);
			this.Height = 0;
		}

		/// <summary>
		/// Gets the current height of the tree.
		/// </summary>
		public int Height { get; private set; }

		/// <summary>
		/// Begins a new path through the current tree.
		/// </summary>
		/// <returns>The path.</returns>
		public virtual LookupTreePath<TKeyElement, TValue> BeginPath() {
			return new LookupTreePath<TKeyElement, TValue>(this);
		}

		/// <summary>
		/// Adds the specified item to the tree lookup.
		/// </summary>
		/// <param name="key">The key of the item to add.</param>
		/// <param name="value">The value of the item to add.</param>
		public virtual void Add(IList<TKeyElement> key, TValue value) {
			if (key == null)
				throw new ArgumentNullException(nameof(key));
			if (!key.Any())
				throw new ArgumentException("The key cannot be empty.", nameof(key));

			this.Path.Reset();
			this.Path.StepToEnd(key.GetEnumerator());

			// Add nodes until desired depth reached.
			while (this.Path.Depth < key.Count) {
				LookupTreeNode<TKeyElement, TValue> node = this.CreateNode(this.Path.Depth);
				this.Path.CurrentNode.AddChild(key[this.Path.Depth++], node);
				this.Path.CurrentNode = node;
			}

			// Set value for the node.
			if (this.Path.CurrentNode.HasValue) {
				throw new ArgumentException("An item with this key already exists in the tree.", nameof(key));
			}
			this.Path.CurrentNode.Value = value;

			if (this.Path.Depth > this.Height) {
				this.Height = this.Path.Depth;
			}
		}

		internal virtual LookupTreeNode<TKeyElement, TValue> CreateNode(int depth) {
			LookupTreeNode<TKeyElement, TValue> node;
			if (this.equalityComparer != null) {
				node = new LookupTreeNode<TKeyElement, TValue>(this.equalityComparer);
			} else {
				node = new LookupTreeNode<TKeyElement, TValue>();
			}

			return node;
		}

		/// <summary>
		/// Finds all matches for values associated with the key(s) read from the specified key element enumerator.
		/// </summary>
		/// <param name="keyElementEnumerator">The key element enumerator to read from.</param>
		/// <returns>The values that matched, ordered by ascending key length.</returns>
		public IEnumerable<TValue> Match(IEnumerator<TKeyElement> keyElementEnumerator) {
			if (keyElementEnumerator == null)
				throw new ArgumentNullException(nameof(keyElementEnumerator));

			this.Path.Reset();
			while (this.Path.TryStepToValue(keyElementEnumerator, out TValue value)) {
				yield return value;
			}
		}

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
