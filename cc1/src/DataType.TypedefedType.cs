namespace AnsiCParser {
namespace DataType {
    /// <summary>
    ///     Typedefされた型
    /// </summary>
    public class TypedefedType : CType {
        public TypedefedType(Token ident, CType type) {
            Ident = ident;
            Type = type;
        }

        public override CType Duplicate() {
            var ret = new TypedefedType(Ident, Type.Duplicate());
            return ret;
        }

        public Token Ident {
            get;
        }

        public CType Type {
            get;
        }

        public override int Sizeof() {
            return Type.Sizeof();
        }

    }
}

}
