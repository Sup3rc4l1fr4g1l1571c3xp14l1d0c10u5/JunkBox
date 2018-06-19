using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;

namespace CSCPP {
    public class Token {
        public override string ToString() {
            var sb = new StringBuilder();
            switch (Kind) {
                case TokenKind.Ident:
                    sb.Append($"<Ident Str='{StrVal}' ");
                    break;
                case TokenKind.Keyword:
                    sb.Append($"<Keyword Value='{KeywordToStr(KeywordVal)}' ");
                    break;
                case TokenKind.Number:
                    sb.Append($"<Number Value='{StrVal}' ");
                    break;
                case TokenKind.Char:
                    sb.Append($"<Char Value='{QuoteChar(StrVal)}' ");
                    break;
                case TokenKind.String:
                    sb.Append($"<String Value=\"{QuoteStr(StrVal)}\" ");
                    break;
                case TokenKind.EoF:
                    sb.Append($"<EoF ");
                    break;
                case TokenKind.Invalid:
                    sb.Append($"<Invalid Value=\"{QuoteStr(StrVal)}\" ");
                    break;
                case TokenKind.MinCppToken:
                    // not use
                    break;
                case TokenKind.NewLine:
                    sb.Append($"<NewLine ");
                    break;
                case TokenKind.Space:
                    sb.Append($"<Space ");
                    break;
                case TokenKind.MacroParam:
                    sb.Append($"<MacroParam Name='{ArgName}' ");
                    break;
                case TokenKind.MacroParamRef:
                    sb.Append($"<MacroParamRef Ref='{MacroParamRef.Id}' ");
                    break;
                case TokenKind.MacroRangeFixup:
                    sb.Append($"<MacroRangeFixup");
                    break;
                default:
                    //throw new ArgumentOutOfRangeException();
                    sb.Append($"<{Kind} ");
                    break;
            }
            // common information
            sb.Append(
                $"Id='{Id}' " +
                $"File='{Pos.FileName}' " +
                $"Line='{Pos.Line}' " +
                $"Column='{Pos.Column}' " +
                $"Space='{Space}' " +
                $"Verbatim='{Verbatim}' " +
                $"BeginOfLine='{BeginOfLine}' " +
                $"/>");
            return sb.ToString();
        }

        public string ToRawString() {
            var sb = new StringBuilder();
            switch (Kind) {
                case TokenKind.Ident: return StrVal;
                case TokenKind.Keyword: return KeywordToStr(KeywordVal);
                case TokenKind.Number: return StrVal;
                case TokenKind.Char: return $"'{QuoteChar(StrVal)}'";
                case TokenKind.String: return $"\"{QuoteStr(StrVal)}\"";
                case TokenKind.EoF: return "";
                case TokenKind.Invalid: return StrVal;
                case TokenKind.MinCppToken: return ""; // not use
                case TokenKind.NewLine: return "";
                case TokenKind.Space: return "";
                case TokenKind.MacroParam: return ArgName;
                case TokenKind.MacroParamRef: return MacroParamRef.ArgName;
                case TokenKind.MacroRangeFixup: return "<MacroRangeFixup>";
                default: return "";
            }
        }

        public enum TokenKind {
            Ident,
            Keyword,
            Number,
            Char,
            String,
            EoF,
            Invalid,
            // Only in CPP
            MinCppToken,
            NewLine,
            Space,
            MacroParam,
            MacroParamRef,
            // special
            MacroRangeFixup,
            // only in export
            UserIncludePath,
            SystemIncludePath,
        };

        public enum Keyword {
            Arrow,
            AssignAdd,
            AssignAnd,
            AssignDiv,
            AssignMod,
            AssignMul,
            AssignOr,
            AssignShiftArithLeft,
            AssignShiftArithRight,
            AssignSub,
            AssignXor,
            Dec,
            Equal,
            GreatEqual,
            Inc,
            LessEqual,
            LogicalAnd,
            LogincalOr,
            NotEqual,
            ShiftArithLeft,
            ShiftArithRight,
            HashHash,
            Ellipsis,
        };

        /// <summary>
        /// トークンに割り当てるユニークなID生成用カウンタ
        /// </summary>
        private static ulong _uniqueIdCount = 0;

        /// <summary>
        /// トークンに割り当てるユニークなID
        /// </summary>
        public ulong Id { get; }

        public TokenKind Kind { get; }

        public File File { get; set; }

        public Position Pos { get; set; }

        public SpaceInfo Space { get; set; }

        /// <summary>
        /// トークンの前に仮想的な空白が挿入された場合 true になる
        /// </summary>
        public bool HeadSpace { get; set; }

        /// <summary>
        /// トークンの後ろに仮想的な空白が挿入された場合 true になる
        /// </summary>
        public bool TailSpace { get; set; }

        /// <summary>
        /// マクロ展開を禁止するフラグ。
        /// 未対応プラグマなどプリプロセッサを素通しする必要があるトークンの場合に true とする。
        /// </summary>
        public bool Verbatim { get; set; }

        /// <summary>
        /// トークンが行頭にある場合trueになる
        /// </summary>
        public bool BeginOfLine { get; set; }

        /// <summary>
        /// ファイルから読み取ったトークンに割り当てられる番号
        /// </summary>
        public int IndexOfFile { get; private set; } // token number in a file, counting from 0.

        public Set Hideset { get; set; }  // used by the preprocessor for macro expansion

        //Keyword
        public Keyword KeywordVal { get; set; }

        //StringOrCharOrNumberOrIdent
        public string StrVal { get; set; }

        // MacroParam 
        public bool IsVarArg { get; private set; }
        public int Position { get; private set; }
        public string ArgName { get; private set; }

        // MacroParamRef
        public Token MacroParamRef { get; set; }

        // MacroRangeFixup
        public Macro MacroRangeFixupMacro;
        public Token MacroRangeFixupTok;
        public int MacroRangeFixupStartLine, MacroRangeFixupStartColumn;

        private Token(Token.TokenKind kind) {
            Id = ++_uniqueIdCount;
            Space = new SpaceInfo();
            Kind = kind;
        }

        public Token(Token tmpl, Token.TokenKind kind) {
            Id = ++_uniqueIdCount;
            Kind = kind;
            File = tmpl.File;
            Pos = tmpl.Pos;
            Space = tmpl.Space;
            HeadSpace = tmpl.HeadSpace;
            TailSpace = tmpl.TailSpace;
            BeginOfLine = tmpl.BeginOfLine;
            IndexOfFile = tmpl.IndexOfFile;
            Hideset = tmpl.Hideset;

            //Keyword
            KeywordVal = tmpl.KeywordVal;

            //StringOrChar
            StrVal = tmpl.StrVal;

            // MacroParam 
            IsVarArg = tmpl.IsVarArg;
            Position = tmpl.Position;
            ArgName = tmpl.ArgName;

            // MacroParamRef
            MacroParamRef = tmpl.MacroParamRef;

            // MacroRangeFixup
            MacroRangeFixupMacro = tmpl.MacroRangeFixupMacro;
            MacroRangeFixupTok = tmpl.MacroRangeFixupTok;
            MacroRangeFixupStartLine = tmpl.MacroRangeFixupStartLine;
            MacroRangeFixupStartColumn = tmpl.MacroRangeFixupStartColumn;

        }

        public static Token make_macro_token(int position, bool isVarArg, string argName, File file) {
            return new Token(TokenKind.MacroParam) {
                Hideset = null,
                File = file,
                IndexOfFile = -1,
                Pos = new Position(file.Name, file.Line, file.Column),

                // MacroParam
                IsVarArg = isVarArg,
                Position = position,
                ArgName = argName,
            };
        }

        /// <summary>
        /// 識別子トークンを作る
        /// </summary>
        /// <param name="s">識別子名</param>
        /// <returns></returns>
        public static Token make_ident(Position pos, string s) {
            return new Token(Token.TokenKind.Ident) {
                // common
                Hideset = null,
                File = File.current_file(),
                IndexOfFile = File.current_file().NumOfToken++,
                Pos = pos,

                // Ident
                StrVal = s,
            };
        }

        /// <summary>
        /// 文字列トークンを作る
        /// </summary>
        /// <param name="s">文字列値</param>
        /// <returns></returns>
        public static Token make_strtok(Position pos, string s) {
            return new Token(Token.TokenKind.String) {
                // common
                Hideset = null,
                File = File.current_file(),
                IndexOfFile = File.current_file().NumOfToken++,
                Pos = pos,

                // String
                StrVal = s,
            };
        }

        /// <summary>
        /// キーワード
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static Token make_keyword(Position pos, Token.Keyword id) {
            return new Token(Token.TokenKind.Keyword) {
                // common
                Hideset = null,
                File = File.current_file(),
                IndexOfFile = File.current_file().NumOfToken++,
                Pos = pos,

                // Char
                KeywordVal = id,
            };
        }

        public static Token make_number(Position pos, string s) {
            return new Token(Token.TokenKind.Number) {
                // common
                Hideset = null,
                File = File.current_file(),
                IndexOfFile = File.current_file().NumOfToken++,
                Pos = pos,

                // Number
                StrVal = s,
            };
        }

        public static Token make_invalid(Position pos, string s) {
            return new Token(Token.TokenKind.Invalid) {
                // common
                Hideset = null,
                File = File.current_file(),
                IndexOfFile = File.current_file().NumOfToken++,
                Pos = pos,

                // Invalid
                StrVal = s,
            };
        }

        public static Token make_char(Position pos, string s) {
            return new Token(Token.TokenKind.Char) {
                // common
                Hideset = null,
                File = File.current_file(),
                IndexOfFile = File.current_file().NumOfToken++,
                Pos = pos,

                // Invalid
                StrVal = s,
            };
        }

        public static Token make_space(Position pos, SpaceInfo sb) {
            return new Token(Token.TokenKind.Space) {
                //common 
                Hideset = null,
                File = null,
                IndexOfFile = -1,
                Pos = pos,

                // Space
                Space = sb
            };
        }

        public static Token make_newline(Position pos) {
            return new Token(Token.TokenKind.NewLine) {
                //common 
                Hideset = null,
                File = File.current_file(),
                IndexOfFile = -1,
                Pos = pos,

                // NewLine
            };
        }

        public static Token make_eof(Position pos) {
            return new Token(Token.TokenKind.EoF) {
                //common 
                Hideset = null,
                File = File.current_file(),
                IndexOfFile = -1,
                Pos = pos,

                // EoF
            };
        }
        public static Token make_MacroRangeFixup(Position pos, Token tok, Macro mac, int startline, int startcolumn) {
            return new Token(Token.TokenKind.MacroRangeFixup) {
                //common 
                Hideset = null,
                File = File.current_file(),
                IndexOfFile = -1,
                Pos = pos,

                // MacroRangeFixup
                MacroRangeFixupTok = tok,
                MacroRangeFixupMacro = mac,
                MacroRangeFixupStartLine = startline,
                MacroRangeFixupStartColumn = startcolumn
            };
        }

        public bool IsIdent(params string[] s) {
            return Kind == TokenKind.Ident && s.Contains(StrVal);
        }

        public bool IsKeyword(Keyword c) {
            return Kind == TokenKind.Keyword && KeywordVal == c;
        }
        public bool IsKeyword(char c) {
            return Kind == TokenKind.Keyword && KeywordVal == (Keyword)c;
        }

        public static string TokenToStr(Token tok) {
            if (tok == null)
                return "(null)";
            switch (tok.Kind) {
                case TokenKind.Ident:
                    return tok.StrVal;
                case TokenKind.Keyword:
                    return KeywordToStr(tok.KeywordVal);
                case TokenKind.Char:
                    return $"'{tok.StrVal}'";
                case TokenKind.Number:
                    return tok.StrVal;
                case TokenKind.String:
                    return $"\"{tok.StrVal}\"";
                case TokenKind.EoF:
                    return "(eof)";
                case TokenKind.Invalid:
                    return $"{tok.StrVal}";
                case TokenKind.NewLine:
                    return "\n";
                case TokenKind.Space:
                    return "";
                case TokenKind.MacroParam:
                    return tok.ArgName;
                case TokenKind.MacroParamRef:
                    return $"(macro-param-ref {tok.MacroParamRef.ArgName})";
            }
            CppContext.InternalError(tok, $"トークン種別が不正な値です。(tok.Kind={tok.Kind})");
            return "";
        }

        public static string KeywordToStr(Keyword id) {
            switch (id) {
                case Keyword.Arrow:
                    return "->";
                case Keyword.AssignAdd:
                    return "+=";
                case Keyword.AssignAnd:
                    return "&=";
                case Keyword.AssignDiv:
                    return "/=";
                case Keyword.AssignMod:
                    return "%=";
                case Keyword.AssignMul:
                    return "*=";
                case Keyword.AssignOr:
                    return "|=";
                case Keyword.AssignShiftArithLeft:
                    return "<<=";
                case Keyword.AssignShiftArithRight:
                    return ">>=";
                case Keyword.AssignSub:
                    return "-=";
                case Keyword.AssignXor:
                    return "^=";
                case Keyword.Dec:
                    return "--";
                case Keyword.Equal:
                    return "==";
                case Keyword.GreatEqual:
                    return ">=";
                case Keyword.Inc:
                    return "++";
                case Keyword.LessEqual:
                    return "<=";
                case Keyword.LogicalAnd:
                    return "&&";
                case Keyword.LogincalOr:
                    return "||";
                case Keyword.NotEqual:
                    return "!=";
                case Keyword.ShiftArithLeft:
                    return "<<";
                case Keyword.ShiftArithRight:
                    return ">>";
                case Keyword.HashHash:
                    return "##";
                case Keyword.Ellipsis:
                    return "...";
                default:
                    return $"{(char)id}";
            }
        }

        private static string QuoteChar(string charString) {
            return charString;
        }

        private static string QuoteStr(string charString) {
            return charString;
        }

        private static readonly string HexmalPattern = @"(?:0x(?<hex>[0-9A-Fa-f]+))";
        private static readonly string OctetPattern = @"(?<oct>0[0-7]*)";
        private static readonly string DigitPattern = @"(?<dig>[0-9]+)";
        private static readonly string SuffixPattern = @"(?<suffix>U|L|UL|LU|LL|ULL|LUL|LLU)";
        private static readonly string NumberPattern = $@"^(?:{HexmalPattern}|{OctetPattern}|{DigitPattern}){SuffixPattern}?$";
        private static readonly Regex RegexIntegerNumberPattern = new Regex(NumberPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);


        /* from http://www.quut.com/c/ANSI-C-grammar-l.html */
        private static readonly string D = $@"\d";
        private static readonly string E = $@"([Ee][+-]?{D}+)";
        private static readonly string FS = $@"(f|F|l|L)";
        private static readonly string P = $@"([Pp][+-]?{D}+)";
        private static readonly string HP = $@"(0[xX])";
        private static readonly string H = $@"[a-fA-F0-9]";
        private static readonly string[] FloatingNumberPatterns = new[] {
            $@"{D}+{E}{FS}?",
            $@"{D}*\.{D}+{E}?{FS}?",
            $@"{D}+\.{E}?{FS}?",
            $@"{HP}{H}+{P}{FS}?",
            $@"{HP}{H}*\.{H}+{P}{FS}?",
            $@"{HP}{H}+\.{P}{FS}?",
        };
        private static readonly Regex RegexFloatingNumberPattern = new Regex("^" + String.Join("|", FloatingNumberPatterns.Select(x => $"(?:{x})")) + "$", RegexOptions.Compiled);

        public static IntMaxT ToInt(Token t, string s) {
            BigInteger value;

            Match m = RegexIntegerNumberPattern.Match(s);
            if (m.Groups["hex"].Success) {
                // 十六進数
                // BigInteger.ParseとSystem.Globalization.NumberStyles.HexNumberを組み合わせると
                // 最上位ビットが立っている場合は負数と見なす動作になるので対策として先頭に`0`を付与して解析
                value = BigInteger.Parse("0" + m.Groups["hex"].Value, System.Globalization.NumberStyles.HexNumber);
            } else if (m.Groups["oct"].Success) {
                // 八進数
                value = m.Groups["oct"].Value.Aggregate(new BigInteger(), (b, c) => b * 8 + c - '0');
            } else if (m.Groups["dig"].Success) {
                // 十進数
                value = BigInteger.Parse(m.Groups["dig"].Value);
            } else {
                // 整数値として不正なもの。浮動小数点数かどうか判定してメッセージを変化させる
                if (RegexFloatingNumberPattern.IsMatch(s)) {
                    CppContext.Error(t, $"プリプロセス指令の条件式中で浮動小数点定数 {s} が使われています。条件式中で使える定数は整数定値のみです。");
                } else {
                    CppContext.Error(t, $"{s} は整数値として不正な書式です。");
                }
                return IntMaxT.CreateSigned(0);
            }

            string suffix = "";
            if (m.Groups["suffix"].Success) {
                suffix = m.Groups[2].Value;
            }

            bool isUnsigned = suffix.ToUpper().Contains('U');    // 今は使っていないが将来的に使う予定がある
            int size = suffix.Count(x => x == 'L' || x == 'l');   // 0 is int, 1 is long, 2 is long long

            // 値は uintmax_t として読み取る

            if (CppContext.Features.Contains(Feature.LongLongConstant) == false) {
                if (size == 2) {
                    CppContext.Error(t, $"64ビット型の定数値 `{s}` が使われています。64ビット型の定数値は ISO/IEC 9899-1999 以降で利用可能となった言語機能です。64ビット型の定数値を有効にする場合は実行時引数に -FLongLongConstant を設定してください。");
                }
            } else if (CppContext.Warnings.Contains(Warning.LongLongConstant)) {
                if (size == 2) {
                    CppContext.Warning(t, $"64ビット型の定数値 `{s}` が使われています。");
                }
            }

            if (isUnsigned || value > IntMaxT.SignedMaxValue) {
                if (IntMaxT.UnsignedMaxValue < value) {
                    CppContext.Warning(t, $"定数 `{s}` は uintmax_t の範囲を超えています。");
                }
                return IntMaxT.CreateUnsigned((ulong)(value & IntMaxT.UnsignedMaxValue));
            } else {
                return IntMaxT.CreateSigned((long)(value & IntMaxT.UnsignedMaxValue));
            }

        }

    }
}