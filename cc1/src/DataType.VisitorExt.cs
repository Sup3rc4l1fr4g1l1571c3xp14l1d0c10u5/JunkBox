namespace AnsiCParser {
namespace DataType {

    public static class VisitorExt {

        private static TResult AcceptInner<TResult, TArg>(dynamic type, IVisitor<TResult, TArg> visitor, TArg value) {
            return Accept<TResult, TArg>(type, visitor, value);
        }
        public static TResult Accept<TResult, TArg>(this CType type, IVisitor<TResult, TArg> visitor, TArg value) {
            return AcceptInner(type, visitor, value);
        }
        public static TResult Accept<TResult, TArg>(this ArrayType self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnArrayType(self, value);
        }
        public static TResult Accept<TResult, TArg>(this BasicType self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnBasicType(self, value);
        }
        public static TResult Accept<TResult, TArg>(this FunctionType self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnFunctionType(self, value);
        }
        public static TResult Accept<TResult, TArg>(this PointerType self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnPointerType(self, value);
        }
        public static TResult Accept<TResult, TArg>(this StubType self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnStubType(self, value);
        }
        public static TResult Accept<TResult, TArg>(this TaggedType.EnumType self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnEnumType(self, value);
        }
        public static TResult Accept<TResult, TArg>(this TaggedType.StructUnionType self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnStructUnionType(self, value);
        }
        public static TResult Accept<TResult, TArg>(this TypedefedType self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnTypedefedType(self, value);
        }
        public static TResult Accept<TResult, TArg>(this TypeQualifierType self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnTypeQualifierType(self, value);
        }
        public static TResult Accept<TResult, TArg>(this BitFieldType self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnBitFieldType(self, value);
        }
    }
}

}
