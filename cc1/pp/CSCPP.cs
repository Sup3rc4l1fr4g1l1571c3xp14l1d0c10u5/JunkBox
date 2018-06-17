using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Numerics;

namespace CSCPP {
    public static class Cpp {
        /// <summary>
        /// マクロの定義状況を示す辞書
        /// </summary>
        public static Dictionary<string, Macro> Macros { get; } = new Dictionary<string, Macro>();

        /// <summary>
        /// 過去に defined や ifdef / ifndef 等で存在チェックされたマクロの集合
        /// （未定義マクロ使用の判定時に、未定義であることをチェックしたマクロについては警告を抑制する）
        /// </summary>
        public static HashSet<string> RefMacros { get; } = new HashSet<string>();

        /// <summary>
        /// 過去に一度でも定義されたことのあるマクロ
        /// </summary>
        public static List<Macro> DefinedMacros { get; } = new List<Macro>();

        /// <summary>
        /// #pragma once が指定されているファイルを記録
        /// </summary>
        static Dictionary<string, string> Once { get; } = new Dictionary<string, string>();
        static Dictionary<string, Token.Keyword> Keywords { get; } = new Dictionary<string, Token.Keyword>();
        static Dictionary<string, string> IncludeGuards { get; } = new Dictionary<string, string>();
        static List<Condition> ConditionStack { get; } = new List<Condition>();

        /// <summary>
        /// インクルードパス（絶対パスを格納）
        /// </summary>
        static List<string> UserIncludePath { get; } = new List<string>();

        /// <summary>
        /// システムインクルードディレクトリパス（絶対パスを格納）
        /// </summary>
        static List<string> StdIncludePath { get; } = new List<string>();

        /// <summary>
        /// (CertC PRE08-Cのために追加)
        /// 一度でもインクルードしたことがあるファイルとインクルード位置を示す表
        /// </summary>
        static Dictionary<string, List<Tuple<string, string, Position>>> PRE08IncludeFileTable = new Dictionary<string, List<Tuple<string, string, Position>>>();

        public static string DateString { get; }
        public static string TimeString { get; }

        static Cpp() {
            // 書式指定文字列での変換でもいいが、あえて全て組み立てている
            var now = DateTime.Now;
            var monthName = new[]
            {
                "", "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"
            };
            DateString = $"{monthName[now.Month]} {now.Day,2:D0} {now.Year,4:D4}";
            TimeString = $"{now.Hour,2:D2}:{now.Minute,2:D2}:{now.Second,2:D2}";
        }

        private static Token CppTokenZero(Token tok) {
            return new Token(tok, Token.TokenKind.Number) { StrVal = "0" };
        }

        private static Token CppTokenOne(Token tok) {
            return new Token(tok, Token.TokenKind.Number) { StrVal = "1" };
        }

        /// <summary>
        /// #if/#ifdef/#else/#elif等で使う条件分岐コンテキストの管理用
        /// </summary>
        public class Condition {
            public enum State { InThen, InElif, InElse }
            public State CurrentState { get; set; }
            public string IncludeGuard { get; set; }
            public File File { get; set; }
            public bool WasTrue { get; set; }    // else以外の節の条件がTrueとなったことがあるかどうかを示す

            public Condition(bool wasTrue) {
                CurrentState = State.InThen;
                WasTrue = wasTrue;
            }
        }

        /// <summary>
        /// トークン列の先頭要素の space を tmpl のものに設定する。ただし、tmplがマクロ引数の場合は空白を伝搬させない。
        /// </summary>
        /// <param name="tokens"></param>
        /// <param name="tmpl"></param>
        static void PropagateSpace(List<Token> tokens, Token tmpl) {
            if (tokens.Count != 0 && tmpl.Kind != Token.TokenKind.MacroParam) {
                tokens.First().Space = tmpl.Space;
            }
        }

        /*
         * Macro expander
         */

        static Token read_ident(bool limit_space = false) {
            Token tok = Lex.LexToken(limit_space: limit_space);
            if (tok.Kind != Token.TokenKind.Ident) {
                if (tok.Kind == Token.TokenKind.EoF || tok.Kind == Token.TokenKind.NewLine) {
                    CppContext.Error(tok, $"識別子があるべき場所に行末/ファイル末尾がありました。");
                } else {
                    CppContext.Error(tok, $"識別子があるべき場所に {Token.TokenToStr(tok)} がありました。");
                }
                // 読み戻して、不正トークンを読み取ったことにする
                Lex.unget_token(tok);
                return new Token(tok, Token.TokenKind.Invalid);
            }
            return tok;
        }

        public static void expect_newline(bool limit_space = false) {
            Token tok = Lex.LexToken(limit_space: limit_space);
            if (tok.Kind != Token.TokenKind.NewLine) {
                if (tok.Kind == Token.TokenKind.EoF) {
                    CppContext.Error(tok, $"改行があるべき場所にファイル終端がありました。");
                } else {
                    CppContext.Error(tok, $"改行があるべき場所に {Token.TokenToStr(tok)} がありました。");
                    while (tok.Kind != Token.TokenKind.NewLine && tok.Kind != Token.TokenKind.EoF) {
                        tok = Lex.LexToken(limit_space: limit_space);
                    }
                    if (tok.Kind == Token.TokenKind.EoF) {
                        Lex.unget_token(tok);
                    }
                }
            }
        }

        static List<Token> read_one_arg(Macro.FuncMacro macro, ref bool end, bool readall, bool limit_space = false)
        {
            List<Token> r = new List<Token>();
            int level = 0;
            for (;;) {
                Token tok = Lex.LexToken(handle_eof:true, limit_space: limit_space);
                if (tok.Kind == Token.TokenKind.EoF) {
                    CppContext.Error(tok, $"関数形式マクロ {macro.Name.StrVal} の呼び出しの引数リストが閉じる前にファイルが終わっています。");
                    end = true;
                    return null;
                }
                if (level == 0 && tok.IsKeyword(')')) {
                    Lex.unget_token(tok);
                    end = true;
                    return r;
                }
                if (tok.Kind == Token.TokenKind.NewLine) {
                    continue;
                }
                if (tok.BeginOfLine && tok.IsKeyword('#')) {
                    if (CppContext.Warnings.Contains(Warning.CertCCodingStandard)) {
                        // PRE32-C. 関数形式マクロの呼出しのなかで前処理指令を使用しない
                        CppContext.Warning(tok, $@"[PRE32-C] 関数形式マクロ {macro.Name.StrVal} の呼び出しのなかで前処理指令が使用されていますが、C90以降のC言語規格では、このようなコードは未定義の動作となることが示されています(参考文献:[ISO/IEC 9899:2011] 6.10.3 P11)。このルールについては、MISRA-C:1998 95、および、MISRA-C:2004 19.9 でも触れられています。");
                    }
                    ParseDirective(tok);
                    continue;
                }
                if (level == 0 && tok.IsKeyword(',') && !readall) {
                    return r;
                }
                if (tok.IsKeyword('(')) {
                    level++;
                } else if (tok.IsKeyword(')')) {
                    level--;
                }
                // C11 6.10.3p10: Within the macro argument list,
                // newline is considered a normal whitespace character.
                // I don't know why the standard specifies such a minor detail,
                // but the difference of newline and space is observable
                // if you stringize tokens using #.
                if (tok.BeginOfLine) {
                    tok = new Token(tok, tok.Kind) {
                        BeginOfLine = false,
                        HeadSpace = true,
                    };
                }
                r.Add(tok);
            }
        }

        static List<List<Token>> do_read_args(Macro.FuncMacro macro, bool limit_space = false)
        {
            List<List<Token>> r = new List<List<Token>>();
            bool end = false;
            while (!end) {
                bool inEllipsis = (macro.IsVarg && r.Count + 1 == macro.Args.Count);
                var ret = read_one_arg(macro, ref end, inEllipsis, limit_space: limit_space);
                if (ret == null)
                {
                    return null;
                }
                r.Add(ret);
            }
            if (macro.IsVarg && r.Count == macro.Args.Count - 1) {
                r.Add(new List<Token>());
            }
            return r;
        }

        static List<List<Token>> read_args(Token tok, Macro.FuncMacro macro, bool limit_space = false)
        {
            if (macro.Args.Count == 0 && PeekToken().IsKeyword(')')) {
                // If a macro M has no parameter, argument list of M()
                // is an empty list. If it has one parameter,
                // argument list of M() is a list containing an empty list.
                return new List<List<Token>>();
            }
            List<List<Token>> args = do_read_args(macro, limit_space: limit_space);
            if (args == null)
            {
                return null;
            }
            if (args.Count != macro.Args.Count) {
                CppContext.Error(tok, $"関数形式マクロ {macro.Name.StrVal} の実引数の数が一致しません。" +
                                      $"宣言時の仮引数は {macro.Args.Count} 個ですが、指定されている実引数は {args.Count} 個です。");
                // エラー回復は呼び出し元に任せる
                return null;
            }
            /* C99から利用可能となった空の実引数をチェック */
            if (!CppContext.Features.Contains(Feature.EmptyMacroArgument)) {
                for (var i = 0; i < args.Count; i++) {
                    if (!args[i].Any()) {
                        CppContext.Error(tok, $"関数形式マクロ {macro.Name.StrVal} の呼び出しの第 {i + 1} 実引数が空です。" +
                                               "空の実引数は ISO/IEC 9899-1999 以降で利用可能となった言語機能です。" +
                                               "空の実引数を有効にする場合は実行時引数に -FEmptyMacroArgument を設定してください。");
                    }
                }
                // プリプロセッサ内部では空の実引数に対応しているので、エラーは出力するがそのまま処理を進める。
            } else {
                if (CppContext.Warnings.Contains(Warning.EmptyMacroArgument)) {
                    for (var i = 0; i < args.Count; i++) {
                        if (!args[i].Any()) {
                            CppContext.Warning(tok, $"関数形式マクロ {macro.Name.StrVal} の呼び出しの第 {i + 1} 実引数が空です。");
                        }
                    }
                }
            }
            return args;
        }

        /// <summary>
        /// トークンのhidesetに新しいHidesetをマージする
        /// </summary>
        /// <param name="tokens"></param>
        /// <param name="hideset"></param>
        /// <returns></returns>
        static List<Token> AddHideSetToTokens(List<Token> tokens, Set hideset) {
            return tokens.Select(x => new Token(x, x.Kind) { Hideset = x.Hideset.Union(hideset) }).ToList();
        }

        /// <summary>
        /// ## 演算子の動作
        /// 現在のトークン列の末尾要素にトークンを結合する
        /// </summary>
        /// <param name="m"></param>
        /// <param name="tokens"></param>
        /// <param name="tok"></param>
        static void glue_push(Macro m, List<Token> tokens, Token tok) {
            var lasttoken = tokens.Pop();

            // トークン列の末尾要素とトークンtokを文字列として結合してからトークンの読み出しを行う
            var str = Token.TokenToStr(lasttoken) + Token.TokenToStr(tok);
            var newtoken = Lex.lex_string(m, lasttoken.Pos, str);
            tokens.AddRange(newtoken);
        }

        /// <summary>
        /// # 演算子の動作
        /// トークン列 args を文字列化する
        /// x=foo, y=bar のとき
        ///   x#y   -> 空白を挟まずに結合 -> "foobar" となる
        ///   x# y  -> 空白を挟まずに結合 -> "foobar" となる
        ///   x #y  -> 空白を挟んでに結合 -> "foo bar" となる
        ///   x # y -> 空白を挟んでに結合 -> "foo bar" となる
        /// </summary>
        /// <param name="tmpl">文字列化したトークンの位置情報などのコピー元にするトークン</param>
        /// <param name="args">結合するトークン列</param>
        /// <returns></returns>
        static Token Stringize(Token tmpl, List<Token> args) {
            StringBuilder b = new StringBuilder();
            foreach (Token tok in args) {
                if (b.Length != 0 && tok.Space.Length > 0) {
                    b.Append(" ");
                }
                b.Append(Token.TokenToStr(tok));
            }
            var ret = new Token(tmpl, Token.TokenKind.String) {
                StrVal = b.ToString().Replace(@"\", @"\\").Replace(@"""", @"\"""),// エスケープが必要な文字はエスケープする
            };
            return ret;
        }

        /// <summary>
        /// トークン列 inputTokens をすべてマクロ展開する
        /// 字句解析器にトークン列を積み上げ read_expand()で読み取ると展開になる
        /// </summary>
        /// <param name="inputTokens"></param>
        /// <param name="tmpl"></param>
        /// <returns></returns>
        static List<Token> expand_all(List<Token> inputTokens, Token tmpl) {
            Lex.token_buffer_stash(inputTokens.Reverse<Token>().ToList());
            List<Token> r = new List<Token>();
            for (Token tok = read_expand(); tok.Kind != Token.TokenKind.EoF; tok = read_expand()) {
                r.Add(tok);
            }

            PropagateSpace(r, tmpl);

            Lex.token_buffer_unstash();
            return r;
        }

        /// <summary>
        /// マクロ展開を行う
        /// </summary>
        /// <param name="m"></param>
        /// <param name="body">マクロの本文</param>
        /// <param name="args">関数形式マクロの場合、与えられた引き数列</param>
        /// <param name="hideset">既に展開したため、再帰展開を抑制するマクロ名集合</param>
        /// <returns></returns>
        static List<Token> Subst(Macro m, List<Token> body, List<List<Token>> args, Set hideset) {
            List<Token> r = new List<Token>();
            int len = body.Count;
            for (int i = 0; i < len; i++) {
                // 今のトークンと次のトークンの二つを読み出しておく
                Token t0Value = body.ElementAtOrDefault(i + 0);
                Token t1Value = body.ElementAtOrDefault(i + 1);
                Token t0 = (t0Value != null && t0Value.Kind == Token.TokenKind.MacroParamRef) ? t0Value.MacroParamRef : t0Value;
                Token t1 = (t1Value != null && t1Value.Kind == Token.TokenKind.MacroParamRef) ? t1Value.MacroParamRef : t1Value;
                bool t0Param = (t0 != null && t0.Kind == Token.TokenKind.MacroParam);
                bool t1Param = (t1 != null && t1.Kind == Token.TokenKind.MacroParam);

                if (args != null && t0.IsKeyword('#')) {

                    // 6.10.3.2 #演算子 に
                    // 制約  関数形式マクロの置換要素並びの中にある各#前処理字句の次の前処理字句は，仮引数でなければならない
                    // とあるので、関数形式マクロの時だけ '#' を特別視する
                    if (t1Param) {
                        // (t0,t1) = ('#', <マクロ引数1>) の場合
                        // マクロ引数1を構成するトークン列を文字列としたものを結果に挿入
                        var newTok = Stringize(t0, args[t1.Position]);
                        if (r.Any()) {
                            r.Last().TailSpace = false;
                        }
                        r.Add(newTok);
                        i++;
                    } else {
                        CppContext.Error(t0, $"`#` 演算子の次はマクロ仮引数名でなければなりません。");
                        r.Add(t0);
                    }
                } else if (t0.IsKeyword(Token.Keyword.HashHash) && t1Param) {
                    // (t0,t1) = ('##', <マクロ引数1>) の場合

                    List<Token> arg = args[t1.Position];
                    if (CppContext.Features.Contains(Feature.ExtensionForVariadicMacro) && t1.IsVarArg && r.Count > 0 && r.Last().IsKeyword(',')) {
                        /* GCC系は ,##__VA_ARGS__ と書かれた関数形式マクロの本体を展開する際に、 __VA_ARGS__ 部分のトークン列が空の場合はコンマを出力しない動作を行う
                         * #define func(x, ...) func2(x,##__VA_ARGS__) の場合
                         *   func(A, 1) は func2(A, 1)になるが
                         *   func(A   ) は func2(A   )にならないといけない
                         */
                        if (CppContext.Warnings.Contains(Warning.ExtensionForVariadicMacro)) {
                            CppContext.Warning(t0, $"関数形式マクロ {m.GetName()} の可変長マクロ引数の展開でISO/IEC 9899-1999 では定義されていない非標準の拡張 `,##__VA_ARGS__` が使われています。");
                        }
                        if (arg.Any()) {
                            //可変長マクロ引数があるのでそのまま出力
                            r.AddRange(arg);
                        } else {
                            //可変長マクロ引数が空なの逆に末尾のコンマを取り除く
                            r.Pop();
                        }
                    } else if (arg.Any()) {
                        // 引数の最初のトークンを出力末尾のトークンと連結し、
                        // 引数の残りのトークンは出力の末尾に追加する
                        glue_push(m, r, arg.First());
                        for (int j = 1; j < arg.Count; j++) {
                            r.Add(arg[j]);
                        }
                    }
                    i++;
                } else if (t0.IsKeyword(Token.Keyword.HashHash) && t1 != null) {
                    // 最初のトークンが## でその後ろに別のトークンがある（つまり連結演算子式の先頭以外の)場合 
                    hideset = t1.Hideset;
                    // トークンの連結処理を行う
                    glue_push(m, r, t1);

                    i++;
                } else if (t0Param && t1 != null && t1.IsKeyword(Token.Keyword.HashHash)) {
                    // 最初のトークンがマクロ引数で その後ろに##がある（つまり連結演算子式の先頭の)場合 
                    hideset = t1.Hideset;
                    //マクロ引数列を展開せずにトークン列に追加する
                    List<Token> arg = args[t0.Position];
                    if (arg.Count == 0) {
                        i++;
                    } else {
                        r.AddRange(arg);
                    }
                } else if (t0Param) {
                    // 最初のトークン t0 がマクロ仮引数名の場合、関数型マクロの実引数 args からt0に対応する引数を取得
                    List<Token> arg = args[t0.Position];

                    // 引き数列を展開してトークン列に追加
                    var newtokens = expand_all(arg, t0);
                    if (newtokens.Any()) {
                        newtokens.First().HeadSpace = true;
                        newtokens.Last().TailSpace = true;
                    }
                    r.AddRange(newtokens);
                } else {
                    // それ以外の場合は何もせずにトークン列に追加
                    r.Add(t0);
                }
            }
            return AddHideSetToTokens(r, hideset);
        }

        /// <summary>
        /// トークン列を字句解析器に押し戻す。
        /// </summary>
        /// <param name="tokens"></param>
        static void unget_all(List<Token> tokens) {
            for (int i = tokens.Count - 1; i >= 0; i--) {
                Lex.unget_token(tokens[i]);
            }
        }



        /// <summary>
        /// Dave Prosser's C Preprocessing Algorithm (http://www.spinellis.gr/blog/20060626/)
        /// におけるexpandに対応するトークン読み取り処理
        /// </summary>
        /// <returns></returns>
        static Token read_expand(bool limit_space = false)
        {
            // トークンを一つ読み取る
            Token tok = Lex.LexToken(limit_space: limit_space);

            // 識別子以外もしくは、verbatimなら終わり
            if (tok.Kind != Token.TokenKind.Ident || tok.Verbatim) {
                return tok;
            }

            // 識別子がマクロとして登録されているか調べる
            string name = tok.StrVal;
            Macro macro = Macros.ContainsKey(name) ? Macros[name] : null;

            // マクロとして登録されていない場合や、既に一度マクロ展開をしている場合は展開不要
            if (macro == null || (tok.Hideset.Contains(name))) {
                return tok;
            }

            // マクロの使用フラグを立てる
            macro.Used = true;

            // 展開後のトークン列
            List<Token> expandedTokens;

            // マクロの種別に応じて処理を行う
            if (macro is Macro.ObjectMacro) {
                // 定数マクロの場合
                Macro.ObjectMacro m = macro as Macro.ObjectMacro;

                // マクロ再展開禁止用のhidesetに追加
                Set hideset = tok.Hideset.Add(name);

                // 実際のマクロ展開を行い、展開後のトークン列を取得する。
                expandedTokens = Subst(m, m.Body, null, hideset);

                if (CppContext.Verboses.Contains(Verbose.TraceMacroExpand)) {
                    CppContext.CppWriter.EnterDummy();
                    var rawPos = CppContext.CppWriter.Write(tok);
                    CppContext.CppWriter.LeaveDummy();
                    expandedTokens.Add(Token.make_MacroRangeFixup(new Position("dummy", -1, -1), tok, macro, rawPos.Item1, rawPos.Item2));
                }

            } else if (macro is Macro.FuncMacro) {
                // 関数形式マクロの場合

                Macro.FuncMacro m = macro as Macro.FuncMacro;

                /* 6.10.3: 関数形式マクロの呼出しを構成する前処理字句列の中では，改行は普通の空白類文字とみなす。を考慮してマクロ名と括弧の間に改行がある場合は先読みを行う。
                 * そうしないと6.10.3.5の例3で異なった結果が得られる 
                 */
                List<Token> lookAHeads = new List<Token>();
                for (;;) {
                    var tok2 = Lex.LexToken(limit_space: limit_space);
                    if (tok2.IsKeyword('(')) {
                        // マクロ関数呼び出しである
                        break;
                    } else if (tok2.Kind == Token.TokenKind.NewLine) {
                        // 改行は読み飛ばす
                        lookAHeads.Add(tok2);
                        continue;
                    } else {
                        // マクロ関数呼び出しではない
                        Lex.unget_token(tok2);
                        unget_all(lookAHeads);
                        return tok;
                    }
                }

                // マクロ関数呼び出しのの実引数を読み取る
                var args = read_args(tok, m, limit_space: limit_space);
                if (args == null) {
                    // マクロ引数読み取りにエラーがあった場合は')'まで読み飛ばし
                    for (;;) {
                        var tok2 = Lex.LexToken(limit_space: limit_space);
                        if (tok2.IsKeyword(')')) {
                            break;
                        }
                        if (tok2.Kind == Token.TokenKind.NewLine || tok2.Kind == Token.TokenKind.EoF) {
                            Lex.unget_token(tok2);
                            break;
                        }
                    }
                    return tok;
                }

                if (args.Count > 127) {
                    CppContext.Warning($"関数形式マクロ `{name}` のマクロ呼出しにおける実引数の個数が127個を超えており、ISO/IEC 9899 5.2.4.1 翻訳限界 の制約に抵触しています。");
                }

                var rparen = Lex.ExceptKeyword(')');

                // マクロ再展開禁止用のhidesetをマクロ関数名と引数の展開結果の両方に出現するものから作って追加
                Set hideset = tok.Hideset.Intersect(rparen.Hideset).Add(name);

                // 実際のマクロ展開を行う
                expandedTokens = Subst(m, m.Body, args, hideset);

            } else if (macro is Macro.BuildinMacro) {
                // 組み込みマクロの場合、マクロオブジェクトの処理関数に投げる
                Macro.BuildinMacro m = macro as Macro.BuildinMacro;
                expandedTokens = m.Hander(m, tok);
            } else {
                CppContext.InternalError(tok, $"マクロの型が不正です。(macro.Type={macro.GetType().FullName})");
                expandedTokens = null;
            }

            // マクロオブジェクトを処置できなかったのでマクロオブジェクトをそのまま返す。
            if (expandedTokens == null) {
                return tok;
            }

            // tokの空白情報を展開後のトークン列 tokensの先頭要素に伝搬させる
            PropagateSpace(expandedTokens, tok);

            // 展開して得られたトークン列の位置を調整
            expandedTokens.ForEach(x => {
                x.File = tok.File;
                x.Pos = tok.Pos;
            });

            // 最初のトークン以外の空白位置も補正
            expandedTokens.Skip(1).ToList().ForEach(x =>
            {
                var prevSpace = x.Space;
                x.Space = new SpaceInfo();
                x.Space.chunks.AddRange(prevSpace.chunks.Select(y => new SpaceInfo.Chunk() {Pos = tok.Pos, Space = y.Space} ));
            });

            if (expandedTokens.Any()) {
                // マクロ列の前後に行頭情報や仮想的な空白を挿入
                expandedTokens.First().BeginOfLine = tok.BeginOfLine;
                expandedTokens.First().HeadSpace = true;
                expandedTokens.Last().TailSpace = true;
            } else {
                // マクロ列がない場合は次のトークンの頭に仮想的な空白を挿入
                PeekToken().HeadSpace = true;
            }

            // 構築した展開後のトークンをバッファに戻す。
            unget_all(expandedTokens);

            // 再帰的に処理する
            return read_expand();

        }

        static bool read_funclike_macro_params(Macro m, List<Tuple<string, Token>> param) {
            int pos = 0;
            for (;;) {
                Token tok = Lex.LexToken();
                if (tok.IsKeyword(')')) {
                    return false;
                }

                if (pos != 0) {
                    if (!tok.IsKeyword(',')) {
                        CppContext.Error(tok, $"関数形式マクロ {m.GetName()} の宣言で仮引数を区切るコンマがあるべき場所に {Token.TokenToStr(tok)} がありました。");
                        if (tok.Kind != Token.TokenKind.Ident) {
                            Lex.unget_token(tok);
                        }
                    } else {
                        tok = Lex.LexToken();
                    }
                }
                if (tok.Kind == Token.TokenKind.NewLine) {
                    CppContext.Error(m.GetPosition(), $"関数形式マクロ {m.GetName()} の宣言で仮引数宣言の括弧が閉じていません。");
                    Lex.unget_token(tok);
                    return false;
                }
                if (tok.IsKeyword(Token.Keyword.Ellipsis)) {
                    if (CppContext.Features.Contains(Feature.VariadicMacro)) {
                        if (CppContext.Warnings.Contains(Warning.VariadicMacro)) {
                            CppContext.Warning(tok, $"関数形式マクロ {m.GetName()} は可変個引数マクロとして宣言されています。");
                        }
                        var tokVaArgs = Token.make_macro_token(pos, true, "__VA_ARGS__", tok.File);
                        param.Add(Tuple.Create("__VA_ARGS__", tokVaArgs));
                        Lex.ExceptKeyword(')');
                        return true;
                    } else {
                        CppContext.Error(tok, $"関数形式マクロ {m.GetName()} は可変個引数マクロとして宣言されていますが、可変個引数マクロは ISO/IEC 9899-1999 以降で利用可能となった言語機能です。可変個引数マクロを有効にする場合は実行時引数に -FVariadicMacro を設定してください。");
                    }
                }

                if (tok.Kind != Token.TokenKind.Ident) {
                    CppContext.Error(tok, $"関数形式マクロ {m.GetName()} の宣言で仮引数(識別子)があるべき場所に {Token.TokenToStr(tok)} がありました。");
                    Lex.unget_token(tok);
                    return false;
                }

                string arg = tok.StrVal;
                if (Lex.NextKeyword(Token.Keyword.Ellipsis)) {
                    Lex.ExceptKeyword(')');
                    var tokArg = Token.make_macro_token(pos, true, arg, tok.File);
                    param.Add(Tuple.Create(arg, tokArg));
                    return true;
                } else {
                    if (param.Any(x => x.Item1 == arg)) {
                        CppContext.Error(tok, $"関数形式マクロ {m.GetName()} の宣言で仮引数 {arg} が複数回定義されています。");
                    }
                    var tokArg = Token.make_macro_token(pos++, false, arg, tok.File);
                    param.Add(Tuple.Create(arg, tokArg));
                }
            }
        }

        static void hashhash_check(List<Token> v) {
            if (v.Count == 0)
                return;
            if (v.First().IsKeyword(Token.Keyword.HashHash)) {
                var tok = v.First();
                CppContext.Error(tok, "'##' はマクロ展開部の先頭では使えません。");
                v.Insert(0, new Token(tok, Token.TokenKind.Ident) { KeywordVal = (Token.Keyword)' ' });
            }
            if (v.Last().IsKeyword(Token.Keyword.HashHash)) {
                var tok = v.Last();
                CppContext.Error(tok, "'##' はマクロ展開部の末尾では使えません。");
                v.Add(new Token(tok, Token.TokenKind.Ident) { KeywordVal = (Token.Keyword)' ' });
            }
        }

        static List<Token> read_funclike_macro_body(Macro m, List<Tuple<string, Token>> param) {
            List<Token> r = new List<Token>();
            for (;;) {
                Token tok = Lex.LexToken();
                if (tok.Kind == Token.TokenKind.NewLine) {
                    return r;
                }
                tok.Space.chunks.ForEach(x => x.Space = System.Text.RegularExpressions.Regex.Replace(x.Space, "\r?\n", ""));
                if (tok.Kind == Token.TokenKind.Ident) {
                    var t = param.Find(x => x.Item1 == tok.StrVal);
                    if (t != null) {
                        var t2 = new Token(t.Item2, Token.TokenKind.MacroParamRef) {Space = tok.Space, MacroParamRef = t.Item2 };
                        r.Add(t2);
                        continue;
                    }
                }
                r.Add(tok);
            }
        }

        /// <summary>
        /// トークン列が (...) の形式の繰り返しとして妥当か調べて繰り返し回数を返す
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        static int QuoteParentheses(IEnumerable<Token> tokens) {

            if (tokens.First().IsKeyword('(') == false || tokens.Last().IsKeyword(')') == false) {
                return -1;
            }

            int nest = 0;
            int quotes = 0;
            foreach (var token in tokens) {
                if (token.IsKeyword('(')) {
                    nest += 1;
                } else if (token.IsKeyword(')')) {
                    if (nest == 0) {
                        return -1;
                    }
                    nest -= 1;
                    if (nest == 0) {
                        quotes++;
                    }
                } else if (nest == 0) {
                    return -1;
                }
            }
            return (nest == 0) ? quotes : -1;
        }

        static Token read_funclike_macro(Token name) {
            List<Tuple<string, Token>> param = new List<Tuple<string, Token>>();
            Macro.FuncMacro macro = new Macro.FuncMacro(name, null, null, false);
            bool isVarg = read_funclike_macro_params(macro, param);

            if (param.Count > 127) {
                CppContext.Warning($"関数形式マクロ `{name}` の定義において仮引数が {param.Count} 個定義されています。ISO/IEC 9899 5.2.4.1 翻訳限界 で規定されている関数形式マクロの仮引数は 127 個ですので処理系依存の動作となります。");
            }

            List<Token> body = read_funclike_macro_body(macro, param);

            if (CppContext.Warnings.Contains(Warning.CertCCodingStandard)) {

                // PRE01-C: マクロ内の引数名は括弧で囲む
                foreach (var b in body.Select((x, i) => Tuple.Create(x, i)).Where(x => x.Item1.Kind == Token.TokenKind.MacroParamRef)) {
                    var index = b.Item2;
                    if ((index > 0) && (index + 1 < body.Count)) {
                        if (body[index - 1].IsKeyword('(') && body[index + 1].IsKeyword(')')) {
                            // ok
                            continue;
                        }
                        CppContext.Warning(b.Item1, $"[PRE01-C] 関数形式マクロ {macro.GetName()} 内の引数 {b.Item1.ArgName} が括弧で囲まれていません。");
                    }
                }

                // PRE02-C: マクロ置換リストは括弧で囲む
                do {
                    if (body.Any()) {
                        // 例外: PRE02-C-EX1
                        if (body.Count == 1) {
                            // 「単一の識別子に展開されるマクロ」に対する例外処理。
                            if (body.First().Kind == Token.TokenKind.Ident || body.First().Kind == Token.TokenKind.Number || body.First().Kind == Token.TokenKind.MacroParamRef) {
                                break;
                            }
                        } else {
                            // 「関数呼び出しに展開されるマクロ」に対する例外処理。
                            if (body.First().Kind == Token.TokenKind.Ident && QuoteParentheses(body.Skip(1)) == 1) {
                                // foo(...)のような形式
                                break;
                            }
                            // (*foo)(...)のような形式はどうしますかね…
                        }

                        // PRE02-C-EX2: 配列の添字演算子 [] を使った配列参照に展開されるマクロや、. 演算子や -> 演算子を使って構造体のメンバや共用体オブジェクトを意味する式に展開されるマクロの場合は、演算子が前についても評価順序に影響はないので、括弧を付けなくてもよい。
                        // には未対応

                        if (QuoteParentheses(body) == 1) {
                            // ok
                            break;
                        }
                        var locate = macro.GetPosition();
                        CppContext.Warning(locate, $"[PRE02-C] 関数形式マクロ {macro.GetName()} の置換リストが括弧で囲まれていません。");
                    }
                } while (false);

                // PRE11-C: マクロ定義をセミコロンで終端しない
                do {
                    if (body.Any()) {
                        var last = body.Last();
                        if (last.IsKeyword(';')) {
                            var locate = macro.GetPosition();
                            CppContext.Warning(locate, $"[PRE11-C] 関数形式マクロ {macro.GetName()} の置換リストがセミコロンで終端されています。");
                        }
                    }
                } while (false);
            }

            hashhash_check(body);
            macro.Args = param.Select(x => x.Item2).ToList();
            macro.Body = body;
            macro.IsVarg = isVarg;

            Macro pre = null;
            Macros.TryGetValue(name.StrVal, out pre);
            if (pre is Macro.BuildinMacro) {
                CppContext.Warning(name, $"マクロ {name.StrVal} は再定義できません。");
            } else {
                if (CppContext.Warnings.Contains(Warning.RedefineMacro)) {
                    // 再定義の判定が必要
                    if (Macros.ContainsKey(name.StrVal) == true) {
                        // 同一定義の場合は再定義と見なさない
                        if (Macro.EqualDefine(pre, macro) == false) {
                            var location = pre.GetPosition();
                            CppContext.Warning(name, $"マクロ {name.StrVal} が再定義されました。以前のマクロ {name.StrVal} は {location} で定義されました。");
                        }
                    }
                }

                // 5.2.4.1 翻訳限界
                // 内部識別子又はマクロ名において意味がある先頭の文字数［各国際文字名又は各ソース拡張文字は，1個の文字とみなす。］(63)
                if (name.StrVal.Length > 63) {
                    CppContext.Warning(name, $"マクロ名 `{name.StrVal}` は63文字を超えており、ISO/IEC 9899 5.2.4.1 翻訳限界 の制約に抵触しています。");
                }

                Macros[name.StrVal] = macro;
                DefinedMacros.Add(macro);
            }
            return new Token(name, Token.TokenKind.NewLine);
        }

        static void read_obj_macro(Token token) {
            var name = token.StrVal;
            List<Token> body = new List<Token>();
            for (;;) {
                Token tok = Lex.LexToken();
                if (tok.Kind == Token.TokenKind.NewLine || tok.Kind == Token.TokenKind.EoF) {
                    break;
                }
                tok.Space.chunks.ForEach(x => x.Space = System.Text.RegularExpressions.Regex.Replace(x.Space, "\r?\n", ""));
                body.Add(tok);
            }
            hashhash_check(body);
            var macro = new Macro.ObjectMacro(token, body);
            var locate = macro.GetPosition();

            if (CppContext.Warnings.Contains(Warning.CertCCodingStandard)) {
                // PRE11-C: マクロ定義をセミコロンで終端しない
                do {
                    if (body.Any()) {
                        var last = body.Last();
                        if (last.IsKeyword(';')) {
                            CppContext.Warning(locate, $"[PRE11-C] オブジェクト形式マクロ {macro.GetName()} の置換リストがセミコロンで終端されています。");
                        }
                    }
                } while (false);
            }

            if (name.Length > 63) {
                CppContext.Warning(locate, $"マクロ名 `{name}` は63文字を超えており、ISO/IEC 9899 5.2.4.1 翻訳限界 の制約に抵触しています。");
            }
            Macros[name] = macro;
            DefinedMacros.Add(macro);
        }

        /*
         * #define
         */

        static Token read_define(Token hash, Token tdefine) {
            var nl = new Token(hash, Token.TokenKind.NewLine);
            Token name = read_ident(limit_space : true);
            if (name.Kind == Token.TokenKind.Invalid) {
                //CppContext.Error(name, $"オブジェクト形式マクロのマクロ名があるべき場所に {Token.TokenToStr(name)} がありました。");
                for (;;) {
                    var tok2 = Lex.LexToken();
                    if (tok2.Kind == Token.TokenKind.NewLine || tok2.Kind == Token.TokenKind.EoF) {
                        return nl;
                    }
                }
            }
            Token tok = Lex.LexToken();
            if (tok.IsKeyword('(') && tok.Space.Length == 0) {
                read_funclike_macro(name);
            } else {
                Lex.unget_token(tok);
                read_obj_macro(name);
            }
            return nl;
        }

        /*
         * #undef
         */

        static Token read_undef(Token hash, Token tundef) {
            var nl = new Token(hash, Token.TokenKind.NewLine);
            Token name = read_ident(limit_space: true);
            expect_newline();
            if (name.Kind == Token.TokenKind.Ident) {
                Macros.Remove(name.StrVal);
            }
            return nl;
        }

        /*
         * #if and the like
         */

        static Token read_defined_op() {
            Token tok = Lex.LexToken(limit_space: true);
            if (tok.IsKeyword('(')) {
                tok = Lex.LexToken(limit_space: true);
                Lex.ExceptKeyword(')');
            }
            if (tok.Kind != Token.TokenKind.Ident) {
                CppContext.Error(tok, $"識別子があるべき場所に {Token.TokenToStr(tok)} がありました。");
            }

            // 参照されたことのあるマクロとして記録する
            RefMacros.Add(tok.StrVal);

            if (Macros.ContainsKey(tok.StrVal) && Macros[tok.StrVal] != null) {
                Macros[tok.StrVal].Used = true;
                return CppTokenOne(tok);
            } else {
                return CppTokenZero(tok);
            }
        }

        static List<Token> read_intexpr_line() {
            List<Token> r = new List<Token>();
            for (;;) {
                Token tok = read_expand(limit_space: true);
                if (tok.Kind == Token.TokenKind.NewLine) {
                    return r;
                }

#warning "既知の問題：#if defined(C) && (C == 1 || D == 2) のような前処理式に対して未定義参照チェックができないので、C、Dが未定義というエラーになる"
#warning "既知の問題：ここで式木を作るようにしないと、Dの未定義参照チェックができない"

                if (tok.IsIdent("defined")) {
                    r.Add(read_defined_op());
                } else if (tok.Kind == Token.TokenKind.Ident) {
                    // 6.10.1 条件付き取込み
                    //   マクロ展開及び defined 単項演算子によるすべてのマクロ置き換えの実行後，
                    //   残っているすべての識別子を前処理数 0 で置き換えてから，各前処理字句を字句に変換する。
                    // つまり、前処理の条件式評価中に登場した未定義の識別子は数字 0 に置き換えられる
                    if (!Cpp.RefMacros.Contains(tok.StrVal)) {
                        if (CppContext.Warnings.Contains(Warning.UndefinedToken)) {
                            CppContext.Warning(tok, $"未定義の識別子 {Token.TokenToStr(tok)} が使用されています。");
                        }
                        Cpp.RefMacros.Add(tok.StrVal);
                    }
                    r.Add(CppTokenZero(tok));
                } else {
                    r.Add(tok);
                }
            }
        }

        // 簡易評価
        private static int expr_priority(Token op) {
            if (op.Kind != Token.TokenKind.Keyword) {
                return 0;
            }
            switch (op.KeywordVal) {
                case (Token.Keyword)'/':
                    return 11;
                case (Token.Keyword)'%':
                    return 11;
                case (Token.Keyword)'*':
                    return 11;
                case (Token.Keyword)'+':
                    return 10;
                case (Token.Keyword)'-':
                    return 10;
                case Token.Keyword.ShiftArithLeft:
                    return 9;
                case Token.Keyword.ShiftArithRight:
                    return 9;
                case (Token.Keyword)'<':
                    return 8;
                case (Token.Keyword)'>':
                    return 8;
                case Token.Keyword.LessEqual:
                    return 8;
                case Token.Keyword.GreatEqual:
                    return 8;
                case Token.Keyword.Equal:
                    return 7;
                case Token.Keyword.NotEqual:
                    return 7;
                case (Token.Keyword)'&':
                    return 6;
                case (Token.Keyword)'^':
                    return 5;
                case (Token.Keyword)'|':
                    return 4;
                case Token.Keyword.LogicalAnd:
                    return 3;
                case Token.Keyword.LogincalOr:
                    return 2;
                case (Token.Keyword)'?':
                    return 1;
                default:
                    // System.out.WriteLine("Unrecognised operator " + op);
                    return 0;
            }
        }

        private static int parse_char(Token tok) {
            System.Diagnostics.Debug.Assert(tok.Kind == Token.TokenKind.Char);
            if (tok.StrVal.Length == 0) {
                CppContext.Error(tok, $"空の文字定数が使われています。");
                return 0;
            }
            var str = tok.StrVal;
            List<byte> r = new List<byte>();
            for (var i = 0; i < str.Length; i++) {
                var ch = str.ElementAtOrDefault(i);
                if (ch == '\\') {
                    i++;
                    r.AddRange(read_escaped_char(tok, str, ref i));
                } else {
                    r.AddRange(System.Text.Encoding.UTF8.GetBytes(new[] { (char)ch }));
                }
            }
            if (r.Count > 1) {
                CppContext.Error(tok, $"2文字以上を含む文字定数 '{str}' が使われていますが、その値は処理系定義の結果となります。本処理系では { (int)r.Last() } として扱います。");
            }
            return r.Last();
        }

        private static byte[] read_escaped_char(Token tok, string str, ref int i) {
            System.Diagnostics.Debug.Assert(tok.Kind == Token.TokenKind.Char);
            int c = str.ElementAtOrDefault(i+0);
            switch (c) {
                case '\'':
                case '"':
                case '?':
                case '\\': {
                        i += 1;
                        return new byte[] { (byte)c };
                    }
                case 'a': return new byte[] { (byte)'\a' };
                case 'b': return new byte[] { (byte)'\b' };
                case 'f': return new byte[] { (byte)'\f' };
                case 'n': return new byte[] { (byte)'\n' };
                case 'r': return new byte[] { (byte)'\r' };
                case 't': return new byte[] { (byte)'\t' };
                case 'v': return new byte[] { (byte)'\v' };
                case 'x': {
                        int c2 = str.ElementAtOrDefault(i+1);
                        if (!CType.IsXdigit(c2)) {
                            CppContext.Error(tok, $"\\x に続く文字 {(char)c2} は16進数表記で使える文字ではありません。");
                            i += 1;
                            return new byte[] { (byte)0 };
                        } else {
                            UInt32 r = 0;
                            bool over = false;
                            int j;
                            for (j=0; j<2 &&  i+j < str.Length; j++) {
                                if (over == false && r > Byte.MaxValue) {
                                    over = true;
                                    CppContext.Error(tok, $"16進数文字表記 \\{str} は 文字定数の表現範囲(現時点では8bit整数値)を超えます。 ");
                                }
                                c2 = str.ElementAtOrDefault(i+1+j);
                                if ('0' <= c2 && c2 <= '9') { r = (r << 4) | (UInt32)(c2 - '0'); continue; }
                                if ('a' <= c2 && c2 <= 'f') { r = (r << 4) | (UInt32)(c2 - 'a' + 10); continue; }
                                if ('A' <= c2 && c2 <= 'F') { r = (r << 4) | (UInt32)(c2 - 'A' + 10); continue; }
                                break;
                            }
                            i += j+1;
                            return new byte[] { (byte)r };
                        }
                    }
                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7': {
                        UInt32 r = 0;
                        int j;
                        for (j = 0; j < 3; j++) {
                            int c2 = str.ElementAtOrDefault(i+j);
                            if ('0' <= c2 && c2 <= '7') {
                                r = (r << 3) | (UInt32)(c2 - '0');
                            } else {
                                break;
                            }
                        }
                        i += j;
                        return new byte[] { (byte)r };
                    }
                default: {
                    CppContext.Warning(tok, $"\\{(char)c} はISO/IEC 9899：1999で定められていないエスケープ文字です。" +
                                            $"規格で定められていないエスケープ文字のうち、小文字は将来に向けての予約語、それ以外は処理系定義の動作となります(ISO/IEC 9899：1999 6.4.4.4 注釈(64) および 6.11.4 参照)。" +
                                            $"本処理系では文字 `{(char)c}` として解釈します。");
                    i += 1;
                    return System.Text.Encoding.UTF8.GetBytes(new[] { (char)c });
                }
            }

        }

        private static BigInteger Expr(int priority, int skip) {
            Token tok = Lex.LexToken(limit_space: true);

            BigInteger lhs;
            if (tok.IsKeyword('(')) {
                lhs = Expr(0, skip);
                tok = Lex.LexToken(limit_space: true);
                if (tok.IsKeyword(')') == false) {
                    if (tok.Kind == Token.TokenKind.EoF) {
                        CppContext.Error(tok, $"プリプロセス指令の定数式中で始め丸括弧 `(` に対応する終わり丸括弧 `)` がありませんでした。");
                    } else {
                        CppContext.Error(tok, $"プリプロセス指令の定数式中で始め丸括弧 `(` に対応する終わり丸括弧 `)` があるべき場所に {Token.TokenToStr(tok)} がありました。");
                    }
                    Lex.unget_token(tok);
                }
            } else if (tok.IsKeyword('~')) {
                lhs = ~Expr(11, skip);
                lhs = skip > 0 ? 0 : lhs;
            } else if (tok.IsKeyword('!')) {
                lhs = (Expr(11, skip) == 0 ? 1 : 0);
                lhs = skip > 0 ? 0 : lhs;
            } else if (tok.IsKeyword('+')) {
                lhs = Expr(11, skip);
                lhs = skip > 0 ? 0 : lhs;
            } else if (tok.IsKeyword('-')) {
                lhs = -Expr(11, skip);
                lhs = skip > 0 ? 0 : lhs;
            } else if (tok.Kind == Token.TokenKind.Number) {
                lhs = Token.ToInt(tok, tok.StrVal);
                lhs = skip > 0 ? 0 : lhs;
            } else if (tok.Kind == Token.TokenKind.Char) {
                lhs = parse_char(tok);
                lhs = skip > 0 ? 0 : lhs;
            } else if (tok.Kind == Token.TokenKind.Ident) {
                if (skip == 0) {
                    CppContext.Warning(tok, $"プリプロセス指令の定数式中に未定義の識別子 '{tok.StrVal}' が出現しています。 0 に読み替えられます。");
                }
                lhs = 0;
            } else if (tok.Kind == Token.TokenKind.NewLine || tok.Kind == Token.TokenKind.EoF) {
                CppContext.Error(tok, "プリプロセス指令の条件式が不完全なまま行末を迎えています。");
                return 0;
            } else {
                if (skip == 0) {
                    if (tok.Kind ==  Token.TokenKind.String)
                    CppContext.Error(tok, $"プリプロセス指令の条件式中で 文字列リテラル は使用できません。");
                } else {
                    CppContext.Error(tok, $"プリプロセス指令の条件式中で {Token.TokenToStr(tok)} は使用できません。");
                }
                return 0;
            }

            for (;;) {
                Token op = Lex.LexToken(limit_space: true);
                int pri = expr_priority(op); // 0 if not a binop.
                if (pri == 0 || priority >= pri) {
                    Lex.unget_token(op);
                    return lhs;
                }
                if (op.Kind != Token.TokenKind.Keyword) {
                    CppContext.Error(op, $"演算子のあるべき場所に {Token.TokenToStr(op)} がありますが、これは演算子として定義されていません。");
                    return 0;
                }
                switch (op.KeywordVal) {
                    case (Token.Keyword)'/': {
                            BigInteger rhs = Expr(pri, skip);
                            if (skip > 0) {
                                lhs = 0;
                            } else if (rhs == 0) {
                                CppContext.Error(op, "除算式の除数がゼロです。");
                                lhs = 0;    // NaN相当のほうがいいのかね
                            } else {
                                lhs = lhs / rhs;
                            }
                            break;
                        }
                    case (Token.Keyword)'%': {
                            BigInteger rhs = Expr(pri, skip);
                            if (skip > 0) {
                                lhs = 0;
                            } else if (rhs == 0) {
                                CppContext.Error(op, "剰余算式の除数がゼロです。");
                                lhs = 0;
                            } else {
                                lhs = lhs % rhs;
                            }
                            break;
                        }
                    case (Token.Keyword)'*': {
                            BigInteger rhs = Expr(pri, skip);
                            lhs = (skip > 0) ? 0 : (lhs * rhs);
                            break;
                        }
                    case (Token.Keyword)'+': {
                            BigInteger rhs = Expr(pri, skip);
                            lhs = (skip > 0) ? 0 : (lhs + rhs);
                            break;
                        }
                    case (Token.Keyword)'-': {
                            BigInteger rhs = Expr(pri, skip);
                            lhs = (skip > 0) ? 0 : (lhs - rhs);
                            break;
                        }
                    case (Token.Keyword)'<': {
                            BigInteger rhs = Expr(pri, skip);
                            lhs = (skip > 0) ? 0 : (lhs < rhs ? 1 : 0);
                            break;
                        }
                    case (Token.Keyword)'>': {
                            BigInteger rhs = Expr(pri, skip);
                            lhs = (skip > 0) ? 0 : (lhs > rhs ? 1 : 0);
                            break;
                        }
                    case (Token.Keyword)'&': {
                            BigInteger rhs = Expr(pri, skip);
                            lhs = (skip > 0) ? 0 : (lhs & rhs);
                            break;
                        }
                    case (Token.Keyword)'^': {
                            BigInteger rhs = Expr(pri, skip);
                            lhs = (skip > 0) ? 0 : (lhs ^ rhs);
                            break;
                        }
                    case (Token.Keyword)'|': {
                            BigInteger rhs = Expr(pri, skip);
                            lhs = (skip > 0) ? 0 : (lhs | rhs);
                            break;
                        }

                    case Token.Keyword.ShiftArithLeft: {
                            BigInteger rhs = Expr(pri, skip);
                            if (skip > 0) {
                                lhs = 0;
                            } else if (rhs < 0) {
                                CppContext.Error(op, "左シフトで0ビット未満のシフトは行えません。");
                            } else if (rhs >= 64) {
                                CppContext.Error(op, "左シフトで64ビット以上のシフトは行えません。");
                            } else {
                                lhs = lhs << (int)rhs;
                            }
                            break;
                        }
                    case Token.Keyword.ShiftArithRight: {
                            BigInteger rhs = Expr(pri, skip);
                            if (skip > 0) {
                                lhs = 0;
                            } else if (rhs < 0) {
                                CppContext.Error(op, "右シフトで0ビット未満のシフトは行えません。");
                            } else if (rhs >= 64) {
                                CppContext.Error(op, "右シフトで64ビット以上のシフトは行えません。");
                            } else {
                                lhs = lhs >> (int)rhs;
                            }
                            break;
                        }
                    case Token.Keyword.LessEqual: {
                            BigInteger rhs = Expr(pri, skip);
                            lhs = (skip > 0) ? 0 : (lhs <= rhs ? 1 : 0);
                            break;
                        }
                    case Token.Keyword.GreatEqual: {
                            BigInteger rhs = Expr(pri, skip);
                            lhs = (skip > 0) ? 0 : (lhs >= rhs ? 1 : 0);
                            break;
                        }
                    case Token.Keyword.Equal: {
                            BigInteger rhs = Expr(pri, skip);
                            lhs = (skip > 0) ? 0 : (lhs == rhs ? 1 : 0);
                            break;
                        }
                    case Token.Keyword.NotEqual: {
                            BigInteger rhs = Expr(pri, skip);
                            lhs = (skip > 0) ? 0 : (lhs != rhs ? 1 : 0);
                            break;
                        }
                    case Token.Keyword.LogicalAnd:
                        // 短絡評価しなければならない
                        if (skip == 0 && lhs != 0) {
                            BigInteger rhs = Expr(pri, skip);
                            lhs = rhs != 0 ? 1 : 0;
                        } else {
                            Expr(pri, skip + 1);
                            lhs = 0;
                        }
                        break;
                    case Token.Keyword.LogincalOr:
                        // 短絡評価しなければならない
                        if (skip == 0 && lhs == 0) {
                            BigInteger rhs = Expr(pri, skip);
                            lhs = rhs != 0 ? 1 : 0;
                        } else {
                            Expr(pri, skip + 1);
                            lhs = 1;
                        }
                        break;
                    case (Token.Keyword)'?': {
                            // ３項演算子
                            BigInteger rhs1 = Expr(0, skip + (lhs == 0 ? 1 : 0));
                            Token tok2 = Lex.LexToken();
                            if (tok2.IsKeyword(':') == false) {
                                CppContext.Error(op, $"三項演算子の`:`があるべき場所に `{Token.TokenToStr(tok2)}` があります。");
                                return 0;
                            }
                            BigInteger rhs2 = Expr(0, skip + (lhs != 0 ? 1 : 0));
                            if (skip > 0) {
                                lhs = 0;
                            } else {
                                lhs = (lhs != 0) ? rhs1 : rhs2;
                            }
                            return lhs;
                        }

                    default:
                        CppContext.Error(op, $"演算子のあるべき場所に `{Token.TokenToStr(op)}` がありますが、これは演算子として定義されていません。");
                        return 0;

                }
            }
        }

        //

        static bool read_constexpr(out List<Token> exprtokens) {
            /* コンパイルスイッチの記録のために前処理トークン列を展開無しで先読みして記録する */
            {
                exprtokens = new List<Token>();
                Token pretok = Lex.LexToken(limit_space: true);
                while (pretok.Kind != Token.TokenKind.EoF && pretok.Kind != Token.TokenKind.NewLine) {
                    exprtokens.Add(pretok);
                    pretok = Lex.LexToken(limit_space: true);
                    if (pretok.Kind == Token.TokenKind.Invalid) {
                        CppContext.Error(pretok, $@"プリプロセス指令の条件式中に不正な文字 `\x{(int)pretok.StrVal[0]:X2}` がありました。");
                    }
                }
                    exprtokens.Add(Token.make_eof(pretok.Pos));
                Lex.unget_token(pretok);
                foreach (var t in exprtokens.Reverse<Token>()) {
                    Lex.unget_token(t);
                }
            }

            var intexprtoks = read_intexpr_line().Reverse<Token>().ToList();
            Lex.token_buffer_stash(intexprtoks);
            var expr = Expr(0, 0);
            Token tok = Lex.LexToken(limit_space: true);
            if (tok.Kind != Token.TokenKind.EoF) {
                CppContext.Error(tok, $"プリプロセス指令の条件式中に余分なトークン {Token.TokenToStr(tok)} がありました。");
                while (tok.Kind != Token.TokenKind.EoF) {
                    tok = Lex.LexToken(limit_space: true);
                }
            }
            Lex.token_buffer_unstash();
            return !expr.IsZero;
        }

        static void do_read_if(bool isTrue) {
            ConditionStack.Add(new Condition(isTrue));
            if (!isTrue) {
                Lex.skip_cond_incl();
            }
        }

        static Token read_if(Token hash, Token tif) {
            var nl = new Token(hash, Token.TokenKind.NewLine);
            List<Token> exprtoken;
            var cond = read_constexpr(out exprtoken);
            do_read_if(cond);
            // コンパイルスイッチを記録
            Reporting.TraceCompileSwitch.OnIf(hash.Pos, string.Join(" ", exprtoken.Select(x => Token.TokenToStr(x))), cond);
            return nl;
        }

        static Token read_ifdef(Token hash, Token tifdef) {
            var nl = new Token(hash, Token.TokenKind.NewLine);
            Token tok = Lex.LexToken(limit_space: true);
            if (tok.Kind != Token.TokenKind.Ident) {
                CppContext.Error(tok, $"識別子があるべき場所に {Token.TokenToStr(tok)} がありました。");
                while (tok.Kind != Token.TokenKind.NewLine && tok.Kind != Token.TokenKind.EoF) {
                    tok = Lex.LexToken(limit_space: true);
                }
                Lex.unget_token(tok);
            } else {
                expect_newline();

                // 参照されたことのあるマクロとして記録する
                RefMacros.Add(tok.StrVal);

                var cond = Macros.ContainsKey(tok.StrVal) && Macros[tok.StrVal] != null;
                if (cond) {
                    Macros[tok.StrVal].Used = true;
                }

                // コンパイルスイッチを記録
                Reporting.TraceCompileSwitch.OnIfdef(hash.Pos, tok.StrVal, cond);

                do_read_if(cond);
            }
            return nl;
        }

        /// <summary>
        /// 'ifndef'指令の処理
        /// </summary>
        static Token read_ifndef(Token hash, Token tifndef) {
            var nl = new Token(hash, Token.TokenKind.NewLine);

            Token tok = Lex.LexToken(limit_space: true);
            bool cond = false;
            if (tok.Kind != Token.TokenKind.Ident) {
                if (tok.Kind == Token.TokenKind.NewLine || tok.Kind == Token.TokenKind.EoF) {
                    CppContext.Error(tok, $"識別子がありません。");
                } else {
                    CppContext.Error(tok, $"識別子があるべき場所に {Token.TokenToStr(tok)} がありました。");
                }
                while (tok.Kind != Token.TokenKind.NewLine && tok.Kind != Token.TokenKind.EoF) {
                    tok = Lex.LexToken(limit_space: true);
                }
            } else {
                expect_newline();


                // 参照されたことのあるマクロとして記録する
                RefMacros.Add(tok.StrVal);

                cond = !Macros.ContainsKey(tok.StrVal) || Macros[tok.StrVal] == null;
                if (cond == false) {
                    Macros[tok.StrVal].Used = true;
                }

                // コンパイルスイッチを記録
                Reporting.TraceCompileSwitch.OnIfndef(hash.Pos, tok.StrVal, cond);
            }

            do_read_if(cond);
            if (tok.IndexOfFile == 1) {
                // ファイルから読み取ったトークンが
                //   一つ目が '#'
                //   二つ目が 'ifndef'
                // の場合、「インクルードガード」パターンかもしれないので
                // 検出の準備をする
                Condition ci = ConditionStack.Last();
                ci.IncludeGuard = tok.StrVal;
                ci.File = tok.File;
            }
            return nl;
        }

        /// <summary>
        /// 'else'指令の処理
        /// </summary>
        static Token read_else(Token hash, Token telse) {
            var nl = new Token(hash, Token.TokenKind.NewLine);
            if (ConditionStack.Count == 0) {
                CppContext.Error(hash, "#if / #ifdef と対応の取れない #else がありました。");
                // エラー回復方法がコンパイラによってまちまちなので #else を読み飛ばす方向で
                var tok = hash;
                while (tok.Kind != Token.TokenKind.NewLine && tok.Kind != Token.TokenKind.EoF) {
                    tok = Lex.LexToken();
                }
                Lex.unget_token(tok);
            } else {
                // 現在の条件コンパイル状態を確認して、else解析中の場合、elseが多重なのでエラー
                Condition ci = ConditionStack.Last();
                if (ci.CurrentState == Condition.State.InElse) {
                    CppContext.Error(hash, "#else の後に再度 #else が出現しました。");
                    // エラー回復方法がコンパイラによってまちまちなので #else を読み飛ばす方向で
                    var tok = hash;
                    while (tok.Kind != Token.TokenKind.NewLine && tok.Kind != Token.TokenKind.EoF) {
                        tok = Lex.LexToken();
                    }
                    Lex.unget_token(tok);
                } else {
                    // ELSE解析中に遷移する。
                    //tokens.Add(hash);
                    {
                        Token tok = Lex.LexToken();
                        if (tok.Kind != Token.TokenKind.NewLine) {
                            if (CppContext.Warnings.Contains(Warning.Pedantic)) {
                                CppContext.Warning(tok, "#else ディレクティブの末尾に余分なトークンがあります。");
                            }
                            while (tok.Kind != Token.TokenKind.NewLine) {
                                tok = Lex.LexToken();
                            }
                        }
                    }

                    // コンパイルスイッチを記録
                    Reporting.TraceCompileSwitch.OnElse(hash.Pos, !ci.WasTrue);

                    ci.CurrentState = Condition.State.InElse;
                    // #else が出てきたということは、インクルードガードパターンに合致しないので、
                    // 現在の処理している条件コンパイルブロックはインクルードガードではない。
                    // なので、条件コンパイル情報からインクルードガードであることを消す。
                    ci.IncludeGuard = null;

                    // elseに出会うまでに有効な条件ブロックがあった場合はelseブロックを読み飛ばす
                    if (ci.WasTrue) {
                        Lex.skip_cond_incl();
                    }
                }
            }
            return nl;
        }

        /// <summary>
        /// elif指令の処理
        /// </summary>
        /// <param name="hash"></param>
        /// <param name="telif"></param>
        static Token read_elif(Token hash, Token telif) {
            var nl = new Token(hash, Token.TokenKind.NewLine);
            if (ConditionStack.Count == 0) {
                // #if ～ #endif の間以外で #elif が使われている
                CppContext.Error(hash, "対応の取れない #elif がありました。");
                // エラー回復方法がコンパイラによってまちまちなので #elif を読み飛ばす方向で
                var tok = hash;
                while (tok.Kind != Token.TokenKind.NewLine && tok.Kind != Token.TokenKind.EoF) {
                    tok = Lex.LexToken(limit_space: true);
                }
                Lex.unget_token(tok);
            } else {
                Condition ci = ConditionStack.Last();
                if (ci.CurrentState == Condition.State.InElse) {
                    // #else の後に #elif  が使われている

                    CppContext.Error(hash, "#else の後に #elif が出現しました。");
                    // エラー回復方法がコンパイラによってまちまちなので #elif を読み飛ばす方向で
                    var tok = hash;
                    while (tok.Kind != Token.TokenKind.NewLine && tok.Kind != Token.TokenKind.EoF) {
                        tok = Lex.LexToken(limit_space: true);
                    }
                    Lex.unget_token(tok);
                } else {
                    // 妥当な位置で #elif  が使われている
                    ci.CurrentState = Condition.State.InElif;
                    // #elif が出てきたということは、インクルードガードパターンに合致しないので、
                    // 現在の処理している条件コンパイルブロックはインクルードガードではない。
                    // なので、条件コンパイル情報からインクルードガードであることを消す。
                    ci.IncludeGuard = null;
                    List<Token> exprtoken;
                    if (!read_constexpr(out exprtoken) || ci.WasTrue) {
                        // コンパイルスイッチを記録
                        Reporting.TraceCompileSwitch.OnElif(hash.Pos, string.Join(" ", exprtoken.Select(x => Token.TokenToStr(x))), false);
                        Lex.skip_cond_incl();
                    } else {
                        // コンパイルスイッチを記録
                        Reporting.TraceCompileSwitch.OnElif(hash.Pos, string.Join(" ", exprtoken.Select(x => Token.TokenToStr(x))), true);
                        ci.WasTrue = true;
                    }
                }
            }
            return nl;
        }

        static Token read_endif(Token hash, Token tendif) {
            var nl = new Token(hash, Token.TokenKind.NewLine);
            if (ConditionStack.Count == 0) {
                CppContext.Error(hash, "対応の取れない #endif がありました。");
                // エラー回復方法がコンパイラによってまちまちなので #endif を読み飛ばす方向で
                var tok = hash;
                while (tok.Kind != Token.TokenKind.NewLine && tok.Kind != Token.TokenKind.EoF) {
                    tok = Lex.LexToken(limit_space: true);
                }
                Lex.unget_token(tok);
            } else {

                Condition ci = ConditionStack.Pop();

                {
                    Token tok = Lex.LexToken(limit_space: true);
                    if (tok.Kind != Token.TokenKind.NewLine) {
                        if (CppContext.Warnings.Contains(Warning.Pedantic)) {
                            CppContext.Warning(tok, "#endif ディレクティブの末尾に余分なトークンがあります。");
                        }
                        while (tok.Kind != Token.TokenKind.NewLine) {
                            tok = Lex.LexToken(limit_space: true);
                        }
                    }
                }

                // コンパイルスイッチを記録
                Reporting.TraceCompileSwitch.OnEndif(hash.Pos);

                // Detect an #ifndef and #endif pair that guards the entire
                // header file. Remember the macro name guarding the file
                // so that we can skip the file next time.
                if (ci.IncludeGuard == null || ci.File != hash.File) {
                    // 現在の処理している条件コンパイルブロックはインクルードガードパターンに合致しない
                } else {
                    // 空白と改行を読み飛ばして先読みを行う
                    Token last = Lex.LexToken(limit_space: true);
                    SpaceInfo sp = last.Space;
                    while (last.Kind == Token.TokenKind.NewLine && ci.File == last.File) {
                        sp.Append(new Position(last.File.Name, last.File.Line, last.File.Column), "\n");
                        last = Lex.LexToken(limit_space: true);
                    }
                    last.Space = sp;
                    Lex.unget_token(last);

                    if (ci.File != last.File) {
                        // 先読みしたトークンのファイル情報が違っている＝#include処理が終了していることになる。
                        // これにより現在の処理している条件コンパイルブロックはインクルードガードパターンに合致したので
                        // インクルードガード情報にファイルを保存する
                        IncludeGuards[ci.File.Name] = ci.IncludeGuard;
                    }
                }
            }
            return nl;
        }

        /*
         * #error and #warning
         */

        static string read_error_message() {
            StringBuilder sb = new StringBuilder();
            for (;;) {
                Token tok = Lex.LexToken(limit_space: true);
                if (tok.Kind == Token.TokenKind.NewLine) { return sb.ToString(); }
                if (tok.Kind == Token.TokenKind.EoF) { return sb.ToString(); }
                if (sb.Length != 0 && tok.Space.Length > 0) { sb.Append(' '); }
                sb.Append(Token.TokenToStr(tok));
            }
        }

        static Token read_error(Token hash, Token terror) {
            var nl = new Token(hash, Token.TokenKind.NewLine);
            CppContext.Error(hash, $"#error: {read_error_message()}");
            return nl;
        }

        static Token read_warning(Token hash, Token twarning) {
            var nl = new Token(hash, Token.TokenKind.NewLine);
            CppContext.Warning(hash, $"#warning: {read_error_message()}");
            return nl;
        }

        /*
         * #include ディレクティブ
         */

        /// <summary>
        /// 6.10.2 ソースファイル取込み に従って
        /// #include に続くファイルパス部分の読み取りと処理を行う
        /// </summary>
        /// <param name="hash"></param>
        /// <param name="isGuillemet">パスが山括弧で括られている場合は真</param>
        /// <returns></returns>
        static string read_cpp_header_name(Token hash, out bool isGuillemet) {
            
            /* 
             * ファイルパス部分の読み取り
             * <～>形式の場合は std が true に、"～"形式の場合は false になる
             */
            string path = Lex.read_header_file_name(hash, out isGuillemet);
            if (path != null) {
                // 読み取れたのでパスを返す
                return path;
            }

            /**
             *  
             * 6.10.2 では次のように規定されている。
             * 
             * 次の（上の二つの形式のいずれとも一致しない）形式の前処理指令
             *   # include 前処理字句列 改行
             * も許す。この指令の中の，include の後ろにある前処理字句列は，通常のテキストと同様に処理する
             * （その時点でマクロ名として定義している各識別子は，その識別子の置換要素並びの前処理字句列に置き換える。）。
             * すべての置き換えを行った後の指令は，この箇条で規定する二つの形式のうちのいずれかと一致しなければならない(143)。
             * 前処理字句<と>，又は二つの"文字の対に囲まれた前処理字句列を，一つのヘッダ名の前処理字句に解釈する方法は，処理系定義とする。
             * 
             * (143) 隣接した文字列リテラルが，連結して一つの文字列リテラルになることはない（5.1.1.2 の翻訳フェーズ参照） 。
             *       したがって，展開の結果二つの文字列リテラルが現れると正しくない指令行となる。
             * 
             * なので、gccとclangで違う結果が得られても処理系依存だからの一言で済む。
             * だから、移植性を考えるなら include 指令内 で前処理字句列を使うのは避けた方が良い。
             */

            // マクロ展開を有効にしてトークンを読み取る
            Token tok = read_expand();
            if (tok.Kind == Token.TokenKind.NewLine) {
                CppContext.Error(hash, "識別子があるべき場所に改行がありました。");
                Lex.unget_token(tok);
                return null;
            }
            if (tok.Kind == Token.TokenKind.String) {
                // マクロ展開の結果文字列が得られた場合は "～"形式
                // 上記(143)に「隣接した文字列リテラルが，連結して一つの文字列リテラルになることはない」とあるので、
                // 先読みなどは行わない。
                // (でもエラー出したいよね。)
                isGuillemet = false;
                return tok.StrVal;
            } else if (tok.IsKeyword('<')) {
                // マクロ展開の結果'<'が得られた場合は<～>形式と推定して入力を読み進める
                var sb = new StringBuilder();
                bool saveComment = CppContext.Switchs.Contains("-C");
                CppContext.Switchs.Remove("-C");
                for (;;) {
                    Token tok2 = read_expand();
                    if (tok2.Kind == Token.TokenKind.NewLine) {
                        CppContext.Error(tok, "ヘッダファイルが閉じられないまま行末に到達しました。");
                        Lex.unget_token(tok);
                        break;
                    }
                    sb.Append(tok2.Space.Any() ? " ":"");
                    if (tok2.IsKeyword('>')) {
                        break;
                    }
                    sb.Append(Token.TokenToStr(tok2));
                }
                if (saveComment)
                {
                    CppContext.Switchs.Add("-C");
                }
                isGuillemet = true;
                // トークンを全て単純に連結した文字列を返す
                return sb.ToString();
            } else {
                // どっちでもない場合はダメ
                CppContext.Error(tok, $"ヘッダファイル名のあるべき場所に {Token.TokenToStr(tok)} がありました。");
                while (tok.Kind != Token.TokenKind.NewLine && tok.Kind != Token.TokenKind.EoF) {
                    tok = Lex.LexToken(limit_space: true);
                }
                Lex.unget_token(tok);
                return null;
            }
        }

        /// <summary>
        /// インクルードガードされているか判定
        /// </summary>
        /// <param name="hash"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        static bool Guarded(Token hash, string path) {
            string guard = IncludeGuards.ContainsKey(path) ? IncludeGuards[path] : null;
            bool r = (guard != null && Macros.ContainsKey(guard) && Macros[guard] != null);
            if (guard != null && Macros.ContainsKey(guard) && Macros[guard] != null) {
                Macros[guard].Used = true;
            }
            return r;
        }

        /// <summary>
        /// include 対象ファイルの存在を調べる
        /// </summary>
        /// <param name="hash"></param>
        /// <param name="dir"></param>
        /// <param name="filename"></param>
        /// <returns></returns>
        static string SearchInclude(Token hash, string dir, string filename) {
            string path = "";
            if (System.IO.Path.IsPathRooted(filename)) {
                try {
                    path = System.IO.Path.GetFullPath(filename);
                } catch {
                    return null;
                }
            } else {
                if (String.IsNullOrWhiteSpace(dir)) {
                    dir = ".";
                }
                try {
                    path = System.IO.Path.GetFullPath($"{dir}/{filename}");
                } catch {
                    return null;
                }
            }
            if (System.IO.File.Exists(path) == false) {
                return null;
            }

            return path;
        }


        /// <summary>
        /// include を行う
        /// </summary>
        /// <param name="hash"></param>
        /// <param name="dir"></param>
        /// <param name="filename"></param>
        /// <returns></returns>
        static bool TryInclude(Token hash, string dir, string filename) {
            string path = "";
            if (System.IO.Path.IsPathRooted(filename)) {
                try {
                    path = System.IO.Path.GetFullPath(filename);
                } catch {
                    return false;
                }
            } else {
                if (String.IsNullOrWhiteSpace(dir)) {
                    dir = ".";
                }
                try {
                    path = System.IO.Path.GetFullPath($"{dir}/{filename}");
                } catch {
                    return false;
                }
            }

            if (CppContext.Warnings.Contains(Warning.CertCCodingStandard)) {
                #region PRE04-C. 標準ヘッダファイル名を再利用しない
                /*
                 * PRE04-C. 標準ヘッダファイル名を再利用しない
                 * 標準ヘッダと同じ名前のファイルが、インクルードされるソースファイルのサーチパスにある場合、動作は未定義である。
                 * 
                 * 7.1.2 標準ヘッダ
                 * - 次に示すヘッダを，標準ヘッダとする
                 *   <assert.h> <inttypes.h> <signal.h> <stdlib.h> <complex.h> <iso646.h> <stdarg.h> <string.h> <ctype.h> <limits.h> <stdbool.h> <tgmath.h>
                 *   <errno.h>  <locale.h> <stddef.h> <time.h> <fenv.h> <math.h> <stdint.h> <wchar.h> <float.h> <setjmp.h> <stdio.h> <wctype.h>
                 * 
                 * - 処理系の一部として提供するもの以外に，この箇条で規定する標準ヘッダの<及び>で囲まれた列のいずれかと同じ名前のファイルが，
                 *   取り込まれるソースファイルを探索する標準の位置のいずれかにある場合，その動作は未定義とする
                 */
                var target = SearchInclude(hash, dir, filename);
                if (target == null || StdIncludePath.Any(x => target.StartsWith(x, StringComparison.OrdinalIgnoreCase)) == false) {
                    // システムインクルードディレクトリ内のファイルが"..."で参照するケースではないときにチェック
                    var conflicts = StdIncludePath.Select(x => SearchInclude(hash, x, filename)).Where(x => x != null).ToList();
                    if (conflicts.Any()) {
                        CppContext.Warning(hash, $@"[PRE04-C] 標準ヘッダと同じ名前のファイル {filename} をシステムインクルードディレクトリ以外からインクルードしています。C90以降のC言語規格では、このようなコードは未定義の動作となることが示されています(参考文献:[ISO/IEC 9899:2011] 7.1.2 P3)。");
                    }
                }
                #endregion

            }

            if (Once.ContainsKey(path) && Once[path] == "1") {
                // #pragma once 済みの場合
                return true;
            }
            if (Guarded(hash, path)) {
                // インクルードガードパターンを伴う二回目以降のincludeの場合
                return true;
            }
            if (System.IO.File.Exists(path) == false) {
                // ファイルがない場合
                return false;
            }

            // ファイルを開いてファイルスタックに積む
            var sr = Encoding.CreateStreamReaderFromFile(path, CppContext.AutoDetectEncoding, CppContext.DefaultEncoding);
            if (sr == null) {
                return false;
            }

            // 入力ストリームにファイルを積む
            File.stream_push(new File(sr, path));

            return true;
        }

        static string NormalizeRelativePath(string path) {
            Stack<string> s = new Stack<string>();
            foreach (var part in path.Split(System.IO.Path.DirectorySeparatorChar)) {
                switch (part) {
                    case ".":
                        break;
                    case "..":
                        if (s.Count == 0 || s.Peek() == "..") {
                            s.Push(part);
                        } else {
                            s.Pop();
                        }
                        break;
                    default:
                        s.Push(part);
                        break;
                }
            }
            return System.IO.Path.Combine(s.ToArray());
        }

        static Token ParseIncludeDirective(Token hash, Token tinclude) {
            //var nl = new Token(hash, Token.TokenKind.NewLine);
            var nl = new Token(hash, Token.TokenKind.Space);
            File file = tinclude.File;
            bool isGuillemet;
            string filename = read_cpp_header_name(hash, out isGuillemet);

            {
                Token tok2 = read_expand();
                if (tok2.Kind != Token.TokenKind.NewLine) {
                    CppContext.Error(tok2, "#include指令の後ろに空白以外の要素があります。");
                    while (tok2.Kind != Token.TokenKind.NewLine) {
                        tok2 = read_expand();
                    }
                }
                Lex.unget_token(tok2);
            }

            if (filename == null) {
                // read_cpp_header_name 中でエラー出力しているはずなので何も出さずに抜ける
                return nl;  // 失敗ケースなので、goto succ はダメ
            } else if (System.IO.Path.IsPathRooted(filename)) {
                // レアケースだが、絶対パスが指定されている場合はinclude形式に関係なく直接そのファイルを探して読み込む
                if (TryInclude(hash, "/", filename)) {  // "/" を渡しているが絶対パス指定なので意味はない。
                    goto succ;
                }
                goto err;
            }

            /* #include "..." 形式の場合、カレントディレクトリ⇒インクルードパス(-Iオプション)⇒システムインクルードディレクトリ の順で検索 */
            /* #include <...> 形式の場合、システムインクルードディレクトリ のみ検索 */


            if (CppContext.Warnings.Contains(Warning.CertCCodingStandard)) {
                #region PRE08-C. ヘッダファイル名が一意であることを保証する
                /*
                 * PRE08-C. ヘッダファイル名が一意であることを保証する
                 * インクルードするヘッダファイル名は一意となるよう配慮。
                 * 
                 * ISO/IEC 9899:2011 6.10.2 ソースファイル取込み
                 * - 処理系は，一つ以上の英字又は数字（5.2.1 で定義する）の列の後ろにピリオド（.）及び一つの英字が続く形式に対して，一意の対応付けを提供しなければならない。
                 *   最初の文字は，英字でなければならない。
                 *   処理系は，アルファベットの大文字と小文字の区別を無視してもよく，対応付けはピリオドの前8文字に対してだけ有効であると限定してもよい
                 * 
                 * これはつまり以下を意味する。
                 * -ファイル名の最初の8文字までは有効であることが保証されている。
                 * -ファイル名中のピリオドの後には、非数字文字を一文字だけ持つ。
                 * -ファイル名中の文字の大文字小文字の区別が有効であることは保証されていない。
                 *   
                 * ヘッダファイル名が一意であることを保証するには、インクルードされるすべてのファイル名が、その最初の8文字あるいは(1文字の)ファイル拡張子において(大文字小文字を区別せずに)異なっていなくてはならない。
                 * 
                 * 相対パス形式の場合は明示されていないので、Cert-Cのcommentから判断基準例を抜粋
                 * これらは全てパスとしてみると別なので妥当
                 * #include "a/b.h" => "a/b"
　　　　　　　　 * #include "c/b.h" => "c/b"
                 * #include "b.h"   => "b"
                 * これらは全てパスとしてみると同一なので違反
                 * #include <D.h>   => "d" 
                 * #include "d.h"   => "d"
                 * これらは全てパスとしてみると同一なので違反
                 * #include "e.h"   => "e" 
                 * #include "e"     => "e"
                 */
                var dir = NormalizeRelativePath(System.IO.Path.GetDirectoryName(filename));
                var name = System.IO.Path.GetFileName(filename);
                var match = System.Text.RegularExpressions.Regex.Match(name, @"^([^\.]{0,8})([^\.]*?)(\.[^\d])?$");
                if (match.Success) {
                    var head = match.Groups[1].Value.ToLower();
                    var ext = match.Groups[3].Value;
                    var key = System.IO.Path.Combine(dir, head);
                    List<Tuple<string, string, Position>> entries;
                    if (PRE08IncludeFileTable.TryGetValue(key, out entries)) {
                        var other = entries.FirstOrDefault(x => x.Item1 != name);
                        if (other != null) {
                            CppContext.Warning(hash, $@"[PRE08-C] インクルードで指定されたファイル {filename} のディレクトリと拡張子を除いたファイル名の先頭最大８文字 {key} が 別のインクルードで指定されたファイル {other.Item2} と大文字小文字を区別せずに一致するため、C言語規格上は曖昧なインクルードとなる可能性が示されています(参考文献:[ISO/IEC 9899:2011] 6.10.2)。");
                        }
                        if (entries.Any(x => x.Item1 == name) == false) {
                            entries.Add(Tuple.Create(name, filename, hash.Pos));
                        }
                    } else {
                        PRE08IncludeFileTable[key] = new List<Tuple<string, string, Position>>() { Tuple.Create(name, filename, hash.Pos) };
                    }
                }

                #endregion
            }

            if (isGuillemet == false) {
                /**
                 *  ISO/IEC 9899 6.10.2 ソースファイル取込み　で、
                 *    指定したソースファイルの探索手順は処理系定義とする。
                 *  と規定されている。
                 */

                /* 現在のファイルと同じディレクトリから探索 */
                string dir = ".";
                try {
                    dir = file.Name != null ? System.IO.Path.GetDirectoryName(file.Name) : ".";
                } catch {
                    // System.IO.Path.GetDirectoryNameに失敗
                }
                if (TryInclude(hash, dir, filename)) {
                    goto succ;
                }

                /* -Iオプションの指定順で探索 */
                foreach (var path in UserIncludePath) {
                    if (TryInclude(hash, path, filename)) {
                        goto succ;
                    }
                }
                /*
                 * ISO/IEC 9899 6.10.2 ソースファイル取込み　では、#include "～" のファイル探索に失敗した場合、#include <～> に読み替えて再探索することと規定されている。
                 * そのため、ユーザーインクルードパス中から見つからなかった場合でもエラーとせず、標準インクルードパスからの探索を行う。
                 */
            }

            /* システムインクルードディレクトリを探索する */
            {
                /**
                 * ISO/IEC 9899 6.10.2 ソースファイル取込み　で、
                 *   どのようにして探索の場所を指定するか，またどのようにしてヘッダを識別するかは，処理系定義とする。
                 * とされている。
                 */
                foreach (var path in StdIncludePath) {
                    if (TryInclude(hash, path, filename)) {
                        if (!isGuillemet && CppContext.Warnings.Contains(Warning.ImplicitSystemHeaderInclude)) {
                            CppContext.Warning(hash, $"二重引用符形式で指定されたファイル {filename} がインクルードパス中では見つからず、システムインクルードディレクトリ中から見つかりました。この動作は「ISO/IEC 9899 6.10.2 ソースファイル取込み」で規定されている標準動作ですが、意図したものでなければ二重引用符ではなく山括弧の利用を検討してください。");
                        }
                        goto succ;
                    }
                }
                goto err;
            }

            succ:
            return nl;

            err:
            CppContext.Error(hash, $"#includeで指定されたファイル {(isGuillemet ? "<" : "\"")}{filename}{(isGuillemet ? ">" : "\"")} が見つかりません。");
            return nl;
        }

        /// <summary>
        /// #pragma ディレクティブの処理
        /// </summary>
        /// <param name="hash"></param>
        /// <param name="tpragma"></param>
        /// <returns></returns>
        static Token ParsePragmaDirective(Token hash, Token tpragma) {
            var nl = new Token(hash, Token.TokenKind.NewLine);
            Token tok = read_ident(limit_space: true);
            string s = tok.StrVal;
            switch (s) {
                case "once": {
                        string path = System.IO.Path.GetFullPath(tok.File.Name);
                        Once[path] = "1";
                        var t = Lex.LexToken(limit_space: true);
                        if (t.Kind != Token.TokenKind.NewLine && t.Kind != Token.TokenKind.EoF) {
                            for (;;) {
                                t = Lex.LexToken(limit_space: true);
                                //t.Verbatim = true;
                                if (t.Kind == Token.TokenKind.NewLine || t.Kind == Token.TokenKind.EoF) { break; }
                            }
                        }

                        return nl;
                    }
                case "STDC": {
                        // 6.10.6 プラグマ指令
                        // （マクロ置き換えに先だって）pragma の直後の前処理字句が STDC である場合，この指令に対してマクロ置き換えは行わない。
                        // とあるので素通しする。
                        List<Token> tokens = new List<Token> { hash, tpragma };
                        tokens.Add(tok);
                        for (;;) {
                            Token t = Lex.LexToken(limit_space: true);
                            tokens.Add(t);
                            if (t.Kind == Token.TokenKind.NewLine || t.Kind == Token.TokenKind.EoF) { break; }
                        }
                        tokens.Skip(1).Reverse().ToList().ForEach(x => {
                            x.Verbatim = true;
                            Lex.unget_token(x);
                        });
                        return hash;
                    }
                default: {
                        if (CppContext.Warnings.Contains(Warning.UnknownPragmas)) {
                            // 不明なプラグマなので警告を出す。
                            CppContext.Warning(hash, $"#pragma {s} は不明なプラグマです。");
                        }
                        List<Token> tokens = new List<Token> { hash, tpragma };
                        tokens.Add(tok);
                        for (;;) {
                            Token t = Lex.LexToken(limit_space: true);
                            tokens.Add(t);
                            if (t.Kind == Token.TokenKind.NewLine || t.Kind == Token.TokenKind.EoF) { break; }
                        }
                        tokens.Skip(1).Reverse().ToList().ForEach(x => {
                            x.Verbatim = true;
                            Lex.unget_token(x);
                        });
                        return hash;
                    }
            }
        }

        /*
         * #lineディレクティブ
         */

        static bool IsDigitSequence(string str) {
            return str.All(p => CType.IsDigit(p));
        }

        /// <summary>
        /// #line linenum filename 形式
        /// </summary>
        static Token ParseLineDirective(Token hash, Token tline) {
            var nl = new Token(hash, Token.TokenKind.NewLine);

            Token tok = read_expand(limit_space: true);
            if (tok.Kind != Token.TokenKind.Number || !IsDigitSequence(tok.StrVal)) {
                if (tok.Kind == Token.TokenKind.NewLine || tok.Kind == Token.TokenKind.EoF) {
                    CppContext.Error(tok, "#line ディレクティブの行番号があるべき場所で行が終わっています。。");
                } else if (tok.Kind == Token.TokenKind.Number && !IsDigitSequence(tok.StrVal)) {
                    CppContext.Error(tok, $"#line ディレクティブの行番号の表記は10進数に限られています。");
                } else {
                    CppContext.Error(tok, $"#line ディレクティブで指定されている {Token.TokenToStr(tok)} は行番号ではありません。");
                }
                for (;;) {
                    Token t = Lex.LexToken(limit_space: true);
                    if (t.Kind == Token.TokenKind.NewLine || t.Kind == Token.TokenKind.EoF) {
                        break;
                    }
                }
                return nl;
            }
            long line = long.Parse(tok.StrVal);
            if (line <= 0 || 2147483647 < line ) {
                CppContext.Error(tok, $"#line ディレクティブの行番号に {tok.StrVal} が指定されていますが、 1 以上 2147483647 以下でなければなりません。");
                for (;;) {
                    Token t = Lex.LexToken(limit_space: true);
                    if (t.Kind == Token.TokenKind.NewLine || t.Kind == Token.TokenKind.EoF) {
                        break;
                    }
                }
                return nl;
            }
            tok = read_expand(limit_space: true);
            string filename = null;
            if (tok.Kind == Token.TokenKind.String) {
                filename = tok.StrVal;
                expect_newline(limit_space: true);
            } else if (tok.Kind != Token.TokenKind.NewLine) {
                if (tok.Kind == Token.TokenKind.NewLine || tok.Kind == Token.TokenKind.EoF) {
                    CppContext.Error(tok, "#line ディレクティブのファイル名があるべき場所で行が終わっています。。");
                } else {
                    CppContext.Error(tok, $"#line ディレクティブで指定されている {Token.TokenToStr(tok)} はファイル名ではありません。");
                }
                for (;;) {
                    Token t = Lex.LexToken(limit_space: true);
                    if (t.Kind == Token.TokenKind.NewLine || t.Kind == Token.TokenKind.EoF) {
                        break;
                    }
                }
                return nl;
            }

            /* 現在のファイルの位置情報を変更 */
            File.OverWriteCurrentPosition((int)line, filename);

            return nl;
        }

        /// <summary>
        /// GCCスタイル
        /// # linenum filename flags
        /// </summary>
        /// <param name="tok"></param>
        static Token ParseGccStyleLineDirective(Token hash, Token tok) {
            var nl = new Token(hash, Token.TokenKind.NewLine);
            if (tok.Kind != Token.TokenKind.Number || !IsDigitSequence(tok.StrVal)) {
                if (tok.Kind == Token.TokenKind.NewLine || tok.Kind == Token.TokenKind.EoF) {
                    CppContext.Error(tok, "#line ディレクティブの行番号があるべき場所で行が終わっています。。");
                } else if (tok.Kind == Token.TokenKind.Number && !IsDigitSequence(tok.StrVal)) {
                    CppContext.Error(tok, $"#line ディレクティブの行番号の表記は10進数に限られています。");
                } else {
                    CppContext.Error(tok, $"#line ディレクティブで指定されている {Token.TokenToStr(tok)} は行番号ではありません。");
                }
                for (;;) {
                    Token t = Lex.LexToken(limit_space: true);
                    if (t.Kind == Token.TokenKind.NewLine || t.Kind == Token.TokenKind.EoF) {
                        break;
                    }
                }
                return nl;
            } else { 
                long line = long.Parse(tok.StrVal);
                if (line <= 0 || 2147483647 < line) {
                    CppContext.Error(tok, $"#line ディレクティブの行番号に {tok.StrVal} が指定されていますが、 1 以上 2147483647 以下でなければなりません。");
                    for (;;) {
                        Token t = Lex.LexToken(limit_space: true);
                        if (t.Kind == Token.TokenKind.NewLine || t.Kind == Token.TokenKind.EoF) {
                            break;
                        }
                    }
                    return nl;
                }

                // ファイル名を読み取る
                tok = read_expand(limit_space: true);
                if (tok.Kind != Token.TokenKind.String) {
                    if (tok.Kind == Token.TokenKind.NewLine || tok.Kind == Token.TokenKind.EoF) {
                        CppContext.Error(tok, "gcc 形式の line ディレクティブのファイル名があるべき場所で行が終わっています。。");
                    } else {
                        CppContext.Error(tok, $"gcc 形式の line ディレクティブで指定されている {Token.TokenToStr(tok)} はファイル名ではありません。");
                    }
                    for (;;) {
                        Token t = Lex.LexToken(limit_space: true);
                        if (t.Kind == Token.TokenKind.NewLine || t.Kind == Token.TokenKind.EoF) {
                            break;
                        }
                    }
                    return nl;
                } else {
                    string filename = tok.StrVal;

                    // 残りのフラグなどは纏めて行末まで読み飛ばす
                    do {
                        tok = Lex.LexToken(limit_space: true);
                    } while (tok.Kind != Token.TokenKind.NewLine && tok.Kind != Token.TokenKind.EoF);

                    /* 現在のファイルの位置情報を変更 */
                    File.OverWriteCurrentPosition(line, filename);

                    return nl;
                }
            }
        }

        /*
         * 未知のディレクティブの処理
         */

        static Token UnsupportedPreprocessorDirective(Token hash, Token tok) {
            if (CppContext.Features.Contains(Feature.UnknownDirectives)) {
                if (CppContext.Warnings.Contains(Warning.UnknownDirectives)) {
                    CppContext.Warning(tok, $"{Token.TokenToStr(tok)} は未知のプリプロセッサディレクティブです。");
                }
                // 行末まで読み取って、マクロ展開禁止フラグを付けて押し戻す。
                List<Token> buf = new List<Token> { tok };
                for (;;) {
                    Token t = Lex.LexToken(limit_space: true);
                    buf.Add(t);
                    if (t.Kind == Token.TokenKind.NewLine || t.Kind == Token.TokenKind.EoF) {
                        break;
                    }
                }
                buf.ForEach(x => x.Verbatim = true);
                unget_all(buf);
                return hash;
            } else {
                CppContext.Warning(tok, $"{Token.TokenToStr(tok)} は未知のプリプロセッサディレクティブです。");
                for (;;) {
                    Token t = Lex.LexToken(limit_space: true);
                    if (t.Kind == Token.TokenKind.NewLine || t.Kind == Token.TokenKind.EoF) {
                        break;
                    }
                }
                return new Token(hash, Token.TokenKind.NewLine);
            }
        }

        /// <summary>
        /// プリプロセッサ指令の解析
        /// </summary>
        /// <param name="hash">'#'に対応するトークン</param>
        /// <returns>解析結果のトークン</returns>
        static Token ParseDirective(Token hash) {
            Token tok = Lex.LexToken(limit_space: true);

            switch (tok.Kind) {
                case Token.TokenKind.NewLine: {
                    // 行末の場合、6.10.7で定義されている空指令
                    return new Token(hash, Token.TokenKind.NewLine);
                }
                case Token.TokenKind.Number: {
                    // 数字の場合、GCC拡張形式の行番号設定指令
                    return ParseGccStyleLineDirective(hash, tok);
                }
                case Token.TokenKind.Ident: {
                    // 識別子はプリプロセッサ指令
                    switch (tok.StrVal) {
                        case "define": return read_define(hash, tok);
                        case "elif": return read_elif(hash, tok);
                        case "else": return read_else(hash, tok);
                        case "endif": return read_endif(hash, tok);
                        case "error": return read_error(hash, tok);
                        case "if": return read_if(hash, tok);
                        case "ifdef": return read_ifdef(hash, tok);
                        case "ifndef": return read_ifndef(hash, tok);
                        case "include": return ParseIncludeDirective(hash, tok);
                        case "line": return ParseLineDirective(hash, tok);
                        case "pragma": return ParsePragmaDirective(hash, tok);
                        case "undef": return read_undef(hash, tok);
                        case "warning": return read_warning(hash, tok); // 非標準動作
                    }
                    goto default;    // 未知のプリプロセッサとして処理
                }
                default: {
                    // それら以外は未知のプリプロセッサとして処理
                    return UnsupportedPreprocessorDirective(hash, tok);
                }
            }
        }

        /*
         * Special macros
         */

        public static class SpecialMacros {

            private static List<Token> handle_stdc_macro(Macro.BuildinMacro m, Token tmpl) {
                return new[] { new Token(tmpl, Token.TokenKind.Number) { StrVal = "1" }}.ToList();
            }
            private static List<Token> handle_date_macro(Macro.BuildinMacro m, Token tmpl) {
                return new[] { new Token(tmpl, Token.TokenKind.String) { StrVal = DateString } }.ToList();
            }

            private static List<Token> handle_time_macro(Macro.BuildinMacro m, Token tmpl) {
                return new[] { new Token(tmpl, Token.TokenKind.String) { StrVal = TimeString } }.ToList();
            }

            private static List<Token> handle_file_macro(Macro.BuildinMacro m, Token tmpl) {
                return new[] { new Token(tmpl, Token.TokenKind.String) { StrVal = tmpl.File.Name } }.ToList();
            }

            private static List<Token> handle_line_macro(Macro.BuildinMacro m, Token tmpl) {
                return new[] { new Token(tmpl, Token.TokenKind.Number) { StrVal = $"{tmpl.File.Line}"} }.ToList();
            }


            public static void Install() {
                define_special_macro("__STDC__", handle_stdc_macro);
                define_special_macro("__DATE__", handle_date_macro);
                define_special_macro("__TIME__", handle_time_macro);
                define_special_macro("__FILE__", handle_file_macro);
                define_special_macro("__LINE__", handle_line_macro);
            }
        }


        /*
         * Initializer
         */

        public static void add_include_path(string path) {
            StdIncludePath.Add(path);
        }

        public static void add_user_include_path(string path) {
            UserIncludePath.Add(path);
        }

        //static void define_obj_macro(string name, Token value, bool used = false) {
        //    Macros[name] = new Macro.ObjectMacro(new Token() { Kind = Token.TokenKind.Ident, StrVal = name }, new List<Token> { value }) { Used = used };
        //    DefinedMacros.Add(Macros[name]);
        //}

        public static void define_special_macro(string name, Macro.BuildinMacro.BuiltinMacroHandler fn) {
            if (name.Length > 63) {
                CppContext.Warning($"組込みマクロ名 `{name}` は63文字を超えており、ISO/IEC 9899 5.2.4.1 翻訳限界 の制約に抵触しています。");
            }
            Macros[name] = new Macro.BuildinMacro(name, fn);
            DefinedMacros.Add(Macros[name]);
            Macros[name].Used = true;
        }

        static void init_keywords() {
            Keywords["->"] = Token.Keyword.Arrow;
            Keywords["+="] = Token.Keyword.AssignAdd;
            Keywords["&="] = Token.Keyword.AssignAnd;
            Keywords["/="] = Token.Keyword.AssignDiv;
            Keywords["%="] = Token.Keyword.AssignMod;
            Keywords["*="] = Token.Keyword.AssignMul;
            Keywords["|="] = Token.Keyword.AssignOr;
            Keywords["<<="] = Token.Keyword.AssignShiftArithLeft;
            Keywords[">>="] = Token.Keyword.AssignShiftArithRight;
            Keywords["-="] = Token.Keyword.AssignSub;
            Keywords["^="] = Token.Keyword.AssignXor;
            Keywords["--"] = Token.Keyword.Dec;
            Keywords["=="] = Token.Keyword.Equal;
            Keywords[">="] = Token.Keyword.GreatEqual;
            Keywords["++"] = Token.Keyword.Inc;
            Keywords["<="] = Token.Keyword.LessEqual;
            Keywords["&&"] = Token.Keyword.LogicalAnd;
            Keywords["||"] = Token.Keyword.LogincalOr;
            Keywords["!="] = Token.Keyword.NotEqual;
            Keywords["<<"] = Token.Keyword.ShiftArithLeft;
            Keywords[">>"] = Token.Keyword.ShiftArithRight;
            Keywords["##"] = Token.Keyword.HashHash;
        }

        /// <summary>
        /// 事前定義マクロの設定
        /// </summary>
        static void init_predefined_macros() {
            // 組み込みマクロもインストールする
            SpecialMacros.Install();
        }

        public static void Init() {
            init_keywords();
            init_predefined_macros();
        }

        /// <summary>
        /// トークンが予約語かどうかチェックし、予約後なら変換する
        /// </summary>
        /// <param name="tok"></param>
        /// <returns></returns>
        private static Token maybe_convert_keyword(Token tok) {
            if (tok.Kind != Token.TokenKind.Ident) {
                return tok;
            }
            if (Keywords.ContainsKey(tok.StrVal) == false) {
                return tok;
            }
            var ret = new Token(tok, Token.TokenKind.Keyword) {
                KeywordVal = Keywords[tok.StrVal]
            };

            return ret;
        }

        private static Token PeekToken() {
            Token r = ReadToken();
            Lex.unget_token(r);
            return r;
        }

        /// <summary>
        /// プリプロセス処理を行って得られたトークンを一つ読み取る
        /// </summary>
        /// <returns></returns>
        public static Token ReadToken() {
            for (;;) {
                var tok = read_expand();

                if (tok.BeginOfLine && tok.IsKeyword('#') && tok.Hideset == null) {
                    //プリプロセッサ指令を処理する。出力は空白行になる。
                    return ParseDirective(tok);
                }
                if (tok.Kind < Token.TokenKind.MinCppToken && tok.Verbatim == false) {
                    // 読み取ったものがプリプロセッサキーワードになり得るものかつ、verbatimでないなら
                    // プリプロセス処理を試みる
                    return maybe_convert_keyword(tok);
                } else {
                    // キーワードになり得ないものならそのまま
                    return tok;
                }
            }
        }


        public static IEnumerable<Macro> EnumUnusedMacro() {
            return DefinedMacros.Where(x => x.Used == false && !Cpp.RefMacros.Contains(x.GetName()));
        }
    }

}
