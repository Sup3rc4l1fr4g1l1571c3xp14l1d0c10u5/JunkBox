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
#if false
            {
                var generator = new SyntaxTreeCompileVisitor.Generator();
                // a + 1
                generator.push(new SyntaxTreeCompileVisitor.Value() { Kind = SyntaxTreeCompileVisitor.Value.ValueKind.Var, Label = "_a", Offset = 0, Type = CType.CreateFloat() });
                generator.push(new SyntaxTreeCompileVisitor.Value() { Kind = SyntaxTreeCompileVisitor.Value.ValueKind.FloatConst, FloatConst= 3.14, Type = CType.CreateFloat() });
                generator.add(CType.CreateFloat());
                generator.push(new SyntaxTreeCompileVisitor.Value() { Kind = SyntaxTreeCompileVisitor.Value.ValueKind.FloatConst, FloatConst = 2, Type = CType.CreateFloat() });
                generator.add(CType.CreateFloat());

            }
            {
                CTypeDumpVisitor2 vis = new CTypeDumpVisitor2();
                //Console.WriteLine(CType.CreateUnsignedInt().Accept(vis, "a"));
                //Console.WriteLine(CType.CreatePointer(CType.CreateUnsignedInt()).Accept(vis, "a"));
                //Console.WriteLine(CType.CreateArray(10, CType.CreatePointer(CType.CreateUnsignedInt())).Accept(vis, "a"));   // unsigned int *x[10]
                //Console.WriteLine(CType.CreatePointer(CType.CreateArray(10, CType.CreateUnsignedInt())).Accept(vis, "a"));   // unsigned int (*x)[10]
                {
                    var ret2 = new Parser("unsigned int (*x)[10];").Parse();
                    Console.WriteLine((ret2 as SyntaxTree.TranslationUnit).Declarations[0].Type.Accept(vis, (ret2 as SyntaxTree.TranslationUnit).Declarations[0].Ident));
                }
                {
                    var ret2 = new Parser("int a(void);").Parse();
                    Console.WriteLine((ret2 as SyntaxTree.TranslationUnit).Declarations[0].Type.Accept(vis, (ret2 as SyntaxTree.TranslationUnit).Declarations[0].Ident));
                }
                {
                    var ret2 = new Parser("int (*a)(void);").Parse();
                    Console.WriteLine((ret2 as SyntaxTree.TranslationUnit).Declarations[0].Type.Accept(vis, (ret2 as SyntaxTree.TranslationUnit).Declarations[0].Ident));
                }
                {
                    var ret2 = new Parser("int (*a(void))(void);").Parse();
                    Console.WriteLine((ret2 as SyntaxTree.TranslationUnit).Declarations[0].Type.Accept(vis, (ret2 as SyntaxTree.TranslationUnit).Declarations[0].Ident));
                }
                {
                    var ret2 = new Parser("int (*a(void))[10];").Parse();
                    Console.WriteLine((ret2 as SyntaxTree.TranslationUnit).Declarations[0].Type.Accept(vis, (ret2 as SyntaxTree.TranslationUnit).Declarations[0].Ident));
                }
                {
                    var ret2 = new Parser("int (*(*a(void))[10])(void);").Parse();
                    Console.WriteLine((ret2 as SyntaxTree.TranslationUnit).Declarations[0].Type.Accept(vis, (ret2 as SyntaxTree.TranslationUnit).Declarations[0].Ident));
                }
                {
                    var ret2 = new Parser("void (*signal(int sig, void (*func)(int)))(int);").Parse();
                    Console.WriteLine((ret2 as SyntaxTree.TranslationUnit).Declarations[0].Type.Accept(vis, (ret2 as SyntaxTree.TranslationUnit).Declarations[0].Ident));
                }
                {
                    var ret2 = new Parser("struct hoge { int x; double y; } x;").Parse();
                    Console.WriteLine((ret2 as SyntaxTree.TranslationUnit).Declarations[0].Type.Accept(vis, (ret2 as SyntaxTree.TranslationUnit).Declarations[0].Ident));
                }
                {
                    var ret2 = new Parser("typedef int INT32; INT32 x;").Parse();
                    Console.WriteLine((ret2 as SyntaxTree.TranslationUnit).Declarations[0].Type.Accept(vis, (ret2 as SyntaxTree.TranslationUnit).Declarations[0].Ident));
                    Console.WriteLine((ret2 as SyntaxTree.TranslationUnit).Declarations[1].Type.Accept(vis, (ret2 as SyntaxTree.TranslationUnit).Declarations[1].Ident));
                }
                {
                    var ret2 = new Parser("char (*(*x[3])())[5];").Parse();
                    Console.WriteLine((ret2 as SyntaxTree.TranslationUnit).Declarations[0].Type.Accept(vis, (ret2 as SyntaxTree.TranslationUnit).Declarations[0].Ident));
                }
            }

#endif

            var ret = new Parser(System.IO.File.ReadAllText(@"..\..\tcctest\tmp\19_pointer_arithmetic.i")).Parse();
            Console.WriteLine(Cell.PrettyPrint(ret.Accept(new SyntaxTreeDumpVisitor(), null)));

            var v = new SyntaxTreeCompileVisitor.Value();
            using (var o = new System.IO.StreamWriter(@"..\..\tcctest\test.s")) {
                var visitor = new SyntaxTreeCompileVisitor();
                ret.Accept(visitor, v);
                visitor.WriteCode(o);
            }
        }
    }
}
