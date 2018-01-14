using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace AnsiCParser {
    class Program {


        static void Main(string[] args) {
            if (System.Diagnostics.Debugger.IsAttached == false) {
                //args = System.IO.Directory.EnumerateFiles(@"..\..\tcctest", "*.c").ToArray();

                CommandLineOptionsParser clop = new CommandLineOptionsParser();

                string output_file = null;
                string ast_file = null;

                clop.Entry("-o", 1, (s) => {
                    output_file = s[0];
                    return true;
                });
                clop.Entry("-ast", 1, (s) => {
                    ast_file = s[0];
                    return true;
                });
                args = clop.Parse(args);
                if (args.Length == 0) {
                    Console.Error.WriteLine("ソースファイルをひとつ指定してください。");
                    System.Environment.Exit(-1);
                    return;
                } else if (args.Length > 1) {
                        Console.Error.WriteLine("ソースファイルが２つ以上指定されています。");
                    System.Environment.Exit(-1);
                }
                var arg = args[0];
                {
                    if (System.IO.File.Exists(arg) == false) {
                        Console.Error.WriteLine($"{arg}がみつかりません。");
                        System.Environment.Exit(-1);
                    }
                    if (output_file == null) {
                        output_file = System.IO.Path.ChangeExtension(arg, "s");
                    }
                    if (ast_file == null) {
                        ast_file = System.IO.Path.ChangeExtension(arg, "ast");
                    }
                    try {
                        var ret = new Parser(System.IO.File.ReadAllText(arg)).Parse();
                        using (var o = new System.IO.StreamWriter(ast_file)) {
                            o.WriteLine(Cell.PrettyPrint(ret.Accept(new SyntaxTreeDumpVisitor(), null)));
                        }

                        using (var o = new System.IO.StreamWriter(output_file)) {
                            var v = new SyntaxTreeCompileVisitor.Value();
                            var visitor = new SyntaxTreeCompileVisitor();
                            ret.Accept(visitor, v);
                            visitor.WriteCode(o);
                        }
                    }
                    catch (Exception e) {
                        Console.Error.WriteLine(e.Message);
                        Console.Error.WriteLine(e.StackTrace);
                        System.Environment.Exit(-1);
                    }
                }

                return;
            } else {

                var ret = new Parser(System.IO.File.ReadAllText(@"..\..\tcctest\00_assignment.c")).Parse();
                Console.WriteLine(Cell.PrettyPrint(ret.Accept(new SyntaxTreeDumpVisitor(), null)));

                var v = new SyntaxTreeCompileVisitor.Value();
                //using (var o = new System.IO.StreamWriter(@"C:\cygwin\home\0079595\test.s")) {
                using (var o = new System.IO.StreamWriter(@"..\..\tcctest\test.s")) {
                    var visitor = new SyntaxTreeCompileVisitor();
                    ret.Accept(visitor, v);
                    visitor.WriteCode(o);
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
}

