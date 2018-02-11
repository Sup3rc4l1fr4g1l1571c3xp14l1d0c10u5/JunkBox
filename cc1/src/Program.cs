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

            args = new CommandLineOptionsParser()
                .Entry("-o", 1, (s) => {
                    outputFile = s[0];
                    return true;
                })
                .Entry("-ast", 1, (s) => {
                    astFile = s[0];
                    return true;
                })
                .Parse(args);

            if (args.Length == 0) {
                Logger.Error("コンパイル対象のCソースファイルを１つ指定してください。");
                Environment.Exit(-1);
            } else if (args.Length > 1) {
                Logger.Error("コンパイル対象のCソースファイルが２つ以上指定されています。");
                Environment.Exit(-1);
            } else {
                var arg = args[0];

                if (System.IO.File.Exists(arg) == false) {
                    Logger.Error($"{arg}がみつかりません。");
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

                    using (var o = new System.IO.StreamWriter(outputFile)) {
                        var v = new SyntaxTreeCompileVisitor.Value();
                        var visitor = new SyntaxTreeCompileVisitor();
                        ret.Accept(visitor, v);
                        visitor.WriteCode(o);
                    }
                } catch (Exception e) {
                    Logger.Error(e.Message);
                    Logger.Error(e.StackTrace);
                    Environment.Exit(-1);
                }
            }

        }

        static void DebugMain(string[] args) {
            var ret = new Parser(System.IO.File.ReadAllText(@"..\..\algo\tmp\delta2.i")).Parse();
            Console.WriteLine(Cell.PrettyPrint(ret.Accept(new SyntaxTreeDumpVisitor(), null)));

            var v = new SyntaxTreeCompileVisitor.Value();
            using (var o = new System.IO.StreamWriter(@"..\..\algo\test.s")) {
                var visitor = new SyntaxTreeCompileVisitor();
                ret.Accept(visitor, v);
                visitor.WriteCode(o);
            }
        }
    }
}
