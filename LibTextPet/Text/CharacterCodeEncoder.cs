using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LibTextPet.Text {
	/// <summary>
	/// An encoder that encodes text containing character codes consisting of multiple characters..
	/// </summary>
	public class CharacterCodeEncoder {
		/// <summary>
		/// Gets the encoding that is being used.
		/// </summary>
		public IgnoreFallbackEncoding Encoding { get; }

		/// <summary>
		/// Creates a new conservative
		/// </summary>
		/// <param name="encoding"></param>
		public CharacterCodeEncoder(Encoding encoding) {
			if (encoding == null)
				throw new ArgumentNullException(nameof(encoding), "The encoding cannot be null.");

			if (encoding is IgnoreFallbackEncoding ignoreFallbackEncoding) {
				this.Encoding = ignoreFallbackEncoding;
			} else {
				this.Encoding = new IgnoreFallbackEncoding(encoding);
			}
		}

		/// <summary>
		/// Writes a single character code from the specified source string to the specified destination list of bytes, beginning at the specified indices.
		/// </summary>
		/// <param name="source">The source string to encode characters from.</param>
		/// <param name="sourceIndex">The index of the source string at which to begin reading.</param>
		/// <param name="dest">A resizeable list of bytes to write to.</param>
		/// <param name="destIndex">The index in the byte array at which to begin writing.</param>
		/// <returns>The number of characters in the string that were encoded.</returns>
		public int WriteSingle(string source, int sourceIndex, IList<byte> dest, int destIndex) {
			return Write(source, sourceIndex, dest, destIndex, 1);
		}

		/// <summary>
		/// Writes a specified maximum number of character codes from the specified source string to the specified destination list of bytes, beginning at the specified indices.
		/// </summary>
		/// <param name="source">The source string to encode characters from.</param>
		/// <param name="sourceIndex">The index of the source string at which to begin reading.</param>
		/// <param name="dest">A resizeable list of bytes to write to.</param>
		/// <param name="destIndex">The index in the byte array at which to begin writing.</param>
		/// <param name="count">The maximum amount of character codes to write.</param>
		/// <returns>The number of characters in the string that were encoded.</returns>
		private int Write(string source, int sourceIndex, IList<byte> dest, int destIndex, int count) {
			if (source == null)
				throw new ArgumentNullException(nameof(source), "The source string cannot be null.");
			if (dest == null)
				throw new ArgumentNullException(nameof(dest), "The destination array cannot be null.");
			if (sourceIndex < 0 || sourceIndex >= source.Length)
				throw new ArgumentOutOfRangeException(nameof(sourceIndex), sourceIndex, "The source string index is out of range.");
			if (destIndex < 0)
				throw new ArgumentOutOfRangeException(nameof(destIndex), destIndex, "The destination list index cannot be negative.");

			// Set up some buffers.
			// Might be a bit too generous depending on the Encoding implementation.
			char[] charBuffer = new char[this.Encoding.GetMaxCharCount(this.Encoding.GetMaxByteCount(1))];
			byte[] byteBuffer = new byte[this.Encoding.GetMaxByteCount(this.Encoding.GetMaxCharCount(1))];

			int maxCharCount = Math.Min(charBuffer.Length, source.Length - sourceIndex);

			// Encode the characters into the buffer.
			int bytesWritten;
			int charsRead;
			int seqsRead;
			bool success;
			int totalCharsRead = 0;
			for (seqsRead = 0; seqsRead < count; seqsRead++) {
				// Try to encode an increasing number of characters.
				bytesWritten = 0;
				success = false;
				for (charsRead = 1; charsRead <= maxCharCount; charsRead++) {
					// Get next char.
					charBuffer[charsRead - 1] = source[sourceIndex + charsRead - 1];

					// Try to encode.
					success = this.Encoding.TryGetBytes(charBuffer, 0, charsRead, byteBuffer, 0, out int byteCount);
					bytesWritten = byteCount;
					if (success) {
						totalCharsRead += charsRead;
						break;
					}
				}

				if (!success) {
					// Could not encode this.
					break;
				}

				// Expand the destination list, if needed.
				while (dest.Count < destIndex + bytesWritten) {
					dest.Add(0);
				}

				// Write the bytes to the destination.
				for (int i = 0; i < bytesWritten; i++) {
					dest[destIndex + i] = byteBuffer[i];
				}

				destIndex += bytesWritten;
			}

			return totalCharsRead;
		}
	}
}
