using System;
using System.Linq;
using MiniMAL.Syntax;

namespace MiniMAL
{
    public static partial class Typing
    {
        public static class MonomorphicTyping
        {

            /// <summary>
            ///     評価結果
            /// </summary>
            public class Result
            {
                public Result(string id, Environment<Type> env, Type value)
                {
                    Id = id;
                    Env = env;
                    Value = value;
                }

                public string Id { get; }
                public Environment<Type> Env { get; }
                public Type Value { get; }
            }

            /// <summary>
            ///     式の評価
            /// </summary>
            /// <param name="env"></param>
            /// <param name="e"></param>
            /// <returns></returns>
            private static Tuple<LinkedList<TypeSubst>, Type> EvalExpressions(Environment<Type> env, Expressions e)
            {
                if (e is Expressions.Var)
                {
                    var x = ((Expressions.Var)e).Id;
                    try
                    {
                        return Tuple.Create(
                            LinkedList<TypeSubst>.Empty,
                            Environment.LookUp(x, env)
                        );
                    }
                    catch (Exception.NotBound)
                    {
                        throw new Exception.NotBound($"Variable not bound: {x}");
                    }
                }
                if (e is Expressions.IntLit)
                {
                    return Tuple.Create(
                    LinkedList<TypeSubst>.Empty,
                    (Type)new Type.TyInt()
                    );
                }
                if (e is Expressions.StrLit)
                {
                    return Tuple.Create(
                    LinkedList<TypeSubst>.Empty,
                    (Type)new Type.TyStr()
                    );
                }
                if (e is Expressions.BoolLit)
                {
                    return Tuple.Create(
                    LinkedList<TypeSubst>.Empty,
                    (Type)new Type.TyBool()
                    );
                }
                if (e is Expressions.EmptyListLit)
                {
                    return Tuple.Create(
                    LinkedList<TypeSubst>.Empty,
                    (Type)new Type.TyList(Type.TyVar.Fresh())
                    );
                }
                if (e is Expressions.UnitLit)
                {
                    return Tuple.Create(
                    LinkedList<TypeSubst>.Empty,
                    (Type)new Type.TyUnit()
                    );
                }
                if (e is Expressions.IfExp)
                {
                    var cond = EvalExpressions(env, ((Expressions.IfExp)e).Cond);
                    var then = EvalExpressions(env, ((Expressions.IfExp)e).Then);
                    var @else = EvalExpressions(env, ((Expressions.IfExp)e).Else);
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
                if (e is Expressions.LetExp)
                {
                    var exp = (Expressions.LetExp)e;

                    var newenv = env;
                    var substs = LinkedList<TypeSubst>.Empty;
                    foreach (var bind in exp.Binds)
                    {
                        var v = EvalExpressions(env, bind.Item2);
                        var s = v.Item1;
                        var ty = v.Item2;
                        substs = LinkedList.Concat(s, substs);
                        newenv = Environment.Extend(bind.Item1, ty, newenv);
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
                if (e is Expressions.LetRecExp)
                {
                    var exp = (Expressions.LetRecExp)e;

                    var dummyenv = env;

                    var binds = exp.Binds.Select(x => Tuple.Create(x.Item1, x.Item2, Type.TyVar.Fresh())).ToArray();
                    foreach (var bind in binds)
                    {
                        dummyenv = Environment.Extend(bind.Item1, bind.Item3, dummyenv);
                    }

                    var substs = LinkedList<TypeSubst>.Empty;
                    var eqs = LinkedList<TypeEquality>.Empty;

                    foreach (var bind in binds)
                    {
                        var v = EvalExpressions(dummyenv, bind.Item2);
                        var s = v.Item1;
                        var ty = v.Item2;
                        eqs = LinkedList.Extend(new TypeEquality(bind.Item3, ty), eqs);
                        substs = LinkedList.Concat(s, substs);
                    }
                    var eqsBinds = LinkedList.Concat(eqs, eqs_of_subst(substs));
                    var substBinds = Unify(eqsBinds);

                    var newenv = env;
                    foreach (var bind in binds)
                    {
                        newenv = Environment.Extend(bind.Item1, subst_type(substBinds, bind.Item3), newenv);
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
                if (e is Expressions.FunExp)
                {
                    var exp = (Expressions.FunExp)e;
                    var domty = Type.TyVar.Fresh();
                    var v = EvalExpressions(Environment.Extend(exp.Arg, domty, env), exp.Body);
                    var eqs = eqs_of_subst(v.Item1);
                    var s = Unify(eqs);
                    return Tuple.Create(
                    v.Item1,
                    subst_type(s, new Type.TyFunc(domty, v.Item2))
                    );
                }
                if (e is Expressions.AppExp)
                {
                    var exp = (Expressions.AppExp)e;

                    var v1 = EvalExpressions(env, exp.Fun);
                    var s1 = v1.Item1;
                    var ty1 = v1.Item2;

                    var v2 = EvalExpressions(env, exp.Arg);
                    var s2 = v2.Item1;
                    var ty2 = v2.Item2;

                    var domty = Type.TyVar.Fresh();

                    var eqs = LinkedList.Concat(
                        LinkedList.Create(
                            new TypeEquality(
                                ty1,
                                new Type.TyFunc(ty2, domty)
                            )
                        ),
                        eqs_of_subst(s1),
                        eqs_of_subst(s2)
                    );
                    var s3 = Unify(eqs);
                    return Tuple.Create(
                        s3,
                        subst_type(s3, domty)
                    );
                }
                if (e is Expressions.MatchExp)
                {
                    var exp = (Expressions.MatchExp)e;

                    var domty = Type.TyVar.Fresh();

                    // 式の型推論
                    var v1 = EvalExpressions(env, exp.Exp);
                    var st = v1.Item1;
                    var ty = v1.Item2;

                    // 
                    var eqs = eqs_of_subst(st);
                    foreach (var pattern in exp.Patterns)
                    {
                        var pt = pattern.Item1;
                        var ex = pattern.Item2;

                        var v2 = EvalPatternExpressions(pt, ty);
                        var eqs1 = v2.Item1;
                        var binds1 = v2.Item2;

                        var env1 = binds1.Aggregate(env, (s, x) => Environment.Extend(x.Key, x.Value, s));
                        var v3 = EvalExpressions(env1, ex);
                        var se = v3.Item1;
                        var tye = v3.Item2;

                        eqs = LinkedList.Concat(
                        LinkedList.Create(new TypeEquality(domty, tye)),
                        eqs1,
                        eqs_of_subst(se));
                    }

                    var s3 = Unify(eqs);

                    return Tuple.Create(
                    s3,
                    subst_type(s3, domty)
                    );
                }
                if (e is Expressions.TupleExp)
                {
                    var exp = (Expressions.TupleExp)e;
                    var tyMembers = exp.Members.Select(x => EvalExpressions(env, x)).ToArray();
                    var ss = tyMembers.Aggregate(LinkedList<TypeEquality>.Empty, (s, x) => LinkedList.Concat(s, eqs_of_subst(x.Item1)));
                    var eqs = Unify(ss);

                    return Tuple.Create(
                        eqs,
                        (Type)new Type.TyTuple(tyMembers.Select(x => subst_type(eqs, x.Item2)).ToArray())
                    );
                }
                if (e is Expressions.OptionExp)
                {
                    if (e == Expressions.OptionExp.None)
                    {
                        var domty = Type.TyVar.Fresh();
                        return Tuple.Create(
                        LinkedList<TypeSubst>.Empty,
                        (Type)new Type.TyOption(domty)
                        );
                    }
                    var exp = (Expressions.OptionExp)e;
                    var mem = EvalExpressions(env, exp.Expr);
                    var ss = eqs_of_subst(mem.Item1);
                    var ty = mem.Item2;
                    var eqs = Unify(ss);

                    return Tuple.Create(
                    eqs,
                    (Type)new Type.TyOption(subst_type(eqs, ty))
                    );
                }
                if (e is Expressions.HaltExp)
                {
                    throw new Exception.HaltException((e as Expressions.HaltExp).Message);
                }

                throw new NotSupportedException($"expression {e} cannot eval.");
            }

            private static Result EvalBinding(Environment<Type> env, Toplevel.Binding.DeclBase p)
            {
                if (p is Toplevel.Binding.LetDecl)
                {
                    var decl = (Toplevel.Binding.LetDecl)p;

                    var newenv = env;
                    var substs = LinkedList<TypeSubst>.Empty;
                    var ret = new Result("", env, null);
                    foreach (var bind in decl.Binds)
                    {
                        var v = EvalExpressions(env, bind.Item2);
                        var s = v.Item1;
                        var ty = v.Item2;
                        substs = LinkedList.Concat(s, substs);
                        newenv = Environment.Extend(bind.Item1, ty, newenv);
                        ret = new Result(bind.Item1, newenv, null);
                    }

                    {
                        var s3 = Unify(eqs_of_subst(substs));
                        return new Result(ret.Id, newenv, subst_type(s3, ret.Value));
                    }
                }
                if (p is Toplevel.Binding.LetRecDecl)
                {
                    var decl = (Toplevel.Binding.LetRecDecl)p;

                    var dummyenv = env;

                    var binds = decl.Binds.Select(x => Tuple.Create(x.Item1, x.Item2, Type.TyVar.Fresh())).ToArray();
                    foreach (var bind in binds)
                    {
                        dummyenv = Environment.Extend(bind.Item1, bind.Item3, dummyenv);
                    }

                    var substs = LinkedList<TypeSubst>.Empty;
                    var eqs = LinkedList<TypeEquality>.Empty;

                    foreach (var bind in binds)
                    {
                        var v = EvalExpressions(dummyenv, bind.Item2);
                        var s = v.Item1;
                        var ty = v.Item2;
                        eqs = LinkedList.Extend(new TypeEquality(bind.Item3, ty), eqs);
                        substs = LinkedList.Concat(s, substs);
                    }
                    var eqsBinds = LinkedList.Concat(eqs, eqs_of_subst(substs));
                    var substBinds = Unify(eqsBinds);

                    var newenv = env;
                    var ret = new Result("", newenv, null);
                    foreach (var bind in binds)
                    {
                        var ty = subst_type(substBinds, bind.Item3);
                        newenv = Environment.Extend(bind.Item1, ty, newenv);
                        ret = new Result(bind.Item1, newenv, ty);
                    }

                    {
                        var s3 = Unify(eqs_of_subst(substs));
                        return new Result(ret.Id, newenv, subst_type(s3, ret.Value));
                    }
                }
                throw new NotSupportedException($"{p.GetType().FullName} cannot eval.");
            }

            public static Result EvalToplevel(Environment<Type> env, Toplevel p)
            {
                if (p is Toplevel.Exp)
                {
                    var e = (Toplevel.Exp)p;
                    var v = EvalExpressions(env, e.Syntax);
                    return new Result("-", env, v.Item2);
                }
                if (p is Toplevel.Binding)
                {
                    var ds = (Toplevel.Binding)p;
                    var newenv = env;
                    var ret = new Result("", env, null);
                    foreach (var d in ds.Entries)
                    {
                        ret = EvalBinding(newenv, d);
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
        /// <summary>
        ///     パターンマッチの評価
        /// </summary>
        /// <param name="tyEnv"></param>
        /// <param name="pattern"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private static Tuple<LinkedList<TypeEquality>, Dictionary<string, Type>> EvalPatternExpressions(
            Environment<PolymorphicTyping.TypeScheme> tyEnv,
            PatternExpressions pattern,
            Type value
        )
        {
            if (pattern is PatternExpressions.WildP)
            {
                return Tuple.Create(
                    LinkedList<TypeEquality>.Empty,
                    new Dictionary<string, Type>()
                );
            }
            if (pattern is PatternExpressions.IntP)
            {
                return Tuple.Create(
                    LinkedList.Create(new TypeEquality(value, new Type.TyInt())),
                    new Dictionary<string, Type>()
                );
            }
            if (pattern is PatternExpressions.StrP)
            {
                return Tuple.Create(
                    LinkedList.Create(new TypeEquality(value, new Type.TyStr())),
                    new Dictionary<string, Type>()
                );
            }
            if (pattern is PatternExpressions.BoolP)
            {
                return Tuple.Create(
                    LinkedList.Create(new TypeEquality(value, new Type.TyBool())),
                    new Dictionary<string, Type>()
                );
            }
            if (pattern is PatternExpressions.RecordP)
            {
                var p = (PatternExpressions.RecordP)pattern;
                for (var it = tyEnv; it != Environment<PolymorphicTyping.TypeScheme>.Empty; it = it.Next)
                {
                    var ty = it.Value.Type;
                    while (ty is Type.TyTypeRef)
                    {
                        ty = ((Type.TyTypeRef)ty).Type;
                    }
                    if (!(ty is Type.TyRecord))
                    {
                        continue;
                    }
                    var tyRecord = (Type.TyRecord)ty;
                    if (tyRecord.Members.Select(x => x.Item1).OrderBy(x => x)
                            .SequenceEqual(p.Members.Select(x => x.Item1).OrderBy(x => x)) == false)
                    {
                        continue;
                    }

                    var tyMemberMap = p.Members.Select(x => Tuple.Create(x.Item1, x.Item2, Type.TyVar.Fresh())).ToArray();
                    var dic = new Dictionary<string, Type>();
                    var members = new List<Tuple<string, Type>>();
                    foreach (var x in tyMemberMap)
                    {
                        var r = EvalPatternExpressions(tyEnv, x.Item2, x.Item3);
                        members.Add(Tuple.Create(x.Item1, (Type)x.Item3));
                        dic = r.Item2.Aggregate(dic, (s, y) =>
                        {
                            s[y.Key] = y.Value;
                            return s;
                        });
                    }

                    return Tuple.Create(
                        LinkedList.Create(new TypeEquality(value, new Type.TyRecord(members.ToArray()))),
                        dic
                    );
                }
            }
            if (pattern is PatternExpressions.VariantP)
            {
                var p = (PatternExpressions.VariantP)pattern;
                for (var it = tyEnv; it != Environment<PolymorphicTyping.TypeScheme>.Empty; it = it.Next)
                {
                    var ty = it.Value.Type;
                    while (ty is Type.TyTypeRef)
                    {
                        ty = ((Type.TyTypeRef)ty).Type;
                    }
                    if (!(ty is Type.TyVariant))
                    {
                        continue;
                    }
                    var constructor = ((Type.TyVariant)ty).Members.FirstOrDefault(x => x.Item1 == p.ConstructorName);
                    if (constructor == null)
                    {
                        continue;
                    }

                    var tyBodyVar = Type.TyVar.Fresh();
                    var ret = EvalPatternExpressions(tyEnv, p.Body, tyBodyVar);

                    return Tuple.Create(
                        LinkedList.Concat(
                            LinkedList.Create(new TypeEquality(value, ty)),
                            ret.Item1
                        ),
                        ret.Item2
                    );
                }
            }
            if (pattern is PatternExpressions.OptionP)
            {
                if (pattern == PatternExpressions.OptionP.None)
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
                    var p = (PatternExpressions.OptionP)pattern;
                    var tyitem = Type.TyVar.Fresh();
                    var ret1 = EvalPatternExpressions(tyEnv, p.Value, tyitem);
                    return Tuple.Create(
                        LinkedList.Concat(
                            LinkedList.Create(new TypeEquality(value, new Type.TyOption(tyitem))),
                            ret1.Item1
                        ),
                        ret1.Item2
                    );
                }
            }
            if (pattern is PatternExpressions.UnitP)
            {
                return Tuple.Create(
                LinkedList.Create(new TypeEquality(value, new Type.TyUnit())),
                new Dictionary<string, Type>()
                );
            }
            if (pattern is PatternExpressions.VarP)
            {
                return Tuple.Create(
                    LinkedList<TypeEquality>.Empty,
                    new Dictionary<string, Type> { { ((PatternExpressions.VarP)pattern).Id, value } }
                );
            }
            if (pattern is PatternExpressions.ConsP)
            {
                var p = (PatternExpressions.ConsP)pattern;
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
                    var ret1 = EvalPatternExpressions(tyEnv, p.Value, tyitem);
                    var ret2 = EvalPatternExpressions(tyEnv, p.Next, tyList);
                    return Tuple.Create(
                        LinkedList.Concat(
                            LinkedList.Create(new TypeEquality(tyList, value)),
                            ret1.Item1,
                            ret2.Item1
                        ),
                        ret2.Item2.Aggregate(new Dictionary<string, Type>(ret1.Item2), (s, x) => { s[x.Key] = x.Value; return s; })
                    );
                }
            }
            if (pattern is PatternExpressions.TupleP)
            {
                var p = (PatternExpressions.TupleP)pattern;

                var members = p.Members.Select(x => Tuple.Create(x, Type.TyVar.Fresh())).ToArray();
                var tupleType = new Type.TyTuple(members.Select(x => (Type)x.Item2).ToArray());

                var eqs = LinkedList.Create(new TypeEquality(value, tupleType));
                var binds = new Dictionary<string, Type>();
                foreach (var ptv in members)
                {
                    var patexpr = ptv.Item1;
                    var pattyvar = ptv.Item2;

                    var ret = EvalPatternExpressions(tyEnv, patexpr, pattyvar);
                    var pateqs = ret.Item1;
                    var patbind = ret.Item2;

                    eqs = LinkedList.Concat(pateqs, eqs);
                    binds = patbind.Aggregate(binds, (s, x) => {
                        s[x.Key] = x.Value;
                        return s;
                    });
                }
                return Tuple.Create(
                eqs,
                binds
                );
            }

            return null;
        }

    }
}