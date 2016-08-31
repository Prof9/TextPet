using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using LibTextPet.General;
using LibTextPet.Plugins;

namespace LibTextPet.Msg {
	/// <summary>
	/// A database of script command definition.
	/// </summary>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix")]
	public class CommandDatabase : IPlugin, IEnumerable<CommandDefinition>, INameable {
		public string PluginType => "command database";

		/// <summary>
		/// The commands in this database.
		/// </summary>
		private List<CommandDefinition> definitions;
		/// <summary>
		/// An index mapping for the command names in this database.
		/// </summary>
		private Dictionary<string, List<CommandDefinition>> nameMap;
		/// <summary>
		/// A cached read-only collection of all definitions in this command database.
		/// </summary>
		private ReadOnlyCollection<CommandDefinition> readOnlyDefinitions = null;

		/// <summary>
		/// Gets the last results obtained from a database query.
		/// </summary>
		private ICollection<CommandDefinition> LastResults { get; set; }
		/// <summary>
		/// Gets the last byte sequence used in a database query.
		/// </summary>
		private IList<byte> LastSequence { get; set; }

		/// <summary>
		/// Gets the script snippet used for splitting text boxes.
		/// </summary>
		public Script TextBoxSplitSnippet { get; internal set; }

		/// <summary>
		/// Gets the name of this command database.
		/// </summary>
		public string Name { get; private set; }

		/// <summary>
		/// Creates an empty command database with the specified name.
		/// </summary>
		/// <param name="name">The name of this command database.</param>
		public CommandDatabase(string name) {
			if (name == null)
				throw new ArgumentNullException(nameof(name), "The database name cannot be null.");
			if (String.IsNullOrWhiteSpace(name))
				throw new ArgumentException("The database name cannot be empty.", nameof(name));

			this.Name = name;

			this.definitions = new List<CommandDefinition>();
			this.nameMap = new Dictionary<string, List<CommandDefinition>>();
		}

		/// <summary>
		/// Gets all commands with the given name in this database.
		/// </summary>
		/// <param name="name">The name to search for; case-insensitive.</param>
		/// <returns>The commands found.</returns>
		public IList<CommandDefinition> this[string name] {
			get {
				if (name == null)
					throw new ArgumentNullException(nameof(name), "The name cannot be null.");
				if (name.Length <= 0)
					throw new ArgumentException("The name cannot be empty.", nameof(name));

				return this.Find(name);
			}
		}

		/// <summary>
		/// Gets all commands in this database.
		/// </summary>
		public IList<CommandDefinition> Commands {
			get {
				if (this.readOnlyDefinitions == null) {
					// If uncached, cache the current definitions in the command database as a read-only array.
					this.readOnlyDefinitions = new ReadOnlyCollection<CommandDefinition>(this.readOnlyDefinitions.ToArray());
				}
				return this.readOnlyDefinitions;
			}
		}

		/// <summary>
		/// Adds the given command definition to the database.
		/// </summary>
		/// <param name="definition">The command definition to add.</param>
		public void Add(CommandDefinition definition) {
			if (definition == null)
				throw new ArgumentNullException(nameof(definition), "The command cannot be null.");

			// Add the command to the commands list.
			this.definitions.Add(definition);

			// Add the command to the name map.
			string name = definition.Name.ToUpperInvariant();
			List<CommandDefinition> defs;
			if (!this.nameMap.TryGetValue(name, out defs)) {
				defs = new List<CommandDefinition>();
			}
			defs.Add(definition);
			this.nameMap[name] = defs;

			// Delete the cached read-only definitions collection.
			this.readOnlyDefinitions = null;
		}

		/// <summary>
		/// Finds all commands with the given name in this database.
		/// </summary>
		/// <param name="name">The name to search for; case-insensitive.</param>
		/// <returns>The commands found.</returns>
		public IList<CommandDefinition> Find(string name) {
			if (name == null)
				throw new ArgumentNullException(nameof(name), "The name cannot be null.");
			if (name.Length <= 0)
				throw new ArgumentException("The name cannot be empty.", nameof(name));

			List<CommandDefinition> result;

			// Find all command definitions with the given name.
			List<CommandDefinition> defs;
			if (this.nameMap.TryGetValue(name.ToUpperInvariant(), out defs)) {
				result = new List<CommandDefinition>(defs.Count);
				foreach (CommandDefinition def in defs) {
					result.Add(def);
				}
			} else {
				result = new List<CommandDefinition>(0);
			}

			return result;
		}

		/// <summary>
		/// Caches the given command definition results for the given sequence lookup.
		/// </summary>
		/// <param name="sequence">The byte sequence that was matched.</param>
		/// <param name="results">The resulting command definitions.</param>
		private void Cache(IList<byte> sequence, ICollection<CommandDefinition> results) {
			if (sequence == null)
				throw new ArgumentNullException(nameof(sequence), "The byte sequence cannot be null.");
			if (results == null)
				throw new ArgumentNullException(nameof(results), "The results cannot be null.");

			// If the sequence is empty or there are no results, it makes no sense to cache.
			if (sequence.Count > 0 && results.Count > 0) {
				// Cache the results, making sure to clone everything.
				this.LastSequence = new List<byte>(sequence);
				this.LastResults = new List<CommandDefinition>(results);
			}
		}

		/// <summary>
		/// Gets a cached set of cached command definitions based on the specified
		/// byte sequence, if the byte sequence is a subset of the previously queried
		/// byte sequence.
		/// </summary>
		/// <param name="sequence">The byte sequence to get cached results for.</param>
		/// <returns>A list of cached command definitions, or all command definitions if no suitable cache was available.</returns>
		private ICollection<CommandDefinition> GetCache(IList<byte> sequence) {
			if (sequence == null)
				throw new ArgumentNullException(nameof(sequence), "The byte sequence cannot be null.");

			LinkedList<CommandDefinition> defs = null;

			// Check if there is a cached sequence smaller than the current one.
			if (this.LastSequence != null && this.LastSequence.Count <= sequence.Count) {
				// Check if the cached sequence is a subset of the current one.
				bool isSuitable = true;
				for (int i = 0; i < this.LastSequence.Count; i++) {
					if (this.LastSequence[i] != sequence[i]) {
						isSuitable = false;
						break;
					}
				}

				// Load the cached results if applicable.
				if (isSuitable) {
					defs = new LinkedList<CommandDefinition>(this.LastResults);
				}
			}

			// If no suitable cache found, filter only on first byte.
			if (defs == null) {
				byte first = sequence[0];
				defs = new LinkedList<CommandDefinition>();
				foreach (CommandDefinition def in this.definitions) {
					if ((first & def.Mask[0]) == def.Base[0]) {
						defs.AddLast(def);
					}
				}
			}

			return defs;
		}

		/// <summary>
		/// Finds all commands that are a potential match for the given byte sequence.
		/// If the sequence is a subset of the previously used sequence, the cached
		/// results are used as a starting point.
		/// </summary>
		/// <param name="sequence">The byte sequence to match.</param>
		/// <returns>The matched commands.</returns>
		public IList<CommandDefinition> Match(IList<byte> sequence) {
			if (sequence == null)
				throw new ArgumentNullException(nameof(sequence), "The byte sequence cannot be null.");

			// Get a cached set of results, if available.
			ICollection<CommandDefinition> defs = GetCache(sequence);

			// Keep track of the maximum base length.
			int maxBaseLength = 0;

			// Check every byte in the sequence, while there are command definitions left.
			for (int i = 0; i < sequence.Count && defs.Count > 0; i++) {
				// Remove each command that does not match.
				foreach (CommandDefinition def in defs.ToArray()) {
					// Update the max base length.
					if (def.Base.Count > maxBaseLength) {
						maxBaseLength = def.Base.Count;
					}

					// Remove if base length exceeded or mismatch.
					if (sequence.Count > def.MinimumLength || (sequence[i] & def.Mask[i]) != def.Base[i]) {
						defs.Remove(def);
					}
				}
			}

			// If multiple commands were found, but all bases have been checked completely, return the command with the highest priority.
			//if (defs.Count > 1 && sequence.Count >= maxBaseLength) {
			//	CommandDefinition r = defs.First();
			//	if (r.PriorityLength <= 0) {
			//		defs = new List<CommandDefinition>(1);
			//		defs.Add(r);
			//		Console.WriteLine("WARNING: Full base priority is deprecated (used for \"" + r.Name + "\")");
			//		Console.ReadKey();
			//	}
			//}

			// Cache the results.
			Cache(sequence, defs);

			return new List<CommandDefinition>(defs);
		}

		/// <summary>
		/// Sets the parameter with the given name in the given script command to the given value. If necessary, the command is replaced by an extension command that supports the new range.
		/// </summary>
		/// <param name="cmd">The script command to modify a parameter of.</param>
		/// <param name="parName">The name of the parameter to modify.</param>
		/// <param name="value">The value to set the parameter to.</param>
		/// <param name="dataEntry">The index of the data entry to set.</param>
		/// <returns>The modified command.</returns>
		public Command SetParameter(Command cmd, string parName, string value) {
			return SetParameter(cmd, parName, value, false, 0);
		}

		/// <summary>
		/// Sets the data parameter with the given name in the given script command to the given value. If necessary, the command is replaced by an extension command that supports the new range.
		/// </summary>
		/// <param name="cmd">The script command to modify a data parameter of.</param>
		/// <param name="parName">The name of the data parameter to modify.</param>
		/// <param name="value">The value to set the data parameter to.</param>
		/// <param name="dataEntry">The index of the data entry to set.</param>
		/// <returns>The modified command.</returns>
		public Command SetParameter(Command cmd, string parName, string value, int dataEntry) {
			return SetParameter(cmd, parName, value, true, dataEntry);
		}

		/// <summary>
		/// Sets the parameter with the given name in the given script command to the given value. If necessary, the command is replaced by an extension command that supports the new range.
		/// </summary>
		/// <param name="cmd">The script command to modify a parameter of.</param>
		/// <param name="parName">The name of the parameter to modify.</param>
		/// <param name="value">The value to set the parameter to.</param>
		/// <param name="isDataPar">Whether the parameter is a data parameter or a regular parameter.</param>
		/// <param name="dataEntry">The index of the data entry to set.</param>
		/// <returns>The modified command.</returns>
		private Command SetParameter(Command cmd, string parName, string value, bool isDataPar, int dataEntry) {
			if (cmd == null)
				throw new ArgumentNullException(nameof(cmd), "The script command cannot be null.");
			if (parName == null)
				throw new ArgumentNullException(nameof(parName), "The parameter name cannot be null.");
			if (String.IsNullOrWhiteSpace(parName))
				throw new ArgumentException("The parameter name cannot be empty.", nameof(parName));
			if (isDataPar && (dataEntry < 0 || dataEntry >= cmd.Data.Count))
				throw new ArgumentOutOfRangeException(nameof(dataEntry), dataEntry, "The data entry index falls outside the range of the script command's current collection of data entries.");
			if (!isDataPar && !cmd.Definition.Parameters.Contains(parName))
				throw new ArgumentException("The command does not contain a parameter named \"" + parName + "\".", nameof(parName));
			if (isDataPar && !cmd.Definition.DataParameters.Contains(parName))
				throw new ArgumentException("The command does not contain a data parameter named \"" + parName + "\".", nameof(parName));

			// Get the parameter definition.
			Parameter currentPar;
			if (isDataPar) {
				currentPar = cmd.Data[dataEntry][parName];
			} else {
				currentPar = cmd.Parameters[parName];
			}

			long parsed = currentPar.Definition.ParseString(value);
			if (currentPar.InRange(parsed)) {
				// Set the parameter normally.
				currentPar.SetInt64(parsed);

				// Return the modified original command.
				return cmd;
			} else {
				// Find parameters where the value falls within range.
				IEnumerable<CommandDefinition> cmdDefs = this.FindParameterRange(cmd.Name, parName, isDataPar, parsed);

				foreach (CommandDefinition extDef in cmdDefs) {
					// Check if command is suitable.
					if (!IsSuitable(cmd, extDef, parName)) {
						continue;
					}

					// Create augmented command.
					return AugmentCommand(cmd, extDef, parName, parsed, isDataPar, dataEntry);
				}

				throw new ArgumentOutOfRangeException(nameof(value), value, "Could not find any suitable command extension where the value falls within range.");
			}
		}

		/// <summary>
		/// Creates a new script command augmented from the specified command, with the specified parameter set.
		/// </summary>
		/// <param name="cmd">The script command to augment.</param>
		/// <param name="extDef">The command definition of the augmented command.</param>
		/// <param name="parName">The name of the parameter to set.</param>
		/// <param name="value">The new value to set the parameter to.</param>
		/// <param name="isDataPar">Whether the parameter to set is a data parameter.</param>
		/// <param name="dataEntry">The index of the data entry to set the parameter of. If the parameter is not a data parameter, this method parameter is ignored.</param>
		/// <returns>The augmented command.</returns>
		private static Command AugmentCommand(Command cmd, CommandDefinition extDef, string parName, long value, bool isDataPar, int dataEntry) {
			// Create a new command.
			Command ext = new Command(extDef);

			// Copy over the parameters.
			foreach (Parameter par in cmd.Parameters) {
				// Skip the parameter to be modified.
				if (!isDataPar && StringComparer.OrdinalIgnoreCase.Equals(par.Name, parName)) {
					continue;
				}

				ext.Parameters[par.Name].SetInt64(par.ToInt64());
			}
			// Copy over the data parameters.
			for (int i = 0; i < cmd.Data.Count; i++) {
				ext.Data.Add(ext.Data.CreateDefaultEntry());

				foreach (Parameter par in cmd.Data[i]) {
					// Skip the data parameter to be modified.
					if (isDataPar && dataEntry == i && StringComparer.OrdinalIgnoreCase.Equals(par.Name, parName)) {
						continue;
					}

					ext.Data[i][par.Name].SetInt64(par.ToInt64());
				}
			}

			// Set the new parameter.
			if (isDataPar) {
				ext.Data[dataEntry][parName].SetInt64(value);
			} else {
				ext.Parameters[parName].SetInt64(value);
			}

			return ext;
		}

		/// <summary>
		/// Checks whether the specified command definition is suitable for the specified command, with optional exclusion of the specified parameters.
		/// </summary>
		/// <param name="command">The command to check against.</param>
		/// <param name="newDefinition">The new command definition to check for suitability.</param>
		/// <param name="excludedParameters">The parameters to exclude from the check.</param>
		/// <returns>true if the command definition is suitable; otherwise, false.</returns>
		public static bool IsSuitable(Command command, CommandDefinition newDefinition, params string[] excludedParameters) {
			if (command == null)
				throw new ArgumentNullException(nameof(command), "The command cannot be null.");
			if (newDefinition == null)
				return false;			
			
			// Check regular parameters.
			foreach (Parameter par in command.Parameters) {
				// Check if the new definition contains all parameters.
				if (!newDefinition.Parameters.Contains(par.Name)) {
					return false;
				}

				// If parameter is excluded, skip range check.
				if (excludedParameters.Contains(par.Name, StringComparer.OrdinalIgnoreCase)) {
					continue;
				}

				// Check if parameter is in range.
				if (!newDefinition.Parameters[par.Name].InRange(par.ToInt64())) {
					return false;
				}
			}

			// Check data parameters.
			foreach (ReadOnlyNamedCollection<Parameter> entry in command.Data) {
				foreach (Parameter par in entry) {
					// Check if the new definition contains all parameters.
					if (!newDefinition.DataParameters.Contains(par.Name)) {
						return false;
					}

					// If parameter is excluded, skip range check.
					if (excludedParameters.Contains(par.Name, StringComparer.OrdinalIgnoreCase)) {
						continue;
					}

					// Check if parameter is in range.
					if (!newDefinition.DataParameters[par.Name].InRange(par.ToInt64())) {
						return false;
					}
				}
			}

			// All checks passed.
			return true;
		}

		/// <summary>
		/// Finds all commands with the given name where the given value falls within the range of the given parameter.
		/// </summary>
		/// <param name="cmdName">The name of the script command to search for; case-insensitive.</param>
		/// <param name="parName">The name of the parameter to check the range of; case-insensitive.</param>
		/// <param name="dataPar">Whether the parameter is a data parameter or a regular parameter.</param>
		/// <param name="value">The value to check.</param>
		/// <returns>The commands found.</returns>
		public IEnumerable<CommandDefinition> FindParameterRange(string cmdName, string parName, bool dataPar, long value) {
			if (cmdName == null)
				throw new ArgumentNullException(nameof(cmdName), "The command name cannot be null.");
			if (String.IsNullOrWhiteSpace(cmdName))
				throw new ArgumentException("The command name cannot be empty.", nameof(cmdName));
			if (parName == null)
				throw new ArgumentNullException(nameof(parName), "The parameter name cannot be null.");
			if (String.IsNullOrWhiteSpace(parName))
				throw new ArgumentException("The parameter name cannot be empty.", nameof(parName));
			
			// Check every command with the given name.
			foreach (CommandDefinition cmdDef in this.Find(cmdName)) {
				// Find the proper parameter definition.
				ParameterDefinition parDef = null;
				if (dataPar && cmdDef.DataParameters.Contains(parName)) {
					parDef = cmdDef.DataParameters[parName];
				} else if (!dataPar && cmdDef.Parameters.Contains(parName)) {
					parDef = cmdDef.Parameters[parName];
				}

				if (parDef != null) {
					// If the given value is in range for the given parameter, return it as a result.
					if (parDef.InRange(value)) {
						yield return cmdDef;
					}
				}
			}
		}

		/// <summary>
		/// Returns an enumerator that iterates through the script commands in this command database.
		/// </summary>
		/// <returns>The enumerator.</returns>
		IEnumerator IEnumerable.GetEnumerator() {
			return this.definitions.GetEnumerator();
		}

		/// <summary>
		/// Returns an enumerator that iterates through the script commands in this command database.
		/// </summary>
		/// <returns>The enumerator.</returns>
		public IEnumerator<CommandDefinition> GetEnumerator() {
			return this.definitions.GetEnumerator();
		}
	}
}
