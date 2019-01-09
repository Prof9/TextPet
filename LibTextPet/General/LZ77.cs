using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LibTextPet.General {
	/// <summary>
	/// A helper class that provides methods for LZ77-compressed data. Does not
	/// support compressing data, but does provide a method to wrap uncompressed
	/// data in an LZ77 data container.
	/// </summary>
	public static class LZ77 {
		/// <summary>
		/// Decompresses LZ77-compressed data from the given input stream to the given output stream.
		/// </summary>
		/// <param name="input">The input stream to read from.</param>
		/// <param name="output">The output stream to write to.</param>
		/// <returns>true if the data in the given input stream was decompressed successfully; otherwise, false.</returns>
		public static bool Decompress(Stream input, Stream output) {
			if (input == null)
				throw new ArgumentNullException(nameof(input), "The input stream cannot be null.");
			if (!input.CanRead)
				throw new ArgumentException("The input stream does not support reading.", nameof(input));

			if (input.Length - input.Position < 4) {
				return false;
			}

			// Create input reader.
			BinaryReader reader = new BinaryReader(input);

			// Check LZ77 type.
			if (input.ReadByte() != 0x10) {
				return false;
			}

			// Read the decompressed size.
			int size = reader.ReadUInt16() | (reader.ReadByte() << 16);

			// Create decompression stream.
			using (MemoryStream temp = new MemoryStream(size)) {
				// Begin decompression.
				while (temp.Length < size) {
					// Load flags for the next 8 blocks.
					int flagByte = input.ReadByte();
					if (flagByte < 0) {
						return false;
					}

					// Process the next 8 blocks unless all data has been decompressed.
					for (int i = 0; i < 8 && temp.Length < size; i++) {
						// Check if the block is compressed.
						if ((flagByte & (0x80 >> i)) == 0) {
							// Uncompressed block; copy single byte.
							int b = input.ReadByte();
							if (b < 0) {
								return false;
							}

							temp.WriteByte((byte)b);
						} else {
							if (input.Length - input.Position < 2) {
								return false;
							}

							// Compressed block; read block.
							ushort block = reader.ReadUInt16();
							// Get byte count.
							int count = ((block >> 4) & 0xF) + 3;
							// Get displacement.
							int disp = ((block & 0xF) << 8) | (block >> 8);

							// Check for invalid displacement.
							if (disp + 1 > temp.Position) {
								return false;
							}

							// Save current position and copying position.
							long outPos = temp.Position;
							long copyPos = temp.Position - disp - 1;

							// Copy all bytes.
							for (int j = 0; j < count; j++) {
								// Read byte to be copied.
								temp.Position = copyPos++;
								int b = temp.ReadByte();

								if (b < 0) {
									return false;
								}

								// Write byte to be copied.
								temp.Position = outPos++;
								temp.WriteByte((byte)b);
							}
						}
					}
				}

				// Write the decompressed data to the output stream.
				temp.WriteTo(output);
				return true;
			}
		}

		/// <summary>
		/// Returns the maximum size after compression for a data stream of a given size.
		/// </summary>
		/// <param name="size">The size of the data stream to be compressed.</param>
		/// <returns>The maximum size of the compressed data stream.</returns>
		public static int GetMaxCompressedSize(int size) {
			// Worst case: all blocks are uncompressed; introduces ceil(size / 8) flag bytes. Round up to multiple of 4.
			return ((size + (size + 7) / 8 + 4) + 3) / 4 * 4;
		}

		/// <summary>
		/// Compresses data from the given input stream to LZ77. This compression is VRAM-safe.
		/// </summary>
		/// <param name="input">The input stream to read from.</param>
		/// <param name="output">The output stream to write to.</param>
		/// <param name="size">The size of the data to compress.</param>
		/// <returns>The size of the compressed data.</returns>
		public static int Compress(Stream input, Stream output, int size)
			=> EncodeLZ77(input, output, size, true);

		/// <summary>
		/// Converts data from the given input stream to an LZ77 data container by wrapping it in uncompressed data blocks.
		/// </summary>
		/// <param name="input">The input stream to read from.</param>
		/// <param name="output">The output stream to write to.</param>
		/// <param name="size">The size of the data to wrap.</param>
		/// <returns>The size of the wrapped data.</returns>
		public static int Wrap(Stream input, Stream output, int size)
			=> EncodeLZ77(input, output, size, false);

		private static int EncodeLZ77(Stream input, Stream output, int size, bool compress) {
			if (input == null)
				throw new ArgumentNullException(nameof(input), "The input stream cannot be null.");
			if (!input.CanRead)
				throw new ArgumentException("The input stream does not support reading.", nameof(input));
			if (size < 0)
				throw new ArgumentOutOfRangeException(nameof(size), "The size cannot be negative.");

			// Create buffer more than twice the size of sliding window, so next part can be read asynchronously.
			byte[] wind = new byte[4096 * 4];
			// Use * 4 to be able to use a mask rather than remainder.
			int windMask = 4096 * 4 - 1;
			// Read first block of window.
			int bytesRead = input.Read(wind, 0, Math.Min(4096 * 1, size));
			int windEnd = bytesRead;

			byte[] bytesToWrite = new byte[1 + 2 * 8];
			int bytesWritten = 0;

			// Start read of next block of window.
			IAsyncResult readRes = null;
			int windReadEnd = windEnd + Math.Min(4096, size - windEnd);
			if (windReadEnd > windEnd) {
				readRes = input.BeginRead(wind, windEnd & windMask, windReadEnd - windEnd, null, input);
			}

			// Write LZ77 data header.
			bytesToWrite[0] = 0x10;
			// Write decompressed size.
			bytesToWrite[1] = (byte)size;
			bytesToWrite[2] = (byte)(size >> 8);
			bytesToWrite[3] = (byte)(size >> 16);
			output.Write(bytesToWrite, 0, 4);
			int compressedSize = 4;

			// Compress data to output stream.
			int inPos = 0;
			int blockNum = 0;
			int flagByte = 0;
			while (inPos < size) {
				// If first block, allocate a flag byte.
				if (blockNum == 0) {
					flagByte = 0;
					bytesWritten = 1;
				}

				// Finish read if we need to read past end of window.
				int prefixLen = Math.Min(size - inPos, 18);
				if (inPos + prefixLen > windEnd) {
					// Finish previous read.
					bytesRead = input.EndRead(readRes);
					windEnd += bytesRead;
					if (windEnd < windReadEnd) {
						throw new IOException("Could not read enough bytes from the input stream.");
					}

					// Start next read.
					windReadEnd += Math.Min(4096, size - windEnd);
					if (windReadEnd > windEnd) {
						readRes = input.BeginRead(wind, windEnd & windMask, windReadEnd - windEnd, null, input);
					}
				}

				// Find longest prefix.
				int maxMatchLen = 0;
				int maxMatchLenDisp = -1;
				if (prefixLen >= 3 && compress) {
					int curMatchLen = 0;
					int curMatchOffset = 0;
					int rewindOffset = -1;
					// searchOffset > 0 if VRAM safety not needed
					for (int searchOffset = Math.Min(inPos, 4096); searchOffset > 1 || curMatchLen > 0; searchOffset--) {
						byte b = wind[(inPos - searchOffset) & windMask];

						// Set rewind offset if possible start of prefix.
						if (curMatchLen > 0 && rewindOffset == -1 && wind[inPos & windMask] == b) {
							rewindOffset = searchOffset;
						}
						
						// Check if byte matches prefix.
						if (wind[(inPos + curMatchLen) & windMask] == b) {
							// Initialize match offset, if needed.
							if (curMatchLen == 0) {
								curMatchOffset = searchOffset;
							}
							if (++curMatchLen == prefixLen) {
								// Cannot match longer than this.
								maxMatchLen = curMatchLen;
								maxMatchLenDisp = curMatchOffset - 1;
								break;
							}
						} else {
							// Set new longest match.
							if (curMatchLen > maxMatchLen) {
								maxMatchLen = curMatchLen;
								maxMatchLenDisp = curMatchOffset - 1;
							}

							// Reset current match.
							curMatchLen = 0;

							// Rewind if possible.
							if (rewindOffset != -1) {
								searchOffset = rewindOffset + 1;
								rewindOffset = -1;
							}
						}
					}
				}

				// If prefix of sufficient length found, use it.
				if (maxMatchLen >= 3) {
					// Mark block as compressed.
					flagByte |= 0x80 >> blockNum;
					int block = ((maxMatchLen - 3) << 12) | maxMatchLenDisp;
					bytesToWrite[bytesWritten++] = (byte)(block >> 8);
					bytesToWrite[bytesWritten++] = (byte)block;
					inPos += maxMatchLen;
				} else {
					// Write uncompressed byte.
					bytesToWrite[bytesWritten++] = (byte)wind[inPos & windMask];
					inPos += 1;
				}

				if (++blockNum >= 8) {
					// Finish current block.
					bytesToWrite[0] = (byte)flagByte;
					output.Write(bytesToWrite, 0, bytesWritten);
					compressedSize += bytesWritten;

					// Reset block number.
					blockNum = 0;
				}
			}
			// Finish current flag byte.
			if (blockNum > 0) {
				// Finish current block.
				bytesToWrite[0] = (byte)flagByte;
				output.Write(bytesToWrite, 0, bytesWritten);
				compressedSize += bytesWritten;
			}
			
			// Pad the output length to a multiple of 4 bytes.
			while (compressedSize % 4 != 0) {
				output.WriteByte(0);
				compressedSize++;
			}

			// Return the compressed data.
			return compressedSize;
		}
	}
}
