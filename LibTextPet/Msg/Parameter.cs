using LibTextPet.General;
using LibTextPet.Text;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace LibTextPet.Msg {
	/// <summary>
	/// A parameter of a script command.
	/// </summary>
	public class Parameter : IDefined<ParameterDefinition>, INameable, IEquatable<Parameter> {
		/// <summary>
		/// Gets the definition for this parameter.
		/// </summary>
		public ParameterDefinition Definition { get; }

		/// <summary>
		/// Gets the name of this parameter.
		/// </summary>
		public string Name => this.Definition.Name;

		private long numberValue;
		/// <summary>
		/// Gets or sets this parameter's number value. If this parameter is not a number, an exception is thrown.
		/// </summary>
		public long NumberValue {
			get {
				if (!this.IsNumber)
					throw new InvalidOperationException("This parameter is not a number.");

				return this.numberValue;
			}
			set {
				if (!this.IsNumber)
					throw new InvalidOperationException("This parameter is not a number.");

				this.numberValue = value;
			}
		}

		private string stringValue;
		/// <summary>
		/// Gets or sets this parameter's string value. If this parameter is not a string, an exception is thrown.
		/// </summary>
		public string StringValue {
			get {
				if (!this.IsString)
					throw new InvalidOperationException("This parameter is not a string.");
				
				return this.stringValue;
			}
			set {
				if (!this.IsString)
					throw new InvalidOperationException("This parameter is not a string.");
				if (value == null)
					throw new ArgumentNullException(nameof(value), "The string value cannot be null.");

				this.stringValue = value;
			}
		}

		/// <summary>
		/// Gets a boolean that indicates whether this parameter is a jump target.
		/// </summary>
		public bool IsJump
			=> this.Definition.IsJump;
		/// <summary>
		/// Gets a boolean that indicates whether this parameter continues the current script, if selected as jump target.
		/// </summary>
		public bool JumpContinuesScript
			=> this.Definition.JumpContinueValues.Contains(this.NumberValue);

		/// <summary>
		/// Gets a boolean that indicates whether this parameter is a string.
		/// </summary>
		public bool IsString
			=> this.Definition.IsString;
		/// <summary>
		/// Gets a boolean that indicates whether this parameter is a number.
		/// </summary>
		public bool IsNumber
			=> !this.IsString;

		/// <summary>
		/// Gets a boolean that indicates whether this parameter is in range.
		/// </summary>
		public bool IsInRange
			=> this.Definition.InRange(this.NumberValue);

		/// <summary>
		/// Constructs a command parameter from the specified definition.
		/// </summary>
		/// <param name="definition">The parameter definition to use.</param>
		public Parameter(ParameterDefinition definition) {
			if (definition == null)
				throw new ArgumentNullException(nameof(definition), "The parameter definition cannot be null.");

			this.Definition = definition;
		}

		/// <summary>
		/// Converts this parameter to a string representation.
		/// This method can be used regardless of the parameter's underlying type.
		/// </summary>
		/// <returns>A string representation of this parameter.</returns>
		public override string ToString() {
			if (this.IsString) {
				return this.StringValue;
			}

			string result = null;

			// Try encoding using value encoding.
			if (this.Definition.ValueEncoding != null) {
				byte[] bytes = BitConverter.GetBytes(this.NumberValue);

				// Keep adding more bytes until we have something we can encode
				for (int l = 1; l <= (this.Definition.Bits + 7) / 8; l++) {
					this.Definition.ValueEncoding.ResetFallbackCount();

					string encoded = this.Definition.ValueEncoding.GetString(bytes, 0, l);

					// Did we encode it correctly?
					if (this.Definition.ValueEncoding.FallbackCount == 0) {
						result = encoded;
						break;
					}
				}
			}
			
			// Encode as normal number.
			if (result == null) {
				result = this.NumberValue.ToString(CultureInfo.InvariantCulture);
			}

			return result;
		}
		/// <summary>
		/// Sets the value of this parameter to a value parsed from the specified string.
		/// This method can be used regardless of the parameter's underlying type.
		/// </summary>
		/// <param name="value">The string to parse.</param>
		public void SetString(string value) {
			if (value == null)
				throw new ArgumentNullException(nameof(value), "The string cannot be null.");

			if (this.IsString) {
				this.StringValue = value;
			} else {
				// Parse string.
				this.NumberValue = this.Definition.ParseString(value);
			}
		}

		/// <summary>
		/// Returns the hash code for this command parameter's value.
		/// </summary>
		/// <returns>The hash code for this command parameter.</returns>
		public override int GetHashCode() {
			if (this.IsNumber) {
				return this.numberValue.GetHashCode();
			}
			if (this.IsString) {
				return this.stringValue.GetHashCode();
			}
			return 0;
		}

		/// <summary>
		/// Indicates whether this instance and a given object are equal.
		/// </summary>
		/// <param name="obj">Another object to compare to.</param>
		/// <returns>true if obj and this instance are the same type and represent the same value; otherwise, false.</returns>
		public override bool Equals(object obj) {
			if (obj == null || GetType() != obj.GetType())
				return false;

			Parameter par = (Parameter)obj;

			return this.Equals(par);
		}

		/// <summary>
		/// Indicates whether this instance and a given parameter are equal.
		/// </summary>
		/// <param name="other">Another parameter to compare to.</param>
		/// <returns>true if other and this instance are the same type and represent the same value; otherwise, false.</returns>
		public bool Equals(Parameter other) {
			if (other == null) {
				return false;
			}

			if (this.Definition != other.Definition) {
				return false;
			}
			if (this.IsNumber && other.IsNumber) {
				return this.numberValue == other.numberValue;
			}
			if (this.IsString && other.IsString) {
				return this.stringValue == other.stringValue;
			}
			return false;
		}

		public static bool operator ==(Parameter parameter1, Parameter parameter2) {
			if (ReferenceEquals(parameter1, parameter2)) {
				return true;
			} else if (parameter1 is null || parameter2 is null) {
				return false;
			} else {
				return parameter1.Equals(parameter2);
			}
		}

		public static bool operator !=(Parameter parameter1, Parameter parameter2) {
			return !(parameter1 == parameter2);
		}
	}
}
