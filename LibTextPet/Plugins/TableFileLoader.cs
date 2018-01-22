using LibTextPet.Text;
using LibTextPet.General;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LibTextPet.Plugins {
	internal class TableFileLoader : IniLoader {
		public override IEnumerable<string> SectionNames {
			get {
				return new string[] {
					"TableFile"
				};
			}
		}

		public override bool IsVerbatim {
			get {
				return true;
			}
		}

		public override IPlugin LoadPlugin(IEnumerator<IniSection> enumerator) {
			if (enumerator == null)
				throw new ArgumentNullException(nameof(enumerator), "The enumerator cannot be null.");

			string name = enumerator.Current.TableId;

			IEnumerable<byte> bytes = null;
			Dictionary<byte[], string> dict = new Dictionary<byte[], string>();

			// Cycle through all properties.
			foreach (KeyValuePair<string, string> pair in enumerator.Current) {
				// Parse the key as hex string.
				if (NumberParser.TryParseHexString(pair.Key, out bytes)) {
					// Map the character(s) to the byte sequence.
					dict.Add(bytes.ToArray(), pair.Value);
				} else {
					switch (pair.Key.ToUpperInvariant()) {
						case "NAME":
							name = pair.Value;
							break;
						default:
							throw new ArgumentException("Could not parse \"" + pair.Key + "\" as a hexadecimal string.");
					}
				}
			}

			if (name == null)
				throw new ArgumentException("No encoding name or table ID specified.", nameof(enumerator));

			// Create a new lookup table encoding.
			return new IgnoreFallbackEncoding(new LookupTableEncoding(name, dict));
		}
	}
}
