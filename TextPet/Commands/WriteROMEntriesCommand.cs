using LibTextPet.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace TextPet.Commands {
	internal class WriteROMEntriesCommand : CliCommand {
		public override string Name => "write-rom-entries";
		public override string RunString => "Writing ROM entries...";

		private const string pathArg = "path";

		private const string commentsArg = "comments";
		private const string bytesArg = "bytes";

		public WriteROMEntriesCommand(CommandLineInterface cli, TextPetCore core)
			: base(cli, core, new string[] {
				pathArg,
			}, new OptionalArgument[] {
				new OptionalArgument(commentsArg, 'c'),
				new OptionalArgument(bytesArg, 'b'),
			}) {
		}

		protected override void RunImplementation() {
			string path = GetRequiredValue(pathArg);
			bool comments = GetOptionalValues(commentsArg) != null;
			bool bytes = GetOptionalValues(bytesArg) != null;

			using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read)) {
				ROMEntriesWriter writer = new ROMEntriesWriter(fs);
				writer.IncludeFormatComments = true;

				if (comments) {
					writer.IncludeGapComments = true;
					writer.IncludeOverlapComments = true;
				}
				if (bytes) {
					writer.IncludePostBytesComments = true;
				}

				writer.Write(this.Core.ROMEntries, this.Core.ROM);
			}
		}
	}
}
