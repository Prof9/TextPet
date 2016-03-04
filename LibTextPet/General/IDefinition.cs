using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LibTextPet.General {
	/// <summary>
	/// An object that defines an instance of another object.
	/// </summary>
	internal interface IDefinition {
		/// <summary>
		/// Gets the name of the defined instance.
		/// </summary>
		string Name { get; }

		/// <summary>
		/// Gets a description of the defined instance.
		/// </summary>
		string Description { get;  }
	}
}
