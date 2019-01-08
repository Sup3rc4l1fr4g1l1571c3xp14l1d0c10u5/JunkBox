namespace AnsiCParser {
    namespace DataType {
        public interface IVisitor<out TResult, in TArg> {
            TResult OnArrayType(ArrayType self, TArg value);
            TResult OnBasicType(BasicType self, TArg value);
            TResult OnFunctionType(FunctionType self, TArg value);
            TResult OnPointerType(PointerType self, TArg value);
            TResult OnStubType(StubType self, TArg value);
            TResult OnEnumType(TaggedType.EnumType self, TArg value);
            TResult OnStructUnionType(TaggedType.StructUnionType self, TArg value);
            TResult OnTypedefType(TypedefType self, TArg value);
            TResult OnTypeQualifierType(TypeQualifierType self, TArg value);
            TResult OnBitFieldType(BitFieldType self, TArg value);
        }
    }
}
