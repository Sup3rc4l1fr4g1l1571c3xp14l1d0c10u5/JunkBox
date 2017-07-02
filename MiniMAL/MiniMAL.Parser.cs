using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using Parsing;

namespace MiniMAL {
    /// <summary>
    /// パーサ定義
    /// </summary>
    public static class Parser {
        private static readonly Parser<string> WhiteSpace = Combinator.Many(Combinator.AnyChar(" \t\r\n"), 1).Select(x => string.Join("", x));
        private static readonly Parser<string> Comment =
            from _1 in Combinator.Token("(*")
            from _2 in Combinator.Many(
                Combinator.Choice(
                    Combinator.Lazy(() => Comment),
                    Combinator.Not(Combinator.Token("*)")).Then(Combinator.AnyChar().Select(x => $"{x}"))
                )
            ).Select(string.Concat)
            from _3 in Combinator.Token("*)")
            select _1 + _2 + _3;

        private static readonly Parser<string> WS = Combinator.Many(Combinator.Choice(WhiteSpace, Comment)).Select(string.Concat);

        private static readonly Parser<char> LowerChar = Combinator.AnyChar().Where(x => (('a' <= x) && (x <= 'z')));
        private static readonly Parser<char> UpperChar = Combinator.AnyChar().Where(x => (('A' <= x) && (x <= 'Z')));

        private static readonly Parser<string> Ident =
            from _1 in LowerChar
            from _2 in Combinator.Choice(LowerChar, UpperChar, DigitChar, Combinator.AnyChar("_'")).Many()
            select new StringBuilder().Append(_1).Append(_2).ToString();

        private static readonly Parser<string> Constructor =
            from _1 in UpperChar
            from _2 in Combinator.Choice(LowerChar, UpperChar, DigitChar, Combinator.AnyChar("_'")).Many()
            select new StringBuilder().Append(_1).Append(_2).ToString();

        private static readonly Parser<string> ConstractorId = WS.Then(Constructor).Where(x => !(ReservedWords(new Source("", new System.IO.StringReader(x)), Position.Empty, Position.Empty)).Success);

        private static readonly Parser<string> True = WS.Then(Ident.Where(x => x == "true"));
        private static readonly Parser<string> False = WS.Then(Ident.Where(x => x == "false"));
        private static readonly Parser<string> If = WS.Then(Ident.Where(x => x == "if"));
        private static readonly Parser<string> Then = WS.Then(Ident.Where(x => x == "then"));
        private static readonly Parser<string> Else = WS.Then(Ident.Where(x => x == "else"));
        private static readonly Parser<string> Let = WS.Then(Ident.Where(x => x == "let"));
        private static readonly Parser<string> Rec = WS.Then(Ident.Where(x => x == "rec"));
        private static readonly Parser<string> In = WS.Then(Ident.Where(x => x == "in"));
        private static readonly Parser<string> And = WS.Then(Ident.Where(x => x == "and"));
        private static readonly Parser<string> Fun = WS.Then(Ident.Where(x => x == "fun"));
        private static readonly Parser<string> DFun = WS.Then(Ident.Where(x => x == "dfun"));
        private static readonly Parser<string> Match = WS.Then(Ident.Where(x => x == "match"));
        private static readonly Parser<string> With = WS.Then(Ident.Where(x => x == "with"));
        private static readonly Parser<string> Type = WS.Then(Ident.Where(x => x == "type"));
        private static readonly Parser<string> Int = WS.Then(Ident.Where(x => x == "int"));
        private static readonly Parser<string> Bool = WS.Then(Ident.Where(x => x == "bool"));
        private static readonly Parser<string> String = WS.Then(Ident.Where(x => x == "string"));
        private static readonly Parser<string> Unit = WS.Then(Ident.Where(x => x == "unit"));
        private static readonly Parser<string> List = WS.Then(Ident.Where(x => x == "list"));
        private static readonly Parser<string> Some = WS.Then(Constructor.Where(x => x == "Some"));
        private static readonly Parser<string> None = WS.Then(Constructor.Where(x => x == "None"));

        private static readonly Parser<string> ReservedWords = Combinator.Choice(True, False, If, Then, Else, Let, Rec, In, And, Fun, DFun, Match, With, Type, 
            Int, Bool, String, List, Some, None);

        private static readonly Parser<string> Id = WS.Then(ReservedWords.Not()).Then(Ident);

        private static readonly Parser<char> DigitChar = Combinator.AnyChar("0123456789");
        private static readonly Parser<BigInteger> DigitNumber =
            from _1 in Combinator.Token("-").Option().Select(x => x == null ? 1 : -1)
            from _2 in DigitChar.Many(1)
            select _1 * BigInteger.Parse(string.Concat(_2));
        private static readonly Parser<BigInteger> IntV = WS.Then(DigitNumber);

        private static readonly Parser<string> LParen = WS.Then(Combinator.Token("("));
        private static readonly Parser<string> RParen = WS.Then(Combinator.Token(")"));
        private static readonly Parser<string> LBracket = WS.Then(Combinator.Token("["));
        private static readonly Parser<string> RBracket = WS.Then(Combinator.Token("]"));
        private static readonly Parser<string> LBrace = WS.Then(Combinator.Token("{"));
        private static readonly Parser<string> RBrace = WS.Then(Combinator.Token("}"));
        private static readonly Parser<string> Semi = WS.Then(Combinator.Token(";"));
        private static readonly Parser<string> SemiSemi = WS.Then(Combinator.Token(";;"));
        private static readonly Parser<string> RArrow = WS.Then(Combinator.Token("->"));
        private static readonly Parser<string> Bar = WS.Then(Combinator.Token("|"));
        private static readonly Parser<string> Comma = WS.Then(Combinator.Token(","));
        private static readonly Parser<string> Quote = WS.Then(Combinator.Token("'"));
        private static readonly Parser<string> Wild = WS.Then(Combinator.Token("_"));
        private static readonly Parser<Expressions.BuiltinOp.Kind> Plus = WS.Then(Combinator.Token("+")).Select(x => Expressions.BuiltinOp.Kind.Plus);
        private static readonly Parser<Expressions.BuiltinOp.Kind> Minus = WS.Then(Combinator.Token("-")).Select(x => Expressions.BuiltinOp.Kind.Minus);
        private static readonly Parser<Expressions.BuiltinOp.Kind> Mult = WS.Then(Combinator.Token("*")).Select(x => Expressions.BuiltinOp.Kind.Mult);
        private static readonly Parser<Expressions.BuiltinOp.Kind> Div = WS.Then(Combinator.Token("/")).Select(x => Expressions.BuiltinOp.Kind.Div);
        private static readonly Parser<Expressions.BuiltinOp.Kind> Eq = WS.Then(Combinator.Token("=")).Select(x => Expressions.BuiltinOp.Kind.Eq);
        private static readonly Parser<Expressions.BuiltinOp.Kind> Ne = WS.Then(Combinator.Token("<>")).Select(x => Expressions.BuiltinOp.Kind.Ne);
        private static readonly Parser<Expressions.BuiltinOp.Kind> Lt = WS.Then(Combinator.Token("<")).Select(x => Expressions.BuiltinOp.Kind.Lt);
        private static readonly Parser<Expressions.BuiltinOp.Kind> Le = WS.Then(Combinator.Token("<=")).Select(x => Expressions.BuiltinOp.Kind.Le);
        private static readonly Parser<Expressions.BuiltinOp.Kind> Gt = WS.Then(Combinator.Token(">")).Select(x => Expressions.BuiltinOp.Kind.Gt);
        private static readonly Parser<Expressions.BuiltinOp.Kind> Ge = WS.Then(Combinator.Token(">=")).Select(x => Expressions.BuiltinOp.Kind.Ge);
        private static readonly Parser<Expressions.BuiltinOp.Kind> ColCol = WS.Then(Combinator.Token("::")).Select(x => Expressions.BuiltinOp.Kind.ColCol);

        private static readonly Parser<Expressions.BuiltinOp.Kind> BinOp = Combinator.Choice(Plus, Minus, Mult, Div, Eq, Ne, Le, Lt, Ge, Gt, ColCol);

        private static readonly Parser<string> LAnd = WS.Then(Combinator.Token("&&"));
        private static readonly Parser<string> LOr = WS.Then(Combinator.Token("||"));



        private static readonly Parser<string> StringLiteral =
            from _1 in Combinator.Token("\"")
            from _2 in Combinator.Choice(Combinator.Token("\\").Then(Combinator.AnyChar()),
                    Combinator.Token("\"").Not().Then(Combinator.AnyChar()))
                .Many()
                .Select(x => new string(x))
            from _3 in Combinator.Token("\"")
            select _2;

        private static readonly Parser<string> StrV = WS.Then(StringLiteral);


        private static readonly Parser<Expressions> Expr = Combinator.Lazy(() => LogicalOrExpression);

        private static readonly Parser<Expressions> FunExpr =
            from _1 in Fun
            from _2 in Id.Many(1)
            from _3 in RArrow
            from _4 in Expr
            select _2.Reverse().Aggregate(_4, (s, x) => new Expressions.FunExp(x, s));

        private static readonly Parser<Expressions> DFunExpr =
            from _1 in DFun
            from _2 in Id.Many(1)
            from _3 in RArrow
            from _4 in Expr
            select _2.Reverse().Aggregate(_4, (s, x) => new Expressions.DFunExp(x, s));

        private static readonly Parser<Tuple<string, Expressions>> LetBind =
            from _1 in Id
            from _2 in Id.Many()
            from _3 in Eq
            from _4 in Expr
            select Tuple.Create(_1, _2.Reverse().Aggregate(_4, (s, x) => new Expressions.FunExp(x, s)));

        private static readonly Parser<Expressions> LetExpr =
            from _1 in Let
            from _2 in LetBind.Repeat1(And)
            from _3 in In
            from _4 in Expr
            select (Expressions)new Expressions.LetExp(_2, _4);

        private static readonly Parser<Expressions> LetRecExpr =
            from _1 in Let
            from _2 in Rec
            from _3 in LetBind.Repeat1(And)
            from _4 in In
            from _5 in Expr
            select (Expressions)new Expressions.LetRecExp(_3, _5);

        private static readonly Parser<Expressions> IfExpr =
            from _1 in If
            from _2 in Expr
            from _3 in Then
            from _4 in Expr
            from _5 in Else
            from _6 in Expr
            select (Expressions)new Expressions.IfExp(_2, _4, _6);

        private static readonly Parser<PatternExpressions> PatternExpr =
            Combinator.Choice(
                (from _1 in IntV select (PatternExpressions)new PatternExpressions.IntP(_1)),
                (from _1 in StrV select (PatternExpressions)new PatternExpressions.StrP(_1)),
                (from _1 in True select (PatternExpressions)new PatternExpressions.BoolP(true)),
                (from _1 in False select (PatternExpressions)new PatternExpressions.BoolP(false)),
                (from _1 in Wild select (PatternExpressions)new PatternExpressions.WildP()),
                (from _1 in Id select (PatternExpressions)new PatternExpressions.VarP(_1)),
                (from _1 in None select (PatternExpressions)PatternExpressions.OptionP.None),
                (from _1 in Some from _2 in Combinator.Lazy(() => PatternExpr) select (PatternExpressions)new PatternExpressions.OptionP(_2)),
                (from _1 in LParen from _2 in RParen select (PatternExpressions)new PatternExpressions.UnitP()),
                (from _1 in LParen from _2 in Combinator.Lazy(() => PatternCons.Repeat1(Comma)) from _3 in RParen select _2.Length > 1 ? _2.Reverse().Aggregate(PatternExpressions.TupleP.Tail, (s,x) => new PatternExpressions.TupleP(x,s)) : _2[0]),
                (from _1 in LBracket from _2 in RBracket select (PatternExpressions)PatternExpressions.ConsP.Empty),
                (from _1 in LBracket
                 from _2 in Combinator.Lazy(() => PatternCons.Repeat1(Semi))
                 from _3 in RBracket
                 select (PatternExpressions)_2.Reverse().Aggregate(PatternExpressions.ConsP.Empty, (s, x) => new PatternExpressions.ConsP(x, s))
                )
            );

        private static readonly Parser<PatternExpressions> PatternCons =
            from _1 in PatternExpr.Repeat1(ColCol)
            select _1.Reverse().Aggregate((s, x) => new PatternExpressions.ConsP(x, s));

#if false
        private static readonly Parser<Tuple<PatternExpressions, Expressions>> PatternEntry =
            from _1 in PatternCons
            from _2 in RArrow
            from _3 in Expr
            select Tuple.Create(_1, _3)
            ;

        private static readonly Parser<Expressions> MatchExpr =
            from _1 in Match
            from _2 in Expr
            from _3 in With
            from _4 in Bar.Option().Then(PatternEntry.Repeat1(Bar))
            select (Expressions)new Expressions.MatchExp(_2, _4);
#else
        private static readonly Parser<Expressions> PatternEntry =
                from _1 in PatternCons
                from _2 in RArrow
                from _3 in Expr
                select PatternCompiler.CompilePattern(new Expressions.Var("@v"), _1, _3)
            ;

        private static readonly Parser<Expressions> MatchExpr =
            from _1 in Match
            from _2 in Expr
            from _3 in With
            from _4 in Bar.Option().Then(PatternEntry.Repeat1(Bar))
            select (Expressions)
            new Expressions.LetExp(
                new[] { Tuple.Create<string, Expressions>("@v", _2) },
                _4.Reverse().Aggregate(
                    (Expressions)new Expressions.HaltExp("not match"),
                    (s, x) => new Expressions.LetExp(
                        new[] { Tuple.Create<string, Expressions>("@ret", x) },
                        new Expressions.IfExp(
                            new Expressions.BuiltinOp(Expressions.BuiltinOp.Kind.IsNone, new Expressions[] { new Expressions.Var("@ret") }),
                            s,
                            new Expressions.Var("@ret")
                        )
                    )
                )
            );
#endif
        private static readonly Parser<Expressions> PrimaryExpression =
            Combinator.Choice(
                IfExpr, LetRecExpr, LetExpr, FunExpr, DFunExpr, MatchExpr,
                from _1 in IntV select (Expressions)new Expressions.IntLit(_1),
                from _1 in StrV select (Expressions)new Expressions.StrLit(_1),
                from _1 in True select (Expressions)new Expressions.BoolLit(true),
                from _1 in False select (Expressions)new Expressions.BoolLit(false),
                from _1 in Id select (Expressions)new Expressions.Var(_1),
                from _1 in None select (Expressions)Expressions.OptionExp.None,
                from _1 in Some from _2 in Combinator.Lazy(() => Expr) select (Expressions)new Expressions.OptionExp(_2),
                from _1 in LParen from _2 in RParen select (Expressions)new Expressions.UnitLit(),
                from _1 in LParen from _2 in BinOp from _3 in RParen select (Expressions)new Expressions.FunExp("@1", new Expressions.FunExp("@2", new Expressions.BuiltinOp(_2, new Expressions[] { new Expressions.Var("@1"), new Expressions.Var("@2") }))),
                from _1 in LParen from _2 in Expr.Repeat1(Comma) from _3 in RParen select _2.Length > 1 ? _2.Reverse().Aggregate(Expressions.TupleExp.Tail, (s,x) => new Expressions.TupleExp(x,s)) : _2[0],
                from _1 in LBracket from _2 in RBracket select (Expressions)new Expressions.EmptyListLit(),
                from _1 in LBracket from _2 in Expr.Repeat1(Semi) from _3 in RBracket select _2.Reverse().Aggregate((Expressions)new Expressions.EmptyListLit(), (s, x) => new Expressions.BuiltinOp(Expressions.BuiltinOp.Kind.ColCol, new Expressions[] { x, s }))
            );

        private static readonly Parser<Expressions> ApplyExpression =
            from _1 in PrimaryExpression.Many(1)
            select _1.Aggregate((s, x) => new Expressions.AppExp(s, x));

        private static readonly Parser<Expressions.BuiltinOp.Kind> UnaryOperator = WS.Then(
            Combinator.Choice(
                Combinator.Token("-").Select(x => Expressions.BuiltinOp.Kind.UnaryMinus),
                Combinator.Token("+").Select(x => Expressions.BuiltinOp.Kind.UnaryPlus)
            )
        );

        private static readonly Parser<Expressions> UnaryExpression =
            from _1 in UnaryOperator.Many()
            from _2 in ApplyExpression
            select _1.Reverse().Aggregate(_2, (s, x) => new Expressions.BuiltinOp(x, new [] {s}));

        private static readonly Parser<Expressions> MultiplicativeExpression =
            from _1 in UnaryExpression
            from _2 in Combinator.Many(
                from _3 in Combinator.Choice(Mult, Div)
                from _4 in UnaryExpression
                select (Func<Expressions, Expressions>)(x => new Expressions.BuiltinOp(_3, new Expressions[] { x, _4 }))
            )
            select _2.Aggregate(_1, (s, x) => x(s));

        private static readonly Parser<Expressions> AdditiveExpression =
            from _1 in MultiplicativeExpression
            from _2 in Combinator.Many(
                from _3 in Combinator.Choice(Plus, Minus)
                from _4 in MultiplicativeExpression
                select (Func<Expressions, Expressions>)(x => new Expressions.BuiltinOp(_3, new Expressions[] { x, _4 }))
            )
            select _2.Aggregate(_1, (s, x) => x(s));

        private static readonly Parser<Expressions> ConsExpression =
            from _1 in AdditiveExpression.Repeat1(ColCol)
            select _1.Reverse().Aggregate((s, x) => new Expressions.BuiltinOp(Expressions.BuiltinOp.Kind.ColCol, new Expressions[] { x, s }));

        private static readonly Parser<Expressions> RelationalExpression =
            from _1 in ConsExpression
            from _2 in Combinator.Many(
                from _3 in Combinator.Not(Ne).Then(Combinator.Choice(Le, Lt, Ge, Gt))
                from _4 in ConsExpression
                select (Func<Expressions, Expressions>)(x => new Expressions.BuiltinOp(_3, new Expressions[] { x, _4 }))
            )
            select _2.Aggregate(_1, (s, x) => x(s));

        private static readonly Parser<Expressions> EqualityExpression =
            from _1 in RelationalExpression
            from _2 in Combinator.Many(
                from _3 in Combinator.Choice(Eq, Ne)
                from _4 in RelationalExpression
                select (Func<Expressions, Expressions>)(x => new Expressions.BuiltinOp(_3, new Expressions[] { x, _4 }))
            )
            select _2.Aggregate(_1, (s, x) => x(s));

        private static readonly Parser<Expressions> LogicalAndExpression =
            from _1 in EqualityExpression
            from _2 in Combinator.Many(
                from _3 in LAnd
                from _4 in EqualityExpression
                select (Func<Expressions, Expressions>)(x => new Expressions.IfExp(x, _4, new Expressions.BoolLit(false)))
            )
            select _2.Aggregate(_1, (s, x) => x(s));

        private static readonly Parser<Expressions> LogicalOrExpression =
            from _1 in LogicalAndExpression
            from _2 in Combinator.Many(
                from _3 in LOr
                from _4 in LogicalAndExpression
                select (Func<Expressions, Expressions>)(x => new Expressions.IfExp(x, new Expressions.BoolLit(true), _4))
            )
            select _2.Aggregate(_1, (s, x) => x(s));

        private static readonly Func<Environment<int>, Parser<Tuple<Environment<int>, Typing.Type>>> TypeExprTerm = (x) =>
            Combinator.Choice(
            (from _1 in Int select Tuple.Create(x, (Typing.Type)new Typing.Type.TyInt())),
            (from _1 in Bool select Tuple.Create(x, (Typing.Type)new Typing.Type.TyBool())),
            (from _1 in String select Tuple.Create(x, (Typing.Type)new Typing.Type.TyStr())),
            (from _1 in Unit select Tuple.Create(x, (Typing.Type)new Typing.Type.TyUnit())),
            (from _1 in Quote from _2 in Id let ev = Environment.Contains(_2, x) ? x : Environment.Extend(_2, Typing.Type.TyVar.Fresh().Id, x) let id = Environment.LookUp(_2, x) select Tuple.Create(ev, (Typing.Type)new Typing.Type.TyVar(id))),
            (from _1 in LBrace from _2 in TypeRecordBody from _3 in RBrace select _2),
            (from _1 in LParen from _2 in Combinator.Lazy(() => TypeExpr(x)) from _3 in RParen select _2)
            );

        private static readonly Func<Environment<int>, Parser<Tuple<Environment<int>, Typing.Type>>> TypeExprList = (x) =>
            from _1 in TypeExprTerm(x)
            from _2 in Combinator.Choice(List, Option).Many()
            select Tuple.Create(_1.Item1, _2.Aggregate(_1.Item2, (s, y) => y == "list" ? (Typing.Type)new Typing.Type.TyCons(s) : (Typing.Type)new Typing.Type.TyOption(s)));

        private static readonly Func<Environment<int>, Parser<Tuple<Environment<int>, Typing.Type.TyTuple>>> TypeExprTupleBody = (x) =>
            from _1 in TypeExprList(x)
            from _2 in Mult.Then(TypeExprTupleBody(_1.Item1)).Option()
            select _2 == null ? Tuple.Create(_1.Item1, new Typing.Type.TyTuple(_1.Item2, Typing.Type.TyTuple.Tail)) : Tuple.Create(_2.Item1, new Typing.Type.TyTuple(_1.Item2, _2.Item2))
            ;

        private static readonly Func<Environment<int>, Parser<Tuple<Environment<int>, Typing.Type>>> TypeExprTuple = (x) =>
            from _1 in TypeExprList(x)
            from _2 in Mult.Then(TypeExprTupleBody(_1.Item1)).Option()
            select _2 == null ? _1 : Tuple.Create(_2.Item1, (Typing.Type)new Typing.Type.TyTuple(_1.Item2, _2.Item2))
            ;

        private static readonly Func<Environment<int>, Parser<Tuple<Environment<int>, Typing.Type>>> TypeExprFunc = (x) =>
            from _1 in TypeExprTuple(x)
            from _2 in RArrow.Then(TypeExprFunc(_1.Item1)).Option()
            select _2 == null ? _1 : Tuple.Create(_2.Item1, (Typing.Type)new Typing.Type.TyFunc(_1.Item2, _2.Item2))
            ;

        private static readonly Func<Environment<int>, Parser<Tuple<Environment<int>, Typing.Type>>> TypeExpr = (x) =>
            TypeExprFunc(x)
            ;

        private static readonly Parser<Tuple<string, Typing.Type>> Typedef =
            from _1 in Id
            from _2 in Eq
            from _3 in TypeExpr(Environment<int>.Empty)
            select Tuple.Create(_1, _3.Item2);

        private static readonly Parser<Toplevel> TopLevel =
            Combinator.Choice(
                (
                    from _1 in Expr
                    from _2 in SemiSemi
                    select (Toplevel)new Toplevel.Exp(_1)
                ), (
                       from _1 in Type
                       from _2 in Typedef.Repeat1(And)
                       from _3 in SemiSemi
                       select (Toplevel)new Toplevel.TypeDef(_2)
                ), (
                    from _1 in Combinator.Many(
                        Combinator.Choice(
                            (
                                from _2 in Let
                                from _3 in Rec
                                from _4 in LetBind.Repeat1(And)
                                select (Toplevel.Binding.DeclBase)new Toplevel.Binding.LetRecDecl(_4)
                            ), (
                                from _2 in Let
                                from _4 in LetBind.Repeat1(And)
                                select (Toplevel.Binding.DeclBase)new Toplevel.Binding.LetDecl(_4)
                            )
                        ),
                        1
                    )
                    from _6 in SemiSemi
                    select (Toplevel)new Toplevel.Binding(_1)
                ), (
                    from _1 in WS
                    from _2 in SemiSemi.Option()
                    from _3 in Combinator.AnyChar().Not()
                    select (Toplevel)new Toplevel.Empty()
                )
            );

        private static readonly Parser<Toplevel> ErrorRecovery =
            from _1 in Combinator.Choice(SemiSemi, Combinator.EoF().Select(x => "")).Not().Then(Combinator.AnyChar()).Many()
            from _2 in Combinator.Choice(SemiSemi, Combinator.EoF().Select(x => ""))
            select (Toplevel)new Toplevel.Empty();

        public static Result<Toplevel> Parse(Source s) {
            var ret = TopLevel(s, Position.Empty, Position.Empty);
            if (ret.Success == false) {
                var ret2 = ErrorRecovery(s, ret.FailedPosition, ret.FailedPosition);
                return new Result<Toplevel>(false, null, ret2.Position, ret.FailedPosition);
            } else {
                return ret;
            }
        }

    }
}
