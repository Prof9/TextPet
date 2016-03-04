using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace TextPet.Commands {
	/// <summary>
	/// A command line interface command that runs a script file with commands.
	/// </summary>
	internal class RunScriptCommand : CliCommand {
		public override string Name => "run-script";
		public override string RunString => "Running script...";

		private const string pathArg = "path";

		public RunScriptCommand(CommandLineInterface cli, TextPetCore core)
			: base(cli, core, new string[] {
				pathArg,
			}) { }

		protected override void RunImplementation() {
			string path = GetRequiredValue(pathArg);
			this.Cli.RunScript(path);
		}
	}
}
