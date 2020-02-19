using System;

namespace X86Asm.parser
{


	/// <summary>
	/// Decorates a tokenizer with peeking capabilities.
	/// </summary>
	public sealed class BufferedTokenizer : Tokenizer
	{

		/// <summary>
		/// The underlying tokenizer, which provides the tokens.
		/// </summary>
		private Tokenizer tokenizer;

		/// <summary>
		/// The next token. The peek() method saves the token here. It is cleared to {@code null} when next() is called.
		/// </summary>
		private Token nextToken;



		/// <summary>
		/// Constructs a buffered tokenizer from the specified tokenizer. </summary>
		/// <param name="tokenizer"> the underlying tokenizer providing the tokens </param>
		public BufferedTokenizer(Tokenizer tokenizer)
		{
			if (tokenizer == null)
			{
				throw new ArgumentNullException();
			}
			this.tokenizer = tokenizer;
			nextToken = null;
		}



		/// <summary>
		/// Returns the next token from this tokenizer. </summary>
		/// <returns> the next token from this tokenizer </returns>
		public override Token next()
		{
			if (nextToken != null)
			{
				Token result = nextToken;
				nextToken = null;
				return result;
			}
			else
			{
				return tokenizer.next();
			}
		}


		/// <summary>
		/// Returns the next token without consuming it. Consecutive calls to this method return the same value. </summary>
		/// <returns> the next token </returns>
		public Token peek()
		{
			if (nextToken == null)
			{
				nextToken = tokenizer.next();
			}
			return nextToken;
		}


		/// <summary>
		/// Peeks at the next token and tests whether it has the specified type. Returns {@code peek().type == type}. </summary>
		/// <param name="type"> the type to test against </param>
		/// <returns> {@code true} if the next token (without consuming it) has the specified type, {@code false} otherwise </returns>
		public bool check(TokenType type)
		{
			return peek().type == type;
		}

	}

}