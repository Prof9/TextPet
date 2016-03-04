using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace TextPet.Events {
	public class ReadWriteEventArgs<T> : EventArgs {
		public string Path { get; }
		public ReadOnlyCollection<T> Objects { get; }
		public int Count => this.Objects.Count;

		public ReadWriteEventArgs(string path, IList<T> objects) {
			this.Path = path;
			this.Objects = new ReadOnlyCollection<T>(objects);
		}
	}
}
