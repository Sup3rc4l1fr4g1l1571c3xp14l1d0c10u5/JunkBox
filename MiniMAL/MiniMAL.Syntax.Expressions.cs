using System;
using System.ComponentModel;
using System.Linq;
using System.Numerics;

namespace MiniMAL
{
    namespace Syntax
    {
        /// <summary>
        /// 式
        /// </summary>
        public abstract class Expressions
        {

            /// <summary>
            /// 変数式
            /// </summary>
            public class Var : Expressions
            {
                private static int _counter;

                public static Var Fresh()
                {
                    return new Var($"${_counter++}");
                }

                public string Id { get; }

                public Var(string id)
                {
                    Id = id;
                }

            }

            /// <summary>
            /// 整数式
            /// </summary>
            public class IntLit : Expressions
            {
                public BigInteger Value { get; }

                public IntLit(BigInteger value)
                {
                    Value = value;
                }

            }

            /// <summary>
            /// 文字列式
            /// </summary>
            public class StrLit : Expressions
            {
                public string Value { get; }

                public StrLit(string value)
                {
                    Value = value;
                }

            }

            /// <summary>
            /// 真偽式
            /// </summary>
            public class BoolLit : Expressions
            {
                public bool Value { get; }

                public BoolLit(bool value)
                {
                    Value = value;
                }

            }

            /// <summary>
            /// 空リスト
            /// </summary>
            public class EmptyListLit : Expressions { }

            /// <summary>
            /// Option値
            /// </summary>
            public class OptionExp : Expressions
            {
                public static OptionExp None { get; } = new OptionExp(null);
                public Expressions Expr { get; }

                public OptionExp(Expressions expr)
                {
                    Expr = expr;
                }

            }

            /// <summary>
            /// Unit値
            /// </summary>
            public class UnitLit : Expressions { }

            /// <summary>
            /// タプル式
            /// </summary>
            public class TupleExp : Expressions
            {
                public Expressions[] Members { get; }
                public TupleExp(Expressions[] members)
                {
                    Members = members;
                }
            }

            public class RecordExp : Expressions
            {
                public Tuple<string,Expressions>[] Members { get; }
                public RecordExp(Tuple<string, Expressions>[] members)
                {
                    Members = members;
                }
            }


            /// <summary>
            /// if式
            /// </summary>
            public class IfExp : Expressions
            {
                public IfExp(Expressions cond, Expressions then, Expressions @else)
                {
                    Cond = cond;
                    Then = then;
                    Else = @else;
                }

                public Expressions Cond { get; }
                public Expressions Then { get; }
                public Expressions Else { get; }

            }

            /// <summary>
            /// let式
            /// </summary>
            public class LetExp : Expressions
            {
                public LetExp(Tuple<string, Expressions>[] binds, Expressions body)
                {
                    Binds = binds;
                    Body = body;
                }

                public Tuple<string, Expressions>[] Binds { get; }
                public Expressions Body { get; }

            }

            /// <summary>
            /// 無名関数式
            /// </summary>
            public class FunExp : Expressions
            {
                public FunExp(string arg, Expressions body, TypeExpressions argTy, TypeExpressions bodyTy)
                {
                    Arg = arg;
                    Body = body;
                    ArgTy = argTy;
                    BodyTy = bodyTy;
                }

                public string Arg { get; }
                public Expressions Body { get; }

                public TypeExpressions ArgTy { get; }
                public TypeExpressions BodyTy { get; }

            }

            /// <summary>
            /// 関数適用式
            /// </summary>
            public class AppExp : Expressions
            {
                public AppExp(Expressions fun, Expressions arg)
                {
                    Fun = fun;
                    Arg = arg;
                }

                public Expressions Fun { get; }
                public Expressions Arg { get; }

            }

            /// <summary>
            /// let-rec式
            /// </summary>
            public class LetRecExp : Expressions
            {
                public LetRecExp(Tuple<string, Expressions>[] binds, Expressions body)
                {
                    Binds = binds;
                    Body = body;
                }

                public Tuple<string, Expressions>[] Binds { get; }
                public Expressions Body { get; }

            }

            /// <summary>
            /// match式
            /// </summary>
            public class MatchExp : Expressions
            {
                public MatchExp(Expressions exp, Tuple<PatternExpressions, Expressions>[] patterns)
                {
                    Exp = exp;
                    Patterns = patterns;
                }

                public Expressions Exp { get; }
                public Tuple<PatternExpressions, Expressions>[] Patterns { get; }

            }

            /// <summary>
            /// Halt式
            /// </summary>
            public class HaltExp : Expressions
            {
                public string Message { get; }

                public HaltExp(string message)
                {
                    Message = message;
                }

            }

            private static int Priolity(Expressions exp)
            {
                if (exp is Var) { return 100; }
                if (exp is IntLit) { return 100; }
                if (exp is StrLit) { return 100; }
                if (exp is BoolLit) { return 100; }
                if (exp is EmptyListLit) { return 100; }
                if (exp is OptionExp) { return 100; }
                if (exp is UnitLit) { return 100; }
                if (exp is TupleExp) { return 100; }
                if (exp is RecordExp) { return 100; }
                if (exp is IfExp) { return 3; }
                if (exp is LetExp) { return 100; }
                if (exp is FunExp) { return 1; }
                if (exp is AppExp) { return 2; }
                if (exp is LetRecExp) { return 100; }
                if (exp is MatchExp) { return 1; }
                if (exp is HaltExp) { return 2; }
                                if (exp is TupleExp) { return 100; }
throw new NotSupportedException();

            }

            private static string ExpressionToStringBody(int p, Expressions exp)
            {
                if (exp is Var) { return $"{((Var)exp).Id}"; }
                if (exp is IntLit) { return $"{((IntLit)exp).Value}"; }
                if (exp is StrLit) { return $"\"{((StrLit)exp).Value.Replace("\\", "\\\\").Replace("\"", "\\\"")}\""; }
                if (exp is BoolLit) { return $"{((BoolLit)exp).Value}"; }
                if (exp is EmptyListLit) { return "[]"; }
                if (exp is OptionExp)
                {
                    var e = (OptionExp)exp;
                    if (ReferenceEquals(e, OptionExp.None))
                    {
                        return "None";
                    }
                    else if (e.Expr is TupleExp)
                    {
                        return "Some" + ExpressionToString(p, e.Expr);
                    }
                    else
                    {
                        return "Some " + ExpressionToString(p, e.Expr);
                    }
                }
                if (exp is UnitLit)
                {
                    return "()";
                }
                if (exp is TupleExp)
                {
                    var e = (TupleExp)exp;
                    return "(" + string.Join(",", e.Members.Select(x => x.ToString())) + ")";
                }
                if (exp is RecordExp)
                {
                    var e = (RecordExp)exp;
                    return "{" + string.Join(",", e.Members.Select(x => $"{x.Item1.ToString()} = {x.Item2.ToString()}")) + "}";
                }
                if (exp is IfExp)
                {
                    var e = (IfExp)exp;
                    return $"if {ExpressionToString(p, e.Cond)} then {ExpressionToString(p, e.Then)} else {ExpressionToString(p, e.Else)}";
                }
                if (exp is LetExp)
                {
                    var e = (LetExp)exp;
                    return $"let {string.Join(" and ", e.Binds.Select(x => $"{x.Item1} = {ExpressionToString(0, x.Item2)}"))} in {ExpressionToString(0, e.Body)}";
                }
                if (exp is FunExp)
                {
                    var e = (FunExp)exp;
                    return $"fun ({e.Arg} : {e.ArgTy}) : {e.BodyTy} -> {ExpressionToString(0, e.Body)}";
                }
                if (exp is AppExp)
                {
                    var e = (AppExp)exp;
                    return $"{ExpressionToString(p, e.Fun)} {ExpressionToString(p, e.Arg)}";
                }
                if (exp is LetRecExp)
                {
                    var e = (LetRecExp)exp;
                    return $"let rec {string.Join(" and ", e.Binds.Select(x => $"{x.Item1} = {ExpressionToString(0, x.Item2)}"))} in {ExpressionToString(0, e.Body)}";
                }
                if (exp is MatchExp)
                {
                    var e = (MatchExp)exp;
                    return $"match {ExpressionToString(0, e.Exp)} with {string.Join("| ", e.Patterns.Select(x => $"{x.Item1} -> {x.Item2}"))}";
                }
                if (exp is HaltExp)
                {
                    var e = (HaltExp)exp;
                    return $"halt \"{e.Message.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"";
                }
                throw new NotSupportedException();
            }

            private static string ExpressionToString(int pri, Expressions exp)
            {
                var p = Priolity(exp);
                var s = ExpressionToStringBody(p, exp);
                return p >= pri ? s : $"({s})";
            }

            public override string ToString()
            {
                return ExpressionToString(0, this);
            }
        }
    }

}
