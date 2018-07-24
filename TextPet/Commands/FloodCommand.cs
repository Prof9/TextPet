using LibTextPet.Msg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TextPet.Commands {
	/// <summary>
	/// A command line interface command that replaces every single non-newline character in the currently loaded text archives with W's.
	/// Additionally, it inserts the text archive identifier and script number at the start of every script (if there is enough room).
	/// </summary>
	internal class FloodCommand : CliCommand {
		public override string Name => "flood";
		public override string RunString => "Flooding archives...";

		public FloodCommand(CommandLineInterface cli, TextPetCore core)
			: base(cli, core) { }

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "LibTextPet.Msg.TextElement.set_Text(System.String)")]
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "System.Console.WriteLine(System.String)")]
		protected override void RunImplementation() {
			// lol
			foreach (TextArchive ta in this.Core.TextArchives) {
				for (int scriptNum = 0; scriptNum < ta.Count; scriptNum++) {
					Script script = ta[scriptNum];
					string posStr = ta.Identifier + '-' + scriptNum + ' ';
					int posStrIndex = 0;

					foreach (IScriptElement elem in script) {
						TextElement textElem = elem as TextElement;
						if (textElem == null) {
							continue;
						}

						string[] lines = textElem.Text.Split(new string[] { "\n" }, StringSplitOptions.None);
						for (int i = 0; i < lines.Length; i++) {
							char[] l = new string('W', lines[i].Length).ToCharArray();

							for (int j = 0; j < lines[i].Length && posStrIndex < posStr.Length; j++, posStrIndex++) {
								l[j] = posStr[posStrIndex];
							}

							lines[i] = new string(l);
						}
						textElem.Text = String.Join("\n", lines);
					}
				}
			}
		}
	}
}
