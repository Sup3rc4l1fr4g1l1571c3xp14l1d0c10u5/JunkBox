using System;
using System.Text.RegularExpressions;

namespace AnsiCParser {
    class Program {

        static void Main(string[] args) {
            //args = System.IO.Directory.GetFiles(@"C:\Users\whelp\Documents\Visual Studio 2017\Projects\AnsiCParser\AnsiCParser\test\examples","*.c");
            if (args.Length == 0) {
                var ret = new Parser(@"
double x = 1.0;
int main(void) {
	int x = 2;
	{
		extern double x;
		x = 3.0;
	}
	return 0;
}

").Parse();

                Console.WriteLine(Cell.PrettyPrint(ret.Accept(new SyntaxTreeDumpVisitor(), null)));

                TestCase.RunTest();
            } else {
                var defaultStdout = Console.Out;
                var defaultStderr = Console.Error;
                foreach (var arg in args) {
                    if (System.IO.File.Exists(arg) == false) {
                        Console.WriteLine($"ファイル{arg}が見つかりません。");
                        continue;
                    }
                    using (var tw = new System.IO.StreamWriter(arg + ".log")) {
                        Console.SetOut(tw);
                        Console.SetError(tw);
                        try {
                            new Parser(System.IO.File.ReadAllText(arg)).Parse();
                        } catch (Exception e) {
                            Console.WriteLine($"例外: {e.GetType().Name}");
                            Console.WriteLine($"メッセージ: {e.Message}");
                            Console.WriteLine($"スタックトレース: {e.StackTrace}");
                        }
                        Console.SetOut(defaultStdout);
                        Console.SetError(defaultStderr);
                    }
                }
            }
        }
    }


    public static class Ext {
        public static StorageClassSpecifier Marge(this StorageClassSpecifier self, StorageClassSpecifier other) {
            if (self == StorageClassSpecifier.None) {
                return other;
            } else if (other == StorageClassSpecifier.None) {
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
}

