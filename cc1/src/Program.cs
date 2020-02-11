using System;
using System.Linq;
using System.Diagnostics;
using AnsiCParser.SyntaxTree;
using Codeplex.Data;

namespace AnsiCParser {
    class Program {

        static void Main(string[] args) {

            //
            // I Do Not Know C. 
            //
            if (!Debugger.IsAttached) {
                CommonMain(args);
            } else {
//                CommonMain(args);
                DebugMain(args);
            }
        }

        private class CommandLineOptions {
            public string OutputFile;
            public string AstFile;
            public bool FlagSyntaxOnly;
            public string OutputEncoding = System.Text.Encoding.Default.WebName;
            public string[] Args = new string[0];

            public void Validation(Action<string> act) {
                // check outputEncoding
                var enc = System.Text.Encoding.GetEncodings().FirstOrDefault(x => String.Equals(x.Name, OutputEncoding, StringComparison.OrdinalIgnoreCase));
                if (enc == null) {
                    act($"指定されたエンコーディング名 {OutputEncoding} は利用可能なエンコーディングに一致しません。");
                } else {
                    Console.OutputEncoding = enc.GetEncoding();
                }

                // check input source
                if (Args.Length == 0) {
                    act("コンパイル対象のCソースファイルを１つ指定してください。");
                //} else if (Args.Length > 1) {
                //    act("コンパイル対象のCソースファイルが２つ以上指定されています。");
                } else {
                    foreach (var arg in Args) {
                        if (System.IO.File.Exists(arg) == false) {
                            act($"ファイル {arg} が見つかりません。処理を中止します。");
                        }
                    }
                }

                // check output
                if (OutputFile == null) {
                    OutputFile = System.IO.Path.ChangeExtension(Args[0], "s");
                }

            }
        }

        static void CommonMain(string[] args) {
            var opts = new CommandLineOptionsParser<CommandLineOptions>()
                .Entry(@"-o", 1, (t, s) => {
                    t.OutputFile = s[0];
                    return true;
                })
                .Entry(@"-ast", 1, (t, s) => {
                    t.AstFile = s[0];
                    return true;
                })
                .Entry(@"-console-output-encoding", 1, (t, s) => {
                    t.OutputEncoding = s[0];
                    return true;
                })
                .Entry(@"-fsyntax-only", 0, (t, s) => {
                    t.FlagSyntaxOnly = true;
                    return true;
                })
                .Default((t, s) => {
                    t.Args = s;
                    return true;
                })
                .Parse(new CommandLineOptions(), args);

            opts.Validation((e) => {
                Logger.Error(e);
                Environment.Exit(-1);
            });

            try {
                if (opts.Args.Length == 1) {
                    var ret = new Parser(System.IO.File.ReadAllText(opts.Args[0]), opts.Args[0]).Parse();
                    if (opts.AstFile != null) {
                        using (var o = new System.IO.StreamWriter(opts.AstFile)) {
                            o.WriteLine(DynamicJson.Serialize(ret.Accept(new ToJsonVisitor(), null)));
                        }
                    }

                    if (opts.FlagSyntaxOnly == false) {
                        using (var o = new System.IO.StreamWriter(opts.OutputFile)) {
                            var compiler = new Compiler();
                            compiler.Compile(ret, o);
                        }
                    }
                } else {
                    foreach (var arg in opts.Args) {
                        var astFile = System.IO.Path.ChangeExtension(System.IO.Path.GetFullPath(arg), "json");
                        var asmFile = System.IO.Path.ChangeExtension(System.IO.Path.GetFullPath(arg), "s");
                        var ret = new Parser(System.IO.File.ReadAllText(arg), arg).Parse();
                        using (var o = new System.IO.StreamWriter(astFile)) {
                            o.WriteLine(DynamicJson.Serialize(ret.Accept(new ToJsonVisitor(), null)));
                        }

                        if (opts.FlagSyntaxOnly == false) {
                            using (var o = new System.IO.StreamWriter(asmFile)) {
                                var compiler = new Compiler();
                                compiler.Compile(ret, o);
                            }
                        }
                    }
                }
            } catch (CompilerException e) {
                Logger.Error(e.Start, e.End, e.Message);
                Logger.Error(e.StackTrace);
                Environment.Exit(-1);
            } catch (Exception e) {
                Logger.Error(e.Message);
                Logger.Error(e.StackTrace);
                Environment.Exit(-1);
            }

        }

        static void DebugMain(string[] args) {
            var ret = new Parser(
                System.IO.File.ReadAllText(@"C:\Users\0079595\Documents\Visual Studio 2015\Projects\cc1\ShivyC\feature_tests\error_array.c") /*
                @"
struct S1 {
	unsigned long  a : 15;
	unsigned short b : 4;
	unsigned short c : 4;
};
"//*/
                , "<Debug>").Parse();
            using (var o = new System.IO.StreamWriter(System.IO.Path.GetTempFileName())) {
                var compiler = new Compiler();
                compiler.Compile(ret, o);
            }
            return;
        }
    }
}


/*
 * C89 Features 
 * - Lexer   : Complete
 * - Parser  : Complete
 * - Library : NO (now use glibc)
 * C99 Features
 * - __func__ : Complete
 * - Line comment : Complete
 * - Mix Statement and Declaration : Complete
 * - Flexible array member : Complete
 * - Variable length array : Not Supported. It's nothing, but harmful. 
 * - Complex numbers : Complete
 * - Imaginary numbers : Complete.
 * - Bool type : Complete.
 * - Long double type: Incomplete
 * 
 */
