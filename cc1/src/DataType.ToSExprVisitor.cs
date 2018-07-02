using System;
using System.Collections.Generic;
using System.Linq;

namespace AnsiCParser {
    namespace DataType {
        /// <summary>
        /// CTypeをS式化する
        /// </summary>
        public class ToSExprVisitor : DataType.IVisitor<Schene.Pair, Schene.Pair> {
            private readonly HashSet<CType> _visited = new HashSet<CType>();

            public Schene.Pair OnArrayType(DataType.ArrayType self, Schene.Pair value) {
                _visited.Add(self);
                return Schene.Util.makeList(Schene.Util.makeSym("array"), Schene.Util.makeNum(self.Length), self.BaseType.Accept(this, null));
            }

            public Schene.Pair OnBasicType(DataType.BasicType self, Schene.Pair value) {
                _visited.Add(self);
                switch (self.Kind) {
                    case DataType.BasicType.TypeKind.KAndRImplicitInt:
                        return Schene.Util.makeList(Schene.Util.makeSym("int"));
                    case DataType.BasicType.TypeKind.Void:
                        return Schene.Util.makeList(Schene.Util.makeSym("void"));
                    case DataType.BasicType.TypeKind.Char:
                        return Schene.Util.makeList(Schene.Util.makeSym("char"));
                    case DataType.BasicType.TypeKind.SignedChar:
                        return Schene.Util.makeList(Schene.Util.makeSym("signed-char"));
                    case DataType.BasicType.TypeKind.UnsignedChar:
                        return Schene.Util.makeList(Schene.Util.makeSym("unsigned-char"));
                    case DataType.BasicType.TypeKind.SignedShortInt:
                        return Schene.Util.makeList(Schene.Util.makeSym("signed-short-int"));
                    case DataType.BasicType.TypeKind.UnsignedShortInt:
                        return Schene.Util.makeList(Schene.Util.makeSym("unsigned-short-int"));
                    case DataType.BasicType.TypeKind.SignedInt:
                        return Schene.Util.makeList(Schene.Util.makeSym("signed-int"));
                    case DataType.BasicType.TypeKind.UnsignedInt:
                        return Schene.Util.makeList(Schene.Util.makeSym("unsigned-int"));
                    case DataType.BasicType.TypeKind.SignedLongInt:
                        return Schene.Util.makeList(Schene.Util.makeSym("signed-long-int"));
                    case DataType.BasicType.TypeKind.UnsignedLongInt:
                        return Schene.Util.makeList(Schene.Util.makeSym("unsigned-long-int"));
                    case DataType.BasicType.TypeKind.SignedLongLongInt:
                        return Schene.Util.makeList(Schene.Util.makeSym("signed-long-long-int"));
                    case DataType.BasicType.TypeKind.UnsignedLongLongInt:
                        return Schene.Util.makeList(Schene.Util.makeSym("unsigned-long-long-int"));
                    case DataType.BasicType.TypeKind.Float:
                        return Schene.Util.makeList(Schene.Util.makeSym("float"));
                    case DataType.BasicType.TypeKind.Double:
                        return Schene.Util.makeList(Schene.Util.makeSym("double"));
                    case DataType.BasicType.TypeKind.LongDouble:
                        return Schene.Util.makeList(Schene.Util.makeSym("long-double"));
                    case DataType.BasicType.TypeKind._Bool:
                        return Schene.Util.makeList(Schene.Util.makeSym("_Bool"));
                    case DataType.BasicType.TypeKind.Float_Complex:
                        return Schene.Util.makeList(Schene.Util.makeSym("float-_Complex"));
                    case DataType.BasicType.TypeKind.Double_Complex:
                        return Schene.Util.makeList(Schene.Util.makeSym("double-_Complex"));
                    case DataType.BasicType.TypeKind.LongDouble_Complex:
                        return Schene.Util.makeList(Schene.Util.makeSym("long-double-_Complex"));
                    case DataType.BasicType.TypeKind.Float_Imaginary:
                        return Schene.Util.makeList(Schene.Util.makeSym("float-_Imaginary"));
                    case DataType.BasicType.TypeKind.Double_Imaginary:
                        return Schene.Util.makeList(Schene.Util.makeSym("double-_Imaginary"));
                    case DataType.BasicType.TypeKind.LongDouble_Imaginary:
                        return Schene.Util.makeList(Schene.Util.makeSym("long-double-_Imaginary"));
                    default:
                        throw new Exception();

                }
            }

            public Schene.Pair OnEnumType(DataType.TaggedType.EnumType self, Schene.Pair value) {
                if (_visited.Contains(self) == false && self.Members != null) {
                    _visited.Add(self);
                    return Schene.Util.makeList(
                        Schene.Util.makeSym("enum"), Schene.Util.makeStr(self.TagName),
                        Schene.Util.makeList(
                            self.Members.Select(x =>
                                Schene.Util.makeCons(
                                    Schene.Util.makeStr(x.Ident?.Raw ?? ""),
                                    Schene.Util.makeStr(x.Value.ToString())
                                )
                            ).Cast<object>().ToArray()
                        )
                    );
                }
                else {
                    return Schene.Util.makeList(Schene.Util.makeSym("enum"), Schene.Util.makeStr(self.TagName));
                }
            }

            public Schene.Pair OnFunctionType(DataType.FunctionType self, Schene.Pair value) {
                _visited.Add(self);
                return Schene.Util.makeList(
                    Schene.Util.makeSym("func"), Schene.Util.makeStr(self.ResultType.ToString()),
                    self.Arguments != null
                        ? Schene.Util.makeList(
                            self.Arguments.Select(x =>
                                Schene.Util.makeCons(
                                    Schene.Util.makeStr(x.Ident?.Raw ?? ""),
                                    Schene.Util.makeSym(x.StorageClass.ToString().ToLower()),
                                    x.Type.Accept(this, null)
                                )
                            ).Cast<object>().ToArray()
                        )
                        : Schene.Util.Nil
                );
            }

            public Schene.Pair OnPointerType(DataType.PointerType self, Schene.Pair value) {
                _visited.Add(self);
                return Schene.Util.makeList(Schene.Util.makeSym("pointer"), self.BaseType.Accept(this, null));
            }

            public Schene.Pair OnStructUnionType(DataType.TaggedType.StructUnionType self, Schene.Pair value) {
                if (_visited.Contains(self) == false) {
                    _visited.Add(self);
                    return Schene.Util.makeList(
                        Schene.Util.makeSym(self.IsStructureType() ? "struct" : "union"),
                        Schene.Util.makeStr(self.TagName),
                        self.Members != null
                            ? Schene.Util.makeList(
                                self.Members.Select(x =>
                                    Schene.Util.makeCons(
                                        Schene.Util.makeStr(x.Ident?.Raw ?? ""),
                                        x.Type.Accept(this, null),
                                        Schene.Util.makeNum(x.Offset)
                                    )
                                ).Cast<object>().ToArray()
                            )
                            : Schene.Util.Nil
                    );
                }
                else {
                    return Schene.Util.makeList(
                        Schene.Util.makeSym(self.IsStructureType() ? "struct" : "union"),
                        Schene.Util.makeStr(self.TagName)
                    );
                }
            }

            public Schene.Pair OnStubType(DataType.StubType self, Schene.Pair value) {
                _visited.Add(self);
                return Schene.Util.makeList(Schene.Util.makeSym("$"));
            }

            public Schene.Pair OnTypedefedType(DataType.TypedefedType self, Schene.Pair value) {
                _visited.Add(self);
                return Schene.Util.makeList(Schene.Util.makeSym("typedef"), Schene.Util.makeStr(self.Ident?.Raw));
            }

            public Schene.Pair OnTypeQualifierType(DataType.TypeQualifierType self, Schene.Pair value) {
                List<string> qual = new List<string>();
                if (self.Qualifier == TypeQualifier.None) {
                    qual.Add("none");
                }
                else {
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

                return Schene.Util.makeList(
                    Schene.Util.makeSym("type-qual"),
                    Schene.Util.makeList(qual.ToArray().Select(Schene.Util.makeSym).Cast<object>().ToArray()),
                    self.Type.Accept(this, null)
                );
            }

            public Schene.Pair OnBitFieldType(DataType.BitFieldType self, Schene.Pair value) {
                _visited.Add(self);
                return Schene.Util.makeList(
                    Schene.Util.makeSym("bitfield"),
                    self.Type.Accept(this, null),
                    Schene.Util.makeNum(self.BitOffset),
                    Schene.Util.makeNum(self.BitWidth)
                );
            }

        }
    }
}
