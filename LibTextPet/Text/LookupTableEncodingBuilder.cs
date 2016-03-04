using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace LibTextPet.Text {
	/// <summary>
	/// Provides methods to build lookup table encodings.
	/// </summary>
	public static class LookupTableEncodingBuilder {
		/// <summary>
		/// Builds a lookup table encoding with the table in the specified string.
		/// <para>Level 1 table file standard compliance.</para>
		/// </summary>
		/// <param name="tableFile">The contents of the table file, passed as a string.</param>
		/// <returns>A lookup table encoding built from the specified lookup table.</returns>
		public static LookupTableEncoding Make(string tableFile) {
			if (tableFile == null)
				throw new ArgumentNullException(nameof(tableFile), "The table file cannot be null.");

			string name = null;

			// Split the table file into lines.
			string[] lines = Regex.Split(tableFile, @"\r?\n");

			// Initialize lookup table.
			Dictionary<byte[], string> lookupTable = new Dictionary<byte[], string>();

			// Process all lines in the dictionary.
			int equalsPos;
			for (int i = 0; i < lines.Length; i++) {
				string line = lines[i];

				// Table ID
				if (line[0] == '@') {
					if (line.Contains(','))
						throw new ArgumentException("Table ID cannot contain commas (line " + (i + 1) + ").", nameof(tableFile));

					if (name == null) {
						name = line.Substring(1);
					}
				}

				equalsPos = line.IndexOf('=');
				if (equalsPos < 0)
					continue;
				if (equalsPos == 0)
					throw new ArgumentException("Empty byte string on line " + (i + 1) + ".", nameof(tableFile));

				string byteString = line.Substring(0, equalsPos);
				string stringString = line.Substring(equalsPos + 1, line.Length - equalsPos - 1);

				// Ignore special prefixes from advanced table file format.
				int start = 0;
				while (byteString[0] == '$' || byteString[0] == '/' || byteString[0] == '!')
					start++;
				byteString = byteString.Substring(start);

				if (byteString.Length % 2 != 0)
					throw new ArgumentException("Length of byte string on line " + (i + 1) + " is not a multiple of 2.");

				byte[] bytes = new byte[byteString.Length / 2];
				for (int byteNum = 0; byteNum < bytes.Length; byteNum++) {
					bytes[byteNum] = byte.Parse(byteString.Substring(byteNum * 2, 2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture);
				}

				lookupTable.Add(bytes, stringString);
			}

			return new LookupTableEncoding(name, lookupTable);
		}
	}
}
