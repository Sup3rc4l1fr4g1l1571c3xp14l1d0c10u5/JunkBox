using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;

namespace CSCPP {
    public static class Lex {

        /// <summary>
        /// マクロ展開した要素を再度マクロ展開する時に使われる再挿入バッファのスタック
        /// </summary>
        private static Stack<List<Token>> Buffers { get; } = new Stack<List<Token>>();

        /// <summary>
        /// 字句解析器の初期化
        /// </summary>
        public static void Init() {
            Buffers.Clear();
            Buffers.Push(new List<Token>());
        }

        /// <summary>
        /// ファイル filename を開いて読み取りソースに設定する
        /// </summary>
        /// <param name="filename">"-"を与えた場合は標準入力を読み取り元として開く</param>
        public static void Set(string filename) {
            if (filename == "-") {
                File.StreamPush(new File(Console.In, "-"));
            } else if (System.IO.File.Exists(filename) == false) {
                CppContext.Error($"ファイル {filename} を開けません。");
            } else {
                var sr = LibReadJEnc.ReadJEnc.CreateStreamReaderFromFile(filename, CppContext.AutoDetectEncoding, CppContext.DefaultEncoding);
                if (sr == null) {
                    return;
                }
                File.StreamPush(new File(sr, filename));
            }
        }

        /// <summary>
        /// UTF32コードで1文字読み取る
        /// </summary>
        /// <returns>読み取った文字</returns>
        private static Utf32Char PeekChar() {
            var r = File.ReadCh();
            File.UnreadCh(r);
            return r;
        }

        /// <summary>
        /// 文字 ch が読み取れるなら読み取る
        /// </summary>
        /// <param name="ch"></param>
        /// <returns></returns>
        private static bool IsNextChar(int ch) {
            var c = File.ReadCh();
            if (c == (char)ch) {
                return true;
            }
            File.UnreadCh(c);
            return false;
        }

        /// <summary>
        /// 文字列が str が読み取れるなら読み取る
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private static bool IsNextStr(string str) {
            Stack<Utf32Char> chars = new Stack<Utf32Char>();
            foreach (var ch in str) {
                var c = File.ReadCh();
                chars.Push(c);
                if (c != ch) {
                    while (chars.Count > 0) {
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
        /// <returns></returns>
        private static string ReadLineComment() {
            return "//"+SkipCurrentLine();
        }

        /// <summary>
        /// 無効区間とみなして行末まで読み飛ばす
        /// </summary>
        /// <returns></returns>
        private static string SkipCurrentLine() {
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
        /// 空白要素（空白文字orコメント）を一つ読み飛ばし、空白情報を更新する
        /// </summary>
        /// <param name="sb">更新する空白情報</param>
        /// <param name="disableComment">コメントを解析しない場合は真</param>
        /// <param name="handleEof">EoFを読み飛ばさない場合は真</param>
        /// <param name="limitSpace">空白と水平タブ以外を空白文字としない場合（6.10　前処理指令　前処理指令の中の前処理字句の間 に現れてよい空白類文字は，空白と水平タブだけ）は真</param>
        /// <returns>読み飛ばしたら真</returns>
        static bool SkipOneSpace(SpaceInfo sb, bool disableComment, bool handleEof, bool limitSpace) {
            var c = File.ReadCh(handleEof: handleEof);

            if (c.IsEof()) {
                if (handleEof) { File.UnreadCh(c); }
                return false;
            }

            if (c.IsWhiteSpace()) {
                if (limitSpace && (c == '\f' || c == '\v')) {
                    // 6.10前処理指令
                    // 前処理指令の中（先頭の#前処理字句の直後から，最後の改行文字の直前まで）の前処理字句の間 に現れてよい空白類文字は，空白と水平タブだけとする
                    if (CppContext.Warnings.Contains(Warning.Pedantic)) {
                        CppContext.Warning(c.Position, $"前処理指令の中で使えない空白文字 U+{c.Code:x8} が使われています。");
                    }
                }
                sb.Append(c.Position, (char) c);
                return true;
            }

            // コメントの可能性を調査
            if ((c == '/') && (disableComment == false)) {
                if (IsNextChar('*')) {
                    var commentStr = ReadBlockComment(c.Position);
                    if (!CppContext.Switchs.Contains("-C")) {
                        // コメントを保持しないオプションが有効の場合は、行を空白で置き換えてしまう
                        commentStr = new string(commentStr.Where(y => y == '\n').ToArray());
                        if (commentStr.Length == 0) {
                            commentStr = " ";
                        }
                    }
                    sb.Append(c.Position, commentStr);
                    return true;
                } else if (IsNextChar('/')) {
                    if (CppContext.Warnings.Contains(Warning.LineComment)) {
                        CppContext.Warning(c.Position, "ISO/IEC 9899-1999 で導入された行コメントが利用されています。"+
                                                       "ISO/IEC 9899-1990 ではコメントとして扱われないため注意してください。");
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
        /// <param name="disableComment">コメントを解析しない場合は真</param>
        /// <param name="handleEof">EoFを読み飛ばさない場合は真</param>
        /// <param name="limitSpace">空白と水平タブ以外を空白文字としない場合（6.10　前処理指令　前処理指令の中の前処理字句の間 に現れてよい空白類文字は，空白と水平タブだけ）は真</param>
        /// <returns>空白を読み飛ばしたら真</returns>
        static bool SkipManySpaces(SpaceInfo sb, bool disableComment = false, bool handleEof = false, bool limitSpace = false) {
            while (SkipOneSpace(sb, disableComment: disableComment, handleEof: handleEof, limitSpace: limitSpace)) { }
            return sb.Chunks.Count > 0;
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
        public static void SkipCondInclude() {
            int nest = 0;
            var dummy = new SpaceInfo();    // SkipManySpaces用
            for (;;) {
                bool bol = (File.CurrentFile.Column == 1);
                SkipManySpaces(dummy);

                var c = File.ReadCh(skipBadchar: true);
                if (c.IsEof()) {
                    break;
                }
                if (c == '\'') {
                    // 文字リテラル
                    SkipCharacterLiteral();
                    continue;
                }
                if (c == '\"') {
                    // 文字列リテラル
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
                    UngetToken(tok);
                    UngetToken(hash);
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

        /// <summary>
        /// 前処理数リテラルを読み取る
        /// （前処理数の書式については 6.4 字句要素 の例１および 6.4.8 前処理数 を参照せよ）
        /// </summary>
        /// <param name="pos">読み取り開始位置</param>
        /// <param name="ch">最後に読んだ文字</param>
        /// <returns>数値リテラルトークン</returns>
        private static Token ReadNumberLiteral(Position pos, Utf32Char ch) {
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

        /// <summary>
        /// 八進数文字リテラルを読み取る
        /// </summary>
        /// <param name="ch">１文字目</param>
        /// <returns></returns>
        private static string ReadOctalCharLiteralStr(Utf32Char ch) {
            string r = $@"\{ch}";
            if (!PeekChar().IsOctal()) { return r; }
            r += (char)File.ReadCh();
            if (!PeekChar().IsOctal()) { return r; }
            r += (char)File.ReadCh();
            return r;
        }

        /// <summary>
        /// 十六進数文字リテラルの数値部分(\xに続く数値)を読み取る
        /// </summary>
        /// <returns></returns>
        static string ReadHexCharLiteralStr() {
            string r = @"\x";
            var c = File.ReadCh();
            for (; c.IsXdigit(); c = File.ReadCh()) { r += c; }
            File.UnreadCh(c);
            return r;
        }

        /// <summary>
        /// エスケープ文字を読み取る
        /// </summary>
        /// <returns></returns>
        private static string ReadEscapedCharString() {
            var c = File.ReadCh();
            switch ((char)c) {
                case '\'':
                case '"':
                case '?':
                case '\\': return $@"\{c}";
                case 'a': return @"\a";
                case 'b': return @"\b";
                case 'f': return @"\f";
                case 'n': return @"\n";
                case 'r': return @"\r";
                case 't': return @"\t";
                case 'v': return @"\v";
                case 'x': return ReadHexCharLiteralStr();
                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7': return ReadOctalCharLiteralStr(c);
            }
            CppContext.Warning(c.Position, $@"\{c} は未知のエスケープ文字です。");
            return $@"\{c}";
        }

        /// <summary>
        /// 文字定数の読み取り
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        static Token ReadChar(Position pos, Token.EncType encType) {
            StringBuilder b = new StringBuilder();
            for (;;) {
                var c = File.ReadCh();
                if (c.IsEof() || c == '\n') {
                    CppContext.Error(pos, "文字定数が ' で終端していません。");
                    File.UnreadCh(c);
                    break;
                } else if (c == '\'') {
                    break;
                } else if (c != '\\') {
                    b.Append(c);
                    continue;
                }
                var str = ReadEscapedCharString();
                b.Append(str);
            }
            return Token.make_char(pos, b.ToString(), encType);
        }

        /// <summary>
        /// 文字列定数の読み取り
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        static Token ReadString(Position pos, Token.EncType encType) {
            StringBuilder b = new StringBuilder();
            for (;;) {
                var c = File.ReadCh();
                if (c.IsEof() || c == '\n') {
                    CppContext.Error(pos, "文字列が \" で終端していません。");
                    File.UnreadCh(c);
                    break;
                }
                if (c == '"') { break; }
                if (c != '\\') {
                    b.Append(c);
                    continue;
                }
                var str = ReadEscapedCharString();
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
        static Token ReadIdent(Position pos, Utf32Char c) {
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

        /// <summary>
        /// ダイグラフ (digraph) を読み取る 
        /// </summary>
        /// <param name="pos">読み取り開始位置</param>
        /// <returns></returns>
        private static Token ReadDigraph(Position pos) {
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

        /// <summary>
        /// 次に読み取る文字が expect なら、その文字を読み取って t1を、
        /// そうでないならば els を返す
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="expect"></param>
        /// <param name="t1"></param>
        /// <param name="els"></param>
        /// <returns></returns>
        private static Token ReadRep1(Position pos, int expect, Token.Keyword t1, Token.Keyword els) {
            if (IsNextChar(expect)) {
                return Token.make_keyword(pos, t1);
            } else {
                return Token.make_keyword(pos, els);
            }
        }

        /// <summary>
        /// 次に読み取る文字が expect1 なら、その文字を読み取って t1を、
        /// 次に読み取る文字が expect2 なら、その文字を読み取って t2を、
        /// そうでないならば els を返す
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="expect1"></param>
        /// <param name="t1"></param>
        /// <param name="expect2"></param>
        /// <param name="t2"></param>
        /// <param name="els"></param>
        /// <returns></returns>
        private static Token ReadRep2(Position pos, int expect1, Token.Keyword t1, int expect2, Token.Keyword t2, Token.Keyword els) {
            if (IsNextChar(expect1)) {
                return Token.make_keyword(pos, t1);
            } else if (IsNextChar(expect2)) {
                return Token.make_keyword(pos, t2);
            } else {
                return Token.make_keyword(pos, els);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="disableComment">コメントを解析しない場合は真</param>
        /// <param name="handleEof">EoFを読み飛ばさない場合は真</param>
        /// <param name="limitSpace">空白と水平タブ以外を空白文字としない場合（6.10　前処理指令　前処理指令の中の前処理字句の間 に現れてよい空白類文字は，空白と水平タブだけ）は真</param>
        /// <returns></returns>
        private static Token DoReadToken(bool disableComment = false, bool handleEof = false, bool limitSpace = false)
        {
            //トークン前の空白を読み取る
            var sb = new SpaceInfo();
            if (SkipManySpaces(sb, disableComment: disableComment, handleEof: handleEof, limitSpace: limitSpace)) {
                var pos = sb.Chunks.First().Pos;
                return Token.make_space(pos, sb);
            }

            // 一文字読み取り、トークンの開始位置も取得
            var c = File.ReadCh(handleEof: handleEof);
            var tokenPosition = c.Position;
            if (c.IsEof()) {
                return Token.make_eof(tokenPosition);
            }

            switch ((char)c) {
                case '\n': return Token.make_newline(tokenPosition);
                case ':': return Token.make_keyword(tokenPosition, IsNextChar('>') ? (Token.Keyword)']' : (Token.Keyword)':');
                case '#': return Token.make_keyword(tokenPosition, IsNextChar('#') ? Token.Keyword.HashHash : (Token.Keyword)'#');
                case '+': return ReadRep2(tokenPosition, '+', Token.Keyword.Inc, '=', Token.Keyword.AssignAdd, (Token.Keyword)'+');
                case '*': return ReadRep1(tokenPosition, '=', Token.Keyword.AssignMul, (Token.Keyword)'*');
                case '=': return ReadRep1(tokenPosition, '=', Token.Keyword.Equal, (Token.Keyword)'=');
                case '!': return ReadRep1(tokenPosition, '=', Token.Keyword.NotEqual, (Token.Keyword)'!');
                case '&': return ReadRep2(tokenPosition, '&', Token.Keyword.LogicalAnd, '=', Token.Keyword.AssignAnd, (Token.Keyword)'&');
                case '|': return ReadRep2(tokenPosition, '|', Token.Keyword.LogincalOr, '=', Token.Keyword.AssignOr, (Token.Keyword)'|');
                case '^': return ReadRep1(tokenPosition, '=', Token.Keyword.AssignXor, (Token.Keyword)'^');
                case '"': return ReadString(tokenPosition, Token.EncType.None);
                case '\'': return ReadChar(tokenPosition, Token.EncType.None);
                case '/': return Token.make_keyword(tokenPosition, IsNextChar('=') ? Token.Keyword.AssignDiv : (Token.Keyword)'/');
                case '.':
                    if (PeekChar().IsDigit()) {
                        // ドットは前処理数の開始となる小数点である
                        return ReadNumberLiteral(tokenPosition, c);
                    } else if (IsNextChar('.')) {
                        if (IsNextChar('.'))
                        {
                            // ドットが３つ並んだ省略記号である
                            return Token.make_keyword(tokenPosition, Token.Keyword.Ellipsis);
                        } else {
                            // ドットが二つ並んだトークンとする（インクルードパス指定中で使われる表記用）
                            return Token.make_ident(tokenPosition, "..");
                        }
                    } else {
                        // 単なる記号
                        return Token.make_keyword(tokenPosition, (Token.Keyword)'.');
                    }
                case '(':
                case ')':
                case ',':
                case ';':
                case '[':
                case ']':
                case '{':
                case '}':
                case '?':
                case '~': {
                        // 単なる記号
                        return Token.make_keyword(tokenPosition, (Token.Keyword)(char)c);
                    }
                case '-':
                    {
                        if (IsNextChar('-')) { return Token.make_keyword(tokenPosition, Token.Keyword.Dec); }
                        if (IsNextChar('>')) { return Token.make_keyword(tokenPosition, Token.Keyword.Arrow); }
                        if (IsNextChar('=')) { return Token.make_keyword(tokenPosition, Token.Keyword.AssignSub); }
                        return Token.make_keyword(tokenPosition, (Token.Keyword)'-');
                    }
                case '<':
                    {
                        if (IsNextChar('<')) { return ReadRep1(tokenPosition, '=', Token.Keyword.AssignShiftArithLeft, Token.Keyword.ShiftArithLeft); }
                        if (IsNextChar('=')) { return Token.make_keyword(tokenPosition, Token.Keyword.LessEqual); }
                        if (IsNextChar(':')) { return Token.make_keyword(tokenPosition, (Token.Keyword)'['); }
                        if (IsNextChar('%')) { return Token.make_keyword(tokenPosition, (Token.Keyword)'{'); }
                        return Token.make_keyword(tokenPosition, (Token.Keyword)'<');
                    }
                case '>':
                    {
                        if (IsNextChar('=')) { return Token.make_keyword(tokenPosition, Token.Keyword.GreatEqual); }
                        if (IsNextChar('>')) { return ReadRep1(tokenPosition, '=', Token.Keyword.AssignShiftArithRight, Token.Keyword.ShiftArithRight); }
                        return Token.make_keyword(tokenPosition, (Token.Keyword)'>');
                    }
                case '%': {
                        Token tok = ReadDigraph(tokenPosition);
                        if (tok != null) { return tok; }
                        return ReadRep1(tokenPosition, '=', Token.Keyword.AssignMod, (Token.Keyword)'%');
                    }
                case 'L': {
                        if (IsNextChar('"')) {
                            CppContext.Warning(tokenPosition, "ワイド文字列リテラルが使われています。");
                            return ReadString(tokenPosition, Token.EncType.Wide);
                        } else if (IsNextChar('\'')) {
                            CppContext.Warning(tokenPosition, "ワイド文字定数が使われています。");
                            return ReadChar(tokenPosition, Token.EncType.Wide);
                        } else {
                            goto default;
                        }
                    }
                case 'u': {
                        if (IsNextStr("8\"")) {
                            CppContext.Warning(tokenPosition, "UTF8文字列リテラルが使われています。");
                            return ReadString(tokenPosition, Token.EncType.U8);
                        } else if (IsNextChar('"')) {
                            CppContext.Warning(tokenPosition, "UTF16文字列リテラルが使われています。");
                            return ReadString(tokenPosition, Token.EncType.U16);
                        } else if (IsNextChar('\'')) {
                            CppContext.Warning(tokenPosition, "UTF16文字定数が使われています。");
                            return ReadChar(tokenPosition, Token.EncType.U16);
                        } else {
                            goto default;
                        }
                    }
                case 'U': {
                        if (IsNextChar('"')) {
                            CppContext.Warning(tokenPosition, "UTF32文字列リテラルが使われています。");
                            return ReadString(tokenPosition, Token.EncType.U32);
                        } else if (IsNextChar('\'')) {
                            CppContext.Warning(tokenPosition, "UTF32文字定数が使われています。");
                            return ReadChar(tokenPosition, Token.EncType.U32);
                        } else {
                            goto default;
                        }
                    }
                default:
                    if (c.IsAlpha() || (c == '_')) {
                        return ReadIdent(tokenPosition, c);
                    }
                    if (c.IsDigit()) {
                        return ReadNumberLiteral(tokenPosition, c);
                    }

                    return Token.make_invalid(tokenPosition, $"{c}");
            }
        }

        /// <summary>
        /// #include に続く指定されているファイルパス部分を読み取る
        /// </summary>
        /// <param name="hash"></param>
        /// <param name="isGuillemet">山括弧で囲まれている場合は真</param>
        /// <returns></returns>
        public static string ReadHeaderFilePath(Token hash, out bool isGuillemet) {
            // 再挿入バッファが空でない場合は読み取らずに終わる
            if (!(Buffers.Count == 1 && Buffers.Peek().Count == 0)) {
                isGuillemet = false;
                return null;
            }

            //空白を読み飛ばす
            var space = new SpaceInfo();
            SkipManySpaces(space, limitSpace: true);

            // ファイルパス部の開始文字を読み取って、終了文字を決定する
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

            // ファイルパス部を読み取る
            var path = new StringBuilder();
            while (!IsNextChar(close)) {
                var c = File.ReadCh();
                if (c.IsEof() || c == '\n') {
                    CppContext.Error(hash, "ファイルパスが閉じられないまま行末に到達しました。");
                    File.UnreadCh(c);
                    break;
                }
                path.Append(c);
            }

            // チェック
            if (path.Length == 0) {
                CppContext.Error(hash, "includeで指定されたファイル名が空です。");
            }

            return path.ToString();
        }


        /// <summary>
        /// 入力トークン列を一時的に指定されたトークン列に切り替える。
        /// トークンが使い果たされた後は TokenBufferUnstash() が呼ばれるまで LexToken(）からは File.EOF が返される。
        /// (前処理の仕様にある「マクロ展開後のトークンを再度マクロ展開する」ために使う)
        /// </summary>
        /// <param name="buf"></param>
        public static void TokenBufferStash(List<Token> buf) {
            Buffers.Push(buf);
        }

        /// <summary>
        /// 切り替えた入力トークン列を復帰する
        /// </summary>
        public static void TokenBufferUnstash() {
            Buffers.Pop();
        }

        /// <summary>
        /// トークンを現在のトークン列に読み戻す
        /// </summary>
        /// <param name="tok"></param>
        public static void UngetToken(Token tok) {
            Buffers.Peek().Add(tok);
        }

        /// <summary>
        /// マクロ macro 中の トークン連結演算子の結果として得られた文字列 str を再度前処理する
        /// </summary>
        /// <param name="macro"></param>
        /// <param name="pos"></param>
        /// <param name="str"></param>
        /// <returns></returns>
        public static List<Token> LexPastedString(Macro macro, Position pos, string str) {
            // 文字列 s のみを内容として持つファイルを作ってファイルスタックに積む       
            File.StreamStash(new File(str, pos.FileName));

            // トークンの読み出しを行う。
            List<Token> ret = new List<Token>();
            Token tok = DoReadToken(disableComment: true);   /* 連結演算子の処理中はコメントを処理しない */
            ret.Add(tok);

            // 改行文字が付随している場合は読み飛ばす。
            IsNextChar('\n');

            // トークンを読み出し、EOFか判定
            // 連結結果が単一の前処理トークンに展開されている場合はEOFが得られる。
            tok = DoReadToken();
            if (tok.Kind != Token.TokenKind.EoF) {
                // EOFが得られなかった、つまり、連結結果は２つ以上の前処理トークンを含む
                CppContext.Error(pos, $"マクロ {macro.GetName()} 中のトークン連結演算子 ## の結果 {str} は不正なプリプロセッサトークンです。(マクロ {macro.GetName()} は {macro.GetFirstPosition().ToString()} で宣言されています。)");
                // 文字列 s から得られるトークンがなくなるまで読み続ける
                while (tok.Kind != Token.TokenKind.EoF) {
                    ret.Add(tok);
                    IsNextChar('\n');
                    tok = DoReadToken();
                }
            }

            // ファイルスタックを元に戻す
            File.StreamUnstash();

            // 展開結果のトークン列を返す
            return ret;
        }

        /// <summary>
        /// トークンを一つ読む
        /// </summary>
        /// <param name="handleEof">EoFを読み飛ばさない場合は真</param>
        /// <param name="limitSpace">空白と水平タブ以外を空白文字としない場合（6.10　前処理指令　前処理指令の中の前処理字句の間 に現れてよい空白類文字は，空白と水平タブだけ）は真</param>
        /// <returns></returns>
        public static Token LexToken(bool handleEof = false, bool limitSpace = false)
        {
            List<Token> buf = Buffers.Peek();
            if (buf.Count > 0) {
                // 挿入されたトークンバッファにトークンがあるのでそれを返す
                var p = buf.Pop();

                if (p.Kind == Token.TokenKind.MacroRangeFixup) {
                    // MacroRangeFixupトークンの場合はマクロ展開情報を記録し、トークン自体は読み捨てる
                    CppContext.ExpandLog.Add(
                        p.MacroRangeFixupTok,
                        p.MacroRangeFixupMacro,
                        p.MacroRangeFixupStartLine,
                        p.MacroRangeFixupStartColumn,
                        CppContext.TokenWriter.OutputLine,
                        CppContext.TokenWriter.OutputColumn
                    );
                    return LexToken(handleEof, limitSpace);
                } else {
                    return p;
                }
            }
            if (Buffers.Count > 1) {
                // 挿入されたトークンバッファにトークンが無いのでEOFを返す
                return Token.make_eof(new Position(File.CurrentFile.Name, File.CurrentFile.Line, File.CurrentFile.Column));
            }

            // 挿入されたトークンバッファもないので新しくファイルから読み取る
            var beginOfLine = (File.CurrentFile.Column == 1);
            var tok = DoReadToken(handleEof: handleEof, limitSpace: limitSpace);
            var space = tok.Space;
            
            // 読み取ったトークンが空白以外になるまで読み飛ばし続ける
            if (tok.Kind == Token.TokenKind.Space) {
                tok = DoReadToken();
                while (tok.Kind == Token.TokenKind.Space) {
                    space.Chunks.AddRange(tok.Space.Chunks);
                    tok = DoReadToken(handleEof: handleEof, limitSpace: limitSpace);
                }
            }
            tok.Space = space;
            tok.BeginOfLine = beginOfLine;

            return tok;
        }

        /// <summary>
        /// トークンが文字 ch ならば読む。違う場合はエラーを出力した上で不正トークンを返す。
        /// </summary>
        /// <param name="ch">文字</param>
        /// <returns>読み取り結果トークン</returns>
        public static Token ExceptKeyword(char ch) {
            return ExceptKeyword(ch, (tok) => {
                if (tok.Kind == Token.TokenKind.EoF) {
                    CppContext.Error(tok, $"{Token.KeywordToStr((Token.Keyword)ch)} がありません。");
                } else {
                    CppContext.Error(tok, $"{Token.KeywordToStr((Token.Keyword)ch)} があるべき場所に {Token.TokenToStr(tok)} がありました。");
                }
            });
        }

        /// <summary>
        /// トークンが文字 ch ならば読む。違う場合はエラーコールバック failHandler を実行した上で不正トークンを返す。
        /// </summary>
        /// <param name="ch">文字</param>
        /// <param name="failHandler">エラーコールバック</param>
        /// <returns>読み取り結果トークン</returns>
        public static Token ExceptKeyword(char ch, Action<Token> failHandler) {
            Token tok = LexToken();
            if (!tok.IsKeyword(ch)) {
                failHandler(tok);
                UngetToken(tok);
                return Token.make_invalid(tok.Position, "");
            }
            return tok;
        }

        /// <summary>
        /// トークン種別が id ならば読む。
        /// </summary>
        /// <param name="id"></param>
        /// <returns>読み取ったら true, 読み取らなかったら false</returns>
        public static bool NextKeyword(Token.Keyword id) {
            Token tok = LexToken();
            if (tok.IsKeyword(id)) {
                return true;
            }
            UngetToken(tok);
            return false;
        }

    }
}