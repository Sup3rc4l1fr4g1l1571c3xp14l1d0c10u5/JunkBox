using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace MiniMAL
{
    /// <summary>
    /// SECD機械ベースVM
    /// </summary>
    public static class VM {

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
                public UnitV() { }

                public override string ToString() {
                    return $"()";
                }
            }

            /// <summary>
            /// レキシカルクロージャー
            /// </summary>
            public class ProcV : ExprValue {
                public string Id { get; }
                public LinkedList<Instructions> Body { get; }
                public LinkedList<LinkedList<ExprValue>> Env { get; private set; }

                public ProcV(string id, LinkedList<Instructions> body, LinkedList<LinkedList<ExprValue>> env) {
                    Id = id;
                    Body = body;
                    Env = env;
                }

                public void BackPatchEnv(LinkedList<LinkedList<ExprValue>> newenv) {
                    Env = newenv;
                }

                public override string ToString() {
                    return $"<fun>";
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
                    return $"<bproc>";
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
                        for (var p = this.Next; p != Empty; p = p.Next) {
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
                public ExprValue[] Values { get; }

                public TupleV(ExprValue[] values) {
                    Values = values;
                }

                public override string ToString() {
                    return $"({String.Join(", ", Values.Select(x => x.ToString()))})";
                }
            }

            /// <summary>
            /// Nil値
            /// </summary>
            public class NilV : ExprValue {

                public NilV() {
                }

                public override string ToString() {
                    return $"(Nil)";
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
                    return (i1 == i2);
                }
                if (arg1 is ExprValue.StrV && arg2 is ExprValue.StrV) {
                    var i1 = ((ExprValue.StrV)arg1).Value;
                    var i2 = ((ExprValue.StrV)arg2).Value;
                    return (i1 == i2);
                }
                if (arg1 is ExprValue.BoolV && arg2 is ExprValue.BoolV) {
                    var i1 = ((ExprValue.BoolV)arg1).Value;
                    var i2 = ((ExprValue.BoolV)arg2).Value;
                    return (i1 == i2);
                }
                if (arg1 is ExprValue.UnitV && arg2 is ExprValue.UnitV) {
                    return (true);
                }
                if (arg1 is ExprValue.NilV && arg2 is ExprValue.NilV) {
                    return (true);
                }
                if (arg1 is ExprValue.ConsV && arg2 is ExprValue.ConsV) {
                    var i1 = ((ExprValue.ConsV)arg1);
                    var i2 = ((ExprValue.ConsV)arg2);
                    while (i1 != ExprValue.ConsV.Empty && i2 != ExprValue.ConsV.Empty) {
                        if (!Equals(i1.Value, i2.Value)) {
                            return false;
                        }
                        i1 = i1.Next;
                        i2 = i2.Next;
                    }
                    return (i1 != ExprValue.ConsV.Empty && i2 != ExprValue.ConsV.Empty);
                }
                if (arg1 is ExprValue.TupleV && arg2 is ExprValue.TupleV) {
                    var i1 = ((ExprValue.TupleV)arg1);
                    var i2 = ((ExprValue.TupleV)arg2);
                    if (i1.Values.Length != i2.Values.Length) {
                        return false;
                    }

                    return i1.Values.Zip(i2.Values, Tuple.Create).All(x => Equals(x.Item1, x.Item2));
                }
                return (false);
            }

        }

        /// <summary>
        /// 仮想マシン命令
        /// </summary>
        public abstract class Instructions {
            public class Ld : Instructions {
                public int Frame { get; }
                public int Index { get; }
                public Ld(int frame, int index) {
                    this.Frame = frame;
                    this.Index = index;
                }
                public override string ToString() {
                    return $"(ld {Frame} {Index})";
                }
            }

            public class Ldc : Instructions {
                public ExprValue Value { get; }

                public Ldc(ExprValue value) {
                    this.Value = value;
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
                        return $"#<primitive>";
                    }
                    public Primitive(Func<LinkedList<ExprValue>, ExprValue> proc) {
                        Proc = proc;
                    }
                }
                public Fun Function { get; }
                public Ldf(Fun function) {
                    this.Function = function;
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
                public Rtn() {
                }
                public override string ToString() {
                    return $"(rtn)";
                }
            }

            public class Sel : Instructions {
                public LinkedList<Instructions> fclosure { get; }
                public LinkedList<Instructions> tclosure { get; }

                public Sel(LinkedList<Instructions> tclosure, LinkedList<Instructions> fclosure) {
                    this.tclosure = tclosure;
                    this.fclosure = fclosure;
                }
                public override string ToString() {
                    return $"(sel {tclosure} {fclosure})";
                }
            }

            public class Selr : Instructions {
                public LinkedList<Instructions> fclosure { get; }
                public LinkedList<Instructions> tclosure { get; }

                public Selr(LinkedList<Instructions> tclosure, LinkedList<Instructions> fclosure) {
                    this.tclosure = tclosure;
                    this.fclosure = fclosure;
                }
                public override string ToString() {
                    return $"(selr {tclosure} {fclosure})";
                }
            }

            public class Join : Instructions {
                public Join() {
                }
                public override string ToString() {
                    return $"(join)";
                }
            }

            public class Pop : Instructions {
                public Pop() {
                }
                public override string ToString() {
                    return $"(pop)";
                }
            }

            public class Stop : Instructions {
                public Stop() {
                }
                public override string ToString() {
                    return $"(stop)";
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
                public int Num { get; }

                public Tuple(int num) {
                    this.Num = num;
                }
                public override string ToString() {
                    return $"(Tuple {Num})";
                }
            }

            public class Dum : Instructions {
                public Dum() {
                }
                public override string ToString() {
                    return $"(dum)";
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
                    this.Op = op;
                    Argn = argn;
                }
                public override string ToString() {
                    return $"(bapp {Op} {Argn})";
                }
            }

        }

        /// <summary>
        /// 組み込み命令を処理
        /// </summary>
        /// <param name="op"></param>
        /// <returns></returns>
        private static ExprValue BuiltinProc(Expressions.BuiltinOp.Kind op, LinkedList<ExprValue> x) {
            switch (op) {
                case Expressions.BuiltinOp.Kind.Plus:
                    {
                        var arg2 = x.Value;
                        var arg1 = x.Next.Value;

                        if (arg1 is ExprValue.IntV && arg2 is ExprValue.IntV) {
                            var i1 = ((ExprValue.IntV)arg1).Value;
                            var i2 = ((ExprValue.IntV)arg2).Value;
                            return new ExprValue.IntV(i1 + i2);
                        }
                        if (arg1 is ExprValue.StrV && arg2 is ExprValue.StrV) {
                            var i1 = ((ExprValue.StrV)arg1).Value;
                            var i2 = ((ExprValue.StrV)arg2).Value;
                            return new ExprValue.StrV(i1 + i2);
                        }
                        throw new Exception("Both arguments must be integer/string: +");

                    };
                case Expressions.BuiltinOp.Kind.Minus:
                    {
                        var arg2 = x.Value;
                        var arg1 = x.Next.Value;

                        if (arg1 is ExprValue.IntV && arg2 is ExprValue.IntV) {
                            var i1 = ((ExprValue.IntV)arg1).Value;
                            var i2 = ((ExprValue.IntV)arg2).Value;
                            return new ExprValue.IntV(i1 - i2);
                        }
                        throw new Exception("Both arguments must be integer: -");

                    };
                case Expressions.BuiltinOp.Kind.Mult:
                    {
                        var arg2 = x.Value;
                        var arg1 = x.Next.Value;

                        if (arg1 is ExprValue.IntV && arg2 is ExprValue.IntV) {
                            var i1 = ((ExprValue.IntV)arg1).Value;
                            var i2 = ((ExprValue.IntV)arg2).Value;
                            return new ExprValue.IntV(i1 * i2);
                        }
                        throw new Exception("Both arguments must be integer: *");
                    };
                case Expressions.BuiltinOp.Kind.Div:
                    {
                        var arg2 = x.Value;
                        var arg1 = x.Next.Value;

                        if (arg1 is ExprValue.IntV && arg2 is ExprValue.IntV) {
                            var i1 = ((ExprValue.IntV)arg1).Value;
                            var i2 = ((ExprValue.IntV)arg2).Value;
                            return new ExprValue.IntV(i1 / i2);
                        }
                        throw new Exception("Both arguments must be integer: /");
                    };
                case Expressions.BuiltinOp.Kind.Lt:
                    {
                        var arg2 = x.Value;
                        var arg1 = x.Next.Value;

                        if (arg1 is ExprValue.IntV && arg2 is ExprValue.IntV) {
                            var i1 = ((ExprValue.IntV)arg1).Value;
                            var i2 = ((ExprValue.IntV)arg2).Value;
                            return new ExprValue.BoolV(i1 < i2);
                        }
                        throw new Exception("Both arguments must be integer: <");
                    };
                case Expressions.BuiltinOp.Kind.Le:
                    {
                        var arg2 = x.Value;
                        var arg1 = x.Next.Value;

                        if (arg1 is ExprValue.IntV && arg2 is ExprValue.IntV) {
                            var i1 = ((ExprValue.IntV)arg1).Value;
                            var i2 = ((ExprValue.IntV)arg2).Value;
                            return new ExprValue.BoolV(i1 <= i2);
                        }
                        throw new Exception("Both arguments must be integer: <=");
                    };
                case Expressions.BuiltinOp.Kind.Gt:
                    {
                        var arg2 = x.Value;
                        var arg1 = x.Next.Value;

                        if (arg1 is ExprValue.IntV && arg2 is ExprValue.IntV) {
                            var i1 = ((ExprValue.IntV)arg1).Value;
                            var i2 = ((ExprValue.IntV)arg2).Value;
                            return new ExprValue.BoolV(i1 > i2);
                        }
                        throw new Exception("Both arguments must be integer: >");
                    };
                case Expressions.BuiltinOp.Kind.Ge:
                    {
                        var arg2 = x.Value;
                        var arg1 = x.Next.Value;

                        if (arg1 is ExprValue.IntV && arg2 is ExprValue.IntV) {
                            var i1 = ((ExprValue.IntV)arg1).Value;
                            var i2 = ((ExprValue.IntV)arg2).Value;
                            return new ExprValue.BoolV(i1 >= i2);
                        }
                        throw new Exception("Both arguments must be integer: >=");
                    };
                case Expressions.BuiltinOp.Kind.Eq:
                    {
                        var arg2 = x.Value;
                        var arg1 = x.Next.Value;
                        return new ExprValue.BoolV(ExprValue.Equals(arg1, arg2));
                    };
                case Expressions.BuiltinOp.Kind.Ne:
                    {
                        var arg2 = x.Value;
                        var arg1 = x.Next.Value;
                        return new ExprValue.BoolV(!ExprValue.Equals(arg1, arg2));
                    };
                case Expressions.BuiltinOp.Kind.ColCol:
                    {
                        var arg2 = x.Value;
                        var arg1 = x.Next.Value;

                        if (arg2 is ExprValue.ConsV) {
                            var i2 = ((ExprValue.ConsV)arg2);
                            return new ExprValue.ConsV(arg1, i2);
                        }
                        throw new Exception("Right arguments must be List: ::");
                    };
                case Expressions.BuiltinOp.Kind.Head: {
                    var arg1 = x.Value;

                    if (arg1 is ExprValue.ConsV) {
                        if (arg1 == ExprValue.ConsV.Empty) {
                            throw new Exception("null list ");
                        }
                        return ((ExprValue.ConsV)arg1).Value;
                    }
                    throw new Exception("arguments 1 must be ConsV: @Head");
                }
                case Expressions.BuiltinOp.Kind.Tail: {
                    var arg1 = x.Value;

                    if (arg1 is ExprValue.ConsV) {
                        if (arg1 == ExprValue.ConsV.Empty) {
                            throw new Exception("null list ");
                        }
                        return ((ExprValue.ConsV)arg1).Next;
                    }
                    throw new Exception("arguments 1 must be ConsV: @Tail");
                }
                case Expressions.BuiltinOp.Kind.IsCons: {
                    var arg1 = x.Value;

                    return new ExprValue.BoolV(arg1 is ExprValue.ConsV);
                }
                case Expressions.BuiltinOp.Kind.Nth: {
                    var arg2 = x.Value;
                    var arg1 = x.Next.Value;

                    if (arg1 is ExprValue.IntV && arg2 is ExprValue.TupleV) {
                        return ((ExprValue.TupleV)arg2).Values[(int)((ExprValue.IntV)arg1).Value];
                    } else { 
                        throw new Exception("arguments 1 must be ConsV: @Tail");
                    }

                    }
                case Expressions.BuiltinOp.Kind.IsTuple: {
                    var arg1 = x.Value;
                    return new ExprValue.BoolV(arg1 is ExprValue.TupleV);
                }
                case Expressions.BuiltinOp.Kind.Length: {
                    var arg1 = x.Value;
                    if (arg1 is ExprValue.TupleV) {
                        return new ExprValue.IntV(((ExprValue.TupleV)arg1).Values.Length);
                    }
                    throw new Exception("invalid argument num: @Length");
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
        /// <returns></returns>
        private static LinkedList<Instructions> CompileExpr(Expressions expr, LinkedList<LinkedList<string>> env, LinkedList<Instructions> code, bool isTail) {
            if (expr is Expressions.IntLit) {
                return LinkedList.Extend(new Instructions.Ldc(new ExprValue.IntV((expr as Expressions.IntLit).Value)), code);
            }
            if (expr is Expressions.StrLit) {
                return LinkedList.Extend(new Instructions.Ldc(new ExprValue.StrV((expr as Expressions.StrLit).Value)), code);
            }
            if (expr is Expressions.BoolLit) {
                return LinkedList.Extend(new Instructions.Ldc(new ExprValue.BoolV((expr as Expressions.BoolLit).Value)), code);
            }
            if (expr is Expressions.EmptyListLit) {
                return LinkedList.Extend(new Instructions.Ldc(ExprValue.ConsV.Empty), code);
            }
            if (expr is Expressions.UnitLit) {
                return LinkedList.Extend(new Instructions.Ldc(new ExprValue.UnitV()), code);
            }
            if (expr is Expressions.TupleExp) {
                var e = expr as Expressions.TupleExp;
                code = LinkedList.Extend(new Instructions.Tuple(e.Exprs.Length), code);
                foreach (var exp in e.Exprs) {
                    code = CompileExpr(exp, env, code, false);
                }
                return code;
            }
            if (expr is Expressions.Var) {
                var e = expr as Expressions.Var;
                var frame = 0;
                for (var ev = env; ev != LinkedList<LinkedList<string>>.Empty; ev = ev.Next) {
                    var index = LinkedList.FirstIndex(x => x == e.Id, ev.Value);
                    if (index != -1) {
                        return LinkedList.Extend(new Instructions.Ld(frame, index), code);
                    }
                    frame++;
                }
                throw new Exception($"undefined variable: {e.Id}");
            }
            if (expr is Expressions.IfExp) {
                var e = expr as Expressions.IfExp;
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
                var e = expr as Expressions.BuiltinOp;
                code = LinkedList.Extend(new Instructions.Bapp(e.Op, e.Exprs.Length), code);
                for (var i = 0; i < e.Exprs.Length; i++) {
                    code = CompileExpr(e.Exprs[i], env, code, false);
                }
                return code;
            }
            if (expr is Expressions.DFunExp) {
                throw new NotSupportedException();
            }
            if (expr is Expressions.FunExp) {
                var e = expr as Expressions.FunExp;
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
                var e = expr as Expressions.AppExp;
                code = LinkedList.Extend((code.Value is Instructions.Rtn) ? (Instructions)new Instructions.Tapp(1) : new Instructions.App(1), code);
                code = CompileExpr(e.Fun, env, code,true);
                //code = LinkedList.Extend(new Instruction.Args(1), code);
                code = CompileExpr(e.Arg, env, code, false);
                return code;
            }
            if (expr is Expressions.LetExp) {

                var e = expr as Expressions.LetExp;

                var newenv = LinkedList<string>.Empty;
                foreach (var bind in e.Binds.Reverse()) {
                    newenv = LinkedList.Extend(bind.Item1, newenv);
                }

                code = LinkedList.Extend(new Instructions.App(e.Binds.Count()), code);
                var body = new Instructions.Ldf.Closure(CompileExpr(e.Body, LinkedList.Extend(newenv, env), LinkedList.Extend(new Instructions.Rtn(), LinkedList<Instructions>.Empty), true));
                code = LinkedList.Extend(new Instructions.Ldf(body), code);
                foreach (var bind in e.Binds.Reverse()) {
                    code = CompileExpr(bind.Item2, env, code, false);
                }
                return code;
            }
            if (expr is Expressions.LetRecExp) {
                var e = expr as Expressions.LetRecExp;

                var newenv = LinkedList<string>.Empty;
                foreach (var bind in e.Binds.Reverse()) {
                    newenv = LinkedList.Extend(bind.Item1, newenv);
                }

                code = LinkedList.Extend(new Instructions.Rap(e.Binds.Count()), code);
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
                return LinkedList.Extend(new Instructions.Halt((expr as Expressions.HaltExp).Message), code);
            }
            if (expr is Expressions.NilLit) {
                return LinkedList.Extend(new Instructions.Ldc(new ExprValue.NilV()), code);
            }

            throw new Exception();
        }

        public static Tuple<LinkedList<Instructions>[], LinkedList<LinkedList<string>>> CompileDecl(Declarations decl, LinkedList<LinkedList<string>> env) {
            if (decl is Declarations.Exp) {
                var expr = (decl as Declarations.Exp).Syntax;
                return Tuple.Create(
                    new[] { CompileExpr(expr, env, LinkedList.Extend(new Instructions.Stop(), LinkedList<Instructions>.Empty), true) },
                    env
                );
            }
            if (decl is Declarations.Decls) {
                var decls = (decl as Declarations.Decls).Entries;

                List<LinkedList<Instructions>> codes = new List<LinkedList<Instructions>>();
                foreach (var d in decls) {
                    if (d is Declarations.Decl) {
                        var e = d as Declarations.Decl;

                        var newenv = LinkedList<string>.Empty;
                        foreach (var bind in e.Binds.Reverse()) {
                            newenv = LinkedList.Extend(bind.Item1, newenv);
                        }

                        var code = LinkedList.Extend(new Instructions.Stop(), LinkedList<Instructions>.Empty);
                        code = LinkedList.Extend(new Instructions.Ent(e.Binds.Count()), code);
                        foreach (var bind in e.Binds.Reverse()) {
                            code = CompileExpr(bind.Item2, env, code, true);
                        }
                        env = LinkedList.Extend(newenv, env);
                        codes.Add(code);

                        continue;
                    }
                    if (d is Declarations.RecDecl) {
                        var e = d as Declarations.RecDecl;

                        var newenv = LinkedList<string>.Empty;
                        foreach (var bind in e.Binds.Reverse()) {
                            newenv = LinkedList.Extend(bind.Item1, newenv);
                        }

                        var code = LinkedList.Extend(new Instructions.Stop(), LinkedList<Instructions>.Empty);
                        code = LinkedList.Extend(new Instructions.Rent(e.Binds.Count()), code);
                        foreach (var bind in e.Binds.Reverse()) {
                            code = CompileExpr(bind.Item2, LinkedList.Extend(newenv, env), code, true);
                        }
                        code = LinkedList.Extend(new Instructions.Dum(), code);
                        env = LinkedList.Extend(newenv, env);
                        codes.Add(code);

                        continue;
                    }
                    throw new Exception();
                }
                return Tuple.Create(codes.ToArray(), env);
            }
            if (decl is Declarations.Empty) {
                return Tuple.Create(
                    new LinkedList<Instructions>[0],
                    env
                );
            }
            throw new Exception();
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
        /// 仮想マシン命令の実行
        /// </summary>
        /// <param name="Code">仮想マシン命令列</param>
        /// <returns></returns>
        public static Tuple<ExprValue, LinkedList<LinkedList<ExprValue>>> Run(LinkedList<Instructions> Code, LinkedList<LinkedList<ExprValue>> Env) {
            LinkedList<ExprValue> stack = LinkedList<ExprValue>.Empty;
            LinkedList<LinkedList<ExprValue>> env = Env;
            LinkedList<Instructions> code = Code;
            LinkedList<DumpInfo> dump = LinkedList<DumpInfo>.Empty;


            for (;;) {
                LinkedList<ExprValue> _nextstack;
                LinkedList<LinkedList<ExprValue>> _nextenv;
                LinkedList<Instructions> _nextcode;
                LinkedList<DumpInfo> _nextdump;

                if (code.Value is Instructions.Ld) {
                    // ld <i> <j>
                    // E レジスタの <i> 番目のフレームの <j> 番目の要素をスタックに積む
                    var ld = code.Value as Instructions.Ld;
                    var frame = LinkedList.At(env, ld.Frame);
                    var val = LinkedList.At(frame, ld.Index);

                    _nextstack = LinkedList.Extend(val, stack);
                    _nextenv = env;
                    _nextcode = code.Next;
                    _nextdump = dump;
                } else if (code.Value is Instructions.Ldc) {
                    // ldc <const>
                    // 定数 <const> をスタックに積む
                    var ldc = code.Value as Instructions.Ldc;

                    _nextstack = LinkedList.Extend(ldc.Value, stack);
                    _nextenv = env;
                    _nextcode = code.Next;
                    _nextdump = dump;
                } else if (code.Value is Instructions.Ldf) {
                    // ldf <code>
                    // code からクロージャを生成してスタックに積む
                    var ldf = code.Value as Instructions.Ldf;
                    ExprValue closure;
                    if (ldf.Function is Instructions.Ldf.Closure)
                    {
                        var c = ldf.Function as Instructions.Ldf.Closure;
                        closure = new ExprValue.ProcV("", c.Body, env);
                    } else {
                        var p = ldf.Function as Instructions.Ldf.Primitive;
                        closure = new ExprValue.BProcV(p.Proc);
                    }

                    _nextstack = LinkedList.Extend(closure, stack);
                    _nextenv = env;
                    _nextcode = code.Next;
                    _nextdump = dump;

                } else if (code.Value is Instructions.App) {
                    // app <n>
                    // スタックに積まれているクロージャと引数を取り出して関数呼び出しを行う
                    var app = code.Value as Instructions.App;
                    var closure = LinkedList.At(stack, 0);
                    stack = stack.Next;

                    var val = LinkedList<ExprValue>.Empty;
                    for (int i = 0; i < app.Argn; i++) {
                        val = LinkedList.Extend(stack.Value, val);
                        stack = stack.Next;
                    }

                    if (closure is ExprValue.ProcV) {
                        var newdump = new DumpInfo(stack, env, code.Next);

                        _nextstack = LinkedList<ExprValue>.Empty;
                        _nextenv = LinkedList.Extend(val, (closure as ExprValue.ProcV).Env);
                        _nextcode = (closure as ExprValue.ProcV).Body;
                        _nextdump = LinkedList.Extend(newdump, dump);
                    } else if (closure is ExprValue.BProcV) {
                        var ret = (closure as ExprValue.BProcV).Proc(val);
                        _nextstack = LinkedList.Extend(ret, stack);
                        _nextenv = env;
                        _nextcode = code.Next;
                        _nextdump = dump;;
                    } else {
                        throw new Exception($"{closure} is cannot apply.");
                    }
                } else if (code.Value is Instructions.Tapp) {
                    // app <n>
                    // スタックに積まれているクロージャと引数を取り出して関数呼び出しを行う
                    var tapp = code.Value as Instructions.Tapp;
                    var closure = LinkedList.At(stack, 0);
                    stack = stack.Next;

                    var val = LinkedList<ExprValue>.Empty;
                    for (int i = 0; i < tapp.Argn; i++) {
                        val = LinkedList.Extend(stack.Value, val);
                        stack = stack.Next;
                    }

                    if (closure is ExprValue.ProcV) {
                        //var newdump = new DumpInfo(stack, env, code.Next);

                        _nextstack = stack;
                        _nextenv = LinkedList.Extend(val, (closure as ExprValue.ProcV).Env);
                        _nextcode = (closure as ExprValue.ProcV).Body;
                        _nextdump = dump;
                    } else if (closure is ExprValue.BProcV) {
                        var ret = (closure as ExprValue.BProcV).Proc(val);
                        _nextstack = LinkedList.Extend(ret, stack);
                        _nextenv = env;
                        _nextcode = code.Next;
                        _nextdump = dump;
                    } else {
                        throw new NotSupportedException();
                    }
                } else if (code.Value is Instructions.Ent) {
                    // ent <n>
                    // スタックに積まれている引数を取り出して環境を作る
                    var app = code.Value as Instructions.Ent;

                    var val = LinkedList<ExprValue>.Empty;
                    for (int i = 0; i < app.Argn; i++) {
                        val = LinkedList.Extend(stack.Value, val);
                        stack = stack.Next;
                    }

                    _nextstack = stack;
                    _nextenv = LinkedList.Extend(val, env);
                    _nextcode = code.Next;
                    _nextdump = dump;

                } else if (code.Value is Instructions.Rtn) {
                    // rtn
                    // 関数呼び出しから戻る
                    var rtn = code.Value as Instructions.Rtn;
                    var val = LinkedList.At(stack, 0);
                    var prevdump = dump.Value;

                    _nextstack = LinkedList.Extend(val, prevdump.Stack);
                    _nextenv = prevdump.Env;
                    _nextcode = prevdump.Code;
                    _nextdump = dump.Next;
                } else if (code.Value is Instructions.Sel) {
                    // sel <ct> <cf>
                    // スタックトップの値が真ならば <ct> を実行する。偽ならば <cf> を実行する
                    var sel = code.Value as Instructions.Sel;
                    var val = LinkedList.At(stack, 0);
                    var cond = (val as ExprValue.BoolV).Value;
                    var newdump = new DumpInfo(null, null, code.Next);

                    _nextstack = stack.Next;
                    _nextenv = env;
                    _nextcode = (cond ? sel.tclosure : sel.fclosure);
                    _nextdump = LinkedList.Extend(newdump, dump);
                } else if (code.Value is Instructions.Selr) {
                    // selr <ct> <cf>
                    // スタックトップの値が真ならば <ct> を実行する。偽ならば <cf> を実行する
                    // dumpを更新しない
                    var selr = code.Value as Instructions.Selr;
                    var val = LinkedList.At(stack, 0);
                    var cond = (val as ExprValue.BoolV).Value;
                    //var newdump = new DumpInfo(null, null, code.Next);

                    _nextstack = stack.Next;
                    _nextenv = env;
                    _nextcode = (cond ? selr.tclosure : selr.fclosure);
                    _nextdump = dump;
                } else if (code.Value is Instructions.Join) {
                    // join
                    // 条件分岐(sel)から合流する 
                    var join = code.Value as Instructions.Join;
                    var prevdump = dump.Value;

                    _nextstack = stack;
                    _nextenv = env;
                    _nextcode = prevdump.Code;
                    _nextdump = dump.Next;
                } else if (code.Value is Instructions.Pop) {
                    // pop <ct> <cf>
                    // スタックトップの値を取り除く
                    var pop = code.Value as Instructions.Pop;

                    _nextstack = stack.Next;
                    _nextenv = env;
                    _nextcode = code.Next;
                    _nextdump = dump;
                } else if (code.Value is Instructions.Stop) {
                    // stop
                    // スタックトップの値が真ならば <ct> を実行する。偽ならば <cf> を実行する
                    break;

                    //_nextstack = stack;
                    //_nextenv = env;
                    //_nextcode = code.Next;
                    //_nextdump = dump;
                    //_nextglobal = global;
                } else if (code.Value is Instructions.Halt) {
                    // halt
                    // エラーを生成して停止する
                    throw new Exception((code.Value as Instructions.Halt).Message);
                    break;

                    //_nextstack = stack;
                    //_nextenv = env;
                    //_nextcode = code.Next;
                    //_nextdump = dump;
                    //_nextglobal = global;
                } else if (code.Value is Instructions.Tuple) {
                    // tuple <n>
                    // スタックに積まれている値をn個取り出してタプルを作る
                    var val = new List<ExprValue>();
                    for (int i = 0; i < ((Instructions.Tuple)code.Value).Num; i++) {
                        val.Add(stack.Value);
                        stack = stack.Next;
                    }
                    var ret = new ExprValue.TupleV(val.ToArray());

                    _nextstack = LinkedList.Extend(ret, stack);
                    _nextenv = env;
                    _nextcode = code.Next;
                    _nextdump = dump;
                } else if (code.Value is Instructions.Dum) {
                    // dum
                    // 空環境を積む
                    var dum = code.Value as Instructions.Dum;

                    _nextstack = stack;
                    _nextenv = LinkedList.Extend(LinkedList<ExprValue>.Empty, env);
                    _nextcode = code.Next;
                    _nextdump = dump;
                } else if (code.Value is Instructions.Rap) {
                    // rap <n>
                    // 空環境を引数で置き換える
                    var rap = code.Value as Instructions.Rap;
                    var closure = LinkedList.At(stack, 0);
                    stack = stack.Next;

                    var val = LinkedList<ExprValue>.Empty;
                    for (int i = 0; i < rap.Argn; i++) {
                        val = LinkedList.Extend(stack.Value, val);
                        stack = stack.Next;
                    }

                    if (closure is ExprValue.ProcV) {
                        var newdump = new DumpInfo(stack, env, code.Next);
                        env.Replace(val);
                        _nextstack = LinkedList<ExprValue>.Empty;
                        _nextenv = env;
                        _nextcode = (closure as ExprValue.ProcV).Body;
                        _nextdump = LinkedList.Extend(newdump, dump);
                    } else if (closure is ExprValue.BProcV) {
                        var ret = (closure as ExprValue.BProcV).Proc(val);
                        _nextstack = LinkedList.Extend(ret, stack);
                        _nextenv = env;
                        _nextcode = code.Next;
                        _nextdump = dump;
                    } else {
                        throw new NotSupportedException();
                    }
                } else if (code.Value is Instructions.Rent) {
                    // rent <n>
                    // 空環境を引数で置き換える
                    var rap = code.Value as Instructions.Rent;

                    var val = LinkedList<ExprValue>.Empty;
                    for (int i = 0; i < rap.Argn; i++) {
                        val = LinkedList.Extend(stack.Value, val);
                        stack = stack.Next;
                    }

                    env.Replace(val);
                    _nextstack = stack;
                    _nextenv = env;
                    _nextcode = code.Next;
                    _nextdump = dump;
                } else if (code.Value is Instructions.Bapp) {
                    // bltin <op> <n>
                    // 組み込み命令の実行を行う
                    // 引数はスタックに積まれている引数を取り出す
                    var bltin = code.Value as Instructions.Bapp;

                    var val = LinkedList<ExprValue>.Empty;
                    for (int i = 0; i < bltin.Argn; i++) {
                        val = LinkedList.Extend(stack.Value, val);
                        stack = stack.Next;
                    }

                    var ret = BuiltinProc(bltin.Op, val);
                    _nextstack = LinkedList.Extend(ret, stack);
                    _nextenv = env;
                    _nextcode = code.Next;
                    _nextdump = dump;
                } else {
                    throw new Exception();
                }

                stack = _nextstack;
                env = _nextenv;
                code = _nextcode;
                dump = _nextdump;

            }
            return Tuple.Create(stack.Value, env);
        }
    }
}
