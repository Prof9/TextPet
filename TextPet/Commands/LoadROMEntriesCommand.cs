using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TextPet.Commands {
	internal class LoadROMEntriesCommand : CliCommand {
		public override string Name => "load-rom-entries";
		public override string RunString => "Loading ROM entries...";

		private const string pathArg = "path";

		public LoadROMEntriesCommand(CommandLineInterface cli, TextPetCore core)
			: base(cli, core, new string[] {
				pathArg,
			}) { }

		protected override void RunImplementation() {
			string path = GetRequiredValue(pathArg);
			this.Core.LoadROMEntries(path);
		}
	}
}
