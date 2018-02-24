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
		private byte[] byteBuffer;
		private char[] charBuffer;

		private Decoder Decoder { get; }

		/// <summary>
		/// Gets the base stream that is being read from.
		/// </summary>
		public Stream BaseStream { get; private set; }

		/// <summary>
		/// Gets the encoding that is being used.
		/// </summary>
		public IgnoreFallbackEncoding Encoding { get; private set; }

		/// <summary>
		/// Creates a new conservative text reader that reads from the specified stream using the specified encoding.
		/// </summary>
		/// <param name="stream">The stream to read from.</param>
		/// <param name="encoding">The encoding to use.</param>
		public ConservativeStreamReader(Stream stream, IgnoreFallbackEncoding encoding) {
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

			this.maxByteCount = encoding.GetMaxByteCount(1);
			this.byteBuffer = new byte[this.maxByteCount];
			this.charBuffer = new char[encoding.GetMaxCharCount(1)];
		}

		public IEnumerable<char> ReadSingle() {
			long start = this.BaseStream.Position;

			// Read the maximum number of required bytes into a buffer.
			int byteCount = this.BaseStream.Read(this.byteBuffer, 0, this.maxByteCount);

			if (byteCount <= 0) {
				this.BaseStream.Position = start;
				yield break;
			}

			this.Decoder.Reset();
			this.Encoding.ResetFallbackCount();

			// Decode the characters from the buffer.
			int charsRead = 0;
			for (int i = 0; i < byteCount; i++) {
				// Read characters.
				// WARNING: If using non-greedy decoder, may read multiple code points and overflow char buffer!!!!
				charsRead = this.Decoder.GetChars(this.byteBuffer, i, 1, this.charBuffer, 0, false);

				// If errors occurred, decoding was not successful.
				if (this.Encoding.FallbackCount > 0) {
					this.BaseStream.Position = start;
					yield break;
				}

				// If chars read, decoding was successful.
				if (charsRead > 0) {
					this.BaseStream.Position = start + i + 1;
					break;
				}
			}

			// Return chars read.
			for (int i = 0; i < charsRead; i++) {
				yield return this.charBuffer[i];
			}
		}
	}
}
