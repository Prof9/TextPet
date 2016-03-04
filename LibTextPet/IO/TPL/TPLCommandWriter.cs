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
		/// Outputs the specified script command as a string.
		/// </summary>
		/// <param name="obj">The script command to output.</param>
		/// <returns>The resulting string.</returns>
		protected override string Output(Command obj) {
			if (obj == null)
				throw new ArgumentNullException(nameof(obj), "The script command cannot be null.");

			StringBuilder builder = new StringBuilder();
			string indent = new string('\t', this.IndentLevel);
			string newLine = Environment.NewLine;

			// Write the name of the command.
			builder.AppendFormat("{0}{1}{2}", indent, obj.Name, newLine);

			// Write the parameters.
			foreach (Parameter par in obj.Parameters) {
				builder.AppendFormat(CultureInfo.InvariantCulture, "{0}\t{1} = {2}{3}", indent, par.Name, par.ToString(), newLine);
			}

			// Write the data entries.
			if (obj.HasData) {
				builder.AppendFormat("{0}\t[{1}", indent, newLine);
				// Loop through every data entry.
				for (int i = 0; i < obj.Data.Count; i++) {
					ReadOnlyNamedCollection<Parameter> entry = obj.Data[i];
					// Loop through every data parameter.
					for (int j = 0; j < entry.Count; j++) {
						Parameter par = entry[j];
						// Only write a comma after the last data parameter of the non-last entry.
						bool writeComma = j == entry.Count - 1 && i != obj.Data.Count - 1;
						builder.AppendFormat(CultureInfo.InvariantCulture, "{0}\t\t{1} = {2}{3}{4}", indent, par.Name, par.ToString(), writeComma ? "," : "", newLine);
					}
				}
				builder.AppendFormat("{0}\t]{1}", indent, newLine);
			}

			// Return the resulting string.
			return builder.ToString();
		}
	}
}
