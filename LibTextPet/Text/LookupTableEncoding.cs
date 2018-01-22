using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using LibTextPet.General;
using LibTextPet.Plugins;

namespace LibTextPet.Text {
	/// <summary>
	/// An encoding that encodes and decodes text based on a predefined lookup table.
	/// <para>Level 1 table file standard compliance.</para>
	/// </summary>
	public class LookupTableEncoding : Encoding {
		// Two Dictionaries for 2-way lookup.
		private Dictionary<byte[], string> byteToStringDictionary;
		private Dictionary<string, byte[]> stringToByteDictionary;

		// Maximum byte/char count, needed for conversion.
		private readonly int maxByteCount;
		private readonly int maxCharCount;

		public override string EncodingName { get; }

		/// <summary>
		/// Constructs a new lookup table encoding with the given name based on the given lookup table dictionary.
		/// </summary>
		/// <param name="name">The name of the lookup table encoding.</param>
		/// <param name="dictionary">The dictionary to use as the character lookup table.</param>
		public LookupTableEncoding(string name, Dictionary<byte[], string> dictionary) {
			if (name == null)
				throw new ArgumentNullException(nameof(name), "The encoding name cannot be null.");
			if (dictionary == null)
				throw new ArgumentNullException(nameof(dictionary), "The dictionary cannot be null.");

			this.EncodingName = name;

			// Initialize variables.
			this.byteToStringDictionary = new Dictionary<byte[], string>(dictionary.Count,
				ByteSequenceEqualityComparer.Instance);
			this.stringToByteDictionary = new Dictionary<string, byte[]>(dictionary.Count);
			int maxByteCount = 0;
			int maxCharCount = 0;

			// Go through the input dictionary to validate and add all pairs.
			foreach (KeyValuePair<byte[], string> pair in dictionary) {
				if (pair.Key == null)
					throw new ArgumentException("Key cannot be null.", nameof(dictionary));
				if (pair.Value == null)
					throw new ArgumentException("Value cannot be null.", nameof(dictionary));
				if (pair.Key.Length <= 0)
					throw new ArgumentException("Key cannot be empty.", nameof(dictionary));
				if (pair.Value.Length <= 0)
					throw new ArgumentException("Value cannot be empty.", nameof(dictionary));
				if (this.byteToStringDictionary.ContainsKey(pair.Key))
					throw new ArgumentException("Duplicate key " + BitConverter.ToString(pair.Key) + ".", nameof(dictionary));
				if (this.stringToByteDictionary.ContainsKey(pair.Value))
					throw new ArgumentException("Duplicate value " + pair.Value + ".", nameof(dictionary));

				// Adjust recorded maximum counts.
				if (pair.Key.Length > maxByteCount) maxByteCount = pair.Key.Length;
				if (pair.Value.Length > maxCharCount) maxCharCount = pair.Value.Length;

				// Add to internal dictionaries.
				this.byteToStringDictionary.Add(pair.Key, pair.Value);
				this.stringToByteDictionary.Add(pair.Value, pair.Key);
			}

			this.maxByteCount = maxByteCount;
			this.maxCharCount = maxCharCount;
		}

		/// <summary>
		/// Gets a boolean that indicates whether the current encoding uses single-byte code points.
		/// </summary>
		public override bool IsSingleByte {
			get {
				return this.maxByteCount == 1;
			}
		}

		/// <summary>
		/// Calculates the number of bytes produced by encoding a set of characters from the given character array.
		/// <para>Level 1 table file standard compliance.</para>
		/// </summary>
		/// <param name="chars">The character array containing the set of characters to encode.</param>
		/// <param name="index">The index of the first character to encode.</param>
		/// <param name="count">The number of characters to encode.</param>
		/// <returns>The number of bytes produced by encoding the specified characters.</returns>
		public override int GetByteCount(char[] chars, int index, int count) {
			if (chars == null)
				throw new ArgumentNullException(nameof(chars), "The character array cannot be null.");

			if (chars.Length <= 0) {
				return 0;
			}

			if (index < 0 || index >= chars.Length)
				throw new ArgumentOutOfRangeException(nameof(index), "Index is out of range.");
			if (count < 0 || index + count > chars.Length)
				throw new ArgumentOutOfRangeException(nameof(count), "Character count is out of range.");

			// Use GetBytes(string) and return the length.
			string s = new string(chars, index, count);
			return GetBytes(s).Length;
		}

		/// <summary>
		/// Encodes all the characters in the given string into a sequence of bytes.
		/// <para>Level 1 table file standard compliance.</para>
		/// </summary>
		/// <param name="s">The string containing the characters to encode.</param>
		/// <returns>A byte array containing the results of encoding the specified set of characters.</returns>
		public override byte[] GetBytes(string s) {
			if (s == null)
				throw new ArgumentNullException(nameof(s), "The string cannot be null.");

			// Initialize fallback buffer.
			EncoderFallbackBuffer fallbackBuffer = null;

			// Make a stream for easy writing.
			using (MemoryStream stream = new MemoryStream(GetMaxByteCount(s.Length))) {
				// String position.
				int pos = 0;
				// The maximum amount of characters to seek.
				int seek = this.maxCharCount;

				// Go through the whole string.
				while (pos < s.Length) {
					// Adjust seek count to remaining character count.
					if (s.Length - pos < seek) {
						seek = s.Length - pos;
					}

					// Match raw byte.
					if (Regex.IsMatch(s.Substring(pos), @"^\[\$[0-9A-Fa-f]{2}\]")) {
						byte b = Byte.Parse(s.Substring(pos + 2, 2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture);
						stream.WriteByte(b);
						pos += 5;
						continue;
					}

					// Start searching.
					bool found = false;
					for (int l = seek; l > 0; l--) {
						string nextKey = s.Substring(pos, l);

						// Is the key in the lookup table?
						if (stringToByteDictionary.TryGetValue(nextKey, out byte[] nextValue)) {
							// Write it to the stream and advance.
							stream.Write(nextValue, 0, nextValue.Length);
							pos += l;
							found = true;
							break;
						}
					}

					// If character could not be encoded, use fallback.
					if (!found) {
						// Create fallback buffer if it has not been created yet.
						if (fallbackBuffer == null) {
							fallbackBuffer = this.EncoderFallback.CreateFallbackBuffer();
						}

						// Fallback on unknown byte.
						char fallbackChar = s[pos];
						if (fallbackBuffer.Fallback(fallbackChar, pos)) {
							// Append all fallback characters.
							while (fallbackBuffer.Remaining > 0) {
								string nextKey = new string(new char[] { fallbackBuffer.GetNextChar() });

								// Is the fallback char in the lookup table?
								if (stringToByteDictionary.TryGetValue(nextKey, out byte[] nextValue)) {
									stream.Write(nextValue, 0, nextValue.Length);
									pos++;
									found = true;
								} else {
									throw new EncoderFallbackException("Could not encode " + fallbackChar + ".");
								}
							}
						} else {
							throw new EncoderFallbackException("Could not encode " + fallbackChar + ".");
						}
					}

					// If the string is not in the lookup table, throw exception.
					if (!found)
						throw new EncoderFallbackException("Could not encode " + s[0] + ".");
				}

				// Convert stream to array and return.
				return stream.ToArray();
			}
		}

		/// <summary>
		/// Encodes a set of characters from the given character array into the given byte array.
		/// <para>Level 1 table file standard compliance.</para>
		/// </summary>
		/// <param name="chars">The character array containing the set of characters to encode.</param>
		/// <param name="charIndex">The index of the first character to encode.</param>
		/// <param name="charCount">The number of characters to encode.</param>
		/// <param name="bytes">The byte array to contain the resulting sequence of bytes.</param>
		/// <param name="byteIndex">The index at which to start writing the resulting sequence of bytes.</param>
		/// <returns>The actual number of bytes written into bytes.</returns>
		public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex) {
			if (chars == null)
				throw new ArgumentNullException(nameof(chars), "The character array cannot be null.");
			if (bytes == null)
				throw new ArgumentNullException(nameof(bytes), "The byte array cannot be null.");
			if (charIndex < 0 || charIndex >= chars.Length)
				throw new ArgumentOutOfRangeException(nameof(charIndex), "The index of the first character is out of range.");
			if (charCount < 0 || charIndex + charCount > chars.Length)
				throw new ArgumentOutOfRangeException(nameof(charCount), "The number of characters is out of range.");
			if (byteIndex < 0 || byteIndex >= bytes.Length)
				throw new ArgumentOutOfRangeException(nameof(byteIndex), "The index in the byte array is out of range.");

			// Use GetBytes(string).
			byte[] buffer = GetBytes(new string(chars, charIndex, charCount));

			// Is there enough room for the buffer?
			if (bytes.Length - byteIndex < buffer.Length)
				throw new ArgumentException("Not enough bytes available.", nameof(bytes));

			Array.Copy(buffer, 0, bytes, byteIndex, buffer.Length);

			return buffer.Length;
		}

		/// <summary>
		/// Calculates the number of characters produced by decoding a sequence of bytes from the given byte array.
		/// </summary>
		/// <param name="bytes">The byte array containing the sequence of bytes to decode.</param>
		/// <param name="index">The index of the first byte to decode.</param>
		/// <param name="count">The number of bytes to decode.</param>
		/// <returns>The number of characters produced.</returns>
		public override int GetCharCount(byte[] bytes, int index, int count) {
			if (bytes == null)
				throw new ArgumentNullException(nameof(bytes), "The byte array cannot be null.");
			if (index < 0 || index >= bytes.Length)
				throw new ArgumentOutOfRangeException(nameof(index), "Index is out of range.");
			if (count < 0 || index + count > bytes.Length)
				throw new ArgumentOutOfRangeException(nameof(count), "Count is out of range.");

			// Use GetString(byte[]).
			byte[] subBytes = new byte[count];
			Array.Copy(bytes, index, subBytes, 0, count);
			
			return GetString(subBytes).Length;
		}

		/// <summary>
		/// Decodes a sequence of bytes from the given byte array into the given character array.
		/// </summary>
		/// <param name="bytes">The byte array containing the sequence of bytes to decode.</param>
		/// <param name="byteIndex">The index of the first byte to decode.</param>
		/// <param name="byteCount">The number of bytes to decode.</param>
		/// <param name="chars">The character array to contain the resulting set of characters.</param>
		/// <param name="charIndex">The index at which to start writing the resulting set of characters.</param>
		/// <returns>The actual number of characters written into chars.</returns>
		public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex) {
			if (bytes == null)
				throw new ArgumentNullException(nameof(bytes), "The byte array cannot be null.");
			if (chars == null)
				throw new ArgumentNullException(nameof(chars), "The character array cannot be null.");
			if (byteIndex < 0 || byteIndex >= bytes.Length)
				throw new ArgumentOutOfRangeException(nameof(byteIndex), "Byte index is out of range.");
			if (byteCount < 0 || byteIndex + byteCount > bytes.Length)
				throw new ArgumentOutOfRangeException(nameof(byteCount), "Byte count is out of range.");
			if (charIndex < 0)
				throw new ArgumentOutOfRangeException(nameof(charIndex), "Character index cannot be negative..");

			// Use GetString(byte[]).
			byte[] subBytes = new byte[byteCount];
			Buffer.BlockCopy(bytes, byteIndex, subBytes, 0, byteCount);
			char[] buffer = GetString(subBytes).ToCharArray();

			// Is there enough room for the buffer?
			if (chars.Length - charIndex < buffer.Length)
				throw new ArgumentException("Not enough characters available.", nameof(chars));

			Array.Copy(buffer, 0, chars, charIndex, buffer.Length);

			return buffer.Length;
		}

		/// <summary>
		/// Decodes all the bytes in the given byte array into a string.
		/// </summary>
		/// <param name="bytes">The byte array containing the sequence of bytes to decode.</param>
		/// <returns>A string that contains the results of decoding the specified sequence of bytes.</returns>
		public override string GetString(byte[] bytes) {
			if (bytes == null)
				throw new ArgumentNullException(nameof(bytes), "The byte array cannot be null.");

			// Initialize fallback buffer.
			DecoderFallbackBuffer fallbackBuffer = null;

			// Use a builder for easy writing.
			StringBuilder builder = new StringBuilder(bytes.Length, GetMaxCharCount(bytes.Length));

			// Byte array position.
			int pos = 0;
			// The maximum amount of bytes to seek.
			int seek = this.maxByteCount;

			// Go through the whole byte array.
			while (pos < bytes.Length) {
				// Adjust seek count to remaining byte count.
				if (bytes.Length - pos < seek) {
					seek = bytes.Length - pos;
				}

				// Start searching.
				bool found = false;
				for (int l = seek; l > 0; l--) {
					byte[] nextKey = new byte[l];
					Buffer.BlockCopy(bytes, pos, nextKey, 0, l);

					// Is the key in the lookup table?
					if (byteToStringDictionary.TryGetValue(nextKey, out string nextValue)) {
						// Append it to the builder and advance.
						builder.Append(nextValue);
						pos += l;
						found = true;
						break;
					}
				}

				// If byte could not be decoded, use fallback.
				if (!found) {
					// Create fallback buffer if it has not been created yet.
					if (fallbackBuffer == null) {
						fallbackBuffer = this.DecoderFallback.CreateFallbackBuffer();
					}

					// Fallback on unknown byte.
					byte[] fallbackBytes = new byte[] { bytes[pos] };
					pos += 1;
					if (fallbackBuffer.Fallback(fallbackBytes, pos)) {
						// Append all fallback characters.
						while (fallbackBuffer.Remaining > 0) {
							builder.Append(fallbackBuffer.GetNextChar());
						}
					} else {
						throw new DecoderFallbackException("Could not decode " + BitConverter.ToString(fallbackBytes) + ".", fallbackBytes, 0);
					}
				}
			}

			return builder.ToString();
		}

		/// <summary>
		/// Calculates the maximum number of bytes produced by encoding the given number of characters.
		/// </summary>
		/// <param name="charCount">The number of characters to encode.</param>
		/// <returns>The maximum number of bytes produced.</returns>
		public override int GetMaxByteCount(int charCount) {
			if (charCount < 0)
				throw new ArgumentOutOfRangeException(nameof(charCount), "Character count is out of range.");

			// Compute worst case scenario.
			return charCount * this.maxByteCount;
		}

		/// <summary>
		/// Calculates the maximum number of characters produced by decoding the given number of bytes.
		/// </summary>
		/// <param name="byteCount">The number of bytes to decode.</param>
		/// <returns>The maximum number of characters produced.</returns>
		public override int GetMaxCharCount(int byteCount) {
			if (byteCount < 0)
				throw new ArgumentOutOfRangeException(nameof(byteCount), "Byte count is out of range.");

			// Compute worst case scenario.
			return byteCount * Math.Max(this.maxCharCount, this.DecoderFallback.MaxCharCount);
		}
	}
}
