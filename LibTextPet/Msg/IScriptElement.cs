namespace LibTextPet.Msg {
	/// <summary>
	/// A single element of a text script.
	/// </summary>
	public interface IScriptElement {
		/// <summary>
		/// Gets the name of this script element.
		/// </summary>
		string Name { get; }

		/// <summary>
		/// Gets a boolean that indicates whether this script element ends script execution.
		/// </summary>
		bool EndsScript { get; }
	}
}
