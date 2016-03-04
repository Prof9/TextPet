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
		/// Gets the name of this script element, i.e. "byte".
		/// </summary>
		public string Name {
			get {
				return "byte";
			}
		}

		/// <summary>
		/// Gets a boolean that indicates whether this script element ends the script.
		/// </summary>
		public bool EndsScript {
			get {
				return false;
			}
		}

		private byte rawByte;
		/// <summary>
		/// Gets or sets the byte represented by this script element.
		/// </summary>
		public byte Byte {
			get {
				return this.rawByte;
			}
			set {
				this.rawByte = value;
			}
		}

		public override string ToString() {
			return "[$" + this.Byte.ToString("X2", CultureInfo.InvariantCulture) + "]";
		}
	}
}
