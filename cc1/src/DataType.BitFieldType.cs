using System;

namespace AnsiCParser {
    namespace DataType {
        /// <summary>
        /// ビットフィールド型
        /// </summary>
        public class BitFieldType : CType {
            /// <summary>
            /// ビットフィールドグループ内でのビット単位でのオフセット位置
            /// </summary>
            public int BitOffset {
                get;
            }

            /// <summary>
            /// ビットフィールドのビット単位でのサイズ
            /// </summary>
            public int BitWidth {
                get;
            }

            public BitFieldType(Token ident, CType type, int bitOffset, int bitWidth) {
                if (bitWidth >= 0) {
                    // 制約
                    // - ビットフィールドの幅を指定する式は，整数定数式でなければならない。
                    //   - その値は，0 以上でなければならず，コロン及び式が省略された場合，指定された型のオブジェクトがもつビット数を超えてはならない。
                    //   - 値が 0 の場合，その宣言に宣言子があってはならない。
                    // - ビットフィールドの型は，修飾版又は非修飾版の_Bool，signed int，unsigned int 又は他の処理系定義の型でなければならない。

                    if (type.Unwrap().IsBasicType(BasicType.TypeKind._Bool, BasicType.TypeKind.SignedInt, BasicType.TypeKind.UnsignedInt)) {
                        // 修飾版又は非修飾版の_Bool，signed int，unsigned int は明示的に定義されているため受理
                    } else if (type.Unwrap().IsBasicType(BasicType.TypeKind.SignedChar, BasicType.TypeKind.UnsignedChar, BasicType.TypeKind.Char, BasicType.TypeKind.SignedShortInt, BasicType.TypeKind.UnsignedShortInt, BasicType.TypeKind.SignedLongInt, BasicType.TypeKind.UnsignedLongInt, BasicType.TypeKind.SignedLongLongInt, BasicType.TypeKind.UnsignedLongLongInt)) {
                        // 処理系定義として受理
                    } else if (type.Unwrap().IsEnumeratedType()) {
                        // int型に無条件でキャストできると規定されているため受理
                    } else {
                        throw new CompilerException.SpecificationErrorException(ident.Range, "ビットフィールドの型は，修飾版又は非修飾版の_Bool，signed int，unsigned int 又は他の処理系定義の型でなければならない。(他の型が使えるのは処理系依存の仕様)");
                    }

                    if (bitWidth > type.SizeOf() * 8) {
                        throw new CompilerException.SpecificationErrorException(ident.Range, "ビットフィールドの幅の値は，指定された型のオブジェクトがもつビット数を超えてはならない。");
                    }
                    if (bitWidth == 0) {
                        // 値が 0 の場合，その宣言に宣言子があってはならない。
                        // 宣言子がなく，コロン及び幅だけをもつビットフィールド宣言は，名前のないビットフィールドを示す。
                        // この特別な場合として，幅が 0 のビットフィールド構造体メンバは，前のビットフィールド（もしあれば）が割り付けられていた単位に，それ以上のビットフィールドを詰め込まないことを指定する。
                        if (ident != null) {
                            throw new CompilerException.SpecificationErrorException(ident.Range, "ビットフィールドの幅の値が 0 の場合，その宣言に宣言子(名前)があってはならない");
                        }
                    }
                }

                Type = type;
                BitOffset = bitOffset;
                BitWidth = bitWidth;
            }

            public override CType Duplicate() {
                var ret = new BitFieldType(/* dummy */null, Type.Duplicate(), BitOffset, BitWidth);
                return ret;
            }

            /// <summary>
            /// ビットフィールドの型
            /// </summary>
            public CType Type {
                get; private set;
            }

            public override void Fixup(CType type) {
                if (Type is StubType) {
                    Type = type;
                } else {
                    Type.Fixup(type);
                }
            }

            /// <summary>
            /// 型のバイトサイズを取得（ビットフィールドの場合、元の型のサイズ）
            /// </summary>
            /// <returns></returns>
            public override int SizeOf() {
                return Type.SizeOf();
            }

            /// <summary>
            /// 型の境界調整（アラインメント）を取得（ビットフィールドの場合、元の型のアラインメント）
            /// </summary>
            /// <returns></returns>
            public override int AlignOf() {
                return Type.AlignOf();
            }
        }
    }
}
