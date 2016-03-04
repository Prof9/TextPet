using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LibTextPet.IO {
	/// <summary>
	/// A reader that reads objects from an input stream.
	/// </summary>
	/// <typeparam name="T">The type of object that is read.</typeparam>
	public interface IReader<T> {
		/// <summary>
		/// Reads an object from the input stream.
		/// </summary>
		/// <returns>The read object.</returns>
		T Read();
	}
}
