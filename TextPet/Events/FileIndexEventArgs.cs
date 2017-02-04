using LibTextPet.IO;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace TextPet.Events {
	public class FileIndexEventArgs : EventArgs {
		public IEnumerable<FileIndexEntry> Entries { get; }

		public string Path { get; }

		public FileIndexEventArgs(IEnumerable<FileIndexEntry> entries, string path) {
			this.Entries = entries;
			this.Path = path;
		}
	}
}
