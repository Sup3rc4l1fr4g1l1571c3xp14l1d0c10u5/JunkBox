namespace AnsiCParser {
    namespace DataType {
        /// <summary>
        /// 配列型
        /// </summary>
        public class ArrayType : CType {

            /// <summary>
            /// 配列型を作成
            /// </summary>
            /// <param name="length">配列長(-1は不定長)</param>
            /// <param name="elementType">要素型</param>
            public ArrayType(int length, CType elementType) {
                Length = length;
                ElementType = elementType;
            }

            /// <summary>
            /// 複製を作成
            /// </summary>
            /// <returns></returns>
            public override CType Duplicate() {
                return new ArrayType(Length, ElementType);
            }

            /// <summary>
            /// 配列長(-1は指定無し)
            /// </summary>
            public int Length {
                get; set;
            }

            /// <summary>
            /// 要素型
            /// </summary>
            public CType ElementType {
                get; private set;
            }

            /// <summary>
            /// 型の構築時にStubTypeを埋める
            /// </summary>
            /// <param name="type"></param>
            public override void Fixup(CType type) {
                if (ElementType is StubType) {
                    ElementType = type;
                } else {
                    ElementType.Fixup(type);
                }
                // 6.2.5 (36) オブジェクト型は不完全型を含まないので，不完全型の配列は作ることができない。
                // 「不完全配列」と「不完全型の配列」は違うので注意
                if (ElementType.IsIncompleteType()) {
                    throw new CompilerException.SpecificationErrorException(LocationRange.Empty, "不完全型の配列を作ることはできない");
                }
                if (ElementType.Unwrap().IsContainFlexibleArrayMemberStruct()) {
                    throw new CompilerException.SpecificationErrorException(LocationRange.Empty, "フレキシブル配列メンバを持つ要素の配列を作ることはできない");
                }
            }

            /// <summary>
            /// 型のバイトサイズを取得
            /// </summary>
            /// <returns></returns>
            public override int SizeOf() {
                return Length < 0 ? SizeOf(BasicType.TypeKind.SignedInt) : ElementType.SizeOf() * Length;
            }

            /// <summary>
            /// 型の境界調整（アラインメント）を取得
            /// </summary>
            /// <returns></returns>
            public override int AlignOf() {
                return ElementType.AlignOf();
            }


        }
    }

}
