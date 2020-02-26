using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace X86Asm.parser {

    /// <summary>
    /// 空白要素以外のトークンを読み取る字句解析器
    /// </summary>
    public sealed class StringTokenizer : ITokenizer {

        /// <summary>
        /// トークンのパターン
        /// </summary>
        private class TokenPattern {

            /// <summary>
            /// トークンの書式を表す正規表現
            /// </summary>
            public Regex Pattern { get; }

            /// <summary>
            /// トークンの種別
            /// </summary>
            public TokenType TokenType { get; }

            /// <summary>
            /// コンストラクタ
            /// </summary>
            /// <param name="pattern">トークンの書式</param>
            /// <param name="tokenType">トークンの種別</param>
            public TokenPattern(string pattern, TokenType tokenType) {
                if (pattern == null) {
                    throw new ArgumentNullException(nameof(pattern));
                }
                this.Pattern = new Regex(pattern);
                this.TokenType = tokenType;
            }

            /// <summary>
            /// このオブジェクトの文字列表現を返す
            /// </summary>
            /// <returns>文字列表現</returns>
            public override string ToString() {
                return $"[{TokenType}: /{Pattern}/]";
            }

        }

        /// <summary>
        /// トークンパターン表
        /// </summary>
        private static TokenPattern[] Patterns = new TokenPattern[] {
            new TokenPattern("[ \t]+", TokenType.WHITESPACE), // Whitespace
            new TokenPattern("[A-Za-z_][A-Za-z0-9_]*:", TokenType.LABEL),
            new TokenPattern("[A-Za-z_][A-Za-z0-9_]*", TokenType.NAME),
            new TokenPattern("\\.[A-Za-z_][A-Za-z0-9_]*", TokenType.DIRECTIVE),
            new TokenPattern("%[A-Za-z][A-Za-z0-9_]*", TokenType.REGISTER),
            new TokenPattern("0[xX][0-9a-fA-F]+", TokenType.HEXADECIMAL),
            new TokenPattern("-?[0-9]+", TokenType.DECIMAL),
            new TokenPattern("\\$", TokenType.DOLLAR),
            new TokenPattern(",", TokenType.COMMA),
            new TokenPattern("\\+", TokenType.PLUS),
            new TokenPattern("-", TokenType.MINUS),
            new TokenPattern("\\(", TokenType.LEFT_PAREN),
            new TokenPattern("\\)", TokenType.RIGHT_PAREN),
            new TokenPattern("[\n\r]+", TokenType.NEWLINE),
            new TokenPattern("#[^\n\r]*", TokenType.WHITESPACE), // Comment
            new TokenPattern("$", TokenType.END_OF_FILE),
        };

        /// <summary>
        /// 文字列表現
        /// </summary>
        private string SourceCode { get; }

        /// <summary>
        /// 読み取り位置オフセット
        /// </summary>
        private int Offset { get; set; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="file">入力元ストリーム</param>
        public StringTokenizer(Stream file) {
            if (file == null) {
                throw new ArgumentNullException(nameof(file));
            }
            using (var reader = new StreamReader(file)) {
                SourceCode = reader.ReadToEnd();
                Offset = 0;
            }
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="file">入力元文字列</param>
        public StringTokenizer(string sourceCode) {
            if (sourceCode == null) {
                throw new ArgumentNullException(nameof(sourceCode));
            }
            this.SourceCode = sourceCode;
            Offset = 0;
        }

        /// <summary>
        /// 次のトークンを読み取り、結果を返す
        /// </summary>
        /// <returns>読み取り結果を表すトークン</returns>
        public Token Next() {
            Retry:
            foreach (TokenPattern pat in Patterns) {
                var m = pat.Pattern.Match(SourceCode, Offset);
                if (m.Success && m.Index == Offset) {
                    Offset += m.Value.Length;
                    if (pat.TokenType != TokenType.WHITESPACE) {
                        // 空白要素でなければトークンを構築する
                        return new Token(pat.TokenType, m.Value);
                    } else {
                        // 空白要素なので読み飛ばす
                        goto Retry;
                    }
                }
            }
            throw new Exception("パターンに一致するトークンがありません。");
        }

    }

}