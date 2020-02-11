namespace AnsiCParser {
    namespace DataType {
        /// <summary>
        ///     ポインタ型
        /// </summary>
        public class PointerType : CType {
            public PointerType(CType referencedType) {
                ReferencedType = referencedType;
            }

            public override CType Duplicate() {
                var ret = new PointerType(ReferencedType);
                return ret;
            }

            /// <summary>
            /// 被参照型
            /// ポインタ型（pointer type）は，被参照型（referenced type）と呼ぶ関数型，オブジェクト型又は不完全型から派生することができる。
            /// </summary>
            public CType ReferencedType {
                get; private set;
            }

            public override void Fixup(CType type) {
                if (ReferencedType is StubType) {
                    ReferencedType = type;
                } else {
                    ReferencedType.Fixup(type);
                }
            }

            /// <summary>
            /// 型のバイトサイズを取得（ビットフィールドの場合、元の型のサイズ）
            /// </summary>
            /// <returns></returns>
            public override int SizeOf() {
                return SizeOf(BasicType.TypeKind.SignedInt);
            }

            /// <summary>
            /// 型の境界調整（アラインメント）を取得（ビットフィールドの場合、元の型のアラインメント）
            /// </summary>
            /// <returns></returns>
            public override int AlignOf() {
                return AlignOf(BasicType.TypeKind.SignedInt);
            }

        }
    }

}
