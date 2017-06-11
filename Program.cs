using System;
using System.Text;
using System.Collections;
using System.Data;
using System.Diagnostics;
using System.IO;

namespace MiniML {
    class Program {
        static void Main(string[] args) {
            MiniML.main.run();
        }
    }

    public static partial class MiniML {
        public static class main {
            public static void read_eval_print(Environment<ExprValue> env) {
                for (;;) {
                    Console.Write("# ");
                    var decl = Parser.Parse(Console.ReadLine());
                    if (decl.Success) {
                        var ret = Eval.eval_decl(env, decl.Value);
                        Console.WriteLine($"val {ret.Id} = {ret.Value}");
                        env = ret.Env;
                    } else {
                        Console.WriteLine($"Syntax error.");
                    }
                }
            }
            public static void file_eval_print(Environment<ExprValue> env, TextReader tr) {
                for (;;) {
                    var decl = Parser.Parse(tr.ReadLine());
                    if (decl.Success) {
                        var ret = Eval.eval_decl(env, decl.Value);
                        env = ret.Env;
                    } else {
                        Console.WriteLine($"Syntax error.");
                    }
                }
            }
            public static Environment<ExprValue> initial_env() {

                return Environment.Extend(
                    "i",
                    new ExprValue.IntV(1),
                    Environment.Extend(
                        "Value",
                        new ExprValue.IntV(5),
                        Environment.Extend(
                            "x",
                            new ExprValue.IntV(10),
                            Environment<ExprValue>.Empty
                        )
                    )
                );
            }

            public static void run() {
                read_eval_print(initial_env());
            }
        }
    }
}
