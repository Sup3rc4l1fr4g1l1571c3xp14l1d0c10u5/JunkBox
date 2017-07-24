using System;
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

            /// <summary>
            /// レコード式
            /// </summary>
            public class RecordExp : Expressions
            {
                public Tuple<string, Expressions>[] Members { get; }
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

            /// <summary>
            /// コンストラクタ式
            /// </summary>
            public class ConstructorExp : Expressions
            {
                public ConstructorExp(string constructorName, Expressions arg)
                {
                    ConstructorName = constructorName;
                    Arg = arg;
                }

                public Expressions Arg { get; }

                public string ConstructorName { get; }
            }

            /// <summary>
            /// ヴァリアント式
            /// </summary>
            public class VariantExp : Expressions
            {
                public IntLit Tag { get; }
                public StrLit TagName { get; }
                public Expressions Value { get; }

                public VariantExp(StrLit tagName, IntLit tag, Expressions value)
                {
                    Tag = tag;
                    TagName = tagName;
                    Value = value;
                }

            }


            public static Func<Expressions, TResult> Match<TResult>(
                Func<Var, TResult> Var,
                Func<IntLit, TResult> IntLit,
                Func<StrLit, TResult> StrLit,
                Func<BoolLit, TResult> BoolLit,
                Func<EmptyListLit, TResult> EmptyListLit,
                Func<OptionExp, TResult> OptionExp,
                Func<UnitLit, TResult> UnitLit,
                Func<TupleExp, TResult> TupleExp,
                Func<RecordExp, TResult> RecordExp,
                Func<IfExp, TResult> IfExp,
                Func<LetExp, TResult> LetExp,
                Func<FunExp, TResult> FunExp,
                Func<AppExp, TResult> AppExp,
                Func<LetRecExp, TResult> LetRecExp,
                Func<MatchExp, TResult> MatchExp,
                Func<HaltExp, TResult> HaltExp,
                Func<ConstructorExp, TResult> ConstructorExp,
                Func<VariantExp, TResult> VariantExp,
                Func<Expressions, TResult> Other)
            {
                return (e) =>
                {
                    if (e is Var) { return Var((Var)e); }
                    if (e is IntLit) { return IntLit((IntLit)e); }
                    if (e is StrLit) { return StrLit((StrLit)e); }
                    if (e is BoolLit) { return BoolLit((BoolLit)e); }
                    if (e is EmptyListLit) { return EmptyListLit((EmptyListLit)e); }
                    if (e is OptionExp) { return OptionExp((OptionExp)e); }
                    if (e is UnitLit) { return UnitLit((UnitLit)e); }
                    if (e is TupleExp) { return TupleExp((TupleExp)e); }
                    if (e is RecordExp) { return RecordExp((RecordExp)e); }
                    if (e is IfExp) { return IfExp((IfExp)e); }
                    if (e is LetExp) { return LetExp((LetExp)e); }
                    if (e is FunExp) { return FunExp((FunExp)e); }
                    if (e is AppExp) { return AppExp((AppExp)e); }
                    if (e is LetRecExp) { return LetRecExp((LetRecExp)e); }
                    if (e is MatchExp) { return MatchExp((MatchExp)e); }
                    if (e is HaltExp) { return HaltExp((HaltExp)e); }
                    if (e is ConstructorExp) { return ConstructorExp((ConstructorExp)e); }
                    if (e is VariantExp) { return VariantExp((VariantExp)e); }
                    return Other(e);
                };
            }

            private static int Priolity(Expressions exp)
            {
                return Match(
                    Var: (e) => 100,
                    IntLit: (e) => 100,
                    StrLit: (e) => 100,
                    BoolLit: (e) => 100,
                    EmptyListLit: (e) => 100,
                    OptionExp: (e) => 100,
                    UnitLit: (e) => 100,
                    TupleExp: (e) => 100,
                    RecordExp: (e) => 100,
                    IfExp: (e) => 3,
                    LetExp: (e) => 100,
                    FunExp: (e) => 1,
                    AppExp: (e) => 2,
                    LetRecExp: (e) => 100,
                    MatchExp: (e) => 1,
                    HaltExp: (e) => 2,
                    ConstructorExp: (e) => 2,
                    VariantExp: (e) => 2,
                    Other: (e) => throw new NotSupportedException()
                )(exp);
            }

            private static string ExpressionToStringBody(int p, Expressions exp)
            {
                return Match(
                    Var: (e) => { return $"{e.Id}"; },
                IntLit: (e) => { return $"{e.Value}"; },
                StrLit: (e) => { return $"\"{e.Value.Replace("\\", "\\\\").Replace("\"", "\\\"")}\""; },
                BoolLit: (e) => { return $"{e.Value}"; },
                EmptyListLit: (e) => { return "[]"; },
                OptionExp: (e) =>
                {
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
                },
                UnitLit: (e) =>
                {
                    return "()";
                },
                TupleExp: (e) =>
                {
                    return "(" + string.Join(",", e.Members.Select(x => x.ToString())) + ")";
                },
                RecordExp: (e) =>
                {
                    return "{" + string.Join(",", e.Members.Select(x => $"{x.Item1.ToString()} = {x.Item2.ToString()}")) + "}";
                },
                IfExp: (e) =>
                {
                    return $"if {ExpressionToString(p, e.Cond)} then {ExpressionToString(p, e.Then)} else {ExpressionToString(p, e.Else)}";
                },
                LetExp: (e) =>
                {
                    return $"let {string.Join(" and ", e.Binds.Select(x => $"{x.Item1} = {ExpressionToString(0, x.Item2)}"))} in {ExpressionToString(0, e.Body)}";
                },
                FunExp: (e) =>
                {
                    return $"fun ({e.Arg} : {e.ArgTy}) : {e.BodyTy} -> {ExpressionToString(0, e.Body)}";
                },
                AppExp: (e) =>
                {
                    return $"{ExpressionToString(p, e.Fun)} {ExpressionToString(p, e.Arg)}";
                },
                LetRecExp: (e) =>
                {
                    return $"let rec {string.Join(" and ", e.Binds.Select(x => $"{x.Item1} = {ExpressionToString(0, x.Item2)}"))} in {ExpressionToString(0, e.Body)}";
                },
                MatchExp: (e) =>
                {
                    return $"match {ExpressionToString(0, e.Exp)} with {string.Join("| ", e.Patterns.Select(x => $"{x.Item1} -> {x.Item2}"))}";
                },
                HaltExp: (e) =>
                {
                    return $"halt \"{e.Message.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"";
                },
                ConstructorExp: (e) =>
                {
                    return $"{e.ConstructorName} {ExpressionToString(p, e.Arg)}";
                },
                VariantExp: (e) =>
                {
                    return $"{e.TagName} {ExpressionToString(p, e.Value)}";
                },
                Other: (e) => throw new NotSupportedException()
                )(exp);
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
