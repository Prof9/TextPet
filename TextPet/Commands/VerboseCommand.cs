using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TextPet.Commands {
	/// <summary>
	/// A command line interface command that increases verbosity.
	/// </summary>
	internal class VerboseCommand : CliCommand {
		public override string Name => "verbose";
		public override string RunString => null;

		public VerboseCommand(CommandLineInterface cli, TextPetCore core)
			: base(cli, core) { }

		protected override void RunImplementation() {
			this.Cli.Verbose = true;
		}
	}
}
