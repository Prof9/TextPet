using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LibTextPet.General;

namespace TextPet.Commands {
	internal class SetCompressionCommand : CliCommand {
		public override string Name => "set-compression";
		public override string RunString => "Setting compression...";

		private const string typeArg = "type";

		public SetCompressionCommand(CommandLineInterface cli, TextPetCore core)
			: base(cli, core, new string[] {
				typeArg
			}) { }

		protected override void RunImplementation() {
			string type = GetRequiredValue(typeArg);

			switch (type.ToUpperInvariant()) {
			case "ON":
			case "TRUE":
			case "YES":
				this.Core.LZ77Compress = true;
				break;
			case "OFF":
			case "FALSE":
			case "NO":
				this.Core.LZ77Compress = false;
				break;
			default:
				if (NumberParser.TryParseInt64(type, out long num)) {
					this.Core.LZ77Compress = num != 0;
				} else {
					Console.ForegroundColor = ConsoleColor.Red;
					Console.WriteLine("ERROR: Could not parse compression type \"" + type + "\".");
					Console.ResetColor();
				}
				break;
			}
		}
	}
}
