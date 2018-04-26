using System;
using System.Diagnostics;

namespace AnsiCParser {
    class Program {

        static void Main(string[] args) {
            if (Debugger.IsAttached == false) {
                CommonMain(args);
            } else {
                DebugMain(args);
            }
        }

        static void CommonMain(string[] args) {
            string outputFile = null;
            string astFile = null;
            bool flag_SyntaxOnly = false;
            string outputEncoding = null;

            args = new CommandLineOptionsParser()
                .Entry("-o", 1, (s) => {
                    outputFile = s[0];
                    return true;
                })
                .Entry("-ast", 1, (s) => {
                    astFile = s[0];
                    return true;
                })
                .Entry("-console-output-encoding", 1, (s) => {
                    outputEncoding = s[0];
                    return true;
                })
                .Entry("-fsyntax-only", 0, (s) => {
                    flag_SyntaxOnly = true;
                    return true;
                })
                .Parse(args);

            
            try {
                if (outputEncoding != null) {
                    Console.OutputEncoding = System.Text.Encoding.GetEncoding(outputEncoding);
                }
            } catch {
                Logger.Error($"指定されたエンコーディング ${outputEncoding}は不正です。");
                Environment.Exit(-1);
            }


            if (args.Length == 0) {
                Logger.Error("コンパイル対象のCソースファイルを１つ指定してください。");
                Environment.Exit(-1);
            } else if (args.Length > 1) {
                Logger.Error("コンパイル対象のCソースファイルが２つ以上指定されています。");
                Environment.Exit(-1);
            } else {
                var arg = args[0];

                if (System.IO.File.Exists(arg) == false) {
                    Logger.Error($"ファイル {arg} が見つかりません。処理を中止します。");
                    Environment.Exit(-1);
                }
                if (outputFile == null) {
                    outputFile = System.IO.Path.ChangeExtension(arg, "s");
                }
                if (astFile == null) {
                    astFile = System.IO.Path.ChangeExtension(arg, "ast");
                }
                try {
                    var ret = new Parser(System.IO.File.ReadAllText(arg)).Parse();
                    using (var o = new System.IO.StreamWriter(astFile)) {
                        o.WriteLine(Cell.PrettyPrint(ret.Accept(new SyntaxTreeDumpVisitor(), null)));
                    }

                    if (flag_SyntaxOnly == false) {
                        using (var o = new System.IO.StreamWriter(outputFile)) {
                            var v = new SyntaxTreeCompileVisitor.Value();
                            var visitor = new SyntaxTreeCompileVisitor();
                            ret.Accept(visitor, v);
                            visitor.WriteCode(o);
                        }
                    }
                } catch (Exception e) {
                    if (e is CompilerException) {
                        var ce = e as CompilerException;
                        Logger.Error(ce.Start, ce.End, ce.Message);
                    } else {
                        Logger.Error(e.Message);
                    }
                    Logger.Error(e.StackTrace);
                    Environment.Exit(-1);
                }
            }

        }

        static void DebugMain(string[] args) {
            var ret = new Parser(System.IO.File.ReadAllText(@"..\..\test.c")).Parse();
            Console.WriteLine(Cell.PrettyPrint(ret.Accept(new SyntaxTreeDumpVisitor(), null)));

            var v = new SyntaxTreeCompileVisitor.Value();
            using (var o = new System.IO.StreamWriter(@"..\..\test.s")) {
                var visitor = new SyntaxTreeCompileVisitor();
                ret.Accept(visitor, v);
                visitor.WriteCode(o);
            }
        }
    }
}
