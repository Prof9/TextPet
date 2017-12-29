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
		/// <param name="jumpContinueValues">The values for jump targets that lead to continuing the current script.</param>
		/// <param name="extension">true if the command definition is an extension; otherwise, false.</param>
		/// <returns>The resulting command definition.</returns>
		private static CommandDefinition LoadCommandDefinition(IEnumerator<IniSection> enumerator, IEnumerable<CommandDefinition> defs, IList<long> jumpContinueValues, bool extension) {
			if (enumerator == null)
				throw new ArgumentNullException(nameof(enumerator), "The enumerator cannot be null.");
			if (defs == null)
				throw new ArgumentNullException(nameof(defs), "The command definitions cannot be null.");

			if (extension) {
				ValidateCurrentSectionNameAny(enumerator, "EXTENSION");
			} else {
				ValidateCurrentSectionNameAny(enumerator, "COMMAND");
			}
			ValidateCurrentSectionPropertiesAll(enumerator, "NAME", "MASK", "BASE");

			IniSection section = enumerator.Current;

			// Set required properties.
			string name = section.PropertyAsString("NAME");
			string baseString = section.PropertyAsString("BASE", "");
			string maskString = section.PropertyAsString("MASK", "");

			// Parse base and mask.
			byte[] baseSeq = NumberParser.ParseHexString(baseString).ToArray();
			byte[] maskSeq = NumberParser.ParseHexString(maskString).ToArray();
			if (baseSeq.Length < maskSeq.Length) {
				Array.Resize(ref baseSeq, maskSeq.Length);
			}

			// Set optional properties to their defaults.
			string desc = section.PropertyAsString("DESC");
			long dadd = section.PropertyAsInt64("DADD", 0);
			long plen = section.PropertyAsInt64("PLEN", 0);
			EndType ends = ParseEndType(section.PropertyAsString("ENDS", "DEFAULT"));
			bool prnt = section.PropertyAsBoolean("PRNT", false);
			string mugs = section.PropertyAsString("MUGS", null);
			long rwnd = section.PropertyAsInt64("RWND", 0);

			List<ParameterDefinition> pars = new List<ParameterDefinition>();
			ParameterDefinition lengthPar = null;
			List<ParameterDefinition> dataPars = new List<ParameterDefinition>();

			// Find the base command.
			CommandDefinition super = defs.FirstOrDefault(cd => cd.Name.ToUpperInvariant() == name.ToUpperInvariant());

			// Load extended properties (for extensions).
			if (extension) {
				if (super == null)
					throw new KeyNotFoundException("Unknown base command " + name + ".");

				// Copy properties.
				desc = super.Description;
				ends = super.EndType;
				dadd = super.DataCountOffset;
				
				// Clone all parameter definitions.
				pars = CloneParameters(super.Elements);
				dataPars = CloneParameters(super.DataParameters);
				if (super.LengthParameter != null) {
					lengthPar = new ParameterDefinition(super.LengthParameter);
				}
			} else if (super != null) {
				// Duplicate command.
				string baseStr = BitConverter.ToString(baseSeq).Replace('-', ' ');
				string superBaseStr = BitConverter.ToString(super.Base.ToArray()).Replace('-', ' ');

				throw new InvalidDataException("Command with base " + baseStr + " attempts to use name " + name + ", "
					+ "but this name is already in use by command with base " + superBaseStr + ".");
			}

			// Load all parameters.
			bool skip = false;
			bool stop = false;
			while (AdvanceEnumerator(enumerator, skip, stop)) {
				ParameterDefinition par = null;
				bool dataPar = false;

				switch (enumerator.Current.Name.ToUpperInvariant()) {
					case "PARAMETER":
						par = LoadParameterDefinition(enumerator, pars, jumpContinueValues, false, extension);
						break;
					case "LENGTH":
						if (extension)
							throw new InvalidDataException("Extension command " + name + " cannot change length parameter.");
						if (lengthPar != null)
							throw new InvalidDataException("Length parameter for " + name + " command is already defined.");

						lengthPar = LoadParameterDefinition(enumerator, new ParameterDefinition[0], jumpContinueValues, true, false);
						break;
					case "DATA":
						par = LoadParameterDefinition(enumerator, dataPars, jumpContinueValues, false, extension);
						dataPar = true;
						break;
					default:
						stop = true;
						break;
				}
				
				// Add or overwrite the (data) parameter.
				if (par != null) {
					List<ParameterDefinition> searchPars = dataPar ? dataPars : pars;

					// If this command is an extension, do not add any parameters.
					if (extension) {
						// Overwrite the (data) parameter.
						int index = searchPars.FindIndex(pd => pd.Name.ToUpperInvariant() == par.Name.ToUpperInvariant());
						if (index == -1)
							throw new KeyNotFoundException("Command " + name + " does not have a parameter " + par.Name + " to extend.");

						searchPars[index] = par;
					} else {
						// Add the (data) parameter.
						searchPars.Add(par);
					}
				}
			}

			return new CommandDefinition(name, desc, baseSeq, maskSeq, ends, prnt, mugs, dadd, plen, rwnd, pars, lengthPar, dataPars);
		}

		/// <summary>
		/// Loads a parameter definition from the specified INI section enumerator.
		/// </summary>
		/// <param name="enumerator">The enumerator to read from.</param>
		/// <param name="defs">The previously loaded parameter definitions.</param>
		/// /// <param name="jumpContinueValues">The values for jump targets that lead to continuing the current script.</param>
		/// <param name="length">true if the parameter definition is a length parameter; otherwise, false.</param>
		/// <param name="extension">true if the parameter definition is an extension; otherwise, false.</param>
		/// <returns>The resulting parameter definition.</returns>
		private static ParameterDefinition LoadParameterDefinition(IEnumerator<IniSection> enumerator, IEnumerable<ParameterDefinition> defs, IList<long> jumpContinueValues, bool length, bool extension) {
			if (enumerator == null)
				throw new ArgumentNullException(nameof(enumerator), "The enumerator cannot be null.");
			if (defs == null)
				throw new ArgumentNullException(nameof(defs), "The parameter definitions cannot be null.");
			if (length && extension)
				throw new ArgumentException("Length parameters cannot be extended.");

			if (extension) {
				ValidateCurrentSectionNameAny(enumerator, "Parameter", "Data");
				ValidateCurrentSectionPropertiesAll(enumerator, "name");
			} else if (length) {
				ValidateCurrentSectionNameAny(enumerator, "Length");
				ValidateCurrentSectionPropertiesAll(enumerator, "offs", "bits");
			} else {
				ValidateCurrentSectionNameAny(enumerator, "Parameter", "Data");
				ValidateCurrentSectionPropertiesAll(enumerator, "name", "offs", "bits");
			}

			IniSection section = enumerator.Current;

			// Set required properties.
			string name = section.PropertyAsString("name");
			if (length && name == null) {
				name = "length";
			}

			// Find the base command.
			ParameterDefinition super = defs.FirstOrDefault(pd => pd.Name.ToUpperInvariant() == name.ToUpperInvariant());
			if (extension && super == null)
				throw new KeyNotFoundException("Unknown base parameter " + name + ".");

			// Load required properties.
			int offset;
			int shift;
			int bits;
			if (extension) {
				// Copy properties.
				offset = super.Offset;
				shift = super.Shift;
				bits = super.Bits;
			} else {
				string[] offs = section.PropertyAsString("offs").Split('.', ',');
				bits = (int)section.PropertyAsInt64("bits");

				// Parse offset.
				if (offs.Length < 1 || offs.Length > 2)
					throw new InvalidDataException("Invalid offset format.");
				offset = NumberParser.ParseInt32(offs[0]);
				shift = offs.Length == 2 ? NumberParser.ParseInt32(offs[1]) : 0;
			}

			// Set optional properties.
			string desc = section.PropertyAsString("desc", null);
			string type = section.PropertyAsString("type", "DEC").ToUpperInvariant();
			int dataGroup = (int)section.PropertyAsInt64("dgrp", 0);
			long extBase = 0;
			string valueEncoding = section.PropertyAsString("valn", null);

			if (length && valueEncoding != null) {
				throw new InvalidDataException("Length parameter does not support value encoding.");
			}

			// Load properties from base.
			if (extension) {
				desc = super.Description;
				extBase = section.PropertyAsInt64("extb", 0);
			}

			bool isJump = type == "JUMP";
			ParameterDefinition parDef = new ParameterDefinition(name, desc, offset, shift, bits, isJump, extBase, dataGroup, valueEncoding);

			// Set jump continue values.
			if (isJump) {
				parDef.JumpContinueValues = jumpContinueValues;
			}

			return parDef;
		}

		/// <summary>
		/// Parses the command end type from the specified value.
		/// </summary>
		/// <param name="value">The value to parse.</param>
		/// <returns>The parsed end type.</returns>
		private static EndType ParseEndType(string value) {
			if (value == null)
				throw new ArgumentNullException(nameof(value), "The value cannot be null.");

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

		/// <summary>
		/// Clones the specified parameters.
		/// </summary>
		/// <param name="pars">The parameters to clone.</param>
		/// <returns>The cloned parameters.</returns>
		private static List<ParameterDefinition> CloneParameters(IEnumerable<ParameterDefinition> pars) {
			if (pars == null)
				throw new ArgumentNullException(nameof(pars), "The parameters cannot be null.");

			List<ParameterDefinition> cloned = new List<ParameterDefinition>();
			foreach (ParameterDefinition par in pars) {
				cloned.Add(new ParameterDefinition(par));
			}
			return cloned;
		}
	}
}
