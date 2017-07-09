using System;

namespace MiniMAL
{
    namespace Interpreter
    {
        public static partial class SecdMachineInterpreter
        {
            /// <summary>
            /// SECDƒ}ƒVƒ“–½—ß
            /// </summary>
            public abstract class Instructions
            {
                public class Ld : Instructions
                {
                    public int Frame { get; }
                    public int Index { get; }

                    public Ld(int frame, int index)
                    {
                        Frame = frame;
                        Index = index;
                    }

                    public override string ToString()
                    {
                        return $"(ld {Frame} {Index})";
                    }
                }

                public class Ldc : Instructions
                {
                    public ExprValue Value { get; }

                    public Ldc(ExprValue value)
                    {
                        Value = value;
                    }

                    public override string ToString()
                    {
                        return $"(ldc {Value})";
                    }

                }

                public class Ldext : Instructions
                {
                    public string Symbol { get; }

                    public Ldext(string symbol)
                    {
                        Symbol = symbol;
                    }

                    public override string ToString()
                    {
                        return $"(ldext \"{Symbol}\")";
                    }

                }

                public class Ldf : Instructions
                {
                    public abstract class Fun
                    {
                    }

                    public class Closure : Fun
                    {
                        public LinkedList<Instructions> Body { get; }

                        public override string ToString()
                        {
                            return $"{Body}";
                        }

                        public Closure(LinkedList<Instructions> body)
                        {
                            Body = body;
                        }
                    }

                    public class Primitive : Fun
                    {
                        public Func<ExprValue, ExprValue> Proc { get; }

                        public override string ToString()
                        {
                            return "#<primitive>";
                        }

                        public Primitive(Func<ExprValue, ExprValue> proc)
                        {
                            Proc = proc;
                        }
                    }

                    public Fun Function { get; }

                    public Ldf(Fun function)
                    {
                        Function = function;
                    }

                    public override string ToString()
                    {
                        return $"(ldf {Function})";
                    }

                }

                public class App : Instructions
                {
                    public int Argn { get; }

                    public App(int argn)
                    {
                        Argn = argn;
                    }

                    public override string ToString()
                    {
                        return $"(app {Argn})";
                    }
                }

                public class Tapp : Instructions
                {
                    public int Argn { get; }

                    public Tapp(int argn)
                    {
                        Argn = argn;
                    }

                    public override string ToString()
                    {
                        return $"(tapp {Argn})";
                    }
                }

                public class Ent : Instructions
                {
                    public int Argn { get; }

                    public Ent(int argn)
                    {
                        Argn = argn;
                    }

                    public override string ToString()
                    {
                        return $"(ent {Argn})";
                    }
                }

                public class Rtn : Instructions
                {
                    public override string ToString()
                    {
                        return "(rtn)";
                    }
                }

                public class Sel : Instructions
                {
                    public LinkedList<Instructions> FalseClosure { get; }
                    public LinkedList<Instructions> TrueClosure { get; }

                    public Sel(LinkedList<Instructions> trueClosure, LinkedList<Instructions> falseClosure)
                    {
                        TrueClosure = trueClosure;
                        FalseClosure = falseClosure;
                    }

                    public override string ToString()
                    {
                        return $"(sel {TrueClosure} {FalseClosure})";
                    }
                }

                public class Selr : Instructions
                {
                    public LinkedList<Instructions> FalseClosure { get; }
                    public LinkedList<Instructions> TrueClosure { get; }

                    public Selr(LinkedList<Instructions> trueClosure, LinkedList<Instructions> falseClosure)
                    {
                        TrueClosure = trueClosure;
                        FalseClosure = falseClosure;
                    }

                    public override string ToString()
                    {
                        return $"(selr {TrueClosure} {FalseClosure})";
                    }
                }

                public class Join : Instructions
                {
                    public override string ToString()
                    {
                        return "(join)";
                    }
                }

                public class Pop : Instructions
                {
                    public override string ToString()
                    {
                        return "(pop)";
                    }
                }

                public class Stop : Instructions
                {
                    public override string ToString()
                    {
                        return "(stop)";
                    }
                }

                public class Halt : Instructions
                {
                    public string Message { get; }

                    public Halt(string message)
                    {
                        Message = message;
                    }

                    public override string ToString()
                    {
                        return $"(Halt \"{Message}\")";
                    }
                }

                public class Tuple : Instructions
                {
                    public int Size { get; }

                    public Tuple(int size)
                    {
                        Size = size;
                    }

                    public override string ToString()
                    {
                        return "(Tuple)";
                    }
                }

                public class Dum : Instructions
                {
                    public override string ToString()
                    {
                        return "(dum)";
                    }
                }

                public class Rap : Instructions
                {
                    public int Argn { get; }

                    public Rap(int argn)
                    {
                        Argn = argn;
                    }

                    public override string ToString()
                    {
                        return $"(rap {Argn})";
                    }
                }

                public class Rent : Instructions
                {
                    public int Argn { get; }

                    public Rent(int argn)
                    {
                        Argn = argn;
                    }

                    public override string ToString()
                    {
                        return $"(rent {Argn})";
                    }
                }

                //public class Bapp : Instructions {
                //    public Expressions.BuiltinOp.Kind Op { get; }
                //    public int Argn { get; }
                //    public Bapp(Expressions.BuiltinOp.Kind op, int argn)
                //    {
                //        Op = op;
                //        Argn = argn;
                //    }
                //    public override string ToString() {
                //        return $"(bapp {Op} {Argn})";
                //    }
                //}

                public class Opti : Instructions
                {
                    public bool None { get; }

                    public Opti(bool none)
                    {
                        None = none;
                    }

                    public override string ToString()
                    {
                        return $"(opti {None})";
                    }
                }

            }
        }
    }
}
