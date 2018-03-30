using System;
using System.Collections.Generic;
using System.Text;
using LibTextPet.General;
using System.Collections.ObjectModel;
using System.Linq;

namespace LibTextPet.Msg {
	/// <summary>
	/// A script element that executes a command.
	/// </summary>
	public class Command : IScriptElement, IDefined<CommandDefinition> {
		/// <summary>
		/// Gets the definition of this script command.
		/// </summary>
		public CommandDefinition Definition { get; private set; }

		/// <summary>
		/// Gets the name of this script command.
		/// </summary>
		public string Name => this.Definition.Name;

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
					int jumpPars = 0;
					int extJumps = 0;
					foreach (Parameter par in this.FlattenParameters()) {
						if (par.Definition.IsJump) {
							jumpPars++;
							if (!par.JumpContinuesScript) {
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
		/// Gets the parameters of this script command.
		/// </summary>
		public ReadOnlyNamedCollection<CommandElement> Elements { get; private set; }

		/// <summary>
		/// Constructs a script command from the given definition.
		/// </summary>
		/// <param name="definition">The definition for the script command.</param>
		public Command(CommandDefinition definition) {
			if (definition == null)
				throw new ArgumentNullException(nameof(definition), "The command definition cannot be null.");

			this.Definition = definition;

			// Create an unlocked read-only collection to save copying.
			this.Elements = new ReadOnlyNamedCollection<CommandElement>();
			
			for (int i = 0; i < definition.Elements.Count; i++) {
				this.Elements.Add(new CommandElement(definition.Elements[i]));
			}

			// Lock the elements collection to make it read-only.
			this.Elements.Locked = true;
		}

		/// <summary>
		/// Gets a one-dimensional list of all (sub-)parameters in this script command.
		/// </summary>
		/// <returns>The list of parameters.</returns>
		public IEnumerable<Parameter> FlattenParameters() {
			foreach (CommandElement elem in this.Elements) {
				foreach (Parameter par in elem.FlattenParameters()) {
					yield return par;
				}
			}
		}

		/// <summary>
		/// Validates the specified command; if it is found to be invalid, creates a valid version of the command using a different command definition, if possible.
		/// </summary>
		/// <param name="cmd">The command to validate.</param>
		/// <returns>The valid command (possibly the original command), or null if no valid command could be created.</returns>
		public Command MakeValidCommand() {
			// Check if already suitable.
			if (IsSuitable(this, this.Definition)) {
				return this;
			}

			// Get the root command.
			CommandDefinition rootDef = this.Definition;
			while (rootDef.Parent != null) {
				rootDef = rootDef.Parent;
			}

			// Try each of the alternatives.
			foreach (CommandDefinition canDef in rootDef.Alternatives) {
				if (IsSuitable(this, canDef)) {
					return AugmentCommand(this, canDef);
				}
			}

			return null;
		}

		/// <summary>
		/// Creates a new script command augmented from the specified command with the new specified command definition.
		/// </summary>
		/// <param name="cmd">The script command to augment.</param>
		/// <param name="extDef">The new definition for the command.</param>
		/// <returns>The augmented command.</returns>
		private static Command AugmentCommand(Command cmd, CommandDefinition newDef) {
			// Create a new command.
			Command newCmd = new Command(newDef);

			// Copy over the elements.
			foreach (CommandElement elem in cmd.Elements) {
				CommandElement newElem = newCmd.Elements[elem.Name];

				// Create the data entries (if there are multiple).
				if (newElem.Definition.HasMultipleDataEntries) {
					for (int i = 0; i < elem.Count; i++) {
						newElem.Add(newElem.CreateDataEntry());
					}
				}

				// Add the data entries.
				for (int i = 0; i < elem.Count; i++) {
					ReadOnlyNamedCollection<Parameter> dataEntry = newElem[i];

					// Copy over the parameters.
					foreach (Parameter par in elem[i]) {
						if (par.IsString) {
							dataEntry[par.Name].StringValue = par.StringValue;
						} else {
							dataEntry[par.Name].NumberValue = par.NumberValue;
						}
					}
				}
			}

			return newCmd;
		}

		/// <summary>
		/// Checks whether the specified command definition is suitable for the current state of the specified command.
		/// </summary>
		/// <param name="command">The command to check against.</param>
		/// <param name="newDefinition">The command definition to check for suitability.</param>
		/// <returns>true if the command definition is suitable; otherwise, false.</returns>
		private static bool IsSuitable(Command cmd, CommandDefinition newCmdDef) {
			// Loop through all elements.
			foreach (CommandElement elem in cmd.Elements) {
				// Check if the new definition contains all elements.
				if (!newCmdDef.Elements.Contains(elem.Name)) {
					return false;
				}
				CommandElementDefinition newElemDef = newCmdDef.Elements[elem.Name];

				// Loop through all data entries.
				foreach (ReadOnlyNamedCollection<Parameter> entry in elem) {
					foreach (Parameter par in entry) {
						// Check if the new definition contains all parameters.
						if (!newElemDef.DataParameterDefinitions.Contains(par.Name)) {
							return false;
						}
						ParameterDefinition newParDef = newElemDef.DataParameterDefinitions[par.Name];

						// Check if parameter is in range.
						if (!newParDef.IsString && !newParDef.InRange(par.NumberValue)) {
							return false;
						}
					}
				}
			}

			// All checks passed.
			return true;
		}

		public override string ToString() => this.Name;

		public override bool Equals(object obj) {
			return obj is Command cmd
				&& this.Equals(cmd);
		}

		public bool Equals(IScriptElement other) {
			Command otherCmd = other as Command;
			if (otherCmd == null) {
				return false;
			}

			if (this.Definition != otherCmd.Definition) {
				return false;
			}

			// Check command elements;
			if (!Enumerable.SequenceEqual(this.Elements, otherCmd.Elements)) {
				return false;
			}

			// Definition and parameter values match.
			return true;
		}

		public override int GetHashCode() {
			int hash = this.Definition.GetHashCode();

			// Really shoddy hash.
			foreach (Parameter par in this.FlattenParameters()) {
				hash ^= par.GetHashCode();
			}

			return hash;
		}
	}
}
