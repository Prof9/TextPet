using LibTextPet.General;
using LibTextPet.IO.Msg;
using LibTextPet.Msg;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LibTextPet.IO {
	/// <summary>
	/// An patcher that patches new text boxes into base scripts.
	/// </summary>
	public class ScriptTextBoxPatcher : IPatcher<Script> {
		/// <summary>
		/// Gets the scripts that are used for text box splitting.
		/// </summary>
		protected ReadOnlyNamedCollection<CommandDatabase> Databases { get; }

		/// <summary>
		/// Creates a new text box patcher that uses the specified command databases.
		/// </summary>
		/// <param name="databases">The command databases to use.</param>
		public ScriptTextBoxPatcher(params CommandDatabase[] databases) {
			if (databases == null)
				throw new ArgumentNullException(nameof(databases), "The command databases cannot be null.");

			this.Databases = new ReadOnlyNamedCollection<CommandDatabase>(databases);
		}

		/// <summary>
		/// Patches the text boxes from the specified patch script into the specified base script.
		/// </summary>
		/// <param name="baseObj">The base script to patch new text boxes into.</param>
		/// <param name="patchObj">The patch script containing new text boxes.</param>
		public void Patch(Script baseObj, Script patchObj) {
			if (baseObj == null)
				throw new ArgumentNullException(nameof(baseObj), "The base script cannot be null.");
			if (patchObj == null)
				throw new ArgumentNullException(nameof(patchObj), "The patch script cannot be null.");
			
			IList<Command> boxA;
			Script boxB;

			// Load the text box split script, if there is one.
			Script textBoxSplitScript = null;
			byte[] writtenTextBoxSplitScript = null;
			if (this.Databases.Contains(baseObj.DatabaseName)) {
				// Load the database.
				CommandDatabase db = this.Databases[baseObj.DatabaseName];
				if (db.TextBoxSplitScript != null && db.TextBoxSplitScript.Any()) {
					textBoxSplitScript = db.TextBoxSplitScript;

					// Write the text box split script to a byte array.
					using (MemoryStream ms = new MemoryStream()) {
						BinaryScriptWriter writer = new BinaryScriptWriter(ms, new UTF8Encoding(false, true));
						writer.Write(textBoxSplitScript);
						writtenTextBoxSplitScript = ms.ToArray();
					}
				}
			}

			for (int a = 0, b = 0; a < baseObj.Count; a++) {
				// Skip any elements that are not part of a text box.
				if (IsPrinted(baseObj[a])) {
					// Start of a new text box.
					// Advance B to the next text box.
					while (b != 0 && b < patchObj.Count) {
						DirectiveElement directive = patchObj[b++] as DirectiveElement;
						if (directive != null && directive.DirectiveType == DirectiveType.TextBoxSeparator) {
							break;
						}
					}

					// Do we have text left in B?
					if (b >= patchObj.Count) {
						throw new ArgumentException("The patch script must have the same number of text boxes as the base script.", nameof(patchObj));
					}

					// Extract text box from B.
					boxB = new Script(baseObj.DatabaseName);
					while (b < patchObj.Count && !EndsTextBox(patchObj[b])) {
						if (SplitsTextBox(patchObj[b])) {
							// Do we have a split script?
							if (textBoxSplitScript == null) {
								throw new ArgumentException("Command database \"" + baseObj.DatabaseName + "\" has no text box split script; text box splitting is not supported.", nameof(patchObj));
							}

							// Apply the text box split script to extend the current text box.
							foreach (IScriptElement elem in textBoxSplitScript) {
								boxB.Add(elem);
							}
						} else if (IsPrinted(patchObj[b])) {
							// Only extract those elements that are actually printed.
							boxB.Add(patchObj[b]);
						}
						b++;
					}

					// If box B is empty, check if the next few commands match the text box split script.
					if (!boxB.Any()) {
						// Do we have a split script?
						if (textBoxSplitScript == null) {
							throw new ArgumentException("Command database \"" + baseObj.DatabaseName + "\" has no text box split script; text box merging is not supported.", nameof(patchObj));
						}

						// Extract the next few commands from the base object.
						Script nextCommands = new Script(baseObj.DatabaseName);
						for (int i = 0, j = 0; i < textBoxSplitScript.Count && (i + j + a) < baseObj.Count; i++) {
							// Skip printed commands (j is used as skipped counter).
							if (IsPrinted(baseObj[i + j + a])) {
								j++;
								i--;
								continue;
							}
							nextCommands.Add(baseObj[i + j + a]);
						}

						// Write the next few commands to a byte array.
						byte[] writtenNextCommands;
						using (MemoryStream ms = new MemoryStream()) {
							BinaryScriptWriter writer = new BinaryScriptWriter(ms, new UTF8Encoding(false, true));
							writer.Write(nextCommands);
							writtenNextCommands = ms.ToArray();
						}

						// Check if equal.
						if (!ByteSequenceEqualityComparer.Instance.Equals(writtenTextBoxSplitScript, writtenNextCommands)) {
							throw new ArgumentException("Next commands following empty text box do not match text box split script. Could not merge text boxes.");
						}

						// Merge with the next text box by removing the split script commands.
						for (int i = 0, j = 0; i < textBoxSplitScript.Count; i++) {
							// Don't discard printed commands.
							if (IsPrinted(baseObj[a + j])) {
								j++;
								i--;
							} else {
								baseObj.RemoveAt(a + j);
							}
						}
					} else {
						// Extract printed commands box from A.
						boxA = new List<Command>();
						while (a < baseObj.Count && !EndsTextBox(baseObj[a])) {
							// We only need to copy the commands, so discard the other elements.
							Command cmdA = baseObj[a] as Command;
							if (cmdA != null) {
								// Extract the command from A.
								boxA.Add(cmdA);
							}
							// Discard the printed element.
							baseObj.RemoveAt(a);
						}

						// Patch box B commands with commands in A.
						PatchTextBox(boxB, boxA);

						foreach (IScriptElement elem in boxB) {
							baseObj.Insert(a++, elem);
						}
					}

					// Rewind a by 1 so the text box-ending element is used as the next element.
					a--;
				}
			}
		}

		/// <summary>
		/// Replaces all script commands in the specified text box with commands in the specified list of commands.
		/// </summary>
		/// <param name="box">The text box.</param>
		/// <param name="commands">The commands to replace with.</param>
		private static void PatchTextBox(Script box, IList<Command> commands) {
			for (int b = 0; b < box.Count; b++) {
				Command cmdB = box[b] as Command;
				if (cmdB == null) {
					continue;
				}

				// Find corresponding command in A.
				bool found = false;
				for (int a = 0; a < commands.Count; a++) {
					Command cmdA = commands[a];
					if (cmdA.Name.Equals(cmdB.Name, StringComparison.OrdinalIgnoreCase)) {
						// Replace the placeholder command in B.
						box[b] = cmdA;
						// Remove the command from A.
						commands.RemoveAt(a);
						// Found the command in A.
						found = true;
						break;
					}
				}
				if (!found) {
					throw new ArgumentException("Could not find a corresponding command in the base script for command \"" + cmdB.Name + "\" in the text script.", nameof(box));
				}
			}
			// Check if all commands were put in.
			if (commands.Any()) {
				throw new ArgumentException("Not all commands are accounted for in the text box.", nameof(box));
			}
		}

		/// <summary>
		/// Checks whether the specified script element prints to the screen.
		/// </summary>
		/// <param name="element">The element to check.</param>
		/// <returns>true if the element prints to the screen; otherwise, false.</returns>
		protected static bool IsPrinted(IScriptElement element) {
			if (element == null)
				throw new ArgumentNullException(nameof(element), "The script element cannot be null.");

			// Treat text and raw byte as part of text box.
			if (element is TextElement || element is ByteElement) {
				return true;
			}

			// Treat printable command as part of text box.
			Command cmd = element as Command;
			if (cmd != null && cmd.Definition.Prints) {
				return true;
			}

			return false;
		}

		/// <summary>
		/// Checks whether the specified script element ends the current text box. This is the case if the element is a non-printing command,
		/// a text box separator, or the start of a new script or text archive.
		/// </summary>
		/// <param name="element">The element to check.</param>
		/// <returns>true if the element ends the current text box; otherwise, false.</returns>
		protected static bool EndsTextBox(IScriptElement element) {
			if (element == null)
				throw new ArgumentNullException(nameof(element), "The script element cannot be null.");

			Command cmd = element as Command;
			if (cmd != null && !cmd.Definition.Prints) {
				return true;
			}

			DirectiveElement directive = element as DirectiveElement;
			if (directive != null && (
				directive.DirectiveType == DirectiveType.TextBoxSeparator ||
				directive.DirectiveType == DirectiveType.Script ||
				directive.DirectiveType == DirectiveType.TextArchive
			)) {
				return true;
			}

			return false;
		}

		/// <summary>
		/// Checks whether the specified element splits the current text box.
		/// </summary>
		/// <param name="element">The element to check.</param>
		/// <returns>true if the element splits the current text box; otherwise, false.</returns>
		protected static bool SplitsTextBox(IScriptElement element) {
			if (element == null)
				throw new ArgumentNullException(nameof(element), "The script element cannot be null.");

			DirectiveElement directive = element as DirectiveElement;
			if (directive != null && directive.DirectiveType == DirectiveType.TextBoxSplit) {
				return true;
			} else {
				return false;
			}
		}
	}
}
