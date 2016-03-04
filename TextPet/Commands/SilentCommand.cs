using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TextPet.Commands {
	/// <summary>
	/// A command line interface command that lowers verbosity.
	/// </summary>
	internal class SilentCommand : CliCommand {
		public override string Name => "silent";
		public override string RunString => null;

		public SilentCommand(CommandLineInterface cli, TextPetCore core)
			: base(cli, core) { }

		protected override void RunImplementation() {
			this.Cli.Verbose = false;
		}
	}
}
