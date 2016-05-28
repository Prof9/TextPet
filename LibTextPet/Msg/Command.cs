﻿using System;
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
		/// Gets a description of this script command.
		/// </summary>
		public string Description => this.Definition.Description;

		/// <summary>
		/// Gets a boolean that indicates whether this script command contains data parameters.
		/// </summary>
		public bool HasData => this.Definition.HasData;

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

		/// <summary>
		/// Calculates the offsets, in bytes, for each data group in the data parameters.
		/// </summary>
		/// <returns>The offsets per data group.</returns>
		public ReadOnlyCollection<int> CalculateDataGroupOffsets() {
			int[] dataGroupOffsets = new int[this.Definition.DataEntryLengths.Count];

			int offset = 0;
			for (int i = 0; i < dataGroupOffsets.Length; i++) {
				dataGroupOffsets[i] = offset;
				// Calculate the next offset.
				offset += this.Definition.DataEntryLengths[i] * this.Data.Count;
			}

			return new ReadOnlyCollection<int>(dataGroupOffsets);
		}

		public override string ToString() => this.Name;

		public override bool Equals(object obj) {
			if (obj == null || GetType() != obj.GetType())
				return false;

			Command cmd = (Command)obj;

			return this.Equals(cmd);
		}

		public bool Equals(IScriptElement other) {
			Command otherCmd = other as Command;
			if (otherCmd == null) {
				return false;
			}

			// This is a reference check, but should be fine as all command definitions must be unique.
			if (this.Definition != otherCmd.Definition) {
				return false;
			}

			// Check normal parameter;
			if (!Enumerable.SequenceEqual(this.Parameters, otherCmd.Parameters)) {
				return false;
			}

			if (this.Data.Count != otherCmd.Data.Count) {
				return false;
			}

			// Check data parameters.
			for (int i = 0; i < this.Data.Count; i++) {
				if (!Enumerable.SequenceEqual(this.Data[i], otherCmd.Data[i])) {
					return false;
				}
			}

			// Definition and parameter values match.
			return true;
		}

		public override int GetHashCode() {
			int hash = this.Definition.GetHashCode();

			foreach (Parameter par in this.Parameters) {
				hash ^= par.GetHashCode();
			}

			foreach (IEnumerable<Parameter> dataEntry in this.Data) {
				foreach (Parameter dataPar in dataEntry) {
					hash ^= dataPar.GetHashCode();
				}
			}

			return hash;
		}
	}
}
