using System;
using System.Net.Configuration;
using System.Security.Cryptography.X509Certificates;
using MiniMAL;

namespace MiniMAL {

    /// <summary>
    /// 対話実行環境
    /// </summary>
    public static class REPL {
        public static void EvalRun() {
            var env = Environment<AbstractSyntaxTreeInterpreter.ExprValue>.Empty;
            //var tyenv = Environment<Typing.MonomorphicTyping>.Empty;
            var tyenv = Environment<Typing.PolymorphicTyping.TypeScheme>.Empty;
            // load init.miniml
            string init = "init.miniml2";
            if (System.IO.File.Exists(init)) {
                using (System.IO.TextReader tr = new System.IO.StreamReader(init)) {
                    Parsing.Source source = new Parsing.Source(init, tr);
                    while (!source.EOS) {
                        var decl = Parser.Parse(source);
                        if (decl.Success) {
                            try {
                                //var ty = Typing.MonomorphicTyping.eval_decl(tyenv, decl.Value);
                                var ty = Typing.PolymorphicTyping.eval_decl(tyenv,  decl.Value);

                                var ret = AbstractSyntaxTreeInterpreter.eval_decl(env, decl.Value);
                                env = ret.Env;
                                tyenv = ty.Env;
                            } catch (Exception e) {
                                Console.Error.WriteLine($"{init}: Runtime error: {e.Message}");
                            }
                        } else {
                            Console.Error.WriteLine($"{init}: Syntax error on line {decl.FailedPosition.Row} column {decl.FailedPosition.Column}.");
                        }
                        source.Discard(decl.Position.Index);
                    }
                }
            }
            // repl
            {
                Parsing.Source source = new Parsing.Source("<stdin>", Console.In);
                while (!source.EOS) {
                    Console.Write("# ");
                    var decl = Parser.Parse(source);
                    if (decl.Success) {
                        try {
                            Console.WriteLine($"expr is {decl.Value}");
                            //var ty = Typing.MonomorphicTyping.eval_decl(tyenv, decl.Value);
                            var ty = Typing.PolymorphicTyping.eval_decl(tyenv,  decl.Value);
                            var ret = AbstractSyntaxTreeInterpreter.eval_decl(env, decl.Value);
                            env = ret.Env;
                            tyenv = ty.Env;
                            Console.WriteLine($"val {ret.Id} : {ty.Value} = {ret.Value}");
                        } catch (Exception e) {
                            Console.Error.WriteLine($"<stdin>: Runtime error: {e.Message}");
                            //Console.Error.WriteLine($"{e.StackTrace}");
                        }
                    } else {
                        Console.Error.WriteLine(
                            $"<stdin>: Syntax error on line {decl.FailedPosition.Row} column {decl.FailedPosition.Column}.");
                    }
                    source.Discard(decl.Position.Index);
                }
            }
        }
        public static void VMRun() {
            var envname = LinkedList<LinkedList<string>>.Empty;
            var envvalue = LinkedList<LinkedList<SecdMachineInterpreter.ExprValue>>.Empty;
            var tyenv = Environment<Typing.PolymorphicTyping.TypeScheme>.Empty;
            var typedefenv = Environment<Typing.Type>.Empty;

            // repl
            Parsing.Source source = new Parsing.Source("<stdin>", Console.In);
            while (!source.EOS) {
                Console.Write("# ");
                var decl = Parser.Parse(source);
                if (decl.Success) {
                    try {
                        Console.WriteLine($"expr is {decl.Value}");
                        var ty = Typing.PolymorphicTyping.eval_decl(tyenv,  decl.Value);
                        var compileret = SecdMachineInterpreter.CompileDecl(decl.Value, envname);
                        var code = compileret.Item1;
                        envname = compileret.Item2;
                        tyenv = ty.Env;
                        Console.WriteLine($"Compiled instruction = ");
                        foreach (var c in code) {
                            Console.WriteLine($"{c}");
                        }
                        foreach (var c in code) {
                            var ret = SecdMachineInterpreter.Run(c, envvalue);
                            if (ret.Item1 != null) {
                                Console.WriteLine($"val - : {ty.Value} = {ret.Item1}");
                            } else {
                                var namee = envname.Value;
                                var vale = ret.Item2.Value;
                                while (namee != LinkedList<string>.Empty) {
                                    Console.WriteLine($"val {namee.Value} = {vale.Value}");
                                    namee = namee.Next;
                                    vale = vale.Next;
                                }
                            }
                            envvalue = ret.Item2;
                        }
                    } catch (Exception e) {
                        Console.Error.WriteLine($"<stdin>: Runtime error: {e.Message}");
                        Console.Error.WriteLine($"{e.StackTrace}");
                    }
                } else {
                    Console.Error.WriteLine($"<stdin>: Syntax error on line {decl.FailedPosition.Row} column {decl.FailedPosition.Column}.");
                }
                source.Discard(decl.Position.Index);
            }
        }
    }
}
