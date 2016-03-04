namespace LibTextPet.Msg {
	/// <summary>
	/// An enum that indicates how a script element ends script execution.
	/// </summary>
	public enum EndType {
		/// <summary>
		/// Indicates that the script element only ends script execution if all jump parameters target an external script.
		/// </summary>
		Default,
		/// <summary>
		/// Indicates that the script element always ends script execution.
		/// </summary>
		Always,
		/// <summary>
		/// Indicates that the script element never ends script execution.
		/// </summary>
		Never
	}
}
