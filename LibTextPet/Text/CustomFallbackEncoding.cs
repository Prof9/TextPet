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
		/// The encoder fallback to use when unknown characters are ignored.
		/// </summary>
		private EncoderFallback IgnoreEncoderFallback { get; set; }
		/// <summary>
		/// The encoder fallback to use when unknown characters are ignored.
		/// </summary>
		private DecoderFallback IgnoreDecoderFallback { get; set; }
		
		/// <summary>
		/// Gets or sets a boolean that indicates whether unknown characters should be skipped in case an exception would be thrown.
		/// </summary>
		public bool IgnoreUnknownChars { get; set; }

		/// <summary>
		/// Gets or sets the encoder fallback for this encoding.
		/// </summary>
		public new EncoderFallback EncoderFallback {
			get {
				EncoderFallback fallback = encoderFallback ?? base.EncoderFallback;

				if (this.IgnoreUnknownChars && fallback is EncoderExceptionFallback) {
					if (this.IgnoreEncoderFallback == null) {
						this.IgnoreEncoderFallback = new EncoderReplacementFallback("?");
					}
					fallback = this.IgnoreEncoderFallback;
				}

				return fallback;
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
				DecoderFallback fallback = decoderFallback ?? base.DecoderFallback;

				if (this.IgnoreUnknownChars && fallback is DecoderExceptionFallback) {
					if (this.IgnoreDecoderFallback == null) {
						this.IgnoreDecoderFallback = new DecoderReplacementFallback("?");
					}
					fallback = this.IgnoreDecoderFallback;
				}

				return decoderFallback;
			}
			set {
				if (value == null)
					throw new ArgumentNullException(nameof(value), "The decoder fallback cannot be null.");
				this.decoderFallback = value;
			}
		}
	}
}
