using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LibTextPet.General;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Runtime.Serialization;

namespace LibTextPet.Plugins {
	/// <summary>
	/// A named section of an INI file, containing any number of properties. All keys and values are by default trimmed of whitespace at the start and end.
	/// </summary>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix")]
	[Serializable]
	public class IniSection : Dictionary<string, string> {
		/// <summary>
		/// Gets the name of this section.
		/// </summary>
		public string Name { get; private set; }

		/// <summary>
		/// Gets the TableID of this section.
		/// </summary>
		public string TableId { get; private set; }

		/// <summary>
		/// Gets a boolean that indicates whether this INI section has verbatim keys and values. If true, then no whitespace trimming is performed.
		/// </summary>
		protected bool Verbatim { get; private set; }

		private const string INI_DIRECTIVE_PREFIX = "INI:";

		private readonly string[] TRUE_NAMES = new string[] { "TRUE", "YES", "T", "Y", "1" };
		private readonly string[] FALSE_NAMES = new string[] { "FALSE", "NO", "F", "N", "0" };

		/// <summary>
		/// Creates an INI section read from the specified text reader with the specified plugin loader.
		/// </summary>
		/// <param name="reader">The text reader to read from.</param>
		/// <param name="loader">The plugin loader that is used.</param>
		public IniSection(TextReader reader, PluginLoader loader)
			: base(StringComparer.OrdinalIgnoreCase) {
			if (reader == null)
				throw new ArgumentNullException(nameof(reader), "The text reader cannot be null.");
			if (reader.Peek() != '[')
				throw new InvalidDataException("The text reader does not contain an INI section name.");

			// Read all directives for this section.
			while (true) {
				// Read section name.
				string name = ReadSectionName(reader).ToUpperInvariant();

				// Check if special INI directive.
				if (!name.StartsWith(INI_DIRECTIVE_PREFIX, StringComparison.OrdinalIgnoreCase)) {
					// If not, stop.
					this.Name = name;
					break;
				}

				// Parse the directive.
				switch (name.Substring(INI_DIRECTIVE_PREFIX.Length)) {
					case "VERBATIM":
						this.Verbatim = true;
						break;
					default:
						// Ignore unknown directives.
						break;
				}
			}

			// Check if the section should be read in verbatim mode.
			if (loader != null) {
				IniLoader iniLoader = loader.GetPluginLoader(this.Name);
				if (iniLoader != null && iniLoader.IsVerbatim) {
					this.Verbatim = true;
				}
			}

			// Read until end of section.
			while (reader.Peek() != -1 && reader.Peek() != '[') {
				// Read next line.
				string l = reader.ReadLine();
				if (!this.Verbatim) {
					l = l.Trim();
				}

				// Skip empty/comment line.
				if (IniFile.IsEmptyOrCommentLine(l)) {
					continue;
				}

				// Read TableID, if it exists.
				if (Regex.IsMatch(l, @"^@[0-9A-Za-z]+$")) {
					this.TableId = l.Substring(1);
					continue;
				}

				// Read property and add it to the dictionary.
				KeyValuePair<string, string> property = ReadProperty(l);
				if (this.ContainsKey(property.Key)) {
					throw new InvalidDataException("The property \"" + property.Key + "\" is already defined.");
				} else {
					this.Add(property.Key, property.Value);
				}
			}
		}

		/// <summary>
		/// Creates an INI section from serialized data.
		/// </summary>
		/// <param name="info">The serialization info.</param>
		/// <param name="context">The serialization context.</param>
		protected IniSection(SerializationInfo info, StreamingContext context)
			: base(info, context) {
			if (info != null) {
				this.Name = info.GetString("Name");
			}
		}

		/// <summary>
		/// Reads an INI section name from the specified text reader.
		/// </summary>
		/// <param name="reader">The text reader to read from.</param>
		/// <returns>The section name that was read.</returns>
		private static string ReadSectionName(TextReader reader) {
			string l = reader.ReadLine();
			
			if (!Regex.IsMatch(l, @"^\[[\w:]+\]$"))
				throw new InvalidDataException("Invalid section name \"" + l + "\".");

			return l.Substring(1, l.Length - 2);
		}

		/// <summary>
		/// Reads a property from the specified text reader.
		/// </summary>
		/// <param name="reader">The text reader to read from.</param>
		/// <returns>The property that was read.</returns>
		private KeyValuePair<string, string> ReadProperty(string l) {
			// Check if valid property line.
			if (!Regex.IsMatch(l, @"^\s*\w+\s*=.*$"))
				throw new InvalidDataException("Invalid property \"" + l + "\".");

			// Extract key and value.
			int sep = l.IndexOf('=');
			string key = l.Substring(0, sep);
			string value = l.Substring(sep + 1);

			// Trim the key and value.
			if (!this.Verbatim) {
				key = key.Trim();
				value = value.Trim();
			}

			return new KeyValuePair<string,string>(key, value);
		}
		
		/// <summary>
		/// Populates a SerializationInfo with the data needed to serialize this object.
		/// </summary>
		/// <param name="info">The SerializationInfo to populate with data.</param>
		/// <param name="context">The destination for this serialization.</param>
		public override void GetObjectData(SerializationInfo info, StreamingContext context) {
			base.GetObjectData(info, context);
			if (info != null) {
				info.AddValue("Name", this.Name);
			}
		}

		/// <summary>
		/// Gets the value of the specified property as a boolean.
		/// </summary>
		/// <param name="key">The key of the property.</param>
		/// <param name="defaultValue">The default value.</param>
		/// <returns>The value of the property, or the specified default value if the property does not exist or its value is invalid.</returns>
		public bool PropertyAsBoolean(string key, bool defaultValue) {
			if (key == null)
				throw new ArgumentNullException(nameof(key), "The key cannot be null.");

			bool r = defaultValue;

			if (!this.Verbatim) {
				key = key.Trim();
			}

			if (this.ContainsKey(key)) {
				if (TRUE_NAMES.Contains(this[key].ToUpperInvariant())) {
					r = true;
				} else if (FALSE_NAMES.Contains(this[key].ToUpperInvariant())) {
					r = false;
				}
			}
			return r;
		}

		/// <summary>
		/// Gets the value of the specified property as a boolean.
		/// </summary>
		/// <param name="key">The key of the property.</param>
		/// <returns>The value of the property, or false if the property does not exist or its value is invalid.</returns>
		public bool PropertyAsBoolean(string key) {
			return this.PropertyAsBoolean(key, false);
		}

		/// <summary>
		/// Gets the value of the specified property as a 64-bit signed integer.
		/// </summary>
		/// <param name="key">The key of the property.</param>
		/// <param name="defaultValue">The default value.</param>
		/// <returns>The value of the property, or the specified default value if the property does not exist or its value is invalid.</returns>
		public long PropertyAsInt64(string key, long defaultValue) {
			if (key == null)
				throw new ArgumentNullException(nameof(key), "The key cannot be null.");

			if (!this.Verbatim) {
				key = key.Trim();
			}

			if (!this.ContainsKey(key) || !NumberParser.TryParseInt64(this[key], out long r)) {
				r = defaultValue;
			}
			return r;
		}

		/// <summary>
		/// Gets the value of the specified property as a 64-bit signed integer.
		/// </summary>
		/// <param name="key">The key of the property.</param>
		/// <returns>The value of the property, or 0 if the property does not exist or its value is invalid.</returns>
		public long PropertyAsInt64(string key) {
			return this.PropertyAsInt64(key, 0);
		}

		/// <summary>
		/// Gets the value of the specified property as a string.
		/// </summary>
		/// <param name="key">The key of the property.</param>
		/// <param name="defaultValue">The default value.</param>
		/// <returns>The value of the property, or the specified default value if the property does not exist or its value is invalid.</returns>
		public string PropertyAsString(string key, string defaultValue) {
			if (key == null)
				throw new ArgumentNullException(nameof(key), "The key cannot be null.");

			string r = defaultValue;

			if (!this.Verbatim) {
				key = key.Trim();
			}

			if (this.ContainsKey(key)) {
				r = this[key];
			}
			return r;
		}

		/// <summary>
		/// Gets the value of the specified property as a string.
		/// </summary>
		/// <param name="key">The key of the property.</param>
		/// <returns>The value of the property, or null if the property does not exist.</returns>
		public string PropertyAsString(string key) {
			return this.PropertyAsString(key, null);
		}

		/// <summary>
		/// Gets the value of the specified property as a comma-separated list of strings.
		/// </summary>
		/// <param name="key">The key of the property.</param>
		/// <returns>The values of the property, or null if the property does not exist.</returns>
		public IList<string> PropertyAsStringList(string key) {
			if (key == null)
				throw new ArgumentNullException(nameof(key), "The key cannot be null.");

			if (!this.Verbatim) {
				key = key.Trim();
			}

			string s = "";
			string[] list;
			if (this.ContainsKey(key)) {
				s = this[key];
				list = s.Split(',');
			} else {
				return null;
			}

			if (!this.Verbatim) {
				for (int i = 0; i < list.Length; i++) {
					list[i] = list[i].Trim();
				}
			}

			return list.ToList();
		}

		/// <summary>
		/// Gets the value of the specified property as a comma-separated list of 64-bit signed integers.
		/// </summary>
		/// <param name="key">The key of the property.</param>
		/// <returns>The values of the property, or null if the property does not exist.</returns>
		public IList<long> PropertyAsInt64List(string key) {
			IList<string> stringList = PropertyAsStringList(key);
			if (stringList == null) {
				return null;
			}

			List<long> int64List = new List<long>(stringList.Count);
			foreach (string s in stringList) {
				if (!NumberParser.TryParseInt64(s, out long n)) {
					throw new ArgumentException("Could not parse \"" + s + "\" as a number.", nameof(key));
				}
				int64List.Add(n);
			}

			return int64List;
		}
	}
}
