using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace MiniMAL
{
    /// <summary>
    /// SECDマシンインタプリタ
    /// </summary>
    public static class SecdMachineInterpreter {

        /// <summary>
        /// 評価値
        /// </summary>
        public abstract class ExprValue {
            /// <summary>
            /// 整数値
            /// </summary>
            public class IntV : ExprValue {
                public BigInteger Value { get; }

                public IntV(BigInteger value) {
                    Value = value;
                }

                public override string ToString() {
                    return $"{Value}";
                }
            }

            /// <summary>
            /// 文字列値
            /// </summary>
            public class StrV : ExprValue {
                public string Value { get; }

                public StrV(string value) {
                    Value = value;
                }

                public override string ToString() {
                    return $"\"{Value.Replace("\"", "\\\"")}\"";
                }
            }

            /// <summary>
            /// 論理値
            /// </summary>
            public class BoolV : ExprValue {
                public bool Value { get; }

                public BoolV(bool value) {
                    Value = value;
                }

                public override string ToString() {
                    return $"{Value}";
                }
            }

            /// <summary>
            /// Unit値
            /// </summary>
            public class UnitV : ExprValue {
                public override string ToString() {
                    return "()";
                }
            }

            /// <summary>
            /// レキシカルクロージャー
            /// </summary>
            public class ProcV : ExprValue {
                public string Id { get; }
                public LinkedList<Instructions> Body { get; }
                public LinkedList<LinkedList<ExprValue>> Env { get; }

                public ProcV(string id, LinkedList<Instructions> body, LinkedList<LinkedList<ExprValue>> env) {
                    Id = id;
                    Body = body;
                    Env = env;
                }

                public override string ToString() {
                    return "<fun>";
                }
            }

            /// <summary>
            /// ビルトインクロージャー
            /// </summary>
            public class BProcV : ExprValue {
                public Func<LinkedList<ExprValue>, ExprValue> Proc { get; }

                public BProcV(Func<LinkedList<ExprValue>, ExprValue> proc) {
                    Proc = proc;
                }

                public override string ToString() {
                    return "<bproc>";
                }
            }

            /// <summary>
            /// consセル
            /// </summary>
            public class ConsV : ExprValue {
                public static ConsV Empty { get; } = new ConsV(null, null);
                public ExprValue Value { get; }
                public ConsV Next { get; }

                public ConsV(ExprValue value, ConsV next) {
                    Value = value;
                    Next = next;
                }

                public override string ToString() {
                    StringBuilder sb = new StringBuilder();
                    sb.Append("[");
                    if (this != Empty) {
                        sb.Append($"{Value}");
                        for (var p = Next; p != Empty; p = p.Next) {
                            sb.Append($"; {p.Value}");
                        }
                    }
                    sb.Append("]");
                    return sb.ToString();
                }
            }

            /// <summary>
            /// タプル
            /// </summary>
            public class TupleV : ExprValue {
                public static TupleV Tail { get; } = new TupleV(null, null);
                public ExprValue Car { get; }
                public TupleV Cdr { get; }

                public TupleV(ExprValue car, TupleV cdr) {
                    Car = car;
                    Cdr = cdr;
                }

                public override string ToString() {
                    StringBuilder sb = new StringBuilder();
                    sb.Append("(");
                    var it = this;
                    if (!ReferenceEquals(it, Tail)) {
                        sb.Append($"{it.Car}");
                        it = it.Cdr;
                        while (!ReferenceEquals(it, Tail)) {
                            sb.Append($", {it.Car}");
                            it = it.Cdr;
                        }
                    }
                    sb.Append(")");
                    return sb.ToString();
                }
            }

            /// <summary>
            /// Option値
            /// </summary>
            public class OptionV : ExprValue {
                public static OptionV None { get; } = new OptionV(null);
                public ExprValue Value { get; }

                public OptionV(ExprValue value) {
                    Value = value;
                }

                public override string ToString() {
                    if (this == None)
                    {
                        return "None";
                    } else {
                        return $"Some {Value}";
                    }
                }
            }

            /// <summary>
            /// 比較
            /// </summary>
            /// <param name="arg1"></param>
            /// <param name="arg2"></param>
            /// <returns></returns>
            public static bool Equals(ExprValue arg1, ExprValue arg2) {
                if (arg1 is ExprValue.IntV && arg2 is ExprValue.IntV) {
                    var i1 = ((ExprValue.IntV)arg1).Value;
                    var i2 = ((ExprValue.IntV)arg2).Value;
                    return i1 == i2;
                }
                if (arg1 is ExprValue.StrV && arg2 is ExprValue.StrV) {
                    var i1 = ((ExprValue.StrV)arg1).Value;
                    var i2 = ((ExprValue.StrV)arg2).Value;
                    return i1 == i2;
                }
                if (arg1 is ExprValue.BoolV && arg2 is ExprValue.BoolV) {
                    var i1 = ((ExprValue.BoolV)arg1).Value;
                    var i2 = ((ExprValue.BoolV)arg2).Value;
                    return i1 == i2;
                }
                if (arg1 is ExprValue.UnitV && arg2 is ExprValue.UnitV) {
                    return true;
                }
                if (arg1 is ExprValue.OptionV && arg2 is ExprValue.OptionV)
                {
                    if (arg1 == ExprValue.OptionV.None)
                    {
                        return arg2 == ExprValue.OptionV.None;
                    }
                    else if (arg2 == ExprValue.OptionV.None)
                    {
                        return arg1 == ExprValue.OptionV.None;
                    }
                    else
                    {
                        var i1 = (ExprValue.OptionV) arg1;
                        var i2 = (ExprValue.OptionV) arg2;
                        return Equals(i1.Value, i2.Value);
                    }
                }
                if (arg1 is ExprValue.ConsV && arg2 is ExprValue.ConsV) {
                    var i1 = (ExprValue.ConsV)arg1;
                    var i2 = (ExprValue.ConsV)arg2;
                    while (i1 != ExprValue.ConsV.Empty && i2 != ExprValue.ConsV.Empty) {
                        if (!Equals(i1.Value, i2.Value)) {
                            return false;
                        }
                        i1 = i1.Next;
                        i2 = i2.Next;
                    }
                    return i1 != ExprValue.ConsV.Empty && i2 != ExprValue.ConsV.Empty;
                }
                if (arg1 is ExprValue.TupleV && arg2 is ExprValue.TupleV) {
                    var i1 = (ExprValue.TupleV) arg1;
                    var i2 = (ExprValue.TupleV) arg2;

                    var it1 = i1;
                    var it2 = i2;

                    while (!ReferenceEquals(it1, TupleV.Tail) && !ReferenceEquals(it2, TupleV.Tail)) {
                        if (Equals(it1.Car, it2.Car) == false) {
                            return false;
                        }
                        it1 = it1.Cdr;
                        it2 = it2.Cdr;
                    }

                    return ReferenceEquals(it1, TupleV.Tail) && ReferenceEquals(it2, TupleV.Tail);
                }
                return false;
            }

        }

        /// <summary>
        /// SECDマシン命令
        /// </summary>
        public abstract class Instructions {
            public class Ld : Instructions {
                public int Frame { get; }
                public int Index { get; }
                public Ld(int frame, int index) {
                    Frame = frame;
                    Index = index;
                }
                public override string ToString() {
                    return $"(ld {Frame} {Index})";
                }
            }

            public class Ldc : Instructions {
                public ExprValue Value { get; }

                public Ldc(ExprValue value) {
                    Value = value;
                }
                public override string ToString() {
                    return $"(ldc {Value})";
                }

            }

            public class Ldf : Instructions {
                public abstract class Fun {
                }
                public class Closure : Fun {
                    public LinkedList<Instructions> Body { get; }
                    public override string ToString() {
                        return $"{Body}";
                    }
                    public Closure(LinkedList<Instructions> body) {
                        Body = body;
                    }
                }
                public class Primitive : Fun {
                    public Func<LinkedList<ExprValue>, ExprValue> Proc { get; }
                    public override string ToString() {
                        return "#<primitive>";
                    }
                    public Primitive(Func<LinkedList<ExprValue>, ExprValue> proc) {
                        Proc = proc;
                    }
                }
                public Fun Function { get; }
                public Ldf(Fun function) {
                    Function = function;
                }
                public override string ToString() {
                    return $"(ldf {Function})";
                }

            }

            public class App : Instructions {
                public int Argn { get; }
                public App(int argn) {
                    Argn = argn;
                }
                public override string ToString() {
                    return $"(app {Argn})";
                }
            }

            public class Tapp : Instructions {
                public int Argn { get; }
                public Tapp(int argn) {
                    Argn = argn;
                }
                public override string ToString() {
                    return $"(tapp {Argn})";
                }
            }

            public class Ent : Instructions {
                public int Argn { get; }
                public Ent(int argn) {
                    Argn = argn;
                }
                public override string ToString() {
                    return $"(ent {Argn})";
                }
            }

            public class Rtn : Instructions {
                public override string ToString() {
                    return "(rtn)";
                }
            }

            public class Sel : Instructions {
                public LinkedList<Instructions> FalseClosure { get; }
                public LinkedList<Instructions> TrueClosure { get; }

                public Sel(LinkedList<Instructions> trueClosure, LinkedList<Instructions> falseClosure) {
                    TrueClosure = trueClosure;
                    FalseClosure = falseClosure;
                }
                public override string ToString() {
                    return $"(sel {TrueClosure} {FalseClosure})";
                }
            }

            public class Selr : Instructions {
                public LinkedList<Instructions> FalseClosure { get; }
                public LinkedList<Instructions> TrueClosure { get; }

                public Selr(LinkedList<Instructions> trueClosure, LinkedList<Instructions> falseClosure) {
                    TrueClosure = trueClosure;
                    FalseClosure = falseClosure;
                }
                public override string ToString() {
                    return $"(selr {TrueClosure} {FalseClosure})";
                }
            }

            public class Join : Instructions {
                public override string ToString() {
                    return "(join)";
                }
            }

            public class Pop : Instructions {
                public override string ToString() {
                    return "(pop)";
                }
            }

            public class Stop : Instructions {
                public override string ToString() {
                    return "(stop)";
                }
            }

            public class Halt : Instructions {
                public string Message { get; }
                public Halt(string message) {
                    Message = message;
                }
                public override string ToString() {
                    return $"(Halt \"{Message}\")";
                }
            }

            public class Tuple : Instructions {
                public override string ToString() {
                    return $"(Tuple)";
                }
            }

            public class Dum : Instructions {
                public override string ToString() {
                    return "(dum)";
                }
            }

            public class Rap : Instructions {
                public int Argn { get; }
                public Rap(int argn) {
                    Argn = argn;
                }
                public override string ToString() {
                    return $"(rap {Argn})";
                }
            }

            public class Rent : Instructions {
                public int Argn { get; }
                public Rent(int argn) {
                    Argn = argn;
                }
                public override string ToString() {
                    return $"(rent {Argn})";
                }
            }

            public class Bapp : Instructions {
                public Expressions.BuiltinOp.Kind Op { get; }
                public int Argn { get; }
                public Bapp(Expressions.BuiltinOp.Kind op, int argn)
                {
                    Op = op;
                    Argn = argn;
                }
                public override string ToString() {
                    return $"(bapp {Op} {Argn})";
                }
            }

            public class Opti : Instructions {
                public bool None { get; }

                public Opti(bool none)
                {
                    None = none;
                }


                public override string ToString() {
                    return $"(opti {None})";
                }
            }

        }

        /// <summary>
        /// 組み込み演算子の処理
        /// </summary>
        /// <param name="op"></param>
        /// <param name="x"></param>
        /// <returns></returns>
        private static ExprValue BuiltinProc(Expressions.BuiltinOp.Kind op, LinkedList<ExprValue> x) {
            switch (op) {
                case Expressions.BuiltinOp.Kind.UnaryMinus: {
                    var arg1 = x.Value;

                    if (arg1 is ExprValue.IntV ) {
                        var i1 = ((ExprValue.IntV)arg1).Value;
                        return new ExprValue.IntV(-i1);
                    }
                    throw new Exception.InvalidArgumentTypeException("Both arguments must be integer: -");

                }
                case Expressions.BuiltinOp.Kind.UnaryPlus: {
                    var arg1 = x.Value;

                    if (arg1 is ExprValue.IntV) {
                        var i1 = ((ExprValue.IntV)arg1).Value;
                        return new ExprValue.IntV(-i1);
                    }
                    throw new Exception.InvalidArgumentTypeException("Both arguments must be integer: +");

                    }
                case Expressions.BuiltinOp.Kind.Plus: {
                    var arg2 = x.Value;
                    var arg1 = x.Next.Value;

                    if (arg1 is ExprValue.IntV && arg2 is ExprValue.IntV) {
                        var i1 = ((ExprValue.IntV)arg1).Value;
                        var i2 = ((ExprValue.IntV)arg2).Value;
                        return new ExprValue.IntV(i1 + i2);
                    }
                    throw new Exception.InvalidArgumentTypeException("Both arguments must be integer: +");

                }
                case Expressions.BuiltinOp.Kind.Minus: {
                    var arg2 = x.Value;
                    var arg1 = x.Next.Value;

                    if (arg1 is ExprValue.IntV && arg2 is ExprValue.IntV) {
                        var i1 = ((ExprValue.IntV)arg1).Value;
                        var i2 = ((ExprValue.IntV)arg2).Value;
                        return new ExprValue.IntV(i1 - i2);
                    }
                    throw new Exception.InvalidArgumentTypeException("Both arguments must be integer: -");

                }
                case Expressions.BuiltinOp.Kind.Mult:
                    {
                        var arg2 = x.Value;
                        var arg1 = x.Next.Value;

                        if (arg1 is ExprValue.IntV && arg2 is ExprValue.IntV) {
                            var i1 = ((ExprValue.IntV)arg1).Value;
                            var i2 = ((ExprValue.IntV)arg2).Value;
                            return new ExprValue.IntV(i1 * i2);
                        }
                        throw new Exception.InvalidArgumentTypeException("Both arguments must be integer: *");
                    }
                case Expressions.BuiltinOp.Kind.Div:
                    {
                        var arg2 = x.Value;
                        var arg1 = x.Next.Value;

                        if (arg1 is ExprValue.IntV && arg2 is ExprValue.IntV) {
                            var i1 = ((ExprValue.IntV)arg1).Value;
                            var i2 = ((ExprValue.IntV)arg2).Value;
                            return new ExprValue.IntV(i1 / i2);
                        }
                        throw new Exception.InvalidArgumentTypeException("Both arguments must be integer: /");
                    }
                case Expressions.BuiltinOp.Kind.Lt:
                    {
                        var arg2 = x.Value;
                        var arg1 = x.Next.Value;

                        if (arg1 is ExprValue.IntV && arg2 is ExprValue.IntV) {
                            var i1 = ((ExprValue.IntV)arg1).Value;
                            var i2 = ((ExprValue.IntV)arg2).Value;
                            return new ExprValue.BoolV(i1 < i2);
                        }
                        throw new Exception.InvalidArgumentTypeException("Both arguments must be integer: <");
                    }
                case Expressions.BuiltinOp.Kind.Le:
                    {
                        var arg2 = x.Value;
                        var arg1 = x.Next.Value;

                        if (arg1 is ExprValue.IntV && arg2 is ExprValue.IntV) {
                            var i1 = ((ExprValue.IntV)arg1).Value;
                            var i2 = ((ExprValue.IntV)arg2).Value;
                            return new ExprValue.BoolV(i1 <= i2);
                        }
                        throw new Exception.InvalidArgumentTypeException("Both arguments must be integer: <=");
                    }
                case Expressions.BuiltinOp.Kind.Gt:
                    {
                        var arg2 = x.Value;
                        var arg1 = x.Next.Value;

                        if (arg1 is ExprValue.IntV && arg2 is ExprValue.IntV) {
                            var i1 = ((ExprValue.IntV)arg1).Value;
                            var i2 = ((ExprValue.IntV)arg2).Value;
                            return new ExprValue.BoolV(i1 > i2);
                        }
                        throw new Exception.InvalidArgumentTypeException("Both arguments must be integer: >");
                    }
                case Expressions.BuiltinOp.Kind.Ge:
                    {
                        var arg2 = x.Value;
                        var arg1 = x.Next.Value;

                        if (arg1 is ExprValue.IntV && arg2 is ExprValue.IntV) {
                            var i1 = ((ExprValue.IntV)arg1).Value;
                            var i2 = ((ExprValue.IntV)arg2).Value;
                            return new ExprValue.BoolV(i1 >= i2);
                        }
                        throw new Exception.InvalidArgumentTypeException("Both arguments must be integer: >=");
                    }
                case Expressions.BuiltinOp.Kind.Eq:
                    {
                        var arg2 = x.Value;
                        var arg1 = x.Next.Value;
                        return new ExprValue.BoolV(ExprValue.Equals(arg1, arg2));
                    }
                case Expressions.BuiltinOp.Kind.Ne:
                    {
                        var arg2 = x.Value;
                        var arg1 = x.Next.Value;
                        return new ExprValue.BoolV(!ExprValue.Equals(arg1, arg2));
                    }
                case Expressions.BuiltinOp.Kind.ColCol:
                    {
                        var arg2 = x.Value;
                        var arg1 = x.Next.Value;

                        if (arg2 is ExprValue.ConsV) {
                            var i2 = ((ExprValue.ConsV)arg2);
                            return new ExprValue.ConsV(arg1, i2);
                        }
                        throw new Exception.InvalidArgumentTypeException("Right arguments must be List: ::");
                    }
                case Expressions.BuiltinOp.Kind.Head: {
                    var arg1 = x.Value;

                    if (arg1 is ExprValue.ConsV) {
                        if (arg1 == ExprValue.ConsV.Empty) {
                            throw new Exception.InvalidArgumentTypeException("arguments must be not empty list: Head");
                        }
                            return ((ExprValue.ConsV)arg1).Value;
                    }
                    throw new Exception.InvalidArgumentTypeException("arguments must be List: Head");
                }
                case Expressions.BuiltinOp.Kind.Tail: {
                    var arg1 = x.Value;

                    if (arg1 is ExprValue.ConsV) {
                        if (arg1 == ExprValue.ConsV.Empty) {
                            throw new Exception.InvalidArgumentTypeException("arguments must be not empty list: Tail");
                        }
                            return ((ExprValue.ConsV)arg1).Next;
                    }
                    throw new Exception.InvalidArgumentTypeException("arguments must be List: Head");
                }
                case Expressions.BuiltinOp.Kind.IsCons: {
                    var arg1 = x.Value;

                    return new ExprValue.BoolV(arg1 is ExprValue.ConsV);
                }
                case Expressions.BuiltinOp.Kind.Car: {
                    var arg1 = x.Value;

                    if (arg1 is ExprValue.TupleV) {
                        if (arg1 == ExprValue.TupleV.Tail) {
                            throw new Exception.InvalidArgumentTypeException("arguments must be not empty Tuple: Car");
                        }
                        return ((ExprValue.TupleV)arg1).Car;
                    }
                    throw new Exception.InvalidArgumentTypeException("arguments must be Tuple: Car");
                }

                case Expressions.BuiltinOp.Kind.Cdr: {
                    var arg1 = x.Value;

                    if (arg1 is ExprValue.TupleV) {
                        if (arg1 == ExprValue.TupleV.Tail) {
                            throw new Exception.InvalidArgumentTypeException("arguments must be not empty Tuple: Car");
                        }
                        return ((ExprValue.TupleV)arg1).Cdr;
                    }
                    throw new Exception.InvalidArgumentTypeException("arguments must be Tuple: Car");
                }

                case Expressions.BuiltinOp.Kind.IsTail: {
                    var arg1 = x.Value;
                    return new ExprValue.BoolV(arg1 == ExprValue.TupleV.Tail);
                }
                case Expressions.BuiltinOp.Kind.IsTuple: {
                    var arg1 = x.Value;
                    return new ExprValue.BoolV(arg1 is ExprValue.TupleV);
                }
                case Expressions.BuiltinOp.Kind.Length: {
                    var arg1 = x.Value;
                    if (arg1 is ExprValue.TupleV) {
                        var v1 = arg1 as ExprValue.TupleV;
                        int n = 0;
                        while (!ReferenceEquals(v1,ExprValue.TupleV.Tail)) {
                            n++;
                            v1 = v1.Cdr;
                        }
                        return new ExprValue.IntV(n);
                    }
                    throw new Exception.InvalidArgumentTypeException("arguments must be TupleV: Length");
                }
                case Expressions.BuiltinOp.Kind.IsNone: {
                    var arg1 = x.Value;
                    return new ExprValue.BoolV(arg1 is ExprValue.OptionV && arg1 == ExprValue.OptionV.None);
                }
                case Expressions.BuiltinOp.Kind.IsSome: {
                    var arg1 = x.Value;
                    return new ExprValue.BoolV(arg1 is ExprValue.OptionV && arg1 != ExprValue.OptionV.None);
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(op), op, null);
            }
        }

        /// <summary>
        /// 式のコンパイル
        /// </summary>
        /// <param name="expr">式</param>
        /// <param name="env">名前環境</param>
        /// <param name="code">後続のコード</param>
        /// <param name="isTail"></param>
        /// <returns></returns>
        private static LinkedList<Instructions> CompileExpr(Expressions expr, LinkedList<LinkedList<string>> env, LinkedList<Instructions> code, bool isTail) {
            if (expr is Expressions.IntLit) {
                return LinkedList.Extend(new Instructions.Ldc(new ExprValue.IntV(((Expressions.IntLit) expr).Value)), code);
            }
            if (expr is Expressions.StrLit) {
                return LinkedList.Extend(new Instructions.Ldc(new ExprValue.StrV(((Expressions.StrLit) expr).Value)), code);
            }
            if (expr is Expressions.BoolLit) {
                return LinkedList.Extend(new Instructions.Ldc(new ExprValue.BoolV(((Expressions.BoolLit) expr).Value)), code);
            }
            if (expr is Expressions.EmptyListLit) {
                return LinkedList.Extend(new Instructions.Ldc(ExprValue.ConsV.Empty), code);
            }
            if (expr is Expressions.UnitLit) {
                return LinkedList.Extend(new Instructions.Ldc(new ExprValue.UnitV()), code);
            }
            if (expr is Expressions.TupleExp) {
                var e = (Expressions.TupleExp) expr;
                for (var it = e; it != Expressions.TupleExp.Tail; it = it.Cdr) {
                    code = LinkedList.Extend(new Instructions.Tuple(), code);
                    code = CompileExpr(it.Car, env, code, false);
                }
                code = LinkedList.Extend(new Instructions.Ldc(ExprValue.TupleV.Tail), code);
                return code;
            }
            if (expr is Expressions.Var) {
                var e = (Expressions.Var) expr;
                var frame = 0;
                for (var ev = env; ev != LinkedList<LinkedList<string>>.Empty; ev = ev.Next) {
                    var index = LinkedList.FirstIndex(x => x == e.Id, ev.Value);
                    if (index != -1) {
                        return LinkedList.Extend(new Instructions.Ld(frame, index), code);
                    }
                    frame++;
                }
                throw new Exception.NotBound($"Variable not bound: {e.Id}");
            }
            if (expr is Expressions.IfExp) {
                var e = (Expressions.IfExp) expr;
                if (isTail) {
                    var tclosure = CompileExpr(e.Then, env, LinkedList.Extend(new Instructions.Rtn(), LinkedList<Instructions>.Empty), true);
                    var fclosure = CompileExpr(e.Else, env, LinkedList.Extend(new Instructions.Rtn(), LinkedList<Instructions>.Empty), true);
                    return CompileExpr(e.Cond, env, LinkedList.Extend(new Instructions.Selr(tclosure, fclosure), code), false);
                } else {
                    var tclosure = CompileExpr(e.Then, env, LinkedList.Extend(new Instructions.Join(), LinkedList<Instructions>.Empty), false);
                    var fclosure = CompileExpr(e.Else, env, LinkedList.Extend(new Instructions.Join(), LinkedList<Instructions>.Empty), false);
                    return CompileExpr(e.Cond, env, LinkedList.Extend(new Instructions.Sel(tclosure, fclosure), code), false);
                }

            }
            if (expr is Expressions.BuiltinOp) {
                var e = (Expressions.BuiltinOp) expr;
                code = LinkedList.Extend(new Instructions.Bapp(e.Op, e.Exprs.Length), code);
                return e.Exprs.Aggregate(code, (current, t) => CompileExpr(t, env, current, false));
            }
            if (expr is Expressions.DFunExp) {
                throw new NotSupportedException();
            }
            if (expr is Expressions.FunExp) {
                var e = (Expressions.FunExp) expr;
                var body = CompileExpr(
                    e.Body,
                    LinkedList.Extend(LinkedList.Extend(e.Arg, LinkedList<string>.Empty), env),
                    LinkedList.Extend(new Instructions.Rtn(), LinkedList<Instructions>.Empty),
                    true
                );
                var closure = new Instructions.Ldf.Closure(body);
                code = LinkedList.Extend(new Instructions.Ldf(closure), code);
                return code;
            }
            if (expr is Expressions.AppExp) {
                var e = (Expressions.AppExp) expr;
                code = LinkedList.Extend((code.Value is Instructions.Rtn) ? (Instructions)new Instructions.Tapp(1) : new Instructions.App(1), code);
                code = CompileExpr(e.Fun, env, code,true);
                //code = LinkedList.Extend(new Instruction.Args(1), code);
                code = CompileExpr(e.Arg, env, code, false);
                return code;
            }
            if (expr is Expressions.LetExp) {

                var e = (Expressions.LetExp) expr;

                var newenv = LinkedList<string>.Empty;
                foreach (var bind in e.Binds.Reverse()) {
                    newenv = LinkedList.Extend(bind.Item1, newenv);
                }

                code = LinkedList.Extend(new Instructions.App(e.Binds.Length), code);
                var body = new Instructions.Ldf.Closure(CompileExpr(e.Body, LinkedList.Extend(newenv, env), LinkedList.Extend(new Instructions.Rtn(), LinkedList<Instructions>.Empty), true));
                code = LinkedList.Extend(new Instructions.Ldf(body), code);
                foreach (var bind in e.Binds.Reverse()) {
                    code = CompileExpr(bind.Item2, env, code, false);
                }
                return code;
            }
            if (expr is Expressions.LetRecExp) {
                var e = (Expressions.LetRecExp) expr;

                var newenv = LinkedList<string>.Empty;
                foreach (var bind in e.Binds.Reverse()) {
                    newenv = LinkedList.Extend(bind.Item1, newenv);
                }

                code = LinkedList.Extend(new Instructions.Rap(e.Binds.Length), code);
                var body = new Instructions.Ldf.Closure(CompileExpr(e.Body, LinkedList.Extend(newenv, env), LinkedList.Extend(new Instructions.Rtn(), LinkedList<Instructions>.Empty), false));
                code = LinkedList.Extend(new Instructions.Ldf(body), code);
                foreach (var bind in e.Binds.Reverse()) {
                    code = CompileExpr(bind.Item2, LinkedList.Extend(newenv, env), code, false);
                }
                code = LinkedList.Extend(new Instructions.Dum(), code);
                return code;
            }
            if (expr is Expressions.MatchExp) {
                throw new NotSupportedException();
            }
            if (expr is Expressions.HaltExp) {
                return LinkedList.Extend(new Instructions.Halt(((Expressions.HaltExp) expr).Message), code);
            }
            if (expr is Expressions.OptionExp) {
                if (expr == Expressions.OptionExp.None)
                {
                    return LinkedList.Extend(new Instructions.Ldc(ExprValue.OptionV.None), code);
                }
                else
                {
                    var e = (Expressions.OptionExp) expr;
                    if (e == Expressions.OptionExp.None)
                    {
                        code = LinkedList.Extend(new Instructions.Opti(false), code);
                        code = CompileExpr(e.Expr, env, code, false);
                    }
                    else
                    {
                        code = LinkedList.Extend(new Instructions.Opti(true), code);
                    }
                    return code;

                }
            }

            throw new NotSupportedException($"cannot compile expr: {expr}");
        }

        /// <summary>
        /// 宣言のコンパイル
        /// </summary>
        /// <param name="decl"></param>
        /// <param name="env"></param>
        /// <returns></returns>
        public static Tuple<LinkedList<Instructions>[], LinkedList<LinkedList<string>>> CompileDecl(Toplevel decl, LinkedList<LinkedList<string>> env) {
            if (decl is Toplevel.Exp) {
                var expr = ((Toplevel.Exp) decl).Syntax;
                return Tuple.Create(
                    new[] { CompileExpr(expr, env, LinkedList.Extend(new Instructions.Stop(), LinkedList<Instructions>.Empty), true) },
                    env
                );
            }
            if (decl is Toplevel.Binding) {
                var decls = ((Toplevel.Binding) decl).Entries;

                List<LinkedList<Instructions>> codes = new List<LinkedList<Instructions>>();
                foreach (var d in decls) {
                    if (d is Toplevel.Binding.LetDecl) {
                        var e = d as Toplevel.Binding.LetDecl;

                        var newenv = LinkedList<string>.Empty;
                        foreach (var bind in e.Binds.Reverse()) {
                            newenv = LinkedList.Extend(bind.Item1, newenv);
                        }

                        var code = LinkedList.Extend(new Instructions.Stop(), LinkedList<Instructions>.Empty);
                        code = LinkedList.Extend(new Instructions.Ent(e.Binds.Length), code);
                        foreach (var bind in e.Binds.Reverse()) {
                            code = CompileExpr(bind.Item2, env, code, true);
                        }
                        env = LinkedList.Extend(newenv, env);
                        codes.Add(code);

                        continue;
                    }
                    if (d is Toplevel.Binding.LetRecDecl) {
                        var e = d as Toplevel.Binding.LetRecDecl;

                        var newenv = LinkedList<string>.Empty;
                        foreach (var bind in e.Binds.Reverse()) {
                            newenv = LinkedList.Extend(bind.Item1, newenv);
                        }

                        var code = LinkedList.Extend(new Instructions.Stop(), LinkedList<Instructions>.Empty);
                        code = LinkedList.Extend(new Instructions.Rent(e.Binds.Length), code);
                        foreach (var bind in e.Binds.Reverse()) {
                            code = CompileExpr(bind.Item2, LinkedList.Extend(newenv, env), code, true);
                        }
                        code = LinkedList.Extend(new Instructions.Dum(), code);
                        env = LinkedList.Extend(newenv, env);
                        codes.Add(code);

                        continue;
                    }
                    throw new NotSupportedException($"cannot compile decls: {d}");
                }
                return Tuple.Create(codes.ToArray(), env);
            }
            if (decl is Toplevel.Empty) {
                return Tuple.Create(
                    new LinkedList<Instructions>[0],
                    env
                );
            }
            throw new NotSupportedException($"cannot compile declarations: {decl}");
        }

        /// <summary>
        /// コンパイル
        /// </summary>
        /// <param name="expr">式</param>
        /// <returns>仮想マシン命令列</returns>
        public static LinkedList<Instructions> Compile(Expressions expr) {
            return CompileExpr(expr, LinkedList<LinkedList<string>>.Empty, LinkedList.Extend(new Instructions.Stop(), LinkedList<Instructions>.Empty), true);
        }

        /// <summary>
        /// Dumpレジスタ型
        /// </summary>
        public class DumpInfo {
            public LinkedList<ExprValue> Stack { get; }
            public LinkedList<LinkedList<ExprValue>> Env { get; }
            public LinkedList<Instructions> Code { get; }
            public DumpInfo(
                LinkedList<ExprValue> stack,
                LinkedList<LinkedList<ExprValue>> env,
                LinkedList<Instructions> code

            ) {
                Stack = stack;
                Env = env;
                Code = code;
            }
        }

        /// <summary>
        /// 命令列の実行
        /// </summary>
        /// <param name="code">仮想マシン命令列</param>
        /// <param name="env"></param>
        /// <returns></returns>
        public static Tuple<ExprValue, LinkedList<LinkedList<ExprValue>>> Run(LinkedList<Instructions> code, LinkedList<LinkedList<ExprValue>> env) {
            if (code == null) {
                throw new ArgumentNullException(nameof(code));
            }
            if (env == null) {
                throw new ArgumentNullException(nameof(env));
            }
            LinkedList<ExprValue> currentstack = LinkedList<ExprValue>.Empty;
            LinkedList<LinkedList<ExprValue>> currentenv = env;
            LinkedList<Instructions> currentcode = code;
            LinkedList<DumpInfo> currentdump = LinkedList<DumpInfo>.Empty;


            for (;;) {
                LinkedList<ExprValue> nextstack;
                LinkedList<LinkedList<ExprValue>> nextenv;
                LinkedList<Instructions> nextcode;
                LinkedList<DumpInfo> nextdump;

                if (currentcode.Value is Instructions.Ld) {
                    // ld <i> <j>
                    // E レジスタの <i> 番目のフレームの <j> 番目の要素をスタックに積む
                    var ld = currentcode.Value as Instructions.Ld;
                    var frame = LinkedList.At(currentenv, ld.Frame);
                    var val = LinkedList.At(frame, ld.Index);

                    nextstack = LinkedList.Extend(val, currentstack);
                    nextenv = currentenv;
                    nextcode = currentcode.Next;
                    nextdump = currentdump;
                } else if (currentcode.Value is Instructions.Ldc) {
                    // ldc <const>
                    // 定数 <const> をスタックに積む
                    var ldc = currentcode.Value as Instructions.Ldc;

                    nextstack = LinkedList.Extend(ldc.Value, currentstack);
                    nextenv = currentenv;
                    nextcode = currentcode.Next;
                    nextdump = currentdump;
                } else if (currentcode.Value is Instructions.Ldf) {
                    // ldf <code>
                    // code からクロージャを生成してスタックに積む
                    var ldf = currentcode.Value as Instructions.Ldf;
                    ExprValue closure;
                    if (ldf.Function is Instructions.Ldf.Closure) {
                        var c = ldf.Function as Instructions.Ldf.Closure;
                        closure = new ExprValue.ProcV("", c.Body, currentenv);
                    }
                    else if (ldf.Function is Instructions.Ldf.Primitive) {
                        var p = ldf.Function as Instructions.Ldf.Primitive;
                        closure = new ExprValue.BProcV(p.Proc);
                    } else {
                        throw new NotSupportedException();
                    }

                    nextstack = LinkedList.Extend(closure, currentstack);
                    nextenv = currentenv;
                    nextcode = currentcode.Next;
                    nextdump = currentdump;

                } else if (currentcode.Value is Instructions.App) {
                    // app <n>
                    // スタックに積まれているクロージャと引数を取り出して関数呼び出しを行う
                    var app = currentcode.Value as Instructions.App;
                    var closure = LinkedList.At(currentstack, 0);
                    currentstack = currentstack.Next;

                    var val = LinkedList<ExprValue>.Empty;
                    for (int i = 0; i < app.Argn; i++) {
                        val = LinkedList.Extend(currentstack.Value, val);
                        currentstack = currentstack.Next;
                    }

                    if (closure is ExprValue.ProcV) {
                        var newdump = new DumpInfo(currentstack, currentenv, currentcode.Next);

                        nextstack = LinkedList<ExprValue>.Empty;
                        nextenv = LinkedList.Extend(val, (closure as ExprValue.ProcV).Env);
                        nextcode = (closure as ExprValue.ProcV).Body;
                        nextdump = LinkedList.Extend(newdump, currentdump);
                    } else if (closure is ExprValue.BProcV) {
                        var ret = (closure as ExprValue.BProcV).Proc(val);
                        nextstack = LinkedList.Extend(ret, currentstack);
                        nextenv = currentenv;
                        nextcode = currentcode.Next;
                        nextdump = currentdump;
                    } else {
                        throw new Exception.RuntimeErrorException($"App cannot eval: {closure}.");
                    }
                } else if (currentcode.Value is Instructions.Tapp) {
                    // app <n>
                    // スタックに積まれているクロージャと引数を取り出して関数呼び出しを行う
                    var tapp = currentcode.Value as Instructions.Tapp;
                    var closure = LinkedList.At(currentstack, 0);
                    currentstack = currentstack.Next;

                    var val = LinkedList<ExprValue>.Empty;
                    for (int i = 0; i < tapp.Argn; i++) {
                        val = LinkedList.Extend(currentstack.Value, val);
                        currentstack = currentstack.Next;
                    }

                    if (closure is ExprValue.ProcV) {
                        //var newdump = new DumpInfo(stack, env, code.Next);

                        nextstack = currentstack;
                        nextenv = LinkedList.Extend(val, (closure as ExprValue.ProcV).Env);
                        nextcode = (closure as ExprValue.ProcV).Body;
                        nextdump = currentdump;
                    } else if (closure is ExprValue.BProcV) {
                        var ret = (closure as ExprValue.BProcV).Proc(val);
                        nextstack = LinkedList.Extend(ret, currentstack);
                        nextenv = currentenv;
                        nextcode = currentcode.Next;
                        nextdump = currentdump;
                    } else {
                        throw new NotSupportedException();
                    }
                } else if (currentcode.Value is Instructions.Ent) {
                    // ent <n>
                    // スタックに積まれている引数を取り出して環境を作る
                    var app = currentcode.Value as Instructions.Ent;

                    var val = LinkedList<ExprValue>.Empty;
                    for (int i = 0; i < app.Argn; i++) {
                        val = LinkedList.Extend(currentstack.Value, val);
                        currentstack = currentstack.Next;
                    }

                    nextstack = currentstack;
                    nextenv = LinkedList.Extend(val, currentenv);
                    nextcode = currentcode.Next;
                    nextdump = currentdump;

                } else if (currentcode.Value is Instructions.Rtn) {
                    // rtn
                    // 関数呼び出しから戻る
                    //var rtn = code.Value as Instructions.Rtn;
                    var val = LinkedList.At(currentstack, 0);
                    var prevdump = currentdump.Value;

                    nextstack = LinkedList.Extend(val, prevdump.Stack);
                    nextenv = prevdump.Env;
                    nextcode = prevdump.Code;
                    nextdump = currentdump.Next;
                } else if (currentcode.Value is Instructions.Sel) {
                    // sel <ct> <cf>
                    // スタックトップの値が真ならば <ct> を実行する。偽ならば <cf> を実行する
                    var sel = currentcode.Value as Instructions.Sel;
                    var val = LinkedList.At(currentstack, 0);
                    var cond = (val as ExprValue.BoolV).Value;
                    var newdump = new DumpInfo(null, null, currentcode.Next);

                    nextstack = currentstack.Next;
                    nextenv = currentenv;
                    nextcode = (cond ? sel.TrueClosure : sel.FalseClosure);
                    nextdump = LinkedList.Extend(newdump, currentdump);
                } else if (currentcode.Value is Instructions.Selr) {
                    // selr <ct> <cf>
                    // スタックトップの値が真ならば <ct> を実行する。偽ならば <cf> を実行する
                    // dumpを更新しない
                    var selr = currentcode.Value as Instructions.Selr;
                    var val = LinkedList.At(currentstack, 0);
                    var cond = (val as ExprValue.BoolV).Value;
                    //var newdump = new DumpInfo(null, null, code.Next);

                    nextstack = currentstack.Next;
                    nextenv = currentenv;
                    nextcode = (cond ? selr.TrueClosure : selr.FalseClosure);
                    nextdump = currentdump;
                } else if (currentcode.Value is Instructions.Join) {
                    // join
                    // 条件分岐(sel)から合流する 
                    //var join = code.Value as Instructions.Join;
                    var prevdump = currentdump.Value;

                    nextstack = currentstack;
                    nextenv = currentenv;
                    nextcode = prevdump.Code;
                    nextdump = currentdump.Next;
                } else if (currentcode.Value is Instructions.Pop) {
                    // pop <ct> <cf>
                    // スタックトップの値を取り除く
                    //var pop = code.Value as Instructions.Pop;

                    nextstack = currentstack.Next;
                    nextenv = currentenv;
                    nextcode = currentcode.Next;
                    nextdump = currentdump;
                } else if (currentcode.Value is Instructions.Stop) {
                    // stop
                    // スタックトップの値が真ならば <ct> を実行する。偽ならば <cf> を実行する
                    break;

                    //_nextstack = stack;
                    //_nextenv = env;
                    //_nextcode = code.Next;
                    //_nextdump = dump;
                    //_nextglobal = global;
                } else if (currentcode.Value is Instructions.Halt) {
                    // halt
                    // エラーを生成して停止する
                    throw new Exception.HaltException((currentcode.Value as Instructions.Halt).Message);
                    //break;

                    //_nextstack = stack;
                    //_nextenv = env;
                    //_nextcode = code.Next;
                    //_nextdump = dump;
                    //_nextglobal = global;
                } else if (currentcode.Value is Instructions.Tuple) {
                    // tuple
                    // スタックに積まれている値を2個取り出してタプルを作る
                    var val = new List<ExprValue>();
                    for (int i = 0; i < 2; i++) {
                        val.Add(currentstack.Value);
                        currentstack = currentstack.Next;
                    }
                    System.Diagnostics.Debug.Assert(val[1] is ExprValue.TupleV);
                    var ret = new ExprValue.TupleV(val[0], val[1] as ExprValue.TupleV);

                    nextstack = LinkedList.Extend(ret, currentstack);
                    nextenv = currentenv;
                    nextcode = currentcode.Next;
                    nextdump = currentdump;
                } else if (currentcode.Value is Instructions.Dum) {
                    // dum
                    // 空環境を積む
                    //var dum = code.Value as Instructions.Dum;

                    nextstack = currentstack;
                    nextenv = LinkedList.Extend(LinkedList<ExprValue>.Empty, currentenv);
                    nextcode = currentcode.Next;
                    nextdump = currentdump;
                } else if (currentcode.Value is Instructions.Rap) {
                    // rap <n>
                    // 空環境を引数で置き換える
                    var rap = currentcode.Value as Instructions.Rap;
                    var closure = LinkedList.At(currentstack, 0);
                    currentstack = currentstack.Next;

                    var val = LinkedList<ExprValue>.Empty;
                    for (int i = 0; i < rap.Argn; i++) {
                        val = LinkedList.Extend(currentstack.Value, val);
                        currentstack = currentstack.Next;
                    }

                    if (closure is ExprValue.ProcV) {
                        var newdump = new DumpInfo(currentstack, currentenv, currentcode.Next);
                        currentenv.Replace(val);
                        nextstack = LinkedList<ExprValue>.Empty;
                        nextenv = currentenv;
                        nextcode = (closure as ExprValue.ProcV).Body;
                        nextdump = LinkedList.Extend(newdump, currentdump);
                    } else if (closure is ExprValue.BProcV) {
                        var ret = (closure as ExprValue.BProcV).Proc(val);
                        nextstack = LinkedList.Extend(ret, currentstack);
                        nextenv = currentenv;
                        nextcode = currentcode.Next;
                        nextdump = currentdump;
                    } else {
                        throw new NotSupportedException();
                    }
                } else if (currentcode.Value is Instructions.Rent) {
                    // rent <n>
                    // 空環境を引数で置き換える
                    var rap = currentcode.Value as Instructions.Rent;

                    var val = LinkedList<ExprValue>.Empty;
                    for (int i = 0; i < rap.Argn; i++) {
                        val = LinkedList.Extend(currentstack.Value, val);
                        currentstack = currentstack.Next;
                    }

                    currentenv.Replace(val);
                    nextstack = currentstack;
                    nextenv = currentenv;
                    nextcode = currentcode.Next;
                    nextdump = currentdump;
                } else if (currentcode.Value is Instructions.Bapp) {
                    // bltin <op> <n>
                    // 組み込み命令の実行を行う
                    // 引数はスタックに積まれている引数を取り出す
                    var bltin = currentcode.Value as Instructions.Bapp;

                    var val = LinkedList<ExprValue>.Empty;
                    for (int i = 0; i < bltin.Argn; i++) {
                        val = LinkedList.Extend(currentstack.Value, val);
                        currentstack = currentstack.Next;
                    }

                    var ret = BuiltinProc(bltin.Op, val);
                    nextstack = LinkedList.Extend(ret, currentstack);
                    nextenv = currentenv;
                    nextcode = currentcode.Next;
                    nextdump = currentdump;
                } else {
                    throw new NotSupportedException($"instruction {currentcode.Value} is not supported");
                }

                currentstack = nextstack;
                currentenv = nextenv;
                currentcode = nextcode;
                currentdump = nextdump;

            }
            return Tuple.Create(currentstack.Value, currentenv);
        }
    }
}
