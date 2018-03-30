using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using LibTextPet.Msg;
using LibTextPet.Plugins;
using LibTextPet.Text;

namespace LibTextPet.General {
	/// <summary>
	/// An info object that describes the settings, command databases and table files for a specific game.
	/// </summary>
	public class GameInfo : INameable, IPlugin {
		public string PluginType => "Game Info";

		/// <summary>
		/// The command databases used for this game, in order of preference.
		/// </summary>
		private ReadOnlyCollection<CommandDatabase> loadedDatabases;
		/// <summary>
		/// The encoding used for this game.
		/// </summary>
		private IgnoreFallbackEncoding loadedTextEncoding;
		/// <summary>
		/// The sets of value names used for this game.
		/// </summary>
		private ReadOnlyCollection<IgnoreFallbackEncoding> loadedValueEncodings;

		/// <summary>
		/// Creates a new game info object with the specified abbreviated game name, encoding name and command database names, in order of preference.
		/// </summary>
		/// <param name="name">The abbreviated name of the game.</param>
		/// <param name="encodingName">The name of the encoding for the game.</param>
		/// <param name="databaseNames">The command database names, in order of preference.</param>
		public GameInfo(string name, string encodingName, IList<string> databaseNames)
			:this(name, name, encodingName, databaseNames, new string[0]) { }

		/// <summary>
		/// Creates a new game info object with the specified abbreviated game name, full game name, encoding name and command database names, in order of preference.
		/// </summary>
		/// <param name="name">The abbreviated name of the game.</param>
		/// <param name="encodingName">The name of the encoding for the game.</param>
		/// <param name="databaseNames">The command database names, in order of preference.</param>
		public GameInfo(string name, string fullName, string encodingName, IList<string> databaseNames)
			: this(name, fullName, encodingName, databaseNames, new string[0]) { }

		/// <summary>
		/// Creates a new game info object with the specified abbreviated game name, encoding name and command database names, in order of preference.
		/// </summary>
		/// <param name="name">The abbreviated name of the game.</param>
		/// <param name="encodingName">The name of the encoding for the game.</param>
		/// <param name="databaseNames">The command database names, in order of preference.</param>
		/// <param name="valueEncodingNames">The value encoding names.</param>
		public GameInfo(string name, string encodingName, IList<string> databaseNames, IEnumerable<string> valueEncodingNames)
			: this(name, name, encodingName, databaseNames, valueEncodingNames) { }

		/// <summary>
		/// Creates a new game info object with the specified abbreviated game name, full game name, encoding name, command database names (in order of preference) and value encoding names.
		/// </summary>
		/// <param name="name">The abbreviated name of the game.</param>
		/// <param name="fullName">The full name of the game.</param>
		/// <param name="encodingName">The name of the encoding for the game.</param>
		/// <param name="databaseNames">The command database names, in order of preference.</param>
		public GameInfo(string name, string fullName, string encodingName, IList<string> databaseNames, IEnumerable<string> valueEncodingNames) {
			if (name == null)
				throw new ArgumentNullException(nameof(name), "The game name cannot be null.");
			if (encodingName == null)
				throw new ArgumentNullException(nameof(encodingName), "The encoding name cannot be null.");
			if (databaseNames == null)
				throw new ArgumentNullException(nameof(databaseNames), "The command database names cannot be null.");
			if (!databaseNames.Any())
				throw new ArgumentException("At least one command database name must be specified.", nameof(databaseNames));
			if (!databaseNames.All(s => s.Length > 0))
				throw new ArgumentException("A command database name cannot be an empty string.", nameof(databaseNames));

			loadedDatabases = null;
			loadedTextEncoding = null;
			loadedValueEncodings = null;

			this.Name = name;
			this.FullName = fullName;
			this.EncodingName = encodingName;
			this.DatabaseNames = databaseNames.Select(s => s.ToUpperInvariant()).ToList();
			this.ValueEncodingNames = valueEncodingNames;
		}

		/// <summary>
		/// Loads the command databases and encoding for this game from the specified set of plugins.
		/// </summary>
		/// <param name="plugins">The set of plugins to load from.</param>
		public void Load(IEnumerable<IPlugin> plugins) {
			if (plugins == null) {
				throw new ArgumentNullException(nameof(plugins), "The set of plugins cannot be null.");
			}

			CommandDatabase[] databases = new CommandDatabase[this.DatabaseNames.Count];
			IgnoreFallbackEncoding encoding = null;
			IDictionary<string, IgnoreFallbackEncoding> valueEncodings
				= new Dictionary<string, IgnoreFallbackEncoding>(StringComparer.OrdinalIgnoreCase);

			// Loop through every plugin.
			foreach (IPlugin plugin in plugins) {
				if (TryLoadCommandDatabase(plugin, databases)) {
					continue;
				}

				if (plugin is IgnoreFallbackEncoding pluginAsEncoding) {
					if (plugin.Name.Equals(this.EncodingName, StringComparison.OrdinalIgnoreCase)) {
						if (encoding != null)
							throw new ArgumentException("Encoding name \"" + this.EncodingName + "\" is ambiguous.", nameof(plugins));

						encoding = pluginAsEncoding;
					} else if (this.ValueEncodingNames.Contains(plugin.Name, StringComparer.OrdinalIgnoreCase)) {
						if (valueEncodings.ContainsKey(plugin.Name))
							throw new ArgumentException("Value encoding name \"" + this.EncodingName + "\" is ambiguous.", nameof(plugins));

						valueEncodings[plugin.Name] = pluginAsEncoding;
					}
				}
			}

			// Load the value encodings for each database.
			for (int i = 0; i < databases.Length; i++) {
				if (databases[i] == null) {
					throw new ArgumentException("Could not find command database with name \"" + this.DatabaseNames[i] + "\".", nameof(plugins));
				}
			}
			// Check if the encoding was loaded.
			if (encoding == null) {
				throw new ArgumentException("Could not find encoding with name \"" + this.EncodingName + "\".", nameof(plugins));
            }

			// Load the value encodings for each database.
			foreach (CommandDatabase db in databases) {
				LoadValueEncodings(db, valueEncodings);
			}

			this.Databases = databases;
			this.Encoding = encoding;
			this.ValueEncodings = valueEncodings.Values;
		}

		/// <summary>
		/// Loads the specified plugin as a command database into the specified list of command databases, and returns a boolean indicating whether this succeeded.
		/// </summary>
		/// <param name="plugin">The plugin to load as a command database.</param>
		/// <param name="databases">The list of command databases to load into.</param>
		/// <returns>true if the plugin could be loaded as a command database; otherwise, false.</returns>
		private bool TryLoadCommandDatabase(IPlugin plugin, IList<CommandDatabase> databases) {
			CommandDatabase database = plugin as CommandDatabase;
			int preference = -1;

			if (database != null) {
				preference = this.DatabaseNames.IndexOf(database.Name.ToUpperInvariant());
			}

			if (preference >= 0) {
				if (databases[preference] != null) {
					throw new ArgumentException($"Command database name \"" + this.DatabaseNames[preference] + "\" is ambiguous.", nameof(plugin));
				}

				databases[preference] = database;
				return true;
			} else {
				return false;
			}
		}

		/// <summary>
		/// Loads the value encodings for the specified command database from the specified value encodings.
		/// </summary>
		/// <param name="db">The command database.</param>
		/// <param name="valueEncodings">The value encodings.</param>
		private static void LoadValueEncodings(CommandDatabase db, IDictionary<string, IgnoreFallbackEncoding> valueEncodings) {
			// Iterate through all commands in the database.
			foreach (CommandDefinition cmd in db) {
				// Iterate through all parameters.
				foreach (ParameterDefinition par in cmd.FlattenParameters()) {
					// Does the parameter not have value encoding or is it already loaded?
					if (par.ValueEncodingName == null || par.ValueEncoding != null) {
						continue;
					}

					// Check if the value encoding is available.
					if (!valueEncodings.ContainsKey(par.ValueEncodingName)) {
						throw new ArgumentException("Unrecognized value encoding name \"" + par.ValueEncodingName + "\".", nameof(db));
					}

					// Set the value encoding.
					par.ValueEncoding = valueEncodings[par.ValueEncodingName];
				}
			}
		}

		/// <summary>
		/// Gets the abbreviated name of the game.
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// Gets the full name of the game.
		/// </summary>
		public string FullName { get; }

		/// <summary>
		/// Gets the names of the command databases used for the game, in order of preference.
		/// </summary>
		public IList<string> DatabaseNames { get; }

		/// <summary>
		/// Gets the name of the encoding for the game.
		/// </summary>
		public string EncodingName { get; }

		/// <summary>
		/// Gets the names of the value encoding used for the game.
		/// </summary>
		public IEnumerable<string> ValueEncodingNames { get; }

		/// <summary>
		/// Gets the command databases used for the game, in order of preference.
		/// </summary>
		public IList<CommandDatabase> Databases {
			get {
				if (this.loadedDatabases == null)
					throw new InvalidOperationException("The command databases for this game info object have not yet been loaded.");
				
				return this.loadedDatabases;
			}
			private set {
				if (value == null) 
					throw new ArgumentNullException(nameof(value), "The command databases cannot be null.");
				
				this.loadedDatabases = new ReadOnlyCollection<CommandDatabase>(value);
			}
		}

		/// <summary>
		/// Gets the encoding used for the game.
		/// </summary>
		public IgnoreFallbackEncoding Encoding {
			get {
				if (this.loadedTextEncoding == null) {
					throw new InvalidOperationException("The encoding for this game info object has not yet been loaded.");
				} else {
					return this.loadedTextEncoding;
				}
			}
			private set {
				if (value == null)
					throw new ArgumentNullException(nameof(value), "The encoding cannot be null.");

				this.loadedTextEncoding = value;
			}
		}

		/// <summary>
		/// Gets the value encodings used for the game.
		/// </summary>
		public ICollection<IgnoreFallbackEncoding> ValueEncodings {
			get {
				if (this.loadedValueEncodings == null) {
					throw new InvalidOperationException("The encoding for this game info object has not yet been loaded.");
				} else {
					return this.loadedValueEncodings;
				}
			}
			private set {
				if (value == null)
					throw new ArgumentNullException(nameof(value), "The value encodings cannot be null.");

				this.loadedValueEncodings = new ReadOnlyCollection<IgnoreFallbackEncoding>(value.ToList());
			}
		}

		/// <summary>
		/// Gets a boolean that indicates whether the plugins for the game have been loaded.
		/// </summary>
		public bool Loaded {
			get {
				return this.loadedDatabases != null && this.Encoding != null;
			}
		}
	}
}
