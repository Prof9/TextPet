using LibTextPet.Plugins;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using TextPet.Commands;
using TextPet.Events;

namespace TextPet {
	/// <summary>
	/// Manages the command line interface for TextPet.
	/// </summary>
	internal sealed class CommandLineInterface {
		/// <summary>
		/// Gets the commands available to use in this command line interface.
		/// </summary>
		internal IList<CliCommand> Commands { get; private set; }
		/// <summary>
		/// Gets the TextPet core that is being acted on.
		/// </summary>
		private TextPetCore Core { get; }

		/// <summary>
		/// The IDs of the text archives that passed testing.
		/// </summary>
		private List<string> PassedIDs;
		/// <summary>
		/// The IDs of the text archives that failed testing.
		/// </summary>
		private List<string> FailedIDs;
		/// <summary>
		/// The current progress.
		/// </summary>
		private int CurrentProgress;
		/// <summary>
		/// The total progress.
		/// </summary>
		private int TotalProgress;
		/// <summary>
		/// Whether this command line interface is in test mode.
		/// </summary>
		private bool Testing;
		/// <summary>
		/// The current stack of loaded script files.
		/// </summary>
		private List<string> ScriptPaths;
		/// <summary>
		/// Whether printing should be (mostly) surpressed.
		/// </summary>
		internal bool Verbose;
		/// <summary>
		/// The singular name of the object currently being read/written.
		/// </summary>
		private string ObjectNameSingle;
		/// <summary>
		/// The multiple name of the objects currently being read/written.
		/// </summary>
		private string ObjectNameMultiple;

		/// <summary>
		/// Creates a new command line interface that acts on the specified TextPet core.
		/// </summary>
		/// <param name="core">The TextPet core to act on.</param>
		public CommandLineInterface(TextPetCore core) {
			this.Core = core;

			this.PassedIDs = new List<string>();
			this.FailedIDs = new List<string>();
			this.CurrentProgress = 0;
			this.TotalProgress = 0;
			this.Testing = false;
			this.ScriptPaths = new List<string>();
			this.Verbose = false;
			this.SetObjectNames(null, null);

			this.Commands = new CliCommand[] {
				new ClearCommand(this, this.Core),
				new FloodCommand(this, this.Core),
				new GameCommand(this, this.Core),
				new HelpCommand(this, this.Core),
				new InsertTextBoxesCommand(this, this.Core),
				new LoadPluginsCommand(this, this.Core),
				new LoadROMEntriesCommand(this, this.Core),
				new ReadTextArchivesCommand(this, this.Core),
				new RegexCommand(this, this.Core),
				new RunScriptCommand(this, this.Core),
				new SearchCommand(this, this.Core),
				new SilentCommand(this, this.Core),
				new TestTextArchivesCommand(this, this.Core),
				new TrimCommand(this, this.Core),
				new VerboseCommand(this, this.Core),
				new WriteROMEntriesCommand(this, this.Core),
				new WriteTextArchivesCommand(this, this.Core),
			};

			this.Core.BeginLoadingPlugins += Core_BeginReadWriting;
			this.Core.LoadedPlugin += Core_PluginLoaded;
			this.Core.GameInitialized += Core_GameInitialized;

			this.Core.ClearedTextArchives += Core_ClearedTextArchives;

			this.Core.BeginReadingTextArchives += Core_BeginReadWriting;
			this.Core.ReadingTextArchive += Core_ReadingTextArchive;
			this.Core.ReadTextArchive += Core_ReadWroteTextArchive;
			this.Core.FinishedReadingTextArchives += Core_FinishedReadingTextArchives;

			this.Core.BeginWritingTextArchives += Core_BeginReadWriting;
			this.Core.WritingTextArchive += Core_WritingTextArchive;
			this.Core.WroteTextArchive += Core_ReadWroteTextArchive;
			this.Core.FinishedWritingTextArchives += Core_FinishedWritingTextArchives;

			this.Core.BeginTestingTextArchives += Core_BeginReadWriting;
			this.Core.BeginTestingTextArchives += Core_BeginTestingTextArchives;
			this.Core.TestedTextArchive += Core_TestedTextArchive;
			this.Core.FinishedTestingTextArchives += Core_FinishedTestingTextArchives;
		}

		/// <summary>
		/// Runs the specified commands in this command line interface.
		/// </summary>
		/// <param name="args">The commands to run, and their arguments.</param>
		public void Run(IList<string> args) {
			bool suppressDone = false;

			// Print usage if no arguments were provided.
			if (args.Count <= 0) {
				args = new string[] {
					"help"
				};
				suppressDone = true;
			}

			Run(args, suppressDone);
		}

		/// <summary>
		/// Runs the specified commands in this command line interface.
		/// </summary>
		/// <param name="args">The commands to run, and their arguments.</param>
		/// <param name="suppressDone">Whether the "done" message at the end should be suppressed.</param>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "System.Console.WriteLine(System.String)")]
		private void Run(IList<string> args, bool suppressDone) {
			if (args == null) {
				args = new string[0];
			}

			int i = 0;
			while (i < args.Count) {
				// Reset object names.
				this.SetObjectNames(null, null);

				// Find a matching command.
				CliCommand cmd = null;
				foreach (CliCommand c in this.Commands) {
					if (StringComparer.OrdinalIgnoreCase.Equals(args[i], c.Name)) {
						cmd = c;
						break;
					}
				}

				// No commands found?
				if (cmd == null) {
					Console.WriteLine("ERROR: Unknown command \"" + args[i] + "\".");
					break;
				}

				if (cmd.RunString != null) {
					Console.WriteLine(cmd.RunString);
				}
				int j = i + 1 + cmd.Run(args, i + 1);
				// Just in case.
				if (j <= i) {
					Console.WriteLine("FATAL: Loop detected.");
					break;
				}
				i = j;
			}

			if (!suppressDone) {
				Console.WriteLine("Done.");
			}
		}

		/// <summary>
		/// Runs commands from a script loaded from the specified path.
		/// </summary>
		/// <param name="path">The path of the script file.</param>
		public void RunScript(string path) {
			path = Path.GetFullPath(path);

			// Check for recursion.
			if (this.ScriptPaths.Contains(path)) {
				Console.WriteLine("ERROR: Recursive script inclusion is not allowed.");
				return;
			}

			// Add the script path onto the list of included scripts.
			this.ScriptPaths.Add(path);

			// Read the script from the file.
			string script;
			FileStream fs = null;
			try {
				fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.SequentialScan);
				StreamReader reader = new StreamReader(fs);
				script = reader.ReadToEnd();
			} catch (Exception ex) when (
				ex is FileNotFoundException ||
				ex is IOException ||
				ex is SecurityException ||
				ex is DirectoryNotFoundException ||
				ex is UnauthorizedAccessException ||
				ex is PathTooLongException) {
				Console.WriteLine("ERROR: Could not open script file \"" + path + "\".");
				return;
			} finally {
				if (fs != null) {
					fs.Dispose();
				}
			}

			// Extract the arguments from the string.
			List<string> args = new List<string>();
			foreach (Match match in Regex.Matches(script, @"\"".*?\""|[^""\s]+")) {
				if (match.Value.StartsWith("\"", StringComparison.Ordinal) && match.Value.EndsWith("\"", StringComparison.Ordinal)) {
					args.Add(match.Value.Substring(1, match.Value.Length - 2));
				} else {
					args.Add(match.Value);
				}
			}

			// Run the script.
			this.Run(args, true);

			// Remove the script path.
			this.ScriptPaths.Remove(path);
		}

		/// <summary>
		/// Sets the names of the objects currently being read/written to the specified names.
		/// </summary>
		/// <param name="singular">The singular name. If null, will use "file".</param>
		/// <param name="multiple">The multiple name. If null, will use the singular name with an 's' appended.</param>
		public void SetObjectNames(string singular, string multiple) {
			if (singular == null) {
				singular = "file";
				multiple = "files";
			}
			if (multiple == null) {
				multiple = singular + "s";
			}

			this.ObjectNameSingle = singular;
			this.ObjectNameMultiple = multiple;
		}

		/// <summary>
		/// Prints the current progress to the console.
		/// </summary>
		private void PrintProgress() {
			int current = this.CurrentProgress;
			if (current > this.TotalProgress) {
				current = this.TotalProgress;
			}

			Console.ResetColor();
			Console.Write("[");
			if (Console.BackgroundColor == ConsoleColor.White) {
				Console.ForegroundColor = ConsoleColor.Black;
			} else {
				Console.ForegroundColor = ConsoleColor.White;
			}
			Console.Write((current * 100 / this.TotalProgress).ToString(CultureInfo.CurrentCulture).PadLeft(2, ' '));
			if (current != this.TotalProgress) {
				Console.Write("%");
			}
			Console.ResetColor();
			Console.Write("] ");
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "System.Console.WriteLine(System.String)")]
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "System.Console.Write(System.String)")]
		private void Core_BeginReadWriting(object sender, BeginReadWriteEventArgs e) {
			if (this.Testing) {
				return;
			}

			this.CurrentProgress = 0;
			this.TotalProgress = e.Amount;
			this.PassedIDs = new List<string>();
			this.FailedIDs = new List<string>();

			if (e.IsWrite) {
				Console.Write("Writing ");
			} else {
				Console.Write("Reading ");
			}
			Console.Write(e.Amount);

			Console.Write(" ");
			if (e.Amount == 1) {
				Console.Write(this.ObjectNameSingle);
			} else {
				Console.Write(this.ObjectNameMultiple);
			}
			Console.WriteLine("...");
		}

		private void Core_ReadWroteTextArchive(object sender, TextArchivesEventArgs e) {
			if (this.Testing) {
				return;
			}
			
			this.CurrentProgress += e.Count;
		}

		private void Core_PluginLoaded(object sender, PluginsEventArgs e) {
			foreach (IPlugin plugin in e.Plugins) {
				this.CurrentProgress++;

				Console.Write("Loaded ");
				Console.ForegroundColor = ConsoleColor.Cyan;
				Console.Write(plugin.GetType().Name);
				Console.ResetColor();
				Console.Write(" plugin ");
				Console.ForegroundColor = ConsoleColor.Yellow;
				Console.Write(plugin.Name);
				Console.ResetColor();
				Console.WriteLine(".");
			}
		}

		private void Core_GameInitialized(object sender, GameInfoEventArgs e) {
			Console.Write("Initialized plugins for game ");
			Console.ForegroundColor = ConsoleColor.Magenta;
			Console.Write(e.GameInfo.FullName);
			Console.ResetColor();
			Console.WriteLine(".");
		}

		private void Core_ClearedTextArchives(object sender, EventArgs e) {
			if (Testing) {
				return;
			}

			Console.WriteLine("Cleared text archives.");
		}

		private void Core_ReadingTextArchive(object sender, TextArchivesEventArgs e) {
			if (this.Testing || !this.Verbose) {
				return;
			}

			PrintProgress();
			Console.Write("Reading text archives from ");
			Console.ForegroundColor = ConsoleColor.Green;
			Console.Write(Path.GetFileName(e.Path));
			Console.ResetColor();

			if (e.Offset >= 0) {
				Console.Write(" at offset ");
				Console.ForegroundColor = ConsoleColor.Green;
				Console.Write("0x");
				Console.Write(e.Offset.ToString("X6", CultureInfo.InvariantCulture));
				Console.ResetColor();
			}

			Console.WriteLine("...");
		}

		private void Core_FinishedReadingTextArchives(object sender, TextArchivesEventArgs e) {
			if (this.Testing) {
				return;
			}

			if (!this.Verbose) {
				return;
			}

			Console.WriteLine("Finished reading " + e.Count + " text archives.");
		}

		private void Core_WritingTextArchive(object sender, TextArchivesEventArgs e) {
			if (this.Testing || !this.Verbose) {
				return;
			}

			PrintProgress();
			Console.Write("Writing text archive to ");
			Console.ForegroundColor = ConsoleColor.Green;
			Console.Write(Path.GetFileName(e.Path));
			Console.ResetColor();
			
			if (e.Offset >= 0) {
				Console.Write(" at offset ");
				Console.ForegroundColor = ConsoleColor.Green;
				Console.Write("0x");
				Console.Write(e.Offset.ToString("X6", CultureInfo.InvariantCulture));
				Console.ResetColor();
			}

			Console.WriteLine("...");
		}

		private void Core_FinishedWritingTextArchives(object sender, TextArchivesEventArgs e) {
			if (this.Testing) {
				return;
			}

			if (!this.Verbose) {
				return;
			}

			Console.WriteLine("Finished writing " + e.Count + " text archives.");
		}

		private void Core_BeginTestingTextArchives(object sender, BeginReadWriteEventArgs e) {
			this.Testing = true;
		}

		private void Core_TestedTextArchive(object sender, TestEventArgs e) {
			this.CurrentProgress++;

			if (!this.Verbose) {
				return;
			}

			PrintProgress();

			if (e.Passed) {
				this.PassedIDs.Add(e.BeforeTextArchive.Identifier);
				Console.ForegroundColor = ConsoleColor.Green;
				Console.WriteLine("Text archive " + e.BeforeTextArchive.Identifier + " passed test.");
			} else {
				this.FailedIDs.Add(e.BeforeTextArchive.Identifier);
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine("Text archive " + e.BeforeTextArchive.Identifier + " failed test.");
			}
			Console.ResetColor();
		}

		private void Core_FinishedTestingTextArchives(object sender, EventArgs e) {
			this.Testing = false;

			Console.Write("Results: ");

			if (this.PassedIDs.Any()) {
				Console.ForegroundColor = ConsoleColor.Green;
				Console.Write(this.PassedIDs.Count);
				Console.ResetColor();
				Console.Write(" passed");
			}
			if (this.PassedIDs.Any() && this.FailedIDs.Any()) {
				Console.Write(", ");
			}
			if (this.FailedIDs.Any()) {
				Console.ForegroundColor = ConsoleColor.Red;
				Console.Write(this.FailedIDs.Count);
				Console.ResetColor();
				Console.Write(" failed");
			}

			Console.Write(". (");
			if (Console.BackgroundColor == ConsoleColor.White) {
				Console.ForegroundColor = ConsoleColor.Black;
			} else {
				Console.ForegroundColor = ConsoleColor.White;
			}

			int total = this.PassedIDs.Count + this.FailedIDs.Count;
			if (total == 0) {
				Console.Write("100");
			} else {
				Console.Write(this.PassedIDs.Count * 100 / (total));
			}
			Console.Write("%");
			Console.ResetColor();
			Console.WriteLine(")");

			if (this.FailedIDs.Any()) {
				Console.Write("Failed: ");
				bool first = true;
				foreach (string id in this.FailedIDs) {
					if (!first) {
						Console.Write(", ");
					}
					first = false;

					Console.Write(id);
				}
				Console.WriteLine();
			}
		}
	}
}
