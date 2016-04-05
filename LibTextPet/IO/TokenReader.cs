using LibTextPet.General;
using LibTextPet.Msg;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LibTextPet.IO {
	/// <summary>
	/// A token reader that reads string tokens from an input stream and parses them as the specified type.
	/// </summary>
	/// <typeparam name="T">The type of object read by this reader.</typeparam>
	public abstract class TokenReader<T> : Manager, IReader<IList<T>> {
		/// <summary>
		/// A value that indicates the result of processing a single token.
		/// </summary>
		protected struct ProcessResult {
			internal static readonly ProcessResult ConsumeAndContinue = new ProcessResult(true, true);
			internal static readonly ProcessResult ConsumeAndStop = new ProcessResult(true, false);
			internal static readonly ProcessResult Continue = new ProcessResult(false, true);
			internal static readonly ProcessResult Stop = new ProcessResult(false, false);

			/// <summary>
			/// Gets or sets the modified object.
			/// </summary>
			private T modifiedObject;

			/// <summary>
			/// Gets the modified object.
			/// </summary>
			public T ModifiedObject {
				get {
					if (this.Modified) {
						return this.modifiedObject;
					} else {
						throw new InvalidOperationException("This process result does not contain a modified object.");
					}
				}
			}

			/// <summary>
			/// Gets a boolean that indicates whether the provided object was modified.
			/// </summary>
			public bool Modified { get; private set; }

			/// <summary>
			/// Gets a boolean that indicates whether the provided token was consumed.
			/// </summary>
			public bool Consumed { get; private set; }

			/// <summary>
			/// Gets a boolean that indicates whether token reading should continue.
			/// </summary>
			public bool ToContinue { get; private set; }

			/// <summary>
			/// Creates new processing results with no modified object and the specified results.
			/// </summary>
			/// <param name="consumed">Whether the provided token was consumed.</param>
			/// <param name="toContinue">Whether token reading should continue.</param>
			private ProcessResult(bool consumed, bool toContinue) {
				this.modifiedObject = default(T);
				this.Modified = false;
				this.Consumed = consumed;
				this.ToContinue = toContinue;
			}

			/// <summary>
			/// Creates new processing results with the specified modified object and the specified results.
			/// </summary>
			/// <param name="obj">The modified object.</param>
			/// <param name="consumed">Whether the provided token was consumed.</param>
			/// <param name="toContinue">Whether token reading should continue.</param>
			public ProcessResult(T obj, bool consumed, bool toContinue) {
				this.modifiedObject = obj;
				this.Modified = true;
				this.Consumed = consumed;
				this.ToContinue = toContinue;
			}

			public override bool Equals(object obj) {
				if (obj == null || GetType() != obj.GetType())
					return false;

				ProcessResult result = (ProcessResult)obj;

				if (this.Consumed == result.Consumed && this.ToContinue == result.ToContinue) {
					if (this.Modified) {
						return this.modifiedObject.Equals(result.modifiedObject);
					} else {
						return !result.Modified;
					}
				} else {
					return false;
				}
			}

			public override int GetHashCode() {
				int hash = 17;
				hash += hash * 23 + this.Modified.GetHashCode();
				hash += hash * 23 + this.Consumed.GetHashCode();
				hash += hash * 23 + this.ToContinue.GetHashCode();
				return hash ^ this.modifiedObject.GetHashCode();
			}

			public static bool operator ==(ProcessResult result1, ProcessResult result2) {
				if (ReferenceEquals(result1, result2)) {
					return true;
				} else if (ReferenceEquals(result1, null) || ReferenceEquals(result2, null)) {
					return false;
				} else {
					return result1.Equals(result2);
				}
			}

			public static bool operator !=(ProcessResult result1, ProcessResult result2) {
				return !(result1 == result2);
			}
		}

		/// <summary>
		/// Gets the stream reader that is used to read text from the input stream.
		/// </summary>
		protected StreamReader TextReader { get; private set; }

		/// <summary>
		/// Gets or sets the token enumerator that is currently being read from.
		/// </summary>
		private IEnumerator<Token> TokenEnumerator { get; set; }

		/// <summary>
		/// Gets a boolean that indicates whether this token reader has consumed the current token.
		/// </summary>
		public bool Consumed { get; private set; }

		/// <summary>
		/// Gets the last token that was read by this token reader.
		/// </summary>
		public Token Current {
			get {
				return this.TokenEnumerator.Current;
			}
		}

		/// <summary>
		/// Creates a new token reader that reads from the specified input stream, using the specified command databases.
		/// </summary>
		/// <param name="stream">The stream to read from.</param>
		/// <param name="databases">The command databases to use. At least one command database must be specified.</param>
		protected TokenReader(Stream stream, params CommandDatabase[] databases)
			: base(stream, true, FileAccess.Read, databases) {
			if (databases == null)
				throw new ArgumentNullException(nameof(databases), "The command databases cannot be null.");

			if (databases.Length < 1)
				throw new ArgumentException("At least one command database must be provided.", nameof(databases));

			this.TextReader = new StreamReader(stream, new UTF8Encoding(false, true));
		}

		/// <summary>
		/// Reads tokenized objects from the input stream.
		/// </summary>
		/// <returns>The objects that were read.</returns>
		public IList<T> Read() {
			return Read(this.TextReader.ReadToEnd());
		}

		/// <summary>
		/// Reads tokenized objects from the given text.
		/// </summary>
		/// <param name="fullText">The text to read from.</param>
		/// <returns>The objects that were read.</returns>
		protected internal IList<T> Read(string fullText) {
			if (fullText == null)
				throw new ArgumentNullException(fullText, "The full text cannot be null.");

			// Tokenize the full text.
			IList<Token> tokens = Tokenize(fullText).ToList();

			return Read(tokens);
		}

		/// <summary>
		/// Reads multiple tokenized objects from the given tokens.
		/// </summary>
		/// <param name="tokens">The tokens to read from.</param>
		/// <returns>The objects that were read.</returns>
		protected internal IList<T> Read(IList<Token> tokens) {
			if (tokens == null)
				throw new ArgumentNullException(nameof(tokens), "The tokens cannot be null.");

			List<T> readObjects = new List<T>();
			while (tokens.Any()) {
				// Read objects until no tokens are left.
				readObjects.Add(ReadSingle(tokens));

				// Get the remaining tokens.
				List<Token> remaining = new List<Token>(tokens.Count);
				while (!this.Consumed || this.TokenEnumerator.MoveNext()) {
					remaining.Add(this.TokenEnumerator.Current);
					this.Consumed = true;
				}
				tokens = remaining;
			}
			return readObjects;
		}

		/// <summary>
		/// Reads a single tokenized object from the input stream. If no objects or read or more than one object was read, an exception is thrown.
		/// </summary>
		/// <returns>The object that was read.</returns>
		public T ReadSingle() {
			IList<T> readObjects = this.Read();
			if (readObjects.Count != 1) {
				throw new InvalidDataException("Expected exactly 1 tokenized " + nameof(T) + " but read " + readObjects.Count + ".");
			}
			return readObjects[0];
		}

		/// <summary>
		/// Reads tokenized objects from the given tokens.
		/// </summary>
		/// <param name="tokens">The tokens to read from.</param>
		/// <returns>The object that was read.</returns>
		protected internal T ReadSingle(IList<Token> tokens) {
			if (tokens == null)
				throw new ArgumentNullException(nameof(tokens), "The tokens cannot be null.");

			// Read text archives using each database until success.
			object result = null;
			List<Token> failedTokens = new List<Token>(this.Databases.Count);
			IEnumerator<Token> tokenEnumerator = null;

			foreach (CommandDatabase db in this.Databases) {
				// Get the token enumerator.
				tokenEnumerator = tokens.GetEnumerator();

				result = Read(tokenEnumerator, db, true);

				if (result != null) {
					break;
				}

				failedTokens.Add(tokenEnumerator.Current);
			}

			if (result == null) {
				// Find out what token(s) we failed on.
				bool allMatched = true;
				foreach (Token failedToken in failedTokens) {
					if (failedToken != failedTokens[0]) {
						allMatched = false;
						break;
					}
				}

				if (allMatched) {
					throw new FormatException("Could not parse token \"" + failedTokens[0].Value + "\" (all).");
				} else {
					StringBuilder builder = new StringBuilder();
					builder.Append("Could not parse token");
					bool first = true;
					for (int i = 0; i < failedTokens.Count; i++) {
						if (first) {
							builder.Append(',');
							first = false;
						}
						builder.Append(" \"" + failedTokens[i].Value + "\" (" + this.Databases[i].Name + ")");
					}
					builder.Append('.');
					throw new FormatException(builder.ToString());
				}
			}

			return (T)result;
		}

		/// <summary>
		/// Reads a tokenized object from the specified token enumerator.
		/// </summary>
		/// <param name="te">The token enumerator to read from.</param>
		/// /// <param name="db">The command database to use.</param>
		/// <param name="moveNext">Whether the current token has been consumed yet.</param>
		/// <returns>The object that was read.</returns>
		private object Read(IEnumerator<Token> te, CommandDatabase db, bool moveNext) {
			if (te == null)
				throw new ArgumentNullException(nameof(te), "The token enumerator cannot be null.");

			this.TokenEnumerator = te;

			T t = BeginRead();

			this.Consumed = moveNext;
			while (!this.Consumed || this.TokenEnumerator.MoveNext()) {
				ProcessResult result = ProcessToken(t, this.TokenEnumerator.Current, db);
				// Was the object modified?
				if (result.Modified) {
					t = result.ModifiedObject;
				}
				// Was the token consumed?
				this.Consumed = result.Consumed;
				// Should we continue?
				if (!result.ToContinue) {
					break;
				}
			}

			return EndRead(t, db);
		}

		/// <summary>
		/// Reads a tokenized object from this token reader using the token enumerator provided by the specified parent reader.
		/// </summary>
		/// <typeparam name="TParent">The type of the parent token reader.</typeparam>
		/// <param name="parent">The parent token reader.</param>
		/// <param name="db">The command database to use.</param>
		/// <param name="moveNext">Whether the current token has been consumed yet.</param>
		/// <returns>The object that was read.</returns>
		protected internal T SubRead<TParent>(TokenReader<TParent> parent, CommandDatabase db, bool moveNext) {
			if (parent == null)
				throw new ArgumentNullException(nameof(parent), "The parent token reader cannot be null.");

			return (T)this.Read(parent.TokenEnumerator, db, moveNext);
		}

		/// <summary>
		/// When overridden in a derived class, initializes an object to be read from the token enumerator.
		/// </summary>
		/// <returns>The initialized object.</returns>
		protected abstract T BeginRead();

		/// <summary>
		/// When overridden in a derived class, processes the specified token for the specified object.
		/// </summary>
		/// <param name="obj">The object to modify.</param>
		/// <param name="token">The token to process.</param>
		/// <param name="db">The command database to use.</param>
		/// <returns>A result value that indicates whether the token was consumed, and whether to continue reading.</returns>
		protected abstract ProcessResult ProcessToken(T obj, Token token, CommandDatabase db);

		/// <summary>
		/// When overridden in a derived class, finalized the object that was read.
		/// </summary>
		/// <param name="obj">The object that was read.</param>
		/// <param name="db">The command database that was used.</param>
		/// <returns>The finalized object.</returns>
		protected virtual T EndRead(T obj, CommandDatabase db) {
			return obj;
		}

		/// <summary>
		/// When overridden in a derived class, extracts tokens from the specified full text.
		/// </summary>
		/// <param name="fullText">The full text to extract tokens from.</param>
		/// <returns>The extracted tokens.</returns>
		protected abstract IEnumerable<Token> Tokenize(string fullText);

		/// <summary>
		/// Reads a string from the current token enumerator, throwing an exception if it does not have one of the specified types.
		/// </summary>
		/// <param name="types">The type of tokens that are expected, or null or empty to accept any token.</param>
		/// <returns>The read string.</returns>
		protected string ReadString(params int[] types) {
			return ReadToken(types).Value;
		}

		/// <summary>
		/// Reads an integer from the current token enumerator, throwing an exception if it does not have one of the specified types.
		/// </summary>
		/// <param name="types">The type of tokens that are expected, or null or empty to accept any token.</param>
		/// <returns>The integer that was read.</returns>
		protected long ReadNumber(params int[] types) {
			return NumberParser.ParseInt64(ReadToken(types).Value);
		}

		/// <summary>
		/// Reads a token from the current token enumerator, throwing an exception if it does not have one of the specified types.
		/// </summary>
		/// <param name="types">The type of tokens that are expected, or null or empty to accept any token.</param>
		/// <returns>The read token.</returns>
		protected Token ReadToken(params int[] types) {
			if (!this.TokenEnumerator.MoveNext()) {
				throw new InvalidDataException("Unexpected end of file while reading tokens.");
			}
			Token token = this.TokenEnumerator.Current;

			this.Consumed = false;

			if (types != null && types.Length > 0 && !types.Contains((int)token.Class)) {
				throw new InvalidDataException("Unexpected token of type \"" + types.ToString() + "\".");
			}
			return this.TokenEnumerator.Current;
		}
	}
}
