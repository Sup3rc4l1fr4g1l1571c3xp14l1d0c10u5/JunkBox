using System;
using System.Collections.Generic;
using System.Linq;
using MiniMAL.Syntax;

namespace MiniMAL
{
    namespace Interpreter
    {
        /// <summary>
        /// 抽象構文木インタプリタ
        /// </summary>
        public static partial class AbstractSyntaxTreeInterpreter
        {
            /// <summary>
            /// パターンマッチの評価
            /// </summary>
            /// <param name="pattern"></param>
            /// <param name="value"></param>
            /// <returns></returns>
            private static Dictionary<string, ExprValue> EvalPatternExpressions(PatternExpressions pattern,
                ExprValue value)
            {
                if (pattern is PatternExpressions.WildP)
                {
                    return new Dictionary<string, ExprValue>();
                }
                if (pattern is PatternExpressions.IntP && value is ExprValue.IntV)
                {
                    if (((PatternExpressions.IntP) pattern).Value == ((ExprValue.IntV) value).Value)
                    {
                        return new Dictionary<string, ExprValue>();
                    }
                    else
                    {
                        return null;
                    }
                }
                if (pattern is PatternExpressions.StrP && value is ExprValue.StrV)
                {
                    if (((PatternExpressions.StrP) pattern).Value == ((ExprValue.StrV) value).Value)
                    {
                        return new Dictionary<string, ExprValue>();
                    }
                    else
                    {
                        return null;
                    }
                }
                if (pattern is PatternExpressions.BoolP && value is ExprValue.BoolV)
                {
                    if (((PatternExpressions.BoolP) pattern).Value == ((ExprValue.BoolV) value).Value)
                    {
                        return new Dictionary<string, ExprValue>();
                    }
                    else
                    {
                        return null;
                    }
                }
                if (pattern is PatternExpressions.UnitP && value is ExprValue.UnitV)
                {
                    return new Dictionary<string, ExprValue>();
                }
                if (pattern is PatternExpressions.VarP)
                {
                    return new Dictionary<string, ExprValue>() {{((PatternExpressions.VarP) pattern).Id, value}};
                }
                if (pattern is PatternExpressions.ConsP && value is ExprValue.ListV)
                {
                    var p = (PatternExpressions.ConsP) pattern;
                    var q = (ExprValue.ListV) value;
                    var dic = new Dictionary<string, ExprValue>();
                    if (q == ExprValue.ListV.Empty)
                    {
                        if (p == PatternExpressions.ConsP.Empty)
                        {
                            return dic;
                        }
                        else
                        {
                            return null;
                        }
                    }
                    var ret1 = EvalPatternExpressions(p.Value, q.Value);
                    if (ret1 == null)
                    {
                        return null;
                    }
                    dic = ret1.Aggregate(dic, (s, x) =>
                    {
                        s[x.Key] = x.Value;
                        return s;
                    });

                    var ret2 = EvalPatternExpressions(p.Next, q.Next);
                    if (ret2 == null)
                    {
                        return null;
                    }
                    dic = ret2.Aggregate(dic, (s, x) =>
                    {
                        s[x.Key] = x.Value;
                        return s;
                    });

                    return dic;
                }
                if (pattern is PatternExpressions.OptionP && value is ExprValue.OptionV)
                {
                    var p = (PatternExpressions.OptionP) pattern;
                    var q = (ExprValue.OptionV) value;
                    var dic = new Dictionary<string, ExprValue>();
                    if (p == PatternExpressions.OptionP.None || q == ExprValue.OptionV.None)
                    {
                        if (p == PatternExpressions.OptionP.None && q == ExprValue.OptionV.None)
                        {
                            return dic;
                        }
                        else
                        {
                            return null;
                        }
                    }
                    else
                    {
                        var ret1 = EvalPatternExpressions(p.Value, q.Value);
                        if (ret1 == null)
                        {
                            return null;
                        }
                        dic = ret1.Aggregate(dic,
                            (s, x) =>
                            {
                                s[x.Key] = x.Value;
                                return s;
                            });
                        return dic;
                    }
                }
                if (pattern is PatternExpressions.TupleP && value is ExprValue.TupleV)
                {
                    var p = (PatternExpressions.TupleP) pattern;
                    var q = (ExprValue.TupleV) value;
                    var dic = new Dictionary<string, ExprValue>();

                    if (p.Members.Length != q.Members.Length)
                    {
                        return null;
                    }
                    foreach (var pq in p.Members.Zip(q.Members, Tuple.Create))
                    {
                        var ret1 = EvalPatternExpressions(pq.Item1, pq.Item2);
                        if (ret1 == null)
                        {
                            return null;
                        }
                        dic = ret1.Aggregate(dic, (s, x) =>
                        {
                            s[x.Key] = x.Value;
                            return s;
                        });
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
            private static ExprValue EvalExpressions(Environment<ExprValue> env, Expressions e)
            {
                if (e is Expressions.Var)
                {
                    var x = ((Expressions.Var) e).Id;
                    try
                    {
                        return Environment.LookUp(x, env);
                    }
                    catch (Exception.NotBound)
                    {
                        throw new Exception.NotBound($"Variable not bound: {x}");
                    }
                }
                if (e is Expressions.IntLit)
                {
                    return new ExprValue.IntV(((Expressions.IntLit) e).Value);
                }
                if (e is Expressions.StrLit)
                {
                    return new ExprValue.StrV(((Expressions.StrLit) e).Value);
                }
                if (e is Expressions.BoolLit)
                {
                    return new ExprValue.BoolV(((Expressions.BoolLit) e).Value);
                }
                if (e is Expressions.EmptyListLit)
                {
                    return ExprValue.ListV.Empty;
                }
                if (e is Expressions.UnitLit)
                {
                    return new ExprValue.UnitV();
                }
                //if (e is Expressions.BuiltinOp)
                //{
                //    var op = ((Expressions.BuiltinOp)e).Op;
                //    var args = ((Expressions.BuiltinOp)e).Exprs.Select(x => EvalExpressions(env, x)).ToArray();
                //    return EvalBuiltinExpressions(op, args);
                //}
                if (e is Expressions.IfExp)
                {
                    var cond = EvalExpressions(env, ((Expressions.IfExp) e).Cond);
                    if (cond is ExprValue.BoolV)
                    {
                        var v = ((ExprValue.BoolV) cond).Value;
                        if (v)
                        {
                            return EvalExpressions(env, ((Expressions.IfExp) e).Then);
                        }
                        else
                        {
                            return EvalExpressions(env, ((Expressions.IfExp) e).Else);
                        }
                    }
                    throw new NotSupportedException("Test expression must be boolean: if");
                }
                if (e is Expressions.LetExp)
                {
                    var newenv = env;
                    foreach (var bind in ((Expressions.LetExp) e).Binds)
                    {
                        var value = EvalExpressions(env, bind.Item2);
                        newenv = Environment.Extend(bind.Item1, value, newenv);
                    }
                    return EvalExpressions(newenv, ((Expressions.LetExp) e).Body);
                }
                if (e is Expressions.LetRecExp)
                {
                    var dummyenv = Environment<ExprValue>.Empty;
                    var newenv = env;
                    var procs = new List<ExprValue.ProcV>();

                    foreach (var bind in ((Expressions.LetRecExp) e).Binds)
                    {
                        var value = EvalExpressions(dummyenv, bind.Item2);
                        if (value is ExprValue.ProcV)
                        {
                            procs.Add((ExprValue.ProcV) value);
                        }
                        newenv = Environment.Extend(bind.Item1, value, newenv);
                    }

                    foreach (var proc in procs)
                    {
                        proc.BackPatchEnv(newenv);
                    }
                    return EvalExpressions(newenv, ((Expressions.LetRecExp) e).Body);
                }
                if (e is Expressions.FunExp)
                {
                    return new ExprValue.ProcV(((Expressions.FunExp) e).Arg, ((Expressions.FunExp) e).Body, env);
                }
                if (e is Expressions.AppExp)
                {
                    var funval = EvalExpressions(env, ((Expressions.AppExp) e).Fun);
                    var arg = EvalExpressions(env, ((Expressions.AppExp) e).Arg);
                    if (funval is ExprValue.ProcV)
                    {
                        var newenv = Environment.Extend(((ExprValue.ProcV) funval).Id, arg,
                            ((ExprValue.ProcV) funval).Env);
                        return EvalExpressions(newenv, ((ExprValue.ProcV) funval).Body);
                    }
                    else if (funval is ExprValue.BProcV)
                    {
                        return ((ExprValue.BProcV) funval).Proc(arg);
                    }
                    else
                    {
                        throw new NotSupportedException($"{funval.GetType().FullName} cannot eval.");
                    }
                }
                if (e is Expressions.MatchExp)
                {
                    var val = EvalExpressions(env, ((Expressions.MatchExp) e).Exp);
                    foreach (var pattern in ((Expressions.MatchExp) e).Patterns)
                    {
                        var ret = EvalPatternExpressions(pattern.Item1, val);
                        if (ret != null)
                        {
                            var newenv = ret.Aggregate(env, (s, x) => Environment.Extend(x.Key, x.Value, s));
                            return EvalExpressions(newenv, pattern.Item2);
                        }
                    }
                    throw new NotSupportedException($"value {val} is not match.");
                }
                if (e is Expressions.TupleExp)
                {
                    var t = (Expressions.TupleExp) e;
                    return new ExprValue.TupleV(t.Members.Select(x => EvalExpressions(env, x)).ToArray());
                }
                if (e is Expressions.OptionExp)
                {
                    if (e == Expressions.OptionExp.None)
                    {
                        return ExprValue.OptionV.None;
                    }
                    else
                    {
                        return new ExprValue.OptionV(EvalExpressions(env, ((Expressions.OptionExp) e).Expr));
                    }
                }
                if (e is Expressions.HaltExp)
                {
                    throw new Exception.HaltException(((Expressions.HaltExp) e).Message);
                }

                throw new NotSupportedException($"expression {e} cannot eval.");
            }

            private static Result eval_declEntry(Environment<ExprValue> env, Toplevel.Binding.DeclBase p)
            {
                if (p is Toplevel.Binding.LetDecl)
                {
                    var d = (Toplevel.Binding.LetDecl) p;
                    var newenv = env;
                    var ret = new Result("", env, null);

                    foreach (var bind in d.Binds)
                    {
                        var v = EvalExpressions(newenv, bind.Item2);
                        ret = new Result(bind.Item1, Environment.Extend(bind.Item1, v, ret.Env), v);
                    }
                    return ret;
                }
                if (p is Toplevel.Binding.LetRecDecl)
                {
                    var d = (Toplevel.Binding.LetRecDecl) p;
                    var newenv = env;
                    var ret = new Result("", env, null);

                    var dummyenv = Environment<ExprValue>.Empty;
                    var procs = new List<ExprValue.ProcV>();

                    foreach (var bind in d.Binds)
                    {
                        var v = EvalExpressions(dummyenv, bind.Item2);
                        if (v is ExprValue.ProcV)
                        {
                            procs.Add((ExprValue.ProcV) v);
                        }
                        newenv = Environment.Extend(bind.Item1, v, newenv);
                        ret = new Result(bind.Item1, newenv, v);
                    }

                    foreach (var proc in procs)
                    {
                        proc.BackPatchEnv(newenv);
                    }
                    return ret;
                }
                throw new NotSupportedException($"{p.GetType().FullName} cannot eval.");
            }

            public static Result eval_decl(Environment<ExprValue> env, Environment<ExprValue> builtins,
                Toplevel p)
            {
                if (p is Toplevel.Exp)
                {
                    var e = (Toplevel.Exp) p;
                    var v = EvalExpressions(env, e.Syntax);
                    return new Result("-", env, v);
                }
                if (p is Toplevel.ExternalDecl)
                {
                    var e = (Toplevel.ExternalDecl) p;
                    var val = Environment.LookUp(e.Symbol, builtins);
                    var newenv = Environment.Extend(e.Id, val, env);
                    return new Result(e.Id, newenv, val);
                }
                if (p is Toplevel.Binding)
                {
                    var ds = (Toplevel.Binding) p;
                    var newenv = env;
                    Result ret = new Result("", env, null);
                    foreach (var d in ds.Entries)
                    {
                        ret = eval_declEntry(newenv, d);
                        newenv = ret.Env;
                    }
                    return ret;
                }
                if (p is Toplevel.Empty)
                {
                    return new Result("", env, null);
                }
                throw new NotSupportedException($"{p.GetType().FullName} cannot eval.");
            }
        }
    }

}