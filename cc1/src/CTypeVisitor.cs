namespace AnsiCParser {
    public static class CTypeVisitor {
        public interface IVisitor<out TResult, in TArg> {
            TResult OnArrayType(CType.ArrayType self, TArg value);
            TResult OnBasicType(CType.BasicType self, TArg value);
            TResult OnFunctionType(CType.FunctionType self, TArg value);
            TResult OnPointerType(CType.PointerType self, TArg value);
            TResult OnStubType(CType.StubType self, TArg value);
            TResult OnEnumType(CType.TaggedType.EnumType self, TArg value);
            TResult OnStructUnionType(CType.TaggedType.StructUnionType self, TArg value);
            TResult OnTypedefedType(CType.TypedefedType self, TArg value);
            TResult OnTypeQualifierType(CType.TypeQualifierType self, TArg value);
        }

        private static TResult AcceptInner<TResult, TArg>(dynamic type, IVisitor<TResult, TArg> visitor, TArg value) {
            return Accept<TResult, TArg>(type, visitor, value);
        }

        public static TResult Accept<TResult, TArg>(this CType type, IVisitor<TResult, TArg> visitor, TArg value) {
            return AcceptInner(type, visitor, value);
        }

        public static TResult Accept<TResult, TArg>(this CType.ArrayType self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnArrayType(self, value);
        }
        public static TResult Accept<TResult, TArg>(this CType.BasicType self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnBasicType(self, value);
        }
        public static TResult Accept<TResult, TArg>(this CType.FunctionType self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnFunctionType(self, value);
        }
        public static TResult Accept<TResult, TArg>(this CType.PointerType self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnPointerType(self, value);
        }
        public static TResult Accept<TResult, TArg>(this CType.StubType self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnStubType(self, value);
        }
        public static TResult Accept<TResult, TArg>(this CType.TaggedType.EnumType self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnEnumType(self, value);
        }
        public static TResult Accept<TResult, TArg>(this CType.TaggedType.StructUnionType self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnStructUnionType(self, value);
        }
        public static TResult Accept<TResult, TArg>(this CType.TypedefedType self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnTypedefedType(self, value);
        }
        public static TResult Accept<TResult, TArg>(this CType.TypeQualifierType self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnTypeQualifierType(self, value);
        }
    }
}
