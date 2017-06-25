using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;

namespace MiniMAL {
    public class Typing {
        public abstract class Type
        {
            public class TyInt : Type
            {
                public override string ToString()
                {
                    return "int";
                }
            }

            public class TyBool : Type
            {
                public override string ToString()
                {
                    return "bool";
                }
            }

            public class TyStr : Type
            {
                public override string ToString()
                {
                    return "string";
                }
            }

            public class TyUnit : Type
            {
                public override string ToString()
                {
                    return "unit";
                }
            }

            public class TyNil : Type {
                public override string ToString() {
                    return "nil";
                }
            }

            public class TyVar : Type
            {
                private static int counter = 0;
                public int Id { get; }

                public static TyVar Fresh() {
                    return new TyVar(counter++);
                }

                public TyVar(int id) {
                    this.Id = id;
                }

                public override string ToString() {
                    return "'a";
                }
            }

            public class TyFunc : Type
            {
                public Type ArgType { get; }
                public Type RetType { get; }

                public TyFunc(Type argType, Type retType) {
                    this.ArgType = argType;
                    this.RetType = retType;
                }

                public override string ToString()
                {
                    return $"{ArgType} -> {RetType}";
                }
            }

            public class TyCons : Type
            {
                public Type ItemType { get; }
                public static TyCons Empty { get; } = new TyCons(null);

                public TyCons(Type itemType)
                {
                    ItemType = itemType;
                }

                public override string ToString()
                {
                    return $"{ItemType} list";
                }
            }

            public class TyTuple : Type
            {
                public Type[] ItemType { get; }

                public TyTuple(Type[] itemType)
                {
                    ItemType = itemType;
                }

                public override string ToString()
                {
                    return $"({string.Join(" * ", ItemType.Select(x => x.ToString()))})";
                }
            }

            /// <summary>
            /// 比較
            /// </summary>
            /// <param name="arg1"></param>
            /// <param name="arg2"></param>
            /// <returns></returns>
            public static bool Equals(Type arg1, Type arg2)
            {
                if (arg1 is Type.TyVar && arg2 is Type.TyVar) {
                    return (arg1 as Type.TyVar).Id == (arg2 as Type.TyVar).Id;
                }
                if (arg1 is Type.TyInt && arg2 is Type.TyInt) {
                    return true;
                }
                if (arg1 is Type.TyStr && arg2 is Type.TyStr)
                {
                    return true;
                }
                if (arg1 is Type.TyBool && arg2 is Type.TyBool)
                {
                    return true;
                }
                if (arg1 is Type.TyUnit && arg2 is Type.TyUnit)
                {
                    return true;
                }
                if (arg1 is Type.TyNil && arg2 is Type.TyNil)
                {
                    return true;
                }
                if (arg1 is Type.TyFunc && arg2 is Type.TyFunc)
                {
                    var i1 = (arg1 as Type.TyFunc);
                    var i2 = (arg2 as Type.TyFunc);
                    return Equals(i1.ArgType, i2.ArgType) && Equals(i1.RetType, i2.RetType);
                }
                if (arg1 is Type.TyCons && arg2 is Type.TyCons)
                {
                    var i1 = ((Type.TyCons) arg1);
                    var i2 = ((Type.TyCons) arg2);
                    if (i1 == TyCons.Empty)
                    {
                        return (i2 == TyCons.Empty);
                    }
                    else if (i2 == TyCons.Empty)
                    {
                        return (i1 == TyCons.Empty);
                    }
                    return Equals(i1.ItemType, i2.ItemType);
                }
                if (arg1 is Type.TyTuple && arg2 is Type.TyTuple)
                {
                    var i1 = ((Type.TyTuple) arg1);
                    var i2 = ((Type.TyTuple) arg2);
                    if (i1.ItemType.Length != i2.ItemType.Length)
                    {
                        return false;
                    }

                    return i1.ItemType.Zip(i2.ItemType, Tuple.Create).All(x => Equals(x.Item1, x.Item2));
                }
                return (false);
            }

            public override bool Equals(object obj)
            {
                if (obj == null)
                {
                    return false;
                }
                if (!(obj is Type))
                {
                    return false;
                }
                return Equals(this, obj as Type);
            }
        }

        /// <summary>
            /// 型等式
            /// </summary>
        public class TypeEquality {
            public Type Type1 { get; }
            public Type Type2 { get; }

            public TypeEquality(Type t1, Type t2) {
                Type1 = t1;
                Type2 = t2;
            }
        }

        /// <summary>
        /// 型置換(
        /// </summary>
        public class TypeSubst {
            public Type.TyVar Var { get; }
            public Type Type { get; }

            public TypeSubst(Type.TyVar tyvar, Type ty) {
                Var = tyvar;
                Type = ty;
            }
        }

        private static Type resolve_type(LinkedList<TypeSubst> s, Type typ)
        {
            if (typ is Type.TyVar) {
                var ret = LinkedList.First(x => Type.Equals(x.Var, typ), s);
                if (ret == null) {
                    return typ;
                } else {
                    return ret.Type;
                }
            }
            if (typ is Type.TyFunc) {
                var ty1 = (typ as Type.TyFunc).ArgType;
                var ty2 = (typ as Type.TyFunc).RetType;
                return new Type.TyFunc(resolve_type(s, ty1), resolve_type(s, ty2));
            }
            if (typ is Type.TyCons) {
                var ty1 = (typ as Type.TyCons).ItemType;
                return new Type.TyCons(resolve_type(s, ty1));
            }
            if (typ is Type.TyTuple) {
                var ty1 = (typ as Type.TyTuple).ItemType;
                return new Type.TyTuple(ty1.Select(x => resolve_type(s, x)).ToArray());
            }
            return typ;
        }

        public static LinkedList<TypeSubst> resolve_subst(LinkedList<TypeSubst> s)
        {
            if (s == LinkedList<TypeSubst>.Empty)
            {
                return LinkedList<TypeSubst>.Empty;
            }
            else
            {
                var x = s.Value;
                var xs = s.Next;
                var id = x.Var;
                var typ = x.Type;
                var new_subst = resolve_subst(xs);
                return LinkedList.Extend(new TypeSubst(id, resolve_type(new_subst, typ)), new_subst);
            }
        }

        public static  Type subst_type(LinkedList<TypeSubst> s, Type typ)
        {
            return resolve_type(resolve_subst(s), typ);
        }

/* eqs_of_subst : subst -> (ty* ty) list
型代入を型の等式集合に変換*/
        public static LinkedList<TypeEquality> eqs_of_subst(LinkedList<TypeSubst> s)
        {
            return LinkedList.Map(x => new TypeEquality(x.Var, x.Type), s);
        }

/* subst_eqs: subst -> (ty* ty) list -> (ty* ty) list
型の等式集合に型代入を適用*/
        public static LinkedList<TypeEquality> subst_eqs(LinkedList<TypeSubst> s, LinkedList<TypeEquality> eqs)
        {
            return LinkedList.Map(x => new TypeEquality(subst_type(s, x.Type1), subst_type(s,x.Type2)), eqs);
        }

        public static Set<Type.TyVar> freevar_ty(Type ty)
        {
            if (ty is Type.TyVar)
            {
                return Set.singleton(ty as Type.TyVar);
            }
            if (ty is Type.TyFunc)
            {
                var f = ty as Type.TyFunc;
                var ty1 = f.ArgType;
                var ty2 = f.RetType;
                return Set.union(freevar_ty(ty1), freevar_ty(ty2));
            }
            return Set<Type.TyVar>.Empty;
        }

        public static LinkedList<TypeSubst> unify(LinkedList<TypeEquality> eqs) {
            if (eqs == LinkedList<TypeEquality>.Empty) { return LinkedList<TypeSubst>.Empty; }
            if (eqs.Value.Type1 is Type.TyInt && eqs.Value.Type2 is Type.TyInt) { return unify(eqs.Next); }
            if (eqs.Value.Type1 is Type.TyBool && eqs.Value.Type2 is Type.TyBool) { return unify(eqs.Next); }
            if (eqs.Value.Type1 is Type.TyStr && eqs.Value.Type2 is Type.TyStr) { return unify(eqs.Next); }
            if (eqs.Value.Type1 is Type.TyUnit && eqs.Value.Type2 is Type.TyUnit) { return unify(eqs.Next); }
            if (eqs.Value.Type1 is Type.TyNil && eqs.Value.Type2 is Type.TyNil) { return unify(eqs.Next); }
            if (eqs.Value.Type1 is Type.TyFunc && eqs.Value.Type2 is Type.TyFunc) {
                var f1 = eqs.Value.Type1 as Type.TyFunc;
                var ty11 = f1.ArgType;
                var ty12 = f1.RetType;
                var f2 = eqs.Value.Type2 as Type.TyFunc;
                var ty21 = f2.ArgType;
                var ty22 = f2.RetType;
                return unify(LinkedList.Concat(LinkedList.Create(new TypeEquality(ty12, ty22), new TypeEquality(ty11, ty21)), eqs.Next));
            }
            if (eqs.Value.Type1 is Type.TyVar && eqs.Value.Type2 is Type.TyVar) {
                var v1 = eqs.Value.Type1 as Type.TyVar;
                var v2 = eqs.Value.Type2 as Type.TyVar;
                if (v1.Id == v2.Id) {
                    return unify(eqs.Next);
                } else {
                    var neweqs = LinkedList.Create(new TypeSubst(v1, v2));
                    return LinkedList.Concat(neweqs, unify(subst_eqs(neweqs, eqs.Next)));
                }
            }
            if (eqs.Value.Type1 is Type.TyVar) {
                var v1 = eqs.Value.Type1 as Type.TyVar;
                var ty = eqs.Value.Type2;
                var rest = eqs.Next;
                if (Set.member(v1, freevar_ty(ty)) != null) {
                    throw new Exception("type err");
                } else {
                    var eqs2 = LinkedList.Create(new TypeSubst(v1, ty));
                    return LinkedList.Concat(eqs2, unify(subst_eqs(eqs2, rest)));
                }
            }
            if (eqs.Value.Type2 is Type.TyVar) {
                var v1 = eqs.Value.Type2 as Type.TyVar;
                var ty = eqs.Value.Type1;
                var rest = eqs.Next;
                if (Set.member(v1, freevar_ty(ty)) != null) {
                    throw new Exception("type err");
                } else {
                    var eqs2 = LinkedList.Create(new TypeSubst(v1, ty));
                    return LinkedList.Concat(eqs2, unify(subst_eqs(eqs2, rest)));
                }
            }
            throw new Exception("type err");
        }


    /// <summary>
    /// 評価結果
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
        /// 二項演算子式の結果型
        /// </summary>
        /// <param name="op"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        private static Tuple<Type, LinkedList<TypeEquality>> EvalBuiltinExpressions(Expressions.BuiltinOp.Kind op, Type[] args) {
            switch (op) {
                case Expressions.BuiltinOp.Kind.Plus: {
                    if (args.Length == 2) {
                        return Tuple.Create(
                            (Type)new Type.TyInt(),
                            LinkedList.Create(args.Select(x => new TypeEquality(x,new Type.TyInt())).ToArray())
                        );
                    }
                    throw new Exception("Both arguments must be integer: +");

                }
                case Expressions.BuiltinOp.Kind.Minus: {
                    if (args.Length == 2) {
                        return Tuple.Create(
                            (Type)new Type.TyInt(),
                            LinkedList.Create(args.Select(x => new TypeEquality(x, new Type.TyInt())).ToArray())
                        );
                    }
                        throw new Exception("Both arguments must be integer: -");

                }
                case Expressions.BuiltinOp.Kind.Mult: {
                    if (args.Length == 2) {
                        return Tuple.Create(
                            (Type)new Type.TyInt(),
                            LinkedList.Create(args.Select(x => new TypeEquality(x, new Type.TyInt())).ToArray())
                        );
                    }
                        throw new Exception("Both arguments must be integer: *");
                }
                case Expressions.BuiltinOp.Kind.Div: {
                    if (args.Length == 2) {
                        return Tuple.Create(
                            (Type)new Type.TyInt(),
                            LinkedList.Create(args.Select(x => new TypeEquality(x, new Type.TyInt())).ToArray())
                        );
                    }
                        throw new Exception("Both arguments must be integer: /");
                }
                case Expressions.BuiltinOp.Kind.Lt: {
                    if (args.Length == 2) {
                        return Tuple.Create(
                            (Type)new Type.TyBool(),
                            LinkedList.Create(args.Select(x => new TypeEquality(x, new Type.TyInt())).ToArray())
                        );
                    }
                        throw new Exception("Both arguments must be integer: <");
                }
                case Expressions.BuiltinOp.Kind.Le: {
                    if (args.Length == 2) {
                        return Tuple.Create(
                            (Type)new Type.TyBool(),
                            LinkedList.Create(args.Select(x => new TypeEquality(x, new Type.TyInt())).ToArray())
                        );
                    }
                        throw new Exception("Both arguments must be integer: <=");
                }
                case Expressions.BuiltinOp.Kind.Gt: {
                    if (args.Length == 2) {
                        return Tuple.Create(
                            (Type)new Type.TyBool(),
                            LinkedList.Create(args.Select(x => new TypeEquality(x, new Type.TyInt())).ToArray())
                        );
                    }
                        throw new Exception("Both arguments must be integer: >");
                }
                case Expressions.BuiltinOp.Kind.Ge: {
                    if (args.Length == 2) {
                        return Tuple.Create(
                            (Type)new Type.TyBool(),
                            LinkedList.Create(args.Select(x => new TypeEquality(x, new Type.TyInt())).ToArray())
                        );
                    }
                        throw new Exception("Both arguments must be integer: >=");
                }
                case Expressions.BuiltinOp.Kind.Eq: {
                    if (args.Length == 2) {
                        return Tuple.Create(
                            (Type)new Type.TyBool(),
                            LinkedList.Create(new TypeEquality(args[0], args[1]))
                        );
                    }
                    throw new Exception("Both arguments must be same type: =");
                }
                case Expressions.BuiltinOp.Kind.Ne: {
                    if (args.Length == 2) {
                        return Tuple.Create(
                            (Type)new Type.TyBool(),
                            LinkedList.Create(new TypeEquality(args[0], args[1]))
                        );
                    }
                    throw new Exception("Both arguments must be same type: <>");
                }
                case Expressions.BuiltinOp.Kind.ColCol: {
                    if (args.Length == 2) {
                        return Tuple.Create(
                            (Type)new Type.TyCons(args[0]),
                            LinkedList.Create(new TypeEquality(new Type.TyCons(args[0]), args[1]))
                        );
                    }
                    throw new Exception("Both arguments must be same type: ::");
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
        /// パターンマッチの評価
        /// </summary>
        /// <param name="env"></param>
        /// <param name="pattern"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private static Tuple<LinkedList<TypeEquality>, Dictionary<string, Type>,Type> EvalPatternExpressions(Environment<Type> env, PatternExpressions pattern, Type value) {
            if (pattern is PatternExpressions.WildP)
            {
                var tyvar = Type.TyVar.Fresh();
                return Tuple.Create<LinkedList<TypeEquality>, Dictionary<string, Type>, Type>(
                    LinkedList.Create(new TypeEquality(tyvar, value)),
                    new Dictionary<string, Type>(),
                    tyvar
                );
            }
            if (pattern is PatternExpressions.IntP) {
                var tyvar = new Type.TyInt();
                return Tuple.Create<LinkedList<TypeEquality>, Dictionary<string, Type>, Type>(
                    LinkedList.Create(new TypeEquality(tyvar, value)),
                    new Dictionary<string, Type>(),
                    tyvar
                );
            }
            if (pattern is PatternExpressions.StrP) {
                var tyvar = new Type.TyStr();
                return Tuple.Create<LinkedList<TypeEquality>, Dictionary<string, Type>, Type>(
                    LinkedList.Create(new TypeEquality(tyvar, value)),
                    new Dictionary<string, Type>(),
                    tyvar
                );
            }
            if (pattern is PatternExpressions.BoolP) {
                var tyvar = new Type.TyBool();
                return Tuple.Create<LinkedList<TypeEquality>, Dictionary<string, Type>, Type>(
                    LinkedList.Create(new TypeEquality(tyvar, value)),
                    new Dictionary<string, Type>(),
                    tyvar
                );
            }
            if (pattern is PatternExpressions.UnitP) {
                var tyvar = new Type.TyUnit();
                return Tuple.Create<LinkedList<TypeEquality>, Dictionary<string, Type>, Type>(
                    LinkedList.Create(new TypeEquality(tyvar, value)),
                    new Dictionary<string, Type>(),
                    tyvar
                );
            }
            if (pattern is PatternExpressions.VarP) {
                var tyvar = Type.TyVar.Fresh();
                return Tuple.Create<LinkedList<TypeEquality>, Dictionary<string, Type>, Type>(
                    LinkedList.Create(new TypeEquality(tyvar, value)),
                    new Dictionary<string, Type>() { { ((PatternExpressions.VarP)pattern).Id, tyvar } },
                    tyvar
                );
            }
            if (pattern is PatternExpressions.ConsP) {
                var p = pattern as PatternExpressions.ConsP;
                if (p == PatternExpressions.ConsP.Empty)
                {
                    var tyvar = Type.TyCons.Empty;
                    return Tuple.Create<LinkedList<TypeEquality>, Dictionary<string, Type>, Type>(
                        LinkedList.Create(new TypeEquality(tyvar, value)),
                        new Dictionary<string, Type>(),
                        tyvar
                    );
                } else
                {
                    var tyvar = Type.TyVar.Fresh();
                    var tyitem = Type.TyVar.Fresh();
                    var ret1 = EvalPatternExpressions(env, p.Value, tyitem);
                    return Tuple.Create<LinkedList<TypeEquality>, Dictionary<string, Type>, Type>(
                        LinkedList.Concat(
                            LinkedList.Create(
                                new TypeEquality(tyvar, value), 
                                new TypeEquality(tyvar, new Type.TyCons(tyitem))
                            ),
                            ret1.Item1
                        ),
                        ret1.Item2,
                        tyvar
                    );

                }
            }
            if (pattern is PatternExpressions.TupleP) {
                var p = pattern as PatternExpressions.TupleP;

                var tyvar = Type.TyVar.Fresh();
                var itemtypes = new List<Type>();
                var te = LinkedList<TypeEquality>.Empty;
                var dic = new Dictionary<string, Type>();

                foreach (var pat in p.Value)
                {
                    var tyitem = Type.TyVar.Fresh();
                    var ret1 = EvalPatternExpressions(env, pat, tyitem);
                    te = LinkedList.Concat(ret1.Item1, te);
                    itemtypes.Add(tyitem);
                    dic = ret1.Item2.Aggregate(dic, (s, x) => { s[x.Key] = x.Value; return s; });
                }
                
                return Tuple.Create<LinkedList<TypeEquality>, Dictionary<string, Type>, Type>(
                    LinkedList.Extend(
                        new TypeEquality(tyvar, new Type.TyTuple(itemtypes.ToArray())),
                        te
                    ),
                    dic,
                    tyvar
                );

            }
            return null;
        }

        /// <summary>
        /// 式の評価
        /// </summary>
        /// <param name="env"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        private static Tuple<LinkedList<TypeSubst>, Type>  EvalExpressions(Environment<Type> env, Expressions e) {
            if (e is Expressions.Var) {
                var x = ((Expressions.Var)e).Id;
                try {
                    return Tuple.Create(
                        LinkedList<TypeSubst>.Empty,
                        Environment.LookUp(x, env)
                    );
                } catch (Environment.NotBound) {
                    throw new Exception($"Variable not bound: {x}");
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
                    (Type)Type.TyCons.Empty
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
                var ss = LinkedList.Concat(LinkedList.Concat(args.Select(x => eqs_of_subst(x.Item1)).ToArray()), ret.Item2);
                var eqs = unify(ss);
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
                    eqs_of_subst(cond.Item1),
                    eqs_of_subst(then.Item1),
                    eqs_of_subst(@else.Item1),
                    LinkedList.Create(new TypeEquality(then.Item2, @else.Item2))
                );
                var eqs = unify(s);
                return Tuple.Create(
                    eqs,
                    subst_type(eqs, then.Item2)
                );
            }
            if (e is Expressions.LetExp)
            {
                var exp = (Expressions.LetExp)e;

                var newenv = env;
                var eqs = LinkedList<TypeEquality>.Empty;
                foreach (var bind in exp.Binds) {
                    var v = EvalExpressions(env, bind.Item2);
                    var s  = v.Item1;
                    var ty = v.Item2;
                    eqs = LinkedList.Concat(eqs_of_subst(s), eqs);
                    newenv = Environment.Extend(bind.Item1, ty, newenv);
                }

                { 
                    var v = EvalExpressions(newenv, exp.Body);
                    var s = v.Item1;
                    var ty = v.Item2;
                    eqs = LinkedList.Concat(eqs_of_subst(s), eqs);

                    var s3 = unify(eqs);
                    return Tuple.Create(
                        s3,
                        subst_type(s3, ty)
                    );
                }
            }
            if (e is Expressions.LetRecExp) {
                var exp = (Expressions.LetExp)e;

                var newenv = env;
                var eqs = LinkedList<TypeEquality>.Empty;
                var binds = exp.Binds.Select(x => Tuple.Create(x.Item1, x.Item2, Type.TyVar.Fresh())).ToArray();
                foreach (var bind in binds) {
                    newenv = Environment.Extend(bind.Item1, bind.Item3, newenv);
                }

                foreach (var bind in binds) {
                    var v = EvalExpressions(newenv, bind.Item2);
                    var s = v.Item1;
                    var ty = v.Item2;
                    eqs = LinkedList.Concat(LinkedList.Create(new TypeEquality(ty, bind.Item3)), eqs_of_subst(s), eqs);
                }

                {
                    var v = EvalExpressions(newenv, exp.Body);
                    var s = v.Item1;
                    var ty = v.Item2;
                    eqs = LinkedList.Concat(eqs_of_subst(s), eqs);

                    var s3 = unify(eqs);
                    return Tuple.Create(
                        s3,
                        subst_type(s3, ty)
                    );
                }
            }
            if (e is Expressions.FunExp)
            {
                var exp = e as Expressions.FunExp;
                var domty = Type.TyVar.Fresh();
                var v = EvalExpressions(Environment.Extend(exp.Arg, domty, env), exp.Body);
                return Tuple.Create(
                    v.Item1,
                    (Type)new Type.TyFunc(subst_type(v.Item1, domty),v.Item2)
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
                    LinkedList.Create(new TypeEquality(ty1, new Type.TyFunc(ty2, domty))),
                    eqs_of_subst(s1),
                    eqs_of_subst(s2)
                );
                var s3 = unify(eqs);
                return Tuple.Create(
                    s3,
                    subst_type(s3, domty)
                );
            }
            if (e is Expressions.MatchExp)
            {
                var exp = e as Expressions.MatchExp;
                var domty = Type.TyVar.Fresh();

                var v1 = EvalExpressions(env, exp.Exp);
                var s1 = v1.Item1;
                var ty1 = v1.Item2;

                var eqs = eqs_of_subst(s1);
                foreach (var pattern in exp.Patterns) {
                    var p1 = EvalPatternExpressions(env, pattern.Item1, ty1);
                    var b1 = EvalExpressions(p1.Item2.Aggregate(env, (s,x) => Environment.Extend(x.Key, x.Value, s)), pattern.Item2);
                    eqs = LinkedList.Concat(LinkedList.Create(new TypeEquality(domty, b1.Item2)), p1.Item1, eqs_of_subst(b1.Item1), eqs);
                };
                var s3 = unify(eqs);

                return Tuple.Create(
                    s3,
                    subst_type(s3, domty)
                );
            }
            if (e is Expressions.TupleExp)
            {
                var exp = e as Expressions.TupleExp;
                var mems = exp.Exprs.Select(x => EvalExpressions(env, x)).ToArray();
                var ss = mems.Aggregate(LinkedList<TypeEquality>.Empty, (s, x) => LinkedList.Concat(eqs_of_subst(x.Item1), s));
                var ty = mems.Select(x => x.Item2).ToArray();
                var eqs = unify(ss);

                return Tuple.Create(
                    eqs,
                    (Type)new Type.TyTuple(ty.Select(x => subst_type(eqs, x)).ToArray())
                );
            }
            if (e is Expressions.NilLit) {
                return Tuple.Create(
                    LinkedList<TypeSubst>.Empty,
                    (Type)new Type.TyNil()
                );
            }
            if (e is Expressions.HaltExp) {
                throw new Exception((e as Expressions.HaltExp).Message);
            }

            throw new NotSupportedException($"expression {e} cannot eval.");
        }

        private static Result eval_declEntry(Environment<Type> env, Declarations.DeclBase p) {
            if (p is Declarations.Decl) {
                var d = (Declarations.Decl)p;

                var newenv = env;
                var eqs = LinkedList<TypeEquality>.Empty;
                var ret = new Result("", newenv, null);
                foreach (var bind in d.Binds) {
                    var v = EvalExpressions(env, bind.Item2);
                    var s = v.Item1;
                    var ty = v.Item2;
                    eqs = LinkedList.Concat(eqs_of_subst(s), eqs);
                    newenv = Environment.Extend(bind.Item1, ty, newenv);
                    ret = new Result(bind.Item1, newenv, ty);
                }

                {
                    var s3 = unify(eqs);
                    return new Result(ret.Id, newenv, subst_type(s3, ret.Value));
                }
            }
            if (p is Declarations.RecDecl) {
                var d = (Declarations.RecDecl)p;

                var newenv = env;
                var eqs = LinkedList<TypeEquality>.Empty;
                var binds = d.Binds.Select(x => Tuple.Create(x.Item1, x.Item2, Type.TyVar.Fresh())).ToArray();
                foreach (var bind in binds) {
                    newenv = Environment.Extend(bind.Item1, bind.Item3, newenv);
                }

                var ret = new Result("", newenv, null);
                foreach (var bind in binds) {
                    var v = EvalExpressions(newenv, bind.Item2);
                    var s = v.Item1;
                    var ty = v.Item2;
                    eqs = LinkedList.Concat(LinkedList.Create(new TypeEquality(ty, bind.Item3)), eqs_of_subst(s), eqs);
                    ret = new Result(bind.Item1, newenv, ty);
                }

                {
                    var s3 = unify(eqs);
                    return new Result(ret.Id, newenv, subst_type(s3, ret.Value));
                }
            }
            throw new NotSupportedException($"{p.GetType().FullName} cannot eval.");
        }

        public static Result eval_decl(Environment<Type> env, Declarations p) {
            if (p is Declarations.Exp) {
                var e = (Declarations.Exp)p;
                var v = EvalExpressions(env, e.Syntax);
                return new Result("-", env, v.Item2);
            }
            if (p is Declarations.Decls) {
                var ds = (Declarations.Decls)p;
                var newenv = env;
                Result ret = new Result("", env, (Type)null);
                foreach (var d in ds.Entries) {
                    ret = eval_declEntry(newenv, d);
                    newenv = ret.Env;
                }
                return ret;
            }
            if (p is Declarations.Empty) {
                return new Result("", env, (Type)null);
            }
            throw new NotSupportedException($"{p.GetType().FullName} cannot eval.");
        }
    }
}
