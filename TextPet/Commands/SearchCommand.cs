using LibTextPet.General;
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

		private const string startArg = "start";
		private const string offsetArg = "offset";
		private const string lengthArg = "length";
		private const string deepArg = "deep";

		public SearchCommand(CommandLineInterface cli, TextPetCore core)
			: base(cli, core, new string[] {
				pathArg,
			}, new OptionalArgument[] {
				new OptionalArgument(startArg, 's', offsetArg),
				new OptionalArgument(lengthArg, 'l', lengthArg),
				new OptionalArgument(deepArg, 'd')
			}) { }

		protected override void RunImplementation() {
			string path = GetRequiredValue(pathArg);
			string startArg = GetOptionalValues(SearchCommand.startArg)?[0];
			string lengthArg = GetOptionalValues(SearchCommand.lengthArg)?[0];
			bool deep = GetOptionalValues(deepArg) != null;

			this.Core.LoadROM(path);

			long start = 0;
			if (startArg != null) {
				start = NumberParser.ParseInt64(startArg);
			}
			if (start < 0) {
				start = 0;
			}

			long length = this.Core.ROM.Length;
			if (lengthArg != null) {
				length = NumberParser.ParseInt64(lengthArg);
			}
			if (length < 0) {
				length = 0;
			}
			if (start + length > this.Core.ROM.Length) {
				length = this.Core.ROM.Length - start;
			}

			ROMTextArchiveReader reader = new ROMTextArchiveReader(this.Core.ROM, this.Core.Game, this.Core.ROMEntries);
			reader.CheckGoodTextArchive = !deep;
			reader.TextArchiveReader.IgnorePointerSyncErrors = true;
			reader.TextArchiveReader.AutoSortPointers = false;
			reader.UpdateROMEntriesAndIdentifiers = true;

			int found = 0;

			if (this.Cli.Verbose) {
				Console.Write("Searching... ");
			}
			int cursorLeft = Console.CursorLeft;
			for (long p = 0; p < length; p += 4) {
				long offset = start + p;
				offset &= ~0x3;

				if (this.Cli.Verbose) {
					int percentage = (int)(100 * p / length);

					Console.CursorLeft = cursorLeft;
					Console.ForegroundColor = ConsoleColor.Green;
					Console.Write(offset.ToString("X6", CultureInfo.InvariantCulture));
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
					Console.Write(") (");
					Console.ForegroundColor = ConsoleColor.Green;
					Console.Write(found);
					Console.ResetColor();
					Console.Write(" found)");
				}

				this.Core.ROM.Position = offset;
				TextArchive ta = reader.Read();

				if (ta != null) {
					this.Core.TextArchives.Add(ta);
					found++;
				}
			}
			Console.WriteLine();
		}
	}
}
