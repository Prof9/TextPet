using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using LibTextPet.General;

namespace LibTextPet.Msg {
	/// <summary>
	/// A definition of a script command.
	/// </summary>
	public class CommandDefinition : IDefinition, ICloneable, INameable {
		/// <summary>
		/// Gets the name of the script command.
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// Gets a description of the script command.
		/// </summary>
		public string Description { get; }

		/// <summary>
		/// Gets the base bytes of the script command.
		/// </summary>
		public ReadOnlyCollection<byte> Base { get; }

		/// <summary>
		/// Gets the base mask of the script command.
		/// </summary>
		public ReadOnlyCollection<byte> Mask { get; }

		/// <summary>
		/// Gets the amount of bytes after which this command should be forcibly chosen if it is still a potential candidate.
		/// </summary>
		public long PriorityLength { get; }

		/// <summary>
		/// Gets the minimum length of the script command in bytes.
		/// </summary>
		public int MinimumLength {
			get {
				return this.Base.Count;
			}
		}

		/// <summary>
		/// Gets an enum that indicates how this script command ends script execution.
		/// </summary>
		public EndType EndType { get; }

		/// <summary>
		/// Gets a boolean that indicates whether this script command should be printed in text.
		/// </summary>
		public bool Prints { get; }

		/// <summary>
		/// Gets the name of the mugshot parameter if this command changes the active mugshot, or null if this command does not change the active mugshot.
		/// </summary>
		public string MugshotParameterName { get; private set; }

		/// <summary>
		/// Gets a boolean that indicates whether this command hides the active mugshot.
		/// </summary>
		public bool HidesMugshot { get; private set; }

		/// <summary>
		/// Gets the definitions of the parameters of the script command.
		/// </summary>
		public ReadOnlyNamedCollection<CommandElementDefinition> Elements { get; }

		/// <summary>
		/// Gets the parent command definition of this command definition, if this command definition is an alternative to it.
		/// </summary>
		public CommandDefinition Parent { get; private set; }

		private List<CommandDefinition> alternatives;
		/// <summary>
		/// Gets a list of alternative command definitions for this command definition, which includes this command definition.
		/// </summary>
		public ReadOnlyCollection<CommandDefinition> Alternatives => new ReadOnlyCollection<CommandDefinition>(alternatives);

		/// <summary>
		/// Gets a one-dimensional list of all (sub-)parameter definitions in this script command definition.
		/// </summary>
		/// <returns>The list of parameter definitions.</returns>
		public IEnumerable<ParameterDefinition> FlattenParameters() {
			foreach (CommandElementDefinition elemDef in this.Elements) {
				foreach (ParameterDefinition parDef in elemDef.DataParameterDefinitions) {
					yield return parDef;
				}
			}
		}

		/// <summary>
		/// Gets the number of bytes this command should rewind after being read.
		/// </summary>
		public long RewindCount { get; }

		/// <summary>
		/// Constructs a script command definition with the given name, description, base, mask and parameter definitions.
		/// </summary>
		/// <param name="name">The name of the script command.</param>
		/// <param name="description">The description of the script command, or null.</param>
		/// <param name="baseSequence">The base bytes of the script command.</param>
		/// <param name="mask">The base mask of the script command.</param>
		/// <param name="endType">An enum that indicates when the script command ends script execution.</param>
		/// <param name="prints">A boolean that indicates whether this script command prints to text.</param>
		/// <param name="mugshotName">The name of the mugshot parameter, if this command changes the active mugshot; an empty string, if this command clears the active mugshot; otherwise, null if this command does not affect the active mugshot.</param>
		/// <param name="dataEntryLength">The length in bytes of each data entry, or 0 if the script command has no data.</param>
		/// <param name="dataCountOffset">The value offset for the amount of data entries as dictated by the script command's length parameter.</param>
		/// <param name="priorityLength">The amount of bytes after which this command should be forcibly chosen if it is still a potential candidate, or 0 to never forcibly choose this command.</param>
		/// <param name="pars">The parameter definitions of the script command, or null.</param>
		/// <param name="lengthPar">The parameter definition of the data length, or null.</param>
		/// <param name="dataPars">The parameter definitions of the data parameters, or null.</param>
		public CommandDefinition(string name, string description, byte[] baseSequence, byte[] mask, EndType endType, bool prints, string mugshotName,
			long priorityLength, long rewind, IEnumerable<CommandElementDefinition> elems) {
			if (name == null)
				throw new ArgumentNullException(nameof(name), "The name cannot be null.");
			if (String.IsNullOrWhiteSpace(name))
				throw new ArgumentException("The name cannot be empty.", nameof(name));
			if (baseSequence == null)
				throw new ArgumentNullException(nameof(baseSequence), "The base cannot be null.");
			if (!baseSequence.Any())
				throw new ArgumentException("The base cannot be empty.", nameof(baseSequence));
			if (mask == null)
				throw new ArgumentNullException(nameof(mask), "The mask cannot be null.");
			if (mask.Length != baseSequence.Length)
				throw new ArgumentException("The mask must be the same length as the base.", nameof(mask));
			if (priorityLength < 0)
				throw new ArgumentOutOfRangeException(nameof(priorityLength), priorityLength, "The priority length cannot be negative.");

			this.Name = name.Trim();
			this.Description = description?.Trim() ?? "";
			this.Base = new ReadOnlyCollection<byte>(baseSequence);
			this.Mask = new ReadOnlyCollection<byte>(mask);
			this.EndType = endType;
			this.Prints = prints;
			this.RewindCount = rewind;
			this.PriorityLength = priorityLength;
			this.Elements = new ReadOnlyNamedCollection<CommandElementDefinition>(elems ?? new CommandElementDefinition[0]);

			this.Parent = null;
			this.alternatives = new List<CommandDefinition> {
				this
			};

			// Set the mugshot parameter name.
			SetMugshotName(mugshotName);
		}

		private void SetMugshotName(string mugshotName) {
			if (mugshotName == null) {
				this.MugshotParameterName = null;
				this.HidesMugshot = false;
			} else if (mugshotName.Length == 0) {
				this.MugshotParameterName = null;
				this.HidesMugshot = true;
			} else {
				// Check that the mugshot parameter exists.
				bool found = false;
				foreach (CommandElementDefinition elem in this.Elements) {
					if (elem.HasMultipleDataEntries) {
						continue;
					}
					foreach (ParameterDefinition par in elem.DataParameterDefinitions) {
						if (par.Name == mugshotName) {
							found = true;
							break;
						}
					}
				}
				if (!found) {
					throw new ArgumentException("The mugshot parameter name must exist as a regular parameter.", nameof(mugshotName));
				}

				this.MugshotParameterName = mugshotName;
				this.HidesMugshot = false;
			}
		}

		/// <summary>
		/// Adds the specified command definition as an alternative to this command definition.
		/// </summary>
		/// <param name="altDef">The alternative command definition.</param>
		public void AddAlternative(CommandDefinition altDef) {
			if (altDef == null)
				throw new ArgumentNullException(nameof(altDef), "The alternative command definition cannot be null.");
			if (!altDef.Name.Equals(this.Name, StringComparison.OrdinalIgnoreCase))
				throw new ArgumentException("The alternative command must have the same name as this command.", nameof(altDef));
			if (this.Parent != null)
				throw new InvalidOperationException("Nested alternative command definitions are not allowed.");
			if (altDef.Parent != null)
				throw new ArgumentException("The alternative command is already assigned to a different parent command.", nameof(altDef));

			this.alternatives.Add(altDef);
			altDef.Parent = this;
		}

		/// <summary>
		/// Creates a new script command definition that is a copy of the current instance.
		/// </summary>
		/// <returns>A new script command definition that is a copy of this instance.</returns>
		public CommandDefinition Clone() {
			byte[] newBase = new byte[this.Base.Count];
			for (int i = 0; i < newBase.Length; i++) {
				newBase[i] = this.Base[i];
			}

			byte[] newMask = new byte[this.Mask.Count];
			for (int i = 0; i < newMask.Length; i++) {
				newMask[i] = this.Mask[i];
			}

			return new CommandDefinition(
				String.Copy(this.Name), String.Copy(this.Description),
				newBase, newMask,
				this.EndType, this.Prints, String.Copy(this.MugshotParameterName),
				this.PriorityLength, this.RewindCount,
				this.Elements.Select(elemDef => elemDef.Clone())
			) {
				alternatives = this.alternatives,
			};
		}

		/// <summary>
		/// Creates a new object that is a copy of the current instance.
		/// </summary>
		/// <returns>A new object that is a copy of this instance.</returns>
		object ICloneable.Clone() {
			return this.Clone();
		}

		public override string ToString() {
			return this.Name;
		}
	}
}
