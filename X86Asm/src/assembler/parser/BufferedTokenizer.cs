using System;

namespace X86Asm.parser {
    /// <summary>
    /// Decorates a tokenizer with peeking capabilities.
    /// </summary>
    public sealed class BufferedTokenizer : Tokenizer {
        /// <summary>
        /// The underlying tokenizer, which provides the tokens.
        /// </summary>
        private Tokenizer Tokenizer { get; }

        /// <summary>
        /// The next token. The peek() method saves the token here. It is cleared to {@code null} when next() is called.
        /// </summary>
        private Token NextToken { get; set; }

        /// <summary>
        /// Constructs a buffered tokenizer from the specified tokenizer. </summary>
        /// <param name="tokenizer"> the underlying tokenizer providing the tokens </param>
        public BufferedTokenizer(Tokenizer tokenizer) {
            if (tokenizer == null) {
                throw new ArgumentNullException();
            }
            Tokenizer = tokenizer;
            NextToken = null;
        }



        /// <summary>
        /// Returns the next token from this tokenizer. </summary>
        /// <returns> the next token from this tokenizer </returns>
        public override Token Next() {
            if (NextToken != null) {
                Token result = NextToken;
                NextToken = null;
                return result;
            } else {
                return Tokenizer.Next();
            }
        }

        /// <summary>
        /// Returns the next token without consuming it. Consecutive calls to this method return the same value. </summary>
        /// <returns> the next token </returns>
        public Token Peek() {
            if (NextToken == null) {
                NextToken = Tokenizer.Next();
            }
            return NextToken;
        }

        /// <summary>
        /// Peeks at the next token and tests whether it has the specified type. Returns {@code peek().type == type}. </summary>
        /// <param name="type"> the type to test against </param>
        /// <returns> {@code true} if the next token (without consuming it) has the specified type, {@code false} otherwise </returns>
        public bool Check(TokenType type) {
            return Peek().type == type;
        }

    }

}