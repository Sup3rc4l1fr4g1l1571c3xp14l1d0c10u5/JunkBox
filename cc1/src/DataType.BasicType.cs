using System;
using System.Collections.Generic;
using System.Linq;

namespace AnsiCParser {

    namespace DataType {
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

            public enum TypeKind : byte {
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

            /// <summary>
            /// 複製を作成
            /// </summary>
            /// <returns></returns>
            public override CType Duplicate() {
                return this;
            }

            public static BasicType FromTypeSpecifier(TypeSpecifier typeSpecifier) {
                return new BasicType(ToKind(typeSpecifier));
            }

            /// <summary>
            /// 基本型コンストラクタ
            /// </summary>
            /// <param name="kind"></param>
            private BasicType(TypeKind kind) {
                Kind = kind;
            }

            private static Dictionary<TypeKind, BasicType> InstanceType { get; } = Enum.GetValues(typeof(TypeKind)).Cast<TypeKind>().ToDictionary(x => x, x => new BasicType(x));

            public static BasicType Create(TypeKind kind) {
                return InstanceType[kind];
            }


            /// <summary>
            /// 基本型種別
            /// </summary>
            public TypeKind Kind {
                get;
            }

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

            /// <summary>
            /// 型の構築時にStubTypeを埋める
            /// </summary>
            /// <param name="type"></param>
            public override void Fixup(CType type) {
                // 基本型なのでFixup不要
            }

            /// <summary>
            /// 型のバイトサイズを取得（ビットフィールドの場合、元の型のサイズ）
            /// </summary>
            /// <returns></returns>
            public override int SizeOf() {
                return CType.SizeOf(Kind);
            }

            /// <summary>
            ///     型の境界調整（アラインメント）を取得（ビットフィールドの場合、元の型のアラインメント）
            /// </summary>
            /// <returns></returns>
            public override int AlignOf() {
                return CType.AlignOf(Kind);
            }

        }
    }

}
