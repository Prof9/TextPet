using LibTextPet.General;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace LibTextPet.Text2 {
	/// <summary>
	/// Represents an encoding that can map a single byte to multiple characters.
	/// </summary>
	public class LookupTableEncoding : Encoding {
		private LookupTree<byte, string> BytesToStringLookup { get; }
		private LookupTree<char, byte[]> StringToBytesLookup { get; }

		private LookupTableDecoder _commonDecoder;
		internal LookupTableDecoder CommonDecoder {
			get {
				// Only create the decoder one time, since we always flush it anyway.
				if (this._commonDecoder == null) {
					// Use private method, GetDecoder may be overridden.
					this._commonDecoder = this.MakeDecoder();
				}
				return this._commonDecoder;
			}
		}
		private LookupTableEncoder _commonEncoder;
		internal LookupTableEncoder CommonEncoder {
			get {
				// Only create the decoder one time, since we always flush it anyway.
				if (this._commonEncoder == null) {
					// Use private method, GetDecoder may be overridden.
					this._commonEncoder = this.MakeEncoder();
				}
				return this._commonEncoder;
			}
		}

		public int MaxBytesPerCodePoint => this.BytesToStringLookup.Height;
		public int MaxCharsPerCodePoint => this.StringToBytesLookup.Height;

		private LookupTableDecoder MakeDecoder() {
			return new LookupTableDecoder(this.BytesToStringLookup) {
				Fallback = this.DecoderFallback
			};
		}
		private LookupTableEncoder MakeEncoder() {
			return new LookupTableEncoder(this.StringToBytesLookup) {
				Fallback = this.EncoderFallback
			};
		}
		public override Decoder GetDecoder() {
			return this.MakeDecoder();
		}
		public override Encoder GetEncoder() {
			return this.MakeEncoder();
		}

		public override int GetByteCount(char[] chars, int index, int count) {
			// Use the encoder.
			Encoder encoder;
			if (this.IsReadOnly) {
				encoder = this.CommonEncoder;
			} else {
				// This might be kinda slow???
				encoder = this.MakeEncoder();
			}
			return encoder.GetByteCount(chars, index, count, true);
		}

		public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex) {
			// Use the encoder.
			Encoder encoder;
			if (this.IsReadOnly) {
				encoder = this.CommonEncoder;
			} else {
				// This might be kinda slow???
				encoder = this.MakeEncoder();
			}
			return encoder.GetBytes(chars, charIndex, charCount, bytes, byteIndex, true);
		}

		public override int GetCharCount(byte[] bytes, int index, int count) {
			// Use the decoder.
			Decoder decoder;
			if (this.IsReadOnly) {
				decoder = this.CommonDecoder;
			} else {
				// This might be kinda slow???
				decoder = this.MakeDecoder();
			}
			return decoder.GetCharCount(bytes, index, count, true);
		}

		public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex) {
			// Use the decoder.
			Decoder decoder;
			if (this.IsReadOnly) {
				decoder = this.CommonDecoder;
			} else {
				// This might be kinda slow???
				decoder = this.MakeDecoder();
			}
			return decoder.GetChars(bytes, byteIndex, byteCount, chars, charIndex, true);
		}

		public override int GetMaxByteCount(int charCount) {
			return charCount * this.MaxBytesPerCodePoint;
		}

		public override int GetMaxCharCount(int byteCount) {
			return byteCount * this.MaxCharsPerCodePoint;
		}
	}
}
