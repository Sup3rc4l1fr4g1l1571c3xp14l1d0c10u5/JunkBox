using System;
using System.Linq;

namespace MiniMAL
{
    namespace Syntax
    {

        /// <summary>
        /// 最上位要素
        /// </summary>
        public abstract class Toplevel {

            /// <summary>
            /// 空文(宣言も式も伴わない)
            /// </summary>
            public class Empty : Toplevel
            {
                public override string ToString()
                {
                    return "";
                }
            }

            /// <summary>
            /// 式
            /// </summary>
            public class Exp : Toplevel
            {
                public Expressions Syntax { get; }

                public Exp(Expressions syntax)
                {
                    Syntax = syntax;
                }

                public override string ToString()
                {
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
                public abstract class DeclBase
                {
                    public Tuple<string, Expressions>[] Binds { get; }

                    protected DeclBase(Tuple<string, Expressions>[] binds)
                    {
                        Binds = binds;
                    }

                    public override string ToString()
                    {
                        return String.Join(" and ", Binds.Select(x => $"{x.Item1} = {x.Item2}"));
                    }

                    /// <summary>
                    /// let束縛
                    /// </summary>
                    public class LetDecl : DeclBase
                    {
                        public LetDecl(Tuple<string, Expressions>[] binds) : base(binds) { }

                        public override string ToString()
                        {
                            return $"let {base.ToString()}";
                        }
                    }


                    /// <summary>
                    /// let rec 束縛
                    /// </summary>
                    public class LetRecDecl : DeclBase
                    {
                        public LetRecDecl(Tuple<string, Expressions>[] binds) : base(binds) { }

                        public override string ToString()
                        {
                            return $"let rec {base.ToString()}";
                        }
                    }

                    public static Func<DeclBase, TResult> Match<TResult>(
                        Func<LetDecl, TResult> LetDecl,
                        Func<LetRecDecl, TResult> LetRecDecl,
                        Func<DeclBase, TResult> Other
                    )
                    {
                        return (obj) =>
                        {
                            if (obj is LetDecl) { return LetDecl((LetDecl)obj); }
                            if (obj is LetRecDecl) { return LetRecDecl((LetRecDecl)obj); }
                            return Other(obj);
                        };
                    }

                }

                /// <summary>
                /// 定義
                /// </summary>
                public DeclBase[] Entries { get; }

                public Binding(DeclBase[] entries)
                {
                    Entries = entries;
                }

                public override string ToString()
                {
                    return string.Join(" ", Entries.Select(x => x.ToString())) + ";;";
                }
            }

            /// <summary>
            /// 型宣言
            /// </summary>
            public class TypeDef : Toplevel
            {

                public TypeDef(string id, string[] vars, TypeExpressions type)
                {
                    Id = id;
                    Vars = vars;
                    Type = type;
                }

                public string Id { get; }
                public string[] Vars { get; }
                public TypeExpressions Type { get; }

                public override string ToString()
                {
                    var vs = (Vars.Any()) ? " (" + string.Join(", ", Vars) + ")" : "";
                    return $"type{vs} {Id} = {Type};;";
                }

            }

            /// <summary>
            /// 外部宣言
            /// </summary>
            public class ExternalDecl : Toplevel
            {
                public string Id { get; }
                public TypeExpressions Type { get; }
                public string Symbol { get; }

                public ExternalDecl(string id, TypeExpressions type, string symbol)
                {
                    Id = id;
                    Type = type;
                    Symbol = symbol;
                }
                public override string ToString()
                {
                    return $"external {Id} : {Type} = \"{Symbol}\";;";
                }
            }

            public static Func<Toplevel,TResult> Match<TResult>(
                Func<Empty, TResult> Empty,
                Func<Exp, TResult> Exp,
                Func<Binding, TResult> Binding,
                Func<TypeDef, TResult> TypeDef,
                Func<ExternalDecl, TResult> ExternalDecl,
                Func<Toplevel, TResult> Other
            )
            {
                return (obj) =>
                {
                    if (obj is Empty) { return Empty((Empty)obj); }
                    if (obj is Exp) { return Exp((Exp)obj); }
                    if (obj is Binding) { return Binding((Binding)obj); }
                    if (obj is TypeDef) { return TypeDef((TypeDef)obj); }
                    if (obj is ExternalDecl) { return ExternalDecl((ExternalDecl)obj); }
                    return Other(obj);
                };
            }

        }

    }

}