using LibTextPet.General;
using LibTextPet.Msg;
using LibTextPet.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LibTextPet.IO.Msg {
	/// <summary>
	/// A binary script reader that reads a script from an input stream.
	/// </summary>
	public class FixedSizeScriptReader : BinaryScriptReader {
		private long fixedLength;
		/// <summary>
		/// Gets the fixed byte length for a script.
		/// </summary>
		public long FixedLength {
			get {
				return this.UseFixedLength ? this.fixedLength : long.MaxValue;
			}
			private set {
				if (value < 0)
					throw new ArgumentOutOfRangeException(nameof(value), value, "The length must be at least 0.");
				
				this.fixedLength = value;
				this.UseFixedLength = true;
			}
		}

		/// <summary>
		/// Gets a boolean that determines whether to use a fixed byte length for scripts.
		/// </summary>
		public bool UseFixedLength {
			get; private set;
		}

		/// <summary>
		/// Creates a new fixed size binary script reader that reads from the specified input stream.
		/// </summary>
		/// <param name="stream">The stream to read from.</param>
		/// <param name="encoding">The encoding to use.</param>
		/// <param name="databases">The command databases to use, in order of preference.</param>
		public FixedSizeScriptReader(Stream stream, IgnoreFallbackEncoding encoding, params CommandDatabase[] databases)
			: base(stream, encoding, databases) {
			this.fixedLength = int.MaxValue;
			this.UseFixedLength = false;
		}

		public override Script Read() {
			Script script = base.Read();

			// Check if the fixed length was exceeded.
			if (this.BaseStream.Position - this.StartPosition > this.FixedLength) {
				script = null;
			}

			return script;
		}

		/// <summary>
		/// Enables the use of a maximum byte length for read scripts, and sets the maximum length to the specified value.
		/// </summary>
		/// <param name="length">The fixed byte length.</param>
		public void SetFixedLength(long length) {
			if (length < 0)
				throw new ArgumentOutOfRangeException(nameof(length), length, "The fixed length cannot be negative.");

			this.FixedLength = length;
			this.UseFixedLength = true;
		}

		/// <summary>
		/// Disables the use of a maximum byte length for read scripts.
		/// </summary>
		public void ClearFixedLength() {
			this.UseFixedLength = false;
		}

		/// <summary>
		/// Reads the next script command from the input stream.
		/// </summary>
		/// <returns>The next script command read from the input stream, or null if no script command exists at the current position in the input stream.</returns>
		protected override Command ReadCommand(IReader<Command> commandReader) {
			Command cmd = base.ReadCommand(commandReader);

			// Abort if the command would exceed the fixed script size.
			if (cmd != null && this.BaseStream.Position - this.StartPosition > this.FixedLength) {
				cmd = null;
			}

			return cmd;
		}

		/// <summary>
		/// Checks whether the current stream has script elements left.
		/// </summary>
		/// <returns>true if there are script elements left; otherwise, false.</returns>
		protected override bool HasNext() {
			if (!this.UseFixedLength) {
				return base.HasNext();
			}
			
			// Check if the script length has been reached.
			if (this.BaseStream.Position - this.StartPosition >= this.FixedLength) {
				return false;
			}
			return true;
		}
	}
}
