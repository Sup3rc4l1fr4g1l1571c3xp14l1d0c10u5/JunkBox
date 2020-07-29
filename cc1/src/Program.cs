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
                System.IO.File.ReadAllText(@"C:\Users\whelp\Desktop\cc1\TestCase\array-initializer_00001.c") /*
                @"
int x =1;

struct {
    int i;
    int f;
    int a[2];
} s1 = {
    .f=3,
    .i=2,
    .a[1]=9
};

struct {
    int i;
    int f;
    struct { int x; int y; } a[2];
} s2 = {
    .f=3,
    .i=2,
    .a[1]=9,10
};

struct {
    int x;
    int y;
} a1[3] = {1, 2, 3, 4, 5, 6};

struct {
    int x;
    int y;
} a2[3] = {
    {1, 2},
    {3, 4},
    5, 6
};

struct {
  int x;
  int y;
} a3[3] = {
  [2].y=6, [2].x=5,
  [1].y=4, [1].x=3,
  [0].y=2, [0].x=1
};

struct { int a[3]; int b; } w1[] = { 
    [0].a = {1}, 
    [1].a[0] = 2 
};

struct { int a[3]; int b; } w2[] = {
   { { 1, 0, 0 }, 0 },
   { { 2, 0, 0 }, 0 } 
};

int x1 = 1;
int x2 = {1};
int x3 = {1,2,3};
int x4 = {{1},2,3};

int y1[] = {1};
int y2[] = {1,2,3};
int y3[] = {{1},2,3};

int b[100] = {  [98]=98,99,[10] = 10,11,12, [0] = 0,1,2,};


"//*/
                , "<Debug>").Parse();
            using (var o = new System.IO.StreamWriter("debug.ast")) {
                o.WriteLine(DynamicJson.Serialize(ret.Accept(new ToJsonVisitor(), null)));
            }
            using (var o = new System.IO.StreamWriter("debug.s")) {
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
