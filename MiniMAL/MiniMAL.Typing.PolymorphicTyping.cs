using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using MiniMAL.Syntax;

namespace MiniMAL {
    public static partial class Typing {
        public static class PolymorphicTyping {
            /// <summary>
            /// 型スキーム
            /// </summary>
            public class TypeScheme {
                // 自由に置き換えてよい型
                public Set<Type.TyVar> Vars { get; }

                // 型
                public Type Type { get; }

                public TypeScheme(Set<Type.TyVar> vars, Type type) {
                    Vars = vars;
                    Type = type;
                }

                public override string ToString() {
                    return $"(∀{Vars} . {Type})";
                }
            }

            public static Set<Type.TyVar> freevar_tyenv(Environment<TypeScheme> tyenv) {
                return Environment.FoldLeft(
                    (s, x) => Set.Union(freevar_tysc(tyenv.Value.Vars, tyenv.Value.Type), s),
                    tyenv,
                    Set<Type.TyVar>.Empty
                );
            }

            public static TypeScheme tysc_of_ty(Type ty) {
                return new TypeScheme(Set<Type.TyVar>.Empty, ty);
            }

            /// <summary>
            /// 型τ と型環境 Γ と型代入 S から，条件「α1,...,αn は τ に自由に出現する型変数で SΓ には自由に出現しない」を満たす変数集合（型スキーム用）
            /// </summary>
            /// <param name="ty">型τ</param>
            /// <param name="tyenv">型環境 Γ </param>
            /// <param name="subst">型代入 S</param>
            /// <returns></returns>
            public static TypeScheme Closure(Type ty, Environment<TypeScheme> tyenv, LinkedList<TypeSubst> subst) {
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

            /// <summary>
            ///     評価結果
            /// </summary>
            public class Result {
                public Result(string id, Environment<TypeScheme> env, Type value) {
                    Id = id;
                    Env = env;
                    Value = value;
                }

                public string Id { get; }
                public Environment<TypeScheme> Env { get; }
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
                Expressions e) {
                if (e is Expressions.Var) {
                    var vid = ((Expressions.Var) e).Id;
                    try {
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
                    catch (Exception.NotBound) {
                        throw new Exception.NotBound($"Variable not bound: {vid}");
                    }
                }
                if (e is Expressions.IntLit) {
                    return Tuple.Create(
                        LinkedList<TypeSubst>.Empty,
                        (Type) new Type.TyInt()
                    );
                }
                if (e is Expressions.StrLit) {
                    return Tuple.Create(
                        LinkedList<TypeSubst>.Empty,
                        (Type) new Type.TyStr()
                    );
                }
                if (e is Expressions.BoolLit) {
                    return Tuple.Create(
                        LinkedList<TypeSubst>.Empty,
                        (Type) new Type.TyBool()
                    );
                }
                if (e is Expressions.EmptyListLit) {
                    return Tuple.Create(
                        LinkedList<TypeSubst>.Empty,
                        (Type) new Type.TyList(Type.TyVar.Fresh())
                    );
                }
                if (e is Expressions.UnitLit) {
                    return Tuple.Create(
                        LinkedList<TypeSubst>.Empty,
                        (Type) new Type.TyUnit()
                    );
                }
                if (e is Expressions.IfExp) {
                    var cond = EvalExpressions(env, ((Expressions.IfExp) e).Cond);
                    var then = EvalExpressions(env, ((Expressions.IfExp) e).Then);
                    var @else = EvalExpressions(env, ((Expressions.IfExp) e).Else);
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
                }
                if (e is Expressions.LetExp) {
                    var exp = (Expressions.LetExp) e;

                    var newenv = env;
                    var substs = LinkedList<TypeSubst>.Empty;
                    foreach (var bind in exp.Binds) {
                        var v = EvalExpressions(env, bind.Item2);
                        var s = v.Item1;
                        var ty = v.Item2;
                        substs = LinkedList.Concat(s, substs);
                        newenv = Environment.Extend(bind.Item1, Closure(ty, env, s), newenv);
                    }

                    {
                        var v = EvalExpressions(newenv, exp.Body);
                        var s = v.Item1;
                        var ty = v.Item2;
                        var eqs = eqs_of_subst(LinkedList.Concat(substs, s));

                        var s3 = Unify(eqs);
                        return Tuple.Create(
                            s3,
                            subst_type(s3, ty)
                        );
                    }
                }
                if (e is Expressions.LetRecExp) {
                    var exp = (Expressions.LetRecExp) e;

                    var dummyenv = env;

                    var binds = exp.Binds.Select(x => Tuple.Create(x.Item1, x.Item2, Type.TyVar.Fresh())).ToArray();
                    foreach (var bind in binds) {
                        dummyenv = Environment.Extend(bind.Item1, tysc_of_ty(bind.Item3), dummyenv);
                    }

                    var substs = LinkedList<TypeSubst>.Empty;
                    var eqs = LinkedList<TypeEquality>.Empty;

                    foreach (var bind in binds) {
                        var v = EvalExpressions(dummyenv, bind.Item2);
                        var s = v.Item1;
                        var ty = v.Item2;
                        eqs = LinkedList.Extend(new TypeEquality(bind.Item3, ty), eqs);
                        substs = LinkedList.Concat(s, substs);
                    }
                    var eqsBinds = LinkedList.Concat(eqs, eqs_of_subst(substs));
                    var substBinds = Unify(eqsBinds);

                    var newenv = env;
                    foreach (var bind in binds) {
                        var tysc = Closure(subst_type(substBinds, bind.Item3), env, substBinds);
                        newenv = Environment.Extend(bind.Item1, tysc, newenv);
                    }


                    {
                        var v = EvalExpressions(newenv, exp.Body);
                        var s = v.Item1;
                        var ty = v.Item2;
                        eqs = LinkedList.Concat(eqs_of_subst(s), eqs);

                        var s3 = Unify(eqs);
                        return Tuple.Create(
                            s3,
                            subst_type(s3, ty)
                        );
                    }
                }
                if (e is Expressions.FunExp) {
                    var exp = (Expressions.FunExp) e;
                    var domty = Type.TyVar.Fresh();
                    var newenv = Environment.Extend(exp.Arg, tysc_of_ty(domty), env);
                    var v = EvalExpressions(newenv, exp.Body);
                    var s = v.Item1;
                    var ty = v.Item2;
                    return Tuple.Create(
                        s,
                        (Type) new Type.TyFunc(subst_type(s, domty), ty)
                    );
                }
                if (e is Expressions.AppExp) {
                    var exp = (Expressions.AppExp) e;

                    var v1 = EvalExpressions(env, exp.Fun);
                    var s1 = v1.Item1;
                    var ty1 = v1.Item2;

                    var v2 = EvalExpressions(env, exp.Arg);
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
                }
                if (e is Expressions.MatchExp) {
                    var exp = (Expressions.MatchExp) e;

                    var domty = Type.TyVar.Fresh();

                    // 式の型推論
                    var v1 = EvalExpressions(env, exp.Exp);
                    var st = v1.Item1;
                    var ty = v1.Item2;

                    var eqs = eqs_of_subst(st);
                    foreach (var pattern in exp.Patterns) {
                        var pt = pattern.Item1;
                        var ex = pattern.Item2;

                        // パターン式から型等式と束縛を導出
                        var v2 = EvalPatternExpressions(pt, ty);
                        var eqs1 = v2.Item1;
                        var binds1 = v2.Item2;

                        // 束縛を環境に結合
                        var env1 = binds1.Aggregate(env, (s, x) => Environment.Extend(x.Key, tysc_of_ty(x.Value), s));

                        // 本体から型等式と戻り値型を導出
                        var v3 = EvalExpressions(env1, ex);
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
                }
                if (e is Expressions.TupleExp) {
                    var exp = (Expressions.TupleExp)e;
                    var tyMembers = exp.Members.Select(x => EvalExpressions(env, x)).ToArray();
                    var ss = tyMembers.Aggregate(LinkedList<TypeEquality>.Empty, (s, x) => LinkedList.Concat(s, eqs_of_subst(x.Item1)));
                    var eqs = Unify(ss);

                    return Tuple.Create(
                        eqs,
                        (Type)new Type.TyTuple(tyMembers.Select(x => subst_type(eqs, x.Item2)).ToArray())
                    );
                }
                if (e is Expressions.OptionExp) {
                    var exp = (Expressions.OptionExp)e;
                    if (exp == Expressions.OptionExp.None) {
                        var domty = Type.TyVar.Fresh();
                        return Tuple.Create(
                            LinkedList<TypeSubst>.Empty,
                            (Type) new Type.TyOption(domty)
                        );
                    } else { 
                        var mem = EvalExpressions(env, exp.Expr);
                        var ss = eqs_of_subst(mem.Item1);
                        var ty = mem.Item2;
                        var eqs = Unify(ss);

                        return Tuple.Create(
                            eqs,
                            (Type) new Type.TyOption(subst_type(eqs, ty))
                        );
                    }
                }
                if (e is Expressions.HaltExp) {
                    return Tuple.Create(
                        LinkedList<TypeSubst>.Empty,
                        (Type)Type.TyVar.Fresh()
                    );
                }

                throw new NotSupportedException($"expression {e} cannot eval.");
            }

            private static Result eval_declEntry(Environment<TypeScheme> env, Toplevel.Binding.DeclBase p) {
                if (p is Toplevel.Binding.LetDecl) {
                    var decl = (Toplevel.Binding.LetDecl) p;

                    var newenv = env;
                    var substs = LinkedList<TypeSubst>.Empty;
                    var ret = new Result("", env,   null);
                    foreach (var bind in decl.Binds) {
                        var v = EvalExpressions(env, bind.Item2);
                        var s = v.Item1;
                        var ty = v.Item2;
                        substs = LinkedList.Concat(s, substs);
                        newenv = Environment.Extend(bind.Item1, Closure(ty, env, s), newenv);
                        ret = new Result(bind.Item1, newenv,  ty);
                    }

                    {
                        var s3 = Unify(eqs_of_subst(substs));
                        return new Result(ret.Id, newenv,  subst_type(s3, ret.Value));
                    }
                }
                if (p is Toplevel.Binding.LetRecDecl) {
                    var decl = (Toplevel.Binding.LetRecDecl) p;

                    var dummyenv = env;

                    var binds = decl.Binds.Select(x => Tuple.Create(x.Item1, x.Item2, Type.TyVar.Fresh())).ToArray();
                    foreach (var bind in binds) {
                        dummyenv = Environment.Extend(bind.Item1, tysc_of_ty(bind.Item3), dummyenv);
                    }

                    var substs = LinkedList<TypeSubst>.Empty;
                    var eqs = LinkedList<TypeEquality>.Empty;

                    foreach (var bind in binds) {
                        var v = EvalExpressions(dummyenv, bind.Item2);
                        var s = v.Item1;
                        var ty = v.Item2;
                        eqs = LinkedList.Extend(new TypeEquality(bind.Item3, ty), eqs);
                        substs = LinkedList.Concat(s, substs);
                    }
                    var eqsBinds = LinkedList.Concat(eqs, eqs_of_subst(substs));
                    var substBinds = Unify(eqsBinds);

                    var newenv = env;
                    var ret = new Result("", newenv,  null);
                    foreach (var bind in binds) {
                        var tysc = Closure(subst_type(substBinds, bind.Item3), env, substBinds);
                        newenv = Environment.Extend(bind.Item1, tysc, newenv);
                        ret = new Result(bind.Item1, newenv,  tysc.Type);
                    }

                    {
                        var s3 = Unify(eqs_of_subst(substs));
                        return new Result(ret.Id, newenv,  subst_type(s3, ret.Value));
                    }
                }
                throw new NotSupportedException($"{p.GetType().FullName} cannot eval.");
            }

            public static Result eval_decl(Environment<TypeScheme> env, Toplevel p) {
                if (p is Toplevel.Exp) {
                    var e = (Toplevel.Exp) p;
                    var v = EvalExpressions(env, e.Syntax);
                    return new Result("-", env,  v.Item2);
                }
                if (p is Toplevel.ExternalDecl) {
                    var e = (Toplevel.ExternalDecl)p;
                    var tysc = EvalTypeExpressions(e.Type, new Dictionary<string, Type.TyVar>());
                    var newenv = Environment.Extend(e.Id, tysc, env);
                    return new Result(e.Id, newenv, tysc.Type);
                }
                if (p is Toplevel.Binding) {
                    var ds = (Toplevel.Binding) p;
                    var newenv = env;
                    var ret = new Result("", env,  null);
                    foreach (var d in ds.Entries) {
                        ret = eval_declEntry(newenv,  d);
                        newenv = ret.Env;
                    }
                    return ret;
                }
                if (p is Toplevel.TypeDef)
                {
                    var e = (Toplevel.TypeDef)p;
                    var tysc = EvalTypeExpressions(e.Type, new Dictionary<string, Type.TyVar>());
                    var newenv = Environment.Extend(e.Id, tysc, env);
                    return new Result(e.Id, newenv, tysc.Type);
                }
                if (p is Toplevel.Empty) {
                    return new Result("", env,  null);
                }
                throw new NotSupportedException($"{p.GetType().FullName} cannot eval.");
            }

            private static TypeScheme EvalTypeExpressions(TypeExpressions expressions, Dictionary<string,Type.TyVar> vars) {
                if (expressions is TypeExpressions.TypeVar) { var e = (TypeExpressions.TypeVar) expressions; if (vars.ContainsKey(e.Id)==false) { vars[e.Id] = Type.TyVar.Fresh(); } return new TypeScheme(Set.Singleton(vars[e.Id]), vars[e.Id]); }
                if (expressions is TypeExpressions.IntType) { return tysc_of_ty(new Type.TyInt());}
                if (expressions is TypeExpressions.BoolType) { return tysc_of_ty(new Type.TyBool()); }
                if (expressions is TypeExpressions.StrType) { return tysc_of_ty(new Type.TyStr()); }
                if (expressions is TypeExpressions.UnitType) { return tysc_of_ty(new Type.TyUnit()); }
                if (expressions is TypeExpressions.ListType)
                {
                    var e = (TypeExpressions.ListType) expressions;
                    var tysc = EvalTypeExpressions(e.Type, vars);
                    return new TypeScheme(tysc.Vars, new Type.TyList(tysc.Type));
                }
                if (expressions is TypeExpressions.OptionType)
                {
                    var e = (TypeExpressions.OptionType) expressions;
                    var tysc = EvalTypeExpressions(e.Type, vars);
                    return new TypeScheme(tysc.Vars, new Type.TyOption(tysc.Type));
                }
                if (expressions is TypeExpressions.TupleType) {
                    var e = (TypeExpressions.TupleType) expressions;
                    var tyscs = e.Members.Select(x => EvalTypeExpressions(x, vars)).ToArray();
                    return new TypeScheme(tyscs.Aggregate(Set<Type.TyVar>.Empty, (s,x) => Set.Union(x.Vars,s)), new Type.TyTuple(tyscs.Select(x => x.Type).ToArray()));
                }
                if (expressions is TypeExpressions.FuncType)
                {
                    var e = (TypeExpressions.FuncType) expressions;
                    var tysc1 = EvalTypeExpressions(e.DomainType, vars);
                    var tysc2 = EvalTypeExpressions(e.RangeType, vars);
                    return new TypeScheme(
                        Set.Union(tysc1.Vars, tysc2.Vars),
                        new Type.TyFunc( tysc1.Type,  tysc2.Type )
                    );
                }
                if (expressions is TypeExpressions.TypeName) {
                    throw new NotImplementedException();
                }
                if (expressions is TypeExpressions.TypeConstruct) {
                    var e = (TypeExpressions.TypeConstruct) expressions;
                    if (e.Base.Name == "list") {
                        if (e.Params.Length != 1) {
                            throw new Exception.InvalidArgumentNumException();
                        }
                        var tysc = EvalTypeExpressions(e.Params[0], vars);
                        return new TypeScheme(
                            tysc.Vars, 
                            new Type.TyList(tysc.Type)
                        );
                    }
                    if (e.Base.Name == "option") {
                        if (e.Params.Length != 1) {
                            throw new Exception.InvalidArgumentNumException();
                        }
                        var tysc = EvalTypeExpressions(e.Params[0], vars);
                        return new TypeScheme(
                            tysc.Vars,
                            new Type.TyOption(tysc.Type)
                        );
                    }
                    throw new NotImplementedException();
                }
                throw new NotSupportedException();
            }
        }
    }
}
