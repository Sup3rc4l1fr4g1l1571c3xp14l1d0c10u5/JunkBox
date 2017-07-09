using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MiniMAL.Syntax;

namespace MiniMAL {
    public static partial class Typing {
        /// <summary>
        ///     型
        /// </summary>
        public abstract class Type {
            public class TyInt : Type {
            }

            public class TyBool : Type {
            }

            public class TyStr : Type {
            }

            public class TyUnit : Type {
            }

            public class TyVar : Type {
                private static int _counter;

                public static TyVar Fresh() {
                    return new TyVar(_counter++);
                }

                public int Id { get; }

                public TyVar(int id) {
                    Id = id;
                }
            }

            public class TyFunc : Type {
                public Type ArgType { get; }
                public Type RetType { get; }

                public TyFunc(Type argType, Type retType) {
                    ArgType = argType;
                    RetType = retType;
                }
            }

            public class TyList : Type {
                public Type ItemType { get; }
                public static TyList Empty { get; } = new TyList(null);

                public TyList(Type itemType) {
                    ItemType = itemType;
                }
            }

            public class TyTuple : Type {
                public Type[] Members { get; }
                public TyTuple(Type[] members) {
                    Members = members;
                }
            }

            public class TyOption : Type {
                public Type ItemType { get; }

                public TyOption(Type itemType) {
                    ItemType = itemType;
                }
            }

            /// <summary>
            ///     比較
            /// </summary>
            /// <param name="arg1"></param>
            /// <param name="arg2"></param>
            /// <returns></returns>
            public static bool Equals(Type arg1, Type arg2) {
                if (arg1 is TyVar && arg2 is TyVar) {
                    return ((TyVar)arg1).Id == ((TyVar)arg2).Id;
                }
                if (arg1 is TyInt && arg2 is TyInt) {
                    return true;
                }
                if (arg1 is TyStr && arg2 is TyStr) {
                    return true;
                }
                if (arg1 is TyBool && arg2 is TyBool) {
                    return true;
                }
                if (arg1 is TyUnit && arg2 is TyUnit) {
                    return true;
                }
                if (arg1 is TyOption && arg2 is TyOption) {
                    var i1 = (TyOption)arg1;
                    var i2 = (TyOption)arg2;
                    return Equals(i1.ItemType, i2.ItemType);
                }
                if (arg1 is TyFunc && arg2 is TyFunc) {
                    var i1 = (TyFunc)arg1;
                    var i2 = (TyFunc)arg2;
                    return Equals(i1.ArgType, i2.ArgType) && Equals(i1.RetType, i2.RetType);
                }
                if (arg1 is TyList && arg2 is TyList) {
                    var i1 = (TyList)arg1;
                    var i2 = (TyList)arg2;
                    if (ReferenceEquals(i1, TyList.Empty)) {
                        return ReferenceEquals(i1, TyList.Empty);
                    }
                    if (ReferenceEquals(i2, TyList.Empty)) {
                        return ReferenceEquals(i1, TyList.Empty);
                    }
                    return Equals(i1.ItemType, i2.ItemType);
                }
                if (arg1 is TyTuple && arg2 is TyTuple) {
                    var i1 = (TyTuple)arg1;
                    var i2 = (TyTuple)arg2;
                    return ReferenceEquals(i1.Members, i2.Members);
                }
                return false;
            }

            public override bool Equals(object obj) {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != GetType()) return false;
                return Equals(this, (Type)obj);
            }

            public override int GetHashCode() {
                return GetType().GetHashCode();
            }

            private static void type_stringizer(Dictionary<int, string> vars,
                                                StringBuilder buffer,
                                                int priority,
                                                Type t) {
                if (t is TyVar) {
                    var tt = (TyVar) t;
                    if (!vars.ContainsKey(tt.Id)) {
                        int i = vars.Count;
                        string str = "'";
                        for (;;) {
                            str = str + (char) ('a' + i % 26);
                            if (i < 26) {
                                break;
                            }
                            i /= 26;
                        }
                        vars[tt.Id] = str;
                    }
                    buffer.Append(vars[tt.Id]);
                    return;
                }
                if (t is TyUnit) {
                    buffer.Append("unit");
                    return;
                }
                if (t is TyInt) {
                    buffer.Append("int");
                    return;
                }
                if (t is TyBool) {
                    buffer.Append("bool");
                    return;
                }
                if (t is TyStr) {
                    buffer.Append("string");
                    return;
                }
                if (t is TyTuple) {
                    var tt = (TyTuple) t;
                    if (priority > 1) {
                        buffer.Append("(");
                    }
                    tt.Members.Aggregate("", (s, x) =>
                    {
                        buffer.Append(s);
                        type_stringizer(vars, buffer, 2, x);
                        return " * ";
                    });
                    if (priority > 1) {
                        buffer.Append(")");
                    }
                    return;
                }
                if (t is TyList) {
                    var tt = (TyList) t;
                    if (priority > 2) {
                        buffer.Append("(");
                    }
                    type_stringizer(vars, buffer, 2, tt.ItemType);
                    buffer.Append(" list");
                    if (priority > 2) {
                        buffer.Append(")");
                    }
                    return;
                }
                if (t is TyOption) {
                    var tt = (TyOption) t;
                    if (priority > 2) {
                        buffer.Append("(");
                    }
                    type_stringizer(vars, buffer, 2, tt.ItemType);
                    buffer.Append(" option");
                    if (priority > 2) {
                        buffer.Append(")");
                    }
                    return;
                }
                if (t is TyFunc) {
                    var tt = (TyFunc) t;
                    if (priority > 0) {
                        buffer.Append("(");
                    }
                    type_stringizer(vars, buffer, 1, tt.ArgType);
                    buffer.Append(" -> ");
                    type_stringizer(vars, buffer, 0, tt.RetType);
                    if (priority > 0) {
                        buffer.Append(")");
                    }
                    return;
                }
                throw new NotSupportedException();
            }

            public override string ToString() {
                var sb = new StringBuilder();
                var dic = new Dictionary<int,string>();
                type_stringizer(dic, sb, 0, this);
                return sb.ToString();
            }
        }

        /// <summary>
        ///     型等式
        /// </summary>
        public class TypeEquality {
            public Type Type1 { get; }
            public Type Type2 { get; }

            public TypeEquality(Type t1, Type t2) {
                Type1 = t1;
                Type2 = t2;
            }

            public override string ToString() {
                return $"({Type1} = {Type2})";
            }
        }

        /// <summary>
        ///     型置換
        /// </summary>
        public class TypeSubst {
            public Type.TyVar Var { get; }
            public Type Type { get; }

            public TypeSubst(Type.TyVar tyvar, Type ty) {
                Var = tyvar;
                Type = ty;
            }

            public override string ToString() {
                return $"({Var} |-> {Type})";
            }
        }

        /// <summary>
        /// 型 typ に 型代入リスト substs を先頭から順に適用した結果の型を求める
        /// </summary>
        /// <param name="substs"></param>
        /// <param name="typ"></param>
        /// <returns></returns>
        private static Type resolve_type(LinkedList<TypeSubst> substs, Type typ) {
            if (typ is Type.TyVar) {
                var ret = LinkedList.First(x => Type.Equals(x.Var, typ), substs);
                if (ret == null) {
                    return typ;
                }
                return ret.Type;
            }
            if (typ is Type.TyFunc) {
                var ty1 = ((Type.TyFunc)typ).ArgType;
                var ty2 = ((Type.TyFunc)typ).RetType;
                return new Type.TyFunc(resolve_type(substs, ty1), resolve_type(substs, ty2));
            }
            if (typ is Type.TyList) {
                var ty1 = ((Type.TyList)typ).ItemType;
                return new Type.TyList(resolve_type(substs, ty1));
            }
            if (typ is Type.TyOption) {
                var ty1 = ((Type.TyOption)typ).ItemType;
                return new Type.TyOption(resolve_type(substs, ty1));
            }
            if (typ is Type.TyTuple) {
                var ty1 = ((Type.TyTuple)typ);
                return new Type.TyTuple(ty1.Members.Select(x => resolve_type(substs, x)).ToArray());
            }
            return typ;
        }

        public static LinkedList<TypeSubst> resolve_subst(LinkedList<TypeSubst> s) {
            if (s == LinkedList<TypeSubst>.Empty) {
                return LinkedList<TypeSubst>.Empty;
            }
            var x = s.Value;
            var xs = s.Next;
            var id = x.Var;
            var typ = x.Type;
            var newSubst = resolve_subst(xs);
            return LinkedList.Extend(new TypeSubst(id, resolve_type(newSubst, typ)), newSubst);
        }

        public static Type subst_type(LinkedList<TypeSubst> s, Type typ) {
            return resolve_type(resolve_subst(s), typ);
        }

        /* eqs_of_subst : subst -> (ty* ty) list
        型代入を型の等式集合に変換*/
        public static LinkedList<TypeEquality> eqs_of_subst(LinkedList<TypeSubst> s) {
            return LinkedList.Map(x => new TypeEquality(x.Var, x.Type), s);
        }

        /* subst_eqs: subst -> (ty* ty) list -> (ty* ty) list
        型の等式集合に型代入を適用*/
        public static LinkedList<TypeEquality> subst_eqs(LinkedList<TypeSubst> s, LinkedList<TypeEquality> eqs) {
            return LinkedList.Map(x => new TypeEquality(subst_type(s, x.Type1), subst_type(s, x.Type2)), eqs);
        }

        public static Set<Type.TyVar> freevar_ty(Type ty) {
            if (ty is Type.TyVar) {
                return Set.Singleton((Type.TyVar)ty);
            }
            if (ty is Type.TyFunc) {
                var f = (Type.TyFunc)ty;
                var ty1 = f.ArgType;
                var ty2 = f.RetType;
                return Set.Union(freevar_ty(ty1), freevar_ty(ty2));
            }
            if (ty is Type.TyList) {
                var f = (Type.TyList)ty;
                return freevar_ty(f.ItemType);
            }
            if (ty is Type.TyOption) {
                var f = (Type.TyOption)ty;
                return freevar_ty(f.ItemType);
            }
            if (ty is Type.TyTuple) {
                var f = (Type.TyTuple)ty;
                return f.Members.Aggregate(Set<Type.TyVar>.Empty, (s, x) => Set.Union(freevar_ty(x), s));
            }
            return Set<Type.TyVar>.Empty;
        }

        public static Set<Type.TyVar> freevar_tysc(Set<Type.TyVar> tvs, Type ty) {
            return Set.Diff(freevar_ty(ty), tvs);
        }

        public static LinkedList<TypeSubst> Unify(LinkedList<TypeEquality> eqs) {
            if (eqs == LinkedList<TypeEquality>.Empty) {
                return LinkedList<TypeSubst>.Empty;
            }
            if (eqs.Value.Type1 is Type.TyInt && eqs.Value.Type2 is Type.TyInt) {
                return Unify(eqs.Next);
            }
            if (eqs.Value.Type1 is Type.TyBool && eqs.Value.Type2 is Type.TyBool) {
                return Unify(eqs.Next);
            }
            if (eqs.Value.Type1 is Type.TyStr && eqs.Value.Type2 is Type.TyStr) {
                return Unify(eqs.Next);
            }
            if (eqs.Value.Type1 is Type.TyUnit && eqs.Value.Type2 is Type.TyUnit) {
                return Unify(eqs.Next);
            }
            if (eqs.Value.Type1 is Type.TyOption && eqs.Value.Type2 is Type.TyOption) {
                var f1 = (Type.TyOption)eqs.Value.Type1;
                var f2 = (Type.TyOption)eqs.Value.Type2;
                return Unify(LinkedList.Concat(LinkedList.Create(new TypeEquality(f1.ItemType, f2.ItemType)),
                eqs.Next));
            }
            if (eqs.Value.Type1 is Type.TyList && eqs.Value.Type2 is Type.TyList) {
                var f1 = (Type.TyList)eqs.Value.Type1;
                var f2 = (Type.TyList)eqs.Value.Type2;
                return Unify(LinkedList.Concat(LinkedList.Create(new TypeEquality(f1.ItemType, f2.ItemType)),
                eqs.Next));
            }
            if (eqs.Value.Type1 is Type.TyFunc && eqs.Value.Type2 is Type.TyFunc) {
                var f1 = (Type.TyFunc)eqs.Value.Type1;
                var ty11 = f1.ArgType;
                var ty12 = f1.RetType;
                var f2 = (Type.TyFunc)eqs.Value.Type2;
                var ty21 = f2.ArgType;
                var ty22 = f2.RetType;
                return Unify(LinkedList.Concat(
                LinkedList.Create(new TypeEquality(ty12, ty22),
                new TypeEquality(ty11, ty21)), eqs.Next));
            }
            if (eqs.Value.Type1 is Type.TyFunc && eqs.Value.Type2 is Type.TyFunc) {
                var f1 = (Type.TyFunc)eqs.Value.Type1;
                var ty11 = f1.ArgType;
                var ty12 = f1.RetType;
                var f2 = (Type.TyFunc)eqs.Value.Type2;
                var ty21 = f2.ArgType;
                var ty22 = f2.RetType;
                return Unify(LinkedList.Concat(
                LinkedList.Create(new TypeEquality(ty11, ty21), new TypeEquality(ty12, ty22)),
                eqs.Next));
            }
            if (eqs.Value.Type1 is Type.TyTuple && eqs.Value.Type2 is Type.TyTuple) {
                var f1 = (Type.TyTuple)eqs.Value.Type1;
                var f2 = (Type.TyTuple)eqs.Value.Type2;

                if (f1.Members.Length != f2.Members.Length) {
                    throw new Exception.TypingException("Type missmatch");
                }

                var neweqs = f1.Members.Zip(f2.Members,Tuple.Create).Aggregate(LinkedList<TypeEquality>.Empty,
                    (s, x) => LinkedList.Extend(new TypeEquality(x.Item1, x.Item2), s));
                return Unify(neweqs);
            }
            if (eqs.Value.Type1 is Type.TyVar && eqs.Value.Type2 is Type.TyVar) {
                var v1 = (Type.TyVar)eqs.Value.Type1;
                var v2 = (Type.TyVar)eqs.Value.Type2;
                if (v1.Id == v2.Id) {
                    return Unify(eqs.Next);
                }
                var neweqs = LinkedList.Create(new TypeSubst(v1, v2));
                return LinkedList.Concat(neweqs, Unify(subst_eqs(neweqs, eqs.Next)));
            }
            if (eqs.Value.Type1 is Type.TyVar) {
                var v1 = (Type.TyVar)eqs.Value.Type1;
                var ty = eqs.Value.Type2;
                var rest = eqs.Next;
                if (Set.Member(v1, freevar_ty(ty))) {
                    throw new Exception.TypingException("Recursive type");
                }
                var eqs2 = LinkedList.Create(new TypeSubst(v1, ty));
                return LinkedList.Concat(eqs2, Unify(subst_eqs(eqs2, rest)));
            }
            if (eqs.Value.Type2 is Type.TyVar) {
                var v1 = (Type.TyVar)eqs.Value.Type2;
                var ty = eqs.Value.Type1;
                var rest = eqs.Next;
                if (Set.Member(v1, freevar_ty(ty))) {
                    throw new Exception.TypingException("Recursive type");
                }
                var eqs2 = LinkedList.Create(new TypeSubst(v1, ty));
                return LinkedList.Concat(eqs2, Unify(subst_eqs(eqs2, rest)));
            }
            throw new Exception.TypingException($"Cannot unify type: {eqs.Value.Type1} and {eqs.Value.Type2}");
        }

        /// <summary>
        ///     パターンマッチの評価
        /// </summary>
        /// <param name="pattern"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private static Tuple<LinkedList<TypeEquality>, Dictionary<string, Type>> EvalPatternExpressions(
        PatternExpressions pattern, Type value) {
            if (pattern is PatternExpressions.WildP) {
                return Tuple.Create(
                    LinkedList<TypeEquality>.Empty, 
                    new Dictionary<string, Type>()
                );
            }
            if (pattern is PatternExpressions.IntP) {
                return Tuple.Create(
                    LinkedList.Create(new TypeEquality(value, new Type.TyInt())),
                    new Dictionary<string, Type>()
                );
            }
            if (pattern is PatternExpressions.StrP) {
                return Tuple.Create(
                    LinkedList.Create(new TypeEquality(value, new Type.TyStr())),
                    new Dictionary<string, Type>()
                );
            }
            if (pattern is PatternExpressions.BoolP) {
                return Tuple.Create(
                    LinkedList.Create(new TypeEquality(value, new Type.TyBool())),
                    new Dictionary<string, Type>()
                );
            }
            if (pattern is PatternExpressions.OptionP) {
                if (pattern == PatternExpressions.OptionP.None) {
                    var tyitem = Type.TyVar.Fresh();
                    return Tuple.Create(
                        LinkedList.Create(
                            new TypeEquality(value, new Type.TyOption(tyitem))
                        ),
                        new Dictionary<string, Type>()
                    );
                } else {
                    var p = (PatternExpressions.OptionP)pattern;
                    var tyitem = Type.TyVar.Fresh();
                    var ret1 = EvalPatternExpressions(p.Value, tyitem);
                    return Tuple.Create(
                        LinkedList.Concat(
                            LinkedList.Create( new TypeEquality(value,new Type.TyOption(tyitem)) ),
                            ret1.Item1
                        ),
                        ret1.Item2
                    );
                }
            }
            if (pattern is PatternExpressions.UnitP) {
                return Tuple.Create(
                LinkedList.Create(new TypeEquality(value, new Type.TyUnit())),
                new Dictionary<string, Type>()
                );
            }
            if (pattern is PatternExpressions.VarP) {
                return Tuple.Create(
                    LinkedList<TypeEquality>.Empty,
                    new Dictionary<string, Type> { { ((PatternExpressions.VarP)pattern).Id, value } }
                );
            }
            if (pattern is PatternExpressions.ConsP) {
                var p = (PatternExpressions.ConsP)pattern;
                if (p == PatternExpressions.ConsP.Empty) {
                    return Tuple.Create(
                        LinkedList.Create(new TypeEquality(value, Type.TyList.Empty)),
                        new Dictionary<string, Type>()
                    );
                } else {
                    var tyitem = Type.TyVar.Fresh();
                    var tyList = new Type.TyList(tyitem);
                    var ret1 = EvalPatternExpressions(p.Value, tyitem);
                    var ret2 = EvalPatternExpressions(p.Next, tyList);
                    return Tuple.Create(
                        LinkedList.Concat(
                            LinkedList.Create(new TypeEquality(tyList, value)),
                            ret1.Item1,
                            ret2.Item1
                        ),
                        ret2.Item2.Aggregate(new Dictionary<string,Type>(ret1.Item2), (s, x) => { s[x.Key] = x.Value; return s; })
                    );
                }
            }
            if (pattern is PatternExpressions.TupleP) {
                var p = (PatternExpressions.TupleP)pattern;

                var members = p.Members.Select(x => Tuple.Create(x, Type.TyVar.Fresh())).ToArray();
                var tupleType = new Type.TyTuple(members.Select(x => (Type)x.Item2).ToArray());

                var eqs = LinkedList.Create(new TypeEquality(value, tupleType));
                var binds = new Dictionary<string, Type>();
                foreach (var ptv in members) {
                    var patexpr = ptv.Item1;
                    var pattyvar = ptv.Item2;

                    var ret = EvalPatternExpressions(patexpr, pattyvar);
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
