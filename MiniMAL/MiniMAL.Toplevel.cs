using System;
using System.Linq;

namespace MiniMAL {

    /// <summary>
    /// 最上位要素
    /// </summary>
    public abstract class Toplevel {

        /// <summary>
        /// 空文(宣言も式も伴わない)
        /// </summary>
        public class Empty : Toplevel {
            public override string ToString() {
                return "";
            }
        }

        /// <summary>
        /// 式
        /// </summary>
        public class Exp : Toplevel {
            public Expressions Syntax { get; }

            public Exp(Expressions syntax) {
                Syntax = syntax;
            }

            public override string ToString() {
                return Syntax + ";;";
            }
        }

        /// <summary>
        /// 束縛
        /// </summary>
        public class Binding : Toplevel {

            /// <summary>
            /// 基底クラス
            /// </summary>
            public abstract class DeclBase {
                public Tuple<string, Expressions>[] Binds { get; }

                protected DeclBase(Tuple<string, Expressions>[] binds) {
                    Binds = binds;
                }

                public override string ToString() {
                    return string.Join(" and ", Binds.Select(x => $"{x.Item1} = {x.Item2}"));
                }
            }

            /// <summary>
            /// let束縛
            /// </summary>
            public class LetDecl : DeclBase {
                public LetDecl(Tuple<string, Expressions>[] binds) : base(binds) { }

                public override string ToString() {
                    return $"let {base.ToString()}";
                }
            }

            /// <summary>
            /// let rec 束縛
            /// </summary>
            public class LetRecDecl : DeclBase {
                public LetRecDecl(Tuple<string, Expressions>[] binds) : base(binds) { }

                public override string ToString() {
                    return $"let rec {base.ToString()}";
                }
            }

            /// <summary>
            /// 定義
            /// </summary>
            public DeclBase[] Entries { get; }

            public Binding(DeclBase[] entries) {
                Entries = entries;
            }

            public override string ToString() {
                return string.Join(" ", Entries.Select(x => x.ToString())) + ";;";
            }
        }

        public class TypeDef : Toplevel {

            public TypeDef(string id, string[] vars, TypeExp type) {
                Id = id;
                Vars = vars;
                Type = type;
            }

            public string Id { get; }
            public string[] Vars { get; }
            public TypeExp Type { get; }

            public override string ToString() {
                var vs = (Vars.Any()) ? " (" + string.Join(", ", Vars) +")" : "";
                return $"type{vs} {Id} = {Type};;";
            }

        }

        public class ExternalDecl : Toplevel {
            public string Id { get; }
            public TypeExp Type { get; }
            public string Symbol { get; }

            public ExternalDecl(string id, TypeExp type, string symbol) {
                Id = id;
                Type = type;
                Symbol = symbol;
            }
        }
    }

}