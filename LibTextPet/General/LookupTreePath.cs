using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LibTextPet.General {
	public class LookupTreePath<TKeyElement, TValue> : ICloneable where TKeyElement : IEquatable<TKeyElement> {
		/// <summary>
		/// Gets the tree.
		/// </summary>
		public LookupTree<TKeyElement, TValue> Tree { get; }

		/// <summary>
		/// Gets the current depth of the tree path.
		/// </summary>
		public int Depth { get; internal set; }
		/// <summary>
		/// The current node in the tree path.
		/// </summary>
		internal LookupTreeNode<TKeyElement, TValue> CurrentNode { get; set; }
		/// <summary>
		/// Gets a boolean that indicates whether the current node has a value.
		/// </summary>
		public bool AtValue => this.CurrentNode.HasValue;
		/// <summary>
		/// Gets the value of the current node, or the default value of the value type if it has none.
		/// </summary>
		public TValue CurrentValue => this.CurrentNode.Value;
		/// <summary>
		/// Gets or sets a boolean that indicates whether the tree path has hit a dead end.
		/// </summary>
		public bool AtEnd => !this.CurrentNode.HasChildren;

		internal LookupTreePath(LookupTree<TKeyElement, TValue> tree) {
			this.Tree = tree;
			this.Reset();
		}

		/// <summary>
		/// Resets the path through the tree.
		/// </summary>
		public void Reset() {
			this.Depth = 0;
			this.CurrentNode = this.Tree.RootNode;
		}

		/// <summary>
		/// Steps through the tree on the specified key element.
		/// </summary>
		/// <param name="keyElement">The key element.</param>
		/// <returns>true if the step moved to a new node; otherwise, false.</returns>
		public virtual bool StepNext(TKeyElement keyElement) {
			if (this.CurrentNode.TryGetChild(keyElement, out LookupTreeNode<TKeyElement, TValue> child)) {
				this.CurrentNode = child;
				this.Depth += 1;
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
		internal bool TryStepToValue(IEnumerator<TKeyElement> keyElementEnumerator, out TValue value) {
			if (keyElementEnumerator == null)
				throw new ArgumentNullException(nameof(keyElementEnumerator));

			// Root node is empty and should not have a value.
			while (keyElementEnumerator.MoveNext() && this.StepNext(keyElementEnumerator.Current)) {
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
		internal void StepToEnd(IEnumerator<TKeyElement> keyElementEnumerator) {
			// Step to next value until end reached.
			while (this.TryStepToValue(keyElementEnumerator, out _)) { }
		}

		public LookupTreePath<TKeyElement, TValue> Clone() {
			return new LookupTreePath<TKeyElement, TValue>(this.Tree) {
				Depth = this.Depth,
				CurrentNode = this.CurrentNode
			};
		}

		object ICloneable.Clone() {
			return this.Clone();
		}
	}
}
