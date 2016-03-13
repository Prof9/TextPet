using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TextPet.Commands {
	/// <summary>
	/// A command line interface command that inserts text boxes into the currently loaded text archives.
	/// </summary>
	internal class InsertTextBoxesCommand : CliCommand {
		public override string Name => "insert-text-boxes";
		public override string RunString {
			get {
				this.Cli.SetObjectNames("text file", null);
				return null;
			}
		}

		private const string pathArg = "path";
		private const string recursiveArg = "recursive";

		public InsertTextBoxesCommand(CommandLineInterface cli, TextPetCore core)
			: base (cli, core, new string[] {
				pathArg,
			}, new OptionalArgument[] {
				new OptionalArgument(recursiveArg, 'r'),
			}) { }

		protected override void RunImplementation() {
			string path = GetRequiredValue(pathArg);
			bool recursive = GetOptionalValues(recursiveArg) != null;

			this.Core.InsertTextArchivesTextBoxes(path, recursive);
		}
	}
}
