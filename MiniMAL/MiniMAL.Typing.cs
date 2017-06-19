using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniMAL {
    public class Typing {
        public abstract class Type
        {
            public class TyInt : Type
            {
                public override string ToString()
                {
                    return "int";
                }
            }

            public class TyBool : Type
            {
                public override string ToString()
                {
                    return "bool";
                }
            }

            public class TyStr : Type {
                public override string ToString() {
                    return "string";
                }
            }

            public class TyUnit : Type {
                public override string ToString() {
                    return "unit";
                }
            }

            public class TyNil : Type {
                public override string ToString() {
                    return "nil";
                }
            }

            public class TyCons : Type {
                public Type ItemType { get; }
                public static TyCons Empty { get; } = new TyCons(null);

                public TyCons(Type itemType) {
                    ItemType = itemType;
                }
                public override string ToString() {
                    return $"{ItemType} list";
                }
            }

            public class TyTuple : Type {
                public Type[] ItemType { get; }

                public TyTuple(Type[] itemType) {
                    ItemType = itemType;
                }
                public override string ToString() {
                    return $"({string.Join(" * ", ItemType.Select(x => x.ToString()))})";
                }
            }

            /// <summary>
            /// 比較
            /// </summary>
            /// <param name="arg1"></param>
            /// <param name="arg2"></param>
            /// <returns></returns>
            public static bool Equals(Type arg1, Type arg2) {
                if (arg1 is Type.TyInt && arg2 is Type.TyInt)
                {
                    return true;
                }
                if (arg1 is Type.TyStr && arg2 is Type.TyStr) {
                    return true;
                }
                if (arg1 is Type.TyBool && arg2 is Type.TyBool) {
                    return true;
                }
                if (arg1 is Type.TyUnit && arg2 is Type.TyUnit) {
                    return true;
                }
                if (arg1 is Type.TyNil && arg2 is Type.TyNil) {
                    return true;
                }
                if (arg1 is Type.TyCons && arg2 is Type.TyCons) {
                    var i1 = ((Type.TyCons)arg1);
                    var i2 = ((Type.TyCons)arg2);
                    return Equals(i1.ItemType, i2.ItemType);
                }
                if (arg1 is Type.TyTuple && arg2 is Type.TyTuple) {
                    var i1 = ((Type.TyTuple)arg1);
                    var i2 = ((Type.TyTuple)arg2);
                    if (i1.ItemType.Length != i2.ItemType.Length) {
                        return false;
                    }

                    return i1.ItemType.Zip(i2.ItemType, Tuple.Create).All(x => Equals(x.Item1, x.Item2));
                }
                return (false);
            }
        }


        /// <summary>
        /// 評価結果
        /// </summary>
        public class Result {
            public Result(string id, Environment<Type> env, Type value) {
                Id = id;
                Env = env;
                Value = value;
            }

            public string Id { get; }
            public Environment<Type> Env { get; }
            public Type Value { get; }
        }

        /// <summary>
        /// 二項演算子式の結果型
        /// </summary>
        /// <param name="op"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        private static Type EvalBuiltinExpressions(Expressions.BuiltinOp.Kind op, Type[] args) {
            switch (op) {
                case Expressions.BuiltinOp.Kind.Plus: {
                    if (args.Length == 2 && args[0] is Type.TyInt && args[1] is Type.TyInt) {
                        return new Type.TyInt();
                    }
                    if (args.Length == 2 && args[0] is Type.TyStr && args[1] is Type.TyStr) {
                        return new Type.TyStr();
                    }
                        throw new Exception("Both arguments must be integer/string: +");

                }
                case Expressions.BuiltinOp.Kind.Minus: {
                    if (args.Length == 2 && args[0] is Type.TyInt && args[1] is Type.TyInt) {
                        return new Type.TyInt();
                    }
                    throw new Exception("Both arguments must be integer: -");

                }
                case Expressions.BuiltinOp.Kind.Mult: {
                    if (args.Length == 2 && args[0] is Type.TyInt && args[1] is Type.TyInt) {
                        return new Type.TyInt();
                    }
                    throw new Exception("Both arguments must be integer: *");
                }
                case Expressions.BuiltinOp.Kind.Div: {
                    if (args.Length == 2 && args[0] is Type.TyInt && args[1] is Type.TyInt) {
                        return new Type.TyInt();
                    }
                    throw new Exception("Both arguments must be integer: /");
                }
                case Expressions.BuiltinOp.Kind.Lt: {
                    if (args.Length == 2 && args[0] is Type.TyInt && args[1] is Type.TyInt) {
                        return new Type.TyBool();
                    }
                    throw new Exception("Both arguments must be integer: <");
                }
                case Expressions.BuiltinOp.Kind.Le: {
                    if (args.Length == 2 && args[0] is Type.TyInt && args[1] is Type.TyInt) {
                        return new Type.TyBool();
                    }
                    throw new Exception("Both arguments must be integer: <=");
                }
                case Expressions.BuiltinOp.Kind.Gt: {
                    if (args.Length == 2 && args[0] is Type.TyInt && args[1] is Type.TyInt) {
                        return new Type.TyBool();
                    }
                    throw new Exception("Both arguments must be integer: >");
                }
                case Expressions.BuiltinOp.Kind.Ge: {
                    if (args.Length == 2 && args[0] is Type.TyInt && args[1] is Type.TyInt) {
                        return new Type.TyBool();
                    }
                    throw new Exception("Both arguments must be integer: >=");
                }
                case Expressions.BuiltinOp.Kind.Eq: {
                    if (args.Length == 2 && Type.Equals(args[0], args[1])) {
                        return new Type.TyBool();
                    }
                    throw new Exception("Both arguments must be same type: =");
                }
                case Expressions.BuiltinOp.Kind.Ne: {
                    if (args.Length == 2 && Type.Equals(args[0], args[1])) {
                        return new Type.TyBool();
                    }
                    throw new Exception("Both arguments must be same type: <>");
                }
                case Expressions.BuiltinOp.Kind.ColCol: {
                    if (args.Length == 2 && args[1] is Type.TyCons && Type.Equals(args[0], (args[1] as Type.TyCons).ItemType)) {
                        return args[1];
                    }
                    throw new Exception("Both arguments must be same type: ::");
                }
                case Expressions.BuiltinOp.Kind.Head: {
                    if (args.Length == 1) {
                        var i1 = args[0];
                        if (i1 is Type.TyCons) {
                            return ((Type.TyCons)args[0]).ItemType;
                        }
                        throw new Exception("invalid argument type: @Head");
                    }
                        throw new Exception("invalid argument num: @Head");
                }
                case Expressions.BuiltinOp.Kind.Tail: {
                    if (args.Length == 1) {
                        var i1 = args[0];
                        if (i1 is Type.TyCons) {
                            return i1;
                        }
                        throw new Exception("invalid argument type: @Tail");
                    }
                    throw new Exception("invalid argument num: @Tail");
                }
                case Expressions.BuiltinOp.Kind.IsCons: {
                    if (args.Length == 1) {
                        return new Type.TyBool();
                    }
                        throw new Exception("invalid argument num: @IsCons");
                }
                case Expressions.BuiltinOp.Kind.Nth: {
                    if (args.Length == 2) {
                        var i1 = args[0];
                        var i2 = args[1];
                        if (i1 is Eval.ExprValue.IntV && i2 is Eval.ExprValue.TupleV) {
                            return ((Eval.ExprValue.TupleV)i2).Value[(int)((Eval.ExprValue.IntV)i1).Value];
                        }
                    }
                    throw new Exception("invalid argument num: @Nth");
                }
                case Expressions.BuiltinOp.Kind.IsTuple: {
                    if (args.Length == 1) {
                        return new Type.TyBool();
                    }
                    throw new Exception("invalid argument num: @IsTuple");
                }
                case Expressions.BuiltinOp.Kind.Length: {
                    if (args.Length == 1) {
                        var i1 = args[0];
                        if (i1 is Type.TyTuple) {
                            return new ExprValue.IntV(((Eval.ExprValue.TupleV)i1).Value.Length);
                        }
                    }
                    throw new Exception("invalid argument num: @Length");
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
                } catch (Environment.NotBound) {
                    throw new Exception($"Variable not bound: {x}");
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
                throw new Exception("Test expression must be boolean: if");
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
            if (e is Expressions.NilLit) {
                return new ExprValue.NilV();
            }
            if (e is Expressions.HaltExp) {
                throw new Exception((e as Expressions.HaltExp).Message);
            }

            throw new NotSupportedException($"expression {e} cannot eval.");
        }

        private static Result eval_declEntry(Environment<ExprValue> env, Declarations.DeclBase p) {
            if (p is Declarations.Decl) {
                var d = (Declarations.Decl)p;
                var newenv = env;
                var ret = new Result("", env, null);

                foreach (var bind in d.Binds) {
                    var v = EvalExpressions(newenv, bind.Item2);
                    ret = new Result(bind.Item1, Environment.Extend(bind.Item1, v, ret.Env), v);
                }
                return ret;
            }
            if (p is Declarations.RecDecl) {
                var d = (Declarations.RecDecl)p;
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
