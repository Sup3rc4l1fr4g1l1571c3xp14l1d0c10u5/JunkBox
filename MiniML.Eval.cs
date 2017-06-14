using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace MiniML {
    public static partial class MiniML {
        /// <summary>
        /// ï]âøïî
        /// </summary>
        public static class Eval {
            /// <summary>
            /// ï]âøåãâ 
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
            /// ìÒçÄââéZéq op ÇÃï]âø
            /// </summary>
            /// <param name="op"></param>
            /// <param name="arg1"></param>
            /// <param name="arg2"></param>
            /// <returns></returns>
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
                    case Syntax.BinOp.Kind.Minus: {
                            if (arg1 is ExprValue.IntV && arg2 is ExprValue.IntV) {
                                var i1 = ((ExprValue.IntV)arg1).Value;
                                var i2 = ((ExprValue.IntV)arg2).Value;
                                return new ExprValue.IntV(i1 - i2);
                            }
                            throw new Exception("Both arguments must be integer: -");
                        }
                    case Syntax.BinOp.Kind.Mult: {
                            if (arg1 is ExprValue.IntV && arg2 is ExprValue.IntV) {
                                var i1 = ((ExprValue.IntV)arg1).Value;
                                var i2 = ((ExprValue.IntV)arg2).Value;
                                return new ExprValue.IntV(i1 * i2);
                            }
                            throw new Exception("Both arguments must be integer: *");
                        }
                    case Syntax.BinOp.Kind.Div: {
                            if (arg1 is ExprValue.IntV && arg2 is ExprValue.IntV) {
                                var i1 = ((ExprValue.IntV)arg1).Value;
                                var i2 = ((ExprValue.IntV)arg2).Value;
                                return new ExprValue.IntV(i1 / i2);
                            }
                            throw new Exception("Both arguments must be integer: /");
                        }
                    case Syntax.BinOp.Kind.Lt: {
                            if (arg1 is ExprValue.IntV && arg2 is ExprValue.IntV) {
                                var i1 = ((ExprValue.IntV)arg1).Value;
                                var i2 = ((ExprValue.IntV)arg2).Value;
                                return new ExprValue.BoolV(i1 < i2);
                            }
                            throw new Exception("Both arguments must be integer: <");
                        }
                    case Syntax.BinOp.Kind.Le: {
                            if (arg1 is ExprValue.IntV && arg2 is ExprValue.IntV) {
                                var i1 = ((ExprValue.IntV)arg1).Value;
                                var i2 = ((ExprValue.IntV)arg2).Value;
                                return new ExprValue.BoolV(i1 <= i2);
                            }
                            throw new Exception("Both arguments must be integer: <=");
                        }
                    case Syntax.BinOp.Kind.Gt: {
                            if (arg1 is ExprValue.IntV && arg2 is ExprValue.IntV) {
                                var i1 = ((ExprValue.IntV)arg1).Value;
                                var i2 = ((ExprValue.IntV)arg2).Value;
                                return new ExprValue.BoolV(i1 > i2);
                            }
                            throw new Exception("Both arguments must be integer: >");
                        }
                    case Syntax.BinOp.Kind.Ge: {
                            if (arg1 is ExprValue.IntV && arg2 is ExprValue.IntV) {
                                var i1 = ((ExprValue.IntV)arg1).Value;
                                var i2 = ((ExprValue.IntV)arg2).Value;
                                return new ExprValue.BoolV(i1 >= i2);
                            }
                            throw new Exception("Both arguments must be integer: >=");
                        }
                    case Syntax.BinOp.Kind.Eq: {
                            if (arg1 is ExprValue.IntV && arg2 is ExprValue.IntV) {
                                var i1 = ((ExprValue.IntV)arg1).Value;
                                var i2 = ((ExprValue.IntV)arg2).Value;
                                return new ExprValue.BoolV(i1 == i2);
                            }
                            if (arg1 is ExprValue.BoolV && arg2 is ExprValue.BoolV) {
                                var i1 = ((ExprValue.BoolV)arg1).Value;
                                var i2 = ((ExprValue.BoolV)arg2).Value;
                                return new ExprValue.BoolV(i1 == i2);
                            }
                            throw new Exception("Both arguments must be integer or boolean: =");
                        }
                    case Syntax.BinOp.Kind.Ne: {
                            if (arg1 is ExprValue.IntV && arg2 is ExprValue.IntV) {
                                var i1 = ((ExprValue.IntV)arg1).Value;
                                var i2 = ((ExprValue.IntV)arg2).Value;
                                return new ExprValue.BoolV(i1 != i2);
                            }
                            if (arg1 is ExprValue.BoolV && arg2 is ExprValue.BoolV) {
                                var i1 = ((ExprValue.BoolV)arg1).Value;
                                var i2 = ((ExprValue.BoolV)arg2).Value;
                                return new ExprValue.BoolV(i1 != i2);
                            }
                            throw new Exception("Both arguments must be integer or boolean: <>");
                        }
                    case Syntax.BinOp.Kind.LAnd: {
                            if (arg1 is ExprValue.BoolV && arg2 is ExprValue.BoolV) {
                                var i1 = ((ExprValue.BoolV)arg1).Value;
                                var i2 = ((ExprValue.BoolV)arg2).Value;
                                return new ExprValue.BoolV(i1 && i2);
                            }
                            throw new Exception("Both arguments must be boolean or boolean: &&");
                        }
                    case Syntax.BinOp.Kind.LOr: {
                        if (arg1 is ExprValue.BoolV && arg2 is ExprValue.BoolV) {
                            var i1 = ((ExprValue.BoolV)arg1).Value;
                            var i2 = ((ExprValue.BoolV)arg2).Value;
                            return new ExprValue.BoolV(i1 || i2);
                        }
                        throw new Exception("Both arguments must be integer or boolean: ||");
                    }
                    case Syntax.BinOp.Kind.Cons: {
                        if (arg2 is ExprValue.ConsV) {
                            return new ExprValue.ConsV(arg1, arg2 as ExprValue.ConsV);
                        }
                            throw new Exception("Right arguments must be List: ::");
                    }
                    default:
                        throw new ArgumentOutOfRangeException(nameof(op), op, null);
                }
            }

            public static Environment<ExprValue> eval_match(Environment<ExprValue> env, ExprValue value, Pattern pattern) {
                if (pattern is Pattern.IntP && value is ExprValue.IntV) {
                    if ((pattern as Pattern.IntP).Value == (value as ExprValue.IntV).Value)
                    {
                        return env;
                    }
                    return null;
                }
                if (pattern is Pattern.BoolP && value is ExprValue.BoolV) {
                    if ((pattern as Pattern.BoolP).Value == (value as ExprValue.BoolV).Value) {
                        return env;
                    }
                    return null;
                }
                if (pattern is Pattern.VarP) {
                    return Environment.Extend((pattern as Pattern.VarP).Id, value, env);
                }
                if (pattern is Pattern.WildP) {
                    return env;
                }
                if (pattern is Pattern.UnitP && value is ExprValue.UnitV) {
                    return env;
                }
                if (pattern is Pattern.TupleP && value is ExprValue.TupleV)
                {
                    if (value == ExprValue.TupleV.Empty) {
                        return (pattern == Pattern.TupleP.Empty) ? env : null;
                    } else {
                        var v = value as ExprValue.TupleV;
                        var newenv = eval_match(env, v.Value, (pattern as Pattern.TupleP).Pattern);
                        if (newenv != null) {
                            return eval_match(newenv, v.Next, (pattern as Pattern.TupleP).Next);
                        }
                    }

                }
                if (pattern is Pattern.ConsP && value is ExprValue.ConsV) {
                    if (value == ExprValue.ConsV.Empty)
                    {
                        return (pattern == Pattern.ConsP.Empty) ? env : null;
                    } else {
                        var v = value as ExprValue.ConsV;
                        var newenv = eval_match(env, v.Value, (pattern as Pattern.ConsP).Pattern);
                        if (newenv != null)
                        {
                            return eval_match(newenv, v.Next, (pattern as Pattern.ConsP).Next);
                        }
                    }
                }
                return null;
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
                if (e is Syntax.LLit) {
                    return ExprValue.ConsV.Empty;
                }
                if (e is Syntax.Unit) {
                    return new ExprValue.UnitV();
                }
                if (e is Syntax.TupleExp) {
                    return ((Syntax.TupleExp)e).Values.Select(x => eval_exp(env, x)).Reverse().Aggregate(ExprValue.TupleV.Empty, ((s,x) => new ExprValue.TupleV(x,s)));
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
                if (e is Syntax.LetExp) {
                    var newenv = env;
                    foreach (var bind in ((Syntax.LetExp)e).Binds) {
                        var value = eval_exp(env, bind.Item2);
                        newenv = Environment.Extend(bind.Item1, value, newenv);
                    }
                    return eval_exp(newenv, ((Syntax.LetExp)e).Body);
                }
                if (e is Syntax.LetRecExp) {
                    var newenv = env;
                    var procs = new List<ExprValue.ProcV>();
                    foreach (var bind in ((Syntax.LetRecExp)e).Binds) {
                        var v = eval_exp(newenv, bind.Item2);
                        if (v is ExprValue.ProcV) {
                            procs.Add((ExprValue.ProcV)v);
                        }
                        newenv = Environment.Extend(bind.Item1, v, newenv);
                    }
                    foreach (var proc in procs) {
                        proc.BackPatchEnv(newenv);
                    }
                    return eval_exp(newenv, ((Syntax.LetRecExp)e).Body);
                }
                if (e is Syntax.FunExp) {
                    return new ExprValue.ProcV(((Syntax.FunExp)e).Arg, ((Syntax.FunExp)e).Body, env);
                }
                if (e is Syntax.DFunExp) {
                    return new ExprValue.DProcV(((Syntax.DFunExp)e).Arg, ((Syntax.DFunExp)e).Body);
                }
                if (e is Syntax.AppExp) {
                    var funval = eval_exp(env, ((Syntax.AppExp)e).Fun);
                    var arg = eval_exp(env, ((Syntax.AppExp)e).Arg);
                    if (funval is ExprValue.ProcV) {
                        var newenv = Environment.Extend(((ExprValue.ProcV)funval).Id, arg, ((ExprValue.ProcV)funval).Env);
                        return eval_exp(newenv, ((ExprValue.ProcV)funval).Body);
                    } else if (funval is ExprValue.DProcV) {
                        var newenv = Environment.Extend(((ExprValue.DProcV)funval).Id, arg, env);
                        return eval_exp(newenv, ((ExprValue.DProcV)funval).Body);
                    } else {
                        throw new NotSupportedException($"{funval.GetType().FullName} cannot eval.");
                    }
                }

                if (e is Syntax.MatchExp)
                {
                    var val = eval_exp(env, ((Syntax.MatchExp) e).Expr);
                    foreach (var entry in ((Syntax.MatchExp)e).Entries)
                    {
                        var newenv = eval_match(env, val, entry.Item1);
                        if (newenv != null)
                        {
                            return eval_exp(newenv, entry.Item2); ;
                        }
                    }
                    throw new Exception($"not match.");
                }

                throw new NotSupportedException($"{e.GetType().FullName} cannot eval.");
            }

            public static Result eval_decl(Environment<ExprValue> env, Program p) {
                if (p is Program.Exp) {
                    var e = (Program.Exp)p;
                    var v = eval_exp(env, e.Syntax);
                    return new Result("-", env, v);
                }
                if (p is Program.Decls) {
                    var ds = (Program.Decls)p;
                    var newenv = env;
                    Result ret = new Result("", env, (ExprValue)null);
                    foreach (var d in ds.Entries) {
                        if (d is Program.Decls.LetDecl) {
                            var decl = (Program.Decls.LetDecl)d;
                            foreach (var bind in decl.Binds) {
                                var v = eval_exp(newenv, bind.Item2);
                                ret = new Result(bind.Item1, Environment.Extend(bind.Item1, v, ret.Env), v);
                            }
                        } else if (d is Program.Decls.LetRecDecl) {
                            var decl = (Program.Decls.LetRecDecl)d;
                            var procs = new List<ExprValue.ProcV>();
                            foreach (var bind in decl.Binds) {
                                var v = eval_exp(ret.Env, bind.Item2);
                                if (v is ExprValue.ProcV) {
                                    procs.Add((ExprValue.ProcV)v);
                                }
                                ret = new Result(bind.Item1, Environment.Extend(bind.Item1, v, ret.Env), v);
                            }
                            foreach (var proc in procs) {
                                proc.BackPatchEnv(ret.Env);
                            }
                        } else {
                            throw new NotSupportedException($"{d.GetType().FullName} cannot eval.");
                        }
                        newenv = ret.Env;
                    }
                    return ret;
                }
                throw new NotSupportedException($"{p.GetType().FullName} cannot eval.");
            }
        }
    }
}