using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TextPet.Commands {
	internal class LoadFileIndexCommand : CliCommand {
		public override string Name => "load-file-index";
		public override string RunString => "Loading file index...";

		private const string pathArg = "path";
		private const string recursiveArg = "recursive";
		private const string ignoreSizeArg = "ignore-size";

		public LoadFileIndexCommand(CommandLineInterface cli, TextPetCore core)
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

			this.Core.LoadFileIndex(path, recursive, ignoreSize);
		}
	}
}
