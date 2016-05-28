using System;
using System.Globalization;

namespace LibTextPet.Msg {
	/// <summary>
	/// An unidentified script element, represented as a single byte.
	/// </summary>
	public class ByteElement : IScriptElement {
		/// <summary>
		/// Creates a new byte element from the given byte.
		/// </summary>
		/// <param name="value">The byte.</param>
		public ByteElement(byte value) {
			this.Byte = value;
		}

		/// <summary>
		/// Gets a boolean that indicates whether this script element ends the script.
		/// </summary>
		public bool EndsScript => false;

		/// <summary>
		/// Gets or sets the byte represented by this script element.
		/// </summary>
		public byte Byte { get; set; }

		public override string ToString() {
			return "[$" + this.Byte.ToString("X2", CultureInfo.InvariantCulture) + "]";
		}

		public override bool Equals(object obj) {
			if (obj == null || GetType() != obj.GetType())
				return false;

			ByteElement byteElem = (ByteElement)obj;

			return this.Equals(byteElem);
		}

		public bool Equals(IScriptElement other) {
			ByteElement otherByteElem = other as ByteElement;
			if (otherByteElem == null) {
				return false;
			}

			return this.Byte == otherByteElem.Byte;
		}

		public override int GetHashCode() {
			return this.Byte.GetHashCode();
		}
	}
}
