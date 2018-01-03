using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace AnsiCParser {
    class Program {


        static void Main(string[] args) {
            if (System.Diagnostics.Debugger.IsAttached == false) {
                foreach (var arg in args) {
                    if (System.IO.File.Exists(arg) == false) {
                        Console.Error.WriteLine($"{arg}Ç™Ç›Ç¬Ç©ÇËÇ‹ÇπÇÒÅB");
                        continue;
                    }

                    try {
                        var ret = new Parser(System.IO.File.ReadAllText(arg)).Parse();
                        using (var o = new System.IO.StreamWriter(System.IO.Path.ChangeExtension(arg, "ast"))) {
                            o.WriteLine(Cell.PrettyPrint(ret.Accept(new SyntaxTreeDumpVisitor(), null)));
                        }

                        using (var o = new System.IO.StreamWriter(System.IO.Path.ChangeExtension(arg, "s"))) {
                            var orgOut = Console.Out;
                            Console.SetOut(o);
                            var v = new SyntaxTreeCompileVisitor.Value();
                            ret.Accept(new SyntaxTreeCompileVisitor(), v);
                            Console.SetOut(orgOut);
                        }
                    }
                    catch (Exception e) {
                        Console.Error.WriteLine(e.Message);
                        Console.Error.WriteLine(e.StackTrace);
                    }
                }

                return;
            } else {

                var ret = new Parser(System.IO.File.ReadAllText(@"..\..\tcctest\30_hanoi.c")).Parse();
                Console.WriteLine(Cell.PrettyPrint(ret.Accept(new SyntaxTreeDumpVisitor(), null)));

                var v = new SyntaxTreeCompileVisitor.Value();
                var orgOut = Console.Out;
                //using (var o = new System.IO.StreamWriter(@"C:\cygwin\home\0079595\test.s")) {
                using (var o = new System.IO.StreamWriter(@"..\..\tcctest\test.s")) {
                    Console.SetOut(o);
                    ret.Accept(new SyntaxTreeCompileVisitor(), v);
                    Console.SetOut(orgOut);
                }
                return;
                var tc = new TestCase();
                foreach (var arg in System.IO.Directory.GetFiles(@"..\..\testcase", "*.c")) {
                    tc.AddTest(arg);
                }

                tc.RunTest();
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

