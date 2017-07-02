using System;
using System.Linq;
using System.Collections.Generic;

namespace MiniMAL
{

    /// <summary>
    /// パターン式コンパイラ
    /// </summary>
    public static class PatternCompiler {
        private static int _anonymousV;
        private static string GenAnonymousVariable() { return $"@{(++_anonymousV)}"; }

        public static Expressions CompilePattern(Expressions value, PatternExpressions pattern, Expressions action) {
            if (pattern is PatternExpressions.WildP) {
                return action;
            }
            if (pattern is PatternExpressions.VarP) {
                var p = (PatternExpressions.VarP) pattern;
                // $"(LetExp [((Var {p.Id}), {target})] {code})";
                return new Expressions.LetExp(new[] { Tuple.Create(p.Id, value) }, action);
            }
            if (pattern is PatternExpressions.IntP) {
                var p = (PatternExpressions.IntP) pattern;
                // $"(IfExp (BuiltinOp Eq {p.Value} {target}) {code} None)";
                return new Expressions.IfExp(
                    new Expressions.BuiltinOp(Expressions.BuiltinOp.Kind.Eq, new[] { new Expressions.IntLit(p.Value), value}),
                    action,
                    Expressions.OptionExp.None
                );
            }
            if (pattern is PatternExpressions.StrP) {
                var p = (PatternExpressions.StrP) pattern;
                // $"(IfExp (BuiltinOp Eq {p.Value} {target}) {code} None)";
                return new Expressions.IfExp(
                    new Expressions.BuiltinOp(Expressions.BuiltinOp.Kind.Eq, new[] { new Expressions.StrLit(p.Value), value}),
                    action,
                    Expressions.OptionExp.None
                );
            }
            if (pattern is PatternExpressions.BoolP) {
                var p = (PatternExpressions.BoolP) pattern;
                // $"(IfExp (BuiltinOp Eq {p.Value} {target}) {code} None)";
                return new Expressions.IfExp(
                    new Expressions.BuiltinOp(Expressions.BuiltinOp.Kind.Eq, new[] { new Expressions.BoolLit(p.Value), value}),
                    action,
                    Expressions.OptionExp.None
                );
            }
            if (pattern is PatternExpressions.UnitP) {
                return new Expressions.IfExp(
                    new Expressions.BuiltinOp(Expressions.BuiltinOp.Kind.Eq, new[] { new Expressions.UnitLit(), value}),
                    action,
                    Expressions.OptionExp.None
                );
            }
            if (pattern is PatternExpressions.ConsP) {
                if (pattern == PatternExpressions.ConsP.Empty) {
                    return new Expressions.IfExp(
                        new Expressions.BuiltinOp(Expressions.BuiltinOp.Kind.Eq, new[] { new Expressions.EmptyListLit(), value}),
                        action,
                        Expressions.OptionExp.None
                    );
                } else {
                    var p = (PatternExpressions.ConsP) pattern;
                    var x = p.Value;
                    var xs = p.Next;
                    var varX = new Expressions.Var(GenAnonymousVariable());
                    var varXs = new Expressions.Var(GenAnonymousVariable());
                    var codeXs = CompilePattern(varXs, xs, action);
                    var codeX = CompilePattern(varX, x, codeXs);
                    // $"(IfExp (IsCons {target}) (IfExp (BuiltinOp Eq UnitLit {target}) None (LetExp[({targetx}, (Head {target})); ({targetxs}, (Tail {target}))] {code__})) None)";
                    return new Expressions.IfExp(
                        new Expressions.BuiltinOp(Expressions.BuiltinOp.Kind.IsCons, new[] { value }),
                        new Expressions.IfExp(
                            new Expressions.BuiltinOp(Expressions.BuiltinOp.Kind.Eq, new[] { new Expressions.UnitLit(), value}),
                            Expressions.OptionExp.None,
                            new Expressions.LetExp(
                                new[] {
                                    Tuple.Create<string,Expressions>(varX.Id, new Expressions.BuiltinOp(Expressions.BuiltinOp.Kind.Head, new [] { value })),
                                    Tuple.Create<string,Expressions>(varXs.Id, new Expressions.BuiltinOp(Expressions.BuiltinOp.Kind.Tail, new [] { value }))
                                },
                                codeX
                            )
                        ),
                        Expressions.OptionExp.None
                    );
                }
            }
            if (pattern is PatternExpressions.TupleP) {
                var p = (PatternExpressions.TupleP) pattern;
                // $"(IfExp (AppExp tuple? {target}) 
                //          (IfExp (BuiltinOp Ne (AppExp count? {this}) (IntLit {pattern.Length}))
                //                 None
                //                 (code) 
                //          )
                //   )"

                var patterns = new List<PatternExpressions>();
                
                for (var it = p; !ReferenceEquals(it, PatternExpressions.TupleP.Tail); it = it.Cdr) {
                    patterns.Add(it.Car);
                }

                Func<int, Expressions, Expressions> wrapCar =
                    (cnt, val) => new Expressions.BuiltinOp(
                        Expressions.BuiltinOp.Kind.Car,
                        new [] { 
                        Enumerable.Repeat(0, cnt-1)
                                            .Aggregate(
                                                val, (s, x) => new Expressions.BuiltinOp(
                                                         Expressions.BuiltinOp.Kind.Cdr,
                                                         new[] {s})
                                  )}
                        );

                var body = patterns.Reverse<PatternExpressions>().Aggregate(Tuple.Create(action, patterns.Count), (s, x) => {
                    var tmp = new Expressions.Var(GenAnonymousVariable());
                    var expr = new Expressions.LetExp(
                        new[] {
                            Tuple.Create<string,Expressions>(tmp.Id, wrapCar(s.Item2, value)),
                        },
                        CompilePattern(tmp, x, s.Item1)
                    );
                    return Tuple.Create((Expressions)expr, s.Item2-1);
                });

                return new Expressions.IfExp(
                    new Expressions.BuiltinOp(Expressions.BuiltinOp.Kind.IsTuple, new [] { value}),
                    new Expressions.IfExp(
                        new Expressions.BuiltinOp(Expressions.BuiltinOp.Kind.Ne, new Expressions[] { new Expressions.IntLit(patterns.Count), new Expressions.BuiltinOp(Expressions.BuiltinOp.Kind.Length, new [] { value}), }),
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
