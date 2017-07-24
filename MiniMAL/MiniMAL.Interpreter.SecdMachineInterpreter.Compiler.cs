using System;
using System.Collections.Generic;
using System.Linq;
using MiniMAL.Syntax;

namespace MiniMAL
{
    namespace Interpreter
    {
        public static partial class SecdMachineInterpreter
        {
            public static class Compiler
            {
                /// <summary>
                /// 式のコンパイル
                /// </summary>
                /// <param name="expr">式</param>
                /// <param name="env">名前環境</param>
                /// <param name="code">後続のコード</param>
                /// <param name="isTail"></param>
                /// <returns></returns>
                private static LinkedList<Instructions> CompileExpr(Expressions expr,
                    LinkedList<LinkedList<string>> env,
                    LinkedList<Instructions> code, bool isTail)
                {
                    if (expr is Expressions.IntLit)
                    {
                        return LinkedList.Extend(
                            new Instructions.Ldc(new ExprValue.IntV(((Expressions.IntLit) expr).Value)), code);
                    }
                    if (expr is Expressions.StrLit)
                    {
                        return LinkedList.Extend(
                            new Instructions.Ldc(new ExprValue.StrV(((Expressions.StrLit) expr).Value)), code);
                    }
                    if (expr is Expressions.BoolLit)
                    {
                        return LinkedList.Extend(
                            new Instructions.Ldc(new ExprValue.BoolV(((Expressions.BoolLit) expr).Value)), code);
                    }
                    if (expr is Expressions.EmptyListLit)
                    {
                        return LinkedList.Extend(new Instructions.Ldc(ExprValue.ListV.Empty), code);
                    }
                    if (expr is Expressions.UnitLit)
                    {
                        return LinkedList.Extend(new Instructions.Ldc(new ExprValue.UnitV()), code);
                    }
                    if (expr is Expressions.TupleExp)
                    {
                        var e = (Expressions.TupleExp) expr;
                        code = LinkedList.Extend(new Instructions.Tuple(e.Members.Length), code);
                        code = e.Members.Aggregate(code, (s, x) => CompileExpr(x, env, s, false));
                        return code;
                    }
                    if (expr is Expressions.Var)
                    {
                        var e = (Expressions.Var) expr;
                        var frame = 0;
                        for (var ev = env; ev != LinkedList<LinkedList<string>>.Empty; ev = ev.Next)
                        {
                            var index = LinkedList.FirstIndex(x => x == e.Id, ev.Value);
                            if (index != -1)
                            {
                                return LinkedList.Extend(new Instructions.Ld(frame, index), code);
                            }
                            frame++;
                        }
                        throw new Exception.NotBound($"Variable not bound: {e.Id}");
                    }
                    if (expr is Expressions.IfExp)
                    {
                        var e = (Expressions.IfExp) expr;
                        if (isTail)
                        {
                            var tclosure = CompileExpr(e.Then, env,
                                LinkedList.Extend(new Instructions.Rtn(), LinkedList<Instructions>.Empty), true);
                            var fclosure = CompileExpr(e.Else, env,
                                LinkedList.Extend(new Instructions.Rtn(), LinkedList<Instructions>.Empty), true);
                            return CompileExpr(e.Cond, env,
                                LinkedList.Extend(new Instructions.Selr(tclosure, fclosure), code), false);
                        }
                        else
                        {
                            var tclosure = CompileExpr(e.Then, env,
                                LinkedList.Extend(new Instructions.Join(), LinkedList<Instructions>.Empty), false);
                            var fclosure = CompileExpr(e.Else, env,
                                LinkedList.Extend(new Instructions.Join(), LinkedList<Instructions>.Empty), false);
                            return CompileExpr(e.Cond, env,
                                LinkedList.Extend(new Instructions.Sel(tclosure, fclosure), code), false);
                        }

                    }
                    if (expr is Expressions.FunExp)
                    {
                        var e = (Expressions.FunExp) expr;
                        var body = CompileExpr(
                            e.Body,
                            LinkedList.Extend(LinkedList.Extend(e.Arg, LinkedList<string>.Empty), env),
                            LinkedList.Extend(new Instructions.Rtn(), LinkedList<Instructions>.Empty),
                            true
                        );
                        var closure = new Instructions.Ldf.Closure(body);
                        code = LinkedList.Extend(new Instructions.Ldf(closure), code);
                        return code;
                    }
                    if (expr is Expressions.AppExp)
                    {
                        var e = (Expressions.AppExp) expr;
                        code = LinkedList.Extend(
                            (code.Value is Instructions.Rtn)
                                ? (Instructions) new Instructions.Tapp(1)
                                : new Instructions.App(1), code);
                        code = CompileExpr(e.Fun, env, code, true);
                        code = CompileExpr(e.Arg, env, code, false);
                        return code;
                    }
                    if (expr is Expressions.LetExp)
                    {

                        var e = (Expressions.LetExp) expr;

                        var newenv = LinkedList<string>.Empty;
                        foreach (var bind in e.Binds.Reverse())
                        {
                            newenv = LinkedList.Extend(bind.Item1, newenv);
                        }

                        code = LinkedList.Extend(new Instructions.App(e.Binds.Length), code);
                        var body = new Instructions.Ldf.Closure(CompileExpr(e.Body, LinkedList.Extend(newenv, env),
                            LinkedList.Extend(new Instructions.Rtn(), LinkedList<Instructions>.Empty), true));
                        code = LinkedList.Extend(new Instructions.Ldf(body), code);
                        foreach (var bind in e.Binds.Reverse())
                        {
                            code = CompileExpr(bind.Item2, env, code, false);
                        }
                        return code;
                    }
                    if (expr is Expressions.LetRecExp)
                    {
                        var e = (Expressions.LetRecExp) expr;

                        var newenv = LinkedList<string>.Empty;
                        foreach (var bind in e.Binds.Reverse())
                        {
                            newenv = LinkedList.Extend(bind.Item1, newenv);
                        }

                        code = LinkedList.Extend(new Instructions.Rap(e.Binds.Length), code);
                        var body = new Instructions.Ldf.Closure(CompileExpr(e.Body, LinkedList.Extend(newenv, env),
                            LinkedList.Extend(new Instructions.Rtn(), LinkedList<Instructions>.Empty), false));
                        code = LinkedList.Extend(new Instructions.Ldf(body), code);
                        foreach (var bind in e.Binds.Reverse())
                        {
                            code = CompileExpr(bind.Item2, LinkedList.Extend(newenv, env), code, false);
                        }
                        code = LinkedList.Extend(new Instructions.Dum(), code);
                        return code;
                    }
                    if (expr is Expressions.MatchExp)
                    {
                        throw new NotSupportedException();
                    }
                    if (expr is Expressions.HaltExp)
                    {
                        return LinkedList.Extend(new Instructions.Halt(((Expressions.HaltExp) expr).Message), code);
                    }
                    if (expr is Expressions.OptionExp)
                    {
                        var e = (Expressions.OptionExp) expr;
                        if (e == Expressions.OptionExp.None)
                        {
                            code = LinkedList.Extend(new Instructions.Opti(true), code);
                        }
                        else
                        {
                            code = LinkedList.Extend(new Instructions.Opti(false), code);
                            code = CompileExpr(e.Expr, env, code, false);
                        }
                        return code;
                    }
                    throw new NotSupportedException($"cannot compile expr: {expr}");
                }

                /// <summary>
                /// 宣言のコンパイル
                /// </summary>
                /// <param name="decl"></param>
                /// <param name="env"></param>
                /// <returns></returns>
                public static Tuple<LinkedList<Instructions>[], LinkedList<LinkedList<string>>> CompileDecl(
                    Toplevel decl,
                    LinkedList<LinkedList<string>> env)
                {
                    if (decl is Toplevel.Exp)
                    {
                        var expr = ((Toplevel.Exp) decl).Syntax;
                        return Tuple.Create(
                            new[]
                            {
                                CompileExpr(expr, env,
                                    LinkedList.Extend(new Instructions.Stop(), LinkedList<Instructions>.Empty), true)
                            },
                            env
                        );
                    }
                    if (decl is Toplevel.Binding)
                    {
                        var decls = ((Toplevel.Binding) decl).Entries;

                        var codes = new List<LinkedList<Instructions>>();
                        foreach (var d in decls)
                        {
                            if (d is Toplevel.Binding.DeclBase.LetDecl)
                            {
                                var e = d as Toplevel.Binding.DeclBase.LetDecl;

                                var newenv = LinkedList<string>.Empty;
                                foreach (var bind in e.Binds.Reverse())
                                {
                                    newenv = LinkedList.Extend(bind.Item1, newenv);
                                }

                                var code = LinkedList.Extend(new Instructions.Stop(), LinkedList<Instructions>.Empty);
                                code = LinkedList.Extend(new Instructions.Ent(e.Binds.Length), code);
                                foreach (var bind in e.Binds.Reverse())
                                {
                                    code = CompileExpr(bind.Item2, env, code, true);
                                }
                                env = LinkedList.Extend(newenv, env);
                                codes.Add(code);

                                continue;
                            }
                            if (d is Toplevel.Binding.DeclBase.LetRecDecl)
                            {
                                var e = d as Toplevel.Binding.DeclBase.LetRecDecl;

                                var newenv = LinkedList<string>.Empty;
                                foreach (var bind in e.Binds.Reverse())
                                {
                                    newenv = LinkedList.Extend(bind.Item1, newenv);
                                }

                                var code = LinkedList.Extend(new Instructions.Stop(), LinkedList<Instructions>.Empty);
                                code = LinkedList.Extend(new Instructions.Rent(e.Binds.Length), code);
                                foreach (var bind in e.Binds.Reverse())
                                {
                                    code = CompileExpr(bind.Item2, LinkedList.Extend(newenv, env), code, true);
                                }
                                code = LinkedList.Extend(new Instructions.Dum(), code);
                                env = LinkedList.Extend(newenv, env);
                                codes.Add(code);

                                continue;
                            }
                            throw new NotSupportedException($"cannot compile decls: {d}");
                        }
                        return Tuple.Create(codes.ToArray(), env);
                    }
                    if (decl is Toplevel.Empty)
                    {
                        return Tuple.Create(
                            new LinkedList<Instructions>[0],
                            env
                        );
                    }
                    if (decl is Toplevel.ExternalDecl)
                    {
                        var d = decl as Toplevel.ExternalDecl;

                        var newenv = LinkedList<string>.Empty;
                        newenv = LinkedList.Extend(d.Id, newenv);

                        var code = LinkedList.Extend(new Instructions.Stop(), LinkedList<Instructions>.Empty);
                        code = LinkedList.Extend(new Instructions.Ent(1), code);
                        code = LinkedList.Extend(new Instructions.Ldext(d.Symbol), code);
                        env = LinkedList.Extend(newenv, env);

                        return Tuple.Create(new[] {code}, env);
                    }
                    throw new NotSupportedException($"cannot compile declarations: {decl}");
                }

                /// <summary>
                /// コンパイル
                /// </summary>
                /// <param name="expr">式</param>
                /// <returns>仮想マシン命令列</returns>
                public static LinkedList<Instructions> Compile(Expressions expr)
                {
                    return CompileExpr(expr, LinkedList<LinkedList<string>>.Empty,
                        LinkedList.Extend(new Instructions.Stop(), LinkedList<Instructions>.Empty), true);
                }
            }
        }
    }
}