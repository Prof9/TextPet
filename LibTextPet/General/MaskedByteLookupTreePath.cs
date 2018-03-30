using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LibTextPet.General {
	internal class MaskedByteLookupTreePath<TValue> : LookupTreePath<MaskedByte, TValue> {
		protected new MaskedByteLookupTree<TValue> Tree { get; }

		internal MaskedByteLookupTreePath(MaskedByteLookupTree<TValue> tree)
			: base(tree) {
			this.Tree = tree;
		}

		public override bool Step(MaskedByte keyElement) {
			if (this.Depth > 0) {
				return base.Step(keyElement);
			}

			// Use the bypasses.
			LookupTreeNode<MaskedByte, TValue> bypass = this.Tree.FirstNodes[keyElement.Byte];
			if (bypass == null) {
				return false;
			}

			this.CurrentNode = bypass;
			this.Depth = 1;
			return true;
		}
	}
}
