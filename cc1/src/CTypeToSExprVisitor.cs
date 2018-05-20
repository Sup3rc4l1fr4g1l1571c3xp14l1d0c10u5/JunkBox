using System;
using System.Collections.Generic;
using System.Linq;

namespace AnsiCParser {
    /// <summary>
    /// CTypeをする
    /// </summary>
    public class CTypeToSExprVisitor : CTypeVisitor.IVisitor<Lisp.Pair, Lisp.Pair> {
        private HashSet<CType> visited = new HashSet<CType>();

        public Lisp.Pair OnArrayType(CType.ArrayType self, Lisp.Pair value) {
            visited.Add(self);
            return Lisp.Util.makeList(Lisp.Util.makeSym("array"), Lisp.Util.makeNum(self.Length), self.BaseType.Accept(this, null));
        }

        public Lisp.Pair OnBasicType(CType.BasicType self, Lisp.Pair value) {
            visited.Add(self);
            switch (self.Kind) {
                case CType.BasicType.TypeKind.KAndRImplicitInt:
                    return Lisp.Util.makeList(Lisp.Util.makeSym("int"));
                case CType.BasicType.TypeKind.Void:
                    return Lisp.Util.makeList(Lisp.Util.makeSym("void"));
                case CType.BasicType.TypeKind.Char:
                    return Lisp.Util.makeList(Lisp.Util.makeSym("char"));
                case CType.BasicType.TypeKind.SignedChar:
                    return Lisp.Util.makeList(Lisp.Util.makeSym("signed-char"));
                case CType.BasicType.TypeKind.UnsignedChar:
                    return Lisp.Util.makeList(Lisp.Util.makeSym("unsigned-char"));
                case CType.BasicType.TypeKind.SignedShortInt:
                    return Lisp.Util.makeList(Lisp.Util.makeSym("signed-short-int"));
                case CType.BasicType.TypeKind.UnsignedShortInt:
                    return Lisp.Util.makeList(Lisp.Util.makeSym("unsigned-short-int"));
                case CType.BasicType.TypeKind.SignedInt:
                    return Lisp.Util.makeList(Lisp.Util.makeSym("signed-int"));
                case CType.BasicType.TypeKind.UnsignedInt:
                    return Lisp.Util.makeList(Lisp.Util.makeSym("unsigned-int"));
                case CType.BasicType.TypeKind.SignedLongInt:
                    return Lisp.Util.makeList(Lisp.Util.makeSym("signed-long-int"));
                case CType.BasicType.TypeKind.UnsignedLongInt:
                    return Lisp.Util.makeList(Lisp.Util.makeSym("unsigned-long-int"));
                case CType.BasicType.TypeKind.SignedLongLongInt:
                    return Lisp.Util.makeList(Lisp.Util.makeSym("signed-long-long-int"));
                case CType.BasicType.TypeKind.UnsignedLongLongInt:
                    return Lisp.Util.makeList(Lisp.Util.makeSym("unsigned-long-long-int"));
                case CType.BasicType.TypeKind.Float:
                    return Lisp.Util.makeList(Lisp.Util.makeSym("float"));
                case CType.BasicType.TypeKind.Double:
                    return Lisp.Util.makeList(Lisp.Util.makeSym("double"));
                case CType.BasicType.TypeKind.LongDouble:
                    return Lisp.Util.makeList(Lisp.Util.makeSym("long-double"));
                case CType.BasicType.TypeKind._Bool:
                    return Lisp.Util.makeList(Lisp.Util.makeSym("_Bool"));
                case CType.BasicType.TypeKind.Float_Complex:
                    return Lisp.Util.makeList(Lisp.Util.makeSym("float-_Complex"));
                case CType.BasicType.TypeKind.Double_Complex:
                    return Lisp.Util.makeList(Lisp.Util.makeSym("double-_Complex"));
                case CType.BasicType.TypeKind.LongDouble_Complex:
                    return Lisp.Util.makeList(Lisp.Util.makeSym("long-double-_Complex"));
                case CType.BasicType.TypeKind.Float_Imaginary:
                    return Lisp.Util.makeList(Lisp.Util.makeSym("float-_Imaginary"));
                case CType.BasicType.TypeKind.Double_Imaginary:
                    return Lisp.Util.makeList(Lisp.Util.makeSym("double-_Imaginary"));
                case CType.BasicType.TypeKind.LongDouble_Imaginary:
                    return Lisp.Util.makeList(Lisp.Util.makeSym("long-double-_Imaginary"));
                default:
                    throw new Exception();

            }
        }

        public Lisp.Pair OnEnumType(CType.TaggedType.EnumType self, Lisp.Pair value) {
            if (visited.Contains(self) == false && self.Members != null) {
                visited.Add(self);
                return Lisp.Util.makeList(
                    Lisp.Util.makeSym("enum"), Lisp.Util.makeStr(self.TagName),
                    Lisp.Util.makeList(
                        self.Members.Select(x =>
                            Lisp.Util.makeCons(
                                Lisp.Util.makeStr(x.Ident?.Raw ?? ""),
                                Lisp.Util.makeStr(x.Value.ToString())
                            )
                        ).ToArray()
                    )
                );
            } else {
                return Lisp.Util.makeList(Lisp.Util.makeSym("enum"), Lisp.Util.makeStr(self.TagName));
            }
        }

        public Lisp.Pair OnFunctionType(CType.FunctionType self, Lisp.Pair value) {
            visited.Add(self);
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
                        ).ToArray()
                    )
                    : Lisp.Util.Nil
            );
        }

        public Lisp.Pair OnPointerType(CType.PointerType self, Lisp.Pair value) {
            visited.Add(self);
            return Lisp.Util.makeList(Lisp.Util.makeSym("pointer"), self.BaseType.Accept(this, null));
        }

        public Lisp.Pair OnStructUnionType(CType.TaggedType.StructUnionType self, Lisp.Pair value) {
            if (visited.Contains(self) == false) {
                visited.Add(self);
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
                            ).ToArray()
                          )
                        : Lisp.Util.Nil
                );
            } else {
                return Lisp.Util.makeList(
                    Lisp.Util.makeSym(self.IsStructureType() ? "struct" : "union"),
                    Lisp.Util.makeStr(self.TagName)
                );
            }
        }

        public Lisp.Pair OnStubType(CType.StubType self, Lisp.Pair value) {
            visited.Add(self);
            return Lisp.Util.makeList(Lisp.Util.makeSym("$"));
        }

        public Lisp.Pair OnTypedefedType(CType.TypedefedType self, Lisp.Pair value) {
            visited.Add(self);
            return Lisp.Util.makeList(Lisp.Util.makeSym("typedef"), Lisp.Util.makeStr(self.Ident?.Raw));
        }

        public Lisp.Pair OnTypeQualifierType(CType.TypeQualifierType self, Lisp.Pair value) {
            List<string> qual = new List<string>();
            if (self.Qualifier == TypeQualifier.None) {
                qual.Add("none");
            } else {
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
                Lisp.Util.makeList(qual.ToArray().Select(x => Lisp.Util.makeSym(x)).ToArray()),
                self.Type.Accept(this, null)
            );
        }
        public Lisp.Pair OnBitFieldType(CType.BitFieldType self, Lisp.Pair value) {
            visited.Add(self);
            return Lisp.Util.makeList(
                Lisp.Util.makeSym("bitfield"),
                self.Type.Accept(this, null),
                Lisp.Util.makeNum(self.BitOffset),
                Lisp.Util.makeNum(self.BitWidth)
            );
        }

    }

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
