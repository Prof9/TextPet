using System;
using LibTextPet.General;
using System.Text;
using System.Collections.Generic;

namespace LibTextPet.Msg {
	/// <summary>
	/// A definition of a script command parameter.
	/// </summary>
	public sealed class ParameterDefinition : IDefinition, ICloneable, INameable {
		/// <summary>
		/// Gets the name of the command parameter.
		/// </summary>
		public string Name { get; private set; }

		/// <summary>
		/// Gets a description of the command parameter.
		/// </summary>
		public string Description { get; private set; }

		/// <summary>
		/// Gets the byte offset of the command parameter from the start of the command.
		/// </summary>
		public int Offset { get; private set; }

		/// <summary>
		/// Gets the bit sub-offset of the command parameter from the start of the parameter.
		/// </summary>
		public int Shift { get; private set; }

		/// <summary>
		/// Gets the size of the command parameter in bits.
		/// </summary>
		public int Bits { get; private set; }

		/// <summary>
		/// Gets a boolean that indicates whether the command parameter is a jump target.
		/// </summary>
		public bool IsJump { get; private set; }

		/// <summary>
		/// Gets the base extension value for the command parameter.
		/// </summary>
		public long ExtensionBase { get; private set; }

		/// <summary>
		/// Gets the data group for the command parameter.
		/// </summary>
		public int DataGroup { get; private set; }

		/// <summary>
		/// Gets the name of this parameter's value encoding.
		/// </summary>
		public string ValueEncodingName { get; private set; }

		/// <summary>
		/// Gets or sets this parameter's value encoding.
		/// </summary>
		public Encoding ValueEncoding { get; internal set; }
		
		/// <summary>
		/// Gets the values for jump targets that continue the current script.
		/// </summary>
		public ICollection<long> JumpContinueValues { get; internal set; }

		/// <summary>
		/// Constructs a script command parameter definition with the given name, description, byte offset, bit sub-offset and amount of bits.
		/// </summary>
		/// <param name="name">The name of the command parameter.</param>
		/// <param name="description">The description of the command parameter, or null.</param>
		/// <param name="offset">The byte offset of the command parameter from the start of the command.</param>
		/// <param name="shift">The bit sub-offset of the command parameter from the start of the parameter.</param>
		/// <param name="bits">The size of the command parameter in bits.</param>
		/// <param name="isJump">A boolean that indicates whether the command parameter is a jump target.</param>
		/// <param name="extBase">The extension base value of the command parameter.</param>
		public ParameterDefinition(string name, string description, int offset,
			int shift, int bits, bool isJump, long extBase, int dataGroup, string valueEncoding) {
			if (name == null)
				throw new ArgumentNullException(nameof(name), "The name cannot be null.");
			if (name.Length <= 0)
				throw new ArgumentException("The name cannot be empty.", nameof(name));
			if (offset < 0)
				throw new ArgumentOutOfRangeException(nameof(offset), "The offset cannot be less than 0.");
			if (shift < 0)
				throw new ArgumentOutOfRangeException(nameof(shift), shift, "The bit sub-offset cannot be less than 0.");
			if (bits < 1)
				throw new ArgumentOutOfRangeException(nameof(bits), bits, "The bit count cannot be less than 1.");
			if (dataGroup < 0)
				throw new ArgumentOutOfRangeException(nameof(dataGroup), dataGroup, "The data group number cannot be less than 0.");

			this.Name = name;
			this.Description = description ?? "";
			this.Offset = offset;
			this.Shift = shift;
			this.Bits = bits;
			this.IsJump = isJump;
			this.ExtensionBase = extBase;
			this.DataGroup = dataGroup;
			this.ValueEncodingName = valueEncoding;
			this.ValueEncoding = null;
		}

		/// <summary>
		/// Parses the given value for this parameter. A return value indicates whether the conversion succeeded.
		/// </summary>
		/// <param name="number">The value to parse.</param>
		/// <param name="output">When this method returns, contains the 64-bit signed integer value equivalent to the number contained in value, if the conversion succeeded, or zero if the conversion failed.</param>
		/// <returns>true if the value was converted successfully; otherwise, false.</returns>
		public bool TryParseString(string value, out long result) {
			try {
				result = ParseString(value);
				return true;
			} catch (FormatException) {
				result = 0;
				return false;
			}
		}

		/// <summary>
		/// Parses the given value for this parameter.
		/// </summary>
		/// <param name="value">The value to parse.</param>
		/// <returns>The parsed value.</returns>
		public long ParseString(string value) {
			long result;
			bool parsed = false;

			// Try parsing as number.
			parsed = NumberParser.TryParseInt64(value, out result);

			// Try parsing using value encoding.
			if (!parsed && this.ValueEncoding != null) {
				try {
					byte[] parsedBytes = this.ValueEncoding.GetBytes(value);
					byte[] buffer = new byte[8];
					Array.Copy(parsedBytes, buffer, parsedBytes.Length > 8 ? 8 : parsedBytes.Length);
					result = BitConverter.ToInt64(buffer, 0);
					parsed = true;
				} catch (EncoderFallbackException) { }
			}

			// Can't parse this.
			if (!parsed) {
				throw new FormatException("Could not parse \"" + value + "\".");
			}

			return result;
		}

		/// <summary>
		/// Constructs a new script command parameter definition that is identical to the given parameter definition.
		/// </summary>
		/// <param name="definition">The parameter definition to base the new parameter definition off of.</param>
		public ParameterDefinition(ParameterDefinition definition)
			: this(PassThroughNonNull(definition).Name, PassThroughNonNull(definition).Description, PassThroughNonNull(definition).Offset,
			PassThroughNonNull(definition).Shift, PassThroughNonNull(definition).Bits, PassThroughNonNull(definition).IsJump,
			definition.ExtensionBase, definition.DataGroup, definition.ValueEncodingName) {
			this.ValueEncoding = definition.ValueEncoding;
			this.JumpContinueValues = definition.JumpContinueValues;
		}
		private static ParameterDefinition PassThroughNonNull(ParameterDefinition definition) {
			if (definition == null)
				throw new ArgumentNullException(nameof(definition), "The parameter definition cannot be null.");
			return definition;
		}

		/// <summary>
		/// Gets the minimum value this command parameter can take on.
		/// </summary>
		public long Minimum {
			get {
				return this.ExtensionBase;
			}
		}

		/// <summary>
		/// Gets the maximum value this command parameter can take on.
		/// </summary>
		public long Maximum {
			get {
				// 1 needs to be a long here, otherwise this breaks if this.Bits >= 32.
				return this.ExtensionBase + ((long)1 << this.Bits) - 1;
			}
		}

		/// <summary>
		/// Gets the minimum number of bytes required to read this parameter's value.
		/// </summary>
		public int MinimumByteCount {
			get {
				// Calculate the number of bits required.
				int bits = this.Offset * 8 + this.Shift + this.Bits;
				// Calculate the number of bytes required (rounded up).
				return (bits + 7) / 8;
			}
		}

		/// <summary>
		/// Checks whether the given value is within the range allowed by this command parameter.
		/// </summary>
		/// <param name="value">The value to check.</param>
		/// <returns>true if the value is in range; otherwise, false.</returns>
		public bool InRange(long value) {
			return value >= this.Minimum && value <= this.Maximum;
		}

		/// <summary>
		/// Extends this parameter definition with the given extension base value.
		/// </summary>
		/// <param name="extensionBase">The base value to extend by.</param>
		/// <returns>A new parameter definition that is an extension of this parameter definition.</returns>
		public ParameterDefinition Extend(int extensionBase) {
			ParameterDefinition extension = new ParameterDefinition(this);
			extension.ExtensionBase = extensionBase;
			return extension;
		}

		/// <summary>
		/// The fields of this instance that are used to check for equality.
		/// </summary>
		private object[] equalityFields {
			get {
				return new object[] {
					this.Name,
					this.Offset,
					this.Shift,
					this.Bits,
				};
			}
		}

		/// <summary>
		/// Returns the hash code for this parameter definition's value.
		/// </summary>
		/// <returns>A 32-bit signed integer that is the hash code for this parameter definition.</returns>
		public override int GetHashCode() {			
			// Compute FNV-1a hash from the hash codes of all fields.
			int hash = -2128831035;
			unchecked {
				foreach (object obj in this.equalityFields) {
					int h = obj.GetHashCode();
					for (int i = 0; i < 4; i++) {
						hash ^= h & 0xFF;
						hash *= 16777619;
						h >>= 8;
					}
				}
			}
			return hash;
		}

		/// <summary>
		/// Indicates whether this instance and a given object are equal.
		/// </summary>
		/// <param name="obj">Another object to compare to.</param>
		/// <returns>true if obj and this instance are the same type and represent the same value; otherwise, false.</returns>
		public override bool Equals(object obj) {
			if (obj == null)
				return false;
			Parameter par = obj as Parameter;
			if (par != null) {
				return Equals(par);
			} else {
				return false;
			}
		}

		/// <summary>
		/// Indicates whether this parameter definition and the given parameter definition are equal.
		/// </summary>
		/// <param name="definition">Another parameter definition to compare to.</param>
		/// <returns>true if both parameter definitions are the same type and represent the same value; otherwise, false.</returns>
		public bool Equals(ParameterDefinition definition) {
			if (ReferenceEquals(definition, null))
				return false;

			for (int i = 0; i < this.equalityFields.Length; i++) {
				// Need to use Equals here as != is compares as type object, not int
				if (!this.equalityFields[i].Equals(definition.equalityFields[i])) {
					return false;
				}
			}
			return true;
		}

		public static bool operator ==(ParameterDefinition definition1, ParameterDefinition definition2) {
			if (ReferenceEquals(definition1, definition2)) {
				return true;
			} else if (ReferenceEquals(definition1, null)) {
				return ReferenceEquals(definition2, null);
			} else {
				return definition1.Equals(definition2);
			}
		}

		public static bool operator !=(ParameterDefinition definition1, ParameterDefinition definition2) {
			if (ReferenceEquals(definition1, definition2)) {
				return false;
			} else if (ReferenceEquals(definition1, null)) {
				return !ReferenceEquals(definition2, null);
			} else {
				return !definition1.Equals(definition2);
			}
		}

		/// <summary>
		/// Creates a new parameter definition that is a copy of the current instance.
		/// </summary>
		/// <returns>A new parameter definition that is a copy of this instance.</returns>
		public ParameterDefinition Clone() {
			return new ParameterDefinition(this);
		}

		/// <summary>
		/// Creates a new object that is a copy of the current instance.
		/// </summary>
		/// <returns>A new object that is a copy of this instance.</returns>
		object ICloneable.Clone() {
			return this.Clone();
		}
	}
}
