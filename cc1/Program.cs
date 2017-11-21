using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

// Constraints
// ・Do not use a parser generator such as yacc / lex. 

namespace AnsiCParser {
    class Program {
        static void Main(string[] args) {
            foreach (var arg in args) {
                var grammer = new Grammer();
                string input = System.IO.File.ReadAllText(arg);
                grammer.Parse(input);
            }
        }
    }

    #region Collection Class

    /// <summary>
    /// リスト
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    public class LinkedList<TValue> : IEnumerable<TValue> {
        public TValue Value {
            get;
        }
        public LinkedList<TValue> Parent {
            get;
        }
        public static LinkedList<TValue> Empty { get; } = new LinkedList<TValue>();

        public bool IsEmpty() {
            return this.Parent == null;
        }

        protected LinkedList(TValue value, LinkedList<TValue> parent) {
            this.Value = value;
            this.Parent = parent;
        }
        protected LinkedList() {
            this.Value = default(TValue);
            this.Parent = null;
        }
        public LinkedList<TValue> Extend(TValue value) {
            return new LinkedList<TValue>(value, this);
        }

        public bool Contains(TValue v) {
            return this.Any(x => (v.Equals(x)));
        }

        public IEnumerator<TValue> GetEnumerator() {
            var it = this;
            while (!it.IsEmpty()) {
                yield return it.Value;
                it = it.Parent;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

    }

    /// <summary>
    /// リンク辞書
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public class LinkedDictionary<TKey, TValue> : LinkedList<KeyValuePair<TKey, TValue>> {
        protected LinkedDictionary(TKey key, TValue value, LinkedDictionary<TKey, TValue> parent) : base(new KeyValuePair<TKey, TValue>(key, value), parent) { }
        protected LinkedDictionary() : base() { }
        public LinkedDictionary<TKey, TValue> Extend(TKey key, TValue value) {
            return new LinkedDictionary<TKey, TValue>(key, value, this);
        }
        public static new LinkedDictionary<TKey, TValue> Empty { get; } = new LinkedDictionary<TKey, TValue>();

        public bool ContainsKey(TKey key) {
            return this.Any(x => (key.Equals(x.Key)));
        }
        public bool TryGetValue(TKey key, out TValue value) {
            foreach (var kv in this) {
                if (kv.Key.Equals(key)) {
                    value = kv.Value;
                    return true;
                }
            }
            value = default(TValue);
            return false;
        }
    }

    #endregion

    /// <summary>
    /// 記憶クラス指定子
    /// </summary>
    public enum StorageClass {
        None,
        Auto,
        Register,
        Static,
        Extern,
        Typedef
    }

    /// <summary>
    /// 型指定子
    /// </summary>
    [Flags]
    public enum TypeSpecifier {
        None = 0x0000,
        Void = 0x0001,
        Char = 0x0002,
        Int = 0x0003,
        Float = 0x0004,
        Double = 0x0005,
        TypeMask = 0x000F,
        Short = 0x0010,
        Long = 0x0020,
        LLong = 0x0030,
        SizeMask = 0x00F0,
        Signed = 0x0100,
        Unsigned = 0x0200,
        SignMask = 0x0F00,
        Invalid = 0x1000,
    }

    /// <summary>
    /// 型修飾子
    /// </summary>
    [Flags]
    public enum TypeQualifier {
        None = 0x0000,
        Const = 0x0001,
        Volatile = 0x002,
        Restrict = 0x0004,
        Near = 0x0010,
        Far = 0x0020,
        Invalid = 0x1000,
    }

    /// <summary>
    /// 関数指定子
    /// </summary>
    [Flags]
    public enum FunctionSpecifier {
        None = 0x0000,
        Inline = 0x0001,
    }

    public static class Ext {
        public static StorageClass Marge(this StorageClass self, StorageClass other) {
            if (self == StorageClass.None) {
                return other;
            } else if (other == StorageClass.None) {
                return self;
            } else {
                if (self != other) {
                    throw new Exception("");
                } else {
                    return self;
                }
            }
        }
        public static TypeSpecifier TypeFlag(this TypeSpecifier self) {
            return TypeSpecifier.TypeMask & self;
        }
        public static TypeSpecifier SizeFlag(this TypeSpecifier self) {
            return TypeSpecifier.SizeMask & self;
        }
        public static TypeSpecifier SignFlag(this TypeSpecifier self) {
            return TypeSpecifier.SignMask & self;
        }
        public static TypeSpecifier Marge(this TypeSpecifier self, TypeSpecifier other) {
            TypeSpecifier type = TypeSpecifier.None;

            if (self.TypeFlag() == TypeSpecifier.None) {
                type = other.TypeFlag();
            } else if (other.TypeFlag() == TypeSpecifier.None) {
                type = self.TypeFlag();
            } else if (self.TypeFlag() != other.TypeFlag()) {
                throw new Exception();
            }

            TypeSpecifier size = TypeSpecifier.None;
            if (self.SizeFlag() == TypeSpecifier.None) {
                size = other.SizeFlag();
            } else if (other.SizeFlag() == TypeSpecifier.None) {
                size = self.SizeFlag();
            } else {
                if (self.SizeFlag() == other.SizeFlag()) {
                    if (self.SizeFlag() == TypeSpecifier.Long || self.SizeFlag() == TypeSpecifier.LLong) {
                        size = TypeSpecifier.LLong;
                    }
                } else if (self.SizeFlag() != other.SizeFlag()) {
                    if (self.SizeFlag() == TypeSpecifier.Long && other.SizeFlag() == TypeSpecifier.LLong) {
                        size = TypeSpecifier.LLong;
                    } else if (self.SizeFlag() == TypeSpecifier.LLong && other.SizeFlag() == TypeSpecifier.Long) {
                        size = TypeSpecifier.LLong;
                    } else {
                        throw new Exception();
                    }
                }
            }

            TypeSpecifier sign = TypeSpecifier.None;
            if (self.SignFlag() == TypeSpecifier.None) {
                sign = other.SignFlag();
            } else if (other.SignFlag() == TypeSpecifier.None) {
                sign = self.SignFlag();
            } else if (self.SignFlag() != other.SignFlag()) {
                throw new Exception();
            }

            return type | size | sign;
        }
        public static TypeQualifier Marge(this TypeQualifier self, TypeQualifier other) {
            return self | other;
        }
        public static FunctionSpecifier Marge(this FunctionSpecifier self, FunctionSpecifier other) {
            return self | other;
        }

    }

    public abstract class CType {


        /// <summary>
        /// 型指定を解決する
        /// </summary>
        /// <param name="baseType"></param>
        /// <param name="stack"></param>
        /// <returns></returns>
        public static CType Resolve(CType baseType, List<CType> stack) {
            return stack.Aggregate(baseType, (s, x) => {
                if (x is StubType) {
                    return s;
                } else {
                    x.Fixup(s);
                }
                return x;
            });
        }

        /// <summary>
        /// StubTypeをtyで置き換える。
        /// </summary>
        /// <param name="ty"></param>
        protected virtual void Fixup(CType ty) {
            throw new ApplicationException();
        }

        /// <summary>
        /// 型のサイズを取得
        /// </summary>
        /// <returns></returns>
        public abstract int Sizeof();

        /// <summary>
        /// 暗黙的int型なら真
        /// </summary>
        /// <returns></returns>
        public abstract bool IsImplicit();

        /// <summary>
        /// 関数型なら真
        /// </summary>
        /// <returns></returns>
        public abstract bool IsFunction();

        /// <summary>
        /// 基本型
        /// </summary>
        public class BasicType : CType {
            public TypeSpecifier type_specifier {
                get;
            }

            public BasicType(TypeSpecifier type_specifier) {
                this.type_specifier = type_specifier;
            }

            protected override void Fixup(CType ty) {
                // 基本型なのでFixup不要
            }

            /// <summary>
            /// サイズ取得
            /// </summary>
            /// <returns></returns>
            public override int Sizeof() {
                switch (type_specifier.TypeFlag()) {
                    case TypeSpecifier.Void:
                        throw new Exception();
                    case TypeSpecifier.Char:
                        return 1;
                    case TypeSpecifier.Short:
                        return 2;
                    case TypeSpecifier.Int:
                    case TypeSpecifier.None:
                        switch (type_specifier.SizeFlag()) {
                            case TypeSpecifier.Short:
                                return 2;
                            case TypeSpecifier.None:
                            case TypeSpecifier.Long:
                                return 4;
                            case TypeSpecifier.LLong:
                                return 8;
                            default:
                                throw new Exception();
                        }
                    case TypeSpecifier.Float:
                        return 4;
                    case TypeSpecifier.Double:
                        return type_specifier.SizeFlag() == TypeSpecifier.Long ? 12 : 8;
                    default:
                        throw new Exception();
                }
            }

            public override bool IsImplicit() {
                return type_specifier == TypeSpecifier.None;
            }

            public override bool IsFunction() {
                return false;
            }

            public override string ToString() {
                var sb = new List<string>();

                switch (type_specifier.SignFlag()) {
                    case TypeSpecifier.Signed:
                        sb.Add("signed");
                        break;
                    case TypeSpecifier.Unsigned:
                        sb.Add("unsigned");
                        break;
                    default:
                        break;
                }

                switch (type_specifier.SizeFlag()) {
                    case TypeSpecifier.Short:
                        sb.Add("short");
                        break;
                    case TypeSpecifier.Long:
                        sb.Add("long");
                        break;
                    case TypeSpecifier.LLong:
                        sb.Add("long");
                        sb.Add("long");
                        break;
                    default:
                        break;
                }

                switch (type_specifier.TypeFlag()) {
                    case TypeSpecifier.Void:
                        sb.Add("void");
                        break;
                    case TypeSpecifier.Char:
                        sb.Add("char");
                        break;
                    case TypeSpecifier.Short:
                        sb.Add("short");
                        break;
                    case TypeSpecifier.Int:
                        sb.Add("int");
                        break;
                    case TypeSpecifier.Float:
                        sb.Add("float");
                        break;
                    case TypeSpecifier.Double:
                        sb.Add("double");
                        break;
                    default:
                        break;
                }
                return String.Join(" ", sb);
            }

        }

        /// <summary>
        /// タグ付き型
        /// </summary>
        public abstract class TaggedType : CType {
            public override bool IsImplicit() {
                return false;
            }

            public override bool IsFunction() {
                return false;
            }

            public class StructUnionType : TaggedType {
                public bool IsStruct {
                    get;
                }

                public string Ident {
                    get;
                }

                public bool IsAnonymous {
                    get;
                }

                public List<Tuple<string, CType, AST.Expression>> struct_declarations {
                    get; internal set;
                }

                public StructUnionType(bool isStruct, string ident, bool is_anonymous) {
                    this.IsStruct = isStruct;
                    this.Ident = ident;
                    this.IsAnonymous = is_anonymous;
                }

                protected override void Fixup(CType ty) {
                    for (var i = 0; i < struct_declarations.Count; i++) {
                        var struct_declaration = struct_declarations[i];
                        if (struct_declaration.Item2 is StubType) {
                            struct_declarations[i] = Tuple.Create(struct_declaration.Item1, ty, struct_declaration.Item3);
                        } else {
                            struct_declaration.Item2.Fixup(ty);
                        }
                    }

                    foreach (var struct_declaration in struct_declarations) {
                        struct_declaration.Item2.Fixup(ty);
                    }
                }

                public override int Sizeof() {
                    // ビットフィールドは未実装
                    if (IsStruct) {
                        return struct_declarations.Sum(x => x.Item2.Sizeof());
                    } else {
                        return struct_declarations.Max(x => x.Item2.Sizeof());
                    }
                }
                public override string ToString() {
                    var sb = new List<string>();
                    sb.Add(IsStruct ? "strunct" : "union");
                    sb.Add(Ident);
                    if (struct_declarations != null) {
                        sb.Add("{");
                        sb.AddRange(struct_declarations.SelectMany(x =>
                            new string[] { x.Item2?.ToString(), x.Item1, x.Item3 != null ? ":" : null, x.Item3?.ToString(), ";" }.Where(y => y != null)
                        ));
                        sb.Add("}");
                    }
                    sb.Add(";");
                    return string.Join(" ", sb);
                }
            }

            /// <summary>
            /// 列挙型
            /// </summary>
            public class EnumType : TaggedType {
                public string Ident {
                    get;
                }
                public bool IsAnonymous {
                    get;
                }
                public List<Tuple<string, int>> enumerator_list {
                    get; set;
                }

                public EnumType(string ident, bool isAnonymous) {
                    this.Ident = ident;
                    this.IsAnonymous = isAnonymous;
                }

                protected override void Fixup(CType ty) {
                }

                public override int Sizeof() {
                    return 4;
                }

                public override bool IsImplicit() {
                    return false;
                }
                public override string ToString() {
                    var sb = new List<string>();
                    sb.Add("enum");
                    sb.Add(Ident);
                    if (enumerator_list != null) {
                        sb.Add("{");
                        sb.AddRange(enumerator_list.SelectMany(x =>
                            new string[] { x.Item1, "=", x.Item2.ToString(), "," }
                        ));
                        sb.Add("}");
                    }
                    sb.Add(";");
                    return string.Join(" ", sb);
                }
            }
        }

        /// <summary>
        /// スタブ型（型の解決中でのみ用いる他の型が入る穴）
        /// </summary>
        public class StubType : CType {
            public override int Sizeof() {
                throw new Exception();
            }
            public override bool IsImplicit() {
                throw new Exception();
            }
            public override bool IsFunction() {
                throw new Exception();
            }
            public override string ToString() {
                return "$";
            }
        }

        /// <summary>
        /// 関数型
        /// </summary>
        public class FunctionType : CType {
            /// <summary>
            /// 引数型
            /// </summary>
            public List<Tuple<string, CType>> Arguments {
                get;
            }

            /// <summary>
            /// 戻り値型
            /// </summary>
            public CType ResultType {
                get; private set;
            }

            /// <summary>
            /// 可変長引数の有無
            /// </summary>
            public bool HasVariadic {
                get;
            }

            public FunctionType(List<Tuple<string, CType>> arguments, bool hasVariadic, CType resultType) {
                this.Arguments = arguments;
                this.ResultType = resultType;
                this.HasVariadic = hasVariadic;
            }

            protected override void Fixup(CType ty) {
                if (ResultType is StubType) {
                    ResultType = ty;
                } else {
                    ResultType.Fixup(ty);
                }
            }

            public override int Sizeof() {
                throw new Exception();
            }

            public override bool IsImplicit() {
                return false;
            }

            public override bool IsFunction() {
                return true;
            }
            public override string ToString() {
                var sb = new List<string>();
                sb.Add(ResultType.ToString());
                if (Arguments != null) {
                    sb.Add("(");
                    sb.Add(String.Join(", ", Arguments.Select(x => x.Item2.ToString())));
                    if (HasVariadic) {
                        sb.Add(", ...");
                    }
                    sb.Add(")");
                }
                sb.Add(";");
                return string.Join(" ", sb);
            }
        }

        /// <summary>
        /// ポインタ型
        /// </summary>
        public class PointerType : CType {
            public CType cType {
                get; private set;
            }

            public PointerType(CType cType) {
                this.cType = cType;
            }

            protected override void Fixup(CType ty) {
                if (cType is StubType) {
                    cType = ty;
                } else {
                    cType.Fixup(ty);
                }
            }

            public override int Sizeof() {
                return 4;
            }

            public override bool IsImplicit() {
                return false;
            }

            public override bool IsFunction() {
                return false;
            }
            public override string ToString() {
                return "*" + cType.ToString();
            }
        }

        /// <summary>
        /// 配列型
        /// </summary>
        public class ArrayType : CType {
            /// <summary>
            /// 配列長(-1は指定無し)
            /// </summary>
            public int Length {
                get;
            }

            public CType cType {
                get; private set;
            }

            public ArrayType(int length, CType cType) {
                this.Length = length;
                this.cType = cType;
            }

            protected override void Fixup(CType ty) {
                if (cType is StubType) {
                    cType = ty;
                } else {
                    cType.Fixup(ty);
                }
            }

            public override int Sizeof() {
                return (Length < 0) ? 4 : cType.Sizeof() * (Length);
            }

            public override bool IsImplicit() {
                return false;
            }
            public override bool IsFunction() {
                return false;
            }
            public override string ToString() {
                return cType.ToString() + $"[{Length}]";
            }
        }

        /// <summary>
        /// Typedefされた型
        /// </summary>
        public class TypedefedType : CType {
            public string Ident {
                get;
            }
            public CType cType {
                get;
            }

            public TypedefedType(string ident, CType ctype) {
                this.Ident = ident;
                this.cType = ctype;
            }

            public override int Sizeof() {
                return this.cType.Sizeof();
            }

            public override bool IsImplicit() {
                return false;
            }
            public override bool IsFunction() {
                return false;
            }
            public override string ToString() {
                return Ident;
            }
        }

        /// <summary>
        /// 型修飾子
        /// </summary>
        public class TypeQualifierType : CType {
            public CType cType {
                get; private set;
            }
            public TypeQualifier type_qualifier {
                get;
            }

            public TypeQualifierType(CType baseType, TypeQualifier type_qualifier) {
                this.cType = baseType;
                this.type_qualifier = type_qualifier;
            }

            protected override void Fixup(CType ty) {
                if (cType is StubType) {
                    cType = ty;
                } else {
                    cType.Fixup(ty);
                }
            }

            public override int Sizeof() {
                return this.cType.Sizeof();
            }

            public override bool IsImplicit() {
                return this.cType.IsImplicit();
            }
            public override bool IsFunction() {
                return false;
            }
            public override string ToString() {
                var sb = new List<string>();
                sb.Add((type_qualifier & TypeQualifier.Const) != 0 ? "const" : null);
                sb.Add((type_qualifier & TypeQualifier.Volatile) != 0 ? "volatile" : null);
                sb.Add((type_qualifier & TypeQualifier.Restrict) != 0 ? "restrict" : null);
                sb.Add((type_qualifier & TypeQualifier.Near) != 0 ? "near" : null);
                sb.Add((type_qualifier & TypeQualifier.Far) != 0 ? "far" : null);
                sb.Add(cType.ToString());
                return string.Join(" ", sb.Where(x => x != null));
            }
        }

        /// <summary>
        /// 型が同一型であるかどうかを比較する
        /// </summary>
        /// <param name="t1"></param>
        /// <param name="t2"></param>
        /// <returns></returns>
        public static bool EqualType(CType t1, CType t2) {
            for (; ; ) {
                if (ReferenceEquals(t1, t2)) {
                    return true;
                }
                if (t1 is CType.TypedefedType || t2 is CType.TypedefedType) {
                    if (t1 is CType.TypedefedType) {
                        t1 = (t1 as CType.TypedefedType).cType;
                    }
                    if (t2 is CType.TypedefedType) {
                        t2 = (t2 as CType.TypedefedType).cType;
                    }
                    continue;
                }
                if (t1.GetType() != t2.GetType()) {
                    return false;
                }
                if (t1 is CType.TypeQualifierType && t2 is CType.TypeQualifierType) {
                    if ((t1 as CType.TypeQualifierType).type_qualifier != (t2 as CType.TypeQualifierType).type_qualifier) {
                        return false;
                    }
                    t1 = (t1 as CType.TypeQualifierType).cType;
                    t2 = (t2 as CType.TypeQualifierType).cType;
                    continue;
                }
                if (t1 is CType.PointerType && t2 is CType.PointerType) {
                    t1 = (t1 as CType.PointerType).cType;
                    t2 = (t2 as CType.PointerType).cType;
                    continue;
                }
                if (t1 is CType.ArrayType && t2 is CType.ArrayType) {
                    if ((t1 as CType.ArrayType).Length != (t2 as CType.ArrayType).Length) {
                        return false;
                    }
                    t1 = (t1 as CType.ArrayType).cType;
                    t2 = (t2 as CType.ArrayType).cType;
                    continue;
                }
                if (t1 is CType.FunctionType && t2 is CType.FunctionType) {
                    if ((t1 as CType.FunctionType).Arguments.Count != (t2 as CType.FunctionType).Arguments.Count) {
                        return false;
                    }
                    if ((t1 as CType.FunctionType).HasVariadic != (t2 as CType.FunctionType).HasVariadic) {
                        return false;
                    }
                    if ((t1 as CType.FunctionType).Arguments.Zip((t2 as CType.FunctionType).Arguments, (x, y) => EqualType(x.Item2, y.Item2)).All(x => x) == false) {
                        return false;
                    }
                    t1 = (t1 as CType.FunctionType).ResultType;
                    t2 = (t2 as CType.FunctionType).ResultType;
                    continue;
                }
                if (t1 is CType.StubType && t2 is CType.StubType) {
                    throw new Exception();
                }
                if (t1 is CType.TaggedType.StructUnionType && t2 is CType.TaggedType.StructUnionType) {
                    if ((t1 as CType.TaggedType.StructUnionType).IsStruct != (t2 as CType.TaggedType.StructUnionType).IsStruct) {
                        return false;
                    }
                    if ((t1 as CType.TaggedType.StructUnionType).IsAnonymous != (t2 as CType.TaggedType.StructUnionType).IsAnonymous) {
                        return false;
                    }
                    if ((t1 as CType.TaggedType.StructUnionType).Ident != (t2 as CType.TaggedType.StructUnionType).Ident) {
                        return false;
                    }
                    if ((t1 as CType.TaggedType.StructUnionType).struct_declarations.Count != (t2 as CType.TaggedType.StructUnionType).struct_declarations.Count) {
                        return false;
                    }
                    if ((t1 as CType.TaggedType.StructUnionType).struct_declarations.Zip((t2 as CType.TaggedType.StructUnionType).struct_declarations, (x, y) => EqualType(x.Item2, y.Item2)).All(x => x) == false) {
                        return false;
                    }
                    return true;
                }
                if (t1 is CType.BasicType && t2 is CType.BasicType) {
                    if ((t1 as CType.BasicType).type_specifier != (t2 as CType.BasicType).type_specifier) {
                        return false;
                    }
                    return true;
                }
                throw new Exception();
            }
        }

        public class ExpressionValue {

        }
    }

    public abstract class AST {
        public static int ConstantEval(AST.Expression expr) {
            if (expr is AST.Expression.AdditiveExpression) {
                var e = expr as AST.Expression.AdditiveExpression;
                var lhs = ConstantEval(e.Lhs);
                var rhs = ConstantEval(e.Rhs);
                switch (e.Op) {
                    case "+":
                        return lhs + rhs;
                    case "-":
                        return lhs - rhs;
                    default:
                        throw new Exception();
                }
            }
            if (expr is AST.Expression.AndExpression) {
                var e = expr as AST.Expression.AndExpression;
                var lhs = ConstantEval(e.Lhs);
                if (lhs != 0) {
                    return ConstantEval(e.Rhs) == 0 ? 0 : 1;
                } else {
                    return 0;
                }
            }
            if (expr is AST.Expression.ArrayIndexExpression) {
                var e = expr as AST.Expression.ArrayIndexExpression;
                throw new Exception();
            }
            if (expr is AST.Expression.AssignmentExpression) {
                var e = expr as AST.Expression.AssignmentExpression;
                throw new Exception();
            }
            if (expr is AST.Expression.CastExpression) {
                var e = expr as AST.Expression.CastExpression;
                // キャストは未実装
                return ConstantEval(e.Expr);
            }
            if (expr is AST.Expression.CharacterConstant) {
                var e = expr as AST.Expression.CharacterConstant;
                return (int)e.Str[1];
            }
            if (expr is AST.Expression.CommaExpression) {
                var e = expr as AST.Expression.CommaExpression;
                throw new Exception();
            }
            if (expr is AST.Expression.ConditionalExpression) {
                var e = expr as AST.Expression.ConditionalExpression;
                var cond = ConstantEval(e.Cond);
                if (cond != 0) {
                    return ConstantEval(e.ThenExpr);
                } else {
                    return ConstantEval(e.ElseExpr);
                }
            }
            if (expr is AST.Expression.EnumerationConstant) {
                var e = expr as AST.Expression.EnumerationConstant;
                return e.Ret.Item3;
            }
            if (expr is AST.Expression.EqualityExpression) {
                var e = expr as AST.Expression.EqualityExpression;
                var lhs = ConstantEval(e.Lhs);
                var rhs = ConstantEval(e.Rhs);
                switch (e.Op) {
                    case "==":
                        return lhs == rhs ? 1 : 0;
                    case "!=":
                        return lhs != rhs ? 1 : 0;
                    default:
                        throw new Exception();
                }
            }
            if (expr is AST.Expression.ExclusiveOrExpression) {
                var e = expr as AST.Expression.ExclusiveOrExpression;
                var lhs = ConstantEval(e.Lhs);
                var rhs = ConstantEval(e.Rhs);
                return lhs ^ rhs;
            }
            if (expr is AST.Expression.FloatingConstant) {
                var e = expr as AST.Expression.FloatingConstant;
                // 未実装
                throw new Exception();
            }
            if (expr is AST.Expression.FunctionCallExpression) {
                var e = expr as AST.Expression.FunctionCallExpression;
                throw new Exception();
            }
            if (expr is AST.Expression.FunctionExpression) {
                var e = expr as AST.Expression.FunctionExpression;
                throw new Exception();
            }
            if (expr is AST.Expression.IdentifierExpression) {
                var e = expr as AST.Expression.IdentifierExpression;
                throw new Exception();
            }
            if (expr is AST.Expression.InclusiveOrExpression) {
                var e = expr as AST.Expression.InclusiveOrExpression;
                var lhs = ConstantEval(e.Lhs);
                var rhs = ConstantEval(e.Rhs);
                return lhs | rhs;
            }
            if (expr is AST.Expression.IntegerConstant) {
                var e = expr as AST.Expression.IntegerConstant;
                return (int)e.Value;
            }
            if (expr is AST.Expression.LogicalAndExpression) {
                var e = expr as AST.Expression.LogicalAndExpression;
                var lhs = ConstantEval(e.Lhs);
                if (lhs != 0) {
                    return ConstantEval(e.Rhs) == 0 ? 0 : 1;
                } else {
                    return 1;
                }
            }
            if (expr is AST.Expression.LogicalOrExpression) {
                var e = expr as AST.Expression.LogicalOrExpression;
                var lhs = ConstantEval(e.Lhs);
                if (lhs == 0) {
                    return ConstantEval(e.Rhs) == 0 ? 0 : 1;
                } else {
                    return 1;
                }
            }
            if (expr is AST.Expression.MemberDirectAccess) {
                var e = expr as AST.Expression.MemberDirectAccess;
                throw new Exception();
            }
            if (expr is AST.Expression.MemberIndirectAccess) {
                var e = expr as AST.Expression.MemberIndirectAccess;
                throw new Exception();
            }
            if (expr is AST.Expression.MultiplicitiveExpression) {
                var e = expr as AST.Expression.MultiplicitiveExpression;
                var lhs = ConstantEval(e.Lhs);
                var rhs = ConstantEval(e.Rhs);
                switch (e.Op) {
                    case "*":
                        return lhs * rhs;
                    case "/":
                        return lhs / rhs;
                    case "%":
                        return lhs % rhs;
                    default:
                        throw new Exception();
                }
            }
            if (expr is AST.Expression.RelationalExpression) {
                var e = expr as AST.Expression.RelationalExpression;
                var lhs = ConstantEval(e.Lhs);
                var rhs = ConstantEval(e.Rhs);
                switch (e.Op) {
                    case "<":
                        return lhs < rhs ? 1 : 0;
                    case ">":
                        return lhs > rhs ? 1 : 0;
                    case "<=":
                        return lhs <= rhs ? 1 : 0;
                    case ">=":
                        return lhs >= rhs ? 1 : 0;
                    default:
                        throw new Exception();
                }
            }
            if (expr is AST.Expression.ShiftExpression) {
                var e = expr as AST.Expression.ShiftExpression;
                var lhs = ConstantEval(e.Lhs);
                var rhs = ConstantEval(e.Rhs);
                switch (e.Op) {
                    case "<<":
                        return lhs << rhs;
                    case ">>":
                        return lhs >> rhs;
                    default:
                        throw new Exception();
                }
            }
            if (expr is AST.Expression.SizeofExpression) {
                var e = expr as AST.Expression.SizeofExpression;
                // 未実装につきintサイズ固定
                return 4;
            }
            if (expr is AST.Expression.SizeofTypeExpression) {
                var e = expr as AST.Expression.SizeofTypeExpression;
                return e.Ty.Sizeof();
            }
            if (expr is AST.Expression.StringExpression) {
                var e = expr as AST.Expression.StringExpression;
                throw new Exception();
            }
            if (expr is AST.Expression.UnaryAddressExpression) {
                var e = expr as AST.Expression.UnaryAddressExpression;
                throw new Exception();
            }
            if (expr is AST.Expression.UnaryMinusExpression) {
                var e = expr as AST.Expression.UnaryMinusExpression;
                return -ConstantEval(e.Expr);
            }
            if (expr is AST.Expression.UnaryNegateExpression) {
                var e = expr as AST.Expression.UnaryNegateExpression;
                return ~ConstantEval(e.Expr);
            }
            if (expr is AST.Expression.UnaryNotExpression) {
                var e = expr as AST.Expression.UnaryNotExpression;
                return ConstantEval(e.Expr) == 0 ? 0 : 1;
            }
            if (expr is AST.Expression.UnaryPlusExpression) {
                var e = expr as AST.Expression.UnaryPlusExpression;
                return ConstantEval(e.Expr);
            }
            if (expr is AST.Expression.UnaryPostfixExpression) {
                var e = expr as AST.Expression.UnaryPostfixExpression;
                throw new Exception();
            }
            if (expr is AST.Expression.UnaryPrefixExpression) {
                var e = expr as AST.Expression.UnaryPrefixExpression;
                throw new Exception();
            }
            if (expr is AST.Expression.UnaryReferenceExpression) {
                var e = expr as AST.Expression.UnaryReferenceExpression;
                throw new Exception();
            }
            if (expr is AST.Expression.VariableExpression) {
                var e = expr as AST.Expression.VariableExpression;
                throw new Exception();
            }
            throw new Exception();
        }

        public abstract class Expression : AST {
            public class CommaExpression : Expression {
                public List<AST.Expression> expressions { get; } = new List<AST.Expression>();
            }

            public class AssignmentExpression : Expression {
                public string Op {
                    get;
                }
                public Expression Lhs {
                    get;
                }
                public Expression Rhs {
                    get;
                }

                public AssignmentExpression(string op, Expression lhs, Expression rhs) {
                    Op = op;
                    Lhs = lhs;
                    Rhs = rhs;
                }

            }

            public class ConditionalExpression : Expression {
                public Expression Cond {
                    get;
                }
                public Expression ThenExpr {
                    get;
                }
                public Expression ElseExpr {
                    get;
                }

                public ConditionalExpression(Expression cond, Expression thenExpr, Expression elseExpr) {
                    Cond = cond;
                    ThenExpr = thenExpr;
                    ElseExpr = elseExpr;
                }
            }

            public class LogicalOrExpression : Expression {
                public Expression Lhs {
                    get;
                }
                public Expression Rhs {
                    get;
                }

                public LogicalOrExpression(Expression lhs, Expression rhs) {
                    Lhs = lhs;
                    Rhs = rhs;
                }

            }

            public class LogicalAndExpression : Expression {
                public Expression Lhs {
                    get;
                }
                public Expression Rhs {
                    get;
                }

                public LogicalAndExpression(Expression lhs, Expression rhs) {
                    Lhs = lhs;
                    Rhs = rhs;
                }
            }

            public class InclusiveOrExpression : Expression {
                public Expression Lhs {
                    get;
                }
                public Expression Rhs {
                    get;
                }

                public InclusiveOrExpression(Expression lhs, Expression rhs) {
                    Lhs = lhs;
                    Rhs = rhs;
                }
            }

            public class ExclusiveOrExpression : Expression {
                public Expression Lhs {
                    get;
                }
                public Expression Rhs {
                    get;
                }

                public ExclusiveOrExpression(Expression lhs, Expression rhs) {
                    Lhs = lhs;
                    Rhs = rhs;
                }

            }

            public class AndExpression : Expression {
                public Expression Lhs {
                    get;
                }
                public Expression Rhs {
                    get;
                }

                public AndExpression(Expression lhs, Expression rhs) {
                    Lhs = lhs;
                    Rhs = rhs;
                }
            }

            public class EqualityExpression : Expression {
                public string Op {
                    get;
                }
                public Expression Lhs {
                    get;
                }
                public Expression Rhs {
                    get;
                }

                public EqualityExpression(string op, Expression lhs, Expression rhs) {
                    Op = op;
                    Lhs = lhs;
                    Rhs = rhs;
                }
            }

            public class RelationalExpression : Expression {
                public string Op {
                    get;
                }
                public Expression Lhs {
                    get;
                }
                public Expression Rhs {
                    get;
                }

                public RelationalExpression(string op, Expression lhs, Expression rhs) {
                    Op = op;
                    Lhs = lhs;
                    Rhs = rhs;
                }
            }

            public class ShiftExpression : Expression {
                public string Op {
                    get;
                }
                public Expression Lhs {
                    get;
                }
                public Expression Rhs {
                    get;
                }

                public ShiftExpression(string op, Expression lhs, Expression rhs) {
                    Op = op;
                    Lhs = lhs;
                    Rhs = rhs;
                }
            }

            public class AdditiveExpression : Expression {
                public string Op {
                    get;
                }
                public Expression Lhs {
                    get;
                }
                public Expression Rhs {
                    get;
                }

                public AdditiveExpression(string op, Expression lhs, Expression rhs) {
                    Op = op;
                    Lhs = lhs;
                    Rhs = rhs;
                }
            }

            public class MultiplicitiveExpression : Expression {
                public string Op {
                    get;
                }
                public Expression Lhs {
                    get;
                }
                public Expression Rhs {
                    get;
                }

                public MultiplicitiveExpression(string op, Expression lhs, Expression rhs) {
                    Op = op;
                    Lhs = lhs;
                    Rhs = rhs;
                }
            }

            public class CastExpression : Expression {
                public CType Ty {
                    get;
                }
                public Expression Expr {
                    get;
                }

                public CastExpression(CType ty, Expression expr) {
                    Ty = ty;
                    Expr = expr;
                }
            }

            public class UnaryPrefixExpression : Expression {
                public string Op {
                    get;
                }
                public Expression Expr {
                    get;
                }

                public UnaryPrefixExpression(string op, Expression expr) {
                    Op = op;
                    Expr = expr;
                }
            }

            public class UnaryAddressExpression : Expression {
                public Expression Expr {
                    get;
                }

                public UnaryAddressExpression(Expression expr) {
                    Expr = expr;
                }
            }

            public class UnaryReferenceExpression : Expression {
                public Expression Expr {
                    get;
                }

                public UnaryReferenceExpression(Expression expr) {
                    Expr = expr;
                }
            }

            public class UnaryPlusExpression : Expression {
                public Expression Expr {
                    get;
                }

                public UnaryPlusExpression(Expression expr) {
                    Expr = expr;
                }
            }

            public class UnaryMinusExpression : Expression {
                public Expression Expr {
                    get;
                }

                public UnaryMinusExpression(Expression expr) {
                    Expr = expr;
                }
            }

            public class UnaryNegateExpression : Expression {
                public Expression Expr {
                    get;
                }

                public UnaryNegateExpression(Expression expr) {
                    Expr = expr;
                }
            }

            public class UnaryNotExpression : Expression {
                public Expression Expr {
                    get;
                }

                public UnaryNotExpression(Expression expr) {
                    Expr = expr;
                }
            }

            public class SizeofTypeExpression : Expression {
                public CType Ty {
                    get;
                }

                public SizeofTypeExpression(CType ty) {
                    Ty = ty;
                }
            }

            public class SizeofExpression : Expression {
                public Expression Expr {
                    get;
                }

                public SizeofExpression(Expression expr) {
                    Expr = expr;
                }
            }

            public class ArrayIndexExpression : Expression {
                public Expression Expr {
                    get;
                }
                public Expression Index {
                    get;
                }

                public ArrayIndexExpression(Expression expr, Expression index) {
                    Expr = expr;
                    Index = index;
                }
            }

            public class FunctionCallExpression : Expression {
                public Expression Expr {
                    get;
                }
                public List<Expression> Args {
                    get;
                }

                public FunctionCallExpression(Expression expr, List<Expression> args) {
                    Expr = expr;
                    Args = args;
                }
            }

            public class MemberDirectAccess : Expression {
                public Expression Expr {
                    get;
                }
                public string Ident {
                    get;
                }

                public MemberDirectAccess(Expression expr, string ident) {
                    Expr = expr;
                    Ident = ident;
                }
            }

            public class MemberIndirectAccess : Expression {
                public Expression Expr {
                    get;
                }
                public string Ident {
                    get;
                }

                public MemberIndirectAccess(Expression expr, string ident) {
                    Expr = expr;
                    Ident = ident;
                }
            }

            public class UnaryPostfixExpression : Expression {
                public string Op {
                    get;
                }
                public Expression Expr {
                    get;
                }

                public UnaryPostfixExpression(string op, Expression expr) {
                    Op = op;
                    Expr = expr;
                }
            }

            public class IdentifierExpression : Expression {
                public string Ident {
                    get;
                }

                public IdentifierExpression(string ident) {
                    Ident = ident;
                }
            }

            public class StringExpression : Expression {
                public List<string> Strings {
                    get;
                }

                public StringExpression(List<string> strings) {
                    Strings = strings;
                }
            }

            public class IntegerConstant : Expression {

                public string Str {
                    get;
                }
                public long Value {
                    get;
                }

                public IntegerConstant(string str, long value) {
                    this.Str = str;
                    this.Value = value;
                }
            }

            public class CharacterConstant : Expression {
                public string Str {
                    get;
                }

                public CharacterConstant(string str) {
                    Str = str;
                }
            }

            public class FloatingConstant : Expression {
                public string Str {
                    get;
                }

                public FloatingConstant(string str) {
                    Str = str;
                }
            }

            public class EnumerationConstant : Expression {
                public Tuple<string, CType, int> Ret {
                    get;
                }

                public EnumerationConstant(Tuple<string, CType, int> ret) {
                    Ret = ret;
                }
            }

            internal class VariableExpression : Expression {
                public string ident {
                    get;
                }
                public Declaration.VariableDeclaration variableDeclaration {
                    get;
                }

                public VariableExpression(string ident, Declaration.VariableDeclaration variableDeclaration) {
                    this.ident = ident;
                    this.variableDeclaration = variableDeclaration;
                }
            }

            internal class FunctionExpression : Expression {
                public Declaration.FunctionDeclaration functionDeclaration {
                    get;
                }
                public string ident {
                    get;
                }

                public FunctionExpression(string ident, Declaration.FunctionDeclaration functionDeclaration) {
                    this.ident = ident;
                    this.functionDeclaration = functionDeclaration;
                }
            }
        }

        public abstract class Statement : AST {
            public class GotoStatement : Statement {
                public string Label {
                    get;
                }

                public GotoStatement(string label) {
                    Label = label;
                }
            }

            public class ContinueStatement : Statement {
                public Statement Stmt {
                    get;
                }

                public ContinueStatement(Statement stmt) {
                    Stmt = stmt;
                }
            }

            public class BreakStatement : Statement {
                public Statement Stmt {
                    get;
                }

                public BreakStatement(Statement stmt) {
                    Stmt = stmt;
                }
            }

            public class ReturnStatement : Statement {
                public Expression Expr {
                    get;
                }

                public ReturnStatement(Expression expr) {
                    Expr = expr;
                }
            }

            public class WhileStatement : Statement {
                public Expression Cond {
                    get;
                }
                public Statement Stmt {
                    get; set;
                }

                public WhileStatement(Expression cond) {
                    Cond = cond;
                }
            }

            public class DoWhileStatement : Statement {
                public Statement Stmt {
                    get; set;
                }
                public Expression Cond {
                    get; set;
                }
            }

            public class ForStatement : Statement {
                public Expression Init {
                    get;
                }
                public Expression Cond {
                    get;
                }
                public Expression Update {
                    get;
                }
                public Statement Stmt {
                    get; set;
                }

                public ForStatement(Expression init, Expression cond, Expression update) {
                    Init = init;
                    Cond = cond;
                    Update = update;
                }
            }

            public class IfStatement : Statement {
                public Expression Cond {
                    get;
                }
                public Statement ThenStmt {
                    get;
                }
                public Statement ElseStmt {
                    get;
                }

                public IfStatement(Expression cond, Statement thenStmt, Statement elseStmt) {
                    Cond = cond;
                    ThenStmt = thenStmt;
                    ElseStmt = elseStmt;
                }
            }

            public class SwitchStatement : Statement {
                public Expression Cond {
                    get;
                }
                public Statement Stmt {
                    get; set;
                }

                public SwitchStatement(Expression cond) {
                    Cond = cond;
                }
            }

            public class CompoundStatement : Statement {
                public List<Declaration> Decls {
                    get;
                }
                public List<Statement> Stmts {
                    get;
                }
                public Scope<CType.TaggedType> TagScope {
                    get;
                }
                public Scope<Grammer.IdentifierValue> IdentScope {
                    get;
                }

                public CompoundStatement(List<Declaration> decls, List<Statement> stmts, Scope<CType.TaggedType> tagScope, Scope<Grammer.IdentifierValue> identScope) {
                    Decls = decls;
                    Stmts = stmts;
                    TagScope = tagScope;
                    IdentScope = identScope;
                }
            }

            public class EmptyStatement : Statement {
            }

            public class ExpressionStatement : Statement {
                public Expression Expr {
                    get;
                }

                public ExpressionStatement(Expression expr) {
                    Expr = expr;
                }
            }

            public class CaseStatement : Statement {
                public Expression Expr {
                    get;
                }
                public Statement Stmt {
                    get;
                }

                public CaseStatement(Expression expr, Statement stmt) {
                    Expr = expr;
                    Stmt = stmt;
                }
            }

            public class DefaultStatement : Statement {
                public Statement Stmt {
                    get;
                }

                public DefaultStatement(Statement stmt) {
                    Stmt = stmt;
                }
            }

            public class GenericLabeledStatement : Statement {
                public string Ident {
                    get;
                }
                public Statement Stmt {
                    get;
                }

                public GenericLabeledStatement(string ident, Statement stmt) {
                    Ident = ident;
                    Stmt = stmt;
                }
            }
        }

        public abstract class Initializer : AST {
            public class CompilxInitializer : Initializer {
                public List<Initializer> Ret {
                    get;
                }

                public CompilxInitializer(List<Initializer> ret) {
                    Ret = ret;
                }
            }

            public class SimpleInitializer : Initializer {
                public Expression AssignmentExpression {
                    get;
                }

                public SimpleInitializer(Expression assignmentExpression) {
                    AssignmentExpression = assignmentExpression;
                }
            }
        }

        public abstract class Declaration {

            public class FunctionDeclaration : Declaration {

                public string Ident {
                    get;
                }
                public CType Ty {
                    get;
                }
                public StorageClass StorageClass {
                    get;
                }
                public Statement Body {
                    get;
                }

                public FunctionDeclaration(string ident, CType ty, StorageClass storage_class, Statement body) {
                    Ident = ident;
                    Ty = ty;
                    StorageClass = storage_class;
                    Body = body;
                }

                public FunctionDeclaration(string ident, CType ty, StorageClass storage_class) {
                    Ident = ident;
                    Ty = ty;
                    StorageClass = storage_class;
                    Body = null;
                }
            }

            public class VariableDeclaration : Declaration {
                public string Ident {
                    get;
                }
                public CType Ctype {
                    get;
                }
                public StorageClass StorageClass {
                    get;
                }
                public Initializer Init {
                    get;
                }

                public VariableDeclaration(string ident, CType ctype, StorageClass storage_class, Initializer init) {
                    Ident = ident;
                    Ctype = ctype;
                    StorageClass = storage_class;
                    Init = init;
                }
            }

            public class TypeDeclaration : Declaration {
                public string Ident {
                    get;
                }
                public CType Ctype {
                    get;
                }

                public TypeDeclaration(string ident, CType ctype) {
                    Ident = ident;
                    Ctype = ctype;
                }
            }
        }

        public class TranslationUnit : AST {
            public List<AST.Declaration> declarations { get; } = new List<Declaration>();
        }
    }

    /// <summary>
    /// スコープ
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    public class Scope<TValue> {
        public static Scope<TValue> Empty { get; } = new Scope<TValue>();

        private LinkedDictionary<string, TValue> entries = LinkedDictionary<string, TValue>.Empty;

        public Scope<TValue> Parent { get; } = Empty;

        private Scope() {
        }
        protected Scope(Scope<TValue> Parent) {
            this.Parent = Parent;
        }
        public Scope<TValue> Extend() {
            return new Scope<TValue>(this);
        }

        public void Add(string ident, TValue value) {
            this.entries = this.entries.Extend(ident, value);
        }

        public bool ContainsKey(string v) {
            var it = this;
            while (it != null) {
                if (it.entries.ContainsKey(v)) {
                    return true;
                }
                it = it.Parent;
            }
            return false;
        }

        public bool TryGetValue(string v, out TValue value) {
            var it = this;
            while (it != null) {
                if (it.entries.TryGetValue(v, out value)) {
                    return true;
                }
                it = it.Parent;
            }
            value = default(TValue);
            return false;
        }
    }

    public class Grammer {
        public abstract class IdentifierValue {
            public virtual bool IsEnumValue() {
                return false;
            }

            public virtual Tuple<string, CType, int> ToEnumValue() {
                throw new Exception("");
            }

            public virtual bool IsVariable() {
                return false;
            }

            public virtual AST.Declaration.VariableDeclaration ToVariable() {
                throw new Exception("");
            }

            public virtual bool IsFunction() {
                return false;
            }

            public virtual AST.Declaration.FunctionDeclaration ToFunction() {
                throw new Exception("");
            }

            public class EnumValue : IdentifierValue {
                public override bool IsEnumValue() {
                    return true;
                }
                public override Tuple<string, CType, int> ToEnumValue() {
                    return Tuple.Create(ident, (CType)ctype, ctype.enumerator_list.FindIndex(x => x.Item1 == ident));
                }
                public CType.TaggedType.EnumType ctype {
                    get;
                }
                public string ident {
                    get;
                }
                public EnumValue(CType.TaggedType.EnumType ctype, string ident) {
                    this.ctype = ctype;
                    this.ident = ident;
                }
            }

            public class Declaration : IdentifierValue {
                public override bool IsVariable() {
                    return Decl is AST.Declaration.VariableDeclaration;
                }

                public override AST.Declaration.VariableDeclaration ToVariable() {
                    return Decl as AST.Declaration.VariableDeclaration;
                }

                public override bool IsFunction() {
                    return Decl is AST.Declaration.FunctionDeclaration;
                }

                public override AST.Declaration.FunctionDeclaration ToFunction() {
                    return Decl as AST.Declaration.FunctionDeclaration;
                }

                public AST.Declaration Decl {
                    get;
                }

                public Declaration(AST.Declaration decl) {
                    Decl = decl;
                }
            }
        }

        /// <summary>
        /// 名前空間(ステートメント ラベル)
        /// </summary>
        private Scope<AST.Statement.GenericLabeledStatement> label_scope = Scope<AST.Statement.GenericLabeledStatement>.Empty;

        /// <summary>
        /// 名前空間(構造体、共用体、列挙体のタグ名)
        /// </summary>
        private Scope<CType.TaggedType> tag_scope = Scope<CType.TaggedType>.Empty;

        /// <summary>
        /// 名前空間(通常の識別子（変数、関数、引数、列挙定数)
        /// </summary>
        private Scope<IdentifierValue> ident_scope = Scope<IdentifierValue>.Empty;

        /// <summary>
        /// 名前空間(Typedef名)
        /// </summary>
        private Scope<AST.Declaration.TypeDeclaration> typedef_scope = Scope<AST.Declaration.TypeDeclaration>.Empty;

        // 構造体または共用体のメンバーについてはそれぞれの宣言オブジェクトに付与される

        /// <summary>
        /// break命令についてのスコープ
        /// </summary>
        private LinkedList<AST.Statement> break_scope = LinkedList<AST.Statement>.Empty;

        /// <summary>
        /// continue命令についてのスコープ
        /// </summary>
        private LinkedList<AST.Statement> continue_scope = LinkedList<AST.Statement>.Empty;

        //
        // lex spec
        //

        public class Token {
            [Flags]
            public enum TokenKind {
                EOF = -1,
                // ReserveWords
                AUTO = 256,
                BREAK,
                CASE,
                CHAR,
                CONST,
                CONTINUE,
                DEFAULT,
                DO,
                DOUBLE,
                ELSE,
                ENUM,
                EXTERN,
                FLOAT,
                FOR,
                GOTO,
                IF,
                INT,
                LONG,
                REGISTER,
                RETURN,
                SHORT,
                SIGNED,
                SIZEOF,
                STATIC,
                STRUCT,
                SWITCH,
                TYPEDEF,
                UNION,
                UNSIGNED,
                VOID,
                VOLATILE,
                WHILE,
                // C99
                INLINE,
                RESTRICT,
                // Special
                NEAR,
                FAR,
                __ASM__,
                __VOLATILE__,
                // Identifiers
                IDENTIFIER,
                TYPE_NAME,
                // Constants
                STRING_CONSTANT,
                HEXIMAL_CONSTANT,
                OCTAL_CONSTANT,
                DECIAML_CONSTANT,
                FLOAT_CONSTANT,
                DOUBLE_CONSTANT,
                // StringLiteral
                STRING_LITERAL,
                // Symbols
                ELLIPSIS,
                RIGHT_ASSIGN,
                LEFT_ASSIGN,
                ADD_ASSIGN,
                SUB_ASSIGN,
                MUL_ASSIGN,
                DIV_ASSIGN,
                MOD_ASSIGN,
                AND_ASSIGN,
                XOR_ASSIGN,
                OR_ASSIGN,
                RIGHT_OP,
                LEFT_OP,
                INC_OP,
                DEC_OP,
                PTR_OP,
                AND_OP,
                OR_OP,
                LE_OP,
                GE_OP,
                EQ_OP,
                NE_OP,
            }
            public int start {
                get;
            }
            public int length {
                get;
            }
            public string raw {
                get;
            }
            public TokenKind kind {
                get;
            }
            public Token(TokenKind kind, int start, int length, string raw) {
                this.kind = kind;
                this.start = start;
                this.length = length;
                this.raw = raw;
            }
            public override string ToString() {
                return $"(\"{raw}\", {kind}, {start}, {length})";
            }
        }

        private bool IsIdentifierHead(int ch) {
            return ('A' <= ch && ch <= 'Z') || ('a' <= ch && ch <= 'z') || (ch == '_');
        }
        private bool IsIdentifierBody(int ch) {
            return ('A' <= ch && ch <= 'Z') || ('a' <= ch && ch <= 'z') || ('0' <= ch && ch <= '9') || (ch == '_');
        }
        private bool IsDigit(int ch) {
            return ('0' <= ch && ch <= '9');
        }
        private bool IsSpace(int ch) {
            return "\r\n\v\f\t ".Any(x => (int)x == ch);
        }

        private Dictionary<string, Token.TokenKind> reserve_words = new Dictionary<string, Token.TokenKind>() {

            {"auto", Token.TokenKind.AUTO},
            {"break" , Token.TokenKind.BREAK},
            {"case" , Token.TokenKind.CASE},
            {"char" , Token.TokenKind.CHAR},
            {"const" , Token.TokenKind.CONST},
            {"continue" , Token.TokenKind.CONTINUE},
            {"default" , Token.TokenKind.DEFAULT},
            {"do" , Token.TokenKind.DO},
            {"double" , Token.TokenKind.DOUBLE},
            {"else" , Token.TokenKind.ELSE},
            {"enum" , Token.TokenKind.ENUM},
            {"extern" , Token.TokenKind.EXTERN},
            {"float" , Token.TokenKind.FLOAT},
            {"for" , Token.TokenKind.FOR},
            {"goto" , Token.TokenKind.GOTO},
            {"if" , Token.TokenKind.IF},
            {"int" , Token.TokenKind.INT},
            {"long" , Token.TokenKind.LONG},
            {"register" , Token.TokenKind.REGISTER},
            {"return" , Token.TokenKind.RETURN},
            {"short" , Token.TokenKind.SHORT},
            {"signed" , Token.TokenKind.SIGNED},
            {"sizeof" , Token.TokenKind.SIZEOF},
            {"static" , Token.TokenKind.STATIC},
            {"struct" , Token.TokenKind.STRUCT},
            {"switch" , Token.TokenKind.SWITCH},
            {"typedef" , Token.TokenKind.TYPEDEF},
            {"union" , Token.TokenKind.UNION},
            {"unsigned" , Token.TokenKind.UNSIGNED},
            {"void" , Token.TokenKind.VOID},
            {"volatile" , Token.TokenKind.VOLATILE},
            {"while" , Token.TokenKind.WHILE},
            // c99
            {"inline" , Token.TokenKind.INLINE},
            {"restrict" , Token.TokenKind.RESTRICT},
            // special
            {"near" , Token.TokenKind.NEAR},
            {"far" , Token.TokenKind.FAR},
            {"__asm__" , Token.TokenKind.__ASM__},
            {"__volatile__" , Token.TokenKind.__VOLATILE__},
        };

        private List<Tuple<string, Token.TokenKind>> symbols = new List<Tuple<string, Token.TokenKind>>() {
            Tuple.Create("...", Token.TokenKind.ELLIPSIS),
            Tuple.Create(">>=", Token.TokenKind.RIGHT_ASSIGN),
            Tuple.Create("<<=", Token.TokenKind.LEFT_ASSIGN),
            Tuple.Create("+=", Token.TokenKind.ADD_ASSIGN),
            Tuple.Create("-=", Token.TokenKind.SUB_ASSIGN),
            Tuple.Create("*=", Token.TokenKind.MUL_ASSIGN),
            Tuple.Create("/=", Token.TokenKind.DIV_ASSIGN),
            Tuple.Create("%=", Token.TokenKind.MOD_ASSIGN),
            Tuple.Create("&=", Token.TokenKind.AND_ASSIGN),
            Tuple.Create("^=", Token.TokenKind.XOR_ASSIGN),
            Tuple.Create("|=", Token.TokenKind.OR_ASSIGN),
            Tuple.Create(">>", Token.TokenKind.RIGHT_OP),
            Tuple.Create("<<", Token.TokenKind.LEFT_OP),
            Tuple.Create("++", Token.TokenKind.INC_OP),
            Tuple.Create("--", Token.TokenKind.DEC_OP),
            Tuple.Create("->", Token.TokenKind.PTR_OP),
            Tuple.Create("&&", Token.TokenKind.AND_OP),
            Tuple.Create("||", Token.TokenKind.OR_OP),
            Tuple.Create("<=", Token.TokenKind.LE_OP),
            Tuple.Create(">=", Token.TokenKind.GE_OP),
            Tuple.Create("==", Token.TokenKind.EQ_OP),
            Tuple.Create("!=", Token.TokenKind.NE_OP),
            Tuple.Create(";", (Token.TokenKind)';'),
            Tuple.Create("{", (Token.TokenKind)'{'),
            Tuple.Create("<%", (Token.TokenKind)'{'),
            Tuple.Create("}", (Token.TokenKind)'}'),
            Tuple.Create("%>", (Token.TokenKind)'}'),
            Tuple.Create("<:", (Token.TokenKind)'['),
            Tuple.Create(":>", (Token.TokenKind)']'),
            Tuple.Create(",", (Token.TokenKind)','),
            Tuple.Create(":", (Token.TokenKind)':'),
            Tuple.Create("=", (Token.TokenKind)'='),
            Tuple.Create("(", (Token.TokenKind)'('),
            Tuple.Create(")", (Token.TokenKind)')'),
            Tuple.Create("[", (Token.TokenKind)'['),
            Tuple.Create("]", (Token.TokenKind)']'),
            Tuple.Create(".", (Token.TokenKind)'.'),
            Tuple.Create("&", (Token.TokenKind)'&'),
            Tuple.Create("!", (Token.TokenKind)'!'),
            Tuple.Create("~", (Token.TokenKind)'~'),
            Tuple.Create("-", (Token.TokenKind)'-'),
            Tuple.Create("+", (Token.TokenKind)'+'),
            Tuple.Create("*", (Token.TokenKind)'*'),
            Tuple.Create("/", (Token.TokenKind)'/'),
            Tuple.Create("%", (Token.TokenKind)'%'),
            Tuple.Create("<", (Token.TokenKind)'<'),
            Tuple.Create(">", (Token.TokenKind)'>'),
            Tuple.Create("^", (Token.TokenKind)'^'),
            Tuple.Create("|", (Token.TokenKind)'|'),
            Tuple.Create("?", (Token.TokenKind)'?'),
        }.OrderByDescending((x) => x.Item1.Length).ToList();

        private static string D { get; } = $@"[0-9]";
        private static string L { get; } = $@"[a-zA-Z_]";
        private static string H { get; } = $@"[a-fA-F0-9]";
        private static string E { get; } = $@"[Ee][+-]?{D}+";
        private static string FS { get; } = $@"(f|F|l|L)";
        private static string IS { get; } = $@"(u|U|l|L)*";
        private static Regex RegexPreprocessingNumber { get; } = new Regex($@"^(\.?\d([eEpP][\+\-]|\.|({L}|{D}|_))*)$");
        private static Regex RegexFlating { get; } = new Regex($@"^({D}+{E}|{D}*\.{D}+({E})?|{D}+\.{D}*({E})?){FS}?$");
        private static Regex RegexHeximal { get; } = new Regex($@"^0[xX]({H}+)({IS})?$");
        private static Regex RegexDecimal { get; } = new Regex($@"^({D}+){IS}?$");
        private static Regex RegexOctal { get; } = new Regex($@"^0({D}+){IS}?$");
        private static Regex RegexChar { get; } = new Regex($@"^L?'(\.|[^\'])+'$");
        private static Regex RegexStringLiteral { get; } = new Regex($@"^L?""(\.|[^\""])*""$");


        private string _inputText;
        private int _inputPos = 0;

        private bool _beginOfLine = true;
        private int line = 1;
        private int column = 1;

        private void IncPos(int n) {
            for (int i = 0; i < n; i++) {
                if (_inputText[_inputPos + i] == '\n') {
                    line++;
                    column = 1;
                } else {
                    column++;
                }
            }
            _inputPos += n;
        }

        private int scanch(int offset = 0) {
            if (_inputPos + offset >= _inputText.Length) {
                return -1;
            } else {
                return _inputText[_inputPos + offset];
            }
        }

        private bool scanch(string s) {
            for (var i = 0; i < s.Length; i++) {
                if (scanch(i) != s[i]) {
                    return false;
                }
            }
            return true;
        }

        private List<Token> _tokens { get; } = new List<Token>();


        private bool scan() {
            if (_tokens.LastOrDefault()?.kind == Token.TokenKind.EOF) {
                return false;
            }
            rescan:
            while (IsSpace(scanch())) {
                if (scanch("\n")) {
                    _beginOfLine = true;
                }
                IncPos(1);
            }
            if (scanch("/*")) {
                int start = _inputPos;
                IncPos(2);

                bool terminated = false;
                while (_inputPos < _inputText.Length) {
                    if (scanch("\\")) {
                        IncPos(2);
                    } else if (scanch("*/")) {
                        IncPos(2);
                        terminated = true;
                        break;
                    } else {
                        IncPos(1);
                    }
                }
                if (terminated == false) {
                    _tokens.Add(new Token(Token.TokenKind.EOF, _inputPos, 0, ""));
                    return false;
                }
                goto rescan;
            }

            if (scanch() == -1) {
                _tokens.Add(new Token(Token.TokenKind.EOF, _inputPos, 0, ""));
                return false;
            } else if (scanch("#")) {
                if (_beginOfLine) {
                    // pragma は特殊
                    while (scanch("\n") == false) {
                        IncPos(1);
                    }
                    IncPos(1);
                    goto rescan;
                } else {
                    _tokens.Add(new Token((Token.TokenKind)'#', _inputPos, 1, "#"));
                    IncPos(1);
                }
                return true;
            }

            _beginOfLine = false;

            if (IsIdentifierHead(scanch())) {
                int start = _inputPos;
                while (IsIdentifierBody(scanch())) {
                    IncPos(1);
                }
                int len = _inputPos - start;
                var str = _inputText.Substring(start, len);
                Token.TokenKind reserveWordId;
                if (reserve_words.TryGetValue(str, out reserveWordId)) {
                    _tokens.Add(new Token(reserveWordId, start, len, str));
                } else {
                    AST.Declaration.TypeDeclaration val;
                    if (typedef_scope.TryGetValue(str, out val)) {
                        _tokens.Add(new Token(Token.TokenKind.TYPE_NAME, start, len, str));
                    } else {
                        _tokens.Add(new Token(Token.TokenKind.IDENTIFIER, start, len, str));
                    }
                }
                return true;
            } else if ((scanch(0) == '.' && IsDigit(scanch(1))) || IsDigit(scanch(0))) {
                // preprocessor number
                // \.?\d([eEpP][\+\-]|\.|({L}|{D}|_))*
                int start = _inputPos;
                if (scanch() == '.') {
                    IncPos(1);
                }
                IncPos(1);
                while (scanch() != -1) {
                    if ("eEpP".Any(x => (int)x == scanch(0)) && "+-".Any(x => (int)x == scanch(1))) {
                        IncPos(2);
                    } else if (scanch(".") || IsIdentifierBody(scanch())) {
                        IncPos(1);
                    } else {
                        break;
                    }
                }
                int len = _inputPos - start;
                var str = _inputText.Substring(start, len);
                if (RegexFlating.IsMatch(str)) {
                    _tokens.Add(new Token(Token.TokenKind.FLOAT_CONSTANT, start, len, str));
                } else if (RegexHeximal.IsMatch(str)) {
                    _tokens.Add(new Token(Token.TokenKind.HEXIMAL_CONSTANT, start, len, str));
                } else if (RegexOctal.IsMatch(str)) {
                    _tokens.Add(new Token(Token.TokenKind.OCTAL_CONSTANT, start, len, str));
                } else if (RegexDecimal.IsMatch(str)) {
                    _tokens.Add(new Token(Token.TokenKind.DECIAML_CONSTANT, start, len, str));
                } else {
                    throw new Exception();
                }
                return true;
            } else if (scanch("'")) {
                int start = _inputPos;
                IncPos(1);
                while (_inputPos < _inputText.Length) {
                    if (scanch("\\")) {
                        IncPos(2);
                    } else if (scanch("'")) {
                        IncPos(1);
                        int len = _inputPos - start;
                        var str = _inputText.Substring(start, len);
                        _tokens.Add(new Token(Token.TokenKind.STRING_CONSTANT, start, len, str));
                        return true;
                    } else {
                        IncPos(1);
                    }
                }
                throw new Exception();
            } else if (scanch("\"")) {
                int start = _inputPos;
                IncPos(1);
                while (_inputPos < _inputText.Length) {
                    if (scanch("\\")) {
                        IncPos(2);
                    } else if (scanch("\"")) {
                        IncPos(1);
                        int len = _inputPos - start;
                        var str = _inputText.Substring(start, len);
                        _tokens.Add(new Token(Token.TokenKind.STRING_LITERAL, start, len, str));
                        return true;
                    } else {
                        IncPos(1);
                    }
                }
                throw new Exception();
            } else {
                foreach (var sym in symbols) {
                    if (scanch(sym.Item1)) {
                        _tokens.Add(new Token(sym.Item2, _inputPos, sym.Item1.Length, sym.Item1));
                        IncPos(sym.Item1.Length);
                        return true;
                    }
                }
                throw new Exception();
            }
        }

        private int current = 0;

        public Grammer() {
        }


        public void Parse(string s) {
            _inputText = s;
            _tokens.Clear();
            _inputPos = 0;
            current = 0;

            // GCCの組み込み型の設定
            typedef_scope.Add("__builtin_va_list", new AST.Declaration.TypeDeclaration("__builtin_va_list", new CType.PointerType(new CType.BasicType(TypeSpecifier.Void))));

            var ret = translation_unit();
        }


        private void next_token() {
            current++;
        }
        private bool is_eof() {
            return _tokens[current].kind == Token.TokenKind.EOF;
        }
        private void eof() {
            if (!is_eof()) {
                throw new Exception();
            }
        }

        private Token current_token() {
            if (_tokens.Count == current) {
                scan();
            }
            return _tokens[current];
        }

        private void Read(params Token.TokenKind[] s) {
            if (s.Contains(current_token().kind)) {
                next_token();
                return;
            }
            throw new Exception();
        }
        private void Read(params char[] s) {
            Read(s.Select(x => (Token.TokenKind)x).ToArray());
        }
        private bool Peek(params Token.TokenKind[] s) {
            return s.Contains(current_token().kind);
        }
        private bool Peek(params char[] s) {
            return Peek(s.Select(x => (Token.TokenKind)x).ToArray());
        }

        private bool is_nexttoken(params Token.TokenKind[] s) {
            if (_tokens.Count <= current + 1) {
                scan();
                if (is_eof()) {
                    return false;
                }
            }
            return s.Contains(_tokens[current + 1].kind);
        }
        private bool is_nexttoken(params char[] s) {
            return is_nexttoken(s.Select(x => (Token.TokenKind)x).ToArray());
        }
        private bool is_identifier() {
            return current_token().kind == Token.TokenKind.IDENTIFIER;
        }
        private bool is_tagname() {
            return current_token().kind == Token.TokenKind.IDENTIFIER || current_token().kind == Token.TokenKind.TYPE_NAME;
        }
        private string tagname() {
            if (is_tagname() == false) {
                throw new Exception();
            }
            var ret = current_token().raw;
            next_token();
            return ret;
        }

        private bool is_ENUMERATION_CONSTANT() {
            if (!is_identifier()) {
                return false;
            }
            var ident = current_token();
            IdentifierValue v;
            if (ident_scope.TryGetValue(ident.raw, out v) == false) {
                return false;
            }
            if (!(v is IdentifierValue.EnumValue)) {
                return false;
            }
            return (v as IdentifierValue.EnumValue).ctype.enumerator_list.First(x => x.Item1 == ident.raw) != null;
        }

        private Tuple<string, CType, int> ENUMERATION_CONSTANT() {
            var ident = IDENTIFIER();
            IdentifierValue v;
            if (ident_scope.TryGetValue(ident, out v) == false) {
                throw new Exception();
            }
            if (!(v is IdentifierValue.EnumValue)) {
                throw new Exception();
            }
            var ev = (v as IdentifierValue.EnumValue);
            var el = ev.ctype.enumerator_list.First(x => x.Item1 == ident);
            return Tuple.Create(el.Item1, (CType)ev.ctype, el.Item2);
        }

        private bool is_CHARACTER_CONSTANT() {
            return current_token().kind == Token.TokenKind.STRING_CONSTANT;
        }

        private string CHARACTER_CONSTANT() {
            if (is_CHARACTER_CONSTANT() == false) {
                throw new Exception();
            }
            var ret = current_token().raw;
            next_token();
            return ret;
        }

        private bool is_FLOATING_CONSTANT() {
            return current_token().kind == Token.TokenKind.FLOAT_CONSTANT;
        }
        private string FLOATING_CONSTANT() {
            if (is_FLOATING_CONSTANT() == false) {
                throw new Exception();
            }
            var ret = current_token().raw;
            next_token();
            return ret;
        }

        private bool is_INTEGER_CONSTANT() {
            return current_token().kind == Token.TokenKind.HEXIMAL_CONSTANT | current_token().kind == Token.TokenKind.OCTAL_CONSTANT | current_token().kind == Token.TokenKind.DECIAML_CONSTANT;
        }
        private Tuple<string, long> INTEGER_CONSTANT() {
            if (is_INTEGER_CONSTANT() == false) {
                throw new Exception();
            }
            long value = 0;
            switch (current_token().kind) {
                case Token.TokenKind.HEXIMAL_CONSTANT: {
                        var m = RegexHeximal.Match(current_token().raw);
                        if (m.Success == false) {
                            throw new Exception();
                        }
                        value = Convert.ToInt64(m.Groups[1].Value, 16);
                        // m.Groups[2].Valueのサフィックスに応じていろいろする
                        break;
                    }
                case Token.TokenKind.OCTAL_CONSTANT:
                    value = Convert.ToInt64(current_token().raw, 8);
                    break;
                case Token.TokenKind.DECIAML_CONSTANT: {
                        var m = RegexDecimal.Match(current_token().raw);
                        if (m.Success == false) {
                            throw new Exception();
                        }
                        value = Convert.ToInt64(m.Groups[1].Value, 10);
                        // m.Groups[2].Valueのサフィックスに応じていろいろする
                        break;
                    }
                default:
                    throw new Exception();

            }
            var ret = current_token().raw;
            next_token();
            return Tuple.Create(ret, value);
        }

        private bool is_STRING() {
            return current_token().kind == Token.TokenKind.STRING_LITERAL;
        }
        private string STRING() {
            if (is_STRING() == false) {
                throw new Exception();
            }
            var ret = current_token().raw;
            next_token();
            return ret;
        }

        //
        // Grammers
        //


        public AST.TranslationUnit translation_unit() {
            var ret = new AST.TranslationUnit();
            while (is_external_declaration(null, TypeSpecifier.None)) {
                ret.declarations.AddRange(external_declaration());
            }
            is_eof();
            return ret;
        }


        private bool is_external_declaration(CType baseType, TypeSpecifier typeSpecifier) {
            return (is_declaration_specifier(baseType, typeSpecifier) || Peek(';') || is_declarator());
        }

        private List<AST.Declaration> external_declaration() {
            CType baseType = null;
            StorageClass storageClass = StorageClass.None;
            TypeSpecifier typeSpecifier = TypeSpecifier.None;
            TypeQualifier typeQualifier = TypeQualifier.None;
            FunctionSpecifier functionSpecifier = FunctionSpecifier.None;
            while (is_declaration_specifier(baseType, typeSpecifier)) {
                declaration_specifier(ref baseType, ref storageClass, ref typeSpecifier, ref typeQualifier, ref functionSpecifier);
            }
            if (typeSpecifier != TypeSpecifier.None) {
                if (baseType != null) {
                    throw new Exception("");
                }
                else {
                    baseType = new CType.BasicType(typeSpecifier);
                }
            } else if (baseType == null) {
                baseType  = new CType.BasicType(TypeSpecifier.None);
            }
            baseType = new CType.TypeQualifierType(baseType, typeQualifier);

            var ret = new List<AST.Declaration>();

            if (!is_declarator()) {
                Read(';');
                return ret;
            } else {

                for (; ; ) {
                    string ident = "";
                    List<CType> stack = new List<CType>() { new CType.StubType() };
                    declarator(ref ident, stack, 0);
                    var ctype = CType.Resolve(baseType, stack);
                    if (Peek('=', ',', ';')) {
                        if (functionSpecifier != FunctionSpecifier.None) {
                            throw new Exception("inlineは関数定義でのみ使える。");
                        }
                        AST.Declaration decl = null;
                        if (Peek('=')) {
                            if (storageClass == StorageClass.Typedef || storageClass == StorageClass.Extern) {
                                // 初期化できない記憶クラス指定子
                                throw new Exception();
                            }
                            // 関数宣言に初期値は指定できない
                            if (ctype.IsFunction()) {
                                throw new Exception("");
                            }
                            Read('=');
                            var init = initializer();
                            decl = new AST.Declaration.VariableDeclaration(ident, ctype, storageClass, init);
                        } else {
                            if (storageClass == StorageClass.Typedef) {
                                AST.Declaration.TypeDeclaration tdecl;
                                if (typedef_scope.TryGetValue(ident, out tdecl)) {
                                    if (CType.EqualType(tdecl.Ctype, ctype) == false) {
                                        throw new Exception("再定義型の不一致");
                                    }    
                                } else { 
                                    tdecl = new AST.Declaration.TypeDeclaration(ident, ctype);
                                    decl = tdecl;
                                    typedef_scope.Add(ident, tdecl);
                                }
                            } else if (ctype.IsFunction()) {
                                decl = new AST.Declaration.FunctionDeclaration(ident, ctype, storageClass);
                                ident_scope.Add(ident, new IdentifierValue.Declaration(decl));
                            } else {
                                decl = new AST.Declaration.VariableDeclaration(ident, ctype, storageClass, null);
                                ident_scope.Add(ident, new IdentifierValue.Declaration(decl));
                            }
                        }
                        ret.Add(decl);


                        if (Peek(',')) {
                            Read(',');
                            continue;
                        }
                        break;
                    } else if (ctype.IsFunction()) {
                        // 関数定義

                        // K&Rの引数型宣言があるか調べる。
                        var argmuents = is_declaration() ? declaration() : null;

                        // 判定は適当。修正予定
                        // ctypeがK&R型の宣言ならここでctypeの引数部分とargumentsを照合してマージする。
                        if (ctype.IsFunction()) {
                            var ctype_fun = ctype as CType.FunctionType;
                            if (ctype_fun.Arguments == null) {
                                if (argmuents != null) {
                                    throw new Exception("K&Rの空の引数リストに対して引数宣言がある");
                                }
                                else {
                                    // ANSIの引数指定なし関数
                                }
                            }
                            else if (ctype_fun.Arguments.Any(x => x.Item2.IsImplicit())) {
                                if (!ctype_fun.Arguments.All(x => x.Item2.IsImplicit())) {
                                }

                                if (!argmuents.All(x => x is AST.Declaration.VariableDeclaration)) {
                                    throw new Exception("宣言部に引数宣言以外が混ざってない？");
                                }

                                var dic = argmuents.Cast<AST.Declaration.VariableDeclaration>().ToDictionary(x => x.Ident, x => x);
                                var mapped = ctype_fun.Arguments.Select(x => {
                                    if (dic.ContainsKey(x.Item1)) {
                                        return Tuple.Create(x.Item1, dic[x.Item1].Ctype);
                                    }
                                    else {
                                        return Tuple.Create(x.Item1, (CType) new CType.BasicType(TypeSpecifier.None));
                                    }
                                }).ToList();
                                ctype_fun.Arguments.Clear();
                                ctype_fun.Arguments.AddRange(mapped);
                            }
                        }

                        var stmts = compound_statement();

                        var funcdecl = new AST.Declaration.FunctionDeclaration(ident, ctype, storageClass, stmts);
                        ret.Add(funcdecl);
                        ident_scope.Add(ident, new IdentifierValue.Declaration(funcdecl));
                        return ret;
                    }
                    else {
                        throw new Exception("");
                    }

                }
                Read(';');
                return ret;
            }

        }

        private bool is_declaration() {
            return is_declaration_specifiers(null, TypeSpecifier.None);
        }
        private List<AST.Declaration> declaration() {
            StorageClass storageClass;
            CType baseType = declaration_specifiers(out storageClass);

            List<AST.Declaration> decls = null;
            if (!Peek(';')) {
                decls = new List<AST.Declaration>();
                decls.Add(init_declarator(baseType, storageClass));
                while (Peek(',')) {
                    Read(',');
                    decls.Add(init_declarator(baseType, storageClass));
                }
            }
            Read(';');
            return decls;
        }
        private bool is_declaration_specifiers(CType ctype, TypeSpecifier typeSpecifier) {
            return is_declaration_specifier(ctype, typeSpecifier);
        }

        private CType declaration_specifiers(out StorageClass sc) {
            CType baseType = null;
            StorageClass storageClass = StorageClass.None;
            TypeSpecifier typeSpecifier = TypeSpecifier.None;
            TypeQualifier typeQualifier = TypeQualifier.None;
            FunctionSpecifier functionSpecifier = FunctionSpecifier.None;

            declaration_specifier(ref baseType, ref storageClass, ref typeSpecifier, ref typeQualifier, ref functionSpecifier);

            while (is_declaration_specifier(baseType, typeSpecifier)) {
                declaration_specifier(ref baseType, ref storageClass, ref typeSpecifier, ref typeQualifier, ref functionSpecifier);
            }

            if (functionSpecifier != FunctionSpecifier.None) {
                throw new Exception("inlineは関数定義でのみ使える。");
            }

            if (typeSpecifier != TypeSpecifier.None) {
                if (baseType != null) {
                    throw new Exception("");
                } else {
                    baseType = new CType.BasicType(typeSpecifier);
                }
            } else if (baseType == null) {
                baseType = new CType.BasicType(TypeSpecifier.None);
            }
            sc = storageClass;
            return new CType.TypeQualifierType(baseType, typeQualifier);

        }

        private bool is_declaration_specifier(CType ctype, TypeSpecifier typeSpecifier) {
            return (is_storage_class_specifier() ||
                (is_type_specifier() && ctype == null) ||
                (is_struct_or_union_specifier() && ctype == null ) ||
                (is_enum_specifier() && ctype == null ) ||
                (is_TYPEDEF_NAME() && ctype == null && typeSpecifier == TypeSpecifier.None) ||
                is_type_qualifier() ||
                is_function_specifier());
        }

        private void declaration_specifier(ref CType ctype, ref StorageClass storageClass, ref TypeSpecifier typeSpecifier, ref TypeQualifier typeQualifier, ref FunctionSpecifier functionSpecifier) {
            if (is_storage_class_specifier()) {
                storageClass = storageClass.Marge(storage_class_specifier());
            } else if (is_type_specifier()) {
                typeSpecifier = typeSpecifier.Marge(type_specifier());
            } else if (is_struct_or_union_specifier()) {
                if (ctype != null) {
                    throw new Exception("");
                }
                ctype = struct_or_union_specifier();
            } else if (is_enum_specifier()) {
                if (ctype != null) {
                    throw new Exception("");
                }
                ctype = enum_specifier();
            } else if (is_TYPEDEF_NAME()) {
                AST.Declaration.TypeDeclaration value;
                if (typedef_scope.TryGetValue(current_token().raw, out value) == false) {
                    throw new Exception();
                }
                if (ctype != null) {
                    if (CType.EqualType(ctype, value.Ctype) == false) {
                        throw new Exception("");
                    }
                }
                ctype = new CType.TypedefedType(current_token().raw, value.Ctype);
                next_token();
            } else if (is_type_qualifier()) {
                typeQualifier.Marge(type_qualifier());
            } else if (is_function_specifier()) {
                functionSpecifier.Marge(function_specifier());
            } else {
                throw new Exception("");
            }
        }
        private bool is_storage_class_specifier() {
            return Peek(Token.TokenKind.AUTO, Token.TokenKind.REGISTER, Token.TokenKind.STATIC, Token.TokenKind.EXTERN, Token.TokenKind.TYPEDEF);
        }
        private StorageClass storage_class_specifier() {
            switch (current_token().kind) {
                case Token.TokenKind.AUTO:
                    next_token();
                    return StorageClass.Auto;
                case Token.TokenKind.REGISTER:
                    next_token();
                    return StorageClass.Register;
                case Token.TokenKind.STATIC:
                    next_token();
                    return StorageClass.Static;
                case Token.TokenKind.EXTERN:
                    next_token();
                    return StorageClass.Extern;
                case Token.TokenKind.TYPEDEF:
                    next_token();
                    return StorageClass.Typedef;
                default:
                    throw new Exception();
            }
        }
        private bool is_type_specifier() {
            return Peek(Token.TokenKind.VOID, Token.TokenKind.CHAR, Token.TokenKind.INT, Token.TokenKind.FLOAT, Token.TokenKind.DOUBLE, Token.TokenKind.SHORT, Token.TokenKind.LONG, Token.TokenKind.SIGNED, Token.TokenKind.UNSIGNED
);
        }
        private TypeSpecifier type_specifier() {
            switch (current_token().kind) {
                case Token.TokenKind.VOID:
                    next_token();
                    return TypeSpecifier.Void;
                case Token.TokenKind.CHAR:
                    next_token();
                    return TypeSpecifier.Char;
                case Token.TokenKind.INT:
                    next_token();
                    return TypeSpecifier.Int;
                case Token.TokenKind.FLOAT:
                    next_token();
                    return TypeSpecifier.Float;
                case Token.TokenKind.DOUBLE:
                    next_token();
                    return TypeSpecifier.Double;
                case Token.TokenKind.SHORT:
                    next_token();
                    return TypeSpecifier.Short;
                case Token.TokenKind.LONG:
                    next_token();
                    return TypeSpecifier.Long;
                case Token.TokenKind.SIGNED:
                    next_token();
                    return TypeSpecifier.Signed;
                case Token.TokenKind.UNSIGNED:
                    next_token();
                    return TypeSpecifier.Unsigned;
                default:
                    throw new Exception();
            }
        }
        private bool is_struct_or_union_specifier() {
            return Peek(Token.TokenKind.STRUCT, Token.TokenKind.UNION);
        }
        private bool is_enum_specifier() {
            return Peek(Token.TokenKind.ENUM);
        }
        private bool is_TYPEDEF_NAME() {
            return current_token().kind == Token.TokenKind.TYPE_NAME;
        }

        private bool is_typedefed_type(string v) {
            return typedef_scope.ContainsKey(v);
        }

        private bool is_type_qualifier() {
            return Peek(Token.TokenKind.CONST, Token.TokenKind.VOLATILE, Token.TokenKind.RESTRICT, Token.TokenKind.NEAR, Token.TokenKind.FAR);
        }
        private TypeQualifier type_qualifier() {
            switch (current_token().kind) {
                case Token.TokenKind.CONST:
                    next_token();
                    return TypeQualifier.Const;
                case Token.TokenKind.VOLATILE:
                    next_token();
                    return TypeQualifier.Volatile;
                case Token.TokenKind.RESTRICT:
                    next_token();
                    return TypeQualifier.Restrict;
                case Token.TokenKind.NEAR:
                    next_token();
                    return TypeQualifier.Near;
                case Token.TokenKind.FAR:
                    next_token();
                    return TypeQualifier.Far;
                default:
                    throw new Exception();
            }
        }

        private bool is_function_specifier() {
            return Peek(Token.TokenKind.INLINE);
        }
        private FunctionSpecifier function_specifier() {
            switch (current_token().kind) {
                case Token.TokenKind.INLINE:
                    next_token();
                    return FunctionSpecifier.Inline;
                default:
                    throw new Exception();
            }
        }

        private int anony = 0;

        private CType struct_or_union_specifier() {
            var struct_or_union = current_token().kind;
            Read(Token.TokenKind.STRUCT, Token.TokenKind.UNION);

            if (is_tagname()) {
                var ident = tagname();
                var ctype = new CType.TaggedType.StructUnionType(struct_or_union == Token.TokenKind.STRUCT, ident, false);
                // ctype を tag に 登録
                tag_scope.Add(ident, ctype);
                if (Peek('{')) {
                    Read('{');
                    ctype.struct_declarations = struct_declarations();
                    Read('}');
                }
                return ctype;
            } else {
                var ident = $"${struct_or_union}_{anony++}";
                var ctype = new CType.TaggedType.StructUnionType(struct_or_union == Token.TokenKind.STRUCT, ident, true);
                // ctype を tag に 登録
                tag_scope.Add(ident, ctype);
                Read('{');
                ctype.struct_declarations = struct_declarations();
                Read('}');
                return ctype;
            }
        }
        private List<Tuple<string, CType, AST.Expression>> struct_declarations() {
            var items = new List<Tuple<string, CType, AST.Expression>>();
            items.AddRange(struct_declaration());
            while (is_struct_declaration()) {
                items.AddRange(struct_declaration());
            }
            return items;
        }

        private bool is_struct_declaration() {
            return is_specifier_qualifiers();
        }

        private List<Tuple<string, CType, AST.Expression>> struct_declaration() {
            CType baseType = specifier_qualifiers();
            var ret = struct_declarator_list(baseType);
            Read(';');
            return ret;
        }

        private bool is_init_declarator() {
            return is_declarator();
        }

        private AST.Declaration init_declarator(CType ctype, StorageClass storage_class) {
            string ident = "";
            List<CType> stack = new List<CType>() { new CType.StubType() };
            declarator(ref ident, stack, 0);
            ctype = CType.Resolve(ctype, stack);
            AST.Declaration decl;
            if (Peek('=')) {
                if (storage_class == StorageClass.Typedef || storage_class == StorageClass.Extern) {
                    // 初期化式を持つことができない記憶クラス指定子
                    throw new Exception();
                }

                if (ctype.IsFunction()) {
                    // 変数じゃない
                    throw new Exception("");
                }
                Read('=');
                var init = initializer();
                decl = new AST.Declaration.VariableDeclaration(ident, ctype, storage_class, init);
            } else if (storage_class == StorageClass.Typedef) {
                var tdecl = new AST.Declaration.TypeDeclaration(ident, ctype);
                decl = tdecl;
                typedef_scope.Add(ident, tdecl);
            } else if (ctype.IsFunction()) {
                decl = new AST.Declaration.FunctionDeclaration(ident, ctype, storage_class);
                ident_scope.Add(ident, new IdentifierValue.Declaration(decl));
            } else {
                decl = new AST.Declaration.VariableDeclaration(ident, ctype, storage_class, null);
                ident_scope.Add(ident, new IdentifierValue.Declaration(decl));
            }
            return decl;
        }

        private bool is_specifier_qualifier() {
            return (is_type_specifier() ||
                is_struct_or_union_specifier() ||
                is_enum_specifier() ||
                is_TYPEDEF_NAME() ||
                is_type_qualifier());
        }

        private void specifier_qualifier(ref CType ctype, ref TypeSpecifier typeSpecifier, ref TypeQualifier typeQualifier) {
            if (is_type_specifier()) {
                typeSpecifier = typeSpecifier.Marge(type_specifier());
            } else if (is_struct_or_union_specifier()) {
                if (ctype != null) {
                    throw new Exception("");
                }
                ctype = struct_or_union_specifier();
            } else if (is_enum_specifier()) {
                if (ctype != null) {
                    throw new Exception("");
                }
                ctype = enum_specifier();
            } else if (is_TYPEDEF_NAME()) {
                AST.Declaration.TypeDeclaration value;
                if (typedef_scope.TryGetValue(current_token().raw, out value) == false) {
                    throw new Exception();
                }
                if (ctype != null) {
                    throw new Exception("");
                }
                ctype = new CType.TypedefedType(current_token().raw, value.Ctype);
                next_token();
            } else if (is_type_qualifier()) {
                typeQualifier.Marge(type_qualifier());
            } else {
                throw new Exception("");
            }
        }
        private bool is_specifier_qualifiers() {
            return is_specifier_qualifier();
        }

        private CType specifier_qualifiers() {
            CType baseType = null;
            TypeSpecifier typeSpecifier = TypeSpecifier.None;
            TypeQualifier typeQualifier = TypeQualifier.None;

            specifier_qualifier(ref baseType, ref typeSpecifier, ref typeQualifier);
            while (is_specifier_qualifier()) {
                specifier_qualifier(ref baseType, ref typeSpecifier, ref typeQualifier);
            }

            if (typeSpecifier != TypeSpecifier.None) {
                if (baseType != null) {
                    throw new Exception("");
                } else {
                    baseType = new CType.BasicType(typeSpecifier);
                }
            } else if (baseType == null) {
                baseType = new CType.BasicType(TypeSpecifier.None);
            }
            return new CType.TypeQualifierType(baseType, typeQualifier);
        }


        private List<Tuple<string, CType, AST.Expression>> struct_declarator_list(CType ctype) {
            var ret = new List<Tuple<string, CType, AST.Expression>>();
            ret.Add(struct_declarator(ctype));
            while (Peek(',')) {
                Read(',');
                ret.Add(struct_declarator(ctype));
            }
            return ret;
        }
        private Tuple<string, CType, AST.Expression> struct_declarator(CType ctype) {
            Tuple<string, CType> decl = null;
            string ident = null;
            if (is_declarator()) {
                List<CType> stack = new List<CType>() { new CType.StubType() };
                declarator(ref ident, stack, 0);
                ctype = CType.Resolve(ctype, stack);
            }
            AST.Expression expr = null;
            if (Peek(':')) {
                Read(':');
                expr = constant_expression();
            }
            return Tuple.Create(ident, ctype, expr);

        }
        private CType enum_specifier() {
            Read(Token.TokenKind.ENUM);

            if (is_tagname()) {
                var ident = tagname();
                var ctype = new CType.TaggedType.EnumType(ident, false);
                // ctype を tag に 登録
                tag_scope.Add(ident, ctype);
                if (Peek('{')) {
                    Read('{');
                    enumerator_list(ctype);
                    Read('}');
                }
                return ctype;
            } else {
                var ident = $"$enum_{anony++}";
                var ctype = new CType.TaggedType.EnumType(ident, true);
                Read('{');
                enumerator_list(ctype);
                Read('}');
                return ctype;
            }
        }
        private void enumerator_list(CType.TaggedType.EnumType ctype) {
            var ret = new List<Tuple<string, int>>();
            ctype.enumerator_list = ret;
            var e = enumerator(ctype, 0);
            ident_scope.Add(e.Item1, new IdentifierValue.EnumValue(ctype, e.Item1));
            ret.Add(e);
            while (Peek(',')) {
                var i = e.Item2 + 1;
                Read(',');
                if (is_enumerator() == false) {
                    break;
                }
                e = enumerator(ctype, i);
                ident_scope.Add(e.Item1, new IdentifierValue.EnumValue(ctype, e.Item1));
                ret.Add(e);
            }
        }

        private bool is_enumerator() {
            return is_identifier();
        }

        private Tuple<string, int> enumerator(CType ctype, int i) {
            var ident = IDENTIFIER();
            if (Peek('=')) {
                Read('=');
                var expr = constant_expression();
                i = AST.ConstantEval(expr);
            }
            return Tuple.Create(ident, i);
        }
        private bool is_declarator() {
            return is_pointer() || is_direct_declarator();
        }
        private void declarator(ref string ident, List<CType> stack, int index) {
            if (is_pointer()) {
                pointer(stack, index);
            }
            direct_declarator(ref ident, stack, index);
        }

        private bool is_direct_declarator() {
            return Peek('(') || is_identifier() || is_tagname();    // ToDo: typedefの多重宣言に対するアドホックな修正。
        }
        private void direct_declarator(ref string ident, List<CType> stack, int index) {
            if (Peek('(')) {
                Read('(');
                stack.Add(new CType.StubType());
                declarator(ref ident, stack, index + 1);
                Read(')');
            } else {
                ident = current_token().raw;
                next_token();
            }
            more_direct_declarator(stack, index);
        }
        private void more_direct_declarator(List<CType> stack, int index) {
            if (Peek('[')) {
                Read('[');
                // array
                int len = -1;
                if (Peek(']') == false) {
                    var expr = constant_expression();
                    len = AST.ConstantEval(expr);
                }
                Read(']');
                stack[index] = new CType.ArrayType(len, stack[index]);
                more_direct_declarator(stack, index);
            } else if (Peek('(')) {
                Read('(');
                if (Peek(')')) {
                    // k&r or ANSI empty parameter list
                    Read(')');
                    stack[index] = new CType.FunctionType(null, false, stack[index]);
                    more_direct_declarator(stack, index);
                } else if (is_identifier_list()) {
                    // K&R parameter name list
                    var args = identifier_list().Select(x => Tuple.Create(x, (CType)new CType.BasicType(TypeSpecifier.None))).ToList();
                    Read(')');
                    stack[index] = new CType.FunctionType(args, false, stack[index]);
                    more_direct_declarator(stack, index);
                } else {
                    // ANSI parameter list
                    bool vargs = false;
                    var args = parameter_type_list(ref vargs);
                    Read(')');
                    stack[index] = new CType.FunctionType(args, vargs, stack[index]);
                    more_direct_declarator(stack, index);

                }
            } else {
                //_epsilon_
            }
        }

        private bool is_pointer() {
            return Peek('*');
        }

        private void pointer(List<CType> stack, int index) {
            Read('*');
            stack[index] = new CType.PointerType(stack[index]);
            TypeQualifier typeQualifier = TypeQualifier.None;
            while (is_type_qualifier()) {
                typeQualifier = typeQualifier.Marge(type_qualifier());
            }
            stack[index] = new CType.TypeQualifierType(stack[index], typeQualifier);

            if (is_pointer()) {
                pointer(stack, index);
            }
        }
        private bool is_parameter_type_list() {
            return is_parameter_declaration();
        }
        private List<Tuple<string, CType>> parameter_type_list(ref bool vargs) {
            var items = new List<Tuple<string, CType>>();
            items.Add(parameter_declaration());
            while (Peek(',')) {
                Read(',');
                if (Peek(Token.TokenKind.ELLIPSIS)) {
                    Read(Token.TokenKind.ELLIPSIS);
                    vargs = true;
                    break;
                } else {
                    items.Add(parameter_declaration());
                }
            }
            return items;
        }
        public bool is_parameter_declaration() {
            return is_declaration_specifier(null, TypeSpecifier.None);
        }
        private Tuple<string, CType> parameter_declaration() {
            StorageClass storageClass;
            CType baseType = declaration_specifiers(out storageClass);

            if (is_declarator_or_abstract_declarator()) {
                string ident = "";
                List<CType> stack = new List<CType>() { new CType.StubType() };
                declarator_or_abstract_declarator(ref ident, stack, 0);
                var ctype = CType.Resolve(baseType, stack);
                return Tuple.Create(ident, ctype);
            } else {
                return Tuple.Create((string)null, baseType);
            }

        }

        private bool is_declarator_or_abstract_declarator() {
            return is_pointer() || is_direct_declarator_or_direct_abstract_declarator();
        }
        private void declarator_or_abstract_declarator(ref string ident, List<CType> stack, int index) {
            if (is_pointer()) {
                pointer(stack, index);
                if (is_direct_declarator_or_direct_abstract_declarator()) {
                    direct_declarator_or_direct_abstract_declarator(ref ident, stack, index);
                }
            } else {
                direct_declarator_or_direct_abstract_declarator(ref ident, stack, index);
            }
        }

        private bool is_direct_declarator_or_direct_abstract_declarator() {
            return is_identifier() || Peek('(', '[');
        }

        private void direct_declarator_or_direct_abstract_declarator(ref string ident, List<CType> stack, int index) {
            if (is_identifier()) {
                ident = IDENTIFIER();
                more_dd_or_dad(stack, index);
            } else if (Peek('(')) {
                Read('(');
                if (Peek(')')) {
                    // function?
                } else if (is_parameter_type_list()) {
                    // function 
                    // ANSI parameter list
                    bool vargs = false;
                    var args = parameter_type_list(ref vargs);
                } else {
                    stack.Add(new CType.StubType());
                    declarator_or_abstract_declarator(ref ident, stack, index + 1);
                }
                Read(')');
                more_dd_or_dad(stack, index);
            } else if (Peek('[')) {
                Read('[');
                int len = -1;
                if (Peek(']') == false) {
                    var expr = constant_expression();
                    len = AST.ConstantEval(expr);
                }
                Read(']');
                stack[index] = new CType.ArrayType(len, stack[index]);
                more_dd_or_dad(stack, index);
            } else {
                throw new Exception();
            }

        }
        private void more_dd_or_dad(List<CType> stack, int index) {
            if (Peek('(')) {
                Read('(');
                if (Peek(')')) {
                    // function?
                    stack[index] = new CType.FunctionType(null, false, stack[index]);
                } else if (is_parameter_type_list()) {
                    // function 
                    // ANSI parameter list
                    bool vargs = false;
                    var args = parameter_type_list(ref vargs);
                    stack[index] = new CType.FunctionType(args, vargs, stack[index]);
                } else {
                    // K&R parameter name list
                    var args = identifier_list().Select(x => Tuple.Create(x, (CType)new CType.BasicType(TypeSpecifier.None))).ToList();
                    stack[index] = new CType.FunctionType(args, false, stack[index]);
                }
                Read(')');
                more_dd_or_dad(stack, index);
            } else if (Peek('[')) {
                Read('[');
                int len = -1;
                if (Peek(']') == false) {
                    var expr = constant_expression();
                    len = AST.ConstantEval(expr);
                }
                Read(']');
                stack[index] = new CType.ArrayType(len, stack[index]);
                more_dd_or_dad(stack, index);
            } else {
                // _epsilon_
            }
        }

        private bool is_identifier_list() {
            return is_identifier();
        }

        private List<string> identifier_list() {
            var items = new List<string>();
            items.Add(IDENTIFIER());
            while (Peek(',')) {
                Read(',');
                items.Add(IDENTIFIER());
            }
            return items;
        }


        private AST.Initializer initializer() {
            if (Peek('{')) {
                Read('{');
                var ret = initializer_list();
                if (Peek(',')) {
                    Read(',');
                }
                Read('}');
                return new AST.Initializer.CompilxInitializer(ret);
            } else {
                return new AST.Initializer.SimpleInitializer(assignment_expression());
            }
        }
        private List<AST.Initializer> initializer_list() {
            var ret = new List<AST.Initializer>();
            ret.Add(initializer());
            while (Peek(',')) {
                Read(',');
                ret.Add(initializer());
            }
            return ret;
        }

        private bool is_type_name() {
            return is_specifier_qualifiers();
        }

        private CType type_name() {
            CType baseType = specifier_qualifiers();
            if (is_abstract_declarator()) {
                List<CType> stack = new List<CType>() { new CType.StubType() };
                abstract_declarator(stack, 0);
                baseType = CType.Resolve(baseType, stack);
            }
            return baseType;
        }
        private bool is_abstract_declarator() {
            return (is_pointer() || is_direct_abstract_declarator());
        }
        private void abstract_declarator(List<CType> stack, int index) {
            if (is_pointer()) {
                pointer(stack, index);
                if (is_direct_abstract_declarator()) {
                    direct_abstract_declarator(stack, index);
                }
            } else {
                direct_abstract_declarator(stack, index);
            }
        }
        private bool is_direct_abstract_declarator() {
            return Peek('(', '[');
        }

        private void direct_abstract_declarator(List<CType> stack, int index) {
            if (Peek('(')) {
                Read('(');
                if (is_abstract_declarator()) {
                    stack.Add(new CType.StubType());
                    abstract_declarator(stack, index + 1);
                } else if (Peek(')') == false) {
                    // ansi args
                    bool vargs = false;
                    var items = parameter_type_list(ref vargs);
                } else {
                    // k&r or ansi
                }
                Read(')');
                more_direct_abstract_declarator(stack, index);
            } else {
                Read('[');
                int len = -1;
                if (Peek(']') == false) {
                    var expr = constant_expression();
                    len = AST.ConstantEval(expr);
                }
                Read(']');
                stack[index] = new CType.ArrayType(len, stack[index]);
                more_direct_abstract_declarator(stack, index);
            }
        }

        private void more_direct_abstract_declarator(List<CType> stack, int index) {
            if (Peek('[')) {
                Read('[');
                int len = -1;
                if (Peek(']') == false) {
                    var expr = constant_expression();
                    len = AST.ConstantEval(expr);
                }
                Read(']');
                stack[index] = new CType.ArrayType(len, stack[index]);
                more_direct_abstract_declarator(stack, index);
            } else if (Peek('(')) {
                Read('(');
                if (Peek(')') == false) {
                    bool vargs = false;
                    var items = parameter_type_list(ref vargs);
                    stack[index] = new CType.FunctionType(items, vargs, stack[index]);
                } else {
                    stack[index] = new CType.FunctionType(null, false, stack[index]);
                }
                Read(')');
                more_direct_abstract_declarator(stack, index);
            } else {
                // _epsilon_
            }
        }

        private string IDENTIFIER() {
            if (is_identifier() == false) {
                throw new Exception();
            }
            var ret = current_token().raw;
            next_token();
            return ret;
        }


        private AST.Statement statement() {
            if ((is_identifier() && is_nexttoken(':')) || Peek(Token.TokenKind.CASE, Token.TokenKind.DEFAULT)) {
                return labeled_statement();
            } else if (Peek('{')) {
                return compound_statement();
            } else if (Peek(Token.TokenKind.IF, Token.TokenKind.SWITCH)) {
                return selection_statement();
            } else if (Peek(Token.TokenKind.WHILE, Token.TokenKind.DO, Token.TokenKind.FOR)) {
                return iteration_statement();
            } else if (Peek(Token.TokenKind.GOTO, Token.TokenKind.CONTINUE, Token.TokenKind.BREAK, Token.TokenKind.RETURN)) {
                return jump_statement();
            } else if (Peek(Token.TokenKind.__ASM__)) {
                return gnu_asm_statement();
            } else {
                return expression_statement();
            }

        }

        private AST.Statement labeled_statement() {
            if (Peek(Token.TokenKind.CASE)) {
                Read(Token.TokenKind.CASE);
                var expr = constant_expression();
                Read(':');
                var stmt = statement();
                return new AST.Statement.CaseStatement(expr, stmt);
            } else if (Peek(Token.TokenKind.DEFAULT)) {
                Read(Token.TokenKind.DEFAULT);
                Read(':');
                var stmt = statement();
                return new AST.Statement.DefaultStatement(stmt);
            } else {
                var ident = IDENTIFIER();
                Read(':');
                var stmt = statement();
                return new AST.Statement.GenericLabeledStatement(ident, stmt);
            }
        }
        private AST.Statement expression_statement() {
            AST.Statement ret;
            if (!Peek(';')) {
                var expr = expression();
                ret = new AST.Statement.ExpressionStatement(expr);
            } else {
                ret = new AST.Statement.EmptyStatement();
            }
            Read(';');
            return ret;
        }
        private AST.Statement compound_statement() {
            tag_scope = tag_scope.Extend();
            ident_scope = ident_scope.Extend();
            Read('{');
            var decls = new List<AST.Declaration>();
            while (is_declaration()) {
                var d = declaration();
                if (d != null) {
                    decls.AddRange(d);
                }
            }
            var stmts = new List<AST.Statement>();
            while (Peek('}') == false) {
                stmts.Add(statement());
            }
            Read('}');
            var stmt = new AST.Statement.CompoundStatement(decls, stmts, tag_scope, ident_scope);
            ident_scope = ident_scope.Parent;
            tag_scope = tag_scope.Parent;
            return stmt;

        }
        private AST.Statement selection_statement() {
            if (Peek(Token.TokenKind.IF)) {
                Read(Token.TokenKind.IF);
                Read('(');
                var cond = expression();
                Read(')');
                var then_stmt = statement();
                AST.Statement else_stmt = null;
                if (Peek(Token.TokenKind.ELSE)) {
                    Read(Token.TokenKind.ELSE);
                    else_stmt = statement();
                }
                return new AST.Statement.IfStatement(cond, then_stmt, else_stmt);
            }
            if (Peek(Token.TokenKind.SWITCH)) {
                Read(Token.TokenKind.SWITCH);
                Read('(');
                var cond = expression();
                Read(')');
                var ss = new AST.Statement.SwitchStatement(cond);
                break_scope = break_scope.Extend(ss);
                ss.Stmt = statement();
                break_scope = break_scope.Parent;
                return ss;
            }
            throw new Exception();
        }
        private AST.Statement iteration_statement() {
            if (Peek(Token.TokenKind.WHILE)) {
                Read(Token.TokenKind.WHILE);
                Read('(');
                var cond = expression();
                Read(')');
                var ss = new AST.Statement.WhileStatement(cond);
                break_scope = break_scope.Extend(ss);
                continue_scope = continue_scope.Extend(ss);
                ss.Stmt = statement();
                break_scope = break_scope.Parent;
                continue_scope = continue_scope.Parent;
                return ss;
            }
            if (Peek(Token.TokenKind.DO)) {
                Read(Token.TokenKind.DO);
                var ss = new AST.Statement.DoWhileStatement();
                break_scope = break_scope.Extend(ss);
                continue_scope = continue_scope.Extend(ss);
                ss.Stmt = statement();
                break_scope = break_scope.Parent;
                continue_scope = continue_scope.Parent;
                Read(Token.TokenKind.WHILE);
                Read('(');
                ss.Cond = expression();
                Read(')');
                Read(';');
                return ss;
            }
            if (Peek(Token.TokenKind.FOR)) {
                Read(Token.TokenKind.FOR);
                Read('(');

                var init = Peek(';') ? (AST.Expression)null : expression();
                Read(';');
                var cond = Peek(';') ? (AST.Expression)null : expression();
                Read(';');
                var update = Peek(')') ? (AST.Expression)null : expression();
                Read(')');
                var ss = new AST.Statement.ForStatement(init, cond, update);
                break_scope = break_scope.Extend(ss);
                continue_scope = continue_scope.Extend(ss);
                ss.Stmt = statement();
                break_scope = break_scope.Parent;
                continue_scope = continue_scope.Parent;
                return ss;
            }
            throw new Exception();

        }
        private AST.Statement jump_statement() {
            if (Peek(Token.TokenKind.GOTO)) {
                Read(Token.TokenKind.GOTO);
                var label = IDENTIFIER();
                Read(';');
                return new AST.Statement.GotoStatement(label);
            }
            if (Peek(Token.TokenKind.CONTINUE)) {
                Read(Token.TokenKind.CONTINUE);
                Read(';');
                return new AST.Statement.ContinueStatement(continue_scope.Value);
            }
            if (Peek(Token.TokenKind.BREAK)) {
                Read(Token.TokenKind.BREAK);
                Read(';');
                return new AST.Statement.BreakStatement(break_scope.Value);
            }
            if (Peek(Token.TokenKind.RETURN)) {
                Read(Token.TokenKind.RETURN);
                var expr = Peek(';') ? null : expression();
                //現在の関数の戻り値と型チェック
                Read(';');
                return new AST.Statement.ReturnStatement(expr);
            }
            throw new Exception();
        }
        private AST.Statement gnu_asm_statement() {
            // __ASM__ statement not support!!!!!
            // skil
            Read(Token.TokenKind.__ASM__);
            if (Peek(Token.TokenKind.__VOLATILE__)) {
                Read(Token.TokenKind.__VOLATILE__);
            }
            Read('(');
            Stack<char> parens = new Stack<char>();
            parens.Push('(');
            while (parens.Any()) {
                if (Peek('(', '[')) {
                    parens.Push((char)current_token().kind);
                } else if (Peek(')')) {
                    if (parens.Peek() == '(') {
                        parens.Pop();
                    } else {
                        throw new Exception();
                    }
                } else if (Peek(']')) {
                    if (parens.Peek() == '[') {
                        parens.Pop();
                    } else {
                        throw new Exception();
                    }
                } 
                next_token();
            }
            Read(';');
            return new AST.Statement.EmptyStatement();;
        }
        private AST.Expression expression() {
            var e = assignment_expression();
            if (Peek(',')) {
                var ce = new AST.Expression.CommaExpression();
                ce.expressions.Add(e);
                while (Peek(',')) {
                    Read(',');
                    e = assignment_expression();
                    ce.expressions.Add(e);
                }
                return ce;
            } else {
                return e;
            }
        }
        private AST.Expression assignment_expression() {
            var lhs = conditional_expression();
            while (is_assignment_operator()) {
                var op = assignment_operator();
                var rhs = conditional_expression();
                lhs = new AST.Expression.AssignmentExpression(op, lhs, rhs);
            }
            return lhs;

        }

        private bool is_assignment_operator() {
            return Peek((Token.TokenKind)'=', Token.TokenKind.MUL_ASSIGN, Token.TokenKind.DIV_ASSIGN, Token.TokenKind.MOD_ASSIGN, Token.TokenKind.ADD_ASSIGN, Token.TokenKind.SUB_ASSIGN, Token.TokenKind.LEFT_ASSIGN, Token.TokenKind.RIGHT_ASSIGN, Token.TokenKind.AND_ASSIGN, Token.TokenKind.XOR_ASSIGN, Token.TokenKind.OR_ASSIGN);
        }

        private string assignment_operator() {
            if (is_assignment_operator() == false) {
                throw new Exception();
            }
            var ret = current_token().raw;
            next_token();
            return ret;
        }
        private AST.Expression conditional_expression() {
            var cond = logical_OR_expression();
            if (Peek('?')) {
                Read('?');
                var then_expr = expression();
                Read(':');
                var else_expr = conditional_expression();
                return new AST.Expression.ConditionalExpression(cond, then_expr, else_expr);
            } else {
                return cond;
            }
        }
        private AST.Expression constant_expression() {
            // 定数式かどうか調べる必要がある
            return conditional_expression();

        }
        private AST.Expression logical_OR_expression() {
            var lhs = logical_AND_expression();
            while (Peek(Token.TokenKind.OR_OP)) {
                Read(Token.TokenKind.OR_OP);
                var rhs = logical_AND_expression();
                lhs = new AST.Expression.LogicalOrExpression(lhs, rhs);
            }
            return lhs;
        }
        private AST.Expression logical_AND_expression() {
            var lhs = inclusive_OR_expression();
            while (Peek(Token.TokenKind.AND_OP)) {
                Read(Token.TokenKind.AND_OP);
                var rhs = inclusive_OR_expression();
                lhs = new AST.Expression.LogicalAndExpression(lhs, rhs);
            }
            return lhs;
        }
        private AST.Expression inclusive_OR_expression() {
            var lhs = exclusive_OR_expression();
            while (Peek('|')) {
                Read('|');
                var rhs = exclusive_OR_expression();
                lhs = new AST.Expression.InclusiveOrExpression(lhs, rhs);
            }
            return lhs;
        }
        private AST.Expression exclusive_OR_expression() {
            var lhs = and_expression();
            while (Peek('^')) {
                Read('^');
                var rhs = and_expression();
                return lhs = new AST.Expression.ExclusiveOrExpression(lhs, rhs);
            }
            return lhs;
        }
        private AST.Expression and_expression() {
            var lhs = equality_expression();
            while (Peek('&')) {
                Read('&');
                var rhs = equality_expression();
                return lhs = new AST.Expression.AndExpression(lhs, rhs);
            }
            return lhs;
        }
        private AST.Expression equality_expression() {
            var lhs = relational_expression();
            while (Peek(Token.TokenKind.EQ_OP, Token.TokenKind.NE_OP)) {
                var op = current_token().raw;
                next_token();
                var rhs = relational_expression();
                lhs = new AST.Expression.EqualityExpression(op, lhs, rhs);
            }
            return lhs;
        }
        private AST.Expression relational_expression() {
            var lhs = shift_expression();
            while (Peek((Token.TokenKind)'<', (Token.TokenKind)'>', Token.TokenKind.LE_OP, Token.TokenKind.GE_OP)) {
                var op = current_token().raw;
                next_token();
                var rhs = shift_expression();
                lhs = new AST.Expression.RelationalExpression(op, lhs, rhs);
            }
            return lhs;
        }
        private AST.Expression shift_expression() {
            var lhs = additive_expression();
            while (Peek(Token.TokenKind.LEFT_OP, Token.TokenKind.RIGHT_OP)) {
                var op = current_token().raw;
                next_token();
                var rhs = additive_expression();
                lhs = new AST.Expression.ShiftExpression(op, lhs, rhs);
            }
            return lhs;

        }
        private AST.Expression additive_expression() {
            var lhs = multiplicitive_expression();
            while (Peek('+', '-')) {
                var op = current_token().raw;
                next_token();
                var rhs = multiplicitive_expression();
                lhs = new AST.Expression.AdditiveExpression(op, lhs, rhs);
            }
            return lhs;
        }
        private AST.Expression multiplicitive_expression() {
            var lhs = cast_expression();
            while (Peek('*', '/', '%')) {
                var op = current_token().raw;
                next_token();
                var rhs = cast_expression();
                lhs = new AST.Expression.MultiplicitiveExpression(op, lhs, rhs);
            }
            return lhs;
        }
        private AST.Expression cast_expression() {
            if (Peek('(')) {
                // どちらにも'('の出現が許されるためさらに先読みを行う。
                var saveCurrent = current;
                Read('(');
                if (is_type_name()) {
                    var ty = type_name();
                    Read(')');
                    var expr = cast_expression();
                    return new AST.Expression.CastExpression(ty, expr);
                } else {
                    current = saveCurrent;
                    return unary_expression();
                }
            } else {
                return unary_expression();
            }
        }

        private AST.Expression unary_expression() {
            if (Peek(Token.TokenKind.INC_OP, Token.TokenKind.DEC_OP)) {
                var op = current_token().raw;
                next_token();
                var expr = unary_expression();
                return new AST.Expression.UnaryPrefixExpression(op, expr);
            }
            if (Peek('&')) {
                next_token();
                var expr = cast_expression();
                return new AST.Expression.UnaryAddressExpression(expr);
            }
            if (Peek('*')) {
                next_token();
                var expr = cast_expression();
                return new AST.Expression.UnaryReferenceExpression(expr);
            }
            if (Peek('+')) {
                next_token();
                var expr = cast_expression();
                return new AST.Expression.UnaryPlusExpression(expr);
            }
            if (Peek('-')) {
                next_token();
                var expr = cast_expression();
                return new AST.Expression.UnaryMinusExpression(expr);
            }
            if (Peek('~')) {
                next_token();
                var expr = cast_expression();
                return new AST.Expression.UnaryNegateExpression(expr);
            }
            if (Peek('!')) {
                next_token();
                var expr = cast_expression();
                return new AST.Expression.UnaryNotExpression(expr);
            }
            if (Peek(Token.TokenKind.SIZEOF)) {
                next_token();
                if (Peek('(')) {
                    // どっちにも'('が出ることが出来るのでさらに先読みする（LL(2))
                    var saveCurrent = current;
                    Read('(');
                    if (is_type_name()) {
                        var ty = type_name();
                        Read(')');
                        return new AST.Expression.SizeofTypeExpression(ty);
                    } else {
                        current = saveCurrent;
                        var expr = unary_expression();
                        return new AST.Expression.SizeofExpression(expr);
                    }
                }
            }
            return postfix_expression();
        }

        private AST.Expression postfix_expression() {
            var expr = primary_expression();
            return more_postfix_expression(expr);

        }
        private AST.Expression more_postfix_expression(AST.Expression expr) {
            if (Peek('[')) {
                Read('[');
                var index = expression();
                Read(']');
                return more_postfix_expression(new AST.Expression.ArrayIndexExpression(expr, index));
            }
            if (Peek('(')) {
                Read('(');
                List<AST.Expression> args = null;
                if (Peek(')') == false) {
                    args = argument_expression_list();
                } else {
                    args = new List<AST.Expression>();
                }
                Read(')');
                return more_postfix_expression(new AST.Expression.FunctionCallExpression(expr, args));
            }
            if (Peek('.')) {
                Read('.');
                var ident = IDENTIFIER();
                return more_postfix_expression(new AST.Expression.MemberDirectAccess(expr, ident));
            }
            if (Peek(Token.TokenKind.PTR_OP)) {
                Read(Token.TokenKind.PTR_OP);
                var ident = IDENTIFIER();
                return more_postfix_expression(new AST.Expression.MemberIndirectAccess(expr, ident));
            }
            if (Peek(Token.TokenKind.INC_OP, Token.TokenKind.DEC_OP)) {
                var op = current_token().raw;
                next_token();
                return more_postfix_expression(new AST.Expression.UnaryPostfixExpression(op, expr));
            }
            return expr;
        }
        private AST.Expression primary_expression() {
            if (is_identifier()) {
                var ident = IDENTIFIER();
                IdentifierValue value;
                if (ident_scope.TryGetValue(ident, out value) == false) {
                    //throw new Exception("未宣言");
                    return new AST.Expression.IdentifierExpression(ident);
                }
                if (value.IsVariable()) {
                    return new AST.Expression.VariableExpression(ident, value.ToVariable());
                }
                if (value.IsEnumValue()) {
                    var ev = value.ToEnumValue();
                    return new AST.Expression.EnumerationConstant(ev);
                }
                if (value.IsFunction()) {
                    return new AST.Expression.FunctionExpression(ident, value.ToFunction());
                }
                throw new Exception("");
            }
            if (is_constant()) {
                return constant();
            }
            if (is_STRING()) {
                List<string> strings = new List<string>();
                while (is_STRING()) {
                    strings.Add(STRING());
                }
                return new AST.Expression.StringExpression(strings);
            }
            if (Peek('(')) {
                Read('(');
                var expr = expression();
                Read(')');
                return expr;
            }
            throw new Exception();
        }
        private List<AST.Expression> argument_expression_list() {
            var ret = new List<AST.Expression>();
            ret.Add(assignment_expression());
            while (Peek(',')) {
                Read(',');
                ret.Add(assignment_expression());
            }
            return ret;
        }

        private bool is_constant() {
            return is_INTEGER_CONSTANT() ||
                   is_CHARACTER_CONSTANT() ||
                   is_FLOATING_CONSTANT() ||
                   is_ENUMERATION_CONSTANT();
        }

        private AST.Expression constant() {
            if (is_INTEGER_CONSTANT()) {
                var ret = INTEGER_CONSTANT();
                return new AST.Expression.IntegerConstant(ret.Item1, ret.Item2);
            }
            if (is_CHARACTER_CONSTANT()) {
                var ret = CHARACTER_CONSTANT();
                return new AST.Expression.CharacterConstant(ret);
            }
            if (is_FLOATING_CONSTANT()) {
                var ret = FLOATING_CONSTANT();
                return new AST.Expression.FloatingConstant(ret);
            }
            if (is_ENUMERATION_CONSTANT()) {
                var ret = ENUMERATION_CONSTANT();
                return new AST.Expression.EnumerationConstant(ret);
            }
            throw new Exception();
        }

    }

}

