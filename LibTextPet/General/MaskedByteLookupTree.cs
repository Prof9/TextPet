using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LibTextPet.General {
	internal class MaskedByteLookupTree<TValue> : LookupTree<MaskedByte, TValue> {
		internal LookupTreeNode<MaskedByte, TValue>[] FirstNodes { get; }

		public MaskedByteLookupTree()
			: base() {
			this.FirstNodes = new LookupTreeNode<MaskedByte, TValue>[256];
			// Overwrite path with our custom path class.
			this.Path = new MaskedByteLookupTreePath<TValue>(this);
		}

		public override void Add(IList<MaskedByte> key, TValue value) {
			if (key == null)
				throw new ArgumentNullException(nameof(key));
			if (!key.Any())
				throw new ArgumentException("The key cannot be empty.", nameof(key));

			base.Add(key, value);

			// Go through every variation on the first key element.
			MaskedByte first = key[0];
			LookupTreeNode<MaskedByte, TValue> bypass = null;
			for (int i = 0; i < 256; i++) {
				int b = first.Byte | (i & ~first.Mask);

				// Add a bypass for this variation if it does not exist yet.
				if (this.FirstNodes[b] == null) {
					if (bypass == null) {
						bypass = this.RootNode.GetChild(key[0]);
					}

					this.FirstNodes[b] = bypass;
				}
			}
		}

		public override LookupTreePath<MaskedByte, TValue> BeginPath() {
			return new MaskedByteLookupTreePath<TValue>(this);
		}

		internal override LookupTreeNode<MaskedByte, TValue> CreateNode(int depth) {
			// Use common bits comparer for all child nodes.
			return new LookupTreeNode<MaskedByte, TValue>(CommonBitsEqualityComparer.Instance);
		}
	}
}
