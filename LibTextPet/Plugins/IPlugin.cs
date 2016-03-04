using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LibTextPet.Plugins {
	/// <summary>
	/// Marks a class as a plugin for LibMsgBN.
	/// </summary>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1040:AvoidEmptyInterfaces")]
	public interface IPlugin {
		/// <summary>
		/// Gets the name of the plugin type in normal casing. The first letter is not capitalized.
		/// </summary>
		string PluginType {
			get;
		}

		string Name {
			get;
		}
		// Stuff that could be in here:
		// - Plugin name?
		// - Plugin author?
		// - Plugin version?
		// - Plugin update URL?
	}
}
