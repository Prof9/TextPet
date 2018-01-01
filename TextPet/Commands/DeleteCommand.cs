using LibTextPet.Msg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TextPet.Commands {
	/// <summary>
	/// A command line interface command that deletes a currently loaded text archive.
	/// </summary>
	internal class DeleteCommand : CliCommand {
		public override string Name => "delete";
		public override string RunString => "Deleting text archive...";

		private const string taArg = "text-archive";

		public DeleteCommand(CommandLineInterface cli, TextPetCore core)
			: base(cli, core, new string[] {
				taArg
			}) { }

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "System.Console.WriteLine(System.String)")]
		protected override void RunImplementation() {
			string name = GetRequiredValue(taArg);

			bool deleted = false;
			for (int i = 0; i < this.Core.TextArchives.Count; i++) {
				if (this.Core.TextArchives[i].Identifier == name) {
					this.Core.TextArchives.RemoveAt(i--);
					deleted = true;
				}
			}

			if (!deleted) {
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine("ERROR: Could not find a text archive \"" + name + "\" to delete.");
				Console.ResetColor();
			}
		}
	}
}
