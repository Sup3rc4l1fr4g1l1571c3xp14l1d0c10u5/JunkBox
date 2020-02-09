using System;
using System.Collections.Generic;
using System.Linq;

namespace AnsiCParser {
    namespace DataType {
        /// <summary>
        ///     C言語の型を表現
        /// </summary>
        public abstract class CType {
            /// <summary>
            ///     型指定を解決する
            /// </summary>
            /// <param name="baseType"></param>
            /// <param name="stack"></param>
            /// <returns></returns>
            public static CType Resolve(CType baseType, List<CType> stack) {
                return stack.Aggregate(baseType, (s, x) => {
                    if (x is StubType) {
                        return s;
                    }
                    else {
                        x.Fixup(s);
                        return x;
                    }
                });
            }

            /// <summary>
            ///     StubTypeをtypeで置き換える。
            /// </summary>
            /// <param name="type"></param>
            public virtual void Fixup(CType type) {
                throw new ApplicationException();
            }

            /// <summary>
            /// C言語書式で型の文字列表現を取得する
            /// </summary>
            /// <returns></returns>
            public override string ToString() {
                return this.Accept(new ToStringVisitor(), "");
            }

            /// <summary>
            /// 型情報の deep copy を作る
            /// </summary>
            /// <returns></returns>
            public abstract CType Duplicate();

            /// <summary>
            ///     型のサイズを取得（ビットフィールドの場合、元の型のサイズ）
            /// </summary>
            /// <returns></returns>
            public abstract int Sizeof();

            /// <summary>
            ///     型の境界調整（アラインメント）を取得（ビットフィールドの場合、元の型のアラインメント）
            /// </summary>
            /// <returns></returns>
            public abstract int Alignof();

            /// <summary>
            ///     型が同一であるかどうかを比較する(適合ではない。)
            /// </summary>
            /// <param name="t1"></param>
            /// <param name="t2"></param>
            /// <returns></returns>
            public static bool IsEqual(CType t1, CType t2) {
                for (;;) {
                    if (ReferenceEquals(t1, t2)) {
                        return true;
                    }

                    if (t1 is TypedefType || t2 is TypedefType) {
                        if (t1 is TypedefType) {
                            t1 = ((TypedefType)t1).Type;
                        }

                        if (t2 is TypedefType) {
                            t2 = ((TypedefType)t2).Type;
                        }

                        continue;
                    }

                    if ((t1 as TypeQualifierType)?.Qualifier == TypeQualifier.None) {
                        t1 = ((TypeQualifierType)t1).Type;
                        continue;
                    }

                    if ((t2 as TypeQualifierType)?.Qualifier == TypeQualifier.None) {
                        t2 = ((TypeQualifierType)t2).Type;
                        continue;
                    }

                    if (t1.GetType() != t2.GetType()) {
                        return false;
                    }

                    if (t1 is TypeQualifierType && t2 is TypeQualifierType) {
                        var _t1 = (TypeQualifierType)t1;
                        var _t2 = (TypeQualifierType)t2;
                        if (_t1.Qualifier != _t2.Qualifier) {
                            return false;
                        }

                        t1 = _t1.Type;
                        t2 = _t2.Type;
                        continue;
                    }

                    if (t1 is PointerType && t2 is PointerType) {
                        t1 = ((PointerType)t1).ReferencedType;
                        t2 = ((PointerType)t2).ReferencedType;
                        continue;
                    }
                    if (t1 is BitFieldType && t2 is BitFieldType) {
                        var _t1 = (BitFieldType)t1;
                        var _t2 = (BitFieldType)t2;
                        if (_t1.BitOffset != _t2.BitOffset) {
                            return false;
                        }
                        if (_t1.BitWidth != _t2.BitWidth) {
                            return false;
                        }
                        t1 = _t1.Type;
                        t2 = _t2.Type;
                        continue;
                    }

                    if (t1 is ArrayType && t2 is ArrayType) {
                        var _t1 = (ArrayType)t1;
                        var _t2 = (ArrayType)t2;
                        if (_t1.Length != _t2.Length) {
                            return false;
                        }

                        t1 = _t1.ElementType;
                        t2 = _t2.ElementType;
                        continue;
                    }

                    if (t1 is FunctionType && t2 is FunctionType) {
                        var _t1 = (FunctionType)t1;
                        var _t2 = (FunctionType)t2;
                        if (_t1.Arguments?.Length != _t2.Arguments?.Length) {
                            return false;
                        }

                        if (_t1.HasVariadic != _t2.HasVariadic) {
                            return false;
                        }

                        if (_t1.Arguments != null && _t2.Arguments != null) {
                            if (_t1.Arguments.Zip(_t2.Arguments, (x, y) => IsEqual(x.Type, y.Type)).All(x => x) == false) {
                                return false;
                            }
                        }

                        t1 = _t1.ResultType;
                        t2 = _t2.ResultType;
                        continue;
                    }

                    if (t1 is StubType && t2 is StubType) {
                        throw new CompilerException.InternalErrorException(Location.Empty, Location.Empty, "スタブ型同士の比較はできません。（本処理系の実装の誤りが原因です。）");
                    }

                    if (t1 is TaggedType.StructUnionType && t2 is TaggedType.StructUnionType) {
                        var _t1 = (TaggedType.StructUnionType)t1;
                        var _t2 = (TaggedType.StructUnionType)t2;
                        if (_t1.Kind != _t2.Kind) {
                            return false;
                        }

                        if (_t1.IsAnonymous != _t2.IsAnonymous) {
                            return false;
                        }

                        if (_t1.TagName != _t2.TagName) {
                            return false;
                        }

                        if (_t1.Members.Count != _t2.Members.Count) {
                            return false;
                        }

                        if (_t1.Members.Zip(_t2.Members, (x, y) => IsEqual(x.Type, y.Type)).All(x => x) == false) {
                            return false;
                        }

                        return true;
                    }

                    if (t1 is BasicType && t2 is BasicType) {
                        if (((BasicType)t1).Kind != ((BasicType)t2).Kind) {
                            return false;
                        }

                        return true;
                    }

                    throw new CompilerException.InternalErrorException(Location.Empty, Location.Empty, "型の比較方法が定義されていません。（本処理系の実装の誤りが原因です。）");
                }
            }

            public static BasicType CreateVoid() {
                return BasicType.Create(BasicType.TypeKind.Void);
            }

            public static BasicType CreateChar() {
                return BasicType.Create(BasicType.TypeKind.Char);
            }

            public static BasicType CreateUnsignedChar() {
                return BasicType.Create(BasicType.TypeKind.UnsignedChar);
            }

            public static BasicType CreateUnsignedShortInt() {
                return BasicType.Create(BasicType.TypeKind.UnsignedShortInt);
            }

            public static BasicType CreateKAndRImplicitInt() {
                return BasicType.Create(BasicType.TypeKind.KAndRImplicitInt);
            }

            public static BasicType CreateUnsignedInt() {
                return BasicType.Create(BasicType.TypeKind.UnsignedInt);
            }

            public static BasicType CreateSignedInt() {
                return BasicType.Create(BasicType.TypeKind.SignedInt);
            }

            public static BasicType CreateUnsignedLongInt() {
                return BasicType.Create(BasicType.TypeKind.UnsignedLongInt);
            }

            public static BasicType CreateSignedLongInt() {
                return BasicType.Create(BasicType.TypeKind.SignedLongInt);
            }
            public static BasicType CreateUnsignedLongLongInt() {
                return BasicType.Create(BasicType.TypeKind.UnsignedLongLongInt);
            }

            public static BasicType CreateSignedLongLongInt() {
                return BasicType.Create(BasicType.TypeKind.SignedLongLongInt);
            }

            public static BasicType CreateFloat() {
                return BasicType.Create(BasicType.TypeKind.Float);
            }

            public static BasicType CreateDouble() {
                return BasicType.Create(BasicType.TypeKind.Double);
            }

            public static BasicType CreateLongDouble() {
                return BasicType.Create(BasicType.TypeKind.LongDouble);
            }

            public static ArrayType CreateArray(int length, CType type) {
                return new ArrayType(length, type);
            }

            public static PointerType CreatePointer(CType type) {
                return new PointerType(type);
            }

            // 処理系定義の特殊型

            public static BasicType CreateBool() {
                return BasicType.Create(BasicType.TypeKind._Bool);
            }

            public static BasicType CreateSizeT() { 
                return BasicType.Create(BasicType.TypeKind.UnsignedLongInt);
            }

            public static BasicType CreatePtrDiffT() {
                return BasicType.Create(BasicType.TypeKind.SignedLongInt);
            }

            /// <summary>
            ///     型修飾を得る
            /// </summary>
            /// <returns></returns>
            public TypeQualifier GetTypeQualifier() {
                if (this is TypeQualifierType) {
                    return ((TypeQualifierType) this).Qualifier;
                }

                return TypeQualifier.None;
            }

            /// <summary>
            ///     型修飾を追加する
            /// </summary>
            /// <returns></returns>
            public CType WrapTypeQualifier(TypeQualifier typeQualifier) {
                if (typeQualifier != TypeQualifier.None) {
                    CType elementType;
                    int len;
                    if (this.IsArrayType(out elementType, out len)) {
                        // 6.7.3 型修飾子
                        // 配列型の指定が型修飾子を含む場合，それは要素の型を修飾するだけで，その配列型を修飾するのではない
                        // const int a[16] は (const int)の配列
                        // 紛らわしいケース
                        // - typedef int A[16]; A const x; -> const A型ではなく、const int の配列
                        // - const int A[15][24] -> (const int [15])[24]や (const int [15][24])ではなく、 const intの配列
                        var tq = elementType.GetTypeQualifier();
                        return new ArrayType(len, elementType.WrapTypeQualifier(tq | typeQualifier));
                    }
                    else {
                        return new TypeQualifierType(UnwrapTypeQualifier(), GetTypeQualifier() | typeQualifier);
                    }
                }

                return this;
            }

            /// <summary>
            ///     型修飾を除去する
            /// </summary>
            /// <returns></returns>
            public CType UnwrapTypeQualifier() {
                var self = this;
                while (self is TypeQualifierType) {
                    self = (self as TypeQualifierType).Type;
                }

                return self;
            }

            /// <summary>
            ///     void型ならば真
            /// </summary>
            /// <returns></returns>
            public bool IsVoidType() {
                var unwrappedSelf = Unwrap();
                return (unwrappedSelf as BasicType)?.Kind == BasicType.TypeKind.Void;
            }

            /// <summary>
            ///     Bool型ならば真
            /// </summary>
            /// <returns></returns>
            public bool IsBoolType() {
                var unwrappedSelf = Unwrap();
                return (unwrappedSelf as BasicType)?.Kind == BasicType.TypeKind._Bool;
            }

            /// <summary>
            ///     指定した種別の基本型なら真
            /// </summary>
            /// <param name="kind"></param>
            /// <returns></returns>
            public bool IsBasicType(params BasicType.TypeKind[] kind) {
                var unwrappedSelf = Unwrap();
                if (kind.Length > 0) {
                    if (unwrappedSelf is BasicType) {
                        var targetKind = ((BasicType)unwrappedSelf).Kind;
                        foreach (var k in kind) {
                            if (k == targetKind)
                            {
                                return true;
                            }
                        }
                    }
                    return false;
                }
                else {
                    return unwrappedSelf is BasicType;
                }
            }

            /// <summary>
            /// ビットフィールドなら真
            /// </summary>
            /// <returns></returns>
            public bool IsBitField() {
                return this is BitFieldType;
            }

            /// <summary>
            /// ビットフィールドなら真
            /// </summary>
            /// <param name="bft">ビットフィールド型にキャストした結果</param>
            /// <returns></returns>
            public bool IsBitField(out BitFieldType bft) {
                if (this is BitFieldType) {
                    bft = (this as BitFieldType);
                    return true;
                }
                else {
                    bft = null;
                    return false;
                }
            }

            [Flags]
            public enum UnwrapFlag {
                None = 0x00,
                TypedefType = 0x01,
                TypeQualifierType = 0x02,
                BitFieldType = 0x04,
                All = UnwrapFlag.TypedefType | UnwrapFlag.TypeQualifierType | UnwrapFlag.BitFieldType 
            }

            /// <summary>
            ///     型別名と型修飾（とビットフィールド修飾）を無視した型を得る。
            /// </summary>
            /// <returns></returns>
            public CType Unwrap(UnwrapFlag unwrapFlag = UnwrapFlag.All) {
                var self = this;
                for (;;) {
                    if (unwrapFlag.HasFlag(UnwrapFlag.TypedefType) && self is TypedefType) {
                        self = (self as TypedefType).Type;
                        continue;
                    }

                    if (unwrapFlag.HasFlag(UnwrapFlag.TypeQualifierType) && self is TypeQualifierType) {
                        self = (self as TypeQualifierType).Type;
                        continue;
                    }

                    if (unwrapFlag.HasFlag(UnwrapFlag.BitFieldType) && self is BitFieldType) {
                        self = (self as BitFieldType).Type;
                        continue;
                    }

                    break;
                }

                return self;
            }

            public static int Sizeof(BasicType.TypeKind kind) {
                switch (kind) {
                    case BasicType.TypeKind.KAndRImplicitInt:
                        return 4;
                    case BasicType.TypeKind.Void:
                        throw new CompilerException.SpecificationErrorException(Location.Empty, Location.Empty, "void型に対してsizeof演算子は適用できません。使いたければgccを使え。");
                    case BasicType.TypeKind.Char:
                        return 1;
                    case BasicType.TypeKind.SignedChar:
                        return 1;
                    case BasicType.TypeKind.UnsignedChar:
                        return 1;
                    case BasicType.TypeKind.SignedShortInt:
                        return 2;
                    case BasicType.TypeKind.UnsignedShortInt:
                        return 2;
                    case BasicType.TypeKind.SignedInt:
                        return 4;
                    case BasicType.TypeKind.UnsignedInt:
                        return 4;
                    case BasicType.TypeKind.SignedLongInt:
                        return 4;
                    case BasicType.TypeKind.UnsignedLongInt:
                        return 4;
                    case BasicType.TypeKind.SignedLongLongInt:
                        return 8;
                    case BasicType.TypeKind.UnsignedLongLongInt:
                        return 8;
                    case BasicType.TypeKind.Float:
                        return 4;
                    case BasicType.TypeKind.Double:
                        return 8;
                    case BasicType.TypeKind.LongDouble:
                        return 12;
                    case BasicType.TypeKind._Bool:
                        return 1;
                    case BasicType.TypeKind.Float_Complex:
                        return 4 * 2;
                    case BasicType.TypeKind.Double_Complex:
                        return 8 * 2;
                    case BasicType.TypeKind.LongDouble_Complex:
                        return 12 * 2;
                    case BasicType.TypeKind.Float_Imaginary:
                        return 4;
                    case BasicType.TypeKind.Double_Imaginary:
                        return 8;
                    case BasicType.TypeKind.LongDouble_Imaginary:
                        return 12;
                    default:
                        throw new CompilerException.InternalErrorException(Location.Empty, Location.Empty, "型のサイズを取得しようとしましたが、取得に失敗しました。（本実装の誤りだと思います。）");
                }
            }
            public static int Alignof(BasicType.TypeKind kind) {
                switch (kind) {
                    case BasicType.TypeKind.KAndRImplicitInt:
                        return 4;
                    case BasicType.TypeKind.Void:
                        throw new CompilerException.SpecificationErrorException(Location.Empty, Location.Empty, "void型に対してsizeof演算子は適用できません。使いたければgccを使え。");
                    case BasicType.TypeKind.Char:
                        return 1;
                    case BasicType.TypeKind.SignedChar:
                        return 1;
                    case BasicType.TypeKind.UnsignedChar:
                        return 1;
                    case BasicType.TypeKind.SignedShortInt:
                        return 2;
                    case BasicType.TypeKind.UnsignedShortInt:
                        return 2;
                    case BasicType.TypeKind.SignedInt:
                        return 4;
                    case BasicType.TypeKind.UnsignedInt:
                        return 4;
                    case BasicType.TypeKind.SignedLongInt:
                        return 4;
                    case BasicType.TypeKind.UnsignedLongInt:
                        return 4;
                    case BasicType.TypeKind.SignedLongLongInt:
                        return 8;
                    case BasicType.TypeKind.UnsignedLongLongInt:
                        return 8;
                    case BasicType.TypeKind.Float:
                        return 4;
                    case BasicType.TypeKind.Double:
                        return 8;
                    case BasicType.TypeKind.LongDouble:
                        return 8;
                    case BasicType.TypeKind._Bool:
                        return 1;
                    case BasicType.TypeKind.Float_Complex:
                        return 4;
                    case BasicType.TypeKind.Double_Complex:
                        return 4;
                    case BasicType.TypeKind.LongDouble_Complex:
                        return 4;
                    case BasicType.TypeKind.Float_Imaginary:
                        return 4;
                    case BasicType.TypeKind.Double_Imaginary:
                        return 4;
                    case BasicType.TypeKind.LongDouble_Imaginary:
                        return 4;
                    default:
                        throw new CompilerException.InternalErrorException(Location.Empty, Location.Empty, "型のアラインメントを取得しようとしましたが、取得に失敗しました。（本実装の誤りだと思います。）");
                }
            }

            /// <summary>
            /// 6.2.7適合型及び合成型
            /// 合成型（composite type）は，適合する二つの型から構成することができる。
            /// 合成型は，二つの型の両方に適合し，かつ次の条件を満たす型とする。
            /// - 一方の型が既知の固定長をもつ配列の場合，合成型は，その大きさの配列とする。そうでなく，一方の型が可変長の配列の場合，合成型はその型とする。
            /// - 一方の型だけが仮引数型並びをもつ関数型（関数原型）の場合，合成型は，その仮引数型並びをもつ関数原型とする。
            /// - 両方の型が仮引数型並びをもつ関数型の場合，合成仮引数型並びにおける各仮引数の型は，対応する仮引数の型の合成型とする。
            /// これらの規則は，二つの型が派生される元の型に再帰的に適用する。
            /// 内部結合又は外部結合をもつ識別子が，ある有効範囲の中で宣言され，その識別子の以前の宣言が可視であり，以前の宣言が内部結合又は外部結合を指定している場合，現在の宣言での識別子の型は合成型となる。
            /// </summary>
            /// <param name="t1"></param>
            /// <param name="t2"></param>
            /// <returns></returns>
            public static CType CompositeType(CType t1, CType t2) {
                if (ReferenceEquals(t1, t2)) {
                    return t1;
                }

                if (t1.IsQualifiedType() && t2.IsQualifiedType()) {

                    var ta1 = t1 as TypeQualifierType;
                    var ta2 = t2 as TypeQualifierType;
                    if (ta1.Qualifier != ta2.Qualifier) {
                        return null;
                    }

                    var ret = CompositeType(ta1.Type, ta2.Type);
                    if (ret == null) {
                        return null;
                    }

                    if (ret.IsArrayType()) {
                        // 6.7.3 型修飾子
                        // 配列型の指定が型修飾子を含む場合，それは要素の型を修飾するだけで，その配列型を修飾するのではない
                        var arrayType = ret as ArrayType;

                        return new ArrayType(arrayType.Length, arrayType.ElementType.WrapTypeQualifier(ta1.Qualifier));
                    }
                    else {
                        return new TypeQualifierType(ret, ta1.Qualifier);
                    }
                }

                if (t1 is TypedefType) {

                    var ta1 = t1 as TypedefType;
                    return CompositeType(ta1.Type, t2);
                }

                if (t2 is TypedefType) {

                    var ta2 = t2 as TypedefType;
                    return CompositeType(t1, ta2.Type);
                }

                if (t1.IsPointerType() && t2.IsPointerType()) {
                    var ta1 = t1 as PointerType;
                    var ta2 = t2 as PointerType;
                    var ret = CompositeType(ta1.ReferencedType, ta2.ReferencedType);
                    if (ret == null) {
                        return null;
                    }

                    return new PointerType(ret);
                }

                if ((t1.IsStructureType() && t2.IsStructureType()) || (t1.IsUnionType() && t2.IsUnionType())) {
                    var ta1 = t1 as TaggedType.StructUnionType;
                    var ta2 = t2 as TaggedType.StructUnionType;
                    if (ta1.Kind != ta1.Kind) {
                        return null;
                    }

                    if (ta1.TagName != ta2.TagName) {
                        return null;
                    }

                    if (ta1.IsAnonymous != ta2.IsAnonymous) {
                        return null;
                    }

                    if (ta1.Members.Count != ta2.Members.Count) {
                        return null;
                    }

                    var newType = new TaggedType.StructUnionType(ta1.Kind, ta1.TagName, ta1.IsAnonymous);
                    var newMembers = new List<TaggedType.StructUnionType.MemberInfo>();
                    for (var i = 0; i < ta1.Members.Count; i++) {
                        if (ta1.Members[i].Ident.Raw != ta2.Members[i].Ident.Raw) {
                            return null;
                        }

                        if (ta1.Members[i].Offset != ta2.Members[i].Offset) {
                            return null;
                        }

                        if (ta1.Members[i].Type.IsBitField() != ta2.Members[i].Type.IsBitField()) {
                            return null;
                        }

                        BitFieldType bft1, bft2;
                        if (ta1.Members[i].Type.IsBitField(out bft1) == true && ta2.Members[i].Type.IsBitField(out bft2) == true) {
                            if (bft1.BitOffset != bft2.BitOffset) {
                                return null;
                            }

                            if (bft1.BitWidth != bft2.BitWidth) {
                                return null;
                            }
                        }

                        var composited = CompositeType(ta1.Members[i].Type, ta2.Members[i].Type);
                        if (composited == null) {
                            return null;
                        }

                        newMembers.Add(new TaggedType.StructUnionType.MemberInfo(ta1.Members[i].Ident, composited, ta1.Members[i].Offset));
                    }

                    newType.Members = newMembers;
                    return newType;
                }

                if (t1.IsArrayType() && t2.IsArrayType()) {
                    // 一方の型が既知の固定長をもつ配列の場合，合成型は，その大きさの配列とする。
                    // そうでなく，一方の型が可変長の配列の場合，合成型はその型とする   
                    // 可変長配列は未実装
                    var ta1 = t1.Unwrap() as ArrayType;
                    var ta2 = t2.Unwrap() as ArrayType;
                    if ((ta1.Length != -1 && ta2.Length == -1)
                        || (ta1.Length == -1 && ta2.Length != -1)) {
                        int len = ta1.Length != -1 ? ta1.Length : ta2.Length;
                        var ret = CompositeType(ta1.ElementType, ta2.ElementType);
                        if (ret == null) {
                            return null;
                        }

                        return CreateArray(len, ret);
                    }
                    else if (ta1.Length == ta2.Length) {
                        var ret = CompositeType(ta1.ElementType, ta2.ElementType);
                        if (ret == null) {
                            return null;
                        }

                        return CreateArray(ta1.Length, ret);
                    }

                    return null;
                }

                {
#warning  "C言語の奇妙なルール「int (*)(...)型と int(...)型は同一型」を満たすためのチェック。"
                    CType rt;
                    if (t1.IsPointerType(out rt) && rt.IsFunctionType() && t2.IsFunctionType()) {
                        return CompositeType(t1, CType.CreatePointer(t2));
                    } else if (t2.IsPointerType(out rt) && rt.IsFunctionType() && t1.IsFunctionType()) {
                        return CompositeType(CType.CreatePointer(t1), t2);
                    }
                }

                if (t1.IsFunctionType() && t2.IsFunctionType()) {
                    var ta1 = t1.Unwrap() as FunctionType;
                    var ta2 = t2.Unwrap() as FunctionType;
                    if ((ta1.Arguments != null && ta2.Arguments == null) || (ta1.Arguments == null && ta2.Arguments != null)) {
                        // 一方の型だけが仮引数型並びをもつ関数型（関数原型）の場合，合成型は，その仮引数型並びをもつ関数原型とする。
                        var arguments = (ta1.Arguments ?? ta2.Arguments).ToList();
                        var retType = CompositeType(ta1.ResultType, ta2.ResultType);
                        if (retType == null) {
                            return null;
                        }

                        return new FunctionType(arguments, ta1.HasVariadic, retType);
                    }
                    else if (ta1.Arguments != null && ta2.Arguments != null) {
                        // 両方が仮引数型並びをもつ場合，仮引数の個数及び省略記号の有無に関して一致し，対応する仮引数の型が適合する。

                        // 仮引数の個数が一致？
                        if (ta1.Arguments.Length != ta2.Arguments.Length) {
                            return null;
                        }

                        // 省略記号の有無が一致？
                        if (ta1.HasVariadic != ta2.HasVariadic) {
                            return null;
                        }

                        // 対応する仮引数の型が適合する?
                        var newArguments = new List<FunctionType.ArgumentInfo>();
                        for (var i = 0; i < ta1.Arguments.Length; i++) {
                        //　
                            var pt1 = ta1.Arguments[i].Type;
                            var pt2 = ta2.Arguments[i].Type;
                            var newArgument = CompositeType(pt1, pt2);
                            if (newArgument == null) {
                                return null;
                            }

                            if (ta1.Arguments[i].StorageClass != ta2.Arguments[i].StorageClass) {
                                return null;
                            }

                            var storageClass = ta1.Arguments[i].StorageClass;
                            newArguments.Add(new FunctionType.ArgumentInfo(/*ta1.Arguments[i].Range ?? */ta2.Arguments[i].Range, null, storageClass, newArgument));
                        }

                        // 戻り値の型が適合する？
                        var retType = CompositeType(ta1.ResultType, ta2.ResultType);
                        if (retType == null) {
                            return null;
                        }

                        return new FunctionType(newArguments, ta1.HasVariadic, retType);
                    }
                    else if (ta1.Arguments == null && ta2.Arguments == null) {
                        // 両方仮引数が無いなら仮引数を持たない合成型とする
                        var retType = CompositeType(ta1.ResultType, ta2.ResultType);
                        if (retType == null) {
                            return null;
                        }

                        return new FunctionType(null, ta1.HasVariadic, retType);
                    }

                    return null;
                }

                if (IsEqual(t1, t2)) {
                    return t1;
                }

                return null;
            }

            /// <summary>
            /// 型中の関数型に（型の無い）仮引数名があるか確認
            /// </summary>
            /// <returns></returns>
            public static bool CheckContainOldStyleArgument(CType t1) {
                return CheckContainOldStyleArgument(t1, new HashSet<CType>());
            }

            private static bool CheckContainOldStyleArgument(CType t1, HashSet<CType> checkedType) {
                if (checkedType.Contains(t1)) {
                    return false;
                }

                checkedType.Add(t1);
                for (;;) {
                    if (t1 is TypeQualifierType) {
                        t1 = (t1 as TypeQualifierType).Type;
                        continue;
                    }

                    if (t1 is TypedefType) {
                        // typedef型については宣言時に警告を出し、使用時には警告を出さない。
                        return false;
                    }

                    if (t1 is PointerType) {
                        t1 = (t1 as PointerType).ReferencedType;
                        continue;
                    }

                    if (t1 is ArrayType) {
                        t1 = (t1 as ArrayType).ElementType;
                        continue;
                    }

                    if (t1 is TaggedType.StructUnionType) {
                        if ((t1 as TaggedType.StructUnionType).Members == null) {
                            return false;
                        }

                        return (t1 as TaggedType.StructUnionType).Members.Any(x => CheckContainOldStyleArgument(x.Type, checkedType));
                    }

                    if (t1 is FunctionType) {
                        if ((t1 as FunctionType).Arguments == null) {
                            return false;
                        }

                        if ((t1 as FunctionType).Arguments.Any(x => CheckContainOldStyleArgument(x.Type, checkedType))) {
                            return true;
                        }

                        t1 = (t1 as FunctionType).ResultType;
                        continue;
                    }

                    if (t1.IsBasicType(BasicType.TypeKind.KAndRImplicitInt)) {
                        return true;
                    }

                    return false;
                }
            }

            public virtual bool IsContainFlexibleArrayMemberStruct() {
                return false;
            }

        }
    }

}
