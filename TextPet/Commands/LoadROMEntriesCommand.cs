using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TextPet.Commands {
	internal class LoadROMEntriesCommand : CliCommand {
		public override string Name => "load-rom-entries";
		public override string RunString => "Loading ROM entries...";

		private const string pathArg = "path";
		private const string recursiveArg = "recursive";
		private const string ignoreSizeArg = "ignore-size";

		public LoadROMEntriesCommand(CommandLineInterface cli, TextPetCore core)
			: base(cli, core, new string[] {
				pathArg,
			}, new OptionalArgument[] {
				new OptionalArgument(recursiveArg, 'r'),
				new OptionalArgument(ignoreSizeArg, 'i'),
			}) { }

		protected override void RunImplementation() {
			string path = GetRequiredValue(pathArg);
			bool recursive = GetOptionalValues(recursiveArg) != null;
			bool ignoreSize = GetOptionalValues(ignoreSizeArg) != null;

			this.Core.LoadROMEntries(path, recursive, ignoreSize);
		}
	}
}
