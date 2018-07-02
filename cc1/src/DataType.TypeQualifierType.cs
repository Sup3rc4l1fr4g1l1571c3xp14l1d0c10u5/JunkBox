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

            public override int Sizeof() {
                return Type.Sizeof();
            }

        }
    }

}
