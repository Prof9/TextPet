using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TextPet.Commands {
	internal class LoadFileCommand : CliCommand {
		public override string Name => "load-file";
		public override string RunString => "Loading file...";

		private const string pathArg = "path";

		public LoadFileCommand(CommandLineInterface cli, TextPetCore core)
			: base(cli, core, new string[] {
				pathArg,
			}) { }

		protected override void RunImplementation() {
			string path = GetRequiredValue(pathArg);

			this.Core.LoadFile(path);
		}
	}
}
