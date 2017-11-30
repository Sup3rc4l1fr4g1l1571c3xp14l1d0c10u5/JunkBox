using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

// Constraints
// ・Do not use a parser generator such as yacc / lex. 

namespace AnsiCParser {
    class Program {
        static void Main(string[] args) {
            foreach (var arg in args) {
                RaiseError<Exception>(@"
typedef char BUF[256];
BUF hoge(BUF buf) { /* エラー: hoge は配列を返す関数として宣言されています */
return buf;
}
");

                RaiseError<Exception>(@"
float f(float);

int main(void)
{
float x;
f(x);
}

float f(x)
float x;    /* 警告: promoted argument ‘x’ doesn’t match prototype [-Wpedantic] */
{ return x;}

");

                Success(@"
int count();

int main(void) {
    int n = count(""hello"");
    return n;
}

int count(str)
char* str;
{
    char* p = str;
    while (*p != '\0') {
        p++;
    }
    return p - str;
}
            ");

                //System.IO.File.ReadAllText(arg);
            }
        }

        public static void Success(string input) {
            var grammer = new Grammer();
            grammer.Parse(input);
        }
        public static void RaiseError<T>(string input) where T : Exception {
            try {
                var grammer = new Grammer();
                grammer.Parse(input);
            } catch (T) {
                return;
            }
            throw new Exception($"例外{typeof(T).Name}が発生すべきだが発生しなかった");
        }
    }

    /// <summary>
    /// 記憶クラス指定子
    /// </summary>
    public enum StorageClass {
        None,
        Auto,
        Register,
        Static,
        Extern,
        Typedef
    }

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

    /// <summary>
    /// 型修飾子
    /// </summary>
    [Flags]
    public enum TypeQualifier {
        None = 0x0000,
        Const = 0x0001,
        Volatile = 0x002,
        Restrict = 0x0004,
        Near = 0x0010,
        Far = 0x0020,
        Invalid = 0x1000,
    }

    /// <summary>
    /// 関数指定子
    /// </summary>
    [Flags]
    public enum FunctionSpecifier {
        None = 0x0000,
        Inline = 0x0001,
    }

    public static class Ext {
        public static StorageClass Marge(this StorageClass self, StorageClass other) {
            if (self == StorageClass.None) {
                return other;
            } else if (other == StorageClass.None) {
                return self;
            } else {
                if (self != other) {
                    throw new Exception("");
                } else {
                    return self;
                }
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

        public static TypeSpecifier Marge(this TypeSpecifier self, TypeSpecifier other) {
            TypeSpecifier type = TypeSpecifier.None;

            if (self.TypeFlag() == TypeSpecifier.None) {
                type = other.TypeFlag();
            } else if (other.TypeFlag() == TypeSpecifier.None) {
                type = self.TypeFlag();
            } else if (self.TypeFlag() != other.TypeFlag()) {
                throw new Exception();
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
            if (self.SignFlag() == TypeSpecifier.None) {
                sign = other.SignFlag();
            } else if (other.SignFlag() == TypeSpecifier.None) {
                sign = self.SignFlag();
            } else if (self.SignFlag() != other.SignFlag()) {
                throw new Exception();
            }

            return type | size | sign;
        }
        public static TypeQualifier Marge(this TypeQualifier self, TypeQualifier other) {
            return self | other;
        }
        public static FunctionSpecifier Marge(this FunctionSpecifier self, FunctionSpecifier other) {
            return self | other;
        }

    }

    public abstract class CType {


        /// <summary>
        /// 型指定を解決する
        /// </summary>
        /// <param name="baseType"></param>
        /// <param name="stack"></param>
        /// <returns></returns>
        public static CType Resolve(CType baseType, List<CType> stack) {
            return stack.Aggregate(baseType, (s, x) => {
                if (x is StubType) {
                    return s;
                } else {
                    x.Fixup(s);
                }
                return x;
            });
        }

        /// <summary>
        /// StubTypeをtyで置き換える。
        /// </summary>
        /// <param name="ty"></param>
        protected virtual void Fixup(CType ty) {
            throw new ApplicationException();
        }

        /// <summary>
        /// 型のサイズを取得
        /// </summary>
        /// <returns></returns>
        public abstract int Sizeof();

        /// <summary>
        /// 基本型
        /// </summary>
        /// <remarks>
        /// それぞれの宣言の宣言指定子列の中で，又はそれぞれの構造体宣言及び型名の型指定子型修飾子並びの中で，少なくとも一つの型指定子を指定しなければならない。型指定子の並びは，次に示すもののいずれか一つでなければならない.
        /// - void
        /// - char
        /// - signed char
        /// - unsigned char
        /// - short，signed short，short int，signed short int
        /// - unsigned short，unsigned short int
        /// - int，signed，signed int
        /// - unsigned，unsigned int
        /// - long，signed long，long int，signed long int
        /// - unsigned long，unsigned long int
        /// - long long，signed long long，long long int，signed long long int
        /// - unsigned long long，unsigned long long int
        /// - float
        /// - double
        /// - long double
        /// - _Bool
        /// - float _Complex
        /// - double _Complex
        /// - long double _Complex
        /// - float _Imaginary
        /// - double _Imaginary
        /// - long double _Imaginary
        /// - 構造体共用体指定子
        /// - 列挙型指定子
        /// - 型定義名
        /// </remarks>
        public class BasicType : CType {

            public enum Kind {
                KAndRImplicitInt,
                Void,
                Char,
                SignedChar,
                UnsignedChar,
                SignedShortInt,
                UnsignedShortInt,
                SignedInt,
                UnsignedInt,
                SignedLongInt,
                UnsignedLongInt,
                SignedLongLongInt,
                UnsignedLongLongInt,
                Float,
                Double,
                LongDouble,
                _Bool,
                Float_Complex,
                Double_Complex,
                LongDouble_Complex,
                Float_Imaginary,
                Double_Imaginary,
                LongDouble_Imaginary,

            }
            public Kind kind {
                get;
            }

            private static Kind ToKind(TypeSpecifier type_specifier) {
                switch (type_specifier) {
                    case TypeSpecifier.None:
                        return Kind.KAndRImplicitInt;
                    case TypeSpecifier.Void:
                        return Kind.Void;
                    case TypeSpecifier.Char:
                        return Kind.Char;
                    case TypeSpecifier.Signed | TypeSpecifier.Char:
                        return Kind.SignedChar;
                    case TypeSpecifier.Unsigned | TypeSpecifier.Char:
                        return Kind.UnsignedChar;
                    case TypeSpecifier.Short:
                    case TypeSpecifier.Signed | TypeSpecifier.Short:
                    case TypeSpecifier.Short | TypeSpecifier.Int:
                    case TypeSpecifier.Signed | TypeSpecifier.Short | TypeSpecifier.Int:
                        return Kind.SignedShortInt;
                    case TypeSpecifier.Unsigned | TypeSpecifier.Short:
                    case TypeSpecifier.Unsigned | TypeSpecifier.Short | TypeSpecifier.Int:
                        return Kind.UnsignedShortInt;
                    case TypeSpecifier.Int:
                    case TypeSpecifier.Signed:
                    case TypeSpecifier.Signed | TypeSpecifier.Int:
                        return Kind.SignedInt;
                    case TypeSpecifier.Unsigned:
                    case TypeSpecifier.Unsigned | TypeSpecifier.Int:
                        return Kind.UnsignedInt;
                    case TypeSpecifier.Long:
                    case TypeSpecifier.Signed | TypeSpecifier.Long:
                    case TypeSpecifier.Long | TypeSpecifier.Int:
                    case TypeSpecifier.Signed | TypeSpecifier.Long | TypeSpecifier.Int:
                        return Kind.SignedLongInt;
                    case TypeSpecifier.Unsigned | TypeSpecifier.Long:
                    case TypeSpecifier.Unsigned | TypeSpecifier.Long | TypeSpecifier.Int:
                        return Kind.UnsignedLongInt;
                    case TypeSpecifier.LLong:
                    case TypeSpecifier.Signed | TypeSpecifier.LLong:
                    case TypeSpecifier.LLong | TypeSpecifier.Int:
                    case TypeSpecifier.Signed | TypeSpecifier.LLong | TypeSpecifier.Int:
                        return Kind.SignedLongLongInt;
                    case TypeSpecifier.Unsigned | TypeSpecifier.LLong:
                    case TypeSpecifier.Unsigned | TypeSpecifier.LLong | TypeSpecifier.Int:
                        return Kind.UnsignedLongLongInt;
                    case TypeSpecifier.Float:
                        return Kind.Float;
                    case TypeSpecifier.Double:
                        return Kind.Double;
                    case TypeSpecifier.Long | TypeSpecifier.Double:
                        return Kind.LongDouble;
                    case TypeSpecifier._Bool:
                        return Kind._Bool;
                    case TypeSpecifier.Float | TypeSpecifier._Complex:
                        return Kind.Float_Complex;
                    case TypeSpecifier.Double | TypeSpecifier._Complex:
                        return Kind.Double_Complex;
                    case TypeSpecifier.Long | TypeSpecifier.Double | TypeSpecifier._Complex:
                        return Kind.LongDouble_Complex;
                    case TypeSpecifier.Float | TypeSpecifier._Imaginary:
                        return Kind.Float_Imaginary;
                    case TypeSpecifier.Double | TypeSpecifier._Imaginary:
                        return Kind.Double_Imaginary;
                    case TypeSpecifier.Long | TypeSpecifier.Double | TypeSpecifier._Imaginary:
                        return Kind.LongDouble_Imaginary;
                    default:
                        throw new Exception();
                }
            }

            public BasicType(TypeSpecifier type_specifier) : this(ToKind(type_specifier)) {
            }
            public BasicType(Kind kind) {
                this.kind = kind;
            }

            protected override void Fixup(CType ty) {
                // 基本型なのでFixup不要
            }

            /// <summary>
            /// サイズ取得
            /// </summary>
            /// <returns></returns>
            public override int Sizeof() {
                switch (kind) {
                    case Kind.KAndRImplicitInt:
                        return 4;
                    case Kind.Void:
                        throw new Exception();
                    case Kind.Char:
                        return 1;
                    case Kind.SignedChar:
                        return 1;
                    case Kind.UnsignedChar:
                        return 1;
                    case Kind.SignedShortInt:
                        return 2;
                    case Kind.UnsignedShortInt:
                        return 2;
                    case Kind.SignedInt:
                        return 4;
                    case Kind.UnsignedInt:
                        return 4;
                    case Kind.SignedLongInt:
                        return 4;
                    case Kind.UnsignedLongInt:
                        return 4;
                    case Kind.SignedLongLongInt:
                        return 4;
                    case Kind.UnsignedLongLongInt:
                        return 4;
                    case Kind.Float:
                        return 4;
                    case Kind.Double:
                        return 8;
                    case Kind.LongDouble:
                        return 12;
                    case Kind._Bool:
                        return 1;
                    case Kind.Float_Complex:
                        return 4 * 2;
                    case Kind.Double_Complex:
                        return 8 * 2;
                    case Kind.LongDouble_Complex:
                        return 12 * 2;
                    case Kind.Float_Imaginary:
                        return 4;
                    case Kind.Double_Imaginary:
                        return 8;
                    case Kind.LongDouble_Imaginary:
                        return 12;
                    default:
                        throw new Exception();
                }
            }

            public override string ToString() {
                switch (kind) {
                    case Kind.KAndRImplicitInt:
                        return "int";
                    case Kind.Void:
                        return "void";
                    case Kind.Char:
                        return "char";
                    case Kind.SignedChar:
                        return "signed char";
                    case Kind.UnsignedChar:
                        return "unsigned char";
                    case Kind.SignedShortInt:
                        return "signed short int";
                    case Kind.UnsignedShortInt:
                        return "unsigned short int";
                    case Kind.SignedInt:
                        return "signed int";
                    case Kind.UnsignedInt:
                        return "unsigned int";
                    case Kind.SignedLongInt:
                        return "signed long int";
                    case Kind.UnsignedLongInt:
                        return "unsigned long int";
                    case Kind.SignedLongLongInt:
                        return "signed long long int";
                    case Kind.UnsignedLongLongInt:
                        return "unsigned long long int";
                    case Kind.Float:
                        return "float";
                    case Kind.Double:
                        return "double";
                    case Kind.LongDouble:
                        return "long double";
                    case Kind._Bool:
                        return "_Bool";
                    case Kind.Float_Complex:
                        return "float _Complex";
                    case Kind.Double_Complex:
                        return "double _Complex";
                    case Kind.LongDouble_Complex:
                        return "long double _Complex";
                    case Kind.Float_Imaginary:
                        return "float _Imaginary";
                    case Kind.Double_Imaginary:
                        return "double _Imaginary";
                    case Kind.LongDouble_Imaginary:
                        return "long double _Imaginary";
                    default:
                        throw new Exception();
                }
            }


        }

        /// <summary>
        /// タグ付き型
        /// </summary>
        public abstract class TaggedType : CType {

            public string TagName {
                get;
            }
            public bool IsAnonymous {
                get;
            }

            protected TaggedType(string tagName, bool isAnonymous) {
                this.TagName = tagName;
                this.IsAnonymous = isAnonymous;
            }

            public class StructUnionType : TaggedType {
                public enum StructOrUnion {
                    Struct,
                    Union
                }
                public StructOrUnion Kind {
                    get;
                }

                public class MemberInfo {
                    public string Ident {
                        get;
                    }
                    public CType Type {
                        get;
                    }
                    public int BitSize {
                        get;
                    }

                    public MemberInfo(string ident, CType type, int? bitSize) {
                        // 制約
                        // - ビットフィールドの幅を指定する式は，整数定数式でなければならない。
                        //   - その値は，0 以上でなければならず，コロン及び式が省略された場合，指定された型のオブジェクトがもつビット数を超えてはならない。
                        //   - 値が 0 の場合，その宣言に宣言子があってはならない。
                        // - ビットフィールドの型は，修飾版又は非修飾版の_Bool，signed int，unsigned int 又は他の処理系定義の型でなければならない。
                        switch ((type.Unwrap() as CType.BasicType)?.kind) {
                            case BasicType.Kind._Bool:
                            case BasicType.Kind.SignedInt:
                            case BasicType.Kind.UnsignedInt:
                                break;
                            default:
                                throw new Exception("ビットフィールドの型は，修飾版又は非修飾版の_Bool，signed int，unsigned int 又は他の処理系定義の型でなければならない。");
                        }
                        if (bitSize.HasValue) {
                            if (bitSize.Value < 0) {
                                throw new Exception("ビットフィールドの幅の値は，0 以上でなければならない。");
                            } else if (bitSize.Value > type.Sizeof() * 8) {
                                throw new Exception("ビットフィールドの幅の値は，指定された型のオブジェクトがもつビット数を超えてはならない。");
                            } else if (bitSize.Value == 0) {
                                // 値が 0 の場合，その宣言に宣言子があってはならない。
                                // 宣言子がなく，コロン及び幅だけをもつビットフィールド宣言は，名前のないビットフィールドを示す。
                                // この特別な場合として，幅が 0 のビットフィールド構造体メンバは，前のビットフィールド（もしあれば）が割り付けられていた単位に，それ以上のビットフィールドを詰め込まないことを指定する。
                                if (ident != null) {
                                    throw new Exception("ビットフィールドの幅の値が 0 の場合，その宣言に宣言子(名前)があってはならない");
                                }
                            }
                        }
                        Ident = ident;
                        Type = type;
                        BitSize = bitSize.Value;
                    }
                }

                public List<MemberInfo> struct_declarations {
                    get; internal set;
                }

                public StructUnionType(StructOrUnion kind, string tagName, bool isAnonymous) : base(tagName, isAnonymous) {
                    this.Kind = kind;
                }

                protected override void Fixup(CType ty) {
                    for (var i = 0; i < struct_declarations.Count; i++) {
                        var struct_declaration = struct_declarations[i];
                        if (struct_declaration.Type is StubType) {
                            struct_declarations[i] = new MemberInfo(struct_declaration.Ident, ty, struct_declaration.BitSize);
                        } else {
                            struct_declaration.Type.Fixup(ty);
                        }
                    }

                    foreach (var struct_declaration in struct_declarations) {
                        struct_declaration.Type.Fixup(ty);
                    }
                }

                public override int Sizeof() {
                    // ビットフィールドは未実装
                    if (Kind == StructOrUnion.Struct) {
                        return struct_declarations.Sum(x => x.Type.Sizeof());
                    } else {
                        return struct_declarations.Max(x => x.Type.Sizeof());
                    }
                }
                public override string ToString() {
                    var sb = new List<string>();
                    sb.Add((Kind == StructOrUnion.Struct) ? "strunct" : "union");
                    sb.Add(TagName);
                    if (struct_declarations != null) {
                        sb.Add("{");
                        sb.AddRange(struct_declarations.SelectMany(x =>
                            new string[] { x.Type?.ToString(), x.Ident, x.BitSize > 0 ? $":{x.BitSize}" : null, ";" }.Where(y => y != null)
                        ));
                        sb.Add("}");
                    }
                    sb.Add(";");
                    return string.Join(" ", sb);
                }
            }

            /// <summary>
            /// 列挙型
            /// </summary>
            public class EnumType : TaggedType {

                /// <summary>
                /// 列挙型で宣言されている列挙定数
                /// </summary>
                /// <remarks>
                /// 6.4.4.3 列挙定数 
                /// 意味規則  
                ///   列挙定数として宣言された識別子は，型 int をもつ。
                /// </remarks>
                public class MemberInfo {
                    public string Name {
                        get;
                    }
                    public EnumType ParentType {
                        get;
                    }
                    public int Value {
                        get;
                    }
                    public MemberInfo(EnumType parentType, string name, int value) {
                        ParentType = ParentType;
                        Name = name;
                        Value = value;
                    }
                }


                public List<MemberInfo> enumerator_list {
                    get; set;
                }

                public EnumType(string tagName, bool isAnonymous) : base(tagName, isAnonymous) {
                }

                protected override void Fixup(CType ty) {
                }

                public override int Sizeof() {
                    return 4;
                }

                public override string ToString() {
                    var sb = new List<string>();
                    sb.Add("enum");
                    sb.Add(TagName);
                    if (enumerator_list != null) {
                        sb.Add("{");
                        sb.AddRange(enumerator_list.SelectMany(x =>
                            new string[] { x.Name, "=", x.Value.ToString(), "," }
                        ));
                        sb.Add("}");
                    }
                    sb.Add(";");
                    return string.Join(" ", sb);
                }
            }
        }

        /// <summary>
        /// スタブ型（型の解決中でのみ用いる他の型が入る穴）
        /// </summary>
        public class StubType : CType {
            public override int Sizeof() {
                throw new Exception();
            }
            public override string ToString() {
                return "$";
            }
        }

        /// <summary>
        /// 関数型
        /// </summary>
        public class FunctionType : CType {

            /// K&R書式で関数を定義した場合、仮引数の宣言で宣言した型に規定の実引数拡張を適用した結果が外から見える仮引数の型になる。
            /// 関数本体中では仮引数の型は宣言した型そのものが使われる。
            /// 例: int foo(f) float f { ... } の場合、int foo(double _t) { float f = (float)_t; ... } と読み替えられる。
            public class ArgumentInfo {
                public string Name {
                    get;
                }
                public StorageClass Sc {
                    get;
                }

                // 関数型として外から見える引数型
                public CType cType {
                    get;
                }

                // 実際に宣言された型(nullの場合はK&R形式で宣言されていない)
                public CType realType {
                    get;
                }


                public ArgumentInfo(string name, StorageClass sc, CType ctype, CType rType) {
                    Name = name;
                    Sc = sc;
                    cType = ctype;
                    realType = rType;
                }
            }

            /// <summary>
            /// 引数の情報
            /// nullの場合、int foo(); のように識別子並びが空であることを示す。
            /// 空リストの場合、int foo(void); のように唯一のvoidであることを示す。
            /// 一つ以上の要素を持つリストの場合、int foo(int, double); のように引数を持つことを示す。また、引数リストにvoid型が含まれない。
            /// </summary>
            public List<ArgumentInfo> Arguments {
                get;
            }

            /// <summary>
            /// 戻り値型
            /// </summary>
            public CType ResultType {
                get; private set;
            }

            /// <summary>
            /// 可変長引数の有無
            /// </summary>
            public bool HasVariadic {
                get;
            }

            public FunctionType(List<ArgumentInfo> arguments, bool hasVariadic, CType resultType) {
                // 6.7.5.3 関数宣言子（関数原型を含む）
                // 制約 
                // 関数宣言子は，関数型又は配列型を返却値の型として指定してはならない。(返却値の型が確定するFixupメソッド中で行う)
                // 仮引数宣言に記憶域クラス指定子として，register 以外のものを指定してはならない。
                // 関数定義の一部でない関数宣言子における識別子並びは，空でなければならない。(これは関数定義/宣言中で行う。)
                // 関数定義の一部である関数宣言子の仮引数型並びにある仮引数は，型調整後に不完全型をもってはならない。(これは関数定義/宣言中で行う。)
                // 

                // 仮引数宣言に記憶域クラス指定子として，register 以外のものを指定してはならない。
                if (arguments != null) {
                    foreach (var arg in arguments) {
                        if (arg.Sc != StorageClass.None && arg.Sc != StorageClass.Register) {
                            throw new Exception("仮引数宣言に記憶域クラス指定子として，register 以外のものを指定してはならない。");
                        }
                    }
                }

                this.Arguments = arguments;
                this.ResultType = resultType;
                this.HasVariadic = hasVariadic;
            }

            protected override void Fixup(CType ty) {
                if (ResultType is StubType) {
                    ResultType = ty;
                } else {
                    ResultType.Fixup(ty);
                }
                // 関数宣言子は，関数型又は配列型を返却値の型として指定してはならない。
                if (ResultType.IsFunctionType() || ResultType.IsArrayType()) {
                    throw new Exception("関数宣言子は，関数型又は配列型を返却値の型として指定してはならない。");
                }

            }

            public override int Sizeof() {
                throw new Exception();
            }

            public override string ToString() {
                var sb = new List<string>();
                sb.Add(ResultType.ToString());
                if (Arguments != null) {
                    sb.Add("(");
                    sb.Add(String.Join(", ", Arguments.Select(x => x.cType.ToString())));
                    if (HasVariadic) {
                        sb.Add(", ...");
                    }
                    sb.Add(")");
                }
                sb.Add(";");
                return string.Join(" ", sb);
            }
        }

        /// <summary>
        /// ポインタ型
        /// </summary>
        public class PointerType : CType {
            public CType cType {
                get; private set;
            }

            public PointerType(CType cType) {
                this.cType = cType;
            }

            protected override void Fixup(CType ty) {
                if (cType is StubType) {
                    cType = ty;
                } else {
                    cType.Fixup(ty);
                }
            }

            public override int Sizeof() {
                return 4;
            }

            public override string ToString() {
                return "*" + cType.ToString();
            }
        }

        /// <summary>
        /// 配列型
        /// </summary>
        public class ArrayType : CType {
            /// <summary>
            /// 配列長(-1は指定無し)
            /// </summary>
            public int Length {
                get;
            }

            public CType cType {
                get; private set;
            }

            public ArrayType(int length, CType cType) {
                this.Length = length;
                this.cType = cType;
            }

            protected override void Fixup(CType ty) {
                if (cType is StubType) {
                    cType = ty;
                } else {
                    cType.Fixup(ty);
                }
            }

            public override int Sizeof() {
                return (Length < 0) ? 4 : cType.Sizeof() * (Length);
            }

            public override string ToString() {
                return cType.ToString() + $"[{Length}]";
            }
        }

        /// <summary>
        /// Typedefされた型
        /// </summary>
        public class TypedefedType : CType {
            public string Ident {
                get;
            }
            public CType cType {
                get;
            }

            public TypedefedType(string ident, CType ctype) {
                this.Ident = ident;
                this.cType = ctype;
            }

            public override int Sizeof() {
                return this.cType.Sizeof();
            }

            public override string ToString() {
                return Ident;
            }
        }

        /// <summary>
        /// 型修飾子
        /// </summary>
        public class TypeQualifierType : CType {
            public CType cType {
                get; private set;
            }
            public TypeQualifier type_qualifier {
                get; set;
            }

            public TypeQualifierType(CType baseType, TypeQualifier type_qualifier) {
                this.cType = baseType;
                this.type_qualifier = type_qualifier;
            }

            protected override void Fixup(CType ty) {
                if (cType is StubType) {
                    cType = ty;
                } else {
                    cType.Fixup(ty);
                }
            }

            public override int Sizeof() {
                return this.cType.Sizeof();
            }

            public override string ToString() {
                var sb = new List<string>();
                sb.Add((type_qualifier & TypeQualifier.Const) != 0 ? "const" : null);
                sb.Add((type_qualifier & TypeQualifier.Volatile) != 0 ? "volatile" : null);
                sb.Add((type_qualifier & TypeQualifier.Restrict) != 0 ? "restrict" : null);
                sb.Add((type_qualifier & TypeQualifier.Near) != 0 ? "near" : null);
                sb.Add((type_qualifier & TypeQualifier.Far) != 0 ? "far" : null);
                sb.Add(cType.ToString());
                return string.Join(" ", sb.Where(x => x != null));
            }
        }

        /// <summary>
        /// 型が同一であるかどうかを比較する
        /// </summary>
        /// <param name="t1"></param>
        /// <param name="t2"></param>
        /// <returns></returns>
        public static bool IsEqual(CType t1, CType t2) {
            for (; ; ) {
                if (ReferenceEquals(t1, t2)) {
                    return true;
                }
                if (t1 is CType.TypedefedType || t2 is CType.TypedefedType) {
                    if (t1 is CType.TypedefedType) {
                        t1 = (t1 as CType.TypedefedType).cType;
                    }
                    if (t2 is CType.TypedefedType) {
                        t2 = (t2 as CType.TypedefedType).cType;
                    }
                    continue;
                }
                if ((t1 as CType.TypeQualifierType)?.type_qualifier == TypeQualifier.None) {
                    t1 = (t1 as CType.TypeQualifierType).cType;
                    continue;
                }
                if ((t2 as CType.TypeQualifierType)?.type_qualifier == TypeQualifier.None) {
                    t2 = (t2 as CType.TypeQualifierType).cType;
                    continue;
                }
                if (t1.GetType() != t2.GetType()) {
                    return false;
                }
                if (t1 is CType.TypeQualifierType && t2 is CType.TypeQualifierType) {
                    if ((t1 as CType.TypeQualifierType).type_qualifier != (t2 as CType.TypeQualifierType).type_qualifier) {
                        return false;
                    }
                    t1 = (t1 as CType.TypeQualifierType).cType;
                    t2 = (t2 as CType.TypeQualifierType).cType;
                    continue;
                }
                if (t1 is CType.PointerType && t2 is CType.PointerType) {
                    t1 = (t1 as CType.PointerType).cType;
                    t2 = (t2 as CType.PointerType).cType;
                    continue;
                }
                if (t1 is CType.ArrayType && t2 is CType.ArrayType) {
                    if ((t1 as CType.ArrayType).Length != (t2 as CType.ArrayType).Length) {
                        return false;
                    }
                    t1 = (t1 as CType.ArrayType).cType;
                    t2 = (t2 as CType.ArrayType).cType;
                    continue;
                }
                if (t1 is CType.FunctionType && t2 is CType.FunctionType) {
                    if ((t1 as CType.FunctionType).Arguments.Count != (t2 as CType.FunctionType).Arguments.Count) {
                        return false;
                    }
                    if ((t1 as CType.FunctionType).HasVariadic != (t2 as CType.FunctionType).HasVariadic) {
                        return false;
                    }
                    if ((t1 as CType.FunctionType).Arguments.Zip((t2 as CType.FunctionType).Arguments, (x, y) => IsEqual(x.cType, y.cType)).All(x => x) == false) {
                        return false;
                    }
                    t1 = (t1 as CType.FunctionType).ResultType;
                    t2 = (t2 as CType.FunctionType).ResultType;
                    continue;
                }
                if (t1 is CType.StubType && t2 is CType.StubType) {
                    throw new Exception();
                }
                if (t1 is CType.TaggedType.StructUnionType && t2 is CType.TaggedType.StructUnionType) {
                    if ((t1 as CType.TaggedType.StructUnionType).Kind != (t2 as CType.TaggedType.StructUnionType).Kind) {
                        return false;
                    }
                    if ((t1 as CType.TaggedType.StructUnionType).IsAnonymous != (t2 as CType.TaggedType.StructUnionType).IsAnonymous) {
                        return false;
                    }
                    if ((t1 as CType.TaggedType.StructUnionType).TagName != (t2 as CType.TaggedType.StructUnionType).TagName) {
                        return false;
                    }
                    if ((t1 as CType.TaggedType.StructUnionType).struct_declarations.Count != (t2 as CType.TaggedType.StructUnionType).struct_declarations.Count) {
                        return false;
                    }
                    if ((t1 as CType.TaggedType.StructUnionType).struct_declarations.Zip((t2 as CType.TaggedType.StructUnionType).struct_declarations, (x, y) => IsEqual(x.Type, y.Type)).All(x => x) == false) {
                        return false;
                    }
                    return true;
                }
                if (t1 is CType.BasicType && t2 is CType.BasicType) {
                    if ((t1 as CType.BasicType).kind != (t2 as CType.BasicType).kind) {
                        return false;
                    }
                    return true;
                }
                throw new Exception();
            }
        }
    }

    /// <summary>
    /// 規格書の用語に対応した定義の実装
    /// </summary>
    public static class Specification {
        /// <summary>
        /// 型別名と型修飾を無視した型を得る。
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        public static CType Unwrap(this CType self) {
            for (; ; ) {
                if (self is CType.TypedefedType) {
                    self = (self as CType.TypedefedType).cType;
                    continue;
                } else if (self is CType.TypeQualifierType) {
                    self = (self as CType.TypeQualifierType).cType;
                    continue;
                }
                break;
            }
            return self;
        }

        /// <summary>
        /// void型ならば真
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        public static bool IsVoidType(this CType self) {
            var unwrappedSelf = self.Unwrap();
            return (unwrappedSelf as CType.BasicType)?.kind == CType.BasicType.Kind.Void;
        }


        // 6.2.5 型
        // - オブジェクト型（object type）: オブジェクトを完全に規定する型（脚注：不完全型 (incomplete type) と関数型 (function type) 以外の サイズが確定している型）
        // - 関数型（function type）: 関数を規定する型
        // - 不完全型（incomplete type）: オブジェクトを規定する型で，その大きさを確定するのに必要な情報が欠けたもの
        //                                void型                                完全にすることのできない不完全型とする
        //                                大きさの分からない配列型              それ以降のその型の識別子の宣言（内部結合又は外部結合をもつ）で大きさを指定することによって，完全となる
        //                                内容の分からない構造体型又は共用体型  同じ有効範囲のそれ以降の同じ構造体タグ又は共用体タグの宣言で，内容を定義することによって，その型のすべての宣言に関し完全となる
        //                                
        //
        // - 標準符号付き整数型（standard signed integer type）: signed char，short int，int，long int 及び long long int の 5 種類
        // - 拡張符号付き整数型（extended signed integer type）: 処理系が独自に定義する標準符号付き整数型
        // - 符号付き整数型（signed integer type） : 標準符号付き整数型及び拡張符号付き整数型の総称
        // - 標準符号無し整数型（standard unsigned integer type）: 型_Bool，及び標準符号付き整数型に対応する符号無し整数型
        // - 拡張符号無し整数型（extended unsigned integer type）: 拡張符号付き整数型に対応する符号無し整数型
        // - 符号無し整数型（unsigned integer type）: 標準符号無し整数型及び拡張符号無し整数型の総称
        // - 標準整数型（standard integer type） : 標準符号付き整数型及び標準符号無し整数型の総称
        // - 拡張整数型（extended integer type） : 拡張符号付き整数型及び拡張符号無し整数型の総称
        //
        // - 実浮動小数点型（real floating type）: float，double 及び long doubleの 3 種類
        // - 複素数型（complex type） : float _Complex，double _Complex 及び long double _Complexの 3 種類
        // - 浮動小数点型（floating type） : 実浮動小数点型及び複素数型の総称
        // - 対応する実数型（corresponding real type） : 実浮動小数点型に対しては，同じ型を対応する実数型とする。複素数型に対しては，型名からキーワード_Complex を除いた型を，対応する実数型とする
        //
        // - 基本型（basic type） : 型 char，符号付き整数型，符号無し整数型及び浮動小数点型の総称
        // - 文字型（character type） : 三つの型 char，signed char 及び unsigned char の総称
        // - 列挙体（enumeration）: 名前付けられた整数定数値から成る。それぞれの列挙体は，異なる列挙型（enumerated type）を構成する
        // - 整数型（integer type） : 型 char，符号付き整数型，符号無し整数型，及び列挙型の総称
        // - 実数型（real type） : 整数型及び実浮動小数点型の総称
        // - 算術型（arithmetic type） : 整数型及び浮動小数点型の総称
        //
        // - 派生型（derived type）は，オブジェクト型，関数型及び不完全型から幾つでも構成することができる
        // - 派生型の種類は，次のとおり
        //   - 配列型（array type） : 要素型（element type）から派生
        //   - 構造体型（structure type） : メンバオブジェクトの空でない集合を順に割り付けたもの。各メンバは，名前を指定してもよく異なる型をもってもよい。
        //   - 共用体型（union type） : 重なり合って割り付けたメンバオブジェクトの空でない集合。各メンバは，名前を指定してもよく異なる型をもってもよい。
        //   - 関数型（function type） : 指定された返却値の型をもつ関数を表す。関数型は，その返却値の型，並びにその仮引数の個数及び型によって特徴付ける。
        //   - ポインタ型（pointer type）: 被参照型（referenced type）と呼ぶ関数型，オブジェクト型又は不完全型から派生することができる
        // - スカラ型（scalar type）: 算術型及びポインタ型の総称
        // - 集成体型（aggregate type） : 配列型及び構造体型の総称
        // - 派生宣言子型（derived declarator type） : 配列型，関数型及びポインタ型の総称
        // - 型分類（type category） : 型が派生型を含む場合，最も外側の派生とし，型が派生型を含まない場合，その型自身とする
        // - 非修飾型（unqualified type）: const，volatile，及びrestrict修飾子の一つ，二つ又は三つの組合せを持たない上記の型の総称。
        // - 修飾型（qualified type）: const，volatile，及びrestrict修飾子の一つ，二つ又は三つの組合せを持つ上記の型の総称。一つの型の修飾版と非修飾版は，同じ型分類，同じ表現及び同じ境界調整要求をもつが，異なる型とする
        //                             型修飾子をもつ型から派生したとしても，派生型はその型修飾子によって修飾されない。

        /// <summary>
        /// オブジェクト型（object type）ならば真
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        /// <remarks>
        /// オブジェクトを完全に規定する型（脚注：不完全型 (incomplete type) と関数型 (function type) 以外の サイズが確定している型）
        /// </remarks>
        public static bool IsObjectType(this CType self) {
            var unwrappedSelf = self.Unwrap();
            return !(unwrappedSelf.IsFunctionType() || unwrappedSelf.IsIncompleteType());
        }

        /// <summary>
        /// 関数型（function type）ならば真
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        /// <remarks>
        /// 関数を規定する型
        /// </remarks>
        public static bool IsFunctionType(this CType self) {
            var unwrappedSelf = self.Unwrap();
            return unwrappedSelf is CType.FunctionType;
        }

        /// <summary>
        /// 不完全型（incomplete type）ならば真
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        /// <remarks>
        /// オブジェクトを規定する型で，その大きさを確定するのに必要な情報が欠けたもの
        ///   void型                                完全にすることのできない不完全型とする
        ///   大きさの分からない配列型              それ以降のその型の識別子の宣言（内部結合又は外部結合をもつ）で大きさを指定することによって，完全となる
        ///   内容の分からない構造体型又は共用体型  同じ有効範囲のそれ以降の同じ構造体タグ又は共用体タグの宣言で，内容を定義することによって，その型のすべての宣言に関し完全となる
        /// </remarks>
        public static bool IsIncompleteType(this CType self) {
            var unwrappedSelf = self.Unwrap();
            // void型  
            if (unwrappedSelf is CType.BasicType) {
                var bt = unwrappedSelf as CType.BasicType;
                return bt.kind == CType.BasicType.Kind.Void;
            }
            // 大きさの分からない配列型
            if (unwrappedSelf is CType.ArrayType) {
                var at = unwrappedSelf as CType.ArrayType;
                if (at.Length == -1) {
                    return true;
                }
                return at.cType.IsIncompleteType();
            }
            // 内容の分からない構造体型又は共用体型
            if (unwrappedSelf is CType.TaggedType.StructUnionType) {
                var sut = unwrappedSelf as CType.TaggedType.StructUnionType;
                if (sut.struct_declarations == null) {
                    return true;
                }
                return sut.struct_declarations.Any(x => x.Type.IsIncompleteType());
            }
            return false;
        }

        /// <summary>
        /// 標準符号付き整数型（standard signed integer type）ならば真
        /// </summary>
        /// <returns></returns>
        public static bool IsStandardSignedIntegerType(this CType self) {
            var unwrappedSelf = self.Unwrap();
            if (unwrappedSelf is CType.BasicType) {
                var bt = unwrappedSelf as CType.BasicType;
                switch (bt.kind) {
                    case CType.BasicType.Kind.SignedChar:   // signed char
                    case CType.BasicType.Kind.SignedShortInt:    // short int
                    case CType.BasicType.Kind.SignedInt:    // int
                    case CType.BasicType.Kind.SignedLongInt:    // long int
                    case CType.BasicType.Kind.SignedLongLongInt:    // long long int
                        return true;
                    default:
                        return false;
                }
            }
            return false;
        }

        /// <summary>
        /// 拡張符号付き整数型（extended signed integer type）ならば真
        /// </summary>
        /// <returns></returns>
        public static bool IsExtendedSignedIntegerType(this CType self) {
            var unwrappedSelf = self.Unwrap();
            return false;
        }

        /// <summary>
        /// 符号付き整数型（signed integer type）ならば真
        /// </summary>
        /// <returns></returns>
        public static bool IsSignedIntegerType(this CType self) {
            var unwrappedSelf = self.Unwrap();
            return unwrappedSelf.IsStandardSignedIntegerType() || unwrappedSelf.IsExtendedSignedIntegerType();
        }

        /// <summary>
        /// 標準符号無し整数型（standard unsigned integer type）ならば真
        /// </summary>
        /// <returns></returns>
        public static bool IsStandardUnsignedIntegerType(this CType self) {
            var unwrappedSelf = self.Unwrap();
            if (unwrappedSelf is CType.BasicType) {
                var bt = unwrappedSelf as CType.BasicType;
                switch (bt.kind) {
                    case CType.BasicType.Kind.UnsignedChar:         // unsigned char
                    case CType.BasicType.Kind.UnsignedShortInt:     // unsigned short int
                    case CType.BasicType.Kind.UnsignedInt:          // unsigned int
                    case CType.BasicType.Kind.UnsignedLongInt:      // unsigned long int
                    case CType.BasicType.Kind.UnsignedLongLongInt:  // unsigned long long int
                        return true;
                    default:
                        return false;
                }
            }
            return false;
        }

        /// <summary>
        /// 拡張符号無し整数型（extended unsigned integer type）ならば真
        /// </summary>
        /// <returns></returns>
        public static bool IsExtendedUnsignedIntegerType(this CType self) {
            var unwrappedSelf = self.Unwrap();
            return false;
        }

        /// <summary>
        /// 符号無し整数型（unsigned integer type）ならば真
        /// </summary>
        /// <returns></returns>
        public static bool IsUnsignedIntegerType(this CType self) {
            var unwrappedSelf = self.Unwrap();
            return unwrappedSelf.IsStandardUnsignedIntegerType() || unwrappedSelf.IsExtendedUnsignedIntegerType();
        }

        /// <summary>
        /// 標準整数型（standard integer type）ならば真
        /// </summary>
        /// <returns></returns>
        public static bool IsStandardIntegerType(this CType self) {
            var unwrappedSelf = self.Unwrap();
            return unwrappedSelf.IsStandardSignedIntegerType() || unwrappedSelf.IsStandardUnsignedIntegerType();
        }

        /// <summary>
        /// 拡張整数型（extended integer type）ならば真
        /// </summary>
        /// <returns></returns>
        public static bool IsExtendedIntegerType(this CType self) {
            var unwrappedSelf = self.Unwrap();
            return unwrappedSelf.IsExtendedSignedIntegerType() || unwrappedSelf.IsExtendedUnsignedIntegerType();
        }

        /// <summary>
        /// 実浮動小数点型（real floating type）ならば真
        /// </summary>
        /// <returns></returns>
        public static bool IsRealFloatingType(this CType self) {
            var unwrappedSelf = self.Unwrap();
            if (unwrappedSelf is CType.BasicType) {
                var bt = unwrappedSelf as CType.BasicType;
                switch (bt.kind) {
                    case CType.BasicType.Kind.Float:                // float
                    case CType.BasicType.Kind.Double:               // double
                    case CType.BasicType.Kind.LongDouble:           // long double
                        return true;
                    default:
                        return false;
                }
            }
            return false;
        }

        /// <summary>
        /// 複素数型（complex type）ならば真
        /// </summary>
        /// <returns></returns>
        public static bool IsComplexType(this CType self) {
            var unwrappedSelf = self.Unwrap();
            if (unwrappedSelf is CType.BasicType) {
                var bt = unwrappedSelf as CType.BasicType;
                switch (bt.kind) {
                    case CType.BasicType.Kind.Float_Complex:                // float _Complex
                    case CType.BasicType.Kind.Double_Complex:               // double _Complex
                    case CType.BasicType.Kind.LongDouble_Complex:           // long double _Complex
                        return true;
                    default:
                        return false;
                }
            }
            return false;
        }

        /// <summary>
        /// 浮動小数点型（floating type）ならば真
        /// </summary>
        /// <returns></returns>
        public static bool IsFloatingType(this CType self) {
            var unwrappedSelf = self.Unwrap();
            return unwrappedSelf.IsRealFloatingType() || unwrappedSelf.IsComplexType();
        }

        /// <summary>
        /// 対応する実数型(corresponding real type)を取得
        /// </summary>
        /// <returns></returns>
        public static CType.BasicType GetCorrespondingRealType(this CType self) {
            var unwrappedSelf = self.Unwrap();
            if (unwrappedSelf.IsRealFloatingType()) {
                var bt = unwrappedSelf as CType.BasicType;
                return bt;
            } else if (unwrappedSelf.IsComplexType()) {
                var bt = unwrappedSelf as CType.BasicType;
                switch (bt.kind) {
                    case CType.BasicType.Kind.Float_Complex:
                        return new CType.BasicType(CType.BasicType.Kind.Float);
                    case CType.BasicType.Kind.Double_Complex:
                        return new CType.BasicType(CType.BasicType.Kind.Double);
                    case CType.BasicType.Kind.LongDouble_Complex:
                        return new CType.BasicType(CType.BasicType.Kind.LongDouble);
                    default:
                        throw new Exception();
                }
            } else {
                throw new Exception();
            }
        }


        /// <summary>
        /// 基本型（basic type）ならば真
        /// </summary>
        /// <returns></returns>
        public static bool IsBasicType(this CType self) {
            var unwrappedSelf = self.Unwrap();
            return unwrappedSelf.IsSignedIntegerType() || unwrappedSelf.IsUnsignedIntegerType() || unwrappedSelf.IsFloatingType() || ((unwrappedSelf as CType.BasicType)?.kind == CType.BasicType.Kind.Char);
        }


        /// <summary>
        /// 文字型（character type）ならば真
        /// </summary>
        /// <returns></returns>
        public static bool IsCharacterType(this CType self) {
            var unwrappedSelf = self.Unwrap();
            if (unwrappedSelf is CType.BasicType) {
                var bt = unwrappedSelf as CType.BasicType;
                switch (bt.kind) {
                    case CType.BasicType.Kind.Char:             // char
                    case CType.BasicType.Kind.SignedChar:       // signed char
                    case CType.BasicType.Kind.UnsignedChar:     // unsigned char
                        return true;
                    default:
                        return false;
                }
            }
            return false;
        }

        /// <summary>
        /// 列挙型（enumerated type）ならば真
        /// </summary>
        /// <returns></returns>
        public static bool IsEnumeratedType(this CType self) {
            var unwrappedSelf = self.Unwrap();
            return unwrappedSelf is CType.TaggedType.EnumType;
        }

        /// <summary>
        /// 整数型（integer type）ならば真
        /// </summary>
        /// <returns></returns>
        public static bool IsIntegerType(this CType self) {
            var unwrappedSelf = self.Unwrap();
            return unwrappedSelf.IsSignedIntegerType() || unwrappedSelf.IsUnsignedIntegerType() || unwrappedSelf.IsEnumeratedType() || ((unwrappedSelf as CType.BasicType)?.kind == CType.BasicType.Kind.Char);
        }

        /// <summary>
        /// 実数型（real type）ならば真
        /// </summary>
        /// <returns></returns>
        public static bool IsRealType(this CType self) {
            var unwrappedSelf = self.Unwrap();
            return unwrappedSelf.IsIntegerType() || unwrappedSelf.IsRealFloatingType();
        }

        /// <summary>
        /// 算術型（arithmetic type）ならば真
        /// </summary>
        /// <returns></returns>
        public static bool IsArithmeticType(this CType self) {
            var unwrappedSelf = self.Unwrap();
            return unwrappedSelf.IsIntegerType() || unwrappedSelf.IsFloatingType();
        }


        /// <summary>
        /// 配列型（array type）ならば真
        /// </summary>
        /// <returns></returns>
        public static bool IsArrayType(this CType self) {
            var unwrappedSelf = self.Unwrap();
            return unwrappedSelf is CType.ArrayType;
        }

        /// <summary>
        /// 配列型（array type）ならば真
        /// </summary>
        /// <param name="self"></param>
        /// <param name="elementType">配列型の場合、要素型が入る</param>
        /// <returns></returns>
        public static bool IsArrayType(this CType self, out CType elementType) {
            var unwrappedSelf = self.Unwrap();
            if (unwrappedSelf is CType.ArrayType) {
                elementType = (unwrappedSelf as CType.ArrayType).cType;
                return true;
            } else {
                elementType = null;
                return false;
            }
        }

        /// <summary>
        /// 構造体型（structure type）ならば真
        /// </summary>
        /// <returns></returns>
        public static bool IsStructureType(this CType self) {
            var unwrappedSelf = self.Unwrap();
            return (unwrappedSelf as CType.TaggedType.StructUnionType)?.Kind == CType.TaggedType.StructUnionType.StructOrUnion.Struct;
        }

        /// <summary>
        /// 共用体型（union type）ならば真
        /// </summary>
        /// <returns></returns>
        public static bool IsUnionType(this CType self) {
            var unwrappedSelf = self.Unwrap();
            return (unwrappedSelf as CType.TaggedType.StructUnionType)?.Kind == CType.TaggedType.StructUnionType.StructOrUnion.Union;
        }

        /// <summary>
        /// ポインタ型（pointer type）ならば真
        /// </summary>
        /// <returns></returns>
        public static bool IsPointerType(this CType self) {
            var unwrappedSelf = self.Unwrap();
            return unwrappedSelf is CType.PointerType;
        }

        /// <summary>
        /// ポインタ型（pointer type）ならば真
        /// </summary>
        /// <param name="self"></param>
        /// <param name="referencedType">ポインタ型の場合、被参照型が入る</param>
        /// <returns></returns>
        public static bool IsPointerType(this CType self, out CType referencedType) {
            var unwrappedSelf = self.Unwrap();
            if (unwrappedSelf is CType.PointerType) {
                referencedType = (unwrappedSelf as CType.PointerType).cType;
                return true;
            } else {
                referencedType = null;
                return false;
            }
        }

        /// <summary>
        /// 派生型（derived type）ならば真
        /// </summary>
        /// <returns></returns>
        public static bool IsDerivedType(this CType self) {
            var unwrappedSelf = self.Unwrap();
            return unwrappedSelf.IsArrayType()
                || unwrappedSelf.IsStructureType()
                || unwrappedSelf.IsUnionType()
                || unwrappedSelf.IsFunctionType()
                || unwrappedSelf.IsPointerType()
                ;
        }

        /// <summary>
        /// 被参照型（referenced type）ならば真
        /// </summary>
        /// <returns></returns>
        public static bool IsReferencedType(this CType self) {
            var unwrappedSelf = self.Unwrap();
            return unwrappedSelf.IsFunctionType()
                || unwrappedSelf.IsObjectType()
                || unwrappedSelf.IsIncompleteType()
                ;
        }

        /// <summary>
        /// スカラ型（scalar type）ならば真
        /// </summary>
        /// <returns></returns>
        public static bool IsScalarType(this CType self) {
            var unwrappedSelf = self.Unwrap();
            return unwrappedSelf.IsArithmeticType()
                || unwrappedSelf.IsPointerType()
                || unwrappedSelf.IsIncompleteType()
                ;
        }

        /// <summary>
        /// 集成体型（aggregate type）ならば真
        /// </summary>
        /// <returns></returns>
        public static bool IsAggregateType(this CType self) {
            var unwrappedSelf = self.Unwrap();
            return unwrappedSelf.IsStructureType()
                || unwrappedSelf.IsArrayType()
                ;
        }


        /// <summary>
        /// 派生宣言子型（derived declarator type）ならば真
        /// </summary>
        /// <returns></returns>
        public static bool IsDerivedDeclaratorType(this CType self) {
            var unwrappedSelf = self.Unwrap();
            return unwrappedSelf.IsFunctionType()
                || unwrappedSelf.IsArrayType()
                || unwrappedSelf.IsPointerType()
                ;
        }

        /// <summary>
        /// 非修飾型（unqualified type）ならば真
        /// </summary>
        /// <returns></returns>
        public static bool IsUnqualifiedType(this CType self) {
            return !(self is CType.TypeQualifierType);
        }

        /// <summary>
        /// 修飾型（qualified type）ならば真
        /// </summary>
        /// <returns></returns>
        public static bool IsQualifiedType(this CType self) {
            return self is CType.TypeQualifierType;
        }

        // 6.3 型変換
        // 暗黙の型変換（implicit conversion）
        // 明示的な型変換（explicit conversion）


        /// <summary>
        /// 整数変換の順位（integer conversion rank）
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        /// <remarks>
        /// 6.3.1.1 論理型，文字型及び整数型
        /// すべての整数型は，次のとおり定義される整数変換の順位（integer conversion rank）をもつ
        /// - 二つの符号付き整数型は，同じ表現をもつ場合であっても，同じ順位をもってはならない。
        /// - 符号付き整数型は，より小さい精度の符号付き整数型より高い順位をもたなければならない。
        /// - long long int 型は long int 型より高い順位をもたなければならない。
        ///   long int 型は int 型より高い順位をもたなければならない。
        ///   int 型は short int 型より高い順位をもたなければならない。
        ///   short int 型は signed char 型より高い順位をもたなければならない。
        /// - ある符号無し整数型に対し，対応する符号付き整数型があれば，両方の型は同じ順位をもたなければならない。
        /// - 標準整数型は，同じ幅の拡張整数型より高い順位をもたなければならない。
        /// - char 型は，signed char 型及び unsigned char 型と同じ順位をもたなければならない。
        /// - _Bool 型は，その他のすべての標準整数型より低い順位をもたなければならない。
        /// - すべての列挙型は，それぞれと適合する整数型と同じ順位をもたなければならない（6.7.2.2 参照）。
        /// - 精度の等しい拡張符号付き整数型同士の順位は処理系定義とするが，整数変換の順位を定める他の規則に従わなければならない。
        /// - 任意の整数型 T1，T2，及び T3 について，T1 が T2 より高い順位をもち，かつ T2 が T3 より高い順位をもつならば，T1 は T3 より高い順位をもたなければならない。
        /// </remarks>
        public static int IntegerConversionRank(this CType self) {
            var unwrappedSelf = self.Unwrap();
            if (unwrappedSelf.IsIntegerType()) {
                if (unwrappedSelf.IsEnumeratedType()) {
                    // すべての列挙型は，それぞれと適合する整数型と同じ順位をもたなければならない（6.7.2.2 参照）。
                    return -5;  // == signed int
                } else {
                    switch ((unwrappedSelf as CType.BasicType)?.kind) {
                        case CType.BasicType.Kind.SignedLongLongInt:
                        case CType.BasicType.Kind.UnsignedLongLongInt:
                            //long long = unsigned long long
                            //int64_t = uint64_t
                            return -1;
                        case CType.BasicType.Kind.SignedLongInt:
                        case CType.BasicType.Kind.UnsignedLongInt:
                            //long = unsigned long
                            return -3;
                        case CType.BasicType.Kind.SignedInt:
                        case CType.BasicType.Kind.UnsignedInt:
                            //int = unsigned int
                            //int32_t = uint32_t
                            return -5;
                        case CType.BasicType.Kind.SignedShortInt:
                        case CType.BasicType.Kind.UnsignedShortInt:
                            //short = unsigned short
                            //int16_t = uint16_t
                            return -7;
                        case CType.BasicType.Kind.Char:
                        case CType.BasicType.Kind.SignedChar:
                        case CType.BasicType.Kind.UnsignedChar:
                            //char = signed char = unsigned char
                            //int8_t = uint8_t
                            return -9;
                        case CType.BasicType.Kind._Bool:
                            // bool
                            return -11;
                        default:
                            return 0;
                    }
                }
            } else {
                return 0;
            }
        }


        /// <summary>
        ///  整数拡張（integer promotion）
        /// </summary>
        /// <param name="expr"></param>
        public static AST.Expression IntegerPromotion(AST.Expression expr, int? bitfield = null) {
            if (bitfield.HasValue == false) {
                // ビットフィールドではない
                // 整数変換の順位が int 型及び unsigned int 型より低い整数型をもつオブジェクト又は式?
                if (IntegerConversionRank(expr.Type) < -5) {
                    // 元の型のすべての値を int 型で表現可能な場合，その値を int 型に変換する。そうでない場合，unsigned int 型に変換する
                    if ((expr.Type.Unwrap() as CType.BasicType)?.kind == CType.BasicType.Kind.UnsignedInt) {
                        // unsigned int でないと表現できない
                        return new AST.Expression.PostfixExpression.IntegerPromotionExpression(new CType.BasicType(CType.BasicType.Kind.UnsignedInt), expr);
                    } else {
                        // signed int で表現できる
                        return new AST.Expression.PostfixExpression.IntegerPromotionExpression(new CType.BasicType(CType.BasicType.Kind.SignedInt), expr);
                    }
                } else {
                    // 拡張は不要
                    return expr;
                }
            } else {
                // ビットフィールドである
                switch ((expr.Type.Unwrap() as CType.BasicType)?.kind) {
                    // _Bool 型，int 型，signed int 型，又は unsigned int 型
                    case CType.BasicType.Kind._Bool:
                        // 処理系依存：sizeof(_Bool) == 1 としているため、無条件でint型に変換できる
                        return new AST.Expression.PostfixExpression.IntegerPromotionExpression(new CType.BasicType(CType.BasicType.Kind.SignedInt), expr);
                    case CType.BasicType.Kind.SignedInt:
                        // 無条件でint型に変換できる
                        return new AST.Expression.PostfixExpression.IntegerPromotionExpression(new CType.BasicType(CType.BasicType.Kind.SignedInt), expr);
                    case CType.BasicType.Kind.UnsignedInt:
                        // int 型で表現可能な場合，その値を int 型に変換する。そうでない場合，unsigned int 型に変換する
                        if (bitfield.Value == 4 * 8) {
                            // unsigned int でないと表現できない
                            return new AST.Expression.PostfixExpression.IntegerPromotionExpression(new CType.BasicType(CType.BasicType.Kind.UnsignedInt), expr);
                        } else {
                            // signed int で表現できる
                            return new AST.Expression.PostfixExpression.IntegerPromotionExpression(new CType.BasicType(CType.BasicType.Kind.SignedInt), expr);
                        }
                    default:
                        throw new Exception("ビットフィールドの型は，修飾版又は非修飾版の_Bool，signed int，unsigned int 又は他の処理系定義の型でなければならない。");
                }
            }
        }

        /// <summary>
        ///  規定の実引数拡張（default argument promotion）
        /// </summary>
        /// <param name="self"></param>
        public static CType DefaultArgumentPromotion(this CType self) {
            // 整数変換の順位が int 型及び unsigned int 型より低い整数型?
            if (IsIntegerType(self)) {
                if (IntegerConversionRank(self) < -5) {
                    // 元の型のすべての値を int 型で表現可能な場合，その値を int 型に変換する。そうでない場合，unsigned int 型に変換する
                    if ((self.Unwrap() as CType.BasicType)?.kind == CType.BasicType.Kind.UnsignedInt) {
                        // unsigned int に拡張
                        return new CType.BasicType(CType.BasicType.Kind.UnsignedInt);
                    } else {
                        // signed int に拡張
                        return new CType.BasicType(CType.BasicType.Kind.SignedInt);
                    }
                } else {
                    // 拡張は不要
                    return self;
                }
            } else if (IsRealFloatingType(self)) {
                if ((self.Unwrap() as CType.BasicType)?.kind == CType.BasicType.Kind.Float) {
                    // double に拡張
                    return new CType.BasicType(CType.BasicType.Kind.Double);
                } else {
                    // 拡張は不要
                    return self;
                }
            } else {
                // 拡張は不要
                return self;
            }
        }

        /// <summary>
        /// 通常の算術型変換（usual arithmetic conversion）
        /// </summary>
        /// <remarks>
        /// 6.3.1.8 通常の算術型変換
        /// 算術型のオペランドをもつ多くの演算子は，同じ方法でオペランドの型変換を行い，結果の型を決める。型変換は，オペランドと結果の共通の実数型（common real type）を決めるために行う。
        /// 与えられたオペランドに対し，それぞれのオペランドは，型領域を変えることなく，共通の実数型を対応する実数型とする型に変換する。
        /// この規格で明示的に異なる規定を行わない限り，結果の対応する実数型も，この共通の実数型とし，その型領域は，オペランドの型領域が一致していればその型領域とし，一致していなければ複素数型とする。
        /// これを通常の算術型変換（usual arithmetic conversion）と呼ぶ
        /// </remarks>
        public static CType UsualArithmeticConversion(ref AST.Expression lhs, ref AST.Expression rhs) {
            var tyLhs = lhs.Type.Unwrap();
            var tyRhs = rhs.Type.Unwrap();

            var btLhs = tyLhs as CType.BasicType;
            var btRhs = tyRhs as CType.BasicType;

            if (btLhs == null || btRhs == null) {
                throw new Exception();
            }
            if (btLhs.kind == btRhs.kind) {
                return btLhs;
            }

            // まず，一方のオペランドの対応する実数型が long double ならば，他方のオペランドを，型領域を変えることなく，変換後の型に対応する実数型が long double となるように型変換する。
            // そうでない場合，一方のオペランドの対応する実数型が double ならば，他方のオペランドを，型領域を変えることなく，変換後の型に対応する実数型が double となるように型変換する。
            // そうでない場合，一方のオペランドの対応する実数型が float ならば，他方のオペランドを，型領域を変えることなく，変換後の型に対応する実数型が float となるように型変換する。
            // 例：
            //  - 一方が long double で 他方が double なら double を long double にする。
            //  - 一方が long double で 他方が float _Complex なら float _Complex を long double _Complex にする。（結果の型は long double _Complex 型になる）
            //  - 一方が long double _Complex で 他方が float なら float を long double にする。（結果の型は long double _Complex 型になる）
            var realConversionPairTable = new[] {
                Tuple.Create(CType.BasicType.Kind.LongDouble,CType.BasicType.Kind.LongDouble_Complex),
                Tuple.Create(CType.BasicType.Kind.Double,CType.BasicType.Kind.Double_Complex),
                Tuple.Create(CType.BasicType.Kind.Float,CType.BasicType.Kind.Float_Complex)
            };

            foreach (var realConversionPair in realConversionPairTable) {
                if (btLhs.IsFloatingType() && btLhs.GetCorrespondingRealType().kind == realConversionPair.Item1) {
                    if (btRhs.IsComplexType()) {
                        var retTy = new CType.BasicType(realConversionPair.Item2);
                        rhs = new AST.Expression.PostfixExpression.CastExpression(retTy, rhs);
                        return retTy;
                    } else {
                        rhs = new AST.Expression.PostfixExpression.CastExpression(new CType.BasicType(realConversionPair.Item1), rhs);
                        return btLhs;
                    }
                } else if (btRhs.IsFloatingType() && btRhs.GetCorrespondingRealType().kind == realConversionPair.Item1) {
                    if (btLhs.IsComplexType()) {
                        var retTy = new CType.BasicType(realConversionPair.Item2);
                        lhs = new AST.Expression.PostfixExpression.CastExpression(retTy, lhs);
                        return retTy;
                    } else {
                        lhs = new AST.Expression.PostfixExpression.CastExpression(new CType.BasicType(realConversionPair.Item1), lhs);
                        return btRhs;
                    }
                }
            }

            // そうでない場合，整数拡張を両オペランドに対して行い，拡張後のオペランドに次の規則を適用する。
            lhs = IntegerPromotion(lhs);
            rhs = IntegerPromotion(rhs);

            tyLhs = lhs.Type.Unwrap();
            tyRhs = rhs.Type.Unwrap();

            btLhs = tyLhs as CType.BasicType;
            btRhs = tyRhs as CType.BasicType;

            if (btLhs == null || btRhs == null) {
                throw new Exception();
            }
            // 両方のオペランドが同じ型をもつ場合，更なる型変換は行わない。
            if (btLhs.kind == btRhs.kind) {
                return btLhs;
            }

            // そうでない場合，両方のオペランドが符号付き整数型をもつ，又は両方のオペランドが符号無し整数型をもつならば，
            // 整数変換順位の低い方の型を，高い方の型に変換する。
            if ((btLhs.IsSignedIntegerType() && btRhs.IsSignedIntegerType()) || (btLhs.IsUnsignedIntegerType() && btRhs.IsUnsignedIntegerType())) {
                if (btLhs.IntegerConversionRank() < btRhs.IntegerConversionRank()) {
                    lhs = new AST.Expression.PostfixExpression.CastExpression(btRhs, lhs);
                    return btRhs;
                } else {
                    rhs = new AST.Expression.PostfixExpression.CastExpression(btLhs, rhs);
                    return btLhs;
                }
            }

            // ここに到達した時点で、一方が符号無し、一方が符号付きであることが保障される

            // そうでない場合，符号無し整数型をもつオペランドが，他方のオペランドの整数変換順位より高い又は等しい順位をもつならば，
            // 符号付き整数型をもつオペランドを，符号無し整数型をもつオペランドの型に変換する。
            if (btLhs.IsUnsignedIntegerType() && btLhs.IntegerConversionRank() >= btRhs.IntegerConversionRank()) {
                rhs = new AST.Expression.PostfixExpression.CastExpression(btLhs, rhs);
                return btLhs;
            } else if (btRhs.IsUnsignedIntegerType() && btRhs.IntegerConversionRank() >= btLhs.IntegerConversionRank()) {
                lhs = new AST.Expression.PostfixExpression.CastExpression(btRhs, lhs);
                return btRhs;
            }

            // ここに到達した時点で、符号有りオペランドのほうが符号無しオペランドよりも大きい整数変換順位を持つことが保障される
            // 整数変換順位の大きさと型の表現サイズの大きさは環境によっては一致しない
            // 例：int が 2byte (signed int = signed short) 、char が 16bit以上など

            // そうでない場合，符号付き整数型をもつオペランドの型が，符号無し整数型をもつオペランドの型のすべての値を表現できるならば，
            // 符号無し整数型をもつオペランドを，符号付き整数型をもつオペランドの型に変換する。
            if (btLhs.IsSignedIntegerType() && btRhs.IsUnsignedIntegerType() && btLhs.Sizeof() > btRhs.Sizeof()) {
                rhs = new AST.Expression.PostfixExpression.CastExpression(btLhs, rhs);
                return btLhs;
            } else if (btRhs.IsSignedIntegerType() && btLhs.IsUnsignedIntegerType() && btRhs.Sizeof() > btLhs.Sizeof()) {
                lhs = new AST.Expression.PostfixExpression.CastExpression(btRhs, lhs);
                return btRhs;
            }

            // そうでない場合，両方のオペランドを，符号付き整数型をもつオペランドの型に対応する符号無し整数型に変換する。
            CType.BasicType.Kind tySignedKind = ((btLhs.IsSignedIntegerType()) ? btLhs : btRhs).kind;
            CType.BasicType.Kind tyUnsignedKind;
            switch (tySignedKind) {
                case CType.BasicType.Kind.SignedInt:
                    tyUnsignedKind = CType.BasicType.Kind.UnsignedInt;
                    break;
                case CType.BasicType.Kind.SignedLongInt:
                    tyUnsignedKind = CType.BasicType.Kind.UnsignedLongInt;
                    break;
                case CType.BasicType.Kind.SignedLongLongInt:
                    tyUnsignedKind = CType.BasicType.Kind.UnsignedLongLongInt;
                    break;
                default:
                    throw new Exception("整数拡張が正しく行われていない？");
            }

            var tyUnsigned = new CType.BasicType(tyUnsignedKind);
            lhs = new AST.Expression.PostfixExpression.CastExpression(tyUnsigned, lhs);
            rhs = new AST.Expression.PostfixExpression.CastExpression(tyUnsigned, rhs);
            return tyUnsigned;

        }
    }

    /// <summary>
    /// 構文木
    /// </summary>
    public abstract class AST {
        public static int ConstantEval(AST.Expression expr) {
            if (expr is AST.Expression.PostfixExpression.AdditiveExpression) {
                var e = expr as AST.Expression.PostfixExpression.AdditiveExpression;
                var lhs = ConstantEval(e.Lhs);
                var rhs = ConstantEval(e.Rhs);
                switch (e.Op) {
                    case "+":
                        return lhs + rhs;
                    case "-":
                        return lhs - rhs;
                    default:
                        throw new Exception();
                }
            }
            if (expr is AST.Expression.PostfixExpression.AndExpression) {
                var e = expr as AST.Expression.PostfixExpression.AndExpression;
                var lhs = ConstantEval(e.Lhs);
                if (lhs != 0) {
                    return ConstantEval(e.Rhs) == 0 ? 0 : 1;
                } else {
                    return 0;
                }
            }
            if (expr is AST.Expression.PostfixExpression.ArraySubscriptingExpression) {
                var e = expr as AST.Expression.PostfixExpression.ArraySubscriptingExpression;
                throw new Exception();
            }
            if (expr is AST.Expression.PostfixExpression.AssignmentExpression) {
                var e = expr as AST.Expression.PostfixExpression.AssignmentExpression;
                throw new Exception();
            }
            if (expr is AST.Expression.PostfixExpression.CastExpression) {
                var e = expr as AST.Expression.PostfixExpression.CastExpression;
                // キャストは未実装
                return ConstantEval(e.Expr);
            }
            if (expr is AST.Expression.PrimaryExpression.ConstantExpression.CharacterConstant) {
                var e = expr as AST.Expression.PrimaryExpression.ConstantExpression.CharacterConstant;
                return (int)e.Str[1];
            }
            if (expr is AST.Expression.PostfixExpression.CommaExpression) {
                var e = expr as AST.Expression.PostfixExpression.CommaExpression;
                throw new Exception();
            }
            if (expr is AST.Expression.PostfixExpression.ConditionalExpression) {
                var e = expr as AST.Expression.PostfixExpression.ConditionalExpression;
                var cond = ConstantEval(e.Cond);
                if (cond != 0) {
                    return ConstantEval(e.ThenExpr);
                } else {
                    return ConstantEval(e.ElseExpr);
                }
            }
            if (expr is AST.Expression.PrimaryExpression.IdentifierExpression.EnumerationConstant) {
                var e = expr as AST.Expression.PrimaryExpression.IdentifierExpression.EnumerationConstant;
                return e.Ret.Value;
            }
            if (expr is AST.Expression.PostfixExpression.EqualityExpression) {
                var e = expr as AST.Expression.PostfixExpression.EqualityExpression;
                var lhs = ConstantEval(e.Lhs);
                var rhs = ConstantEval(e.Rhs);
                switch (e.Op) {
                    case "==":
                        return lhs == rhs ? 1 : 0;
                    case "!=":
                        return lhs != rhs ? 1 : 0;
                    default:
                        throw new Exception();
                }
            }
            if (expr is AST.Expression.ExclusiveOrExpression) {
                var e = expr as AST.Expression.ExclusiveOrExpression;
                var lhs = ConstantEval(e.Lhs);
                var rhs = ConstantEval(e.Rhs);
                return lhs ^ rhs;
            }
            if (expr is AST.Expression.PrimaryExpression.ConstantExpression.FloatingConstant) {
                var e = expr as AST.Expression.PrimaryExpression.ConstantExpression.FloatingConstant;
                // 未実装
                throw new Exception();
            }
            if (expr is AST.Expression.PostfixExpression.FunctionCallExpression) {
                var e = expr as AST.Expression.PostfixExpression.FunctionCallExpression;
                throw new Exception();
            }
            if (expr is AST.Expression.PrimaryExpression.IdentifierExpression.FunctionExpression) {
                var e = expr as AST.Expression.PrimaryExpression.IdentifierExpression.FunctionExpression;
                throw new Exception();
            }
            if (expr is AST.Expression.PrimaryExpression.IdentifierExpression.UndefinedIdentifierExpression) {
                var e = expr as AST.Expression.PrimaryExpression.IdentifierExpression.UndefinedIdentifierExpression;
                throw new Exception();
            }
            if (expr is AST.Expression.InclusiveOrExpression) {
                var e = expr as AST.Expression.InclusiveOrExpression;
                var lhs = ConstantEval(e.Lhs);
                var rhs = ConstantEval(e.Rhs);
                return lhs | rhs;
            }
            if (expr is AST.Expression.PrimaryExpression.ConstantExpression.IntegerConstant) {
                var e = expr as AST.Expression.PrimaryExpression.ConstantExpression.IntegerConstant;
                return (int)e.Value;
            }
            if (expr is AST.Expression.LogicalAndExpression) {
                var e = expr as AST.Expression.LogicalAndExpression;
                var lhs = ConstantEval(e.Lhs);
                if (lhs != 0) {
                    return ConstantEval(e.Rhs) == 0 ? 0 : 1;
                } else {
                    return 1;
                }
            }
            if (expr is AST.Expression.LogicalOrExpression) {
                var e = expr as AST.Expression.LogicalOrExpression;
                var lhs = ConstantEval(e.Lhs);
                if (lhs == 0) {
                    return ConstantEval(e.Rhs) == 0 ? 0 : 1;
                } else {
                    return 1;
                }
            }
            if (expr is AST.Expression.PostfixExpression.MemberDirectAccess) {
                var e = expr as AST.Expression.PostfixExpression.MemberDirectAccess;
                throw new Exception();
            }
            if (expr is AST.Expression.PostfixExpression.MemberIndirectAccess) {
                var e = expr as AST.Expression.PostfixExpression.MemberIndirectAccess;
                throw new Exception();
            }
            if (expr is AST.Expression.MultiplicitiveExpression) {
                var e = expr as AST.Expression.MultiplicitiveExpression;
                var lhs = ConstantEval(e.Lhs);
                var rhs = ConstantEval(e.Rhs);
                switch (e.Op) {
                    case "*":
                        return lhs * rhs;
                    case "/":
                        return lhs / rhs;
                    case "%":
                        return lhs % rhs;
                    default:
                        throw new Exception();
                }
            }
            if (expr is AST.Expression.RelationalExpression) {
                var e = expr as AST.Expression.RelationalExpression;
                var lhs = ConstantEval(e.Lhs);
                var rhs = ConstantEval(e.Rhs);
                switch (e.Op) {
                    case "<":
                        return lhs < rhs ? 1 : 0;
                    case ">":
                        return lhs > rhs ? 1 : 0;
                    case "<=":
                        return lhs <= rhs ? 1 : 0;
                    case ">=":
                        return lhs >= rhs ? 1 : 0;
                    default:
                        throw new Exception();
                }
            }
            if (expr is AST.Expression.ShiftExpression) {
                var e = expr as AST.Expression.ShiftExpression;
                var lhs = ConstantEval(e.Lhs);
                var rhs = ConstantEval(e.Rhs);
                switch (e.Op) {
                    case "<<":
                        return lhs << rhs;
                    case ">>":
                        return lhs >> rhs;
                    default:
                        throw new Exception();
                }
            }
            if (expr is AST.Expression.SizeofExpression) {
                var e = expr as AST.Expression.SizeofExpression;
                // 未実装につきintサイズ固定
                return 4;
            }
            if (expr is AST.Expression.SizeofTypeExpression) {
                var e = expr as AST.Expression.SizeofTypeExpression;
                return e.Ty.Sizeof();
            }
            if (expr is AST.Expression.PrimaryExpression.StringExpression) {
                var e = expr as AST.Expression.PrimaryExpression.StringExpression;
                throw new Exception();
            }
            if (expr is AST.Expression.UnaryAddressExpression) {
                var e = expr as AST.Expression.UnaryAddressExpression;
                throw new Exception();
            }
            if (expr is AST.Expression.UnaryMinusExpression) {
                var e = expr as AST.Expression.UnaryMinusExpression;
                return -ConstantEval(e.Expr);
            }
            if (expr is AST.Expression.UnaryNegateExpression) {
                var e = expr as AST.Expression.UnaryNegateExpression;
                return ~ConstantEval(e.Expr);
            }
            if (expr is AST.Expression.UnaryNotExpression) {
                var e = expr as AST.Expression.UnaryNotExpression;
                return ConstantEval(e.Expr) == 0 ? 0 : 1;
            }
            if (expr is AST.Expression.UnaryPlusExpression) {
                var e = expr as AST.Expression.UnaryPlusExpression;
                return ConstantEval(e.Expr);
            }
            if (expr is AST.Expression.PostfixExpression.UnaryPostfixExpression) {
                var e = expr as AST.Expression.PostfixExpression.UnaryPostfixExpression;
                throw new Exception();
            }
            if (expr is AST.Expression.UnaryPrefixExpression) {
                var e = expr as AST.Expression.UnaryPrefixExpression;
                throw new Exception();
            }
            if (expr is AST.Expression.UnaryReferenceExpression) {
                var e = expr as AST.Expression.UnaryReferenceExpression;
                throw new Exception();
            }
            if (expr is AST.Expression.PrimaryExpression.IdentifierExpression.VariableExpression) {
                var e = expr as AST.Expression.PrimaryExpression.IdentifierExpression.VariableExpression;
                throw new Exception();
            }
            if (expr is AST.Expression.PrimaryExpression.EnclosedInParenthesesExpression) {
                var e = expr as AST.Expression.PrimaryExpression.EnclosedInParenthesesExpression;
                return ConstantEval(e.expression);
            }
            throw new Exception();
        }

        /// <summary>
        /// 6.5 式 
        ///   - 式（expression）は，演算子及びオペランドの列とする。式は，値の計算を指定するか，オブジェクト若しくは関数を指し示すか，副作用を引き起こすか，又はそれらの組合せを行う。
        ///   - 直前の副作用完了点から次の副作用完了点までの間に，式の評価によって一つのオブジェクトに格納された値を変更する回数は，高々 1 回でなければならない。
        ///     さらに，変更前の値の読取りは，格納される値を決定するためだけに行われなければならない。
        ///   - （関数呼出しの()，&&，||，?:及びコンマ演算子に対して）後で規定する場合を除いて，部分式の評価順序及び副作用が生じる順序は未規定とする。
        ///   - 幾つかの演算子［総称してビット単位の演算子（bitwise operator）と呼ぶ単項演算子~並びに 2 項演算子<<，>>，&，^及び|］は，整数型のオペランドを必要とする。
        ///     これらの演算子は，整数の内部表現に依存した値を返すので，符号付き整数型に対して処理系定義又は未定義の側面をもつ。
        ///   - 式の評価中に例外条件（exceptional condition）が発生した場合（すなわち，結果が数学的に定義できないか，又は結果の型で表現可能な値の範囲にない場合），その動作は未定義とする。
        ///   - 格納された値にアクセスするときのオブジェクトの有効型（effective type）は，（もしあれば）そのオブジェクトの宣言された型とする。
        ///     宣言された型をもたないオブジェクトへ，文字型以外の型をもつ左辺値を通じて値を格納した場合，左辺値の型をそのアクセス及び格納された値を変更しないそれ以降のアクセスでのオブジェクトの有効型とする。
        ///     宣言された型をもたないオブジェクトに，memcpy 関数若しくは memmove関数を用いて値をコピーするか，又は文字型の配列として値をコピーした場合，
        ///     そのアクセス及び値を変更しないそれ以降のアクセスでのオブジェクトの有効型は，値のコピー元となったオブジェクトの有効型があれば，その型とする。
        ///     宣言された型をもたないオブジェクトに対するその他のすべてのアクセスでは，そのアクセスでの左辺値の型を有効型とする。
        ///   - オブジェクトに格納された値に対するアクセスは，次のうちのいずれか一つの型をもつ左辺値によらなければならない
        ///     - オブジェクトの有効型と適合する型
        ///     - オブジェクトの有効型と適合する型の修飾版
        ///     - オブジェクトの有効型に対応する符号付き型又は符号無し型
        ///     - オブジェクトの有効型の修飾版に対応する符号付き型又は符号無し型
        ///     - メンバの中に上に列挙した型の一つを含む集成体型又は共用体型（再帰的に包含されている部分集成体又は含まれる共用体のメンバを含む。）
        ///     - 文字型
        ///   - 浮動小数点型の式は短縮（contract）してもよい。すなわち，ハードウェアによる不可分な操作として評価して，ソースコードの記述及び式の評価方法どおりなら生じるはずの丸め誤差を省いてもよい
        ///     <math.h>の FP_CONTRACT プラグマは，式の短縮を禁止する方法を提供する。FP_CONTRACT プラグマがない場合，式が短縮されるかどうか，及びそれをどのように短縮するかは処理系定義とする。
        /// </summary>
        public abstract class Expression : AST {

            /// <summary>
            /// 式の結果の型
            /// </summary>
            public abstract CType Type {
                get;
            }

            /// <summary>
            /// 式の結果が左辺値と成りうるか
            /// </summary>
            public virtual bool IsLValue() {
                return false;
            }

            /// <summary>
            /// 6.5.1 一次式
            /// </summary>
            public abstract class PrimaryExpression : Expression {

                /// <summary>
                /// 識別子式
                /// </summary>
                /// <remarks>
                /// 識別子がオブジェクト（この場合，識別子は左辺値となる。），又は関数（この場合，関数指示子となる。）を指し示すと宣言されている場合，識別子は一次式とする。
                /// 宣言されていない識別子は構文規則違反である。（脚注：C89以降では宣言されていない識別子は構文規則違反であるとなっているが、K&Rでは未定義識別子が許されちゃってるので文脈から変数/関数を判断する必要がある。）
                /// </remarks>
                public abstract class IdentifierExpression : PrimaryExpression {

                    public string Ident {
                        get;
                    }
                    protected IdentifierExpression(string ident) {
                        Ident = ident;
                    }


                    /// <summary>
                    /// 未定義識別子式
                    /// </summary>
                    public class UndefinedIdentifierExpression : IdentifierExpression {

                        public override CType Type {
                            get {
                                throw new NotImplementedException();
                            }
                        }

                        public UndefinedIdentifierExpression(string ident) : base(ident) {
                        }

                        public override bool IsLValue() {
                            throw new NotImplementedException();
                        }

                    }

                    public class VariableExpression : IdentifierExpression {
                        public Declaration.VariableDeclaration variableDeclaration {
                            get;
                        }
                        public override CType Type {
                            get {
                                return variableDeclaration.Ctype;
                            }
                        }
                        public override bool IsLValue() {
                            // 6.5.1 一次式
                            // 識別子がオブジェクト（この場合，識別子は左辺値となる。）
                            return true;
                        }

                        public VariableExpression(string ident, Declaration.VariableDeclaration variableDeclaration) : base(ident) {
                            this.variableDeclaration = variableDeclaration;
                        }
                    }

                    public class FunctionExpression : IdentifierExpression {
                        public Declaration.FunctionDeclaration functionDeclaration {
                            get;
                        }
                        public override CType Type {
                            get {
                                return functionDeclaration.Ty;
                            }
                        }

                        public FunctionExpression(string ident, Declaration.FunctionDeclaration functionDeclaration) : base(ident) {
                            this.functionDeclaration = functionDeclaration;
                        }
                    }

                    /// <summary>
                    /// 列挙定数式
                    /// </summary>
                    public class EnumerationConstant : IdentifierExpression {
                        public CType.TaggedType.EnumType.MemberInfo Ret {
                            get;
                        }
                        public override CType Type {
                            get {
                                return new CType.BasicType(CType.BasicType.Kind.SignedInt);
                            }
                        }

                        public EnumerationConstant(CType.TaggedType.EnumType.MemberInfo ret) : base(ret.Name) {
                        }
                    }
                }

                /// <summary>
                /// 定数式
                /// </summary>
                /// <remarks>
                /// 定数は，一次式とする。その型は，その形式と値によって決まる（6.4.4 で規定する。）
                /// </remarks>
                public abstract class ConstantExpression : Expression {
                    /// <summary>
                    /// 整数定数式
                    /// </summary>
                    public class IntegerConstant : ConstantExpression {

                        public string Str {
                            get;
                        }
                        public long Value {
                            get;
                        }
                        private CType _type {
                            get;
                        }
                        public override CType Type {
                            get {
                                return _type;
                            }
                        }

                        public IntegerConstant(string str, long value, CType.BasicType.Kind kind) {
                            this.Str = str;
                            this.Value = value;
                            this._type = new CType.BasicType(kind);
                        }
                    }

                    /// <summary>
                    /// 文字定数式
                    /// </summary>
                    public class CharacterConstant : ConstantExpression {
                        public string Str {
                            get;
                        }
                        public override CType Type {
                            get {
                                return new CType.BasicType(CType.BasicType.Kind.Char);
                            }
                        }


                        public CharacterConstant(string str) {
                            Str = str;
                        }

                    }

                    /// <summary>
                    /// 浮動小数点定数式
                    /// </summary>
                    public class FloatingConstant : ConstantExpression {
                        private CType.BasicType.Kind _type {
                            get;
                        }

                        public string Str {
                            get;
                        }

                        public double Value {
                            get;
                        }
                        public override CType Type {
                            get {
                                return new CType.BasicType(_type);
                            }
                        }

                        public FloatingConstant(string str, double value, CType.BasicType.Kind kind) {
                            Str = str;
                            Value = value;
                            this._type = kind;
                        }
                    }
                }

                /// <summary>
                /// 文字列リテラル式
                /// </summary>
                /// <remarks>
                /// 文字列リテラルは，一次式とする。それは，6.4.5 の規則で決まる型をもつ左辺値とする。
                /// </remarks>
                public class StringExpression : PrimaryExpression {
                    public List<string> Strings {
                        get;
                    }
                    public override CType Type {
                        get {
                            return new CType.BasicType.ArrayType(String.Concat(Strings).Length, new CType.BasicType(CType.BasicType.Kind.Char));
                        }
                    }
                    public override bool IsLValue() {
                        // 6.5.1 一次式
                        // 文字列リテラルは，一次式とする。それは，6.4.5 の規則で決まる型をもつ左辺値とする。
                        return true;
                    }

                    public StringExpression(List<string> strings) {
                        Strings = strings;
                        // Todo: WideChar未対応
                    }
                }

                /// <summary>
                /// 括弧で囲まれた式
                /// </summary>
                /// <remarks>
                /// 括弧で囲まれた式は，一次式とする。その型及び値は，括弧の中の式のそれらと同じとする。
                /// 括弧の中の式が左辺値，関数指示子又はボイド式である場合，それは，それぞれ左辺値，関数指示子又はボイド式とする。
                /// </remarks>
                public class EnclosedInParenthesesExpression : PrimaryExpression {
                    public AST.Expression expression {
                        get;
                    }
                    public override CType Type {
                        get {
                            return expression.Type;
                        }
                    }
                    public override bool IsLValue() {
                        // 6.5.1 一次式
                        // 括弧の中の式が左辺値である場合，それは，左辺値とする
                        return expression.IsLValue();
                    }

                    public EnclosedInParenthesesExpression(AST.Expression expression) {
                        this.expression = expression;
                    }
                }
            }

            /// <summary>
            /// 6.5.2 後置演算子式
            /// </summary>
            public abstract class PostfixExpression : Expression {
                /// <summary>
                /// 配列の添字付け
                /// </summary>
                /// <remarks>
                /// 式の一方は，型“オブジェクト型T型へのポインタ”をもたなければならない。
                /// もう一方の式は，整数型をもたなければならない。
                /// 結果は，型“T型”をもつ。
                /// 
                /// 脚注：有名な話だが「式の一方」とあるように、他の言語と違って配列式の要素を入れ替えても意味は変わらない。すなわち、x[1] と 1[x]は同じ意味。
                /// </remarks>
                public class ArraySubscriptingExpression : PostfixExpression {
                    /// <summary>
                    /// 型“オブジェクト型T型へのポインタ”（もしくは配列）の式
                    /// </summary>
                    public Expression Target {
                        get;
                    }
                    /// <summary>
                    /// 添え字式（整数側）の式
                    /// </summary>
                    public Expression Index {
                        get;
                    }
                    /// <summary>
                    /// 構文上での左辺側
                    /// </summary>
                    public Expression Lhs {
                        get;
                    }
                    /// <summary>
                    /// 構文上での右辺側
                    /// </summary>
                    public Expression Rhs {
                        get;
                    }

                    private CType _referencedType {
                        get;
                    }

                    public override CType Type {
                        get {
                            return _referencedType;
                        }
                    }

                    /// <summary>
                    /// 6.5.2.1
                    /// 配列の添字付け式
                    /// </summary>
                    /// <param name="lhs"></param>
                    /// <param name="rhs"></param>
                    public ArraySubscriptingExpression(Expression lhs, Expression rhs) {
                        // 6.3 型変換 
                        {
                            // 6.3.2.1 左辺値，配列及び関数指示子、
                            //
                            // 左辺値が sizeof 演算子のオペランド，単項&演算子のオペランド，又は文字配列を初期化するのに使われる文字列リテラルである場合を除いて，
                            // 型“～型の配列”をもつ式は，型“～型へのポインタ”の式に型変換する。それは配列オブジェクトの先頭の要素を指し，左辺値ではない。
                            // 配列オブジェクトがレジスタ記憶域クラスをもつ場合，その動作は未定義とする。
                            CType elementType;
                            if (lhs.Type.IsArrayType(out elementType)) {
                                lhs = new AST.Expression.CastExpression(new CType.PointerType(elementType), lhs);
                            } else if (rhs.Type.IsArrayType(out elementType)) {
                                rhs = new AST.Expression.CastExpression(new CType.PointerType(elementType), rhs);
                            }
                        }
                        {
                            // 6.3.1.1 論理型，文字型及び整数型
                            // int型又は unsigned int 型を使用してよい式の中ではどこでも，次に示すものを使用することができる。
                            // - 整数変換の順位が int 型及び unsigned int 型より低い整数型をもつオブジェクト又は式
                            // - _Bool 型，int 型，signed int 型，又は unsigned int 型のビットフィールド
                            // これらのものの元の型のすべての値を int 型で表現可能な場合，その値を int 型に変換する。
                            // そうでない場合，unsigned int 型に変換する。これらの処理を，整数拡張（integer promotion）と呼ぶ
                            if (lhs.Type.IsIntegerType()) {
                                lhs = Specification.IntegerPromotion(lhs);
                            } else if (rhs.Type.IsIntegerType()) {
                                rhs = Specification.IntegerPromotion(rhs);
                            }
                        }


                        // 制約
                        //   式の一方は，型“オブジェクト型T型へのポインタ”をもたなければならない。
                        //   もう一方の式は，整数型をもたなければならない。
                        CType referencedType;
                        if ((lhs.Type.IsPointerType(out referencedType) && referencedType.IsObjectType()) && (rhs.Type.IsIntegerType())) {
                            _referencedType = referencedType;
                            Target = lhs;
                            Index = rhs;
                        } else if ((rhs.Type.IsPointerType(out referencedType) && referencedType.IsObjectType()) && (lhs.Type.IsIntegerType())) {
                            _referencedType = referencedType;
                            Target = rhs;
                            Index = lhs;
                        } else {

                            throw new Exception("式の一方は，型“オブジェクト型へのポインタ”をもたなければならず、もう一方の式は，整数型をもたなければならない。");
                        }
                        Lhs = lhs;
                        Rhs = rhs;
                    }
                }

                /// <summary>
                /// 6.5.2.2
                /// 関数呼出し式
                /// </summary>
                public class FunctionCallExpression : PostfixExpression {
                    public Expression Expr {
                        get;
                    }
                    public List<Expression> Args {
                        get;
                    }
                    private CType _resultType {
                        get;
                    }

                    public override CType Type {
                        get {
                            return _resultType;
                        }
                    }

                    public FunctionCallExpression(Expression expr, List<Expression> args) {
                        // 6.3 型変換 
                        {
                            // 6.3.2.1 左辺値，配列及び関数指示子、
                            //
                            // 関数指示子（function designator）は，関数型をもつ式とする。
                            // 関数指示子が sizeof 演算子又は単項&演算子のオペランドである場合を除いて，型“∼型を返す関数”をもつ関数指示子は，型“∼型を返す関数へのポインタ”をもつ式に変換する。
                            if (expr.Type.IsFunctionType()) {
                                expr = new AST.Expression.PostfixExpression.CastExpression(new CType.PointerType(expr.Type.Unwrap()), expr);
                            }
                        }

                        // 制約
                        // 呼び出される関数を表す式は，void を返す関数へのポインタ型，又は配列型以外のオブジェクト型を返す関数へのポインタ型をもたなければならない。
                        CType referencedType;
                        CType.FunctionType functionType;
                        if (expr.Type.IsPointerType(out referencedType) && referencedType.IsFunctionType()) {
                            functionType = (referencedType as CType.FunctionType);
                            if (functionType.ResultType.IsVoidType() || (functionType.ResultType.IsObjectType() && !functionType.ResultType.IsArrayType())) {
                                goto Valid;
                            }
                        }
                        throw new Exception("呼び出される関数を表す式は，void を返す関数へのポインタ型，又は配列型以外のオブジェクト型を返す関数へのポインタ型をもたなければならない");
                        Valid:
                        if (functionType.Arguments != null) {
                            // 呼び出される関数を表す式が関数原型を含む型をもつ場合，実引数の個数は，仮引数の個数と一致しなければならない。
                            if (functionType.HasVariadic) { // 可変長引数を持つ
                                if (functionType.Arguments.Count > args.Count) {
                                    throw new Exception("実引数の個数が，仮引数の個数よりも少ない。");
                                }
                            } else {
                                if (functionType.Arguments.Count != args.Count) {
                                    throw new Exception("実引数の個数が，仮引数の個数と一致しない。");
                                }
                            }
                            // ToDo: 各実引数は，対応する仮引数の型の非修飾版をもつオブジェクトにその値を代入することのできる型をもたなければならない。

                        } else {

                        }
                        // 各実引数は，対応する仮引数の型の非修飾版をもつオブジェクトにその値を代入することのできる型をもたなければならない
                        _resultType = functionType.ResultType;
                        Expr = expr;
                        Args = args;
                    }
                }

                public class MemberDirectAccess : PostfixExpression {
                    public Expression Expr {
                        get;
                    }
                    public string Ident {
                        get;
                    }
                    private CType _memberType {
                        get;
                    }

                    public override CType Type {
                        get {
                            return _memberType;
                        }
                    }

                    public MemberDirectAccess(Expression expr, string ident) {
                        Expr = expr;
                        Ident = ident;
                    }
                }

                public class MemberIndirectAccess : PostfixExpression {
                    public Expression Expr {
                        get;
                    }
                    public string Ident {
                        get;
                    }
                    private CType _memberType {
                        get;
                    }

                    public override CType Type {
                        get {
                            return _memberType;
                        }
                    }

                    public MemberIndirectAccess(Expression expr, string ident) {
                        Expr = expr;
                        Ident = ident;
                    }
                }

                public class UnaryPostfixExpression : PostfixExpression {
                    public string Op {
                        get;
                    }

                    public Expression Expr {
                        get;
                    }

                    private CType _resultType {
                        get;
                    }

                    public override CType Type {
                        get {
                            return _resultType;
                        }
                    }

                    public UnaryPostfixExpression(string op, Expression expr) {
                        Op = op;
                        Expr = expr;
                    }

                }

                // Todo: C99の複合リテラル式はここに入る
            }

            public class CommaExpression : Expression {
                public List<AST.Expression> expressions { get; } = new List<AST.Expression>();
                public override CType Type {
                    get {
                        return expressions.Last().Type;
                    }
                }
            }


            public class AssignmentExpression : Expression {
                public string Op {
                    get;
                }
                public Expression Lhs {
                    get;
                }
                public Expression Rhs {
                    get;
                }
                private CType _resultType {
                    get;
                }
                public override CType Type {
                    get {
                        return _resultType;
                    }
                }
                public AssignmentExpression(string op, Expression lhs, Expression rhs) {
                    Op = op;
                    Lhs = lhs;
                    Rhs = rhs;
                }

            }

            public class ConditionalExpression : Expression {
                public Expression Cond {
                    get;
                }
                public Expression ThenExpr {
                    get;
                }
                public Expression ElseExpr {
                    get;
                }
                private CType _resultType {
                    get;
                }
                public override CType Type {
                    get {
                        return _resultType;
                    }
                }

                public ConditionalExpression(Expression cond, Expression thenExpr, Expression elseExpr) {
                    Cond = cond;
                    ThenExpr = thenExpr;
                    ElseExpr = elseExpr;
                }
            }

            public class LogicalOrExpression : Expression {
                public Expression Lhs {
                    get;
                }
                public Expression Rhs {
                    get;
                }
                private CType _resultType {
                    get;
                }
                public override CType Type {
                    get {
                        return _resultType;
                    }
                }

                public LogicalOrExpression(Expression lhs, Expression rhs) {
                    Lhs = lhs;
                    Rhs = rhs;
                }

            }

            public class LogicalAndExpression : Expression {
                public Expression Lhs {
                    get;
                }
                public Expression Rhs {
                    get;
                }
                private CType _resultType {
                    get;
                }
                public override CType Type {
                    get {
                        return _resultType;
                    }
                }

                public LogicalAndExpression(Expression lhs, Expression rhs) {
                    Lhs = lhs;
                    Rhs = rhs;
                }
            }

            public class InclusiveOrExpression : Expression {
                public Expression Lhs {
                    get;
                }
                public Expression Rhs {
                    get;
                }
                private CType _resultType {
                    get;
                }
                public override CType Type {
                    get {
                        return _resultType;
                    }
                }

                public InclusiveOrExpression(Expression lhs, Expression rhs) {
                    Lhs = lhs;
                    Rhs = rhs;
                }
            }

            public class ExclusiveOrExpression : Expression {
                public Expression Lhs {
                    get;
                }
                public Expression Rhs {
                    get;
                }
                private CType _resultType {
                    get;
                }
                public override CType Type {
                    get {
                        return _resultType;
                    }
                }

                public ExclusiveOrExpression(Expression lhs, Expression rhs) {
                    Lhs = lhs;
                    Rhs = rhs;
                }

            }

            public class AndExpression : Expression {
                public Expression Lhs {
                    get;
                }
                public Expression Rhs {
                    get;
                }
                private CType _resultType {
                    get;
                }
                public override CType Type {
                    get {
                        return _resultType;
                    }
                }

                public AndExpression(Expression lhs, Expression rhs) {
                    Lhs = lhs;
                    Rhs = rhs;
                }
            }

            public class EqualityExpression : Expression {
                public string Op {
                    get;
                }
                public Expression Lhs {
                    get;
                }
                public Expression Rhs {
                    get;
                }
                private CType _resultType {
                    get;
                }
                public override CType Type {
                    get {
                        return _resultType;
                    }
                }

                public EqualityExpression(string op, Expression lhs, Expression rhs) {
                    Op = op;
                    Lhs = lhs;
                    Rhs = rhs;
                }
            }

            public class RelationalExpression : Expression {
                public string Op {
                    get;
                }
                public Expression Lhs {
                    get;
                }
                public Expression Rhs {
                    get;
                }
                private CType _resultType {
                    get;
                }
                public override CType Type {
                    get {
                        return _resultType;
                    }
                }

                public RelationalExpression(string op, Expression lhs, Expression rhs) {
                    Op = op;
                    Lhs = lhs;
                    Rhs = rhs;
                }
            }

            public class ShiftExpression : Expression {
                public string Op {
                    get;
                }
                public Expression Lhs {
                    get;
                }
                public Expression Rhs {
                    get;
                }
                private CType _resultType {
                    get;
                }
                public override CType Type {
                    get {
                        return _resultType;
                    }
                }

                public ShiftExpression(string op, Expression lhs, Expression rhs) {
                    Op = op;
                    Lhs = lhs;
                    Rhs = rhs;
                }
            }

            public class AdditiveExpression : Expression {
                public string Op {
                    get;
                }
                public Expression Lhs {
                    get;
                }
                public Expression Rhs {
                    get;
                }
                private CType _resultType {
                    get;
                }
                public override CType Type {
                    get {
                        return _resultType;
                    }
                }

                public AdditiveExpression(string op, Expression lhs, Expression rhs) {
                    Op = op;
                    Lhs = lhs;
                    Rhs = rhs;
                }
            }

            public class MultiplicitiveExpression : Expression {
                public string Op {
                    get;
                }
                public Expression Lhs {
                    get;
                }
                public Expression Rhs {
                    get;
                }
                private CType _resultType {
                    get;
                }
                public override CType Type {
                    get {
                        return _resultType;
                    }
                }

                public MultiplicitiveExpression(string op, Expression lhs, Expression rhs) {
                    Op = op;
                    Lhs = lhs;
                    Rhs = rhs;
                }
            }

            public class CastExpression : Expression {
                public CType Ty {
                    get;
                }
                public Expression Expr {
                    get;
                }
                public override CType Type {
                    get {
                        return Ty;
                    }
                }

                public CastExpression(CType ty, Expression expr) {
                    Ty = ty;
                    Expr = expr;
                }
            }

            public class UnaryPrefixExpression : Expression {
                public string Op {
                    get;
                }
                public Expression Expr {
                    get;
                }
                private CType _resultType {
                    get;
                }
                public override CType Type {
                    get {
                        return _resultType;
                    }
                }

                public UnaryPrefixExpression(string op, Expression expr) {
                    Op = op;
                    Expr = expr;
                }
            }

            public class UnaryAddressExpression : Expression {
                public Expression Expr {
                    get;
                }
                private CType _resultType {
                    get;
                }
                public override CType Type {
                    get {
                        return _resultType;
                    }
                }

                public UnaryAddressExpression(Expression expr) {
                    Expr = expr;
                }
            }

            public class UnaryReferenceExpression : Expression {
                public Expression Expr {
                    get;
                }
                private CType _resultType {
                    get;
                }
                public override CType Type {
                    get {
                        return _resultType;
                    }
                }

                public UnaryReferenceExpression(Expression expr) {
                    Expr = expr;
                }
            }

            public class UnaryPlusExpression : Expression {
                public Expression Expr {
                    get;
                }
                private CType _resultType {
                    get;
                }
                public override CType Type {
                    get {
                        return _resultType;
                    }
                }

                public UnaryPlusExpression(Expression expr) {
                    Expr = expr;
                }
            }

            public class UnaryMinusExpression : Expression {
                public Expression Expr {
                    get;
                }
                private CType _resultType {
                    get;
                }
                public override CType Type {
                    get {
                        return _resultType;
                    }
                }

                public UnaryMinusExpression(Expression expr) {
                    Expr = expr;
                }
            }

            public class UnaryNegateExpression : Expression {
                public Expression Expr {
                    get;
                }
                private CType _resultType {
                    get;
                }
                public override CType Type {
                    get {
                        return _resultType;
                    }
                }

                public UnaryNegateExpression(Expression expr) {
                    Expr = expr;
                }
            }

            public class UnaryNotExpression : Expression {
                public Expression Expr {
                    get;
                }
                private CType _resultType {
                    get;
                }
                public override CType Type {
                    get {
                        return _resultType;
                    }
                }

                public UnaryNotExpression(Expression expr) {
                    Expr = expr;
                }
            }

            public class SizeofTypeExpression : Expression {
                public CType Ty {
                    get;
                }
                private CType _resultType {
                    get;
                }
                public override CType Type {
                    get {
                        return _resultType;
                    }
                }

                public SizeofTypeExpression(CType ty) {
                    Ty = ty;
                }
            }

            public class SizeofExpression : Expression {
                public Expression Expr {
                    get;
                }
                private CType _resultType {
                    get;
                }
                public override CType Type {
                    get {
                        return _resultType;
                    }
                }

                public SizeofExpression(Expression expr) {
                    Expr = expr;
                }
            }




            internal class GccStatementExpression : Expression {
                public Statement statements {
                    get;
                }
                private CType _resultType {
                    get;
                }
                public override CType Type {
                    get {
                        return _resultType;
                    }
                }

                public GccStatementExpression(Statement statements) {
                    this.statements = statements;
                }
            }


            public class IntegerPromotionExpression : Expression {
                public CType.BasicType Ty {
                    get;
                }
                public Expression Expr {
                    get;
                }
                public override CType Type {
                    get {
                        return Ty;
                    }
                }

                public IntegerPromotionExpression(CType.BasicType ty, Expression expr) {
                    Ty = ty;
                    Expr = expr;
                }
            }

        }

        public abstract class Statement : AST {
            public class GotoStatement : Statement {
                public string Label {
                    get;
                }

                public GotoStatement(string label) {
                    Label = label;
                }
            }

            public class ContinueStatement : Statement {
                public Statement Stmt {
                    get;
                }

                public ContinueStatement(Statement stmt) {
                    Stmt = stmt;
                }
            }

            public class BreakStatement : Statement {
                public Statement Stmt {
                    get;
                }

                public BreakStatement(Statement stmt) {
                    Stmt = stmt;
                }
            }

            public class ReturnStatement : Statement {
                public Expression Expr {
                    get;
                }

                public ReturnStatement(Expression expr) {
                    Expr = expr;
                }
            }

            public class WhileStatement : Statement {
                public Expression Cond {
                    get;
                }
                public Statement Stmt {
                    get; set;
                }

                public WhileStatement(Expression cond) {
                    Cond = cond;
                }
            }

            public class DoWhileStatement : Statement {
                public Statement Stmt {
                    get; set;
                }
                public Expression Cond {
                    get; set;
                }
            }

            public class ForStatement : Statement {
                public Expression Init {
                    get;
                }
                public Expression Cond {
                    get;
                }
                public Expression Update {
                    get;
                }
                public Statement Stmt {
                    get; set;
                }

                public ForStatement(Expression init, Expression cond, Expression update) {
                    Init = init;
                    Cond = cond;
                    Update = update;
                }
            }

            public class IfStatement : Statement {
                public Expression Cond {
                    get;
                }
                public Statement ThenStmt {
                    get;
                }
                public Statement ElseStmt {
                    get;
                }

                public IfStatement(Expression cond, Statement thenStmt, Statement elseStmt) {
                    Cond = cond;
                    ThenStmt = thenStmt;
                    ElseStmt = elseStmt;
                }
            }

            public class SwitchStatement : Statement {
                public Expression Cond {
                    get;
                }
                public Statement Stmt {
                    get; set;
                }

                public SwitchStatement(Expression cond) {
                    Cond = cond;
                }
            }

            public class CompoundStatement : Statement {
                public List<Declaration> Decls {
                    get;
                }
                public List<Statement> Stmts {
                    get;
                }
                public Scope<CType.TaggedType> TagScope {
                    get;
                }
                public Scope<Grammer.IdentifierValue> IdentScope {
                    get;
                }

                public CompoundStatement(List<Declaration> decls, List<Statement> stmts, Scope<CType.TaggedType> tagScope, Scope<Grammer.IdentifierValue> identScope) {
                    Decls = decls;
                    Stmts = stmts;
                    TagScope = tagScope;
                    IdentScope = identScope;
                }
            }

            public class EmptyStatement : Statement {
            }

            public class ExpressionStatement : Statement {
                public Expression Expr {
                    get;
                }

                public ExpressionStatement(Expression expr) {
                    Expr = expr;
                }
            }

            public class CaseStatement : Statement {
                public Expression Expr {
                    get;
                }
                public Statement Stmt {
                    get;
                }

                public CaseStatement(Expression expr, Statement stmt) {
                    Expr = expr;
                    Stmt = stmt;
                }
            }

            public class DefaultStatement : Statement {
                public Statement Stmt {
                    get;
                }

                public DefaultStatement(Statement stmt) {
                    Stmt = stmt;
                }
            }

            public class GenericLabeledStatement : Statement {
                public string Ident {
                    get;
                }
                public Statement Stmt {
                    get;
                }

                public GenericLabeledStatement(string ident, Statement stmt) {
                    Ident = ident;
                    Stmt = stmt;
                }
            }
        }

        public abstract class Initializer : AST {
            public class CompilxInitializer : Initializer {
                public List<Initializer> Ret {
                    get;
                }

                public CompilxInitializer(List<Initializer> ret) {
                    Ret = ret;
                }
            }

            public class SimpleInitializer : Initializer {
                public Expression AssignmentExpression {
                    get;
                }

                public SimpleInitializer(Expression assignmentExpression) {
                    AssignmentExpression = assignmentExpression;
                }
            }
        }

        public abstract class Declaration {

            public class FunctionDeclaration : Declaration {

                public string Ident {
                    get;
                }
                public CType Ty {
                    get;
                }
                public StorageClass StorageClass {
                    get;
                }
                public Statement Body {
                    get; set;
                }

                public FunctionDeclaration(string ident, CType ty, StorageClass storage_class) {
                    Ident = ident;
                    Ty = ty;
                    StorageClass = storage_class;
                    Body = null;
                }
            }

            public class VariableDeclaration : Declaration {
                public string Ident {
                    get;
                }
                public CType Ctype {
                    get;
                }
                public StorageClass StorageClass {
                    get;
                }
                public Initializer Init {
                    get;
                }

                public VariableDeclaration(string ident, CType ctype, StorageClass storage_class, Initializer init) {
                    Ident = ident;
                    Ctype = ctype;
                    StorageClass = storage_class;
                    Init = init;
                }
            }

            public class TypeDeclaration : Declaration {
                public string Ident {
                    get;
                }
                public CType Ctype {
                    get;
                }

                public TypeDeclaration(string ident, CType ctype) {
                    Ident = ident;
                    Ctype = ctype;
                }
            }
        }

        public class TranslationUnit : AST {
            public List<AST.Declaration> declarations { get; } = new List<Declaration>();
        }
    }

    /// <summary>
    /// スコープ
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    public class Scope<TValue> {
        public static Scope<TValue> Empty { get; } = new Scope<TValue>();
        public Scope<TValue> Parent { get; } = Empty;

#if false
        private LinkedDictionary<string, TValue> entries = LinkedDictionary<string, TValue>.Empty;

        private Scope() {
        }
        protected Scope(Scope<TValue> Parent) {
            this.Parent = Parent;
        }
        public Scope<TValue> Extend() {
            return new Scope<TValue>(this);
        }

        public void Add(string ident, TValue value) {
            this.entries = this.entries.Extend(ident, value);
        }

        public bool ContainsKey(string v) {
            var it = this;
            while (it != null) {
                if (it.entries.ContainsKey(v)) {
                    return true;
                }
                it = it.Parent;
            }
            return false;
        }

        public bool TryGetValue(string v, out TValue value) {
            var it = this;
            while (it != null) {
                if (it.entries.TryGetValue(v, out value)) {
                    return true;
                }
                it = it.Parent;
            }
            value = default(TValue);
            return false;
        }
#else
        private List<Tuple<string, TValue>> entries = new List<Tuple<string, TValue>>();

        private Scope() {
        }

        protected Scope(Scope<TValue> Parent) {
            this.Parent = Parent;
        }

        public Scope<TValue> Extend() {
            return new Scope<TValue>(this);
        }

        public void Add(string ident, TValue value) {
            this.entries.Add(Tuple.Create(ident, value));
        }

        public bool ContainsKey(string v) {
            var it = this;
            while (it != null) {
                if (it.entries.FindLast(x => x.Item1 == v) != null) {
                    return true;
                }
                it = it.Parent;
            }
            return false;
        }

        public bool TryGetValue(string v, out TValue value) {
            var it = this;
            while (it != null) {
                var val = it.entries.FindLast(x => x.Item1 == v);
                if (val != null) {
                    value = val.Item2;
                    return true;
                }
                it = it.Parent;
            }
            value = default(TValue);
            return false;
        }

#endif
    }

    public class Grammer {
        public abstract class IdentifierValue {
            public virtual bool IsEnumValue() {
                return false;
            }

            public virtual CType.TaggedType.EnumType.MemberInfo ToEnumValue() {
                throw new Exception("");
            }

            public virtual bool IsVariable() {
                return false;
            }

            public virtual AST.Declaration.VariableDeclaration ToVariable() {
                throw new Exception("");
            }

            public virtual bool IsFunction() {
                return false;
            }

            public virtual AST.Declaration.FunctionDeclaration ToFunction() {
                throw new Exception("");
            }

            public class EnumValue : IdentifierValue {
                public override bool IsEnumValue() {
                    return true;
                }
                public override CType.TaggedType.EnumType.MemberInfo ToEnumValue() {
                    return ctype.enumerator_list.Find(x => x.Name == ident);
                }
                public CType.TaggedType.EnumType ctype {
                    get;
                }
                public string ident {
                    get;
                }
                public EnumValue(CType.TaggedType.EnumType ctype, string ident) {
                    this.ctype = ctype;
                    this.ident = ident;
                }
            }

            public class Declaration : IdentifierValue {
                public override bool IsVariable() {
                    return Decl is AST.Declaration.VariableDeclaration;
                }

                public override AST.Declaration.VariableDeclaration ToVariable() {
                    return Decl as AST.Declaration.VariableDeclaration;
                }

                public override bool IsFunction() {
                    return Decl is AST.Declaration.FunctionDeclaration;
                }

                public override AST.Declaration.FunctionDeclaration ToFunction() {
                    return Decl as AST.Declaration.FunctionDeclaration;
                }

                public AST.Declaration Decl {
                    get;
                }

                public Declaration(AST.Declaration decl) {
                    Decl = decl;
                }
            }
        }

        /// <summary>
        /// 名前空間(ステートメント ラベル)
        /// </summary>
        private Scope<AST.Statement.GenericLabeledStatement> label_scope = Scope<AST.Statement.GenericLabeledStatement>.Empty;

        /// <summary>
        /// 名前空間(構造体、共用体、列挙体のタグ名)
        /// </summary>
        private Scope<CType.TaggedType> tag_scope = Scope<CType.TaggedType>.Empty;

        /// <summary>
        /// 名前空間(通常の識別子（変数、関数、引数、列挙定数)
        /// </summary>
        private Scope<IdentifierValue> ident_scope = Scope<IdentifierValue>.Empty;

        /// <summary>
        /// 名前空間(Typedef名)
        /// </summary>
        private Scope<AST.Declaration.TypeDeclaration> typedef_scope = Scope<AST.Declaration.TypeDeclaration>.Empty;

        // 構造体または共用体のメンバーについてはそれぞれの宣言オブジェクトに付与される

        /// <summary>
        /// break命令についてのスコープ
        /// </summary>
        private Stack<AST.Statement> break_scope = new Stack<AST.Statement>();

        /// <summary>
        /// continue命令についてのスコープ
        /// </summary>
        private Stack<AST.Statement> continue_scope = new Stack<AST.Statement>();

        //
        // lex spec
        //

        public class Token {
            [Flags]
            public enum TokenKind {
                EOF = -1,
                // ReserveWords
                AUTO = 256,
                BREAK,
                CASE,
                CHAR,
                CONST,
                CONTINUE,
                DEFAULT,
                DO,
                DOUBLE,
                ELSE,
                ENUM,
                EXTERN,
                FLOAT,
                FOR,
                GOTO,
                IF,
                INT,
                LONG,
                REGISTER,
                RETURN,
                SHORT,
                SIGNED,
                SIZEOF,
                STATIC,
                STRUCT,
                SWITCH,
                TYPEDEF,
                UNION,
                UNSIGNED,
                VOID,
                VOLATILE,
                WHILE,
                // C99
                INLINE,
                RESTRICT,
                // Special
                NEAR,
                FAR,
                __ASM__,
                __VOLATILE__,
                // Identifiers
                IDENTIFIER,
                TYPE_NAME,
                // Constants
                STRING_CONSTANT,
                HEXIMAL_CONSTANT,
                OCTAL_CONSTANT,
                DECIAML_CONSTANT,
                FLOAT_CONSTANT,
                DOUBLE_CONSTANT,
                // StringLiteral
                STRING_LITERAL,
                // Symbols
                ELLIPSIS,
                RIGHT_ASSIGN,
                LEFT_ASSIGN,
                ADD_ASSIGN,
                SUB_ASSIGN,
                MUL_ASSIGN,
                DIV_ASSIGN,
                MOD_ASSIGN,
                AND_ASSIGN,
                XOR_ASSIGN,
                OR_ASSIGN,
                RIGHT_OP,
                LEFT_OP,
                INC_OP,
                DEC_OP,
                PTR_OP,
                AND_OP,
                OR_OP,
                LE_OP,
                GE_OP,
                EQ_OP,
                NE_OP,
            }
            public int start {
                get;
            }
            public int length {
                get;
            }
            public string raw {
                get;
            }
            public TokenKind kind {
                get;
            }
            public Token(TokenKind kind, int start, int length, string raw) {
                this.kind = kind;
                this.start = start;
                this.length = length;
                this.raw = raw;
            }
            public override string ToString() {
                return $"(\"{raw}\", {kind}, {start}, {length})";
            }
        }

        private bool IsIdentifierHead(int ch) {
            return ('A' <= ch && ch <= 'Z') || ('a' <= ch && ch <= 'z') || (ch == '_');
        }
        private bool IsIdentifierBody(int ch) {
            return ('A' <= ch && ch <= 'Z') || ('a' <= ch && ch <= 'z') || ('0' <= ch && ch <= '9') || (ch == '_');
        }
        private bool IsDigit(int ch) {
            return ('0' <= ch && ch <= '9');
        }
        private bool IsSpace(int ch) {
            return "\r\n\v\f\t ".Any(x => (int)x == ch);
        }

        private Dictionary<string, Token.TokenKind> reserve_words = new Dictionary<string, Token.TokenKind>() {

            {"auto", Token.TokenKind.AUTO},
            {"break" , Token.TokenKind.BREAK},
            {"case" , Token.TokenKind.CASE},
            {"char" , Token.TokenKind.CHAR},
            {"const" , Token.TokenKind.CONST},
            {"continue" , Token.TokenKind.CONTINUE},
            {"default" , Token.TokenKind.DEFAULT},
            {"do" , Token.TokenKind.DO},
            {"double" , Token.TokenKind.DOUBLE},
            {"else" , Token.TokenKind.ELSE},
            {"enum" , Token.TokenKind.ENUM},
            {"extern" , Token.TokenKind.EXTERN},
            {"float" , Token.TokenKind.FLOAT},
            {"for" , Token.TokenKind.FOR},
            {"goto" , Token.TokenKind.GOTO},
            {"if" , Token.TokenKind.IF},
            {"int" , Token.TokenKind.INT},
            {"long" , Token.TokenKind.LONG},
            {"register" , Token.TokenKind.REGISTER},
            {"return" , Token.TokenKind.RETURN},
            {"short" , Token.TokenKind.SHORT},
            {"signed" , Token.TokenKind.SIGNED},
            {"sizeof" , Token.TokenKind.SIZEOF},
            {"static" , Token.TokenKind.STATIC},
            {"struct" , Token.TokenKind.STRUCT},
            {"switch" , Token.TokenKind.SWITCH},
            {"typedef" , Token.TokenKind.TYPEDEF},
            {"union" , Token.TokenKind.UNION},
            {"unsigned" , Token.TokenKind.UNSIGNED},
            {"void" , Token.TokenKind.VOID},
            {"volatile" , Token.TokenKind.VOLATILE},
            {"while" , Token.TokenKind.WHILE},
            // c99
            {"inline" , Token.TokenKind.INLINE},
            {"restrict" , Token.TokenKind.RESTRICT},
            // special
            {"near" , Token.TokenKind.NEAR},
            {"far" , Token.TokenKind.FAR},
            {"__asm__" , Token.TokenKind.__ASM__},
            {"__volatile__" , Token.TokenKind.__VOLATILE__},
        };

        private List<Tuple<string, Token.TokenKind>> symbols = new List<Tuple<string, Token.TokenKind>>() {
            Tuple.Create("...", Token.TokenKind.ELLIPSIS),
            Tuple.Create(">>=", Token.TokenKind.RIGHT_ASSIGN),
            Tuple.Create("<<=", Token.TokenKind.LEFT_ASSIGN),
            Tuple.Create("+=", Token.TokenKind.ADD_ASSIGN),
            Tuple.Create("-=", Token.TokenKind.SUB_ASSIGN),
            Tuple.Create("*=", Token.TokenKind.MUL_ASSIGN),
            Tuple.Create("/=", Token.TokenKind.DIV_ASSIGN),
            Tuple.Create("%=", Token.TokenKind.MOD_ASSIGN),
            Tuple.Create("&=", Token.TokenKind.AND_ASSIGN),
            Tuple.Create("^=", Token.TokenKind.XOR_ASSIGN),
            Tuple.Create("|=", Token.TokenKind.OR_ASSIGN),
            Tuple.Create(">>", Token.TokenKind.RIGHT_OP),
            Tuple.Create("<<", Token.TokenKind.LEFT_OP),
            Tuple.Create("++", Token.TokenKind.INC_OP),
            Tuple.Create("--", Token.TokenKind.DEC_OP),
            Tuple.Create("->", Token.TokenKind.PTR_OP),
            Tuple.Create("&&", Token.TokenKind.AND_OP),
            Tuple.Create("||", Token.TokenKind.OR_OP),
            Tuple.Create("<=", Token.TokenKind.LE_OP),
            Tuple.Create(">=", Token.TokenKind.GE_OP),
            Tuple.Create("==", Token.TokenKind.EQ_OP),
            Tuple.Create("!=", Token.TokenKind.NE_OP),
            Tuple.Create(";", (Token.TokenKind)';'),
            Tuple.Create("{", (Token.TokenKind)'{'),
            Tuple.Create("<%", (Token.TokenKind)'{'),
            Tuple.Create("}", (Token.TokenKind)'}'),
            Tuple.Create("%>", (Token.TokenKind)'}'),
            Tuple.Create("<:", (Token.TokenKind)'['),
            Tuple.Create(":>", (Token.TokenKind)']'),
            Tuple.Create(",", (Token.TokenKind)','),
            Tuple.Create(":", (Token.TokenKind)':'),
            Tuple.Create("=", (Token.TokenKind)'='),
            Tuple.Create("(", (Token.TokenKind)'('),
            Tuple.Create(")", (Token.TokenKind)')'),
            Tuple.Create("[", (Token.TokenKind)'['),
            Tuple.Create("]", (Token.TokenKind)']'),
            Tuple.Create(".", (Token.TokenKind)'.'),
            Tuple.Create("&", (Token.TokenKind)'&'),
            Tuple.Create("!", (Token.TokenKind)'!'),
            Tuple.Create("~", (Token.TokenKind)'~'),
            Tuple.Create("-", (Token.TokenKind)'-'),
            Tuple.Create("+", (Token.TokenKind)'+'),
            Tuple.Create("*", (Token.TokenKind)'*'),
            Tuple.Create("/", (Token.TokenKind)'/'),
            Tuple.Create("%", (Token.TokenKind)'%'),
            Tuple.Create("<", (Token.TokenKind)'<'),
            Tuple.Create(">", (Token.TokenKind)'>'),
            Tuple.Create("^", (Token.TokenKind)'^'),
            Tuple.Create("|", (Token.TokenKind)'|'),
            Tuple.Create("?", (Token.TokenKind)'?'),
        }.OrderByDescending((x) => x.Item1.Length).ToList();

        private static string D { get; } = $@"[0-9]";
        private static string L { get; } = $@"[a-zA-Z_]";
        private static string H { get; } = $@"[a-fA-F0-9]";
        private static string E { get; } = $@"[Ee][+-]?{D}+";
        private static string FS { get; } = $@"(f|F|l|L)?";
        private static string IS { get; } = $@"(u|U|l|L)*";
        private static Regex RegexPreprocessingNumber { get; } = new Regex($@"^(\.?\d([eEpP][\+\-]|\.|({L}|{D}|_))*)$");
        private static Regex RegexFlating { get; } = new Regex($@"^(?<Body>{D}+{E}|{D}*\.{D}+({E})?|{D}+\.{D}*({E})?)(?<Suffix>{FS})$");
        private static Regex RegexHeximal { get; } = new Regex($@"^0[xX](?<Body>{H}+)(?<Suffix>{IS})$");
        private static Regex RegexDecimal { get; } = new Regex($@"^(?<Body>{D}+)(?<Suffix>{IS})$");
        private static Regex RegexOctal { get; } = new Regex($@"^0(?<Body>{D}+)(?<Suffix>{IS})$");
        private static Regex RegexChar { get; } = new Regex($@"^L?'(\.|[^\'])+'$");
        private static Regex RegexStringLiteral { get; } = new Regex($@"^L?""(\.|[^\""])*""$");


        private string _inputText;
        private int _inputPos = 0;

        private bool _beginOfLine = true;
        private int line = 1;
        private int column = 1;

        private void IncPos(int n) {
            for (int i = 0; i < n; i++) {
                if (_inputText[_inputPos + i] == '\n') {
                    line++;
                    column = 1;
                } else {
                    column++;
                }
            }
            _inputPos += n;
        }

        private int scanch(int offset = 0) {
            if (_inputPos + offset >= _inputText.Length) {
                return -1;
            } else {
                return _inputText[_inputPos + offset];
            }
        }

        private bool scanch(string s) {
            for (var i = 0; i < s.Length; i++) {
                if (scanch(i) != s[i]) {
                    return false;
                }
            }
            return true;
        }

        private List<Token> _tokens { get; } = new List<Token>();


        private bool scan() {
            if (_tokens.LastOrDefault()?.kind == Token.TokenKind.EOF) {
                return false;
            }
            rescan:
            while (IsSpace(scanch())) {
                if (scanch("\n")) {
                    _beginOfLine = true;
                }
                IncPos(1);
            }
            if (scanch("/*")) {
                int start = _inputPos;
                IncPos(2);

                bool terminated = false;
                while (_inputPos < _inputText.Length) {
                    if (scanch("\\")) {
                        IncPos(2);
                    } else if (scanch("*/")) {
                        IncPos(2);
                        terminated = true;
                        break;
                    } else {
                        IncPos(1);
                    }
                }
                if (terminated == false) {
                    _tokens.Add(new Token(Token.TokenKind.EOF, _inputPos, 0, ""));
                    return false;
                }
                goto rescan;
            }
            if (scanch("//")) {
                int start = _inputPos;
                IncPos(2);

                bool terminated = false;
                while (_inputPos < _inputText.Length) {
                    if (scanch("\\")) {
                        IncPos(2);
                    } else if (scanch("\n")) {
                        terminated = true;
                        break;
                    } else {
                        IncPos(1);
                    }
                }
                if (terminated == false) {
                    _tokens.Add(new Token(Token.TokenKind.EOF, _inputPos, 0, ""));
                    return false;
                }
                goto rescan;
            }

            if (scanch() == -1) {
                _tokens.Add(new Token(Token.TokenKind.EOF, _inputPos, 0, ""));
                return false;
            } else if (scanch("#")) {
                if (_beginOfLine) {
                    // pragma は特殊
                    while (scanch("\n") == false) {
                        IncPos(1);
                    }
                    IncPos(1);
                    goto rescan;
                } else {
                    _tokens.Add(new Token((Token.TokenKind)'#', _inputPos, 1, "#"));
                    IncPos(1);
                }
                return true;
            }

            _beginOfLine = false;

            if (IsIdentifierHead(scanch())) {
                int start = _inputPos;
                while (IsIdentifierBody(scanch())) {
                    IncPos(1);
                }
                int len = _inputPos - start;
                var str = _inputText.Substring(start, len);
                Token.TokenKind reserveWordId;
                if (reserve_words.TryGetValue(str, out reserveWordId)) {
                    _tokens.Add(new Token(reserveWordId, start, len, str));
                } else {
                    AST.Declaration.TypeDeclaration val;
                    if (typedef_scope.TryGetValue(str, out val)) {
                        _tokens.Add(new Token(Token.TokenKind.TYPE_NAME, start, len, str));
                    } else {
                        _tokens.Add(new Token(Token.TokenKind.IDENTIFIER, start, len, str));
                    }
                }
                return true;
            } else if ((scanch(0) == '.' && IsDigit(scanch(1))) || IsDigit(scanch(0))) {
                // preprocessor number
                // \.?\d([eEpP][\+\-]|\.|({L}|{D}|_))*
                int start = _inputPos;
                if (scanch() == '.') {
                    IncPos(1);
                }
                IncPos(1);
                while (scanch() != -1) {
                    if ("eEpP".Any(x => (int)x == scanch(0)) && "+-".Any(x => (int)x == scanch(1))) {
                        IncPos(2);
                    } else if (scanch(".") || IsIdentifierBody(scanch())) {
                        IncPos(1);
                    } else {
                        break;
                    }
                }
                int len = _inputPos - start;
                var str = _inputText.Substring(start, len);
                if (RegexFlating.IsMatch(str)) {
                    _tokens.Add(new Token(Token.TokenKind.FLOAT_CONSTANT, start, len, str));
                } else if (RegexHeximal.IsMatch(str)) {
                    _tokens.Add(new Token(Token.TokenKind.HEXIMAL_CONSTANT, start, len, str));
                } else if (RegexOctal.IsMatch(str)) {
                    _tokens.Add(new Token(Token.TokenKind.OCTAL_CONSTANT, start, len, str));
                } else if (RegexDecimal.IsMatch(str)) {
                    _tokens.Add(new Token(Token.TokenKind.DECIAML_CONSTANT, start, len, str));
                } else {
                    throw new Exception();
                }
                return true;
            } else if (scanch("'")) {
                int start = _inputPos;
                IncPos(1);
                while (_inputPos < _inputText.Length) {
                    if (scanch("\\")) {
                        IncPos(2);
                    } else if (scanch("'")) {
                        IncPos(1);
                        int len = _inputPos - start;
                        var str = _inputText.Substring(start, len);
                        _tokens.Add(new Token(Token.TokenKind.STRING_CONSTANT, start, len, str));
                        return true;
                    } else {
                        IncPos(1);
                    }
                }
                throw new Exception();
            } else if (scanch("\"")) {
                int start = _inputPos;
                IncPos(1);
                while (_inputPos < _inputText.Length) {
                    if (scanch("\\")) {
                        IncPos(2);
                    } else if (scanch("\"")) {
                        IncPos(1);
                        int len = _inputPos - start;
                        var str = _inputText.Substring(start, len);
                        _tokens.Add(new Token(Token.TokenKind.STRING_LITERAL, start, len, str));
                        return true;
                    } else {
                        IncPos(1);
                    }
                }
                throw new Exception();
            } else {
                foreach (var sym in symbols) {
                    if (scanch(sym.Item1)) {
                        _tokens.Add(new Token(sym.Item2, _inputPos, sym.Item1.Length, sym.Item1));
                        IncPos(sym.Item1.Length);
                        return true;
                    }
                }
                throw new Exception();
            }
        }

        private int current = 0;

        public Grammer() {
        }


        public void Parse(string s) {
            _inputText = s;
            _tokens.Clear();
            _inputPos = 0;
            current = 0;

            // GCCの組み込み型の設定
            typedef_scope.Add("__builtin_va_list", new AST.Declaration.TypeDeclaration("__builtin_va_list", new CType.PointerType(new CType.BasicType(TypeSpecifier.Void))));

            var ret = translation_unit();
        }


        private void next_token() {
            current++;
        }
        private bool is_eof() {
            return _tokens[current].kind == Token.TokenKind.EOF;
        }
        private void eof() {
            if (!is_eof()) {
                throw new Exception();
            }
        }

        private Token current_token() {
            if (_tokens.Count == current) {
                scan();
            }
            return _tokens[current];
        }

        private void Read(params Token.TokenKind[] s) {
            if (s.Contains(current_token().kind)) {
                next_token();
                return;
            }
            throw new Exception();
        }
        private void Read(params char[] s) {
            Read(s.Select(x => (Token.TokenKind)x).ToArray());
        }
        private bool Peek(params Token.TokenKind[] s) {
            return s.Contains(current_token().kind);
        }
        private bool Peek(params char[] s) {
            return Peek(s.Select(x => (Token.TokenKind)x).ToArray());
        }

        private bool is_nexttoken(params Token.TokenKind[] s) {
            if (_tokens.Count <= current + 1) {
                scan();
                if (is_eof()) {
                    return false;
                }
            }
            return s.Contains(_tokens[current + 1].kind);
        }
        private bool is_nexttoken(params char[] s) {
            return is_nexttoken(s.Select(x => (Token.TokenKind)x).ToArray());
        }

        private bool is_ENUMERATION_CONSTANT() {
            if (!is_IDENTIFIER(false)) {
                return false;
            }
            var ident = current_token();
            IdentifierValue v;
            if (ident_scope.TryGetValue(ident.raw, out v) == false) {
                return false;
            }
            if (!(v is IdentifierValue.EnumValue)) {
                return false;
            }
            return (v as IdentifierValue.EnumValue).ctype.enumerator_list.First(x => x.Name == ident.raw) != null;
        }

        private CType.TaggedType.EnumType.MemberInfo ENUMERATION_CONSTANT() {
            var ident = IDENTIFIER(false);
            IdentifierValue v;
            if (ident_scope.TryGetValue(ident, out v) == false) {
                throw new Exception();
            }
            if (!(v is IdentifierValue.EnumValue)) {
                throw new Exception();
            }
            var ev = (v as IdentifierValue.EnumValue);
            var el = ev.ctype.enumerator_list.First(x => x.Name == ident);
            return el;
        }

        private bool is_CHARACTER_CONSTANT() {
            return current_token().kind == Token.TokenKind.STRING_CONSTANT;
        }

        private string CHARACTER_CONSTANT() {
            if (is_CHARACTER_CONSTANT() == false) {
                throw new Exception();
            }
            var ret = current_token().raw;
            next_token();
            return ret;
        }

        private bool is_FLOATING_CONSTANT() {
            return current_token().kind == Token.TokenKind.FLOAT_CONSTANT;
        }
        private AST.Expression.PrimaryExpression.ConstantExpression.FloatingConstant FLOATING_CONSTANT() {
            if (is_FLOATING_CONSTANT() == false) {
                throw new Exception();
            }
            var raw = current_token().raw;
            var m = RegexHeximal.Match(raw);
            if (m.Success == false) {
                throw new Exception();
            }
            var value = Convert.ToDouble(m.Groups["Body"].Value);
            CType.BasicType.Kind type;
            switch (String.Concat(m.Groups["Suffix"].Value.ToUpper().ToCharArray().OrderBy(x => x))) {
                case "F":
                    type = CType.BasicType.Kind.Float;
                    break;
                case "L":
                    type = CType.BasicType.Kind.LongDouble;
                    break;
                case "":
                    type = CType.BasicType.Kind.Double;
                    break;
                default:
                    throw new Exception();
            }
            next_token();
            return new AST.Expression.PrimaryExpression.ConstantExpression.FloatingConstant(raw, value, type);
        }

        private bool is_INTEGER_CONSTANT() {
            return current_token().kind == Token.TokenKind.HEXIMAL_CONSTANT | current_token().kind == Token.TokenKind.OCTAL_CONSTANT | current_token().kind == Token.TokenKind.DECIAML_CONSTANT;
        }
        private AST.Expression.PrimaryExpression.ConstantExpression.IntegerConstant INTEGER_CONSTANT() {
            if (is_INTEGER_CONSTANT() == false) {
                throw new Exception();
            }
            string raw = current_token().raw;
            string body;
            string suffix;
            int radix;
            CType.BasicType.Kind[] candidates;

            switch (current_token().kind) {
                case Token.TokenKind.HEXIMAL_CONSTANT: {
                        var m = RegexHeximal.Match(raw);
                        if (m.Success == false) {
                            throw new Exception();
                        }
                        body = m.Groups["Body"].Value;
                        suffix = String.Concat(m.Groups["Suffix"].Value.ToUpper().ToCharArray().OrderBy(x => x));
                        radix = 16;
                        switch (suffix) {
                            case "LLU":
                                candidates = new[] { CType.BasicType.Kind.UnsignedLongLongInt };
                                break;
                            case "LL":
                                candidates = new[] { CType.BasicType.Kind.SignedLongLongInt, CType.BasicType.Kind.UnsignedLongLongInt };
                                break;
                            case "UL":
                                candidates = new[] { CType.BasicType.Kind.UnsignedLongInt, CType.BasicType.Kind.UnsignedLongLongInt };
                                break;
                            case "L":
                                candidates = new[] { CType.BasicType.Kind.SignedLongInt, CType.BasicType.Kind.UnsignedLongInt, CType.BasicType.Kind.SignedLongLongInt, CType.BasicType.Kind.UnsignedLongLongInt };
                                break;
                            case "U":
                                candidates = new[] { CType.BasicType.Kind.UnsignedInt, CType.BasicType.Kind.UnsignedLongInt, CType.BasicType.Kind.UnsignedLongLongInt };
                                break;
                            case "":
                                candidates = new[] { CType.BasicType.Kind.SignedInt, CType.BasicType.Kind.UnsignedInt, CType.BasicType.Kind.SignedLongInt, CType.BasicType.Kind.UnsignedLongInt, CType.BasicType.Kind.SignedLongLongInt, CType.BasicType.Kind.UnsignedLongLongInt };
                                break;
                            default:
                                throw new Exception();
                        }

                        break;
                    }
                case Token.TokenKind.OCTAL_CONSTANT: {
                        var m = RegexOctal.Match(raw);
                        if (m.Success == false) {
                            throw new Exception();
                        }
                        body = m.Groups["Body"].Value;
                        suffix = String.Concat(m.Groups["Suffix"].Value.ToUpper().ToCharArray().OrderBy(x => x));
                        radix = 8;
                        switch (suffix) {
                            case "LLU":
                                candidates = new[] { CType.BasicType.Kind.UnsignedLongLongInt };
                                break;
                            case "LL":
                                candidates = new[] { CType.BasicType.Kind.SignedLongLongInt, CType.BasicType.Kind.UnsignedLongLongInt };
                                break;
                            case "UL":
                                candidates = new[] { CType.BasicType.Kind.UnsignedLongInt, CType.BasicType.Kind.UnsignedLongLongInt };
                                break;
                            case "L":
                                candidates = new[] { CType.BasicType.Kind.SignedLongInt, CType.BasicType.Kind.UnsignedLongInt, CType.BasicType.Kind.SignedLongLongInt, CType.BasicType.Kind.UnsignedLongLongInt };
                                break;
                            case "U":
                                candidates = new[] { CType.BasicType.Kind.UnsignedInt, CType.BasicType.Kind.UnsignedLongInt, CType.BasicType.Kind.UnsignedLongLongInt };
                                break;
                            case "":
                                candidates = new[] { CType.BasicType.Kind.SignedInt, CType.BasicType.Kind.UnsignedInt, CType.BasicType.Kind.SignedLongInt, CType.BasicType.Kind.UnsignedLongInt, CType.BasicType.Kind.SignedLongLongInt, CType.BasicType.Kind.UnsignedLongLongInt };
                                break;
                            default:
                                throw new Exception();
                        }
                        break;
                    }
                case Token.TokenKind.DECIAML_CONSTANT: {
                        var m = RegexDecimal.Match(raw);
                        if (m.Success == false) {
                            throw new Exception();
                        }
                        body = m.Groups["Body"].Value;
                        suffix = String.Concat(m.Groups["Suffix"].Value.ToUpper().ToCharArray().OrderBy(x => x));
                        radix = 10;
                        switch (suffix) {
                            case "LLU":
                                candidates = new[] { CType.BasicType.Kind.UnsignedLongLongInt };
                                break;
                            case "LL":
                                candidates = new[] { CType.BasicType.Kind.SignedLongLongInt };
                                break;
                            case "UL":
                                candidates = new[] { CType.BasicType.Kind.UnsignedLongInt, CType.BasicType.Kind.UnsignedLongLongInt };
                                break;
                            case "L":
                                candidates = new[] { CType.BasicType.Kind.SignedLongInt, CType.BasicType.Kind.SignedLongLongInt };
                                break;
                            case "U":
                                candidates = new[] { CType.BasicType.Kind.UnsignedInt, CType.BasicType.Kind.UnsignedLongInt, CType.BasicType.Kind.UnsignedLongLongInt };
                                break;
                            case "":
                                candidates = new[] { CType.BasicType.Kind.SignedInt, CType.BasicType.Kind.SignedLongInt, CType.BasicType.Kind.SignedLongLongInt };
                                break;
                            default:
                                throw new Exception();
                        }
                        break;
                    }
                default:
                    throw new Exception();

            }

            var originalSigned = Convert.ToInt64(body, radix);
            var originalUnsigned = Convert.ToUInt64(body, radix);
            Int64 value = 0;

            CType.BasicType.Kind selectedType = 0;
            System.Diagnostics.Debug.Assert(candidates.Length > 0);
            foreach (var candidate in candidates) {
                switch (candidate) {
                    case CType.BasicType.Kind.SignedInt: {
                            var v = Convert.ToInt32(body, radix);
                            if (v == originalSigned) {
                                value = unchecked((Int64)v);
                                break;
                            }
                            continue;
                        }
                    case CType.BasicType.Kind.UnsignedInt: {
                            var v = Convert.ToUInt32(body, radix);
                            if (v == originalUnsigned) {
                                value = unchecked((Int64)v);
                                break;
                            }
                            continue;
                        }
                    case CType.BasicType.Kind.SignedLongInt: {
                            var v = Convert.ToInt32(body, radix);
                            if (v == originalSigned) {
                                value = unchecked((Int64)v);
                                break;
                            }
                            continue;
                        }
                    case CType.BasicType.Kind.UnsignedLongInt: {
                            var v = Convert.ToUInt32(body, radix);
                            if (v == originalUnsigned) {
                                value = unchecked((Int64)v);
                                break;
                            }
                            continue;
                        }
                    case CType.BasicType.Kind.SignedLongLongInt: {
                            var v = Convert.ToInt64(body, radix);
                            if (v == originalSigned) {
                                value = unchecked((Int64)v);
                                break;
                            }
                            continue;
                        }
                    case CType.BasicType.Kind.UnsignedLongLongInt: {
                            var v = Convert.ToUInt64(body, radix);
                            if (v == originalUnsigned) {
                                value = unchecked((Int64)v);
                                break;
                            }
                            continue;
                        }
                    default:
                        throw new Exception();
                }
                selectedType = candidate;
                break;
            }

            next_token();

            return new AST.Expression.PrimaryExpression.ConstantExpression.IntegerConstant(raw, value, selectedType);

        }

        private bool is_STRING() {
            return current_token().kind == Token.TokenKind.STRING_LITERAL;
        }
        private string STRING() {
            if (is_STRING() == false) {
                throw new Exception();
            }
            var ret = current_token().raw;
            next_token();
            return ret;
        }

        //
        // Grammers
        //


        public AST.TranslationUnit translation_unit() {
            var ret = new AST.TranslationUnit();
            while (is_external_declaration(null, TypeSpecifier.None)) {
                ret.declarations.AddRange(external_declaration());
            }
            eof();
            return ret;
        }


        private bool is_external_declaration(CType baseType, TypeSpecifier typeSpecifier) {
            return (is_declaration_specifier(baseType, typeSpecifier) || Peek(';') || is_declarator());
        }

        private List<AST.Declaration> external_declaration() {
            CType baseType = null;
            StorageClass storageClass = StorageClass.None;
            TypeSpecifier typeSpecifier = TypeSpecifier.None;
            TypeQualifier typeQualifier = TypeQualifier.None;
            FunctionSpecifier functionSpecifier = FunctionSpecifier.None;
            while (is_declaration_specifier(baseType, typeSpecifier)) {
                declaration_specifier(ref baseType, ref storageClass, ref typeSpecifier, ref typeQualifier, ref functionSpecifier);
            }
            if (typeSpecifier != TypeSpecifier.None) {
                if (baseType != null) {
                    throw new Exception("");
                } else {
                    baseType = new CType.BasicType(typeSpecifier);
                }
            } else if (baseType == null) {
                baseType = new CType.BasicType(TypeSpecifier.None);
            }
            if (baseType.IsQualifiedType()) {
                (baseType as CType.TypeQualifierType).type_qualifier |= typeQualifier;
            } else {
                baseType = new CType.TypeQualifierType(baseType, typeQualifier);
            }

            var ret = new List<AST.Declaration>();


            if (!is_declarator()) {
                Read(';');
                return ret;
            } else {
                for (; ; ) {
                    string ident = "";
                    List<CType> stack = new List<CType>() { new CType.StubType() };
                    declarator(ref ident, stack, 0);
                    var ctype = CType.Resolve(baseType, stack);
                    if (Peek('=', ',', ';')) {
                        // 関数定義以外である

                        if (functionSpecifier != FunctionSpecifier.None) {
                            throw new Exception("inlineは関数定義に対してのみ使える。");
                        }
                        if (storageClass == StorageClass.Auto || storageClass == StorageClass.Register) {
                            throw new Exception("inlineは関数定義に対してのみ使える。");
                        }



                        AST.Declaration decl = null;
                        if (Peek('=')) {
                            // 初期化式を伴うので、初期化付きの変数宣言

                            if (storageClass == StorageClass.Typedef || storageClass == StorageClass.Auto || storageClass == StorageClass.Register) {
                                throw new Exception("変数宣言には指定できない記憶クラス指定子が指定されている。");
                            }

                            if (ctype.IsFunctionType()) {
                                throw new Exception("関数宣言に初期値を指定している");
                            }

                            Read('=');
                            var init = initializer();
                            decl = new AST.Declaration.VariableDeclaration(ident, ctype, storageClass, init);
                        } else {
                            // 初期化式を伴わないため、関数宣言、変数宣言、Typedef宣言のどれか

                            if (storageClass == StorageClass.Auto || storageClass == StorageClass.Register) {
                                throw new Exception("ファイル有効範囲での関数宣言、変数宣言、Typedef宣言で指定できない記憶クラス指定子が指定されている。");
                            }


                            if (ctype.IsFunctionType()) {
                                // 6.7.5.3 関数宣言子（関数原型を含む）
                                // 関数定義の一部でない関数宣言子における識別子並びは，空でなければならない。
                                // 脚注　関数宣言でK&Rの関数定義のように int f(a,b,c); と書くことはダメということ。int f(); ならOK
                                if ((ctype as CType.FunctionType).Arguments != null) {
                                    // K&R の記法で宣言を記述した場合、cTypeがnull
                                    // ANSIの記法で宣言を記述した場合、cTypeは非null
                                    if ((ctype as CType.FunctionType).Arguments.Any(x => x.cType == null)) {
                                        throw new Exception("関数定義の一部でない関数宣言子における識別子並びは，空でなければならない。");
                                    }
                                }
                            }

                            if (storageClass == StorageClass.Typedef) {
                                AST.Declaration.TypeDeclaration tdecl;
                                if (typedef_scope.TryGetValue(ident, out tdecl)) {
                                    if (CType.IsEqual(tdecl.Ctype, ctype) == false) {
                                        throw new Exception("再定義型の不一致");
                                    }
                                } else {
                                    tdecl = new AST.Declaration.TypeDeclaration(ident, ctype);
                                    decl = tdecl;
                                    typedef_scope.Add(ident, tdecl);
                                }
                            } else if (ctype.IsFunctionType()) {
                                // 関数宣言
                                IdentifierValue iv;
                                if (ident_scope.TryGetValue(ident, out iv)) {
                                    if (iv.IsFunction() == false) {
                                        throw new Exception("関数型以外で宣言済み");
                                    }
                                    if (CType.IsEqual(iv.ToFunction().Ty, ctype) == false) {
                                        throw new Exception("再定義型の不一致");
                                    }
                                    // Todo: 型の合成
                                }
                                decl = new AST.Declaration.FunctionDeclaration(ident, ctype, storageClass);
                                ident_scope.Add(ident, new IdentifierValue.Declaration(decl));
                            } else {
                                // 変数宣言
                                IdentifierValue iv;
                                if (ident_scope.TryGetValue(ident, out iv)) {
                                    if (iv.IsVariable() == false) {
                                        throw new Exception("変数型以外で宣言済み");
                                    }
                                    if (CType.IsEqual(iv.ToVariable().Ctype, ctype) == false) {
                                        throw new Exception("再定義型の不一致");
                                    }
                                    // Todo: 型の合成
                                }
                                decl = new AST.Declaration.VariableDeclaration(ident, ctype, storageClass, null);
                                ident_scope.Add(ident, new IdentifierValue.Declaration(decl));
                            }
                        }
                        ret.Add(decl);


                        if (Peek(',')) {
                            Read(',');
                            continue;
                        }
                        break;
                    } else if (ctype.IsFunctionType()) {
                        // 関数定義

                        // K&Rにおける宣言並びがあるか調べる。
                        var argmuents = is_declaration() ? declaration() : null;

                        // 宣言並びと仮引数宣言の両方があるのは違反
                        if (argmuents?.Any(x => !(x is AST.Declaration.VariableDeclaration)) == true) {
                            throw new Exception("宣言並び中に仮引数宣言以外がある");
                        }

                        var ctype_fun = ctype as CType.FunctionType;

                        if (ctype_fun.Arguments == null) {
                            // 識別子並び・仮引数型並びなし
                            if (argmuents != null) {
                                throw new Exception("K&R形式の関数定義だが、識別子並びが空なのに、宣言並びがある");
                            } else {
                                // 引数指定なし関数
                            }
                        } else if (ctype_fun.Arguments.Any(x => (x.cType as CType.BasicType)?.kind == CType.BasicType.Kind.KAndRImplicitInt)) {

                            // K&R形式の識別子並びが存在するので K&R 形式として処理
                            if (ctype_fun.Arguments.Any(x => (x.cType as CType.BasicType)?.kind != CType.BasicType.Kind.KAndRImplicitInt)) {
                                throw new Exception("関数定義中でK&R形式の識別子並びとANSI形式の仮引数型並びが混在している");
                            }

                            // 識別子並びに宣言並びの型情報を規定の実引数拡張を伴って反映させる。
                            var dic = argmuents.Cast<AST.Declaration.VariableDeclaration>().ToDictionary(x => x.Ident, x => x);
                            var mapped = ctype_fun.Arguments.Select(x => {
                                if (dic.ContainsKey(x.Name)) {
                                    return new CType.FunctionType.ArgumentInfo(x.Name, x.Sc, dic[x.Name].Ctype.DefaultArgumentPromotion(), dic[x.Name].Ctype);
                                } else {
                                    var type = (CType)new CType.BasicType(CType.BasicType.Kind.SignedInt);
                                    return new CType.FunctionType.ArgumentInfo(x.Name, x.Sc, type.DefaultArgumentPromotion(), type);
                                }
                            }).ToList();



                            ctype_fun.Arguments.Clear();
                            ctype_fun.Arguments.AddRange(mapped);

                            // ToDo: スコープを一つ作り、実引数拡張後の引数変数から実引数拡張前の型で指定された引数変数へのコピーを入れる。

                        } else {
                            // ANSI形式の仮引数型並びのみなので何もしない
                        }

                        // 関数が定義済みの場合は、再定義のチェックを行う
                        IdentifierValue iv;
                        if (ident_scope.TryGetValue(ident, out iv)) {
                            if (iv.IsFunction() == false) {
                                throw new Exception("関数型以外で宣言済み");
                            }
                            if (CType.IsEqual(iv.ToFunction().Ty, ctype_fun) == false) {
                                throw new Exception("再定義型の不一致");
                            }
                            if (iv.ToFunction().Body != null) {
                                throw new Exception("関数はすでに本体を持っている。");
                            }

                        }
                        var funcdecl = new AST.Declaration.FunctionDeclaration(ident, ctype, storageClass);
                        ident_scope.Add(ident, new IdentifierValue.Declaration(funcdecl));

                        funcdecl.Body = compound_statement();



                        ret.Add(funcdecl);
                        return ret;
                    } else {
                        throw new Exception("");
                    }

                }
                Read(';');
                return ret;
            }

        }

        private bool is_declaration() {
            return is_declaration_specifiers(null, TypeSpecifier.None);
        }
        private List<AST.Declaration> declaration() {
            StorageClass storageClass;
            CType baseType = declaration_specifiers(out storageClass);

            List<AST.Declaration> decls = null;
            if (!Peek(';')) {
                decls = new List<AST.Declaration>();
                decls.Add(init_declarator(baseType, storageClass));
                while (Peek(',')) {
                    Read(',');
                    decls.Add(init_declarator(baseType, storageClass));
                }
            }
            Read(';');
            return decls;
        }
        private bool is_declaration_specifiers(CType ctype, TypeSpecifier typeSpecifier) {
            return is_declaration_specifier(ctype, typeSpecifier);
        }

        private CType declaration_specifiers(out StorageClass sc) {
            CType baseType = null;
            StorageClass storageClass = StorageClass.None;
            TypeSpecifier typeSpecifier = TypeSpecifier.None;
            TypeQualifier typeQualifier = TypeQualifier.None;
            FunctionSpecifier functionSpecifier = FunctionSpecifier.None;

            declaration_specifier(ref baseType, ref storageClass, ref typeSpecifier, ref typeQualifier, ref functionSpecifier);

            while (is_declaration_specifier(baseType, typeSpecifier)) {
                declaration_specifier(ref baseType, ref storageClass, ref typeSpecifier, ref typeQualifier, ref functionSpecifier);
            }

            if (functionSpecifier != FunctionSpecifier.None) {
                throw new Exception("inlineは関数定義でのみ使える。");
            }

            if (typeSpecifier != TypeSpecifier.None) {
                if (baseType != null) {
                    throw new Exception("");
                } else {
                    baseType = new CType.BasicType(typeSpecifier);
                }
            } else if (baseType == null) {
                baseType = new CType.BasicType(TypeSpecifier.None);
            }
            sc = storageClass;

            if (baseType.IsQualifiedType()) {
                (baseType as CType.TypeQualifierType).type_qualifier |= typeQualifier;
            } else {
                baseType = new CType.TypeQualifierType(baseType, typeQualifier);
            }
            return baseType;
        }

        private bool is_declaration_specifier(CType ctype, TypeSpecifier typeSpecifier) {
            return (is_storage_class_specifier() ||
                (is_type_specifier() && ctype == null) ||
                (is_struct_or_union_specifier() && ctype == null) ||
                (is_enum_specifier() && ctype == null) ||
                (is_TYPEDEF_NAME() && ctype == null && typeSpecifier == TypeSpecifier.None) ||
                is_type_qualifier() ||
                is_function_specifier());
        }

        private void declaration_specifier(ref CType ctype, ref StorageClass storageClass, ref TypeSpecifier typeSpecifier, ref TypeQualifier typeQualifier, ref FunctionSpecifier functionSpecifier) {
            if (is_storage_class_specifier()) {
                storageClass = storageClass.Marge(storage_class_specifier());
            } else if (is_type_specifier()) {
                typeSpecifier = typeSpecifier.Marge(type_specifier());
            } else if (is_struct_or_union_specifier()) {
                if (ctype != null) {
                    throw new Exception("");
                }
                ctype = struct_or_union_specifier();
            } else if (is_enum_specifier()) {
                if (ctype != null) {
                    throw new Exception("");
                }
                ctype = enum_specifier();
            } else if (is_TYPEDEF_NAME()) {
                AST.Declaration.TypeDeclaration value;
                if (typedef_scope.TryGetValue(current_token().raw, out value) == false) {
                    throw new Exception();
                }
                if (ctype != null) {
                    if (CType.IsEqual(ctype, value.Ctype) == false) {
                        throw new Exception("");
                    }
                }
                ctype = new CType.TypedefedType(current_token().raw, value.Ctype);
                next_token();
            } else if (is_type_qualifier()) {
                typeQualifier.Marge(type_qualifier());
            } else if (is_function_specifier()) {
                functionSpecifier.Marge(function_specifier());
            } else {
                throw new Exception("");
            }
        }
        private bool is_storage_class_specifier() {
            return Peek(Token.TokenKind.AUTO, Token.TokenKind.REGISTER, Token.TokenKind.STATIC, Token.TokenKind.EXTERN, Token.TokenKind.TYPEDEF);
        }
        private StorageClass storage_class_specifier() {
            switch (current_token().kind) {
                case Token.TokenKind.AUTO:
                    next_token();
                    return StorageClass.Auto;
                case Token.TokenKind.REGISTER:
                    next_token();
                    return StorageClass.Register;
                case Token.TokenKind.STATIC:
                    next_token();
                    return StorageClass.Static;
                case Token.TokenKind.EXTERN:
                    next_token();
                    return StorageClass.Extern;
                case Token.TokenKind.TYPEDEF:
                    next_token();
                    return StorageClass.Typedef;
                default:
                    throw new Exception();
            }
        }
        private bool is_type_specifier() {
            return Peek(Token.TokenKind.VOID, Token.TokenKind.CHAR, Token.TokenKind.INT, Token.TokenKind.FLOAT, Token.TokenKind.DOUBLE, Token.TokenKind.SHORT, Token.TokenKind.LONG, Token.TokenKind.SIGNED, Token.TokenKind.UNSIGNED
);
        }
        private TypeSpecifier type_specifier() {
            switch (current_token().kind) {
                case Token.TokenKind.VOID:
                    next_token();
                    return TypeSpecifier.Void;
                case Token.TokenKind.CHAR:
                    next_token();
                    return TypeSpecifier.Char;
                case Token.TokenKind.INT:
                    next_token();
                    return TypeSpecifier.Int;
                case Token.TokenKind.FLOAT:
                    next_token();
                    return TypeSpecifier.Float;
                case Token.TokenKind.DOUBLE:
                    next_token();
                    return TypeSpecifier.Double;
                case Token.TokenKind.SHORT:
                    next_token();
                    return TypeSpecifier.Short;
                case Token.TokenKind.LONG:
                    next_token();
                    return TypeSpecifier.Long;
                case Token.TokenKind.SIGNED:
                    next_token();
                    return TypeSpecifier.Signed;
                case Token.TokenKind.UNSIGNED:
                    next_token();
                    return TypeSpecifier.Unsigned;
                default:
                    throw new Exception();
            }
        }
        private bool is_struct_or_union_specifier() {
            return Peek(Token.TokenKind.STRUCT, Token.TokenKind.UNION);
        }
        private bool is_enum_specifier() {
            return Peek(Token.TokenKind.ENUM);
        }
        private bool is_TYPEDEF_NAME() {
            return current_token().kind == Token.TokenKind.TYPE_NAME;
        }

        private bool is_typedefed_type(string v) {
            return typedef_scope.ContainsKey(v);
        }

        private bool is_type_qualifier() {
            return Peek(Token.TokenKind.CONST, Token.TokenKind.VOLATILE, Token.TokenKind.RESTRICT, Token.TokenKind.NEAR, Token.TokenKind.FAR);
        }
        private TypeQualifier type_qualifier() {
            switch (current_token().kind) {
                case Token.TokenKind.CONST:
                    next_token();
                    return TypeQualifier.Const;
                case Token.TokenKind.VOLATILE:
                    next_token();
                    return TypeQualifier.Volatile;
                case Token.TokenKind.RESTRICT:
                    next_token();
                    return TypeQualifier.Restrict;
                case Token.TokenKind.NEAR:
                    next_token();
                    return TypeQualifier.Near;
                case Token.TokenKind.FAR:
                    next_token();
                    return TypeQualifier.Far;
                default:
                    throw new Exception();
            }
        }

        private bool is_function_specifier() {
            return Peek(Token.TokenKind.INLINE);
        }
        private FunctionSpecifier function_specifier() {
            switch (current_token().kind) {
                case Token.TokenKind.INLINE:
                    next_token();
                    return FunctionSpecifier.Inline;
                default:
                    throw new Exception();
            }
        }

        private int anony = 0;

        private CType struct_or_union_specifier() {
            var struct_or_union = current_token().kind;
            Read(Token.TokenKind.STRUCT, Token.TokenKind.UNION);

            if (is_IDENTIFIER(true)) {
                var ident = IDENTIFIER(true);
                // ctype を tag に 登録
                CType.TaggedType ctype;
                if (Peek('{')) {
                    CType.TaggedType.StructUnionType stype = null;
                    if (tag_scope.TryGetValue(ident, out ctype) == false) {
                        stype = new CType.TaggedType.StructUnionType(struct_or_union == Token.TokenKind.STRUCT ? CType.TaggedType.StructUnionType.StructOrUnion.Struct : CType.TaggedType.StructUnionType.StructOrUnion.Union, ident, false);
                        tag_scope.Add(ident, ctype);
                    } else if (!(ctype is CType.TaggedType.StructUnionType)) {
                        throw new Exception("別のタグ型として定義済み");
                    } else if ((ctype as CType.TaggedType.StructUnionType).struct_declarations != null) {
                        throw new Exception("再定義された");  // 定義は翻訳単位中に一つしか持てない。
                    } else {
                        stype = (ctype as CType.TaggedType.StructUnionType);
                    }
                    tag_scope.Add(ident, stype);
                    Read('{');
                    stype.struct_declarations = struct_declarations();
                    Read('}');
                    ctype = stype;
                } else {
                    if (tag_scope.TryGetValue(ident, out ctype) == false) {
                        ctype = new CType.TaggedType.StructUnionType(struct_or_union == Token.TokenKind.STRUCT ? CType.TaggedType.StructUnionType.StructOrUnion.Struct : CType.TaggedType.StructUnionType.StructOrUnion.Union, ident, false);
                        tag_scope.Add(ident, ctype);
                    }
                }
                return ctype;
            } else {
                var ident = $"${struct_or_union}_{anony++}";
                var ctype = new CType.TaggedType.StructUnionType(struct_or_union == Token.TokenKind.STRUCT ? CType.TaggedType.StructUnionType.StructOrUnion.Struct : CType.TaggedType.StructUnionType.StructOrUnion.Union, ident, true);
                // ctype を tag に 登録
                tag_scope.Add(ident, ctype);
                Read('{');
                ctype.struct_declarations = struct_declarations();
                Read('}');
                return ctype;
            }
        }
        private List<CType.TaggedType.StructUnionType.MemberInfo> struct_declarations() {
            var items = new List<CType.TaggedType.StructUnionType.MemberInfo>();
            items.AddRange(struct_declaration());
            while (is_struct_declaration()) {
                items.AddRange(struct_declaration());
            }
            return items;
        }

        private bool is_struct_declaration() {
            return is_specifier_qualifiers();
        }

        private List<CType.TaggedType.StructUnionType.MemberInfo> struct_declaration() {
            CType baseType = specifier_qualifiers();
            var ret = struct_declarator_list(baseType);
            Read(';');
            return ret;
        }

        private bool is_init_declarator() {
            return is_declarator();
        }

        private AST.Declaration init_declarator(CType ctype, StorageClass storage_class) {
            string ident = "";
            List<CType> stack = new List<CType>() { new CType.StubType() };
            declarator(ref ident, stack, 0);
            ctype = CType.Resolve(ctype, stack);
            AST.Declaration decl;
            if (Peek('=')) {
                if (storage_class == StorageClass.Typedef || storage_class == StorageClass.Extern) {
                    // 初期化式を持つことができない記憶クラス指定子
                    throw new Exception();
                }

                if (ctype.IsFunctionType()) {
                    // 変数じゃない
                    throw new Exception("");
                }
                Read('=');
                var init = initializer();
                decl = new AST.Declaration.VariableDeclaration(ident, ctype, storage_class, init);
            } else if (storage_class == StorageClass.Typedef) {
                var tdecl = new AST.Declaration.TypeDeclaration(ident, ctype);
                decl = tdecl;
                typedef_scope.Add(ident, tdecl);
            } else if (ctype.IsFunctionType()) {
                decl = new AST.Declaration.FunctionDeclaration(ident, ctype, storage_class);
                ident_scope.Add(ident, new IdentifierValue.Declaration(decl));
            } else {
                decl = new AST.Declaration.VariableDeclaration(ident, ctype, storage_class, null);
                ident_scope.Add(ident, new IdentifierValue.Declaration(decl));
            }
            return decl;
        }

        private bool is_specifier_qualifier(CType ctype, TypeSpecifier typeSpecifier) {
            return (
                (is_type_specifier() && ctype == null) ||
                (is_struct_or_union_specifier() && ctype == null) ||
                (is_enum_specifier() && ctype == null) ||
                (is_TYPEDEF_NAME() && ctype == null && typeSpecifier == TypeSpecifier.None) ||
                is_type_qualifier());
        }

        private void specifier_qualifier(ref CType ctype, ref TypeSpecifier typeSpecifier, ref TypeQualifier typeQualifier) {
            if (is_type_specifier()) {
                typeSpecifier = typeSpecifier.Marge(type_specifier());
            } else if (is_struct_or_union_specifier()) {
                if (ctype != null) {
                    throw new Exception("");
                }
                ctype = struct_or_union_specifier();
            } else if (is_enum_specifier()) {
                if (ctype != null) {
                    throw new Exception("");
                }
                ctype = enum_specifier();
            } else if (is_TYPEDEF_NAME()) {
                AST.Declaration.TypeDeclaration value;
                if (typedef_scope.TryGetValue(current_token().raw, out value) == false) {
                    throw new Exception();
                }
                if (ctype != null) {
                    throw new Exception("");
                }
                ctype = new CType.TypedefedType(current_token().raw, value.Ctype);
                next_token();
            } else if (is_type_qualifier()) {
                typeQualifier.Marge(type_qualifier());
            } else {
                throw new Exception("");
            }
        }
        private bool is_specifier_qualifiers() {
            return is_specifier_qualifier(null, TypeSpecifier.None);
        }

        private CType specifier_qualifiers() {
            CType baseType = null;
            TypeSpecifier typeSpecifier = TypeSpecifier.None;
            TypeQualifier typeQualifier = TypeQualifier.None;

            specifier_qualifier(ref baseType, ref typeSpecifier, ref typeQualifier);
            while (is_specifier_qualifier(baseType, typeSpecifier)) {
                specifier_qualifier(ref baseType, ref typeSpecifier, ref typeQualifier);
            }

            if (typeSpecifier != TypeSpecifier.None) {
                if (baseType != null) {
                    throw new Exception("");
                } else {
                    baseType = new CType.BasicType(typeSpecifier);
                }
            } else if (baseType == null) {
                baseType = new CType.BasicType(TypeSpecifier.None);
            }
            if (baseType.IsQualifiedType()) {
                (baseType as CType.TypeQualifierType).type_qualifier |= typeQualifier;
            } else {
                baseType = new CType.TypeQualifierType(baseType, typeQualifier);
            }
            return baseType;
        }


        private List<CType.TaggedType.StructUnionType.MemberInfo> struct_declarator_list(CType ctype) {
            var ret = new List<CType.TaggedType.StructUnionType.MemberInfo>();
            ret.Add(struct_declarator(ctype));
            while (Peek(',')) {
                Read(',');
                ret.Add(struct_declarator(ctype));
            }
            return ret;
        }
        private CType.TaggedType.StructUnionType.MemberInfo struct_declarator(CType ctype) {
            Tuple<string, CType> decl = null;
            string ident = null;
            if (is_declarator()) {
                List<CType> stack = new List<CType>() { new CType.StubType() };
                declarator(ref ident, stack, 0);
                ctype = CType.Resolve(ctype, stack);
            }
            AST.Expression expr = null;
            if (Peek(':')) {
                Read(':');
                expr = constant_expression();
            }
            return new CType.TaggedType.StructUnionType.MemberInfo(ident, ctype, expr == null ? (int?)null : AST.ConstantEval(expr));

        }
        private CType enum_specifier() {
            Read(Token.TokenKind.ENUM);

            if (is_IDENTIFIER(true)) {
                var ident = IDENTIFIER(true);
                var ctype = new CType.TaggedType.EnumType(ident, false);
                // ctype を tag に 登録
                tag_scope.Add(ident, ctype);
                if (Peek('{')) {
                    Read('{');
                    enumerator_list(ctype);
                    Read('}');
                }
                return ctype;
            } else {
                var ident = $"$enum_{anony++}";
                var ctype = new CType.TaggedType.EnumType(ident, true);
                Read('{');
                enumerator_list(ctype);
                Read('}');
                return ctype;
            }
        }
        private void enumerator_list(CType.TaggedType.EnumType ctype) {
            var ret = new List<CType.TaggedType.EnumType.MemberInfo>();
            ctype.enumerator_list = ret;
            var e = enumerator(ctype, 0);
            ident_scope.Add(e.Name, new IdentifierValue.EnumValue(ctype, e.Name));
            ret.Add(e);
            while (Peek(',')) {
                var i = e.Value + 1;
                Read(',');
                if (is_enumerator() == false) {
                    break;
                }
                e = enumerator(ctype, i);
                ident_scope.Add(e.Name, new IdentifierValue.EnumValue(ctype, e.Name));
                ret.Add(e);
            }
        }

        private bool is_enumerator() {
            return is_IDENTIFIER(false);
        }

        private CType.TaggedType.EnumType.MemberInfo enumerator(CType.TaggedType.EnumType ctype, int i) {
            var ident = IDENTIFIER(false);
            if (Peek('=')) {
                Read('=');
                var expr = constant_expression();
                i = AST.ConstantEval(expr);
            }
            return new CType.TaggedType.EnumType.MemberInfo(ctype, ident, i);
        }
        private bool is_declarator() {
            return is_pointer() || is_direct_declarator();
        }
        private void declarator(ref string ident, List<CType> stack, int index) {
            if (is_pointer()) {
                pointer(stack, index);
            }
            direct_declarator(ref ident, stack, index);
        }

        private bool is_direct_declarator() {
            return Peek('(') || is_IDENTIFIER(true);
        }
        private void direct_declarator(ref string ident, List<CType> stack, int index) {
            if (Peek('(')) {
                Read('(');
                stack.Add(new CType.StubType());
                declarator(ref ident, stack, index + 1);
                Read(')');
            } else {
                ident = current_token().raw;
                next_token();
            }
            more_direct_declarator(stack, index);
        }
        private void more_direct_declarator(List<CType> stack, int index) {
            if (Peek('[')) {
                Read('[');
                // array
                int len = -1;
                if (Peek(']') == false) {
                    var expr = constant_expression();
                    len = AST.ConstantEval(expr);
                }
                Read(']');
                stack[index] = new CType.ArrayType(len, stack[index]);
                more_direct_declarator(stack, index);
            } else if (Peek('(')) {
                Read('(');
                if (Peek(')')) {
                    // k&r or ANSI empty parameter list
                    Read(')');
                    stack[index] = new CType.FunctionType(null, false, stack[index]);
                    more_direct_declarator(stack, index);
                } else if (is_identifier_list()) {
                    // K&R parameter name list
                    var args = identifier_list().Select(x => new CType.FunctionType.ArgumentInfo(x, StorageClass.None, (CType)new CType.BasicType(TypeSpecifier.None), null)).ToList();
                    Read(')');
                    stack[index] = new CType.FunctionType(args, false, stack[index]);
                    more_direct_declarator(stack, index);
                } else {
                    // ANSI parameter list
                    bool vargs = false;
                    var args = parameter_type_list(ref vargs);
                    Read(')');
                    stack[index] = new CType.FunctionType(args, vargs, stack[index]);
                    more_direct_declarator(stack, index);

                }
            } else {
                //_epsilon_
            }
        }

        private bool is_pointer() {
            return Peek('*');
        }

        private void pointer(List<CType> stack, int index) {
            Read('*');
            stack[index] = new CType.PointerType(stack[index]);
            TypeQualifier typeQualifier = TypeQualifier.None;
            while (is_type_qualifier()) {
                typeQualifier = typeQualifier.Marge(type_qualifier());
            }
            stack[index] = new CType.TypeQualifierType(stack[index], typeQualifier);

            if (is_pointer()) {
                pointer(stack, index);
            }
        }
        private bool is_parameter_type_list() {
            return is_parameter_declaration();
        }
        private List<CType.FunctionType.ArgumentInfo> parameter_type_list(ref bool vargs) {
            var items = new List<CType.FunctionType.ArgumentInfo>();
            items.Add(parameter_declaration());
            while (Peek(',')) {
                Read(',');
                if (Peek(Token.TokenKind.ELLIPSIS)) {
                    Read(Token.TokenKind.ELLIPSIS);
                    vargs = true;
                    break;
                } else {
                    items.Add(parameter_declaration());
                }
            }
            return items;
        }
        public bool is_parameter_declaration() {
            return is_declaration_specifier(null, TypeSpecifier.None);
        }
        private CType.FunctionType.ArgumentInfo parameter_declaration() {
            StorageClass storageClass;
            CType baseType = declaration_specifiers(out storageClass);

            if (is_declarator_or_abstract_declarator()) {
                string ident = "";
                List<CType> stack = new List<CType>() { new CType.StubType() };
                declarator_or_abstract_declarator(ref ident, stack, 0);
                var ctype = CType.Resolve(baseType, stack);
                return new CType.FunctionType.ArgumentInfo(ident, storageClass, ctype, null);
            } else {
                return new CType.FunctionType.ArgumentInfo((string)null, storageClass, baseType, null);
            }

        }

        private bool is_declarator_or_abstract_declarator() {
            return is_pointer() || is_direct_declarator_or_direct_abstract_declarator();
        }
        private void declarator_or_abstract_declarator(ref string ident, List<CType> stack, int index) {
            if (is_pointer()) {
                pointer(stack, index);
                if (is_direct_declarator_or_direct_abstract_declarator()) {
                    direct_declarator_or_direct_abstract_declarator(ref ident, stack, index);
                }
            } else {
                direct_declarator_or_direct_abstract_declarator(ref ident, stack, index);
            }
        }

        private bool is_direct_declarator_or_direct_abstract_declarator() {
            return is_IDENTIFIER(true) || Peek('(', '[');
        }

        private void direct_declarator_or_direct_abstract_declarator(ref string ident, List<CType> stack, int index) {
            if (is_IDENTIFIER(true)) {
                ident = IDENTIFIER(true);
                more_dd_or_dad(stack, index);
            } else if (Peek('(')) {
                Read('(');
                if (Peek(')')) {
                    // function?
                } else if (is_parameter_type_list()) {
                    // function 
                    // ANSI parameter list
                    bool vargs = false;
                    var args = parameter_type_list(ref vargs);
                } else {
                    stack.Add(new CType.StubType());
                    declarator_or_abstract_declarator(ref ident, stack, index + 1);
                }
                Read(')');
                more_dd_or_dad(stack, index);
            } else if (Peek('[')) {
                Read('[');
                int len = -1;
                if (Peek(']') == false) {
                    var expr = constant_expression();
                    len = AST.ConstantEval(expr);
                }
                Read(']');
                stack[index] = new CType.ArrayType(len, stack[index]);
                more_dd_or_dad(stack, index);
            } else {
                throw new Exception();
            }

        }
        private void more_dd_or_dad(List<CType> stack, int index) {
            if (Peek('(')) {
                Read('(');
                if (Peek(')')) {
                    // function?
                    stack[index] = new CType.FunctionType(null, false, stack[index]);
                } else if (is_parameter_type_list()) {
                    // function 
                    // ANSI parameter list
                    bool vargs = false;
                    var args = parameter_type_list(ref vargs);
                    stack[index] = new CType.FunctionType(args, vargs, stack[index]);
                } else {
                    // K&R parameter name list
                    var args = identifier_list().Select(x => new CType.FunctionType.ArgumentInfo(x, StorageClass.None, (CType)new CType.BasicType(TypeSpecifier.None), null)).ToList();
                    stack[index] = new CType.FunctionType(args, false, stack[index]);
                }
                Read(')');
                more_dd_or_dad(stack, index);
            } else if (Peek('[')) {
                Read('[');
                int len = -1;
                if (Peek(']') == false) {
                    var expr = constant_expression();
                    len = AST.ConstantEval(expr);
                }
                Read(']');
                stack[index] = new CType.ArrayType(len, stack[index]);
                more_dd_or_dad(stack, index);
            } else {
                // _epsilon_
            }
        }

        private bool is_identifier_list() {
            return is_IDENTIFIER(false);
        }

        private List<string> identifier_list() {
            var items = new List<string>();
            items.Add(IDENTIFIER(false));
            while (Peek(',')) {
                Read(',');
                items.Add(IDENTIFIER(false));
            }
            return items;
        }


        private AST.Initializer initializer() {
            if (Peek('{')) {
                Read('{');
                List<AST.Initializer> ret = null;
                if (Peek('}') == false) {
                    ret = initializer_list();
                }
                Read('}');
                return new AST.Initializer.CompilxInitializer(ret);
            } else {
                return new AST.Initializer.SimpleInitializer(assignment_expression());
            }
        }
        private List<AST.Initializer> initializer_list() {
            var ret = new List<AST.Initializer>();
            ret.Add(initializer());
            while (Peek(',')) {
                Read(',');
                if (Peek('}')) {
                    break;
                }
                ret.Add(initializer());
            }
            return ret;
        }

        private bool is_type_name() {
            return is_specifier_qualifiers();
        }

        private CType type_name() {
            CType baseType = specifier_qualifiers();
            if (is_abstract_declarator()) {
                List<CType> stack = new List<CType>() { new CType.StubType() };
                abstract_declarator(stack, 0);
                baseType = CType.Resolve(baseType, stack);
            }
            return baseType;
        }
        private bool is_abstract_declarator() {
            return (is_pointer() || is_direct_abstract_declarator());
        }
        private void abstract_declarator(List<CType> stack, int index) {
            if (is_pointer()) {
                pointer(stack, index);
                if (is_direct_abstract_declarator()) {
                    direct_abstract_declarator(stack, index);
                }
            } else {
                direct_abstract_declarator(stack, index);
            }
        }
        private bool is_direct_abstract_declarator() {
            return Peek('(', '[');
        }

        private void direct_abstract_declarator(List<CType> stack, int index) {
            if (Peek('(')) {
                Read('(');
                if (is_abstract_declarator()) {
                    stack.Add(new CType.StubType());
                    abstract_declarator(stack, index + 1);
                } else if (Peek(')') == false) {
                    // ansi args
                    bool vargs = false;
                    var items = parameter_type_list(ref vargs);
                } else {
                    // k&r or ansi
                }
                Read(')');
                more_direct_abstract_declarator(stack, index);
            } else {
                Read('[');
                int len = -1;
                if (Peek(']') == false) {
                    var expr = constant_expression();
                    len = AST.ConstantEval(expr);
                }
                Read(']');
                stack[index] = new CType.ArrayType(len, stack[index]);
                more_direct_abstract_declarator(stack, index);
            }
        }

        private void more_direct_abstract_declarator(List<CType> stack, int index) {
            if (Peek('[')) {
                Read('[');
                int len = -1;
                if (Peek(']') == false) {
                    var expr = constant_expression();
                    len = AST.ConstantEval(expr);
                }
                Read(']');
                stack[index] = new CType.ArrayType(len, stack[index]);
                more_direct_abstract_declarator(stack, index);
            } else if (Peek('(')) {
                Read('(');
                if (Peek(')') == false) {
                    bool vargs = false;
                    var items = parameter_type_list(ref vargs);
                    stack[index] = new CType.FunctionType(items, vargs, stack[index]);
                } else {
                    stack[index] = new CType.FunctionType(null, false, stack[index]);
                }
                Read(')');
                more_direct_abstract_declarator(stack, index);
            } else {
                // _epsilon_
            }
        }

        private bool is_IDENTIFIER(bool include_type_name) {
            return current_token().kind == Token.TokenKind.IDENTIFIER || (include_type_name && current_token().kind == Token.TokenKind.TYPE_NAME);
        }

        private string IDENTIFIER(bool include_type_name) {
            if (is_IDENTIFIER(include_type_name) == false) {
                throw new Exception();
            }
            var ret = current_token().raw;
            next_token();
            return ret;
        }


        private AST.Statement statement() {
            if ((is_IDENTIFIER(true) && is_nexttoken(':')) || Peek(Token.TokenKind.CASE, Token.TokenKind.DEFAULT)) {
                return labeled_statement();
            } else if (Peek('{')) {
                return compound_statement();
            } else if (Peek(Token.TokenKind.IF, Token.TokenKind.SWITCH)) {
                return selection_statement();
            } else if (Peek(Token.TokenKind.WHILE, Token.TokenKind.DO, Token.TokenKind.FOR)) {
                return iteration_statement();
            } else if (Peek(Token.TokenKind.GOTO, Token.TokenKind.CONTINUE, Token.TokenKind.BREAK, Token.TokenKind.RETURN)) {
                return jump_statement();
            } else if (Peek(Token.TokenKind.__ASM__)) {
                return gnu_asm_statement();
            } else {
                return expression_statement();
            }

        }

        private AST.Statement labeled_statement() {
            if (Peek(Token.TokenKind.CASE)) {
                Read(Token.TokenKind.CASE);
                var expr = constant_expression();
                Read(':');
                var stmt = statement();
                return new AST.Statement.CaseStatement(expr, stmt);
            } else if (Peek(Token.TokenKind.DEFAULT)) {
                Read(Token.TokenKind.DEFAULT);
                Read(':');
                var stmt = statement();
                return new AST.Statement.DefaultStatement(stmt);
            } else {
                var ident = IDENTIFIER(true);
                Read(':');
                var stmt = statement();
                return new AST.Statement.GenericLabeledStatement(ident, stmt);
            }
        }
        private AST.Statement expression_statement() {
            AST.Statement ret;
            if (!Peek(';')) {
                var expr = expression();
                ret = new AST.Statement.ExpressionStatement(expr);
            } else {
                ret = new AST.Statement.EmptyStatement();
            }
            Read(';');
            return ret;
        }
        private AST.Statement compound_statement() {
            tag_scope = tag_scope.Extend();
            ident_scope = ident_scope.Extend();
            Read('{');
            var decls = new List<AST.Declaration>();
            while (is_declaration()) {
                var d = declaration();
                if (d != null) {
                    decls.AddRange(d);
                }
            }
            var stmts = new List<AST.Statement>();
            while (Peek('}') == false) {
                stmts.Add(statement());
            }
            Read('}');
            var stmt = new AST.Statement.CompoundStatement(decls, stmts, tag_scope, ident_scope);
            ident_scope = ident_scope.Parent;
            tag_scope = tag_scope.Parent;
            return stmt;

        }
        private AST.Statement selection_statement() {
            if (Peek(Token.TokenKind.IF)) {
                Read(Token.TokenKind.IF);
                Read('(');
                var cond = expression();
                Read(')');
                var then_stmt = statement();
                AST.Statement else_stmt = null;
                if (Peek(Token.TokenKind.ELSE)) {
                    Read(Token.TokenKind.ELSE);
                    else_stmt = statement();
                }
                return new AST.Statement.IfStatement(cond, then_stmt, else_stmt);
            }
            if (Peek(Token.TokenKind.SWITCH)) {
                Read(Token.TokenKind.SWITCH);
                Read('(');
                var cond = expression();
                Read(')');
                var ss = new AST.Statement.SwitchStatement(cond);
                break_scope.Push(ss);
                ss.Stmt = statement();
                break_scope.Pop();
                return ss;
            }
            throw new Exception();
        }
        private AST.Statement iteration_statement() {
            if (Peek(Token.TokenKind.WHILE)) {
                Read(Token.TokenKind.WHILE);
                Read('(');
                var cond = expression();
                Read(')');
                var ss = new AST.Statement.WhileStatement(cond);
                break_scope.Push(ss);
                continue_scope.Push(ss);
                ss.Stmt = statement();
                break_scope.Pop();
                continue_scope.Pop();
                return ss;
            }
            if (Peek(Token.TokenKind.DO)) {
                Read(Token.TokenKind.DO);
                var ss = new AST.Statement.DoWhileStatement();
                break_scope.Push(ss);
                continue_scope.Push(ss);
                ss.Stmt = statement();
                break_scope.Pop();
                continue_scope.Pop();
                Read(Token.TokenKind.WHILE);
                Read('(');
                ss.Cond = expression();
                Read(')');
                Read(';');
                return ss;
            }
            if (Peek(Token.TokenKind.FOR)) {
                Read(Token.TokenKind.FOR);
                Read('(');

                var init = Peek(';') ? (AST.Expression)null : expression();
                Read(';');
                var cond = Peek(';') ? (AST.Expression)null : expression();
                Read(';');
                var update = Peek(')') ? (AST.Expression)null : expression();
                Read(')');
                var ss = new AST.Statement.ForStatement(init, cond, update);
                break_scope.Push(ss);
                continue_scope.Push(ss);
                ss.Stmt = statement();
                break_scope.Pop();
                continue_scope.Pop();
                return ss;
            }
            throw new Exception();

        }
        private AST.Statement jump_statement() {
            if (Peek(Token.TokenKind.GOTO)) {
                Read(Token.TokenKind.GOTO);
                var label = IDENTIFIER(true);
                Read(';');
                return new AST.Statement.GotoStatement(label);
            }
            if (Peek(Token.TokenKind.CONTINUE)) {
                Read(Token.TokenKind.CONTINUE);
                Read(';');
                return new AST.Statement.ContinueStatement(continue_scope.Peek());
            }
            if (Peek(Token.TokenKind.BREAK)) {
                Read(Token.TokenKind.BREAK);
                Read(';');
                return new AST.Statement.BreakStatement(break_scope.Peek());
            }
            if (Peek(Token.TokenKind.RETURN)) {
                Read(Token.TokenKind.RETURN);
                var expr = Peek(';') ? null : expression();
                //現在の関数の戻り値と型チェック
                Read(';');
                return new AST.Statement.ReturnStatement(expr);
            }
            throw new Exception();
        }
        private AST.Statement gnu_asm_statement() {
            // __ASM__ statement not support!!!!!
            // skil
            Read(Token.TokenKind.__ASM__);
            if (Peek(Token.TokenKind.__VOLATILE__)) {
                Read(Token.TokenKind.__VOLATILE__);
            }
            Read('(');
            Stack<char> parens = new Stack<char>();
            parens.Push('(');
            while (parens.Any()) {
                if (Peek('(', '[')) {
                    parens.Push((char)current_token().kind);
                } else if (Peek(')')) {
                    if (parens.Peek() == '(') {
                        parens.Pop();
                    } else {
                        throw new Exception();
                    }
                } else if (Peek(']')) {
                    if (parens.Peek() == '[') {
                        parens.Pop();
                    } else {
                        throw new Exception();
                    }
                }
                next_token();
            }
            Read(';');
            return new AST.Statement.EmptyStatement();
            ;
        }
        private AST.Expression expression() {
            var e = assignment_expression();
            if (Peek(',')) {
                var ce = new AST.Expression.CommaExpression();
                ce.expressions.Add(e);
                while (Peek(',')) {
                    Read(',');
                    e = assignment_expression();
                    ce.expressions.Add(e);
                }
                return ce;
            } else {
                return e;
            }
        }
        private AST.Expression assignment_expression() {
            var lhs = conditional_expression();
            while (is_assignment_operator()) {
                var op = assignment_operator();
                var rhs = conditional_expression();
                lhs = new AST.Expression.AssignmentExpression(op, lhs, rhs);
            }
            return lhs;

        }

        private bool is_assignment_operator() {
            return Peek((Token.TokenKind)'=', Token.TokenKind.MUL_ASSIGN, Token.TokenKind.DIV_ASSIGN, Token.TokenKind.MOD_ASSIGN, Token.TokenKind.ADD_ASSIGN, Token.TokenKind.SUB_ASSIGN, Token.TokenKind.LEFT_ASSIGN, Token.TokenKind.RIGHT_ASSIGN, Token.TokenKind.AND_ASSIGN, Token.TokenKind.XOR_ASSIGN, Token.TokenKind.OR_ASSIGN);
        }

        private string assignment_operator() {
            if (is_assignment_operator() == false) {
                throw new Exception();
            }
            var ret = current_token().raw;
            next_token();
            return ret;
        }
        private AST.Expression conditional_expression() {
            var cond = logical_OR_expression();
            if (Peek('?')) {
                Read('?');
                var then_expr = expression();
                Read(':');
                var else_expr = conditional_expression();
                return new AST.Expression.ConditionalExpression(cond, then_expr, else_expr);
            } else {
                return cond;
            }
        }
        private AST.Expression constant_expression() {
            // 定数式かどうか調べる必要がある
            return conditional_expression();

        }
        private AST.Expression logical_OR_expression() {
            var lhs = logical_AND_expression();
            while (Peek(Token.TokenKind.OR_OP)) {
                Read(Token.TokenKind.OR_OP);
                var rhs = logical_AND_expression();
                lhs = new AST.Expression.LogicalOrExpression(lhs, rhs);
            }
            return lhs;
        }
        private AST.Expression logical_AND_expression() {
            var lhs = inclusive_OR_expression();
            while (Peek(Token.TokenKind.AND_OP)) {
                Read(Token.TokenKind.AND_OP);
                var rhs = inclusive_OR_expression();
                lhs = new AST.Expression.LogicalAndExpression(lhs, rhs);
            }
            return lhs;
        }
        private AST.Expression inclusive_OR_expression() {
            var lhs = exclusive_OR_expression();
            while (Peek('|')) {
                Read('|');
                var rhs = exclusive_OR_expression();
                lhs = new AST.Expression.InclusiveOrExpression(lhs, rhs);
            }
            return lhs;
        }
        private AST.Expression exclusive_OR_expression() {
            var lhs = and_expression();
            while (Peek('^')) {
                Read('^');
                var rhs = and_expression();
                return lhs = new AST.Expression.ExclusiveOrExpression(lhs, rhs);
            }
            return lhs;
        }
        private AST.Expression and_expression() {
            var lhs = equality_expression();
            while (Peek('&')) {
                Read('&');
                var rhs = equality_expression();
                return lhs = new AST.Expression.AndExpression(lhs, rhs);
            }
            return lhs;
        }
        private AST.Expression equality_expression() {
            var lhs = relational_expression();
            while (Peek(Token.TokenKind.EQ_OP, Token.TokenKind.NE_OP)) {
                var op = current_token().raw;
                next_token();
                var rhs = relational_expression();
                lhs = new AST.Expression.EqualityExpression(op, lhs, rhs);
            }
            return lhs;
        }
        private AST.Expression relational_expression() {
            var lhs = shift_expression();
            while (Peek((Token.TokenKind)'<', (Token.TokenKind)'>', Token.TokenKind.LE_OP, Token.TokenKind.GE_OP)) {
                var op = current_token().raw;
                next_token();
                var rhs = shift_expression();
                lhs = new AST.Expression.RelationalExpression(op, lhs, rhs);
            }
            return lhs;
        }
        private AST.Expression shift_expression() {
            var lhs = additive_expression();
            while (Peek(Token.TokenKind.LEFT_OP, Token.TokenKind.RIGHT_OP)) {
                var op = current_token().raw;
                next_token();
                var rhs = additive_expression();
                lhs = new AST.Expression.ShiftExpression(op, lhs, rhs);
            }
            return lhs;

        }
        private AST.Expression additive_expression() {
            var lhs = multiplicitive_expression();
            while (Peek('+', '-')) {
                var op = current_token().raw;
                next_token();
                var rhs = multiplicitive_expression();
                lhs = new AST.Expression.AdditiveExpression(op, lhs, rhs);
            }
            return lhs;
        }
        private AST.Expression multiplicitive_expression() {
            var lhs = cast_expression();
            while (Peek('*', '/', '%')) {
                var op = current_token().raw;
                next_token();
                var rhs = cast_expression();
                lhs = new AST.Expression.MultiplicitiveExpression(op, lhs, rhs);
            }
            return lhs;
        }
        private AST.Expression cast_expression() {
            if (Peek('(')) {
                // どちらにも'('の出現が許されるためさらに先読みを行う。
                var saveCurrent = current;
                Read('(');
                if (is_type_name()) {
                    var ty = type_name();
                    Read(')');
                    var expr = cast_expression();
                    return new AST.Expression.CastExpression(ty, expr);
                } else {
                    current = saveCurrent;
                    return unary_expression();
                }
            } else {
                return unary_expression();
            }
        }

        private AST.Expression unary_expression() {
            if (Peek(Token.TokenKind.INC_OP, Token.TokenKind.DEC_OP)) {
                var op = current_token().raw;
                next_token();
                var expr = unary_expression();
                return new AST.Expression.UnaryPrefixExpression(op, expr);
            }
            if (Peek('&')) {
                next_token();
                var expr = cast_expression();
                return new AST.Expression.UnaryAddressExpression(expr);
            }
            if (Peek('*')) {
                next_token();
                var expr = cast_expression();
                return new AST.Expression.UnaryReferenceExpression(expr);
            }
            if (Peek('+')) {
                next_token();
                var expr = cast_expression();
                return new AST.Expression.UnaryPlusExpression(expr);
            }
            if (Peek('-')) {
                next_token();
                var expr = cast_expression();
                return new AST.Expression.UnaryMinusExpression(expr);
            }
            if (Peek('~')) {
                next_token();
                var expr = cast_expression();
                return new AST.Expression.UnaryNegateExpression(expr);
            }
            if (Peek('!')) {
                next_token();
                var expr = cast_expression();
                return new AST.Expression.UnaryNotExpression(expr);
            }
            if (Peek(Token.TokenKind.SIZEOF)) {
                next_token();
                if (Peek('(')) {
                    // どっちにも'('が出ることが出来るのでさらに先読みする（LL(2))
                    var saveCurrent = current;
                    Read('(');
                    if (is_type_name()) {
                        if (line == 45581) {

                        }
                        var ty = type_name();
                        Read(')');
                        return new AST.Expression.SizeofTypeExpression(ty);
                    } else {
                        current = saveCurrent;
                        var expr = unary_expression();
                        return new AST.Expression.SizeofExpression(expr);
                    }
                }
            }
            return postfix_expression();
        }

        private AST.Expression postfix_expression() {
            var expr = primary_expression();
            return more_postfix_expression(expr);

        }
        private AST.Expression more_postfix_expression(AST.Expression expr) {
            if (Peek('[')) {
                Read('[');
                var index = expression();
                Read(']');
                return more_postfix_expression(new AST.Expression.PostfixExpression.ArraySubscriptingExpression(expr, index));
            }
            if (Peek('(')) {
                Read('(');
                List<AST.Expression> args = null;
                if (Peek(')') == false) {
                    args = argument_expression_list();
                } else {
                    args = new List<AST.Expression>();
                }
                Read(')');
                return more_postfix_expression(new AST.Expression.PostfixExpression.FunctionCallExpression(expr, args));
            }
            if (Peek('.')) {
                Read('.');
                var ident = IDENTIFIER(false);
                return more_postfix_expression(new AST.Expression.PostfixExpression.MemberDirectAccess(expr, ident));
            }
            if (Peek(Token.TokenKind.PTR_OP)) {
                Read(Token.TokenKind.PTR_OP);
                var ident = IDENTIFIER(false);
                return more_postfix_expression(new AST.Expression.PostfixExpression.MemberIndirectAccess(expr, ident));
            }
            if (Peek(Token.TokenKind.INC_OP, Token.TokenKind.DEC_OP)) {
                var op = current_token().raw;
                next_token();
                return more_postfix_expression(new AST.Expression.PostfixExpression.UnaryPostfixExpression(op, expr));
            }
            return expr;
        }
        private AST.Expression primary_expression() {
            if (is_IDENTIFIER(false)) {
                var ident = IDENTIFIER(false);
                IdentifierValue value;
                if (ident_scope.TryGetValue(ident, out value) == false) {
                    //throw new Exception("未宣言");
                    return new AST.Expression.PrimaryExpression.IdentifierExpression.UndefinedIdentifierExpression(ident);
                }
                if (value.IsVariable()) {
                    return new AST.Expression.PrimaryExpression.IdentifierExpression.VariableExpression(ident, value.ToVariable());
                }
                if (value.IsEnumValue()) {
                    var ev = value.ToEnumValue();
                    return new AST.Expression.PrimaryExpression.IdentifierExpression.EnumerationConstant(ev);
                }
                if (value.IsFunction()) {
                    return new AST.Expression.PrimaryExpression.IdentifierExpression.FunctionExpression(ident, value.ToFunction());
                }
                throw new Exception("");
            }
            if (is_constant()) {
                return constant();
            }
            if (is_STRING()) {
                List<string> strings = new List<string>();
                while (is_STRING()) {
                    strings.Add(STRING());
                }
                return new AST.Expression.PrimaryExpression.StringExpression(strings);
            }
            if (Peek('(')) {
                Read('(');
                if (Peek('{')) {
                    // gcc statement expression
                    var statements = compound_statement();
                    Read(')');
                    return new AST.Expression.GccStatementExpression(statements);
                } else {
                    var expr = new AST.Expression.PrimaryExpression.EnclosedInParenthesesExpression(expression());
                    Read(')');
                    return expr;
                }
            }
            throw new Exception();
        }

        private List<AST.Expression> argument_expression_list() {
            var ret = new List<AST.Expression>();
            ret.Add(assignment_expression());
            while (Peek(',')) {
                Read(',');
                ret.Add(assignment_expression());
            }
            return ret;
        }

        private bool is_constant() {
            return is_INTEGER_CONSTANT() ||
                   is_CHARACTER_CONSTANT() ||
                   is_FLOATING_CONSTANT() ||
                   is_ENUMERATION_CONSTANT();
        }

        private AST.Expression constant() {
            if (is_INTEGER_CONSTANT()) {
                return INTEGER_CONSTANT();
            }
            if (is_CHARACTER_CONSTANT()) {
                var ret = CHARACTER_CONSTANT();
                return new AST.Expression.PrimaryExpression.ConstantExpression.CharacterConstant(ret);
            }
            if (is_FLOATING_CONSTANT()) {
                return FLOATING_CONSTANT();
            }
            if (is_ENUMERATION_CONSTANT()) {
                var ret = ENUMERATION_CONSTANT();
                return new AST.Expression.PrimaryExpression.IdentifierExpression.EnumerationConstant(ret);
            }
            throw new Exception();
        }

    }

}

