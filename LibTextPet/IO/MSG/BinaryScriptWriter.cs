using LibTextPet.Msg;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LibTextPet.IO.Msg {
	/// <summary>
	/// A binary script writer that writes a script to an output stream.
	/// </summary>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "ScriptWriter")]
	public class BinaryScriptWriter : ScriptWriter {
		/// <summary>
		/// Creates a new binary script writer that writes to the specified output stream.
		/// </summary>
		/// <param name="stream">The stream to write to.</param>
		/// <param name="encoding">The encoding to use.</param>
		/// <param name="databases">The command databases to use.</param>
		public BinaryScriptWriter(Stream stream, Encoding encoding)
			: base(stream, true, true, encoding, new BinaryCommandWriter(stream)) { }

		/// <summary>
		/// Writes a fallback element to the output stream.
		/// </summary>
		/// <param name="element">The fallback element to write.</param>
		protected override void WriteFallback(IScriptElement element) {
			ByteElement byteElement = element as ByteElement;
			if (byteElement != null) {
				this.BaseStream.WriteByte(byteElement.Byte);
				return;
			}

			throw new NotSupportedException("Unsupported fallback element.");
		}
	}
}
