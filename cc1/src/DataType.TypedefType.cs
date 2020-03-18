namespace AnsiCParser {
namespace DataType {
    /// <summary>
    /// Typedefされた型
    /// </summary>
    public class TypedefType : CType {
        public TypedefType(Token ident, CType type) {
            Ident = ident;
            Type = type;
        }

        public override CType Duplicate() {
            var ret = new TypedefType(Ident, Type.Duplicate());
            return ret;
        }

        public Token Ident {
            get;
        }

        public CType Type {
            get;
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
