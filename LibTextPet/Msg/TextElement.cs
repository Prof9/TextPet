namespace LibTextPet.Msg
{
	/// <summary>
	/// A script element that prints text.
	/// </summary>
	public class TextElement : IScriptElement
	{
		private string text;

		/// <summary>
		/// Constructs a new text element that prints the given text.
		/// </summary>
		/// <param name="value">The text that this script element contains.</param>
		public TextElement(string value) {
			this.Text = value;
		}

		/// <summary>
		/// Constructs a new text element with no text.
		/// </summary>
		public TextElement()
			: this("") { }

		/// <summary>
		/// Gets or sets the raw text printed by this script element. This text is not XML-safe.
		/// </summary>
		public string Text {
			get {
				return this.text;
			}
			set {
				this.text = value ?? "";
			}
		}

		/// <summary>
		/// Gets a boolean that indicates whether this script element ends script execution.
		/// </summary>
		public bool EndsScript => false;

		public override string ToString() => this.Text;

		public override bool Equals(object obj) {
			if (obj == null || GetType() != obj.GetType())
				return false;

			TextElement textElem = (TextElement)obj;

			return this.Equals(textElem);
		}

		public bool Equals(IScriptElement other) {
			TextElement otherTextElem = other as TextElement;
			if (otherTextElem == null) {
				return false;
			}

			return this.Text == otherTextElem.Text;
		}

		public override int GetHashCode() {
			return this.Text.GetHashCode();
		}
	}
}
