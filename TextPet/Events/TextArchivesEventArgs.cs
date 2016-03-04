using LibTextPet.Msg;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace TextPet.Events {
	public class TextArchivesEventArgs : ReadWriteEventArgs<TextArchive> {
		public ReadOnlyCollection<TextArchive> TextArchives => this.Objects;
		public int Offset { get; }

		public TextArchivesEventArgs(string path)
			: this(path, -1, new List<TextArchive>()) { }

		public TextArchivesEventArgs(string path, int offset)
			: this(path, offset, new List<TextArchive>()) { }

		public TextArchivesEventArgs(string path, TextArchive textArchive)
			: this(path, -1, new List<TextArchive>() { textArchive }) { }

		public TextArchivesEventArgs(string path, int offset, TextArchive textArchive)
			: this(path, offset, new List<TextArchive>() { textArchive }) { }

		public TextArchivesEventArgs(string path, IList<TextArchive> textArchives)
			: this(path, -1, textArchives) { }
		
		public TextArchivesEventArgs(string path, int offset, IList<TextArchive> textArchives)
			: base(path, textArchives) {
			this.Offset = offset;
		}
	}
}
