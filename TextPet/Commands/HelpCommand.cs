using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TextPet.Commands {
	/// <summary>
	/// A command line interface command that prints help info.
	/// </summary>
	internal class HelpCommand : CliCommand {
		public override string Name => "help";
		public override string RunString => null;

		public HelpCommand(CommandLineInterface cli, TextPetCore core)
			: base(cli, core) { }

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "System.Console.WriteLine(System.String)")]
		protected override void RunImplementation() {
			Console.WriteLine("Available commands:");
			foreach (CliCommand cmd in this.Cli.Commands) {
				Console.Write('\t');
				Console.WriteLine(cmd.GetUsageString());
			}
			Console.WriteLine();
		}
	}
}
