using LibTextPet.General;
using LibTextPet.Msg;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace LibTextPet.IO.TPL {
	/// <summary>
	/// A TextPet Language reader that reads script commands from an input stream.
	/// </summary>
	public class TPLCommandReader : TPLReader<Command>, INameable {
		/// <summary>
		/// Gets or sets the command element definition for the current data block.
		/// </summary>
		private CommandElementDefinition currentDataBlockDefinition;

		/// <summary>
		/// Gets a boolean that indicates whether all parameters are required to be set.
		/// </summary>
		public bool RequireAllParameters { get; private set; }

		/// <summary>
		/// Gets the command database used by this reader.
		/// </summary>
		protected CommandDatabase Database {
			get {
				return this.Databases[0];
			}
		}

		/// <summary>
		/// Gets the name of this TPL command reader (based on the command database it is using).
		/// </summary>
		public string Name => this.Database.Name;

		/// <summary>
		/// Creates a new TextPet Language reader that reads script commands from the specified input stream, using the specified command database.
		/// </summary>
		/// <param name="stream">The stream to read from.</param>
		/// <param name="database">The command database to use.</param>
		public TPLCommandReader(Stream stream, CommandDatabase database)
			: this(stream, database, true) { }

		/// <summary>
		/// Creates a new TextPet Language reader that reads script commands from the specified input stream, using the specified command database.
		/// </summary>
		/// <param name="stream">The stream to read from.</param>
		/// <param name="database">The command database to use.</param>
		/// <param name="strict">Whether an exception should be thrown if a command finishes reading without all parameters having been set.</param>
		public TPLCommandReader(Stream stream, CommandDatabase database, bool strict)
			: base(stream, database) {
			this.RequireAllParameters = strict;
		}

		/// <summary>
		/// Initializes the script command to be read from the token enumerator. As this is not possible without a command definition, null is returned.
		/// </summary>
		/// <returns>null.</returns>
		protected override Command BeginRead() {
			this.currentDataBlockDefinition = null;
			// We can't create the command yet, as we don't know which one it is.
			return null;
		}

		/// <summary>
		/// Processes the specified token for the specified script command.
		/// </summary>
		/// <param name="obj">The script command to modify.</param>
		/// <param name="token">The token to process.</param>
		/// <returns>A result value that indicates whether the token was consumed, and whether to continue reading.</returns>
		protected override ProcessResult ProcessToken(Command obj, Token token, CommandDatabase db) {
			switch (token.Class) {
			case (int)TPLTokenType.Word:
				if (obj == null) {
					// First word must be the command name.
					obj = CreateCommand(token);
					return new ProcessResult(obj, true, true);
				} else {
					// Get the definition for the current command element.
					CommandElementDefinition elemDef = this.currentDataBlockDefinition;
					if (elemDef == null) {
						// If not in a data block, get it from the command definition.
						ReadOnlyNamedCollection<CommandElementDefinition> elemDefs = obj.Definition.Elements;
						if (!elemDefs.Contains(token.Value)) {
							// Unrecognized element; treat as if the command ended.
							// TODO: validate command.
							return ProcessResult.Stop;
						}
						elemDef = elemDefs[token.Value];
					}

					ParameterDefinition parDef;
					if (elemDef.HasMultipleDataEntries && this.currentDataBlockDefinition == null) {
						parDef = elemDef.LengthParameterDefinition;
					} else {
						// Check that this is a valid parameter in the current command element.
						ReadOnlyNamedCollection<ParameterDefinition> defs = elemDef.DataParameterDefinitions;
						if (!defs.Contains(token.Value)) {
							// Unrecognized parameter in the middle of a command element.
							throw new InvalidDataException("Unrecognized parameter \"" + token.Value + "\" for element \"" + elemDef.Name + "\".");
						}
						parDef = defs[token.Value];
					}

					// Read the '='.
					if (ReadToken((int)TPLTokenType.Symbol).Value != "=") {
						throw new InvalidDataException("Unexpected token; '=' expected.");
					}

					CommandElement elem = obj.Elements[elemDef.Name];

					if (elemDef.HasMultipleDataEntries && this.currentDataBlockDefinition == null) {
						// Go into a data block.
						// Check that we're not already in a data block.
						if (this.currentDataBlockDefinition != null) {
							throw new InvalidDataException("Nested data blocks are not supported.");
						}

						// Read the '['.
						if (ReadToken((int)TPLTokenType.Symbol).Value != "[") {
							throw new InvalidDataException("Unexpected token; '[' expected.");
						}

						// Set current data block.
						this.currentDataBlockDefinition = elemDef;
					} else {
						// Create first data entry if none exist.
						if (elemDef.HasMultipleDataEntries && !elem.Any()) {
							elem.Add(elem.CreateDataEntry());
						}

						// Read the value.
						string value;
						if (parDef.IsString) {
							value = ReadString((int)TPLTokenType.String);
							// Unescape \" and \\.
							value = Regex.Replace(value, @"\\([\\""])", "$1")
						} else {
							value = ReadString((int)TPLTokenType.Word);
						}

						// Set the value.
						elem[elem.Count - 1][parDef.Name].SetString(value);
					}

					return ProcessResult.ConsumeAndContinue;
				}
			case (int)TPLTokenType.Symbol:
				switch (token.Value) {
				case "}":
					// Read explicit script terminator.
					// TODO: validate command for strict mode.
					return ProcessResult.Stop;
				case ";":
					// Read explicit command terminator.
					// TODO: validate command for strict mode.
					return ProcessResult.ConsumeAndStop;
				case ",":
					// Are we currently reading data parameters?
					if (this.currentDataBlockDefinition != null) {
						if (obj == null) {
							throw new ArgumentNullException(nameof(obj), "The command to be modified cannot be null when reading data entries.");
						}

						// Add a new data entry.
						CommandElement elem = obj.Elements[this.currentDataBlockDefinition.Name];
						elem.Add(elem.CreateDataEntry());

						return ProcessResult.ConsumeAndContinue;
					} else {
						throw new InvalidDataException("Unexpected token ','.");
					}
				case "[":
					throw new InvalidDataException("Unnamed data blocks are not supported.");
				case "]":
					if (this.currentDataBlockDefinition == null) {
						// Not in a data block so this ']' makes no sense.
						throw new InvalidDataException("Unexpected token ']'.");
					}

					// Read data end.
					// TODO: Validate current data entry for strict mode.
					this.currentDataBlockDefinition = null;
					return ProcessResult.ConsumeAndStop;
				}
				break;
			}
			return ProcessResult.Stop;
		}

		/// <summary>
		/// Initializes the script command to be read from the specified token.
		/// </summary>
		/// <param name="token">The token to read from.</param>
		/// <returns>The script command that was initialized.</returns>
		private Command CreateCommand(Token token) {
			IList<CommandDefinition> defs = this.Database.Find(token.Value);
			if (defs.Count <= 0) {
				throw new ArgumentException("Unrecognized script command \"" + token.Value + "\".", nameof(token));
			}
			return new Command(defs[0]);
		}
	}
}
