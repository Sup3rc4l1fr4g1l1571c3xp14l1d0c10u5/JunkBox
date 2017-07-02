using System;
using System.Collections.Generic;
using System.Linq;

namespace MiniMAL {
    public static partial  class Typing { 
        public static class MonomorphicTyping {

        /// <summary>
        ///     評価結果
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
        ///     式の評価
        /// </summary>
        /// <param name="env"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        private static Tuple<LinkedList<TypeSubst>, Type> EvalExpressions(Environment<Type> env, Expressions e) {
            if (e is Expressions.Var) {
                var x = ((Expressions.Var)e).Id;
                try {
                    return Tuple.Create(
                        LinkedList<TypeSubst>.Empty,
                        Environment.LookUp(x, env)
                    );
                } catch (Exception.NotBound) {
                    throw new Exception.NotBound($"Variable not bound: {x}");
                }
            }
            if (e is Expressions.IntLit) {
                return Tuple.Create(
                LinkedList<TypeSubst>.Empty,
                (Type)new Type.TyInt()
                );
            }
            if (e is Expressions.StrLit) {
                return Tuple.Create(
                LinkedList<TypeSubst>.Empty,
                (Type)new Type.TyStr()
                );
            }
            if (e is Expressions.BoolLit) {
                return Tuple.Create(
                LinkedList<TypeSubst>.Empty,
                (Type)new Type.TyBool()
                );
            }
            if (e is Expressions.EmptyListLit) {
                return Tuple.Create(
                LinkedList<TypeSubst>.Empty,
                (Type)new Type.TyCons(Type.TyVar.Fresh())
                );
            }
            if (e is Expressions.UnitLit) {
                return Tuple.Create(
                LinkedList<TypeSubst>.Empty,
                (Type)new Type.TyUnit()
                );
            }
            if (e is Expressions.BuiltinOp) {
                var op = ((Expressions.BuiltinOp)e).Op;
                var args = ((Expressions.BuiltinOp)e).Exprs.Select(x => EvalExpressions(env, x)).ToArray();
                var ret = EvalBuiltinExpressions(op, args.Select(x => x.Item2).ToArray());
                var ss = LinkedList.Concat(LinkedList.Concat(args.Select(x => eqs_of_subst(x.Item1)).ToArray()),
                ret.Item2);
                var eqs = Unify(ss);
                return Tuple.Create(
                eqs,
                subst_type(eqs, ret.Item1)
                );
            }
            if (e is Expressions.IfExp) {
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
            if (e is Expressions.LetExp) {
                var exp = (Expressions.LetExp)e;

                var newenv = env;
                var substs = LinkedList<TypeSubst>.Empty;
                foreach (var bind in exp.Binds) {
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
            if (e is Expressions.LetRecExp) {
                var exp = (Expressions.LetRecExp)e;

                var dummyenv = env;

                var binds = exp.Binds.Select(x => Tuple.Create(x.Item1, x.Item2, Type.TyVar.Fresh())).ToArray();
                foreach (var bind in binds) {
                    dummyenv = Environment.Extend(bind.Item1, bind.Item3, dummyenv);
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
            if (e is Expressions.FunExp) {
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
            if (e is Expressions.DFunExp) {
                throw new NotSupportedException();
            }
            if (e is Expressions.AppExp) {
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
            if (e is Expressions.MatchExp) {
                var exp = (Expressions.MatchExp)e;

                var domty = Type.TyVar.Fresh();

                // 式の型推論
                var v1 = EvalExpressions(env, exp.Exp);
                var st = v1.Item1;
                var ty = v1.Item2;

                // 
                var eqs = eqs_of_subst(st);
                foreach (var pattern in exp.Patterns) {
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
            if (e is Expressions.TupleExp) {
                if (e == Expressions.TupleExp.Tail) {
                    return Tuple.Create(
                        LinkedList<TypeSubst>.Empty,
                        (Type)Type.TyTuple.Tail
                    );
                } else {
                    var exp = (Expressions.TupleExp)e;
                    var tyCar = EvalExpressions(env, exp.Car);
                    var tyCdr = EvalExpressions(env, exp.Cdr);
                    var ss = LinkedList.Concat(eqs_of_subst(tyCar.Item1), eqs_of_subst(tyCdr.Item1));
                    var eqs = Unify(ss);

                    return Tuple.Create(
                        eqs,
                        (Type)new Type.TyTuple(subst_type(eqs, tyCar.Item2), subst_type(eqs, tyCdr.Item2) as Type.TyTuple)
                    );
                }
            }
                if (e is Expressions.OptionExp) {
                if (e == Expressions.OptionExp.None) {
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
            if (e is Expressions.HaltExp) {
                throw new Exception.HaltException((e as Expressions.HaltExp).Message);
            }

            throw new NotSupportedException($"expression {e} cannot eval.");
        }

        private static Result EvalBinding(Environment<Type> env, Toplevel.Binding.DeclBase p) {
            if (p is Toplevel.Binding.LetDecl) {
                var decl = (Toplevel.Binding.LetDecl)p;

                var newenv = env;
                var substs = LinkedList<TypeSubst>.Empty;
                var ret = new Result("", env, null);
                foreach (var bind in decl.Binds) {
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
            if (p is Toplevel.Binding.LetRecDecl) {
                var decl = (Toplevel.Binding.LetRecDecl)p;

                var dummyenv = env;

                var binds = decl.Binds.Select(x => Tuple.Create(x.Item1, x.Item2, Type.TyVar.Fresh())).ToArray();
                foreach (var bind in binds) {
                    dummyenv = Environment.Extend(bind.Item1, bind.Item3, dummyenv);
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
                var ret = new Result("", newenv, null);
                foreach (var bind in binds) {
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

        public static Result EvalToplevel(Environment<Type> env, Toplevel p) {
            if (p is Toplevel.Exp) {
                var e = (Toplevel.Exp)p;
                var v = EvalExpressions(env, e.Syntax);
                return new Result("-", env, v.Item2);
            }
            if (p is Toplevel.Binding) {
                var ds = (Toplevel.Binding)p;
                var newenv = env;
                var ret = new Result("", env, null);
                foreach (var d in ds.Entries) {
                    ret = EvalBinding(newenv, d);
                    newenv = ret.Env;
                }
                return ret;
            }
            if (p is Toplevel.Empty) {
                return new Result("", env, null);
            }
            throw new NotSupportedException($"{p.GetType().FullName} cannot eval.");
        }
    }
    }
}