using LibTextPet.General;
using LibTextPet.IO.TextBox;
using LibTextPet.Msg;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace LibTextPet.IO {
	/// <summary>
	/// An patcher that patches new text boxes into base text archives.
	/// </summary>
	public class TextArchiveTextBoxPatcher : IPatcher<TextArchive> {
		private static readonly Regex ImportScriptRegex = new Regex("^[^,]+,[^,]+$");

		/// <summary>
		/// Gets the patcher that is used for patching scripts.
		/// </summary>
		protected IPatcher<Script> ScriptPatcher { get; }

		/// <summary>
		/// Gets the command databases used by this patcher.
		/// </summary>
		protected ReadOnlyNamedCollection<CommandDatabase> Databases { get; }

		/// <summary>
		/// Creates a new text archive patcher using the specified command databases.
		/// </summary>
		/// <param name="databases">The command databases to use.</param>
		public TextArchiveTextBoxPatcher(params CommandDatabase[] databases) {
			if (databases == null)
				throw new ArgumentNullException(nameof(databases), "The command databases cannot be null.");

			this.ScriptPatcher = new ScriptTextBoxPatcher(databases);
			this.Databases = new ReadOnlyNamedCollection<CommandDatabase>(databases);
		}

		/// <summary>
		/// Patches the text boxes from the specified patch text archive into the specified base text archive.
		/// </summary>
		/// <param name="baseObj">The base text archive to patch new text boxes into.</param>
		/// <param name="patchObj">The patch text archive containing new text boxes.</param>
		public void Patch(TextArchive baseObj, TextArchive patchObj) {
			this.Patch(baseObj, patchObj, new TextArchive[0]);
		}

		/// <summary>
		/// Patches the text boxes from the specified patch text archive into the specified base text archive.
		/// </summary>
		/// <param name="baseObj">The base text archive to patch new text boxes into.</param>
		/// <param name="patchObj">The patch text archive containing new text boxes.</param>
		/// <param name="importableTAs">The set of text archives from which scripts can be imported.</param>
		public void Patch(TextArchive baseObj, TextArchive patchObj, IEnumerable<TextArchive> importableTAs) {
			if (baseObj == null)
				throw new ArgumentNullException(nameof(baseObj), "The base text archive cannot be null.");
			if (patchObj == null)
				throw new ArgumentNullException(nameof(patchObj), "The patch text archive cannot be null.");
			if (patchObj.Count > baseObj.Count)
				throw new ArgumentException("The number of scripts in the patch text archive (" + patchObj.Count + ") exceeds the number of scripts in the base text archive (" + baseObj.Count + ") for text archive " + baseObj.Identifier + ".", nameof(patchObj));

			int total = Math.Min(baseObj.Count, patchObj.Count);
			int scriptNum = 0;
#if !DEBUG
			try {
#endif
				for (scriptNum = 0; scriptNum < total; scriptNum++) {
					Script patchScript = patchObj[scriptNum];

					// Is there a patch script available?
					if (patchScript == null || patchScript.Count <= 0) {
						continue;
					}

					// Is this an imported script?
					if (patchScript[0] is DirectiveElement dirElem && dirElem.DirectiveType == DirectiveType.ImportScript) {
						if (patchScript.Count != 1) {
							throw new FormatException("Script import directive must be the only script element. (Script " + scriptNum + ", text archive " + patchObj.Identifier + ")");
						}
						if (!ImportScriptRegex.IsMatch(dirElem.Value)) {
							throw new FormatException("Invalid syntax for script import directive \"" + dirElem.Value + "\". (Script " + scriptNum + ", text archive " + patchObj.Identifier + ")");
						}

						patchScript = GetImportScript(baseObj, patchObj, importableTAs, scriptNum, dirElem);
						using (MemoryStream ms = new MemoryStream()) {
							TextBoxScriptWriter tbsw = new TextBoxScriptWriter(ms);
							TextBoxScriptTemplateReader tbstr = new TextBoxScriptTemplateReader(ms, this.Databases[patchScript.DatabaseName]);
							tbsw.Write(patchScript);
							ms.Position = 0;
							patchScript = tbstr.ReadSingle();
						}
					}

					if (patchScript != null && patchScript.Count > 0) {
						// Clone the patch script.
						Script patchScriptClone = new Script(patchScript.DatabaseName);
						foreach (IScriptElement elem in patchScript) {
							patchScriptClone.Add(elem);
						}

						this.ScriptPatcher.Patch(baseObj[scriptNum], patchScriptClone);
					}
				}
#if !DEBUG
			} catch (Exception ex) {
				// TODO: THIS BUT NOT AWFUL
				throw new InvalidOperationException(ex.Message + " (Error occurred in script " + scriptNum + " of text archive " + baseObj.Identifier + ")");
			}
#endif
		}

		private static Script GetImportScript(TextArchive baseObj, TextArchive patchObj, IEnumerable<TextArchive> importableTAs, int scriptNum, DirectiveElement dirElem) {
			Script patchScript;
			string[] importPars = dirElem.Value.Split(',');
			string importTAID = importPars[0];
			int importScriptNum = NumberParser.ParseInt32(importPars[1]);

			IList<TextArchive> importTAs = null;
			if (patchObj.Identifier == importTAID && patchObj[importScriptNum].Any()) {
				// Try to import from the current patch object if it's nonempty.
				importTAs = new TextArchive[] { patchObj };
			} else if (baseObj.Identifier == importTAID) {
				// Try to import from the current base object.
				importTAs = new TextArchive[] { baseObj };
			} else {
				importTAs = importableTAs.Where(ta => ta.Identifier == importTAID).ToList();
			}
			if (importTAs.Count < 1) {
				throw new InvalidDataException("Could not find text archive \"" + importTAID + "\" for script importing. (Script " + scriptNum + ", text archive " + patchObj.Identifier + ")");
			}
			if (importTAs.Count > 1) {
				throw new InvalidDataException("Ambiguous text archive \"" + importTAID + "\" for script importing. (Script " + scriptNum + ", text archive " + patchObj.Identifier + ")");
			}
			TextArchive importTA = importTAs[0];

			if (importScriptNum < 0 || importScriptNum >= importTA.Count) {
				throw new InvalidDataException("Cannot import script " + importScriptNum + "from text archive \"" + importTAID + "\"; script does not exist. (Script " + scriptNum + ", text archive " + patchObj.Identifier + ")");
			}

			patchScript = importTA[importScriptNum];
			return patchScript;
		}
	}
}
