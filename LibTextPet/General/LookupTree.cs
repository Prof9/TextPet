using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LibTextPet.General {
	public class LookupTree<TKeyElement, TValue> where TKeyElement : IEquatable<TKeyElement> {
		/// <summary>
		/// Represents a node in the tree with any number of children, optionally containing a value.
		/// </summary>
		protected class TreeNode {
			private TValue _value;

			/// <summary>
			/// Gets the children of this tree node.
			/// </summary>
			private IDictionary<TKeyElement, TreeNode> Children { get; set; }

			/// <summary>
			/// Gets a boolean that indicates whether this tree node has any children.
			/// </summary>
			public bool HasChildren
				=> this.Children?.Any() ?? false;

			/// <summary>
			/// Gets a boolean that indicates whether this tree node has a child with the specified key element.
			/// </summary>
			/// <param name="keyElement">The key element.</param>
			/// <returns>true if there is a child; otherwise, false.</returns>
			public bool HasChild(TKeyElement keyElement)
				=> this.Children?.ContainsKey(keyElement) ?? false;

			/// <summary>
			/// Gets the child of this tree node with the specified key element.
			/// </summary>
			/// <param name="keyElement">The key element.</param>
			/// <returns>The child.</returns>
			public TreeNode GetChild(TKeyElement keyElement)
				=> this.Children[keyElement];

			/// <summary>
			/// Gets the child of this tree node with the specified key element.
			/// </summary>
			/// <param name="keyElement">The key element.</param>
			/// <param name="child">When this method returns, the child, if it exists; otherwise, null.</param>
			/// <returns>true if there is a child; otherwise, false.</returns>
			public bool TryGetChild(TKeyElement keyElement, out TreeNode child) {
				if (this.Children == null || !this.Children.TryGetValue(keyElement, out child)) {
					child = null;
					return false;
				} else {
					return true;
				}
			}

			/// <summary>
			/// Gets or sets the value for this tree node.
			/// </summary>
			public TValue Value {
				get {
					return this._value;
				}
				set {
					this._value = value;
					this.HasValue = true;
				}
			}

			/// <summary>
			/// Gets a boolean that indicates whether this tree node has a value.
			/// </summary>
			public bool HasValue { get; private set; }

			/// <summary>
			/// Creates a new tree node with the specified value.
			/// </summary>
			/// <param name="value">The value of this tree node.</param>
			public TreeNode(TValue value) {
				this.Value = value;
				this.HasValue = true;
			}

			/// <summary>
			/// Creates a new tree node with no value.
			/// </summary>
			public TreeNode() {
				this.Value = default(TValue);
				this.HasValue = false;
			}

			/// <summary>
			/// Adds a child to this tree node.
			/// </summary>
			/// <param name="keyElement">The key element of the child.</param>
			/// <param name="child">The child node.</param>
			public void AddChild(TKeyElement keyElement, TreeNode child) {
				if (this.HasChild(keyElement))
					throw new ArgumentException("A child with this key element already exists.", nameof(keyElement));

				// Create child dictionary if it does not exist.
				if (this.Children == null) {
					this.Children = new Dictionary<TKeyElement, TreeNode>();
				}
				// Add the child to the dictionary.
				this.Children.Add(keyElement, child);
			}
		}

		/// <summary>
		/// Gets the root node of the tree.
		/// </summary>
		protected TreeNode RootNode { get; }

		/// <summary>
		/// Creates an empty lookup tree.
		/// </summary>
		public LookupTree()
			: base() {
			this.RootNode = new TreeNode();
			this.Reset();
			this.Height = 0;
		}

		/// <summary>
		/// Gets the current height of the tree.
		/// </summary>
		public int Height { get; private set; }

		/// <summary>
		/// Gets the current depth of the tree path.
		/// </summary>
		public int CurrentDepth { get; private set; }
		/// <summary>
		/// Gets the current tree node in the tree path.
		/// </summary>
		protected TreeNode CurrentNode { get; private set; }
		/// <summary>
		/// Gets a boolean that indicates whether the current tree node has a value.
		/// </summary>
		public bool AtValue => this.CurrentNode.HasValue;
		/// <summary>
		/// Gets the value of the current tree node, or the default value of the value type if it has none.
		/// </summary>
		public TValue CurrentValue => this.CurrentNode.Value;

		/// <summary>
		/// Begins a path through the current tree.
		/// </summary>
		public void Reset() {
			this.CurrentDepth = 0;
			this.CurrentNode = this.RootNode;
		}

		/// <summary>
		/// Steps through the tree on the specified key element.
		/// </summary>
		/// <param name="keyElement">The key element.</param>
		/// <returns>true if the step moved to a new node; otherwise, false.</returns>
		public bool Step(TKeyElement keyElement) {
			if (this.CurrentNode.TryGetChild(keyElement, out TreeNode child)) {
				this.CurrentNode = child;
				this.CurrentDepth += 1;
				return true;
			} else {
				return false;
			}
		}

		/// <summary>
		/// Steps through the tree to the next value.
		/// </summary>
		/// <param name="keyElementEnumerator">The enumerator for key elements.</param>
		/// <param name="value">When this method returns, the next value, if one was found; otherwise, the default value of the value type.</param>
		/// <returns>true if a value was found; otherwise, false.</returns>
		protected bool TryStepToValue(IEnumerator<TKeyElement> keyElementEnumerator, out TValue value) {
			if (keyElementEnumerator == null)
				throw new ArgumentNullException(nameof(keyElementEnumerator));

			// Root node is empty and should not have a value.
			while (keyElementEnumerator.MoveNext() && this.Step(keyElementEnumerator.Current)) {
				if (this.CurrentNode.HasValue) {
					value = this.CurrentNode.Value;
					return true;
				}
			}

			// End reached.
			value = default(TValue);
			return false;
		}

		/// <summary>
		/// Step through the tree until a dead end is reached.
		/// </summary>
		/// <param name="keyElementEnumerator">The enumerator for key elements.</param>
		/// <returns>true if a value was found; otherwise, false.</returns>
		protected void StepToEnd(IEnumerator<TKeyElement> keyElementEnumerator) {
			// Step to next value until end reached.
			while (this.TryStepToValue(keyElementEnumerator, out _)) { }
		}

		/// <summary>
		/// Adds the specified item to the tree lookup.
		/// </summary>
		/// <param name="key">The key of the item to add.</param>
		/// <param name="value">The value of the item to add.</param>
		public void Add(IList<TKeyElement> key, TValue value) {
			if (key == null)
				throw new ArgumentNullException(nameof(key));
			if (!key.Any())
				throw new ArgumentException("The key cannot be empty.", nameof(key));

			this.Reset();
			this.StepToEnd(key.GetEnumerator());

			// Add nodes until desired depth reached.
			while (this.CurrentDepth < key.Count) {
				TreeNode node = new TreeNode();
				this.CurrentNode.AddChild(key[this.CurrentDepth++], node);
				this.CurrentNode = node;
			}

			// Set value for the node.
			if (this.CurrentNode.HasValue) {
				throw new ArgumentException("An item with this key already exists in the tree.", nameof(key));
			}
			CurrentNode.Value = value;

			if (this.CurrentDepth > this.Height) {
				this.Height = this.CurrentDepth;
			}
		}

		/// <summary>
		/// Finds all matches for values associated with the key(s) read from the specified key element enumerator.
		/// </summary>
		/// <param name="keyElementEnumerator">The key element enumerator to read from.</param>
		/// <returns>The values that matched, ordered by ascending key length.</returns>
		public IEnumerable<TValue> Match(IEnumerator<TKeyElement> keyElementEnumerator) {
			if (keyElementEnumerator == null)
				throw new ArgumentNullException(nameof(keyElementEnumerator));

			this.Reset();
			while (this.TryStepToValue(keyElementEnumerator, out TValue value)) {
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
