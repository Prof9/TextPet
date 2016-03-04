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
		/// Gets the name of this script element, i.e. "text".
		/// </summary>
		public string Name {
			get {
				return "text";
			}
		}

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
		public bool EndsScript {
			get {
				return false;
			}
		}

		public override string ToString() {
			return this.Text;
		}
	}
}
