// SchemeNet.cs version 2013-12-10
// A Scheme subset interpreter in C#
// Features: Tail calls, CL style macros, basic .NET FFI, part of SRFI-1
// Focus is on readability and source code size, not on speed.
// Copyright (c) 2012-2013, Leif Bruder <leifbruder@gmail.com>
//
// Permission to use, copy, modify, and/or distribute this software for any
// purpose with or without fee is hereby granted, provided that the above
// copyright notice and this permission notice appear in all copies.
// 
// THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES
// WITH REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF
// MERCHANTABILITY AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR
// ANY SPECIAL, DIRECT, INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES
// WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS, WHETHER IN AN
// ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION, ARISING OUT OF
// OR IN CONNECTION WITH THE USE OR PERFORMANCE OF THIS SOFTWARE.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

namespace AnsiCParser {
    public class Lisp {

        public static class Util {
            public static Pair makeList(params object[] args) {
                return args.Reverse().Aggregate((Pair)null, (s, x) => new Lisp.Pair(x, s));
            }

            public static Pair makeCons(params object[] args) {
                if (args.Length < 2) {
                    throw new ArgumentException("args.Length must greater than 1", nameof(args));
                }
                return (Pair)args.Reverse().Aggregate((s, x) => (object)new Lisp.Pair(x, s));
            }

            public static Symbol makeSym(string value) {
                return Symbol.FromString(value);
            }

            public static object makeNum(long value) {
                return value;
            }
            public static object makeFloat(double value) {
                return value;
            }
            public static object makeStr(string value) {
                return value;
            }

            public static object Nil = null;
        }

        public sealed class SchemeException : Exception { internal SchemeException(string message) : base(message) { } }
        public sealed class SchemeError : Exception { internal SchemeError(IEnumerable<object> parameters) : base(new Writer(true).Write(Pair.FromEnumerable(parameters))) { } }
        public sealed class ReaderException : Exception { public ReaderException(string message) : base(message) { } }
        public sealed class WriterException : Exception { public WriterException(string message) : base(message) { } }

        public sealed class PrintEventArgs : EventArgs {
            public readonly string WhatToPrint;
            internal PrintEventArgs(string whatToPrint) { WhatToPrint = whatToPrint; }
        }

        public sealed class Pair : IEnumerable<object> {
            public object First;
            public object Second;

            public Pair(object first, object second) { First = first; Second = second; }
            IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }

            public IEnumerator<object> GetEnumerator() {
                Pair i = this;
                while (true) {
                    yield return i.First;
                    if (i.Second == null) yield break;
                    if (!(i.Second is Pair)) {
                        yield return i.Second;
                        yield break;
                    }
                    i = (Pair)i.Second;
                }
            }

            public static Pair FromEnumerable(IEnumerable values) {
                Pair ret = null;
                Pair current = null;
                foreach (var newPair in from object o in values select new Pair(o, null)) {
                    if (current == null) {
                        ret = current = newPair;
                    } else {
                        current.Second = newPair;
                        current = newPair;
                    }
                }
                return ret;
            }

            public bool IsDottedList() {
                Pair i = this;
                while (true) {
                    if (i.Second == null) return false;
                    if (!(i.Second is Pair)) return true;
                    i = (Pair)i.Second;
                }
            }

            public override string ToString() {
                return new Writer(true).Write(this);
            }
        }

        public sealed class Symbol {
            private readonly string value;

            private Symbol(string value) { this.value = value; }
            public override string ToString() { return value; }

            private static readonly Dictionary<string, Symbol> cache = new Dictionary<string, Symbol>();
            public static Symbol FromString(string symbol) {
                lock (cache) {
                    if (cache.ContainsKey(symbol)) return cache[symbol];
                    Symbol ret = new Symbol(symbol);
                    cache[symbol] = ret;
                    return ret;
                }
            }
        }

        public sealed class Reader {
            private readonly StreamReader input;
            private static readonly Symbol dot = Symbol.FromString(".");
            internal static readonly Symbol listEnd = Symbol.FromString(")");
            public static readonly object EOF = new object();

            public Reader(StreamReader input) {
                this.input = input;
            }

            public object Read(bool throwOnEof = true) {
                SkipWhiteSpace();
                if (IsEof()) {
                    if (throwOnEof) throw new EndOfStreamException();
                    return EOF;
                }
                switch (PeekChar()) {
                    case ';': SkipComment(); return Read(throwOnEof);
                    case '\'': ReadChar(); return new Pair(Symbol.FromString("quote"), new Pair(Read(), null));
                    case '`': ReadChar(); return new Pair(Symbol.FromString("quasiquote"), new Pair(Read(), null));
                    case ',': ReadChar(); return new Pair(Symbol.FromString("unquote"), new Pair(Read(), null));
                    case '(': return ReadList();
                    case '"': return ReadString();
                    case '#': return ReadSpecial();
                    default: return ReadSymbolOrNumber();
                }
            }

            private void SkipWhiteSpace() { while (!IsEof() && char.IsWhiteSpace(PeekChar())) ReadChar(); }
            private void SkipComment() { while (!IsEof() && PeekChar() != '\n') ReadChar(); }
            private bool IsEof() { return input.EndOfStream; }
            private char PeekChar() { AssertNotEof(); return (char)input.Peek(); }
            private char ReadChar() { AssertNotEof(); return (char)input.Read(); }
            private void AssertNotEof() { if (IsEof()) throw new EndOfStreamException(); }

            private object ReadList() {
                ReadChar(); // Opening parenthesis
                Pair ret = null;
                Pair current = null;
                while (true) {
                    object o = Read();
                    if (o == listEnd) return ret; // Closing parenthesis
                    if (o == dot) {
                        if (current == null) throw new ReaderException("Invalid dotted list");
                        o = Read();
                        current.Second = o;
                        if (Read() != listEnd) throw new ReaderException("Invalid dotted list");
                        return ret;
                    }

                    var newPair = new Pair(o, null);
                    if (current == null) {
                        ret = current = newPair;
                    } else {
                        current.Second = newPair;
                        current = newPair;
                    }
                }
            }

            private object ReadString() {
                ReadChar(); // Opening quote
                var sb = new StringBuilder();
                while (PeekChar() != '"') {
                    char c = ReadChar();
                    if (c == '\\') {
                        c = ReadChar();
                        if (c == 'n') c = '\n';
                        if (c == 'r') c = '\r';
                        if (c == 't') c = '\t';
                    }
                    sb.Append(c);
                }
                ReadChar(); // Closing quote
                return sb.ToString();
            }

            private object ReadSpecial() {
                ReadChar(); // #
                if (PeekChar() == '(') return ((ReadList() as IEnumerable<object>) ?? new object[0]).ToArray();
                if (PeekChar() != '\\') return ReadSymbolOrNumber("#");
                ReadChar();
                return ReadCharacter();
            }

            private object ReadCharacter() {
                char c = ReadChar();
                if (!char.IsLetter(c)) return c;

                var sb = new StringBuilder();
                sb.Append(c);
                while (!IsEof() && PeekChar() != ')' && !char.IsWhiteSpace(PeekChar())) sb.Append(ReadChar());
                string name = sb.ToString();
                switch (name) {
                    case "cr": return '\r';
                    case "newline": return '\n';
                    case "space": return ' ';
                    case "tab": return '\t';
                    default:
                        if (name.Length == 1) return name[0];
                        throw new ReaderException("Invalid character name: \\" + name);
                }
            }

            private object ReadSymbolOrNumber(string init = "") {
                if (init == "" && PeekChar() == ')') {
                    ReadChar();
                    return listEnd;
                }

                var sb = new StringBuilder(init);
                while (!IsEof() && PeekChar() != ')' && !char.IsWhiteSpace(PeekChar())) sb.Append(ReadChar());
                string symbol = sb.ToString();

                long i; if (long.TryParse(symbol, out i)) return i;
                double d; if (double.TryParse(symbol, NumberStyles.Any, CultureInfo.InvariantCulture, out d)) return d;
                if (symbol == "#t") return true;
                if (symbol == "#f") return false;
                if (symbol == "nil") return null;
                if (symbol.StartsWith("#x")) return Convert.ToInt32(symbol.Substring(2), 16);
                if (symbol.StartsWith("#b")) return Convert.ToInt32(symbol.Substring(2), 2);
                return Symbol.FromString(symbol);
            }
        }

        public sealed class Writer {
            private readonly bool forDisplay;

            public Writer(bool forDisplay) { this.forDisplay = forDisplay; }

            public string Write(object o) {
                if (o == null) return "()";
                if (o is bool) return (bool)o ? "#t" : "#f";
                if (o is char) return forDisplay ? o.ToString() : WriteChar((char)o);
                if (o is long) return ((long)o).ToString(CultureInfo.InvariantCulture);
                if (o is double) return ((double)o).ToString(CultureInfo.InvariantCulture);
                if (o is string) return forDisplay ? o.ToString() : "\"" + ((string)o).Replace("\\", "\\\\").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t") + "\"";
                if (o is Symbol) return o.ToString();
                if (o is Pair) return WritePair((Pair)o);
                if (o is object[]) return "#" + WriteEnumerable((object[])o);
                if (o is IEnumerable) return WriteEnumerable((IEnumerable)o);

                if (o is float) return ((double)((float)o)).ToString(CultureInfo.InvariantCulture);
                if (o is short) return ((long)((short)o)).ToString(CultureInfo.InvariantCulture);
                if (o is ushort) return ((long)((ushort)o)).ToString(CultureInfo.InvariantCulture);
                if (o is uint) return ((uint)o).ToString(CultureInfo.InvariantCulture);
                if (o is long) return ((long)o).ToString(CultureInfo.InvariantCulture);
                if (o is ulong) return ((ulong)o).ToString(CultureInfo.InvariantCulture);
                if (o is byte) return ((long)((byte)o)).ToString(CultureInfo.InvariantCulture);
                if (o is sbyte) return ((long)((sbyte)o)).ToString(CultureInfo.InvariantCulture);
                return forDisplay ? o.ToString() : "#<CLR " + o + ">";
            }

            private static string WriteChar(char p) {
                if (p == '\n') return "#\\newline";
                if (p == '\r') return "#\\cr";
                if (p == ' ') return "#\\space";
                if (p == '\t') return "#\\tab";
                if (p < 32) throw new WriterException("Unable to serialize character with numeric code " + (int)p);
                return "#\\" + p;
            }

            private string WritePair(Pair pair) {
                var sb = new StringBuilder("(");
                while (true) {
                    sb.Append(Write(pair.First));
                    if (pair.Second == null) return sb + ")";
                    if (!(pair.Second is Pair)) {
                        sb.Append(" . ");
                        sb.Append(Write(pair.Second));
                        return sb + ")";
                    }
                    pair = (Pair)pair.Second;
                    sb.Append(' ');
                }
            }

            private string WriteEnumerable(IEnumerable values) {
                var sb = new StringBuilder("(");
                foreach (var o in values) {
                    sb.Append(Write(o));
                    sb.Append(' ');
                }
                if (sb.Length > 1) sb[sb.Length - 1] = ')'; else sb.Append(')');
                return sb.ToString();
            }
        }

        internal sealed class ClrClosure {
            private readonly object o;
            private readonly string name;

            public ClrClosure(object o, string name) {
                this.o = o;
                this.name = name;
            }

            public object Apply(object[] parameters) {
                if (parameters == null) parameters = new object[0];
                var info = o.GetType().GetMethod(name, parameters.Select(i => i.GetType()).ToArray());
                if (info != null) return info.Invoke(o, parameters);
                string noMethodFoundMessage = "No suitable method found with name " + name;
                try { info = o.GetType().GetMethod(name); } catch { throw new SchemeException(noMethodFoundMessage); }
                if (info == null) throw new SchemeException(noMethodFoundMessage);

                var parameterTypes = info.GetParameters().Select(p => p.ParameterType).ToArray();
                if (parameterTypes.Length != parameters.Length) throw new SchemeException(noMethodFoundMessage);

                for (int i = 0; i < parameters.Length; ++i)
                    if (parameterTypes[i] != parameters[i].GetType())
                        parameters[i] = parameterTypes[i].IsEnum ? Enum.Parse(parameterTypes[i], parameters[i].ToString()) : Convert.ChangeType(parameters[i], parameterTypes[i]);

                return info.Invoke(o, parameters.ToArray());
            }

            public override string ToString() {
                return "<CLR closure " + o.GetType() + "." + name + ">";
            }
        }

        internal sealed class Lambda {
            private readonly SchemeInterpreter Interpreter;
            private readonly Symbol Name;
            private readonly Symbol[] ParameterNames;
            private readonly bool HasRestParameter;
            private readonly Environment OuterEnvironment;
            private readonly List<Func<Environment, object>> Forms;

            public Lambda(SchemeInterpreter interp, Symbol name, Symbol[] parameterNames, bool hasRestParameter, Environment env, List<Func<Environment, object>> forms) {
                Interpreter = interp;
                Name = name;
                ParameterNames = parameterNames;
                HasRestParameter = hasRestParameter;
                OuterEnvironment = env;
                Forms = forms;
            }

            internal object Apply(object[] parameters) {
                try {
                    Environment env = new Environment(OuterEnvironment);
                    if (HasRestParameter) PrepareWithRest(env, parameters); else PrepareWithoutRest(env, parameters);
                    return ExecuteForms(env);
                } catch (Exception ex) {
                    throw new SchemeException(Name + ": " + ex.Message);
                }
            }

            private void PrepareWithRest(Environment env, object[] parameters) {
                if (parameters.Length < ParameterNames.Length - 1) throw new Exception("Invalid parameter count");
                for (int i = 0; i < ParameterNames.Length - 1; ++i) env.Define(ParameterNames[i], parameters[i]);
                env.Define(ParameterNames.Last(), Pair.FromEnumerable(parameters.Skip(ParameterNames.Length - 1)));
            }

            private void PrepareWithoutRest(Environment env, object[] parameters) {
                if (parameters.Length != ParameterNames.Length) throw new Exception("Invalid parameter count");
                for (int i = 0; i < ParameterNames.Length; ++i) env.Define(ParameterNames[i], parameters[i]);
            }

            private object ExecuteForms(Environment env) {
                for (int i = 0; i < Forms.Count - 1; ++i) Interpreter.TrampolineLoop(Forms[i], env);
                return Forms.Last()(env);
            }
        }

        internal sealed class Environment {
            private readonly Environment outer;
            private readonly Dictionary<Symbol, object> values = new Dictionary<Symbol, object>();

            public Environment() { outer = null; }
            public Environment(Environment outer) { this.outer = outer; }

            public void Define(Symbol identifier, object value) {
                switch (identifier.ToString()) {
                    case "if":
                    case "define":
                    case "set!":
                    case "lambda":
                    case "quote":
                    case "begin":
                        throw new SchemeException("Symbol '" + identifier + "' is constant and must not be changed");
                    default:
                        values[identifier] = value;
                        break;
                }
            }

            public void Set(Symbol identifier, object value) {
                if (values.ContainsKey(identifier)) values[identifier] = value;
                else if (outer != null) outer.Set(identifier, value);
                else throw new SchemeException("Unknown variable '" + identifier + "'");
            }

            public object Get(Symbol identifier) {
                object ret;
                if (values.TryGetValue(identifier, out ret)) return ret;
                if (outer != null) return outer.Get(identifier);
                throw new SchemeException("Unknown variable '" + identifier + "'");
            }
        }

        public sealed class SchemeInterpreter {
            private static readonly Writer writer = new Writer(false);
            private static readonly Writer displayWriter = new Writer(true);
            private static readonly Symbol undefinedSymbol = Symbol.FromString("undefined");
            private static readonly Symbol fixnumOverflowSymbol = Symbol.FromString("fixnum-overflow");

            private object tailCallProcedure;
            private object[] tailCallParameters;
            private static readonly object tailCall = new object();

            private readonly Environment global = new Environment();
            private readonly Random random = new Random();
            private readonly Dictionary<string, Lambda> Macros = new Dictionary<string, Lambda>();

            public event EventHandler<PrintEventArgs> InterpreterWantsToPrint = delegate { };
            private void Print(string whatToPrint) { InterpreterWantsToPrint(this, new PrintEventArgs(whatToPrint)); }

            public void AddFunction(string identifier, Func<object> f) { AddFunction(identifier, (Delegate)f); }
            public void AddFunction<T1>(string identifier, Func<T1, object> f) { AddFunction(identifier, (Delegate)f); }
            public void AddFunction<T1, T2>(string identifier, Func<T1, T2, object> f) { AddFunction(identifier, (Delegate)f); }
            public void AddFunction<T1, T2, T3>(string identifier, Func<T1, T2, T3, object> f) { AddFunction(identifier, (Delegate)f); }
            public void AddFunction<T1, T2, T3, T4>(string identifier, Func<T1, T2, T3, T4, object> f) { AddFunction(identifier, (Delegate)f); }

            public void SetVariable(string identifier, object value) { global.Define(Symbol.FromString(identifier), value); }
            public void AddFunction(string identifier, Delegate f) { SetVariable(identifier, f); }
            public static string ObjectToString(object value, bool forDisplay) { try { return forDisplay ? displayWriter.Write(value) : writer.Write(value); } catch { return value.ToString(); } }

            public object Evaluate(string expression) {
                using (var input = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(expression)))) {
                    var reader = new Reader(input);

                    object ret = null;
                    while (true) {
                        object o = reader.Read(false);
                        if (o == Reader.EOF) return ret;
                        if (o == Reader.listEnd) throw new ReaderException("Extraneous )");
                        HandleMacros(ref o);
                        ret = TrampolineLoop(Analyze(o), global);
                    }
                }
            }
            public object Eval(object o) {
                HandleMacros(ref o);
                return TrampolineLoop(Analyze(o), global);
            }

            private void HandleMacros(ref object obj) {
                if (obj == null) return;
                if (!(obj is Pair)) return;

                while (true) if (!ExpandMacros(ref obj)) break;
                if (!(((Pair)obj).First is Symbol)) return;
                var form = ((Pair)obj).ToList();
                if (form[0].ToString() != "defmacro") return;

                if (!(form[1] is Symbol)) throw new SchemeException("Invalid defmacro form: Name must be a symbol");
                string name = form[1].ToString();
                var lambda = AnalyzeLambdaSpecialForm(form.Skip(1).ToArray());
                Macros[name] = (Lambda)lambda(global);
                obj = true;
            }

            private bool ExpandMacros(ref object obj) {
                if (obj == null) return false;
                if (!(obj is Pair)) return false;
                if (Symbol.FromString("quote") == ((Pair)obj).First) return false;

                for (object i = obj; i is Pair; i = ((Pair)i).Second) if (ExpandMacros(ref ((Pair)i).First)) return true;

                Symbol o1 = ((Pair)obj).First as Symbol;
                if (o1 == null) return false;
                if (!Macros.ContainsKey(o1.ToString())) return false;

                Lambda l = Macros[o1.ToString()];
                var parameters = (Pair)((Pair)obj).Second;
                obj = parameters == null ? l.Apply(new object[0]) : l.Apply(parameters.ToArray());
                while (obj == tailCall) obj = Apply(tailCallProcedure, true, tailCallParameters);
                return true;
            }

            public SchemeInterpreter() {
                AddFunction("cons", (object a, object b) => new Pair(a, b));
                AddFunction("car", (Pair a) => a.First);
                AddFunction("cdr", (Pair a) => a.Second);
                AddFunction("set-car!", (Pair a, object o) => a.First = o);
                AddFunction("set-cdr!", (Pair a, object o) => a.Second = o);
                AddFunction("+", MakeNumericalFunction("+", (i1, i2) => checked(i1 + i2), (d1, d2) => d1 + d2));
                AddFunction("-", MakeNumericalFunction("-", (i1, i2) => i1 - i2, (d1, d2) => d1 - d2));
                AddFunction("*", MakeNumericalFunction("*", (i1, i2) => checked(i1 * i2), (d1, d2) => d1 * d2));
                AddFunction("/", MakeNumericalFunction("/", (i1, i2) => i1 / i2, (d1, d2) => d1 / d2));
                AddFunction("<", MakeNumericalFunction("<", (i1, i2) => i1 < i2, (d1, d2) => d1 < d2));
                AddFunction(">", MakeNumericalFunction(">", (i1, i2) => i1 > i2, (d1, d2) => d1 > d2));
                AddFunction("=", MakeNumericalFunction("=", (i1, i2) => i1 == i2, (d1, d2) => d1 == d2));
                AddFunction("exact->inexact", (long i) => (double)i);
                AddFunction("sin", (double d) => Math.Sin(d));
                AddFunction("cos", (double d) => Math.Cos(d));
                AddFunction("tan", (double d) => Math.Tan(d));
                AddFunction("sqrt", (object a) => Math.Sqrt(Convert.ToDouble(a)));
                AddFunction("expt", (object b, object exp) => Math.Pow(Convert.ToDouble(b), Convert.ToDouble(exp)));
                AddFunction("quotient", MakeNumericalFunction("quotient", (i1, i2) => i1 / i2, (d1, d2) => (long)(d1 / d2)));
                AddFunction("remainder", MakeNumericalFunction("remainder", (i1, i2) => i1 % i2, (d1, d2) => (long)d1 % (long)d2));
                AddFunction("eq?", (object a, object b) => a == b);
                AddFunction("pair?", (object a) => a is Pair);
                AddFunction("null?", (object a) => a == null);
                AddFunction("list?", (object a) => a == null || a is Pair && !((Pair)a).IsDottedList());
                AddFunction("string?", (object a) => a is string);
                AddFunction("number?", (object a) => a != null && (a is double || a is long));
                AddFunction("char?", (object a) => a is char);
                AddFunction("boolean?", (object a) => a is bool);
                AddFunction("symbol?", (object a) => a is Symbol);
                AddFunction("integer?", (object a) => a is long);
                AddFunction("real?", (object a) => a is double);
                AddFunction("procedure?", (object a) => a != null && (a is Lambda || a is Delegate));
                AddFunction("random", (long a) => random.Next((int)a));
                AddFunction("write", (object a) => { Print(ObjectToString(a, false)); return undefinedSymbol; });
                AddFunction("display", (object a) => { Print(ObjectToString(a, true)); return undefinedSymbol; });
                AddFunction("char=?", (char a, char b) => a == b);
                AddFunction("char>?", (char a, char b) => a > b);
                AddFunction("char<?", (char a, char b) => a < b);
                AddFunction("char-ci=?", (char a, char b) => char.ToLowerInvariant(a) == char.ToLowerInvariant(b));
                AddFunction("char-ci>?", (char a, char b) => char.ToLowerInvariant(a) > char.ToLowerInvariant(b));
                AddFunction("char-ci<?", (char a, char b) => char.ToLowerInvariant(a) < char.ToLowerInvariant(b));
                AddFunction("char-alphabetic?", (char a) => char.IsLetter(a)); // HACK: Re-code in Scheme
                AddFunction("char-numeric?", (char a) => char.IsDigit(a)); // HACK: Re-code in Scheme
                AddFunction("char-whitespace?", (char a) => char.IsWhiteSpace(a)); // HACK: Re-code in Scheme
                AddFunction("char-upper-case?", (char a) => char.IsUpper(a)); // HACK: Re-code in Scheme
                AddFunction("char-lower-case?", (char a) => char.IsLower(a)); // HACK: Re-code in Scheme
                AddFunction("char-upcase", (char a) => char.ToUpperInvariant(a)); // HACK: Re-code in Scheme
                AddFunction("char-downcase", (char a) => char.ToLowerInvariant(a)); // HACK: Re-code in Scheme
                AddFunction("string=?", (string a, string b) => String.Compare(a, b, false, CultureInfo.InvariantCulture) == 0);
                AddFunction("string>?", (string a, string b) => String.Compare(a, b, false, CultureInfo.InvariantCulture) > 0);
                AddFunction("string<?", (string a, string b) => String.Compare(a, b, false, CultureInfo.InvariantCulture) < 0);
                AddFunction("string-ci=?", (string a, string b) => String.Compare(a, b, true, CultureInfo.InvariantCulture) == 0);
                AddFunction("string-ci>?", (string a, string b) => String.Compare(a, b, true, CultureInfo.InvariantCulture) > 0);
                AddFunction("string-ci<?", (string a, string b) => String.Compare(a, b, true, CultureInfo.InvariantCulture) < 0);
                AddFunction("string-length", (string a) => a.Length);
                AddFunction("substring", (string a, long start, long end) => a.Substring((int)start, (int)(end - start))); // HACK: Re-code in Scheme
                AddFunction("string-append", (string a, string b) => a + b); // HACK: Re-code in Scheme
                AddFunction("char->integer", (char c) => (long)c);
                AddFunction("integer->char", (long i) => (char)i);
                AddFunction("string-ref", (string s, long index) => s[(int)index]);
                AddFunction("string->symbol", (string s) => Symbol.FromString(s));
                AddFunction("symbol->string", (Symbol s) => s.ToString());
                AddFunction("string->list", (string s) => Pair.FromEnumerable(s.ToCharArray().Cast<object>())); // HACK: Re-code in Scheme
                AddFunction("list->string", (IEnumerable<object> list) => list == null ? "" : new string(list.Cast<char>().ToArray())); // HACK: Re-code in Scheme
                AddFunction("length", (IEnumerable<object> a) => a == null ? 0 : a.Count());
                AddFunction("reverse", (IEnumerable<object> list) => list == null ? null : Pair.FromEnumerable(list.Reverse())); // HACK: Re-code in Scheme
                AddFunction("sys:strtonum", (string s, long b) => s.Contains('.') ? Convert.ToDouble(s, CultureInfo.InvariantCulture) : Convert.ToInt64(s, (int)b)); // HACK: Re-code in Scheme
                AddFunction("sys:numtostr", (object i, long b) => (i is long) ? Convert.ToString((long)i, (int)b) : Convert.ToString((double)i)); // HACK: Re-code in Scheme
                AddFunction("map", (object f, IEnumerable<object> list) => list == null ? null : Pair.FromEnumerable(list.Select(i => Apply(f, false, i)))); // HACK: Re-code in Scheme
                AddFunction("for-each", (object f, IEnumerable<object> list) => list == null ? 0 : list.Select(i => Apply(f, false, i)).Count()); // HACK: Re-code in Scheme
                AddFunction("filter", (object f, IEnumerable<object> list) => list == null ? null : Pair.FromEnumerable(list.Where(i => EvaluatesToTrue(Apply(f, false, i))))); // HACK: Re-code in Scheme
                AddFunction("all?", (object f, IEnumerable<object> list) => list == null || list.All(i => EvaluatesToTrue(Apply(f, false, i)))); // HACK: Re-code in Scheme
                AddFunction("any?", (object f, IEnumerable<object> list) => list != null && list.Any(i => EvaluatesToTrue(Apply(f, false, i)))); // HACK: Re-code in Scheme
                AddFunction("sort", (IEnumerable<object> list, object f) => list == null ? null : Pair.FromEnumerable(Sort(list.ToList(), f))); // HACK: Re-code in Scheme
                AddFunction("apply", (object f, IEnumerable<object> arguments) => arguments == null ? Apply(f, false) : Apply(f, false, arguments.ToArray()));
                AddFunction("sys:make-vector", (long size) => new object[(int)size]);
                AddFunction("vector-ref", (object[] vector, long index) => vector[(int)index]);
                AddFunction("vector-length", (object[] vector) => vector.Length);
                AddFunction("vector-set!", (object[] vector, long index, object obj) => { vector[(int)index] = obj; return undefinedSymbol; });
                AddFunction("vector?", (object o) => o is object[]);
                AddFunction("wall-time", (object f) => { var sw = new System.Diagnostics.Stopwatch(); sw.Start(); Apply(f, false); sw.Stop(); return (long)sw.ElapsedMilliseconds; });
                AddFunction<object, object>("eqv?", Eqv);
                AddFunction<object, object>("equal?", Equal);
                AddFunction<IEnumerable<object>>("sys:error", ErrorFunction);

                AddFunction("sys:read-file", (string fileName) => File.ReadAllText(fileName));
                AddFunction("sys:write-file", (string fileName, string contents) => { File.WriteAllText(fileName, contents); return true; });
                AddFunction("sys:string->object", (string value) => new Reader(new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(value)))).Read());
                AddFunction("sys:object->string", (object o) => ObjectToString(o, false));

                AddFunction("lb:sleep", (long ms) => { Thread.Sleep((int)ms); return undefinedSymbol; });
                // TODO: string-set, string-fill!, make-string, string-copy. Impossible with .NET strings.
                AddFunction("lb:clr-method", (object o, object name) => new ClrClosure(o, name.ToString()));
                AddFunction("lb:clr-property-names", (object o) => o.GetType().GetProperties().Select(i => Symbol.FromString(i.Name)).ToList());
                AddFunction("lb:clr-method-names", (object o) => o.GetType().GetMethods().Select(i => Symbol.FromString(i.Name)).ToList());
                AddFunction("lb:clr-get", (object o, Symbol name) => GetClrProperty(o, name).GetValue(o, new object[0]));
                AddFunction("lb:clr-set", (object o, Symbol name, object value) => SetClrProperty(o, name, value));
                AddFunction("lb:clr-new", (object className, IEnumerable<object> parameters) => {
                    Type type = Type.GetType(className.ToString(), true);
                    return parameters == null ? Activator.CreateInstance(type) : Activator.CreateInstance(type, parameters.ToArray());
                });
                AddFunction("lb:clr->scheme", (object o) => ClrToScheme(o));

                AddFunction("standard-input-port", () => new StreamReader(System.Console.OpenStandardInput()));
                AddFunction<StreamReader>("read", (StreamReader reader) => {
                    var r = new Reader(reader);
                    var ret = r.Read();
                    return ret;
                });
                AddFunction("write-char", (char a) => { Print(ObjectToString((char)a, true)); return undefinedSymbol; });

                AddFunction<object>("eval", (object o) => {
                    try {
                        return this.Eval(o);
                    }
                    catch (Exception e) {
                        Print(e.Message);
                        return undefinedSymbol;
                    } });

                Evaluate(initScript);
            }

            private static object Eqv(object a, object b) {
                if (a == b) return true;
                if (a is double && b is double) return ((double)a) == ((double)b);
                if (a is bool && b is bool) return ((bool)a) == ((bool)b);
                if (a is char && b is char) return ((char)a) == ((char)b);
                if (a is long && b is long) return ((long)a) == ((long)b);
                return false;
            }

            private static object Equal(object a, object b) {
                if ((bool)Eqv(a, b)) return true;
                if (a is string && b is string) return string.Equals(a, b);
                if (!(a is IEnumerable<object>) || !(b is IEnumerable<object>)) return false;

                var ea = ((IEnumerable<object>)a).GetEnumerator();
                var eb = ((IEnumerable<object>)b).GetEnumerator();
                while (true) {
                    bool b1 = ea.MoveNext();
                    bool b2 = eb.MoveNext();
                    if (b1 != b2) return false; // Different lengths
                    if (!b1) return true; // Comparison done, all equal
                    if (!(bool)Equal(ea.Current, eb.Current)) return false;
                }
            }

            private static object SetClrProperty(object dotNetObject, Symbol name, object value) {
                object ret = value;
                var property = GetClrProperty(dotNetObject, name);
                if (property.PropertyType != value.GetType()) value = property.PropertyType.IsEnum ? Enum.Parse(property.PropertyType, value.ToString()) : Convert.ChangeType(value, property.PropertyType);
                property.SetValue(dotNetObject, value, new object[0]);
                return ret;
            }

            private static PropertyInfo GetClrProperty(object dotNetObject, Symbol name) {
                var property = dotNetObject.GetType().GetProperty(name.ToString());
                if (property == null) throw new SchemeException("Property not found: " + name);
                return property;
            }

            private static object ClrToScheme(object obj) {
                if (obj is IDictionary<string, object>) {
                    IDictionary<string, object> asDict = (IDictionary<string, object>)obj;
                    return Pair.FromEnumerable(asDict.Keys.Select(key => new Pair(Symbol.FromString(":" + key), ClrToScheme(asDict[key]))).ToList());
                }

                if (obj is IEnumerable<object>) {
                    return Pair.FromEnumerable(((IEnumerable<object>)obj).Select(ClrToScheme));
                }

                return obj;
            }

            private IEnumerable<object> Sort(List<object> list, object f) {
                list.Sort((o1, o2) => EvaluatesToTrue(Apply(f, false, o1, o2)) ? -1 : 1);
                return list;
            }

            private static object ErrorFunction(IEnumerable<object> parameters) {
                throw new SchemeError(parameters);
            }

            private static Func<object, object, object> MakeNumericalFunction(string name, Func<long, long, object> iF, Func<double, double, object> dF) {
                return (o1, o2) => {
                    if (o1 is float) o1 = (double)(float)o1;
                    if (o2 is float) o2 = (double)(float)o2;
                    if (o1 == fixnumOverflowSymbol || o2 == fixnumOverflowSymbol) return fixnumOverflowSymbol;
                    if (!(o1 is long) && !(o1 is double)) throw new SchemeException(name + ": Invalid argument type in arg 1, expected long or double, got " + o1.GetType());
                    if (!(o2 is long) && !(o2 is double)) throw new SchemeException(name + ": Invalid argument type in arg 2, expected long or double, got " + o2.GetType());
                    try {
                        return o1 is long && o2 is long
                                   ? iF((long)o1, (long)o2)
                                   : dF(Convert.ToDouble(o1, CultureInfo.InvariantCulture), Convert.ToDouble(o2, CultureInfo.InvariantCulture));
                    } catch (OverflowException) {
                        return fixnumOverflowSymbol;
                    }
                };
            }

            private const string initScript =
                "(define (complex? obj) #f)" +
                "(define (rational? obj) #f)" +
                "(define exact? integer?)" +
                "(define inexact? real?)" +
                "(define (caar x) (car (car x)))" +
                "(define (cadr x) (car (cdr x)))" +
                "(define (cdar x) (cdr (car x)))" +
                "(define (cddr x) (cdr (cdr x)))" +
                "(define (caaar x) (car (car (car x))))" +
                "(define (caadr x) (car (car (cdr x))))" +
                "(define (cadar x) (car (cdr (car x))))" +
                "(define (caddr x) (car (cdr (cdr x))))" +
                "(define (cdaar x) (cdr (car (car x))))" +
                "(define (cdadr x) (cdr (car (cdr x))))" +
                "(define (cddar x) (cdr (cdr (car x))))" +
                "(define (cdddr x) (cdr (cdr (cdr x))))" +
                "(define (caaaar x) (car (car (car (car x)))))" +
                "(define (caaadr x) (car (car (car (cdr x)))))" +
                "(define (caadar x) (car (car (cdr (car x)))))" +
                "(define (caaddr x) (car (car (cdr (cdr x)))))" +
                "(define (cadaar x) (car (cdr (car (car x)))))" +
                "(define (cadadr x) (car (cdr (car (cdr x)))))" +
                "(define (caddar x) (car (cdr (cdr (car x)))))" +
                "(define (cadddr x) (car (cdr (cdr (cdr x)))))" +
                "(define (cdaaar x) (cdr (car (car (car x)))))" +
                "(define (cdaadr x) (cdr (car (car (cdr x)))))" +
                "(define (cdadar x) (cdr (car (cdr (car x)))))" +
                "(define (cdaddr x) (cdr (car (cdr (cdr x)))))" +
                "(define (cddaar x) (cdr (cdr (car (car x)))))" +
                "(define (cddadr x) (cdr (cdr (car (cdr x)))))" +
                "(define (cdddar x) (cdr (cdr (cdr (car x)))))" +
                "(define (cddddr x) (cdr (cdr (cdr (cdr x)))))" +
                "(define (list . lst) lst)" +
                "(define (flip f) (lambda (a b) (f b a)))" +
                "(define (newline) (display \"\\n\") 'undefined)" +
                "(define (zero? x) (= x 0))" +
                "(define (positive? x) (> x 0))" +
                "(define (negative? x) (< x 0))" +
                "(define (<= a b) (if (> a b) #f #t))" +
                "(define (>= a b) (if (< a b) #f #t))" +
                "(define (char>=? a b) (if (char<? a b) #f #t))" +
                "(define (char<=? a b) (if (char>? a b) #f #t))" +
                "(define (char-ci>=? a b) (if (char-ci<? a b) #f #t))" +
                "(define (char-ci<=? a b) (if (char-ci>? a b) #f #t))" +
                "(define (string>=? a b) (if (string<? a b) #f #t))" +
                "(define (string<=? a b) (if (string>? a b) #f #t))" +
                "(define (string-ci>=? a b) (if (string-ci<? a b) #f #t))" +
                "(define (string-ci<=? a b) (if (string-ci>? a b) #f #t))" +
                "(define (error . params) (sys:error params))" +
                "(define (abs x) (if (positive? x) x (- 0 x)))" +
                "(define (sys:sign x) (if (>= x 0) 1 -1))" +
                "(define (modulo a b) (if (= (sys:sign a) (sys:sign b)) (remainder a b) (+ b (remainder a b))))" +
                "(define (even? x) (zero? (remainder x 2)))" +
                "(define (odd? x) (if (even? x) #f #t))" +
                "(define (not x) (if x #f #t))" +
                "(define (string . values) (list->string values))" +
                "(define (list-tail lst k) (if (zero? k) lst (list-tail (cdr lst) (- k 1))))" +
                "(define (list-ref lst k) (car (list-tail lst k)))" +
                "(define (string->number n . rest) (if (pair? rest) (sys:strtonum n (car rest)) (sys:strtonum n 10)))" +
                "(define (number->string n . rest) (if (pair? rest) (sys:numtostr n (car rest)) (sys:numtostr n 10)))" +
                "(define (sys:gcd-of-two a b) (if (= b 0) a (sys:gcd-of-two b (remainder a b))))" +
                "(define (sys:lcm-of-two a b) (/ (* a b) (sys:gcd-of-two a b)))" +
                "(define (fold f acc lst) (if (null? lst) acc (fold f (f (car lst) acc) (cdr lst))))" +
                "(define (reduce f ridentity lst) (if (null? lst) ridentity (fold f (car lst) (cdr lst))))" +
                "(define (gcd . args) (if (null? args) 0 (abs (fold sys:gcd-of-two (car args) (cdr args)))))" +
                "(define (lcm . args) (if (null? args) 1 (abs (fold sys:lcm-of-two (car args) (cdr args)))))" +
                "(define (append . lsts) (define (iter current acc) (if (pair? current) (iter (cdr current) (cons (car current) acc)) acc)) (reverse (fold iter '() lsts)))" +
                "(define (memq obj lst) (if (pair? lst) (if (eq? obj (car lst)) lst (memq obj (cdr lst))) #f))" +
                "(define (memv obj lst) (if (pair? lst) (if (eqv? obj (car lst)) lst (memv obj (cdr lst))) #f))" +
                "(define (member obj lst) (if (pair? lst) (if (equal? obj (car lst)) lst (member obj (cdr lst))) #f))" +
                "(define (assq obj lst) (if (pair? lst) (if (eq? obj (caar lst)) (car lst) (assq obj (cdr lst))) #f))" +
                "(define (assv obj lst) (if (pair? lst) (if (eqv? obj (caar lst)) (car lst) (assv obj (cdr lst))) #f))" +
                "(define (assoc obj lst) (if (pair? lst) (if (equal? obj (caar lst)) (car lst) (assoc obj (cdr lst))) #f))" +
                "(defmacro quasiquote (value) (define (qq i) (if (pair? i) (if (eq? 'unquote (car i)) (cadr i) (cons 'list (map qq i))) (list 'quote i))) (qq value))" +
                "(defmacro letrec (lst . forms) (cons (append '(lambda) (list (map car lst)) (map (lambda (i) (list 'set! (car i) (cadr i))) lst) forms) (map (lambda (x) #f) lst)))" +
                //"(defmacro let (lst . forms) (cons (cons 'lambda (cons (map car lst) forms)) (map cadr lst)))" +
                "(defmacro let data (if (symbol? (car data)) (cons 'letrec (cons (list (cons (car data) (list (cons 'lambda (cons (map car (cadr data)) (cddr data)))))) (list (cons (car data) (map cadr (cadr data)))))) (cons (cons 'lambda (cons (map car (car data)) (cdr data))) (map cadr (car data)))))" +
                "(defmacro let* (lst . forms) (if (null? lst) (cons 'begin forms) (list 'let (list (car lst)) (cons 'let* (cons (cdr lst) forms)))))" +
                "(defmacro cond list-of-forms (define (expand-cond lst) (if (null? lst) #f (if (eq? (caar lst) 'else) (cons 'begin (cdar lst)) (list 'if (caar lst) (cons 'begin (cdar lst)) (expand-cond (cdr lst)))))) (expand-cond list-of-forms))" +
                "(defmacro and list-of-forms (if (null? list-of-forms) #t (if (null? (cdr list-of-forms)) (car list-of-forms) (list 'if (car list-of-forms) (append '(and) (cdr list-of-forms)) #f))))" +
                "(defmacro delay (expression) (list 'let '((##forced_value (quote ##not_forced_yet))) (list 'lambda '() (list 'if '(eq? ##forced_value (quote ##not_forced_yet)) (list 'set! '##forced_value expression)) '##forced_value)))" +
                "(define (force promise) (promise))" +
                "(define (min . args) (define (min-of-two a b) (if (< a b) a b)) (let ((l (length args))) (cond ((= 0 l) (error \"min called without parameters\")) ((= 1 l) (car args)) (else (fold min-of-two (car args) (cdr args))))))" +
                "(define (max . args) (define (max-of-two a b) (if (> a b) a b)) (let ((l (length args))) (cond ((= 0 l) (error \"max called without parameters\")) ((= 1 l) (car args)) (else (fold max-of-two (car args) (cdr args))))))" +
                "(define (find-tail f lst) (cond ((null? lst) #f) ((f (car lst)) lst) (else (find-tail f (cdr lst)))))" +
                "(define (find f lst) (cond ((null? lst) #f) ((f (car lst)) (car lst)) (else (find f (cdr lst)))))" +
                "(define (drop-while f lst) (cond ((null? lst) '()) ((f (car lst)) (drop-while f (cdr lst))) (else lst)))" +
                "(define (take-while f lst) (define (iter l acc) (cond ((null? l) acc) ((f (car l)) (iter (cdr l) (cons (car l) acc))) (else acc))) (reverse (iter lst '())))" +
                "(define (take lst i) (define (iter l totake acc) (cond ((null? l) acc) ((zero? totake) acc) (else (iter (cdr l) (- totake 1) (cons (car l) acc))))) (reverse (iter lst i '())))" +
                "(define drop list-tail)" +
                "(define (last-pair lst) (if (null? (cdr lst)) lst (last-pair (cdr lst))))" +
                "(define (last lst) (car (last-pair lst)))" +
                "(define (dotted-list? lst) (if (null? lst) #f (if (pair? lst) (dotted-list? (cdr lst)) #t)))" +
                "(define (make-proper-list lst) (define (iter i acc) (cond ((pair? i) (iter (cdr i) (cons (car i) acc))) ((null? i) acc) (else (cons i acc)))) (reverse (iter lst '())))" +
                "(defmacro when (expr . body) `(if ,expr ,(cons 'begin body) #f))" +
                "(defmacro unless (expr . body) `(if ,expr #f ,(cons 'begin body)))" +
                "(defmacro aif (expr then . rest) `(let ((it ,expr)) (if it ,then ,(if (null? rest) #f (car rest)))))" +
                "(defmacro awhen (expr . then) `(let ((it ,expr)) (if it ,(cons 'begin then) #f)))" +
                "(defmacro or args (if (null? (cdr args)) (car args) (list 'aif (car args) 'it (cons 'or (cdr args)))))" +
                "(define (sys:count upto f) (define (iter i) (if (= i upto) 'undefined (begin (f i) (iter (+ i 1))))) (iter 0))" +
                "(defmacro dotimes (lst . body) (list 'sys:count (cadr lst) (cons 'lambda (cons (list (car lst)) body))))" +
                "(defmacro dolist (lst . forms) (list 'for-each (cons 'lambda (cons (list (car lst)) forms)) (cadr lst)))" +
                "(define gensym (let ((sym 0)) (lambda () (set! sym (+ sym 1)) (string->symbol (string-append \"##gensym##\" (number->string sym))))))" +
                "(defmacro do (vars pred . body) (let ((symbol (gensym))) `(let ((,symbol '())) (set! ,symbol (lambda ,(map car vars) (if ,(car pred) ,(cadr pred) ,(cons 'begin (append body (list (cons symbol (map caddr vars)))))))) ,(cons symbol (map cadr vars))))) " +
                "(defmacro while (exp . body) (cons 'do (cons '() (cons `((not ,exp) 'undefined) body))))" +
                "(define (flatten lst) (define (iter i acc) (cond ((null? i) acc) ((pair? (car i)) (iter (cdr i) (iter (car i) acc))) (else (iter (cdr i) (cons (car i) acc))))) (reverse (iter lst '())))" +
                "(define (print . args) (for-each display (flatten args)) (newline))" +
                "(define (lb:partial-apply proc . cargs) (lambda args (apply proc (append cargs args))))" +
                "(define (lb:range from to) (define (iter i acc) (if (> from i) acc (iter (- i 1) (cons i acc)))) (iter to '()))" +
                "(define (lb:count from to f) (if (< to from) '() (begin (f from) (lb:count (+ 1 from) to f))))" +
                "(defmacro lb:with-range (var from to . body) (list 'lb:count from to (append (list 'lambda (list var)) body)))" +
                "(define (lb:split str sep) (define (iter acc cur s) (cond ((string=? s \"\") (reverse (cons cur acc))) ((char=? (string-ref s 0) sep) (iter (cons cur acc) \"\" (substring s 1 (string-length s)))) (else (iter acc (string-append cur (substring s 0 1)) (substring s 1 (string-length s)))))) (iter '() \"\" str))" +
                "(define (vector-fill! v obj) (lb:with-range i 0 (- (vector-length v) 1) (vector-set! v i obj)) 'unspecified)" +
                "(define (make-vector . args) (let ((v (sys:make-vector (car args)))) (if (null? (cdr args)) v (begin (vector-fill! v (cadr args)) v))))" +
                "(define (list->vector lst) (define (iter v i vals) (vector-set! v i (car vals)) (if (zero? i) v (iter v (- i 1) (cdr vals)))) (let ((v (sys:make-vector (length lst)))) (if (zero? (vector-length v)) v (iter v (- (vector-length v) 1) (reverse lst)))))" +
                "(define (vector->list v) (define (iter i acc) (if (< i 0) acc (iter (- i 1) (cons (vector-ref v i) acc)))) (iter (- (vector-length v) 1) '()))" +
                "(define (vector . lst) (list->vector lst))" +
                "(define (lb:clr. object name . params) (let ((closure (lb:clr-method object name))) (if (null? closure) (error \"Method not found:\" name) (apply closure params))))" +
                "(define (lb:clr-properties obj) (map (lambda (name) (cons name (lb:clr-get obj name))) (lb:clr-property-names obj)))" +
                "(define (sys:test-assertion name value) (if value 'ok (error 'Assertion 'failed: name)))" +
                "(defmacro assert (form) `(sys:test-assertion (quote ,form) ,form))" +

                "(define (xml->tree node)" +
                  "(define (attributes) (let ((lst (clr-get node 'Attributes))) (map (lambda (i) (xml->tree (clr. lst 'Item i))) (range 0 (- (clr-get lst 'Count) 1)))))" +
                  "(define (sub-nodes) (let ((children (clr-get node 'ChildNodes))) (map (lambda (i) (xml->tree (clr. children 'Item i))) (range 0 (- (clr-get children 'Count) 1)))))" +
                  "(let ((name (string->symbol (clr-get node 'Name))) (type (string->symbol (clr. (clr-get node 'NodeType) 'ToString))))" +
                    "(cond ((eq? type 'Element)   (list name (attributes) (sub-nodes)))" +
                    "((eq? type 'Text)      (clr-get node 'InnerText))" +
                    "((eq? type 'Attribute) (cons name (clr-get (clr-get node 'FirstChild) 'InnerText)))" +
                    "(else type))))" +

                "(define any any?)" +
                "(define every all?)" +
                "(let ((original display)) (set! display (lambda args (for-each original args))))" +
                "(let ((original +)) (set! + (lambda args (fold original 0 args))))" +
                "(let ((original *)) (set! * (lambda args (fold original 1 args))))" +
                "(let ((original -)) (set! - (lambda args (if (null? (cdr args)) (original 0 (car args)) (fold (flip original) (car args) (cdr args))))))" +
                "(let ((original /)) (set! / (lambda args (if (null? (cdr args)) (original 1 (car args)) (fold (flip original) (car args) (cdr args))))))" +
                "(let ((original string-append)) (set! string-append (lambda args (fold (flip original) \"\" args))))" +
                "(define partial-apply lb:partial-apply)" +
                "(define range lb:range)" +
                "(define count lb:count)" +
                "(define sleep lb:sleep)" +
                "(define split lb:split)" +
                "(define clr-property-names lb:clr-property-names)" +
                "(define clr-method-names lb:clr-method-names)" +
                "(define clr-properties lb:clr-properties)" +
                "(define clr-get lb:clr-get)" +
                "(define clr-set lb:clr-set)" +
                "(define clr. lb:clr.)" +
                "(define clr->scheme lb:clr->scheme)";

            internal object TrampolineLoop(Func<Environment, object> f, Environment env) {
                object r = f(env);
                while (r == tailCall) r = Apply(tailCallProcedure, true, tailCallParameters);
                return r;
            }

            private object Apply(object f, bool mayReturnTrampoline, params object[] parameters) {
                try {
                    if (f is Func<Pair, object> && parameters.Length == 1 && parameters[0] is Pair) { return ((Func<Pair, object>)f)((Pair)parameters[0]); }
                    if (f is Func<char, char, object> && parameters.Length == 2 && parameters[0] is char && parameters[1] is char) { return ((Func<char, char, object>)f)((char)parameters[0], (char)parameters[1]); }
                    if (f is Func<string, string, object> && parameters.Length == 2 && parameters[0] is string && parameters[1] is string) { return ((Func<string, string, object>)f)((string)parameters[0], (string)parameters[1]); }
                    if (f is Func<object, object> && parameters.Length == 1) return ((Func<object, object>)f)(parameters[0]);
                    if (f is Func<object, object, object> && parameters.Length == 2) return ((Func<object, object, object>)f)(parameters[0], parameters[1]);
                    if (f is ClrClosure) return ((ClrClosure)f).Apply(parameters);
                    if (f is Delegate) return ((Delegate)f).DynamicInvoke(parameters);
                    if (!(f is Lambda)) throw new SchemeException("Object of type " + f.GetType() + " can not be evaluated as a function");

                    object ret = ((Lambda)f).Apply(parameters);
                    if (mayReturnTrampoline) return ret;
                    while (ret == tailCall) ret = Apply(tailCallProcedure, true, tailCallParameters);
                    return ret;
                } catch (TargetInvocationException ex) {
                    throw new SchemeException(ex.InnerException.Message);
                }
            }

            private static bool EvaluatesToTrue(object p) {
                return !(p is bool) || ((bool)p);
            }

            private Func<Environment, object> Analyze(object o) {
                if (o is Symbol && o.ToString().StartsWith(":")) return env => o;
                if (o is Symbol) return env => env.Get((Symbol)o);
                if (o is object[]) throw new SchemeException("Vector must be quoted");
                if (!(o is Pair)) return env => o;
                return AnalyzeFuncallOrSpecialForm(((Pair)o).ToArray());
            }

            private Func<Environment, object> AnalyzeFuncallOrSpecialForm(object[] o) {
                var symbol = o[0] as Symbol;
                if (symbol != null) {
                    switch (symbol.ToString()) {
                        case "if": return AnalyzeIfSpecialForm(o);
                        case "define": return AnalyzeDefineSpecialForm(o);
                        case "set!": return AnalyzeSetSpecialForm(o);
                        case "lambda": return AnalyzeLambdaSpecialForm(o);
                        case "quote": return AnalyzeQuoteSpecialForm(o);
                        case "begin": return AnalyzeBeginSpecialForm(o);
                    }
                }

                var analyzedForm = o.Select(Analyze).ToList();
                return env => {
                    var f = TrampolineLoop(analyzedForm[0], env);
                    object[] parameters = new object[analyzedForm.Count - 1];
                    for (int i = 1; i < analyzedForm.Count; ++i) parameters[i - 1] = TrampolineLoop(analyzedForm[i], env);
                    tailCallProcedure = f;
                    tailCallParameters = parameters;
                    return tailCall;
                };
            }

            private Func<Environment, object> AnalyzeIfSpecialForm(object[] form) {
                if (form.Length != 3 && form.Length != 4) throw new SchemeException("Invalid if form");
                var condition = Analyze(form[1]);
                var thenPart = Analyze(form[2]);
                var elsePart = form.Length == 4 ? Analyze(form[3]) : env => undefinedSymbol;
                return env => EvaluatesToTrue(TrampolineLoop(condition, env)) ? thenPart(env) : elsePart(env);
            }

            private Func<Environment, object> AnalyzeDefineSpecialForm(object[] form) {
                if (form.Length == 3 && form[1] is Symbol) // Define variable
                {
                    var variable = (Symbol)form[1];
                    var value = Analyze(form[2]);
                    return env => {
                        env.Define(variable, TrampolineLoop(value, env));
                        return undefinedSymbol;
                    };
                }
                if (form.Length >= 3 && form[1] is Pair) // Define procedure
                {
                    var nameAndParameters = ((Pair)form[1]).Cast<Symbol>();
                    var name = nameAndParameters.First();
                    var parameterNames = nameAndParameters.Skip(1).ToArray();
                    bool hasRestParameter = ((Pair)form[1]).IsDottedList();
                    var formsInLambda = form.Skip(2).Select(Analyze).ToList();
                    return env => {
                        env.Define(name, new Lambda(this, name, parameterNames, hasRestParameter, env, formsInLambda));
                        return undefinedSymbol;
                    };
                }
                throw new SchemeException("Invalid define form");
            }

            private Func<Environment, object> AnalyzeSetSpecialForm(object[] form) {
                if (form.Length != 3) throw new SchemeException("Invalid set form");
                var name = (Symbol)form[1];
                var value = Analyze(form[2]);
                return env => {
                    env.Set(name, TrampolineLoop(value, env));
                    return undefinedSymbol;
                };
            }

            private Func<Environment, object> AnalyzeLambdaSpecialForm(object[] form) {
                if (form.Length < 3) throw new SchemeException("Invalid lambda form");
                var forms = form.Skip(2).Select(Analyze).ToList();

                if (form[1] is Symbol) // (lambda a (form) (form) (form))
                {
                    var parameterNames = new[] { (Symbol)form[1] };
                    return env => new Lambda(this, Symbol.FromString("Lambda"), parameterNames, true, env, forms);
                }
                if (form[1] == null) // (lambda () (form) (form) (form))
                {
                    var parameterNames = new Symbol[0];
                    return env => new Lambda(this, Symbol.FromString("Lambda"), parameterNames, false, env, forms);
                }
                if (form[1] is Pair) // (lambda (a b c) (form) (form) (form))
                {
                    bool hasRestParameter = ((Pair)form[1]).IsDottedList();
                    var parameterNames = ((Pair)form[1]).Cast<Symbol>().ToArray();
                    return env => new Lambda(this, Symbol.FromString("Lambda"), parameterNames, hasRestParameter, env, forms);
                }

                throw new SchemeException("Invalid lambda form");
            }

            private static Func<Environment, object> AnalyzeQuoteSpecialForm(object[] form) {
                if (form.Length != 2) throw new SchemeException("Invalid quote form");
                var quotedValue = form[1];
                return env => quotedValue;
            }

            private Func<Environment, object> AnalyzeBeginSpecialForm(object[] form) {
                var forms = form.Select(Analyze).ToArray();
                return env => {
                    for (int i = 1; i < forms.Length - 1; ++i) TrampolineLoop(forms[i], env);
                    return forms.Last()(env);
                };
            }
        }
    }
}
