using System;
using System.Collections.Generic;
using System.Linq;

namespace AnsiCParser {
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
                x.Fixup(s);
                return x;
            });
        }

        /// <summary>
        ///     StubTypeをtypeで置き換える。
        /// </summary>
        /// <param name="type"></param>
        protected virtual void Fixup(CType type) {
            throw new ApplicationException();
        }

        /// <summary>
        ///     型のサイズを取得
        /// </summary>
        /// <returns></returns>
        public abstract int Sizeof();

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
                if (t1 is TypedefedType || t2 is TypedefedType) {
                    if (t1 is TypedefedType) {
                        t1 = (t1 as TypedefedType).Type;
                    }
                    if (t2 is TypedefedType) {
                        t2 = (t2 as TypedefedType).Type;
                    }
                    continue;
                }
                if ((t1 as TypeQualifierType)?.Qualifier == TypeQualifier.None) {
                    t1 = (t1 as TypeQualifierType).Type;
                    continue;
                }
                if ((t2 as TypeQualifierType)?.Qualifier == TypeQualifier.None) {
                    t2 = (t2 as TypeQualifierType).Type;
                    continue;
                }
                if (t1.GetType() != t2.GetType()) {
                    return false;
                }
                if (t1 is TypeQualifierType && t2 is TypeQualifierType) {
                    if ((t1 as TypeQualifierType).Qualifier != (t2 as TypeQualifierType).Qualifier) {
                        return false;
                    }
                    t1 = (t1 as TypeQualifierType).Type;
                    t2 = (t2 as TypeQualifierType).Type;
                    continue;
                }
                if (t1 is PointerType && t2 is PointerType) {
                    t1 = (t1 as PointerType).BaseType;
                    t2 = (t2 as PointerType).BaseType;
                    continue;
                }
                if (t1 is ArrayType && t2 is ArrayType) {
                    if ((t1 as ArrayType).Length != (t2 as ArrayType).Length) {
                        return false;
                    }
                    t1 = (t1 as ArrayType).BaseType;
                    t2 = (t2 as ArrayType).BaseType;
                    continue;
                }
                if (t1 is FunctionType && t2 is FunctionType) {
                    if ((t1 as FunctionType).Arguments?.Length != (t2 as FunctionType).Arguments?.Length) {
                        return false;
                    }
                    if ((t1 as FunctionType).HasVariadic != (t2 as FunctionType).HasVariadic) {
                        return false;
                    }
                    if ((t1 as FunctionType).Arguments != null && (t2 as FunctionType).Arguments != null) {
                        if ((t1 as FunctionType).Arguments.Zip((t2 as FunctionType).Arguments, (x, y) => IsEqual(x.Type, y.Type)).All(x => x) == false) {
                            return false;
                        }
                    }
                    t1 = (t1 as FunctionType).ResultType;
                    t2 = (t2 as FunctionType).ResultType;
                    continue;
                }
                if (t1 is StubType && t2 is StubType) {
                    throw new CompilerException.InternalErrorException(Location.Empty, Location.Empty, "スタブ型同士の比較はできません。（本処理系の実装の誤りが原因です。）");
                }
                if (t1 is TaggedType.StructUnionType && t2 is TaggedType.StructUnionType) {
                    if ((t1 as TaggedType.StructUnionType).Kind != (t2 as TaggedType.StructUnionType).Kind) {
                        return false;
                    }
                    if ((t1 as TaggedType.StructUnionType).IsAnonymous != (t2 as TaggedType.StructUnionType).IsAnonymous) {
                        return false;
                    }
                    if ((t1 as TaggedType.StructUnionType).TagName != (t2 as TaggedType.StructUnionType).TagName) {
                        return false;
                    }
                    if ((t1 as TaggedType.StructUnionType).Members.Count != (t2 as TaggedType.StructUnionType).Members.Count) {
                        return false;
                    }
                    if ((t1 as TaggedType.StructUnionType).Members.Zip((t2 as TaggedType.StructUnionType).Members, (x, y) => IsEqual(x.Type, y.Type)).All(x => x) == false) {
                        return false;
                    }
                    return true;
                }
                if (t1 is BasicType && t2 is BasicType) {
                    if ((t1 as BasicType).Kind != (t2 as BasicType).Kind) {
                        return false;
                    }
                    return true;
                }
                throw new CompilerException.InternalErrorException(Location.Empty, Location.Empty, "型の比較方法が定義されていません。（本処理系の実装の誤りが原因です。）");
            }
        }

        public static BasicType CreateVoid() {
            return new BasicType(BasicType.TypeKind.Void);
        }

        public static BasicType CreateChar() {
            return new BasicType(BasicType.TypeKind.Char);
        }

        public static BasicType CreateUnsignedChar() {
            return new BasicType(BasicType.TypeKind.UnsignedChar);
        }

        public static BasicType CreateUnsignedShortInt() {
            return new BasicType(BasicType.TypeKind.UnsignedShortInt);
        }

        public static BasicType CreateUnsignedInt() {
            return new BasicType(BasicType.TypeKind.UnsignedInt);
        }

        public static BasicType CreateSignedInt() {
            return new BasicType(BasicType.TypeKind.SignedInt);
        }

        public static BasicType CreateFloat() {
            return new BasicType(BasicType.TypeKind.Float);
        }

        public static BasicType CreateDouble() {
            return new BasicType(BasicType.TypeKind.Double);
        }

        public static BasicType CreateLongDouble() {
            return new BasicType(BasicType.TypeKind.LongDouble);
        }

        public static ArrayType CreateArray(int length, CType type) {
            return new ArrayType(length, type);
        }

        public static PointerType CreatePointer(CType type) {
            return new PointerType(type);
        }

        // 処理系定義の特殊型

        public static BasicType CreateSizeT() {
            return new BasicType(BasicType.TypeKind.UnsignedLongInt);
        }

        public static BasicType CreatePtrDiffT() {
            return new BasicType(BasicType.TypeKind.SignedLongInt);
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
                return new TypeQualifierType(UnwrapTypeQualifier(), GetTypeQualifier() | typeQualifier);
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
            return unwrappedSelf is BasicType && kind.Contains((unwrappedSelf as BasicType).Kind);
        }

        /// <summary>
        ///     型別名と型修飾を無視した型を得る。
        /// </summary>
        /// <returns></returns>
        public CType Unwrap() {
            var self = this;
            for (;;) {
                if (self is TypedefedType) {
                    self = (self as TypedefedType).Type;
                    continue;
                }
                if (self is TypeQualifierType) {
                    self = (self as TypeQualifierType).Type;
                    continue;
                }
                break;
            }
            return self;
        }

        /// <summary>
        ///     基本型
        /// </summary>
        /// <remarks>
        ///     6.7.2 型指定子
        ///     制約
        ///     それぞれの宣言の宣言指定子列の中で，又はそれぞれの構造体宣言及び型名の型指定子型修飾子並びの中で，少なくとも一つの型指定子を指定しなければならない。型指定子の並びは，次に示すもののいずれか一つでなければならない.
        ///     - void
        ///     - char
        ///     - signed char
        ///     - unsigned char
        ///     - short，signed short，short int，signed short int
        ///     - unsigned short，unsigned short int
        ///     - int，signed，signed int
        ///     - unsigned，unsigned int
        ///     - long，signed long，long int，signed long int
        ///     - unsigned long，unsigned long int
        ///     - long long，signed long long，long long int，signed long long int
        ///     - unsigned long long，unsigned long long int
        ///     - float
        ///     - double
        ///     - long double
        ///     - _Bool
        ///     - float _Complex
        ///     - double _Complex
        ///     - long double _Complex
        ///     - float _Imaginary
        ///     - double _Imaginary
        ///     - long double _Imaginary
        ///     - 構造体共用体指定子
        ///     - 列挙型指定子
        ///     - 型定義名
        /// </remarks>
        public class BasicType : CType {
            public enum TypeKind {
                KAndRImplicitInt,
                Void,
                Char,
                SignedChar,
                UnsignedChar,
                SignedShortInt,
                UnsignedShortInt,
                SignedInt,
                UnsignedInt,
                SignedLongInt,
                UnsignedLongInt,
                SignedLongLongInt,
                UnsignedLongLongInt,
                Float,
                Double,
                LongDouble,
                _Bool,
                Float_Complex,
                Double_Complex,
                LongDouble_Complex,
                Float_Imaginary,
                Double_Imaginary,
                LongDouble_Imaginary
            }

            public BasicType(TypeSpecifier typeSpecifier) : this(ToKind(typeSpecifier)) {
            }

            public BasicType(TypeKind kind) {
                Kind = kind;
            }

            public TypeKind Kind { get; }

            private static TypeKind ToKind(TypeSpecifier typeSpecifier) {
                switch (typeSpecifier) {
                    case TypeSpecifier.None:
                        return TypeKind.KAndRImplicitInt;
                    case TypeSpecifier.Void:
                        return TypeKind.Void;
                    case TypeSpecifier.Char:
                        return TypeKind.Char;
                    case TypeSpecifier.Signed | TypeSpecifier.Char:
                        return TypeKind.SignedChar;
                    case TypeSpecifier.Unsigned | TypeSpecifier.Char:
                        return TypeKind.UnsignedChar;
                    case TypeSpecifier.Short:
                    case TypeSpecifier.Signed | TypeSpecifier.Short:
                    case TypeSpecifier.Short | TypeSpecifier.Int:
                    case TypeSpecifier.Signed | TypeSpecifier.Short | TypeSpecifier.Int:
                        return TypeKind.SignedShortInt;
                    case TypeSpecifier.Unsigned | TypeSpecifier.Short:
                    case TypeSpecifier.Unsigned | TypeSpecifier.Short | TypeSpecifier.Int:
                        return TypeKind.UnsignedShortInt;
                    case TypeSpecifier.Int:
                    case TypeSpecifier.Signed:
                    case TypeSpecifier.Signed | TypeSpecifier.Int:
                        return TypeKind.SignedInt;
                    case TypeSpecifier.Unsigned:
                    case TypeSpecifier.Unsigned | TypeSpecifier.Int:
                        return TypeKind.UnsignedInt;
                    case TypeSpecifier.Long:
                    case TypeSpecifier.Signed | TypeSpecifier.Long:
                    case TypeSpecifier.Long | TypeSpecifier.Int:
                    case TypeSpecifier.Signed | TypeSpecifier.Long | TypeSpecifier.Int:
                        return TypeKind.SignedLongInt;
                    case TypeSpecifier.Unsigned | TypeSpecifier.Long:
                    case TypeSpecifier.Unsigned | TypeSpecifier.Long | TypeSpecifier.Int:
                        return TypeKind.UnsignedLongInt;
                    case TypeSpecifier.LLong:
                    case TypeSpecifier.Signed | TypeSpecifier.LLong:
                    case TypeSpecifier.LLong | TypeSpecifier.Int:
                    case TypeSpecifier.Signed | TypeSpecifier.LLong | TypeSpecifier.Int:
                        return TypeKind.SignedLongLongInt;
                    case TypeSpecifier.Unsigned | TypeSpecifier.LLong:
                    case TypeSpecifier.Unsigned | TypeSpecifier.LLong | TypeSpecifier.Int:
                        return TypeKind.UnsignedLongLongInt;
                    case TypeSpecifier.Float:
                        return TypeKind.Float;
                    case TypeSpecifier.Double:
                        return TypeKind.Double;
                    case TypeSpecifier.Long | TypeSpecifier.Double:
                        return TypeKind.LongDouble;
                    case TypeSpecifier._Bool:
                        return TypeKind._Bool;
                    case TypeSpecifier.Float | TypeSpecifier._Complex:
                        return TypeKind.Float_Complex;
                    case TypeSpecifier.Double | TypeSpecifier._Complex:
                        return TypeKind.Double_Complex;
                    case TypeSpecifier.Long | TypeSpecifier.Double | TypeSpecifier._Complex:
                        return TypeKind.LongDouble_Complex;
                    case TypeSpecifier.Float | TypeSpecifier._Imaginary:
                        return TypeKind.Float_Imaginary;
                    case TypeSpecifier.Double | TypeSpecifier._Imaginary:
                        return TypeKind.Double_Imaginary;
                    case TypeSpecifier.Long | TypeSpecifier.Double | TypeSpecifier._Imaginary:
                        return TypeKind.LongDouble_Imaginary;
                    default:
                        throw new CompilerException.SpecificationErrorException(Location.Empty, Location.Empty, "型指定子の並びが制約を満たしていません。");
                }
            }

            protected override void Fixup(CType type) {
                // 基本型なのでFixup不要
            }

            /// <summary>
            ///     サイズ取得
            /// </summary>
            /// <returns></returns>
            public override int Sizeof() {
                return CType.Sizeof(Kind);
            }

            public override string ToString() {
                switch (Kind) {
                    case TypeKind.KAndRImplicitInt:
                        return "int";
                    case TypeKind.Void:
                        return "void";
                    case TypeKind.Char:
                        return "char";
                    case TypeKind.SignedChar:
                        return "signed char";
                    case TypeKind.UnsignedChar:
                        return "unsigned char";
                    case TypeKind.SignedShortInt:
                        return "signed short int";
                    case TypeKind.UnsignedShortInt:
                        return "unsigned short int";
                    case TypeKind.SignedInt:
                        return "signed int";
                    case TypeKind.UnsignedInt:
                        return "unsigned int";
                    case TypeKind.SignedLongInt:
                        return "signed long int";
                    case TypeKind.UnsignedLongInt:
                        return "unsigned long int";
                    case TypeKind.SignedLongLongInt:
                        return "signed long long int";
                    case TypeKind.UnsignedLongLongInt:
                        return "unsigned long long int";
                    case TypeKind.Float:
                        return "float";
                    case TypeKind.Double:
                        return "double";
                    case TypeKind.LongDouble:
                        return "long double";
                    case TypeKind._Bool:
                        return "_Bool";
                    case TypeKind.Float_Complex:
                        return "float _Complex";
                    case TypeKind.Double_Complex:
                        return "double _Complex";
                    case TypeKind.LongDouble_Complex:
                        return "long double _Complex";
                    case TypeKind.Float_Imaginary:
                        return "float _Imaginary";
                    case TypeKind.Double_Imaginary:
                        return "double _Imaginary";
                    case TypeKind.LongDouble_Imaginary:
                        return "long double _Imaginary";
                    default:
                        return $"<{Kind}>";
                }
            }
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
                    return 4;
                case BasicType.TypeKind.UnsignedLongLongInt:
                    return 4;
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

        /// <summary>
        ///     タグ付き型
        /// </summary>
        public abstract class TaggedType : CType {
            protected TaggedType(string tagName, bool isAnonymous) {
                TagName = tagName;
                IsAnonymous = isAnonymous;
            }

            public string TagName { get; }

            public bool IsAnonymous { get; }

            /// <summary>
            ///     構造体・共用体型
            /// </summary>
            public class StructUnionType : TaggedType {
                public enum StructOrUnion {
                    Struct,
                    Union
                }

                public StructUnionType(StructOrUnion kind, string tagName, bool isAnonymous) : base(tagName, isAnonymous) {
                    Kind = kind;
                }

                public StructOrUnion Kind { get; }

                public List<MemberInfo> Members { get; internal set; }

                protected override void Fixup(CType type) {
                    for (var i = 0; i < Members.Count; i++) {
                        var member = Members[i];
                        if (member.Type is StubType) {
                            Members[i] = new MemberInfo(member.Ident, type, member.BitSize);
                        }
                        else {
                            member.Type.Fixup(type);
                        }
                    }

                    foreach (var member in Members) {
                        member.Type.Fixup(type);
                    }
                }

                public override int Sizeof() {
                    // ビットフィールドは未実装
                    if (Kind == StructOrUnion.Struct) {
                        return Members.Sum(x => x.Type.Sizeof());
                    }
                    return Members.Max(x => x.Type.Sizeof());
                }

                public override string ToString() {
                    var sb = new List<string> {Kind == StructOrUnion.Struct ? "strunct" : "union", TagName};
                    if (Members != null) {
                        sb.Add("{");
                        sb.AddRange(Members.SelectMany(x =>
                            new[] {x.Type?.ToString(), x.Ident, x.BitSize > 0 ? $":{x.BitSize}" : null, ";"}.Where(y => y != null)
                        ));
                        sb.Add("}");
                    }
                    sb.Add(";");
                    return string.Join(" ", sb);
                }

                public class MemberInfo {
                    public MemberInfo(string ident, CType type, int? bitSize) {
                        if (bitSize.HasValue) {
                            // 制約
                            // - ビットフィールドの幅を指定する式は，整数定数式でなければならない。
                            //   - その値は，0 以上でなければならず，コロン及び式が省略された場合，指定された型のオブジェクトがもつビット数を超えてはならない。
                            //   - 値が 0 の場合，その宣言に宣言子があってはならない。
                            // - ビットフィールドの型は，修飾版又は非修飾版の_Bool，signed int，unsigned int 又は他の処理系定義の型でなければならない。
                            if (!type.Unwrap().IsBasicType(BasicType.TypeKind._Bool, BasicType.TypeKind.SignedInt, BasicType.TypeKind.UnsignedInt)) {
                                throw new CompilerException.SpecificationErrorException(Location.Empty, Location.Empty, "ビットフィールドの型は，修飾版又は非修飾版の_Bool，signed int，unsigned int 又は他の処理系定義の型でなければならない。");
                            }
                            if (bitSize.Value < 0) {
                                throw new CompilerException.SpecificationErrorException(Location.Empty, Location.Empty, "ビットフィールドの幅の値は，0 以上でなければならない。");
                            }
                            if (bitSize.Value > type.Sizeof() * 8) {
                                throw new CompilerException.SpecificationErrorException(Location.Empty, Location.Empty, "ビットフィールドの幅の値は，指定された型のオブジェクトがもつビット数を超えてはならない。");
                            }
                            if (bitSize.Value == 0) {
                                // 値が 0 の場合，その宣言に宣言子があってはならない。
                                // 宣言子がなく，コロン及び幅だけをもつビットフィールド宣言は，名前のないビットフィールドを示す。
                                // この特別な場合として，幅が 0 のビットフィールド構造体メンバは，前のビットフィールド（もしあれば）が割り付けられていた単位に，それ以上のビットフィールドを詰め込まないことを指定する。
                                if (ident != null) {
                                    throw new CompilerException.SpecificationErrorException(Location.Empty, Location.Empty, "ビットフィールドの幅の値が 0 の場合，その宣言に宣言子(名前)があってはならない");
                                }
                            }
                            Ident = ident;
                            Type = type;
                            BitSize = bitSize.Value;
                        }
                        else {
                            Ident = ident;
                            Type = type;
                            BitSize = -1;
                        }
                    }

                    public string Ident { get; }

                    public CType Type { get; }

                    public int BitSize { get; }
                }
            }

            /// <summary>
            ///     列挙型
            /// </summary>
            public class EnumType : TaggedType {
                public EnumType(string tagName, bool isAnonymous) : base(tagName, isAnonymous) {
                }


                public List<MemberInfo> Members { get; set; }

                protected override void Fixup(CType type) {
                }

                public override int Sizeof() {
                    return Sizeof(BasicType.TypeKind.SignedInt);
                }

                public override string ToString() {
                    var sb = new List<string> {"enum", TagName};
                    if (Members != null) {
                        sb.Add("{");
                        sb.AddRange(Members.SelectMany(x =>
                            new[] {x.Ident, "=", x.Value.ToString(), ","}
                        ));
                        sb.Add("}");
                    }
                    sb.Add(";");
                    return string.Join(" ", sb);
                }

                /// <summary>
                ///     列挙型で宣言されている列挙定数
                /// </summary>
                /// <remarks>
                ///     6.4.4.3 列挙定数
                ///     意味規則
                ///     列挙定数として宣言された識別子は，型 int をもつ。
                /// </remarks>
                public class MemberInfo {
                    public MemberInfo(EnumType parentType, string ident, int value) {
                        ParentType = parentType;
                        Ident = ident;
                        Value = value;
                    }

                    public string Ident { get; }

                    public EnumType ParentType { get; }

                    public int Value { get; }
                }
            }
        }

        /// <summary>
        ///     スタブ型（型の解決中でのみ用いる他の型が入る穴）
        /// </summary>
        public class StubType : CType {
            public override int Sizeof() {
                throw new CompilerException.InternalErrorException(Location.Empty, Location.Empty, "スタブ型のサイズを取得しようとしました。（想定では発生しないはずですが、本実装の型解決処理にどうやら誤りがあるようです。）。");
            }

            public override string ToString() {
                return "$";
            }
        }

        /// <summary>
        ///     関数型
        /// </summary>
        public class FunctionType : CType {
            private ArgumentInfo[] _arguments;

            public FunctionType(List<ArgumentInfo> arguments, bool hasVariadic, CType resultType) {
                // 6.7.5.3 関数宣言子（関数原型を含む）
                // 制約 
                // 関数宣言子は，関数型又は配列型を返却値の型として指定してはならない。(返却値の型が確定するFixupメソッド中で行う)
                // 仮引数宣言に記憶域クラス指定子として，register 以外のものを指定してはならない。
                // 関数定義の一部でない関数宣言子における識別子並びは，空でなければならない。(これは関数定義/宣言中で行う。)
                // 関数定義の一部である関数宣言子の仮引数型並びにある仮引数は，型調整後に不完全型をもってはならない。(これは関数定義/宣言中で行う。)
                // 

                Arguments = arguments?.ToArray();
                ResultType = resultType;
                HasVariadic = hasVariadic;
            }

            /// <summary>
            ///     引数の情報
            ///     nullの場合、int foo(); のように識別子並びが空であることを示す。
            ///     空の場合、int foo(void); のように唯一のvoidであることを示す。
            ///     一つ以上の要素を持つ場合、int foo(int, double); のように引数を持つことを示す。また、引数リストにvoid型の要素は含まれない。
            ///     仮引数宣言に記憶域クラス指定子として，register 以外のものを指定してはならない。
            /// </summary>
            public ArgumentInfo[] Arguments {
                get { return _arguments; }
                set {
                    if (value != null) {
                        // 6.7.5.3 関数宣言子（関数原型を含む）
                        // 制約 
                        // 仮引数宣言に記憶域クラス指定子として，register 以外のものを指定してはならない。
                        foreach (var arg in value) {
                            if (arg.StorageClass != StorageClassSpecifier.None && arg.StorageClass != StorageClassSpecifier.Register) {
                                throw new CompilerException.SpecificationErrorException(Location.Empty, Location.Empty, "仮引数宣言に記憶域クラス指定子として，register 以外のものを指定してはならない。");
                            }
                        }
                        // 意味規則
                        // 並びの中の唯一の項目が void 型で名前のない仮引数であるという特別な場合，関数が仮引数をもたないことを指定する。
                        if (value.Any(x => x.Type.IsVoidType())) {
                            if (value.Length != 1) {
                                throw new CompilerException.SpecificationErrorException(Location.Empty, Location.Empty, "仮引数宣言並びがvoid 型を含むが唯一ではない。");
                            }
                            if (value.First().Ident != null) {
                                throw new CompilerException.SpecificationErrorException(Location.Empty, Location.Empty, "仮引数宣言並び中のvoid型が名前を持っている。");
                            }
                            // 空で置き換える。
                            value = new ArgumentInfo[0];
                        }
                    }

                    _arguments = value;
                }
            }

            /// <summary>
            ///     戻り値型
            /// </summary>
            public CType ResultType { get; private set; }

            /// <summary>
            ///     可変長引数の有無
            /// </summary>
            public bool HasVariadic { get; }

            protected override void Fixup(CType type) {
                if (ResultType is StubType) {
                    ResultType = type;
                }
                else {
                    ResultType.Fixup(type);
                }
                // 関数宣言子は，関数型又は配列型を返却値の型として指定してはならない。
                if (ResultType.IsFunctionType() || ResultType.IsArrayType()) {
                    throw new CompilerException.SpecificationErrorException(Location.Empty, Location.Empty, "関数宣言子は，関数型又は配列型を返却値の型として指定してはならない。");
                }
            }

            public override int Sizeof() {
                throw new CompilerException.InternalErrorException(Location.Empty, Location.Empty, "関数型のサイズは取得できません。（C言語規約上では、関数識別子はポインタ型に変換されているはずなので、これは本処理系に誤りがあることを示しています。）");
            }

            public enum FunctionStyle {
                OldStyle,       // 古い形式の関数宣言型（引数部が識別子並び）
                NewStyle,       // 新しい形式の関数宣言型（引数部が仮引数型並び）
                AmbiguityStyle, // 引数が省略されており曖昧
                InvalidStyle,   // 不正な形式
            }

            public FunctionStyle GetFunctionStyle() {
                var candidate = FunctionStyle.AmbiguityStyle;
                if (Arguments == null) {
                    return candidate;
                }
                foreach (var x in Arguments) {
                    if ((x.Type as CType.BasicType)?.Kind == CType.BasicType.TypeKind.KAndRImplicitInt) {
                        // 型が省略されている＝識別子並びの要素
                        System.Diagnostics.Debug.Assert(!string.IsNullOrEmpty(x.Ident));
                        if (candidate == FunctionStyle.AmbiguityStyle || candidate == FunctionStyle.OldStyle) {
                            // 現在の候補が古い形式、もしくは、曖昧形式なら、現在の候補を古い形式として継続判定
                            candidate = FunctionStyle.OldStyle;
                        } else {
                            // それ以外の場合は、不正な形式なので打ち切り。
                            // 識別子並び中に型名を記述してしまった場合もこのエラーになる
                            return FunctionStyle.InvalidStyle;
                        }
                    } else {
                        // 型が省略されていない＝仮引数型並びの要素
                        if (candidate == FunctionStyle.AmbiguityStyle || candidate == FunctionStyle.NewStyle) {
                            // 現在の候補が新しい形式、もしくは、曖昧形式なら、現在の候補を新しい形式として継続判定
                            candidate = FunctionStyle.NewStyle;
                        } else {
                            // それ以外の場合は、不正な形式なので打ち切り。
                            // 識別子並び中に型名を記述してしまった場合もこのエラーになる
                            return FunctionStyle.InvalidStyle;
                        }
                    }
                }
                return candidate;
            }

            public override string ToString() {
                var sb = new List<string> {ResultType.ToString()};
                if (Arguments != null) {
                    sb.Add("(");
                    sb.Add(string.Join(", ", Arguments.Select(x => x.Type.ToString())));
                    if (HasVariadic) {
                        sb.Add(", ...");
                    }
                    sb.Add(")");
                }
                sb.Add(";");
                return string.Join(" ", sb);
            }

            public class ArgumentInfo {
                public ArgumentInfo(string ident, StorageClassSpecifier storageClass, CType type) {
                    Ident = ident;
                    StorageClass = storageClass;
                    // 6.7.5.3 関数宣言子（関数原型を含む）
                    // 制約
                    // 仮引数を“～型の配列”とする宣言は，“～型への修飾されたポインタ”に型調整する。
                    // そのときの型修飾子は，配列型派生の[及び]の間で指定したものとする。
                    // 配列型派生の[及び]の間にキーワード static がある場合，その関数の呼出しの際に対応する実引数の値は，大きさを指定する式で指定される数以上の要素をもつ配列の先頭要素を指していなければならない。
                    CType elementType;
                    if (type.IsArrayType(out elementType)) {
                        //ToDo: 及び。の間の型修飾子、static について実装
                        Console.Error.WriteLine($"仮引数 {ident} は“～型の配列”として宣言されていますが、6.7.5.3 関数宣言子の制約に従って“～型への修飾されたポインタ”に型調整されます。");
                        type = CreatePointer(elementType);
                    }
                    Type = type;
                }

                public string Ident { get; }

                public StorageClassSpecifier StorageClass { get; }

                // 関数型として外から見える引数型
                public CType Type { get; }
            }
        }

        /// <summary>
        ///     ポインタ型
        /// </summary>
        public class PointerType : CType {
            public PointerType(CType type) {
                BaseType = type;
            }

            public CType BaseType { get; private set; }

            protected override void Fixup(CType type) {
                if (BaseType is StubType) {
                    BaseType = type;
                }
                else {
                    BaseType.Fixup(type);
                }
            }

            public override int Sizeof() {
                return Sizeof(BasicType.TypeKind.SignedInt);
            }

            public override string ToString() {
                return "*" + BaseType;
            }
        }

        /// <summary>
        ///     配列型
        /// </summary>
        public class ArrayType : CType {
            public ArrayType(int length, CType type) {
                Length = length;
                BaseType = type;
            }

            /// <summary>
            ///     配列長(-1は指定無し)
            /// </summary>
            public int Length { get; set; }

            public CType BaseType { get; private set; }

            protected override void Fixup(CType type) {
                if (BaseType is StubType) {
                    BaseType = type;
                }
                else {
                    BaseType.Fixup(type);
                }
            }

            public override int Sizeof() {
                return Length < 0 ? Sizeof(BasicType.TypeKind.SignedInt) : BaseType.Sizeof() * Length;
            }

            public override string ToString() {
                return BaseType + $"[{Length}]";
            }
        }

        /// <summary>
        ///     Typedefされた型
        /// </summary>
        public class TypedefedType : CType {
            public TypedefedType(string ident, CType type) {
                Ident = ident;
                Type = type;
            }

            public string Ident { get; }

            public CType Type { get; }

            public override int Sizeof() {
                return Type.Sizeof();
            }

            public override string ToString() {
                return Ident;
            }
        }

        /// <summary>
        ///     型修飾子
        /// </summary>
        public class TypeQualifierType : CType {
            public TypeQualifierType(CType type, TypeQualifier qualifier) {
                Type = type;
                Qualifier = qualifier;
            }

            public CType Type { get; private set; }

            public TypeQualifier Qualifier { get; set; }

            protected override void Fixup(CType type) {
                if (Type is StubType) {
                    Type = type;
                }
                else {
                    Type.Fixup(type);
                }
            }

            public override int Sizeof() {
                return Type.Sizeof();
            }

            public override string ToString() {
                var sb = new List<string> {
                    (Qualifier & TypeQualifier.Const) != 0 ? "const" : null,
                    (Qualifier & TypeQualifier.Volatile) != 0 ? "volatile" : null,
                    (Qualifier & TypeQualifier.Restrict) != 0 ? "restrict" : null,
                    (Qualifier & TypeQualifier.Near) != 0 ? "near" : null,
                    (Qualifier & TypeQualifier.Far) != 0 ? "far" : null,
                    Type.ToString()
                };
                return string.Join(" ", sb.Where(x => x != null));
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
            if (t1.IsQualifiedType() && t2.IsQualifiedType()) {

                var ta1 = t1 as CType.TypeQualifierType;
                var ta2 = t2 as CType.TypeQualifierType;
                if (ta1.Qualifier != ta2.Qualifier) {
                    return null;
                }
                var ret = CompositeType(ta1.Type, ta2.Type);
                if (ret == null) {
                    return null;
                }
                return new TypeQualifierType(ret, ta1.Qualifier);
            }
            if (t1 is TypedefedType) {

                var ta1 = t1 as CType.TypedefedType;
                return CompositeType(ta1.Type, t2);
            }
            if (t2 is TypedefedType) {

                var ta2 = t2 as CType.TypedefedType;
                return CompositeType(t1, ta2.Type);
            }
            if (t1.IsPointerType() && t2.IsPointerType()) {
                var ta1 = t1 as CType.PointerType;
                var ta2 = t2 as CType.PointerType;
                var ret = CompositeType(ta1.BaseType, ta2.BaseType);
                if (ret == null) {
                    return null;
                }
                return new PointerType(ret);
            }
            if ((t1.IsStructureType() && t2.IsStructureType()) ||(t1.IsUnionType() && t2.IsUnionType())) {
                var ta1 = t1 as CType.TaggedType.StructUnionType;
                var ta2 = t2 as CType.TaggedType.StructUnionType;
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
                    if (ta1.Members[i].Ident != ta2.Members[i].Ident) {
                        return null;
                    }
                    if (ta1.Members[i].BitSize != ta2.Members[i].BitSize) {
                        return null;
                    }
                    var composited = CompositeType(ta1.Members[i].Type, ta2.Members[i].Type);
                    if (composited == null) {
                        return null;
                    }
                    newMembers.Add(new TaggedType.StructUnionType.MemberInfo(ta1.Members[i].Ident, composited, ta1.Members[i].BitSize));
                }
                newType.Members = newMembers;
                return newType;
            }
            if (t1.IsArrayType() && t2.IsArrayType()) {
                // 一方の型が既知の固定長をもつ配列の場合，合成型は，その大きさの配列とする。
                // そうでなく，一方の型が可変長の配列の場合，合成型はその型とする   
                // 可変長配列は未実装
                var ta1 = t1.Unwrap() as CType.ArrayType;
                var ta2 = t2.Unwrap() as CType.ArrayType;
                if ((ta1.Length != -1 && ta2.Length == -1)
                    || (ta1.Length == -1 && ta2.Length != -1)) {
                    int len = ta1.Length != -1 ? ta1.Length : ta2.Length;
                    var ret = CompositeType(ta1.BaseType, ta2.BaseType);
                    if (ret == null) {
                        return null;
                    }
                    return CreateArray(len, ret);
                } else if (ta1.Length == ta2.Length) {
                    var ret = CompositeType(ta1.BaseType, ta2.BaseType);
                    if (ret == null) {
                        return null;
                    }
                    return CreateArray(ta1.Length, ret);
                }
                return null;
            }
            if (t1.IsFunctionType() && t2.IsFunctionType()) {
                var ta1 = t1.Unwrap() as CType.FunctionType;
                var ta2 = t2.Unwrap() as CType.FunctionType;
                if ((ta1.Arguments != null && ta2.Arguments == null) || (ta1.Arguments == null && ta2.Arguments != null)) {
                    // 一方の型だけが仮引数型並びをもつ関数型（関数原型）の場合，合成型は，その仮引数型並びをもつ関数原型とする。
                    var arguments = (ta1.Arguments != null ? ta1.Arguments : ta2.Arguments).ToList();
                    var retType = CompositeType(ta1.ResultType, ta2.ResultType);
                    if (retType == null) {
                        return null;
                    }
                    return new CType.FunctionType(arguments, ta1.HasVariadic, retType);
                } else if (ta1.Arguments != null && ta2.Arguments != null) {
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
                        // 既定の実引数拡張を適用
                        var pt1 = Specification.DefaultArgumentPromotion(ta1.Arguments[i].Type);
                        var pt2 = Specification.DefaultArgumentPromotion(ta2.Arguments[i].Type);
                        var newArgument = CompositeType(pt1, pt2);
                        if (newArgument == null) {
                            return null;
                        }
                        if (ta1.Arguments[i].StorageClass != ta2.Arguments[i].StorageClass) {
                            return null;
                        }
                        var storageClass = ta1.Arguments[i].StorageClass;
                        newArguments.Add(new FunctionType.ArgumentInfo(null, storageClass, newArgument));
                    }

                    // 戻り値の型が適合する？
                    var retType = CompositeType(ta1.ResultType, ta2.ResultType);
                    if (retType == null) {
                        return null;
                    }

                    return new CType.FunctionType(newArguments, ta1.HasVariadic, retType);
                } else if (ta1.Arguments == null && ta2.Arguments == null) {
                    // 両方仮引数が無いなら仮引数を持たない合成型とする
                    var retType = CompositeType(ta1.ResultType, ta2.ResultType);
                    if (retType == null) {
                        return null;
                    }
                    return new CType.FunctionType(null, ta1.HasVariadic, retType);
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
        /// <param name="baseType"></param>
        /// <returns></returns>
        public static bool CheckContainOldStyleArgument(CType t1) {
            for (;;) {
                if (t1 is TypeQualifierType) {
                    t1 = (t1 as TypeQualifierType).Type;
                    continue;
                }
                if (t1 is TypedefedType) {
                    // typedef型については宣言時に警告を出し、使用時には警告を出さない。
                    return false;
                }
                if (t1 is PointerType) {
                    t1 = (t1 as PointerType).BaseType;
                    continue;
                }
                if (t1 is ArrayType) {
                    t1 = (t1 as ArrayType).BaseType;
                    continue;
                }
                if (t1 is TaggedType.StructUnionType) {
                    if ((t1 as TaggedType.StructUnionType).Members == null) {
                        return false;
                    }
                    return (t1 as TaggedType.StructUnionType).Members.Any(x => CheckContainOldStyleArgument(x.Type));
                }
                if (t1 is FunctionType) {
                    if ((t1 as FunctionType).Arguments == null) {
                        return false;
                    }
                    if ((t1 as FunctionType).Arguments.Any(x => CheckContainOldStyleArgument(x.Type))) {
                        return true;
                    }
                    t1 = (t1 as FunctionType).ResultType;
                    continue;
                }
                if (t1 is BasicType) {
                    if ((t1 as BasicType).Kind == BasicType.TypeKind.KAndRImplicitInt) {
                        return true;
                    }
                }
                return false;
            }
        }
    }
}