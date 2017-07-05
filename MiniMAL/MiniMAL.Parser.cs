using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography.X509Certificates;
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
        //private static readonly Parser<string> List = WS.Then(Ident.Where(x => x == "list"));
        //private static readonly Parser<string> Option = WS.Then(Ident.Where(x => x == "option"));
        private static readonly Parser<string> External = WS.Then(Ident.Where(x => x == "external"));

        private static readonly Parser<string> Some = WS.Then(Constructor.Where(x => x == "Some"));
        private static readonly Parser<string> None = WS.Then(Constructor.Where(x => x == "None"));

        private static readonly Parser<string> ReservedWords = Combinator.Choice(True, False, If, Then, Else, Let, Rec, In, And, Fun, DFun, Match, With, Type, 
            Int, Bool, String, Unit, Some, None);

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
        private static readonly Parser<string> Colon = WS.Then(Combinator.Token(":"));
        private static readonly Parser<string> Semi = WS.Then(Combinator.Token(";"));
        private static readonly Parser<string> SemiSemi = WS.Then(Combinator.Token(";;"));
        private static readonly Parser<string> RArrow = WS.Then(Combinator.Token("->"));
        private static readonly Parser<string> Bar = WS.Then(Combinator.Token("|"));
        private static readonly Parser<string> Comma = WS.Then(Combinator.Token(","));
        private static readonly Parser<string> Quote = WS.Then(Combinator.Token("'"));
        private static readonly Parser<string> Wild = WS.Then(Combinator.Token("_"));
#if false
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
#endif

        private static readonly Parser<string> LAnd = WS.Then(Combinator.Token("&&"));
        private static readonly Parser<string> LOr = WS.Then(Combinator.Token("||"));

        private static readonly Parser<string> InfixOp = WS.Then(Combinator.AnyChar("!%&*+-/<=>?@^|:").Many(1).Select(string.Concat));
        private static Parser<string> InfixOpOf(string startwith) => 
            from _1 in WS
            from _2 in Combinator.AnyChar(startwith)
            from _3 in Combinator.AnyChar("!%&*+-/<=>?@^|:").Many().Select(string.Concat)
            select _2 + _3;

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
            from _1 in Combinator.Choice(
                           Id,
                           from _1 in LParen
                           from _2 in InfixOp
                           from _3 in RParen
                           select $"{_2}"
                       )
            from _2 in (
                from _3 in Id
                from _4 in Colon.Then(TypeExpr)
                select _3
            }.Many()
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

        private static readonly Parser<Tuple<PatternExpressions, Expressions>> PatternEntry =
                from _1 in PatternCons
                from _2 in RArrow
                from _3 in Expr
                select Tuple.Create(_1, _3)
            ;


#if true
        private static readonly Parser<Expressions> MatchExpr =
            from _1 in Match
            from _2 in Expr
            from _3 in With
            from _4 in Bar.Option().Then(PatternEntry.Repeat1(Bar))
            select (Expressions)new Expressions.MatchExp(_2, _4);
#else

        private static readonly Parser<Expressions> MatchExpr =
            from _1 in Match
            from _2 in Expr
            from _3 in With
            from _4 in Bar.Option().Then(PatternEntry.Repeat1(Bar))
            select (Expressions)
            new Expressions.LetExp(
                new[] { Tuple.Create<string, Expressions>("@v", _2) },
                _4.Select(x => PatternCompiler.CompilePattern(new Expressions.Var("@v"), x.Item1, x.Item2)).Reverse().Aggregate(
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
                from _1 in LParen from _2 in InfixOp from _3 in RParen select (Expressions)new Expressions.Var($"{_2}"),
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
                //from _3 in Combinator.Choice(Mult, Div)
                //from _4 in UnaryExpression
                //select (Func<Expressions, Expressions>)(x => new Expressions.BuiltinOp(_3, new Expressions[] { x, _4 }))
                from _3 in InfixOpOf("*/%")
                from _4 in UnaryExpression
                select (Func<Expressions, Expressions>)(x => new Expressions.AppExp(new Expressions.AppExp(new Expressions.Var(_3), x), _4))
                
            )
            select _2.Aggregate(_1, (s, x) => x(s));

        private static readonly Parser<Expressions> AdditiveExpression =
            from _1 in MultiplicativeExpression
            from _2 in Combinator.Many(
                //from _3 in Combinator.Choice(Plus, Minus)
                //from _4 in MultiplicativeExpression
                //select (Func<Expressions, Expressions>)(x => new Expressions.BuiltinOp(_3, new Expressions[] { x, _4 }))
                from _3 in InfixOpOf("+-")
                from _4 in MultiplicativeExpression
                select (Func<Expressions, Expressions>)(x => new Expressions.AppExp(new Expressions.AppExp(new Expressions.Var(_3), x), _4))
            )
            select _2.Aggregate(_1, (s, x) => x(s));

        private static readonly Parser<Expressions> CapExpression =
            from _1 in AdditiveExpression
            from _2 in (
                from _3 in InfixOpOf("^")
                from _4 in AdditiveExpression
                select Tuple.Create(_3,_4)//(Func<Expressions, Expressions>)(x => new Expressions.AppExp(new Expressions.AppExp(new Expressions.Var(_3), x), _4))
            ).Many()
            let exprs = new [] {_1}.Concat(_2.Select(x => x.Item2)).Reverse().ToArray()
            let e = exprs.First()
            let es = exprs.Skip(1).Zip(_2.Select(x => x.Item1).Reverse(), Tuple.Create).ToArray()
            select es.Aggregate(e, (s, x) => new Expressions.AppExp(new Expressions.AppExp(new Expressions.Var(x.Item2), x.Item1), s));

        private static readonly Parser<Expressions> ConsExpression =
            from _1 in CapExpression
            from _2 in (
                           from _3 in InfixOpOf(":")
                           from _4 in CapExpression
                           select Tuple.Create(_3, _4)//(Func<Expressions, Expressions>)(x => new Expressions.AppExp(new Expressions.AppExp(new Expressions.Var(_3), x), _4))
                       ).Many()
            let exprs = new[] { _1 }.Concat(_2.Select(x => x.Item2)).Reverse().ToArray()
            let e = exprs.First()
            let es = exprs.Skip(1).Zip(_2.Select(x => x.Item1).Reverse(), Tuple.Create).ToArray()
            select es.Aggregate(e, (s, x) => new Expressions.AppExp(new Expressions.AppExp(new Expressions.Var(x.Item2), x.Item1), s));

        //private static readonly Parser<Expressions> RelationalExpression =
        //    from _1 in ConsExpression
        //    from _2 in Combinator.Many(
        //        from _3 in Combinator.Not(Ne).Then(Combinator.Choice(Le, Lt, Ge, Gt))
        //        from _4 in ConsExpression
        //        select (Func<Expressions, Expressions>)(x => new Expressions.BuiltinOp(_3, new Expressions[] { x, _4 }))
        //    )
        //    select _2.Aggregate(_1, (s, x) => x(s));

        private static readonly Parser<Expressions> EqualityExpression =
            from _1 in ConsExpression
            from _2 in Combinator.Many(
                from _3 in InfixOpOf("!<>=")
                from _4 in ConsExpression
                select (Func<Expressions, Expressions>)(x => new Expressions.AppExp(new Expressions.AppExp(new Expressions.Var(_3), x), _4))
            )
            select _2.Aggregate(_1, (s, x) => x(s));

        private static readonly Parser<Expressions> LogicalAndExpression =
            from _1 in EqualityExpression
            from _2 in Combinator.Many(
                from _3 in InfixOpOf("&")
                //from _3 in LAnd
                from _4 in EqualityExpression
                select _3 == "&&" ? (Func<Expressions, Expressions>)(x => new Expressions.IfExp(x, _4, new Expressions.BoolLit(false)))
                                  : (Func<Expressions, Expressions>)(x => new Expressions.AppExp(new Expressions.AppExp(new Expressions.Var(_3), x), _4))
            )
            select _2.Aggregate(_1, (s, x) => x(s));

        private static readonly Parser<Expressions> LogicalOrExpression =
            from _1 in LogicalAndExpression
            from _2 in Combinator.Many(
                from _3 in InfixOpOf("|")
                where _3 != "|"
                //from _3 in LOr
                from _4 in LogicalAndExpression
                select _3 == "||" ? (Func<Expressions, Expressions>)(x => new Expressions.IfExp(x, new Expressions.BoolLit(true), _4))
                                  : (Func<Expressions, Expressions>)(x => new Expressions.AppExp(new Expressions.AppExp(new Expressions.Var(_3), x), _4))
            )
            select _2.Aggregate(_1, (s, x) => x(s));

        private static readonly Parser<TypeExp> SimpleType =
            from _1 in Combinator.Choice(
                Quote.Then(Id).Select(x => (TypeExp) new TypeExp.TypeVar("'" + x)),
                from _1 in Int select (TypeExp)new TypeExp.IntType(),
                from _1 in Bool select (TypeExp)new TypeExp.BoolType(),
                from _1 in Unit select (TypeExp)new TypeExp.UnitType(),
                from _1 in String select (TypeExp)new TypeExp.StrType(),
                Id.Select(x => (TypeExp)new TypeExp.TypeConstruct(new TypeExp.TypeName(x), new TypeExp[0])),
                from _1 in LParen from _2 in Combinator.Lazy(() => TypeExpr).Repeat1(Comma) from _3 in RParen from _4 in Id.Select(x => new TypeExp.TypeName(x)) select (TypeExp) new TypeExp.TypeConstruct(_4, _2),
                from _1 in LParen from _2 in Combinator.Lazy(() => TypeExpr) from _3 in RParen select _2
            )
            from _2 in Id.Select(x => new TypeExp.TypeName(x)).Many()
            select _2.Aggregate(_1, (s, y) => (TypeExp) new TypeExp.TypeConstruct(y, new TypeExp[] {s}));

        private static readonly Parser<TypeExp> TypeExprTuple =
            SimpleType.Repeat1(Mult)
                      .Select(x => (x.Length > 1
                                        ? x.Reverse()
                                           .Aggregate(TypeExp.TupleType.Tail, (s, y) => new TypeExp.TupleType(y, s))
                                        : x[0]));

        private static readonly Parser<TypeExp> TypeExprFunc =
            Combinator.Choice(
                from _1 in TypeExprTuple
                from _2 in RArrow.Then(Combinator.Lazy(()=>TypeExprFunc)).Option()
                select _2 == null ? _1 : (TypeExp)new TypeExp.FuncType(_1, _2)
            );

        private static readonly Parser<TypeExp> TypeExpr =
            TypeExprFunc;

        private static readonly Parser<Toplevel> TopLevel =
            Combinator.Choice(
                (
                    from _1 in Expr
                    from _2 in SemiSemi
                    select (Toplevel)new Toplevel.Exp(_1)
                ), (
                    from _1 in External
                    from _2 in Id
                    from _3 in Colon
                    from _4 in TypeExpr
                    from _5 in Eq
                    from _6 in StrV
                    from _7 in SemiSemi
                    select (Toplevel)new Toplevel.ExternalDecl(_2, _4, _6)
                ), (
                       from _1 in Type
                       from _2 in Combinator.Choice(
                                   from _3 in LParen
                                   from _4 in Quote.Then(Id).Select(x => "'"+x).Repeat1(Comma)
                                   from _5 in RParen
                                   select _4,
                                   Quote.Then(Id).Select(x => new [] { "'" + x })
                               ).Option()
                        from _3 in Id
                        from _4 in Eq
                        from _5 in TypeExpr
                       select (Toplevel)new Toplevel.TypeDef(_3, _2 == null ? new string[] {} : _2, _5)
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
