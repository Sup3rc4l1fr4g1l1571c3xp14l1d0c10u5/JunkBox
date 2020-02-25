using System;

namespace X86Asm.parser {
    /// <summary>
    /// 字句解析器にpeek(先読み)機能を追加するラッパ
    /// </summary>
    public sealed class BufferedTokenizer : ITokenizer {

        /// <summary>
        /// ラッピング対象の字句解析器
        /// </summary>
        private ITokenizer Tokenizer { get; }

        /// <summary>
        /// 保持されている「次のトークン」
        /// </summary>
        private Token NextToken { get; set; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="tokenizer">ラッピングしたい字句解析器</param>
        public BufferedTokenizer(ITokenizer tokenizer) {
            if (tokenizer == null) {
                throw new ArgumentNullException();
            }
            Tokenizer = tokenizer;
            NextToken = null;
        }

        /// <summary>
        /// 次のトークンを読み取り、結果を返す
        /// </summary>
        /// <returns>読み取り結果を表すトークン</returns>
        public Token Next() {
            if (NextToken != null) {
                // 先読み済みのトークンがあればそれを返す。
                Token result = NextToken;
                NextToken = null;
                return result;
            } else {
                // 先読み済みのトークンが無いなら読み取りを行う。
                return Tokenizer.Next();
            }
        }

        /// <summary>
        /// トークンの先読みを行い、結果を返す。
        /// </summary>
        /// <returns>先読みしたトークン</returns>
        public Token Peek() {
            if (NextToken == null) {
                NextToken = Tokenizer.Next();
            }
            return NextToken;
        }

        /// <summary>
        /// 先読みしたトークンのTokenTypeがtypeと等しいか調べる
        /// </summary>
        /// <param name="type">一致してほしいTokenType</param>
        /// <returns>比較結果</returns>
        public bool Check(TokenType type) {
            return Peek().Type == type;
        }

    }

}