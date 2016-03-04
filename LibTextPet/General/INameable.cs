using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LibTextPet.General {
	/// <summary>
	/// An object with a name that uniquely identifies it.
	/// </summary>
	public interface INameable {
		/// <summary>
		/// Gets the name of this object.
		/// </summary>
		string Name { get; }
	}
}
