using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace AnsiCParser {
    /// <summary>
    /// 字句解析器
    /// </summary>
    public class Lexer {

        private static string D { get; } = $@"[0-9]";
        private static string L { get; } = $@"[a-zA-Z_]";
        private static string H { get; } = $@"[a-fA-F0-9]";
        private static string E { get; } = $@"[Ee][+-]?{D}+";
        private static string FS { get; } = $@"(f|F|l|L)?";
        private static string IS { get; } = $@"(u|U|l|L)*";
        private static Regex RegexPreprocessingNumber { get; } = new Regex($@"^(\.?\d([eEpP][\+\-]|\.|({L}|{D}|_))*)$");
        private static Regex RegexFloat { get; } = new Regex($@"^(?<Body>{D}+{E}|{D}*\.{D}+({E})?|{D}+\.{D}*({E})?)(?<Suffix>{FS})$");
        private static Regex RegexHeximal { get; } = new Regex($@"^0[xX](?<Body>{H}+)(?<Suffix>{IS})$");
        private static Regex RegexDecimal { get; } = new Regex($@"^(?<Body>{D}+)(?<Suffix>{IS})$");
        private static Regex RegexOctal { get; } = new Regex($@"^0(?<Body>{D}+)(?<Suffix>{IS})$");
        private static Regex RegexChar { get; } = new Regex($@"^L?'(\.|[^\'])+'$");
        private static Regex RegexStringLiteral { get; } = new Regex($@"^L?""(\.|[^\""])*""$");


        public static Tuple<string, string> ParseFloat(string str) {
            var m = RegexFloat.Match(str);
            if (m.Success == false) {
                throw new Exception();
            }
            return Tuple.Create(m.Groups["Body"].Value, String.Concat(m.Groups["Suffix"].Value.ToUpper().ToCharArray().OrderBy(x => x)));
        }
        public static Tuple<string, string> ParseHeximal(string str) {
            var m = RegexHeximal.Match(str);
            if (m.Success == false) {
                throw new Exception();
            }
            return Tuple.Create(m.Groups["Body"].Value, String.Concat(m.Groups["Suffix"].Value.ToUpper().ToCharArray().OrderBy(x => x)));
        }
        public static Tuple<string, string> ParseDecimal(string str) {
            var m = RegexDecimal.Match(str);
            if (m.Success == false) {
                throw new Exception();
            }
            return Tuple.Create(m.Groups["Body"].Value, String.Concat(m.Groups["Suffix"].Value.ToUpper().ToCharArray().OrderBy(x => x)));
        }
        public static Tuple<string, string> ParseOctal(string str) {
            var m = RegexOctal.Match(str);
            if (m.Success == false) {
                throw new Exception();
            }
            return Tuple.Create(m.Groups["Body"].Value, String.Concat(m.Groups["Suffix"].Value.ToUpper().ToCharArray().OrderBy(x => x)));
        }

        /// <summary>
        /// 識別子の先頭に出現できる文字なら真
        /// </summary>
        /// <param name="ch"></param>
        /// <returns></returns>
        private bool IsIdentifierHead(int ch) {
            return ('A' <= ch && ch <= 'Z') || ('a' <= ch && ch <= 'z') || (ch == '_');
        }

        /// <summary>
        /// 識別子の先頭以外に出現できる文字なら真
        /// </summary>
        /// <param name="ch"></param>
        /// <returns></returns>
        private bool IsIdentifierBody(int ch) {
            return ('A' <= ch && ch <= 'Z') || ('a' <= ch && ch <= 'z') || ('0' <= ch && ch <= '9') || (ch == '_');
        }

        /// <summary>
        /// 数字なら真
        /// </summary>
        /// <param name="ch"></param>
        /// <returns></returns>
        private bool IsDigit(int ch) {
            return ('0' <= ch && ch <= '9');
        }

        /// <summary>
        /// 空白文字なら真
        /// </summary>
        /// <param name="ch"></param>
        /// <returns></returns>
        private bool IsSpace(int ch) {
            return "\r\n\v\f\t ".Any(x => (int)x == ch);
        }

        /// <summary>
        /// ファイル終端なら真
        /// </summary>
        /// <returns></returns>
        public bool is_eof() {
            return _tokens[current].Kind == Token.TokenKind.EOF;
        }

        /// <summary>
        /// 予約語
        /// </summary>
        private Dictionary<string, Token.TokenKind> reserve_words = new Dictionary<string, Token.TokenKind>() {
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
        private List<Tuple<string, Token.TokenKind>> symbols = new List<Tuple<string, Token.TokenKind>>() {
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

        private string _inputText;
        private int _inputPos;

        private bool _beginOfLine;

        private string filepath;
        private int line;
        private int column;

        /// <summary>
        /// 得られたトークンの列
        /// </summary>
        private List<Token> _tokens { get; } = new List<Token>();

        /// <summary>
        /// 現在のトークンの読み取り位置
        /// </summary>
        private int current = 0;

        public Lexer(string source, string Filepath = "") {
            _inputText = source;
            _inputPos = 0;
            _beginOfLine = true;
            filepath = Filepath;
            line = 1;
            column = 1;
        }

        private Location _getLocation() {
            return new Location(filepath, line, column, _inputPos);
        }

        private string Substring(Location start, Location end) {
            return _inputText.Substring(start.Position, end.Position - start.Position);
        }

        private void IncPos(int n) {
            for (int i = 0; i < n; i++) {
                if (_inputText[_inputPos + i] == '\n') {
                    line++;
                    column = 1;
                } else {
                    column++;
                }
            }
            _inputPos += n;
        }

        public int scanch(int offset = 0) {
            if (_inputPos + offset >= _inputText.Length) {
                return -1;
            } else {
                return _inputText[_inputPos + offset];
            }
        }

        public bool scanch(string s) {
            for (var i = 0; i < s.Length; i++) {
                if (scanch(i) != s[i]) {
                    return false;
                }
            }
            return true;
        }
        public bool scan() {
            if (_tokens.LastOrDefault()?.Kind == Token.TokenKind.EOF) {
                return false;
            }
            rescan:
            while (IsSpace(scanch())) {
                if (scanch("\n")) {
                    _beginOfLine = true;
                }
                IncPos(1);
            }
            if (scanch("/*")) {
                IncPos(2);

                bool terminated = false;
                while (_inputPos < _inputText.Length) {
                    if (scanch("\\")) {
                        IncPos(2);
                    } else if (scanch("*/")) {
                        IncPos(2);
                        terminated = true;
                        break;
                    } else {
                        IncPos(1);
                    }
                }
                if (terminated == false) {
                    _tokens.Add(new Token(Token.TokenKind.EOF, _getLocation(), _getLocation(), ""));
                    return false;
                }
                goto rescan;
            }
            if (scanch("//")) {
                IncPos(2);

                bool terminated = false;
                while (_inputPos < _inputText.Length) {
                    if (scanch("\\")) {
                        IncPos(2);
                    } else if (scanch("\n")) {
                        terminated = true;
                        break;
                    } else {
                        IncPos(1);
                    }
                }
                if (terminated == false) {
                    _tokens.Add(new Token(Token.TokenKind.EOF, _getLocation(), _getLocation(), ""));
                    return false;
                }
                goto rescan;
            }

            if (scanch() == -1) {
                _tokens.Add(new Token(Token.TokenKind.EOF, _getLocation(), _getLocation(), ""));
                return false;
            } else if (scanch("#")) {
                var start = _getLocation();
                if (_beginOfLine) {
                    // pragma は特殊
                    while (scanch("\n") == false) {
                        IncPos(1);
                    }
                    IncPos(1);
                    goto rescan;
                } else {
                    _tokens.Add(new Token((Token.TokenKind)'#', start, _getLocation(), "#"));
                    IncPos(1);
                }
                return true;
            }

            _beginOfLine = false;

            if (IsIdentifierHead(scanch())) {
                var start = _getLocation();
                while (IsIdentifierBody(scanch())) {
                    IncPos(1);
                }
                var end = _getLocation();
                var str = Substring(start, end);
                Token.TokenKind reserveWordId;
                if (reserve_words.TryGetValue(str, out reserveWordId)) {
                    _tokens.Add(new Token(reserveWordId, start, end, str));
                } else {
                    _tokens.Add(new Token(Token.TokenKind.IDENTIFIER, start, end, str));
                }
                return true;
            } else if ((scanch(0) == '.' && IsDigit(scanch(1))) || IsDigit(scanch(0))) {
                // preprocessor number
                // \.?\d([eEpP][\+\-]|\.|({L}|{D}|_))*
                var start = _getLocation();
                if (scanch() == '.') {
                    IncPos(1);
                }
                IncPos(1);
                while (scanch() != -1) {
                    if ("eEpP".Any(x => (int)x == scanch(0)) && "+-".Any(x => (int)x == scanch(1))) {
                        IncPos(2);
                    } else if (scanch(".") || IsIdentifierBody(scanch())) {
                        IncPos(1);
                    } else {
                        break;
                    }
                }
                var end = _getLocation();
                var str = Substring(start, end);
                if (RegexFloat.IsMatch(str)) {
                    _tokens.Add(new Token(Token.TokenKind.FLOAT_CONSTANT, start, end, str));
                } else if (RegexHeximal.IsMatch(str)) {
                    _tokens.Add(new Token(Token.TokenKind.HEXIMAL_CONSTANT, start, end, str));
                } else if (RegexOctal.IsMatch(str)) {
                    _tokens.Add(new Token(Token.TokenKind.OCTAL_CONSTANT, start, end, str));
                } else if (RegexDecimal.IsMatch(str)) {
                    _tokens.Add(new Token(Token.TokenKind.DECIAML_CONSTANT, start, end, str));
                } else {
                    throw new Exception();
                }
                return true;
            } else if (scanch("'")) {
                var start = _getLocation();
                IncPos(1);
                while (_inputPos < _inputText.Length) {
                    if (scanch("\\")) {
                        IncPos(2);
                    } else if (scanch("'")) {
                        IncPos(1);
                        var end = _getLocation();
                        var str = Substring(start, end);
                        _tokens.Add(new Token(Token.TokenKind.STRING_CONSTANT, start, end, str));
                        return true;
                    } else {
                        IncPos(1);
                    }
                }
                throw new Exception();
            } else if (scanch("\"")) {
                var start = _getLocation();
                IncPos(1);
                while (_inputPos < _inputText.Length) {
                    if (scanch("\\")) {
                        IncPos(2);
                    } else if (scanch("\"")) {
                        IncPos(1);
                        var end = _getLocation();
                        var str = Substring(start, end);
                        _tokens.Add(new Token(Token.TokenKind.STRING_LITERAL, start, end, str));
                        return true;
                    } else {
                        IncPos(1);
                    }
                }
                throw new Exception();
            } else {
                var start = _getLocation();
                foreach (var sym in symbols) {
                    if (scanch(sym.Item1)) {
                        IncPos(sym.Item1.Length);
                        var end = _getLocation();
                        var str = Substring(start, end);
                        _tokens.Add(new Token(sym.Item2, start, end, str));
                        return true;
                    }
                }
                throw new Exception();
            }
        }
        public Token current_token() {
            if (_tokens.Count == current) {
                scan();
            }
            return _tokens[current];
        }

        public void next_token() {
            current++;
        }


        public void Read(params Token.TokenKind[] s) {
            if (s.Contains(current_token().Kind)) {
                next_token();
                return;
            }
            throw new Exception();
        }
        public void Read(params char[] s) {
            Read(s.Select(x => (Token.TokenKind)x).ToArray());
        }
        public bool Peek(params Token.TokenKind[] s) {
            return s.Contains(current_token().Kind);
        }
        public bool Peek(params char[] s) {
            return Peek(s.Select(x => (Token.TokenKind)x).ToArray());
        }
        public bool ReadIf(params Token.TokenKind[] s) {
            if (Peek(s)) {
                Read(s);
                return true;
            } else {
                return false;
            }
        }
        public bool ReadIf(params char[] s) {
            if (Peek(s)) {
                Read(s);
                return true;
            } else {
                return false;
            }
        }

        public bool is_nexttoken(params Token.TokenKind[] s) {
            if (_tokens.Count <= current + 1) {
                scan();
                if (is_eof()) {
                    return false;
                }
            }
            return s.Contains(_tokens[current + 1].Kind);
        }
        public bool is_nexttoken(params char[] s) {
            return is_nexttoken(s.Select(x => (Token.TokenKind)x).ToArray());
        }

        public int Save() {
            return current;
        }
        public void Restore(int context) {
            current = context;
        }
    }
}