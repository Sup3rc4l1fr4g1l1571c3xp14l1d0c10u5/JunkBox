using System;
using System.Linq;

namespace AnsiCParser {
    /// <summary>
    /// 規格書の用語に対応した定義の実装
    /// </summary>
    public static class Specification {

        // 6.2.5 型
        // - オブジェクト型（object type）: オブジェクトを完全に規定する型（脚注：不完全型 (incomplete type) と関数型 (function type) 以外の サイズが確定している型）
        // - 関数型（function type）: 関数を規定する型
        // - 不完全型（incomplete type）: オブジェクトを規定する型で，その大きさを確定するのに必要な情報が欠けたもの
        //                                void型                                完全にすることのできない不完全型とする
        //                                大きさの分からない配列型              それ以降のその型の識別子の宣言（内部結合又は外部結合をもつ）で大きさを指定することによって，完全となる
        //                                内容の分からない構造体型又は共用体型  同じ有効範囲のそれ以降の同じ構造体タグ又は共用体タグの宣言で，内容を定義することによって，その型のすべての宣言に関し完全となる
        //                                
        //
        // - 標準符号付き整数型（standard signed integer type）: signed char，short int，int，long int 及び long long int の 5 種類
        // - 拡張符号付き整数型（extended signed integer type）: 処理系が独自に定義する標準符号付き整数型
        // - 符号付き整数型（signed integer type） : 標準符号付き整数型及び拡張符号付き整数型の総称
        // - 標準符号無し整数型（standard unsigned integer type）: 型_Bool，及び標準符号付き整数型に対応する符号無し整数型
        // - 拡張符号無し整数型（extended unsigned integer type）: 拡張符号付き整数型に対応する符号無し整数型
        // - 符号無し整数型（unsigned integer type）: 標準符号無し整数型及び拡張符号無し整数型の総称
        // - 標準整数型（standard integer type） : 標準符号付き整数型及び標準符号無し整数型の総称
        // - 拡張整数型（extended integer type） : 拡張符号付き整数型及び拡張符号無し整数型の総称
        //
        // - 実浮動小数点型（real floating type）: float，double 及び long doubleの 3 種類
        // - 複素数型（complex type） : float _Complex，double _Complex 及び long double _Complexの 3 種類
        // - 浮動小数点型（floating type） : 実浮動小数点型及び複素数型の総称
        // - 対応する実数型（corresponding real type） : 実浮動小数点型に対しては，同じ型を対応する実数型とする。複素数型に対しては，型名からキーワード_Complex を除いた型を，対応する実数型とする
        //
        // - 基本型（basic type） : 型 char，符号付き整数型，符号無し整数型及び浮動小数点型の総称
        // - 文字型（character type） : 三つの型 char，signed char 及び unsigned char の総称
        // - 列挙体（enumeration）: 名前付けられた整数定数値から成る。それぞれの列挙体は，異なる列挙型（enumerated type）を構成する
        // - 整数型（integer type） : 型 char，符号付き整数型，符号無し整数型，及び列挙型の総称
        // - 実数型（real type） : 整数型及び実浮動小数点型の総称
        // - 算術型（arithmetic type） : 整数型及び浮動小数点型の総称
        //
        // - 派生型（derived type）は，オブジェクト型，関数型及び不完全型から幾つでも構成することができる
        // - 派生型の種類は，次のとおり
        //   - 配列型（array type） : 要素型（element type）から派生
        //   - 構造体型（structure type） : メンバオブジェクトの空でない集合を順に割り付けたもの。各メンバは，名前を指定してもよく異なる型をもってもよい。
        //   - 共用体型（union type） : 重なり合って割り付けたメンバオブジェクトの空でない集合。各メンバは，名前を指定してもよく異なる型をもってもよい。
        //   - 関数型（function type） : 指定された返却値の型をもつ関数を表す。関数型は，その返却値の型，並びにその仮引数の個数及び型によって特徴付ける。
        //   - ポインタ型（pointer type）: 被参照型（referenced type）と呼ぶ関数型，オブジェクト型又は不完全型から派生することができる
        // - スカラ型（scalar type）: 算術型及びポインタ型の総称
        // - 集成体型（aggregate type） : 配列型及び構造体型の総称
        // - 派生宣言子型（derived declarator type） : 配列型，関数型及びポインタ型の総称
        // - 型分類（type category） : 型が派生型を含む場合，最も外側の派生とし，型が派生型を含まない場合，その型自身とする
        // - 非修飾型（unqualified type）: const，volatile，及びrestrict修飾子の一つ，二つ又は三つの組合せを持たない上記の型の総称。
        // - 修飾型（qualified type）: const，volatile，及びrestrict修飾子の一つ，二つ又は三つの組合せを持つ上記の型の総称。一つの型の修飾版と非修飾版は，同じ型分類，同じ表現及び同じ境界調整要求をもつが，異なる型とする
        //                             型修飾子をもつ型から派生したとしても，派生型はその型修飾子によって修飾されない。

        /// <summary>
        /// オブジェクト型（object type）ならば真
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        /// <remarks>
        /// オブジェクトを完全に規定する型（脚注：不完全型 (incomplete type) と関数型 (function type) 以外の サイズが確定している型）
        /// </remarks>
        public static bool IsObjectType(this CType self) {
            var unwrappedSelf = self.Unwrap();
            return !(unwrappedSelf.IsFunctionType() || unwrappedSelf.IsIncompleteType());
        }

        /// <summary>
        /// 関数型（function type）ならば真
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        /// <remarks>
        /// 関数を規定する型
        /// </remarks>
        public static bool IsFunctionType(this CType self) {
            var unwrappedSelf = self.Unwrap();
            return unwrappedSelf is CType.FunctionType;
        }
        public static bool IsFunctionType(this CType self, out CType.FunctionType funcSelf) {
            var unwrappedSelf = self.Unwrap();
            if (unwrappedSelf is CType.FunctionType) {
                funcSelf = unwrappedSelf as CType.FunctionType;
                return true;
            } else {
                funcSelf = null;
                return false;
            }
        }

        /// <summary>
        /// 不完全型（incomplete type）ならば真
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        /// <remarks>
        /// オブジェクトを規定する型で，その大きさを確定するのに必要な情報が欠けたもの
        ///   void型                                完全にすることのできない不完全型とする
        ///   大きさの分からない配列型              それ以降のその型の識別子の宣言（内部結合又は外部結合をもつ）で大きさを指定することによって，完全となる
        ///   内容の分からない構造体型又は共用体型  同じ有効範囲のそれ以降の同じ構造体タグ又は共用体タグの宣言で，内容を定義することによって，その型のすべての宣言に関し完全となる
        /// </remarks>
        public static bool IsIncompleteType(this CType self) {
            var unwrappedSelf = self.Unwrap();
            // void型  
            if (unwrappedSelf is CType.BasicType) {
                var bt = unwrappedSelf as CType.BasicType;
                return bt.Kind == CType.BasicType.TypeKind.Void;
            }
            // 大きさの分からない配列型
            if (unwrappedSelf is CType.ArrayType) {
                var at = unwrappedSelf as CType.ArrayType;
                if (at.Length == -1) {
                    return true;
                }
                return at.BaseType.IsIncompleteType();
            }
            // 内容の分からない構造体型又は共用体型
            if (unwrappedSelf is CType.TaggedType.StructUnionType) {
                var sut = unwrappedSelf as CType.TaggedType.StructUnionType;
                if (sut.Members == null) {
                    return true;
                }
                return sut.Members.Any(x => IsIncompleteType(x.Type));
            }
            return false;
        }

        /// <summary>
        /// 標準符号付き整数型（standard signed integer type）ならば真
        /// </summary>
        /// <returns></returns>
        public static bool IsStandardSignedIntegerType(this CType self) {
            var unwrappedSelf = self.Unwrap();
            if (unwrappedSelf is CType.BasicType) {
                var bt = unwrappedSelf as CType.BasicType;
                switch (bt.Kind) {
                    case CType.BasicType.TypeKind.SignedChar:   // signed char
                    case CType.BasicType.TypeKind.SignedShortInt:    // short int
                    case CType.BasicType.TypeKind.SignedInt:    // int
                    case CType.BasicType.TypeKind.SignedLongInt:    // long int
                    case CType.BasicType.TypeKind.SignedLongLongInt:    // long long int
                        return true;
                    default:
                        return false;
                }
            }
            return false;
        }

        /// <summary>
        /// 拡張符号付き整数型（extended signed integer type）ならば真
        /// </summary>
        /// <returns></returns>
        public static bool IsExtendedSignedIntegerType(this CType self) {
            var unwrappedSelf = self.Unwrap();
            return false;
        }

        /// <summary>
        /// 符号付き整数型（signed integer type）ならば真
        /// </summary>
        /// <returns></returns>
        public static bool IsSignedIntegerType(this CType self) {
            var unwrappedSelf = self.Unwrap();
            return unwrappedSelf.IsStandardSignedIntegerType() || unwrappedSelf.IsExtendedSignedIntegerType();
        }

        /// <summary>
        /// 標準符号無し整数型（standard unsigned integer type）ならば真
        /// </summary>
        /// <returns></returns>
        public static bool IsStandardUnsignedIntegerType(this CType self) {
            var unwrappedSelf = self.Unwrap();
            if (unwrappedSelf is CType.BasicType) {
                var bt = unwrappedSelf as CType.BasicType;
                switch (bt.Kind) {
                    case CType.BasicType.TypeKind.UnsignedChar:         // unsigned char
                    case CType.BasicType.TypeKind.UnsignedShortInt:     // unsigned short int
                    case CType.BasicType.TypeKind.UnsignedInt:          // unsigned int
                    case CType.BasicType.TypeKind.UnsignedLongInt:      // unsigned long int
                    case CType.BasicType.TypeKind.UnsignedLongLongInt:  // unsigned long long int
                        return true;
                    default:
                        return false;
                }
            }
            return false;
        }

        /// <summary>
        /// 拡張符号無し整数型（extended unsigned integer type）ならば真
        /// </summary>
        /// <returns></returns>
        public static bool IsExtendedUnsignedIntegerType(this CType self) {
            var unwrappedSelf = self.Unwrap();
            return false;
        }

        /// <summary>
        /// 符号無し整数型（unsigned integer type）ならば真
        /// </summary>
        /// <returns></returns>
        public static bool IsUnsignedIntegerType(this CType self) {
            var unwrappedSelf = self.Unwrap();
            return unwrappedSelf.IsStandardUnsignedIntegerType() || unwrappedSelf.IsExtendedUnsignedIntegerType();
        }

        /// <summary>
        /// 標準整数型（standard integer type）ならば真
        /// </summary>
        /// <returns></returns>
        public static bool IsStandardIntegerType(this CType self) {
            var unwrappedSelf = self.Unwrap();
            return unwrappedSelf.IsStandardSignedIntegerType() || unwrappedSelf.IsStandardUnsignedIntegerType();
        }

        /// <summary>
        /// 拡張整数型（extended integer type）ならば真
        /// </summary>
        /// <returns></returns>
        public static bool IsExtendedIntegerType(this CType self) {
            var unwrappedSelf = self.Unwrap();
            return unwrappedSelf.IsExtendedSignedIntegerType() || unwrappedSelf.IsExtendedUnsignedIntegerType();
        }

        /// <summary>
        /// 実浮動小数点型（real floating type）ならば真
        /// </summary>
        /// <returns></returns>
        public static bool IsRealFloatingType(this CType self) {
            var unwrappedSelf = self.Unwrap();
            if (unwrappedSelf is CType.BasicType) {
                var bt = unwrappedSelf as CType.BasicType;
                switch (bt.Kind) {
                    case CType.BasicType.TypeKind.Float:                // float
                    case CType.BasicType.TypeKind.Double:               // double
                    case CType.BasicType.TypeKind.LongDouble:           // long double
                        return true;
                    default:
                        return false;
                }
            }
            return false;
        }

        /// <summary>
        /// 複素数型（complex type）ならば真
        /// </summary>
        /// <returns></returns>
        public static bool IsComplexType(this CType self) {
            var unwrappedSelf = self.Unwrap();
            if (unwrappedSelf is CType.BasicType) {
                var bt = unwrappedSelf as CType.BasicType;
                switch (bt.Kind) {
                    case CType.BasicType.TypeKind.Float_Complex:                // float _Complex
                    case CType.BasicType.TypeKind.Double_Complex:               // double _Complex
                    case CType.BasicType.TypeKind.LongDouble_Complex:           // long double _Complex
                        return true;
                    default:
                        return false;
                }
            }
            return false;
        }

        /// <summary>
        /// 浮動小数点型（floating type）ならば真
        /// </summary>
        /// <returns></returns>
        public static bool IsFloatingType(this CType self) {
            var unwrappedSelf = self.Unwrap();
            return unwrappedSelf.IsRealFloatingType() || unwrappedSelf.IsComplexType();
        }

        /// <summary>
        /// 対応する実数型(corresponding real type)を取得
        /// </summary>
        /// <returns></returns>
        public static CType.BasicType GetCorrespondingRealType(this CType self) {
            var unwrappedSelf = self.Unwrap();
            if (unwrappedSelf.IsRealFloatingType()) {
                var bt = unwrappedSelf as CType.BasicType;
                return bt;
            } else if (unwrappedSelf.IsComplexType()) {
                var bt = unwrappedSelf as CType.BasicType;
                switch (bt.Kind) {
                    case CType.BasicType.TypeKind.Float_Complex:
                        return CType.CreateFloat();
                    case CType.BasicType.TypeKind.Double_Complex:
                        return CType.CreateDouble();
                    case CType.BasicType.TypeKind.LongDouble_Complex:
                        return CType.CreateLongDouble();
                    default:
                        throw new CompilerException.InternalErrorException(Location.Empty, Location.Empty, "対応する実数型を持たない_Complex型です。（本実装に誤りがあるようです。）");
                }
            } else {
                throw new CompilerException.InternalErrorException(Location.Empty, Location.Empty, "実数型以外から「対応する実数型」を得ようとしました。（本実装に誤りがあるようです。）");
            }
        }

        /// <summary>
        /// 基本型（basic type）ならば真
        /// </summary>
        /// <returns></returns>
        public static bool IsBasicType(this CType self) {
            var unwrappedSelf = self.Unwrap();
            return unwrappedSelf.IsSignedIntegerType() || unwrappedSelf.IsUnsignedIntegerType() || unwrappedSelf.IsFloatingType() || ((unwrappedSelf as CType.BasicType)?.Kind == CType.BasicType.TypeKind.Char);
        }

        /// <summary>
        /// 文字型（character type）ならば真
        /// </summary>
        /// <returns></returns>
        public static bool IsCharacterType(this CType self) {
            var unwrappedSelf = self.Unwrap();
            if (unwrappedSelf is CType.BasicType) {
                var bt = unwrappedSelf as CType.BasicType;
                switch (bt.Kind) {
                    case CType.BasicType.TypeKind.Char:             // char
                    case CType.BasicType.TypeKind.SignedChar:       // signed char
                    case CType.BasicType.TypeKind.UnsignedChar:     // unsigned char
                        return true;
                    default:
                        return false;
                }
            }
            return false;
        }

        /// <summary>
        /// 列挙型（enumerated type）ならば真
        /// </summary>
        /// <returns></returns>
        public static bool IsEnumeratedType(this CType self) {
            var unwrappedSelf = self.Unwrap();
            return unwrappedSelf is CType.TaggedType.EnumType;
        }

        /// <summary>
        /// 整数型（integer type）ならば真
        /// </summary>
        /// <returns></returns>
        public static bool IsIntegerType(this CType self) {
            var unwrappedSelf = self.Unwrap();
            return unwrappedSelf.IsSignedIntegerType() || unwrappedSelf.IsUnsignedIntegerType() || unwrappedSelf.IsEnumeratedType() || ((unwrappedSelf as CType.BasicType)?.Kind == CType.BasicType.TypeKind.Char);
        }

        /// <summary>
        /// 実数型（real type）ならば真
        /// </summary>
        /// <returns></returns>
        public static bool IsRealType(this CType self) {
            var unwrappedSelf = self.Unwrap();
            return unwrappedSelf.IsIntegerType() || unwrappedSelf.IsRealFloatingType();
        }

        /// <summary>
        /// 算術型（arithmetic type）ならば真
        /// </summary>
        /// <returns></returns>
        public static bool IsArithmeticType(this CType self) {
            var unwrappedSelf = self.Unwrap();
            return unwrappedSelf.IsIntegerType() || unwrappedSelf.IsFloatingType();
        }

        /// <summary>
        /// 配列型（array type）ならば真
        /// </summary>
        /// <returns></returns>
        public static bool IsArrayType(this CType self) {
            var unwrappedSelf = self.Unwrap();
            return unwrappedSelf is CType.ArrayType;
        }

        /// <summary>
        /// 配列型（array type）ならば真
        /// </summary>
        /// <param name="self"></param>
        /// <param name="elementType">配列型の場合、要素型が入る</param>
        /// <returns></returns>
        public static bool IsArrayType(this CType self, out CType elementType) {
            var unwrappedSelf = self.Unwrap();
            if (unwrappedSelf is CType.ArrayType) {
                elementType = (unwrappedSelf as CType.ArrayType).BaseType;
                return true;
            } else {
                elementType = null;
                return false;
            }
        }

        /// <summary>
        /// 構造体型（structure type）ならば真
        /// </summary>
        /// <returns></returns>
        public static bool IsStructureType(this CType self) {
            var unwrappedSelf = self.Unwrap();
            return (unwrappedSelf as CType.TaggedType.StructUnionType)?.Kind == CType.TaggedType.StructUnionType.StructOrUnion.Struct;
        }
        public static bool IsStructureType(this CType self, out CType.TaggedType.StructUnionType suType) {
            var unwrappedSelf = self.Unwrap();
            if ((unwrappedSelf as CType.TaggedType.StructUnionType)?.Kind == CType.TaggedType.StructUnionType.StructOrUnion.Struct) {
                suType = unwrappedSelf as CType.TaggedType.StructUnionType;
                return true;
            } else {
                suType = null;
                return false;
            }
        }

        /// <summary>
        /// 共用体型（union type）ならば真
        /// </summary>
        /// <returns></returns>
        public static bool IsUnionType(this CType self) {
            var unwrappedSelf = self.Unwrap();
            return (unwrappedSelf as CType.TaggedType.StructUnionType)?.Kind == CType.TaggedType.StructUnionType.StructOrUnion.Union;
        }
        public static bool IsUnionType(this CType self, out CType.TaggedType.StructUnionType suType) {
            var unwrappedSelf = self.Unwrap();
            if ((unwrappedSelf as CType.TaggedType.StructUnionType)?.Kind == CType.TaggedType.StructUnionType.StructOrUnion.Union) {
                suType = unwrappedSelf as CType.TaggedType.StructUnionType;
                return true;
            } else {
                suType = null;
                return false;
            }
        }

        /// <summary>
        /// ポインタ型（pointer type）ならば真
        /// </summary>
        /// <returns></returns>
        public static bool IsPointerType(this CType self) {
            var unwrappedSelf = self.Unwrap();
            return unwrappedSelf is CType.PointerType;
        }

        /// <summary>
        /// ポインタ型（pointer type）ならば真
        /// </summary>
        /// <param name="self"></param>
        /// <param name="referencedType">ポインタ型の場合、被参照型が入る</param>
        /// <returns></returns>
        public static bool IsPointerType(this CType self, out CType referencedType) {
            var unwrappedSelf = self.Unwrap();
            if (unwrappedSelf is CType.PointerType) {
                referencedType = (unwrappedSelf as CType.PointerType).BaseType;
                return true;
            } else {
                referencedType = null;
                return false;
            }
        }

        /// <summary>
        /// 派生型（derived type）ならば真
        /// </summary>
        /// <returns></returns>
        public static bool IsDerivedType(this CType self) {
            var unwrappedSelf = self.Unwrap();
            return unwrappedSelf.IsArrayType()
                   || unwrappedSelf.IsStructureType()
                   || unwrappedSelf.IsUnionType()
                   || unwrappedSelf.IsFunctionType()
                   || unwrappedSelf.IsPointerType()
                ;
        }

        /// <summary>
        /// 被参照型（referenced type）ならば真
        /// </summary>
        /// <returns></returns>
        public static bool IsReferencedType(this CType self) {
            var unwrappedSelf = self.Unwrap();
            return unwrappedSelf.IsFunctionType()
                   || unwrappedSelf.IsObjectType()
                   || unwrappedSelf.IsIncompleteType()
                ;
        }

        /// <summary>
        /// スカラ型（scalar type）ならば真
        /// </summary>
        /// <returns></returns>
        public static bool IsScalarType(this CType self) {
            var unwrappedSelf = self.Unwrap();
            return unwrappedSelf.IsArithmeticType()
                   || unwrappedSelf.IsPointerType()
                   || unwrappedSelf.IsIncompleteType()
                ;
        }

        /// <summary>
        /// 集成体型（aggregate type）ならば真
        /// </summary>
        /// <returns></returns>
        public static bool IsAggregateType(this CType self) {
            var unwrappedSelf = self.Unwrap();
            return unwrappedSelf.IsStructureType()
                   || unwrappedSelf.IsArrayType()
                ;
        }

        /// <summary>
        /// 派生宣言子型（derived declarator type）ならば真
        /// </summary>
        /// <returns></returns>
        public static bool IsDerivedDeclaratorType(this CType self) {
            var unwrappedSelf = self.Unwrap();
            return unwrappedSelf.IsFunctionType()
                   || unwrappedSelf.IsArrayType()
                   || unwrappedSelf.IsPointerType()
                ;
        }

        /// <summary>
        /// 非修飾型（unqualified type）ならば真
        /// </summary>
        /// <returns></returns>
        public static bool IsUnqualifiedType(this CType self) {
            return !(self is CType.TypeQualifierType);
        }

        /// <summary>
        /// 修飾型（qualified type）ならば真
        /// </summary>
        /// <returns></returns>
        public static bool IsQualifiedType(this CType self) {
            return self is CType.TypeQualifierType;
        }

        // 6.3 型変換
        // 暗黙の型変換（implicit conversion）
        // 明示的な型変換（explicit conversion）


        /// <summary>
        /// 整数変換の順位（integer conversion rank）
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        /// <remarks>
        /// 6.3.1.1 論理型，文字型及び整数型
        /// すべての整数型は，次のとおり定義される整数変換の順位（integer conversion rank）をもつ
        /// - 二つの符号付き整数型は，同じ表現をもつ場合であっても，同じ順位をもってはならない。
        /// - 符号付き整数型は，より小さい精度の符号付き整数型より高い順位をもたなければならない。
        /// - long long int 型は long int 型より高い順位をもたなければならない。
        ///   long int 型は int 型より高い順位をもたなければならない。
        ///   int 型は short int 型より高い順位をもたなければならない。
        ///   short int 型は signed char 型より高い順位をもたなければならない。
        /// - ある符号無し整数型に対し，対応する符号付き整数型があれば，両方の型は同じ順位をもたなければならない。
        /// - 標準整数型は，同じ幅の拡張整数型より高い順位をもたなければならない。
        /// - char 型は，signed char 型及び unsigned char 型と同じ順位をもたなければならない。
        /// - _Bool 型は，その他のすべての標準整数型より低い順位をもたなければならない。
        /// - すべての列挙型は，それぞれと適合する整数型と同じ順位をもたなければならない（6.7.2.2 参照）。
        /// - 精度の等しい拡張符号付き整数型同士の順位は処理系定義とするが，整数変換の順位を定める他の規則に従わなければならない。
        /// - 任意の整数型 T1，T2，及び T3 について，T1 が T2 より高い順位をもち，かつ T2 が T3 より高い順位をもつならば，T1 は T3 より高い順位をもたなければならない。
        /// </remarks>
        public static int IntegerConversionRank(this CType self) {
            var unwrappedSelf = self.Unwrap();
            if (unwrappedSelf.IsIntegerType()) {
                if (unwrappedSelf.IsEnumeratedType()) {
                    // すべての列挙型は，それぞれと適合する整数型と同じ順位をもたなければならない（6.7.2.2 参照）。
                    return -5;  // == signed int
                } else {
                    switch ((unwrappedSelf as CType.BasicType)?.Kind) {
                        case CType.BasicType.TypeKind.SignedLongLongInt:
                        case CType.BasicType.TypeKind.UnsignedLongLongInt:
                            //long long = unsigned long long
                            //int64_t = uint64_t
                            return -1;
                        case CType.BasicType.TypeKind.SignedLongInt:
                        case CType.BasicType.TypeKind.UnsignedLongInt:
                            //long = unsigned long
                            return -3;
                        case CType.BasicType.TypeKind.SignedInt:
                        case CType.BasicType.TypeKind.UnsignedInt:
                            //int = unsigned int
                            //int32_t = uint32_t
                            return -5;
                        case CType.BasicType.TypeKind.SignedShortInt:
                        case CType.BasicType.TypeKind.UnsignedShortInt:
                            //short = unsigned short
                            //int16_t = uint16_t
                            return -7;
                        case CType.BasicType.TypeKind.Char:
                        case CType.BasicType.TypeKind.SignedChar:
                        case CType.BasicType.TypeKind.UnsignedChar:
                            //char = signed char = unsigned char
                            //int8_t = uint8_t
                            return -9;
                        case CType.BasicType.TypeKind._Bool:
                            // bool
                            return -11;
                        default:
                            return 0;
                    }
                }
            } else {
                return 0;
            }
        }


        /// <summary>
        ///  整数拡張（integer promotion）
        /// </summary>
        /// <param name="expr"></param>
        /// <remarks>
        /// 整数拡張が適用されるのは以下の部分
        /// - 単項演算子 + - ~ のオペランド
        /// - シフト演算子（ << >> ）の各オペランド
        /// - 既定の実引数拡張中
        /// </remarks>
        public static SyntaxTree.Expression IntegerPromotion(SyntaxTree.Expression expr, int? bitfield = null) {
            if (bitfield.HasValue == false) {
                // ビットフィールドではない
                // 整数変換の順位が int 型及び unsigned int 型より低い整数型をもつオブジェクト又は式?
                if (IntegerConversionRank(expr.Type) < -5) {
                    // 元の型のすべての値を int 型で表現可能な場合，その値を int 型に変換する。そうでない場合，unsigned int 型に変換する
                    if (expr.Type.Unwrap().IsBasicType(CType.BasicType.TypeKind.UnsignedInt)) {
                        // unsigned int でないと表現できない
                        return new SyntaxTree.Expression.PostfixExpression.IntegerPromotionExpression(CType.CreateUnsignedInt(), expr);
                    } else {
                        // signed int で表現できる
                        return new SyntaxTree.Expression.PostfixExpression.IntegerPromotionExpression(CType.CreateSignedInt(), expr);
                    }
                } else {
                    // 拡張は不要
                    return expr;
                }
            } else {
                // ビットフィールドである
                switch ((expr.Type.Unwrap() as CType.BasicType)?.Kind) {
                    // _Bool 型，int 型，signed int 型，又は unsigned int 型
                    case CType.BasicType.TypeKind._Bool:
                        // 処理系依存：sizeof(_Bool) == 1 としているため、無条件でint型に変換できる
                        return new SyntaxTree.Expression.PostfixExpression.IntegerPromotionExpression(CType.CreateSignedInt(), expr);
                    case CType.BasicType.TypeKind.SignedInt:
                        // 無条件でint型に変換できる
                        return new SyntaxTree.Expression.PostfixExpression.IntegerPromotionExpression(CType.CreateSignedInt(), expr);
                    case CType.BasicType.TypeKind.UnsignedInt:
                        // int 型で表現可能な場合，その値を int 型に変換する。そうでない場合，unsigned int 型に変換する
                        if (bitfield.Value == 4 * 8) {
                            // unsigned int でないと表現できない
                            return new SyntaxTree.Expression.PostfixExpression.IntegerPromotionExpression(CType.CreateUnsignedInt(), expr);
                        } else {
                            // signed int で表現できる
                            return new SyntaxTree.Expression.PostfixExpression.IntegerPromotionExpression(CType.CreateSignedInt(), expr);
                        }
                    default:
                        throw new CompilerException.SpecificationErrorException(Location.Empty, Location.Empty, "ビットフィールドの型は，修飾版又は非修飾版の_Bool，signed int，unsigned int 又は他の処理系定義の型でなければならない。");
                }
            }
        }

        /// <summary>
        ///  既定の実引数拡張（default argument promotion）
        /// </summary>
        /// <param name="self"></param>
        /// <remarks>
        /// 6.5.2.2 関数呼出し
        /// - 呼び出される関数を表す式が，関数原型を含まない型をもつ場合，各実引数に対して整数拡張を行い，型 float をもつ実引数は型 double に拡張する。
        ///   この操作を既定の実引数拡張（default argument promotion）と呼ぶ。
        /// - 関数原型宣言子における省略記号表記は，最後に宣言されている仮引数の直後から実引数の型変換を止める。残りの実引数に対しては，既定の実引数拡張を行う。
        /// </remarks>
        public static CType DefaultArgumentPromotion(this CType self) {
            // 整数拡張
            if (IsIntegerType(self)) {
                // 整数変換の順位が int 型及び unsigned int 型より低い整数型?
                if (IntegerConversionRank(self) < -5) {
                    // 元の型のすべての値を int 型で表現可能な場合，その値を int 型に変換する。そうでない場合，unsigned int 型に変換する
                    if (self.Unwrap().IsBasicType(CType.BasicType.TypeKind.UnsignedInt)) {
                        // unsigned int に拡張
                        return CType.CreateUnsignedInt();
                    } else {
                        // signed int に拡張
                        return CType.CreateSignedInt();
                    }
                } else {
                    // 拡張は不要
                    return self;
                }
            } else if (IsRealFloatingType(self)) {
                if (self.Unwrap().IsBasicType(CType.BasicType.TypeKind.Float)) {
                    // double に拡張
                    return CType.CreateDouble();
                } else {
                    // 拡張は不要
                    return self;
                }
            } else {
                // 拡張は不要
                return self;
            }
        }

        /// <summary>
        /// 通常の算術型変換（usual arithmetic conversion）
        /// </summary>
        /// <remarks>
        /// 6.3.1.8 通常の算術型変換
        /// 算術型のオペランドをもつ多くの演算子は，同じ方法でオペランドの型変換を行い，結果の型を決める。型変換は，オペランドと結果の共通の実数型（common real type）を決めるために行う。
        /// 与えられたオペランドに対し，それぞれのオペランドは，型領域を変えることなく，共通の実数型を対応する実数型とする型に変換する。
        /// この規格で明示的に異なる規定を行わない限り，結果の対応する実数型も，この共通の実数型とし，その型領域は，オペランドの型領域が一致していればその型領域とし，一致していなければ複素数型とする。
        /// これを通常の算術型変換（usual arithmetic conversion）と呼ぶ
        /// </remarks>
        /// <remarks>
        /// - 二項算術演算子（* / % + -）、ビット単位演算子（& ^ |）のオペランドに対して通常の算術型変換が適用される（加減演算子についてはポインタオペランドを含む場合は除く）。
        /// - 関係演算子（< > <= >=）、等価演算子（== !=）の算術型オペランドに、通常の算術型変換が適用される。
        /// - 条件演算子?:の第2・第3オペランドが算術型の場合、結果の型は、両オペランドに通常の算術型変換を適用後の型となる。
        /// </remarks>
        public static CType UsualArithmeticConversion(ref SyntaxTree.Expression lhs, ref SyntaxTree.Expression rhs) {
            var tyLhs = lhs.Type.Unwrap();
            var tyRhs = rhs.Type.Unwrap();

            var btLhs = tyLhs as CType.BasicType;
            var btRhs = tyRhs as CType.BasicType;

            if (btLhs == null || btRhs == null) {
                throw new CompilerException.InternalErrorException(Location.Empty, Location.Empty, "二つのオペランドの一方に基本型以外が与えられた。（本実装の誤りが原因だと思われます。）");
            }
            if (btLhs.Kind == btRhs.Kind) {
                return btLhs;
            }

            // まず，一方のオペランドの対応する実数型が long double ならば，他方のオペランドを，型領域を変えることなく，変換後の型に対応する実数型が long double となるように型変換する。
            // そうでない場合，一方のオペランドの対応する実数型が double ならば，他方のオペランドを，型領域を変えることなく，変換後の型に対応する実数型が double となるように型変換する。
            // そうでない場合，一方のオペランドの対応する実数型が float ならば，他方のオペランドを，型領域を変えることなく，変換後の型に対応する実数型が float となるように型変換する。
            // 例：
            //  - 一方が long double で 他方が double なら double を long double にする。
            //  - 一方が long double で 他方が float _Complex なら float _Complex を long double _Complex にする。（結果の型は long double _Complex 型になる）
            //  - 一方が long double _Complex で 他方が float なら float を long double にする。（結果の型は long double _Complex 型になる）
            var realConversionPairTable = new[] {
                Tuple.Create(CType.BasicType.TypeKind.LongDouble,CType.BasicType.TypeKind.LongDouble_Complex),
                Tuple.Create(CType.BasicType.TypeKind.Double,CType.BasicType.TypeKind.Double_Complex),
                Tuple.Create(CType.BasicType.TypeKind.Float,CType.BasicType.TypeKind.Float_Complex)
            };

            foreach (var realConversionPair in realConversionPairTable) {
                if (btLhs.IsFloatingType() && btLhs.GetCorrespondingRealType().Kind == realConversionPair.Item1) {
                    if (btRhs.IsComplexType()) {
                        var retTy = new CType.BasicType(realConversionPair.Item2);
                        rhs = new SyntaxTree.Expression.PostfixExpression.TypeConversionExpression(retTy, rhs);
                        return retTy;
                    } else {
                        rhs = new SyntaxTree.Expression.PostfixExpression.TypeConversionExpression(new CType.BasicType(realConversionPair.Item1), rhs);
                        return btLhs;
                    }
                } else if (btRhs.IsFloatingType() && btRhs.GetCorrespondingRealType().Kind == realConversionPair.Item1) {
                    if (btLhs.IsComplexType()) {
                        var retTy = new CType.BasicType(realConversionPair.Item2);
                        lhs = new SyntaxTree.Expression.PostfixExpression.TypeConversionExpression(retTy, lhs);
                        return retTy;
                    } else {
                        lhs = new SyntaxTree.Expression.PostfixExpression.TypeConversionExpression(new CType.BasicType(realConversionPair.Item1), lhs);
                        return btRhs;
                    }
                }
            }

            // そうでない場合，整数拡張を両オペランドに対して行い，拡張後のオペランドに次の規則を適用する。
            lhs = IntegerPromotion(lhs);
            rhs = IntegerPromotion(rhs);

            tyLhs = lhs.Type.Unwrap();
            tyRhs = rhs.Type.Unwrap();

            btLhs = tyLhs as CType.BasicType;
            btRhs = tyRhs as CType.BasicType;

            if (btLhs == null || btRhs == null) {
                throw new CompilerException.InternalErrorException(Location.Empty, Location.Empty, "整数拡張後のオペランドの型が基本型以外になっています。（本実装の誤りが原因だと思われます。）");
            }
            // 両方のオペランドが同じ型をもつ場合，更なる型変換は行わない。
            if (btLhs.Kind == btRhs.Kind) {
                return btLhs;
            }

            // そうでない場合，両方のオペランドが符号付き整数型をもつ，又は両方のオペランドが符号無し整数型をもつならば，
            // 整数変換順位の低い方の型を，高い方の型に変換する。
            if ((btLhs.IsSignedIntegerType() && btRhs.IsSignedIntegerType()) || (btLhs.IsUnsignedIntegerType() && btRhs.IsUnsignedIntegerType())) {
                if (btLhs.IntegerConversionRank() < btRhs.IntegerConversionRank()) {
                    lhs = new SyntaxTree.Expression.PostfixExpression.TypeConversionExpression(btRhs, lhs);
                    return btRhs;
                } else {
                    rhs = new SyntaxTree.Expression.PostfixExpression.TypeConversionExpression(btLhs, rhs);
                    return btLhs;
                }
            }

            // ここに到達した時点で、一方が符号無し、一方が符号付きであることが保障される

            // そうでない場合，符号無し整数型をもつオペランドが，他方のオペランドの整数変換順位より高い又は等しい順位をもつならば，
            // 符号付き整数型をもつオペランドを，符号無し整数型をもつオペランドの型に変換する。
            if (btLhs.IsUnsignedIntegerType() && btLhs.IntegerConversionRank() >= btRhs.IntegerConversionRank()) {
                rhs = new SyntaxTree.Expression.PostfixExpression.TypeConversionExpression(btLhs, rhs);
                return btLhs;
            } else if (btRhs.IsUnsignedIntegerType() && btRhs.IntegerConversionRank() >= btLhs.IntegerConversionRank()) {
                lhs = new SyntaxTree.Expression.PostfixExpression.TypeConversionExpression(btRhs, lhs);
                return btRhs;
            }

            // ここに到達した時点で、符号有りオペランドのほうが符号無しオペランドよりも大きい整数変換順位を持つことが保障される
            // 整数変換順位の大きさと型の表現サイズの大きさは環境によっては一致しない
            // 例：int が 2byte (signed int = signed short) 、char が 16bit以上など

            // そうでない場合，符号付き整数型をもつオペランドの型が，符号無し整数型をもつオペランドの型のすべての値を表現できるならば，
            // 符号無し整数型をもつオペランドを，符号付き整数型をもつオペランドの型に変換する。
            if (btLhs.IsSignedIntegerType() && btRhs.IsUnsignedIntegerType() && btLhs.Sizeof() > btRhs.Sizeof()) {
                rhs = new SyntaxTree.Expression.PostfixExpression.TypeConversionExpression(btLhs, rhs);
                return btLhs;
            } else if (btRhs.IsSignedIntegerType() && btLhs.IsUnsignedIntegerType() && btRhs.Sizeof() > btLhs.Sizeof()) {
                lhs = new SyntaxTree.Expression.PostfixExpression.TypeConversionExpression(btRhs, lhs);
                return btRhs;
            }

            // そうでない場合，両方のオペランドを，符号付き整数型をもつオペランドの型に対応する符号無し整数型に変換する。
            CType.BasicType.TypeKind tySignedKind = ((btLhs.IsSignedIntegerType()) ? btLhs : btRhs).Kind;
            CType.BasicType.TypeKind tyUnsignedKind;
            switch (tySignedKind) {
                case CType.BasicType.TypeKind.SignedInt:
                    tyUnsignedKind = CType.BasicType.TypeKind.UnsignedInt;
                    break;
                case CType.BasicType.TypeKind.SignedLongInt:
                    tyUnsignedKind = CType.BasicType.TypeKind.UnsignedLongInt;
                    break;
                case CType.BasicType.TypeKind.SignedLongLongInt:
                    tyUnsignedKind = CType.BasicType.TypeKind.UnsignedLongLongInt;
                    break;
                default:
                    throw new CompilerException.InternalErrorException(Location.Empty, Location.Empty, "整数拡張後のオペランドの型がsigned int/signed long int/ signed long long int 型以外になっています。（本実装の誤りが原因だと思われます。）");
            }

            var tyUnsigned = new CType.BasicType(tyUnsignedKind);
            lhs = new SyntaxTree.Expression.PostfixExpression.TypeConversionExpression(tyUnsigned, lhs);
            rhs = new SyntaxTree.Expression.PostfixExpression.TypeConversionExpression(tyUnsigned, rhs);
            return tyUnsigned;

        }

        /// <summary>
        /// 6.3 型変換
        /// </summary>
        /// <param name="expr"></param>
        /// <returns></returns>
        /// <remarks>
        /// 幾つかの演算子は，オペランドの値をある型から他の型へ自動的に型変換する。
        /// 6.3 は，この暗黙の型変換（implicit conversion）の結果及びキャスト演算［明示的な型変換（explicit conversion）］の結果に対する要求を規定する。
        /// 通常の演算子によって行われるほとんどの型変換は，6.3.1.8 にまとめる。
        /// 各演算子における型変換については，必要に応じて 6.5 に補足する。
        /// 適合する型へのオペランドの値の型変換は，値又は表現の変更を引き起こさない
        /// </remarks>
        public static SyntaxTree.Expression TypeConvert(CType targetType, SyntaxTree.Expression expr) {

            // 6.3.1 算術オペランド

            // 6.3.1.1 論理型，文字型及び整数型
            // 6.3.1.3 符号付き整数型及び符号無し整数型 
            // 6.3.1.4 実浮動小数点型及び整数型 
            if (targetType != null) {
                if (targetType.IsIntegerType() && !targetType.IsBoolType()) {
                    if (targetType.Unwrap().IsBasicType(CType.BasicType.TypeKind.SignedInt | CType.BasicType.TypeKind.UnsignedInt)) {
                        // 6.3.1.1 論理型，文字型及び整数型
                        // int型又は unsigned int 型を使用してよい式の中ではどこでも，次に示すものを使用することができる。
                        if (expr.Type.IntegerConversionRank() < -5) {
                            // - 整数変換の順位が int 型及び unsigned int 型より低い整数型をもつオブジェクト又は式
                        } else if (expr.Type.IsBoolType() || expr.Type.Unwrap().IsBasicType(CType.BasicType.TypeKind.SignedInt | CType.BasicType.TypeKind.UnsignedInt) /*ToDo: bitfield*/) {
                            // - _Bool 型，int 型，signed int 型，又は unsigned int 型のビットフィールド
                        } else {
                            throw new CompilerException.SpecificationErrorException(Location.Empty, Location.Empty, "int型又は unsigned int 型を使用してよい式の中で使えないものが指定された。");
                        }
                        // これらのものの元の型のすべての値を int 型で表現可能な場合，その値を int 型に変換する。
                        // そうでない場合，unsigned int 型に変換する。
                        // これらの処理を，整数拡張（integer promotion）と呼ぶ
                        // 整数拡張は，符号を含めてその値を変えない。“単なる”char 型を符号付きとして扱うか否かは，処理系定義とする（6.2.5 参照）
                        return IntegerPromotion(expr);

                    } else if (expr.Type.IsRealFloatingType()) {
                        // 6.3.1.4 実浮動小数点型及び整数型 
                        // 実浮動小数点型の有限の値を_Bool 型以外の整数型に型変換する場合，小数部を捨てる（すなわち，値を 0 方向に切り捨てる。）。
                        // 整数部の値が整数型で表現できない場合， その動作は未定義とする
                        return new SyntaxTree.Expression.PostfixExpression.TypeConversionExpression(targetType, expr);
                    } else if (expr.Type.IsArithmeticType()) {
                        // 6.3.1.1 論理型，文字型及び整数型
                        // これら以外の型が整数拡張によって変わることはない。

                        // 6.3.1.3 符号付き整数型及び符号無し整数型 
                        // 整数型の値を_Bool 型以外の他の整数型に変換する場合，その値が新しい型で表現可能なとき，値は変化しない。
                        return expr;
                    }
                }
            } else {
                if (expr.Type.IsRealFloatingType()) {
                    return (expr);
                } else if (expr.Type.IsIntegerType()) {
                    return IntegerPromotion(expr);
                }
            }

            // 6.3.1.2 論理型 
            if (targetType != null) {
                if (targetType.IsBoolType()) {
                    // 任意のスカラ値を_Bool 型に変換する場合，その値が 0 に等しい場合は結果は 0 とし，それ以外の場合は 1 とする。
                    if (expr.Type.IsScalarType()) {
                        return new SyntaxTree.Expression.PostfixExpression.TypeConversionExpression(targetType, expr);
                    } else if (expr.Type.IsBoolType()) {
                        return expr;
                    } else {
                        throw new CompilerException.SpecificationErrorException(Location.Empty, Location.Empty, "スカラ値以外は_Bool 型に変換できません。");
                    }
                }
            }

            // 6.3.1.4 実浮動小数点型及び整数型
            if (targetType != null) {
                if (targetType.IsRealFloatingType() && expr.Type.IsIntegerType()) {
                    // 整数型の値を実浮動小数点型に型変換する場合，変換する値が新しい型で正確に表現できるとき，その値は変わらない。
                    // 変換する値が表現しうる値の範囲内にあるが正確に表現できないならば，その値より大きく最も近い表現可能な値，又はその値より小さく最も近い表現可能な値のいずれかを処理系定義の方法で選ぶ。
                    // 変換する値が表現しうる値の範囲外にある場合，その動作は未定義とする。
                    return new SyntaxTree.Expression.PostfixExpression.TypeConversionExpression(targetType, expr);
                }
            }

            // 6.3.1.5 実浮動小数点型
            if (targetType != null) {
                if (targetType.IsRealFloatingType() && expr.Type.IsRealFloatingType()) {
                    // float を double 若しくは long double に拡張する場合，又は double を long double に拡張する場合，その値は変化しない。
                    // double を float に変換する場合，long double を double 若しくは float に変換する場合，又は，意味上の型（6.3.1.8 参照）が要求するより高い精度及び広い範囲で表現された値をその意味上の型に明示的に変換する場合，
                    // 変換する値がその新しい型で正確に表現できるならば，その値は変わらない。
                    // 変換する値が，表現しうる値の範囲内にあるが正確に表現できない場合，その結果は，その値より大きく最も近い表現可能な値，又はその値より小さく最も近い表現可能な値のいずれかを処理系定義の方法で選ぶ。
                    // 変換する値が表現しうる値の範囲外にある場合，その動作は未定義とする。
                    return new SyntaxTree.Expression.PostfixExpression.TypeConversionExpression(targetType, expr);
                }
            }

            // 6.3.1.6 複素数型 
            if (targetType != null) {
                if (targetType.IsComplexType() && expr.Type.IsComplexType()) {
                    // 複素数型の値を他の複素数型に変換する場合，実部と虚部の両方に，対応する実数型の変換規則を適用する。
                    if ((targetType.Unwrap() as CType.BasicType).Kind == (expr.Type.Unwrap() as CType.BasicType).Kind) {
                        return expr;
                    } else {
                        return new SyntaxTree.Expression.PostfixExpression.TypeConversionExpression(targetType, expr);
                    }
                }
            }

            // 6.3.1.7 実数型及び複素数型 
            if (targetType != null) {
                if (targetType.IsComplexType() && expr.Type.IsRealType()) {
                    // 実数型の値を複素数型に変換する場合，複素数型の結果の実部は対応する実数型への変換規則により決定し，複素数型の結果の虚部は正の 0 又は符号無しの 0 とする。
                    return new SyntaxTree.Expression.PostfixExpression.TypeConversionExpression(targetType, expr);
                } else if (expr.Type.IsComplexType() && targetType.IsRealType()) {
                    // 複素数型の値を実数型に変換する場合，複素数型の値の虚部を捨て，実部の値を，対応する実数型の変換規則に基づいて変換する
                    return new SyntaxTree.Expression.PostfixExpression.TypeConversionExpression(targetType, expr);
                }
            }

            // 6.3.2 他のオペランド

            // 6.3.2.1 左辺値，配列及び関数指示子
            if (targetType != null) {
                if (targetType.IsPointerType()) {
                    CType elementType;
                    if (expr.Type.IsFunctionType()) {
                        // 関数指示子（function designator）は，関数型をもつ式とする。
                        // 関数指示子が sizeof 演算子又は単項&演算子のオペランドである場合を除いて，型“∼型を返す関数”をもつ関数指示子は，
                        // 型“∼型を返す関数へのポインタ”をもつ式に変換する。
                        return TypeConvert(targetType, new SyntaxTree.Expression.PostfixExpression.UnaryAddressExpression(expr));
                    } else if (expr.Type.IsArrayType(out elementType)) {
                        // 左辺値が sizeof 演算子のオペランド，単項&演算子のオペランド，又は文字配列を初期化するのに使われる文字列リテラルである場合を除いて，
                        // 型“∼型の配列”をもつ式は，型“∼型へのポインタ”の式に型変換する。
                        // それは配列オブジェクトの先頭の要素を指し，左辺値ではない。
                        // 配列オブジェクトがレジスタ記憶域クラスをもつ場合，その動作は未定義とする。
                        return TypeConvert(targetType, new SyntaxTree.Expression.PostfixExpression.TypeConversionExpression(CType.CreatePointer(elementType), expr));
                    }
                }
            } else {
                CType elementType;
                if (expr.Type.IsFunctionType()) {
                    // 関数指示子（function designator）は，関数型をもつ式とする。
                    // 関数指示子が sizeof 演算子又は単項&演算子のオペランドである場合を除いて，型“∼型を返す関数”をもつ関数指示子は，
                    // 型“∼型を返す関数へのポインタ”をもつ式に変換する。
                    return new SyntaxTree.Expression.PostfixExpression.UnaryAddressExpression(expr);
                } else if (expr.Type.IsArrayType(out elementType)) {
                    // 左辺値が sizeof 演算子のオペランド，単項&演算子のオペランド，又は文字配列を初期化するのに使われる文字列リテラルである場合を除いて，
                    // 型“∼型の配列”をもつ式は，型“∼型へのポインタ”の式に型変換する。
                    // それは配列オブジェクトの先頭の要素を指し，左辺値ではない。
                    // 配列オブジェクトがレジスタ記憶域クラスをもつ場合，その動作は未定義とする。
                    return new SyntaxTree.Expression.PostfixExpression.TypeConversionExpression(CType.CreatePointer(elementType), expr);
                }
            }

            // 6.3.2.2 void ボイド式（void expression）
            if (targetType != null) {
                if (expr.Type.IsVoidType()) {
                    // （型 void をもつ式）の（存在しない）値は，いかなる方法で も使ってはならない。
                    // ボイド式には，暗黙の型変換も明示的な型変換（void への型変換を除く。 ）も適用してはならない。
                    // 他の型の式をボイド式として評価する場合，その値又は指示子は捨てる。
                    // （ボイド式は， 副作用のために評価する。 ）
                    if (targetType.IsVoidType()) {
                        return expr;
                    } else {
                        throw new CompilerException.SpecificationErrorException(Location.Empty, Location.Empty, "void型の式の値をvoid型以外へ型変換しようとしました。");
                    }
                } else if (targetType.IsVoidType()) {
                    // 他の型の式をボイド式として評価する場合，その値又は指示子は捨てる。
                    return new SyntaxTree.Expression.PostfixExpression.TypeConversionExpression(targetType, expr);
                }
            }

            // 6.3.2.3 ポインタ
            if (targetType != null) {
                CType exprPointedType;
                CType targetPointedType;
                if (targetType.IsPointerType() && ((expr.Type.IsPointerType(out exprPointedType) && exprPointedType.IsVoidType()) || (expr.IsNullPointerConstant()))) {
                    // void へのポインタは，任意の不完全型若しくはオブジェクト型へのポインタに，又はポインタから，型変換してもよい。
                    // 任意の不完全型又はオブジェクト型へのポインタを，void へのポインタに型変換して再び戻した場合，結果は元のポインタと比較して等しくなければならない。

                    // 値0をもつ整数定数式又はその定数式を型void* にキャストした式を，空ポインタ定数（null pointer constant） と呼ぶ。
                    // 空ポインタ定数をポインタ型に型変換した場合，その結果のポインタを空ポインタ（null pointer）と呼び，いかなるオブジェクト又は関数へのポインタと比較しても等しくないことを保証する。
                    // 空ポインタを他のポインタ型に型変換すると，その型の空ポインタを生成する。

                    return new SyntaxTree.Expression.PostfixExpression.TypeConversionExpression(targetType, expr);
                }
                if (targetType.IsPointerType(out targetPointedType) && targetPointedType.IsQualifiedType()
                    && expr.Type.IsPointerType(out exprPointedType) && !exprPointedType.IsQualifiedType()
                    && CType.IsEqual(targetPointedType.Unwrap(), exprPointedType.Unwrap())) {
                    // 任意の型修飾子 q に対して非 q 修飾型へのポインタは，その型の q 修飾版へのポインタに型変換してもよい。
                    return new SyntaxTree.Expression.PostfixExpression.TypeConversionExpression(targetType, expr);
                }
                if (targetType.IsPointerType() && expr.IsNullPointerConstant()) {
                    return new SyntaxTree.Expression.PostfixExpression.TypeConversionExpression(targetType, expr);
                }
                if (targetType.IsPointerType() && expr.Type.IsIntegerType()) {
                    // 整数は任意のポインタ型に型変換できる。
                    // これまでに規定されている場合を除き，結果は処理系定義とし，正しく境界調整されていないかもしれず，被参照型の実体を指していないかもしれず，トラップ表現であるかもしれない。
                    // 結果が整数型で表現できなければ，その動作は未定義とする。
                    // 結果は何らかの整数型の値の範囲に含まれているとは限らない。
                    return new SyntaxTree.Expression.PostfixExpression.TypeConversionExpression(targetType, expr);
                }

                if (targetType.IsIntegerType() && expr.Type.IsPointerType()) {
                    // 任意のポインタ型は整数型に型変換できる。
                    // これまでに規定されている場合を除き，結果は処理系定義とする。結果が整数型で表現できなければ，その動作は未定義とする。
                    // 結果は何らかの整数型の値の範囲に含まれているとは限らない。                    
                    return new SyntaxTree.Expression.PostfixExpression.TypeConversionExpression(targetType, expr);
                }

                if (targetType.IsPointerType(out targetPointedType) && (targetPointedType.IsObjectType() || targetPointedType.IsIncompleteType())
                    && expr.Type.IsPointerType(out exprPointedType) && (exprPointedType.IsObjectType() || exprPointedType.IsIncompleteType())) {
                    // オブジェクト型又は不完全型へのポインタは，他のオブジェクト型又は不完全型へのポインタに型変換できる。
                    // その結果のポインタが，被参照型に関して正しく境界調整されていなければ，その動作は未定義とする。
                    // そうでない場合，再び型変換で元の型に戻すならば，その結果は元のポインタと比較して等しくなければならない。
                    return new SyntaxTree.Expression.PostfixExpression.TypeConversionExpression(targetType, expr);
                }

                if (targetType.IsPointerType(out targetPointedType) && targetPointedType.IsCharacterType()
                    && expr.Type.IsPointerType(out exprPointedType) && exprPointedType.IsObjectType()) {
                    // オブジェクトへのポインタを文字型へのポインタに型変換する場合，その結果はオブジェクトの最も低位のアドレスを指す。
                    // その結果をオブジェクトの大きさまで連続して増分すると，そのオブジェクトの残りのバイトへのポインタを順次生成できる。
                    return new SyntaxTree.Expression.PostfixExpression.TypeConversionExpression(targetType, expr);
                }

                if (targetType.IsPointerType(out targetPointedType) && targetPointedType.IsFunctionType()
                    && expr.Type.IsPointerType(out exprPointedType) && exprPointedType.IsFunctionType()) {
                    // ある型の関数へのポインタを，別の型の関数へのポインタに型変換することができる。
                    // さらに再び型変換で元の型に戻すことができるが，その結果は元のポインタと比較して等しくなければならない。
                    // 型変換されたポインタを関数呼出しに用い，関数の型がポインタが指すものの型と適合しない場合，その動作は未定義とする。
                    return new SyntaxTree.Expression.PostfixExpression.TypeConversionExpression(targetType, expr);
                }

            } else {
                if (expr.Type.IsPointerType()) {
                    return expr;
                }
            }

            throw new CompilerException.SpecificationErrorException(Location.Empty, Location.Empty, "型変換できない組み合わせを型変換しようとした");

        }

        /// <summary>
        /// 6.3 型変換(暗黙の型変換(implicit conversion))
        /// </summary>
        public static SyntaxTree.Expression ImplicitConversion(CType targetType, SyntaxTree.Expression expr) {
            return TypeConvert(targetType, expr);
        }

        /// <summary>
        /// 6.3 型変換(明示的な型変換(explicit conversion))
        /// </summary>
        /// <returns></returns>
        public static SyntaxTree.Expression ExplicitConversion(CType targetType, SyntaxTree.Expression expr) {
            return TypeConvert(targetType, expr);
        }

        /// <summary>
        /// ポインタ型に対する派生元の型を取得
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        public static CType GetBasePointerType(this CType self) {
            var unwraped = self.Unwrap();
            if (!unwraped.IsPointerType()) {
                throw new CompilerException.InternalErrorException(Location.Empty, Location.Empty, "ポインタ型以外から派生元型を得ようとしました。（本実装の誤りが原因だと思われます。）");
            } else {
                return (unwraped as CType.PointerType).BaseType;
            }
        }

        /// <summary>
        /// 6.3.2.3 ポインタ(空ポインタ定数)
        /// </summary>
        /// <param name="expr"></param>
        /// <returns></returns>
        /// <remarks>
        /// 値0をもつ整数定数式又はその定数式を型 void* にキャストした式を，空ポインタ定数（null pointer constant）と呼ぶ。
        /// </remarks>
        public static bool IsNullPointerConstant(this SyntaxTree.Expression expr) {

            if (expr.Type.IsPointerType() && expr.Type.GetBasePointerType().IsVoidType()) {
                for (;;) {
                    if (expr is SyntaxTree.Expression.UnaryPrefixExpression.TypeConversionExpression) {
                        expr = (expr as SyntaxTree.Expression.UnaryPrefixExpression.TypeConversionExpression).Expr;
                        continue;
                    }
                    if (expr is SyntaxTree.Expression.PrimaryExpression.EnclosedInParenthesesExpression) {
                        expr = (expr as SyntaxTree.Expression.PrimaryExpression.EnclosedInParenthesesExpression).ParenthesesExpression;
                        continue;
                    }
                    break;
                }
            }

            // 整数定数式又はその定数式とあるので定数演算を試みる
            try {
                return Evaluator.ConstantEval(expr) == 0;
            } catch {
                return false;
            }

        }

        /// <summary>
        /// 式がポインタ型に変換できるなら変換する。
        /// </summary>
        /// <param name="lhs"></param>
        /// <returns></returns>
        public static SyntaxTree.Expression ToPointerTypeExpr(SyntaxTree.Expression expr) {

            // 6.3.2 他のオペランド

            // 6.3.2.1 左辺値，配列及び関数指示子
            {
                CType elementType;
                if (expr.Type.IsFunctionType()) {
                    // 関数指示子（function designator）は，関数型をもつ式とする。
                    // 関数指示子が sizeof 演算子又は単項&演算子のオペランドである場合を除いて，型“∼型を返す関数”をもつ関数指示子は，
                    // 型“∼型を返す関数へのポインタ”をもつ式に変換する。
                    return new SyntaxTree.Expression.PostfixExpression.UnaryAddressExpression(expr);
                } else if (expr.Type.IsArrayType(out elementType)) {
                    // 左辺値が sizeof 演算子のオペランド，単項&演算子のオペランド，又は文字配列を初期化するのに使われる文字列リテラルである場合を除いて，
                    // 型“∼型の配列”をもつ式は，型“∼型へのポインタ”の式に型変換する。
                    // それは配列オブジェクトの先頭の要素を指し，左辺値ではない。
                    // 配列オブジェクトがレジスタ記憶域クラスをもつ場合，その動作は未定義とする。
                    return new SyntaxTree.Expression.PostfixExpression.TypeConversionExpression(CType.CreatePointer(elementType), expr);
                }
            }


            // 元々ポインタ型の式はそのまま
            if (expr.Type.IsPointerType()) {
                return expr;
            }

            return null;
        }



        // 6.2.7 適合型及び合成型
        // 二つの型が同じ場合，二つの型は適合する（compatible）とする。
        // これ以外に二つの型が適合する場合を定める規則は，型指定子については 6.7.2 で，型修飾子については 6.7.3 で，宣言子については 6.7.5 で規定する
        // さらに，別々の翻訳単位で宣言された二つの構造体型，共用体型又，それらのタグ及びメンバが次に示す要求を満たす場合に，適合する
        // - 一方がタグ付きで宣言されている場合，もう一方も同じタグ付きで宣言されていなければならない。
        // - 両方が完全型であれば，次に示す要求が新たに満たされなければならない。すなわち，両方のメンバの間に 1 対 1 の対応がつき，対応するメンバ同士が適合する型をもち，更に対応するメンバの一方が名前付きで宣言されているならば，もう一方も同じ名前付きで宣言されていなければならない。
        // - 二つの構造体については，対応するメンバは同じ順序で宣言されていなければならない。
        // - 二つの構造体又は共用体については，対応するビットフィールドは同じ幅をもたなければならない。
        // - 二つの列挙体については，対応するメンバは同じ値をもたなければならない。
        //
        // 6.7.2 型指定子
        // 型指定子の並びは，次に示すもののいずれか一つでなければならない。
        // 型指定子は，いかなる順序で現れてもよく，更に，他の宣言指定子と混合してもよい。
        // - void
        // - char
        // - signed char
        // - unsigned char
        // - short，signed short，short int，signed short int
        // - unsigned short，unsigned short int
        // - int，signed，signed int
        // - unsigned，unsigned int
        // - long，signed long，long int，signed long int
        // - unsigned long，unsigned long int
        // - long long，signed long long，long long int，signed long long int
        // - unsigned long long，unsigned long long int
        // - float
        // - double
        // - long double
        // - _Bool
        // - float _Complex
        // - double _Complex
        // - long double _Complex
        // - float _Imaginary
        // - double _Imaginary
        // - long double _Imaginary
        // - 構造体共用体指定子
        // - 列挙型指定子
        // - 型定義名
        // 構造体，共用体及び列挙型の指定子は，6.7.2.1～6.7.2.3 で規定する。型定義名の宣言は，6.7.7で規定する。他の型の性質は，6.2.5 で規定する。
        // コンマで区切られているそれぞれの組は，同じ型を表す。
        // ただし，ビットフィールドの場合，型指定子 int が signed int と同じ型を表すか，unsigned int と同じ型を表すかは処理系定義とする。
        //
        // 6.7.2.1 構造体指定子及び共用体指定子
        // （適合・合成型に触れる記述なし）
        //
        // 6.7.2.2 列挙型指定子
        // それぞれの列挙型は，char，符号付き整数型又は符号無し整数型と適合する型とする。型の選択は，処理系定義とする(処理系はすべての列挙定数が指定された後で整数型の選択を行うことができる)。
        // しかし，その型は列挙型のすべてのメンバの値を表現できなければならない。
        //
        // 6.7.3 型修飾子
        // 二つの修飾型が適合するためには，双方が適合する型に同じ修飾を行ったものでなければならない。
        // 型指定子又は型修飾子の並びにおける型修飾子の順序は，指定された型に影響を与えない。
        //
        // 6.7.5 宣言子
        //
        // 6.7.5.1 ポインタ宣言子
        // 二つのポインタ型が適合するためには，いずれも同一の修飾がなされていなければならず，かつ両者が適合する型へのポインタでなければならない。
        //
        // 6.7.5.2 配列宣言子
        // 二つの配列型が適合するためには，まず，両者が適合する要素型をもたなければならない。
        // さらに，両者が配列の大きさを指定する整数定数式をもつ場合，それらの値は同じ定数値でなければならない。
        // 二つの配列型が適合することを必要とする文脈で使われ，両者の大きさ指定子を評価した値が異なる場合，その動作は未定義とする。
        //
        // 6.7.5.3 関数宣言子（関数原型を含む）
        // 二つの関数型が適合するためには，次の条件をすべて満たさなければならない。
        // - 両方が適合する返却値の型をもつ
        // - 両方が仮引数型並びをもつ場合，仮引数の個数及び省略記号の有無に関して一致し，対応する仮引数の型が適合する。
        // - 一方の型が仮引数型並びをもち，他方の型が関数定義の一部でない関数宣言子によって指定され，識別子並びが空の場合，仮引数型並びは省略記号を含まない。
        //   各仮引数の型は，既定の実引数拡張を適用した結果の型と適合する。
        // - 一方の型が仮引数型並びをもち，他方の型が関数定義によって指定され，識別子並び（空でもよい）をもつ場合，両方の仮引数の個数は一致する。
        //   さらに関数原型のほうの各仮引数の型は，対応する識別子の型に既定の実引数拡張を適用した結果の型と適合する。
        //  （型の適合及び型の合成を判断するとき，関数型又は配列型で宣言される各仮引数は型調整後の型をもつものとして扱い，修飾型で宣言される各仮引数は宣言された型の非修飾版をもつものとして扱う。）
        //
        public static bool IsCompatible(CType t1, CType t2) {
            for (;;) {
                if (ReferenceEquals(t1, t2)) {
                    return true;
                }
                if (t1 is CType.TypedefedType || t2 is CType.TypedefedType) {
                    if (t1 is CType.TypedefedType) {
                        t1 = (t1 as CType.TypedefedType).Type;
                    }
                    if (t2 is CType.TypedefedType) {
                        t2 = (t2 as CType.TypedefedType).Type;
                    }
                    continue;
                }
                if ((t1 as CType.TypeQualifierType)?.Qualifier == TypeQualifier.None) {
                    t1 = (t1 as CType.TypeQualifierType).Type;
                    continue;
                }
                if ((t2 as CType.TypeQualifierType)?.Qualifier == TypeQualifier.None) {
                    t2 = (t2 as CType.TypeQualifierType).Type;
                    continue;
                }
                if (t1.GetType() != t2.GetType()) {
                    return false;
                }

                if (t1 is CType.BasicType && t2 is CType.BasicType) {
                    // 6.7.2 型指定子
                    // 型指定子の並びは，次に示すもののいずれか一つでなければならない。
                    if ((t1 as CType.BasicType).Kind != (t2 as CType.BasicType).Kind) {
                        return false;
                    }
                    return true;
                }

                if (t1 is CType.TaggedType.StructUnionType && t2 is CType.TaggedType.StructUnionType) {
                    // 6.7.2.1 構造体指定子及び共用体指定子
                    // 構造体型、及び共用体型は無結合であるため、構文上の誤りが無ければ単一翻訳単位内では同名＝同一型である。
                    // しかし、異なる翻訳単位間での規則も示されているためそちらで検証する
                    // - 一方がタグ付きで宣言されている場合，もう一方も同じタグ付きで宣言されていなければならない。
                    // - 両方が完全型であれば，次に示す要求が新たに満たされなければならない。すなわち，両方のメンバの間に 1 対 1 の対応がつき，対応するメンバ同士が適合する型をもち，
                    //   更に対応するメンバの一方が名前付きで宣言されているならば，もう一方も同じ名前付きで宣言されていなければならない。
                    //   - 二つの構造体については，対応するメンバは同じ順序で宣言されていなければならない。
                    //   - 二つの構造体又は共用体については，対応するビットフィールドは同じ幅をもたなければならない。
                    //   - 二つの列挙体については，対応するメンバは同じ値をもたなければならない。

                    // - 一方がタグ付きで宣言されている場合，もう一方も同じタグ付きで宣言されていなければならない。
                    if ((t1 as CType.TaggedType.StructUnionType).Kind != (t2 as CType.TaggedType.StructUnionType).Kind) {
                        return false;
                    } else if ((t1 as CType.TaggedType.StructUnionType).IsAnonymous != (t2 as CType.TaggedType.StructUnionType).IsAnonymous) {
                        return false;
                    } else if ((t1 as CType.TaggedType.StructUnionType).TagName != (t2 as CType.TaggedType.StructUnionType).TagName) {
                        return false;
                    }

                    if ((t1 as CType.TaggedType.StructUnionType).Members != null && (t2 as CType.TaggedType.StructUnionType).Members != null) {
                        // - 両方が完全型であれば，次に示す要求が新たに満たされなければならない。すなわち，両方のメンバの間に 1 対 1 の対応がつき，対応するメンバ同士が適合する型をもち，
                        //   更に対応するメンバの一方が名前付きで宣言されているならば，もう一方も同じ名前付きで宣言されていなければならない。
                        //   - 二つの構造体については，対応するメンバは同じ順序で宣言されていなければならない。
                        //   - 二つの構造体又は共用体については，対応するビットフィールドは同じ幅をもたなければならない。

                        // 両方のメンバの間に 1 対 1 の対応がつくか？
                        if ((t1 as CType.TaggedType.StructUnionType).Members.Count != (t2 as CType.TaggedType.StructUnionType).Members.Count) {
                            return false;
                        }

                        int len = (t1 as CType.TaggedType.StructUnionType).Members.Count;
                        for (var i = 0; i < len; i++) {
                            var m1 = (t1 as CType.TaggedType.StructUnionType).Members[i];
                            var m2 = (t2 as CType.TaggedType.StructUnionType).Members[i];

                            // 対応するメンバ同士が適合する型を持つか？
                            if (IsCompatible(m1.Type, m2.Type) == false) {
                                return false;
                            }
                            // 更に対応するメンバの一方が名前付きで宣言されているならば，もう一方も同じ名前付きで宣言されているか？
                            if ((m1.Ident != null || m2.Ident != null) && (m1.Ident != m2.Ident)) {
                                return false;
                            }
                        }

                        return true;
                    } else {

                        // 一方、もしくは、両方が不完全型
                        return true;
                    }
                }

                // 6.7.2.2 列挙型指定子
                // それぞれの列挙型は，char，符号付き整数型又は符号無し整数型と適合する型とする。型の選択は，処理系定義とする(処理系はすべての列挙定数が指定された後で整数型の選択を行うことができる)。
                // しかし，その型は列挙型のすべてのメンバの値を表現できなければならない。
                if (t1 is CType.TaggedType.EnumType && t2 is CType.TaggedType.EnumType) {
                    return true;
                } else if (t1 is CType.TaggedType.EnumType && t2 is CType.BasicType) {
                    // ToDo: 暫定的
                    return (t2 as CType.BasicType).Kind == CType.BasicType.TypeKind.SignedInt;
                } else if (t1 is CType.BasicType && t2 is CType.TaggedType.EnumType) {
                    // ToDo: 暫定的
                    return (t1 as CType.BasicType).Kind == CType.BasicType.TypeKind.SignedInt;
                }

                // 6.7.3 型修飾子
                // 二つの修飾型が適合するためには，双方が適合する型に同じ修飾を行ったものでなければならない。
                // 型指定子又は型修飾子の並びにおける型修飾子の順序は，指定された型に影響を与えない。
                if (t1 is CType.TypeQualifierType && t2 is CType.TypeQualifierType) {
                    if ((t1 as CType.TypeQualifierType).Qualifier != (t2 as CType.TypeQualifierType).Qualifier) {
                        return false;
                    }
                    t1 = (t1 as CType.TypeQualifierType).Type;
                    t2 = (t2 as CType.TypeQualifierType).Type;
                    continue;
                }

                // 6.7.5.1 ポインタ宣言子
                // 二つのポインタ型が適合するためには，いずれも同一の修飾がなされていなければならず，かつ両者が適合する型へのポインタでなければならない。
                if (t1 is CType.PointerType && t2 is CType.PointerType) {
                    t1 = (t1 as CType.PointerType).BaseType;
                    t2 = (t2 as CType.PointerType).BaseType;
                    continue;
                }

                // 6.7.5.2 配列宣言子
                // 二つの配列型が適合するためには，まず，両者が適合する要素型をもたなければならない。
                // さらに，両者が配列の大きさを指定する整数定数式をもつ場合，それらの値は同じ定数値でなければならない。
                // 二つの配列型が適合することを必要とする文脈で使われ，両者の大きさ指定子を評価した値が異なる場合，その動作は未定義とする。
                if (t1 is CType.ArrayType && t2 is CType.ArrayType) {
                    if (((t1 as CType.ArrayType).Length != -1 && (t2 as CType.ArrayType).Length != -1) && ((t1 as CType.ArrayType).Length != (t2 as CType.ArrayType).Length)) {
                        return false;
                    }
                    t1 = (t1 as CType.ArrayType).BaseType;
                    t2 = (t2 as CType.ArrayType).BaseType;
                    continue;
                }

                // 6.7.5.3 関数宣言子（関数原型を含む）
                // 二つの関数型が適合するためには，次の条件をすべて満たさなければならない。
                // - 両方が適合する返却値の型をもつ
                // - 両方が仮引数型並びをもつ場合，仮引数の個数及び省略記号の有無に関して一致し，対応する仮引数の型が適合する。
                // - 一方の型が仮引数型並びをもち，他方の型が関数定義の一部でない関数宣言子によって指定され，識別子並びが空の場合，仮引数型並びは省略記号を含まない。
                //   各仮引数の型は，既定の実引数拡張を適用した結果の型と適合する。
                // - 一方の型が仮引数型並びをもち，他方の型が関数定義によって指定され，識別子並び（空でもよい）をもつ場合，両方の仮引数の個数は一致する。
                //   さらに関数原型のほうの各仮引数の型は，対応する識別子の型に既定の実引数拡張を適用した結果の型と適合する。
                //  （型の適合及び型の合成を判断するとき，関数型又は配列型で宣言される各仮引数は型調整後の型をもつものとして扱い，修飾型で宣言される各仮引数は宣言された型の非修飾版をもつものとして扱う。）
                //
                if (t1 is CType.FunctionType && t2 is CType.FunctionType) {
                    if ((t1 as CType.FunctionType).Arguments != null && (t2 as CType.FunctionType).Arguments != null) {
                        if ((t1 as CType.FunctionType).HasVariadic != (t2 as CType.FunctionType).HasVariadic) {
                            return false;
                        }
                        if ((t1 as CType.FunctionType).Arguments.Length != (t2 as CType.FunctionType).Arguments.Length) {
                            return false;
                        }

                        int len = (t1 as CType.FunctionType).Arguments.Length;
                        for (var i = 0; i < len; i++) {
                            var m1 = (t1 as CType.FunctionType).Arguments[i];
                            var m2 = (t2 as CType.FunctionType).Arguments[i];

                            // 対応するメンバ同士が適合する型を持つか？
                            if (IsCompatible(m1.Type, m2.Type) == false) {
                                return false;
                            }
                        }
                        t1 = (t1 as CType.FunctionType).ResultType;
                        t2 = (t2 as CType.FunctionType).ResultType;
                        continue;
                    } else if ((t1 as CType.FunctionType).Arguments != null && (t2 as CType.FunctionType).Arguments == null) {
                        // 新しい形式の関数宣言の後に古い形式の宣言が来た

                        //// 各仮引数の型は，既定の実引数拡張を適用した結果の型と見なす
                        //if ((t1 as CType.FunctionType).Arguments.Any(x => !IsCompatible(Specification.DefaultArgumentPromotion(x.Type), x.Type))) {
                        //    return false;
                        //}
                        // t1側は関数は引数部に省略記号を含まないとみなす
                        if ((t1 as CType.FunctionType).HasVariadic == true) {
                            return false;
                        }
                        t1 = (t1 as CType.FunctionType).ResultType;
                        t2 = (t2 as CType.FunctionType).ResultType;
                        continue;
                    } else if ((t1 as CType.FunctionType).Arguments == null && (t2 as CType.FunctionType).Arguments != null) {
                        // 古い形式の関数宣言の後に新しい形式の宣言が来た
                        //// 各仮引数の型は，既定の実引数拡張を適用した結果の型と見なす
                        //if ((t2 as CType.FunctionType).Arguments.Any(x => !IsCompatible(Specification.DefaultArgumentPromotion(x.Type), x.Type))) {
                        //    return false;
                        //}
                        // t2側は関数は引数部に省略記号を含まないとみなす
                        if ((t2 as CType.FunctionType).HasVariadic == true) {
                            return false;
                        }
                        t1 = (t1 as CType.FunctionType).ResultType;
                        t2 = (t2 as CType.FunctionType).ResultType;
                        continue;
                    } else {
                        return false;
                    }
                }

                if (t1 is CType.StubType && t2 is CType.StubType) {
                    throw new CompilerException.InternalErrorException(Location.Empty, Location.Empty, "スタブ型同士は適合できません。（本処理系の実装の誤りが原因です。）");
                }

                throw new CompilerException.InternalErrorException(Location.Empty, Location.Empty, "型の適合検証方法が定義されていません。（本処理系の実装の誤りが原因です。）");
            }

        }
    }
}