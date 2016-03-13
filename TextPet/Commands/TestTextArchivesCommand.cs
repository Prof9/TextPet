using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TextPet.Commands {
	/// <summary>
	/// A command line interface command that tests text archive reading from and writing to files.
	/// </summary>
	internal class TestTextArchivesCommand : CliCommand {
		public override string Name => "test-text-archives";
		public override string RunString => "Testing text archives...";

		private const string pathArg = "path";
		private const string recursiveArg = "recursive";

		public TestTextArchivesCommand(CommandLineInterface cli, TextPetCore core)
			: base(cli, core, new string[] {
				pathArg,
			}, new OptionalArgument[] {
				new OptionalArgument(recursiveArg, 'r'),
			}) { }

		protected override void RunImplementation() {
			string path = GetRequiredValue(pathArg);
			bool recursive = GetOptionalValues(recursiveArg) != null;

			this.Core.TestTextArchivesIO(path, recursive);
		}
	}
}
