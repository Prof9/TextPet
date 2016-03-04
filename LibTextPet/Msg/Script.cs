using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace LibTextPet.Msg {
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix")]
	public class Script : Collection<IScriptElement> {
		/// <summary>
		/// Gets or sets the name of the command database this script uses, or null if no command database has been set.
		/// </summary>
		public string DatabaseName { get; set; }

		/// <summary>
		/// Creates a new empty script.
		/// </summary>
		public Script()
			: this(null) { }

		/// <summary>
		/// Creates a new empty script with the specified command database name.
		/// </summary>
		/// <param name="dbName">The script's command database name.</param>
		public Script(string dbName)
			: base() {
			// Command database name can be null.
			this.DatabaseName = dbName;
		}
    }
}
