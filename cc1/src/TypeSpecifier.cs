using System;

namespace AnsiCParser {
    /// <summary>
    /// 型指定子
    /// </summary>
    [Flags]
    public enum TypeSpecifier {
        None = 0x0000,
        Void = 0x0001,
        Char = 0x0002,
        Int = 0x0003,
        Float = 0x0004,
        Double = 0x0005,
        _Bool = 0x0006,
        TypeMask = 0x000F,
        Short = 0x0010,
        Long = 0x0020,
        LLong = 0x0030,
        SizeMask = 0x00F0,
        Signed = 0x0100,
        Unsigned = 0x0200,
        SignMask = 0x0300,
        _Complex = 0x0400,
        _Imaginary = 0x0800,
        CIMask = 0x0C00,
        Invalid = 0x1000,
    }

    public static class TypeSpecifierExt {
        public static string TypeFlagToCString(this TypeSpecifier self, LocationRange range) {
            switch (self.TypeFlag()) {
                case TypeSpecifier.Void:
                    return "void";
                case TypeSpecifier.Char:
                    return "char";
                case TypeSpecifier.Int:
                    return "int";
                case TypeSpecifier.Float:
                    return "float";
                case TypeSpecifier.Double:
                    return "double";
                case TypeSpecifier._Bool:
                    return "_Bool";
                default:
                    throw new CompilerException.InternalErrorException(range, "型指定子の基本型部分が設定されていない。（おそらく本処理系の実装ミス）");
            }
        }
        public static TypeSpecifier TypeFlag(this TypeSpecifier self) {
            return TypeSpecifier.TypeMask & self;
        }

        public static TypeSpecifier SizeFlag(this TypeSpecifier self) {
            return TypeSpecifier.SizeMask & self;
        }

        public static TypeSpecifier SignFlag(this TypeSpecifier self) {
            return TypeSpecifier.SignMask & self;
        }

        public static TypeSpecifier ComplexFlag(this TypeSpecifier self) {
            return TypeSpecifier.CIMask & self;
        }

        public static TypeSpecifier Marge(this TypeSpecifier self, TypeSpecifier other, LocationRange range) {
            TypeSpecifier type = TypeSpecifier.None;

            if (self.TypeFlag() == TypeSpecifier.None) {
                type = other.TypeFlag();
            } else if (other.TypeFlag() == TypeSpecifier.None) {
                type = self.TypeFlag();
            } else if (self.TypeFlag() != other.TypeFlag()) {
                throw new CompilerException.SpecificationErrorException(range, $"{self.TypeFlagToCString(range)}と{other.TypeFlagToCString(range)}は組み合わせられない型指定子です。");
            }

            TypeSpecifier size = TypeSpecifier.None;
            if (self.SizeFlag() == TypeSpecifier.None) {
                size = other.SizeFlag();
            } else if (other.SizeFlag() == TypeSpecifier.None) {
                size = self.SizeFlag();
            } else {
                if (self.SizeFlag() == other.SizeFlag()) {
                    if (self.SizeFlag() == TypeSpecifier.Long || self.SizeFlag() == TypeSpecifier.LLong) {
                        size = TypeSpecifier.LLong;
                    }
                } else if (self.SizeFlag() != other.SizeFlag()) {
                    if (self.SizeFlag() == TypeSpecifier.Long && other.SizeFlag() == TypeSpecifier.LLong) {
                        size = TypeSpecifier.LLong;
                    } else if (self.SizeFlag() == TypeSpecifier.LLong && other.SizeFlag() == TypeSpecifier.Long) {
                        size = TypeSpecifier.LLong;
                    } else {
                        throw new Exception();
                    }
                }
            }

            TypeSpecifier sign = TypeSpecifier.None;
            if (self.SignFlag() == TypeSpecifier.None && other.SignFlag() != TypeSpecifier.None) {
                sign = other.SignFlag();
            } else if (self.SignFlag() != TypeSpecifier.None && other.SignFlag() == TypeSpecifier.None) {
                sign = self.SignFlag();
            } else if (self.SignFlag() != other.SignFlag()) {
                throw new Exception();
            }

            TypeSpecifier complex = TypeSpecifier.None;
            if (self.ComplexFlag() == TypeSpecifier.None && other.ComplexFlag() != TypeSpecifier.None) {
                complex = other.ComplexFlag();
            } else if (self.ComplexFlag() != TypeSpecifier.None && other.ComplexFlag() == TypeSpecifier.None) {
                complex = self.ComplexFlag();
            } else if (self.ComplexFlag() != other.ComplexFlag()) {
                throw new Exception();
            }

            if (sign != TypeSpecifier.None && complex != TypeSpecifier.None) {
                throw new Exception();
            }

            return type | size | sign | complex;
        }
    }

}
