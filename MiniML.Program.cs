using System;

namespace MiniML
{
    public static partial class MiniML
    {
        public abstract class Program {
            public class Exp : Program {
                public Exp(Syntax syntax) {
                    Syntax = syntax;
                }

                public Syntax Syntax { get; }
            }
            public class Decls : Program {
                public abstract class Decl { }

                public class LetDecl : Decl {
                    public LetDecl(Tuple<string, Syntax>[] binds) {
                        Binds = binds;
                    }

                    public Tuple<string, Syntax>[] Binds { get; }
                }
                public class LetRecDecl : Decl {
                    public LetRecDecl(Tuple<string, Syntax>[] binds) {
                        Binds = binds;
                    }

                    public Tuple<string, Syntax>[] Binds { get; }
                }
                public Decls(Decl[] entries) {
                    Entries = entries;
                }

                public Decl[] Entries { get; }
            }
        }
    }
}