using System;

namespace LibTextPet.Msg {
	/// <summary>
	/// A single element of a text script.
	/// </summary>
	public interface IScriptElement : IEquatable<IScriptElement> {
		/// <summary>
		/// Gets a boolean that indicates whether this script element ends script execution.
		/// </summary>
		bool EndsScript { get; }
	}
}
