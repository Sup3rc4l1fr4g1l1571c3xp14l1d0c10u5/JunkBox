using System;
using System.Collections.Generic;
using System.Linq;

namespace AnsiCParser {
    /// <summary>
    /// C言語書式でCTypeを文字列化する
    /// </summary>
    public class CTypeToStringVisitor : CTypeVisitor.IVisitor<string, string> {

        private HashSet<CType> visited = new HashSet<CType>();

        public string OnArrayType(CType.ArrayType self, string value) {
            visited.Add(self);
            return $"{self.BaseType.Accept(this, value + $"[{(self.Length != -1 ? self.Length.ToString() : "")}]")}";
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
                var members = string.Join(" ", self.Members.Select(x => {
                    CType.BitFieldType bft;
                    if (x.Type.IsBitField(out bft)) {
                        return $"{x.Type.Accept(this, x.Ident?.Raw ?? "")}{((bft.BitWidth != -1) ? " : " + bft.BitWidth.ToString() : "")};";
                    } else {
                        return $"{x.Type.Accept(this, x.Ident?.Raw ?? "")};";
                    }
                }));
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
            qual.Add(self.Type.Accept(this, value));
            return string.Join(" ", qual);//
        }
        public string OnBitFieldType(CType.BitFieldType self, string value) {
            return $"{self.Type.Accept(this, "")}{((self.BitWidth != -1) ? " : " + self.BitWidth.ToString() : "")};";
        }
    }
}
