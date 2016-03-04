using LibTextPet.General;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace LibTextPet.Msg {
	/// <summary>
	/// A collection of command data entries containing command parameters.
	/// </summary>
	public class DataEntryCollection : Collection<ReadOnlyNamedCollection<Parameter>> {
		/// <summary>
		/// Gets the parameter definitions for each data entry.
		/// </summary>
		public ReadOnlyNamedCollection<ParameterDefinition> Definitions { get; private set; }

		/// <summary>
		/// Gets a boolean that indicates whether this data entry collection is locked for editing.
		/// </summary>
		public bool Locked { get; private set; }

		/// <summary>
		/// Creates a new data entry collection with no definitions with the given lock status.
		/// </summary>
		/// <param name="locked">Whether the data entry collection is locked for editing.</param>
		public DataEntryCollection(bool locked)
			: this(new ParameterDefinition[0], locked) { }

		/// <summary>
		/// Creates a new data entry collection with the given definitions for a data parameter entry.
		/// </summary>
		/// <param name="definitions">The parameter definitions for a single data parameter entry.</param>
		public DataEntryCollection(IEnumerable<ParameterDefinition> definitions)
			: this(definitions, false) { }

		/// <summary>
		/// Creates a new data entry collection with the given definitions for a data parameter entry and the given lock status.
		/// </summary>
		/// <param name="definitions">The parameter definitions for a single data parameter entry.</param>
		/// <param name="locked">Whether the data entry collection is locked for editing.</param>
		public DataEntryCollection(IEnumerable<ParameterDefinition> definitions, bool locked) {
			if (definitions == null)
				throw new ArgumentNullException(nameof(definitions), "The parameter definitions cannot be null.");

			this.Definitions = new ReadOnlyNamedCollection<ParameterDefinition>(definitions);
			this.Locked = locked;
		}

		/// <summary>
		/// Gets the amount of parameters for a single data parameter entry.
		/// </summary>
		public int ParameterCount {
			get {
				return this.Definitions.Count;
			}
		}

		/// <summary>
		/// Creates a default data entry for this data entry collection.
		/// </summary>
		/// <returns>The default data entry.</returns>
		public ReadOnlyNamedCollection<Parameter> CreateDefaultEntry() {
			// Create new data parameters.
			Parameter[] pars = new Parameter[this.Definitions.Count];
			for (int i = 0; i < this.Definitions.Count; i++) {
				pars[i] = new Parameter(this.Definitions[i]);
			}
			// Create new data entry from data parameters.
			return new ReadOnlyNamedCollection<Parameter>(pars);
		}

		protected override void InsertItem(int index, ReadOnlyNamedCollection<Parameter> item) {
			Validate(item, nameof(item));
			base.InsertItem(index, item);
		}

		protected override void SetItem(int index, ReadOnlyNamedCollection<Parameter> item) {
			Validate(item, nameof(item));
			base.SetItem(index, item);
		}

		private void Validate(ReadOnlyNamedCollection<Parameter> entry, string paramName) {
			if (entry == null)
				throw new ArgumentNullException(nameof(entry), "The data entry cannot be null.");

			if (paramName == null) {
				paramName = nameof(entry);
			}

			// Check number of data parameters.
			if (entry.Count != this.ParameterCount)
				throw new ArgumentException("The data entry does not have the expected number of data parameters.", paramName);

			// Check if all data parameters exist in the data entry.
			bool[] exists = new bool[this.ParameterCount];
			foreach (Parameter par in entry) {
				for (int i = 0; i < this.ParameterCount; i++) {
					if (par.Definition.Equals(this.Definitions[i])) {
						exists[i] = true;
					}
				}
			}
			for (int i = 0; i < exists.Length; i++) {
				if (!exists[i])
					throw new ArgumentException("Data entry is missing parameter \"" + this.Definitions[i].Name + "\".", paramName);
			}
		}
	}
}
