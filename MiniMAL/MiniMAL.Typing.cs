using System;
using System.Collections.Generic;
using System.Linq;

namespace MiniMAL {
    public static partial class Typing {
        /// <summary>
        ///     型
        /// </summary>
        public abstract class Type {
            public class TyInt : Type {
                public override string ToString() {
                    return "int";
                }
            }

            public class TyBool : Type {
                public override string ToString() {
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

            public class TyVar : Type {
                private static int _counter;
                public int Id { get; }

                public static TyVar Fresh() {
                    return new TyVar(_counter++);
                }

                public TyVar(int id) {
                    Id = id;
                }

                public override string ToString() {
                    return $"'{Id}";
                }
            }

            public class TyFunc : Type {
                public Type ArgType { get; }
                public Type RetType { get; }

                public TyFunc(Type argType, Type retType) {
                    ArgType = argType;
                    RetType = retType;
                }

                public override string ToString() {
                    return $"({ArgType} -> {RetType})";
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

            public class TyOption : Type {
                public Type ItemType { get; }

                public TyOption(Type itemType) {
                    ItemType = itemType;
                }

                public override string ToString() {
                    return $"{ItemType} option";
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
                if (arg1 is TyCons && arg2 is TyCons) {
                    var i1 = (TyCons)arg1;
                    var i2 = (TyCons)arg2;
                    if (ReferenceEquals(i1, TyCons.Empty)) {
                        return ReferenceEquals(i1, TyCons.Empty);
                    }
                    if (ReferenceEquals(i2, TyCons.Empty)) {
                        return ReferenceEquals(i1, TyCons.Empty);
                    }
                    return Equals(i1.ItemType, i2.ItemType);
                }
                if (arg1 is TyTuple && arg2 is TyTuple) {
                    var i1 = (TyTuple)arg1;
                    var i2 = (TyTuple)arg2;
                    if (i1.ItemType.Length != i2.ItemType.Length) {
                        return false;
                    }

                    return i1.ItemType.Zip(i2.ItemType, Tuple.Create).All(x => Equals(x.Item1, x.Item2));
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
            if (typ is Type.TyCons) {
                var ty1 = ((Type.TyCons)typ).ItemType;
                return new Type.TyCons(resolve_type(substs, ty1));
            }
            if (typ is Type.TyOption) {
                var ty1 = ((Type.TyOption)typ).ItemType;
                return new Type.TyOption(resolve_type(substs, ty1));
            }
            if (typ is Type.TyTuple) {
                var ty1 = ((Type.TyTuple)typ).ItemType;
                return new Type.TyTuple(ty1.Select(x => resolve_type(substs, x)).ToArray());
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
                return Set.singleton((Type.TyVar)ty);
            }
            if (ty is Type.TyFunc) {
                var f = (Type.TyFunc)ty;
                var ty1 = f.ArgType;
                var ty2 = f.RetType;
                return Set.union(freevar_ty(ty1), freevar_ty(ty2));
            }
            if (ty is Type.TyCons) {
                var f = (Type.TyCons)ty;
                return freevar_ty(f.ItemType);
            }
            if (ty is Type.TyOption) {
                var f = (Type.TyOption)ty;
                return freevar_ty(f.ItemType);
            }
            if (ty is Type.TyTuple) {
                var f = (Type.TyTuple)ty;
                return f.ItemType.Aggregate(Set<Type.TyVar>.Empty, (s, x) => Set.union(freevar_ty(x), s));
            }
            return Set<Type.TyVar>.Empty;
        }

        public static Set<Type.TyVar> freevar_tysc(Set<Type.TyVar> tvs, Type ty) {
            return Set.diff(freevar_ty(ty), tvs);
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
            if (eqs.Value.Type1 is Type.TyCons && eqs.Value.Type2 is Type.TyCons) {
                var f1 = (Type.TyCons)eqs.Value.Type1;
                var f2 = (Type.TyCons)eqs.Value.Type2;
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
                var ty1 = f1.ItemType;
                var f2 = (Type.TyTuple)eqs.Value.Type2;
                var ty2 = f2.ItemType;
                if (ty1.Length != ty2.Length) {
                    throw new Exception.TypingException("Type missmatch");
                }
                var eqs3 = LinkedList.Concat(
                LinkedList.Create(
                ty1.Zip(ty2, (x, y) => new TypeEquality(x, y))
                .ToArray()),
                eqs.Next
                );
                return Unify(eqs3);
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
                if (Set.member(v1, freevar_ty(ty)) != false) {
                    throw new Exception.TypingException("Recursive type");
                }
                var eqs2 = LinkedList.Create(new TypeSubst(v1, ty));
                return LinkedList.Concat(eqs2, Unify(subst_eqs(eqs2, rest)));
            }
            if (eqs.Value.Type2 is Type.TyVar) {
                var v1 = (Type.TyVar)eqs.Value.Type2;
                var ty = eqs.Value.Type1;
                var rest = eqs.Next;
                if (Set.member(v1, freevar_ty(ty)) != false) {
                    throw new Exception.TypingException("Recursive type");
                }
                var eqs2 = LinkedList.Create(new TypeSubst(v1, ty));
                return LinkedList.Concat(eqs2, Unify(subst_eqs(eqs2, rest)));
            }
            throw new Exception.TypingException($"Cannot unify type: {eqs.Value.Type1} and {eqs.Value.Type2}");
        }

        /// <summary>
        ///     組み込み演算子式の結果型
        /// </summary>
        /// <param name="op"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        private static Tuple<Type, LinkedList<TypeEquality>> EvalBuiltinExpressions(Expressions.BuiltinOp.Kind op,
            Type[] args) {
            switch (op) {
                case Expressions.BuiltinOp.Kind.UnaryMinus: {
                    if (args.Length == 1) {
                        return Tuple.Create(
                            (Type)new Type.TyInt(),
                            LinkedList.Create(args.Select(x => new TypeEquality(x, new Type.TyInt())).ToArray())
                        );
                    }
                    throw new Exception.InvalidArgumentNumException("Argument num must be 1: -");
                }
                case Expressions.BuiltinOp.Kind.UnaryPlus: {
                    if (args.Length == 1) {
                        return Tuple.Create(
                            (Type)new Type.TyInt(),
                            LinkedList.Create(args.Select(x => new TypeEquality(x, new Type.TyInt())).ToArray())
                        );
                    }
                    throw new Exception.InvalidArgumentNumException("Argument num must be 2: +");
                }
                case Expressions.BuiltinOp.Kind.Plus: {
                    if (args.Length == 2) {
                        return Tuple.Create(
                            (Type)new Type.TyInt(),
                            LinkedList.Create(args.Select(x => new TypeEquality(x, new Type.TyInt())).ToArray())
                        );
                    }
                    throw new Exception.InvalidArgumentNumException("Argument num must be 2: +");
                }
                case Expressions.BuiltinOp.Kind.Minus: {
                    if (args.Length == 2) {
                        return Tuple.Create(
                            (Type)new Type.TyInt(),
                            LinkedList.Create(args.Select(x => new TypeEquality(x, new Type.TyInt())).ToArray())
                        );
                    }
                    throw new Exception.InvalidArgumentNumException("Argument num must be 2: -");
                }
                case Expressions.BuiltinOp.Kind.Mult: {
                        if (args.Length == 2) {
                            return Tuple.Create(
                                (Type)new Type.TyInt(),
                                LinkedList.Create(args.Select(x => new TypeEquality(x, new Type.TyInt())).ToArray())
                            );
                        }
                        throw new Exception.InvalidArgumentNumException("Argument num must be 2: *");
                    }
                case Expressions.BuiltinOp.Kind.Div: {
                        if (args.Length == 2) {
                            return Tuple.Create(
                                (Type)new Type.TyInt(),
                                LinkedList.Create(args.Select(x => new TypeEquality(x, new Type.TyInt())).ToArray())
                            );
                        }
                        throw new Exception.InvalidArgumentNumException("Argument num must be 2: /");
                    }
                case Expressions.BuiltinOp.Kind.Lt: {
                        if (args.Length == 2) {
                            return Tuple.Create(
                                (Type)new Type.TyBool(),
                                LinkedList.Create(args.Select(x => new TypeEquality(x, new Type.TyInt())).ToArray())
                            );
                        }
                        throw new Exception.InvalidArgumentNumException("Argument num must be 2: <");
                    }
                case Expressions.BuiltinOp.Kind.Le: {
                        if (args.Length == 2) {
                            return Tuple.Create(
                                (Type)new Type.TyBool(),
                                LinkedList.Create(args.Select(x => new TypeEquality(x, new Type.TyInt())).ToArray())
                            );
                        }
                        throw new Exception.InvalidArgumentNumException("Argument num must be 2: <=");
                    }
                case Expressions.BuiltinOp.Kind.Gt: {
                        if (args.Length == 2) {
                            return Tuple.Create(
                                (Type)new Type.TyBool(),
                                LinkedList.Create(args.Select(x => new TypeEquality(x, new Type.TyInt())).ToArray())
                            );
                        }
                        throw new Exception.InvalidArgumentNumException("Argument num must be 2: >");
                    }
                case Expressions.BuiltinOp.Kind.Ge: {
                        if (args.Length == 2) {
                            return Tuple.Create(
                                (Type)new Type.TyBool(),
                                LinkedList.Create(args.Select(x => new TypeEquality(x, new Type.TyInt())).ToArray())
                            );
                        }
                        throw new Exception.InvalidArgumentNumException("Argument num must be 2: >=");
                    }
                case Expressions.BuiltinOp.Kind.Eq: {
                        if (args.Length == 2) {
                            return Tuple.Create(
                                (Type)new Type.TyBool(),
                                LinkedList.Create(new TypeEquality(args[0], args[1]))
                            );
                        }
                        throw new Exception.InvalidArgumentNumException("Argument num must be 2: =");
                    }
                case Expressions.BuiltinOp.Kind.Ne: {
                        if (args.Length == 2) {
                            return Tuple.Create(
                                (Type)new Type.TyBool(),
                                LinkedList.Create(new TypeEquality(args[0], args[1]))
                            );
                        }
                        throw new Exception.InvalidArgumentNumException("Argument num must be 2: <>");
                    }
                case Expressions.BuiltinOp.Kind.ColCol: {
                        if (args.Length == 2) {
                            return Tuple.Create(
                                (Type)new Type.TyCons(args[0]),
                                LinkedList.Create(new TypeEquality(new Type.TyCons(args[0]), args[1]))
                            );
                        }
                        throw new Exception.InvalidArgumentNumException("Argument num must be 2: ::");
                    }
                case Expressions.BuiltinOp.Kind.Head:
                case Expressions.BuiltinOp.Kind.Tail:
                case Expressions.BuiltinOp.Kind.IsCons:
                case Expressions.BuiltinOp.Kind.Nth:
                case Expressions.BuiltinOp.Kind.IsTuple:
                case Expressions.BuiltinOp.Kind.Length:
                    throw new NotSupportedException();
                default:
                    throw new ArgumentOutOfRangeException(nameof(op), op, null);
            }
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
                        LinkedList.Create(new TypeEquality(value, Type.TyCons.Empty)),
                        new Dictionary<string, Type>()
                    );
                } else {
                    var tyitem = Type.TyVar.Fresh();
                    var ret1 = EvalPatternExpressions(p.Value, tyitem);
                    return Tuple.Create(
                        LinkedList.Concat(
                            LinkedList.Create( new TypeEquality(value, new Type.TyCons(tyitem)) ),
                            ret1.Item1
                        ),
                        ret1.Item2
                    );
                }
            }
            if (pattern is PatternExpressions.TupleP) {
                var p = (PatternExpressions.TupleP)pattern;

                var members = p.Value.Select(x => Tuple.Create(x, Type.TyVar.Fresh())).ToArray();
                var tupleType = new Type.TyTuple(members.Select(x => (Type)x.Item2).ToArray());

                var eqs = LinkedList.Create(new TypeEquality(value, tupleType));
                var binds = new Dictionary<string, Type>();
                foreach (var ptv in members) {
                    var patexpr = ptv.Item1;
                    var pattyvar = ptv.Item2;

                    var ret = EvalPatternExpressions(patexpr, pattyvar);
                    var pateqs = ret.Item1;
                    var patbind = ret.Item2;

                    eqs = LinkedList.Concat(eqs, pateqs);
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
