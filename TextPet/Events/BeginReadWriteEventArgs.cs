using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace TextPet.Events {
	public class BeginReadWriteEventArgs : EventArgs {
		public ReadOnlyCollection<string> Files { get; }
		public int Amount { get; }
		public bool IsWrite { get; }

		public BeginReadWriteEventArgs(string file, bool isWrite)
			: this(new List<string>() { file }, isWrite, 1) { }

		public BeginReadWriteEventArgs(string file, bool isWrite, int amount)
			: this(new List<string>() { file }, isWrite, amount) { }

		public BeginReadWriteEventArgs(IList<string> files, bool isWrite)
			: this(files, isWrite, files?.Count ?? 0) { }

		public BeginReadWriteEventArgs(IList<string> files, bool isWrite, int amount) {
			this.Files = new ReadOnlyCollection<string>(files);
			this.IsWrite = isWrite;
			this.Amount = amount;
		}
	}
}
