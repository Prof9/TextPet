using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LibTextPet.General {
	/// <summary>
	/// Represents a node in the tree with any number of children, optionally containing a value.
	/// </summary>
	internal class LookupTreeNode<TKeyElement, TValue> {
		private TValue _value;
		private IEqualityComparer<TKeyElement> equalityComparer;

		/// <summary>
		/// Gets the children of this tree node.
		/// </summary>
		private IDictionary<TKeyElement, LookupTreeNode<TKeyElement, TValue>> Children { get; set; }

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
		public LookupTreeNode<TKeyElement, TValue> GetChild(TKeyElement keyElement)
			=> this.Children[keyElement];

		/// <summary>
		/// Gets the child of this tree node with the specified key element.
		/// </summary>
		/// <param name="keyElement">The key element.</param>
		/// <param name="child">When this method returns, the child, if it exists; otherwise, null.</param>
		/// <returns>true if there is a child; otherwise, false.</returns>
		public bool TryGetChild(TKeyElement keyElement, out LookupTreeNode<TKeyElement, TValue> child) {
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
		public LookupTreeNode(TValue value) {
			this.Value = value;
			this.HasValue = true;
		}

		/// <summary>
		/// Creates a new tree node with no value.
		/// </summary>
		public LookupTreeNode() {
			this.Value = default(TValue);
			this.HasValue = false;
		}

		/// <summary>
		/// Creates a new tree node with the specified value.
		/// </summary>
		/// <param name="value">The value of this tree node.</param>
		/// <param name="equalityComparer">The equality comparer to use for the child keys.</param>
		public LookupTreeNode(TValue value, IEqualityComparer<TKeyElement> equalityComparer)
			: this(value) {
			this.equalityComparer = equalityComparer;
		}

		/// <summary>
		/// Creates a new tree node with no value.
		/// </summary>
		/// <param name="equalityComparer">The equality comparer to use for the child keys.</param>
		public LookupTreeNode(IEqualityComparer<TKeyElement> equalityComparer)
			: this() {
			this.equalityComparer = equalityComparer;
		}

		/// <summary>
		/// Adds a child to this tree node.
		/// </summary>
		/// <param name="keyElement">The key element of the child.</param>
		/// <param name="child">The child node.</param>
		internal void AddChild(TKeyElement keyElement, LookupTreeNode<TKeyElement, TValue> child) {
			if (this.HasChild(keyElement))
				throw new ArgumentException("A child with this key element already exists.", nameof(keyElement));

			// Create child dictionary if it does not exist.
			if (this.Children == null) {
				if (this.equalityComparer != null) {
					this.Children = new Dictionary<TKeyElement, LookupTreeNode<TKeyElement, TValue>>(equalityComparer);
				} else {
					this.Children = new Dictionary<TKeyElement, LookupTreeNode<TKeyElement, TValue>>();
				}
			}
			// Add the child to the dictionary.
			this.Children.Add(keyElement, child);
		}
	}
}
