using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;

namespace CSCPP {
    public static class Lex {

        // マクロ展開した要素を再度マクロ展開する時に使われる再挿入バッファのスタック
        private static Stack<List<Token>> Buffers { get; } = new Stack<List<Token>>();

        public static void Init() {
            Buffers.Clear();
            Buffers.Push(new List<Token>());
        }

        /// <summary>
        /// ファイルfilenameを開いて読み取りソースに設定する
        /// </summary>
        /// <param name="filename">-を与えた場合は標準入力を読み取り元として開く</param>
        public static void Set(string filename) {
            if (filename == "-") {
                File.stream_push(new File(Console.In, "-"));
            } else if (System.IO.File.Exists(filename) == false) {
                CppContext.Error($"ファイル {filename} を開けません。");
            } else {
                var sr = Encoding.CreateStreamReaderFromFile(filename, CppContext.AutoDetectEncoding, CppContext.DefaultEncoding);
                if (sr == null) {
                    return;
                }
                File.stream_push(new File(sr, filename));
            }
        }

        static Utf32Char PeekChar() {
            var r = File.ReadCh();
            File.UnreadCh(r);
            return r;
        }

        static bool IsNextChar(int expect) {
            var c = File.ReadCh();
            if (c == (char)expect)
                return true;
            File.UnreadCh(c);
            return false;
        }
        static bool IsNextStr(string str) {
            Stack<Utf32Char> chars = new Stack<Utf32Char>();
            foreach (var ch in str) {
                var c = File.ReadCh();
                chars.Push(c);
                if (c != ch) {
                    while (chars.Any()) {
                        File.UnreadCh(chars.Pop());
                    }
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 現在の行を行末以外読み飛ばす
        /// </summary>
        static string ReadLineComment() {
            return "//"+SkipCurrentLine();
        }

        /// <summary>
        /// 無効区間とみなして行末まで読み飛ばす
        /// </summary>
        static string SkipCurrentLine() {
            bool escape = false;
            StringBuilder sb = new StringBuilder();
            for (var c = File.Get(skipBadchar: true); (!c.IsEof()); c = File.Get(skipBadchar: true)) {
                if (escape) {
                    escape = false;
                    sb.Append(c);
                    continue;
                } else if (c == '\\') {
                    escape = true;
                    continue;
                } else if (c == '\n') {
                    File.UnreadCh(c);
                    break;
                } else { 
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// 空白要素（空白文字orコメント）を一つ読み飛ばす
        /// </summary>
        /// <returns>読み飛ばしたら真</returns>
        static bool SkipOneSpace(SpaceInfo sb, bool disableComment, bool handleEof, bool limitSpace) {
            var c = File.ReadCh(handleEof: handleEof);

            if (c.IsEof()) {
                if (handleEof)
                {
                    File.UnreadCh(c);
                }
                return false;
            }
            if (c.IsWhiteSpace())
            {
                if (limitSpace && (c == '\f' || c == '\v')) {
                    // 6.10前処理指令
                    // 前処理指令の中（先頭の#前処理字句の直後から，最後の改行文字の直前まで）の前処理字句の間 に現れてよい空白類文字は，空白と水平タブだけとする
                    if (CppContext.Warnings.Contains(Warning.Pedantic))
                    {
                        CppContext.Warning(c.Position, $"前処理指令の中で使えない空白文字 U+{c.Code:x8} が使われています。");
                    }
                }
                sb.Append(c.Position, (char) c);
                return true;
            }
            if (c == '/') {
                if (disableComment == false && IsNextChar('*')) {
                    var commentStr = ReadBlockComment(c.Position);
                    if (!CppContext.Switchs.Contains("-C")) {
                        // コメントを保持しないオプションが有効の場合は、行を空白で置き換えてしまう
                        commentStr = new string(commentStr.Where(y => y == '\n').ToArray());
                        if (commentStr.Any() == false) {
                            commentStr = " ";
                        }
                        //commentStr = " ";
                    }
                    sb.Append(c.Position, commentStr);
                    return true;
                } else if (disableComment == false && PeekChar() == '/') {
                    if (CppContext.Warnings.Contains(Warning.LineComment)) {
                        CppContext.Warning(c.Position, "ISO/IEC 9899-1999 で導入された行コメントが利用されています。ISO/IEC 9899-1990 ではコメントとして扱われないため注意してください。");
                    }
                    if (CppContext.Features.Contains(Feature.LineComment)) {
                        // 行コメントオプション有効時
                        System.Diagnostics.Debug.Assert(File.ReadCh() == '/');
                        var commentStr = ReadLineComment();
                        if (!CppContext.Switchs.Contains("-C")) {
                            // コメントを保持しないオプションが有効の場合は、空白で置き換えてしまう
                            commentStr = " ";
                        }
                        sb.Append(c.Position, commentStr);
                        return true;
                    } else {
                        // 行コメントオプション無効時。
                    }
                }
            }
            File.UnreadCh(c);
            return false;
        }

        /// <summary>
        /// 一つ以上の空白を読み飛ばす
        /// </summary>
        /// <param name="sb"></param>
        /// <param name="disableComment"></param>
        /// <param name="handleEof"></param>
        /// <param name="limitSpace"></param>
        /// <returns>空白を読み飛ばしたら真</returns>
        static bool SkipManySpaces(SpaceInfo sb, bool disableComment = false, bool handleEof = false, bool limitSpace = false) {
            while (SkipOneSpace(sb, disableComment: disableComment, handleEof: handleEof, limitSpace: limitSpace)) {
            }
            return sb.chunks.Any();
        }

        /// <summary>
        /// 文字リテラルを読み飛ばす。
        /// 条件コンパイルで無効となった範囲内でのみ使う。
        /// </summary>
        static void SkipCharacterLiteral() {
            if (File.ReadCh() == '\\') {
                File.ReadCh(skipBadchar: true);
            }
            var c = File.ReadCh(skipBadchar: true);
            while ((!c.IsEof()) && c != '\'') {
                c = File.ReadCh(skipBadchar: true);
            }
        }

        /// <summary>
        /// 文字列リテラルを読み飛ばす。
        /// 条件コンパイルで無効となった範囲内でのみ使う。
        /// </summary>
        static void SkipStringLiteral() {
            for (var c = File.ReadCh(skipBadchar: true); (!c.IsEof()) && c != '"'; c = File.ReadCh(skipBadchar: true)) {
                if (c == '\\') {
                    File.ReadCh(skipBadchar: true);
                }
            }
        }

        /// <summary>
        /// #ifや#ifdefなどの条件コンパイル指令で無効となっている範囲の読み飛ばしを行う
        /// 
        /// ISO/IEC 9899-1999 6.10 前処理指令 で
        /// 「読み飛ばされるグループ（6.10.1）の中では，指令の構文規則を緩和して，指令の名前とそれに続く改行文字の間に任意の前処理字句の並びを置くことを可能とする。」
        /// と記述されているように、読み飛ばされる範囲に前処理字句として妥当ではないトークンが出現してはいけないことが規定されている。
        /// でも現実のプリプロセッサ実装のほとんどは上記のチェックを行っていない。
        /// ということで非標準動作となってしまうが、トークンの確認については行わずに処理する実装になっている。
        /// </summary>
        public static void skip_cond_incl() {
            int nest = 0;
            var dummy = new SpaceInfo();    // SkipManySpaces用
            for (;;) {
                bool bol = (File.current_file().Column == 1);
                SkipManySpaces(dummy);

                var c = File.ReadCh(skipBadchar: true);
                if (c.IsEof()) {
                    break;
                }
                if (c == '\'') {
                    SkipCharacterLiteral();
                    continue;
                }
                if (c == '\"') {
                    SkipStringLiteral();
                    continue;
                }
                if (c != '#' || !bol) {
                    // 行頭ではない場合や、行頭だけど#ではない場合は読み飛ばす
                    continue;
                }


                //
                // 以降は条件コンパイル指令の処理
                //

                Token hash = Token.make_keyword(c.Position, (Token.Keyword)'#');
                hash.BeginOfLine = true;

                Token tok = LexToken(); // 前処理指令部分なのでskip_badcharは使わない
                if (tok.Kind != Token.TokenKind.Ident) {
                    continue;
                } else if (nest == 0 && (tok.IsIdent("else", "elif", "endif"))) {
                    // 現在アクティブなスコープレベルで#else/#elif/#endifが出てきたので
                    // ディレクティブを読み戻して読み飛ばし動作を終了
                    unget_token(tok);
                    unget_token(hash);
                    break;
                } else if (tok.IsIdent("if", "ifdef", "ifndef")) {
                    // ネストを1段深くする
                    nest++;
                } else if (nest != 0 && tok.IsIdent("endif")) {
                    // ネストを1段浅くする
                    nest--;
                } else {
                    // 他は無視する
                }
                // 行末まで読み飛ばし
                SkipCurrentLine();
            }
        }

        // Reads a number literal. Lexer's grammar on numbers is not strict.
        // Integers and floating point numbers and different base numbers are not distinguished.
        /// <summary>
        /// 数値リテラルを読み取る
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="ch"></param>
        /// <returns></returns>
        static Token ReadNumberLiteral(Position pos, Utf32Char ch) {
            StringBuilder b = new StringBuilder();
            b.Append((char)ch);
            Utf32Char last = ch;   // 最後に読んだ文字を保持しておく。
            for (;;) {
                ch = File.ReadCh();
                bool flonum = "eEpP".IndexOf((char)last) >= 0 && "+-".IndexOf((char)ch) >= 0;
                if (ch.IsDigit() || ch.IsAlpha() || ch == '.' || flonum) {
                    b.Append((char)ch);
                    last = ch;
                } else {
                    // 読んだ文字を読み戻す
                    File.UnreadCh(ch);
                    return Token.make_number(pos, b.ToString());
                }
            }
        }




        static bool IsNextOctal() {
            return PeekChar().IsOctal();
        }

        static string read_octal_char2(Utf32Char c) {
            string r = $@"\{c}";
            if (!IsNextOctal())
                return r;
            r += (char)File.ReadCh();
            if (!IsNextOctal())
                return r;
            r += (char)File.ReadCh();
            return r;
        }

        static string read_hex_char2() {
            var c = File.ReadCh();
            if (!c.IsXdigit()) {
                File.UnreadCh(c);
                return @"\x";
            }
            string r = @"\x";
            for (; ; c = File.ReadCh()) {
                if (c.IsXdigit()) {
                    r += c;
                } else { 
                    File.UnreadCh(c);
                    return r;
                }
            }
        }

        static string read_escaped_char_string() {
            var c = File.ReadCh();
            // This switch-cases is an interesting example of magical aspects
            // of self-hosting compilers. Here, we teach the compiler about
            // escaped sequences using escaped sequences themselves.
            // This is a tautology. The information about their real character
            // codes is not present in the source code but propagated from
            // a compiler compiling the source code.
            // See "Reflections on Trusting Trust" by Ken Thompson for more info.
            // http://cm.bell-labs.com/who/ken/trust.html
            switch ((char)c) {
                case '\'':
                case '"':
                case '?':
                case '\\':
                    return $@"\{c}";
                case 'a': return @"\a";
                case 'b': return @"\b";
                case 'f': return @"\f";
                case 'n': return @"\n";
                case 'r': return @"\r";
                case 't': return @"\t";
                case 'v': return @"\v";
                case 'x': return read_hex_char2();
                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                    return read_octal_char2(c);
            }
            CppContext.Warning(c.Position, $@"\{c} は未知のエスケープ文字です。");
            return $@"\{c}";
        }

        /// <summary>
        /// 文字定数の読み取り
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        static Token read_char(Position pos, Token.EncType encType) {
            StringBuilder b = new StringBuilder();
            for (;;) {
                var c = File.ReadCh();
                if (c.IsEof() || c == '\n') {
                    CppContext.Error(pos, "文字定数が ' で終端していません。");
                    File.UnreadCh(c);
                    break;
                }
                if (c == '\'')
                    break;
                if (c != '\\') {
                    b.Append(c);
                    continue;
                }
                var str = read_escaped_char_string();
                b.Append(str);
            }
            return Token.make_char(pos, b.ToString(), encType);
        }
        /// <summary>
        /// 文字列定数の読み取り
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        static Token read_string(Position pos, Token.EncType encType) {
            StringBuilder b = new StringBuilder();
            for (;;) {
                var c = File.ReadCh();
                if (c.IsEof() || c == '\n') {
                    CppContext.Error(pos, "文字列が \" で終端していません。");
                    File.UnreadCh(c);
                    break;
                }
                if (c == '"')
                    break;
                if (c != '\\') {
                    b.Append(c);
                    continue;
                }
                var str = read_escaped_char_string();
                b.Append(str);
            }
            return Token.make_strtok(pos, b.ToString(), encType);
        }

        /// <summary>
        /// 識別子の読み取り
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        static Token read_ident(Position pos, Utf32Char c) {
            StringBuilder b = new StringBuilder();
            b.Append(c);
            for (;;) {
                c = File.ReadCh();
                if ((!c.IsEof()) && (c.IsAlNum() || c == '_')) {
                    b.Append(c);
                    continue;
                }
                File.UnreadCh(c);
                return Token.make_ident(pos, b.ToString());
            }
        }

        /// <summary>
        /// ブロックコメントの読み取り
        /// </summary>
        static string ReadBlockComment(Position startPosition) {
            bool maybeEnd = false;
            bool escape = false;
            var sb = new StringBuilder();
            sb.Append("/*");
            for (;;) {
                var c = File.Get(skipBadchar: true);
                if (c.IsEof()) {
                    // EOFの場合
                    CppContext.Error(startPosition, "ブロックコメントが閉じられないまま、ファイル末尾に到達しました。");
                    break;
                } else if (escape) {
                    // エスケープシーケンスの次の文字の場合
                    escape = false;
                    sb.Append($@"\{c}");
                    maybeEnd = (c == '*');  // /*\*/*/ は */ にならなければならない
                    continue;
                } else if (c == '\\') {
                    // エスケープシーケンスの場合
                    escape = true;
                    maybeEnd = false;
                    continue;
                } else if (c == '/' && maybeEnd) {
                    // コメント終端の場合
                    sb.Append("/");
                    break;
                } else {
                    // それら以外の場合
                    sb.Append(c);
                    maybeEnd = (c == '*');
                    continue;
                }
            }
            return sb.ToString();
        }

        // Reads a digraph starting with '%'. Digraphs are alternative spellings
        // for some punctuation characters. They are useless in ASCII.
        // We implement this just for the standard compliance.
        // See C11 6.4.6p3 for the spec.
        static Token read_hash_digraph(Position pos) {
            if (IsNextChar('>')) {
                return Token.make_keyword(pos, (Token.Keyword)'}');
            }
            if (IsNextChar(':')) {
                var par = PeekChar();
                if (IsNextChar('%')) {
                    if (IsNextChar(':')) {
                        return Token.make_keyword(pos, Token.Keyword.HashHash);
                    }
                    File.UnreadCh(par);
                }
                return Token.make_keyword(pos, (Token.Keyword)'#');
            }
            return null;
        }

        static Token read_rep(Position pos, int expect, Token.Keyword t1, Token.Keyword els) {
            if (IsNextChar(expect)) {
                return Token.make_keyword(pos, t1);
            } else {
                return Token.make_keyword(pos, els);
            }
        }

        static Token read_rep2(Position pos, int expect1, Token.Keyword t1, int expect2, Token.Keyword t2, Token.Keyword els) {
            if (IsNextChar(expect1)) {
                return Token.make_keyword(pos, t1);
            } else if (IsNextChar(expect2)) {
                return Token.make_keyword(pos, t2);
            } else {
                return Token.make_keyword(pos, els);
            }
        }

        static Token do_read_token(bool disableComment = false, bool handleEof = false, bool limitSpace = false)
        {
            var sb = new SpaceInfo();
            if (SkipManySpaces(sb, disableComment: disableComment, handleEof: handleEof, limitSpace: limitSpace)) {
                var pos = sb.chunks.First().Pos;
                return Token.make_space(pos, sb);
            }

            // トークンの開始位置を取得
            var c = File.ReadCh(handleEof: handleEof);
            var tokenPosition = c.Position;
            if (c.IsEof()) {
                return Token.make_eof(tokenPosition);
            }
            switch ((char)c) {
                case '\n':
                    return Token.make_newline(tokenPosition);
                case ':':
                    return Token.make_keyword(tokenPosition, IsNextChar('>') ? (Token.Keyword)']' : (Token.Keyword)':');
                case '#':
                    return Token.make_keyword(tokenPosition, IsNextChar('#') ? Token.Keyword.HashHash : (Token.Keyword)'#');
                case '+':
                    return read_rep2(tokenPosition, '+', Token.Keyword.Inc, '=', Token.Keyword.AssignAdd, (Token.Keyword)'+');
                case '*':
                    return read_rep(tokenPosition, '=', Token.Keyword.AssignMul, (Token.Keyword)'*');
                case '=':
                    return read_rep(tokenPosition, '=', Token.Keyword.Equal, (Token.Keyword)'=');
                case '!':
                    return read_rep(tokenPosition, '=', Token.Keyword.NotEqual, (Token.Keyword)'!');
                case '&':
                    return read_rep2(tokenPosition, '&', Token.Keyword.LogicalAnd, '=', Token.Keyword.AssignAnd, (Token.Keyword)'&');
                case '|':
                    return read_rep2(tokenPosition, '|', Token.Keyword.LogincalOr, '=', Token.Keyword.AssignOr, (Token.Keyword)'|');
                case '^':
                    return read_rep(tokenPosition, '=', Token.Keyword.AssignXor, (Token.Keyword)'^');
                case '"':
                    return read_string(tokenPosition, Token.EncType.None);
                case '\'':
                    return read_char(tokenPosition, Token.EncType.None);
                case '/':
                    return Token.make_keyword(tokenPosition, IsNextChar('=') ? Token.Keyword.AssignDiv : (Token.Keyword)'/');
                case '.':
                    if (PeekChar().IsDigit()) {
                        return ReadNumberLiteral(tokenPosition, c);
                    }
                    if (IsNextChar('.')) {
                        if (IsNextChar('.'))
                            return Token.make_keyword(tokenPosition, Token.Keyword.Ellipsis);
                        return Token.make_ident(tokenPosition, "..");
                    }
                    return Token.make_keyword(tokenPosition, (Token.Keyword)'.');
                case '(':
                case ')':
                case ',':
                case ';':
                case '[':
                case ']':
                case '{':
                case '}':
                case '?':
                case '~':
                    return Token.make_keyword(tokenPosition, (Token.Keyword)(char)c);
                case '-':
                    if (IsNextChar('-'))
                        return Token.make_keyword(tokenPosition, Token.Keyword.Dec);
                    if (IsNextChar('>'))
                        return Token.make_keyword(tokenPosition, Token.Keyword.Arrow);
                    if (IsNextChar('='))
                        return Token.make_keyword(tokenPosition, Token.Keyword.AssignSub);
                    return Token.make_keyword(tokenPosition, (Token.Keyword)'-');
                case '<':
                    if (IsNextChar('<'))
                        return read_rep(tokenPosition, '=', Token.Keyword.AssignShiftArithLeft, Token.Keyword.ShiftArithLeft);
                    if (IsNextChar('='))
                        return Token.make_keyword(tokenPosition, Token.Keyword.LessEqual);
                    if (IsNextChar(':'))
                        return Token.make_keyword(tokenPosition, (Token.Keyword)'[');
                    if (IsNextChar('%'))
                        return Token.make_keyword(tokenPosition, (Token.Keyword)'{');
                    return Token.make_keyword(tokenPosition, (Token.Keyword)'<');
                case '>':
                    if (IsNextChar('='))
                        return Token.make_keyword(tokenPosition, Token.Keyword.GreatEqual);
                    if (IsNextChar('>'))
                        return read_rep(tokenPosition, '=', Token.Keyword.AssignShiftArithRight, Token.Keyword.ShiftArithRight);
                    return Token.make_keyword(tokenPosition, (Token.Keyword)'>');
                case '%': {
                        Token tok = read_hash_digraph(tokenPosition);
                        if (tok != null)
                            return tok;
                        return read_rep(tokenPosition, '=', Token.Keyword.AssignMod, (Token.Keyword)'%');
                    }
                case 'L': {
                        if (IsNextChar('"')) {
                            CppContext.Warning(tokenPosition, "ワイド文字列リテラルが使われています。");
                            return read_string(tokenPosition, Token.EncType.Wide);
                        } else if (IsNextChar('\'')) {
                            CppContext.Warning(tokenPosition, "ワイド文字定数が使われています。");
                            return read_char(tokenPosition, Token.EncType.Wide);
                        } else {
                            goto default;
                        }
                    }
                case 'u': {
                        if (IsNextStr("8\"")) {
                            CppContext.Warning(tokenPosition, "UTF8文字列リテラルが使われています。");
                            return read_string(tokenPosition, Token.EncType.U8);
                        } else if (IsNextChar('"')) {
                            CppContext.Warning(tokenPosition, "UTF16文字列リテラルが使われています。");
                            return read_string(tokenPosition, Token.EncType.U16);
                        } else if (IsNextChar('\'')) {
                            CppContext.Warning(tokenPosition, "UTF16文字定数が使われています。");
                            return read_char(tokenPosition, Token.EncType.U16);
                        } else {
                            goto default;
                        }
                    }
                case 'U': {
                        if (IsNextChar('"')) {
                            CppContext.Warning(tokenPosition, "UTF32文字列リテラルが使われています。");
                            return read_string(tokenPosition, Token.EncType.U32);
                        } else if (IsNextChar('\'')) {
                            CppContext.Warning(tokenPosition, "UTF32文字定数が使われています。");
                            return read_char(tokenPosition, Token.EncType.U32);
                        } else {
                            goto default;
                        }
                    }
                default:
                    if (c.IsAlpha() || (c == '_')) {
                        return read_ident(tokenPosition, c);
                    }
                    if (c.IsDigit()) {
                        return ReadNumberLiteral(tokenPosition, c);
                    }

                    return Token.make_invalid(tokenPosition, $"{c}");
            }
        }


        static bool IsBufferEmpty() {
            return Buffers.Count == 1 && Buffers.Peek().Count == 0;
        }

        /// <summary>
        /// #include に続く指定されているファイルパス部分を読み取る
        /// </summary>
        /// <param name="hash"></param>
        /// <param name="isGuillemet">山括弧で囲まれている場合は真</param>
        /// <returns></returns>
        public static string read_header_file_name(Token hash, out bool isGuillemet) {
            var space = new SpaceInfo();
            StringBuilder path = new StringBuilder();
            if (IsBufferEmpty() == false) {
                isGuillemet = false;
                return null;
            }
            SkipManySpaces(space, limitSpace: true);
            char close;
            if (IsNextChar('"')) {
                isGuillemet = false;
                close = '"';
            } else if (IsNextChar('<')) {
                isGuillemet = true;
                close = '>';
            } else {
                isGuillemet = false;
                return null;
            }

            StringBuilder b = new StringBuilder();
            while (!IsNextChar(close)) {
                var c = File.ReadCh();
                if (c.IsEof() || c == '\n') {
                    CppContext.Error(hash, "ファイルパスが閉じられないまま行末に到達しました。");
                    File.UnreadCh(c);
                    break;
                }
                b.Append(c);
                path.Append(c);
            }

            if (b.Length == 0) {
                CppContext.Error(hash, "includeで指定されたファイル名が空です。");
            }
            return b.ToString();
        }


        // Temporarily switches the input token stream to given list of tokens,
        // so that you can get the tokens as return values of Lex.LexToken() again.
        // After the tokens are exhausted, File.EOF is returned from Lex.LexToken() until
        // "unstash" is called to restore the original state.
        public static void token_buffer_stash(List<Token> buf) {
            Buffers.Push(buf);
        }

        public static void token_buffer_unstash() {
            Buffers.Pop();
        }

        public static void unget_token(Token tok) {
//            if (tok.Kind == Token.TokenKind.EoF)
//                return;
            List<Token> buf = Buffers.Peek();
            buf.Add(tok);
        }

        public static List<Token> lex_string(Macro m, Position p, string s) {
            File.stream_stash(new File(s, p.FileName));
            List<Token> ret = new List<Token>();
            Token tok = do_read_token(disableComment: true);   /* 連結演算子の処理中ではコメントは扱えない */
            ret.Add(tok);
            IsNextChar('\n');

            tok = do_read_token();
            if (tok.Kind != Token.TokenKind.EoF) {
                CppContext.Error(p, $"マクロ {m.GetName()} 中のトークン連結演算子 ## の結果 {s} は不正なプリプロセッサトークンです。(マクロ {m.GetName()} は {m.GetFirstPosition().ToString()} で宣言されています。)");
                while (tok.Kind != Token.TokenKind.EoF) {
                    ret.Add(tok);
                    IsNextChar('\n');
                    tok = do_read_token();
                }
            }
            File.stream_unstash();
            return ret;
        }

        public static Token LexToken(bool handleEof = false, bool limitSpace = false)
        {
            List<Token> buf = Buffers.Peek();
            if (buf.Count > 0) {
                // 挿入されたトークンバッファにトークンがあるのでそれを返す
                var p = buf.Pop();
                if (p.Kind == Token.TokenKind.MacroRangeFixup) {
                    CppContext.ExpandLog.Add(Tuple.Create(
                        p.MacroRangeFixupTok,
                        p.MacroRangeFixupMacro,
                        p.MacroRangeFixupStartLine,
                        p.MacroRangeFixupStartColumn,
                        CppContext.CppWriter.OutputLine,
                        CppContext.CppWriter.OutputColumn
                    ));
                    return LexToken(handleEof, limitSpace);
                } else {
                    return p;
                }
            }
            if (Buffers.Count > 1) {
                // 挿入されたトークンバッファにトークンが無いのでEOFを返す
                return Token.make_eof(new Position(File.current_file().Name, File.current_file().Line, File.current_file().Column));
            }

            //
            // 挿入されたトークンバッファもないので新しくファイルから読み取る
            //
            bool bol = (File.current_file().Column == 1);
            Token tok = do_read_token(handleEof: handleEof, limitSpace: limitSpace);
            var space = tok.Space;
            if (tok.Kind == Token.TokenKind.Space) {
                tok = do_read_token();
                while (tok.Kind == Token.TokenKind.Space) {
                    space.chunks.AddRange(tok.Space.chunks);
                    tok = do_read_token(handleEof: handleEof, limitSpace: limitSpace);
                }
            }
            tok.Space = space;
            tok.BeginOfLine = bol;

            return tok;
        }


        public static Token ExceptKeyword(char id) {
            Token tok = LexToken();
            if (!tok.IsKeyword(id)) {
                if (tok.Kind == Token.TokenKind.EoF) {
                    CppContext.Error(tok, $"{Token.KeywordToStr((Token.Keyword)id)} がありません。");
                } else {
                    CppContext.Error(tok, $"{Token.KeywordToStr((Token.Keyword)id)} があるべき場所に {Token.TokenToStr(tok)} がありました。");
                }
                unget_token(tok);
                return Token.make_invalid(tok.Position, "");
            }
            return tok;
        }
        public static Token ExceptKeyword(char id, Action<Token> failHandler) {
            Token tok = LexToken();
            if (!tok.IsKeyword(id)) {
                failHandler(tok);
                unget_token(tok);
                return Token.make_invalid(tok.Position, "");
            }
            return tok;
        }

        public static bool NextKeyword(Token.Keyword id) {
            Token tok = LexToken();
            if (tok.IsKeyword(id)) {
                return true;
            }
            unget_token(tok);
            return false;
        }

    }
}