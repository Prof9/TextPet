using LibTextPet.General;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LibTextPet.Plugins {
	internal class GameLoader : IniLoader {
		/// <summary>
		/// Gets the INI section names that this plugin loader can read.
		/// </summary>
		public override IEnumerable<string> SectionNames {
			get {
				return new string[] { "Game" };
			}
		}

		/// <summary>
		/// Loads game info from the specified INI section enumerator.
		/// </summary>
		/// <param name="enumerator">The enumerator to read from.</param>
		/// <returns>The resulting game info.</returns>
		public override IPlugin LoadPlugin(IEnumerator<IniSection> enumerator) {
			if (enumerator == null)
				throw new ArgumentNullException(nameof(enumerator), "The enumerator cannot be null.");
			
			return LoadGame(enumerator);
		}

		/// <summary>
		/// Loads game info from the specified INI section enumerator.
		/// </summary>
		/// <param name="enumerator">The enumerator to read from.</param>
		/// <returns>The resulting game info.</returns>
		private static GameInfo LoadGame(IEnumerator<IniSection> enumerator) {
			if (enumerator == null)
				throw new ArgumentNullException(nameof(enumerator), "The enumerator cannot be null.");
			
			ValidateCurrentSectionNameAny(enumerator, "Game");
			ValidateCurrentSectionPropertiesAll(enumerator, "name", "cdbs", "tblf");

			string name = enumerator.Current.PropertyAsString("NAME");
			IEnumerable<string> dbNames = enumerator.Current.PropertyAsStringList("CDBS") ?? new List<string>();
			string encName = enumerator.Current.PropertyAsString("TBLF");

			string fullName = enumerator.Current.PropertyAsString("FULL", "");
			ICollection<string> valNames = enumerator.Current.PropertyAsStringList("VALS") ?? new List<string>();

			// Add default plugins.
			valNames.Add("bool");

			if (fullName != null && fullName.Length > 0) {
				return new GameInfo(name, fullName, encName, dbNames.ToArray(), valNames);
			} else {
				return new GameInfo(name, encName, dbNames.ToArray(), valNames);
			}
		}
	}
}
