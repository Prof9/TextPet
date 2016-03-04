using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LibTextPet.IO {
	/// <summary>
	/// A writer that writes objects to an output stream.
	/// </summary>
	/// <typeparam name="T">The type of object that is written.</typeparam>
	public interface IWriter<T> {
		/// <summary>
		/// Writes an object to the output stream.
		/// </summary>
		/// <param name="obj">The object to write.</param>
		void Write(T obj);
	}
}
