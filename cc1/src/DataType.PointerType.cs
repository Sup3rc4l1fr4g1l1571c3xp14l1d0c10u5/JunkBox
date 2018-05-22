namespace AnsiCParser {
namespace DataType {
    /// <summary>
    ///     ポインタ型
    /// </summary>
    public class PointerType : CType {
        public PointerType(CType type) {
            BaseType = type;
        }

        public override CType Duplicate() {
            var ret = new PointerType(BaseType);
            return ret;
        }

        public CType BaseType {
            get; private set;
        }

        public override void Fixup(CType type) {
            if (BaseType is StubType) {
                BaseType = type;
            } else {
                BaseType.Fixup(type);
            }
        }

        public override int Sizeof() {
            return Sizeof(BasicType.TypeKind.SignedInt);
        }

    }
}

}