using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace AnsiCParser {
    /// <summary>
    /// 字句解析器
    /// </summary>
    public class Lexer {

        private static string D { get; } = @"[0-9]";
        //private static string L { get; } = @"[a-zA-Z_]";
        private static string H { get; } = @"[a-fA-F0-9]";
        private static string E { get; } = $@"[Ee][+-]?{D}+";
        private static string FS { get; } = @"(f|F|l|L)?";
        private static string IS { get; } = @"(u|U|l|L)*";
        //private static Regex RegexPreprocessingNumber { get; } = new Regex($@"^(\.?\d([eEpP][\+\-]|\.|({L}|{D}|_))*)$");
        private static Regex RegexFloat { get; } = new Regex($@"^(?<Body>{D}+{E}|{D}*\.{D}+({E})?|{D}+\.{D}*({E})?)(?<Suffix>{FS})$");
        private static Regex RegexHeximal { get; } = new Regex($@"^0[xX](?<Body>{H}+)(?<Suffix>{IS})$");
        private static Regex RegexDecimal { get; } = new Regex($@"^(?<Body>{D}+)(?<Suffix>{IS})$");
        private static Regex RegexOctal { get; } = new Regex($@"^0(?<Body>{D}+)(?<Suffix>{IS})$");
        //private static Regex RegexChar { get; } = new Regex($@"^L?'(\.|[^\'])+'$");
        //private static Regex RegexStringLiteral { get; } = new Regex($@"^L?""(\.|[^\""])*""$");


        public static Tuple<string, string> ParseFloat(string str) {
            var m = RegexFloat.Match(str);
            if (m.Success == false) {
                throw new Exception();
            }
            return Tuple.Create(m.Groups["Body"].Value, String.Concat(m.Groups["Suffix"].Value.ToCharArray().OrderBy(x => x)));
        }

        public static Tuple<string, string> ParseHeximal(string str) {
            var m = RegexHeximal.Match(str);
            if (m.Success == false) {
                throw new Exception();
            }
            return Tuple.Create(m.Groups["Body"].Value, String.Concat(m.Groups["Suffix"].Value.ToCharArray().OrderBy(x => x)));
        }
        public static Tuple<string, string> ParseDecimal(string str) {
            var m = RegexDecimal.Match(str);
            if (m.Success == false) {
                throw new Exception();
            }
            return Tuple.Create(m.Groups["Body"].Value, String.Concat(m.Groups["Suffix"].Value.ToCharArray().OrderBy(x => x)));
        }
        public static Tuple<string, string> ParseOctal(string str) {
            var m = RegexOctal.Match(str);
            if (m.Success == false) {
                throw new Exception();
            }
            return Tuple.Create(m.Groups["Body"].Value, String.Concat(m.Groups["Suffix"].Value.ToCharArray().OrderBy(x => x)));
        }

        public static void CharIterator(Func<int> peek, Action next, Action<byte> write) {
            int ret = 0;
            if (peek() == '\\') {
                next();
                switch (peek()) {
                    case '\'': case '"': case '?': case '\\':
                        write((byte)peek());
                        next();
                        return;
                    case 'a': next(); write((byte)'\a'); return;
                    case 'b': next(); write((byte)'\b'); return;
                    case 'f': next(); write((byte)'\f'); return;
                    case 'n': next(); write((byte)'\n'); return;
                    case 'r': next(); write((byte)'\r'); return;
                    case 't': next(); write((byte)'\t'); return;
                    case 'v': next(); write((byte)'\v'); return;
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
                            ret = (ret << 3) | (peek()-'0');
                            next();
                        }
                        write((byte)ret);
                        return;
                    default:
                        throw new CompilerException.SyntaxErrorException(Location.Empty, Location.Empty, "unknown escape character");

                }
            } else {
                ret = peek();
                next();
                write((byte)ret);
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
            if ('0' <= ch && ch <= '9') { return ch - '0'; }
            if ('A' <= ch && ch <= 'F') { return ch - 'A' + 10; }
            if ('a' <= ch && ch <= 'f') { return ch - 'a' + 10; }
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
        /// 読み取り位置が行頭の場合には真になる。前処理指令用。
        /// </summary>
        private bool _beginOfLine;

        /// <summary>
        /// 入力ファイル名
        /// </summary>
        private readonly string _filepath;

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

        private Location GetCurrentLocation() {
            return new Location(_filepath, _line, _column, _inputPos);
        }

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
                    IncPos(1);
                    goto rescan;
                } else {
                    _tokens.Add(new Token((Token.TokenKind)'#', start, GetCurrentLocation(), "#"));
                    IncPos(1);
                }
                return;
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
                        CharIterator(() => Peek(), () => IncPos(1), (b) => { });
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
                        CharIterator(() => Peek(), () => IncPos(1), (b) => { });
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
                throw new Exception();
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

        public void NextToken() {
            _currentTokenPos++;
        }

        /// <summary>
        /// 次のトークンが トークン種別候補 candidates に含まれるなら読み取って返す。
        /// 含まれないなら 例外 SyntaxErrorException を投げる
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
        public Token ReadToken(params char[] candidates) {
            return ReadToken(candidates.Select(x => (Token.TokenKind)x).ToArray());
        }
        public bool PeekToken(params Token.TokenKind[] candidates) {
            return candidates.Contains(CurrentToken().Kind);
        }
        public bool PeekToken(params char[] candidates) {
            return PeekToken(candidates.Select(x => (Token.TokenKind)x).ToArray());
        }
        public bool ReadTokenIf(params Token.TokenKind[] s) {
            if (PeekToken(s)) {
                ReadToken(s);
                return true;
            } else {
                return false;
            }
        }
        public bool ReadTokenIf(params char[] s) {
            if (PeekToken(s)) {
                ReadToken(s);
                return true;
            } else {
                return false;
            }
        }

        public bool PeekNextToken(params Token.TokenKind[] s) {
            if (_tokens.Count <= _currentTokenPos + 1) {
                ScanToken();
                if (is_eof()) {
                    return false;
                }
            }
            return s.Contains(_tokens[_currentTokenPos + 1].Kind);
        }
        public bool PeekNextToken(params char[] s) {
            return PeekNextToken(s.Select(x => (Token.TokenKind)x).ToArray());
        }

        public int Save() {
            return _currentTokenPos;
        }

        public void Restore(int context) {
            _currentTokenPos = context;
        }
    }
}