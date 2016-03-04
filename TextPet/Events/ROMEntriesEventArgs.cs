using LibTextPet.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TextPet.Events {
	public class ROMEntriesEventArgs : EventArgs {
		public ROMEntryCollection ROMEntries { get; }

		public string Path { get; }

		public ROMEntriesEventArgs(ROMEntryCollection entries, string path) {
			this.ROMEntries = entries;
			this.Path = path;
		}
	}
}
