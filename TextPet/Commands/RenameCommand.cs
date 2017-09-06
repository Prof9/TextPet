using LibTextPet.Msg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TextPet.Commands {
	/// <summary>
	/// A command line interface command that renames a currently loaded text archive.
	/// </summary>
	internal class RenameCommand : CliCommand {
		public override string Name => "rename";
		public override string RunString => "Renaming text archive...";

		private const string fromArg = "from";
		private const string toArg = "to";

		public RenameCommand(CommandLineInterface cli, TextPetCore core)
			: base(cli, core, new string[] {
				fromArg,
				toArg
			}) { }

		protected override void RunImplementation() {
			string fromName = GetRequiredValue(fromArg);
			string toName = GetRequiredValue(toArg);

			foreach (TextArchive ta in this.Core.TextArchives) {
				if (ta.Identifier == fromName) {
					ta.Identifier = toName;
				}
			}
		}
	}
}
