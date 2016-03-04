using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace LibTextPet.Plugins {
	/// <summary>
	/// An INI file, containing any number of sections.
	/// </summary>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix")]
	internal class IniFile : List<IniSection> {
		/// <summary>
		/// Creates a new INI file read from the specified text reader.
		/// </summary>
		/// <param name="reader">The text reader to read from.</param>
		/// <param name="loader">The plugin loader that is used to process the reader INI file.</param>
		public IniFile(TextReader reader, PluginLoader loader) {
			if (reader == null)
				throw new ArgumentNullException(nameof(reader), "The text reader cannot be null.");

			// Skip to first section.
			while (reader.Peek() != '[') {
				if (reader.Peek() == -1) {
					// Empty INI file.
					return;
				}

				// Skip empty/comment line.
				string l = reader.ReadLine();
				if (!IsEmptyOrCommentLine(l)) {
					throw new InvalidDataException("Invalid section name \"" + l + "\".");
				}
			}

			// Read sections until end.
			while (reader.Peek() != -1) {
				this.Add(new IniSection(reader, loader));
			}
		}

		/// <summary>
		/// Checks whether the specified line is empty or contains a comment.
		/// </summary>
		/// <param name="l">The line to check.</param>
		/// <returns>true if the specified line is empty or contains a comment; otherwise, false.</returns>
		public static bool IsEmptyOrCommentLine(string l) {
			if (l == null)
				throw new ArgumentNullException(nameof(l), "The line cannot be null.");

			return Regex.IsMatch(l, @"^\s*([;#].*)?$");
		}
	}
}