using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TextPet.Commands {
	/// <summary>
	/// A command line interface command that clears the currently loaded text archives.
	/// </summary>
	internal class ClearCommand : CliCommand {
		public override string Name => "clear";
		public override string RunString => "Clearing text archives...";

		public ClearCommand(CommandLineInterface cli, TextPetCore core)
			: base(cli, core) { }

		protected override void RunImplementation() {
			this.Core.ClearTextArchives();
		}
	}
}
