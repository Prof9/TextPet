﻿using LibTextPet.General;
using LibTextPet.IO.Msg;
using LibTextPet.IO.TPL;
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
		/// Gets the command databases used by this patcher.
		/// </summary>
		protected ReadOnlyNamedCollection<CommandDatabase> Databases { get; }
		protected ReadOnlyNamedCollection<TPLCommandReader> CommandReaders { get; }

		/// <summary>
		/// Creates a new text box patcher that uses the specified command databases.
		/// </summary>
		/// <param name="databases">The command databases to use.</param>
		public ScriptTextBoxPatcher(params CommandDatabase[] databases) {
			if (databases == null)
				throw new ArgumentNullException(nameof(databases), "The command databases cannot be null.");

			this.Databases = new ReadOnlyNamedCollection<CommandDatabase>(databases);

			List<TPLCommandReader> cmdReaders = new List<TPLCommandReader>(databases.Length);
			using (MemoryStream dummyStream = new MemoryStream()) {
				// We're not actually reading from the dummy stream, so we can close it after this.
				foreach (CommandDatabase db in databases) {
					cmdReaders.Add(new TPLCommandReader(dummyStream, db));
				}
			}
			this.CommandReaders = new ReadOnlyNamedCollection<TPLCommandReader>(cmdReaders);
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

			IList<Command> cmds;
			Script newBox;

			// Load the text box split script, if there is one.
			IList<Command> splitSnippet = GetTextBoxSplitSnippet(baseObj);
			IList<string> splitCmdNames = splitSnippet?.Select(cmd => cmd.Name).ToList();

			int a = 0;
			int b = 0;
			while (a < baseObj.Count) {
				// Skip any elements that are not part of a text box.
				if (!IsPrinted(baseObj[a])) {
					a++;
					continue;
				}

				// Start of a new text box.
				// Advance B to the next text box.
				b = FindNextTextBox(patchObj, b);

				// Do we have text left in B?
				if (b >= patchObj.Count) {
					throw new ArgumentException("The patch script has fewer text boxes than the base script.", nameof(patchObj));
				}

				// Extract text box from B.
				newBox = RemoveSplitTextBox(patchObj, b);



				// If box B is empty, merge the current and next text box in the base script.
				if (!newBox.Any()) {
					// Do we have a split script?
					if (splitSnippet == null) {
						throw new ArgumentException("Command database \"" + baseObj.DatabaseName + "\" has no text box split script; text box merging is not supported.", nameof(patchObj));
					}

					MergeNextTextBox(baseObj, splitCmdNames, a);
				} else if (newBox.Count == 1 &&
					newBox[0] is DirectiveElement dirElem &&
					dirElem.DirectiveType == DirectiveType.TextBoxUnsplit) {

					string[] removedCmdNames = dirElem.Value.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
					MergeNextTextBox(baseObj, removedCmdNames, a);
				} else {
					// Remove the old text box from A.
					RemoveTextBoxCommands(baseObj, a);
					cmds = new List<Command>();

					// Process the directives in boxB.
					IList<DirectiveElement> directives = ExtractDirectives(newBox);
					if (!this.CommandReaders.Contains(baseObj.DatabaseName)) {
						throw new ArgumentException("Base script uses unknown command database \"" + baseObj.DatabaseName + "\".", nameof(baseObj));
					}
					ProcessDirectives(cmds, directives, this.CommandReaders[baseObj.DatabaseName]);

					// If we have no split snippet make sure we don't try to split the box
					if (splitSnippet == null && newBox.Any(elem => elem is DirectiveElement dir && dir.DirectiveType == DirectiveType.TextBoxSplit)) {
						throw new ArgumentException("Command database \"" + baseObj.DatabaseName + "\" has no text box split script; text box splitting is not supported.", nameof(patchObj));
					}

					// Patch box B commands with commands in A.
					PatchTextBox(newBox, cmds, splitSnippet);

					// Re-insert the patched text box in A.
					foreach (IScriptElement elem in newBox) {
						baseObj.Insert(a++, elem);
					}
				}
			}

			// Do we have text left?
			if (FindNextTextBox(patchObj, b) < patchObj.Count) {
				throw new ArgumentException("The patch script has more text boxes than the base script.", nameof(patchObj));
			}
		}

		/// <summary>
		/// Extracts all script commands from the current text box in the specified script, and discards any other commands from the script.
		/// </summary>
		/// <param name="script">The script to extract from.</param>
		/// <param name="index">The index at which to begin extracting.</param>
		/// <returns>The extracted script commands.</returns>
		private static void RemoveTextBoxCommands(Script script, int index) {
			while (index < script.Count && !EndsTextBox(script[index])) {
				script.RemoveAt(index);
			}
		}

		/// <summary>
		/// Extracts all relevant directives from the specified text box.
		/// </summary>
		/// <param name="box">The text box to extract directives from.</param>
		/// <returns>The extracted directives.</returns>
		private static IList<DirectiveElement> ExtractDirectives(Script box) {
			List<DirectiveElement> directives = new List<DirectiveElement>();

			for (int i = 0; i < box.Count; i++) {
				IScriptElement elem = box[i];

				if (elem is DirectiveElement directive) {
					if (directive.DirectiveType == DirectiveType.Command) {
						box.RemoveAt(i--);
						directives.Add(directive);
					}
				}
			}

			return directives;
		}

		/// <summary>
		/// Extracts a (possibly split) text box from the specified script at the specified index, removing the elements in the script.
		/// </summary>
		/// <param name="script">The script to extract a text box from.</param>
		/// <param name="index">The index to start extracting from.</param>
		/// <returns>The text box that was extracted.</returns>
		private static Script RemoveSplitTextBox(Script script, int index) {
			Script boxB = new Script();
			while (index < script.Count) {
				IScriptElement elem = script[index];

				if (SplitsTextBox(elem) || IsPrinted(elem)) {
					// Only extract those elements that are actually printed.
					boxB.Add(elem);
				}
				if (elem is DirectiveElement dirElem && (
					dirElem.DirectiveType == DirectiveType.Command ||
					dirElem.DirectiveType == DirectiveType.TextBoxUnsplit
				)) {
					// Also add directives that should be here.
					boxB.Add(elem);
				}
				script.RemoveAt(index);

				if (EndsTextBox(elem)) {
					break;
				}
			}

			return boxB;
		}

		/// <summary>
		/// Merges the next two text boxes in the specified script, by removing the script elements in the specified text box split script.
		/// </summary>
		/// <param name="script">The script to modify.</param>
		/// <param name="splitSnippet">The text box split commands to use.</param>
		/// <param name="index">The index at which to begin merging.</param>
		private static void MergeNextTextBox(Script script, IList<string> splitCmds, int index) {
			// Do we have a split script?
			if (splitCmds == null) {
				throw new ArgumentNullException(nameof(splitCmds), "The text box split script snippet cannot be null.");
			}

			// Merge with the next text box by removing the split script commands.
			for (int i = 0, j = 0; i < splitCmds.Count;) {
				if (j + index >= script.Count) {
					throw new ArgumentException("Not enough commands following empty text box. Could not merge text boxes.", nameof(script));
				}

				IScriptElement elem = script[j + index];

				// Skip printed commands (j is used as skipped counter).
				if (IsPrinted(script[j + index])) {
					j++;
					continue;
				}

				if (!(elem is Command cmd && cmd.Name.Equals(splitCmds[i], StringComparison.OrdinalIgnoreCase))) {
					throw new ArgumentException("Next commands following empty text box do not match text box split script. Could not merge text boxes.", nameof(script));
				}

				script.RemoveAt(index + j);
				i++;
			}
		}

		/// <summary>
		/// Finds the script command index of the start of the next text box in the specified script, starting from the specified position.
		/// </summary>
		/// <param name="script">The script to search.</param>
		/// <param name="start">The index to begin searching from.</param>
		/// <returns>The index of the start of the next text box, or the size of the script if no next text box was found.</returns>
		private static int FindNextTextBox(Script script, int start) {
			while (start < script.Count) {
				if (IsPrinted(script[start])) {
					break;
				}

				if (script[start] is DirectiveElement directive && (
					directive.DirectiveType == DirectiveType.TextBoxSeparator ||
					directive.DirectiveType == DirectiveType.TextBoxUnsplit
				)) {
					break;
				}

				start++;
			}

			return start;
		}

		/// <summary>
		/// Gets the text box split script snippet that are to be used for the specified script.
		/// </summary>
		/// <param name="script">The script to get the text box split script snippet for.</param>
		/// <returns>The text box split script snippet, or null if none were found.</returns>
		private IList<Command> GetTextBoxSplitSnippet(Script script) {
			IList<Command> snippet = null;
			if (this.Databases.Contains(script.DatabaseName)) {
				// Load the database.
				CommandDatabase db = this.Databases[script.DatabaseName];
				if (db.TextBoxSplitSnippet != null) {
					snippet = db.TextBoxSplitSnippet;
				}
			}

			return snippet;
		}

		/// <summary>
		/// Replaces all script commands in the specified text box with commands in the specified list of commands.
		/// </summary>
		/// <param name="box">The text box.</param>
		/// <param name="commands">The commands to replace with.</param>
		/// <param name="splitSnippet">The text box split script snippet to use.</param>
		private static void PatchTextBox(Script box, IList<Command> commands, IList<Command> splitSnippet) {
			for (int b = 0; b < box.Count; b++) {
				if (box[b] is DirectiveElement dirB && dirB.DirectiveType == DirectiveType.TextBoxSplit) {
					box.RemoveAt(b);
					foreach (Command splitCmd in splitSnippet) {
						Command cmd = (Command)splitCmd.Clone();
						box.Insert(b++, cmd);
					}
					b--;
					continue;
				}

				if (!(box[b] is Command cmdB)) {
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
				throw new ArgumentException("Not all commands are accounted for in the text box. Missing commands: " + String.Join(", ", commands.Select(cmd => cmd.Name)), nameof(box));
			}
		}

		/// <summary>
		/// Processes the specified directives for the specified commands.
		/// </summary>
		/// <param name="cmds">The commands to apply changes to.</param>
		/// <param name="directives">The directives to process.</param>
		/// <param name="cmdReader">The TPL command reader to use.</param>
		private static void ProcessDirectives(IList<Command> cmds, IList<DirectiveElement> directives, TPLReader<Command> cmdReader) {
			foreach (DirectiveElement directive in directives) {
				switch (directive.DirectiveType) {
				case DirectiveType.Command:
					foreach (Command cmd in cmdReader.Read(directive.Value)) {
						cmds.Add(cmd);
					}
					break;
				case DirectiveType.TextBoxUnsplit:
					throw new ArgumentException(nameof(DirectiveType.TextBoxUnsplit) + " directive must appear standalone in the text box.", nameof(directives));
				}
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
			if (element is Command cmd && cmd.Definition.Prints) {
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

			if (element is Command cmd && !cmd.Definition.Prints) {
				return true;
			}

			if (element is DirectiveElement directive && (
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

			if (element is DirectiveElement directive && directive.DirectiveType == DirectiveType.TextBoxSplit) {
				return true;
			} else {
				return false;
			}
		}
	}
}
