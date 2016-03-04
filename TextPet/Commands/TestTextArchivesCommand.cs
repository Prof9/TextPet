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

		public TestTextArchivesCommand(CommandLineInterface cli, TextPetCore core)
			: base(cli, core, new string[] {
				pathArg,
			}) { }

		protected override void RunImplementation() {
			string path = GetRequiredValue(pathArg);
			this.Core.TestTextArchivesIO(path);
		}
	}
}
