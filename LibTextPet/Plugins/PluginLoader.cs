using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using LibTextPet.General;
using LibTextPet.Text;

namespace LibTextPet.Plugins {
	/// <summary>
	/// Provides methods to load LibMsgBN plugins.
	/// </summary>
	public class PluginLoader {
		private static readonly string[] BUILTIN_PLUGIN_PATHS = new string[] {
			"LibTextPet.Plugins.BuiltIn.bool.ini",
		};

		/// <summary>
		/// The INI loaders that are used to load plugins.
		/// </summary>
		private IniLoader[] loaders;
		/// <summary>
		/// All plugins loaded up to this point, indexed by file path.
		/// </summary>
		private Dictionary<string, IEnumerable<IPlugin>> loadedPlugins;

		/// <summary>
		/// Creates a new plugin loader that can load various plugins.
		/// </summary>
		public PluginLoader() {
			this.loaders = new IniLoader[] {
				new GameLoader(),
				new CommandDatabaseLoader(),
				new TableFileLoader(),
			};

			this.loadedPlugins = new Dictionary<string, IEnumerable<IPlugin>>();
			
			foreach (string path in BUILTIN_PLUGIN_PATHS) {
				IEnumerable<IPlugin> plugins = LoadPlugins(new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream(path)));
				this.loadedPlugins.Add(path, plugins);
			}
		}

		/// <summary>
		/// Gets the plugins loaded by this plugin loader.
		/// </summary>
		public IEnumerable<IPlugin> Plugins {
			get {
				foreach (IEnumerable<IPlugin> pluginSet in this.loadedPlugins.Values) {
					foreach (IPlugin plugin in pluginSet) {
						yield return plugin;
					}
				}
			}
		}

		/// <summary>
		/// Loads TextPet plugins from the specified file. If the plugins were previously loaded, they are not reloaded.
		/// </summary>
		/// <param name="filePath">The path of the file to read.</param>
		/// <returns>The loaded plugins.</returns>
		public IEnumerable<IPlugin> LoadPlugins(string filePath) {
			return this.LoadPlugins(filePath, false);
		}

		/// <summary>
		/// Loads TextPet plugins from the specified file.
		/// </summary>
		/// <param name="filePath">The path of the file to read.</param>
		/// <param name="reload">Whether to reload the file if it has already been loaded.</param>
		/// <returns>The loaded plugins.</returns>
		public IEnumerable<IPlugin> LoadPlugins(string filePath, bool reload) {
			if (filePath == null)
				throw new ArgumentNullException(nameof(filePath), "The file path cannot be null.");


			// Check if plugins for this file have previously been loaded.
			if (!reload && this.loadedPlugins.TryGetValue(filePath, out IEnumerable<IPlugin> plugins)) {
				return plugins;
			}

			using (MemoryStream ms = new MemoryStream()) {
				// Prepend [TableFile] for .tbl files only.
				bool isTableFile = false;
				if (filePath.EndsWith(".TBL", StringComparison.OrdinalIgnoreCase)) {
					isTableFile = true;
					StreamWriter writer = new StreamWriter(ms, new UTF8Encoding(false, true));
					writer.WriteLine("[TableFile]");
					writer.WriteLine("name=" + Path.GetFileNameWithoutExtension(filePath));
					writer.Flush();
				}

				using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.SequentialScan)) {
					// Get rid of the BOM for table files.
					if (isTableFile) {
						byte[] bomBuffer = new byte[3];
						fs.Read(bomBuffer, 0, 3);
						// Skip writing the first three bytes if they comprise the BOM.
						if (!ByteSequenceEqualityComparer.Instance.Equals(bomBuffer, new byte[] { 0xEF, 0xBB, 0xBF })) {
							ms.Write(bomBuffer, 0, 3);
						}
					}
					fs.CopyTo(ms);
				}

				// Read from the file.
				ms.Position = 0;
				StreamReader reader = new StreamReader(ms);
				plugins = LoadPlugins(reader);
			}

			// Store the plugins for later.
			this.loadedPlugins.Add(filePath, plugins);

			return plugins;
		}

		/// <summary>
		/// Loads LibMsgBN plugins from the specified text reader.
		/// </summary>
		/// <param name="reader">The text reader to read from.</param>
		/// <returns>The loaded plugins.</returns>
		public IEnumerable<IPlugin> LoadPlugins(TextReader reader) {
			if (reader == null)
				throw new ArgumentNullException(nameof(reader), "The text reader cannot be null.");

			// Load entire INI file first.
			IniFile ini = new IniFile(reader, this);

			List<IPlugin> loaded = new List<IPlugin>();
			
			// Start parsing.
			IEnumerator<IniSection> enumerator = ini.GetEnumerator();
			while (enumerator.MoveNext()) {
				// Find a suitable plugin loader.
				IniLoader loader = GetPluginLoader(enumerator.Current.Name);
				if (loader == null) {
					throw new KeyNotFoundException("No compatible plugin loader found for \"" + enumerator.Current.Name + "\".");
				}

				loaded.Add(loader.LoadPlugin(enumerator));
			}

			return loaded;
		}

		/// <summary>
		/// Gets the appropriate plugin loader for the specified INI section name.
		/// </summary>
		/// <param name="name">The name of the INI section.</param>
		/// <returns>The plugin loader, or null if no specific plugin loader could be found.</returns>
		public IniLoader GetPluginLoader(string name) {
			foreach (IniLoader loader in this.loaders) {
				if (loader.SectionNames.Contains(name, StringComparer.OrdinalIgnoreCase)) {
					return loader;
				}
			}
			return null;
		}
	}
}
