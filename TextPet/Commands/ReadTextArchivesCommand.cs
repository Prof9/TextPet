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
			}) { }

		protected override void RunImplementation() {
			string manualFormat = GetOptionalValues(formatArg)?[0];
			string path = GetRequiredValue(pathArg);

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
				this.Core.ReadTextArchivesBinary(path);
			} else if (tplFormats.Contains(format)) {
				this.Core.ReadTextArchivesTPL(path);
			} else if (txtFormats.Contains(format)) {
				this.Core.ReadTextArchivesTextBoxes(path);
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
