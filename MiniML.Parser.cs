using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml.Schema;
using Parsing;

namespace MiniML
{
    public static partial class MiniML
    {
        public static class Parser {
            /// <summary>
            /// 一つ以上の空白
            /// </summary>
            private static readonly Parser<string> WhiteSpace   = Combinator.Regex(@"\s+");

            /// <summary>
            /// ブロックコメント（ネスト可能）
            /// </summary>
            private static readonly Parser<string> BlockComment =
                from _1 in Combinator.Token(@"(*")
                from _2 in Combinator.Choice(Combinator.Not(Combinator.Token(@"(*")).Then(Combinator.AnyChar().Select(x => $"{x}")), Combinator.Lazy(() => BlockComment)).Many().Map(x => string.Join("",x))
                from _3 in Combinator.Token(@"*)")
                select _1 + 2 + 3
                ;

            /// <summary>
            /// トークン読み取り時に読み飛ばす空白
            /// </summary>
            private static readonly Parser<string> SkipWhiteSpace = Combinator.Choice(WhiteSpace, BlockComment).Many().Map(x => string.Join("", x));

            private static readonly Parser<string> Ident = SkipWhiteSpace.Then(Combinator.Regex(@"[a-z][a-z0-9_']*"));

            private static readonly Parser<string> Match = SkipWhiteSpace.Then(Ident.Just("match"));
            private static readonly Parser<string> With = SkipWhiteSpace.Then(Ident.Just("with"));
            private static readonly Parser<string> True = SkipWhiteSpace.Then(Ident.Just("true"));
            private static readonly Parser<string> False = SkipWhiteSpace.Then(Ident.Just("false"));
            private static readonly Parser<string> If = SkipWhiteSpace.Then(Ident.Just("if"));
            private static readonly Parser<string> Then = SkipWhiteSpace.Then(Ident.Just("then"));
            private static readonly Parser<string> Else = SkipWhiteSpace.Then(Ident.Just("else"));
            private static readonly Parser<string> Let = SkipWhiteSpace.Then(Ident.Just("let"));
            private static readonly Parser<string> Rec = SkipWhiteSpace.Then(Ident.Just("rec"));
            private static readonly Parser<string> In = SkipWhiteSpace.Then(Ident.Just("in"));
            private static readonly Parser<string> And = SkipWhiteSpace.Then(Ident.Just("and"));
            private static readonly Parser<string> Fun = SkipWhiteSpace.Then(Ident.Just("fun"));
            private static readonly Parser<string> DFun = SkipWhiteSpace.Then(Ident.Just("dfun"));

            private static readonly Parser<string> Id = Combinator.Choice(True,False,If,Then,Else,Let,Rec,In,And, Fun, DFun,Match,With).Not().Then(Ident);

            private static readonly Parser<int> IntV = SkipWhiteSpace.Then(Combinator.Regex(@"-?[0-9]+")).Select(int.Parse);
            private static readonly Parser<string> LParen = SkipWhiteSpace.Then(Combinator.Token("("));
            private static readonly Parser<string> RParen = SkipWhiteSpace.Then(Combinator.Token(")"));
            private static readonly Parser<string> SemiSemi = SkipWhiteSpace.Then(Combinator.Token(";;"));
            private static readonly Parser<string> Semi     = SkipWhiteSpace.Then(Combinator.Token(";"));
            private static readonly Parser<string> RArrow = SkipWhiteSpace.Then(Combinator.Token("->"));
            private static readonly Parser<string> Bar = SkipWhiteSpace.Then(Combinator.Token("|"));
            private static readonly Parser<string> Undarbar = SkipWhiteSpace.Then(Combinator.Token("_"));
            private static readonly Parser<string> LBracket = SkipWhiteSpace.Then(Combinator.Token("["));
            private static readonly Parser<string> RBracket = SkipWhiteSpace.Then(Combinator.Token("]"));
            private static readonly Parser<string> Comma    = SkipWhiteSpace.Then(Combinator.Token(","));
            private static readonly Parser<Syntax.BinOp.Kind> Plus = SkipWhiteSpace.Then(Combinator.Token("+")).Select(x => Syntax.BinOp.Kind.Plus);
            private static readonly Parser<Syntax.BinOp.Kind> Minus = SkipWhiteSpace.Then(Combinator.Token("-")).Select(x => Syntax.BinOp.Kind.Minus);
            private static readonly Parser<Syntax.BinOp.Kind> Mult = SkipWhiteSpace.Then(Combinator.Token("*")).Select(x => Syntax.BinOp.Kind.Mult);
            private static readonly Parser<Syntax.BinOp.Kind> Div = SkipWhiteSpace.Then(Combinator.Token("/")).Select(x => Syntax.BinOp.Kind.Div);
            private static readonly Parser<Syntax.BinOp.Kind> Lt = SkipWhiteSpace.Then(Combinator.Not(Combinator.Token("<>")).Then(Combinator.Token("<"))).Select(x => Syntax.BinOp.Kind.Lt);
            private static readonly Parser<Syntax.BinOp.Kind> Le = SkipWhiteSpace.Then(Combinator.Not(Combinator.Token("<>")).Then(Combinator.Token("<="))).Select(x => Syntax.BinOp.Kind.Le);
            private static readonly Parser<Syntax.BinOp.Kind> Gt = SkipWhiteSpace.Then(Combinator.Token(">")).Select(x => Syntax.BinOp.Kind.Gt);
            private static readonly Parser<Syntax.BinOp.Kind> Ge = SkipWhiteSpace.Then(Combinator.Token(">=")).Select(x => Syntax.BinOp.Kind.Ge);
            private static readonly Parser<Syntax.BinOp.Kind> Eq = SkipWhiteSpace.Then(Combinator.Token("=")).Select(x => Syntax.BinOp.Kind.Eq);
            private static readonly Parser<Syntax.BinOp.Kind> Ne = SkipWhiteSpace.Then(Combinator.Token("<>")).Select(x => Syntax.BinOp.Kind.Ne);
            private static readonly Parser<Syntax.BinOp.Kind> LAnd = SkipWhiteSpace.Then(Combinator.Token("&&")).Select(x => Syntax.BinOp.Kind.LAnd);
            private static readonly Parser<Syntax.BinOp.Kind> LOr = SkipWhiteSpace.Then(Combinator.Token("||")).Select(x => Syntax.BinOp.Kind.LOr);
            private static readonly Parser<Syntax.BinOp.Kind> ColCol = SkipWhiteSpace.Then(Combinator.Token("::")).Select(x => Syntax.BinOp.Kind.Cons);
            private static readonly Parser<Syntax.BinOp.Kind> BinOp = SkipWhiteSpace.Then(Combinator.Choice(Plus,Minus,Mult,Div,Lt,Le,Gt,Ge,Eq,Ne,LAnd,LOr, ColCol));

            private static readonly Parser<Syntax> Expr = Combinator.Lazy(() => LogicalOrExpression);

            private static readonly Parser<Syntax> FunExpr =
                from _1 in Fun
                from _2 in Id.Many(1)
                from _3 in RArrow
                from _4 in Expr
                select _2.Reverse().Aggregate(_4, (s, x) => new Syntax.FunExp(x, s));

            private static readonly Parser<Syntax> DFunExpr =
                from _1 in DFun
                from _2 in Id.Many(1)
                from _3 in RArrow
                from _4 in Expr
                select _2.Reverse().Aggregate(_4, (s, x) => new Syntax.DFunExp(x, s));

            private static readonly Parser<Tuple<string,Syntax>> LetBind =
                from _1 in Id
                from _2 in Id.Many()
                from _3 in Eq
                from _4 in Expr
                select 
                Tuple.Create(_1, _2.Reverse().Aggregate(_4, (s,x) => new Syntax.FunExp(x, s)));

            private static readonly Parser<Syntax> LetExpr =
                from _1 in Let
                from _2 in LetBind.Repeat1(And)
                from _3 in In
                from _4 in Expr
                select (Syntax)new Syntax.LetExp(_2.ToArray(), _4);

            private static readonly Parser<Syntax> LetRecExpr =
                from _1 in Let
                from _2 in Rec
                from _3 in LetBind.Repeat1(And)
                from _4 in In
                from _5 in Expr
                select (Syntax)new Syntax.LetRecExp(_3.ToArray(), _5);

            private static readonly Parser<Syntax> IfExpr =
                from _1 in If
                from _2 in Expr
                from _3 in Then
                from _4 in Expr
                from _5 in Else
                from _6 in Expr
                select (Syntax)new Syntax.IfExp(_2, _4, _6);

            private class Set<T> : IEnumerable<T>
            {
                public T Value { get; }
                public Set<T> Next { get; } 
                public Set(T value, Set<T> next)
                {
                    Value = value;
                    Next = next;
                }

                public IEnumerator<T> GetEnumerator()
                {
                    for (var it = this; it != null; it = it.Next)
                    {
                        yield return it.Value;
                    }
                }

                IEnumerator IEnumerable.GetEnumerator()
                {
                    return GetEnumerator();
                }
            }

            private static Set<string> CheckBind(Pattern p, Set<string> binds)
            {
                if (p is Pattern.VarP) {
                    if (binds.Contains(((Pattern.VarP)p).Id))
                    {
                        return null;
                    } else
                    {
                        return new Set<string>(((Pattern.VarP) p).Id, binds);
                    }
                }
                if (p is Pattern.ConsP)
                {
                    var b1 = CheckBind(((Pattern.ConsP) p).Lhs, binds);
                    if (b1 == null)
                    {
                        return null;
                    }
                    return CheckBind(((Pattern.ConsP)p).Rhs, b1);
                }
                return binds;
            }

            private static readonly Parser<Pattern> PatternExpr = Combinator.Lazy(() => PatternCons);

            private static readonly Parser<Pattern> PatternAtom =
                Combinator.Choice(
                    from _1 in IntV select (Pattern)new Pattern.IntP(_1),
                    from _1 in True select (Pattern)new Pattern.BoolP(true),
                    from _1 in False select (Pattern)new Pattern.BoolP(false),
                    from _1 in Id select (Pattern)new Pattern.VarP(_1),
                    from _1 in LBracket from _2 in RBracket select (Pattern)Pattern.ConsP.Empty,
                    from _1 in LBracket from _2 in PatternExpr.Repeat1(Semi) from _3 in RBracket select _2.Reverse().Aggregate((Pattern)Pattern.ConsP.Empty, (s, x) => new Pattern.ConsP(x, s)),
                    from _1 in Undarbar select (Pattern)new Pattern.WildP(),
                    from _1 in LParen from _2 in RParen select (Pattern)new Pattern.UnitP(),
                    from _1 in LParen from _2 in PatternExpr.Repeat1(Comma) from _3 in RParen select (_2.Length == 1) ? _2[0] : (Pattern)new Pattern.TupleP(_2)
                );

            private static readonly Parser<Pattern> PatternCons =
                PatternAtom.Repeat1(ColCol).Select(y => y.Reverse().Aggregate((s, x) => new Pattern.ConsP(x, s)));

            private static readonly Parser<Tuple<Pattern, Syntax>> PatternBlock =
                from _1 in Bar.Option()
                from _2 in PatternExpr
                where CheckBind(_2, new Set<string>(null, null)) != null
                from _3 in RArrow
                from _4 in Expr
                select Tuple.Create(_2, _4);

            private static readonly Parser<Syntax> MatchExpr =
                from _1 in Match
                from _2 in Expr
                from _3 in With
                from _4 in PatternBlock.Many(1)
                select (Syntax)new Syntax.MatchExp(_2, _4);

            private static readonly Parser<Syntax> AExpr =
                Combinator.Choice(
                    IfExpr, LetRecExpr, LetExpr, FunExpr, DFunExpr, MatchExpr,
                    from _1 in IntV select (Syntax)new Syntax.ILit(_1),
                    from _1 in True select (Syntax)new Syntax.BLit(true),
                    from _1 in False select (Syntax)new Syntax.BLit(false),
                    from _1 in Id select (Syntax)new Syntax.Var(_1),
                    from _1 in LBracket from _2 in RBracket select (Syntax)new Syntax.LLit(),
                    from _1 in LBracket from _2 in Expr.Repeat1(Semi) from _3 in RBracket select _2.Reverse().Aggregate((Syntax)new Syntax.LLit(), (s, x) => new Syntax.BinOp(Syntax.BinOp.Kind.Cons, x, s)),
                    from _1 in LParen from _2 in BinOp from _3 in RParen select (Syntax)new Syntax.FunExp("@lhs", new Syntax.FunExp("@rhs", new Syntax.BinOp(_2, new Syntax.Var("@lhs"), new Syntax.Var("@rhs")))),
                    from _1 in LParen from _2 in RParen select (Syntax)new Syntax.Unit(),
                    from _1 in LParen from _2 in Expr.Repeat1(Comma) from _3 in RParen select (_2.Length == 1) ? _2[0] : (Syntax)new Syntax.TupleExp(_2)
                );

            private static readonly Parser<Syntax> AppExpr =
                from _1 in AExpr.Many(1)
                select _1.Aggregate((s, x) => new Syntax.AppExp(s, x));

            private static readonly Parser<Syntax> MultiplicativeExpression =
                from _1 in AppExpr
                from _2 in Combinator.Many(
                    from _3 in Combinator.Choice(Mult, Div)
                    from _4 in AppExpr
                    select (Func<Syntax, Syntax>)(x => new Syntax.BinOp(_3, x, _4))
                )
                select _2.Aggregate(_1, (s, x) => x(s));

            private static readonly Parser<Syntax> AdditiveExpression =
                from _1 in MultiplicativeExpression
                from _2 in Combinator.Many(
                    from _3 in Combinator.Choice(Plus,Minus)
                    from _4 in MultiplicativeExpression
                    select (Func<Syntax, Syntax>)(x => new Syntax.BinOp(_3, x, _4))
                )
                select _2.Aggregate(_1, (s, x) => x(s));

            private static readonly Parser<Syntax> ConsExpression =
                from _1 in AdditiveExpression.Repeat1(ColCol)
                select _1.Reverse().Aggregate((s, x) => new Syntax.BinOp(Syntax.BinOp.Kind.Cons, x, s));

            private static readonly Parser<Syntax> RelationalExpression =
                from _1 in ConsExpression
                from _2 in Combinator.Many(
                    from _3 in Ne.Not().Then(Combinator.Choice(Le, Ge, Lt, Gt))
                    from _4 in ConsExpression
                    select (Func<Syntax, Syntax>) (x => new Syntax.BinOp(_3, x, _4))
                )
                select _2.Aggregate(_1, (s, x) => x(s));

            private static readonly Parser<Syntax> EqualityExpression =
                from _1 in RelationalExpression
                from _2 in Combinator.Many(
                    from _3 in Combinator.Choice(Ne, Eq)
                    from _4 in RelationalExpression
                    select (Func<Syntax, Syntax>)(x => new Syntax.BinOp(_3, x, _4))
                )
                select _2.Aggregate(_1, (s, x) => x(s));

            private static readonly Parser<Syntax> LogicalAndExpression =
                from _1 in EqualityExpression
                from _2 in Combinator.Many(
                    from _3 in LAnd
                    from _4 in EqualityExpression
                    select _4
                )
                select new[] { _1 }.Concat(_2).Reverse().Aggregate((s, x) => new Syntax.IfExp(s, x, new Syntax.BLit(false)));

            private static readonly Parser<Syntax> LogicalOrExpression =
                from _1 in LogicalAndExpression
                from _2 in Combinator.Many(
                    from _3 in LOr
                    from _4 in LogicalAndExpression
                    select _4
                )
                select new[] { _1 }.Concat(_2).Reverse().Aggregate((s, x) => new Syntax.IfExp(s, x, new Syntax.BLit(false)));


            private static readonly Parser<Program> TopLevel =
                Combinator.Choice(
                    (
                        from _1 in Expr
                        from _2 in SemiSemi
                        select (Program)new Program.Exp(_1)
                    ), (
                        from _1 in Combinator.Many(
                            from _2 in Let
                            from _3 in Rec
                            from _4 in LetBind.Repeat1(And)
                            select new Program.Decls.LetRecDecl(_4)
                        )
                        from _6 in SemiSemi
                        select (Program)new Program.Decls(_1)
                    ), (
                        from _1 in Combinator.Many(
                            from _2 in Let
                            from _3 in LetBind.Repeat1(And)
                            select new Program.Decls.LetDecl(_3)
                        )
                        from _6 in SemiSemi
                        select (Program)new Program.Decls(_1)
                    )
                );

            public static Result<Program> Parse(string s) {
                return TopLevel(s, 0);
            }

        }
    }
}