using System;
using System.Collections.Generic;
using System.Linq;

namespace AnsiCParser {
    public class CTypeDumpVisitor : CTypeVisitor.IVisitor<Cell, Cell> {
        public Cell OnArrayType(CType.ArrayType self, Cell value) {
            return Cell.Create("array", self.Length.ToString(), self.BaseType.Accept(this, null));
        }

        public Cell OnBasicType(CType.BasicType self, Cell value) {
            switch (self.Kind) {
                case CType.BasicType.TypeKind.KAndRImplicitInt:
                    return Cell.Create("int");
                case CType.BasicType.TypeKind.Void:
                    return Cell.Create("void");
                case CType.BasicType.TypeKind.Char:
                    return Cell.Create("char");
                case CType.BasicType.TypeKind.SignedChar:
                    return Cell.Create("signed char");
                case CType.BasicType.TypeKind.UnsignedChar:
                    return Cell.Create("unsigned char");
                case CType.BasicType.TypeKind.SignedShortInt:
                    return Cell.Create("signed short int");
                case CType.BasicType.TypeKind.UnsignedShortInt:
                    return Cell.Create("unsigned short int");
                case CType.BasicType.TypeKind.SignedInt:
                    return Cell.Create("signed int");
                case CType.BasicType.TypeKind.UnsignedInt:
                    return Cell.Create("unsigned int");
                case CType.BasicType.TypeKind.SignedLongInt:
                    return Cell.Create("signed long int");
                case CType.BasicType.TypeKind.UnsignedLongInt:
                    return Cell.Create("unsigned long int");
                case CType.BasicType.TypeKind.SignedLongLongInt:
                    return Cell.Create("signed long long int");
                case CType.BasicType.TypeKind.UnsignedLongLongInt:
                    return Cell.Create("unsigned long long int");
                case CType.BasicType.TypeKind.Float:
                    return Cell.Create("float");
                case CType.BasicType.TypeKind.Double:
                    return Cell.Create("double");
                case CType.BasicType.TypeKind.LongDouble:
                    return Cell.Create("long double");
                case CType.BasicType.TypeKind._Bool:
                    return Cell.Create("_Bool");
                case CType.BasicType.TypeKind.Float_Complex:
                    return Cell.Create("float _Complex");
                case CType.BasicType.TypeKind.Double_Complex:
                    return Cell.Create("double _Complex");
                case CType.BasicType.TypeKind.LongDouble_Complex:
                    return Cell.Create("long double _Complex");
                case CType.BasicType.TypeKind.Float_Imaginary:
                    return Cell.Create("float _Imaginary");
                case CType.BasicType.TypeKind.Double_Imaginary:
                    return Cell.Create("double _Imaginary");
                case CType.BasicType.TypeKind.LongDouble_Imaginary:
                    return Cell.Create("long double _Imaginary");
                default:
                    throw new Exception();

            }
        }

        public Cell OnEnumType(CType.TaggedType.EnumType self, Cell value) {
            return Cell.Create("enum", self.TagName, Cell.Create(self.Members.Select(x => Cell.Create(x.Ident?.Raw??"", x.Value.ToString())).ToArray()));
        }

        public Cell OnFunctionType(CType.FunctionType self, Cell value) {
            return Cell.Create("func", self.ResultType.ToString(), self.Arguments != null ? Cell.Create(self.Arguments.Select(x => Cell.Create(x.Ident?.Raw ?? "", x.StorageClass.ToString(), x.Type.Accept(this, null))).ToArray()) : Cell.Nil);
        }

        public Cell OnPointerType(CType.PointerType self, Cell value) {
            return Cell.Create("pointer", self.BaseType.Accept(this, null));
        }

        public Cell OnStructUnionType(CType.TaggedType.StructUnionType self, Cell value) {
            return Cell.Create(self.IsStructureType() ? "struct" : "union", self.TagName, self.Members != null ? Cell.Create(self.Members.Select(x => Cell.Create(x.Ident?.Raw ?? "", x.Type.Accept(this, null), x.BitSize.ToString())).ToArray()) : Cell.Nil);
        }

        public Cell OnStubType(CType.StubType self, Cell value) {
            return Cell.Create("$");
        }

        public Cell OnTypedefedType(CType.TypedefedType self, Cell value) {
            return Cell.Create("typedef", self.Ident?.Raw??"", self.Type.Accept(this, null));
        }

        public Cell OnTypeQualifierType(CType.TypeQualifierType self, Cell value) {
            List<string> qual = new List<string>();
            if ((self.Qualifier & TypeQualifier.None) == TypeQualifier.Const) {
                qual.Add("none");
            }
            if ((self.Qualifier & TypeQualifier.Const) == TypeQualifier.Const) {
                qual.Add("const");
            }
            if ((self.Qualifier & TypeQualifier.Restrict) == TypeQualifier.Const) {
                qual.Add("restrict");
            }
            if ((self.Qualifier & TypeQualifier.Volatile) == TypeQualifier.Const) {
                qual.Add("volatile");
            }
            if ((self.Qualifier & TypeQualifier.Near) == TypeQualifier.Const) {
                qual.Add("near");
            }
            if ((self.Qualifier & TypeQualifier.Far) == TypeQualifier.Const) {
                qual.Add("far");
            }
            if ((self.Qualifier & TypeQualifier.Invalid) == TypeQualifier.Const) {
                qual.Add("invalid");
            }
            return Cell.Create("type-qual", Cell.Create(qual.ToArray()), self.Type.Accept(this, null));
        }
    }
}