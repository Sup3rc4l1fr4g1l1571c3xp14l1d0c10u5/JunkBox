using System;
using System.Collections.Generic;
using System.Linq;

namespace AnsiCParser {
    namespace DataType {
        /// <summary>
        /// CTypeをS式化する
        /// </summary>
        public class ToSExprVisitor : DataType.IVisitor<Lisp.Pair, Lisp.Pair> {
            private readonly HashSet<CType> _visited = new HashSet<CType>();

            public Lisp.Pair OnArrayType(DataType.ArrayType self, Lisp.Pair value) {
                _visited.Add(self);
                return Lisp.Util.makeList(Lisp.Util.makeSym("array"), Lisp.Util.makeNum(self.Length), self.BaseType.Accept(this, null));
            }

            public Lisp.Pair OnBasicType(DataType.BasicType self, Lisp.Pair value) {
                _visited.Add(self);
                switch (self.Kind) {
                    case DataType.BasicType.TypeKind.KAndRImplicitInt:
                        return Lisp.Util.makeList(Lisp.Util.makeSym("int"));
                    case DataType.BasicType.TypeKind.Void:
                        return Lisp.Util.makeList(Lisp.Util.makeSym("void"));
                    case DataType.BasicType.TypeKind.Char:
                        return Lisp.Util.makeList(Lisp.Util.makeSym("char"));
                    case DataType.BasicType.TypeKind.SignedChar:
                        return Lisp.Util.makeList(Lisp.Util.makeSym("signed-char"));
                    case DataType.BasicType.TypeKind.UnsignedChar:
                        return Lisp.Util.makeList(Lisp.Util.makeSym("unsigned-char"));
                    case DataType.BasicType.TypeKind.SignedShortInt:
                        return Lisp.Util.makeList(Lisp.Util.makeSym("signed-short-int"));
                    case DataType.BasicType.TypeKind.UnsignedShortInt:
                        return Lisp.Util.makeList(Lisp.Util.makeSym("unsigned-short-int"));
                    case DataType.BasicType.TypeKind.SignedInt:
                        return Lisp.Util.makeList(Lisp.Util.makeSym("signed-int"));
                    case DataType.BasicType.TypeKind.UnsignedInt:
                        return Lisp.Util.makeList(Lisp.Util.makeSym("unsigned-int"));
                    case DataType.BasicType.TypeKind.SignedLongInt:
                        return Lisp.Util.makeList(Lisp.Util.makeSym("signed-long-int"));
                    case DataType.BasicType.TypeKind.UnsignedLongInt:
                        return Lisp.Util.makeList(Lisp.Util.makeSym("unsigned-long-int"));
                    case DataType.BasicType.TypeKind.SignedLongLongInt:
                        return Lisp.Util.makeList(Lisp.Util.makeSym("signed-long-long-int"));
                    case DataType.BasicType.TypeKind.UnsignedLongLongInt:
                        return Lisp.Util.makeList(Lisp.Util.makeSym("unsigned-long-long-int"));
                    case DataType.BasicType.TypeKind.Float:
                        return Lisp.Util.makeList(Lisp.Util.makeSym("float"));
                    case DataType.BasicType.TypeKind.Double:
                        return Lisp.Util.makeList(Lisp.Util.makeSym("double"));
                    case DataType.BasicType.TypeKind.LongDouble:
                        return Lisp.Util.makeList(Lisp.Util.makeSym("long-double"));
                    case DataType.BasicType.TypeKind._Bool:
                        return Lisp.Util.makeList(Lisp.Util.makeSym("_Bool"));
                    case DataType.BasicType.TypeKind.Float_Complex:
                        return Lisp.Util.makeList(Lisp.Util.makeSym("float-_Complex"));
                    case DataType.BasicType.TypeKind.Double_Complex:
                        return Lisp.Util.makeList(Lisp.Util.makeSym("double-_Complex"));
                    case DataType.BasicType.TypeKind.LongDouble_Complex:
                        return Lisp.Util.makeList(Lisp.Util.makeSym("long-double-_Complex"));
                    case DataType.BasicType.TypeKind.Float_Imaginary:
                        return Lisp.Util.makeList(Lisp.Util.makeSym("float-_Imaginary"));
                    case DataType.BasicType.TypeKind.Double_Imaginary:
                        return Lisp.Util.makeList(Lisp.Util.makeSym("double-_Imaginary"));
                    case DataType.BasicType.TypeKind.LongDouble_Imaginary:
                        return Lisp.Util.makeList(Lisp.Util.makeSym("long-double-_Imaginary"));
                    default:
                        throw new Exception();

                }
            }

            public Lisp.Pair OnEnumType(DataType.TaggedType.EnumType self, Lisp.Pair value) {
                if (_visited.Contains(self) == false && self.Members != null) {
                    _visited.Add(self);
                    return Lisp.Util.makeList(
                        Lisp.Util.makeSym("enum"), Lisp.Util.makeStr(self.TagName),
                        Lisp.Util.makeList(
                            self.Members.Select(x =>
                                Lisp.Util.makeCons(
                                    Lisp.Util.makeStr(x.Ident?.Raw ?? ""),
                                    Lisp.Util.makeStr(x.Value.ToString())
                                )
                            ).Cast<object>().ToArray()
                        )
                    );
                }
                else {
                    return Lisp.Util.makeList(Lisp.Util.makeSym("enum"), Lisp.Util.makeStr(self.TagName));
                }
            }

            public Lisp.Pair OnFunctionType(DataType.FunctionType self, Lisp.Pair value) {
                _visited.Add(self);
                return Lisp.Util.makeList(
                    Lisp.Util.makeSym("func"), Lisp.Util.makeStr(self.ResultType.ToString()),
                    self.Arguments != null
                        ? Lisp.Util.makeList(
                            self.Arguments.Select(x =>
                                Lisp.Util.makeCons(
                                    Lisp.Util.makeStr(x.Ident?.Raw ?? ""),
                                    Lisp.Util.makeSym(x.StorageClass.ToString().ToLower()),
                                    x.Type.Accept(this, null)
                                )
                            ).Cast<object>().ToArray()
                        )
                        : Lisp.Util.Nil
                );
            }

            public Lisp.Pair OnPointerType(DataType.PointerType self, Lisp.Pair value) {
                _visited.Add(self);
                return Lisp.Util.makeList(Lisp.Util.makeSym("pointer"), self.BaseType.Accept(this, null));
            }

            public Lisp.Pair OnStructUnionType(DataType.TaggedType.StructUnionType self, Lisp.Pair value) {
                if (_visited.Contains(self) == false) {
                    _visited.Add(self);
                    return Lisp.Util.makeList(
                        Lisp.Util.makeSym(self.IsStructureType() ? "struct" : "union"),
                        Lisp.Util.makeStr(self.TagName),
                        self.Members != null
                            ? Lisp.Util.makeList(
                                self.Members.Select(x =>
                                    Lisp.Util.makeCons(
                                        Lisp.Util.makeStr(x.Ident?.Raw ?? ""),
                                        x.Type.Accept(this, null),
                                        Lisp.Util.makeNum(x.Offset)
                                    )
                                ).Cast<object>().ToArray()
                            )
                            : Lisp.Util.Nil
                    );
                }
                else {
                    return Lisp.Util.makeList(
                        Lisp.Util.makeSym(self.IsStructureType() ? "struct" : "union"),
                        Lisp.Util.makeStr(self.TagName)
                    );
                }
            }

            public Lisp.Pair OnStubType(DataType.StubType self, Lisp.Pair value) {
                _visited.Add(self);
                return Lisp.Util.makeList(Lisp.Util.makeSym("$"));
            }

            public Lisp.Pair OnTypedefedType(DataType.TypedefedType self, Lisp.Pair value) {
                _visited.Add(self);
                return Lisp.Util.makeList(Lisp.Util.makeSym("typedef"), Lisp.Util.makeStr(self.Ident?.Raw));
            }

            public Lisp.Pair OnTypeQualifierType(DataType.TypeQualifierType self, Lisp.Pair value) {
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

                return Lisp.Util.makeList(
                    Lisp.Util.makeSym("type-qual"),
                    Lisp.Util.makeList(qual.ToArray().Select(Lisp.Util.makeSym).Cast<object>().ToArray()),
                    self.Type.Accept(this, null)
                );
            }

            public Lisp.Pair OnBitFieldType(DataType.BitFieldType self, Lisp.Pair value) {
                _visited.Add(self);
                return Lisp.Util.makeList(
                    Lisp.Util.makeSym("bitfield"),
                    self.Type.Accept(this, null),
                    Lisp.Util.makeNum(self.BitOffset),
                    Lisp.Util.makeNum(self.BitWidth)
                );
            }

        }
    }
}