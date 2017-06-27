using System;
using System.Linq;

namespace MiniMAL
{
    /// <summary>
    /// 宣言
    /// </summary>
    public abstract class Declarations {
        public class Empty : Declarations {
            public Empty() {
            }
            public override string ToString() {
                return "";
            }
        }
        public class Exp : Declarations {
            public Exp(Expressions syntax) {
                Syntax = syntax;
            }

            public Expressions Syntax { get; }
            public override string ToString() {
                return Syntax.ToString() + ";;";
            }
        }
        public class Decls : Declarations {
            public abstract class DeclBase {
                protected DeclBase(Tuple<string, Expressions>[] binds) {
                    Binds = binds;
                }

                public Tuple<string, Expressions>[] Binds { get; }
                public override string ToString() {
                    return String.Join(" and ", Binds.Select(x => $"{x.Item1} = {x.Item2}"));
                }
            }

            public class Decl : DeclBase {
                public Decl(Tuple<string, Expressions>[] binds) : base(binds) { }
                public override string ToString() {
                    return $"let {base.ToString()}";
                }
            }

            public class RecDecl : DeclBase {
                public RecDecl(Tuple<string, Expressions>[] binds) : base(binds) { }
                public override string ToString() {
                    return $"let rec {base.ToString()}";
                }
            }

            public Decls(DeclBase[] entries) {
                Entries = entries;
            }

            public DeclBase[] Entries { get; }
            public override string ToString() {
                return String.Join(" and ", Entries.Select(x => x.ToString())) + ";;";
            }
        }
    }
}
