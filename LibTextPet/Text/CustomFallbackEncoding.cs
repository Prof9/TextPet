using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LibTextPet.Text
{
	/// <summary>
	/// An abstract encoding that makes the encoder and decoder fallbacks mutable.
	/// </summary>
    public abstract class CustomFallbackEncoding : Encoding
    {
		/// <summary>
		/// The encoder fallback.
		/// </summary>
		private EncoderFallback encoderFallback;
		/// <summary>
		/// The decoder fallback.
		/// </summary>
		private DecoderFallback decoderFallback;

		/// <summary>
		/// Gets or sets the encoder fallback for this encoding.
		/// </summary>
		public new EncoderFallback EncoderFallback {
			get {
				return encoderFallback ?? base.EncoderFallback;
			}
			set {
				if (value == null)
					throw new ArgumentNullException(nameof(value), "The encoder fallback cannot be null.");
				this.encoderFallback = value;
			}
		}

		/// <summary>
		/// Gets or sets the decoder fallback for this encoding.
		/// </summary>
		public new DecoderFallback DecoderFallback {
			get {
				return decoderFallback ?? base.DecoderFallback;
			}
			set {
				if (value == null)
					throw new ArgumentNullException(nameof(value), "The decoder fallback cannot be null.");
				this.decoderFallback = value;
			}
		}
	}
}
