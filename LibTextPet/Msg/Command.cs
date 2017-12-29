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

			List<CommandElement> elems = new List<CommandElement>(definition.Elements.Count);
			foreach (CommandElementDefinition elemDef in definition.Elements) {
				elems.Add(new CommandElement(elemDef));
			}
			this.Elements = new ReadOnlyNamedCollection<CommandElement>(elems);
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
