using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LibTextPet.IO {
	/// <summary>
	/// An inserter that inserts elements from an object into another object.
	/// </summary>
	/// <typeparam name="T">The type of object that is inserted from and to.</typeparam>
	public interface IPatcher<T> {
		/// <summary>
		/// Patches the specified patch object into the specified base object.
		/// </summary>
		/// <param name="baseObj">The base object.</param>
		/// <param name="patchObj">The patch object.</param>
		void Patch(T baseObj, T patchObj);
	}
}
