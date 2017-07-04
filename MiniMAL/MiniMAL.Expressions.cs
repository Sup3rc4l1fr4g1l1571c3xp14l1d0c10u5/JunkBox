using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;

namespace MiniMAL {
    /// <summary>
    /// 式
    /// </summary>
    public abstract class Expressions {

        /// <summary>
        /// 変数式
        /// </summary>
        public class Var : Expressions {
            public string Id { get; }

            public Var(string id) {
                Id = id;
            }

            //public override string ToString() {
            //    return $"{Id}";
            //}
        }

        /// <summary>
        /// 整数式
        /// </summary>
        public class IntLit : Expressions {
            public BigInteger Value { get; }

            public IntLit(BigInteger value) {
                Value = value;
            }

            //public override string ToString() {
            //    return $"{Value}";
            //}
        }

        /// <summary>
        /// 文字列式
        /// </summary>
        public class StrLit : Expressions {
            public string Value { get; }

            public StrLit(string value) {
                Value = value;
            }

            //public override string ToString() {
            //    return $"\"{Value.Replace("\"", "\\\"")}\"";
            //}
        }

        /// <summary>
        /// 真偽式
        /// </summary>
        public class BoolLit : Expressions {
            public bool Value { get; }

            public BoolLit(bool value) {
                Value = value;
            }

            //public override string ToString() {
            //    return $"{Value}";
            //}
        }

        /// <summary>
        /// 空リスト
        /// </summary>
        public class EmptyListLit : Expressions {

            public EmptyListLit() { }

            //public override string ToString() {
            //    return $"[]";
            //}
        }

        /// <summary>
        /// Option型値
        /// </summary>
        public class OptionExp : Expressions {
            public static OptionExp None { get; } = new OptionExp(null);
            public Expressions Expr { get; }

            public OptionExp(Expressions expr) {
                Expr = expr;
            }

            //public override string ToString() {
            //    if (this == None)
            //    {
            //        return $"None";
            //    } else {
            //        return $"Some {Expr}";
            //    }
            //}
        }

        /// <summary>
        /// Unit
        /// </summary>
        public class UnitLit : Expressions {

            public UnitLit() {
            }

            //public override string ToString() {
            //    return $"()";
            //}
        }

        /// <summary>
        /// タプル式
        /// </summary>
        public class TupleExp : Expressions {
            public static TupleExp Tail { get; } = new TupleExp(null, null);
            public TupleExp(Expressions car, TupleExp cdr) {
                Car = car;
                Cdr = cdr;
            }

            public Expressions Car { get; }
            public TupleExp Cdr { get; }

            //public override string ToString() {
            //    StringBuilder sb = new StringBuilder();
            //    sb.Append("(");
            //    var it = this;
            //    if (!ReferenceEquals(it, Tail)) {
            //        sb.Append($"{it.Car}");
            //        it = it.Cdr;
            //        while (!ReferenceEquals(it, Tail)) {
            //            sb.Append($", {it.Car}");
            //            it = it.Cdr;
            //        }
            //    }
            //    sb.Append(")");
            //    return sb.ToString();
            //}
        }

        /// <summary>
        /// 組み込み演算子
        /// </summary>
        public class BuiltinOp : Expressions {
            public enum Kind {
                // unary operators
                UnaryPlus,
                UnaryMinus,
                // binary operators
                Plus,
                Minus,
                Mult,
                Div,
                Lt,
                Gt,
                Le,
                Ge,
                Eq,
                Ne,
                ColCol,
                // builtin operators 
                Head,
                Tail,
                IsCons,
                Car,
                Cdr,
                IsTail,
                IsTuple,
                Length,
                IsNone,
                IsSome,
                Get

            }
            public static IDictionary<Kind, string> BinOpToStr { get; }= new Dictionary<Kind, string>(){
                { Kind.UnaryPlus, "+" },
                { Kind.UnaryMinus, "-" },
                { Kind.Plus, "+" },
                { Kind.Minus, "-" },
                { Kind.Mult, "*" },
                { Kind.Div, "/" },
                { Kind.Lt, "<" },
                { Kind.Gt, ">" },
                { Kind.Le, "<=" },
                { Kind.Ge, ">=" },
                { Kind.Eq, "=" },
                { Kind.Ne, "<>" },
                { Kind.ColCol, "::" },
                { Kind.Head, "@Head" },
                { Kind.Tail, "@Tail" },
                { Kind.IsCons, "@IsCons" },
                { Kind.Car, "@Car" },
                { Kind.Cdr, "@Cdr" },
                { Kind.IsTail, "@IsTail" },
                { Kind.IsTuple, "@IsTuple" },
                { Kind.Length, "@Length" },
                { Kind.IsNone, "@IsNone" },
                { Kind.IsSome, "@IsSome" },
                { Kind.Get, "@Get" },
            };

            public BuiltinOp(Kind op, Expressions[] exprs) {
                Op = op;
                Exprs = exprs;
            }

            public Kind Op { get; }
            public Expressions[] Exprs { get; }
            //public override string ToString() {
            //    switch (Op)
            //    {
            //        case Kind.UnaryPlus:
            //        case Kind.UnaryMinus:
            //            return $"({BinOpToStr[Op]} {Exprs[1]})";
            //        case Kind.Plus:
            //        case Kind.Minus:
            //        case Kind.Mult:
            //        case Kind.Div:
            //        case Kind.Lt:
            //        case Kind.Gt:
            //        case Kind.Le:
            //        case Kind.Ge:
            //        case Kind.Eq:
            //        case Kind.Ne:
            //        case Kind.ColCol:
            //            return $"({Exprs[0]} {BinOpToStr[Op]} {Exprs[1]})";
            //        case Kind.Head:
            //        case Kind.Tail:
            //        case Kind.IsCons:
            //        case Kind.Car:
            //        case Kind.Cdr:
            //        case Kind.IsTail:
            //        case Kind.IsTuple:
            //        case Kind.Length:
            //        case Kind.IsNone:
            //        case Kind.IsSome:
            //        case Kind.Get:
            //            return $"({BinOpToStr[Op]} {string.Join(" ", Exprs.Select(x => x.ToString()))})";
            //        default:
            //            throw new ArgumentOutOfRangeException();
            //    }
            //}
        }

        /// <summary>
        /// if式
        /// </summary>
        public class IfExp : Expressions {
            public IfExp(Expressions cond, Expressions then, Expressions @else) {
                Cond = cond;
                Then = then;
                Else = @else;
            }

            public Expressions Cond { get; }
            public Expressions Then { get; }
            public Expressions Else { get; }

            //public override string ToString() {
            //    return $"if {Cond} then {Then} else {Else}";
            //}
        }

        /// <summary>
        /// let式
        /// </summary>
        public class LetExp : Expressions {
            public LetExp(Tuple<string, Expressions>[] binds, Expressions body) {
                Binds = binds;
                Body = body;
            }

            public Tuple<string, Expressions>[] Binds { get; }
            public Expressions Body { get; }

            //public override string ToString() {
            //    return $"let {string.Join(" and ", Binds.Select(x => $"{x.Item1} = {x.Item2}"))} in {Body}";
            //}
        }

        /// <summary>
        /// 無名関数式（静的スコープ版）
        /// </summary>
        public class FunExp : Expressions {
            public FunExp(string arg, Expressions body) {
                Arg = arg;
                Body = body;
            }

            public string Arg { get; }
            public Expressions Body { get; }

            //public override string ToString() {
            //    return $"fun {Arg} -> {Body}";
            //}
        }

        /// <summary>
        /// 無名関数式（動的スコープ版）
        /// </summary>
        public class DFunExp : Expressions {
            public DFunExp(string arg, Expressions body) {
                Arg = arg;
                Body = body;
            }

            public string Arg { get; }
            public Expressions Body { get; }

            //public override string ToString() {
            //    return $"dfun {Arg} -> {Body}";
            //}
        }

        /// <summary>
        /// 関数適用式
        /// </summary>
        public class AppExp : Expressions {
            public AppExp(Expressions fun, Expressions arg) {
                Fun = fun;
                Arg = arg;
            }

            public Expressions Fun { get; }
            public Expressions Arg { get; }

            //public override string ToString() {
            //    return $"({Fun} {Arg})";
            //}
        }

        /// <summary>
        /// let-rec式
        /// </summary>
        public class LetRecExp : Expressions {
            public LetRecExp(Tuple<string, Expressions>[] binds, Expressions body) {
                Binds = binds;
                Body = body;
            }

            public Tuple<string, Expressions>[] Binds { get; }
            public Expressions Body { get; }

            //public override string ToString() {
            //    return $"let rec {string.Join(" and ", Binds.Select(x => $"{x.Item1} = {x.Item2}"))} in {Body}";
            //}
        }

        /// <summary>
        /// match式
        /// </summary>
        public class MatchExp : Expressions {
            public MatchExp(Expressions exp, Tuple<PatternExpressions, Expressions>[] patterns) {
                Exp = exp;
                Patterns = patterns;
            }

            public Expressions Exp { get; }
            public Tuple<PatternExpressions, Expressions>[] Patterns { get; }

            //public override string ToString() {
            //    return $"match {Exp} with {string.Join(" | ", Patterns.Select(x => $"{x.Item1.ToString()} -> {x.Item2.ToString()}"))}";
            //}
        }

        /// <summary>
        /// Halt式
        /// </summary>
        public class HaltExp : Expressions {
            public string Message { get; }

            public HaltExp(string message) {
                Message = message;
            }

            //public override string ToString() {
            //    return $"(halt \"{Message}\")";
            //}
        }

        private static int Priolity(Expressions exp) {
            if (exp is Var) { return 100; }
            if (exp is IntLit) { return 100; }
            if (exp is StrLit) { return 100; }
            if (exp is BoolLit) { return 100; }
            if (exp is EmptyListLit) { return 100; }
            if (exp is OptionExp) { return 100; }
            if (exp is UnitLit) { return 100; }
            if (exp is TupleExp) { return 100; }
            if (exp is BuiltinOp) {
                switch ((exp as BuiltinOp).Op) {
                    case BuiltinOp.Kind.UnaryPlus: return 2;
                    case BuiltinOp.Kind.UnaryMinus: return 2;

                    case BuiltinOp.Kind.Mult: return 8;
                    case BuiltinOp.Kind.Div: return 8;
                    case BuiltinOp.Kind.Plus: return 7;
                    case BuiltinOp.Kind.Minus: return 7;
                    case BuiltinOp.Kind.ColCol: return 6;
                    case BuiltinOp.Kind.Lt: return 5;
                    case BuiltinOp.Kind.Gt: return 5;
                    case BuiltinOp.Kind.Le: return 5;
                    case BuiltinOp.Kind.Ge: return 5;
                    case BuiltinOp.Kind.Eq: return 4;
                    case BuiltinOp.Kind.Ne: return 4;

                    case BuiltinOp.Kind.Head: return 2;
                    case BuiltinOp.Kind.Tail: return 2;
                    case BuiltinOp.Kind.IsCons: return 2;
                    case BuiltinOp.Kind.Car: return 2;
                    case BuiltinOp.Kind.Cdr: return 2;
                    case BuiltinOp.Kind.IsTail: return 2;
                    case BuiltinOp.Kind.IsTuple: return 2;
                    case BuiltinOp.Kind.Length: return 2;
                    case BuiltinOp.Kind.IsNone: return 2;
                    case BuiltinOp.Kind.IsSome: return 2;
                    case BuiltinOp.Kind.Get: return 2;
                    default: throw new ArgumentOutOfRangeException();
                }
            }
            if (exp is IfExp) { return 3; }
            if (exp is LetExp) { return 100; }
            if (exp is FunExp) { return 1; }
            if (exp is DFunExp) { return 1; }
            if (exp is AppExp) { return 2; }
            if (exp is LetRecExp) { return 100; }
            if (exp is MatchExp) { return 1; }
            if (exp is HaltExp) { return 2; }
            throw new NotSupportedException();

        }

        private static string ExpressionToStringBody(int p, Expressions exp) {
            if (exp is Var) { return $"{((Var)exp).Id}"; }
            if (exp is IntLit) { return $"{((IntLit)exp).Value}"; }
            if (exp is StrLit) { return $"\"{((StrLit)exp).Value.Replace("\\", "\\\\").Replace("\"", "\\\"")}\""; }
            if (exp is BoolLit) { return $"{((BoolLit)exp).Value}"; }
            if (exp is EmptyListLit) { return $"[]"; }
            if (exp is OptionExp) {
                if (ReferenceEquals(exp, OptionExp.None)) {
                    return "None";
                }
                else {
                    var e = exp as OptionExp;
                    if (e.Expr is TupleExp) {
                        return "Some" + ExpressionToString(p, e.Expr);
                    }
                    else {
                        return "Some " + ExpressionToString(p, e.Expr);
                    }
                }
            }
            if (exp is UnitLit) { return "()"; }
            if (exp is TupleExp) {
                var e = exp as TupleExp;
                var s = new List<string>();
                for (var it = e; it != TupleExp.Tail; it = it.Cdr) {
                    s.Add(ExpressionToString(0,it.Car));
                }
                return "(" + string.Join(",",s) + ")";
            }
            if (exp is BuiltinOp) {
                var e = (exp as BuiltinOp);
                switch (e.Op) {
                    case BuiltinOp.Kind.UnaryPlus:
                    case BuiltinOp.Kind.UnaryMinus:
                        return BuiltinOp.BinOpToStr[e.Op] + ExpressionToString(p, e.Exprs[0]);
                    case BuiltinOp.Kind.Plus:
                    case BuiltinOp.Kind.Minus:
                    case BuiltinOp.Kind.Mult:
                    case BuiltinOp.Kind.Div:
                    case BuiltinOp.Kind.Lt:
                    case BuiltinOp.Kind.Gt:
                    case BuiltinOp.Kind.Le:
                    case BuiltinOp.Kind.Ge:
                    case BuiltinOp.Kind.Eq:
                    case BuiltinOp.Kind.Ne:
                    case BuiltinOp.Kind.ColCol:
                        return ExpressionToString(p, e.Exprs[0]) + BuiltinOp.BinOpToStr[e.Op] + ExpressionToString(p, e.Exprs[1]);
                    case BuiltinOp.Kind.Head:
                    case BuiltinOp.Kind.Tail:
                    case BuiltinOp.Kind.IsCons:
                    case BuiltinOp.Kind.Car:
                    case BuiltinOp.Kind.Cdr:
                    case BuiltinOp.Kind.IsTail:
                    case BuiltinOp.Kind.IsTuple:
                    case BuiltinOp.Kind.Length:
                    case BuiltinOp.Kind.IsNone:
                    case BuiltinOp.Kind.IsSome:
                    case BuiltinOp.Kind.Get:
                        return BuiltinOp.BinOpToStr[e.Op] + string.Join(" ", e.Exprs.Select(x => ExpressionToString(p, x)));
                    default:
                        throw new ArgumentOutOfRangeException();
                }

            }
            if (exp is IfExp) {
                var e = exp as IfExp;
                return $"if {ExpressionToString(p, e.Cond)} then {ExpressionToString(p, e.Then)} else {ExpressionToString(p, e.Else)}";
            }
            if (exp is LetExp) {
                var e = exp as LetExp;
                return $"let {string.Join(" and ", e.Binds.Select(x => $"{x.Item1} = {ExpressionToString(0, x.Item2)}"))} in {ExpressionToString(0, e.Body)}";
            }
            if (exp is FunExp) {
                var e = exp as FunExp;
                return $"fun {e.Arg} -> {ExpressionToString(0, e.Body)}";
            }
            if (exp is DFunExp) {
                var e = exp as DFunExp;
                return $"dfun {e.Arg} -> {ExpressionToString(0, e.Body)}";
            }
            if (exp is AppExp) {
                var e = exp as AppExp;
                return $"{ExpressionToString(p, e.Fun)} {ExpressionToString(p, e.Arg)}";
            }
            if (exp is LetRecExp) {
                var e = exp as LetRecExp;
                return $"let rec {string.Join(" and ", e.Binds.Select(x => $"{x.Item1} = {ExpressionToString(0, x.Item2)}"))} in {ExpressionToString(0, e.Body)}";
            }
            if (exp is MatchExp) {
                var e = exp as MatchExp;
                return $"match {ExpressionToString(0, e.Exp)} with {string.Join("| ", e.Patterns.Select(x => $"{x.Item1} -> {x.Item2}"))}";
            }
            if (exp is HaltExp) {
                var e = exp as HaltExp;
                return $"halt \"{e.Message.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"";
            }
            throw new NotSupportedException();
        }

        private static string ExpressionToString(int pri, Expressions exp) {
            var p = Priolity(exp);
            var s = ExpressionToStringBody(p, exp);
            return (p >= pri) ? s : $"({s})";
        }
        public override string ToString() {
            return ExpressionToString(0, this);
        }
    }
}
