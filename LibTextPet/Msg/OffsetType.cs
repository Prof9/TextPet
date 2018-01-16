using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LibTextPet.Msg {
	/// <summary>
	/// An enum that indicates what a command parameter's offset is relative to.
	/// </summary>
	public enum OffsetType {
		/// <summary>
		/// Indicates that the offset is relative to the start of the command.
		/// </summary>
		Start,
		/// <summary>
		/// Indicates that the offset is relative to the end of the command.
		/// </summary>
		End,
		/// <summary>
		/// Indicates that the offset is relative to a specific label.
		/// </summary>
		Label
	}
}
