using LibTextPet.General;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace LibTextPet.Text {
	internal class LookupTableEncoder : Encoder {
		private class Path : ICloneable {
			public List<byte> Bytes;
			public List<char> Queue;
			public int QueueIndex;
			public int CodePointLength;
			public byte[] CodePointBytes;
			public LookupTreePath<char, byte[]> LookupPath;
			public bool IsCritical;
			public bool DidFallback;
			public char FallbackChar;

			private Path() {
				this.Bytes = new List<byte>();
				this.Queue = new List<char>();
				this.QueueIndex = 0;
				this.CodePointLength = 0;
				this.IsCritical = false;
				this.DidFallback = false;
			}

			public Path(LookupTree<char, byte[]> lookupTree)
				: this() {
				this.LookupPath = lookupTree.BeginPath();
			}

			public Path Clone() {
				Path clone = new Path() {
					Bytes = new List<byte>(this.Bytes),
					Queue = new List<char>(this.Queue),
					QueueIndex = this.QueueIndex,
					CodePointLength = this.CodePointLength,
					CodePointBytes = new byte[this.CodePointBytes.Length],
					LookupPath = this.LookupPath.Clone(),
					IsCritical = this.IsCritical,
					DidFallback = this.DidFallback,
					FallbackChar = this.FallbackChar
				};
				this.CodePointBytes.CopyTo(clone.CodePointBytes, 0);
				return clone;
			}

			object ICloneable.Clone() {
				return this.Clone();
			}
		}

		private List<Path> prevPaths;
		private char[] byteParseBuffer;

		private LookupTree<char, byte[]> StringToBytesLookup { get; set; }

		private List<Path> Paths { get; set; }

		public bool OptimalPath { get; set; }

		public LookupTableEncoder(LookupTree<char, byte[]> stringToBytesLookup) {
			this.prevPaths = new List<Path>();
			this.byteParseBuffer = new char[2];

			this.StringToBytesLookup = stringToBytesLookup;

			// Initialize the paths.
			this.Paths = new List<Path>();

			this.OptimalPath = false;
		}

		public override int GetByteCount(char[] chars, int index, int count, bool flush) {
			if (chars == null)
				throw new ArgumentNullException(nameof(chars), "The character array cannot be null.");
			if (index < 0 || index >= chars.Length)
				throw new ArgumentOutOfRangeException(nameof(index), "Index is out of range.");
			if (count < 0 || index + count > chars.Length)
				throw new ArgumentOutOfRangeException(nameof(count), "Character count is out of range.");

			if (count == 0) {
				return 0;
			}

			// Save encoder state.
			this.prevPaths.Clear();
			foreach (Path path in this.Paths) {
				// Clone every path.
				prevPaths.Add(path.Clone());
			}

			// Process every char in the char array.
			for (int i = 0; i < count; i++) {
				this.AddToQueue(chars[index + i]);
			}
			int byteCount = this.ProcessPaths(flush, null, 0);

			// Restore encoder state.
			this.Paths.Clear();
			this.Paths.AddRange(this.prevPaths);

			return byteCount;
		}

		public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex, bool flush) {
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

			if (charCount == 0) {
				return 0;
			}

			// Process every char in the char array.
			for (int i = 0; i < charCount; i++) {
				this.AddToQueue(chars[charIndex + i]);
			}
			
			int byteCount = this.ProcessPaths(flush, bytes, byteIndex);

			if (flush) {
				// If flushing, remove all paths.
				this.Paths.Clear();
			}

			return byteCount;
		}

		public override void Reset() {
			this.Paths.Clear();
		}

		private void AddToQueue(char c) {
			// If no paths, spawn initial path.
			if (this.Paths.Count == 0) {
				this.Paths.Add(new Path(this.StringToBytesLookup) {
					IsCritical = true
				});
			}

			foreach (Path path in this.Paths) {
				path.Queue.Add(c);
			}
		}

		private int ProcessPaths(bool flush, byte[] bytes, int byteIndex) {
			int byteCount = 0;

			// Process every path.
			for (int i = 0; i < this.Paths.Count; i++) {
				if (!ProcessQueue(this.Paths[i], flush)) {
					// Dismiss path.
					this.Paths.RemoveAt(i--);
				}
			}

			// If flushing, get the shortest path.
			if (flush) {
				// If flushing, find the shortest path.
				Path shortestPath = this.Paths[0];
				for (int i = 1; i < this.Paths.Count; i++) {
					Path path = this.Paths[i];
					if (path.Bytes.Count < shortestPath.Bytes.Count) {
						shortestPath = path;
					}
				}
				// Return this path.
				foreach (byte b in shortestPath.Bytes) {
					if (bytes != null) {
						bytes[byteIndex + byteCount] = b;
					}
					byteCount++;
				}
			} else {
				// Return longest common prefix.
				while (this.Paths[0].Bytes.Count > 0) {
					bool stop = false;

					// Check if all paths start with same byte.
					byte b = this.Paths[0].Bytes[0];
					for (int i = 1; i < this.Paths.Count; i++) {
						Path path = this.Paths[i];
						if (path.Bytes.Count <= 0 || path.Bytes[0] != b) {
							stop = true;
							break;
						}
					}
					if (stop) {
						break;
					}

					// Return this prefix byte and remove it from all paths.
					if (bytes != null) {
						bytes[byteIndex + byteCount] = b;
						byteCount++;
					}

					foreach (Path path in this.Paths) {
						path.Bytes.RemoveAt(0);
					}
				}
			}

			return byteCount;
		}

		private bool ProcessQueue(Path path, bool flush) {
			// If flushing, keep going until the queue is empty.
			do {
				// Process new chars in the current queue.
				for (int i = path.QueueIndex; i < path.Queue.Count; i++) {
					// If this is the first char, clear the current code point.
					if (i == 0) {
						path.CodePointLength = 0;

						// Check if this is raw byte syntax.
						if (doRawByte()) {
							// Successfully encoded raw byte, continue.
							continue;
						}
					}

					// Step on next char in queue.
					if (path.LookupPath.StepNext(path.Queue[i])) {
						// Check if we reached a value.
						if (path.LookupPath.AtValue) {
							if (this.OptimalPath && !path.LookupPath.AtEnd) {
								// If not at dead end, create a new path where the code point is not applied.
								Path clone = path.Clone();
								clone.IsCritical = false;
								this.Paths.Add(clone);
							}
							// Get the code point.
							path.CodePointBytes = path.LookupPath.CurrentValue;
							path.CodePointLength = i + 1;
							if (!this.OptimalPath) {
								// Apply the code point immediately if using branching paths.
								doCodePoint();
								i = -1;
							}
						} else {
							// Did not reach value, but queue is still valid.
							continue;
						}
					} else if (path.CodePointLength > 0) {
						// Could not step on this char but we have a valid code point.
						doCodePoint();
					} else {
						// Could not step on this char and no valid code point read.
						if (doRawByte()) {
							// This was a raw byte, so continue.
							continue;
						} else if (path.IsCritical) {
							// Path is critical (new code points were read), so do a fallback.
							doFallback();
						} else {
							// Path is not critical path (no new code points read), so dismiss it.
							return false;
						}
					}
				}

				// At this point, all chars in queue processed.
				// If there are chars left in the queue, these are trailing.
				// Continue from current position on next call.
				path.QueueIndex = path.Queue.Count;

				// Trailing chars that we cannot match.
				// If flushing, have to fallback on the first char to get rid of it.
				if (flush && path.Queue.Count > 0) {
					// Fallback on first char.
					doFallback();
					path.QueueIndex = 0;
				}
			} while (flush && path.Queue.Count > 0);

			return true;

			void doCodePoint() {
				// Apply the code point to this path.
				path.Bytes.AddRange(path.CodePointBytes);
				// Remove bytes from queue.
				path.Queue.RemoveRange(0, path.CodePointLength);
				// Start next code point.
				path.LookupPath.Reset();
				// Clear fallback flag.
				path.DidFallback = false;
				// Mark path as critical.
				path.IsCritical = true;
			}
			void doFallback() {
				if (path.DidFallback) {
					// Already tried a fallback and it didn't work.
					throw new EncoderFallbackException("Could not encode '" + path.FallbackChar + "' or fallback '" + path.Queue[0] + "'.");
				} else if (this.FallbackBuffer.Fallback(path.Queue[0], 0)) {
					// Do a fallback, append all fallback characters.
					int i = 1;
					while (this.FallbackBuffer.Remaining > 0) {
						path.Queue.Insert(i++, this.FallbackBuffer.GetNextChar());
					}
					path.FallbackChar = path.Queue[0];
					path.DidFallback = true;
					// Remove char from queue.
					path.Queue.RemoveAt(0);
				} else {
					// Cannot fallback on this.
					throw new EncoderFallbackException("Could not encode '" + path.Queue[0] + "'.");
				}
				// Start next code point.
				path.LookupPath.Reset();
			}
			bool doRawByte() {
				if (path.Queue.Count < 5
					|| path.Queue[0] != '['
					|| path.Queue[1] != '$'
					|| path.Queue[4] != ']') {
					return false;
				}
				this.byteParseBuffer[0] = path.Queue[2];
				this.byteParseBuffer[1] = path.Queue[3];
				if (!byte.TryParse(
					new string(this.byteParseBuffer),
					NumberStyles.AllowHexSpecifier,
					CultureInfo.InvariantCulture,
					out byte b)) {
					return false;
				}
				path.Bytes.Add(b);
				path.Queue.RemoveRange(0, 5);
				// Clear fallback flag.
				path.DidFallback = false;
				// Mark path as critical.
				path.IsCritical = true;
				return true;
			}
		}
	}
}
