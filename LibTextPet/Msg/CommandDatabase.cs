using LibTextPet.General;
using LibTextPet.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LibTextPet.Msg {
	/// <summary>
	/// A database of script command definition.
	/// </summary>
	public class CommandDatabase : IPlugin, INameable {
		public string PluginType => "Command Database";

		/// <summary>
		/// Gets the lookup tree for looking up command definitions by masked byte sequences.
		/// </summary>
		public LookupTree<MaskedByte, CommandDefinition> BytesToCommandLookup { get; }
		/// <summary>
		/// Gets the dictionary for looking up command definitions by name.
		/// </summary>
		public Dictionary<string, List<CommandDefinition>> StringToCommandDictionary { get; }

		/// <summary>
		/// Gets the script snippet used for splitting text boxes.
		/// </summary>
		public Script TextBoxSplitSnippet { get; internal set; }

		/// <summary>
		/// Gets the name of this command database.
		/// </summary>
		public string Name { get; }

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

			this.BytesToCommandLookup = new MaskedByteLookupTree<CommandDefinition>();
			this.StringToCommandDictionary = new Dictionary<string, List<CommandDefinition>>(StringComparer.OrdinalIgnoreCase);
		}

		/// <summary>
		/// Adds the given command definition to the database.
		/// </summary>
		/// <param name="definition">The command definition to add.</param>
		public void Add(CommandDefinition definition) {
			if (definition == null)
				throw new ArgumentNullException(nameof(definition), "The command definition cannot be nul.");

			// Add the command to the lookup.
			this.BytesToCommandLookup.Add(definition.Base, definition);

			// Add the command to the name map.
			if (!this.StringToCommandDictionary.TryGetValue(definition.Name, out List<CommandDefinition> defs)) {
				defs = new List<CommandDefinition>();
			}
			defs.Add(definition);
			this.StringToCommandDictionary[definition.Name] = defs;
		}

		/// <summary>
		/// Gets all commands with the given name in this database.
		/// </summary>
		/// <param name="name">The name to search for; case-insensitive.</param>
		/// <returns>The commands found.</returns>
		public IEnumerable<CommandDefinition> this[string name] {
			get {
				if (name == null)
					throw new ArgumentNullException(nameof(name), "The name cannot be null.");
				if (name.Length <= 0)
					throw new ArgumentException("The name cannot be empty.", nameof(name));

				if (this.StringToCommandDictionary.TryGetValue(name, out List<CommandDefinition> defs)) {
					for (int i = 0; i < defs.Count; i++) {
						yield return defs[i];
					}
				} else {
					yield break;
				}
			}
		}

		/// <summary>
		/// Gets all commands in this database.
		/// </summary>
		public IEnumerable<CommandDefinition> Commands {
			get {
				foreach (IList<CommandDefinition> defs in this.StringToCommandDictionary.Values) {
					for (int i = 0; i < defs.Count; i++) {
						yield return defs[i];
					}
				}
			}
		}
	}
}
