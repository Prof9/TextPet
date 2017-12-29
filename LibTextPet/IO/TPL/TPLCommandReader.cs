using LibTextPet.General;
using LibTextPet.Msg;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LibTextPet.IO.TPL {
	/// <summary>
	/// A TextPet Language reader that reads script commands from an input stream.
	/// </summary>
	public class TPLCommandReader : TPLReader<Command>, INameable {
		/// <summary>
		/// Gets or sets a boolean that indicates whether this reader is currently reading data parameters.
		/// </summary>
		private bool readingDataParameters;

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
			this.readingDataParameters = false;
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
						// Check if the parameter name exists.
						// Are we reading regular parameters or data parameters?
						ReadOnlyNamedCollection<ParameterDefinition> defs = readingDataParameters ? obj.Definition.DataParameters : obj.Definition.Elements;
						if (!defs.Contains(token.Value)) {
							// Unrecognized parameter; treat as if the command ended.
							// TODO: validate command.
							return ProcessResult.Stop;
						}
						
						// Read the '='.
						if (ReadToken((int)TPLTokenType.Symbol).Value != "=") {
							throw new InvalidDataException("Unexpected token; '=' expected.");
						}

						// Read the value.
						string value = ReadString((int)TPLTokenType.Word);

						// Set the value.
						if (this.readingDataParameters) {
							this.Database.SetParameter(obj, token.Value, value, obj.Data.Count - 1);
						} else {
							this.Database.SetParameter(obj, token.Value, value);
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
							if (readingDataParameters) {
								if (obj == null) {
									throw new ArgumentNullException(nameof(obj), "The command to be modified cannot be null when reading data entries.");
								}

								// TODO: Validate current data entry for strict mode.
								// Add a new data entry.
								obj.Data.Add(obj.Data.CreateDataEntry());
								return ProcessResult.ConsumeAndContinue;
							} else {
								// I don't know what this is.
								// Just ignore it. Maybe it'll go away.
								break;
							}
						case "[":
							// Read data start.
							if (this.readingDataParameters) {
								throw new InvalidOperationException("Nested data parameter reading is not supported.");
							}
							this.readingDataParameters = true;

							if (obj == null) {
								throw new ArgumentNullException(nameof(obj), "The command to be modified cannot be null when reading data parameters.");
							}

							// Begin a new data entry.
							obj.Data.Add(obj.Data.CreateDataEntry());

							return ProcessResult.ConsumeAndContinue;
						case "]":
							// Read data end.
							// TODO: Validate current data entry for strict mode.
							this.readingDataParameters = false;
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
