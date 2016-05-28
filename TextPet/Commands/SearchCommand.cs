using LibTextPet.IO.Msg;
using LibTextPet.Msg;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace TextPet.Commands {
	/// <summary>
	/// A command line interface command that searches a ROM for all text archives.
	/// </summary>
	internal class SearchCommand : CliCommand {
		public override string Name => "search";
		public override string RunString {
			get {
				this.Cli.SetObjectNames("text archive", null);
				return "Searching ROM...";
			}
		}

		private const string pathArg = "path";

		public SearchCommand(CommandLineInterface cli, TextPetCore core)
			: base(cli, core, new string[] {
				pathArg,
			}) { }

		protected override void RunImplementation() {
			string path = GetRequiredValue(pathArg);

			this.Core.LoadROM(path);
			ROMTextArchiveReader reader = new ROMTextArchiveReader(this.Core.ROM, this.Core.Game, this.Core.ROMEntries);
			reader.CheckGoodTextArchive = true;
			reader.TextArchiveReader.IgnorePointerSyncErrors = true;
			reader.TextArchiveReader.AutoSortPointers = false;

			if (this.Cli.Verbose) {
				Console.Write("Searching... ");
			}
			int cursorLeft = Console.CursorLeft;
			for (long p = 0; p < this.Core.ROM.Length; p += 4) {
				p &= ~0x3;

				if (this.Cli.Verbose) {
					int percentage = (int)(100 * p / this.Core.ROM.Length);

					Console.CursorLeft = cursorLeft;
					Console.ForegroundColor = ConsoleColor.Green;
					Console.Write(p.ToString("X6", CultureInfo.InvariantCulture));
					Console.ResetColor();
					Console.Write(" (");
					if (Console.BackgroundColor == ConsoleColor.White) {
						Console.ForegroundColor = ConsoleColor.Black;
					} else {
						Console.ForegroundColor = ConsoleColor.White;
					}
					Console.Write(percentage.ToString(CultureInfo.InvariantCulture).PadLeft(2));
					if (percentage < 100) {
						Console.Write("%");
					}
					Console.ResetColor();
					Console.Write(")");
				}

				this.Core.ROM.Position = p;
				TextArchive ta = reader.Read();

				if (ta != null) {
					this.Core.TextArchives.Add(ta);
				}
			}
		}
	}
}
