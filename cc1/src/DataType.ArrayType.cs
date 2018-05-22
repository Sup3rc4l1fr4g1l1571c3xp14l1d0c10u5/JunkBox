namespace AnsiCParser {
    namespace DataType {
        /// <summary>
        ///     配列型
        /// </summary>
        public class ArrayType : CType {

            /// <summary>
            /// 配列型を作成
            /// </summary>
            /// <param name="length">配列長(-1は不定長)</param>
            /// <param name="type">要素型</param>
            public ArrayType(int length, CType type) {
                Length = length;
                BaseType = type;
            }

            /// <summary>
            /// 複製を作成
            /// </summary>
            /// <returns></returns>
            public override CType Duplicate() {
                var ret = new ArrayType(Length, BaseType);
                return ret;
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
            public CType BaseType {
                get; private set;
            }

            /// <summary>
            /// 型の構築時にStubTypeを埋める
            /// </summary>
            /// <param name="type"></param>
            public override void Fixup(CType type) {
                if (BaseType is StubType) {
                    BaseType = type;
                } else {
                    BaseType.Fixup(type);
                }
            }

            /// <summary>
            /// 型のバイトサイズを取得
            /// </summary>
            /// <returns></returns>
            public override int Sizeof() {
                return Length < 0 ? Sizeof(BasicType.TypeKind.SignedInt) : BaseType.Sizeof() * Length;
            }

        }
    }

}