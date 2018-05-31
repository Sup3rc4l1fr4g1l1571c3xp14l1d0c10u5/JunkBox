using System;
using System.Collections.Generic;
using System.Linq;

namespace AnsiCParser {
    namespace DataType {
        /// <summary>
        /// C言語書式でCTypeを文字列化する
        /// </summary>
        public class ToStringVisitor : IVisitor<string, string> {

            private readonly HashSet<CType> _visited = new HashSet<CType>();

            public string OnArrayType(ArrayType self, string value) {
                _visited.Add(self);
                return $"{self.BaseType.Accept(this, value + $"[{(self.Length != -1 ? self.Length.ToString() : "")}]")}";
            }

            public string OnBasicType(BasicType self, string value) {
                _visited.Add(self);
                string str;
                switch (self.Kind) {
                    case BasicType.TypeKind.KAndRImplicitInt:
                        str = "int";
                        break;
                    case BasicType.TypeKind.Void:
                        str = "void";
                        break;
                    case BasicType.TypeKind.Char:
                        str = "char";
                        break;
                    case BasicType.TypeKind.SignedChar:
                        str = "signed char";
                        break;
                    case BasicType.TypeKind.UnsignedChar:
                        str = "unsigned char";
                        break;
                    case BasicType.TypeKind.SignedShortInt:
                        str = "signed short int";
                        break;
                    case BasicType.TypeKind.UnsignedShortInt:
                        str = "unsigned short int";
                        break;
                    case BasicType.TypeKind.SignedInt:
                        str = "signed int";
                        break;
                    case BasicType.TypeKind.UnsignedInt:
                        str = "unsigned int";
                        break;
                    case BasicType.TypeKind.SignedLongInt:
                        str = "signed long int";
                        break;
                    case BasicType.TypeKind.UnsignedLongInt:
                        str = "unsigned long int";
                        break;
                    case BasicType.TypeKind.SignedLongLongInt:
                        str = "signed long long int";
                        break;
                    case BasicType.TypeKind.UnsignedLongLongInt:
                        str = "unsigned long long int";
                        break;
                    case BasicType.TypeKind.Float:
                        str = "float";
                        break;
                    case BasicType.TypeKind.Double:
                        str = "double";
                        break;
                    case BasicType.TypeKind.LongDouble:
                        str = "long double";
                        break;
                    case BasicType.TypeKind._Bool:
                        str = "_Bool";
                        break;
                    case BasicType.TypeKind.Float_Complex:
                        str = "float _Complex";
                        break;
                    case BasicType.TypeKind.Double_Complex:
                        str = "double _Complex";
                        break;
                    case BasicType.TypeKind.LongDouble_Complex:
                        str = "long double _Complex";
                        break;
                    case BasicType.TypeKind.Float_Imaginary:
                        str = "float _Imaginary";
                        break;
                    case BasicType.TypeKind.Double_Imaginary:
                        str = "double _Imaginary";
                        break;
                    case BasicType.TypeKind.LongDouble_Imaginary:
                        str = "long double _Imaginary";
                        break;
                    default:
                        throw new Exception();

                }

                return str + (String.IsNullOrEmpty(value) ? "" : (" " + value));
            }

            public string OnEnumType(TaggedType.EnumType self, string value) {
                if (_visited.Contains(self)) {
                    return $"enum {self.TagName}";
                }

                _visited.Add(self);
                var members = string.Join(", ", self.Members.Select(x => $"{x.Ident.Raw} = {x.Value.ToString()}"));
                return $"enum {self.TagName} {{ {members} }}";
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

            public string OnFunctionType(FunctionType self, string value) {
                _visited.Add(self);
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

            public string OnPointerType(PointerType self, string value) {
                _visited.Add(self);
                if (self.BaseType is ArrayType || self.BaseType is FunctionType) {
                    return $"{self.BaseType.Accept(this, $"(*{value})")}";
                }

                return $"{self.BaseType.Accept(this, $"*{value}")}";
            }

            public string OnStructUnionType(TaggedType.StructUnionType self, string value) {
                if (_visited.Contains(self)) {
                    return $"{(self.IsStructureType() ? "struct" : "union")} {self.TagName}";
                }

                _visited.Add(self);
                if (self.Members != null) {
                    var members = string.Join(" ", self.Members.Select(x => {
                        BitFieldType bft;
                        if (x.Type.IsBitField(out bft)) {
                            return $"{x.Type.Accept(this, x.Ident?.Raw ?? "")}{((bft.BitWidth != -1) ? " : " + bft.BitWidth.ToString() : "")};";
                        }

                        return $"{x.Type.Accept(this, x.Ident?.Raw ?? "")};";
                    }));
                    return $"{(self.IsStructureType() ? "struct" : "union")} {self.TagName} {{ {members} }}" + (String.IsNullOrEmpty(value) ? "" : (" " + value));
                } else {
                    return $"{(self.IsStructureType() ? "struct" : "union")} {self.TagName} " + (String.IsNullOrEmpty(value) ? "" : (" " + value));
                }
            }

            public string OnStubType(StubType self, string value) {
                return "$";
            }

            public string OnTypedefedType(TypedefedType self, string value) {
                _visited.Add(self);
                return self.Ident.Raw + (String.IsNullOrEmpty(value) ? "" : (" " + value));
            }

            public string OnTypeQualifierType(TypeQualifierType self, string value) {
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
                return string.Join(" ", qual); //
            }

            public string OnBitFieldType(BitFieldType self, string value) {
                return $"{self.Type.Accept(this, "")}{((self.BitWidth != -1) ? " : " + self.BitWidth : "")};";
            }
        }
    }
}
