using System;
using System.Collections.Generic;
using System.Linq;

namespace AnsiCParser {
    public class CTypeDumpVisitor : CTypeVisitor.IVisitor<Cell, Cell> {
        private HashSet<CType> visited = new HashSet<CType>();

        public Cell OnArrayType(CType.ArrayType self, Cell value) {
            visited.Add(self);
            return Cell.Create("array", self.Length.ToString(), self.BaseType.Accept(this, null));
        }

        public Cell OnBasicType(CType.BasicType self, Cell value) {
            visited.Add(self);
            switch (self.Kind) {
                case CType.BasicType.TypeKind.KAndRImplicitInt:
                    return Cell.Create("int");
                case CType.BasicType.TypeKind.Void:
                    return Cell.Create("void");
                case CType.BasicType.TypeKind.Char:
                    return Cell.Create("char");
                case CType.BasicType.TypeKind.SignedChar:
                    return Cell.Create("signed-char");
                case CType.BasicType.TypeKind.UnsignedChar:
                    return Cell.Create("unsigned-char");
                case CType.BasicType.TypeKind.SignedShortInt:
                    return Cell.Create("signed-short-int");
                case CType.BasicType.TypeKind.UnsignedShortInt:
                    return Cell.Create("unsigned-short-int");
                case CType.BasicType.TypeKind.SignedInt:
                    return Cell.Create("signed-int");
                case CType.BasicType.TypeKind.UnsignedInt:
                    return Cell.Create("unsigned-int");
                case CType.BasicType.TypeKind.SignedLongInt:
                    return Cell.Create("signed-long-int");
                case CType.BasicType.TypeKind.UnsignedLongInt:
                    return Cell.Create("unsigned-long-int");
                case CType.BasicType.TypeKind.SignedLongLongInt:
                    return Cell.Create("signed-long-long-int");
                case CType.BasicType.TypeKind.UnsignedLongLongInt:
                    return Cell.Create("unsigned-long-long-int");
                case CType.BasicType.TypeKind.Float:
                    return Cell.Create("float");
                case CType.BasicType.TypeKind.Double:
                    return Cell.Create("double");
                case CType.BasicType.TypeKind.LongDouble:
                    return Cell.Create("long-double");
                case CType.BasicType.TypeKind._Bool:
                    return Cell.Create("_Bool");
                case CType.BasicType.TypeKind.Float_Complex:
                    return Cell.Create("float-_Complex");
                case CType.BasicType.TypeKind.Double_Complex:
                    return Cell.Create("double-_Complex");
                case CType.BasicType.TypeKind.LongDouble_Complex:
                    return Cell.Create("long-double-_Complex");
                case CType.BasicType.TypeKind.Float_Imaginary:
                    return Cell.Create("float-_Imaginary");
                case CType.BasicType.TypeKind.Double_Imaginary:
                    return Cell.Create("double-_Imaginary");
                case CType.BasicType.TypeKind.LongDouble_Imaginary:
                    return Cell.Create("long-double-_Imaginary");
                default:
                    throw new Exception();

            }
        }

        public Cell OnEnumType(CType.TaggedType.EnumType self, Cell value) {
            if (visited.Contains(self) == false && self.Members != null) {
                visited.Add(self);
                return Cell.Create("enum", self.TagName, Cell.Create(self.Members.Select(x => Cell.Create(x.Ident?.Raw ?? "", x.Value.ToString())).ToArray()));
            } else {
                return Cell.Create("enum", self.TagName);
            }
        }

        public Cell OnFunctionType(CType.FunctionType self, Cell value) {
                visited.Add(self);
            return Cell.Create("func", self.ResultType.ToString(), self.Arguments != null ? Cell.Create(self.Arguments.Select(x => Cell.Create(x.Ident?.Raw ?? "", x.StorageClass.ToString(), x.Type.Accept(this, null))).ToArray()) : Cell.Nil);
        }

        public Cell OnPointerType(CType.PointerType self, Cell value) {
                visited.Add(self);
            return Cell.Create("pointer", self.BaseType.Accept(this, null));
        }

        public Cell OnStructUnionType(CType.TaggedType.StructUnionType self, Cell value) {
            if (visited.Contains(self) == false) {
                visited.Add(self);
                return Cell.Create(self.IsStructureType() ? "struct" : "union", self.TagName, self.Members != null ? Cell.Create(self.Members.Select(x => Cell.Create(x.Ident?.Raw ?? "", x.Type.Accept(this, null), x.BitSize.ToString())).ToArray()) : Cell.Nil);
            } else {
                return Cell.Create(self.IsStructureType() ? "struct" : "union", self.TagName);
            }
        }

        public Cell OnStubType(CType.StubType self, Cell value) {
            visited.Add(self);
            return Cell.Create("$");
        }

        public Cell OnTypedefedType(CType.TypedefedType self, Cell value) {
            visited.Add(self);
            return Cell.Create("typedef", self.Ident?.Raw);
        }

        public Cell OnTypeQualifierType(CType.TypeQualifierType self, Cell value) {
            List<string> qual = new List<string>();
            if (self.Qualifier.HasFlag(TypeQualifier.None)) {
                qual.Add("none");
            }
            if (self.Qualifier.HasFlag(TypeQualifier.Const)) {
                qual.Add("const");
            }
            if (self.Qualifier.HasFlag(TypeQualifier.Restrict)) {
                qual.Add("restrict");
            }
            if (self.Qualifier.HasFlag(TypeQualifier.Volatile)) {
                qual.Add("volatile");
            }
            if (self.Qualifier.HasFlag(TypeQualifier.Near)) {
                qual.Add("near");
            }
            if (self.Qualifier.HasFlag(TypeQualifier.Far)) {
                qual.Add("far");
            }
            if (self.Qualifier.HasFlag(TypeQualifier.Invalid)) {
                qual.Add("invalid");
            }
            return Cell.Create("type-qual", Cell.Create(qual.ToArray()), self.Type.Accept(this, null));
        }
    }

    public class CTypeDumpVisitor2 : CTypeVisitor.IVisitor<string, string> {

        private HashSet<CType> visited = new HashSet<CType>();

        public string OnArrayType(CType.ArrayType self, string value) {
            visited.Add(self);
            return $"{self.BaseType.Accept(this, value+$"[{(self.Length != -1 ? self.Length.ToString() : "")}]")}";
        }

        public string OnBasicType(CType.BasicType self, string value) {
            visited.Add(self);
            var str = "";
            switch (self.Kind) {
                case CType.BasicType.TypeKind.KAndRImplicitInt:
                    str = "int";
                    break;
                case CType.BasicType.TypeKind.Void:
                    str = "void";
                    break;
                case CType.BasicType.TypeKind.Char:
                    str = "char";
                    break;
                case CType.BasicType.TypeKind.SignedChar:
                    str = "signed char";
                    break;
                case CType.BasicType.TypeKind.UnsignedChar:
                    str = "unsigned char";
                    break;
                case CType.BasicType.TypeKind.SignedShortInt:
                    str = "signed short int";
                    break;
                case CType.BasicType.TypeKind.UnsignedShortInt:
                    str = "unsigned short int";
                    break;
                case CType.BasicType.TypeKind.SignedInt:
                    str = "signed int";
                    break;
                case CType.BasicType.TypeKind.UnsignedInt:
                    str = "unsigned int";
                    break;
                case CType.BasicType.TypeKind.SignedLongInt:
                    str = "signed long int";
                    break;
                case CType.BasicType.TypeKind.UnsignedLongInt:
                    str = "unsigned long int";
                    break;
                case CType.BasicType.TypeKind.SignedLongLongInt:
                    str = "signed long long int";
                    break;
                case CType.BasicType.TypeKind.UnsignedLongLongInt:
                    str = "unsigned long long int";
                    break;
                case CType.BasicType.TypeKind.Float:
                    str = "float";
                    break;
                case CType.BasicType.TypeKind.Double:
                    str = "double";
                    break;
                case CType.BasicType.TypeKind.LongDouble:
                    str = "long double";
                    break;
                case CType.BasicType.TypeKind._Bool:
                    str = "_Bool";
                    break;
                case CType.BasicType.TypeKind.Float_Complex:
                    str = "float _Complex";
                    break;
                case CType.BasicType.TypeKind.Double_Complex:
                    str = "double _Complex";
                    break;
                case CType.BasicType.TypeKind.LongDouble_Complex:
                    str = "long double _Complex";
                    break;
                case CType.BasicType.TypeKind.Float_Imaginary:
                    str = "float _Imaginary";
                    break;
                case CType.BasicType.TypeKind.Double_Imaginary:
                    str = "double _Imaginary";
                    break;
                case CType.BasicType.TypeKind.LongDouble_Imaginary:
                    str = "long double _Imaginary";
                    break;
                default:
                    throw new Exception();

            }
            return str + (String.IsNullOrEmpty(value) ? "" : (" " + value));
        }

        public string OnEnumType(CType.TaggedType.EnumType self, string value) {
            if (visited.Contains(self)) {
                return $"enum {self.TagName}";
            } else {
                visited.Add(self);
                var members = string.Join(", ", self.Members.Select(x => $"{x.Ident.Raw} = {x.Value.ToString()}"));
                return $"enum {self.TagName} {{ {members} }}";
            }
        }

        private string StorageClassToString(StorageClassSpecifier sc) {
            switch (sc) {
                case StorageClassSpecifier.None:
                    return "";
                case StorageClassSpecifier.Auto:
                case StorageClassSpecifier.Register:
                case StorageClassSpecifier.Static:
                case StorageClassSpecifier.Extern:
                case StorageClassSpecifier.Typedef:
                    return sc.ToString().ToLower();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public string OnFunctionType(CType.FunctionType self, string value) {
            visited.Add(self);
            var args = self.Arguments?.Select(x => StorageClassToString(x.StorageClass) + x.Type.Accept(this, x.Ident?.Raw ?? "")).ToList();
            if (args == null) {
                args = new List<string>();
            } else if (args.Count == 0) {
                args.Add("void");
            }
            if (self.HasVariadic) {
                args.Add("...");
            }
            return $"{self.ResultType.Accept(this, value + $"({String.Join(", ", args)})")}";
        }

        public string OnPointerType(CType.PointerType self, string value) {
            visited.Add(self);
            if (self.BaseType is CType.ArrayType || self.BaseType is CType.FunctionType) {
                return $"{self.BaseType.Accept(this, $"(*{value})")}";
            } else {
                return $"{self.BaseType.Accept(this, $"*{value}")}";
            }
        }

        public string OnStructUnionType(CType.TaggedType.StructUnionType self, string value) {
            if (visited.Contains(self)) {
                return $"{(self.IsStructureType() ? "struct" : "union")} {self.TagName}";
            } else {
                visited.Add(self);
                var members = string.Join(" ", self.Members.Select(x => $"{x.Type.Accept(this, x.Ident?.Raw ?? "")}{((x.BitSize != -1) ? " : " + x.BitSize.ToString() : "")};"));
                return $"{(self.IsStructureType() ? "struct" : "union")} {self.TagName} {{ {members} }}" + (String.IsNullOrEmpty(value) ? "" : (" " + value));
            }
        }

        public string OnStubType(CType.StubType self, string value) {
            return "$";
        }

        public string OnTypedefedType(CType.TypedefedType self, string value) {
            visited.Add(self);
            return self.Ident.Raw + (String.IsNullOrEmpty(value) ? "" : (" " + value));
        }

        public string OnTypeQualifierType(CType.TypeQualifierType self, string value) {
            List<string> qual = new List<string>();
            if (self.Qualifier.HasFlag(TypeQualifier.None)) {
                qual.Add("");
            }
            if (self.Qualifier.HasFlag(TypeQualifier.Const)) {
                qual.Add("const");
            }
            if (self.Qualifier.HasFlag(TypeQualifier.Restrict)) {
                qual.Add("restrict");
            }
            if (self.Qualifier.HasFlag(TypeQualifier.Volatile)) {
                qual.Add("volatile");
            }
            if (self.Qualifier.HasFlag(TypeQualifier.Near)) {
                qual.Add("near");
            }
            if (self.Qualifier.HasFlag(TypeQualifier.Far)) {
                qual.Add("far");
            }
            if (self.Qualifier.HasFlag(TypeQualifier.Invalid)) {
                qual.Add("invalid");
            }
            return string.Join(" ", qual) + self.Type.Accept(this, value);
        }
    }
}
