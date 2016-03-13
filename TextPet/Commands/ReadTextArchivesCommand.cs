using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace TextPet.Commands {
	/// <summary>
	/// A command line interface command that reads text archives from files.
	/// </summary>
	internal class ReadTextArchivesCommand : CliCommand {
		public override string Name => "read-text-archives";
		public override string RunString {
			get {
				this.Cli.SetObjectNames("text archive", null);
				return null;
			}
		}

		private const string formatArg = "format";
		private const string pathArg = "path";
		private const string recursiveArg = "recursive";

		private readonly string[] binFormats = new string[] {
			"BIN", "BINARY", "DMP", "DUMP", "MSG", "MESSAGE",
		};
		private readonly string[] tplFormats = new string[] {
			"TPL", "TEXTPET", "TEXTPETLANGUAGE",
		};
		private readonly string[] txtFormats = new string[] {
			"TXT", "TEXT", "TEXTS", "BOX", "BOXES", "TEXTBOX", "TEXTBOXES",
		};
		private readonly string[] romFormats = new string[] {
			"ROM", "READONLYMEMORY", "GBA", "AGB", "GAMEBOYADVANCE",
		};

		public ReadTextArchivesCommand(CommandLineInterface cli, TextPetCore core)
			: base(cli, core, new string[] {
				pathArg,
			}, new OptionalArgument[] {
				new OptionalArgument(formatArg, 'f', "format"),
				new OptionalArgument(recursiveArg, 'r'),
			}) { }

		protected override void RunImplementation() {
			string manualFormat = GetOptionalValues(formatArg)?[0];
			string path = GetRequiredValue(pathArg);
			bool recursive = GetOptionalValues(recursiveArg) != null;

			// If format is not specified, use file extension.
			string format;
			if (manualFormat != null) {
				format = manualFormat;
			} else {
				string extension = Path.GetExtension(path);

				if (extension.Length <= 1 || extension[0] != '.') {
					Console.WriteLine("ERROR: Text archive format not specified.");
					return;
				}

				format = extension.Substring(1);
			}

			format = format.ToUpperInvariant().Replace("-", "");

			if (binFormats.Contains(format)) {
				this.Core.ReadTextArchivesBinary(path, recursive);
			} else if (tplFormats.Contains(format)) {
				this.Core.ReadTextArchivesTPL(path, recursive);
			} else if (txtFormats.Contains(format)) {
				this.Core.ReadTextArchivesTextBoxes(path, recursive);
			} else if (romFormats.Contains(format)) {
				this.Core.ReadTextArchivesROM(path);
			} else if (manualFormat == null) {
				Console.WriteLine("ERROR: Unknown text archive extension \"" + format + "\". Change the file extension or specify the format manually.");
			} else {
				Console.WriteLine("ERROR: Unknown text archive format \"" + format + "\".");
			}
		}
	}
}
