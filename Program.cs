using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Parsing;

namespace ParserCombinator {
    class Program {
        static void Main(string[] args) {
            ML1.main.run();
        }
    }

    public static class ML1 {

        public abstract class Syntax {
            public class Var : Syntax {
                public string Id { get; }

                public Var(string id) {
                    Id = id;
                }

                public override string ToString() {
                    return $"{Id}";
                }
            }

            public class ILit : Syntax {
                public int Value { get; }

                public ILit(int value) {
                    Value = value;
                }

                public override string ToString() {
                    return $"{Value}";
                }
            }

            public class BLit : Syntax {
                public bool Value { get; }

                public BLit(bool value) {
                    Value = value;
                }

                public override string ToString() {
                    return $"{Value}";
                }
            }
            public class BinOp : Syntax {
                public enum Kind {
                    Plus,
                    Mult,
                    Lt,
                    Eq
                }

                public BinOp(Kind op, Syntax left, Syntax right) {
                    Op = op;
                    Left = left;
                    Right = right;
                }

                public Kind Op { get; }
                public Syntax Left { get; }
                public Syntax Right { get; }
            }
            public class IfExp : Syntax {
                public IfExp(Syntax cond, Syntax then, Syntax @else) {
                    Cond = cond;
                    Then = then;
                    Else = @else;
                }

                public Syntax Cond { get; }
                public Syntax Then { get; }
                public Syntax Else { get; }
            }
            public class LetExp : Syntax {
                public LetExp(Tuple<string, Syntax>[] binds, Syntax body) {
                    Binds = binds;
                    Body = body;
                }

                public Tuple<string, Syntax>[] Binds { get; }
                public Syntax Body { get; }
            }
            public class FunExp : Syntax {
                public FunExp(string arg, Syntax body) {
                    Arg = arg;
                    Body = body;
                }

                public string Arg { get; }
                public Syntax Body { get; }
            }
            public class AppExp : Syntax {
                public AppExp(Syntax fun, Syntax arg) {
                    Fun = fun;
                    Arg = arg;
                }

                public Syntax Fun { get; }
                public Syntax Arg { get; }
            }
        }

        public abstract class Program {
            public class Exp : Program {
                public Exp(Syntax syntax) {
                    Syntax = syntax;
                }

                public Syntax Syntax { get; }
            }
            public class Decl : Program {
                public Decl(Tuple<string, Syntax>[] binds) {
                    Binds = binds;
                }

                public Tuple<string, Syntax>[] Binds { get; }
            }
            public class Decls : Program {
                public Decls(Decl[] entries) {
                    Entries = entries;
                }

                public Decl[] Entries { get; }
            }
        }

        public static class Parser {
            private static readonly Parser<string> WhiteSpace = Combinator.Regex(@"\s*");

            private static readonly Parser<string> True = WhiteSpace.Then(Combinator.Token("true"));
            private static readonly Parser<string> False = WhiteSpace.Then(Combinator.Token("false"));
            private static readonly Parser<string> If = WhiteSpace.Then(Combinator.Token("if"));
            private static readonly Parser<string> Then = WhiteSpace.Then(Combinator.Token("then"));
            private static readonly Parser<string> Else = WhiteSpace.Then(Combinator.Token("else"));
            private static readonly Parser<string> Let = WhiteSpace.Then(Combinator.Token("let"));
            private static readonly Parser<string> In = WhiteSpace.Then(Combinator.Token("in"));
            private static readonly Parser<string> And = WhiteSpace.Then(Combinator.Token("and"));
            private static readonly Parser<string> Fun = WhiteSpace.Then(Combinator.Token("fun"));

            private static readonly Parser<int> IntV = WhiteSpace.Then(Combinator.Regex(@"-?[0-9]+")).Select(int.Parse);
            private static readonly Parser<string> LParen = WhiteSpace.Then(Combinator.Token("("));
            private static readonly Parser<string> RParen = WhiteSpace.Then(Combinator.Token(")"));
            private static readonly Parser<string> SemiSemi = WhiteSpace.Then(Combinator.Token(";;"));
            private static readonly Parser<string> RArrow = WhiteSpace.Then(Combinator.Token("->"));
            private static readonly Parser<Syntax.BinOp.Kind> Plus = WhiteSpace.Then(Combinator.Token("+")).Select(x => Syntax.BinOp.Kind.Plus);
            private static readonly Parser<Syntax.BinOp.Kind> Mult = WhiteSpace.Then(Combinator.Token("*")).Select(x => Syntax.BinOp.Kind.Mult);
            private static readonly Parser<Syntax.BinOp.Kind> Lt = WhiteSpace.Then(Combinator.Token("<")).Select(x => Syntax.BinOp.Kind.Lt);
            private static readonly Parser<Syntax.BinOp.Kind> Eq = WhiteSpace.Then(Combinator.Token("=")).Select(x => Syntax.BinOp.Kind.Eq);
            private static readonly Parser<string> Id = WhiteSpace.Then(Combinator.Not(Combinator.Choice(True, False,If,Then,Else,Let,In, And, Fun))).Then(Combinator.Regex(@"[a-z][a-z0-9_']*"));

            private static readonly Parser<Syntax> Expr = Combinator.Lazy(() => Combinator.Choice(IfExpr, LetExpr, FunExpr, LTExpr));

            private static readonly Parser<Syntax> FunExpr =
                from _1 in Fun
                from _2 in Id
                from _3 in RArrow
                from _4 in Expr
                select (Syntax)new Syntax.FunExp(_2, _4);

            private static readonly Parser<Tuple<string,Syntax>> LetBind =
                from _1 in Id
                from _2 in Eq
                from _3 in Expr
                select Tuple.Create(_1, _3);

            private static readonly Parser<Syntax> LetExpr =
                from _1 in Let
                from _2 in (
                                from _3 in LetBind 
                                from _4 in And.Then(LetBind).Many()
                                select new [] {_3}.Concat(_4)
                            )
                from _3 in In
                from _4 in Expr
                select (Syntax)new Syntax.LetExp(_2.ToArray(), _4);

            private static readonly Parser<Syntax> IfExpr =
                from _1 in If
                from _2 in Expr
                from _3 in Then
                from _4 in Expr
                from _5 in Else
                from _6 in Expr
                select (Syntax)new Syntax.IfExp(_2, _4, _6);

            private static readonly Parser<Syntax> AExpr =
                Combinator.Choice(
                    from _1 in IntV select (Syntax)new Syntax.ILit(_1),
                    from _1 in True select (Syntax)new Syntax.BLit(true),
                    from _1 in False select (Syntax)new Syntax.BLit(false),
                    from _1 in Id select (Syntax)new Syntax.Var(_1),
                    from _1 in LParen from _2 in Expr from _3 in RParen select _2
                );

            private static readonly Parser<Syntax> AppExpr =
                from _1 in AExpr.Many(1)
                select _1.Aggregate((s, x) => new Syntax.AppExp(s, x));

            private static readonly Parser<Syntax> MExpr =
                from _1 in AppExpr
                from _2 in Combinator.Many(
                    from _3 in Mult
                    from _4 in AppExpr
                    select (Func<Syntax, Syntax>)(x => new Syntax.BinOp(_3, x, _4))
                )
                select _2.Aggregate(_1, (s, x) => x(s));

            private static readonly Parser<Syntax> PExpr =
                from _1 in MExpr
                from _2 in Combinator.Many(
                    from _3 in Plus
                    from _4 in MExpr
                    select (Func<Syntax, Syntax>)(x => new Syntax.BinOp(_3, x, _4))
                )
                select _2.Aggregate(_1, (s, x) => x(s));

            private static readonly Parser<Syntax> LTExpr =
                from _1 in PExpr
                from _2 in Combinator.Option(
                    from _3 in Lt
                    from _4 in PExpr
                    select (Func<Syntax, Syntax>)(x => new Syntax.BinOp(_3, x, _4))
                )
                select _2 == null ? _1 : _2(_1);


            private static readonly Parser<Program> TopLevel =
                Combinator.Choice(
                    (
                        from _1 in Expr
                        from _2 in SemiSemi
                        select (Program)new Program.Exp(_1)
                    ), (
                        from _1 in Combinator.Many(
                            from _2 in Let
                            from _3 in (
                                from _4 in LetBind
                                from _5 in And.Then(LetBind).Many()
                                select new[] { _4 }.Concat(_5)
                            )
                            select new Program.Decl(_3.ToArray())
                        )
                        from _6 in SemiSemi
                        select (Program) new Program.Decls(_1)
                    )
                );

            public static Result<Program> Parse(string s) {
                return TopLevel(s, 0);
            }

        }

        /// <summary>
        /// 環境の実装 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public class Environment<T> {

            public string Id { get; }
            public T Value { get; }
            public Environment<T> Next { get; }
            public static Environment<T> Empty { get; } = new Environment<T>(null, default(T), null);
            public Environment(string id, T value, Environment<T> next) {
                Id = id;
                Value = value;
                Next = next;
            }
        }

        public static class Environment {
            public class NotBound : Exception {
                public NotBound(string s) : base(s) {
                }
            }

            public static Environment<T> Extend<T>(string x, T v, Environment<T> env) {
                return new Environment<T>(x, v, env);
            }
            public static T LookUp<T>(string x, Environment<T> env) {
                for (var e = env; e != Environment<T>.Empty; e = e.Next) {
                    if (e.Id == x) { return e.Value; }
                }
                throw new NotBound(x);
            }
            public static Environment<T2> Map<T1, T2>(Func<T1, T2> f, Environment<T1> env) {
                List<Tuple<string, T2>> kv = new List<Tuple<string, T2>>();
                for (var e = env; e != Environment<T1>.Empty; e = e.Next) {
                    kv.Add(Tuple.Create(e.Id, f(e.Value)));
                }
                return kv.Reverse<Tuple<string, T2>>().Aggregate(Environment<T2>.Empty, (s, x) => new Environment<T2>(x.Item1, x.Item2, s));
            }

            public static T2 FoldRight<T1, T2>(Func<T2, T1, T2> f, Environment<T1> env, T2 a) {
                List<T1> kv = new List<T1>();
                for (var e = env; e != Environment<T1>.Empty; e = e.Next) {
                    kv.Add(e.Value);
                }
                return kv.Reverse<T1>().Aggregate(a, f);
            }
        }

        public abstract class ExprValue {
            public class IntV : ExprValue {
                public int Value { get; }

                public IntV(int value) {
                    Value = value;
                }

                public override string ToString() {
                    return $"{Value}";
                }
            }
            public class BoolV : ExprValue {
                public bool Value { get; }

                public BoolV(bool value) {
                    Value = value;
                }

                public override string ToString() {
                    return $"{Value}";
                }
            }
            public class ProcV : ExprValue {
                public string Id { get; }
                public Syntax Body { get; }
                public Environment<ExprValue> Env { get; }

                public ProcV(string id, Syntax body, Environment<ExprValue> env) {
                    Id = id;
                    Body = body;
                    Env = env;
                }

                public override string ToString() {
                    return $"{Id} -> {Body}";
                }
            }

        }

        /// <summary>
        /// 評価部
        /// </summary>
        public static class Eval {
            /// <summary>
            /// 評価結果
            /// </summary>
            public class Result {
                public Result(string id, Environment<ExprValue> env, ExprValue value) {
                    Id = id;
                    Env = env;
                    Value = value;
                }

                public string Id { get; }
                public Environment<ExprValue> Env { get; }
                public ExprValue Value { get; }
            }

      
            private static ExprValue ApplyPrim(Syntax.BinOp.Kind op, ExprValue arg1, ExprValue arg2) {
                switch (op) {
                    case Syntax.BinOp.Kind.Plus: {
                            if (arg1 is ExprValue.IntV && arg2 is ExprValue.IntV) {
                                var i1 = ((ExprValue.IntV)arg1).Value;
                                var i2 = ((ExprValue.IntV)arg2).Value;
                                return new ExprValue.IntV(i1 + i2);
                            }
                            throw new Exception("Both arguments must be integer: +");

                        }
                    case Syntax.BinOp.Kind.Mult: {
                            if (arg1 is ExprValue.IntV && arg2 is ExprValue.IntV) {
                                var i1 = ((ExprValue.IntV)arg1).Value;
                                var i2 = ((ExprValue.IntV)arg2).Value;
                                return new ExprValue.IntV(i1 * i2);
                            }
                            throw new Exception("Both arguments must be integer: *");
                        }
                    case Syntax.BinOp.Kind.Lt: {
                            if (arg1 is ExprValue.IntV && arg2 is ExprValue.IntV) {
                                var i1 = ((ExprValue.IntV)arg1).Value;
                                var i2 = ((ExprValue.IntV)arg2).Value;
                                return new ExprValue.BoolV(i1 < i2);
                            }
                            throw new Exception("Both arguments must be integer: <");
                        }
                    default:
                        throw new ArgumentOutOfRangeException(nameof(op), op, null);
                }
            }

            public static ExprValue eval_exp(Environment<ExprValue> env, Syntax e) {
                if (e is Syntax.Var) {
                    var x = ((Syntax.Var)e).Id;
                    try {
                        return Environment.LookUp(x, env);
                    } catch (Environment.NotBound) {
                        throw new Exception($"Variable not bound: {x}");
                    }
                }
                if (e is Syntax.ILit) {
                    return new ExprValue.IntV(((Syntax.ILit)e).Value);
                }
                if (e is Syntax.BLit) {
                    return new ExprValue.BoolV(((Syntax.BLit)e).Value);
                }
                if (e is Syntax.BinOp) {
                    var op = ((Syntax.BinOp)e).Op;
                    var arg1 = eval_exp(env, ((Syntax.BinOp)e).Left);
                    var arg2 = eval_exp(env, ((Syntax.BinOp)e).Right);
                    return ApplyPrim(op, arg1, arg2);
                }
                if (e is Syntax.IfExp) {
                    var cond = eval_exp(env, ((Syntax.IfExp)e).Cond);
                    if (cond is ExprValue.BoolV) {
                        var v = ((ExprValue.BoolV)cond).Value;
                        if (v) {
                            return eval_exp(env, ((Syntax.IfExp)e).Then);
                        } else {
                            return eval_exp(env, ((Syntax.IfExp)e).Else);
                        }
                    }
                    throw new Exception("Test expression must be boolean: if");
                }
                if (e is Syntax.LetExp)
                {
                    var newenv = env;
                    foreach (var bind in ((Syntax.LetExp)e).Binds) { 
                        var value = eval_exp(env, bind.Item2);
                        newenv = Environment.Extend(bind.Item1, value, newenv);
                    }
                    return eval_exp(newenv, ((Syntax.LetExp)e).Body);
                }
                if (e is Syntax.FunExp) {
                    return new ExprValue.ProcV(((Syntax.FunExp)e).Arg, ((Syntax.FunExp)e).Body, env);
                }
                if (e is Syntax.AppExp) {
                    var funval = eval_exp(env, ((Syntax.AppExp)e).Fun);
                    var arg = eval_exp(env, ((Syntax.AppExp)e).Arg);
                    if (funval is ExprValue.ProcV)
                    {
                        var newenv = Environment.Extend(((ExprValue.ProcV)funval).Id, arg, ((ExprValue.ProcV)funval).Env);
                        return eval_exp(newenv, ((ExprValue.ProcV)funval).Body);
                    }
                    else
                    {
                        throw new NotSupportedException($"{funval.GetType().FullName} cannot eval.");
                    }
                }
                throw new NotSupportedException($"{e.GetType().FullName} cannot eval.");
            }

            public static Result eval_decl(Environment<ExprValue> env, Program p) {
                if (p is Program.Exp) {
                    var e = (Program.Exp)p;
                    var v = eval_exp(env, e.Syntax);
                    return new Result("-", env, v);
                }
                if (p is Program.Decl) {
                    var d = (Program.Decl)p;
                    var ret = new Result("", env, (ExprValue)null);
                    foreach (var bind in d.Binds) {
                        var v = eval_exp(env, bind.Item2);
                        ret = new Result(bind.Item1, Environment.Extend(bind.Item1, v, ret.Env), v);
                    }
                    return ret;
                }
                if (p is Program.Decls) {
                    var ds = (Program.Decls)p;
                    var newenv = env;
                    Result ret = new Result("", env, (ExprValue)null);
                    foreach (var d in ds.Entries) {
                        foreach (var bind in d.Binds) {
                            var v = eval_exp(newenv, bind.Item2);
                            ret = new Result(bind.Item1, Environment.Extend(bind.Item1, v, ret.Env), v);
                        }
                        newenv = ret.Env;
                    }
                    return ret;
                }
                throw new NotSupportedException($"{p.GetType().FullName} cannot eval.");
            }
        }

        public static class main {
            public static void read_eval_print(Environment<ExprValue> env) {
                for (;;) {
                    Console.Write("# ");
                    var decl = Parser.Parse(Console.ReadLine());
                    if (decl.Success) {
                        var ret = Eval.eval_decl(env, decl.Value);
                        Console.WriteLine($"val {ret.Id} = {ret.Value}");
                        env = ret.Env;
                    } else {
                        Console.WriteLine($"Syntax error.");
                    }
                }
            }
            public static void file_eval_print(Environment<ExprValue> env, TextReader tr) {
                for (;;) {
                    var decl = Parser.Parse(tr.ReadLine());
                    if (decl.Success) {
                        var ret = Eval.eval_decl(env, decl.Value);
                        env = ret.Env;
                    } else {
                        Console.WriteLine($"Syntax error.");
                    }
                }
            }
            public static Environment<ExprValue> initial_env() {

                return Environment.Extend(
                    "i",
                    new ExprValue.IntV(1),
                    Environment.Extend(
                        "Value",
                        new ExprValue.IntV(5),
                        Environment.Extend(
                            "x",
                            new ExprValue.IntV(10),
                            Environment<ExprValue>.Empty
                        )
                    )
                );
            }

            public static void run() {
                read_eval_print(initial_env());
            }
        }
    }
}
