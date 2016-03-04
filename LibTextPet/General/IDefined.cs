using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LibTextPet.General {
	/// <summary>
	/// An object of which instances can be defined by another object.
	/// </summary>
	/// <typeparam name="T">The definition object type.</typeparam>
	internal interface IDefined<T> where T : IDefinition {
		/// <summary>
		/// Gets the definition of this instance.
		/// </summary>
		T Definition { get; }
	}
}
