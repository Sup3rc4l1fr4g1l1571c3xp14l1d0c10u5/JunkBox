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
            private static Dictionary<string, ExprValue> EvalPatternExpressions(
                PatternExpressions pattern,
                ExprValue value
            )
            {
                return PatternExpressions.Match(
                    WildP: (p) =>
                    {
                        return new Dictionary<string, ExprValue>();
                    },
                    IntP: (p) =>
                    {
                        var v = value as ExprValue.IntV;
                        if (v != null && p.Value == v.Value)
                        {
                            return new Dictionary<string, ExprValue>();
                        }
                        else
                        {
                            return null;
                        }
                    },
                    StrP: (p) =>
                    {
                        var v = value as ExprValue.StrV;
                        if (v != null && p.Value == v.Value)
                        {
                            return new Dictionary<string, ExprValue>();
                        }
                        else
                        {
                            return null;
                        }
                    },
                    BoolP: (p) =>
                    {
                        var v = value as ExprValue.BoolV;
                        if (v != null && p.Value == v.Value)
                        {
                            return new Dictionary<string, ExprValue>();
                        }
                        else
                        {
                            return null;
                        }
                    },
                    UnitP: (p) =>
                    {
                        if (value is ExprValue.UnitV)
                        {
                            return new Dictionary<string, ExprValue>();
                        }
                        else
                        {
                            return null;
                        }
                    },
                    VarP: (p) =>
                    {
                        return new Dictionary<string, ExprValue>() {{p.Id, value}};
                    },
                    ConsP: (p) =>
                    {
                        var v = value as ExprValue.ListV;
                        if (v != null)
                        {
                            var dic = new Dictionary<string, ExprValue>();
                            if (ReferenceEquals(v, ExprValue.ListV.Empty))
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
                            var ret1 = EvalPatternExpressions(p.Value, v.Value);
                            if (ret1 == null)
                            {
                                return null;
                            }
                            dic = ret1.Aggregate(dic, (s, x) =>
                            {
                                s[x.Key] = x.Value;
                                return s;
                            });

                            var ret2 = EvalPatternExpressions(p.Next, v.Next);
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
                        return null;
                    },
                    OptionP: (p) =>
                    {
                        var v = value as ExprValue.OptionV;
                        if (v != null)
                        {
                            var dic = new Dictionary<string, ExprValue>();
                            if (p == PatternExpressions.OptionP.None || ReferenceEquals(v, ExprValue.OptionV.None))
                            {
                                if (p == PatternExpressions.OptionP.None && ReferenceEquals(v, ExprValue.OptionV.None))
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
                                var ret1 = EvalPatternExpressions(p.Value, v.Value);
                                if (ret1 == null)
                                {
                                    return null;
                                }
                                else
                                {
                                    dic = ret1.Aggregate(dic,
                                        (s, x) =>
                                        {
                                            s[x.Key] = x.Value;
                                            return s;
                                        });
                                    return dic;
                                }
                            }
                        }
                        return null;
                    },
                    TupleP: (p) =>
                    {
                        var v = value as ExprValue.TupleV;
                        if (v != null)
                        {
                            var dic = new Dictionary<string, ExprValue>();

                            if (p.Members.Length != v.Members.Length)
                            {
                                return null;
                            }
                            foreach (var pq in p.Members.Zip(v.Members, Tuple.Create))
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
                    },
                    RecordP: (p) =>
                    {
                        var v = value as ExprValue.RecordV;
                        if (v != null)
                        {
                            var dic = new Dictionary<string, ExprValue>();

                            if (p.Members.Length != v.Members.Length)
                            {
                                return null;
                            }
                            foreach (var pq in p.Members.Zip(v.Members, Tuple.Create))
                            {
                                var ret1 = EvalPatternExpressions(pq.Item1.Item2, pq.Item2.Item2);
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
                    },
                    VariantP: (p) =>
                    {
                        var v = value as ExprValue.VariantV;
                        if (v != null)
                        {
                            var dic = new Dictionary<string, ExprValue>();

                            if (p.ConstructorId != v.Tag)
                            {
                                return null;
                            }
                            var ret1 = EvalPatternExpressions(p.Body, v.Value);
                            if (ret1 == null)
                            {
                                return null;
                            }
                            dic = ret1.Aggregate(dic, (s, x) =>
                            {
                                s[x.Key] = x.Value;
                                return s;
                            });
                            return dic;
                        }
                        return null;
                    },
                    Other: (p) => { return null; }
                )(pattern);

            }

            /// <summary>
            /// 式の評価
            /// </summary>
            /// <param name="env"></param>
            /// <param name="e"></param>
            /// <returns></returns>
            private static ExprValue EvalExpressions(Environment<ExprValue> env, Expressions e)
            {
                return Expressions.Match(
                    Var: (exp) =>
                    {
                        var x = exp.Id;
                        try
                        {
                            return Environment.LookUp(x, env);
                        }
                        catch (Exception.NotBound)
                        {
                            throw new Exception.NotBound($"Variable not bound: {x}");
                        }
                    },
                    IntLit: (exp) =>
                    {
                        return new ExprValue.IntV(exp.Value);
                    },
                    StrLit: (exp) =>
                    {
                        return new ExprValue.StrV(exp.Value);
                    },
                    BoolLit: (exp) =>
                    {
                        return new ExprValue.BoolV(exp.Value);
                    },
                    EmptyListLit: (exp) =>
                    {
                        return ExprValue.ListV.Empty;
                    },
                    UnitLit: (exp) =>
                    {
                        return new ExprValue.UnitV();
                    },
                    IfExp: (exp) =>
                    {
                        var cond = EvalExpressions(env, exp.Cond);
                        if (cond is ExprValue.BoolV)
                        {
                            var v = ((ExprValue.BoolV) cond).Value;
                            if (v)
                            {
                                return EvalExpressions(env, exp.Then);
                            }
                            else
                            {
                                return EvalExpressions(env, exp.Else);
                            }
                        }
                        throw new NotSupportedException("Test expression must be boolean: if");
                    },
                    LetExp: (exp) =>
                    {
                        var newenv = env;
                        foreach (var bind in exp.Binds)
                        {
                            var value = EvalExpressions(env, bind.Item2);
                            newenv = Environment.Extend(bind.Item1, value, newenv);
                        }
                        return EvalExpressions(newenv, exp.Body);
                    },
                    LetRecExp: (exp) =>
                    {
                        var dummyenv = Environment<ExprValue>.Empty;
                        var newenv = env;
                        var procs = new List<ExprValue.ProcV>();

                        foreach (var bind in exp.Binds)
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
                        return EvalExpressions(newenv, exp.Body);
                    },
                    FunExp: (exp) =>
                    {
                        return new ExprValue.ProcV(exp.Arg, exp.Body, env);
                    },
                    AppExp: (exp) =>
                    {
                        var funval = EvalExpressions(env, exp.Fun);
                        var arg = EvalExpressions(env, exp.Arg);
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
                    },
                    MatchExp: (exp) =>
                    {
                        var val = EvalExpressions(env, exp.Exp);
                        foreach (var pattern in exp.Patterns)
                        {
                            var ret = EvalPatternExpressions(pattern.Item1, val);
                            if (ret != null)
                            {
                                var newenv = ret.Aggregate(env, (s, x) => Environment.Extend(x.Key, x.Value, s));
                                return EvalExpressions(newenv, pattern.Item2);
                            }
                        }
                        throw new NotSupportedException($"value {val} is not match.");
                    },
                    TupleExp: (exp) =>
                    {
                        return new ExprValue.TupleV(exp.Members.Select(x => EvalExpressions(env, x)).ToArray());
                    },
                    OptionExp: (exp) =>
                    {
                        if (exp == Expressions.OptionExp.None)
                        {
                            return ExprValue.OptionV.None;
                        }
                        else
                        {
                            return new ExprValue.OptionV(EvalExpressions(env, exp.Expr));
                        }
                    },
                    HaltExp: (exp) =>
                    {
                        throw new Exception.HaltException(exp.Message);
                    },

                    RecordExp: (exp) =>
                    {
                        return new ExprValue.RecordV(exp.Members
                            .Select(x => Tuple.Create(x.Item1, EvalExpressions(env, x.Item2))).ToArray());
                    },
                    MemberExp: (exp) =>
                    {
                        var v = EvalExpressions(env, exp.Expression);
                        System.Diagnostics.Debug.Assert(v is ExprValue.RecordV);
                        var recv = ((ExprValue.RecordV) v);
                        return recv.Members.First(x => x.Item1 == exp.Member).Item2;
                    },
                    DestructiveUpdateExp: (exp) =>
                    {
                        var v1 = EvalExpressions(env, exp.Expression);
                        var v2 = EvalExpressions(env, exp.Value);
                        System.Diagnostics.Debug.Assert(v1 is ExprValue.RecordV);
                        var recv = ((ExprValue.RecordV) v1);
                        var idx = recv.Members.TakeWhile(x => x.Item1 != exp.Member).Count();
                        recv.Members[idx] = Tuple.Create(recv.Members[idx].Item1, v2);
                        return new ExprValue.UnitV();
                    },
                    ConstructorExp: (exp) =>
                    {
                        var funval = Environment.LookUp(exp.ConstructorName, env);
                        var arg = EvalExpressions(env, exp.Arg);
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
                    },
                    VariantExp: (exp) =>
                    {
                        return new ExprValue.VariantV(exp.TagName.Value, (int)exp.Tag.Value,
                            EvalExpressions(env, exp.Value));
                    },
                    Other: (exp) => throw new NotSupportedException($"expression {exp} cannot eval.")
                )(e);
            }

            /// let もしくは let rec の評価
            private static Result eval_declEntry(Environment<ExprValue> env, Toplevel.Binding.DeclBase p)
            {
                return Toplevel.Binding.DeclBase.Match(
                    LetDecl: (d) =>
                    {
                        var newenv = env;
                        var ret = new Result("", env, null);

                        foreach (var bind in d.Binds)
                        {
                            var v = EvalExpressions(newenv, bind.Item2);
                            ret = new Result(bind.Item1, Environment.Extend(bind.Item1, v, ret.Env), v);
                        }
                        return ret;
                    },
                    LetRecDecl: (d) =>
                    {
                        var newenv = env;
                        var ret = new Result("", env, null);

                        var dummyenv = Environment<ExprValue>.Empty;
                        var procs = new List<ExprValue.ProcV>();

                        foreach (var bind in d.Binds)
                        {
                            var v = EvalExpressions(dummyenv, bind.Item2);
                            var procv = v as ExprValue.ProcV;
                            if (procv != null)
                            {
                                procs.Add(procv);
                            }
                            newenv = Environment.Extend(bind.Item1, v, newenv);
                            ret = new Result(bind.Item1, newenv, v);
                        }

                        foreach (var proc in procs)
                        {
                            proc.BackPatchEnv(newenv);
                        }
                        return ret;
                    },
                    Other: (e) => throw new NotSupportedException($"{e.GetType().FullName} cannot eval.")
                )(p);
                
            }

            /// Todo: eval_toplevelへリネーム
            public static Result eval_decl(Environment<ExprValue> env, Environment<ExprValue> builtins, Toplevel p)
            {
                return Toplevel.Match(
                    Empty: (e) => new Result("", env, null),
                    Exp: (e) => {
                        var v = EvalExpressions(env, e.Syntax);
                        return new Result("-", env, v);
                    },
                    Binding: (e) => {
                        var newenv = env;
                        Result ret = new Result("", env, null);
                        foreach (var d in e.Entries)
                        {
                            ret = eval_declEntry(newenv, d);
                            newenv = ret.Env;
                        }
                        return ret;
                    },
                    TypeDef: (e) => {
                        var vt = e.Type as TypeExpressions.VariantType;
                        if (vt != null)
                        {
                            var index = 0;
                            foreach (var member in vt.Members)
                            {
                                var constructor = (ExprValue)new ExprValue.ProcV("@p", new Expressions.VariantExp(new Expressions.StrLit(member.Item1), new Expressions.IntLit(index), new Expressions.Var("@p")), env);
                                env = Environment.Extend(member.Item1, constructor, env);
                                index++;
                            }
                        }
                        return new Result("", env, null);
                    },
                    ExternalDecl: (e) =>{
                        var val = Environment.LookUp(e.Symbol, builtins);
                        var newenv = Environment.Extend(e.Id, val, env);
                        return new Result(e.Id, newenv, val);
                    },
                    Other: (e) => throw new NotSupportedException($"{e.GetType().FullName} cannot eval.")
                )(p);
            }
        }
    }

}