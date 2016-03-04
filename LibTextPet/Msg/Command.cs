using System;
using System.Collections.Generic;
using System.Text;
using LibTextPet.General;
using System.Collections.ObjectModel;

namespace LibTextPet.Msg {
	/// <summary>
	/// A script element that executes a command.
	/// </summary>
	public class Command : IScriptElement, IDefined<CommandDefinition>, INameable {
		/// <summary>
		/// Gets the definition of this script command.
		/// </summary>
		public CommandDefinition Definition { get; private set; }

		/// <summary>
		/// Gets the name of this script command.
		/// </summary>
		public string Name {
			get {
				return this.Definition.Name;
			}
		}

		/// <summary>
		/// Gets a description of this script command.
		/// </summary>
		public string Description {
			get {
				return this.Definition.Description;
			}
		}

		/// <summary>
		/// Gets a boolean that indicates whether this script command contains data parameters.
		/// </summary>
		public bool HasData {
			get {
				return this.Definition.HasData;
			}
		}

		/// <summary>
		/// Gets a boolean that indicates whether this script command ends script execution.
		/// </summary>
		public bool EndsScript {
			get {
				switch (this.Definition.EndType) {
					case EndType.Always:
						return true;
					case EndType.Never:
						return false;
					case EndType.Default:
					default:
						// Create a list of every parameter in this command.
						List<Parameter> pars = new List<Parameter>();
						// Add regular parameters.
						pars.AddRange(this.Parameters);
						// Add data parameters.
						foreach (ReadOnlyNamedCollection<Parameter> entry in this.Data) {
							pars.AddRange(entry);
						}

						// Check all parameters for jump parameters.
						int jumpPars = 0, extJumps = 0;
						foreach (Parameter par in pars) {
							if (par.IsJump) {
								jumpPars++;
								if (!par.Definition.JumpContinueValues.Contains(par.ToInt64())) {
									extJumps++;
								}
							}
						}

						// End script execution if the command has jump parameters that all target external scripts.
						return jumpPars > 0 && jumpPars == extJumps;
				}
			}
		}

		/// <summary>
		/// Gets the length of this script command, and all parameters, in bytes.
		/// </summary>
		public long ByteLength {
			get {
				return this.Definition.MinimumLength
					+ this.Data.Count * this.Definition.TotalDataEntryLength;
			}
		}

		public ReadOnlyCollection<int> DataGroupOffsets {
			get {
				int[] dataGroupOffsets = new int[this.Definition.DataEntryLengths.Count];
				
				int offset = 0;
				for (int i = 0; i < dataGroupOffsets.Length; i++) {
					dataGroupOffsets[i] = offset;
					// Calculate the next offset.
					offset += this.Definition.DataEntryLengths[i] * this.Data.Count;
				}
				
				return new ReadOnlyCollection<int>(dataGroupOffsets);
			}
		}

		/// <summary>
		/// Gets the parameters of this script command.
		/// </summary>
		public ReadOnlyNamedCollection<Parameter> Parameters { get; private set; }

		private readonly DataEntryCollection data;
		/// <summary>
		/// Gets the data parameters of this script command.
		/// </summary>
		public DataEntryCollection Data {
			get {				
				return this.data;
			}
		}

		/// <summary>
		/// Constructs a script command from the given definition.
		/// </summary>
		/// <param name="definition">The definition for the script command.</param>
		public Command(CommandDefinition definition) {
			if (definition == null)
				throw new ArgumentNullException(nameof(definition), "The command definition cannot be null.");

			this.Definition = definition;
			
			// Create all empty parameters.
			List<Parameter> pars = new List<Parameter>(definition.Parameters.Count);
			foreach (ParameterDefinition par in definition.Parameters) {
				pars.Add(new Parameter(par));
			}
			this.Parameters = new ReadOnlyNamedCollection<Parameter>(pars);

			// Create length parameter and data parameter array.
			if (definition.HasData) {
				this.data = new DataEntryCollection(definition.DataParameters);
			} else {
				this.data = new DataEntryCollection(true);
			}
		}

		public override string ToString() {
			return this.Name;
		}
	}
}
