namespace AnsiCParser {
    namespace DataType {
        /// <summary>
        ///     型修飾子
        /// </summary>
        public class TypeQualifierType : CType {
            public TypeQualifierType(CType type, TypeQualifier qualifier) {
                Type = type;
                Qualifier = qualifier;
            }

            public override CType Duplicate() {
                var ret = new TypeQualifierType(Type.Duplicate(), Qualifier);
                return ret;
            }

            public CType Type {
                get; private set;
            }

            public TypeQualifier Qualifier {
                get; set;
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
            public override int Sizeof() {
                return Type.Sizeof();
            }

            /// <summary>
            /// 型の境界調整（アラインメント）を取得（ビットフィールドの場合、元の型のアラインメント）
            /// </summary>
            /// <returns></returns>
            public override int Alignof() {
                return Type.Alignof();
            }

        }
    }

}
