using LibTextPet.Msg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LibTextPet.IO {
	/// <summary>
	/// An patcher that patches new text boxes into base text archives.
	/// </summary>
	public class TextArchiveTextBoxPatcher : IPatcher<TextArchive> {
		/// <summary>
		/// Gets the patcher that is used for patching scripts.
		/// </summary>
		protected IPatcher<Script> ScriptPatcher { get; }

		/// <summary>
		/// Creates a new text archive patcher using the specified command databases.
		/// </summary>
		/// <param name="databases">The command databases to use.</param>
		public TextArchiveTextBoxPatcher(params CommandDatabase[] databases) {
			if (databases == null)
				throw new ArgumentNullException(nameof(databases), "The command databases cannot be null.");

			this.ScriptPatcher = new ScriptTextBoxPatcher(databases);
		}

		/// <summary>
		/// Patches the text boxes from the specified patch text archive into the specified base text archive.
		/// </summary>
		/// <param name="baseObj">The base text archive to patch new text boxes into.</param>
		/// <param name="patchObj">The patch text archive containing new text boxes.</param>
		public void Patch(TextArchive baseObj, TextArchive patchObj) {
			if (baseObj == null)
				throw new ArgumentNullException(nameof(baseObj), "The base text archive cannot be null.");
			if (patchObj == null)
				throw new ArgumentNullException(nameof(patchObj), "The patch text archive cannot be null.");
			if (patchObj.Count > baseObj.Count)
				throw new ArgumentException("The number of scripts in the patch text archive exceeds the number of scripts in the base text archive.", nameof(patchObj));

			int total = Math.Min(baseObj.Count, patchObj.Count);
			for (int scriptNum = 0; scriptNum < total; scriptNum++) {
				// Is there a patch script available?
				if (patchObj[scriptNum] != null && patchObj[scriptNum].Count > 0) {
					this.ScriptPatcher.Patch(baseObj[scriptNum], patchObj[scriptNum]);
				}
			}
		}
	}
}
