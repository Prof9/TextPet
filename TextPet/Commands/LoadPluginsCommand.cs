using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TextPet.Commands {
	/// <summary>
	/// A command line interface command that loads plugins from files.
	/// </summary>
	internal class LoadPluginsCommand : CliCommand {
		public override string Name => "load-plugins";
		public override string RunString => "Loading plugins...";

		private const string pathArg = "path";
		private const string recursiveArg = "recursive";

		public LoadPluginsCommand(CommandLineInterface cli, TextPetCore core)
			: base(cli, core, new string[] {
				pathArg,
			}, new OptionalArgument[] {
				new OptionalArgument(recursiveArg, 'r'),
			}) { }

		protected override void RunImplementation() {
			string path = GetRequiredValue(pathArg);
			bool recursive = GetOptionalValues(recursiveArg) != null;

			this.Core.LoadPlugins(path, recursive);
		}
	}
}
