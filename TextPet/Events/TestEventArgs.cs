using LibTextPet.Msg;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace TextPet.Events {
	public class TestEventArgs : EventArgs {
		public string Path { get; }
		public TextArchive BeforeTextArchive { get; }
		public TextArchive AfterTextArchive { get; }
		public ReadOnlyCollection<byte> BeforeBytes { get; }
		public ReadOnlyCollection<byte> AfterBytes { get; }
		public bool Passed { get; }

		public TestEventArgs(string path, TextArchive before, TextArchive after, IList<byte> beforeBytes, IList<byte> afterBytes, bool passed) {
			this.Path = path;
			this.BeforeTextArchive = before;
			this.AfterTextArchive = after;
			this.BeforeBytes = new ReadOnlyCollection<byte>(beforeBytes);
			this.AfterBytes = new ReadOnlyCollection<byte>(afterBytes);
			this.Passed = passed;
		}
	}
}
