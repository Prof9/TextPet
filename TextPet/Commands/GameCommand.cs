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

		public GameCommand(CommandLineInterface cli, TextPetCore core)
			: base(cli, core, new string[] {
				nameArg,
			}) { }

		protected override void RunImplementation() {
			string gameCode = GetRequiredValue(nameArg);
			if (!this.Core.SetActiveGame(gameCode)) {
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine("ERROR: Unrecognized game name \"" + gameCode + "\".");
			}
		}
	}
}
