using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LibTextPet.Text {
	/// <summary>
	/// A stream reader that reads text from a stream, without unnecessarily advancing the stream position.
	/// </summary>
    public class ConservativeStreamReader {
		private int maxByteCount;
		private int maxCharCount;

		private Decoder Decoder { get; }
		private DecoderIgnoreFallback DecoderIgnoreFallback { get; }

		/// <summary>
		/// Gets the base stream that is being read from.
		/// </summary>
		public Stream BaseStream { get; private set; }

		/// <summary>
		/// Gets the encoding that is being used.
		/// </summary>
		public Encoding Encoding { get; private set; }

		/// <summary>
		/// Gets the maximum character count that this reader can read for the specified number of code points.
		/// </summary>
		/// <param name="count">The amount of code points.</param>
		public int GetMaxCharCount(int count) {
			// Account for worst case where a fallback leads to extra characters being produced.
			return count * this.maxCharCount * this.maxByteCount;
		}
		public int GetMaxByteCount(int count) {
			return count * this.maxByteCount;
		}

		/// <summary>
		/// Creates a new conservative text reader that reads from the specified stream using the specified encoding.
		/// </summary>
		/// <param name="stream">The stream to read from.</param>
		/// <param name="encoding">The encoding to use.</param>
		public ConservativeStreamReader(Stream stream, Encoding encoding) {
			if (stream == null)
				throw new ArgumentNullException(nameof(stream), "The input stream cannot be null.");
			if (!stream.CanRead)
				throw new ArgumentException("The input stream does not support reading.", nameof(stream));
			if (!stream.CanSeek)
				throw new ArgumentException("The input stream does not support seeking.", nameof(stream));
			if (encoding == null)
				throw new ArgumentNullException(nameof(encoding), "The encoding cannot be null.");

			this.BaseStream = stream;

			this.Encoding = encoding;
			this.Decoder = encoding.GetDecoder();
			this.DecoderIgnoreFallback = new DecoderIgnoreFallback();

			this.maxByteCount = encoding.GetMaxByteCount(1);
			this.maxCharCount = encoding.GetMaxCharCount(1);
		}

		/// <summary>
		/// Attempts to read a single code point from the base stream into the specified character and byte arrays.
		/// </summary>
		/// <param name="chars">The character array to write read characters to.</param>
		/// <param name="charIndex">The index in the character array at which to begin writing.</param>
		/// <param name="bytes">The byte array to write read bytes to.</param>
		/// <param name="byteIndex">The index in the byte array at which to begin writing.</param>
		/// <param name="charsUsed">When this method exits, the amount of characters that were read from the stream.</param>
		/// <param name="bytesUsed">When this method exits, the amount of bytes that were read from the stream.</param>
		/// <returns>true if a code point was successfully read; otherwise, false.</returns>
		// Try pattern; suppress out parameter message, suppress chars/bytes naming message (modeled after Encoding).
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "chars")]
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "bytes")]
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "byte")]
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "4#")]
		public bool TryReadSingleCodePoint(char[] chars, int charIndex, byte[] bytes, int byteIndex, out int charsUsed, out int bytesUsed) {
			if (chars == null)
				throw new ArgumentNullException(nameof(chars), "The character array cannot be null.");
			if (charIndex < 0)
				throw new ArgumentOutOfRangeException(nameof(charIndex), charIndex, "The character index cannot be negative.");
			if (bytes == null)
				throw new ArgumentNullException(nameof(bytes), "The byte array cannot be null.");
			if (byteIndex < 0)
				throw new ArgumentOutOfRangeException(nameof(byteIndex), byteIndex, "The byte index cannot be negative.");

			long start = this.BaseStream.Position;

			this.Decoder.Reset();
			DecoderFallback origFallback = this.Decoder.Fallback;
			this.Decoder.Fallback = this.DecoderIgnoreFallback;
			this.DecoderIgnoreFallback.ResetFallbackCount();

			try {
				// Decode the characters from the stream.
				bytesUsed = 0;
				charsUsed = 0;
				while (bytesUsed < this.maxByteCount) {
					if (byteIndex + bytesUsed > bytes.Length) {
						throw new ArgumentException("The byte array is not large enough to hold the required number of bytes.", nameof(bytes));
					}
					int b = this.BaseStream.ReadByte();
					if (b == -1) {
						// End of stream, unable to read chars.
						break;
					}
					bytes[byteIndex + bytesUsed] = (byte)b;

					// Read characters.
					// WARNING: If using non-greedy decoder, may read multiple code points and overflow char buffer!!!!
					charsUsed += this.Decoder.GetChars(bytes, byteIndex + bytesUsed, 1, chars, 0, false);
					bytesUsed++;

					// If errors occurred, decoding was not successful.
					if (this.DecoderIgnoreFallback.FallbackCount > 0) {
						break;
					}

					// If chars read, decoding was successful.
					if (charsUsed > 0) {
						return true;
					}
				}

				// Decoding failed.
				this.BaseStream.Position = start;
				return false;
			} finally {
				this.Decoder.Fallback = origFallback;
			}
		}
	}
}
