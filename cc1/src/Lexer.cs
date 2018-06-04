using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace AnsiCParser {
    /// <summary>
    /// 字句解析器
    /// </summary>
    public class Lexer {

        /// <summary>
        /// line 指令に一致する正規表現。
        /// </summary>
        private static Regex RegexLineDirective { get; } = new Regex(@"^#\s*(line\s+)?(?<line>\d+)\s+""(?<file>(\\""|[^""])*)""(\s+(\d+)){0,3}\s*$");

        /// <summary>
        /// pragma pack 指令に一致する正規表現。
        /// </summary>
        private static Regex RegexPackDirective { get; } = new Regex(@"^#\s*(pragma\s+)(pack(\s*\(\s*(?<packsize>[1|2|4])?\s*\)|\s*\s*(?<packsize>[1|2|4])?\s*)?\s*)$");

        /// <summary>
        /// 10進数文字に一致する正規表現。
        /// </summary>
        private static string D { get; } = @"\d";
        
        /// <summary>
        /// 16進数文字に一致する正規表現。
        /// </summary>
        private static string H { get; } = @"[a-fA-F0-9]";

        /// <summary>
        /// 指数部に一致する正規表現。
        /// </summary>
        private static string E { get; } = $@"[Ee][+-]?{D}+";

        /// <summary>
        /// 浮動小数点数形式のサフィックスに一致する正規表現。
        /// </summary>
        private static string FS { get; } = @"(f|F|l|L)?";

        /// <summary>
        /// 整数数形式のサフィックスに一致する正規表現。
        /// </summary>
        private static string IS { get; } = @"(u|U|l|L)*";

        /// <summary>
        /// 10進浮動小数点形式に一致する正規表現
        /// </summary>
        private static Regex RegexDecimalFloat { get; } = new Regex($@"^(?<Body>{D}+{E}|{D}*\.{D}+({E})?|{D}+\.{D}*({E})?)(?<Suffix>{FS})$");

        /// <summary>
        /// 16進浮動小数点形式に一致する正規表現
        /// </summary>
        private static Regex RegexHeximalFloat { get; } = new Regex($@"^(0[xX])(?<Fact>({H}*?\.{H}+|{H}+\.?))[pP](?<Exp>[\+\-]?{D}+)(?<Suffix>{FS})$");

        /// <summary>
        /// 10進数形式に一致する正規表現
        /// </summary>
        private static Regex RegexDecimal { get; } = new Regex($@"^(?<Body>{D}+)(?<Suffix>{IS})$");

        /// <summary>
        /// 16進数形式に一致する正規表現
        /// </summary>
        private static Regex RegexHeximal { get; } = new Regex($@"^0[xX](?<Body>{H}+)(?<Suffix>{IS})$");

        /// <summary>
        /// 8進数形式に一致する正規表現
        /// </summary>
        private static Regex RegexOctal { get; } = new Regex($@"^0(?<Body>{D}+)(?<Suffix>{IS})$");

        /// <summary>
        /// 浮動小数点文字列の解析
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static Tuple<int, string, string, string> ScanFloat(string str) {
            var m = RegexDecimalFloat.Match(str);
            if (m.Success) {
                return Tuple.Create(10, m.Groups["Body"].Value, "", String.Concat(m.Groups["Suffix"].Value.ToCharArray().OrderBy(x => x)));
            }
            m = RegexHeximalFloat.Match(str);
            if (m.Success) {
                return Tuple.Create(16, m.Groups["Fact"].Value, m.Groups["Exp"].Value, String.Concat(m.Groups["Suffix"].Value.ToCharArray().OrderBy(x => x)));
            }
            throw new Exception();
        }

        /// <summary>
        /// 16進浮動小数点形式を double 型に変換
        /// </summary>
        /// <param name="range"></param>
        /// <param name="fact"></param>
        /// <param name="exp"></param>
        /// <param name="suffix"></param>
        /// <returns></returns>
        public static double ParseHeximalFloat(LocationRange range, string fact, string exp, string suffix) {
            // 小数点の初期位置を決める
            var dp = fact.IndexOf('.');
            string fs;
            if (dp == -1) {
                // 小数点が無かったので、仮数部の末尾に小数点があるものとする
                dp = fact.Length;
                fs = fact;
            } else {
                // 小数点があった
                fs = fact.Remove(dp, 1);
            }

            // 符号を読んでおく
            var sign = false;
            if (fs.FirstOrDefault() == '-') {
                fs = fs.Remove(0, 1);
                sign = true;
            } else if (fs.FirstOrDefault() == '+') {
                fs = fs.Remove(0, 1);
            }

            // 仮数部が0の場合は特別扱い
            if (ToUInt64(range, fs, 16) == 0) {
                return 0;
            }

            if (suffix == "f") {
                // float 型として解析
                fs = (fs + new string(Enumerable.Repeat('0',8).ToArray())).Substring(0, 8);
                var f = ToUInt32(range, fs, 16);
                dp *= 4;
                while ((f & (1UL << 31)) == 0) {
                    f <<= 1;
                    dp--;
                }
                // ケチ表現化
                {
                    f <<= 1;
                    dp--;
                }
                var e = dp + int.Parse(exp);

                var qw = (sign ? (1U << 31) : 0) | (((UInt32)(e + 127) & ((1U << 8) - 1)) << 23) | ((f >> (32 - 23)) & ((1U << 23) - 1));
                var d = BitConverter.ToSingle(BitConverter.GetBytes(qw), 0);

                return d;
            } else {
                // double 型として解析
                fs = (fs + new string(Enumerable.Repeat('0', 16).ToArray())).Substring(0, 16);
                var f = ToUInt64(range, fs, 16);
                dp *= 4;
                while ((f & (1UL << 63)) == 0) {
                    f <<= 1;
                    dp--;
                }
                // ケチ表現化
                {
                    f <<= 1;
                    dp--;
                }
                var e = dp + int.Parse(exp);

                var qw = (sign ? (1UL << 63) : 0) | (((UInt64)(e + 1023) & ((1UL << 11) - 1)) << 52) | ((f >> (64 - 52)) & ((1UL << 52) - 1));
                var d = BitConverter.ToDouble(BitConverter.GetBytes(qw), 0);

                return d;
            }
            throw new NotImplementedException();
        }

        /// <summary>
        /// 16進数整数の解析
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static Tuple<string, string> ParseHeximal(string str) {
            var m = RegexHeximal.Match(str);
            if (m.Success == false) {
                throw new Exception();
            }
            return Tuple.Create(m.Groups["Body"].Value, String.Concat(m.Groups["Suffix"].Value.ToCharArray().OrderBy(x => x)));
        }

        /// <summary>
        /// 10進数整数の解析
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static Tuple<string, string> ParseDecimal(string str) {
            var m = RegexDecimal.Match(str);
            if (m.Success == false) {
                throw new Exception();
            }
            return Tuple.Create(m.Groups["Body"].Value, String.Concat(m.Groups["Suffix"].Value.ToCharArray().OrderBy(x => x)));
        }

        /// <summary>
        /// 8進数整数の解析
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static Tuple<string, string> ParseOctal(string str) {
            var m = RegexOctal.Match(str);
            if (m.Success == false) {
                throw new Exception();
            }
            return Tuple.Create(m.Groups["Body"].Value, String.Concat(m.Groups["Suffix"].Value.ToCharArray().OrderBy(x => x)));
        }

        /// <summary>
        /// 符号なし整数文字列を数値として読み取る
        /// </summary>
        /// <param name="range"></param>
        /// <param name="s"></param>
        /// <param name="radix"></param>
        /// <returns></returns>
        private static System.Numerics.BigInteger Read(LocationRange range, string s, int radix) {
            System.Numerics.BigInteger ret = 0;
            switch (radix) {
                case 8:
                    foreach (var ch in s) {
                        if ("01234567".IndexOf(ch) == -1) {
                            throw new CompilerException.SpecificationErrorException(range, $"八進数に使えない文字{ch}が含まれています。");
                        }
                        ret = ret * 8 + (ch - '0');
                    }
                    break;
                case 10:
                    foreach (var ch in s) {
                        if ("0123456789".IndexOf(ch) == -1) {
                            throw new CompilerException.SpecificationErrorException(range, $"十進数に使えない文字{ch}が含まれています。");
                        }
                        ret = ret * 10 + (ch - '0');
                    }
                    break;
                case 16:
                    foreach (var ch in s) {
                        if ("0123456789".IndexOf(ch) != -1) {
                            ret = ret * 16 + (ch - '0');
                        } else if ("ABCDEF".IndexOf(ch) != -1) {
                            ret = ret * 16 + (ch - 'A' + 10);
                        } else if ("abcdef".IndexOf(ch) != -1) {
                            ret = ret * 16 + (ch - 'a' + 10);
                        } else {
                            throw new CompilerException.SpecificationErrorException(range, $"十六進数に使えない文字{ch}が含まれています。");
                        }
                    }
                    break;
                default:
                    throw new CompilerException.SpecificationErrorException(range, $"{radix}は対応していない基数です。");
            }
            return ret;
        }

        /// <summary>
        /// 32bit符号付き数値読み取り (Convert.ToInt32は桁あふれエラーを起こすため)
        /// </summary>
        /// <param name="range"></param>
        /// <param name="s"></param>
        /// <param name="radix"></param>
        /// <returns></returns>
        public static Int32 ToInt32(LocationRange range, string s, int radix) {
            var ret = Read(range, s, radix).ToByteArray();
            return BitConverter.ToInt32(ret.Concat(Enumerable.Repeat((byte)((ret.Last() & 0x80) != 0x00 ? 0xFF : 0x00),4)).ToArray(),0);
        }

        /// <summary>
        /// 32bit符号無し数値読み取り (Convert.ToUInt32は桁あふれエラーを起こすため)
        /// </summary>
        /// <param name="range"></param>
        /// <param name="s"></param>
        /// <param name="radix"></param>
        /// <returns></returns>
        public static UInt32 ToUInt32(LocationRange range, string s, int radix) {
            var ret = Read(range, s, radix).ToByteArray();
            return BitConverter.ToUInt32(ret.Concat(Enumerable.Repeat((byte)0,4)).ToArray(),0);
        }

        /// <summary>
        /// 64bit符号付き数値読み取り (Convert.ToInt64は桁あふれエラーを起こすため)
        /// </summary>
        /// <param name="range"></param>
        /// <param name="s"></param>
        /// <param name="radix"></param>
        /// <returns></returns>
        public static Int64 ToInt64(LocationRange range, string s, int radix) {
            var ret = Read(range, s, radix).ToByteArray();
            return BitConverter.ToInt64(ret.Concat(Enumerable.Repeat((byte)((ret.Last() & 0x80) != 0x00 ? 0xFF : 0x00),8)).ToArray(),0);
        }

        /// <summary>
        /// 64bit符号無し数値読み取り (Convert.ToUInt64は桁あふれエラーを起こすため)
        /// </summary>
        /// <param name="range"></param>
        /// <param name="s"></param>
        /// <param name="radix"></param>
        /// <returns></returns>
        public static UInt64 ToUInt64(LocationRange range, string s, int radix) {
            var ret = Read(range, s, radix).ToByteArray();
            return BitConverter.ToUInt64(ret.Concat(Enumerable.Repeat((byte)0,8)).ToArray(),0);
        }

        /// <summary>
        /// 文字定数の解析
        /// </summary>
        /// <param name="peek"></param>
        /// <param name="next"></param>
        /// <param name="write"></param>
        public static void CharIterator(Func<int> peek, Action next, Action<byte> write) {
            int ret = 0;
            if (peek() == '\\') {
                next();
                switch (peek()) {
                    case '\'':
                    case '"':
                    case '?':
                    case '\\':
                        write((byte)peek());
                        next();
                        return;
                    case 'a':
                        next();
                        write((byte)'\a');
                        return;
                    case 'b':
                        next();
                        write((byte)'\b');
                        return;
                    case 'f':
                        next();
                        write((byte)'\f');
                        return;
                    case 'n':
                        next();
                        write((byte)'\n');
                        return;
                    case 'r':
                        next();
                        write((byte)'\r');
                        return;
                    case 't':
                        next();
                        write((byte)'\t');
                        return;
                    case 'v':
                        next();
                        write((byte)'\v');
                        return;
                    case 'x': {
                            next();
                            int n = 0;
                            while (IsXDigit(peek())) {
                                ret = (ret << 4) | XDigitToInt(peek());
                                next();
                                n++;
                                if (n == 2) {
                                    write((byte)ret);
                                    n = 0;
                                    ret = 0;
                                }
                            }
                            if (n != 0) {
                                write((byte)ret);
                            }
                            return;
                        }
                    case 'u':
                        next();
                        for (var i = 0; i < 4; i++) {
                            if (IsXDigit(peek()) == false) {
                                throw new CompilerException.SyntaxErrorException(Location.Empty, Location.Empty, "invalid universal character");
                            }
                            ret = (ret << 4) | XDigitToInt(peek());
                            next();
                            if ((i % 2) == 1) {
                                write((byte)ret);
                                ret = 0;
                            }
                        }
                        return;
                    case 'U':
                        next();
                        for (var i = 0; i < 8; i++) {
                            if (IsXDigit(peek()) == false) {
                                throw new CompilerException.SyntaxErrorException(Location.Empty, Location.Empty, "invalid universal character");
                            }
                            ret = (ret << 4) | XDigitToInt(peek());
                            next();
                            if ((i % 2) == 1) {
                                write((byte)ret);
                                ret = 0;
                            }
                        }
                        return;
                    case '0':
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                        for (var i = 0; i < 3; i++) {
                            if (IsOct(peek()) == false) {
                                break;
                            }
                            ret = (ret << 3) | (peek() - '0');
                            next();
                        }
                        write((byte)ret);
                        return;
                    default:
                        throw new CompilerException.SyntaxErrorException(Location.Empty, Location.Empty, "unknown escape character");

                }
            } else {
                char ch = (char)peek();
                next();
                var bytes = System.Text.Encoding.UTF8.GetBytes(new[] { ch });
                foreach (var b in bytes) {
                    write(b);
                }
                return;
            }
        }

        /// <summary>
        /// 識別子の先頭に出現できる文字なら真
        /// </summary>
        /// <param name="ch"></param>
        /// <returns></returns>
        private static bool IsIdentifierHead(int ch) {
            return ('A' <= ch && ch <= 'Z') || ('a' <= ch && ch <= 'z') || (ch == '_');
        }

        /// <summary>
        /// 識別子の先頭以外に出現できる文字なら真
        /// </summary>
        /// <param name="ch"></param>
        /// <returns></returns>
        private static bool IsIdentifierBody(int ch) {
            return ('A' <= ch && ch <= 'Z') || ('a' <= ch && ch <= 'z') || ('0' <= ch && ch <= '9') || (ch == '_');
        }

        /// <summary>
        /// 八進数字なら真
        /// </summary>
        /// <param name="ch"></param>
        /// <returns></returns>
        private static bool IsOct(int ch) {
            return ('0' <= ch && ch <= '7');
        }

        /// <summary>
        /// 十進数字なら真
        /// </summary>
        /// <param name="ch"></param>
        /// <returns></returns>
        private static bool IsDigit(int ch) {
            return ('0' <= ch && ch <= '9');
        }

        /// <summary>
        /// 十六進数字なら真
        /// </summary>
        /// <param name="ch"></param>
        /// <returns></returns>
        private static bool IsXDigit(int ch) {
            return ('0' <= ch && ch <= '9') || ('A' <= ch && ch <= 'F') || ('a' <= ch && ch <= 'f');
        }

        /// <summary>
        /// 十六進数字をintに
        /// </summary>
        /// <param name="ch"></param>
        /// <returns></returns>
        private static int XDigitToInt(int ch) {
            if ('0' <= ch && ch <= '9') {
                return ch - '0';
            }
            if ('A' <= ch && ch <= 'F') {
                return ch - 'A' + 10;
            }
            if ('a' <= ch && ch <= 'f') {
                return ch - 'a' + 10;
            }
            throw new Exception();
        }

        /// <summary>
        /// 空白文字なら真
        /// </summary>
        /// <param name="ch"></param>
        /// <returns></returns>
        private static bool IsSpace(int ch) {
            return "\r\n\v\f\t ".Any(x => (int)x == ch);
        }

        /// <summary>
        /// ファイル終端なら真
        /// </summary>
        /// <returns></returns>
        public bool is_eof() {
            return _tokens[_currentTokenPos].Kind == Token.TokenKind.EOF;
        }

        /// <summary>
        /// 予約語
        /// </summary>
        private readonly Dictionary<string, Token.TokenKind> _reserveWords = new Dictionary<string, Token.TokenKind>() {
            {"auto", Token.TokenKind.AUTO},
            {"break" , Token.TokenKind.BREAK},
            {"case" , Token.TokenKind.CASE},
            {"char" , Token.TokenKind.CHAR},
            {"const" , Token.TokenKind.CONST},
            {"continue" , Token.TokenKind.CONTINUE},
            {"default" , Token.TokenKind.DEFAULT},
            {"do" , Token.TokenKind.DO},
            {"double" , Token.TokenKind.DOUBLE},
            {"else" , Token.TokenKind.ELSE},
            {"enum" , Token.TokenKind.ENUM},
            {"extern" , Token.TokenKind.EXTERN},
            {"float" , Token.TokenKind.FLOAT},
            {"for" , Token.TokenKind.FOR},
            {"goto" , Token.TokenKind.GOTO},
            {"if" , Token.TokenKind.IF},
            {"int" , Token.TokenKind.INT},
            {"long" , Token.TokenKind.LONG},
            {"register" , Token.TokenKind.REGISTER},
            {"return" , Token.TokenKind.RETURN},
            {"short" , Token.TokenKind.SHORT},
            {"signed" , Token.TokenKind.SIGNED},
            {"sizeof" , Token.TokenKind.SIZEOF},
            {"static" , Token.TokenKind.STATIC},
            {"struct" , Token.TokenKind.STRUCT},
            {"switch" , Token.TokenKind.SWITCH},
            {"typedef" , Token.TokenKind.TYPEDEF},
            {"union" , Token.TokenKind.UNION},
            {"unsigned" , Token.TokenKind.UNSIGNED},
            {"void" , Token.TokenKind.VOID},
            {"volatile" , Token.TokenKind.VOLATILE},
            {"while" , Token.TokenKind.WHILE},
            // c99
            {"inline" , Token.TokenKind.INLINE},
            {"restrict" , Token.TokenKind.RESTRICT},
            // special
            {"near" , Token.TokenKind.NEAR},
            {"far" , Token.TokenKind.FAR},
            {"__asm__" , Token.TokenKind.__ASM__},
            {"__volatile__" , Token.TokenKind.__VOLATILE__},
        };

        /// <summary>
        /// 予約記号
        /// </summary>
        private readonly List<Tuple<string, Token.TokenKind>> _symbols = new List<Tuple<string, Token.TokenKind>>() {
            Tuple.Create("...", Token.TokenKind.ELLIPSIS),
            Tuple.Create(">>=", Token.TokenKind.RIGHT_ASSIGN),
            Tuple.Create("<<=", Token.TokenKind.LEFT_ASSIGN),
            Tuple.Create("+=", Token.TokenKind.ADD_ASSIGN),
            Tuple.Create("-=", Token.TokenKind.SUB_ASSIGN),
            Tuple.Create("*=", Token.TokenKind.MUL_ASSIGN),
            Tuple.Create("/=", Token.TokenKind.DIV_ASSIGN),
            Tuple.Create("%=", Token.TokenKind.MOD_ASSIGN),
            Tuple.Create("&=", Token.TokenKind.AND_ASSIGN),
            Tuple.Create("^=", Token.TokenKind.XOR_ASSIGN),
            Tuple.Create("|=", Token.TokenKind.OR_ASSIGN),
            Tuple.Create(">>", Token.TokenKind.RIGHT_OP),
            Tuple.Create("<<", Token.TokenKind.LEFT_OP),
            Tuple.Create("++", Token.TokenKind.INC_OP),
            Tuple.Create("--", Token.TokenKind.DEC_OP),
            Tuple.Create("->", Token.TokenKind.PTR_OP),
            Tuple.Create("&&", Token.TokenKind.AND_OP),
            Tuple.Create("||", Token.TokenKind.OR_OP),
            Tuple.Create("<=", Token.TokenKind.LE_OP),
            Tuple.Create(">=", Token.TokenKind.GE_OP),
            Tuple.Create("==", Token.TokenKind.EQ_OP),
            Tuple.Create("!=", Token.TokenKind.NE_OP),
            Tuple.Create(";", (Token.TokenKind)';'),
            Tuple.Create("{", (Token.TokenKind)'{'),
            Tuple.Create("<%", (Token.TokenKind)'{'),
            Tuple.Create("}", (Token.TokenKind)'}'),
            Tuple.Create("%>", (Token.TokenKind)'}'),
            Tuple.Create("<:", (Token.TokenKind)'['),
            Tuple.Create(":>", (Token.TokenKind)']'),
            Tuple.Create(",", (Token.TokenKind)','),
            Tuple.Create(":", (Token.TokenKind)':'),
            Tuple.Create("=", (Token.TokenKind)'='),
            Tuple.Create("(", (Token.TokenKind)'('),
            Tuple.Create(")", (Token.TokenKind)')'),
            Tuple.Create("[", (Token.TokenKind)'['),
            Tuple.Create("]", (Token.TokenKind)']'),
            Tuple.Create(".", (Token.TokenKind)'.'),
            Tuple.Create("&", (Token.TokenKind)'&'),
            Tuple.Create("!", (Token.TokenKind)'!'),
            Tuple.Create("~", (Token.TokenKind)'~'),
            Tuple.Create("-", (Token.TokenKind)'-'),
            Tuple.Create("+", (Token.TokenKind)'+'),
            Tuple.Create("*", (Token.TokenKind)'*'),
            Tuple.Create("/", (Token.TokenKind)'/'),
            Tuple.Create("%", (Token.TokenKind)'%'),
            Tuple.Create("<", (Token.TokenKind)'<'),
            Tuple.Create(">", (Token.TokenKind)'>'),
            Tuple.Create("^", (Token.TokenKind)'^'),
            Tuple.Create("|", (Token.TokenKind)'|'),
            Tuple.Create("?", (Token.TokenKind)'?'),
        }.OrderByDescending((x) => x.Item1.Length).ToList();

        /// <summary>
        /// Ｃソースコード
        /// </summary>
        private readonly string _inputText;

        /// <summary>
        /// ソースコード上の読み取り位置
        /// </summary>
        private int _inputPos;

        /// <summary>
        /// 読み取り位置が行頭の場合には真になる。前処理指令認識用。
        /// </summary>
        private bool _beginOfLine;

        /// <summary>
        /// 入力ファイル名
        /// </summary>
        private string _filepath;

        /// <summary>
        /// 読み取り位置の行番号
        /// </summary>
        private int _line;

        /// <summary>
        /// 読み取り位置の列番号
        /// </summary>
        private int _column;

        /// <summary>
        /// ソースコードから得られたトークンの列
        /// </summary>
        private readonly List<Token> _tokens;

        /// <summary>
        /// 現在のトークンの読み取り位置
        /// </summary>
        private int _currentTokenPos;

        public Lexer(string source, string filepath = "") {
            _inputText = source;
            _inputPos = 0;
            _beginOfLine = true;
            _filepath = filepath;
            _line = 1;
            _column = 1;
            _tokens = new List<Token>();
            _currentTokenPos = 0;
        }

        /// <summary>
        /// 字句解析器が示す現在の読み取り位置情報を取得する
        /// </summary>
        /// <returns></returns>
        private Location GetCurrentLocation() {
            return new Location(_filepath, _line, _column, _inputPos);
        }

        /// <summary>
        /// 始点と終点から部分文字列を取得する。
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        private string Substring(Location start, Location end) {
            return _inputText.Substring(start.Position, end.Position - start.Position);
        }

        /// <summary>
        /// ソースコードの読み取り位置をn進める
        /// </summary>
        /// <param name="n"></param>
        private void IncPos(int n) {
            if (n < 0) {
                throw new ArgumentOutOfRangeException($"{nameof(n)}に負数が与えられた。");
            }
            for (int i = 0; i < n; i++) {
                if (_inputText[_inputPos + i] == '\n') {
                    _line++;
                    _column = 1;
                } else {
                    _column++;
                }
            }
            _inputPos += n;
        }

        /// <summary>
        /// ソースコードの現在位置から offset だけ先の文字を先読みする
        /// </summary>
        /// <param name="offset"></param>
        /// <returns></returns>
        private int Peek(int offset = 0) {
            if (offset < 0) {
                throw new ArgumentOutOfRangeException($"{nameof(offset)}に負数が与えられた。");
            }
            if (_inputPos + offset >= _inputText.Length) {
                return -1;
            } else {
                return _inputText[_inputPos + offset];
            }
        }

        /// <summary>
        /// 現在位置から 文字列 str が先読みできるか調べる
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private bool Peek(string str) {
            for (var i = 0; i < str.Length; i++) {
                if (Peek(i) != str[i]) {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// トークンを一つ読み取って _tokens に入れる。
        /// </summary>
        private void ScanToken() {
            // トークン列の末尾にＥＯＦトークンがある＝すでにファイル終端に到達
            if (_tokens.LastOrDefault()?.Kind == Token.TokenKind.EOF) {
                // 読み取りを行わずに終わる
                return;
            }
            rescan:

            // 空白文字の連続の処理
            while (IsSpace(Peek())) {
                if (Peek("\n")) {
                    _beginOfLine = true;
                }
                IncPos(1);
            }

            // ブロックコメントの処理
            if (Peek("/*")) {
                IncPos(2);

                bool terminated = false;
                while (_inputPos < _inputText.Length) {
                    if (Peek("\\")) {
                        IncPos(2);
                    } else if (Peek("*/")) {
                        IncPos(2);
                        terminated = true;
                        break;
                    } else {
                        IncPos(1);
                    }
                }
                if (terminated == false) {
                    _tokens.Add(new Token(Token.TokenKind.EOF, GetCurrentLocation(), GetCurrentLocation(), ""));
                    return;
                }
                goto rescan;
            }
            // ブロックコメントの処理(拡張)
            if (Peek("```")) {
                IncPos(3);

                bool terminated = false;
                while (_inputPos < _inputText.Length) {
                    if (Peek("\\")) {
                        IncPos(2);
                    } else if (Peek("```")) {
                        IncPos(3);
                        terminated = true;
                        break;
                    } else {
                        IncPos(1);
                    }
                }
                if (terminated == false) {
                    _tokens.Add(new Token(Token.TokenKind.EOF, GetCurrentLocation(), GetCurrentLocation(), ""));
                    return;
                }
                goto rescan;
            }
            // 行コメントの処理
            if (Peek("//")) {
                IncPos(2);

                bool terminated = false;
                while (_inputPos < _inputText.Length) {
                    if (Peek("\\")) {
                        IncPos(2);
                    } else if (Peek("\n")) {
                        terminated = true;
                        break;
                    } else {
                        IncPos(1);
                    }
                }
                if (terminated == false) {
                    _tokens.Add(new Token(Token.TokenKind.EOF, GetCurrentLocation(), GetCurrentLocation(), ""));
                    return;
                }
                goto rescan;
            }


            // ファイル終端到達の確認
            if (Peek() == -1) {
                // ToDo: ファイル末尾が改行文字でない場合は警告を出力
                _tokens.Add(new Token(Token.TokenKind.EOF, GetCurrentLocation(), GetCurrentLocation(), ""));
                return;
            }

            // 読み取り開始位置を記録
            var start = GetCurrentLocation();

            // 前処理指令の扱い
            if (Peek("#")) {
                if (_beginOfLine) {
                    // 前処理指令（#lineや#pragma等）
                    while (Peek("\n") == false) {
                        IncPos(1);
                    }
                    var end = GetCurrentLocation();
                    var str = Substring(start, end);
                    IncPos(1);

                    {
                        // line 指令
                        var match = RegexLineDirective.Match(str);
                        if (match.Success) {
                            _filepath = match.Groups["file"].Value;
                            _line = int.Parse(match.Groups["line"].Value);
                            _column = 1;
                            goto rescan;
                        }
                    }
                    {
                        // pragma pack 指令
                        // プラグマ後の最初の struct、union、宣言から有効
                        var match = RegexPackDirective.Match(str);
                        if (match.Success) {
                            if (match.Groups["packsize"].Success) {
                                Settings.PackSize = int.Parse(match.Groups["packsize"].Value);
                            } else {
                                Settings.PackSize = Settings.DefaultPackSize;
                            }
                            goto rescan;
                        }
                    }
                    goto rescan;
                } else {
                    _tokens.Add(new Token((Token.TokenKind)'#', start, GetCurrentLocation(), "#"));
                    IncPos(1);
                    return;
                }
            }

            // トークンを読み取った結果、行頭以外になるので先に行頭フラグを立てておく
            _beginOfLine = false;

            // 識別子の読み取り
            if (IsIdentifierHead(Peek())) {
                while (IsIdentifierBody(Peek())) {
                    IncPos(1);
                }
                var end = GetCurrentLocation();
                var str = Substring(start, end);
                Token.TokenKind kind;
                if (_reserveWords.TryGetValue(str, out kind)) {
                    _tokens.Add(new Token(kind, start, end, str));
                } else {
                    _tokens.Add(new Token(Token.TokenKind.IDENTIFIER, start, end, str));
                }
                return;
            }

            // 前処理数の読み取り
            if ((Peek(0) == '.' && IsDigit(Peek(1))) || IsDigit(Peek(0))) {
                // 翻訳フェーズの規定に従う場合、定数は前処理数として読み取ってから分類することになる。
                // そのため、0xe-0xe は ["0xe", "-", "0xe"] として正常に読み取ることは誤りで、["0xe-0xe"]として読み取り、サフィックスエラーとしなければならない。

                // \.?\d([eEpP][\+\-]|\.|({L}|{D}|_))*
                if (Peek() == '.') {
                    IncPos(1);
                }
                IncPos(1);
                while (Peek() != -1) {
                    if ("eEpP".Any(x => (int)x == Peek(0)) && "+-".Any(x => (int)x == Peek(1))) {
                        IncPos(2);
                    } else if (Peek(".") || IsIdentifierBody(Peek())) {
                        IncPos(1);
                    } else {
                        break;
                    }
                }
                var end = GetCurrentLocation();
                var str = Substring(start, end);
                if (RegexDecimalFloat.IsMatch(str) || RegexHeximalFloat.IsMatch(str)) {
                    _tokens.Add(new Token(Token.TokenKind.FLOAT_CONSTANT, start, end, str));
                } else if (RegexHeximal.IsMatch(str)) {
                    _tokens.Add(new Token(Token.TokenKind.HEXIMAL_CONSTANT, start, end, str));
                } else if (RegexOctal.IsMatch(str)) {
                    _tokens.Add(new Token(Token.TokenKind.OCTAL_CONSTANT, start, end, str));
                } else if (RegexDecimal.IsMatch(str)) {
                    _tokens.Add(new Token(Token.TokenKind.DECIAML_CONSTANT, start, end, str));
                } else {
//                    throw new Exception();
                    _tokens.Add(new Token(Token.TokenKind.INVALID, start, end, str));
                }
                return;
            } else if (Peek("'")) {
                // 文字定数の読み取り
                IncPos(1);
                while (_inputPos < _inputText.Length) {
                    if (Peek("'")) {
                        IncPos(1);
                        var end = GetCurrentLocation();
                        var str = Substring(start, end);
                        _tokens.Add(new Token(Token.TokenKind.STRING_CONSTANT, start, end, str));
                        return;
                    } else {
                        CharIterator(() => Peek(), () => IncPos(1), (b) => {});
                    }
                }
                throw new Exception();
            }

            // 文字列リテラルの読み取り
            if (Peek("\"")) {
                IncPos(1);
                while (_inputPos < _inputText.Length) {
                    if (Peek("\"")) {
                        IncPos(1);
                        var end = GetCurrentLocation();
                        var str = Substring(start, end);
                        _tokens.Add(new Token(Token.TokenKind.STRING_LITERAL, start, end, str));
                        return;
                    } else {
                        CharIterator(() => Peek(), () => IncPos(1), (b) => {});
                    }
                }
                throw new Exception();
            }

            // 区切り子の読み取り
            {
                foreach (var sym in _symbols) {
                    if (Peek(sym.Item1)) {
                        IncPos(sym.Item1.Length);
                        var end = GetCurrentLocation();
                        var str = Substring(start, end);
                        _tokens.Add(new Token(sym.Item2, start, end, str));
                        return;
                    }
                }
            }

            // 不正な文字
            {
                IncPos(1);
                var end = GetCurrentLocation();
                var str = Substring(start, end);
                _tokens.Add(new Token(Token.TokenKind.INVALID, start, end, str));
                return;

            }

        }

        /// <summary>
        /// 現在の読み取り位置のトークンを得る
        /// </summary>
        /// <returns></returns>
        public Token CurrentToken() {
            if (_tokens.Count == _currentTokenPos) {
                ScanToken();
            }
            return _tokens[_currentTokenPos];
        }

        /// <summary>
        /// トークンを一つ読み進める
        /// </summary>
        public void NextToken() {
            _currentTokenPos++;
        }

        /// <summary>
        /// 現在のトークンが トークン種別候補 candidates に含まれるなら読み取って返す。
        /// 含まれないなら 例外 SyntaxErrorException を投げる
        /// トークン種別候補 candidates が空の場合はどんなトークンでもいいので一つ読むという動作になる。
        /// </summary>
        /// <param name="candidates"></param>
        public Token ReadToken(params Token.TokenKind[] candidates) {
            if (candidates.Contains(CurrentToken().Kind)) {
                var token = CurrentToken();
                NextToken();
                return token;
            }
            throw new CompilerException.SyntaxErrorException(CurrentToken().Start, CurrentToken().End, $" {String.Join(", ", candidates.Select(x => (((int)x < 256) ? ((char)x).ToString() : x.ToString())))} があるべき {(CurrentToken().Kind == Token.TokenKind.EOF ? "ですがファイル終端に到達しました。" : $"場所に{CurrentToken().Raw} があります。")} ");
        }

        /// <summary>
        /// 現在のトークンの種別が トークン種別候補 candidates に含まれるなら読み取って返す。
        /// 含まれないなら 例外 SyntaxErrorException を投げる
        /// トークン種別候補 candidates が空の場合はどんなトークンでもいいので一つ読むという動作になる。
        /// </summary>
        /// <param name="candidates"></param>
        public Token ReadToken(params char[] candidates) {
            return ReadToken(candidates.Select(x => (Token.TokenKind)x).ToArray());
        }


        /// <summary>
        /// 現在のトークンの種別が トークン種別候補 candidates に含まれるかどうかを調べる。。
        /// </summary>
        /// <param name="candidates"></param>
        public bool PeekToken(params Token.TokenKind[] candidates) {
            return candidates.Contains(CurrentToken().Kind);
        }

        /// <summary>
        /// 現在のトークンの種別が トークン種別候補 candidates に含まれるかどうかを調べ、含まれる場合は t にそのトークンの情報を格納する
        /// </summary>
        public bool PeekToken(out Token t, params Token.TokenKind[] candidates) {
            if (candidates.Contains(CurrentToken().Kind)) {
                t = CurrentToken();
                return true;
            } else {
                t = null;
                return false;
            }
        }

        /// <summary>
        /// 現在のトークンの種別が トークン種別候補 candidates に含まれるかどうかを調べる。。
        /// </summary>
        public bool PeekToken(params char[] candidates) {
            return PeekToken(candidates.Select(x => (Token.TokenKind)x).ToArray());
        }

        /// <summary>
        /// 現在のトークンの種別が トークン種別候補 candidates に含まれるかどうかを調べ、含まれる場合は t にそのトークンの情報を格納する
        /// </summary>
        public bool PeekToken(out Token t, params char[] candidates) {
            if (PeekToken(candidates.Select(x => (Token.TokenKind)x).ToArray())) {
                t = CurrentToken();
                return true;
            } else {
                t = null;
                return false;
            }
        }

        /// <summary>
        /// 現在のトークンの種別が トークン種別候補 candidates に含まれるかどうかを調べ、含まれる場合は読み取る。
        /// </summary>
        public bool ReadTokenIf(params Token.TokenKind[] candidates) {
            if (PeekToken(candidates)) {
                ReadToken(candidates);
                return true;
            } else {
                return false;
            }
        }

        /// <summary>
        /// 現在のトークンの種別が トークン種別候補 candidates に含まれるかどうかを調べ、含まれる場合は読み取り、 t にそのトークンの情報を格納する
        /// </summary>

        public bool ReadTokenIf(out Token t, params Token.TokenKind[] candidates) {
            if (PeekToken(candidates)) {
                t = ReadToken(candidates);
                return true;
            } else {
                t = null;
                return false;
            }
        }

        /// <summary>
        /// 現在のトークンの種別が トークン種別候補 candidates に含まれるかどうかを調べ、含まれる場合は読み取る。
        /// </summary>
        public bool ReadTokenIf(params char[] candidates) {
            if (PeekToken(candidates)) {
                ReadToken(candidates);
                return true;
            } else {
                return false;
            }
        }

        /// <summary>
        /// 現在のトークンの種別が トークン種別候補 candidates に含まれるかどうかを調べ、含まれる場合は読み取り、 t にそのトークンの情報を格納する
        /// </summary>
        public bool ReadTokenIf(out Token t, params char[] candidates) {
            if (PeekToken(candidates)) {
                t = ReadToken(candidates);
                return true;
            } else {
                t = null;
                return false;
            }
        }

        /// <summary>
        /// 次のトークンの種別が トークン種別候補 candidates に含まれるかどうかを調べる。
        /// </summary>
        public bool PeekNextToken(params Token.TokenKind[] candidates) {
            if (_tokens.Count <= _currentTokenPos + 1) {
                ScanToken();
                if (is_eof()) {
                    return false;
                }
            }
            return candidates.Contains(_tokens[_currentTokenPos + 1].Kind);
        }

        /// <summary>
        /// 次のトークンの種別が トークン種別候補 candidates に含まれるかどうかを調べる。
        /// </summary>
        public bool PeekNextToken(params char[] candidates) {
            return PeekNextToken(candidates.Select(x => (Token.TokenKind)x).ToArray());
        }

        /// <summary>
        /// 現在の読み取り位置についての情報を保存する
        /// </summary>
        /// <returns></returns>
        public int Save() {
            return _currentTokenPos;
        }

        /// <summary>
        /// 現在の読み取り位置についての情報を復帰する
        /// </summary>
        /// <returns></returns>
        public void Restore(int context) {
            _currentTokenPos = context;
        }
    }
}
