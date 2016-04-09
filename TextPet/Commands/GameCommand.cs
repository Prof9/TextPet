using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TextPet.Commands {
	/// <summary>
	/// A command line interface command that sets the current active game.
	/// </summary>
	internal class GameCommand : CliCommand {
		public override string Name => "game";
		public override string RunString => "Initializing game...";

		private const string nameArg = "name";
		private const string ignoreUnknownCharsArg = "ignore-unknown-chars";

		public GameCommand(CommandLineInterface cli, TextPetCore core)
			: base(cli, core, new string[] {
				nameArg,
			}, new OptionalArgument[] {
				new OptionalArgument(ignoreUnknownCharsArg, 'i'),
			}) { }

		protected override void RunImplementation() {
			string gameCode = GetRequiredValue(nameArg);
			bool ignoreUnknownChars = GetOptionalValues(ignoreUnknownCharsArg) != null;

			if (!this.Core.SetActiveGame(gameCode, ignoreUnknownChars)) {
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine("ERROR: Unrecognized game name \"" + gameCode + "\".");
				Console.ResetColor();
			}
		}
	}
}
