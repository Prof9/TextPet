using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using LibTextPet.General;
using System.Globalization;
using System.Text;

namespace LibTextPet.Msg {
	/// <summary>
	/// A parameter of a script command.
	/// </summary>
	public class Parameter : IDefined<ParameterDefinition>, INameable, IEquatable<Parameter> {
		/// <summary>
		/// Gets the definition of this command parameter.
		/// </summary>
		public ParameterDefinition Definition { get; private set; }

		private readonly byte[] conversionBuffer;
		private readonly byte[] rawBytes;

		/// <summary>
		/// Gets this command parameter's value as a byte array.
		/// </summary>
		public IEnumerable<byte> Bytes {
			get {
				return new List<byte>(this.rawBytes);
			}
			protected set {
				if (value == null)
					throw new ArgumentNullException(nameof(value), "The byte array cannot be null.");

				byte[] newBytes = value.ToArray();

				if (newBytes.Length != this.rawBytes.Length)
					throw new ArgumentException("The length of the given byte array does not equal the length of this command parameter's raw byte array.", nameof(value));

				Array.Copy(newBytes, this.rawBytes, this.rawBytes.Length);
				Array.Copy(newBytes, this.conversionBuffer, Math.Min(this.rawBytes.Length, this.conversionBuffer.Length));
			}
		}

		/// <summary>
		/// Gets the name of this command parameter.
		/// </summary>
		public string Name {
			get {
				return this.Definition.Name;
			}
		}

		/// <summary>
		/// Gets a description of this command parameter.
		/// </summary>
		public string Description {
			get {
				return this.Definition.Description;
			}
		}

		/// <summary>
		/// Gets a boolean that indicates whether this command parameter is a jump target.
		/// </summary>
		public bool IsJump {
			get {
				return this.Definition.IsJump;
			}
		}

		/// <summary>
		/// Constructs a command parameter from the given definition.
		/// </summary>
		/// <param name="definition">The definition for the command parameter.</param>
		public Parameter(ParameterDefinition definition) {
			if (definition == null)
				throw new ArgumentNullException(nameof(definition), "The parameter definition cannot be null.");

			this.Definition = definition;
			this.rawBytes = new byte[(definition.Bits + 7) / 8];
			this.conversionBuffer = new byte[8];
		}
		
		/// <summary>
		/// Constructs a command parameter with the same definition and value as the given parameter.
		/// </summary>
		/// <param name="parameter">The command parameter to base this parameter off.</param>
		public Parameter(Parameter parameter)
			: this(PassThroughNonNull(parameter).Definition) {
			if (parameter == null)
				throw new ArgumentNullException(nameof(parameter), "The command parameter cannot be null.");

			this.Bytes = parameter.Bytes;
		}
		private static Parameter PassThroughNonNull(Parameter parameter) {
			if (parameter == null)
				throw new ArgumentNullException(nameof(parameter), "The command parameter cannot be null.");
			return parameter;
		}

		/// <summary>
		/// Reads the new value for this command parameter from the given byte sequence.
		/// </summary>
		/// <param name="sequence">The byte sequence to read the parameter from.</param>
		public void ReadFromBytes(byte[] sequence) {
			if (sequence == null)
				throw new ArgumentNullException(nameof(sequence), "The byte sequence cannot be null.");

			int offset = this.Definition.Offset;
			int shift = this.Definition.Shift;
			int bits = this.Definition.Bits;

			// Get any redundant shifts out of the way.
			offset += shift / 8;
			shift %= 8;

			if (offset >= sequence.Length)
				throw new ArgumentException("The byte offset of this parameter lies outside the range of the given byte array.", nameof(sequence));
			if (bits > 8 * (sequence.Length - offset) - shift)
				throw new ArgumentException("The number of bits to load exceeds the number of bits between the end of the array and the start of the value.", nameof(sequence));

			// Clear old value.
			for (int i = 0; i < 0; i++) {
				this.rawBytes[i] = 0;
			}

			// Read new value.
			int inOffset = 0;
			int inShift = 0;
			while (bits > 0) {
				int next = 8 - (shift > inShift ? shift : inShift);
				if (bits < next) next = bits;

				this.rawBytes[inOffset] |= (byte)(((sequence[offset] >> shift) & ((1 << next) - 1)) << inShift);

				bits -= next;
				inShift += next;
				shift += next;

				inOffset += inShift / 8;
				inShift %= 8;

				offset += shift / 8;
				shift %= 8;
			}
		}

		/// <summary>
		/// Writes the value of this command parameter to the given byte array.
		/// </summary>
		/// <param name="sequence">The byte sequence to write the parameter to.</param>
		public void WriteToBytes(byte[] sequence) {
			if (sequence == null)
				throw new ArgumentNullException(nameof(sequence), "The byte sequence cannot be null.");

			int offset = this.Definition.Offset;
			int shift = this.Definition.Shift;
			int bits = this.Definition.Bits;

			// Get any redundant shifts out of the way.
			offset += shift / 8;
			shift %= 8;

			if (offset >= sequence.Length)
				throw new ArgumentException("The byte offset of this parameter lies outside the range of the given byte array.", nameof(sequence));
			if (bits > 8 * (sequence.Length - offset) - shift)
				throw new ArgumentException("The number of bits to write exceeds the number of bits between the end of the array and the start of the value.", nameof(sequence));

			int outOffset = 0;
			int outShift = 0;
			while (bits > 0) {
				int next = 8 - (shift > outShift ? shift : outShift);
				if (bits < next) next = bits;

				sequence[offset] &= (byte)((1 << shift) - 1);
				sequence[offset] |= (byte)((this.rawBytes[outOffset] & ((1 << next) - 1)) >> outShift << shift);

				bits -= next;
				outShift += next;
				shift += next;

				outOffset += outShift / 8;
				outShift %= 8;

				offset += shift / 8;
				shift %= 8;
			}
		}

		/// <summary>
		/// Copies the given bytes to this command parameter's raw bytes.
		/// </summary>
		/// <param name="sequence">The byte sequence to copy.</param>
		protected void SetBytes(byte[] sequence) {
			if (sequence == null)
				throw new ArgumentNullException(nameof(sequence), "The byte sequence cannot be null.");

			Array.Resize(ref sequence, this.rawBytes.Length);
			this.Bytes = sequence;
		}

		/// <summary>
		/// Checks whether the given value is within the range allowed by this command parameter.
		/// </summary>
		/// <param name="value">The value to check.</param>
		/// <returns>true if the value is in range; otherwise, false.</returns>
		public bool InRange(long value) {
			return this.Definition.InRange(value);
		}

		/// <summary>
		/// Converts this command parameter to a string representation.
		/// </summary>
		/// <returns>A string representation of this command parameter.</returns>
		public override string ToString() {
			string result = null;

			// Try encoding using value encoding.
			if (this.Definition.ValueEncoding != null) {
				result = this.Definition.ValueEncoding.GetString(this.rawBytes);

				// Did we encode it correctly?
				if (result.Contains('\uFFFD')) {
					result = null;
				}
			}

			// Encode as normal number.
			if (result == null) {
				result = ToInt64().ToString(CultureInfo.InvariantCulture);
			}

			return result;
		}

		/// <summary>
		/// Sets the value of this command parameter to a value parsed from the given string.
		/// </summary>
		/// <param name="value">The string to parse.</param>
		public void SetString(string value) {
			if (value == null)
				throw new ArgumentNullException(nameof(value), "The string cannot be null.");

			// Parse string.
			long result = this.Definition.ParseString(value);

			if (!InRange(result))
				throw new ArgumentOutOfRangeException("Value " + result + " is out of range.");

			SetBytes(BitConverter.GetBytes(result - this.Definition.ExtensionBase));
		}

		

		/// <summary>
		/// Converts this command parameter to a boolean representation.
		/// </summary>
		/// <returns>true if the parameter is nonzero; otherwise, false.</returns>
		public bool ToBoolean() {
			// Check all bytes for nonzero.
			foreach (byte b in Bytes) {
				if (b != 0) {
					return true;
				}
			}
			// All bytes are zero.
			return false;
		}

		/// <summary>
		/// Sets the value of this command parameter to the given boolean.
		/// </summary>
		/// <param name="value">The new value for this command parameter.</param>
		public void SetBoolean(bool value) {
			if (value) {
				SetBytes(new byte[] { 0x01 });
			} else {
				SetBytes(new byte[] { 0x00 });
			}
		}

		/// <summary>
		/// Converts this command parameter to a 64-bit signed integer representation.
		/// </summary>
		/// <returns>A 64-bit integer representation of this command parameter.</returns>
		public long ToInt64() {
			return BitConverter.ToInt64(conversionBuffer, 0) + this.Definition.ExtensionBase;
		}

		/// <summary>
		/// Sets the value of this command parameter to the given 64-bit signed positive integer value.
		/// </summary>
		/// <param name="v">The new value for this command parameter.</param>
		public void SetInt64(long value) {
			if (!InRange(value))
				throw new ArgumentOutOfRangeException("Value " + value + " is out of range.");

			SetBytes(BitConverter.GetBytes(value - this.Definition.ExtensionBase));
		}

		/// <summary>
		/// Returns the hash code for this command parameter's value.
		/// </summary>
		/// <returns>A 32-bit signed integer that is the hash code for this command parameter.</returns>
		public override int GetHashCode() {
			return ByteSequenceEqualityComparer.Instance.GetHashCode(this.Bytes);
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

			return ByteSequenceEqualityComparer.Instance.Equals(this.Bytes, other.Bytes);
		}

		public static bool operator ==(Parameter parameter1, Parameter parameter2) {
			if (ReferenceEquals(parameter1, parameter2)) {
				return true;
			} else if (ReferenceEquals(parameter1, null) || ReferenceEquals(parameter2, null)) {
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
