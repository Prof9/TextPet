﻿using LibTextPet.General;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LibTextPet.Text {
	internal class LookupTableDecoder : Decoder {
		private byte[] fallbackBytes;

		private List<byte> Queue { get; }
		private int QueueIndex { get; set; }
		private int CodePointLength { get; set; }
		private string CodePointString { get; set; }

		private LookupTreePath<byte, string> LookupPath { get; }

		public bool Greedy { get; set; }

		public LookupTableDecoder(LookupTree<byte, string> bytesToStringLookup) {
			this.fallbackBytes = new byte[1];

			// Initialize the queue.
			this.Queue = new List<byte>(bytesToStringLookup.Height);
			this.LookupPath = bytesToStringLookup.BeginPath();
			this.ResetSelf();

			this.Greedy = true;
		}

		public override int GetCharCount(byte[] bytes, int index, int count) {
			return this.GetCharCount(bytes, index, count, false);
		}

		public override int GetCharCount(byte[] bytes, int index, int count, bool flush) {
			if (bytes == null)
				throw new ArgumentNullException(nameof(bytes), "The byte array cannot be null.");
			if (index < 0 || index >= bytes.Length)
				throw new ArgumentOutOfRangeException(nameof(index), index, "Index is out of range.");
			if (count < 0 || index + count > bytes.Length)
				throw new ArgumentOutOfRangeException(nameof(count), count, "Count is out of range.");

			// Save the state of the decoder.
			int prevQueueIndex = this.QueueIndex;
			int prevCodeLen = this.CodePointLength;
			string prevCodeStr = this.CodePointString;
			int prevQueueCount = this.Queue.Count;

			// Process every byte in the byte array.
			for (int i = 0; i < count; i++) {
				this.Queue.Add(bytes[index + i]);
			}
			int charCount = this.ProcessQueue(flush, null, 0);

			// Restore the state of the decoder.
			this.Queue.RemoveRange(prevQueueCount, this.Queue.Count - prevQueueCount);
			this.CodePointString = prevCodeStr;
			this.CodePointLength = prevCodeLen;
			this.QueueIndex = prevQueueIndex;

			return charCount;
		}

		public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex) {
			return this.GetChars(bytes, byteIndex, byteCount, chars, charIndex, false);
		}

		public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex, bool flush) {
			if (bytes == null)
				throw new ArgumentNullException(nameof(bytes), "The byte array cannot be null.");
			if (chars == null)
				throw new ArgumentNullException(nameof(chars), "The character array cannot be null.");
			if (byteIndex < 0 || byteIndex >= bytes.Length)
				throw new ArgumentOutOfRangeException(nameof(byteIndex), byteIndex, "Byte index is out of range.");
			if (byteCount < 0 || byteIndex + byteCount > bytes.Length)
				throw new ArgumentOutOfRangeException(nameof(byteCount), byteCount, "Byte count is out of range.");
			if (charIndex < 0)
				throw new ArgumentOutOfRangeException(nameof(charIndex), charIndex, "Character index cannot be negative.");

			// Process every byte in the byte array.
			for (int i = 0; i < byteCount; i++) {
				this.Queue.Add(bytes[byteIndex + i]);
			}

			return this.ProcessQueue(flush, chars, charIndex);
		}

		private void ResetSelf() {
			this.Queue.Clear();
			this.QueueIndex = 0;
			this.CodePointLength = 0;
			this.CodePointString = null;
			this.LookupPath.Reset();
		}

		public override void Reset() {
			this.ResetSelf();
		}

		/// <summary>
		/// Processes the current queue.
		/// </summary>
		/// <param name="flush">If true, the queue will be flushed by forcing fallbacks and repeating until it is empty.</param>
		/// <param name="chars">If set, the array to store the decoded characters into.</param>
		/// <param name="charIndex">The index in chars to start storing decoded characters.</param>
		/// <returns>The amount of characters (partially) decoded from the string.</returns>
		private int ProcessQueue(bool flush, char[] chars, int charIndex) {
			int charCount = 0;

			// If flushing, keep going until the queue is empty.
			do {
				// Process new bytes in the current queue.
				for (int i = this.QueueIndex; i < this.Queue.Count; i++) {
					// If this is the first byte, clear the current code point.
					if (i == 0) {
						this.CodePointLength = 0;
					}

					// Step on next byte in queue.
					if (this.LookupPath.StepNext(this.Queue[i])) {
						// Check if we reached a value.
						if (this.LookupPath.AtValue) {
							// Get the code point.
							this.CodePointString = this.LookupPath.CurrentValue;
							this.CodePointLength = i + 1;
							if (this.Greedy || this.LookupPath.AtEnd) {
								// If greedy or dead end, use the first code point found.
								doCodePoint();
							} else {
								// Otherwise, keep looking.
								continue;
							}
						} else {
							// Did not reach value, but queue is still valid.
							continue;
						}
					} else if (this.CodePointLength > 0) {
						// Could not step on this byte. Use the last read code point.
						doCodePoint();
					} else {
						// Could not step on this byte and no valid code point read.
						doFallback();
					}

					// Reset to start of queue.
					i = -1;
				}

				// At this point, all bytes in queue processed.
				// If there are bytes left in the queue, these are trailing.
				// Continue from current position on next call.
				this.QueueIndex = this.Queue.Count;

				// Trailing bytes that we cannot match.
				// If flushing, have to fallback on the first byte to get rid of it.
				if (flush && this.Queue.Count > 0) {
					// Fallback on first byte.
					doFallback();
					this.QueueIndex = 0;
				}
			} while (flush && this.Queue.Count > 0);

			return charCount;

			void doCodePoint() {
				foreach (char c in this.CodePointString) {
					if (chars != null) {
						chars[charIndex + charCount] = c;
					}
					charCount++;
				}
				// Remove bytes from queue.
				this.Queue.RemoveRange(0, this.CodePointLength);
				// Start next code point.
				this.LookupPath.Reset();
			}
			void doFallback() {
				this.fallbackBytes[0] = this.Queue[0];
				if (this.FallbackBuffer.Fallback(this.fallbackBytes, 0)) {
					// Append all fallback characters.
					while (this.FallbackBuffer.Remaining > 0) {
						if (chars != null) {
							chars[charIndex + charCount] = this.FallbackBuffer.GetNextChar();
						}
						charCount++;
					}
				} else {
					byte[] queueArr = this.Queue.ToArray();
					throw new DecoderFallbackException("Could not decode " + BitConverter.ToString(queueArr) + ".", queueArr, 0);
				}
				this.FallbackBuffer.Reset();
				// Remove byte from queue.
				this.Queue.RemoveAt(0);
				// Start next code point.
				this.LookupPath.Reset();
			}
		}
	}
}
