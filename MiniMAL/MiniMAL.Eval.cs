using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using Parsing;

namespace MiniMAL {
    /// <summary>
    /// 構文インタプリタ評価部
    /// </summary>
    public static class Eval {
        /// <summary>
        /// 評価値
        /// </summary>
        public abstract class ExprValue {
            /// <summary>
            /// 整数値
            /// </summary>
            public class IntV : ExprValue {
                public BigInteger Value { get; }

                public IntV(BigInteger value) {
                    Value = value;
                }

                public override string ToString() {
                    return $"{Value}";
                }
            }

            /// <summary>
            /// 文字列値
            /// </summary>
            public class StrV : ExprValue {
                public string Value { get; }

                public StrV(string value) {
                    Value = value;
                }

                public override string ToString() {
                    return $"\"{Value.Replace("\"", "\\\"")}\"";
                }
            }

            /// <summary>
            /// 論理値
            /// </summary>
            public class BoolV : ExprValue {
                public bool Value { get; }

                public BoolV(bool value) {
                    Value = value;
                }

                public override string ToString() {
                    return $"{Value}";
                }
            }

            /// <summary>
            /// Unit値
            /// </summary>
            public class UnitV : ExprValue {
                public UnitV() { }

                public override string ToString() {
                    return $"()";
                }
            }

            /// <summary>
            /// レキシカルクロージャー
            /// </summary>
            public class ProcV : ExprValue {
                public string Id { get; }
                public Expressions Body { get; }
                public Environment<ExprValue> Env { get; private set; }

                public ProcV(string id, Expressions body, Environment<ExprValue> env) {
                    Id = id;
                    Body = body;
                    Env = env;
                }

                public void BackPatchEnv(Environment<ExprValue> newenv) {
                    Env = newenv;
                }

                public override string ToString() {
                    return $"<fun>";
                }
            }

            /// <summary>
            /// ダイナミッククロージャー
            /// </summary>
            public class DProcV : ExprValue {
                public string Id { get; }
                public Expressions Body { get; }

                public DProcV(string id, Expressions body) {
                    Id = id;
                    Body = body;
                }

                public override string ToString() {
                    return $"<dfun>";
                }
            }

            /// <summary>
            /// ビルトインクロージャー
            /// </summary>
            public class BProcV : ExprValue {
                public Func<ExprValue, ExprValue> Proc { get; }

                public BProcV(Func<ExprValue, ExprValue> proc) {
                    Proc = proc;
                }

                public override string ToString() {
                    return $"<bproc>";
                }
            }

            /// <summary>
            /// consセル
            /// </summary>
            public class ConsV : ExprValue {
                public static ConsV Empty { get; } = new ConsV(null, null);
                public ExprValue Value { get; }
                public ConsV Next { get; }

                public ConsV(ExprValue value, ConsV next) {
                    Value = value;
                    Next = next;
                }

                public override string ToString() {
                    StringBuilder sb = new StringBuilder();
                    sb.Append("[");
                    if (this != Empty) {
                        sb.Append($"{Value}");
                        for (var p = this.Next; p != Empty; p = p.Next) {
                            sb.Append($"; {p.Value}");
                        }
                    }
                    sb.Append("]");
                    return sb.ToString();
                }
            }

            /// <summary>
            /// タプル
            /// </summary>
            public class TupleV : ExprValue {
                public ExprValue[] Value { get; }

                public TupleV(ExprValue[] value) {
                    Value = value;
                }

                public override string ToString() {
                    return $"({String.Join(", ", Value.Select(x => x.ToString()))})";
                }
            }

            /// <summary>
            /// Option型
            /// </summary>
            public class OptionV : ExprValue {

                public static OptionV None { get; } = new OptionV(null);

                public ExprValue Value { get; }

                public OptionV(ExprValue value) {
                    Value = value;
                }

                public override string ToString() {
                    if (this == None) {
                        return $"None";
                    } else {
                        return $"Some {Value}";
                    }
                }
            }

            /// <summary>
            /// 比較
            /// </summary>
            /// <param name="arg1"></param>
            /// <param name="arg2"></param>
            /// <returns></returns>
            public static bool Equals(ExprValue arg1, ExprValue arg2) {
                if (arg1 is ExprValue.IntV && arg2 is ExprValue.IntV) {
                    var i1 = ((ExprValue.IntV)arg1).Value;
                    var i2 = ((ExprValue.IntV)arg2).Value;
                    return (i1 == i2);
                }
                if (arg1 is ExprValue.StrV && arg2 is ExprValue.StrV) {
                    var i1 = ((ExprValue.StrV)arg1).Value;
                    var i2 = ((ExprValue.StrV)arg2).Value;
                    return (i1 == i2);
                }
                if (arg1 is ExprValue.BoolV && arg2 is ExprValue.BoolV) {
                    var i1 = ((ExprValue.BoolV)arg1).Value;
                    var i2 = ((ExprValue.BoolV)arg2).Value;
                    return (i1 == i2);
                }
                if (arg1 is ExprValue.UnitV && arg2 is ExprValue.UnitV) {
                    return (true);
                }
                if (arg1 is ExprValue.OptionV && arg2 is ExprValue.OptionV) {
                    return (true);
                }
                if (arg1 is ExprValue.ConsV && arg2 is ExprValue.ConsV) {
                    var i1 = ((ExprValue.ConsV)arg1);
                    var i2 = ((ExprValue.ConsV)arg2);
                    while (i1 != ExprValue.ConsV.Empty && i2 != ExprValue.ConsV.Empty) {
                        if (!Equals(i1.Value, i2.Value)) {
                            return false;
                        }
                        i1 = i1.Next;
                        i2 = i2.Next;
                    }
                    return (i1 != ExprValue.ConsV.Empty && i2 != ExprValue.ConsV.Empty);
                }
                if (arg1 is ExprValue.TupleV && arg2 is ExprValue.TupleV) {
                    var i1 = ((ExprValue.TupleV)arg1);
                    var i2 = ((ExprValue.TupleV)arg2);
                    if (i1.Value.Length != i2.Value.Length) {
                        return false;
                    }

                    return i1.Value.Zip(i2.Value, Tuple.Create).All(x => Equals(x.Item1, x.Item2));
                }
                return (false);
            }
        }

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

        /// <summary>
        /// 二項演算子式の評価
        /// </summary>
        /// <param name="op"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        private static ExprValue EvalBuiltinExpressions(Expressions.BuiltinOp.Kind op, ExprValue[] args) {
            switch (op) {
                case Expressions.BuiltinOp.Kind.Plus: {
                        if (args.Length != 2) {
                            throw new Exception.InvalidArgumentNumException("Argument num must be 2: +");
                        }
                        if (args[0] is ExprValue.IntV && args[1] is ExprValue.IntV) {
                            var i1 = ((ExprValue.IntV)args[0]).Value;
                            var i2 = ((ExprValue.IntV)args[1]).Value;
                            return new ExprValue.IntV(i1 + i2);
                        } else {
                            throw new Exception.InvalidArgumentTypeException("Both arguments must be integer: +");
                        }
                    }
                case Expressions.BuiltinOp.Kind.Minus: {
                        if (args.Length != 2) {
                            throw new Exception.InvalidArgumentNumException("Argument num must be 2: -");
                        }
                        if (args[0] is ExprValue.IntV && args[1] is ExprValue.IntV) {
                            var i1 = ((ExprValue.IntV)args[0]).Value;
                            var i2 = ((ExprValue.IntV)args[1]).Value;
                            return new ExprValue.IntV(i1 - i2);
                        } else {
                            throw new Exception.InvalidArgumentTypeException("Both arguments must be integer: -");
                        }
                    }
                case Expressions.BuiltinOp.Kind.Mult: {
                        if (args.Length != 2) {
                            throw new Exception.InvalidArgumentNumException("Argument num must be 2: *");
                        }
                        if (args[0] is ExprValue.IntV && args[1] is ExprValue.IntV) {
                            var i1 = ((ExprValue.IntV)args[0]).Value;
                            var i2 = ((ExprValue.IntV)args[1]).Value;
                            return new ExprValue.IntV(i1 * i2);
                        } else {
                            throw new Exception.InvalidArgumentTypeException("Both arguments must be integer: *");
                        }
                    }
                case Expressions.BuiltinOp.Kind.Div: {
                        if (args.Length != 2) {
                            throw new Exception.InvalidArgumentNumException("Argument num must be 2: /");
                        }
                        if (args[0] is ExprValue.IntV && args[1] is ExprValue.IntV) {
                            var i1 = ((ExprValue.IntV)args[0]).Value;
                            var i2 = ((ExprValue.IntV)args[1]).Value;
                            return new ExprValue.IntV(i1 / i2);
                        } else {
                            throw new Exception.InvalidArgumentTypeException("Both arguments must be integer: /");
                        }
                    }
                case Expressions.BuiltinOp.Kind.Lt: {
                        if (args.Length != 2) {
                            throw new Exception.InvalidArgumentNumException("Argument num must be 2: <");
                        }
                        if (args[0] is ExprValue.IntV && args[1] is ExprValue.IntV) {
                            var i1 = ((ExprValue.IntV)args[0]).Value;
                            var i2 = ((ExprValue.IntV)args[1]).Value;
                            return new ExprValue.BoolV(i1 < i2);
                        } else {
                            throw new Exception.InvalidArgumentTypeException("Both arguments must be integer: <");
                        }
                    }
                case Expressions.BuiltinOp.Kind.Le: {
                        if (args.Length != 2) {
                            throw new Exception.InvalidArgumentNumException("Argument num must be 2: <=");
                        }
                        if (args[0] is ExprValue.IntV && args[1] is ExprValue.IntV) {
                            var i1 = ((ExprValue.IntV)args[0]).Value;
                            var i2 = ((ExprValue.IntV)args[1]).Value;
                            return new ExprValue.BoolV(i1 <= i2);
                        } else {
                            throw new Exception.InvalidArgumentTypeException("Both arguments must be integer: <=");
                        }
                    }
                case Expressions.BuiltinOp.Kind.Gt: {
                        if (args.Length != 2) {
                            throw new Exception.InvalidArgumentNumException("Argument num must be 2: >");
                        }
                        if (args[0] is ExprValue.IntV && args[1] is ExprValue.IntV) {
                            var i1 = ((ExprValue.IntV)args[0]).Value;
                            var i2 = ((ExprValue.IntV)args[1]).Value;
                            return new ExprValue.BoolV(i1 > i2);
                        } else {
                            throw new Exception.InvalidArgumentTypeException("Both arguments must be integer: >");
                        }
                    }
                case Expressions.BuiltinOp.Kind.Ge: {
                        if (args.Length != 2) {
                            throw new Exception.InvalidArgumentNumException("Argument num must be 2: >=");
                        }
                        if (args[0] is ExprValue.IntV && args[1] is ExprValue.IntV) {
                            var i1 = ((ExprValue.IntV)args[0]).Value;
                            var i2 = ((ExprValue.IntV)args[1]).Value;
                            return new ExprValue.BoolV(i1 >= i2);
                        } else {
                            throw new Exception.InvalidArgumentTypeException("Both arguments must be integer: >=");
                        }
                    }
                case Expressions.BuiltinOp.Kind.Eq: {
                        if (args.Length != 2) {
                            throw new Exception.InvalidArgumentNumException("Argument num must be 2: =");
                        }
                        return new ExprValue.BoolV(ExprValue.Equals(args[0], args[1]));
                    }
                case Expressions.BuiltinOp.Kind.Ne: {
                        if (args.Length != 2) {
                            throw new Exception.InvalidArgumentNumException("Argument num must be 2: <>");
                        }
                        return new ExprValue.BoolV(!ExprValue.Equals(args[0], args[1]));
                    }
                case Expressions.BuiltinOp.Kind.ColCol: {
                        if (args.Length != 2) {
                            throw new Exception.InvalidArgumentNumException("Argument num must be 2: ::");
                        }
                        if (args[1] is ExprValue.ConsV) {
                            var i1 = ((ExprValue.ConsV)args[1]);
                            return new ExprValue.ConsV(args[0], i1);
                        }
                        throw new Exception.InvalidArgumentNumException("Right arguments must be List: ::");
                    }
                case Expressions.BuiltinOp.Kind.Head: {
                        if (args.Length != 1) {
                            throw new Exception.InvalidArgumentNumException("Argument num must be 1: @Head");
                        }
                        if (args[0] is Eval.ExprValue.ConsV) {
                            if (args[0] == Eval.ExprValue.ConsV.Empty) {
                                throw new Exception.InvalidArgumentTypeException("arguments must be not empty list: @Head");
                            }
                            return ((Eval.ExprValue.ConsV)args[0]).Value;
                        }
                        throw new Exception.InvalidArgumentTypeException("arguments must be List: @Head");
                    }
                case Expressions.BuiltinOp.Kind.Tail: {
                        if (args.Length != 1) {
                            throw new Exception.InvalidArgumentNumException("Argument num must be 1: @Tail");
                        }
                        if (args[0] is Eval.ExprValue.ConsV) {
                            if (args[0] == Eval.ExprValue.ConsV.Empty) {
                                throw new Exception.InvalidArgumentTypeException("arguments must be not empty list: @Tail");
                            }
                            return ((Eval.ExprValue.ConsV)args[0]).Value;
                        }
                        throw new Exception.InvalidArgumentTypeException("arguments must be List: @Tail");
                    }
                case Expressions.BuiltinOp.Kind.IsCons: {
                        if (args.Length != 1) {
                            throw new Exception.InvalidArgumentNumException("Argument num must be 1: @IsCons");
                        }
                        return new ExprValue.BoolV(args[0] is Eval.ExprValue.ConsV);
                    }
                case Expressions.BuiltinOp.Kind.Nth: {
                        if (args.Length != 2) {
                            throw new Exception.InvalidArgumentNumException("Argument num must be 2: @Nth");
                        }
                        var i1 = args[0];
                        var i2 = args[1];
                        if (i1 is Eval.ExprValue.IntV && i2 is Eval.ExprValue.TupleV) {
                            return ((Eval.ExprValue.TupleV)i2).Value[(int)((Eval.ExprValue.IntV)i1).Value];
                        }
                        throw new Exception.InvalidArgumentTypeException("arguments must be int and Tuple: @Nth");
                    }
                case Expressions.BuiltinOp.Kind.IsTuple: {
                        if (args.Length != 1) {
                            throw new Exception.InvalidArgumentNumException("Argument num must be 1: @IsTuple");
                        }
                        return new ExprValue.BoolV(args[0] is Eval.ExprValue.TupleV);
                    }
                case Expressions.BuiltinOp.Kind.Length: {
                        if (args.Length != 1) {
                            throw new Exception.InvalidArgumentNumException("Argument num must be 1: @Length");
                        }
                        var i1 = args[0];
                        if (i1 is Eval.ExprValue.TupleV) {
                            return new ExprValue.IntV(((Eval.ExprValue.TupleV)i1).Value.Length);
                        }
                        throw new Exception.InvalidArgumentTypeException("arguments must be int and Tuple: @Length");
                    }

                default:
                    throw new ArgumentOutOfRangeException(nameof(op), op, null);
            }
        }

        /// <summary>
        /// パターンマッチの評価
        /// </summary>
        /// <param name="env"></param>
        /// <param name="pattern"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private static Dictionary<string, ExprValue> EvalPatternExpressions(Environment<ExprValue> env, PatternExpressions pattern, ExprValue value) {
            if (pattern is PatternExpressions.WildP) {
                return new Dictionary<string, ExprValue>();
            }
            if (pattern is PatternExpressions.IntP && value is ExprValue.IntV) {
                if (((PatternExpressions.IntP)pattern).Value == ((ExprValue.IntV)value).Value) {
                    return new Dictionary<string, ExprValue>();
                } else {
                    return null;
                }
            }
            if (pattern is PatternExpressions.StrP && value is ExprValue.StrV) {
                if (((PatternExpressions.StrP)pattern).Value == ((ExprValue.StrV)value).Value) {
                    return new Dictionary<string, ExprValue>();
                } else {
                    return null;
                }
            }
            if (pattern is PatternExpressions.BoolP && value is ExprValue.BoolV) {
                if (((PatternExpressions.BoolP)pattern).Value == ((ExprValue.BoolV)value).Value) {
                    return new Dictionary<string, ExprValue>();
                } else {
                    return null;
                }
            }
            if (pattern is PatternExpressions.UnitP && value is ExprValue.UnitV) {
                return new Dictionary<string, ExprValue>();
            }
            if (pattern is PatternExpressions.VarP) {
                return new Dictionary<string, ExprValue>() { { ((PatternExpressions.VarP)pattern).Id, value } };
            }
            if (pattern is PatternExpressions.ConsP && value is ExprValue.ConsV) {
                var p = pattern as PatternExpressions.ConsP;
                var q = value as ExprValue.ConsV;
                var dic = new Dictionary<string, ExprValue>();
                if (q == ExprValue.ConsV.Empty) {
                    if (p == PatternExpressions.ConsP.Empty) {
                        return dic;
                    } else {
                        return null;
                    }
                }
                var ret1 = EvalPatternExpressions(env, p.Value, q.Value);
                if (ret1 == null) {
                    return null;
                }
                dic = ret1.Aggregate(dic, (s, x) => { s[x.Key] = x.Value; return s; });

                var ret2 = EvalPatternExpressions(env, p.Next, q.Next);
                if (ret2 == null) {
                    return null;
                }
                dic = ret2.Aggregate(dic, (s, x) => { s[x.Key] = x.Value; return s; });

                return dic;
            }
            if (pattern is PatternExpressions.TupleP && value is ExprValue.TupleV) {
                var p = pattern as PatternExpressions.TupleP;
                var q = value as ExprValue.TupleV;
                var dic = new Dictionary<string, ExprValue>();
                if (p.Value.Length != q.Value.Length) {
                    return null;
                }
                foreach (var pq in p.Value.Zip(q.Value, Tuple.Create)) {
                    var ret1 = EvalPatternExpressions(env, pq.Item1, pq.Item2);
                    if (ret1 == null) {
                        return null;
                    }
                    dic = ret1.Aggregate(dic, (s, x) => { s[x.Key] = x.Value; return s; });
                }
                return dic;

            }
            return null;
        }

        /// <summary>
        /// 式の評価
        /// </summary>
        /// <param name="env"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        private static ExprValue EvalExpressions(Environment<ExprValue> env, Expressions e) {
            if (e is Expressions.Var) {
                var x = ((Expressions.Var)e).Id;
                try {
                    return Environment.LookUp(x, env);
                } catch (Exception.NotBound) {
                    throw new Exception.NotBound($"Variable not bound: {x}");
                }
            }
            if (e is Expressions.IntLit) {
                return new ExprValue.IntV(((Expressions.IntLit)e).Value);
            }
            if (e is Expressions.StrLit) {
                return new ExprValue.StrV(((Expressions.StrLit)e).Value);
            }
            if (e is Expressions.BoolLit) {
                return new ExprValue.BoolV(((Expressions.BoolLit)e).Value);
            }
            if (e is Expressions.EmptyListLit) {
                return ExprValue.ConsV.Empty;
            }
            if (e is Expressions.UnitLit) {
                return new ExprValue.UnitV();
            }
            if (e is Expressions.BuiltinOp) {
                var op = ((Expressions.BuiltinOp)e).Op;
                var args = ((Expressions.BuiltinOp)e).Exprs.Select(x => EvalExpressions(env, x)).ToArray();
                return EvalBuiltinExpressions(op, args);
            }
            if (e is Expressions.IfExp) {
                var cond = EvalExpressions(env, ((Expressions.IfExp)e).Cond);
                if (cond is ExprValue.BoolV) {
                    var v = ((ExprValue.BoolV)cond).Value;
                    if (v) {
                        return EvalExpressions(env, ((Expressions.IfExp)e).Then);
                    } else {
                        return EvalExpressions(env, ((Expressions.IfExp)e).Else);
                    }
                }
                throw new NotSupportedException("Test expression must be boolean: if");
            }
            if (e is Expressions.LetExp) {
                var newenv = env;
                foreach (var bind in ((Expressions.LetExp)e).Binds) {
                    var value = EvalExpressions(env, bind.Item2);
                    newenv = Environment.Extend(bind.Item1, value, newenv);
                }
                return EvalExpressions(newenv, ((Expressions.LetExp)e).Body);
            }
            if (e is Expressions.LetRecExp) {
                var dummyenv = Environment<ExprValue>.Empty;
                var newenv = env;
                var procs = new List<ExprValue.ProcV>();

                foreach (var bind in ((Expressions.LetRecExp)e).Binds) {
                    var value = EvalExpressions(dummyenv, bind.Item2);
                    if (value is ExprValue.ProcV) {
                        procs.Add((ExprValue.ProcV)value);
                    }
                    newenv = Environment.Extend(bind.Item1, value, newenv);
                }

                foreach (var proc in procs) {
                    proc.BackPatchEnv(newenv);
                }
                return EvalExpressions(newenv, ((Expressions.LetRecExp)e).Body);
            }
            if (e is Expressions.FunExp) {
                return new ExprValue.ProcV(((Expressions.FunExp)e).Arg, ((Expressions.FunExp)e).Body, env);
            }
            if (e is Expressions.DFunExp) {
                return new ExprValue.DProcV(((Expressions.DFunExp)e).Arg, ((Expressions.DFunExp)e).Body);
            }
            if (e is Expressions.AppExp) {
                var funval = EvalExpressions(env, ((Expressions.AppExp)e).Fun);
                var arg = EvalExpressions(env, ((Expressions.AppExp)e).Arg);
                if (funval is ExprValue.ProcV) {
                    var newenv = Environment.Extend(((ExprValue.ProcV)funval).Id, arg, ((ExprValue.ProcV)funval).Env);
                    return EvalExpressions(newenv, ((ExprValue.ProcV)funval).Body);
                } else if (funval is ExprValue.DProcV) {
                    var newenv = Environment.Extend(((ExprValue.DProcV)funval).Id, arg, env);
                    return EvalExpressions(newenv, ((ExprValue.DProcV)funval).Body);
                } else if (funval is ExprValue.BProcV) {
                    return ((ExprValue.BProcV)funval).Proc(arg);
                } else {
                    throw new NotSupportedException($"{funval.GetType().FullName} cannot eval.");
                }
            }
            if (e is Expressions.MatchExp) {
                var val = EvalExpressions(env, ((Expressions.MatchExp)e).Exp);
                foreach (var pattern in ((Expressions.MatchExp)e).Patterns) {
                    var ret = EvalPatternExpressions(env, pattern.Item1, val);
                    if (ret != null) {
                        var newenv = ret.Aggregate(env, (s, x) => Environment.Extend(x.Key, x.Value, s));
                        return EvalExpressions(newenv, pattern.Item2);
                    }
                }
                throw new NotSupportedException($"value {val} is not match.");
            }
            if (e is Expressions.TupleExp) {
                var t = e as Expressions.TupleExp;
                return new ExprValue.TupleV(t.Exprs.Select((x) => EvalExpressions(env, x)).ToArray());
            }
            if (e is Expressions.OptionExp) {
                if (e == Expressions.OptionExp.None) {
                    return ExprValue.OptionV.None;
                } else {
                    return new ExprValue.OptionV(EvalExpressions(env, (e as Expressions.OptionExp).Expr));
                }
            }
            if (e is Expressions.HaltExp) {
                throw new Exception.HaltException((e as Expressions.HaltExp).Message);
            }

            throw new NotSupportedException($"expression {e} cannot eval.");
        }

        private static Result eval_declEntry(Environment<ExprValue> env, Declarations.Decls.DeclBase p) {
            if (p is Declarations.Decls.Decl) {
                var d = (Declarations.Decls.Decl)p;
                var newenv = env;
                var ret = new Result("", env, null);

                foreach (var bind in d.Binds) {
                    var v = EvalExpressions(newenv, bind.Item2);
                    ret = new Result(bind.Item1, Environment.Extend(bind.Item1, v, ret.Env), v);
                }
                return ret;
            }
            if (p is Declarations.Decls.RecDecl) {
                var d = (Declarations.Decls.RecDecl)p;
                var newenv = env;
                var ret = new Result("", env, null);

                var dummyenv = Environment<ExprValue>.Empty;
                var procs = new List<ExprValue.ProcV>();

                foreach (var bind in d.Binds) {
                    var v = EvalExpressions(dummyenv, bind.Item2);
                    if (v is ExprValue.ProcV) {
                        procs.Add((ExprValue.ProcV)v);
                    }
                    newenv = Environment.Extend(bind.Item1, v, newenv);
                    ret = new Result(bind.Item1, newenv, v);
                }

                foreach (var proc in procs) {
                    proc.BackPatchEnv(newenv);
                }
                return ret;
            }
            throw new NotSupportedException($"{p.GetType().FullName} cannot eval.");
        }

        public static Result eval_decl(Environment<ExprValue> env, Declarations p) {
            if (p is Declarations.Exp) {
                var e = (Declarations.Exp)p;
                var v = EvalExpressions(env, e.Syntax);
                return new Result("-", env, v);
            }
            if (p is Declarations.Decls) {
                var ds = (Declarations.Decls)p;
                var newenv = env;
                Result ret = new Result("", env, (ExprValue)null);
                foreach (var d in ds.Entries) {
                    ret = eval_declEntry(newenv, d);
                    newenv = ret.Env;
                }
                return ret;
            }
            if (p is Declarations.Empty) {
                return new Result("", env, (ExprValue)null);
            }
            throw new NotSupportedException($"{p.GetType().FullName} cannot eval.");
        }

    }
}
