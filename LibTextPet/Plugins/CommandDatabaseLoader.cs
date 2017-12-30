using LibTextPet.General;
using LibTextPet.IO.TPL;
using LibTextPet.Msg;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LibTextPet.Plugins {
	internal class CommandDatabaseLoader : IniLoader {
		/// <summary>
		/// Gets the INI section names that this plugin loader can read.
		/// </summary>
		public override IEnumerable<string> SectionNames {
			get {
				return new string[] {
					"CommandDatabase"
				};
			}
		}

		/// <summary>
		/// Loads a command database from the specified INI section enumerator.
		/// </summary>
		/// <param name="enumerator">The enumerator to read from.</param>
		/// <returns>The resulting command database.</returns>
		public override IPlugin LoadPlugin(IEnumerator<IniSection> enumerator) {
			if (enumerator == null)
				throw new ArgumentNullException(nameof(enumerator), "The enumerator cannot be null.");

			return LoadCommandDatabase(enumerator);
		}

		/// <summary>
		/// Loads a command database from the specified INI section enumerator.
		/// </summary>
		/// <param name="enumerator">The enumerator to read from.</param>
		/// <returns>The resulting command database.</returns>
		private static CommandDatabase LoadCommandDatabase(IEnumerator<IniSection> enumerator) {
			if (enumerator == null)
				throw new ArgumentNullException(nameof(enumerator), "The enumerator cannot be null.");

			ValidateCurrentSectionNameAny(enumerator, "CommandDatabase");
			ValidateCurrentSectionPropertiesAll(enumerator, "name");

			CommandDatabase db = new CommandDatabase(enumerator.Current["name"]);
			string split = enumerator.Current.PropertyAsString("splt", null);
			IList<long> jumpContVals = enumerator.Current.PropertyAsInt64List("cont");

			bool skip = false;
			bool stop = false;
			while (AdvanceEnumerator(enumerator, skip, stop)) {
				skip = false;
				switch (enumerator.Current.Name.ToUpperInvariant()) {
					case "COMMAND":
						db.Add(LoadCommandDefinition(enumerator, db, jumpContVals, false));
						skip = true;
						break;
					case "EXTENSION":
						db.Add(LoadCommandDefinition(enumerator, db, jumpContVals, true));
						skip = true;
						break;
					default:
						stop = true;
						break;
				}
			}

			if (split != null) {
				Script splitScript;

				// Create a temporary stream for the split script.
				using (MemoryStream splitScriptStream = new MemoryStream()) {
					StreamWriter writer = new StreamWriter(splitScriptStream, new UTF8Encoding(false, true));
					writer.WriteLine(split);
					writer.Flush();

					// Parse the split script.
					splitScriptStream.Position = 0;
					TPLScriptReader scriptReader = new TPLScriptReader(splitScriptStream, db);
					IList<Script> readScripts = scriptReader.Read();
					if (readScripts.Count == 0) {
						splitScript = new Script();
					} else if (readScripts.Count == 1) {
						splitScript = readScripts[0];
					} else {
						throw new InvalidDataException("Only one split script can be defined.");
					}
				}

				splitScript.DatabaseName = db.Name;
				db.TextBoxSplitSnippet = splitScript;
			}

			return db;
		}

		/// <summary>
		/// Loads a command definition from the specified INI section enumerator.
		/// </summary>
		/// <param name="enumerator">The enumerator to read from.</param>
		/// <param name="defs">The previously loaded command defitions.</param>
		/// <param name="jumpContVals">The values for jump targets that lead to continuing the current script.</param>
		/// <param name="isExt">true if the command definition is an extension; otherwise, false.</param>
		/// <returns>The resulting command definition.</returns>
		private static CommandDefinition LoadCommandDefinition(IEnumerator<IniSection> enumerator, IEnumerable<CommandDefinition> defs, IList<long> jumpContVals, bool isExt) {
			if (enumerator == null)
				throw new ArgumentNullException(nameof(enumerator), "The enumerator cannot be null.");
			if (defs == null)
				throw new ArgumentNullException(nameof(defs), "The command definitions cannot be null.");

			if (isExt) {
				ValidateCurrentSectionNameAny(enumerator, "EXTENSION");
			} else {
				ValidateCurrentSectionNameAny(enumerator, "COMMAND");
			}
			ValidateCurrentSectionPropertiesAll(enumerator, "NAME", "MASK", "BASE");

			IniSection cmdSection = enumerator.Current;

			// Set required properties.
			string name = cmdSection.PropertyAsString("NAME");
			string baseString = cmdSection.PropertyAsString("BASE", "");
			string maskString = cmdSection.PropertyAsString("MASK", "");

			// Parse base and mask.
			byte[] baseSeq = NumberParser.ParseHexString(baseString).ToArray();
			byte[] maskSeq = NumberParser.ParseHexString(maskString).ToArray();
			if (baseSeq.Length < maskSeq.Length) {
				Array.Resize(ref baseSeq, maskSeq.Length);
			} else if (baseSeq.Length > maskSeq.Length) {
				throw new InvalidDataException("Base sequence for command \"" + name + "\" is longer than mask sequence.");
			}

			// Find the base command.
			CommandDefinition superCmdDef = FindPreviousCommand(defs, isExt, name);

			// Set optional properties.
			string desc = cmdSection.PropertyAsString("DESC", superCmdDef?.Description ?? "");
			long plen = cmdSection.PropertyAsInt64("PLEN", superCmdDef?.PriorityLength ?? 0);
			EndType ends = ParseEndType(cmdSection.PropertyAsString("ENDS", null) ?? (superCmdDef?.EndType.ToString() ?? EndType.Default.ToString()));
			bool prnt = cmdSection.PropertyAsBoolean("PRNT", superCmdDef?.Prints ?? false);
			string mugs = cmdSection.PropertyAsString("MUGS", superCmdDef?.MugshotParameterName);
			long rwnd = cmdSection.PropertyAsInt64("RWND", superCmdDef?.RewindCount ?? 0);

			// Load parameters.
			List<CommandElementDefinition> elemDefs = LoadCommandElementDefinitions(enumerator, jumpContVals, superCmdDef);

			return new CommandDefinition(name, desc, baseSeq, maskSeq, ends, prnt, mugs, plen, rwnd, elemDefs);
		}

		/// <summary>
		/// Loads command element definitions from the specified INI section enumerator.
		/// </summary>
		/// <param name="enumerator">The enumerator to read from.</param>
		/// <param name="jumpContVals">The values for jump targets that lead to continuing the current script</param>
		/// <param name="superCmdDef">The command definition for the base command, or null if this command is not an extension.</param>
		/// <returns></returns>
		private static List<CommandElementDefinition> LoadCommandElementDefinitions(IEnumerator<IniSection> enumerator, IList<long> jumpContVals, CommandDefinition superCmdDef) {
			// List of command elements in insertion order.
			List<string> elemList = new List<string>();
			// Dictionary of command elements mapping to parameters.
			Dictionary<string, IList<ParameterDefinition>> parDict = CreateParameterDictionary(superCmdDef, elemList);

			bool skip = false;
			bool stop = false;
			while (AdvanceEnumerator(enumerator, skip, stop)) {
				IniSection section = enumerator.Current;

				switch (section.Name.ToUpperInvariant()) {
				case "PARAMETER":
					break;
				default:
					stop = true;
					continue;
				}

				// Validate parameter block.
				ValidateCurrentSectionNameAny(enumerator, "Parameter");
				ValidateCurrentSectionPropertiesAll(enumerator, "name");

				// Get parameter name.
				string parNameFull = section.PropertyAsString("NAME");
				string[] parName = parNameFull.Split('.');
				if (parName.Length >= 2) {
					throw new InvalidDataException("Nested data parameters are not supported.");
				}
				if (parName.Length < 1) {
					throw new InvalidDataException("Unnamed parameters are not supported.");
				}
				string parNameMain = parName[parName.Length - 1];

				// Find the base parameter.
				ParameterDefinition superPar = null;
				if (parDict.ContainsKey(parName[0])) {
					superPar = parDict[parName[0]].FirstOrDefault(pd => pd.Name.Equals(parNameMain, StringComparison.InvariantCultureIgnoreCase));
				}
				if (superCmdDef != null && superPar == null) {
					throw new InvalidDataException("Base parameter \"" + parNameFull + "\" not found.");
				}
				if (superCmdDef == null && superPar != null) {
					throw new InvalidDataException("Parameter \"" + parNameFull + "\" is already defined.");
				}

				// Load properties.
				int offset = 0;
				int shift = 0;
				string offs = section.PropertyAsString("offs", null);
				if (offs != null) {
					string[] offsParts = offs.Split('.', ',');

					if (offsParts.Length < 1 || offsParts.Length > 2) {
						throw new InvalidDataException("Invalid offset format for parameter \"" + parNameFull + "\".");
					}

					offset = NumberParser.ParseInt32(offsParts[0]);
					shift = offsParts.Length == 2 ? NumberParser.ParseInt32(offsParts[1]) : 0;
				} else if (superPar != null) {
					offset = superPar.Offset;
					shift = superPar.Shift;
				}

				int parBits = (int)section.PropertyAsInt64("bits", superPar?.Bits ?? 8);
				string parDesc = section.PropertyAsString("desc", superPar?.Description);
				string parType = section.PropertyAsString("type", "DEC").ToUpperInvariant(); // TODO
				int parAddv = (int)section.PropertyAsInt64("addv", superPar?.Add ?? 0);
				string parValn = section.PropertyAsString("valn", superPar?.ValueEncodingName);

				// Create the parameter.
				bool isJump = parType == "JUMP";
				ParameterDefinition parDef = new ParameterDefinition(parNameMain, parDesc, offset, shift, parBits, parAddv, isJump, parValn);

				// Set jump continue values.
				if (isJump) {
					parDef.JumpContinueValues = jumpContVals;
				}

				// Process the new parameter.
				if (superCmdDef == null) {
					// Add as new parameter.
					if (parName.Length == 1) {
						// Add to top-level elements.
						elemList.Add(parDef.Name);
					}
					if (!parDict.ContainsKey(parName[0])) {
						parDict[parName[0]] = new List<ParameterDefinition>();
					}
					parDict[parName[0]].Add(parDef);
				} else {
					// Replace parameter.
					int i = parDict[parName[0]].IndexOf(superPar);
					parDict[parName[0]][i] = parDef;
				}
			}

			// Create the command elements.
			List<CommandElementDefinition> elemDefs = new List<CommandElementDefinition>();
			foreach (string elemName in elemList) {
				CommandElementDefinition elemDef;
				ICollection<ParameterDefinition> parDefs = parDict[elemName];

				if (parDefs.Count == 1) {
					elemDef = new CommandElementDefinition(parDefs.First());
				} else {
					elemDef = new CommandElementDefinition(parDefs.First(), parDefs.Skip(1));
				}

				elemDefs.Add(elemDef);
			}

			return elemDefs;
		}

		/// <summary>
		/// Creates a parameter dictionary using the specified base command, populating the specified element list.
		/// </summary>
		/// <param name="superCmdDef"></param>
		/// <param name="elemList"></param>
		/// <returns></returns>
		private static Dictionary<string, IList<ParameterDefinition>> CreateParameterDictionary(CommandDefinition superCmdDef, List<string> elemList) {
			Dictionary<string, IList<ParameterDefinition>> parDict = new Dictionary<string, IList<ParameterDefinition>>(StringComparer.InvariantCultureIgnoreCase);

			if (superCmdDef != null) {
				// Add the super elements to the list and dictionary.
				foreach (CommandElementDefinition superElemDef in superCmdDef.Elements) {
					elemList.Add(superElemDef.Name);

					List<ParameterDefinition> dataPars = new List<ParameterDefinition>();
					if (superElemDef.HasMultipleDataEntries) {
						dataPars.Add(superElemDef.LengthParameterDefinition.Clone());
						dataPars.AddRange(superElemDef.DataParameterDefinitions.Select(pd => pd.Clone()));
					} else {
						dataPars.Add(superElemDef.MainParameterDefinition.Clone());
					}
					parDict[superElemDef.Name] = dataPars;
				}
			}

			return parDict;
		}

		/// <summary>
		/// Finds a base command in the specified set of command definitions with the specified name.
		/// </summary>
		/// <param name="defs">The previously defined command definitions.</param>
		/// <param name="isExt">true if the base command is to be extended; otherwise, false.</param>
		/// <param name="name">The name of the command to find.</param>
		/// <returns>The base command, or null if there is no base command.</returns>
		private static CommandDefinition FindPreviousCommand(IEnumerable<CommandDefinition> defs, bool isExt, string name) {
			CommandDefinition super = defs.FirstOrDefault(cd => cd.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
			if (isExt && super == null) {
				throw new InvalidDataException("No command \"" + name + "\" found to extend.");
			}
			if (!isExt && super != null) {
				throw new InvalidDataException("Command with name \"" + name + "\" already defined.");
			}

			return super;
		}

		/// <summary>
		/// Parses the command end type from the specified value.
		/// </summary>
		/// <param name="value">The value to parse.</param>
		/// <returns>The parsed end type.</returns>
		private static EndType ParseEndType(string value) {
			EndType endType = EndType.Default;
			switch (value.ToUpperInvariant()) {
				case "ALWAYS":
					endType = EndType.Always;
					break;
				case "NEVER":
					endType = EndType.Never;
					break;
				case "DEFAULT":
				default:
					endType = EndType.Default;
					break;
			}
			return endType;
		}
	}
}
