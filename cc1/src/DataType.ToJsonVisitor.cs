using System;
using System.Collections.Generic;
using System.Linq;
using Codeplex.Data;

namespace AnsiCParser {
    namespace DataType {
        /// <summary>
        /// CTypeをJSON化する
        /// </summary>
        public class ToJsonVisitor : DataType.IVisitor<object, object> {
            private readonly HashSet<CType> _visited = new HashSet<CType>();

            public object OnArrayType(DataType.ArrayType self, object value) {
                _visited.Add(self);
                return new {
                    Class ="ArrayType",
                    Length = self.Length,
                    ElementType = self.ElementType.Accept(this, null)
                };
            }

            public object OnBasicType(DataType.BasicType self, object value) {
                _visited.Add(self);
                return new {
                    Class ="BasicType",
                    Kind = self.Kind.ToString()
                };
            }

            public object OnEnumType(DataType.TaggedType.EnumType self, object value) {
                if (_visited.Contains(self) == false && self.Members != null) {
                    _visited.Add(self);
                    return new {
                        Class ="EnumType",
                        TagName = self.TagName,
                        Members = self.Members.Select(x => { return new { Ident = x.Ident?.Raw ?? "", Value = x.Value }; }).ToArray()
                    };
                }
                else {
                    return new {
                        Class ="EnumType",
                        TagName = self.TagName,
                    };
                }
            }

            public object OnFunctionType(DataType.FunctionType self, object value) {
                _visited.Add(self);
                return new {
                    Class ="FunctionType",
                    ResultType = self.ResultType.Accept(this,null),
                    Arguments = self.Arguments?.Select(x => { return new { Ident = x.Ident?.Raw ?? "", StorageClass = x.StorageClass.ToString(), Type = x.Type.Accept(this, null) }; }).ToArray()
                };
            }

            public object OnPointerType(DataType.PointerType self, object value) {
                _visited.Add(self);
                return new {
                    Class ="PointerType",
                    ReferencedType = self.ReferencedType.Accept(this, null),
                };
            }

            public object OnStructUnionType(DataType.TaggedType.StructUnionType self, object value) {
                if (_visited.Contains(self) == false) {
                    _visited.Add(self);
                    return new {
                        Class = self.IsStructureType() ? "StructType" : "UnionType",
                        TagName = self.TagName,
                        Members = self.Members?.Select(x => { return new { ident = x.Ident?.Raw ?? "", type = x.Type.Accept(this, null), offset= x.Offset }; }).ToArray()
                    };
                }
                else {
                    return new {
                        Class = self.IsStructureType() ? "StructType" : "UnionType",
                        TagName = self.TagName
                    };
                }
            }

            public object OnStubType(DataType.StubType self, object value) {
                _visited.Add(self);
                return new {
                    Class ="StubType",
                };
            }

            public object OnTypedefType(DataType.TypedefType self, object value) {
                _visited.Add(self);
                return new {
                    Class ="TypedefType",
                    Ident = self.Ident?.Raw
                };
            }

            public object OnTypeQualifierType(DataType.TypeQualifierType self, object value) {
                List<string> qual = new List<string>();
                if (self.Qualifier != TypeQualifier.None) {
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
                }
                _visited.Add(self);
                return new {
                    Class ="TypeQualifierType",
                    Qualifier = qual.ToArray(),
                    Type = self.Type.Accept(this, null)
                };
            }

            public object OnBitFieldType(DataType.BitFieldType self, object value) {
                _visited.Add(self);
                return new {
                    Class ="BitFieldType",
                    Type = self.Type.Accept(this, null),
                    BitOffset = self.BitOffset,
                    BitWidth = self.BitWidth,
                };
            }

        }
    }
}
