using System;
using System.Collections.Generic;
using System.Linq;

namespace MiniML
{
    public static partial class MiniML
    {
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

            public class LLit : Syntax {
                public LLit() {
                }

                public override string ToString() {
                    return $"[]";
                }
            }

            public class Unit : Syntax {
                public Unit() {
                }

                public override string ToString() {
                    return $"()";
                }
            }

            public class BinOp : Syntax {
                public enum Kind {
                    Plus,
                    Minus,
                    Mult,
                    Div,
                    Lt,
                    Le,
                    Gt,
                    Ge,
                    Eq,
                    Ne,
                    LAnd,
                    LOr,
                    Cons,
                }

                private readonly IReadOnlyDictionary<Kind, string> KindToStr = new Dictionary<Kind, string>()
                {
                    {Kind.Plus, "+"},
                    {Kind.Minus, "-"},
                    {Kind.Mult, "*"},
                    {Kind.Div, "/"},
                    {Kind.Lt, "<"},
                    {Kind.Le, "<="},
                    {Kind.Gt, ">"},
                    {Kind.Ge, ">="},
                    {Kind.Eq, "="},
                    {Kind.Ne, "<>"},
                    {Kind.LAnd, "&&"},
                    {Kind.LOr, "||"},
                    {Kind.Cons, "::"},
                };

                public BinOp(Kind op, Syntax left, Syntax right) {
                    Op = op;
                    Left = left;
                    Right = right;
                }

                public Kind Op { get; }
                public Syntax Left { get; }
                public Syntax Right { get; }

                public override string ToString()
                {
                    return $"({Left} {KindToStr[Op]} {Right})";
                }
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

                public override string ToString() {
                    return $"if {Cond} then {Then} else {Else}";
                }

            }

            public class LetExp : Syntax {
                public LetExp(Tuple<string, Syntax>[] binds, Syntax body) {
                    Binds = binds;
                    Body = body;
                }

                public Tuple<string, Syntax>[] Binds { get; }
                public Syntax Body { get; }

                public override string ToString() {
                    return $"let {string.Join(" and ", Binds.Select(x => $"{x.Item1} = {x.Item2}"))} in {Body}";
                }
            }

            public class LetRecExp : Syntax {
                public LetRecExp(Tuple<string, Syntax>[] binds, Syntax body) {
                    Binds = binds;
                    Body = body;
                }

                public Tuple<string, Syntax>[] Binds { get; }
                public Syntax Body { get; }

                public override string ToString() {
                    return $"let rec {string.Join(" and ", Binds.Select(x => $"{x.Item1} = {x.Item2}"))} in {Body}";
                }

            }

            public class FunExp : Syntax {
                public FunExp(string arg, Syntax body) {
                    Arg = arg;
                    Body = body;
                }

                public string Arg { get; }
                public Syntax Body { get; }

                public override string ToString() {
                    return $"fun {Arg} -> {Body}";
                }
            }

            public class DFunExp : Syntax {
                public DFunExp(string arg, Syntax body) {
                    Arg = arg;
                    Body = body;
                }

                public string Arg { get; }
                public Syntax Body { get; }

                public override string ToString() {
                    return $"fun {Arg} -> {Body}";
                }
            }

            public class AppExp : Syntax {
                public AppExp(Syntax fun, Syntax arg) {
                    Fun = fun;
                    Arg = arg;
                }

                public Syntax Fun { get; }
                public Syntax Arg { get; }

                public override string ToString() {
                    return $"({Fun} {Arg})";
                }
            }

            public class MatchExp : Syntax {
                public MatchExp(Syntax expr, Tuple<Pattern, Syntax>[] entries) {
                    this.Expr = expr;
                    this.Entries = entries;
                }

                public Syntax Expr { get; }
                public Tuple<Pattern, Syntax>[] Entries { get; }

                public override string ToString() {
                    return $"match {Expr} with { String.Join(" | ", Entries.Select(x => $"{x.Item1} -> {x.Item2}").ToArray())})";
                }
            }

            public class TupleExp : Syntax {
                public TupleExp(Syntax[] values) {
                    Values = values;
                }

                public Syntax[] Values { get; }

                public override string ToString() {
                    return $"({String.Join(" ",Values.Select(x => x.ToString()))})";
                }
            }


        }
    }
}