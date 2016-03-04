using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LibTextPet.Msg {
	/// <summary>
	/// A script element that indicates a directive for TextPet.
	/// </summary>
	public class DirectiveElement : IScriptElement {
		/// <summary>
		/// Gets the name of this directive.
		/// </summary>
		public string Name => this.DirectiveType.ToString();

		/// <summary>
		/// Gets the type of this directive.
		/// </summary>
		public DirectiveType DirectiveType { get; }

		/// <summary>
		/// Gets a boolean that indicates whether this script element ends the script, i.e. false.
		/// </summary>
		public bool EndsScript => false;

		/// <summary>
		/// Gets a boolean that indicates whether this directive element has a non-empty value.
		/// </summary>
		public bool HasNonemptyValue => !String.IsNullOrEmpty(this.Value);

		/// <summary>
		/// Gets or sets the value of this directive.
		/// </summary>
		public string Value { get; set; }

		/// <summary>
		/// Creates a new directive element with the specified type and no value.
		/// </summary>
		/// <param name="type">The directive type.</param>
		public DirectiveElement(DirectiveType type)
			: this(type, null) { }

		/// <summary>
		/// Creates a new directive element with the specified type and value.
		/// </summary>
		/// <param name="type">The directive type.</param>
		/// <param name="value">The value of the directive.</param>
		public DirectiveElement(DirectiveType type, string value) {
			this.DirectiveType = type;
			this.Value = value;
		}

		public override string ToString() {
			if (this.Value != null) {
				return this.DirectiveType.ToString() + ":" + this.Value;
			} else {
				return this.DirectiveType.ToString();
			}
		}
	}
}
