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
		/// <summary>
		/// Gets the base stream that is being read from.
		/// </summary>
		public Stream BaseStream { get; private set; }

		/// <summary>
		/// Gets the encoding that is being used.
		/// </summary>
		public CustomFallbackEncoding Encoding { get; private set; }

		/// <summary>
		/// Creates a new conservative text reader that reads from the specified stream using the specified encoding.
		/// </summary>
		/// <param name="stream">The stream to read from.</param>
		/// <param name="encoding">The encoding to use.</param>
		public ConservativeStreamReader(Stream stream, CustomFallbackEncoding encoding) {
			if (stream == null)
				throw new ArgumentNullException(nameof(stream), "The input stream cannot be null.");
			if (!stream.CanRead)
				throw new ArgumentException("The input stream does not support reading.", nameof(stream));
			if (!stream.CanSeek)
				throw new ArgumentException("The input stream does not support seeking.", nameof(stream));
			if (encoding == null)
				throw new ArgumentNullException(nameof(encoding), "The encoding cannot be null.");

			this.BaseStream = stream;
			// This is probably not 100% safe...
			this.Encoding = (CustomFallbackEncoding)encoding.Clone();
			this.Encoding.DecoderFallback = new DecoderReplacementFallback("\uFFFD");
		}

		public IEnumerable<char> Read() {
			char[] buffer = new char[this.Encoding.GetMaxCharCount(1)];
			int read = this.Read(buffer, 0, 1);
			return buffer.Take(read);
		}

		/// <summary>
		/// Reads a specified maximum number of characters from the current reader and writes the data to a buffer, beginning at the specified index.
		/// </summary>
		/// <param name="buffer">When this method returns, contains the specified character array with the values between index and (index + count - 1) replaced by the characters read from the current source.</param>
		/// <param name="index">The position in buffer at which to begin writing.</param>
		/// <param name="count">The maximum number of characters to read. If the end of the reader is reached before the specified number of characters is read into the buffer, the method returns.</param>
		/// <returns>The number of characters that have been read. The number will be less than or equal to count, depending on whether the data is available within the reader. This method returns 0 (zero) if it is called when no more characters are left to read.</returns>
		public int Read(char[] buffer, int index, int count) {
			if (buffer == null)
				throw new ArgumentNullException(nameof(buffer), "The buffer cannot be null.");
			if (buffer.Length - index < count)
				throw new ArgumentException("The buffer length minus index is less than count.");
			if (index < 0)
				throw new ArgumentOutOfRangeException(nameof(index), index, "The index cannot be negative.");
			if (count < 0)
				throw new ArgumentOutOfRangeException(nameof(count), count, "The count cannot be negative.");

			long start = this.BaseStream.Position;
			int maxByteCount = Encoding.GetMaxByteCount(count);

			// Read the maximum number of required bytes into a buffer.
			byte[] bytes = new byte[maxByteCount];
			// Adjust the maximum byte count if necessary.
			maxByteCount = this.BaseStream.Read(bytes, 0, maxByteCount);

			// Is there any point in going on?
			if (maxByteCount <= 0) {
				return 0;
			}

			// Keep a character buffer, too.
			int maxCharCount = this.Encoding.GetMaxCharCount(count) * maxByteCount;
			char[] charBuffer = new char[maxCharCount];

			// Decode the characters from the buffer.
			int bytesRead;
			int charsRead = 0;
			bool success = false;
			for (bytesRead = 1; bytesRead <= maxByteCount; bytesRead++) {
				// Read characters.
				charsRead = this.Encoding.GetChars(bytes, 0, bytesRead, charBuffer, 0);

				// Are all characters valid?
				bool invalidCharacters = false;
				for (int i = 0; i < charsRead; i++) {
					if (charBuffer[i] == '\uFFFD') {
						invalidCharacters = true;
						break;
					}
				}
				// If there are no invalid characters, stop here.
				if (!invalidCharacters) {
					success = true;
					break;
				}
			}

			// Did we read any characters?
			if (!success) {
				charsRead = 0;
				bytesRead = 0;
			}

			// Set the new stream position properly.
			this.BaseStream.Position = start + bytesRead;

			// Copy the characters to the output buffer.
			Array.Copy(charBuffer, buffer, charsRead);

			return charsRead;
		}
	}
}
