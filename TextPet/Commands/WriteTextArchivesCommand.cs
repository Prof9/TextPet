using LibTextPet.General;
using LibTextPet.IO;
using LibTextPet.IO.Msg;
using LibTextPet.Msg;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace TextPet.Commands {
	/// <summary>
	/// A command line interface command that writes the currently loaded text archives to a file or folder.
	/// </summary>
	internal class WriteTextArchivesCommand : CliCommand {
		public override string Name => "write-text-archives";
		public override string RunString {
			get {
				this.Cli.SetObjectNames("text archive", null);
				return null;
			}
		}

		private const string formatArg = "format";
		private const string freeSpaceArg = "free-space";
		private const string singleArg = "single";
		private const string pathArg = "path";
		private const string noIdsArg = "no-ids";

		private readonly string[] binFormats = new string[] {
			"BIN", "BINARY", "DMP", "DUMP", "MSG", "MESSAGE"
		};
		private readonly string[] tplFormats = new string[] {
			"TPL", "TEXTPET", "TEXTPETLANGUAGE"
		};
		private readonly string[] txtFormats = new string[] {
			"TXT", "TEXT", "TEXTS", "BOX", "BOXES", "TEXTBOX", "TEXTBOXES"
		};
		private readonly string[] romFormats = new string[] {
			"ROM", "READONLYMEMORY", "GBA", "AGB", "GAMEBOYADVANCE",
		};
		private readonly string[] mmsf3Formats = new string[] {
			"GSM", "MMSF3", "SF3", "SSR3", "RNR3"
		};

		public WriteTextArchivesCommand(CommandLineInterface cli, TextPetCore core)
			: base(cli, core, new string[] {
				pathArg,
			}, new OptionalArgument[] {
				new OptionalArgument(formatArg, 'f', "format"),
				new OptionalArgument(freeSpaceArg, 'o', "offset"),
				new OptionalArgument(singleArg, 's'),
				new OptionalArgument(noIdsArg, 'n'),
			}) { }

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "System.Console.WriteLine(System.String)")]
		protected override void RunImplementation() {
			string manualFormat = GetOptionalValues(formatArg)?[0];
			string path = GetRequiredValue(pathArg);
			bool single = GetOptionalValues(singleArg) != null;
			bool noIds = GetOptionalValues(noIdsArg) != null;

			// If format is not specified, use file extension.
			string format;
			if (manualFormat != null) {
				format = manualFormat;
			} else {
				string extension = Path.GetExtension(path);

				if (extension.Length <= 1 || extension[0] != '.') {
					Console.ForegroundColor = ConsoleColor.Red;
					Console.WriteLine("ERROR: Text archive format not specified.");
					Console.ResetColor();
					return;
				}

				format = extension.Substring(1);
			}

			format = format.ToUpperInvariant().Replace("-", "");

			if (binFormats.Contains(format)) {
				WriteTextArchivesBinary(path, false);
			} else if (tplFormats.Contains(format)) {
				this.Core.WriteTextArchivesTPL(path, single, noIds);
			} else if (txtFormats.Contains(format)) {
				this.Core.ExtractTextBoxes(path, single, noIds);
			} else if (romFormats.Contains(format)) {
				string fspaceVal = GetOptionalValues(freeSpaceArg)?[0] ?? "-1";
				long fspace = NumberParser.ParseInt64(fspaceVal);
				this.Core.WriteTextArchivesFile(path, fspace);
			} else if (mmsf3Formats.Contains(format)) {
				WriteTextArchivesBinary(path, true);
			} else if (manualFormat == null) {
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine("ERROR: Unknown text archive extension \"" + format + "\". Change the file extension or specify the format manually.");
				Console.ResetColor();
			} else {
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine("ERROR: Unknown text archive format \"" + format + "\".");
				Console.ResetColor();
			}
		}

		/// <summary>
		/// Writes binary text archives to the specified path.
		/// </summary>
		/// <param name="path">The path to write to. Can be a file or folder.</param>
		/// <param name="mmsf3">Whether the MMSF3 text archive format should be used.</param>
		public void WriteTextArchivesBinary(string path, bool mmsf3) {
			this.Core.WriteTextArchives(path, "msg", false, delegate (MemoryStream ms, TextArchive ta) {
				IWriter<TextArchive> writer;
				if (mmsf3) {
					writer = new MMSF3TextArchiveWriter(ms, this.Core.Game);
				} else {
					writer = new BinaryTextArchiveWriter(ms, this.Core.Game.Encoding);
				}
				writer.Write(ta);
			});
		}
	}
}
