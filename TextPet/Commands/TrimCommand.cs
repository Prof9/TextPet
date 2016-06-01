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

		public TrimCommand(CommandLineInterface cli, TextPetCore core)
			: base(cli, core, new string[0], new OptionalArgument[] {
				new OptionalArgument(maxArg, 'm', new string[] {
					amountArg,
				}),
				new OptionalArgument(preserveTextArg, 'p'),
			}) { }

		protected override void RunImplementation() {
			string maxStr = GetOptionalValues(maxArg)?[0];
			int max = maxStr != null ? NumberParser.ParseInt32(maxStr) : int.MaxValue;
			bool preserveText = GetOptionalValues(preserveTextArg) != null;

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

					// Remove commands from the end of the script until the script-ending command or maximum is reached.
					int trimmed = 0;
					while (script.Count > endPos && (maxStr == null || trimmed < max)) {
						// If the preverse text option is enabled, cancel if this would trim non-whitespace text.
						if (preserveText) {
							TextElement textElem = script[script.Count - 1] as TextElement;
							if (textElem != null && !textElem.Text.All(c => Char.IsWhiteSpace(c))) {
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
