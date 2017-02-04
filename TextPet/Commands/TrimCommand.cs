using LibTextPet.General;
using LibTextPet.Msg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TextPet.Commands {
	internal class TrimCommand : CliCommand {
		public override string Name => "trim";
		public override string RunString => "Trimming scripts...";
		
		private const string maxArg = "max";
		private const string amountArg = "amount";
		private const string preserveTextArg = "preserve-text";
		private const string preserveCommandsArg = "preserve-commands";

		public TrimCommand(CommandLineInterface cli, TextPetCore core)
			: base(cli, core, new string[0], new OptionalArgument[] {
				new OptionalArgument(maxArg, 'm', new string[] {
					amountArg,
				}),
				new OptionalArgument(preserveTextArg, 't'),
				new OptionalArgument(preserveCommandsArg, 'c'),
			}) { }

		protected override void RunImplementation() {
			string maxStr = GetOptionalValues(maxArg)?[0];
			int max = maxStr != null ? NumberParser.ParseInt32(maxStr) : int.MaxValue;
			bool preserveText = GetOptionalValues(preserveTextArg) != null;
			bool preserveCommands = GetOptionalValues(preserveCommandsArg) != null;

			// Cycle through every script in every text archive.
			foreach (TextArchive ta in this.Core.TextArchives) {
				foreach (Script script in ta) {
					// Find the first script-ending command.
					int endPos = 0;
					for (int i = 0; i < script.Count; i++) {
						if (script[i].EndsScript) {
							endPos = i;
							break;
						}
					}

					// Remove elements from the end of the script until the script-ending element or maximum is reached.
					int trimmed = 0;
					while (script.Count > endPos && (maxStr == null || trimmed < max)) {
						TextElement textElem = script[script.Count - 1] as TextElement;
						if (textElem != null) {
							// If the preserve text option is enabled, cancel.
							if (preserveText) {
								break;
							}

							// Determine number of chars to trim.
							int trimSize = max - trimmed;
							if (textElem.Text.Length <= trimSize) {
								// Remove entire text element.
								trimSize = textElem.Text.Length;
								script.RemoveAt(script.Count - 1);
								trimmed += trimSize;
								continue;
							} else {
								// Text element is too small; cancel.
								break;
							}
						}
						
						// If the preserve commands option is enabled, cancel if this would trim commands.
						if (preserveCommands) {
							Command cmdElem = script[script.Count - 1] as Command;
							if (cmdElem != null) {
								break;
							}
						}

						script.RemoveAt(script.Count - 1);
						trimmed++;
					}
				}
			}
		}
	}
}
