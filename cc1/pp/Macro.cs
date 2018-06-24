using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CSCPP {
    /// <summary>
    /// 宣言されたマクロを示す基底クラス
    /// </summary>
    public abstract class Macro {
        private static long UniqueIdCount {
            get; set;
        }
        public long UniqueId {
            get;
        }
        public bool Used { get; set; } = false;

        public abstract string GetName();
        public abstract Position GetFirstPosition();
        public abstract Position GetLastPosition();

        protected Macro() {
            UniqueId = UniqueIdCount++;
        }
        public static bool IsObjectMacro(Macro macro) {
            return macro is ObjectMacro;
        }

        public static bool IsFuncMacro(Macro macro) {
            return macro is FuncMacro;
        }

        public static bool IsBuildinMacro(Macro macro) {
            return macro is BuildinMacro;
        }

        public static bool EqualDefine(Macro o, Macro n) {
            if (o is ObjectMacro && n is ObjectMacro) {
                return ObjectMacro.EqualDefine((ObjectMacro)o, (ObjectMacro)n);
            }
            if (o is FuncMacro && n is FuncMacro) {
                return FuncMacro.EqualDefine((FuncMacro)o, (FuncMacro)n);
            }
            // BuildinMacroについては比較不能とする
            return false;
        }

        public static class LambdaComparer {
            private class LambdaComparerImpl<T> : IEqualityComparer<T> {
                private Func<T, T, bool> EqualsPred {
                    get;
                }
                private Func<T, int> GetHashCodePred {
                    get;
                }
                public bool Equals(T x, T y) {
                    return EqualsPred(x, y);
                }

                public int GetHashCode(T obj) {
                    return GetHashCodePred(obj);
                }

                protected LambdaComparerImpl(Func<T, T, bool> equalsPred, Func<T, int> getHashCodePred) {
                    EqualsPred = equalsPred;
                    GetHashCodePred = getHashCodePred;
                }
                public static IEqualityComparer<T> Create(Func<T, T, bool> equalsPred, Func<T, int> getHashCodePred) {
                    return new LambdaComparerImpl<T>(
                        equalsPred,
                        getHashCodePred ?? new Func<T, int>((x) => 0)
                    );
                }
            }
            public static IEqualityComparer<T> Create<T>(Func<T, T, bool> equalsPred) {
                return LambdaComparerImpl<T>.Create(equalsPred, null);
            }
            public static IEqualityComparer<T> Create<T>(Func<T, T, bool> equalsPred, Func<T, int> getHashCodePred) {
                return LambdaComparerImpl<T>.Create(equalsPred, getHashCodePred);
            }
        }

        protected static bool CompareToken(Token x, Token y) {
            if (x.Kind != y.Kind) {
                return false;
            }
            if (x.Space.Any() != y.Space.Any()) {
                return false;
            }
            switch (x.Kind) {
                case Token.TokenKind.Keyword:
                    return x.KeywordVal == y.KeywordVal;
                case Token.TokenKind.String:
                    return x.StrVal == y.StrVal;
                case Token.TokenKind.Number:
                    return x.StrVal == y.StrVal;
                case Token.TokenKind.Char:
                    return x.StrVal == y.StrVal;
                case Token.TokenKind.MacroParam:
                    if (x.IsVarArg == y.IsVarArg) {
                        return (x.ArgIndex == y.ArgIndex);
                    } else {
                        return (x.ArgIndex == y.ArgIndex) && (x.ArgName == y.ArgName);
                    }
                case Token.TokenKind.MacroParamRef:
                    return CompareToken(x.MacroParamRef, y.MacroParamRef);
                default:
                    return false;
            }
        }

        /// <summary>
        /// 定数型マクロ
        /// </summary>
        public class ObjectMacro : Macro {
            public Token Name {
                get;
            }
            public List<Token> Body {
                get;
            }
            public ObjectMacro(Token name, List<Token> body) {
                Name = name;
                Body = new List<Token>(body);
            }

            public override string GetName() {
                return Name.StrVal;
            }

            public override Position GetFirstPosition() {
                return Name.Position;
            }
            public override Position GetLastPosition() {
                var last = Body.LastOrDefault();
                if (last != null) {
                    var p = last.Position;
                    return new CSCPP.Position(p.FileName, p.Line, p.Column + last.ToRawString().Length);
                } else {
                    var p = Name.Position;
                    return new CSCPP.Position(p.FileName, p.Line, p.Column + Name.StrVal.Length);
                }
            }

            public override string ToString() {
                var sb = new StringBuilder();
                sb.AppendLine($"<ObjectMacro id='{UniqueId}' name='{Name.StrVal}'>");
                sb.AppendLine("<Body>");
                Body.ForEach(x => sb.AppendLine(x.ToString()));
                sb.AppendLine("</Body>");
                sb.AppendLine(
                    "</ObjectMacro>");
                return sb.ToString();
            }
            public static bool EqualDefine(ObjectMacro o, ObjectMacro n) {
                if (o.Name.StrVal != n.Name.StrVal) {
                    return false;
                }
                return o.Body.SequenceEqual(n.Body, LambdaComparer.Create<Token>(CompareToken));
            }

        }

        /// <summary>
        /// 関数型マクロ
        /// </summary>
        public class FuncMacro : Macro {
            public Token Name {
                get;
            }

            /// <summary>
            /// マクロ引数(あとでFixupするのでreadonlyにはしない)
            /// </summary>
            public List<Token> Args;

            /// <summary>
            /// マクロ本体(あとでFixupするのでreadonlyにはしない)
            /// </summary>
            public List<Token> Body;

            /// <summary>
            /// 可変長引数を持つかどうか(あとでFixupするのでreadonlyにはしない)
            /// </summary>
            public bool IsVarg;

            /// <summary>
            /// 宣言の末尾のトークン(あとでFixupするのでreadonlyにはしない)
            /// </summary>
            public Token LastToken;

            public FuncMacro(Token name, List<Token> body, List<Token> args, bool isVarg) {
                Name = name;
                Args = args;
                Body = body?.ToList();
                IsVarg = isVarg;
            }

            public override string GetName() {
                return Name.StrVal;
            }

            public override Position GetFirstPosition() {
                return Name.Position;
            }
            public override Position GetLastPosition() {
                var p = LastToken.Position;
                return new CSCPP.Position(p.FileName, p.Line, p.Column + LastToken.ToRawString().Length);
            }

            public override string ToString() {
                var sb = new StringBuilder();
                sb.AppendLine($"<FuncMacro id='{UniqueId}' name='{Name.StrVal}'>");
                sb.AppendLine("<Args>");
                Args.ForEach(x => sb.AppendLine(x.ToString()));
                sb.AppendLine("</Args>");
                sb.AppendLine("<Body>");
                Body.ForEach(x => sb.AppendLine(x.ToString()));
                sb.AppendLine("</Body>");
                sb.AppendLine("</FuncMacro>");
                return sb.ToString();
            }

            public static bool EqualDefine(FuncMacro o, FuncMacro n) {
                if (o.Name.StrVal != n.Name.StrVal) {
                    return false;
                }
                if (o.IsVarg != n.IsVarg) {
                    return false;
                }
                return o.Args.SequenceEqual(n.Args, LambdaComparer.Create<Token>(CompareToken)) &&
                       o.Body.SequenceEqual(n.Body, LambdaComparer.Create<Token>(CompareToken));
            }

        }

        /// <summary>
        /// ビルトインマクロ
        /// </summary>
        public class BuildinMacro : Macro {
            public delegate List<Token> BuiltinMacroHandler(BuildinMacro m, Token tok);
            public string Name {
                get;
            }
            public BuiltinMacroHandler Hander {
                get;
            }
            public BuildinMacro(string name, BuiltinMacroHandler hander) {
                Name = name;
                Hander = hander;
            }
            public override string GetName() {
                return Name;
            }
            public override Position GetFirstPosition() {
                return new Position("<builtin>", 1, 1);
            }
            public override Position GetLastPosition() {
                return new Position("<builtin>", 1, 1);
            }

            public override string ToString() {
                var sb = new StringBuilder();
                sb.AppendLine($"<BuildinMacro id='{UniqueId}' name='{Name}' />");
                return sb.ToString();
            }

        }
    }
}