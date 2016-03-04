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
		/// Decompresses LZ77-compressed data from the given input stream.
		/// </summary>
		/// <param name="input">The input stream to read from.</param>
		/// <returns>The decompressed data.</returns>
		public static MemoryStream Decompress(Stream input) {
			if (input == null)
				throw new ArgumentNullException(nameof(input), "The input stream cannot be null.");
			if (!input.CanRead)
				throw new ArgumentException("The input stream does not support reading.", nameof(input));

			// Create input reader.
			BinaryReader reader = new BinaryReader(input);

			// Check LZ77 type.
			if (reader.ReadByte() != 0x10)
				throw new ArgumentException("The input stream does not contain LZ77-compressed data.", nameof(input));

			// Read the decompressed size.
			int size = reader.ReadUInt16() | (reader.ReadByte() << 16);

			// Create output stream.
			MemoryStream output = new MemoryStream(size);

			try {
				// Begin decompression.
				while (output.Length < size) {
					// Load flags for the next 8 blocks.
					int flagByte = reader.ReadByte();

					// Process the next 8 blocks unless all data has been decompressed.
					for (int i = 0; i < 8 && output.Length < size; i++) {
						// Check if the block is compressed.
						if ((flagByte & (0x80 >> i)) == 0) {
							// Uncompressed block; copy single byte.
							output.WriteByte(reader.ReadByte());
						} else {
							// Compressed block; read block.
							ushort block = reader.ReadUInt16();
							// Get byte count.
							int count = ((block >> 4) & 0xF) + 3;
							// Get displacement.
							int disp = ((block & 0xF) << 8) | (block >> 8);

							// Save current position and copying position.
							long outPos = output.Position;
							long copyPos = output.Position - disp - 1;

							// Copy all bytes.
							for (int j = 0; j < count; j++) {
								// Read byte to be copied.
								output.Position = copyPos++;
								byte b = (byte)output.ReadByte();

								// Write byte to be copied.
								output.Position = outPos++;
								output.WriteByte(b);
							}
						}
					}
				}

				// Return the decompressed data.
				output.Position = 0;
				return output;
			} catch (Exception) {
				output.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Converts data from the given input stream to an LZ77 data container by wrapping it in uncompressed data blocks.
		/// </summary>
		/// <param name="input">The input stream to read from.</param>
		/// <param name="size">The size of the data to wrap.</param>
		/// <returns>The wrapped data.</returns>
		public static MemoryStream Wrap(Stream input, int size) {
			if (input == null)
				throw new ArgumentNullException(nameof(input), "The input stream cannot be null.");
			if (!input.CanRead)
				throw new ArgumentException("The input stream does not support reading.", nameof(input));
			if (size < 0)
				throw new ArgumentOutOfRangeException(nameof(size), "The size cannot be negative.");

			// Create output stream.
			MemoryStream output = new MemoryStream(size + (size + 7) / 8 + 4);

			try {
				BinaryWriter writer = new BinaryWriter(output);

				// Write LZ77 data header.
				writer.Write((byte)0x10);

				// Write decompressed size.
				writer.Write((ushort)(size & 0xFFFF));
				writer.Write((byte)(size >> 16));

				// Start wrapping.
				while (size > 0) {
					// Write uncompressed data block flags.
					writer.Write((byte)0);

					// Write 8 data blocks unless all data has been written.
					for (int i = 0; i < 8 && size > 0; i++, size--) {
						writer.Write((byte)input.ReadByte());
					}
				}

				// Pad the output length to a multiple of 4 bytes.
				while (output.Length % 4 != 0) {
					writer.Write((byte)0);
				}

				// Return the wrapped data.
				output.Position = 0;
				return output;
			} catch (Exception) {
				output.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Gets the compressed size of LZ77-compressed data from the given input stream.
		/// </summary>
		/// <param name="input">The input stream to read from.</param>
		/// <returns>The compressed size of the data.</returns>
		public static int GetCompressedSize(Stream input) {
			if (input == null)
				throw new ArgumentNullException(nameof(input), "The input stream cannot be null.");
			if (!input.CanRead)
				throw new ArgumentException("The input stream does not support reading.", nameof(input));

			// Create input reader.
			BinaryReader reader = new BinaryReader(input);
			long start = input.Position;

			// Check LZ77 type.
			if (reader.ReadByte() != 0x10)
				throw new ArgumentException("The input stream does not contain LZ77-compressed data.", nameof(input));

			// Read the decompressed size.
			int size = reader.ReadUInt16() | (reader.ReadByte() << 16);

			// Begin processing.
			while (size > 0) {
				// Load flags for the next 8 blocks.
				int flagByte = reader.ReadByte();

				// Process the next 8 blocks unless all data has been processed.
				for (int i = 0; i < 8 && size > 0; i++) {
					// Check if the block is compressed.
					if ((flagByte & (0x80 >> i)) == 0) {
						// Uncompressed block; copy single byte.
						input.Position++;
						size--;
					} else {
						// Compressed block; read block.
						ushort block = reader.ReadUInt16();
						// Get byte count.
						int count = ((block >> 4) & 0xF) + 3;

						size -= count;
					}
				}
			}

			// Return the size of the compressed data.
			return (int)(input.Position - start);
		}
	}
}
