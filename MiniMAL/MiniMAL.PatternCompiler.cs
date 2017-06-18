using System;
using System.Linq;

namespace MiniMAL
{

    /// <summary>
    /// パターン式コンパイラ
    /// </summary>
    public static class PatternCompiler {
        private static int anonymousV = 0;
        private static string GenAnonymousVariable() { return $"@{(++anonymousV)}"; }

        public static Expressions CompilePattern(Expressions value, PatternExpressions pattern, Expressions action) {
            if (pattern is PatternExpressions.WildP) {
                return action;
            }
            if (pattern is PatternExpressions.VarP) {
                var p = pattern as PatternExpressions.VarP;
                // $"(LetExp [((Var {p.Id}), {target})] {code})";
                return new Expressions.LetExp(new[] { Tuple.Create(p.Id, value) }, action);
            }
            if (pattern is PatternExpressions.IntP) {
                var p = pattern as PatternExpressions.IntP;
                // $"(IfExp (BuiltinOp Eq {p.Value} {target}) {code} None)";
                return new Expressions.IfExp(
                    new Expressions.BuiltinOp(Expressions.BuiltinOp.Kind.Eq, new Expressions[] { new Expressions.IntLit(p.Value), value}),
                    action,
                    new Expressions.NilLit()
                );
            }
            if (pattern is PatternExpressions.StrP) {
                var p = pattern as PatternExpressions.StrP;
                // $"(IfExp (BuiltinOp Eq {p.Value} {target}) {code} None)";
                return new Expressions.IfExp(
                    new Expressions.BuiltinOp(Expressions.BuiltinOp.Kind.Eq, new Expressions[] { new Expressions.StrLit(p.Value), value}),
                    action,
                    new Expressions.NilLit()
                );
            }
            if (pattern is PatternExpressions.BoolP) {
                var p = pattern as PatternExpressions.BoolP;
                // $"(IfExp (BuiltinOp Eq {p.Value} {target}) {code} None)";
                return new Expressions.IfExp(
                    new Expressions.BuiltinOp(Expressions.BuiltinOp.Kind.Eq, new Expressions[] { new Expressions.BoolLit(p.Value), value}),
                    action,
                    new Expressions.NilLit()
                );
            }
            if (pattern is PatternExpressions.UnitP) {
                return new Expressions.IfExp(
                    new Expressions.BuiltinOp(Expressions.BuiltinOp.Kind.Eq, new Expressions[] { new Expressions.UnitLit(), value}),
                    action,
                    new Expressions.NilLit()
                );
            }
            if (pattern is PatternExpressions.ConsP) {
                if (pattern == PatternExpressions.ConsP.Empty) {
                    return new Expressions.IfExp(
                        new Expressions.BuiltinOp(Expressions.BuiltinOp.Kind.Eq, new Expressions[] { new Expressions.EmptyListLit(), value}),
                        action,
                        new Expressions.NilLit()
                    );
                } else {
                    var p = pattern as PatternExpressions.ConsP;
                    var x = p.Value;
                    var xs = p.Next;
                    var var_x = new Expressions.Var(GenAnonymousVariable());
                    var var_xs = new Expressions.Var(GenAnonymousVariable());
                    var code_ = CompilePattern(var_xs, xs, action);
                    var code__ = CompilePattern(var_x, x, code_);
                    // $"(IfExp (IsCons {target}) (IfExp (BuiltinOp Eq UnitLit {target}) None (LetExp[({targetx}, (Head {target})); ({targetxs}, (Tail {target}))] {code__})) None)";
                    return new Expressions.IfExp(
                        new Expressions.BuiltinOp(Expressions.BuiltinOp.Kind.IsCons, new[] { value }),
                        new Expressions.IfExp(
                            new Expressions.BuiltinOp(Expressions.BuiltinOp.Kind.Eq, new Expressions[] { new Expressions.UnitLit(), value}),
                            new Expressions.NilLit(),
                            new Expressions.LetExp(
                                new[] {
                                    Tuple.Create<string,Expressions>(var_x.Id, new Expressions.BuiltinOp(Expressions.BuiltinOp.Kind.Head, new [] { value })),
                                    Tuple.Create<string,Expressions>(var_xs.Id, new Expressions.BuiltinOp(Expressions.BuiltinOp.Kind.Tail, new [] { value }))
                                },
                                code__
                            )
                        ),
                        new Expressions.NilLit()
                    );
                }
            }
            if (pattern is PatternExpressions.TupleP) {
                var p = pattern as PatternExpressions.TupleP;
                // $"(IfExp (AppExp tuple? {target}) 
                //          (IfExp (BuiltinOp Ne (AppExp count? {this}) (IntLit {pattern.Length}))
                //                 None
                //                 (code) 
                //          )
                //   )"

                var body = p.Value.Reverse<PatternExpressions>().Aggregate(Tuple.Create((Expressions)action, p.Value.Length - 1), (s, x) => {
                    var tmp = new Expressions.Var(GenAnonymousVariable());
                    var expr = new Expressions.LetExp(
                        new[] {
                            Tuple.Create<string,Expressions>(tmp.Id, new Expressions.BuiltinOp(Expressions.BuiltinOp.Kind.Nth, new [] { new Expressions.IntLit(s.Item2), value })),
                        },
                        CompilePattern(tmp, x, s.Item1)
                    );
                    return Tuple.Create((Expressions)expr, s.Item2 - 1);
                });

                return new Expressions.IfExp(
                    new Expressions.BuiltinOp(Expressions.BuiltinOp.Kind.IsTuple, new [] { value}),
                    new Expressions.IfExp(
                        new Expressions.BuiltinOp(Expressions.BuiltinOp.Kind.Ne, new Expressions[] { new Expressions.IntLit(p.Value.Length), new Expressions.BuiltinOp(Expressions.BuiltinOp.Kind.Length, new [] { value}), }),
                        new Expressions.NilLit(),
                        body.Item1
                    ),
                    new Expressions.NilLit()
                );
            }
            throw new Exception();
        }
    }
}
