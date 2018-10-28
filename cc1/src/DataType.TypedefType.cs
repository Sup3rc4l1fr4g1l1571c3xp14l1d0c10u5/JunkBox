namespace AnsiCParser {
namespace DataType {
    /// <summary>
    ///     Typedefされた型
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

        public override int Sizeof() {
            return Type.Sizeof();
        }

    }
}

}
