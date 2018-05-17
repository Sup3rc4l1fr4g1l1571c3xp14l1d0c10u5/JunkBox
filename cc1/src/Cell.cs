using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AnsiCParser {

    public enum Type { Nil, Num, Str, Sym, Error, Cons, Subr, Expr };

    public class Cell {
        public Type Tag { get; }
        public object Data { get; }

        public Cell(Type type, object obj) {
            this.Tag = type;
            this.Data = obj;
        }

        public Int32 num() {
            return (Int32)Data;
        }
        public String sym() {
            return (String)Data;
        }
        public String str() {
            return (String)Data;
        }
        public Cons cons() {
            return (Cons)Data;
        }
        public Subr subr() {
            return (Subr)Data;
        }
        public Expr expr() {
            return (Expr)Data;
        }

        public static string Escape(string str) {
            return str.Aggregate(new StringBuilder(), (s, x) => {
                switch (x) {
                    case '\r': s.Append("\\r"); break;
                    case '\n': s.Append("\\n"); break;
                    case '\\': s.Append("\\\\"); break;
                    case '"': s.Append("\\\""); break;
                    default: s.Append(x); break;
                }
                return s;
            }).ToString();
        }

        public override String ToString() {
            switch (Tag) {
                case Type.Nil:
                    return "nil";
                case Type.Num:
                    return num().ToString();
                case Type.Sym:
                    return str();
                case Type.Str:
                    return $"\"{Escape(str())}\"";
                case Type.Error:
                    return "<error: " + str() + ">";
                case Type.Cons:
                    return listToString(this);
                case Type.Subr:
                    return "<subr>";
                case Type.Expr:
                    return "<expr>";
            }
            return "<unknown>";
        }

        private String listToString(Cell obj) {
            String ret = "";
            bool first = true;
            while (obj.Tag == Type.Cons) {
                if (first) {
                    first = false;
                } else {
                    ret += " ";
                }
                ret += obj.cons().car.ToString();
                obj = obj.cons().cdr;
            }
            if (obj.Tag == Type.Nil) {
                return "(" + ret + ")";
            }
            return "(" + ret + " . " + obj.ToString() + ")";
        }

        public static string PrettyPrint(Cell cell) {
            return PrettyPrinter.Print(cell);
        }

    }

    public class Cons {
        public Cons(Cell a, Cell d) {
            car = a;
            cdr = d;
        }
        public Cell car;
        public Cell cdr;
    }

    public delegate Cell Subr(Cell ags);

    public class Expr {
        public Expr(Cell a, Cell b, Cell e) {
            args = a;
            body = b;
            env = e;
        }
        public Cell args;
        public Cell body;
        public Cell env;
    }

    public class Util {
        public static Cell makeNum(Int32 num) {
            return new Cell(Type.Num, num);
        }
        public static Cell makeError(String str) {
            return new Cell(Type.Error, str);
        }
        public static Cell makeCons(Cell a, Cell d) {
            return new Cell(Type.Cons, new Cons(a, d));
        }
        public static Cell makeCons(params Cell[] a) {
            return a.Reverse().Aggregate((s,x) => new Cell(Type.Cons, new Cons(x, s)));
        }
        public static Cell makeList(params Cell[] a) {
            return a.Reverse().Aggregate(Util.Nil, (s,x) => new Cell(Type.Cons, new Cons(x, s)));
        }
        public static Cell makeSubr(Subr subr) {
            return new Cell(Type.Subr, subr);
        }
        public static Cell makeExpr(Cell args, Cell env) {
            return new Cell(Type.Expr, new Expr(safeCar(args), safeCdr(args), env));
        }
        public static Cell makeSym(String str) {
            if (str == "nil") {
                return Nil;
            } else if (!symbolMap.ContainsKey(str)) {
                symbolMap[str] = new Cell(Type.Sym, str);
            }
            return (Cell)symbolMap[str];
        }
        public static Cell makeStr(String str) {
            return new Cell(Type.Str, str);
        }

        public static Cell safeCar(Cell obj) {
            if (obj.Tag == Type.Cons) {
                return obj.cons().car;
            }
            return Nil;
        }
        public static Cell safeCdr(Cell obj) {
            if (obj.Tag == Type.Cons) {
                return obj.cons().cdr;
            }
            return Nil;
        }

        public static Cell nreverse(Cell lst) {
            Cell ret = Nil;
            while (lst.Tag == Type.Cons) {
                Cell tmp = lst.cons().cdr;
                lst.cons().cdr = ret;
                ret = lst;
                lst = tmp;
            }
            return ret;
        }

        public static Cell pairlis(Cell lst1, Cell lst2) {
            Cell ret = Nil;
            while (lst1.Tag == Type.Cons && lst2.Tag == Type.Cons) {
                ret = makeCons(makeCons(lst1.cons().car, lst2.cons().car), ret);
                lst1 = lst1.cons().cdr;
                lst2 = lst2.cons().cdr;
            }
            return nreverse(ret);
        }

        public static Cell Nil {get; } = new Cell(Type.Nil, "nil");
        private static Dictionary<string, Cell> symbolMap = new Dictionary<string, Cell>();
    }
#if false
        class ParseState {
            public ParseState(Cell o, String s) {
                obj = o;
                next = s;
            }
            public Cell obj;
            public String next;
        }

        class Reader {
            private static bool isSpace(Char c) {
                return c == '\t' || c == '\r' || c == '\n' || c == ' ';
            }
            private static bool isDelimiter(Char c) {
                return c == kLPar || c == kRPar || c == kQuote || isSpace(c);
            }

            private static String skipSpaces(String str) {
                int i;
                for (i = 0; i < str.Length; i++) {
                    if (!isSpace(str[i])) {
                        break;
                    }
                }
                return str.Substring(i);
            }

            private static Cell makeNumOrSym(String str) {
                try {
                    return Util.makeNum(Int32.Parse(str));
                } catch (FormatException) {
                    return Util.makeSym(str);
                }
            }

            private static ParseState parseError(string s) {
                return new ParseState(Util.makeError(s), "");
            }

            private static ParseState readAtom(String str) {
                String next = "";
                for (int i = 0; i < str.Length; i++) {
                    if (isDelimiter(str[i])) {
                        next = str.Substring(i);
                        str = str.Substring(0, i);
                        break; ;
                    }
                }
                return new ParseState(makeNumOrSym(str), next);
            }

            public static ParseState read(String str) {
                str = skipSpaces(str);
                if (str.Length == 0) {
                    return parseError("empty input");
                } else if (str[0] == kRPar) {
                    return parseError("invalid syntax: " + str);
                } else if (str[0] == kLPar) {
                    return readList(str.Substring(1));
                } else if (str[0] == kQuote) {
                    ParseState tmp = read(str.Substring(1));
                    return new ParseState(Util.makeCons(Util.makeSym("quote"),
                                                        Util.makeCons(tmp.obj, Util.Nil)),
                                          tmp.next);
                }
                return readAtom(str);
            }

            private static ParseState readList(String str) {
                Cell ret = Util.Nil;
                while (true) {
                    str = skipSpaces(str);
                    if (str.Length == 0) {
                        return parseError("unfinished parenthesis");
                    } else if (str[0] == kRPar) {
                        break;
                    }
                    ParseState tmp = read(str);
                    if (tmp.obj.Tag == Type.Error) {
                        return tmp;
                    }
                    ret = Util.makeCons(tmp.obj, ret);
                    str = tmp.next;
                }
                return new ParseState(Util.nreverse(ret), str.Substring(1));
            }

            private static char kLPar = '(';
            private static char kRPar = ')';
            private static char kQuote = '\'';
        }

        class Evaluator {
            private static Cell findVar(Cell sym, Cell env) {
                while (env.Tag == Type.Cons) {
                    Cell alist = env.cons().car;
                    while (alist.Tag == Type.Cons) {
                        if (alist.cons().car.cons().car == sym) {
                            return alist.cons().car;
                        }
                        alist = alist.cons().cdr;
                    }
                    env = env.cons().cdr;
                }
                return Util.Nil;
            }

            public static void addToEnv(Cell sym, Cell val, Cell env) {
                env.cons().car = Util.makeCons(Util.makeCons(sym, val), env.cons().car);
            }

            public static Cell eval(Cell obj, Cell env) {
                if (obj.Tag == Type.Nil || obj.Tag == Type.Num ||
                    obj.Tag == Type.Error) {
                    return obj;
                } else if (obj.Tag == Type.Sym) {
                    Cell bind = findVar(obj, env);
                    if (bind == Util.Nil) {
                        return Util.makeError(obj.str() + " has no value");
                    }
                    return bind.cons().cdr;
                }

                Cell op = Util.safeCar(obj);
                Cell args = Util.safeCdr(obj);
                if (op == Util.makeSym("quote")) {
                    return Util.safeCar(args);
                } else if (op == Util.makeSym("if")) {
                    if (eval(Util.safeCar(args), env) == Util.Nil) {
                        return eval(Util.safeCar(Util.safeCdr(Util.safeCdr(args))), env);
                    }
                    return eval(Util.safeCar(Util.safeCdr(args)), env);
                } else if (op == Util.makeSym("lambda")) {
                    return Util.makeExpr(args, env);
                } else if (op == Util.makeSym("defun")) {
                    Cell expr = Util.makeExpr(Util.safeCdr(args), env);
                    Cell sym = Util.safeCar(args);
                    addToEnv(sym, expr, gEnv);
                    return sym;
                } else if (op == Util.makeSym("setq")) {
                    Cell val = eval(Util.safeCar(Util.safeCdr(args)), env);
                    Cell sym = Util.safeCar(args);
                    Cell bind = findVar(sym, env);
                    if (bind == Util.Nil) {
                        addToEnv(sym, val, gEnv);
                    } else {
                        bind.cons().cdr = val;
                    }
                    return val;
                }
                return apply(eval(op, env), evlis(args, env), env);
            }

            private static Cell evlis(Cell lst, Cell env) {
                Cell ret = Util.Nil;
                while (lst.Tag == Type.Cons) {
                    Cell elm = eval(lst.cons().car, env);
                    if (elm.Tag == Type.Error) {
                        return elm;
                    }
                    ret = Util.makeCons(elm, ret);
                    lst = lst.cons().cdr;
                }
                return Util.nreverse(ret);
            }

            private static Cell progn(Cell body, Cell env) {
                Cell ret = Util.Nil;
                while (body.Tag == Type.Cons) {
                    ret = eval(body.cons().car, env);
                    body = body.cons().cdr;
                }
                return ret;
            }

            private static Cell apply(Cell fn, Cell args, Cell env) {
                if (fn.Tag == Type.Error) {
                    return fn;
                } else if (args.Tag == Type.Error) {
                    return args;
                } else if (fn.Tag == Type.Subr) {
                    return fn.subr()(args);
                } else if (fn.Tag == Type.Expr) {
                    return progn(fn.expr().body,
                                 Util.makeCons(Util.pairlis(fn.expr().args, args),
                                 fn.expr().env));
                }
                return Util.makeError(fn.ToString() + " is not function");
            }

            delegate Int32 NumOp(Int32 x, Int32 y);
            private static Subr subrAddOrMul(Int32 initVal, NumOp fn) {
                return delegate (Cell args) {
                    Int32 ret = initVal;
                    while (args.Tag == Type.Cons) {
                        if (args.cons().car.Tag != Type.Num) {
                            return Util.makeError("wrong type");
                        }
                        ret = fn(ret, args.cons().car.num());
                        args = args.cons().cdr;
                    }
                    return Util.makeNum(ret);
                };
            }
            private static Subr subrSubOrDivOrMod(NumOp fn) {
                return delegate (Cell args) {
                    Cell x = Util.safeCar(args);
                    Cell y = Util.safeCar(Util.safeCdr(args));
                    if (x.Tag != Type.Num || y.Tag != Type.Num) {
                        return Util.makeError("wrong type");
                    }
                    return Util.makeNum(fn(x.num(), y.num()));
                };
            }

            private static Cell makeGlobalEnv() {
                Cell env = Util.makeCons(Util.Nil, Util.Nil);
                Subr subrCar = delegate (Cell args) {
                    return Util.safeCar(Util.safeCar(args));
                };
                Subr subrCdr = delegate (Cell args) {
                    return Util.safeCdr(Util.safeCar(args));
                };
                Subr subrCons = delegate (Cell args) {
                    return Util.makeCons(Util.safeCar(args),
                                         Util.safeCar(Util.safeCdr(args)));
                };
                Subr subrEq = delegate (Cell args) {
                    Cell x = Util.safeCar(args);
                    Cell y = Util.safeCar(Util.safeCdr(args));
                    if (x.Tag == Type.Num && y.Tag == Type.Num) {
                        if (x.num() == y.num()) {
                            return Util.makeSym("t");
                        }
                        return Util.Nil;
                    } else if (x == y) {
                        return Util.makeSym("t");
                    }
                    return Util.Nil;
                };
                Subr subrAtom = delegate (Cell args) {
                    if (Util.safeCar(args).Tag == Type.Cons) {
                        return Util.Nil;
                    }
                    return Util.makeSym("t");
                };
                Subr subrNumberp = delegate (Cell args) {
                    if (Util.safeCar(args).Tag == Type.Num) {
                        return Util.makeSym("t");
                    }
                    return Util.Nil;
                };
                Subr subrSymbolp = delegate (Cell args) {
                    if (Util.safeCar(args).Tag == Type.Sym) {
                        return Util.makeSym("t");
                    }
                    return Util.Nil;
                };
                Subr subrAdd =
                    subrAddOrMul(0, delegate (Int32 x, Int32 y) { return x + y; });
                Subr subrMul =
                    subrAddOrMul(1, delegate (Int32 x, Int32 y) { return x * y; });
                Subr subrSub =
                    subrSubOrDivOrMod(delegate (Int32 x, Int32 y) { return x - y; });
                Subr subrDiv =
                    subrSubOrDivOrMod(delegate (Int32 x, Int32 y) { return x / y; });
                Subr subrMod =
                    subrSubOrDivOrMod(delegate (Int32 x, Int32 y) { return x % y; });
                addToEnv(Util.makeSym("car"), Util.makeSubr(subrCar), env);
                addToEnv(Util.makeSym("cdr"), Util.makeSubr(subrCdr), env);
                addToEnv(Util.makeSym("cons"), Util.makeSubr(subrCons), env);
                addToEnv(Util.makeSym("eq"), Util.makeSubr(subrEq), env);
                addToEnv(Util.makeSym("atom"), Util.makeSubr(subrAtom), env);
                addToEnv(Util.makeSym("numberp"), Util.makeSubr(subrNumberp), env);
                addToEnv(Util.makeSym("symbolp"), Util.makeSubr(subrSymbolp), env);
                addToEnv(Util.makeSym("+"), Util.makeSubr(subrAdd), env);
                addToEnv(Util.makeSym("*"), Util.makeSubr(subrMul), env);
                addToEnv(Util.makeSym("-"), Util.makeSubr(subrSub), env);
                addToEnv(Util.makeSym("/"), Util.makeSubr(subrDiv), env);
                addToEnv(Util.makeSym("mod"), Util.makeSubr(subrMod), env);
                addToEnv(Util.makeSym("t"), Util.makeSym("t"), env);
                return env;
            }

            public static Cell globalEnv() { return gEnv; }

            private static Cell gEnv = makeGlobalEnv();
        }

        class Lisp {
            public static void Repl() {
                Cell gEnv = Evaluator.globalEnv();
                string line;
                Console.Write("> ");
                while ((line = Console.In.ReadLine()) != null) {
                    Console.Write(Evaluator.eval(Reader.read(line).obj, gEnv));
                    Console.Write("\n> ");
                }
            }
        }
    
    /// <summary>
    /// S式用のセル
    /// </summary>
    public abstract partial class Cell {

        /// <summary>
        /// 空セル
        /// </summary>
        public static Cell Nil { get; } = new ConsCell();

        /// <summary>
        /// コンスセル
        /// </summary>
        public class ConsCell : Cell {
            public Cell Car {
                get;
            }
            public Cell Cdr {
                get;
            }
            public ConsCell() {
            }
            public ConsCell(Cell car, Cell cdr) {
                Car = car ?? Nil;
                Cdr = cdr ?? Nil;
            }
            public override string ToString() {
                List<string> cars = new List<string>();
                var self = this;
                while (self != null && self != Nil) {
                    cars.Add(self.Car.ToString());
                    self = self.Cdr as ConsCell;
                }
                return "(" + string.Join(" ", cars) + ")";
            }
        }

        /// <summary>
        /// 文字列セル
        /// </summary>
        public class StringCell : Cell {
            public string Value {
                get;
            }
            public StringCell(string value) {
                Value = value;
            }
            public override string ToString() {
                return "\"" + Value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n") + "\"";
            }
        }

        /// <summary>
        /// 数値セル
        /// </summary>
        public class NumberCell : Cell {
            public string Value {
                get;
            }
            public NumberCell(string value) {
                Value = value;
            }
            public override string ToString() {
                return Value;
            }
        }

        /// <summary>
        /// 記号セル
        /// </summary>
        public class SymbolCell : Cell {
            public string Value {
                get;
            }
            public SymbolCell(string value) {
                Value = value;
            }
            public override string ToString() {
                return Value;
            }
        }


        /// <summary>
        /// リスト作成
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static Cell Create(params object[] args) {
            var chain = Nil;
            foreach (var arg in args.Reverse()) {
                if (arg is String) {
                    chain = new ConsCell(new StringCell(arg as string), chain);
                } else if (arg is Int32) {
                    chain = new ConsCell(new StringCell(arg.ToString()), chain);
                } else if (arg is Cell) {
                    chain = new ConsCell(arg as Cell, chain);
                } else if (arg is Location) {
                    chain = new ConsCell(Create((arg as Location).FilePath, (arg as Location).Line, (arg as Location).Column), chain);
                } else if (arg is LocationRange) {
                    chain = new ConsCell(Create((arg as LocationRange).Start.FilePath ?? "builtin", (arg as LocationRange).Start.Line, (arg as LocationRange).Start.Column, (arg as LocationRange).End.FilePath ?? "builtin", (arg as LocationRange).End.Line, (arg as LocationRange).End.Column), chain);
                } else {
                    throw new Exception();
                }
            }
            return chain;
        }
#endif
    /// <summary>
    /// S式の整形出力
    /// </summary>
    public static class PrettyPrinter {
        private static void Indent(StringBuilder sb, int lebel) {
            sb.Append(String.Concat(Enumerable.Repeat("  ", lebel)));
        }
        private static void OpenParen(StringBuilder sb) { sb.Append("("); }
        private static void CloseParen(StringBuilder sb) { sb.Append(")"); }
        private static void Atom(StringBuilder sb, Cell e, bool prefix) { sb.Append(prefix ? " " : "").Append(e.ToString()); }

        private static void List(StringBuilder sb, Cell s, int lebel, bool prefix) {
            if (prefix) {
                Indent(sb, lebel);
            }
            OpenParen(sb);
            prefix = false;
            for (; ; ) {
                if (s.Tag == Type.Nil) {
                    CloseParen(sb);
                    break;
                } else if (s.Tag == Type.Cons) {
                    var e = Util.safeCar(s);
                    if (e.Tag == Type.Cons) {
                        if (prefix) {
                            sb.AppendLine();
                        }
                        List(sb, e, lebel + 1, prefix);
                    } else {
                        Atom(sb, e, prefix);
                    }
                    s = Util.safeCdr(s);
                    prefix = true;
                } else {
                    throw new Exception();
                }
            }
        }

        public static string Print(Cell cell) {
            StringBuilder sb = new StringBuilder();
            if (cell.Tag == Type.Cons) {
                List(sb, cell, 0, false);
            } else {
                sb.Append(cell.ToString());
            }
            sb.AppendLine();

            return sb.ToString();
        }
    }

    //}

    public static class S {
        public static bool IsAtom(this Cell self) {
            return (!IsPair(self) && !IsNull(self));
        }
        public static bool IsPair(this Cell self) {
            return (self.Tag == Type.Cons);
        }
        public static bool IsNull(this Cell self) {
            return (self.Tag == Type.Nil);
        }
        public static Cell Car(this Cell self) {
            return Util.safeCar(self);
        }
        public static Cell Cdr(this Cell self) {
            return Util.safeCdr(self);
        }
        public static Cell Cons(Cell lhs, Cell rhs) {
            return Util.makeCons(lhs, rhs);
        }
    }

}
