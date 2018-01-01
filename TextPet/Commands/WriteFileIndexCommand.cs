using LibTextPet.General;
using LibTextPet.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace TextPet.Commands {
	internal class WriteFileIndexCommand : CliCommand {
		public override string Name => "write-file-index";
		public override string RunString => "Writing file index...";

		private const string pathArg = "path";

		private const string commentsArg = "comments";
		private const string bytesArg = "bytes";
		private const string addSizeArg = "add-size";
		private const string amountArg = "amount";
		private const string excludeByteArg = "exclude-byte";
		private const string byteArg = "byte";

		public WriteFileIndexCommand(CommandLineInterface cli, TextPetCore core)
			: base(cli, core, new string[] {
				pathArg,
			}, new OptionalArgument[] {
				new OptionalArgument(commentsArg, 'c'),
				new OptionalArgument(bytesArg, 'b'),
				new OptionalArgument(addSizeArg, 'a', new string[] {
					amountArg,
				}),
				new OptionalArgument(excludeByteArg, 'e', new string[] {
					byteArg,
				}),
			}) {
		}

		protected override void RunImplementation() {
			string path = GetRequiredValue(pathArg);
			bool comments = GetOptionalValues(commentsArg) != null;
			bool bytes = GetOptionalValues(bytesArg) != null;
			string addSizeStr = GetOptionalValues(addSizeArg)?[0];
			int addSize = addSizeStr != null ? NumberParser.ParseInt32(addSizeStr) : 0;
			string excludeByteStr = GetOptionalValues(excludeByteArg)?[0];
			int excludeByte = excludeByteStr != null ? NumberParser.ParseInt32(excludeByteStr) : -1;

			using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read)) {
				new FileIndexWriter(fs) {
					AddSize = addSize,
					ExcludeByte = excludeByte,
					IncludeFormatComments = true,
					IncludeGapComments = comments,
					IncludeOverlapComments = comments,
					IncludePointerWarnings = comments,
					IncludePostBytesComments = bytes
				}.Write(this.Core.FileIndex, this.Core.LoadedFile);
			}
		}
	}
}
