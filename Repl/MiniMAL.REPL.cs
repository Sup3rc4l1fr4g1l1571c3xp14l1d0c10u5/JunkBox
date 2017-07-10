using System;
using System.Collections.Generic;
using System.Linq;
using MiniMAL.Interpreter;
using MiniMAL.Syntax;
using Parsing;

namespace MiniMAL
{

    /// <summary>
    /// 対話実行環境
    /// </summary>
    public static class REPL
    {
        public static class AbstractSyntaxTreeInterpreterRepl
        {

            public static AbstractSyntaxTreeInterpreter.ExprValue.BProcV Call<T>(
                Func<T, AbstractSyntaxTreeInterpreter.ExprValue> body)
                where T : AbstractSyntaxTreeInterpreter.ExprValue
            {
                return new AbstractSyntaxTreeInterpreter.ExprValue.BProcV((x) =>
                {
                    var arg = (T)x;
                    return body(arg);
                });
            }

            public static AbstractSyntaxTreeInterpreter.ExprValue.BProcV Call<T1, T2>(
                Func<T1, T2, AbstractSyntaxTreeInterpreter.ExprValue> body)
                where T1 : AbstractSyntaxTreeInterpreter.ExprValue
                where T2 : AbstractSyntaxTreeInterpreter.ExprValue
            {
                return new AbstractSyntaxTreeInterpreter.ExprValue.BProcV((x) =>
                {
                    var arg = x as AbstractSyntaxTreeInterpreter.ExprValue.TupleV;
                    var lhs = (T1)arg.Members[0];
                    var rhs = (T2)arg.Members[1];
                    return body(lhs, rhs);
                });
            }

            public static void Run()
            {
                var builtins = new Dictionary<string, AbstractSyntaxTreeInterpreter.ExprValue.BProcV>();
                builtins["add"] =
                    Call(
                        (AbstractSyntaxTreeInterpreter.ExprValue.IntV lhs,
                                AbstractSyntaxTreeInterpreter.ExprValue.IntV rhs) =>
                                new AbstractSyntaxTreeInterpreter.ExprValue.IntV(lhs.Value + rhs.Value));
                builtins["sub"] =
                    Call(
                        (AbstractSyntaxTreeInterpreter.ExprValue.IntV lhs,
                                AbstractSyntaxTreeInterpreter.ExprValue.IntV rhs) =>
                                new AbstractSyntaxTreeInterpreter.ExprValue.IntV(lhs.Value - rhs.Value));
                builtins["mul"] =
                    Call(
                        (AbstractSyntaxTreeInterpreter.ExprValue.IntV lhs,
                                AbstractSyntaxTreeInterpreter.ExprValue.IntV rhs) =>
                                new AbstractSyntaxTreeInterpreter.ExprValue.IntV(lhs.Value * rhs.Value));
                builtins["div"] =
                    Call(
                        (AbstractSyntaxTreeInterpreter.ExprValue.IntV lhs,
                                AbstractSyntaxTreeInterpreter.ExprValue.IntV rhs) =>
                                new AbstractSyntaxTreeInterpreter.ExprValue.IntV(lhs.Value / rhs.Value));
                builtins["mod"] =
                    Call(
                        (AbstractSyntaxTreeInterpreter.ExprValue.IntV lhs,
                                AbstractSyntaxTreeInterpreter.ExprValue.IntV rhs) =>
                                new AbstractSyntaxTreeInterpreter.ExprValue.IntV(lhs.Value % rhs.Value));
                builtins["lt"] =
                    Call(
                        (AbstractSyntaxTreeInterpreter.ExprValue.IntV lhs,
                                AbstractSyntaxTreeInterpreter.ExprValue.IntV rhs) =>
                                new AbstractSyntaxTreeInterpreter.ExprValue.BoolV(lhs.Value < rhs.Value));
                builtins["le"] =
                    Call(
                        (AbstractSyntaxTreeInterpreter.ExprValue.IntV lhs,
                                AbstractSyntaxTreeInterpreter.ExprValue.IntV rhs) =>
                                new AbstractSyntaxTreeInterpreter.ExprValue.BoolV(lhs.Value <= rhs.Value));
                builtins["gt"] =
                    Call(
                        (AbstractSyntaxTreeInterpreter.ExprValue.IntV lhs,
                                AbstractSyntaxTreeInterpreter.ExprValue.IntV rhs) =>
                                new AbstractSyntaxTreeInterpreter.ExprValue.BoolV(lhs.Value > rhs.Value));
                builtins["ge"] =
                    Call(
                        (AbstractSyntaxTreeInterpreter.ExprValue.IntV lhs,
                                AbstractSyntaxTreeInterpreter.ExprValue.IntV rhs) =>
                                new AbstractSyntaxTreeInterpreter.ExprValue.BoolV(lhs.Value >= rhs.Value));
                builtins["equal"] =
                    Call(
                        (AbstractSyntaxTreeInterpreter.ExprValue lhs,
                                AbstractSyntaxTreeInterpreter.ExprValue rhs) =>
                                new AbstractSyntaxTreeInterpreter.ExprValue.BoolV(Equals(lhs, rhs)));

                builtins["cons"] =
                    Call(
                        (AbstractSyntaxTreeInterpreter.ExprValue lhs,
                                AbstractSyntaxTreeInterpreter.ExprValue.ListV rhs) =>
                                new AbstractSyntaxTreeInterpreter.ExprValue.ListV(lhs, rhs));

                builtins["head"] =
                    Call((AbstractSyntaxTreeInterpreter.ExprValue.ListV arg) => arg.Value);

                builtins["tail"] =
                    Call((AbstractSyntaxTreeInterpreter.ExprValue.ListV arg) => arg.Next);

                builtins["get"] =
                    Call((AbstractSyntaxTreeInterpreter.ExprValue.OptionV arg) =>
                    {
                        if (arg == AbstractSyntaxTreeInterpreter.ExprValue.OptionV.None)
                        {
                            throw new Exception.InvalidArgumentTypeException();
                        }
                        return arg.Value;
                    });

                builtins["field"] =
                    Call((AbstractSyntaxTreeInterpreter.ExprValue.TupleV tuple,
                        AbstractSyntaxTreeInterpreter.ExprValue.IntV index) =>
                    {
                        if (index.Value < 0 || tuple.Members.Length <= index.Value)
                        {
                            throw new Exception.InvalidArgumentTypeException();
                        }
                        return tuple.Members[(int)index.Value];
                    });

                builtins["size"] =
                    Call((AbstractSyntaxTreeInterpreter.ExprValue.TupleV tuple) =>
                        new AbstractSyntaxTreeInterpreter.ExprValue.IntV(tuple.Members.Length));

                builtins["istuple"] =
                    Call((AbstractSyntaxTreeInterpreter.ExprValue arg) =>
                        new AbstractSyntaxTreeInterpreter.ExprValue.BoolV(arg is AbstractSyntaxTreeInterpreter.ExprValue.TupleV));

                var context = new AbstractSyntaxTreeInterpreter.Context();
                context = new AbstractSyntaxTreeInterpreter.Context(
                    context.Env,
                    builtins.Aggregate(context.BuiltinEnv, (s, x) => Environment.Extend(x.Key, x.Value, s)), 
                    context.TypingEnv
                );

                // load init.miniml
                var init = "init.minimal";
                if (System.IO.File.Exists(init))
                {
                    using (System.IO.TextReader tr = new System.IO.StreamReader(init))
                    {
                        Parsing.Source source = new Parsing.Source(init, tr);
                        context = Evaluate(context, source);
                    }
                }
                // repl
                { 
                    Source source = new Parsing.Source("<stdin>", Console.In);
                    context = Evaluate(context, source);
                }
            }

            private static AbstractSyntaxTreeInterpreter.Context Evaluate(AbstractSyntaxTreeInterpreter.Context context, Source source)
            {
                while (!source.EOS)
                {
                    Console.Write("# ");
                    var decl = Parser.Parse(source);
                    if (decl.Success)
                    {
                        try
                        {
                            Console.WriteLine($"expr is {decl.Value}");
                            //var ty = Typing.MonomorphicTyping.eval_decl(context.TypingEnv, decl.Value);
                            var ty = Typing.PolymorphicTyping.eval_decl(context.TypingEnv, decl.Value);
                            var ret = AbstractSyntaxTreeInterpreter.eval_decl(context.Env, context.BuiltinEnv, decl.Value);
                            context = new AbstractSyntaxTreeInterpreter.Context(ret.Env, context.BuiltinEnv, ty.Env);
                            Console.WriteLine($"val {ret.Id} : {ty.Value} = {ret.Value}");
                        }
                        catch (Exception e)
                        {
                            Console.Error.WriteLine($"<stdin>: Runtime error: {e.Message}");
                            //Console.Error.WriteLine($"{e.StackTrace}");
                        }
                    }
                    else
                    {
                        Console.Error.WriteLine(
                            $"<stdin>: Syntax error on line {decl.FailedPosition.Row} column {decl.FailedPosition.Column}.");
                    }
                    source.Discard(decl.Position.Index);
                }
                return context;
            }
        }

        public static class SecdMachineInterpreterRepl
        {
            public static SecdMachineInterpreter.ExprValue.BProcV Call<T>(
                Func<T, SecdMachineInterpreter.ExprValue> body)
                where T : SecdMachineInterpreter.ExprValue
            {
                return new SecdMachineInterpreter.ExprValue.BProcV((x) =>
                {
                    var arg = (T)x;
                    return body(arg);
                });
            }

            public static SecdMachineInterpreter.ExprValue.BProcV Call<T1, T2>(
                Func<T1, T2, SecdMachineInterpreter.ExprValue> body)
                where T1 : SecdMachineInterpreter.ExprValue
                where T2 : SecdMachineInterpreter.ExprValue
            {
                return new SecdMachineInterpreter.ExprValue.BProcV((x) =>
                {
                    var arg = x as SecdMachineInterpreter.ExprValue.TupleV;
                    var lhs = (T1)arg.Members[0];
                    var rhs = (T2)arg.Members[1];
                    return body(lhs, rhs);
                });
            }

            public static void Run()
            {

                var builtins = new Dictionary<string, SecdMachineInterpreter.ExprValue.BProcV>();
                builtins["add"] =
                    Call(
                        (SecdMachineInterpreter.ExprValue.IntV lhs,
                                SecdMachineInterpreter.ExprValue.IntV rhs) =>
                                new SecdMachineInterpreter.ExprValue.IntV(lhs.Value + rhs.Value));
                builtins["sub"] =
                    Call(
                        (SecdMachineInterpreter.ExprValue.IntV lhs,
                                SecdMachineInterpreter.ExprValue.IntV rhs) =>
                                new SecdMachineInterpreter.ExprValue.IntV(lhs.Value - rhs.Value));
                builtins["mul"] =
                    Call(
                        (SecdMachineInterpreter.ExprValue.IntV lhs,
                                SecdMachineInterpreter.ExprValue.IntV rhs) =>
                                new SecdMachineInterpreter.ExprValue.IntV(lhs.Value * rhs.Value));
                builtins["div"] =
                    Call(
                        (SecdMachineInterpreter.ExprValue.IntV lhs,
                                SecdMachineInterpreter.ExprValue.IntV rhs) =>
                                new SecdMachineInterpreter.ExprValue.IntV(lhs.Value / rhs.Value));
                builtins["mod"] =
                    Call(
                        (SecdMachineInterpreter.ExprValue.IntV lhs,
                                SecdMachineInterpreter.ExprValue.IntV rhs) =>
                                new SecdMachineInterpreter.ExprValue.IntV(lhs.Value % rhs.Value));
                builtins["lt"] =
                    Call(
                        (SecdMachineInterpreter.ExprValue.IntV lhs,
                                SecdMachineInterpreter.ExprValue.IntV rhs) =>
                                new SecdMachineInterpreter.ExprValue.BoolV(lhs.Value < rhs.Value));
                builtins["le"] =
                    Call(
                        (SecdMachineInterpreter.ExprValue.IntV lhs,
                                SecdMachineInterpreter.ExprValue.IntV rhs) =>
                                new SecdMachineInterpreter.ExprValue.BoolV(lhs.Value <= rhs.Value));
                builtins["gt"] =
                    Call(
                        (SecdMachineInterpreter.ExprValue.IntV lhs,
                                SecdMachineInterpreter.ExprValue.IntV rhs) =>
                                new SecdMachineInterpreter.ExprValue.BoolV(lhs.Value > rhs.Value));
                builtins["ge"] =
                    Call(
                        (SecdMachineInterpreter.ExprValue.IntV lhs,
                                SecdMachineInterpreter.ExprValue.IntV rhs) =>
                                new SecdMachineInterpreter.ExprValue.BoolV(lhs.Value >= rhs.Value));
                builtins["equal"] =
                    Call(
                        (SecdMachineInterpreter.ExprValue lhs,
                                SecdMachineInterpreter.ExprValue rhs) =>
                                new SecdMachineInterpreter.ExprValue.BoolV(SecdMachineInterpreter.ExprValue.Equals(lhs, rhs)));

                builtins["cons"] =
                    Call(
                        (SecdMachineInterpreter.ExprValue lhs,
                                SecdMachineInterpreter.ExprValue.ListV rhs) =>
                                new SecdMachineInterpreter.ExprValue.ListV(lhs, rhs));

                builtins["head"] =
                    Call((SecdMachineInterpreter.ExprValue.ListV arg) => arg.Value);

                builtins["tail"] =
                    Call((SecdMachineInterpreter.ExprValue.ListV arg) => arg.Next);

                builtins["get"] =
                    Call((SecdMachineInterpreter.ExprValue.OptionV arg) =>
                    {
                        if (arg == SecdMachineInterpreter.ExprValue.OptionV.None)
                        {
                            throw new Exception.InvalidArgumentTypeException();
                        }
                        return arg.Value;
                    });

                builtins["field"] =
                    Call((SecdMachineInterpreter.ExprValue.TupleV tuple,
                        SecdMachineInterpreter.ExprValue.IntV index) =>
                    {
                        if (index.Value < 0 || tuple.Members.Length <= index.Value)
                        {
                            throw new Exception.InvalidArgumentTypeException();
                        }
                        return tuple.Members[(int)index.Value];
                    });

                builtins["size"] =
                    Call((SecdMachineInterpreter.ExprValue.TupleV tuple) =>
                        new SecdMachineInterpreter.ExprValue.IntV(tuple.Members.Length));

                builtins["istuple"] =
                    Call((SecdMachineInterpreter.ExprValue arg) =>
                        new SecdMachineInterpreter.ExprValue.BoolV(arg is SecdMachineInterpreter.ExprValue.TupleV));

                var builtinEnv = builtins.Aggregate(Environment<SecdMachineInterpreter.ExprValue.BProcV>.Empty,
                    (s, x) => Environment.Extend(x.Key, x.Value, s));

                var context = new SecdMachineInterpreter.Context();
                context = new SecdMachineInterpreter.Context(
                    context.NameEnv,
                    context.ValueEnv,
                    builtins.Aggregate(context.BuiltinEnv, (s, x) => Environment.Extend(x.Key, x.Value, s)),
                    context.TypingEnv
                );
                // init
                string init = "init.minimal";
                if (System.IO.File.Exists(init))
                {
                    using (System.IO.TextReader tr = new System.IO.StreamReader(init))
                    {
                        Parsing.Source source = new Parsing.Source(init, tr);
                        context = GetValue(context, source);
                    }
                }
                // repl
                {
                    Parsing.Source source = new Parsing.Source("<stdin>", Console.In);
                    context = GetValue(context, source);
                }
            }

            private static SecdMachineInterpreter.Context GetValue(
                SecdMachineInterpreter.Context context, Source source)
            {
                while (!source.EOS)
                {
                    Console.Write("# ");
                    var decl = Parser.Parse(source);
                    if (decl.Success)
                    {
                        try
                        {
                            Console.WriteLine($"expr is {decl.Value}");

                            var ty = Typing.PolymorphicTyping.eval_decl(context.TypingEnv, decl.Value);
                            var typingEnv = ty.Env;

                            var compileret = SecdMachineInterpreter.Compiler.CompileDecl(decl.Value, context.NameEnv);
                            var code = compileret.Item1;
                            var nameEnv = compileret.Item2;

                            context = new SecdMachineInterpreter.Context(
                                nameEnv,
                                context.ValueEnv,
                                context.BuiltinEnv,
                                typingEnv
                            );
                            Console.WriteLine($"Compiled instruction = ");
                            foreach (var c in code)
                            {
                                Console.WriteLine($"{c}");
                            }
                            foreach (var c in code)
                            {
                                var ret = SecdMachineInterpreter.Run(c, context.ValueEnv, context.BuiltinEnv);
                                if (ret.Item1 != null)
                                {
                                    Console.WriteLine($"val - : {ty.Value} = {ret.Item1}");
                                }
                                else
                                {
                                    var namee = nameEnv.Value;
                                    var vale = ret.Item2.Value;
                                    while (namee != LinkedList<string>.Empty)
                                    {
                                        Console.WriteLine($"val {namee.Value} = {vale.Value}");
                                        namee = namee.Next;
                                        vale = vale.Next;
                                    }
                                }
                                var valueEnv = ret.Item2;
                                context = new SecdMachineInterpreter.Context(
                                    context.NameEnv,
                                    valueEnv,
                                    context.BuiltinEnv,
                                    context.TypingEnv
                                );
                            }
                        }
                        catch (Exception e)
                        {
                            Console.Error.WriteLine($"<stdin>: Runtime error: {e.Message}");
                            Console.Error.WriteLine($"{e.StackTrace}");
                        }
                    }
                    else
                    {
                        Console.Error.WriteLine(
                            $"<stdin>: Syntax error on line {decl.FailedPosition.Row} column {decl.FailedPosition.Column}.");
                    }
                    source.Discard(decl.Position.Index);
                }
                return context;
            }
        }
    }
}
