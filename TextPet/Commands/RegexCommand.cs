using LibTextPet.Msg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace TextPet.Commands {
	/// A command line interface command that performs a regex replace in all text elements of all currently loaded text archives.
	internal class RegexCommand : CliCommand {
		public override string Name => "regex";
		public override string RunString => "Performing regex replacement...";

		private const string patternArg = "pattern";
		private const string replacementArg = "replacement";
		private const string filterArg = "filter";

		public RegexCommand(CommandLineInterface cli, TextPetCore core)
			: base (cli, core, new string[] {
				patternArg,
				replacementArg,
			}, new OptionalArgument[] {
				new OptionalArgument(filterArg, 'f', new string[] {
					"database"
				}),
			}) { }

		protected override void RunImplementation() {
			string pattern = GetRequiredValue(patternArg);
			string replacement = GetRequiredValue(replacementArg);

			IList<string> filterArgs = GetOptionalValues(filterArg);
			string filter = null;
			if (filterArgs != null && filterArgs.Count > 0) {
				filter = filterArgs[0];
			}

			Regex regex = new Regex(pattern);

			foreach (TextArchive ta in this.Core.TextArchives) {
				foreach (Script script in ta) {
					// Skip scripts that do not match database name filter.
					if (filter != null && script.DatabaseName != filter) {
						continue;
					}
					
					foreach (IScriptElement elem in script) {
						if (elem is TextElement textElem) {
							textElem.Text = regex.Replace(textElem.Text, replacement);
						}
					}
				}
			}
		}
	}
}
