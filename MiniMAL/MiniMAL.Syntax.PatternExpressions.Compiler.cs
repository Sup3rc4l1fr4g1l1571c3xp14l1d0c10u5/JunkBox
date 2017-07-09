using System;
using System.Linq;

namespace MiniMAL
{
    namespace Syntax
    {
        public abstract partial class PatternExpressions
        {
            /// <summary>
            /// パターン式コンパイラ
            /// </summary>
            public static class Compiler
            {
                private static Expressions Call(string name, params Expressions[] args)
                {
                    return args.Aggregate((Expressions)new Expressions.Var(name), (s, x) => new Expressions.AppExp(s, x));
                }

                private static Expressions GenEq(Expressions lhs, Expressions rhs)
                {
                    return Call("=", lhs, rhs);
                }
                private static Expressions GenNe(Expressions lhs, Expressions rhs)
                {
                    return Call("<>", lhs, rhs);
                }

                public static Expressions Compile(Expressions value, PatternExpressions pattern, Expressions action)
                {
                    if (pattern is WildP)
                    {
                        return action;
                    }
                    if (pattern is VarP)
                    {
                        var p = (VarP)pattern;
                        // $"(LetExp [((Var {p.Id}), {target})] {code})";
                        return new Expressions.LetExp(new[] { Tuple.Create(p.Id, value) }, action);
                    }
                    if (pattern is IntP)
                    {
                        var p = (IntP)pattern;
                        // $"(IfExp (BuiltinOp Eq {p.Value} {target}) {code} None)";
                        return new Expressions.IfExp(
                            GenEq(new Expressions.IntLit(p.Value), value),
                            action,
                            Expressions.OptionExp.None
                        );
                    }
                    if (pattern is StrP)
                    {
                        var p = (StrP)pattern;
                        // $"(IfExp (BuiltinOp Eq {p.Value} {target}) {code} None)";
                        return new Expressions.IfExp(
                            GenEq(new Expressions.StrLit(p.Value), value),
                            action,
                            Expressions.OptionExp.None
                        );
                    }
                    if (pattern is BoolP)
                    {
                        var p = (BoolP)pattern;
                        // $"(IfExp (BuiltinOp Eq {p.Value} {target}) {code} None)";
                        return new Expressions.IfExp(
                            GenEq(new Expressions.BoolLit(p.Value), value),
                            action,
                            Expressions.OptionExp.None
                        );
                    }
                    if (pattern is UnitP)
                    {
                        return new Expressions.IfExp(
                            GenEq(new Expressions.UnitLit(), value),
                            action,
                            Expressions.OptionExp.None
                        );
                    }
                    if (pattern is ConsP)
                    {
                        if (pattern == ConsP.Empty)
                        {
                            return new Expressions.IfExp(
                                GenEq(new Expressions.EmptyListLit(), value),
                                action,
                                Expressions.OptionExp.None
                            );
                        }
                        else
                        {
                            var p = (ConsP)pattern;
                            var x = p.Value;
                            var xs = p.Next;
                            var varX = Expressions.Var.Fresh();
                            var varXs = Expressions.Var.Fresh();
                            var codeXs = Compile(varXs, xs, action);
                            var codeX = Compile(varX, x, codeXs);
                            // $"(IfExp (IsCons {target}) (IfExp (BuiltinOp Eq UnitLit {target}) None (LetExp[({targetx}, (Head {target})); ({targetxs}, (Tail {target}))] {code__})) None)";
                            return new Expressions.IfExp(
                                GenEq(new Expressions.UnitLit(), value),
                                Expressions.OptionExp.None,
                                new Expressions.LetExp(
                                    new[] {
                                        Tuple.Create(varX.Id, Call("head", value)),
                                        Tuple.Create(varXs.Id, Call("tail", value))
                                    },
                                    codeX
                                )
                            );
                        }
                    }
                    if (pattern is TupleP)
                    {
                        var p = (TupleP)pattern;
                        // $"(IfExp (AppExp tuple? {target}) 
                        //          (IfExp (BuiltinOp Ne (AppExp count? {this}) (IntLit {pattern.Length}))
                        //                 None
                        //                 (code) 
                        //          )
                        //   )"

                        var body = p.Members.Reverse().Aggregate(Tuple.Create(action, p.Members.Length - 1), (s, x) =>
                        {
                            var tmp = Expressions.Var.Fresh();
                            var expr = new Expressions.LetExp(
                                new[] {
                                    Tuple.Create(tmp.Id, Call("field", value, new Expressions.IntLit(s.Item2))),
                                },
                                Compile(tmp, x, s.Item1)
                            );
                            return Tuple.Create((Expressions)expr, s.Item2 - 1);
                        });

                        return new Expressions.IfExp(
                            Call("istuple", value),
                            new Expressions.IfExp(
                                GenNe(new Expressions.IntLit(p.Members.Length), Call("size", value)),
                                Expressions.OptionExp.None,
                                body.Item1
                            ),
                            Expressions.OptionExp.None
                        );
                    }
                    throw new NotSupportedException(pattern.ToString());
                }
            }

        }
    }

}
