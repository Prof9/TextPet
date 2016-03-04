using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LibTextPet.Plugins {
	public abstract class IniLoader {
		/// <summary>
		/// Gets the INI section names that this INI loader can read.
		/// </summary>
		public abstract IEnumerable<string> SectionNames { get; }

		/// <summary>
		/// Gets a boolean that indicates whether the plugin's sections should be read in verbatim mode.
		/// In this mode, whitespace is treated as significant and comments are not possible.
		/// </summary>
		public virtual bool IsVerbatim {
			get {
				return false;
			}
		}

		/// <summary>
		/// Loads a plugin from the specified INI section enumerator.
		/// </summary>
		/// <param name="enumerator">The enumerator to read from.</param>
		/// <returns>The resulting plugin.</returns>
		public abstract IPlugin LoadPlugin(IEnumerator<IniSection> enumerator);

		/// <summary>
		/// Checks whether the name of the current INI section in the specified INI section enumerator
		/// matches any of the specified names, and throws an exception if it does not; case insensitive.
		/// </summary>
		/// <param name="enumerator">The enumerator to check.</param>
		/// <param name="names">The names to check against.</param>
		public static void ValidateCurrentSectionNameAny(IEnumerator<IniSection> enumerator, params string[] names) {
			if (enumerator == null)
				throw new ArgumentNullException(nameof(enumerator), "The enumerator cannot be null.");
			if (names == null)
				throw new ArgumentNullException(nameof(names), "The property names cannot be null.");

			if (names.Length == 0) {
				return;
			}

			foreach (string name in names) {
				if (name == null)
					throw new ArgumentNullException(nameof(names), "The property names cannot be null.");

				if (String.Equals(enumerator.Current.Name, name, StringComparison.OrdinalIgnoreCase)) {
					return;
				}
			}
			throw new InvalidDataException("The current INI section does not have the expected name.");
		}

		/// <summary>
		/// Checks whether the current INI section in the specified INI section enumerator contains all
		/// of the specified properties, and throws an exception if it does not; case insensitive.
		/// </summary>
		/// <param name="enumerator">The enumerator to check.</param>
		/// <param name="properties">The properties to check for.</param>
		public static void ValidateCurrentSectionPropertiesAll(IEnumerator<IniSection> enumerator, params string[] properties) {
			if (properties == null)
				throw new ArgumentNullException(nameof(properties), "The property names cannot be null.");

			if (enumerator == null)
				throw new ArgumentNullException(nameof(enumerator), "The enumerator cannot be null.");

			foreach (string property in properties) {
				if (!enumerator.Current.ContainsKey(property)) {
					throw new ArgumentException("Required property \"" + property + "\" is missing in " + enumerator.Current.Name + " section.");
				}
			}
		}

		/// <summary>
		/// Advances the specified enumerator, taking into account whether it should skip or stop.
		/// </summary>
		/// <param name="enumerator">The enumerator to advance.</param>
		/// <param name="skip">true if the enumerator should advance without moving to the next element; otherwise, false.</param>
		/// <param name="stop">true if the enumerator should stop; otherwise, false.</param>
		/// <returns>true if the enumerator advances; otherwise, false.</returns>
		public static bool AdvanceEnumerator(IEnumerator<object> enumerator, bool skip, bool stop) {
			if (enumerator == null)
				throw new ArgumentNullException(nameof(enumerator), "The enumerator cannot be null.");

			return !stop && ((skip && enumerator.Current != null) || (enumerator.MoveNext()));
		}
	}
}
