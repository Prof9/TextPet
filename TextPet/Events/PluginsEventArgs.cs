using LibTextPet.Plugins;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace TextPet.Events {
	public class PluginsEventArgs : ReadWriteEventArgs<IPlugin> {
		public ReadOnlyCollection<IPlugin> Plugins => this.Objects;

		public PluginsEventArgs(string path, IPlugin plugin)
			: this(path, new List<IPlugin>() { plugin }) { }

		public PluginsEventArgs(string path, IList<IPlugin> plugins)
			: base(path, plugins) { }
	}
}
