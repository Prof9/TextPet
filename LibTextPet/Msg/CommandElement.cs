using LibTextPet.General;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace LibTextPet.Msg {
	/// <summary>
	/// A script command element, being a collection of command data entries containing command parameters.
	/// </summary>
	public class CommandElement : Collection<ReadOnlyNamedCollection<Parameter>>, IDefined<CommandElementDefinition>, INameable, IEquatable<CommandElement> {
		/// <summary>
		/// Gets the name of this command element.
		/// </summary>
		public string Name
			=> this.Definition.Name;

		/// <summary>
		/// Gets the definition of this command element.
		/// </summary>
		public CommandElementDefinition Definition { get; }


		/// <summary>
		/// Creates a new command element with the specified definition.
		/// </summary>
		/// <param name="definition">The command element definition to use.</param>
		public CommandElement(CommandElementDefinition definition) {
			if (definition == null)
				throw new ArgumentNullException("The command element definition cannot be null.");

			this.Definition = definition;

			// Create initial data entry.
			if (!definition.HasMultipleDataEntries) {
				this.Add(this.CreateDataEntry());
			}
		}


		protected override void InsertItem(int index, ReadOnlyNamedCollection<Parameter> item) {
			if (!this.Definition.HasMultipleDataEntries && this.Count >= 1) {
				throw new InvalidOperationException("This command element does not support multiple data entries.");
			}
			base.InsertItem(index, PassThroughValidate(item, nameof(item)));
		}

		protected override void SetItem(int index, ReadOnlyNamedCollection<Parameter> item)
			=> base.SetItem(index, PassThroughValidate(item, nameof(item)));

		private ReadOnlyNamedCollection<Parameter> PassThroughValidate(ReadOnlyNamedCollection<Parameter> entry, string paramName) {
			if (paramName == null) {
				paramName = nameof(entry);
			}

			if (entry == null) {
				throw new ArgumentNullException(paramName, "The data entry cannot be null.");
			}

			// Check number of data parameters.
			if (entry.Count != this.Definition.DataEntrySize)
				throw new ArgumentException("The data entry does not have the expected number of data parameters.", paramName);

			// Check if all data parameters exist in the data entry.
			bool[] exists = new bool[this.Definition.DataEntrySize];
			foreach (Parameter par in entry) {
				for (int i = 0; i < this.Definition.DataEntrySize; i++) {
					if (par.Definition.Equals(this.Definition.DataParameterDefinitions[i])) {
						exists[i] = true;
					}
				}
			}
			for (int i = 0; i < exists.Length; i++) {
				if (!exists[i])
					throw new ArgumentException("Data entry is missing parameter \"" + this.Definition.DataParameterDefinitions[i].Name + "\".", paramName);
			}

			return entry;
		}

		/// <summary>
		/// Creates a default data entry for this multi parameter.
		/// </summary>
		/// <returns>The default data entry.</returns>
		public ReadOnlyNamedCollection<Parameter> CreateDataEntry() {
			// Create new data parameters.
			Parameter[] pars = new Parameter[this.Definition.DataParameterDefinitions.Count];
			for (int i = 0; i < pars.Length; i++) {
				pars[i] = new Parameter(this.Definition.DataParameterDefinitions[i]);
			}

			// Create new data entry from data parameters.
			return new ReadOnlyNamedCollection<Parameter>(pars);
		}

		/// <summary>
		/// Gets all concrete parameters contained in this parameter.
		/// </summary>
		public IEnumerable<Parameter> FlattenParameters() {
			// In each data entry...
			foreach (ReadOnlyNamedCollection<Parameter> dataEntry in this) {
				// Yield each sub parameter.
				foreach (Parameter par in dataEntry) {
					yield return par;
				}
			}
		}



		public override int GetHashCode() {
			int hash = this.Definition.GetHashCode();

			// Really shoddy hash.
			foreach (Parameter par in this.FlattenParameters()) {
				hash ^= par.GetHashCode();
			}

			return hash;
		}

		public bool Equals(CommandElement other) {
			if (other == null) {
				return false;
			}

			// Check if definitions are equal.
			if (this.Definition != other.Definition) {
				return false;
			}

			// Check if data entries are equal.
			if (this.Count != other.Count) {
				return false;
			}
			for (int i = 0; i < this.Count; i++) {
				if (!Enumerable.SequenceEqual(this[i], other[i])) {
					return false;
				}
			}

			// Definition and data entries match.
			return true;
		}
	}
}
