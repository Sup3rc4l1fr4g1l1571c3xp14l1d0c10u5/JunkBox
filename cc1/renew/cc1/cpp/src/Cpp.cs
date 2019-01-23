using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;

namespace CSCPP {
    public static partial class Cpp {
        /// <summary>
        /// オリジナルの起動時引数
        /// </summary>
        public static string[] OriginalArguments = new string[0];

        /// <summary>
        /// プリプロセス対象ファイル
        /// </summary>
        public static string TargetFilePath { get; set; } = "-";

        /// <summary>
        /// 出力ファイル
        /// </summary>
        public static string OutputFilePath { get; set; } = null;

        /// <summary>
        /// ログファイル
        /// </summary>
        public static string LogFilePath { get; set; } = null;

        /// <summary>
        /// 警告フラグ
        /// </summary>
        public static HashSet<WarningOption> Warnings { get; } = new HashSet<WarningOption>();

        /// <summary>
        /// 機能フラグ
        /// </summary>
        public static HashSet<FeatureOption> Features { get; } = new HashSet<FeatureOption>();

        /// <summary>
        /// 冗長出力フラグ
        /// </summary>
        public static HashSet<VerboseOption> Verboses { get; } = new HashSet<VerboseOption>();

        /// <summary>
        /// オプションスイッチ
        /// </summary>
        public static HashSet<string> Switchs { get; } = new HashSet<string>();

        /// <summary>
        /// レポートフラグ
        /// </summary>
        public static HashSet<Report> Reports { get; } = new HashSet<Report>();

        /// <summary>
        /// 出力器(マクロ展開位置の正確な取得に使用)
        /// </summary>
        public static TokenWriter TokenWriter { get; set; } = null;

        /// <summary>
        /// マクロ展開のログを記録
        /// </summary>
        public static MacroExpandLog ExpandLog { get; } = new MacroExpandLog();

        /// <summary>
        /// プリプロセスエラー数
        /// </summary>
        public static int ErrorCount { get; private set; }

        /// <summary>
        /// プリプロセス警告数
        /// </summary>
        public static int WarningCount { get; private set; }

        /// <summary>
        /// デフォルト文字コード
        /// </summary>
        public static System.Text.Encoding DefaultEncoding { get; set; } = System.Text.Encoding.Default;

        /// <summary>
        /// 文字コード自動認識の有効/無効
        /// </summary>
        public static bool AutoDetectEncoding { get; set; }

        /// <summary>
        /// インクルードパス（絶対パスを格納）
        /// </summary>
        static List<string> UserIncludePaths { get; } = new List<string>();

        /// <summary>
        /// システムインクルードパス（絶対パスを格納）
        /// </summary>
        static List<string> SystemIncludePaths { get; } = new List<string>();

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

        /// <summary>
        /// 予約語表
        /// </summary>
        static Dictionary<string, Token.Keyword> Keywords { get; } = new Dictionary<string, Token.Keyword>();

        /// <summary>
        /// インクルードガードが記述されていると思われるファイル
        /// </summary>
        static Dictionary<string, string> IncludeGuards { get; } = new Dictionary<string, string>();

        /// <summary>
        /// #if/#ifdef/#else/#elifによる条件分岐状態スタック
        /// </summary>
        static Stack<Condition> ConditionStack { get; } = new Stack<Condition>();

        /// <summary>
        /// (CertC PRE08-Cのために追加)
        /// 一度でもインクルードしたことがあるファイルとインクルード位置を示す表
        /// </summary>
        static Dictionary<string, List<Tuple<string, string, Position>>> PRE08IncludeFileTable = new Dictionary<string, List<Tuple<string, string, Position>>>();

        /// <summary>
        /// __DATE__ で得られる文字列
        /// </summary>
        public static string DateString { get; }

        /// <summary>
        /// __TIME__ で得られる文字列
        /// </summary>
        public static string TimeString { get; }

        /// <summary>
        /// 指令行中の解析を示す状態カウンタ。
        /// </summary>
        private static int InDirectiveLine { get; set; }

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

        /// <summary>
        /// エラーメッセージ出力
        /// </summary>
        /// <param name="caption">エラーの見出し</param>
        /// <param name="pos">エラー発生位置</param>
        /// <param name="message">エラーメッセージ</param>
        private static void OutputError(Position pos, string caption, string message) {
            Console.Error.Write(pos.ToString());
            Console.Error.WriteLine($" : ** {caption} ** : {message}");
        }

        /// <summary>
        /// 内部エラーメッセージ出力
        /// </summary>
        /// <param name="tok"></param>
        /// <param name="message"></param>
        public static void InternalError(Token tok, string message) {
            Debug.Assert(tok != null);
            OutputError(tok.Position, "INTERNAL-ERROR", message);
            ErrorCount++;
        }

        /// <summary>
        /// エラーメッセージ出力
        /// </summary>
        /// <param name="message"></param>
        public static void Error(string message) {
            OutputError(Position.Empty, "ERROR", message);
            ErrorCount++;
        }

        /// <summary>
        /// エラーメッセージ出力
        /// </summary>
        /// <param name="tok"></param>
        /// <param name="message"></param>
        public static void Error(Token tok, string message) {
            Debug.Assert(tok != null);
            Error(tok.Position, message);
        }

        /// <summary>
        /// エラーメッセージ出力
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="message"></param>
        public static void Error(Position pos, string message) {
            OutputError(pos, "ERROR", message);
            ErrorCount++;
        }

        /// <summary>
        /// 警告メッセージ出力
        /// </summary>
        /// <param name="tok"></param>
        /// <param name="message"></param>
        public static void Warning(Token tok, string message) {
            Debug.Assert(tok != null);
            if (Warnings.Contains(WarningOption.Error)) {
                Error(tok, message);
            } else {
                OutputError(tok.Position, "WARNING", message);
                WarningCount++;
            }
        }

        /// <summary>
        /// 警告メッセージ出力
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="message"></param>
        public static void Warning(Position pos, string message) {
            if (Warnings.Contains(WarningOption.Error)) {
                Error(pos, message);
            } else {
                OutputError(pos, "WARNING", message);
                WarningCount++;
            }
        }

        /// <summary>
        /// 警告メッセージ出力
        /// </summary>
        /// <param name="message"></param>
        public static void Warning(string message) {
            if (Warnings.Contains(WarningOption.Error)) {
                Error(message);
            } else {
                OutputError(Position.Empty, "WARNING", message);
                WarningCount++;
            }
        }

        /// <summary>
        /// メッセージ出力
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="message"></param>
        public static void Notify(Position pos, string message) {
            OutputError(pos, "NOTIFY", message);
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
        /// トークン列の先頭要素の space を tmpl のものに設定する。
        /// ただし、tmplがマクロ引数の場合は空白を伝搬させない。
        /// </summary>
        /// <param name="tokens"></param>
        /// <param name="tmpl"></param>
        static void PropagateSpace(List<Token> tokens, Token tmpl) {
            if (tokens.Count != 0 && tmpl.Kind != Token.TokenKind.MacroParam) {
                tokens.First().Space = tmpl.Space;
            }
        }

        /// <summary>
        /// 行末まで読み飛ばす
        /// </summary>
        /// <param name="token">先読みしていたトークンがある場合は指定する</param>
        /// <param name="skippedTokens">行末までに読み飛ばされたトークンの格納が必要な場合は指定する</param>
        /// <returns>改行もしくはEoFトークン</returns>
        static Token SkipToNextLine(Token token = null, List<Token> skippedTokens = null) {
            if (token == null) {
                token = Lex.LexToken(limitSpace: true);
                skippedTokens?.Add(token);
            }
            while (token.Kind != Token.TokenKind.NewLine && token.Kind != Token.TokenKind.EoF) {
                skippedTokens?.Add(token);
                token = Lex.LexToken(limitSpace: true);
            }
            return token;
        }

        /// <summary>
        /// 改行トークンを一つ読み取る。
        /// </summary>
        /// <param name="limitSpace">前処理指令中に</param>
        public static void ExpectNewLine(bool limitSpace = false) {
            Token tok = Lex.LexToken(limitSpace: limitSpace);
            if (tok.Kind != Token.TokenKind.NewLine) {
                if (tok.Kind == Token.TokenKind.EoF) {
                    Error(tok, $"改行があるべき場所にファイル終端がありました。");
                } else {
                    Error(tok, $"改行があるべき場所に {Token.TokenToStr(tok)} がありました。");
                    tok = SkipToNextLine(tok);
                    if (tok.Kind == Token.TokenKind.EoF) {
                        Lex.UngetToken(tok);
                    }
                }
            }
        }

        /// <summary>
        /// 関数形式マクロの呼び出し式の引数一つに対応するトークン列を読み取る
        /// </summary>
        /// <param name="macro"></param>
        /// <param name="end"></param>
        /// <param name="readall"></param>
        /// <param name="limitSpace"></param>
        /// <returns></returns>
        static List<Token> ReadOneArg(Macro.FuncMacro macro, out bool end, bool readall, bool limitSpace = false) {
            List<Token> r = new List<Token>();
            int level = 0;
            for (; ; ) {
                Token tok = Lex.LexToken(handleEof: true, limitSpace: limitSpace);
                if (tok.Kind == Token.TokenKind.EoF) {
                    Error(tok, $"関数形式マクロ {macro.Name.StrVal} の呼び出しの引数リストの始め丸括弧 `(` に対応する終わり丸括弧 `)` がありません。");
                    end = true;
                    Lex.UngetToken(tok);
                    return null;
                }
                if (level == 0 && tok.IsKeyword(')')) {
                    Lex.UngetToken(tok);
                    end = true;
                    return r;
                }
                if (tok.Kind == Token.TokenKind.NewLine) {
                    if (InDirectiveLine > 0) {
                        Error(tok, $"関数形式マクロ {macro.Name.StrVal} の呼び出しの引数リストの始め丸括弧 `(` に対応する終わり丸括弧 `)` がありません。");
                        end = true;
                        Lex.UngetToken(tok);
                        return null;
                    } else {
                        continue;
                    }
                }
                if (tok.BeginOfLine && tok.IsKeyword('#')) {
                    if (Warnings.Contains(WarningOption.CertCCodingStandard)) {
                        // PRE32-C. 関数形式マクロの呼出しのなかで前処理指令を使用しない
                        Warning(tok, $@"[PRE32-C] 関数形式マクロ {macro.Name.StrVal} の呼び出しのなかで前処理指令が使用されていますが、C90以降のC言語規格では、このようなコードは未定義の動作となることが示されています(参考文献:[ISO/IEC 9899:2011] 6.10.3 P11)。このルールについては、MISRA-C:1998 95、および、MISRA-C:2004 19.9 でも触れられています。");
                    }
                    ParseDirective(tok);
                    continue;
                }
                if (level == 0 && tok.IsKeyword(',') && !readall) {
                    end = false;
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

        /// <summary>
        /// 関数形式マクロの呼び出し式の引数列を読み取る
        /// </summary>
        /// <param name="macro"></param>
        /// <param name="limitSpace"></param>
        /// <returns></returns>
        static List<List<Token>> DoReadArgs(Macro.FuncMacro macro, bool limitSpace = false) {
            List<List<Token>> r = new List<List<Token>>();
            bool end = false;
            while (!end) {
                bool inEllipsis = (macro.IsVarg && r.Count + 1 == macro.Args.Count);
                var ret = ReadOneArg(macro, out end, inEllipsis, limitSpace: limitSpace);
                if (ret == null) {
                    return null;
                }
                r.Add(ret);
            }
            if (macro.IsVarg && r.Count == macro.Args.Count - 1) {
                r.Add(new List<Token>());
            }
            return r;
        }

        static List<List<Token>> read_args(Token tok, Macro.FuncMacro macro, bool limitSpace = false) {
            if (macro.Args.Count == 0 && PeekToken().IsKeyword(')')) {
                // If a macro M has no parameter, argument list of M()
                // is an empty list. If it has one parameter,
                // argument list of M() is a list containing an empty list.
                return new List<List<Token>>();
            }
            List<List<Token>> args = DoReadArgs(macro, limitSpace: limitSpace);
            if (args == null) {
                return null;
            }
            if (args.Count != macro.Args.Count) {
                Error(tok, $"関数形式マクロ {macro.Name.StrVal} の実引数の数が一致しません。" +
                                      $"宣言時の仮引数は {macro.Args.Count} 個ですが、指定されている実引数は {args.Count} 個です。");
                // エラー回復は呼び出し元に任せる
                return null;
            }

            /* C99から利用可能となった空の実引数をチェック */
            if (!Features.Contains(FeatureOption.EmptyMacroArgument)) {
                for (var i = 0; i < args.Count; i++) {
                    if (args[i].Count == 0) {
                        Error(tok, $"関数形式マクロ {macro.Name.StrVal} の呼び出しの第 {i + 1} 実引数が空です。" +
                                               "空の実引数は ISO/IEC 9899-1999 以降で利用可能となった言語機能です。" +
                                               "空の実引数を有効にする場合は実行時引数に -FEmptyMacroArgument を設定してください。");
                    }
                }
                // プリプロセッサ内部では空の実引数に対応しているので、エラーは出力するがそのまま処理を進める。
            } else {
                if (Warnings.Contains(WarningOption.EmptyMacroArgument)) {
                    for (var i = 0; i < args.Count; i++) {
                        if (args[i].Count == 0) {
                            Warning(tok, $"関数形式マクロ {macro.Name.StrVal} の呼び出しの第 {i + 1} 実引数が空です。");
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
        static void GluePush(Macro m, List<Token> tokens, Token tok) {
            var lasttoken = tokens.Pop();

            // トークン列の末尾要素とトークンtokを文字列として結合してからトークンの読み出しを行う
            var str = Token.TokenToStr(lasttoken) + Token.TokenToStr(tok);
            var newtoken = Lex.LexPastedString(m, lasttoken.Position, str);
            PropagateSpace(newtoken, lasttoken);
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
        static List<Token> ExpandAll(List<Token> inputTokens, Token tmpl) {
            Lex.TokenBufferStash(inputTokens.Reverse<Token>().ToList());
            List<Token> r = new List<Token>();
            for (Token tok = ReadExpand(); tok.Kind != Token.TokenKind.EoF; tok = ReadExpand()) {
                r.Add(tok);
            }

            PropagateSpace(r, tmpl);

            Lex.TokenBufferUnstash();
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

                if (args != null && t0 != null && t0.IsKeyword('#')) {

                    // 6.10.3.2 #演算子 に
                    // 制約  関数形式マクロの置換要素並びの中にある各#前処理字句の次の前処理字句は，仮引数でなければならない
                    // とあるので、関数形式マクロの時だけ '#' を特別視する
                    if (t1Param) {
                        // (t0,t1) = ('#', <マクロ引数1>) の場合
                        // マクロ引数1を構成するトークン列を文字列としたものを結果に挿入
                        var newTok = Stringize(t0, args[t1.ArgIndex]);
                        if (r.Count > 0) {
                            r.Last().TailSpace = false;
                        }
                        r.Add(newTok);
                        i++;
                    } else {
                        Error(t0, "`#` 演算子の次はマクロ仮引数名でなければなりません。");
                        r.Add(t0);
                    }
                } else if (t0 != null && t0.IsKeyword(Token.Keyword.HashHash) && t1Param) {
                    // (t0,t1) = ('##', <マクロ引数1>) の場合

                    List<Token> arg = args[t1.ArgIndex];
                    if (Features.Contains(FeatureOption.ExtensionForVariadicMacro) && t1.IsVarArg && r.Count > 0 && r.Last().IsKeyword(',')) {
                        /* GCC系は ,##__VA_ARGS__ と書かれた関数形式マクロの本体を展開する際に、 __VA_ARGS__ 部分のトークン列が空の場合はコンマを出力しない動作を行う
                         * #define func(x, ...) func2(x,##__VA_ARGS__) の場合
                         *   func(A, 1) は func2(A, 1)になるが
                         *   func(A   ) は func2(A   )にならないといけない
                         */
                        if (Warnings.Contains(WarningOption.ExtensionForVariadicMacro)) {
                            Warning(t0, $"関数形式マクロ {m.GetName()} の可変長マクロ引数の展開でISO/IEC 9899-1999 では定義されていない非標準の拡張 `,##__VA_ARGS__` が使われています。");
                        }
                        if (arg.Count > 0) {
                            //可変長マクロ引数があるのでそのまま出力
                            r.AddRange(arg);
                        } else {
                            //可変長マクロ引数が空なの逆に末尾のコンマを取り除く
                            r.Pop();
                        }
                    } else if (arg.Count > 0) {
                        // 引数の最初のトークンを出力末尾のトークンと連結し、
                        // 引数の残りのトークンは出力の末尾に追加する
                        GluePush(m, r, arg.First());
                        for (int j = 1; j < arg.Count; j++) {
                            r.Add(arg[j]);
                        }
                    }
                    i++;
                } else if (t0 != null && t0.IsKeyword(Token.Keyword.HashHash) && t1 != null) {
                    // 最初のトークンが## でその後ろに別のトークンがある（つまり連結演算子式の先頭以外の)場合 
                    hideset = t1.Hideset;
                    // トークンの連結処理を行う
                    GluePush(m, r, t1);

                    i++;
                } else if (t0Param && t1 != null && t1.IsKeyword(Token.Keyword.HashHash)) {
                    // 最初のトークンがマクロ引数で その後ろに##がある（つまり連結演算子式の先頭の)場合 
                    hideset = t1.Hideset;
                    //マクロ引数列を展開せずにトークン列に追加する
                    List<Token> arg = args[t0.ArgIndex];
                    if (arg.Count == 0) {
                        i++;
                    } else {
                        r.AddRange(arg);
                    }
                } else if (t0Param) {
                    // 最初のトークン t0 がマクロ仮引数名の場合、関数型マクロの実引数 args からt0に対応する引数を取得
                    List<Token> arg = args[t0.ArgIndex];

                    // 引き数列を展開してトークン列に追加
                    var newtokens = ExpandAll(arg, t0);
                    if (newtokens.Count > 0) {
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
        static void UngetAll(List<Token> tokens) {
            for (int i = tokens.Count - 1; i >= 0; i--) {
                Lex.UngetToken(tokens[i]);
            }
        }



        /// <summary>
        /// Dave Prosser's C Preprocessing Algorithm (http://www.spinellis.gr/blog/20060626/)
        /// におけるexpandに対応するトークン読み取り処理
        /// </summary>
        /// <returns></returns>
        static Token ReadExpand(bool limit_space = false) {
            for (; ; ) {
                // トークンを一つ読み取る
                Token tok = Lex.LexToken(limitSpace: limit_space);

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

                    // マクロ展開を記録
                    if (Verboses.Contains(VerboseOption.TraceMacroExpand)) {
                        var rawPos = TokenWriter.WriteDummy(tok);
                        expandedTokens.Add(Token.make_MacroRangeFixup(new Position("dummy", -1, -1), tok, macro, (int)rawPos.Line, rawPos.Column));
                    }

                } else if (macro is Macro.FuncMacro) {
                    // 関数形式マクロの場合

                    Macro.FuncMacro m = macro as Macro.FuncMacro;

                    /* 6.10.3: 関数形式マクロの呼出しを構成する前処理字句列の中では，改行は普通の空白類文字とみなす。
                     * を考慮してマクロ名と括弧の間に改行がある場合は先読みを行う。
                     * そうしないと6.10.3.5の例3で異なった結果が得られる 
                     */
                    List<Token> lookAHeads = new List<Token>();
                    for (; ; ) {
                        var tok2 = Lex.LexToken(limitSpace: limit_space);
                        if (tok2.IsKeyword('(')) {
                            // マクロ関数呼び出しである
                            break;
                        } else if (tok2.Kind == Token.TokenKind.NewLine) {
                            // 改行は読み飛ばす
                            lookAHeads.Add(tok2);
                            continue;
                        } else {
                            // マクロ関数呼び出しではない
                            Lex.UngetToken(tok2);
                            UngetAll(lookAHeads);
                            return tok;
                        }
                    }

                    // マクロ関数呼び出しの実引数を読み取る
                    var args = read_args(tok, m, limitSpace: limit_space);
                    if (args == null) {
                        // マクロ引数読み取りにエラーがあった場合は')'もしくは末尾まで読み飛ばし展開も行わない。
                        for (; ; ) {
                            var tok2 = Lex.LexToken(limitSpace: limit_space);
                            if (tok2.IsKeyword(')')) {
                                break;
                            }
                            if (tok2.Kind == Token.TokenKind.NewLine || tok2.Kind == Token.TokenKind.EoF) {
                                Lex.UngetToken(tok2);
                                break;
                            }
                        }
                        return Token.make_invalid(tok.Position, "マクロ呼び出しの実引数中に誤り");
                    } else {

                        if (args.Count > 127) {
                            Warning($"関数形式マクロ `{name}` のマクロ呼出しにおける実引数の個数が127個を超えており、ISO/IEC 9899 5.2.4.1 翻訳限界 の制約に抵触しています。");
                        }

                        var rparen = Lex.ExceptKeyword(')');

                        // マクロ再展開禁止用のhidesetをマクロ関数名と引数の展開結果の両方に出現するものから作って追加
                        Set hideset = tok.Hideset.Intersect(rparen.Hideset).Add(name);

                        // 実際のマクロ展開を行う
                        expandedTokens = Subst(m, m.Body, args, hideset);
                    }

                } else if (macro is Macro.BuildinMacro) {
                    // 組み込みマクロの場合、マクロオブジェクトの処理関数に投げる
                    Macro.BuildinMacro m = macro as Macro.BuildinMacro;
                    expandedTokens = m.Hander(m, tok);
                } else {
                    InternalError(tok, $"マクロの型が不正です。(macro.Type={macro.GetType().FullName})");
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
                    x.Position = tok.Position;
                });

                // 最初のトークン以外の空白位置も補正
                expandedTokens.Skip(1).ToList().ForEach(x => {
                    var prevSpace = x.Space;
                    x.Space = new SpaceInfo();
                    x.Space.Chunks.AddRange(prevSpace.Chunks.Select(y => new SpaceInfo.Chunk() { Pos = tok.Position, Space = y.Space }));
                });

                if (expandedTokens.Count > 0) {
                    // マクロ列の前後に行頭情報や仮想的な空白を挿入
                    expandedTokens.First().BeginOfLine = tok.BeginOfLine;
                    expandedTokens.First().HeadSpace = true;
                    expandedTokens.Last().TailSpace = true;
                } else {
                    // マクロ列がない場合は次のトークンの頭に仮想的な空白を挿入
                    PeekToken().HeadSpace = true;
                }

                // 構築した展開後のトークンをバッファに戻す。
                UngetAll(expandedTokens);

                // 再度展開処理を行う
                continue;
            }
        }

        /// <summary>
        /// 関数形式マクロ m の仮引数宣言列を読み取る
        /// </summary>
        /// <param name="m">関数形式マクロ</param>
        /// <param name="params">仮引数宣言の解析情報が格納される</param>
        /// <param name="lastTok">最後に読み取ったトークン</param>
        /// <returns></returns>
        static bool ReadFunctionLikeMacroParams(Macro m, List<Tuple<string, Token>> @params, out Token lastTok) {
            int index = 0;
            Token prevTok = null;
            for (; ; ) {
                Token tok = Lex.LexToken();

                if (tok.IsKeyword(')')) {
                    // 閉じ括弧があるなら終了
                    lastTok = tok;
                    return false;
                }

                if (index != 0) {
                    // 1要素目以降は区切りコンマがあるなら読み飛ばす
                    if (tok.IsKeyword(',')) {
                        tok = Lex.LexToken();
                    } else {
                        // コンマが無い
                        Error(tok, $"関数形式マクロ {m.GetName()} の宣言で仮引数を区切るコンマがあるべき場所に {Token.TokenToStr(tok)} がありました。");
                    }
                }

                // 行末が出現した場合
                if (tok.Kind == Token.TokenKind.NewLine) {
                    Error(m.GetFirstPosition(), $"関数形式マクロ {m.GetName()} の宣言で仮引数宣言の括弧が閉じていません。");
                    Lex.UngetToken(tok);
                    lastTok = prevTok;
                    return false;
                }

                // "..."の場合はISO C 可変個引数マクロとして処理
                if (tok.IsKeyword(Token.Keyword.Ellipsis)) {
                    if (Features.Contains(FeatureOption.VariadicMacro)) {
                        if (Warnings.Contains(WarningOption.VariadicMacro)) {
                            Warning(tok, $"関数形式マクロ {m.GetName()} は可変個引数マクロとして宣言されています。");
                        }
                        var tokVaArgs = Token.MakeMacroToken(index, true, "__VA_ARGS__", tok.File);
                        @params.Add(Tuple.Create("__VA_ARGS__", tokVaArgs));
                        lastTok = Lex.ExceptKeyword(')');
                        return true;
                    } else {
                        Error(tok, $"関数形式マクロ {m.GetName()} は可変個引数マクロとして宣言されていますが、可変個引数マクロは ISO/IEC 9899-1999 以降で利用可能となった言語機能です。可変個引数マクロを有効にする場合は実行時引数に -FVariadicMacro を設定してください。");
                    }
                }

                if (tok.Kind != Token.TokenKind.Ident) {
                    Error(tok, $"関数形式マクロ {m.GetName()} の宣言で仮引数(識別子)があるべき場所に {Token.TokenToStr(tok)} がありました。");
                    Lex.UngetToken(tok);
                    lastTok = prevTok;
                    return false;
                }

                string arg = tok.StrVal;
                if (Lex.NextKeyword(Token.Keyword.Ellipsis)) {
                    if (Features.Contains(FeatureOption.VariadicMacro)) {
                        // gcc拡張の可変長引数 args... みたいな書式への対応
                        if (Features.Contains(FeatureOption.ExtensionForVariadicMacro)) {
                            lastTok = Lex.ExceptKeyword(')');
                            var tokArg = Token.MakeMacroToken(index, true, arg, tok.File);
                            @params.Add(Tuple.Create(arg, tokArg));
                            return true;
                        } else {
                            Error(tok, $"関数形式マクロ {m.GetName()} はgcc拡張を伴う可変個引数マクロとして宣言されています。gcc拡張を伴う可変個引数マクロを有効にする場合は実行時引数に -FExtensionForVariadicMacro を設定してください。");
                        }
                    } else {
                        Error(tok, $"関数形式マクロ {m.GetName()} はgcc拡張を伴う可変個引数マクロとして宣言されています。gcc拡張を伴う可変個引数マクロを有効にする場合は実行時引数に -FVariadicMacro と -FExtensionForVariadicMacro を設定してください。");
                    }
                } else {
                    // 普通の仮引数
                    if (@params.Any(x => x.Item1 == arg)) {
                        Error(tok, $"関数形式マクロ {m.GetName()} の宣言で仮引数 {arg} が複数回定義されています。");
                    }
                    var tokArg = Token.MakeMacroToken(index++, false, arg, tok.File);
                    @params.Add(Tuple.Create(arg, tokArg));
                }
                prevTok = tok;
            }
        }

        static void HashHashCheck(List<Token> v) {
            if (v.Count == 0) { return; }
            if (v.First().IsKeyword(Token.Keyword.HashHash)) {
                var tok = v.First();
                Error(tok, "'##' はマクロ展開部の先頭では使えません。");
                v.Insert(0, new Token(tok, Token.TokenKind.Ident) { KeywordVal = (Token.Keyword)' ' });
            }
            if (v.Last().IsKeyword(Token.Keyword.HashHash)) {
                var tok = v.Last();
                Error(tok, "'##' はマクロ展開部の末尾では使えません。");
                v.Add(new Token(tok, Token.TokenKind.Ident) { KeywordVal = (Token.Keyword)' ' });
            }
        }

        static List<Token> read_funclike_macro_body(Macro m, List<Tuple<string, Token>> param) {
            List<Token> r = new List<Token>();
            for (; ; ) {
                Token tok = Lex.LexToken();
                if (tok.Kind == Token.TokenKind.NewLine) {
                    break;
                }
                tok.Space.Chunks.ForEach(x => x.Space = System.Text.RegularExpressions.Regex.Replace(x.Space, "\r?\n", ""));
                if (tok.Kind == Token.TokenKind.Ident) {
                    var t = param.Find(x => x.Item1 == tok.StrVal);
                    if (t != null) {
                        var t2 = new Token(t.Item2, Token.TokenKind.MacroParamRef) { Space = tok.Space, MacroParamRef = t.Item2 };
                        r.Add(t2);
                        continue;
                    }
                }
                r.Add(tok);
            }
            // 
            for (var i = 0; i < r.Count; i++) {
                if (r[i].IsKeyword('#')) {
                    var next = r.ElementAtOrDefault(i + 1);
                    if (next == null || (next.Kind != Token.TokenKind.MacroParamRef)) {
                        Error(r[i], "前処理演算子 `#` の後ろにマクロ引数がありません。");
                    }
                }
            }
            if (Warnings.Contains(WarningOption.UnspecifiedBehavior)) {
                // ISO/IEC 9899-1999 6.10.3.2 意味規則: #演算子及び##演算子の評価順序は，未規定とする。 のチェック
                if (r.Count(x => x.IsKeyword('#') || x.IsKeyword(Token.Keyword.HashHash)) > 1) {
                    Warning(param.First().Item2, "マクロ定義で `#`演算子 または `##`演算子 が複数回用いていますが、これらの評価順序は未規定です。(参考文献:[ISO/IEC 9899:2011] 6.10.3.2、および、MISRA-C:2004 ルール19.12)。");
                }
            }
            return r;
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

        static void read_funclike_macro(Token name) {
            List<Tuple<string, Token>> param = new List<Tuple<string, Token>>();
            Macro.FuncMacro macro = new Macro.FuncMacro(name, null, null, false);
            Token lastToken;
            bool isVarg = ReadFunctionLikeMacroParams(macro, param, out lastToken);

            if (param.Count > 127) {
                Warning($"関数形式マクロ `{name}` の定義において仮引数が {param.Count} 個定義されています。ISO/IEC 9899 5.2.4.1 翻訳限界 で規定されている関数形式マクロの仮引数は 127 個ですので処理系依存の動作となります。");
            }

            List<Token> body = read_funclike_macro_body(macro, param);
            macro.LastToken = (body.Count > 0) ? body.Last() : lastToken;

            if (Warnings.Contains(WarningOption.CertCCodingStandard)) {

                // PRE01-C: マクロ内の引数名は括弧で囲む
                foreach (var b in body.Select((x, i) => Tuple.Create(x, i)).Where(x => x.Item1.Kind == Token.TokenKind.MacroParamRef)) {
                    var index = b.Item2;
                    if ((index > 0) && (index + 1 < body.Count)) {
                        if (body[index - 1].IsKeyword('(') && body[index + 1].IsKeyword(')')) {
                            // ok
                            continue;
                        }
                        Warning(b.Item1, $"[PRE01-C] 関数形式マクロ {macro.GetName()} 内の引数 {b.Item1.ArgName} が括弧で囲まれていません。");
                    }
                }

                // PRE02-C: マクロ置換リストは括弧で囲む
                do {
                    if (body.Count > 0) {
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
                        var locate = macro.GetFirstPosition();
                        Warning(locate, $"[PRE02-C] 関数形式マクロ {macro.GetName()} の置換リストが括弧で囲まれていません。");
                    }
                } while (false);

                // PRE11-C: マクロ定義をセミコロンで終端しない
                do {
                    if (body.Count > 0) {
                        var last = body.Last();
                        if (last.IsKeyword(';')) {
                            var locate = macro.GetFirstPosition();
                            Warning(locate, $"[PRE11-C] 関数形式マクロ {macro.GetName()} の置換リストがセミコロンで終端されています。");
                        }
                    }
                } while (false);
            }

            HashHashCheck(body);
            macro.Args = param.Select(x => x.Item2).ToList();
            macro.Body = body;
            macro.IsVarg = isVarg;

            Macro pre = null;
            Macros.TryGetValue(name.StrVal, out pre);
            if (pre is Macro.BuildinMacro) {
                Warning(name, $"マクロ {name.StrVal} は再定義できません。");
            } else {
                if (Warnings.Contains(WarningOption.RedefineMacro)) {
                    // 再定義の判定が必要
                    if (Macros.ContainsKey(name.StrVal) == true) {
                        // 同一定義の場合は再定義と見なさない
                        if (Macro.EqualDefine(pre, macro) == false) {
                            var location = pre.GetFirstPosition();
                            Warning(name, $"マクロ {name.StrVal} が再定義されました。以前のマクロ {name.StrVal} は {location} で定義されました。");
                        }
                    }
                }

                // 5.2.4.1 翻訳限界
                // 内部識別子又はマクロ名において意味がある先頭の文字数［各国際文字名又は各ソース拡張文字は，1個の文字とみなす。］(63)
                if (name.StrVal.Length > 63) {
                    Warning(name, $"マクロ名 `{name.StrVal}` は63文字を超えており、ISO/IEC 9899 5.2.4.1 翻訳限界 の制約に抵触しています。");
                }

                Macros[name.StrVal] = macro;
                DefinedMacros.Add(macro);
            }
        }

        static void read_obj_macro(Token token) {
            var name = token.StrVal;
            List<Token> body = new List<Token>();
            SkipToNextLine(null, body);
            body.ForEach((tok) => tok.Space.Chunks.ForEach(x => x.Space = System.Text.RegularExpressions.Regex.Replace(x.Space, "\r?\n", "")));
            HashHashCheck(body);
            var macro = new Macro.ObjectMacro(token, body);
            var locate = macro.GetFirstPosition();

            if (Warnings.Contains(WarningOption.CertCCodingStandard)) {
                // PRE11-C: マクロ定義をセミコロンで終端しない
                do {
                    if (body.Count > 0) {
                        var last = body.Last();
                        if (last.IsKeyword(';')) {
                            Warning(locate, $"[PRE11-C] オブジェクト形式マクロ {macro.GetName()} の置換リストがセミコロンで終端されています。");
                        }
                    }
                } while (false);
            }

            Macro pre = null;
            Macros.TryGetValue(name, out pre);
            if (pre is Macro.BuildinMacro) {
                Warning(token, $"マクロ {name} は再定義できません。");
            } else {
                if (Warnings.Contains(WarningOption.RedefineMacro)) {
                    // 再定義の判定が必要
                    if (Macros.ContainsKey(name) == true) {
                        // 同一定義の場合は再定義と見なさない
                        if (Macro.EqualDefine(pre, macro) == false) {
                            var location = pre.GetFirstPosition();
                            Warning(token, $"マクロ {name} が再定義されました。以前のマクロ {name} は {location} で定義されました。");
                        }
                    }
                }

                // 5.2.4.1 翻訳限界
                // 内部識別子又はマクロ名において意味がある先頭の文字数［各国際文字名又は各ソース拡張文字は，1個の文字とみなす。］(63)
                if (name.Length > 63) {
                    Warning(token, $"マクロ名 `{name}` は63文字を超えており、ISO/IEC 9899 5.2.4.1 翻訳限界 の制約に抵触しています。");
                }

                Macros[name] = macro;
                DefinedMacros.Add(macro);
            }

        }

        /*
         * #define
         */

        static Token read_define(Token hash, Token tdefine) {
            var nl = new Token(hash, Token.TokenKind.NewLine);
            Token name = Lex.LexToken(limitSpace: true);
            if (name.Kind != Token.TokenKind.Ident) {
                if (name.Kind == Token.TokenKind.NewLine || name.Kind == Token.TokenKind.EoF) {
                    Error(name, $"#define 指令の引数となる識別子がありません。");
                } else {
                    Error(name, $"#define 指令の引数となる識別子があるべき場所に {Token.TokenToStr(name)} がありました。");
                }
                SkipToNextLine(name);
            } else {
                Token tok = Lex.LexToken();
                if (tok.IsKeyword('(') && tok.Space.Length == 0) {
                    read_funclike_macro(name);
                } else {
                    Lex.UngetToken(tok);
                    read_obj_macro(name);
                }
            }
            return nl;
        }

        /*
         * #undef
         */

        static Token read_undef(Token hash, Token tundef) {
            var nl = new Token(hash, Token.TokenKind.NewLine);
            Token name = Lex.LexToken(limitSpace: true);
            if (name.Kind != Token.TokenKind.Ident) {
                if (name.Kind == Token.TokenKind.NewLine || name.Kind == Token.TokenKind.EoF) {
                    Error(name, $"#undef 指令の引数となる識別子がありません。");
                } else {
                    Error(name, $"#undef 指令の引数となる識別子があるべき場所に {Token.TokenToStr(name)} がありました。");
                }
                SkipToNextLine(name);
            } else {
                ExpectNewLine();
                if (name.Kind == Token.TokenKind.Ident) {
                    Macros.Remove(name.StrVal);
                }
            }
            return nl;
        }

        /*
         * #if and the like
         */

        static Token read_defined_op() {
            Token tok = Lex.LexToken(limitSpace: true);
            if (tok.IsKeyword('(')) {
                tok = Lex.LexToken(limitSpace: true);
                Lex.ExceptKeyword(')', (t) => {
                    if (t.Kind == Token.TokenKind.EoF || t.Kind == Token.TokenKind.NewLine) {
                        Error(t, $"defined 演算子の始め丸括弧 `(` に対応する終わり丸括弧 `)` がありません。");
                    } else {
                        Error(t, $"defined 演算子の始め丸括弧 `(` に対応する終わり丸括弧 `)` があるべき場所に {Token.TokenToStr(t)} がありました。");
                    }
                });
            }
            if (tok.Kind != Token.TokenKind.Ident) {
                if (tok.Kind == Token.TokenKind.EoF || tok.Kind == Token.TokenKind.NewLine) {
                    Error(tok, $"defined 演算子のオペランドとなる識別子がありません。");
                } else {
                    Error(tok, $"defined 演算子のオペランドとなる識別子があるべき場所に {Token.TokenToStr(tok)} がありました。");
                }
                return CppTokenZero(tok);
            } else {
                // 参照されたことのあるマクロとして記録する
                RefMacros.Add(tok.StrVal);
                if (Macros.ContainsKey(tok.StrVal) && Macros[tok.StrVal] != null) {
                    Macros[tok.StrVal].Used = true;
                    return CppTokenOne(tok);
                } else {
                    return CppTokenZero(tok);
                }
            }


        }

        static List<Token> read_intexpr_line() {
            List<Token> r = new List<Token>();
            for (; ; ) {
                Token tok = ReadExpand(limit_space: true);
                if (tok.Kind == Token.TokenKind.NewLine) {
                    return r;
                }

#warning "既知の問題：#if defined(C) && (C == 1 || D == 2) のような前処理式に対して未定義参照チェックができないので、C、Dが未定義というエラーになる"

                if (tok.IsIdent("defined")) {
                    r.Add(read_defined_op());
                } else if (tok.Kind == Token.TokenKind.Ident) {
                    // 6.10.1 条件付き取込み
                    //   マクロ展開及び defined 単項演算子によるすべてのマクロ置き換えの実行後，
                    //   残っているすべての識別子を前処理数 0 で置き換えてから，各前処理字句を字句に変換する。
                    // つまり、前処理の条件式評価中に登場した未定義の識別子は数字 0 に置き換えられる
                    if (!RefMacros.Contains(tok.StrVal)) {
                        if (Warnings.Contains(WarningOption.UndefinedToken)) {
                            Warning(tok, $"未定義の識別子 {Token.TokenToStr(tok)} が使用されています。6.10.1 に従い 数字 0 に置き換えられます。");
                        }
                        RefMacros.Add(tok.StrVal);
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

        private static ulong parse_char(Token tok) {
            System.Diagnostics.Debug.Assert(tok.Kind == Token.TokenKind.Char);
            byte[] code1 = tok.EncodedStrVal.CodeAt(0);
            byte[] code2 = tok.EncodedStrVal.CodeAt(1);
            if (code1 == null || code1.Length == 0) {
                Error(tok, $"空の文字定数が使われています。");
                return 0;
            }

            var code = code1.Reverse().Aggregate(0UL, (s, x) => s << 8 | x);

            if (code2 != null) {
                Warning(tok, $"2文字以上を含む文字定数 {Token.TokenToStr(tok)} が使われていますが、その値は処理系定義の結果となります。本処理系では { code } として扱います。");
            } else if (code1.Length > 8) {
                Warning(tok, $"文字定数  {Token.TokenToStr(tok)} も文字コードが uintmax_t を超えます。");
            }
            return code;
        }

        private static byte[] read_escaped_char(Token tok, string str, ref int i) {
            System.Diagnostics.Debug.Assert(tok.Kind == Token.TokenKind.Char);
            int c = str.ElementAtOrDefault(i + 0);
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
                        int c2 = str.ElementAtOrDefault(i + 1);
                        if (!CType.IsXdigit(c2)) {
                            Error(tok, $"\\x に続く文字 {(char)c2} は16進数表記で使える文字ではありません。\\xを x として読みます。");
                            return System.Text.Encoding.UTF8.GetBytes(new[] { (char)'x' });
                        } else {
                            UInt32 r = 0;
                            bool over = false;
                            int j;
                            for (j = 0; i + j < str.Length; j++) {
                                if (over == false && r > Byte.MaxValue) {
                                    over = true;
                                    Error(tok, $"16進数文字表記 \\{str} は 文字定数の表現範囲(現時点では8bit整数値)を超えます。 ");
                                }
                                c2 = str.ElementAtOrDefault(i + 1 + j);
                                if ('0' <= c2 && c2 <= '9') { r = (r << 4) | (UInt32)(c2 - '0'); continue; }
                                if ('a' <= c2 && c2 <= 'f') { r = (r << 4) | (UInt32)(c2 - 'a' + 10); continue; }
                                if ('A' <= c2 && c2 <= 'F') { r = (r << 4) | (UInt32)(c2 - 'A' + 10); continue; }
                                break;
                            }
                            i += j;
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
                            int c2 = str.ElementAtOrDefault(i + j);
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
                        Warning(tok, $"\\{(char)c} はISO/IEC 9899：1999で定められていないエスケープ文字です。" +
                                                $"規格で定められていないエスケープ文字のうち、小文字は将来に向けての予約語、それ以外は処理系定義の動作となります(ISO/IEC 9899：1999 6.4.4.4 注釈(64) および 6.11.4 参照)。" +
                                                $"本処理系では文字 `{(char)c}` として解釈します。");
                        i += 1;
                        return System.Text.Encoding.UTF8.GetBytes(new[] { (char)c });
                    }
            }

        }

        private static IntMaxT Expr(int priority, int skip) {
            Token tok = Lex.LexToken(limitSpace: true);
            Action<bool, string> handler = (e, s) => { if (e) { Error(tok, s); } else { Warning(tok, s); } };
            IntMaxT lhs;
            if (tok.IsKeyword('(')) {
                lhs = Expr(0, skip);
                tok = Lex.LexToken(limitSpace: true);
                if (tok.IsKeyword(')') == false) {
                    if (tok.Kind == Token.TokenKind.EoF) {
                        Error(tok, $"プリプロセス指令の定数式中で始め丸括弧 `(` に対応する終わり丸括弧 `)` がありませんでした。");
                    } else {
                        Error(tok, $"プリプロセス指令の定数式中で始め丸括弧 `(` に対応する終わり丸括弧 `)` があるべき場所に {Token.TokenToStr(tok)} がありました。");
                    }
                    Lex.UngetToken(tok);
                }
            } else if (tok.IsKeyword('~')) {
                lhs = IntMaxT.BitNot(Expr(11, skip), handler);
                lhs = skip > 0 ? IntMaxT.CreateSigned(0L) : lhs;
            } else if (tok.IsKeyword('!')) {
                lhs = IntMaxT.Equal(Expr(11, skip), IntMaxT.CreateSigned(0L), handler) ? IntMaxT.CreateSigned(1L) : IntMaxT.CreateSigned(0L);
                lhs = skip > 0 ? (lhs.IsUnsigned() ? IntMaxT.CreateUnsigned(0UL) : IntMaxT.CreateSigned(0L)) : lhs;
            } else if (tok.IsKeyword('+')) {
                lhs = Expr(11, skip);
                lhs = skip > 0 ? (lhs.IsUnsigned() ? IntMaxT.CreateUnsigned(0UL) : IntMaxT.CreateSigned(0L)) : lhs;
            } else if (tok.IsKeyword('-')) {
                lhs = IntMaxT.Neg(Expr(11, skip), handler);
                lhs = skip > 0 ? (lhs.IsUnsigned() ? IntMaxT.CreateUnsigned(0UL) : IntMaxT.CreateSigned(0L)) : lhs;
            } else if (tok.Kind == Token.TokenKind.Number) {
                lhs = Token.ToInt(tok, tok.StrVal);
                lhs = skip > 0 ? (lhs.IsUnsigned() ? IntMaxT.CreateUnsigned(0UL) : IntMaxT.CreateSigned(0L)) : lhs;
            } else if (tok.Kind == Token.TokenKind.Char) {
                lhs = IntMaxT.CreateUnsigned((ulong)parse_char(tok));
                lhs = skip > 0 ? IntMaxT.CreateUnsigned(0UL) : lhs;
            } else if (tok.Kind == Token.TokenKind.Ident) {
                if (skip == 0) {
                    Warning(tok, $"プリプロセス指令の定数式中に未定義の識別子 '{tok.StrVal}' が出現しています。 0 に読み替えられます。");
                }
                lhs = IntMaxT.CreateSigned(0L);
            } else if (tok.Kind == Token.TokenKind.NewLine || tok.Kind == Token.TokenKind.EoF) {
                Error(tok, "プリプロセス指令の条件式が不完全なまま行末を迎えています。");
                lhs = IntMaxT.CreateSigned(0L);
            } else {
                if (skip == 0) {
                    if (tok.Kind == Token.TokenKind.String)
                        Error(tok, $"プリプロセス指令の条件式中で 文字列リテラル は使用できません。");
                } else {
                    Error(tok, $"プリプロセス指令の条件式中で {Token.TokenToStr(tok)} は使用できません。");
                }
                lhs = IntMaxT.CreateSigned(0L);
            }

            for (; ; ) {
                Token op = Lex.LexToken(limitSpace: true);
                Action<bool, string> handler2 = (e, s) => { if (e) { Error(op, s); } else { Warning(op, s); } };
                int pri = expr_priority(op); // 0 if not a binop.
                if (pri == 0 || priority >= pri) {
                    Lex.UngetToken(op);
                    return lhs;
                }
                if (op.Kind != Token.TokenKind.Keyword) {
                    Error(op, $"演算子のあるべき場所に {Token.TokenToStr(op)} がありますが、これは演算子として定義されていません。");
                    lhs = IntMaxT.CreateSigned(0L);
                }
                switch (op.KeywordVal) {
                    case (Token.Keyword)'/': {
                            IntMaxT rhs = Expr(pri, skip);
                            if (skip > 0) {
                                lhs = IntMaxT.CreateSigned(0L);
                            } else {
                                lhs = IntMaxT.Div(lhs, rhs, handler2);
                            }
                            break;
                        }
                    case (Token.Keyword)'%': {
                            IntMaxT rhs = Expr(pri, skip);
                            if (skip > 0) {
                                lhs = IntMaxT.CreateSigned(0L);
                            } else {
                                lhs = IntMaxT.Mod(lhs, rhs, handler2);
                            }
                            break;
                        }
                    case (Token.Keyword)'*': {
                            IntMaxT rhs = Expr(pri, skip);
                            lhs = (skip > 0) ? IntMaxT.CreateSigned(0L) : IntMaxT.Mul(lhs, rhs, handler2);
                            break;
                        }
                    case (Token.Keyword)'+': {
                            IntMaxT rhs = Expr(pri, skip);
                            lhs = (skip > 0) ? IntMaxT.CreateSigned(0L) : IntMaxT.Add(lhs, rhs, handler2);
                            break;
                        }
                    case (Token.Keyword)'-': {
                            IntMaxT rhs = Expr(pri, skip);
                            lhs = (skip > 0) ? IntMaxT.CreateSigned(0L) : IntMaxT.Sub(lhs, rhs, handler2);
                            break;
                        }
                    case (Token.Keyword)'<': {
                            IntMaxT rhs = Expr(pri, skip);
                            lhs = (skip > 0) ? IntMaxT.CreateSigned(0L) : (IntMaxT.LessThan(lhs, rhs, handler2) ? IntMaxT.CreateSigned(1L) : IntMaxT.CreateSigned(0L));
                            break;
                        }
                    case (Token.Keyword)'>': {
                            IntMaxT rhs = Expr(pri, skip);
                            lhs = (skip > 0) ? IntMaxT.CreateSigned(0L) : (IntMaxT.GreatThan(lhs, rhs, handler2) ? IntMaxT.CreateSigned(1L) : IntMaxT.CreateSigned(0L));
                            break;
                        }
                    case (Token.Keyword)'&': {
                            IntMaxT rhs = Expr(pri, skip);
                            lhs = (skip > 0) ? IntMaxT.CreateSigned(0L) : IntMaxT.BitAnd(lhs, rhs, handler2);
                            break;
                        }
                    case (Token.Keyword)'^': {
                            IntMaxT rhs = Expr(pri, skip);
                            lhs = (skip > 0) ? IntMaxT.CreateSigned(0L) : IntMaxT.BitXor(lhs, rhs, handler2);
                            break;
                        }
                    case (Token.Keyword)'|': {
                            IntMaxT rhs = Expr(pri, skip);
                            lhs = (skip > 0) ? IntMaxT.CreateSigned(0L) : IntMaxT.BitOr(lhs, rhs, handler2);
                            break;
                        }

                    case Token.Keyword.ShiftArithLeft: {
                            IntMaxT rhs = Expr(pri, skip);
                            if (skip > 0) {
                                lhs = IntMaxT.CreateSigned(0);
                            } else {
                                lhs = IntMaxT.ShiftLeft(lhs, rhs.AsInt32(handler2), handler2);
                            }
                            break;
                        }
                    case Token.Keyword.ShiftArithRight: {
                            IntMaxT rhs = Expr(pri, skip);
                            if (skip > 0) {
                                lhs = IntMaxT.CreateSigned(0);
                            } else {
                                lhs = IntMaxT.ShiftRight(lhs, rhs.AsInt32(handler2), handler2);
                            }
                            break;
                        }
                    case Token.Keyword.LessEqual: {
                            IntMaxT rhs = Expr(pri, skip);
                            lhs = IntMaxT.CreateSigned((skip > 0) ? 0 : (IntMaxT.LessEqual(lhs, rhs, handler2) ? 1 : 0));
                            break;
                        }
                    case Token.Keyword.GreatEqual: {
                            IntMaxT rhs = Expr(pri, skip);
                            lhs = IntMaxT.CreateSigned((skip > 0) ? 0 : (IntMaxT.GreatEqual(lhs, rhs, handler2) ? 1 : 0));
                            break;
                        }
                    case Token.Keyword.Equal: {
                            IntMaxT rhs = Expr(pri, skip);
                            lhs = IntMaxT.CreateSigned((skip > 0) ? 0 : (IntMaxT.Equal(lhs, rhs, handler2) ? 1 : 0));
                            break;
                        }
                    case Token.Keyword.NotEqual: {
                            IntMaxT rhs = Expr(pri, skip);
                            lhs = IntMaxT.CreateSigned((skip > 0) ? 0 : (IntMaxT.NotEqual(lhs, rhs, handler2) ? 1 : 0));
                            break;
                        }
                    case Token.Keyword.LogicalAnd:
                        // 短絡評価しなければならない
                        if (skip == 0) {
                            if (IntMaxT.NotEqual(lhs, IntMaxT.CreateSigned(0))) {
                                IntMaxT rhs = Expr(pri, skip);
                                lhs = IntMaxT.CreateSigned(IntMaxT.NotEqual(rhs, IntMaxT.CreateSigned(0)) ? 1 : 0);
                            } else {
                                IntMaxT rhs = Expr(pri, skip + 1);
                                lhs = IntMaxT.CreateSigned(0);
                            }
                        } else {
                            Expr(pri, skip + 1);
                            lhs = IntMaxT.CreateSigned(0);
                        }
                        break;
                    case Token.Keyword.LogincalOr:
                        // 短絡評価しなければならない
                        if (skip == 0) {
                            if (IntMaxT.Equal(lhs, IntMaxT.CreateSigned(0))) {
                                IntMaxT rhs = Expr(pri, skip);
                                lhs = IntMaxT.CreateSigned(IntMaxT.NotEqual(rhs, IntMaxT.CreateSigned(0)) ? 1 : 0);
                            } else {
                                IntMaxT rhs = Expr(pri, skip + 1);
                                lhs = IntMaxT.CreateSigned(1);
                            }
                        } else {
                            Expr(pri, skip + 1);
                            lhs = IntMaxT.CreateSigned(0);
                        }
                        break;
                    case (Token.Keyword)'?': {
                            // ３項演算子
                            var cond = IntMaxT.NotEqual(lhs, IntMaxT.CreateSigned(0));
                            IntMaxT rhs1 = Expr(0, skip + (cond ? 0 : 1));
                            Token tok2 = Lex.LexToken();
                            if (tok2.IsKeyword(':') == false) {
                                Error(op, $"三項演算子の`:`があるべき場所に `{Token.TokenToStr(tok2)}` があります。");
                                return IntMaxT.CreateSigned(0);
                            }
                            IntMaxT rhs2 = Expr(0, skip + (cond ? 1 : 0));
                            if (skip > 0) {
                                lhs = IntMaxT.CreateSigned(0);
                            } else {
                                lhs = (cond) ? rhs1 : rhs2;
                            }
                            return lhs;
                        }

                    default:
                        Error(op, $"演算子のあるべき場所に `{Token.TokenToStr(op)}` がありますが、これは演算子として定義されていません。");
                        return IntMaxT.CreateSigned(0);

                }
            }
        }

        //

        static bool read_constexpr(out List<Token> exprtokens) {
            /* コンパイルスイッチの記録のために前処理トークン列を展開無しで先読みして記録する */
            {
                exprtokens = new List<Token>();
                Token pretok = SkipToNextLine(null, exprtokens);
                exprtokens.ForEach((t) => {
                    if (t.Kind == Token.TokenKind.Invalid) {
                        Error(t, $@"プリプロセス指令の条件式中に不正な文字 `\u{(int)t.StrVal[0]:X4}` がありました。");
                    }
                });
                exprtokens.Add(Token.make_eof(pretok.Position));
                Lex.UngetToken(pretok);
                foreach (var t in exprtokens.Reverse<Token>()) {
                    Lex.UngetToken(t);
                }
            }

            var intexprtoks = read_intexpr_line().Reverse<Token>().ToList();
            Lex.TokenBufferStash(intexprtoks);
            var expr = Expr(0, 0);
            Token tok = Lex.LexToken(limitSpace: true);
            if (tok.Kind != Token.TokenKind.EoF) {
                Error(tok, $"プリプロセス指令の条件式中に余分なトークン {Token.TokenToStr(tok)} がありました。");
                while (tok.Kind != Token.TokenKind.EoF) {
                    tok = Lex.LexToken(limitSpace: true);
                }
            }
            Lex.TokenBufferUnstash();
            return IntMaxT.NotEqual(expr, IntMaxT.CreateSigned(0));
        }

        static void do_read_if(bool isTrue) {
            ConditionStack.Push(new Condition(isTrue));
            if (!isTrue) {
                Lex.SkipCondInclude();
            }
        }

        static Token read_if(Token hash, Token tif) {
            var nl = new Token(hash, Token.TokenKind.NewLine);
            List<Token> exprtoken;
            var cond = read_constexpr(out exprtoken);
            do_read_if(cond);
            // コンパイルスイッチを記録
            Reporting.TraceCompileSwitch.OnIf(hash.Position, string.Join(" ", exprtoken.Select(x => Token.TokenToStr(x))), cond);
            return nl;
        }

        static Token read_ifdef(Token hash, Token tifdef) {
            var nl = new Token(hash, Token.TokenKind.NewLine);
            Token tok = Lex.LexToken(limitSpace: true);
            bool cond = false;
            if (tok.Kind != Token.TokenKind.Ident) {
                if (tok.Kind == Token.TokenKind.NewLine || tok.Kind == Token.TokenKind.EoF) {
                    Error(tok, $"#ifdef 指令の引数となる識別子がありません。");
                } else {
                    Error(tok, $"#ifdef 指令の引数となる識別子があるべき場所に {Token.TokenToStr(tok)} がありました。");
                }
                SkipToNextLine(tok);
            } else {
                ExpectNewLine();

                // 参照されたことのあるマクロとして記録する
                RefMacros.Add(tok.StrVal);

                cond = Macros.ContainsKey(tok.StrVal) && Macros[tok.StrVal] != null;
                if (cond) {
                    Macros[tok.StrVal].Used = true;
                }

                // コンパイルスイッチを記録
                Reporting.TraceCompileSwitch.OnIfdef(hash.Position, tok.StrVal, cond);

            }
            do_read_if(cond);
            return nl;
        }

        /// <summary>
        /// 'ifndef'指令の処理
        /// </summary>
        static Token read_ifndef(Token hash, Token tifndef) {
            var nl = new Token(hash, Token.TokenKind.NewLine);

            Token tok = Lex.LexToken(limitSpace: true);
            bool cond = false;
            if (tok.Kind != Token.TokenKind.Ident) {
                if (tok.Kind == Token.TokenKind.NewLine || tok.Kind == Token.TokenKind.EoF) {
                    Error(tok, $"#ifndef 指令の引数となる識別子がありません。");
                } else {
                    Error(tok, $"#ifndef 指令の引数となる識別子があるべき場所に {Token.TokenToStr(tok)} がありました。");
                }
                SkipToNextLine(tok);
            } else {
                ExpectNewLine();


                // 参照されたことのあるマクロとして記録する
                RefMacros.Add(tok.StrVal);

                cond = !Macros.ContainsKey(tok.StrVal) || Macros[tok.StrVal] == null;
                if (cond == false) {
                    Macros[tok.StrVal].Used = true;
                }

                // コンパイルスイッチを記録
                Reporting.TraceCompileSwitch.OnIfndef(hash.Position, tok.StrVal, cond);
            }

            do_read_if(cond);
            if (tok.IndexOfFile == 1) {
                // ファイルから読み取ったトークンが
                //   一つ目が '#'
                //   二つ目が 'ifndef'
                // の場合、「インクルードガード」パターンかもしれないので
                // 検出の準備をする
                Condition ci = ConditionStack.Peek();
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
                Error(hash, "#if / #ifdef と対応の取れない #else がありました。");
                // エラー回復方法がコンパイラによってまちまちなので #else を読み飛ばす方向で
                var tok = SkipToNextLine(hash);
                Lex.UngetToken(tok);
            } else {
                // 現在の条件コンパイル状態を確認して、else解析中の場合、elseが多重なのでエラー
                Condition ci = ConditionStack.Peek();
                if (ci.CurrentState == Condition.State.InElse) {
                    Error(hash, "#else の後に再度 #else が出現しました。");
                    // エラー回復方法がコンパイラによってまちまちなので #else を読み飛ばす方向で
                    var tok = SkipToNextLine(hash);
                    Lex.UngetToken(tok);
                } else {
                    // ELSE解析中に遷移する。
                    //tokens.Add(hash);
                    {
                        Token tok = Lex.LexToken();
                        if (tok.Kind != Token.TokenKind.NewLine) {
                            if (Warnings.Contains(WarningOption.Pedantic)) {
                                Warning(tok, "#else 指令の末尾に余分なトークンがあります。");
                            }
                            SkipToNextLine(tok);
                        }
                    }

                    // コンパイルスイッチを記録
                    Reporting.TraceCompileSwitch.OnElse(hash.Position, !ci.WasTrue);

                    ci.CurrentState = Condition.State.InElse;
                    // #else が出てきたということは、インクルードガードパターンに合致しないので、
                    // 現在の処理している条件コンパイルブロックはインクルードガードではない。
                    // なので、条件コンパイル情報からインクルードガードであることを消す。
                    ci.IncludeGuard = null;

                    // elseに出会うまでに有効な条件ブロックがあった場合はelseブロックを読み飛ばす
                    if (ci.WasTrue) {
                        Lex.SkipCondInclude();
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
                Error(hash, "#if / #ifdef と対応の取れない #elif がありました。");
                // エラー回復方法がコンパイラによってまちまちなので #elif を読み飛ばす方向で
                var tok = SkipToNextLine(hash);
                Lex.UngetToken(tok);
            } else {
                Condition ci = ConditionStack.Peek();
                if (ci.CurrentState == Condition.State.InElse) {
                    // #else の後に #elif  が使われている

                    Error(hash, "#else の後に #elif が出現しました。");
                    // エラー回復方法がコンパイラによってまちまちなので #elif を読み飛ばす方向で
                    var tok = SkipToNextLine(hash);
                    Lex.UngetToken(tok);
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
                        Reporting.TraceCompileSwitch.OnElif(hash.Position, string.Join(" ", exprtoken.Select(x => Token.TokenToStr(x))), false);
                        Lex.SkipCondInclude();
                    } else {
                        // コンパイルスイッチを記録
                        Reporting.TraceCompileSwitch.OnElif(hash.Position, string.Join(" ", exprtoken.Select(x => Token.TokenToStr(x))), true);
                        ci.WasTrue = true;
                    }
                }
            }
            return nl;
        }

        static Token read_endif(Token hash, Token tendif) {
            var nl = new Token(hash, Token.TokenKind.NewLine);
            if (ConditionStack.Count == 0) {
                Error(hash, "#if / #ifdef と対応の取れない #endif がありました。");
                // エラー回復方法がコンパイラによってまちまちなので #endif を読み飛ばす方向で
                var tok = SkipToNextLine(hash);
                Lex.UngetToken(tok);
            } else {

                Condition ci = ConditionStack.Pop();

                {
                    Token tok = Lex.LexToken(limitSpace: true);
                    if (tok.Kind != Token.TokenKind.NewLine && tok.Kind != Token.TokenKind.EoF) {
                        if (Warnings.Contains(WarningOption.Pedantic)) {
                            Warning(tok, "#endif 指令の末尾に余分なトークンがあります。");
                        }
                        SkipToNextLine(tok);
                    }
                }

                // コンパイルスイッチを記録
                Reporting.TraceCompileSwitch.OnEndif(hash.Position);

                // Detect an #ifndef and #endif pair that guards the entire
                // header file. Remember the macro name guarding the file
                // so that we can skip the file next time.
                if (ci.IncludeGuard == null || ci.File != hash.File) {
                    // 現在の処理している条件コンパイルブロックはインクルードガードパターンに合致しない
                } else {
                    // 空白と改行を読み飛ばして先読みを行う
                    Token last = Lex.LexToken(limitSpace: true);
                    SpaceInfo sp = last.Space;
                    while (last.Kind == Token.TokenKind.NewLine && ci.File == last.File) {
                        sp.Append(new Position(last.File.Name, last.File.Line, last.File.Column), "\n");
                        last = Lex.LexToken(limitSpace: true);
                    }
                    last.Space = sp;
                    Lex.UngetToken(last);

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

        #region error/warning 指令の解析

        static string ParseMessage() {
            StringBuilder sb = new StringBuilder();
            for (; ; ) {
                Token tok = Lex.LexToken(limitSpace: true);
                if (tok.Kind == Token.TokenKind.NewLine) { return sb.ToString(); }
                if (tok.Kind == Token.TokenKind.EoF) { return sb.ToString(); }
                if (sb.Length != 0 && tok.Space.Length > 0) { sb.Append(' '); }
                sb.Append(Token.TokenToStr(tok));
            }
        }

        static Token ParseErrorDirective(Token hash, Token terror) {
            var nl = new Token(hash, Token.TokenKind.NewLine);
            Error(hash, $"#error: {ParseMessage()}");
            return nl;
        }

        static Token ParseWarningDirective(Token hash, Token twarning) {
            var nl = new Token(hash, Token.TokenKind.NewLine);
            Warning(hash, $"#warning: {ParseMessage()}");
            return nl;
        }

        #endregion

        #region include 指令の解析

        /// <summary>
        /// 6.10.2 ソースファイル取込み に従って
        /// #include に続くファイルパス部分の読み取りと処理を行う
        /// </summary>
        /// <param name="hash"></param>
        /// <param name="isGuillemet">パスが山括弧で括られている場合は真</param>
        /// <returns></returns>
        static string ReadCppHeaderName(Token hash, out bool isGuillemet) {

            /* 
             * ファイルパス部分の読み取り
             * <～>形式の場合は std が true に、"～"形式の場合は false になる
             */
            string path = Lex.ReadHeaderFilePath(hash, out isGuillemet);
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
            Token tok = ReadExpand();
            if (tok.Kind == Token.TokenKind.NewLine) {
                Error(hash, "識別子があるべき場所に改行がありました。");
                Lex.UngetToken(tok);
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
                bool saveComment = Switchs.Contains("-C");
                Switchs.Remove("-C");
                for (; ; ) {
                    Token tok2 = ReadExpand();
                    if (tok2.Kind == Token.TokenKind.NewLine) {
                        Error(tok, "ヘッダファイルが閉じられないまま行末に到達しました。");
                        Lex.UngetToken(tok);
                        break;
                    }
                    sb.Append(tok2.Space.Any() ? " " : "");
                    if (tok2.IsKeyword('>')) {
                        break;
                    }
                    sb.Append(Token.TokenToStr(tok2));
                }
                if (saveComment) {
                    Switchs.Add("-C");
                }
                isGuillemet = true;
                // トークンを全て単純に連結した文字列を返す
                return sb.ToString();
            } else if (tok.Kind == Token.TokenKind.Invalid) {
                // 不正文字の場合は既にエラーが出ているはずなのでメッセージを出さない
                tok = SkipToNextLine(tok);
                Lex.UngetToken(tok);
                return null;
            } else {
                // どっちでもない場合はダメ
                Error(tok, $"ヘッダファイル名のあるべき場所に {Token.TokenToStr(tok)} がありました。");
                tok = SkipToNextLine(tok);
                Lex.UngetToken(tok);
                return null;
            }
        }

        /// <summary>
        /// ファイルがインクルードガードされているか判定
        /// </summary>
        /// <param name="hash"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        static bool IsIncludeGuarded(Token hash, string path) {
            string guard = IncludeGuards.ContainsKey(path) ? IncludeGuards[path] : null;
            bool r = (guard != null && Macros.ContainsKey(guard) && Macros[guard] != null);
            if (guard != null && Macros.ContainsKey(guard) && Macros[guard] != null) {
                Macros[guard].Used = true;
            }
            return r;
        }

        /// <summary>
        /// (PRE04-C検査用) include 対象ファイルの存在を調べる
        /// </summary>
        /// <param name="hash"></param>
        /// <param name="dir"></param>
        /// <param name="filename"></param>
        /// <returns>存在する場合はフルパスを、存在しない場合はnullを返す</returns>
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
        /// include を試行する
        /// </summary>
        /// <param name="hash"></param>
        /// <param name="dir"></param>
        /// <param name="filename"></param>
        /// <returns>成功時は真、失敗時は偽</returns>
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

            if (Warnings.Contains(WarningOption.CertCCodingStandard)) {
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
                if (target == null || SystemIncludePaths.Any(x => target.StartsWith(x, StringComparison.OrdinalIgnoreCase)) == false) {
                    // システムインクルードパス内のファイルが"..."で参照するケースではないときにチェック
                    var conflicts = SystemIncludePaths.Select(x => SearchInclude(hash, x, filename)).Where(x => x != null).ToList();
                    if (conflicts.Count > 0) {
                        Warning(hash, $@"[PRE04-C] 標準ヘッダと同じ名前のファイル {filename} をシステムインクルードパス以外からインクルードしています。C90以降のC言語規格では、このようなコードは未定義の動作となることが示されています(参考文献:[ISO/IEC 9899:2011] 7.1.2 P3)。");
                    }
                }
                #endregion

            }

            if (Once.ContainsKey(path) && Once[path] == "1") {
                // #pragma once 済みの場合
                return true;
            }
            if (IsIncludeGuarded(hash, path)) {
                // インクルードガードパターンを伴う二回目以降のincludeの場合
                return true;
            }
            if (System.IO.File.Exists(path) == false) {
                // ファイルがない場合
                return false;
            }

            // ファイルを開いてファイルスタックに積む
            var sr = LibReadJEnc.ReadJEnc.CreateStreamReaderFromFile(path, AutoDetectEncoding, DefaultEncoding);
            if (sr == null) {
                return false;
            }

            // 入力ストリームにファイルを積む
            File.StreamPush(new File(sr, path));

            return true;
        }

        /// <summary>
        /// 相対パスを正規化する
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
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

        /// <summary>
        /// #include 指令の処理
        /// </summary>
        /// <param name="hash"></param>
        /// <param name="tinclude"></param>
        /// <returns></returns>
        static Token ParseIncludeDirective(Token hash, Token tinclude) {
            var nl = new Token(hash, Token.TokenKind.Space);

            var file = tinclude.File;

            bool isGuillemet;   // #include <...> 形式の場合は真になる
            var filename = ReadCppHeaderName(hash, out isGuillemet);

            {
                var tok2 = ReadExpand();
                if (tok2.Kind != Token.TokenKind.NewLine) {
                    Error(tok2, "#include指令の後ろに空白以外の要素があります。");
                    tok2 = SkipToNextLine(tok2);
                }
                Lex.UngetToken(tok2);
            }

            if (filename == null) {
                // ReadCppHeaderName中でエラー出力しているので何も出さずに抜ける
                return nl;
            } else if (System.IO.Path.IsPathRooted(filename)) {
                // レアケースだが、絶対パスが指定されている場合はinclude形式に関係なく直接そのファイルを探して読み込む
                if (TryInclude(hash, "/", filename)) {  // ディレクトリに "/" を渡しているが絶対パス指定なので無視される
                    goto succ;
                }
                goto err;
            }

            // #include "..." 形式の場合、カレントディレクトリ⇒インクルードパス(-Iオプション)⇒システムインクルードパス の順で検索
            // #include <...> 形式の場合、システムインクルードパス のみ検索

            if (Warnings.Contains(WarningOption.CertCCodingStandard)) {
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
                            Warning(hash, $@"[PRE08-C] インクルードで指定されたファイル {filename} のディレクトリと拡張子を除いたファイル名の先頭最大８文字 {key} が 別のインクルードで指定されたファイル {other.Item2} と大文字小文字を区別せずに一致するため、C言語規格上は曖昧なインクルードとなる可能性が示されています(参考文献:[ISO/IEC 9899:2011] 6.10.2)。");
                        }
                        if (entries.Any(x => x.Item1 == name) == false) {
                            entries.Add(Tuple.Create(name, filename, hash.Position));
                        }
                    } else {
                        PRE08IncludeFileTable[key] = new List<Tuple<string, string, Position>>() { Tuple.Create(name, filename, hash.Position) };
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

                // 現在のファイルと同じディレクトリから include を試行
                string dir = ".";
                try {
                    dir = file.Name != null ? System.IO.Path.GetDirectoryName(file.Name) : ".";
                } catch {
                    // System.IO.Path.GetDirectoryNameに失敗
                }
                if (TryInclude(hash, dir, filename)) {
                    goto succ;
                }

                // -Iオプションの指定順で探索して include を試行
                foreach (var path in UserIncludePaths) {
                    if (TryInclude(hash, path, filename)) {
                        goto succ;
                    }
                }
                /*
                 * ISO/IEC 9899 6.10.2 ソースファイル取込み　では、#include "～" のファイル探索に失敗した場合、#include <～> に読み替えて再探索することと規定されている。
                 * そのため、ユーザーインクルードパス中から見つからなかった場合でもエラーとせず、標準インクルードパスからの探索を行う。
                 */
            }

            // システムインクルードパスから探索して include を試行
            {
                /**
                 * ISO/IEC 9899 6.10.2 ソースファイル取込み　で、
                 *   どのようにして探索の場所を指定するか，またどのようにしてヘッダを識別するかは，処理系定義とする。
                 * とされている。
                 */
                foreach (var path in SystemIncludePaths) {
                    if (TryInclude(hash, path, filename)) {
                        if (!isGuillemet && Warnings.Contains(WarningOption.ImplicitSystemHeaderInclude)) {
                            Warning(hash, $"二重引用符形式で指定されたファイル {filename} がインクルードパス中では見つからず、システムインクルードパス中から見つかりました。" +
                                          $"この動作は「ISO/IEC 9899 6.10.2 ソースファイル取込み」で規定されている標準動作ですが、意図したものでなければ二重引用符ではなく山括弧の利用を検討してください。");
                        }
                        goto succ;
                    }
                }
                goto err;
            }

        succ:
            return nl;

        err:
            Error(hash, $"#includeで指定されたファイル {(isGuillemet ? "<" : "\"")}{filename}{(isGuillemet ? ">" : "\"")} が見つかりません。");
            return nl;
        }

        #endregion

        #region pragma指令の解析

        /// <summary>
        /// #pragma 指令の処理
        /// </summary>
        /// <param name="hash"></param>
        /// <param name="tpragma"></param>
        /// <returns></returns>
        static Token ParsePragmaDirective(Token hash, Token tpragma) {
            var nl = new Token(hash, Token.TokenKind.NewLine);
            Token tok = Lex.LexToken(limitSpace: true);
            if (tok.Kind != Token.TokenKind.Ident) {
                if (tok.Kind == Token.TokenKind.NewLine || tok.Kind == Token.TokenKind.EoF) {
                    Error(tok, $"#pragma 指令の引数となる識別子がありません。");
                } else {
                    Error(tok, $"#pragma 指令の引数となる識別子があるべき場所に {Token.TokenToStr(tok)} がありました。");
                }
                // pragma は処理系依存であるため、このpragma全体を素通しする
                List<Token> tokens = new List<Token> { hash, tpragma, tok };
                SkipToNextLine(null, tokens);

                // Verbatimフラグをつけて読み戻しを行う
                tokens.Skip(1).Reverse().ToList().ForEach(x => {
                    x.Verbatim = true;
                    Lex.UngetToken(x);
                });
                return hash;
            }
            string s = tok.StrVal;
            switch (s) {
                case "once": {
                        // # pragma once は非標準だが、広く実装されており有益であるため本処理系でも実装する。
                        string path = System.IO.Path.GetFullPath(tok.File.Name);
                        Once[path] = "1";
                        SkipToNextLine(null);

                        return nl;
                    }
                case "STDC": {
                        // 6.10.6 プラグマ指令
                        // （マクロ置き換えに先だって）pragma の直後の前処理字句が STDC である場合，この指令に対してマクロ置き換えは行わない。
                        // とあるので素通しする。
                        List<Token> tokens = new List<Token> { hash, tpragma, tok };
                        SkipToNextLine(null, tokens);

                        tokens.Skip(1).Reverse().ToList().ForEach(x => {
                            x.Verbatim = true;
                            Lex.UngetToken(x);
                        });
                        return hash;
                    }
                default: {
                        if (Warnings.Contains(WarningOption.UnknownPragmas)) {
                            // 不明なプラグマなので警告を出す。
                            Warning(hash, $"#pragma {s} は不明なプラグマです。");
                        }
                        List<Token> tokens = new List<Token> { hash, tpragma, tok };
                        SkipToNextLine(null, tokens);

                        tokens.Skip(1).Reverse().ToList().ForEach(x => {
                            x.Verbatim = true;
                            Lex.UngetToken(x);
                        });
                        return hash;
                    }
            }
        }

        #endregion

        #region line指令の解析

        /// <summary>
        /// 文字列が10進数表記か調べる
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        static bool IsDigitSequence(string str) {
            return str.All(p => CType.IsDigit(p));
        }

        /// <summary>
        /// line指令の読み取りを行う
        /// </summary>
        /// <param name="hash"></param>
        /// <param name="tok"></param>
        /// <param name="kind"></param>
        /// <returns></returns>
        private static Token DoParseLineDirective(Token hash, Token tok, string kind) {
            var nl = new Token(hash, Token.TokenKind.NewLine);
            if (tok.Kind != Token.TokenKind.Number || !IsDigitSequence(tok.StrVal)) {
                if (tok.Kind == Token.TokenKind.NewLine || tok.Kind == Token.TokenKind.EoF) {
                    Error(tok, $"{kind} の行番号があるべき場所で行が終わっています。。");
                } else if (tok.Kind == Token.TokenKind.Number && !IsDigitSequence(tok.StrVal)) {
                    Error(tok, $"{kind} の行番号の表記は10進数に限られています。");
                } else {
                    Error(tok, $"{kind} で指定されている {Token.TokenToStr(tok)} は行番号ではありません。");
                }
                SkipToNextLine(null);
                return nl;
            } else {
                long line = long.Parse(tok.StrVal);
                if (line <= 0 || Int32.MaxValue < line) {
                    Error(tok, $"{kind} の行番号に {tok.StrVal} が指定されていますが、 1 以上 {Int32.MaxValue} 以下でなければなりません。");
                    SkipToNextLine(null);
                    return nl;
                }

                // ファイル名を読み取る
                tok = ReadExpand(limit_space: true);
                if (tok.Kind != Token.TokenKind.String) {
                    if (tok.Kind == Token.TokenKind.NewLine || tok.Kind == Token.TokenKind.EoF) {
                        Error(tok, $"{kind} のファイル名があるべき場所で行が終わっています。。");
                    } else {
                        Error(tok, $"{kind} で指定されている {Token.TokenToStr(tok)} はファイル名ではありません。");
                    }
                    SkipToNextLine(null);
                    return nl;
                } else {
                    string filename = tok.StrVal;

                    // 残りのフラグなどは纏めて行末まで読み飛ばす
                    SkipToNextLine(null);

                    // 現在のファイルの位置情報を変更 
                    File.OverwriteCurrentPosition((int)line, filename);

                    return nl;
                }
            }
        }

        /// <summary>
        /// 一般的なline指令(#line linenum filename 形式)の処理
        /// </summary>
        static Token ParseLineDirective(Token hash, Token tline) {
            Token tok = ReadExpand(limit_space: true);
            return DoParseLineDirective(hash, tok, "line指令");
        }

        /// <summary>
        /// gcc形式のline指令(# linenum filename flags 形式)の処理
        /// </summary>
        /// <param name="tok"></param>
        static Token ParseGccStyleLineDirective(Token hash, Token tok) {
            return DoParseLineDirective(hash, tok, "gcc形式のline指令");
        }

        #endregion

        /// <summary>
        /// 未知の指令の処理
        /// </summary>
        /// <param name="hash"></param>
        /// <param name="tok"></param>
        /// <returns></returns>
        static Token UnsupportedPreprocessorDirective(Token hash, Token tok) {
            if (Features.Contains(FeatureOption.UnknownDirectives)) {
                if (Warnings.Contains(WarningOption.UnknownDirectives)) {
                    Warning(tok, $"{Token.TokenToStr(tok)} は未知のプリプロセッサ指令です。");
                }
                // 行末まで読み取って、マクロ展開禁止フラグを付けて押し戻す。
                List<Token> buf = new List<Token> { tok };
                SkipToNextLine(null, buf);
                buf.ForEach(x => x.Verbatim = true);
                UngetAll(buf);
                return hash;
            } else {
                Warning(tok, $"{Token.TokenToStr(tok)} は未知のプリプロセッサ指令です。");
                SkipToNextLine(null);
                return new Token(hash, Token.TokenKind.NewLine);
            }
        }

        /// <summary>
        /// プリプロセッサ指令の解析
        /// </summary>
        /// <param name="hash">'#'に対応するトークン</param>
        /// <returns>解析結果のトークン</returns>
        static Token ParseDirective(Token hash) {
            InDirectiveLine++;
            var ret = ParseDirectiveBody(hash);
            InDirectiveLine--;
            return ret;
        }

        static Token ParseDirectiveBody(Token hash) {
            Token tok = Lex.LexToken(limitSpace: true);
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
                            case "error": return ParseErrorDirective(hash, tok);
                            case "if": return read_if(hash, tok);
                            case "ifdef": return read_ifdef(hash, tok);
                            case "ifndef": return read_ifndef(hash, tok);
                            case "include": return ParseIncludeDirective(hash, tok);
                            case "line": return ParseLineDirective(hash, tok);
                            case "pragma": return ParsePragmaDirective(hash, tok);
                            case "undef": return read_undef(hash, tok);
                            case "warning": return ParseWarningDirective(hash, tok); // 非標準動作
                        }
                        goto default;    // 未知のプリプロセッサとして処理
                    }
                default: {
                        // それら以外は未知のプリプロセッサとして処理
                        return UnsupportedPreprocessorDirective(hash, tok);
                    }
            }
        }

        /// <summary>
        /// 組み込みマクロ
        /// </summary>
        public static class SpecialMacros {

            private static List<Token> HandleStdcMacro(Macro.BuildinMacro m, Token tmpl) {
                return new[] { new Token(tmpl, Token.TokenKind.Number) { StrVal = "1" } }.ToList();
            }

            private static List<Token> HandleDateMacro(Macro.BuildinMacro m, Token tmpl) {
                return new[] { new Token(tmpl, Token.TokenKind.String) { StrVal = DateString } }.ToList();
            }

            private static List<Token> HandleTimeMacro(Macro.BuildinMacro m, Token tmpl) {
                return new[] { new Token(tmpl, Token.TokenKind.String) { StrVal = TimeString } }.ToList();
            }

            private static List<Token> HandleFileMacro(Macro.BuildinMacro m, Token tmpl) {
                return new[] { new Token(tmpl, Token.TokenKind.String) { StrVal = tmpl.File.Name.Replace(@"\", @"\\") } }.ToList();
            }

            private static List<Token> HandleLineMacro(Macro.BuildinMacro m, Token tmpl) {
                return new[] { new Token(tmpl, Token.TokenKind.Number) { StrVal = $"{tmpl.File.Line}" } }.ToList();
            }

            public static void Install() {
                DefineSpecialMacro("__STDC__", HandleStdcMacro);
                DefineSpecialMacro("__DATE__", HandleDateMacro);
                DefineSpecialMacro("__TIME__", HandleTimeMacro);
                DefineSpecialMacro("__FILE__", HandleFileMacro);
                DefineSpecialMacro("__LINE__", HandleLineMacro);
            }
        }

        /// <summary>
        /// システムインクルードパスを追加する
        /// </summary>
        /// <param name="path"></param>
        public static void AddSystemIncludePath(string path) {
            SystemIncludePaths.Add(path);
        }

        /// <summary>
        /// インクルードパスを追加する
        /// </summary>
        /// <param name="path"></param>
        public static void AddUserIncludePath(string path) {
            UserIncludePaths.Add(path);
        }

        /// <summary>
        /// 組み込みマクロの定義
        /// </summary>
        /// <param name="name">マクロ名</param>
        /// <param name="fn">マクロを処理するハンドラ</param>
        public static void DefineSpecialMacro(string name, Macro.BuildinMacro.BuiltinMacroHandler fn) {
            if (name.Length > 63) {
                Warning($"組込みマクロ名 `{name}` は63文字を超えており、ISO/IEC 9899 5.2.4.1 翻訳限界 の制約に抵触しています。");
            }
            Macros[name] = new Macro.BuildinMacro(name, fn);
            DefinedMacros.Add(Macros[name]);
            Macros[name].Used = true;
        }

        static void InitKeywords() {
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
        static void InitPredefinedMacros() {
            SpecialMacros.Install();
        }

        /// <summary>
        /// 初期化
        /// </summary>
        public static void Init() {
            InitKeywords();
            InitPredefinedMacros();
        }

        /// <summary>
        /// トークン tok が識別子の場合は予約語かどうかチェックし、予約語なら予約語トークンに変換する
        /// </summary>
        /// <param name="tok"></param>
        /// <returns></returns>
        private static Token MaybeConvertKeyword(Token tok) {
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

        /// <summary>
        /// 現在のトークンを覗き見る
        /// </summary>
        /// <returns></returns>
        private static Token PeekToken() {
            Token r = ReadToken();
            Lex.UngetToken(r);
            return r;
        }

        /// <summary>
        /// プリプロセス処理を行って得られたトークンを一つ読み取る
        /// </summary>
        /// <returns></returns>
        public static Token ReadToken() {
            for (; ; ) {
                var tok = ReadExpand();

                if (tok.BeginOfLine && tok.IsKeyword('#') && tok.Hideset == null) {
                    //プリプロセッサ指令を処理する。出力は空白行になる。
                    return ParseDirective(tok);
                }
                if (tok.Kind < Token.TokenKind.MinCppToken && tok.Verbatim == false) {
                    // 読み取ったものがプリプロセッサキーワードになり得るものかつ、verbatimでないなら
                    // プリプロセス処理を試みる
                    return MaybeConvertKeyword(tok);
                } else {
                    // キーワードになり得ないものならそのまま
                    return tok;
                }
            }
        }

        /// <summary>
        /// 未使用マクロの列挙
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<Macro> EnumUnusedMacro() {
            return DefinedMacros.Where(x => x.Used == false && !RefMacros.Contains(x.GetName()));
        }
    }
}
