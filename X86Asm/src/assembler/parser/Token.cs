using System;

namespace X86Asm.parser {

    /// <summary>
    /// 字句解析器が返すトークン
    /// </summary>
    public sealed class Token {

        /// <summary>
        /// トークンの種別
        /// </summary>
        public TokenType Type { get; }

        /// <summary>
        /// トークンの文字列
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="type">トークンの種別</param>
        /// <param name="text">トークンの文字列</param>
        public Token(TokenType type, string text) {
            if (text == null) {
                throw new ArgumentNullException(nameof(text));
            }
            if (type == TokenType.WHITESPACE) {
                throw new ArgumentException(nameof(type));
            }
            this.Type = type;
            this.Text = text;
        }

        /// <summary>
        /// 比較処理
        /// </summary>
        /// <param name="obj">比較対象</param>
        /// <returns>トークン種別とトークン文字列が一致すれば真</returns>
        public override bool Equals(object obj) {
            if (!(obj is Token)) {
                return false;
            } else {
                Token other = (Token)obj;
                return Type.Equals(other.Type) && Text.Equals(other.Text);
            }
        }

        /// <summary>
        /// このオブジェクトのハッシュ値を返す
        /// </summary>
        /// <returns>ハッシュ値</returns>
        public override int GetHashCode() {
            return Type.GetHashCode() + Text.GetHashCode();
        }

        /// <summary>
        /// このオブジェクトの文字列表現を返す
        /// </summary>
        /// <returns>文字列表現</returns>
        public override string ToString() {
            return string.Format("[{0} {1}]", Type, Text);
        }

    }

}