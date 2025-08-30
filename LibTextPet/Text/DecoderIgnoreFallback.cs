using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LibTextPet.Text {
	/// <summary>
	/// A decoder fallback that simply ignores unknown characters and records the amount of fallbacks that occur.
	/// </summary>
	public class DecoderIgnoreFallback : DecoderFallback {
		private class DecoderIgnoreFallbackBuffer : DecoderFallbackBuffer {
			public int FallbackCount { get; private set; }

			public override int Remaining => 0;

			public DecoderIgnoreFallbackBuffer() {
				this.FallbackCount = 0;
			}

			public void ResetFallbackCount() {
				this.FallbackCount = 0;
			}

			public override bool Fallback(byte[] bytesUnknown, int index) {
				// Record the fallback and do nothing.
				checked {
					this.FallbackCount++;
				}
				return true;
			}

			public override char GetNextChar() => '\0';

			public override bool MovePrevious() => true;
		}

		private DecoderIgnoreFallbackBuffer DecoderFallbackBuffer { get; }

		public override int MaxCharCount => 0;

		/// <summary>
		/// Gets the amount of fallbacks that have occurred.
		/// </summary>
		public int FallbackCount => this.DecoderFallbackBuffer.FallbackCount;

		/// <summary>
		/// Creates a new decoder ignore fallback.
		/// </summary>
		public DecoderIgnoreFallback() {
			this.DecoderFallbackBuffer = new DecoderIgnoreFallbackBuffer();
		}

		/// <summary>
		/// Resets the amount of fallbacks that have occurred.
		/// </summary>
		public void ResetFallbackCount() => this.DecoderFallbackBuffer.ResetFallbackCount();

		public override DecoderFallbackBuffer CreateFallbackBuffer() => this.DecoderFallbackBuffer;
	}
}
