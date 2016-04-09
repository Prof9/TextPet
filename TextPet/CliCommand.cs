using LibTextPet.General;
using LibTextPet.Msg;
using LibTextPet.Plugins;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace TextPet {
	/// <summary>
	/// A single command line interface command with specific functionality.
	/// </summary>
	internal abstract class CliCommand {
		/// <summary>
		/// An optional argument for this command line interface command.
		/// </summary>
		public struct OptionalArgument {
			/// <summary>
			/// Gets the long name of this optional argument, which is used with a -- prefix.
			/// </summary>
			public string LongName { get; }
			/// <summary>
			/// Gets the short name of this optional argument, which is used with a - prefix.
			/// </summary>
			public string ShortName { get; }
			/// <summary>
			/// Gets the names of the values that this optional argument requires.
			/// </summary>
			public IList<string> ValueNames { get; }

			/// <summary>
			/// Creates a new optional argument with the specified argument names and value names.
			/// </summary>
			/// <param name="longName">The long name of the optional argument.</param>
			/// <param name="shortName">The short name of the optional argument.</param>
			/// <param name="values">The names of the values that the optional argument requires.</param>
			public OptionalArgument(string longName, char shortName, params string[] values) {
				if (values == null) {
					values = new string[0];
				}

				if (longName == null)
					throw new ArgumentNullException(nameof(longName), "The long argument name cannot be null.");
				if (longName.Length <= 0)
					throw new ArgumentException("The long argument name cannot be empty.", nameof(longName));

				this.LongName = longName;
				this.ShortName = shortName.ToString();
				this.ValueNames = values;
			}
		}

		/// <summary>
		/// Gets the name of this command line interface command.
		/// </summary>
		public abstract string Name { get; }
		/// <summary>
		/// Gets the string that is printed to the command line when this command is ran. If null, nothing is printed.
		/// </summary>
		public virtual string RunString => "Running command " + this.Name + "...";

		/// <summary>
		/// Gets the names of the required arguments of this command.
		/// </summary>
		public IList<string> RequiredArguments { get; }
		/// <summary>
		/// Gets the optional arguments of this command.
		/// </summary>
		public IList<OptionalArgument> OptionalArguments { get; }

		/// <summary>
		/// Gets the TextPet core that this command is acting on.
		/// </summary>
		protected TextPetCore Core { get; }
		/// <summary>
		/// Gets the command line interface that is using this command.
		/// </summary>
		protected CommandLineInterface Cli { get; }

		/// <summary>
		/// Gets the current values of the required arguments of this command.
		/// </summary>
		private IDictionary<string, string> RequiredValues { get; }
		/// <summary>
		/// Gets the current values of the optional arguments of this command.
		/// </summary>
		private IDictionary<string, IList<string>> OptionalValues { get; }
		/// <summary>
		/// 
		/// </summary>
		private ICollection<string> UsedOptionalValues { get; }

		/// <summary>
		/// Creates a new command line interface command with no arguments.
		/// </summary>
		/// <param name="cli">The command line interface that is using the command.</param>
		/// <param name="core">The TextPet core to act on.</param>
		public CliCommand(CommandLineInterface cli, TextPetCore core)
			: this(cli, core, new string[0], new OptionalArgument[0]) { }

		/// <summary>
		/// Creates a new command line interface command with the specified required and no optional arguments.
		/// </summary>
		/// <param name="cli">The command line interface that is using the command.</param>
		/// <param name="core">The TextPet core to act on.</param>
		/// <param name="requiredArguments">The names of the required arguments.</param>
		public CliCommand(CommandLineInterface cli, TextPetCore core, IList<string> requiredArguments)
			: this(cli, core, requiredArguments, new OptionalArgument[0]){ }

		/// <summary>
		/// Creates a new command line interface command with the specified required and optional arguments.
		/// </summary>
		/// <param name="cli">The command line interface that is using the command.</param>
		/// <param name="core">The TextPet core to act on.</param>
		/// <param name="requiredArguments">The names of the required arguments.</param>
		/// <param name="optionalArguments">The optional arguments.</param>
		public CliCommand(CommandLineInterface cli, TextPetCore core, IList<string> requiredArguments, IList<OptionalArgument> optionalArguments) {
			if (cli == null)
				throw new ArgumentNullException(nameof(cli), "The command line interface cannot be null.");
			if (core == null)
				throw new ArgumentNullException(nameof(core), "The TextPet core cannot be null.");

			if (requiredArguments == null) {
				requiredArguments = new string[0];
			}
			if (optionalArguments == null) {
				optionalArguments = new OptionalArgument[0];
			}
			
			this.Cli = cli;
			this.Core = core;

			this.RequiredArguments = requiredArguments;
			this.OptionalArguments = optionalArguments;

			this.RequiredValues = new Dictionary<string, string>(requiredArguments.Count, StringComparer.OrdinalIgnoreCase);
			this.OptionalValues = new Dictionary<string, IList<string>>(optionalArguments.Count, StringComparer.OrdinalIgnoreCase);
			this.UsedOptionalValues = new HashSet<string>();
		}

		/// <summary>
		/// Runs this command with the specified list of arguments, starting from the specified position.
		/// </summary>
		/// <param name="args">The arguments for this command.</param>
		/// <param name="start">The position to start from in the list of arguments.</param>
		/// <returns></returns>
		public int Run(IList<string> args, int start) {
			if (args == null) {
				args = new string[0];
			}

			if (args.Count < this.RequiredArguments.Count)
				throw new ArgumentException("At least " + this.RequiredArguments.Count + " arguments must be specified.", nameof(args));
			if (start < 0 || start > args.Count)
				throw new ArgumentOutOfRangeException(nameof(start), "The starting position falls outside the range of the arguments.");

			int pos = start;
			pos += ParseRequiredArguments(args, pos);
			pos += ParseOptionalArguments(args, pos);

			// Clear the optional arguments usage flags and run the implementation.
			this.UsedOptionalValues.Clear();
			RunImplementation();

			PrintIgnoredOptionalArguments();

			return pos - start;
		}

		/// <summary>
		/// Prints a warning to the console if any optional arguments that were provided by the user were not used when this
		/// command's implementation was last run.
		/// </summary>
		private void PrintIgnoredOptionalArguments() {
			// Check which optional arguments were not used.
			IList<string> unused = new List<string>(this.OptionalArguments.Count);
			foreach (string longName in this.OptionalValues.Keys) {
				if (!this.UsedOptionalValues.Contains(longName)) {
					unused.Add(longName);
				}
			}
			// Print a warning if some optional arguments were unused.
			if (unused.Any()) {
				Console.ForegroundColor = ConsoleColor.Yellow;
				Console.Write("WARNING: Argument");
				if (unused.Count != 1) {
					Console.Write('s');
				}
				for (int i = 0; i < unused.Count; i++) {
					if (unused.Count != 1 && i == unused.Count - 1) {
						Console.Write(" and");
					} else if (i != 0) {
						Console.Write(',');
					}

					Console.Write(' ');
					Console.ForegroundColor = ConsoleColor.DarkYellow;
					Console.Write(unused[i]);
					Console.ForegroundColor = ConsoleColor.Yellow;
				}
				Console.Write(' ');
				if (unused.Count != 1) {
					Console.Write("were");
				} else {
					Console.Write("was");
				}
				Console.WriteLine(" ignored.");
				Console.ResetColor();
			}
		}

		/// <summary>
		/// Gets the usage string of this command.
		/// </summary>
		/// <returns></returns>
		public string GetUsageString() {
			StringBuilder builder = new StringBuilder();

			// Print the name of the command.
			builder.Append(this.Name);

			// Print required arguments as <arg>
			foreach (string argName in this.RequiredArguments) {
				builder.Append(" <");
				builder.Append(argName);
				builder.Append('>');
			}

			// Print optional arguments as [--arg|-a <val>]
			foreach (OptionalArgument arg in this.OptionalArguments) {
				builder.Append(" [--");
				builder.Append(arg.LongName);
				builder.Append("|-");
				builder.Append(arg.ShortName);
				
				// Print optional argument values as <val>
				foreach (string valName in arg.ValueNames) {
					builder.Append(" <");
					builder.Append(valName);
					builder.Append(">");
				}

				builder.Append("]");
			}

			return builder.ToString();
		}

		/// <summary>
		/// Parses the required arguments of this command from the specified list of arguments, starting from the specified position.
		/// </summary>
		/// <param name="args">The arguments for this command.</param>
		/// <param name="start">The position to start from in the list of arguments.</param>
		/// <returns>The number of arguments that were consumed.</returns>
		private int ParseRequiredArguments(IList<string> args, int start) {
			this.RequiredValues.Clear();

			for (int i = 0; i < this.RequiredArguments.Count; i++) {
				this.RequiredValues.Add(this.RequiredArguments[i], args[start + i]);
			}

			return this.RequiredArguments.Count;
		}

		/// <summary>
		/// Parses the optional arguments of this command from the specified list of arguments, starting from the specified position.
		/// </summary>
		/// <param name="args">The arguments for this command.</param>
		/// <param name="start">The position to start from in the list of arguments.</param>
		/// <returns>The number of arguments that were consumed.</returns>
		private int ParseOptionalArguments(IList<string> args, int start) {
			this.OptionalValues.Clear();

			int i;
			for (i = 0; i + start < args.Count; i++) {
				string argName = args[start + i];

				// Is it long enough to be an optional argument?
				if (argName.Length < 2) {
					break;
				}
				// Is this an optional argument?
				if (argName[0] != '-') {
					break;
				}

				// Which name is used?
				bool longName = false;
				if (argName[1] == '-') {
					argName = args[start + i].Substring(2);
					longName = true;
				} else {
					argName = args[start + i].Substring(1);
					longName = false;
				}

				// Find the argument matching this name.
				bool found = false;
				foreach (OptionalArgument optArt in this.OptionalArguments) {
					// Does the argument name match?
					if ((longName && optArt.LongName == argName) ||
						(!longName && optArt.ShortName == argName)) {
						// Check if enough values are available.
						if (args.Count < start + i + 1 + optArt.ValueNames.Count)
							throw new ArgumentException("Optional argument " + args[start + i] + " requires " + optArt.ValueNames.Count + " values.", nameof(args));

						// Extract the values.
						IList<string> values = args.Skip(start + i + 1).Take(optArt.ValueNames.Count).ToList();
						i += optArt.ValueNames.Count;

						// Check if the optional argument has already been defined.
						if (this.OptionalValues.ContainsKey(optArt.LongName))
							throw new ArgumentException("Optional argument " + args[start + i] + " has already been defined.", nameof(args));

						// Set the values;
						this.OptionalValues.Add(optArt.LongName, values);
						found = true;
					}
				}
				// No argument found?
				if (!found) {
					break;
				}
			}

			return i;
		}

		/// <summary>
		/// Gets the value of the specified required argument.
		/// </summary>
		/// <param name="name">The name of the required argument.</param>
		/// <returns>The value of the specified required argument.</returns>
		protected string GetRequiredValue(string name) {
			return this.RequiredValues[name];
		}

		/// <summary>
		/// Gets the values of the specified optional argument.
		/// </summary>
		/// <param name="longName">The long name of the optional argument.</param>
		/// <returns>The value of the specified optional argument, or null if the argument was not present.</returns>
		protected IList<string> GetOptionalValues(string longName) {
			IList<string> values;
			if (this.OptionalValues.TryGetValue(longName, out values)) {
				this.UsedOptionalValues.Add(longName);
				return values;
			} else {
				return null;
			}
		}

		/// <summary>
		/// When overridden in a derived class, runs the implementation of this command.
		/// </summary>
		protected abstract void RunImplementation();
	}
}
