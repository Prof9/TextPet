using LibTextPet.General;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LibTextPet.Msg {
	/// <summary>
	/// Defines a script command element, which is a collection of data entries containing one or more parameters.
	/// </summary>
	public class CommandElementDefinition : IDefinition, INameable, ICloneable {
		/// <summary>
		/// Gets the name of this command element.
		/// </summary>
		public string Name
			=> this.MainParameterDefinition.Name;

		/// <summary>
		/// Gets the description of this command element.
		/// </summary>
		public string Description
			=> this.MainParameterDefinition.Description;

		/// <summary>
		/// Gets the main parameter definition for this command element. If this command element has multiple parameter data entries, this is the definition of the length parameter.
		/// </summary>
		public ParameterDefinition MainParameterDefinition
			=> this.HasMultipleDataEntries ? this.LengthParameterDefinition : this.DataParameterDefinitions[0];

		/// <summary>
		/// Gets the amount of parameters per data entry in this command element.
		/// </summary>
		public int DataEntrySize
			=> this.DataParameterDefinitions.Count;


		/// <summary>
		/// Gets the parameter definition for the length parameter of this command element.
		/// </summary>
		public ParameterDefinition LengthParameterDefinition { get; }

		/// <summary>
		/// Gets the definitions for the parameters in each data entry of this command element.
		/// </summary>
		public ReadOnlyNamedCollection<ParameterDefinition> DataParameterDefinitions { get; }

		/// <summary>
		/// Gets a boolean that indicates whether this command element has multiple parameters.
		/// </summary>
		public bool HasMultipleDataEntries => this.LengthParameterDefinition != null;

		/// <summary>
		/// Enumerates the data groups in this command element.
		/// </summary>
		public IEnumerable<IEnumerable<ParameterDefinition>> DataGroups {
			get {
				int offset = 0;
				if (this.HasMultipleDataEntries) {
					foreach (int size in this.LengthParameterDefinition.DataGroupSizes) {
						// Return the current data group.
						int count = Math.Min(size, this.DataParameterDefinitions.Count - offset);
						yield return this.DataParameterDefinitions.Skip(offset).Take(count);
						offset += count;
					}
				}
				// Return any remaining data parameters.
				if (offset < this.DataParameterDefinitions.Count) {
					yield return this.DataParameterDefinitions.Skip(offset);
				}
			}
		}

		
		/// <summary>
		/// Creates a new multi parameter with the given parameter definitions.
		/// </summary>
		/// <param name="lengthDef">The parameter definition for the length parameter.</param>
		/// <param name="entryDefs">The parameter definitions for a single data parameter entry.</param>
		public CommandElementDefinition(ParameterDefinition parDef) {
			if (parDef == null)
				throw new ArgumentNullException(nameof(parDef), "The parameter definition cannot be null.");

			this.LengthParameterDefinition = null;
			this.DataParameterDefinitions = new ReadOnlyNamedCollection<ParameterDefinition>(
				new ParameterDefinition[] { parDef }
			);
		}

		/// <summary>
		/// Creates a new command element with the specified definitions for the length parameter and data parameters.
		/// </summary>
		/// <param name="lengthParDef">The length parameter definition.</param>
		/// <param name="dataParDefs">The data parameter definitions.</param>
		public CommandElementDefinition(ParameterDefinition lengthParDef, IEnumerable<ParameterDefinition> dataParDefs) {
			if (lengthParDef == null)
				throw new ArgumentNullException(nameof(lengthParDef), "The length parameter definition cannot be null.");
			if (dataParDefs == null)
				throw new ArgumentNullException(nameof(dataParDefs), "The data parameter definitions cannot be null.");
			if (!dataParDefs.Any())
				throw new ArgumentException("The data parameter definitions cannot be empty.", nameof(dataParDefs));

			this.LengthParameterDefinition = lengthParDef;
			this.DataParameterDefinitions = new ReadOnlyNamedCollection<ParameterDefinition>(dataParDefs);
		}


		/// <summary>
		/// Creates a new command element definition that is a deep clone of the current instance.
		/// </summary>
		/// <returns>A new command element definition that is a deep clone of this instance.</returns>
		public CommandElementDefinition Clone() {
			if (this.HasMultipleDataEntries) {
				return new CommandElementDefinition(
					this.LengthParameterDefinition.Clone(),
					this.DataParameterDefinitions.Select(def => def.Clone())
				);
			} else {
				return new CommandElementDefinition(
					this.MainParameterDefinition.Clone()
				);
			}
		}

		/// <summary>
		/// Creates a new object that is a deep clone of the current instance.
		/// </summary>
		/// <returns>A new object that is a deep clone of this instance.</returns>
		object ICloneable.Clone() {
			return this.Clone();
		}
	}
}
