using LibTextPet.General;
using LibTextPet.Msg;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace LibTextPet.IO.TPL {
	/// <summary>
	/// A TextPet Language command writer that writes a script command to an output stream.
	/// </summary>
	public class TPLCommandWriter : IndentedWriter<Command> {
		/// <summary>
		/// Creates a new TextPet Language command writer that writes to the specified output stream.
		/// </summary>
		/// <param name="stream">The stream to write to.</param>
		public TPLCommandWriter(Stream stream)
			: base(stream) { }

		/// <summary>
		/// Writes the specified script command to the output stream.
		/// </summary>
		/// <param name="obj">The command to write.</param>
		public override void Write(Command obj) {
			if (obj == null)
				throw new ArgumentNullException(nameof(obj), "The script command cannot be null.");

			// Write the name of the command.
			this.WriteLine(obj.Name);
			this.IndentLevel++;

			// Write the command elements.
			foreach (CommandElement elem in obj.Elements) {
				if (elem.Definition.HasMultipleDataEntries) {
					// Write the data entries.
					this.Write(elem.Name);
					if (this.Flatten) {
						this.WriteLine("=[");
					} else {
						this.WriteLine(" = [");
					}
					this.IndentLevel++;
				}

				// Write the data entries.
				for (int i = 0; i < elem.Count; i++) {
					ReadOnlyNamedCollection<Parameter> entry = elem[i];
					// Write every parameter.
					for (int j = 0; j < entry.Count; j++) {
						Parameter par = entry[j];

						this.Write(par.Name);
						if (this.Flatten) {
							this.Write("=");
						} else {
							this.Write(" = ");
						}
						if (par.IsString) {
							this.Write('"' + par.ToString().Replace("\"", "\\\"") + '"');
						} else {
							this.Write(par.ToString());
						}

						// Only write a comma after the last data parameter of the non-last entry.
						if (j == entry.Count - 1 && i != elem.Count - 1) {
							this.Write(",");
						}

						this.WriteLine();
					}
				}

				if (elem.Definition.HasMultipleDataEntries) {
					this.IndentLevel--;
					this.WriteLine("]");
				}
			}

			this.IndentLevel--;
			this.Flush();
		}
	}
}
