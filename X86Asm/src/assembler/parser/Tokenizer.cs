namespace X86Asm.parser
{


	/// <summary>
	/// A stream of <seealso cref="Token"/>s.
	/// </summary>
	public abstract class Tokenizer
	{

		/// <summary>
		/// Returns the next token from this tokenizer. This is never {@code null} because there is an end-of-file token type. </summary>
		/// <returns> the next token from this tokenizer </returns>
		public abstract Token next();

	}

}