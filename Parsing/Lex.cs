using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CParser2
{
    public class Lex
    {

        /// <summary>
        /// Line指令を無視するか否か
        /// </summary>
        public static bool IgnoreLineDirective = false;

        /// <summary>
        /// 字句解析器で読み取ったトークン
        /// </summary>
        private class Token
        {
            public Token Next;	/// トークンの先読みで使うリンクリスト
            public TokenId TokenId = TokenId.Eof;
            public int LineNumber;
            public string FileName = "";
            public long LongValue;
            public double DoubleValue;
            public string TextValue = "";
        }

        /// <summary>
        /// 最後に読み取った文字
        /// </summary>
        private int _lastChar { get; set; }

        private const int EoF = -1;

        /// <summary>
        /// 最後に読み取った字句ＩＤ（Ｃ言語の構文を構成するアルファベットに対応）
        /// </summary>
        private TokenId _lastTokenId { get; set; }

        /// <summary>
        /// 現在の行番号
        /// </summary>
        private int _lineNumber { get; set; }

        /// <summary>
        /// 現在のファイル名
        /// </summary>
        private String _fileName { get; set; }

        /// <summary>
        /// 字句解析中のトークン
        /// </summary>
        private Token _currentToken { get; set; }

        /// <summary>
        /// 先読み中のトークン
        /// </summary>
        private Token _lookAheadTokens { get; set; }

        /// 予約語変換テーブル
        private static readonly Dictionary<string, TokenId> KeywordTable = new Dictionary<string, TokenId> {
            {"__builtin_va_list", TokenId.Int},
            {"asm", TokenId.Asm},
            {"auto", TokenId.Auto},
            {"break", TokenId.Break},
            {"case", TokenId.Case},
            {"char", TokenId.Char},
            {"const", TokenId.Const},
            {"continue", TokenId.Continue},
            {"default", TokenId.Default},
            {"do", TokenId.Do},
            {"double", TokenId.Double},
            {"else", TokenId.Else},
            {"enum", TokenId.Enum},
            {"extern", TokenId.Extern},
            {"float", TokenId.Float},
            {"for", TokenId.For},
            {"goto", TokenId.Goto},
            {"if", TokenId.If},
            {"inline", TokenId.Inline},
            {"int", TokenId.Int},
            {"long", TokenId.Long},
            {"register", TokenId.Register},
            {"restrict", TokenId.Restrict},
            {"return", TokenId.Return},
            {"short", TokenId.Short},
            {"signed", TokenId.Signed},
            {"sizeof", TokenId.Sizeof},
            {"static", TokenId.Static},
            {"struct", TokenId.Struct},
            {"switch", TokenId.Switch},
            {"typedef", TokenId.Typedef},
            {"union", TokenId.Union},
            {"unsigned", TokenId.Unsigned},
            {"void", TokenId.Void},
            {"volatile", TokenId.Volatile},
            {"while", TokenId.While}
        };

        /// <summary>
        /// 字句解析器コンストラクタ
        /// </summary>
        /// <param name="fileName">初期設定となるファイル名</param>
        protected Lex(string fileName)
        {
            _lastChar = EoF;
            _lastTokenId = TokenId.Lf;
            _lineNumber = 1;
            _fileName = String.IsNullOrEmpty(fileName) ? "unknown" : fileName;
            _currentToken = new Token();
            _lookAheadTokens = null;
        }

        /// <summary>
        /// トークンを一つ読み取り、現在のトークンに設定する
        /// </summary>
        /// <returns>読み取ったトークンの種別</returns>
        public TokenId GetToken()
        {
            if (_lookAheadTokens != null)
            {
                // 先読みしたトークンがある場合
                Token t;
                _currentToken = t = _lookAheadTokens;
                _lookAheadTokens = _lookAheadTokens.Next;
                return t.TokenId;
            }
            else
            {
                // 先読みしたトークンがない場合は読み込む
                Token t = new Token();
                var ret = GetToken(ref t);
                _currentToken = t;
                return ret.TokenId;
            }
        }


        /// <summary>
        /// トークンを読み取るが、現在のトークンに設定しない
        /// </summary>
        /// <param name="token">読み取ったトークンの情報</param>
        /// <returns>読み取ったトークンの種別</returns>
        private Token GetToken(ref Token token)
        {
            Token t;
            do
            {
                t = ReadToken();
                _lastTokenId = t.TokenId;
                switch (t.TokenId)
                {
                    case TokenId.SkipGccAsm:
                        t = new Token() {TokenId = TokenId.Ignore};
                        SkipAsm();
                        break;
                    case TokenId.SkipGccAttribute:
                        t = new Token() { TokenId = TokenId.Ignore };
                        SkipAsm();
                        break;
                    case TokenId.Asm:
                        token.TextValue = "asm";
                        break;
                }
            } while ((t.TokenId == TokenId.Lf) || (t.TokenId == TokenId.Ignore));
            token.LineNumber = _lineNumber;
            token.FileName = _fileName;
            return t;
        }

        /// <summary>
        /// gcc形式のasm文を読み飛ばす
        /// </summary>
        private void SkipAsm()
        {
            int c;
            do
            {
                c = GetCh();
            } while ((c != -1) && (c != '('));
            int i = 1;
            do
            {
                c = GetCh();
                if (c == '(')
                {
                    ++i;
                }
                else if (c == ')')
                {
                    --i;
                }
                else if (c == -1)
                {
                    break;
                }
            } while (i > 0);
        }
        /// <summary>
        /// トークンを一つ読み取る。
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private Token ReadToken()
        {
            int c = GetNextNonWhiteChar();
            if (c < 0) {
                return new Token() {TokenId = (TokenId)c};  // EOF?
            }
            if (c == '\n')
            {
                _lineNumber++;
                return new Token() { TokenId = TokenId.Lf };
            }
            if (c == '#' && _lastTokenId == TokenId.Lf)
            {
                // 行頭にあるプラグマ以外のプリプロセッサ指令は改行として扱う
                Token tok = ReadLineDirective();
                if (tok.TokenId == TokenId.Pragma)
                {
                    return tok;
                }
                return new Token() {TokenId=TokenId.Lf};
            }
            if (c == '\'')
            {
                return ReadCharConst();
            }
            if (c == '"')
            {
                return ReadStringL();
            }
            if ('0' <= c && c <= '9')
            {
                return ReadNumber(c);
            }
            if (c == '.')
            {
                c = GetCh();
                if ('0' <= c && c <= '9')
                {
                    // .1234 のような整数部省略記法の場合
                    var tbuf = new StringBuilder(".");
                    return ReadDouble(tbuf, c);
                }
                UngetCh(c);
                return ReadSeparator('.');
            }
            if (c == 'L')
            {
                int c2 = GetCh();
                if (c2 == '"')
                {
                    // ワイド文字列定数値
                    return ReadStringWl();
                }
                // Lから始まる識別子
                UngetCh(c2);
                return ReadIdentifier(c);
            }
            if ('A' <= c && c <= 'Z' || 'a' <= c && c <= 'z' || c == '_' || c == '$')
            {
                return ReadIdentifier(c);
            }
            return ReadSeparator(c);
        }

        /// <summary>
        /// 空白文字をスキップして１文字読み取る
        /// </summary>
        /// <returns>読み取った文字</returns>
        private int GetNextNonWhiteChar()
        {
            int c;
            for (; ; )
            {
                // 空白文字をスキップ
                do
                {
                    c = GetCh();
                } while (IsBlank(c));

                // コメント文の恐れ有り
                if (c == '/')
                {

                    int c2 = GetCh();
                    if (c2 == '/')
                    {
                        // 行コメント
                        // 行末まで読み飛ばす
                        do
                        {
                            c = GetCh();
                            if (c == '\\')
                            {
                                c = GetCh();
                                if (c == '\r')
                                {
                                    c = GetCh();
                                    if (c != '\n')
                                    {
                                        UngetCh(c);
                                    }
                                    _lineNumber++;
                                    c = ' ';   // dummy
                                }
                                else if (c == '\n')
                                {
                                    _lineNumber++;
                                    c = ' ';    // dummy
                                }
                            }
                        } while ((c != '\n') && (c != '\r'));
                        if (c == '\n')
                        {
                            _lineNumber++;
                        }
                        return GetNextNonWhiteChar();
                    }
                    if (c2 == '*')
                    {
                        // ブロックコメント
                        // ブロック末尾まで読み飛ばす
                        for (; ; )
                        {
                            c = GetCh();
                            if (c == '*')
                            {
                                c = GetCh();
                                if (c == '/')
                                {
                                    break;
                                }
                                UngetCh(c);
                            }
                            else if (c == '\\')
                            {
                                c = GetCh();
                                if (c == '\r')
                                {
                                    c = GetCh();
                                    if (c != '\n')
                                    {
                                        UngetCh(c);
                                    }
                                    _lineNumber++;
                                }
                                else if (c == '\n')
                                {
                                    _lineNumber++;
                                }
                                continue;
                            }
                            else if (c == '\n')
                            {
                                _lineNumber++;
                            }
                        }
                        return GetNextNonWhiteChar();
                    }
                    // どちらでもないので読み戻す
                    UngetCh(c2);
                }

                // エスケープ文字か判定
                if (c != '\\')
                {
                    break;
                }

                // 行末エスケープか判定する
                c = GetCh();
                if (c != '\n' && c != '\r')
                {
                    // 行末エスケープではないので読み戻して終了
                    UngetCh(c);
                    break;
                }
                else
                {
                    if (c == '\n')
                    {
                        _lineNumber++;
                    }
                }
            }

            return c;
        }

        /// <summary>
        /// プリプロセッサ指令を解釈する
        /// </summary>
        /// <param name="token">解釈結果を格納するトークン(nullはline指令)</param>
        /// <returns>読み取ったトークン</returns>
        private Token ReadLineDirective()
        {
            Token lReturnValue = null;
            int c;
            do
            {
                c = GetCh();
            } while (IsBlank(c));

            var tbuf = new StringBuilder();
            do
            {
                tbuf.Append((char)c);
                c = GetCh();
            } while (c != '\n');
            UngetCh(c);

            string s = tbuf.ToString();
            if (s.Length != 0)
            {
                if (IsDigit(s[0]))
                {
                    ParseLineDirectiveGnuStyle(s);
                }
                else if (s.StartsWith("pragma", StringComparison.Ordinal))
                {
                    lReturnValue = new Token() {TokenId = TokenId.Pragma, TextValue = s.Substring(6).Trim()};
                }
                else if (s.StartsWith("line", StringComparison.Ordinal))
                {
                    // # line <digit-sequence> <filename>? 形式で最初の # は読み飛ばし済み
                    ParseLineDirectiveGnuStyle(s.Substring(4).Trim());
                }
                else
                {
                    lReturnValue = new Token() { TokenId = TokenId.Ignore };
                }
            }

            return lReturnValue;
        }

        /// <summary>
        /// GNUスタイルのline指示子の解析
        /// </summary>
        /// <param name="str">解析するGNUスタイルのline行</param>
        private void ParseLineDirectiveGnuStyle(string str)
        {
            // # <digit-sequence> <filename>? 形式で最初の # は読み飛ばし済み
            var m = System.Text.RegularExpressions.Regex.Match(str, @"^\s*(\d+)(?:\s+(""([^""]+)""))?(?:\s(.*))?$");
            if (m.Success == false)
            {
                return;
            }
            if (!IgnoreLineDirective)
            {
                _lineNumber = int.Parse(m.Groups[1].Value) - 1;
                if (string.IsNullOrWhiteSpace(m.Groups[2].Value) == false)
                {
                    _fileName = m.Groups[3].Value;
                }
            }
        }

        /// <summary>
        /// 文字定数を読み取る
        /// </summary>
        /// <param name="token">解釈結果を格納するトークン</param>
        /// <returns>読み取ったトークン</returns>
        private Token ReadCharConst()
        {
            int c;
            long value = 0;
            while ((c = GetCh()) != '\'')
            {
                if (c == '\\')
                {
                    value = ReadEscapeChar();
                }
                else if (Char.IsControl((char)c))
                {
                    if (c == '\n')
                    {
                        ++_lineNumber;
                    }

                    return new Token() { TokenId = TokenId.BadToken, LongValue = value };
                }
                else
                {
                    value = c;
                }
            }
            return new Token() { TokenId = TokenId.CharConst, LongValue = value };
        }

        /// <summary>
        /// エスケープシーケンスの本体を読み取る
        /// </summary>
        /// <returns>読み取りったエスケープシーケンス</returns>
        private int ReadEscapeChar()
        {
            var c = GetCh();
            switch (c)
            {
                case 'n':
                    c = '\n';
                    break;
                case 't':
                    c = '\t';
                    break;
                case 'r':
                    c = '\r';
                    break;
                case 'f':
                    c = '\f';
                    break;
                case '\n':
                    ++_lineNumber;
                    break;
                case 'b':
                    c = '\b';
                    break;
                case 'v':
                    c = '\u000b';
                    break;
                case 'a':
                    c = '\u0007';
                    break;
                case '\\':
                    c = '\\';
                    break;
                case '\'':
                    c = '\'';
                    break;
                case '"':
                    c = '\"';
                    break;
                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                    {
                        // 8進数の読み取り
                        int c1 = c;
                        for (int i = 0; i < 3; i++)
                        {
                            if (('0' <= c1) && (c1 <= '7'))
                            {
                                c = c * 8 + (c1 - '0');
                                c1 = GetCh();
                            }
                            else
                            {
                                break;
                            }
                        }
                        UngetCh(c);
                        break;
                    }
                case 'x':
                    {
                        // 16進数の読み取り
                        int c1 = 0;
                        c = 0;
                        for (; ; )
                        {
                            c1 = GetCh();
                            if (IsHex(c1))
                            {
                                int digit = Convert.ToInt32("" + (char)c1, 16);
                                c = c * 16 + digit;
                            }
                            else
                            {
                                break;
                            }
                        }
                        UngetCh(c1);
                    }
                    break;
            }
            return c;
        }


        /// <summary>
        /// ワイド文字列リテラルの読み取りを試みる
        /// </summary>
        /// <param name="token">読み取った文字列の情報が格納されるトークン情報</param>
        /// <returns>読み取ったトークン</returns>
        private Token ReadStringWl()
        {
            Token t = ReadStringL();
            return t.TokenId == TokenId.StringLiteral ? new Token() { TokenId = TokenId.WideStringLiteral, TextValue = t.TextValue } : t;
        }

        /// <summary>
        /// 連続する文字列リテラルを読み取って連結する
        /// </summary>
        /// <param name="token">読み取った文字列の情報が格納されるトークン情報</param>
        /// <returns>読み取ったトークン</returns>
        private Token ReadStringL()
        {
            var tbuf = new StringBuilder();
            for (; ; )
            {
                int c;

                // 文字列リテラルの読み取り
                while ((c = GetCh()) != '"')
                {
                    if (c == '\\')
                    {
                        c = ReadEscapeChar();
                    }
                    else if (c == '\n' || c < 0)
                    {
                        ++_lineNumber;
                        return new Token() {TokenId = TokenId.BadToken};
                    }

                    tbuf.Append((char)c);
                }

                for (; ; )
                {
                    c = GetCh();
                    if (c == '\n')
                    {
                        ++_lineNumber;
                    }
                    else if (!IsBlank(c))
                    {
                        break;
                    }
                }

                if (c != '"')
                {
                    UngetCh(c);
                    break;
                }
            }

            return new Token() {TokenId = TokenId.StringLiteral, TextValue = tbuf.ToString()};
        }

        /// <summary>
        /// 数値定数の読み取りを試みる
        /// </summary>
        /// <param name="c">１文字目</param>
        /// <param name="token">読み取った文字列の情報が格納されるトークン情報</param>
        /// <returns>読み取ったトークン</returns>
        private Token ReadNumber(int c)
        {
            var tbuf = new StringBuilder();
            if (c == '0')
            {
                c = GetCh();
                if (c == 'X' || c == 'x')
                {
                    c = ReadDigits(tbuf, 16);
                    return ReadInteger(tbuf, c, 16);
                }
                if (IsOctal(c))
                {
                    tbuf.Append((char)c);
                    c = ReadDigits(tbuf, 8);
                    return ReadInteger(tbuf, c, 8);
                }
                UngetCh(c);
                c = '0';
            }
            tbuf.Append((char)c);
            c = ReadDigits(tbuf, 10);
            if (c == 'E' || c == 'e' || c == '.')
            {
                return ReadDouble(tbuf, c);
            }
            return ReadInteger(tbuf, c, 10);
        }

        /// <summary>
        /// 整数部の読み取り
        /// </summary>
        /// <param name="tbuf">読み取り結果を格納するバッファ</param>
        /// <param name="radix">基数</param>
        /// <returns>次の文字</returns>
        private int ReadDigits(StringBuilder tbuf, int radix)
        {
            int c;
            Func<int, bool> validator;
            switch (radix)
            {
                case 8:
                    validator = IsOctal;
                    break;
                case 10:
                    validator = IsDigit;
                    break;
                case 16:
                    validator = IsHex;
                    break;
                default:
                    return GetCh();
            }
            for (; ; )
            {
                c = GetCh();
                if (validator(c) == false)
                {
                    break;
                }
                tbuf.Append((char)c);
            }
            return c;
        }

        /// <summary>
        /// 整数定数の読み取り
        /// </summary>
        /// <param name="tbuf">文字を格納するバッファ</param>
        /// <param name="c">先読みしていた文字</param>
        /// <param name="token">読み取り結果を格納するトークン</param>
        /// <param name="radix">基数</param>
        /// <returns>トークン種別</returns>
        private Token ReadInteger(StringBuilder tbuf, int c, int radix)
        {
            bool signed = true;
            int nlong = 0;

            if (c == 'U' || c == 'u')
            {
                // unsigned 指定
                c = GetCh();
                signed = false;
                if (c == 'L' || c == 'l')
                {
                    // long指定
                    c = GetCh();
                    nlong++;
                    if (c == 'L' || c == 'l')
                    {
                        // long-long指定
                        c = GetCh();
                        nlong++;
                    }
                }
            }
            else if (c == 'L' || c == 'l')
            {
                // long指定
                c = GetCh();
                nlong++;
                if (c == 'L' || c == 'l')
                {
                    // long-long指定
                    c = GetCh();
                    nlong++;
                    if (c == 'U' || c == 'u')
                    {
                        // unsigned指定
                        c = GetCh();
                        signed = false;
                    }
                }
                else if (c == 'U' || c == 'u')
                {
                    // unsigned指定
                    c = GetCh();
                    signed = false;
                    if (c == 'L' || c == 'l')
                    {
                        // long-long指定
                        c = GetCh();
                        nlong++;
                    }
                }
            }
            UngetCh(c);
            string s = tbuf.ToString();

            var LongValue = Convert.ToInt64(s, radix);

            if (signed && nlong <= 0 && LongValue <= Int32.MaxValue && LongValue >= Int32.MinValue)
            {
                return new Token() {TokenId = TokenId.IntConst, LongValue = LongValue};
            } else if (nlong <= 0 && LongValue <= UInt32.MaxValue && LongValue >= UInt32.MinValue)
            {
                return new Token() { TokenId = TokenId.UintConst, LongValue = LongValue };
            } else if (signed && nlong <= 1 && LongValue <= UInt32.MaxValue && LongValue >= UInt32.MinValue)
            {
                return new Token() { TokenId = TokenId.LongConst, LongValue = LongValue };
            } else if (nlong <= 1 && LongValue <= UInt32.MaxValue && LongValue >= UInt32.MinValue)
            {
                return new Token() { TokenId = TokenId.UlongConst, LongValue = LongValue };
            } else if (signed && nlong <= 2 && LongValue <= Int64.MaxValue && LongValue >= Int64.MinValue)
            {
                return new Token() { TokenId = TokenId.LongLongConst, LongValue = LongValue };
            } else { 
                return new Token() { TokenId = TokenId.UlongLongConst, LongValue = LongValue };
            }
        }

        /// <summary>
        /// 整数定数の読み取り
        /// </summary>
        /// <param name="sbuf">文字を格納するバッファ</param>
        /// <param name="c">先読みしていた文字</param>
        /// <param name="token">読み取り結果を格納するトークン</param>
        /// <returns>トークン種別</returns>
        private Token ReadDouble(StringBuilder sbuf, int c)
        {
            if (c != 'E' && c != 'e')
            {
                sbuf.Append((char)c);
                for (; ; )
                {
                    c = GetCh();
                    if ('0' <= c && c <= '9')
                    {
                        sbuf.Append((char)c);
                    }
                    else
                    {
                        break;
                    }
                }
            }

            if (c == 'E' || c == 'e')
            {
                sbuf.Append((char)c);
                c = GetCh();
                if (c == '+' || c == '-')
                {
                    sbuf.Append((char)c);
                    c = GetCh();
                }

                while ('0' <= c && c <= '9')
                {
                    sbuf.Append((char)c);
                    c = GetCh();
                }
            }

            TokenId type;
            if (c == 'L' || c == 'l')
            {
                type = TokenId.LongDoubleConst;
            }
            else if (c == 'F' || c == 'f')
            {
                type = TokenId.FloatConst;
            }
            else
            {
                UngetCh(c);
                type = TokenId.DoubleConst;
            }

            try
            {
                return new Token() {TokenId = type, DoubleValue = Double.Parse(sbuf.ToString())};
            }
            catch (FormatException)
            {
                return new Token() { TokenId = TokenId.BadToken };
            }
        }

        /// <summary>
        /// '='と文字cが連続した場合のトークンを判別する
        /// </summary>
        /// <param name="c">'='に続く文字</param>
        /// <returns>判断結果のトークン</returns>
        private static TokenId EqualOps(int c)
        {
            switch (c)
            {
                case '!':
                    return TokenId.NotEq;
                case '%':
                    return TokenId.ModEq;
                case '&':
                    return TokenId.AndEq;
                case '*':
                    return TokenId.MulEq;
                case '+':
                    return TokenId.PlusEq;
                case '-':
                    return TokenId.MinusEq;
                case '.':
                    return TokenId.Ellipsis;
                case '/':
                    return TokenId.DivEq;
                case '<':
                    return TokenId.LessEq;
                case '=':
                    return TokenId.Equal;
                case '>':
                    return TokenId.GreatEq;
                default:
                    return 0;
            }
        }

        /// <summary>
        /// 式の区切りになる記号などを読み取る
        /// </summary>
        /// <param name="c">先読みしていた文字</param>
        /// <returns>読み取ったトークンの種別</returns>
        private Token ReadSeparator(int c)
        {
            int c2;
            if ('!' <= c && c <= '?')
            {
                TokenId t = EqualOps(c);
                if (t == 0)
                {
                    return new Token() {TokenId = (TokenId) c};
                }
                c2 = GetCh();
                int c3;
                if (c == '.')
                {
                    if (c2 == '.')
                    {
                        c3 = GetCh();
                        if (c3 == '.')
                        {
                            return new Token() { TokenId = TokenId.Ellipsis };
                        }
                        UngetCh(c3);
                        return new Token() { TokenId = TokenId.BadToken };
                    }
                }
                else if (c == c2)
                {
                    switch (c)
                    {
                        case '=':
                            return new Token() { TokenId = TokenId.Equal };
                        case '+':
                            return new Token() { TokenId = TokenId.PlusPlus };
                        case '-':
                            return new Token() { TokenId = TokenId.MinusMinus };
                        case '&':
                            return new Token() { TokenId = TokenId.AndAnd };
                        case '<':
                            c3 = GetCh();
                            if (c3 == '=')
                            {
                                return new Token() { TokenId = TokenId.ShiftLeftEq };
                            }
                            UngetCh(c3);
                            return new Token() { TokenId = TokenId.ShiftLeft };
                        case '>':
                            c3 = GetCh();
                            if (c3 == '=')
                            {
                                return new Token() { TokenId = TokenId.RightShiftEq };
                            }
                            UngetCh(c3);
                            return new Token() { TokenId = TokenId.ShiftRight };
                    }
                }
                else if (c2 == '=')
                {
                    return new Token() { TokenId = t };
                }
                else if (c == '-' && c2 == '>')
                {
                    return new Token() { TokenId = TokenId.Arrow };
                }
            }
            else if (c == '^')
            {
                c2 = GetCh();
                if (c2 == '=')
                {
                    return new Token() { TokenId = TokenId.XorEq };
                }
            }
            else if (c == '|')
            {
                c2 = GetCh();
                if (c2 == '=')
                {
                    return new Token() { TokenId = TokenId.OrEq };
                }
                if (c2 == '|')
                {
                    return new Token() { TokenId = TokenId.OrOr };
                }
            }
            else
            {
                return new Token() { TokenId = (TokenId)c };
            }

            UngetCh(c2);
            return new Token() { TokenId = (TokenId)c };
        }

        /// <summary>
        /// 文字 c に続く識別子を読み込む
        /// </summary>
        /// <param name="c">先読みしていた先頭文字</param>
        /// <param name="token">読み取った文字列の情報が格納されるトークン情報</param>
        /// <returns>読み取ったトークン</returns>
        private Token ReadIdentifier(int c)
        {
            var tbuf = new StringBuilder();

            do
            {
                tbuf.Append((char)c);
                c = GetCh();
            } while (IsAlpha(c) || (c == '_') || (c == '$') || IsDigit(c));

            UngetCh(c);
            TokenId t;
            if ((KeywordTable.TryGetValue(tbuf.ToString(), out t)) && (t >= 0))
            {
                return new Token() { TokenId = t };
            }

            String text = tbuf.ToString();
            t = IsTypedefedType(text) ? TokenId.TypedefName : TokenId.Identifier;
            return new Token() { TokenId = t, TextValue = text };
        }

        public Func<string, bool> IsTypedefedType { get; set; }

        /// <summary>
        /// 文字が空白文字であるか判定
        /// </summary>
        /// <param name="c">判定する文字</param>
        /// <returns>判定結果</returns>
        private static bool IsBlank(int c)
        {
            return c == ' ' || c == '\t' || c == '\f' || c == '\r';
        }

        /// <summary>
        /// 文字が十進数を構成する文字であるか判定
        /// </summary>
        /// <param name="c">判定する文字</param>
        /// <returns>判定結果</returns>
        private static bool IsDigit(int c)
        {
            return '0' <= c && c <= '9';
        }

        /// <summary>
        /// 文字が十六進数を構成する文字であるか判定
        /// </summary>
        /// <param name="c">判定する文字</param>
        /// <returns>判定結果</returns>
        private static bool IsHex(int c)
        {
            return ('0' <= c && c <= '9') || ('A' <= c && c <= 'F') || ('a' <= c && c <= 'f');
        }

        /// <summary>
        /// 文字が八進数を構成する文字であるか判定
        /// </summary>
        /// <param name="c">判定する文字</param>
        /// <returns>判定結果</returns>
        private static bool IsOctal(int c)
        {
            return '0' <= c && c <= '7';
        }

        /// <summary>
        /// 文字が半角英字であるか判定
        /// </summary>
        /// <param name="c">判定する文字</param>
        /// <returns>判定結果</returns>
        private static bool IsAlpha(int c)
        {
            return (('A' <= c) && (c <= 'Z')) || (('a' <= c) && (c <= 'z'));
        }

        /// <summary>
        /// １文字読み戻す
        /// </summary>
        /// <param name="c">読み戻す文字</param>
        private void UngetCh(int c)
        {
            if (_lastChar != -1)
            {
                throw new InvalidOperationException("既に読み戻された文字が存在している。");
            }
            _lastChar = c;
        }

        /// <summary>
        /// EOFが検出されたことを示すフラグ
        /// </summary>
        private bool _handleEof;

        /// <summary>
        /// １文字読み取る
        /// </summary>
        /// <returns>読み取った文字(EOFの場合は-1)</returns>
        private int GetCh()
        {
            if (_lastChar < 0)
            {
                if (_handleEof)
                {
                    return -1;
                }
                var ch = OnReadChar();
                if (ch == -1)
                {
                    _handleEof = true;
                }
                return ch;
            }
            int c = _lastChar;
            _lastChar = -1;
            return c;
        }

        public Func<int> OnReadChar { get; set; }


        /// <summary>
        /// 次のトークンを先読みする
        /// </summary>
        /// <returns>先読みしたトークンの種別</returns>
        public TokenId LookAheadToken()
        {
            return LookAheadToken(0);
        }

        /// <summary>
        /// i番目のトークンを先読みする
        /// </summary>
        /// <param name="i">先読みするトークンの番号</param>
        /// <returns>先読みしたトークンの種別</returns>
        public TokenId LookAheadToken(int i)
        {
            Token tk = _lookAheadTokens;	// 0番目のトークン＝つまり先読みではなく現在のトークンをチェック
            if (tk == null)
            {
                // 0番目のトークンを読み取っていない場合、currentTokenを先読みトークンリストの先頭に設定した上で
                // currentTokenに一つ読み取る。
                _lookAheadTokens = tk = _currentToken;
                tk.Next = null;
                GetToken(ref tk);
            }

            // 1番目～i番目までのトークンを読み取る
            for (; i-- > 0; tk = tk.Next)
            {
                if (tk.Next == null)
                {
                    Token tk2;
                    tk.Next = tk2 = new Token();
                    GetToken(ref tk2);
                }
            }

            _currentToken = tk;	// 最後に読み取ったトークンを現在のトークンに設定
            return tk.TokenId;
        }

        /// <summary>
        /// 現在解析中の行番号
        /// </summary>
        public int LineNumber
        {
            get { return _currentToken.LineNumber; }
        }

        /// <summary>
        /// 現在解析中のファイル名
        /// </summary>
        public string FileName
        {
            get { return _currentToken.FileName; }
        }

        /// <summary>
        /// 現在のトークンの文字列表現を取得
        /// </summary>
        public string StringValue
        {
            get { return _currentToken.TextValue; }
        }

        /// <summary>
        /// 現在のトークンの整数値表現を取得
        /// </summary>
        public long LongValue
        {
            get { return _currentToken.LongValue; }
        }

        /// <summary>
        /// 現在のトークンの浮動小数点数表現を取得
        /// </summary>
        public double DoubleValue
        {
            get { return _currentToken.DoubleValue; }
        }

        /// <summary>
        /// トークン種別が型名, 型修飾子, 記憶クラス指定子のいずれかに属するか判定
        /// </summary>
        /// <param name="tokenId">トークン種別</param>
        /// <returns>true:型名, 型修飾子, 記憶クラス指定子のいずれかに属している　false:属していない</returns>
        public static bool IsType(TokenId tokenId)
        {
            switch (tokenId)
            {
                case TokenId.Auto:
                case TokenId.Char:
                case TokenId.Const:
                case TokenId.Double:
                case TokenId.Enum:
                case TokenId.Extern:
                case TokenId.Float:
                case TokenId.Int:
                case TokenId.Long:
                case TokenId.Register:
                case TokenId.Restrict:
                case TokenId.Short:
                case TokenId.Signed:
                case TokenId.Static:
                case TokenId.Struct:
                case TokenId.Typedef:
                case TokenId.Union:
                case TokenId.Unsigned:
                case TokenId.Void:
                case TokenId.Volatile:
                case TokenId.TypedefName:
                    return true;
                default:
                    return false;
            }
        }
    }

    public enum TokenId
    {
        Eof = -1,
        Lf = '\n',
        Asm = 300,
        Auto = 301,
        Char = 302,
        Const = 303,
        Double = 304,
        Enum = 305,
        Extern = 306,
        Float = 307,
        Int = 308,
        Long = 309,
        Register = 310,
        Short = 311,
        Signed = 312,
        Static = 313,
        Struct = 314,
        Typedef = 315,
        Union = 316,
        Unsigned = 317,
        Void = 318,
        Volatile = 319,
        Mutable = 320,
        Break = 321,
        Case = 322,
        Continue = 323,
        Default = 324,
        Do = 325,
        Else = 326,
        For = 327,
        Goto = 328,
        If = 329,
        Return = 330,
        Sizeof = 331,
        Switch = 332,
        While = 333,

        Inline = 334, // now it's part of ANSI C.
        Restrict = 335,

        // If you change the value of NotEq ... ARROW, then
        // check Parser.getOpPrecedence() and Parser.nextIsAssignOp().

        ModEq = 350, // %=
        AndEq = 351, // &=
        MulEq = 352, // *=
        PlusEq = 353, // +=
        MinusEq = 354, // -=
        DivEq = 355, // /=
        ShiftLeftEq = 356, // <<=
        XorEq = 357, // ^=
        OrEq = 358, // |=
        RightShiftEq = 359, // >>=

        NotEq = 360, // !=
        LessEq = 361, // <=
        LessThan = (int)'<', // <
        GreatEq = 362, // >=
        GreatThan = (int)'>', // >
        Equal = 363, // ==
        PlusPlus = 364, // ++
        MinusMinus = 365, // --
        ShiftLeft = 366, // <<
        ShiftRight = 367, // >>
        OrOr = 368, // ||
        AndAnd = 369, // &&
        Ellipsis = 370, // ...
        Arrow = 371, // ->

        CastOp = 380, // cast operator
        FunctionCall = 381, // function call
        CondOp = 382, // conditional expression ?:
        IndexOp = 383, // array index []

        Identifier = 400,
        CharConst = 401,
        IntConst = 402,
        UintConst = 403,
        LongConst = 404,
        UlongConst = 405,
        FloatConst = 406,
        DoubleConst = 407,
        LongDoubleConst = 407, // equivalent to DOUBLE_CONST
        StringLiteral = 408,
        TypedefName = 409,
        WideStringLiteral = 410,

        LongLongConst = 411,
        UlongLongConst = 412,
        Pragma = 413,

        SkipGccAttribute = 498,
        SkipGccAsm = 499,
        BadToken = 500,
        Ignore = 501,
    }

}
