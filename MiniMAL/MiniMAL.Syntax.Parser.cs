using System;
using System.Linq;
using System.Numerics;
using System.Text;
using Parsing;

namespace MiniMAL
{
    namespace Syntax
    {
        /// <summary>
        /// パーサ定義
        /// </summary>
        public static class Parser
        {
            private static readonly Parser<string> WhiteSpace =
                Combinator.AnyChar(" \t\r\n").Many(1).Select(x => string.Join("", x));

            private static readonly Parser<string> Comment =
                from t1 in Combinator.Token("(*")
                from t2 in
                Combinator.Choice(
                    Combinator.Lazy(() => Comment),
                    Combinator.Token("*)").Not().Then(Combinator.AnyChar().Select(x => $"{x}"))
                ).Many().Select(string.Concat)
                from t3 in Combinator.Token("*)")
                select t1 + t2 + t3;

            private static readonly Parser<string> WS = Combinator.Choice(WhiteSpace, Comment).Many()
                .Select(string.Concat);

            private static readonly Parser<char> LowerChar =
                Combinator.AnyChar().Where(x => (('a' <= x) && (x <= 'z')));

            private static readonly Parser<char> UpperChar =
                Combinator.AnyChar().Where(x => (('A' <= x) && (x <= 'Z')));

            private static readonly Parser<string> Ident =
                from t1 in LowerChar
                from t2 in Combinator.Choice(LowerChar, UpperChar, DigitChar, Combinator.AnyChar("_'")).Many()
                select new StringBuilder().Append(t1).Append(t2).ToString();

            private static readonly Parser<string> Constructor =
                from t1 in UpperChar
                from t2 in Combinator.Choice(LowerChar, UpperChar, DigitChar, Combinator.AnyChar("_'")).Many()
                select new StringBuilder().Append(t1).Append(t2).ToString();


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
            private static readonly Parser<string> Match = WS.Then(Ident.Where(x => x == "match"));
            private static readonly Parser<string> With = WS.Then(Ident.Where(x => x == "with"));
            private static readonly Parser<string> Type = WS.Then(Ident.Where(x => x == "type"));
            private static readonly Parser<string> Int = WS.Then(Ident.Where(x => x == "int"));
            private static readonly Parser<string> Bool = WS.Then(Ident.Where(x => x == "bool"));
            private static readonly Parser<string> String = WS.Then(Ident.Where(x => x == "string"));
            private static readonly Parser<string> Unit = WS.Then(Ident.Where(x => x == "unit"));
            private static readonly Parser<string> External = WS.Then(Ident.Where(x => x == "external"));
            private static readonly Parser<string> Of = WS.Then(Ident.Where(x => x == "of"));

            private static readonly Parser<string> ConstructorId =
                WS.Then(Constructor).Where(x => !(ReservedWords(new Source("", new System.IO.StringReader(x)),
                    Position.Empty, Position.Empty)).Success);

            private static readonly Parser<string> Some = WS.Then(Constructor.Where(x => x == "Some"));
            private static readonly Parser<string> None = WS.Then(Constructor.Where(x => x == "None"));

            private static readonly Parser<string> ReservedWords = Combinator.Choice(True, False, If, Then, Else, Let,
                Rec, In, And, Fun, Match, With, Type,
                Int, Bool, String, Unit, External, Of, Some, None);

            private static readonly Parser<string> Id = WS.Then(ReservedWords.Not()).Then(Ident);

            private static readonly Parser<char> DigitChar = Combinator.AnyChar("0123456789");

            private static readonly Parser<BigInteger> DigitNumber =
                from t1 in Combinator.Token("-").Option().Select(x => x == null ? 1 : -1)
                from t2 in DigitChar.Many(1)
                select t1 * BigInteger.Parse(string.Concat(t2));

            private static readonly Parser<BigInteger> IntV = WS.Then(DigitNumber);

            private static readonly Parser<string> LParen = WS.Then(Combinator.Token("("));
            private static readonly Parser<string> RParen = WS.Then(Combinator.Token(")"));
            private static readonly Parser<string> LBracket = WS.Then(Combinator.Token("["));
            private static readonly Parser<string> RBracket = WS.Then(Combinator.Token("]"));
            private static readonly Parser<string> LBrace = WS.Then(Combinator.Token("{"));
            private static readonly Parser<string> RBrace = WS.Then(Combinator.Token("}"));
            private static readonly Parser<string> Colon = WS.Then(Combinator.Token(":"));
            private static readonly Parser<string> Semi = WS.Then(Combinator.Token(";"));
            private static readonly Parser<string> SemiSemi = WS.Then(Combinator.Token(";;"));
            private static readonly Parser<string> RArrow = WS.Then(Combinator.Token("->"));
            private static readonly Parser<string> Bar = WS.Then(Combinator.Token("|"));
            private static readonly Parser<string> Comma = WS.Then(Combinator.Token(","));
            private static readonly Parser<string> Quote = WS.Then(Combinator.Token("'"));
            private static readonly Parser<string> Wild = WS.Then(Combinator.Token("_"));
            private static readonly Parser<string> Eq = WS.Then(Combinator.Token("="));
            private static readonly Parser<string> ColCol = WS.Then(Combinator.Token("::"));

            private static readonly Parser<char> InfixOpChar = Combinator.AnyChar("!%&*+-/<=>?@^|:");
            private static readonly Parser<string> InfixOp = WS.Then(InfixOpChar.Many(1).Select(string.Concat));
            private static readonly Parser<string> InfixOpDef = InfixOp;

            private static readonly Parser<char> PrefixOpChar = Combinator.AnyChar("+-!");
            private static readonly Parser<string> PrefixOp = WS.Then(PrefixOpChar.Many(1).Select(string.Concat));

            private static readonly Parser<string> PrefixOpDef =
                WS.Then(Combinator.AnyChar("~")).Then(PrefixOpChar.Many(1).Select(string.Concat).Select(x => "~" + x));

            private static Parser<string> InfixOpOf(string startwith) =>
                from t1 in WS
                from t2 in Combinator.AnyChar(startwith)
                from t3 in InfixOpChar.Many().Select(string.Concat)
                select t2 + t3;

            private static readonly Parser<string> StringLiteral =
                from t1 in Combinator.Token("\"")
                from t2 in Combinator.Choice(Combinator.Token("\\").Then(Combinator.AnyChar()),
                        Combinator.Token("\"").Not().Then(Combinator.AnyChar()))
                    .Many()
                    .Select(x => new string(x))
                from t3 in Combinator.Token("\"")
                select t2;

            private static readonly Parser<string> StrV = WS.Then(StringLiteral);


            private static readonly Parser<Expressions> Expr = Combinator.Lazy(() => LogicalOrExpression);

            private static readonly Parser<Expressions> FunExpr =
                from t1 in Fun
                from t2 in Id.Many(1)
                from t3 in RArrow
                from t4 in Expr
                select t2.Reverse().Aggregate(t4, (s, x) => new Expressions.FunExp(x, s, TypeExpressions.TypeVar.Fresh(), TypeExpressions.TypeVar.Fresh()));

            private static readonly Parser<Tuple<string, Expressions>> LetBind =
                from t1 in Combinator.Choice(
                    Id,
                    from t1 in LParen
                    from t2 in Combinator.Choice(InfixOpDef, PrefixOpDef)
                    from t3 in RParen
                    select $"{t2}"
                )
                from t2 in Combinator.Choice(
                    from t3 in Id
                    select Tuple.Create<string, TypeExpressions>(t3,TypeExpressions.TypeVar.Fresh()),
                    from t3 in LParen
                    from t4 in Id
                    from t5 in Colon.Then(TypeExpr).Option().Select(x => (x ?? TypeExpressions.TypeVar.Fresh()))
                    from t6 in RParen
                    select Tuple.Create(t4, t5)
                ).Many()
                from t3 in Colon.Then(TypeExpr).Option().Select(x => (x ?? TypeExpressions.TypeVar.Fresh()))
                from t4 in Eq
                from t5 in Expr
                let f = t2.Reverse().Aggregate(Tuple.Create(t3,t5),(s,x) => Tuple.Create(
                    (TypeExpressions)new TypeExpressions.FuncType(x.Item2, s.Item1), 
                    (Expressions)new Expressions.FunExp(x.Item1, s.Item2, x.Item2, s.Item1))
                )
                select Tuple.Create(t1, f.Item2);

            private static readonly Parser<Expressions> LetExpr =
                from t1 in Let
                from t2 in LetBind.Repeat1(And)
                from t3 in In
                from t4 in Expr
                select (Expressions)new Expressions.LetExp(t2, t4);

            private static readonly Parser<Expressions> LetRecExpr =
                from t1 in Let
                from t2 in Rec
                from t3 in LetBind.Repeat1(And)
                from t4 in In
                from t5 in Expr
                select (Expressions)new Expressions.LetRecExp(t3, t5);

            private static readonly Parser<Expressions> IfExpr =
                from t1 in If
                from t2 in Expr
                from t3 in Then
                from t4 in Expr
                from t5 in Else
                from t6 in Expr
                select (Expressions)new Expressions.IfExp(t2, t4, t6);

            private static readonly Parser<PatternExpressions> PatternExpr =
                Combinator.Choice(
                    (from t1 in IntV select (PatternExpressions)new PatternExpressions.IntP(t1)),
                    (from t1 in StrV select (PatternExpressions)new PatternExpressions.StrP(t1)),
                    (from t1 in True select (PatternExpressions)new PatternExpressions.BoolP(true)),
                    (from t1 in False select (PatternExpressions)new PatternExpressions.BoolP(false)),
                    (from t1 in Wild select (PatternExpressions)new PatternExpressions.WildP()),
                    (from t1 in Id select (PatternExpressions)new PatternExpressions.VarP(t1)),
                    (from t1 in None select (PatternExpressions)PatternExpressions.OptionP.None),
                    (from t1 in Some from t2 in Combinator.Lazy(() => PatternExpr) select (PatternExpressions)new PatternExpressions.OptionP(t2)),
                    (from t1 in ConstructorId from t2 in Combinator.Lazy(() => PatternExpr).Option() select (PatternExpressions)new PatternExpressions.VariantP(t1, t2??new PatternExpressions.UnitP())),
                    (from t1 in LParen from t2 in RParen select (PatternExpressions)new PatternExpressions.UnitP()),
                    (from t1 in LParen from t2 in Combinator.Lazy(() => PatternCons.Repeat1(Comma)) from t3 in RParen select t2.Length > 1 ? new PatternExpressions.TupleP(t2) : t2[0]),
                    (from t1 in LBracket from t2 in RBracket select (PatternExpressions)PatternExpressions.ConsP.Empty),
                    (from t1 in LBrace
                     from t2 in Combinator.Lazy(() =>
                         from t3 in Id
                         from t4 in Eq
                         from t5 in PatternCons
                         select Tuple.Create(t3,t5)
                        ).Repeat1(Semi)
                     from t6 in RBrace
                     select (PatternExpressions)new PatternExpressions.RecordP(t2)
                    )
                );

            private static readonly Parser<PatternExpressions> PatternCons =
                from t1 in PatternExpr.Repeat1(ColCol)
                select t1.Reverse().Aggregate((s, x) => new PatternExpressions.ConsP(x, s));

            private static readonly Parser<Tuple<PatternExpressions, Expressions>> PatternEntry =
                    from t1 in PatternCons
                    from t2 in RArrow
                    from t3 in Expr
                    select Tuple.Create(t1, t3)
                ;


            private static readonly Parser<Expressions> MatchExpr =
                from t1 in Match
                from t2 in Expr
                from t3 in With
                from t4 in Bar.Option().Then(PatternEntry.Repeat1(Bar))
#if true
                select (Expressions)new Expressions.MatchExp(t2, t4);
#else
            select (Expressions)
            new Expressions.LetExp(
                new[] { Tuple.Create<string, Expressions>("@v", t2) },
                t4.Select(x => PatternExpressions.Compiler.Compile(new Expressions.Var("@v"), x.Item1, new Expressions.OptionExp(x.Item2))).Reverse().Aggregate(
                    (Expressions)new Expressions.HaltExp("not match"),
                    (s, x) => new Expressions.LetExp(
                        new[] { Tuple.Create<string, Expressions>("@ret", x) },
                        new Expressions.IfExp(
                            new Expressions.AppExp(new Expressions.AppExp(new Expressions.Var("="), new Expressions.Var("@ret")), Expressions.OptionExp.None), 
                            s,
                            new Expressions.AppExp(new Expressions.Var("get"), new Expressions.Var("@ret"))
                        )
                    )
                )
            );
#endif
            private static readonly Parser<Expressions> PrimaryExpression =
                Combinator.Choice(
                    IfExpr, LetRecExpr, LetExpr, FunExpr, MatchExpr,
                    from t1 in IntV select (Expressions)new Expressions.IntLit(t1),
                    from t1 in StrV select (Expressions)new Expressions.StrLit(t1),
                    from t1 in True select (Expressions)new Expressions.BoolLit(true),
                    from t1 in False select (Expressions)new Expressions.BoolLit(false),
                    from t1 in Id select (Expressions)new Expressions.Var(t1),
                    from t1 in None select (Expressions)Expressions.OptionExp.None,
                    from t1 in Some from t2 in Combinator.Lazy(() => Expr) select (Expressions)new Expressions.OptionExp(t2),
                    from t1 in ConstructorId from t2 in Combinator.Lazy(() => Expr).Option() select (Expressions)new Expressions.ConstructorExp(t1, t2 ?? new Expressions.UnitLit()),
                    from t1 in LParen from t2 in RParen select (Expressions)new Expressions.UnitLit(),
                    from t1 in LParen from t2 in InfixOp from t3 in RParen select (Expressions)new Expressions.Var($"{t2}"),
                    from t1 in LParen from t2 in Expr.Repeat1(Comma) from t3 in RParen select t2.Length > 1 ? new Expressions.TupleExp(t2) : t2[0],
                    from t1 in LBracket from t2 in RBracket select (Expressions)new Expressions.EmptyListLit(),
                    from t1 in LBracket
                    from t2 in Expr.Repeat1(Semi)
                    from t3 in RBracket
                    select t2.Reverse().Aggregate((Expressions)new Expressions.EmptyListLit(),
                        (s, x) => new Expressions.AppExp(new Expressions.AppExp(new Expressions.Var("::"), x), s)),
                    (from t1 in LBrace
                        from t2 in Combinator.Lazy(() =>
                            from t3 in Id
                            from t4 in Eq
                            from t5 in Expr
                            select Tuple.Create(t3, t5)
                        ).Repeat1(Semi)
                        from t6 in RBrace
                        select (Expressions)new Expressions.RecordExp(t2)
                    )

                );

            private static readonly Parser<Expressions> ApplyExpression =
                from t1 in PrimaryExpression.Many(1)
                select t1.Aggregate((s, x) => new Expressions.AppExp(s, x));

            private static readonly Parser<Func<Expressions, Expressions>> UnaryOperator = WS.Then(
                PrefixOp.Select<string, Func<Expressions, Expressions>>(x =>
                    (y) => new Expressions.AppExp(new Expressions.Var($"~{x}"), y)
                )
            );

            private static readonly Parser<Expressions> UnaryExpression =
                from t1 in UnaryOperator.Many()
                from t2 in ApplyExpression
                select t1.Reverse().Aggregate(t2, (s, x) => x(s));

            private static readonly Parser<Expressions> MultiplicativeExpression =
                from t1 in UnaryExpression
                from t2 in (
                    from t3 in InfixOpOf("*/%")
                    from t4 in UnaryExpression
                    select (Func<Expressions, Expressions>)(x => new Expressions.AppExp(
                        new Expressions.AppExp(new Expressions.Var(t3), x), t4))
                ).Many()
                select t2.Aggregate(t1, (s, x) => x(s));

            private static readonly Parser<Expressions> AdditiveExpression =
                from t1 in MultiplicativeExpression
                from t2 in (
                    from t3 in InfixOpOf("+-")
                    from t4 in MultiplicativeExpression
                    select (Func<Expressions, Expressions>)(x => new Expressions.AppExp(
                        new Expressions.AppExp(new Expressions.Var(t3), x), t4))
                ).Many()
                select t2.Aggregate(t1, (s, x) => x(s));

            private static readonly Parser<Expressions> CapExpression =
                from t1 in AdditiveExpression
                from t2 in (
                    from t3 in InfixOpOf("^")
                    from t4 in AdditiveExpression
                    select Tuple.Create(t3, t4)
                ).Many()
                let exprs = new[] { t1 }.Concat(t2.Select(x => x.Item2)).Reverse().ToArray()
                let e = exprs.First()
                let es = exprs.Skip(1).Zip(t2.Select(x => x.Item1).Reverse(), Tuple.Create).ToArray()
                select es.Aggregate(e,
                    (s, x) => new Expressions.AppExp(new Expressions.AppExp(new Expressions.Var(x.Item2), x.Item1), s));

            private static readonly Parser<Expressions> ConsExpression =
                from t1 in CapExpression
                from t2 in (
                    from t3 in InfixOpOf(":")
                    from t4 in CapExpression
                    select Tuple.Create(t3, t4)
                ).Many()
                let exprs = new[] { t1 }.Concat(t2.Select(x => x.Item2)).Reverse().ToArray()
                let e = exprs.First()
                let es = exprs.Skip(1).Zip(t2.Select(x => x.Item1).Reverse(), Tuple.Create).ToArray()
                select es.Aggregate(e,
                    (s, x) => new Expressions.AppExp(new Expressions.AppExp(new Expressions.Var(x.Item2), x.Item1), s));

            private static readonly Parser<Expressions> EqualityExpression =
                from t1 in ConsExpression
                from t2 in (
                    from t3 in InfixOpOf("!<>=")
                    from t4 in ConsExpression
                    select (Func<Expressions, Expressions>)(x => new Expressions.AppExp(
                        new Expressions.AppExp(new Expressions.Var(t3), x), t4))
                ).Many()
                select t2.Aggregate(t1, (s, x) => x(s));

            private static readonly Parser<Expressions> LogicalAndExpression =
                from t1 in EqualityExpression
                from t2 in (
                    from t3 in InfixOpOf("&")
                    from t4 in EqualityExpression
                    select t3 == "&&"
                        ? (Func<Expressions, Expressions>)(x => new Expressions.IfExp(x, t4,
                            new Expressions.BoolLit(false)))
                        : (Func<Expressions, Expressions>)(x => new Expressions.AppExp(
                            new Expressions.AppExp(new Expressions.Var(t3), x), t4))
                ).Many()
                select t2.Aggregate(t1, (s, x) => x(s));

            private static readonly Parser<Expressions> LogicalOrExpression =
                from t1 in LogicalAndExpression
                from t2 in (
                    from t3 in InfixOpOf("|")
                    where t3 != "|"
                    from t4 in LogicalAndExpression
                    select t3 == "||"
                        ? (Func<Expressions, Expressions>)(x => new Expressions.IfExp(x, new Expressions.BoolLit(true),
                            t4))
                        : (Func<Expressions, Expressions>)(x => new Expressions.AppExp(
                            new Expressions.AppExp(new Expressions.Var(t3), x), t4))
                ).Many()
                select t2.Aggregate(t1, (s, x) => x(s));

            private static readonly Parser<TypeExpressions> SimpleType =
                from t1 in Combinator.Choice(
                    Quote.Then(Id).Select(x => (TypeExpressions)new TypeExpressions.TypeVar("'" + x)),
                    from t1 in Int select (TypeExpressions)new TypeExpressions.IntType(),
                    from t1 in Bool select (TypeExpressions)new TypeExpressions.BoolType(),
                    from t1 in Unit select (TypeExpressions)new TypeExpressions.UnitType(),
                    from t1 in String select (TypeExpressions)new TypeExpressions.StrType(),
                    from t1 in Id select (TypeExpressions)new TypeExpressions.TypeConstruct(new TypeExpressions.TypeName(t1),new TypeExpressions[0]),
                    from t1 in LParen
                    from t2 in Combinator.Lazy(() => TypeExpr).Repeat1(Comma)
                    from t3 in RParen
                    from t4 in Id.Select(x => new TypeExpressions.TypeName(x))
                    select (TypeExpressions)new TypeExpressions.TypeConstruct(t4, t2),
                    from t1 in LParen from t2 in Combinator.Lazy(() => TypeExpr) from t3 in RParen select t2

                )
                from t2 in Id.Select(x => new TypeExpressions.TypeName(x)).Many()
                select t2.Aggregate(t1, (s, y) => (TypeExpressions)new TypeExpressions.TypeConstruct(y, new[] { s }));

            private static readonly Parser<TypeExpressions> TypeExprTuple =
                SimpleType.Repeat1(WS.Then(Combinator.Token("*")))
                    .Select(x => (x.Length > 1
                        ? new TypeExpressions.TupleType(x)
                        : x[0]));

            private static readonly Parser<TypeExpressions> TypeExprFunc =
                Combinator.Choice(
                    from t1 in TypeExprTuple
                    from t2 in RArrow.Then(Combinator.Lazy(() => TypeExprFunc)).Option()
                    select t2 == null ? t1 : (TypeExpressions)new TypeExpressions.FuncType(t1, t2)
                );

            private static readonly Parser<TypeExpressions> TypeExpr =
                TypeExprFunc;

            private static readonly Parser<TypeExpressions> RecordTypeExpr =
                from t1 in LBrace
                from t2 in Combinator.Lazy(() =>
                    from t3 in Id
                    from t4 in Colon
                    from t5 in TypeExpr
                    select Tuple.Create(t3, t5)
                ).Repeat1(Semi)
                from t6 in RBrace
                select (TypeExpressions) new TypeExpressions.RecordType(t2);

            private static readonly Parser<TypeExpressions> VariantTypeExpr =
                from t1 in Bar.Option()
                from t2 in Combinator.Lazy(() =>
                    from t3 in ConstructorId
                    from t4 in Of.Then(TypeExpr).Option().Select(x => x ?? new TypeExpressions.UnitType())
                    select Tuple.Create(t3, t4)
                ).Repeat1(Bar)
                select (TypeExpressions)new TypeExpressions.VariantType(t2);
            
            private static readonly Parser<Toplevel> TopLevel =
                Combinator.Choice(
                    (
                        from t1 in Expr
                        from t2 in SemiSemi
                        select (Toplevel)new Toplevel.Exp(t1)
                    ), (
                        from t1 in External
                        from t2 in Id
                        from t3 in Colon
                        from t4 in TypeExpr
                        from t5 in Eq
                        from t6 in StrV
                        from t7 in SemiSemi
                        select (Toplevel)new Toplevel.ExternalDecl(t2, t4, t6)
                    ), (
                        // TODO:  let 同様に type ～ and ～ で複数の型を定義できるように
                        from t1 in Type
                        from t2 in Combinator.Choice(
                            from t3 in LParen
                            from t4 in Quote.Then(Id).Select(x => "'" + x).Repeat1(Comma)
                            from t5 in RParen
                            select t4,
                            Quote.Then(Id).Select(x => new[] { "'" + x })
                        ).Option()
                        from t3 in Id
                        from t4 in Eq
                        from t5 in Combinator.Choice(TypeExpr, RecordTypeExpr, VariantTypeExpr)
                        from t6 in SemiSemi
                        select (Toplevel)new Toplevel.TypeDef(t3, t2 ?? new string[] { }, t5)
                    ), (
                        from t1 in Combinator.Many(
                            Combinator.Choice(
                                (
                                    from t2 in Let
                                    from t3 in Rec
                                    from t4 in LetBind.Repeat1(And)
                                    select (Toplevel.Binding.DeclBase)new Toplevel.Binding.LetRecDecl(t4)
                                ), (
                                    from t2 in Let
                                    from t4 in LetBind.Repeat1(And)
                                    select (Toplevel.Binding.DeclBase)new Toplevel.Binding.LetDecl(t4)
                                )
                            ),
                            1
                        )
                        from t6 in SemiSemi
                        select (Toplevel)new Toplevel.Binding(t1)
                    ), (
                        from t1 in WS
                        from t2 in SemiSemi.Option()
                        from t3 in Combinator.AnyChar().Not()
                        select (Toplevel)new Toplevel.Empty()
                    )
                );

            private static readonly Parser<Toplevel> ErrorRecovery =
                from t1 in Combinator.Choice(SemiSemi, Combinator.EoF().Select(x => "")).Not()
                    .Then(Combinator.AnyChar()).Many()
                from t2 in Combinator.Choice(SemiSemi, Combinator.EoF().Select(x => ""))
                select (Toplevel)new Toplevel.Empty();

            public static Result<Toplevel> Parse(Source s, Position pos)
            {
                var ret = TopLevel(s, pos, pos);
                if (ret.Success == false)
                {
                    var ret2 = ErrorRecovery(s, ret.FailedPosition, ret.FailedPosition);
                    return new Result<Toplevel>(false, null, ret2.Position, ret.FailedPosition);
                }
                else
                {
                    return ret;
                }
            }

        }
    }
}
