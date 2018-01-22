using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LibTextPet.Text {
	/// <summary>
	/// An encoder fallback that simply ignores unknown characters and records the amount of fallbacks that occur.
	/// </summary>
	public class EncoderIgnoreFallback : EncoderFallback {
		private class EncoderIgnoreFallbackBuffer : EncoderFallbackBuffer {
			public int FallbackCount { get; private set; }

			public override int Remaining => 0;

			public EncoderIgnoreFallbackBuffer() {
				this.FallbackCount = 0;
			}

			public void ResetFallbackCount() {
				this.FallbackCount = 0;
			}

			public override bool Fallback(char charUnknown, int index) {
				// Record the fallback and do nothing.
				if (this.FallbackCount < int.MaxValue) {
					this.FallbackCount++;
				}
				return true;
			}

			public override bool Fallback(char charUnknownHigh, char charUnknownLow, int index) {
				// Record the fallback and do nothing.
				if (this.FallbackCount < int.MaxValue) {
					this.FallbackCount++;
				}
				return true;
			}

			public override char GetNextChar() => '\0';

			public override bool MovePrevious() => true;
		}

		private EncoderIgnoreFallbackBuffer EncoderFallbackBuffer { get; }

		public override int MaxCharCount => 0;

		/// <summary>
		/// Gets the amount of fallbacks that have occurred.
		/// </summary>
		public int FallbackCount => this.EncoderFallbackBuffer.FallbackCount;

		/// <summary>
		/// Creates a new encoder ignore fallback.
		/// </summary>
		public EncoderIgnoreFallback() {
			this.EncoderFallbackBuffer = new EncoderIgnoreFallbackBuffer();
		}

		/// <summary>
		/// Resets the amount of fallbacks that have occurred.
		/// </summary>
		public void ResetFallbackCount() => this.EncoderFallbackBuffer.ResetFallbackCount();

		public override EncoderFallbackBuffer CreateFallbackBuffer() => this.EncoderFallbackBuffer;
	}
}
