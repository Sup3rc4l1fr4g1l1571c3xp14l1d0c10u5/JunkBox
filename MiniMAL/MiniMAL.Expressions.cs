using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace MiniMAL {
    /// <summary>
    /// ��
    /// </summary>
    public abstract class Expressions {

        /// <summary>
        /// �ϐ���
        /// </summary>
        public class Var : Expressions {
            public string Id { get; }

            public Var(string id) {
                Id = id;
            }

            public override string ToString() {
                return $"{Id}";
            }
        }

        /// <summary>
        /// ������
        /// </summary>
        public class IntLit : Expressions {
            public BigInteger Value { get; }

            public IntLit(BigInteger value) {
                Value = value;
            }

            public override string ToString() {
                return $"{Value}";
            }
        }

        /// <summary>
        /// ������
        /// </summary>
        public class StrLit : Expressions {
            public string Value { get; }

            public StrLit(string value) {
                Value = value;
            }

            public override string ToString() {
                return $"\"{Value.Replace("\"", "\\\"")}\"";
            }
        }

        /// <summary>
        /// �^�U��
        /// </summary>
        public class BoolLit : Expressions {
            public bool Value { get; }

            public BoolLit(bool value) {
                Value = value;
            }

            public override string ToString() {
                return $"{Value}";
            }
        }

        /// <summary>
        /// �󃊃X�g
        /// </summary>
        public class EmptyListLit : Expressions {

            public EmptyListLit() { }

            public override string ToString() {
                return $"[]";
            }
        }

        /// <summary>
        /// Option�^�l
        /// </summary>
        public class OptionExp : Expressions {
            public static OptionExp None { get; } = new OptionExp(null);
            public Expressions Expr { get; }

            public OptionExp(Expressions expr) {
                Expr = expr;
            }

            public override string ToString() {
                if (this == None)
                {
                    return $"None";
                } else {
                    return $"Some {Expr}";
                }
            }
        }

        /// <summary>
        /// Unit
        /// </summary>
        public class UnitLit : Expressions {

            public UnitLit() {
            }

            public override string ToString() {
                return $"()";
            }
        }

        /// <summary>
        /// �^�v����
        /// </summary>
        public class TupleExp : Expressions {
            public TupleExp(Expressions[] exprs) {
                Exprs = exprs;
            }

            public Expressions[] Exprs { get; }
            public override string ToString() {
                return $"({String.Join(", ", Exprs.Select(x => x.ToString()))})";
            }
        }

        /// <summary>
        /// �񍀉��Z�q��
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
                Nth,
                IsTuple,
                Length,
                IsNone,
                IsSome,
                Get

            }
            private Dictionary<Kind, string> BinOpToStr = new Dictionary<Kind, string>(){
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
                { Kind.Nth, "@Nth" },
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
            public override string ToString() {
                switch (Op)
                {
                    case Kind.UnaryPlus:
                    case Kind.UnaryMinus:
                        return $"({BinOpToStr[Op]} {Exprs[1]})";
                    case Kind.Plus:
                    case Kind.Minus:
                    case Kind.Mult:
                    case Kind.Div:
                    case Kind.Lt:
                    case Kind.Gt:
                    case Kind.Le:
                    case Kind.Ge:
                    case Kind.Eq:
                    case Kind.Ne:
                    case Kind.ColCol:
                        return $"({Exprs[0]} {BinOpToStr[Op]} {Exprs[1]})";
                    case Kind.Head:
                    case Kind.Tail:
                    case Kind.IsCons:
                    case Kind.Nth:
                    case Kind.IsTuple:
                    case Kind.Length:
                    case Kind.IsNone:
                    case Kind.IsSome:
                    case Kind.Get:
                        return $"({BinOpToStr[Op]} {string.Join(" ", Exprs.Select(x => x.ToString()))})";
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        /// <summary>
        /// if��
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
            public override string ToString() {
                return $"if {Cond} then {Then} else {Else}";
            }
        }

        /// <summary>
        /// let��
        /// </summary>
        public class LetExp : Expressions {
            public LetExp(Tuple<string, Expressions>[] binds, Expressions body) {
                Binds = binds;
                Body = body;
            }

            public Tuple<string, Expressions>[] Binds { get; }
            public Expressions Body { get; }
            public override string ToString() {
                return $"let {String.Join(" and ", Binds.Select(x => $"{x.Item1} = {x.Item2}"))} in {Body}";
            }
        }

        /// <summary>
        /// �����֐����i�ÓI�X�R�[�v�Łj
        /// </summary>
        public class FunExp : Expressions {
            public FunExp(string arg, Expressions body) {
                Arg = arg;
                Body = body;
            }

            public string Arg { get; }
            public Expressions Body { get; }
            public override string ToString() {
                return $"fun {Arg} -> {Body}";
            }
        }

        /// <summary>
        /// �����֐����i���I�X�R�[�v�Łj
        /// </summary>
        public class DFunExp : Expressions {
            public DFunExp(string arg, Expressions body) {
                Arg = arg;
                Body = body;
            }

            public string Arg { get; }
            public Expressions Body { get; }
            public override string ToString() {
                return $"dfun {Arg} -> {Body}";
            }
        }

        /// <summary>
        /// �֐��K�p��
        /// </summary>
        public class AppExp : Expressions {
            public AppExp(Expressions fun, Expressions arg) {
                Fun = fun;
                Arg = arg;
            }

            public Expressions Fun { get; }
            public Expressions Arg { get; }
            public override string ToString() {
                return $"({Fun} {Arg})";
            }
        }

        /// <summary>
        /// let-rec��
        /// </summary>
        public class LetRecExp : Expressions {
            public LetRecExp(Tuple<string, Expressions>[] binds, Expressions body) {
                Binds = binds;
                Body = body;
            }

            public Tuple<string, Expressions>[] Binds { get; }
            public Expressions Body { get; }
            public override string ToString() {
                return $"let rec {String.Join(" and ", Binds.Select(x => $"{x.Item1} = {x.Item2}"))} in {Body}";
            }
        }

        /// <summary>
        /// match��
        /// </summary>
        public class MatchExp : Expressions {
            public MatchExp(Expressions exp, Tuple<PatternExpressions, Expressions>[] patterns) {
                Exp = exp;
                Patterns = patterns;
            }

            public Expressions Exp { get; }
            public Tuple<PatternExpressions, Expressions>[] Patterns { get; }
            public override string ToString() {
                return $"match {Exp} with {String.Join(" | ", Patterns.Select(x => $"{x.Item1.ToString()} -> {x.Item2.ToString()}"))}";
            }
        }

        /// <summary>
        /// Halt��
        /// </summary>
        public class HaltExp : Expressions {
            public string Message { get; }

            public HaltExp(string message) {
                Message = message;
            }

            public override string ToString() {
                return $"(halt \"{Message}\")";
            }
        }

    }
}
