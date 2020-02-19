using System;

namespace X86Asm.parser
{


	/// <summary>
	/// A lexical token. A token has a type and a string of captured text. </summary>
	/// <seealso cref= Tokenizer </seealso>
	public sealed class Token
	{

		/// <summary>
		/// The type of this token. </summary>
		public readonly TokenType type;

		/// <summary>
		/// The text captured by this token. </summary>
		public readonly string text;



		/// <summary>
		/// Constructs a token with the specified type and text. </summary>
		/// <param name="type"> the type </param>
		/// <param name="text"> the captured text </param>
		/// <exception cref="ArgumentNullException"> if the type or the text is {@code null} </exception>
		public Token(TokenType type, string text)
		{
			if (type == TokenType.WHITESPACE || text == null)
			{
				throw new ArgumentNullException();
			}
			this.type = type;
			this.text = text;
		}



		/// <summary>
		/// Compares this token to the specified object for equality. Returns {@code true} if the specified object is a token with the same type and text. Otherwise returns {@code false}. </summary>
		/// <param name="obj"> the object to compare this token against </param>
		/// <returns> {@code true} if the object is a token with the same type and text, {@code false} otherwise </returns>
		public override bool Equals(object obj)
		{
			if (!(obj is Token))
			{
				return false;
			}
			else
			{
				Token other = (Token)obj;
				return type.Equals(other.type) && text.Equals(other.text);
			}
		}


		/// <summary>
		/// Returns the hash code for this token. </summary>
		/// <returns> the hash code for this token. </returns>
		public override int GetHashCode()
		{
			return type.GetHashCode() + text.GetHashCode();
		}


		/// <summary>
		/// Returns a string representation of this token. The format is subjected to change. </summary>
		/// <returns> a string representation of this token </returns>
		public override string ToString()
		{
			return string.Format("[{0} {1}]", type, text);
		}

	}

}