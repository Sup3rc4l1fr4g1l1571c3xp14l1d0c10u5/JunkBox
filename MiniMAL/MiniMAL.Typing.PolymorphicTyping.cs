using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using MiniMAL.Syntax;

namespace MiniMAL
{
    public static partial class Typing
    {
        public static class PolymorphicTyping
        {
            /// <summary>
            /// 型スキーム
            /// </summary>
            public class TypeScheme
            {
                // 自由に置き換えてよい型
                public Set<Type.TyVar> Vars { get; }

                // 型
                public Type Type { get; }

                public TypeScheme(Set<Type.TyVar> vars, Type type)
                {
                    Vars = vars;
                    Type = type;
                }

                public override string ToString()
                {
                    return $"(∀{Vars} . {Type})";
                }
            }

            private static Set<Type.TyVar> freevar_tyenv(Environment<TypeScheme> tyenv)
            {
                return Environment.FoldLeft(
                    (s, x) => Set.Union(freevar_tysc(tyenv.Value.Vars, tyenv.Value.Type), s),
                    tyenv,
                    Set<Type.TyVar>.Empty
                );
            }

            private static TypeScheme tysc_of_ty(Type ty)
            {
                return new TypeScheme(Set<Type.TyVar>.Empty, ty);
            }

            /// <summary>
            /// 型τ と型環境 Γ と型代入 S から，条件「α1,...,αn は τ に自由に出現する型変数で SΓ には自由に出現しない」を満たす変数集合（型スキーム用）
            /// </summary>
            /// <param name="ty">型τ</param>
            /// <param name="tyenv">型環境 Γ </param>
            /// <param name="subst">型代入 S</param>
            /// <returns></returns>
            public static TypeScheme Closure(Type ty, Environment<TypeScheme> tyenv, LinkedList<TypeSubst> subst)
            {
                // ft_ty = τ に自由に出現する型変数
                var ft_ty = freevar_ty(ty);

                // fv_tyenv = SΓ に自由に出現する型変数
                var fv_tyenv = Set.Fold(
                    (acc, v) => Set.Union(freevar_ty(subst_type(subst, v)), acc),
                    freevar_tyenv(tyenv),
                    Set<Type.TyVar>.Empty);

                // ft_ty - fv_tyenvが条件を満たす型変数
                var ids = Set.Diff(ft_ty, fv_tyenv);
                return new TypeScheme(ids, ty);
            }

            //多相的: 定数式, fun 式, それらの値の tuple, それらの値の変更不可能なデータ構造
            //単相的: 変更可能データ構造, let 式、関数適用の結果, etc. . .
            public static bool IsValue(Expressions expr)
            {
                return Expressions.Match(
                    Var: (e) => true,
                    IntLit: (e) => true,
                    StrLit: (e) => true,
                    BoolLit: (e) => true,
                    EmptyListLit: (e) => true,
                    OptionExp: (e) => true,
                    UnitLit: (e) => true,
                    TupleExp: (e) => true,
                    RecordExp: (e) => false,
                    MemberExp: (e) => true,
                    DestructiveUpdateExp: (e) => false,
                    IfExp: (e) => true,
                    LetExp: (e) => false,
                    FunExp: (e) => true,
                    AppExp: (e) => false,
                    LetRecExp: (e) => true,
                    MatchExp: (e) => false,
                    HaltExp: (e) => false,
                    ConstructorExp: (e) => false,
                    VariantExp: (e) => false,
                    Other: (e) => throw new NotSupportedException()
                )(expr);
            }
            /// <summary>
            ///     評価結果
            /// </summary>
            public class Result
            {
                public Result(string id, Environment<TypeScheme> env, Environment<TypeScheme> tyEnv, Type value)
                {
                    Id = id;
                    Env = env;
                    TyEnv = tyEnv;
                    Value = value;
                }

                public string Id { get; }
                public Environment<TypeScheme> Env { get; }
                public Environment<TypeScheme> TyEnv { get; }
                public Type Value { get; }
            }

            /// <summary>
            ///     式の評価
            /// </summary>
            /// <param name="env"></param>
            /// <param name="e"></param>
            /// <returns></returns>
            private static Tuple<LinkedList<TypeSubst>, Type> EvalExpressions(
                Environment<TypeScheme> env,
                Environment<TypeScheme> tyEnv,
                Dictionary<string, Type.TyVar> dic,
                Expressions exp
            )
            {
                return Expressions.Match(
                    Var: (e) =>
                    {
                        var vid = e.Id;
                        try
                        {
                            var ret = Environment.LookUp(vid, env);
                            var vars = ret.Vars;
                            var ty = ret.Type;

                            var subst = Set.Fold(
                                (s, x) => LinkedList.Extend(new TypeSubst(x, Type.TyVar.Fresh()), s),
                                LinkedList<TypeSubst>.Empty,
                                vars
                            );
                            return Tuple.Create(LinkedList<TypeSubst>.Empty, subst_type(subst, ty));
                        }
                        catch (Exception.NotBound)
                        {
                            throw new Exception.NotBound($"Variable not bound: {vid}");
                        }

                    },
                    IntLit: (e) =>
                    {
                        return Tuple.Create(
                            LinkedList<TypeSubst>.Empty,
                            (Type) new Type.TyInt()
                        );
                    },
                    StrLit: (e) =>
                    {
                        return Tuple.Create(
                            LinkedList<TypeSubst>.Empty,
                            (Type) new Type.TyStr()
                        );
                    },
                    BoolLit: (e) =>
                    {
                        return Tuple.Create(
                            LinkedList<TypeSubst>.Empty,
                            (Type) new Type.TyBool()
                        );
                    },
                    EmptyListLit: (e) =>
                    {
                        return Tuple.Create(
                            LinkedList<TypeSubst>.Empty,
                            (Type) new Type.TyList(Type.TyVar.Fresh())
                        );
                    },
                    OptionExp: (e) =>
                    {
                        if (e == Expressions.OptionExp.None)
                        {
                            var domty = Type.TyVar.Fresh();
                            return Tuple.Create(
                                LinkedList<TypeSubst>.Empty,
                                (Type) new Type.TyOption(domty)
                            );
                        }
                        else
                        {
                            var mem = EvalExpressions(env, tyEnv, dic, e.Expr);
                            var ss = eqs_of_subst(mem.Item1);
                            var ty = mem.Item2;
                            var eqs = Unify(ss);

                            return Tuple.Create(
                                eqs,
                                (Type) new Type.TyOption(subst_type(eqs, ty))
                            );
                        }
                    },
                    UnitLit: (e) =>
                    {
                        return Tuple.Create(
                            LinkedList<TypeSubst>.Empty,
                            (Type) new Type.TyUnit()
                        );
                    },
                    TupleExp: (e) =>
                    {
                        var tyMembers = e.Members.Select(x => EvalExpressions(env, tyEnv, dic, x)).ToArray();
                        var ss = tyMembers.Aggregate(LinkedList<TypeEquality>.Empty,
                            (s, x) => LinkedList.Concat(s, eqs_of_subst(x.Item1)));
                        var eqs = Unify(ss);

                        return Tuple.Create(
                            eqs,
                            (Type) new Type.TyTuple(tyMembers.Select(x => subst_type(eqs, x.Item2)).ToArray())
                        );
                    },
                    RecordExp: (e) =>
                    {
                        for (var it = tyEnv; it != Environment<TypeScheme>.Empty; it = it.Next)
                        {
                            var tyRef = it.Value.Type as Type.TyTypeRef;
                            if (tyRef == null)
                            {
                                continue;
                            }
                            var ty = tyRef.Type;
                            var tyRecord = ty as Type.TyRecord;
                            if (tyRecord == null)
                            {
                                continue;
                            }
                            if (tyRecord.Members.Select(x => x.Item2).SequenceEqual(e.Members.Select(x => x.Item1)) ==
                                false)
                            {
                                continue;
                            }

                            var tyMembers = e.Members.Select(x => EvalExpressions(env, tyEnv, dic, x.Item2)).ToArray();

                            var eqs = LinkedList.Concat(
                                LinkedList.Create(tyMembers.Select(x => x.Item2)
                                    .Zip(tyRecord.Members.Select(x => x.Item3), (x, y) => new TypeEquality(x, y))
                                    .ToArray()),
                                LinkedList.Concat(tyMembers.Select(x => eqs_of_subst(x.Item1)).ToArray())
                            );

                            var eqs2 = Unify(eqs);

                            return Tuple.Create(eqs2, subst_type(eqs2, tyRef));

                        }
                        throw new Exception.NotBound($"Record not bound: {exp}");
                    },
                    MemberExp: (e) =>
                    {
                        for (var it = tyEnv; it != Environment<TypeScheme>.Empty; it = it.Next)
                        {
                            var tyRef = it.Value.Type as Type.TyTypeRef;
                            if (tyRef == null)
                            {
                                continue;
                            }
                            var tyRecord = tyRef.Type as Type.TyRecord;
                            if (tyRecord == null)
                            {
                                continue;
                            }
                            var member = tyRecord.Members.FirstOrDefault(x => x.Item2 == e.Member);
                            if (member == null)
                            {
                                continue;
                            }

                            var ty = EvalExpressions(env, tyEnv, dic, e.Expression);

                            var eqs =
                                LinkedList.Extend(
                                        new TypeEquality(ty.Item2, tyRecord),
                                        eqs_of_subst(ty.Item1)
                                    );
                            var eqs2 = Unify(eqs);

                            return Tuple.Create(eqs2, subst_type(eqs2, member.Item3));

                        }
                        throw new Exception.NotBound($"Record not bound: {exp}");
                    },
                    DestructiveUpdateExp: (e) =>
                    {
                        for (var it = tyEnv; it != Environment<TypeScheme>.Empty; it = it.Next)
                        {
                            var tyRef = it.Value.Type as Type.TyTypeRef;
                            if (tyRef == null)
                            {
                                continue;
                            }
                            var tyRecord = tyRef.Type as Type.TyRecord;
                            if (tyRecord == null)
                            {
                                continue;
                            }
                            var member = tyRecord.Members.FirstOrDefault(x => x.Item2 == e.Member);
                            if (member == null)
                            {
                                continue;
                            }
                            if (member.Item1 == false)
                            {
                                throw new Exception.NotBound($"Record member is immutable: {exp}.{member}");
                            }

                            var ty1 = EvalExpressions(env, tyEnv, dic, e.Expression);
                            var ty2 = EvalExpressions(env, tyEnv, dic, e.Value);

                            var eqs =
                                LinkedList.Concat(
                                    LinkedList.Create(
                                        new TypeEquality(ty1.Item2, tyRecord),
                                        new TypeEquality(ty2.Item2, member.Item3)
                                    ),
                                    eqs_of_subst(ty1.Item1),
                                    eqs_of_subst(ty2.Item1)
                                );
                            var eqs2 = Unify(eqs);

                            return Tuple.Create(eqs2, (Type)new Type.TyUnit());

                        }
                        throw new Exception.NotBound($"Record not bound: {exp}");
                    },
                    IfExp: (e) =>
                    {
                        var cond = EvalExpressions(env, tyEnv, dic, e.Cond);
                        var then = EvalExpressions(env, tyEnv, dic, e.Then);
                        var @else = EvalExpressions(env, tyEnv, dic, e.Else);
                        var s = LinkedList.Concat(
                            LinkedList.Create(new TypeEquality(cond.Item2, new Type.TyBool())),
                            LinkedList.Create(new TypeEquality(then.Item2, @else.Item2)),
                            eqs_of_subst(cond.Item1),
                            eqs_of_subst(then.Item1),
                            eqs_of_subst(@else.Item1)
                        );
                        var eqs = Unify(s);
                        return Tuple.Create(
                            eqs,
                            subst_type(eqs, then.Item2)
                        );
                    },
                    LetExp: (e) =>
                    {
                        var bindret = EvalLetBind(env, tyEnv, dic, e.Binds, null);
                        var substs = bindret.Item1;
                        var newenv = bindret.Item2;

                        {
                            var v = EvalExpressions(newenv, tyEnv, dic, e.Body);
                            var s = v.Item1;
                            var ty = v.Item2;
                            var eqs = eqs_of_subst(LinkedList.Concat(substs, s));

                            var s3 = Unify(eqs);
                            return Tuple.Create(
                                s3,
                                subst_type(s3, ty)
                            );
                        }
                    },
                    FunExp: (e) =>
                    {
                        // 関数定義から読み取った型
                        var fundecty = EvalTypeExpressions(
                            new TypeExpressions.FuncType(e.ArgTy, e.BodyTy),
                            tyEnv,
                            dic
                        ) as Type.TyFunc;

                        // 関数の型
                        var argty = Type.TyVar.Fresh();
                        var newenv = Environment.Extend(e.Arg, tysc_of_ty(argty), env);
                        var funv = EvalExpressions(newenv, tyEnv, dic, e.Body);
                        var st = funv.Item1;
                        var ty = funv.Item2;
                        var funty = (Type) new Type.TyFunc(argty, ty);


                        var eq = LinkedList.Concat(
                            eqs_of_subst(st),
                            LinkedList.Create(
                                new TypeEquality(funty, fundecty)
                            )
                        );

                        var eqs = Unify(eq);


                        return Tuple.Create(
                            eqs,
                            subst_type(eqs, funty)
                        );
                    },
                    AppExp: (e) =>
                    {
                        var v1 = EvalExpressions(env, tyEnv, dic, e.Fun);
                        var s1 = v1.Item1;
                        var ty1 = v1.Item2;

                        var v2 = EvalExpressions(env, tyEnv, dic, e.Arg);
                        var s2 = v2.Item1;
                        var ty2 = v2.Item2;

                        var domty = Type.TyVar.Fresh();

                        var eqs = LinkedList.Concat(
                            eqs_of_subst(s1),
                            eqs_of_subst(s2),
                            LinkedList.Create(new TypeEquality(ty1, new Type.TyFunc(ty2, domty)))
                        );
                        var s3 = Unify(eqs);
                        return Tuple.Create(
                            s3,
                            subst_type(s3, domty)
                        );
                    },
                    LetRecExp: (e) =>
                    {
                        var bindret = EvalLetRecBind(env, tyEnv, dic, e.Binds, null);
                        var substs = bindret.Item1;
                        var newenv = bindret.Item2;
                        var eqs = bindret.Item3;


                        {
                            var v = EvalExpressions(newenv, tyEnv, dic, e.Body);
                            var s = v.Item1;
                            var ty = v.Item2;
                            eqs = LinkedList.Concat(eqs_of_subst(s), eqs);

                            var s3 = Unify(eqs);
                            return Tuple.Create(
                                s3,
                                subst_type(s3, ty)
                            );
                        }
                    },
                    MatchExp: (e) =>
                    {
                        var domty = Type.TyVar.Fresh();

                        // 式の型推論
                        var v1 = EvalExpressions(env, tyEnv, dic, e.Exp);
                        var st = v1.Item1;
                        var ty = v1.Item2;

                        var eqs = eqs_of_subst(st);
                        foreach (var pattern in e.Patterns)
                        {
                            var pt = pattern.Item1;
                            var ex = pattern.Item2;

                            // パターン式から型等式と束縛を導出
                            var v2 = EvalPatternExpressions(pt, tyEnv, ty);
                            var eqs1 = v2.Item1;
                            var binds1 = v2.Item2;

                            // 束縛を環境に結合
                            var env1 = binds1.Aggregate(env,
                                (s, x) => Environment.Extend(x.Key, tysc_of_ty(x.Value), s));

                            // 本体から型等式と戻り値型を導出
                            var v3 = EvalExpressions(env1, tyEnv, dic, ex);
                            var se = v3.Item1;
                            var tye = v3.Item2;

                            eqs = LinkedList.Concat(
                                LinkedList.Create(new TypeEquality(tye, domty)),
                                eqs1,
                                eqs_of_subst(se),
                                eqs
                            );
                        }

                        var s3 = Unify(eqs);

                        return Tuple.Create(
                            s3,
                            subst_type(s3, domty)
                        );
                    },
                    HaltExp: (e) =>
                    {
                        return Tuple.Create(
                            LinkedList<TypeSubst>.Empty,
                            (Type) Type.TyVar.Fresh()
                        );
                    },
                    ConstructorExp: (e) =>
                    {
                        for (var it = tyEnv; it != Environment<TypeScheme>.Empty; it = it.Next)
                        {
                            var tyRef = it.Value.Type as Type.TyTypeRef;
                            if (tyRef == null)
                            {
                                continue;
                            }
                            var ty = tyRef.Type;
                            var tyVariant = ty as Type.TyVariant;
                            if (tyVariant == null)
                            {
                                continue;
                            }
                            var constructor = tyVariant.Members.FirstOrDefault(x => x.Item1 == e.ConstructorName);
                            if (constructor == null)
                            {
                                continue;
                            }

                            var tyArg = EvalExpressions(env, tyEnv, dic, e.Arg);

                            var eqs = LinkedList.Extend(
                                new TypeEquality(constructor.Item2, tyArg.Item2),
                                eqs_of_subst(tyArg.Item1)
                            );

                            var eqs2 = Unify(eqs);

                            return Tuple.Create(eqs2, subst_type(eqs2, tyRef));

                        }

                        throw new Exception.NotBound($"Record not bound: {e}");
                    },
                    VariantExp: (e) => { throw new Exception.RuntimeErrorException("ここには到達しないはず。"); },
                    Other: (e) => { throw new NotSupportedException($"expression {e} cannot eval."); }
                )(exp);
            }

            private static Tuple<LinkedList<TypeSubst>, Environment<TypeScheme>, LinkedList<TypeEquality>>
                EvalLetRecBind(
                    Environment<TypeScheme> env,
                    Environment<TypeScheme> tyEnv,
                    Dictionary<string, Type.TyVar> dic,
                    Tuple<string, Expressions>[] b,
                    Action<string, Environment<TypeScheme>, Type> hook)
            {
                var dummyenv = env;

                var binds = b.Select(x => Tuple.Create(x.Item1, x.Item2, Type.TyVar.Fresh())).ToArray();
                foreach (var bind in binds)
                {
                    dummyenv = Environment.Extend(bind.Item1, tysc_of_ty(bind.Item3), dummyenv);
                }

                var substs = LinkedList<TypeSubst>.Empty;
                var eqs = LinkedList<TypeEquality>.Empty;

                foreach (var bind in binds)
                {
                    var v = EvalExpressions(dummyenv, tyEnv, dic, bind.Item2);
                    var s = v.Item1;
                    var ty = v.Item2;
                    eqs = MiniMAL.LinkedList.Extend(new TypeEquality(bind.Item3, ty), eqs);
                    substs = MiniMAL.LinkedList.Concat(s, substs);
                }
                var eqsBinds = MiniMAL.LinkedList.Concat(eqs, eqs_of_subst(substs));
                var substBinds = Unify(eqsBinds);

                var newenv = env;
                foreach (var bind in binds)
                {
                    var ty = subst_type(substBinds, bind.Item3);
                    //var tysc = (IsValue(bind.Item2)) ? Closure(ty, env, substBinds) : tysc_of_ty(ty);
                    var tysc = Closure(ty, env, substBinds);
                    newenv = Environment.Extend(bind.Item1, tysc, newenv);
                    hook?.Invoke(bind.Item1, newenv, tysc.Type);
                }
                return Tuple.Create(substs, newenv, eqs);
            }

            private static Tuple<LinkedList<TypeSubst>, Environment<TypeScheme>> EvalLetBind(
                Environment<TypeScheme> env,
                Environment<TypeScheme> tyEnv,
                Dictionary<string, Type.TyVar> dic,
                Tuple<string, Expressions>[] binds,
                Action<string, Environment<TypeScheme>, Type> hook)
            {
                var newenv = env;
                var substs = LinkedList<TypeSubst>.Empty;
                foreach (var bind in binds)
                {
                    var v = EvalExpressions(env, tyEnv, dic, bind.Item2);
                    var s = v.Item1;
                    var ty = v.Item2;
                    substs = LinkedList.Concat(s, substs);
                    var ctys = Closure(ty, env, s);
                    var ctys2 = IsValue(bind.Item2) ? ctys : tysc_of_ty(ctys.Type);
                    var weakpoly = IsValue(bind.Item2) ? Set<Type.TyVar>.Empty : ctys.Vars;

                    var newctys = new TypeScheme(dic.Aggregate(ctys.Vars, (ss, x) => Set.Remove(x.Value, ss)),
                        ctys.Type);
                    newenv = MiniMAL.Environment.Extend(bind.Item1, newctys, newenv);
                    hook?.Invoke(bind.Item1, newenv, ty);
                }
                return Tuple.Create(substs, newenv);
            }


            private static Result eval_declEntry(
                Environment<TypeScheme> env, 
                Environment<TypeScheme> tyEnv,
                Dictionary<string, Type.TyVar> dic, 
                Toplevel.Binding.DeclBase p)
            {
                return Toplevel.Binding.DeclBase.Match(
                    LetDecl: (decl) =>
                    {
                        var ret = new Result("", env, tyEnv, null);
                        var bindret = EvalLetBind(env, tyEnv, dic, decl.Binds,
                            (id, ev, ty) => ret = new Result(id, ev, tyEnv, ty));
                        var substs = bindret.Item1;
                        var newenv = bindret.Item2;

                        {
                            var s3 = Unify(eqs_of_subst(substs));
                            return new Result(ret.Id, newenv, tyEnv, subst_type(s3, ret.Value));
                        }
                    },
                    LetRecDecl: (decl) =>
                    {
                        var ret = new Result("", env, tyEnv, null);
                        var bindret = EvalLetRecBind(env, tyEnv, dic, decl.Binds,
                            (id, ev, ty) => ret = new Result(id, ev, tyEnv, ty));
                        var substs = bindret.Item1;
                        var newenv = bindret.Item2;
                        var eqs = bindret.Item3;

                        {
                            var s3 = Unify(eqs_of_subst(substs));
                            return new Result(ret.Id, newenv, tyEnv, subst_type(s3, ret.Value));
                        }
                    },
                    Other: (decl) => throw new NotSupportedException($"{p.GetType().FullName} cannot eval.")
                )(p);
        }

            public static Result eval_decl(
                Environment<TypeScheme> env,
                Environment<TypeScheme> tyEnv,
                Toplevel p)
            {
                return Toplevel.Match(
                    Empty: (e) =>
                    {
                        return new Result("", env, tyEnv, null);
                    },
                    Exp: (e) =>
                    {
                        var v = EvalExpressions(env, tyEnv, new Dictionary<string, Type.TyVar>(), e.Syntax);
                        return new Result("-", env, tyEnv, v.Item2);
                    },
                    Binding: (e) =>
                    {
                        var newenv = env;
                        var dic = new Dictionary<string, Type.TyVar>();
                        var ret = new Result("", env, tyEnv, null);
                        foreach (var d in e.Entries)
                        {
                            ret = eval_declEntry(newenv, tyEnv, dic, d);
                            newenv = ret.Env;
                        }
                        return ret;
                    },
                    TypeDef: (e) =>
                    {
                        var ret = EvalTypeDef(env, tyEnv, e);

                        return new Result(e.Id, ret.Item1, ret.Item2, ret.Item3);
                    },
                    ExternalDecl: (e) =>
                    {
                        var dic = new Dictionary<string, Type.TyVar>();
                        var ty = EvalTypeExpressions(e.Type, tyEnv, dic);
                        var tysc = new TypeScheme(dic.Values.Aggregate(Set<Type.TyVar>.Empty, (s, x) => Set.Insert(x, s)),
                            ty);
                        var newenv = Environment.Extend(e.Id, tysc, env);
                        return new Result(e.Id, newenv, tyEnv, ty);
                    },
                    Other: (e) => throw new NotSupportedException($"{p.GetType().FullName} cannot eval.")

                )(p);
            }

            private static Type EvalTypeExpressions(
                TypeExpressions expressions,
                Environment<TypeScheme> tyEnv,
                Dictionary<string, Type.TyVar> vars
            )
            {
                return TypeExpressions.Match(
                    TypeVar: (e) =>
                    {
                        if (vars.ContainsKey(e.Id))
                        {
                            return vars[e.Id];
                        }
                        else if (e.Id.StartsWith("'"))
                        {
                            vars[e.Id] = Type.TyVar.Fresh();
                            return vars[e.Id];
                        }
                        else
                        {
                            return Type.TyVar.Fresh();
                        }
                    },
                    IntType: (e) =>
                    {
                        return (new Type.TyInt());
                    },
                    BoolType: (e) =>
                    {
                        return (new Type.TyBool());
                    },
                    StrType: (e) =>
                    {
                        return (new Type.TyStr());
                    },
                    UnitType: (e) =>
                    {
                        return (new Type.TyUnit());
                    },
                    ListType: (e) =>
                    {
                        var tysc = EvalTypeExpressions(e.Type, tyEnv, vars);
                        return new Type.TyList(tysc);
                    },
                    OptionType: (e) =>
                    {
                        var tysc = EvalTypeExpressions(e.Type, tyEnv, vars);
                        return new Type.TyOption(tysc);
                    },
                    TupleType: (e) =>
                    {
                        var tyscs = e.Members.Select(x => EvalTypeExpressions(x, tyEnv, vars)).ToArray();
                        return new Type.TyTuple(tyscs);
                    },
                    RecordType: (e) => {
                        throw new Exception.RuntimeErrorException("ここにはこないはず。");
                    },
                    VariantType: (e) => {
                        throw new Exception.RuntimeErrorException("ここにはこないはず。");
                    },
                    FuncType: (e) =>
                    {
                        var tysc1 = EvalTypeExpressions(e.DomainType, tyEnv, vars);
                        var tysc2 = EvalTypeExpressions(e.RangeType, tyEnv, vars);
                        return new Type.TyFunc(tysc1, tysc2);
                    },
                    TypeName: (e) =>
                    {
                        throw new NotImplementedException();
                    },
                    TypeConstruct: (e) =>
                    {
                        if (e.Base.Name == "list")
                        {
                            if (e.Params.Length != 1)
                            {
                                throw new Exception.InvalidArgumentNumException();
                            }
                            var tysc = EvalTypeExpressions(e.Params[0], tyEnv, vars);
                            return new Type.TyList(tysc);
                        }
                        if (e.Base.Name == "option")
                        {
                            if (e.Params.Length != 1)
                            {
                                throw new Exception.InvalidArgumentNumException();
                            }
                            var tysc = EvalTypeExpressions(e.Params[0], tyEnv, vars);
                            return new Type.TyOption(tysc);
                        }
                        {
                            var tyscBase = Environment.LookUp(e.Base.Name, tyEnv);
                            if (Set.Count(tyscBase.Vars) != e.Params.Length)
                            {
                                throw new Exception.TypingException("type param count missmatch.");
                            }
                            var tyArgs = e.Params.Select(x => EvalTypeExpressions(x, tyEnv, vars)).ToArray();
                            var ss = Set.Fold((s, x) =>
                                {
                                    s.Add(x);
                                    return s;
                                }, new List<Type.TyVar>(), tyscBase.Vars)
                                .Reverse<Type.TyVar>()
                                .Zip(tyArgs, (x, y) => new TypeSubst(x, y))
                                .Aggregate(LinkedList<TypeSubst>.Empty, (s, x) => LinkedList.Extend(x, s));
                            return subst_type(ss, tyscBase.Type);
                        }

                        throw new NotImplementedException();
                    },
                    Other: (e) => throw new NotSupportedException()
                )(expressions);
            }


            private static Tuple<Environment<TypeScheme>, Environment<TypeScheme>, Type> EvalTypeDef(
                Environment<TypeScheme> env,
                Environment<TypeScheme> tyEnv,
                Toplevel.TypeDef typedef
            )
            {
                var vars = typedef.Vars.Select(x => Tuple.Create(x, Type.TyVar.Fresh())).ToArray();
                var dic = vars.ToDictionary(x => x.Item1, x => x.Item2);
                var expressions = typedef.Type;

                if (expressions is TypeExpressions.RecordType)
                {
                    var e = (TypeExpressions.RecordType)expressions;
                    var ty = new Type.TyRecord(typedef.Id);
                    var tyRef = new Type.TyTypeRef(typedef.Id, ty, vars.Select(x => (Type)x.Item2).ToArray());
                    var tysc = new TypeScheme(vars.Aggregate(Set<Type.TyVar>.Empty, (s, x) => Set.Insert(x.Item2, s)), tyRef);
                    tyEnv = Environment.Extend(typedef.Id, tysc, tyEnv);
                    var mems = e.Members.Select(x => Tuple.Create(x.Item1, x.Item2, EvalTypeExpressions(x.Item3, tyEnv, dic)))
                        .ToArray();
                    if (typedef.Vars.OrderBy(x => x).SequenceEqual(dic.Keys.OrderBy(x => x)) == false)
                    {
                        throw new Exception.NotBound("type vars missmatch.");
                    }

                    ty.Members = mems;

                    return Tuple.Create(env, tyEnv, (Type)tyRef);
                }
                if (expressions is TypeExpressions.VariantType)
                {
                    var e = (TypeExpressions.VariantType)expressions;
                    var ty = new Type.TyVariant(typedef.Id);
                    var tyRef = new Type.TyTypeRef(typedef.Id, ty, vars.Select(x => (Type)x.Item2).ToArray());
                    var tysc = new TypeScheme(vars.Aggregate(Set<Type.TyVar>.Empty, (s, x) => Set.Insert(x.Item2, s)), tyRef);
                    tyEnv = Environment.Extend(typedef.Id, tysc, tyEnv);
                    var mems = e.Members.Select(x => Tuple.Create(x.Item1, EvalTypeExpressions(x.Item2, tyEnv, dic)))
                        .ToArray();
                    if (typedef.Vars.OrderBy(x => x).SequenceEqual(dic.Keys.OrderBy(x => x)) == false)
                    {
                        throw new Exception.NotBound("type vars missmatch.");
                    }

                    ty.Members = mems;

                    foreach (var mem in mems)
                    {
                        var tyscConstructor = new TypeScheme(tysc.Vars, new Type.TyFunc(mem.Item2, tyRef));
                        env = Environment.Extend(mem.Item1, tyscConstructor, env);
                    }
                    return Tuple.Create(env, tyEnv, (Type)tyRef);
                }

                {
                    var ret = EvalTypeExpressions(expressions, tyEnv, dic);
                    var tysc = new TypeScheme(vars.Aggregate(Set<Type.TyVar>.Empty, (s, x) => Set.Insert(x.Item2, s)), ret);
                    tyEnv = Environment.Extend(typedef.Id, tysc, tyEnv);
                    return Tuple.Create(env, tyEnv, ret);
                }

            }

            /// <summary>
            ///     パターンマッチの評価
            /// </summary>
            /// <param name="pattern"></param>
            /// <param name="value"></param>
            /// <returns></returns>
            private static Tuple<LinkedList<TypeEquality>, Dictionary<string, Type>> EvalPatternExpressions(
                PatternExpressions pattern,
                Environment<TypeScheme> tyEnv,
                Type value)
            {
                return PatternExpressions.Match(
                    WildP: (p) =>
                    {
                        return Tuple.Create(
                            LinkedList<TypeEquality>.Empty,
                            new Dictionary<string, Type>()
                        );
                    },
                    IntP: (p) =>
                    {
                        return Tuple.Create(
                            LinkedList.Create(new TypeEquality(value, new Type.TyInt())),
                            new Dictionary<string, Type>()
                        );
                    },
                    StrP: (p) =>
                    {
                        return Tuple.Create(
                            LinkedList.Create(new TypeEquality(value, new Type.TyStr())),
                            new Dictionary<string, Type>()
                        );
                    },
                    BoolP: (p) =>
                    {
                        return Tuple.Create(
                            LinkedList.Create(new TypeEquality(value, new Type.TyBool())),
                            new Dictionary<string, Type>()
                        );
                    },
                    OptionP: (p) =>
                    {
                        if (p == PatternExpressions.OptionP.None)
                        {
                            var tyitem = Type.TyVar.Fresh();
                            return Tuple.Create(
                                LinkedList.Create(
                                    new TypeEquality(value, new Type.TyOption(tyitem))
                                ),
                                new Dictionary<string, Type>()
                            );
                        }
                        else
                        {
                            var tyitem = Type.TyVar.Fresh();
                            var ret1 = EvalPatternExpressions(p.Value, tyEnv, tyitem);
                            return Tuple.Create(
                                LinkedList.Concat(
                                    LinkedList.Create(new TypeEquality(value, new Type.TyOption(tyitem))),
                                    ret1.Item1
                                ),
                                ret1.Item2
                            );
                        }
                    },
                    UnitP: (p) =>
                    {
                        return Tuple.Create(
                            LinkedList.Create(new TypeEquality(value, new Type.TyUnit())),
                            new Dictionary<string, Type>()
                        );
                    },
                    VarP: (p) =>
                    {
                        return Tuple.Create(
                            LinkedList<TypeEquality>.Empty,
                            new Dictionary<string, Type> { { p.Id, value } }
                        );
                    },
                    ConsP: (p) =>
                    {
                        if (p == PatternExpressions.ConsP.Empty)
                        {
                            return Tuple.Create(
                                LinkedList.Create(new TypeEquality(value, new Type.TyList(Type.TyVar.Fresh()))),
                                new Dictionary<string, Type>()
                            );
                        }
                        else
                        {
                            var tyitem = Type.TyVar.Fresh();
                            var tyList = new Type.TyList(tyitem);
                            var ret1 = EvalPatternExpressions(p.Value, tyEnv, tyitem);
                            var ret2 = EvalPatternExpressions(p.Next, tyEnv, tyList);
                            return Tuple.Create(
                                LinkedList.Concat(
                                    LinkedList.Create(new TypeEquality(tyList, value)),
                                    ret1.Item1,
                                    ret2.Item1
                                ),
                                ret2.Item2.Aggregate(new Dictionary<string, Type>(ret1.Item2), (s, x) => { s[x.Key] = x.Value; return s; })
                            );
                        }
                    },
                    TupleP: (p) =>
                    {
                        var members = p.Members.Select(x => Tuple.Create(x, Type.TyVar.Fresh())).ToArray();
                        var tupleType = new Type.TyTuple(members.Select(x => (Type)x.Item2).ToArray());

                        var eqs = LinkedList.Create(new TypeEquality(value, tupleType));
                        var binds = new Dictionary<string, Type>();
                        foreach (var ptv in members)
                        {
                            var patexpr = ptv.Item1;
                            var pattyvar = ptv.Item2;

                            var ret = EvalPatternExpressions(patexpr, tyEnv, pattyvar);
                            var pateqs = ret.Item1;
                            var patbind = ret.Item2;

                            eqs = LinkedList.Concat(pateqs, eqs);
                            binds = patbind.Aggregate(binds, (s, x) =>
                            {
                                s[x.Key] = x.Value;
                                return s;
                            });
                        }
                        return Tuple.Create(
                            eqs,
                            binds
                        );
                    },
                    RecordP: (p) =>
                    {
                        for (var it = tyEnv; it != Environment<PolymorphicTyping.TypeScheme>.Empty; it = it.Next)
                        {
                            var tyRef = it.Value.Type as Type.TyTypeRef;
                            if (tyRef == null)
                            {
                                continue;
                            }
                            var ty = tyRef.Type;

                            var tyRecord = ty as Type.TyRecord;
                            if (tyRecord == null)
                            {
                                continue;
                            }
                            if (tyRecord.Members.Select(x => x.Item2).SequenceEqual(p.Members.Select(x => x.Item1)) == false)
                            {
                                continue;
                            }

                            var eqs = LinkedList.Create(new TypeEquality(value, tyRef));
                            var binds = new Dictionary<string, Type>();
                            foreach (var pq in tyRecord.Members.Select(x => x.Item3).Zip(p.Members.Select(x => x.Item2), Tuple.Create))
                            {
                                var ret = EvalPatternExpressions(pq.Item2, tyEnv, pq.Item1);
                                var pateqs = ret.Item1;
                                var patbind = ret.Item2;

                                eqs = LinkedList.Concat(pateqs, eqs);
                                binds = patbind.Aggregate(binds, (s, x) =>
                                {
                                    s[x.Key] = x.Value;
                                    return s;
                                });
                            }
                            return Tuple.Create(
                                eqs,
                                binds
                            );
                        }


                        throw new Exception.NotBound($"Record not bound: {p}");

                    },
                    VariantP: (p) =>
                    {
                        for (var it = tyEnv; it != Environment<PolymorphicTyping.TypeScheme>.Empty; it = it.Next)
                        {
                            var tyRef = it.Value.Type as Type.TyTypeRef;
                            if (tyRef == null)
                            {
                                continue;
                            }
                            var ty = tyRef.Type;

                            var tyVari = ty as Type.TyVariant;
                            if (tyVari == null)
                            {
                                continue;
                            }
                            var constructor = tyVari.Members.FirstOrDefault(x => x.Item1 == p.ConstructorName);
                            if (constructor == null)
                            {
                                continue;
                            }
                            var tagId = Array.IndexOf(tyVari.Members, constructor);
                            p.ConstructorId = tagId;

                            var eqs = LinkedList.Create(new TypeEquality(value, tyRef));
                            var binds = new Dictionary<string, Type>();

                            var ret = EvalPatternExpressions(p.Body, tyEnv, constructor.Item2);
                            var pateqs = ret.Item1;
                            var patbind = ret.Item2;

                            eqs = LinkedList.Concat(pateqs, eqs);
                            binds = patbind.Aggregate(binds, (s, x) =>
                            {
                                s[x.Key] = x.Value;
                                return s;
                            });

                            return Tuple.Create(
                                eqs,
                                binds
                            );
                        }

                        throw new Exception.NotBound($"Record not bound: {p}");

                    },
                    Other: (p) => { throw new System.NotSupportedException(); }

                )(pattern);
            }

        }
    }
}
