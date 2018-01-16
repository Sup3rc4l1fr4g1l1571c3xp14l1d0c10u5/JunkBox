using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AnsiCParser {
    /// <summary>
    /// S式用のセル
    /// </summary>
    public abstract class Cell {

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
        /// 文字列値セル
        /// </summary>
        public class ValueCell : Cell {
            public string Value {
                get;
            }
            public ValueCell(string value) {
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
                    chain = new ConsCell(new ValueCell(arg as string), chain);
                } else if (arg is Int32) {
                    chain = new ConsCell(new ValueCell(arg.ToString()), chain);
                } else if (arg is Cell) {
                    chain = new ConsCell(arg as Cell, chain);
                } else if (arg is Location) {
                    chain = new ConsCell(Cell.Create((arg as Location).FilePath, (arg as Location).Line, (arg as Location).Column), chain);
                } else if (arg is LocationRange) {
                    chain = new ConsCell(Cell.Create((arg as LocationRange).Start.FilePath, (arg as LocationRange).Start.Line, (arg as LocationRange).Start.Column, (arg as LocationRange).End.FilePath, (arg as LocationRange).End.Line, (arg as LocationRange).End.Column), chain);
                } else {
                    throw new Exception();
                }
            }
            return chain;
        }


        /// <summary>
        /// S式の整形出力
        /// </summary>
        private static class PrettyPrinter {
            private static void Indent(StringBuilder sb, int lebel) {
                sb.Append(String.Concat(Enumerable.Repeat("  ", lebel)));
            }
            private static void OpenParen(StringBuilder sb) { sb.Append("("); }
            private static void CloseParen(StringBuilder sb) { sb.Append(")"); }
            private static void Atom(StringBuilder sb, Cell e, bool prefix) { sb.Append(prefix ? " " : "").Append((e as ValueCell)?.Value ?? ""); }

            private static void List(StringBuilder sb, Cell s, int lebel, bool prefix) {
                if (prefix) {
                    Indent(sb,lebel);
                }
                OpenParen(sb);
                prefix = false;
                for (; ; ) {
                    if (s == Nil) {
                        CloseParen(sb);
                        break;
                    } else if (s is ConsCell) {
                        var e = (s as ConsCell).Car;
                        if (e is ConsCell) {
                            if (prefix) {
                                sb.AppendLine();
                            }
                            List(sb,e as ConsCell, lebel + 1, prefix);
                        } else {
                            Atom(sb,e, prefix);
                        }
                        s = (s as ConsCell).Cdr;
                        prefix = true;
                    } else {
                        throw new Exception();
                    }
                }
            }

            public static string Print(Cell cell) {
                StringBuilder sb = new StringBuilder();
                if (cell is ConsCell) {
                    List(sb,cell, 0, false);
                } else if (cell is ValueCell) {
                    sb.Append(((ValueCell)cell).Value ?? "");
                } else {
                    throw new Exception();
                }
                sb.AppendLine();

                return sb.ToString();
            }
        }

        public static string PrettyPrint(Cell cell) {
            return PrettyPrinter.Print(cell);
        }
    }

    public static class S {
        public static bool IsAtom(this Cell self) {
            return (!IsPair(self) && !IsNull(self));
        }
        public static bool IsPair(this Cell self) {
            return (self != Cell.Nil) && (self is Cell.ConsCell);
        }
        public static bool IsNull(this Cell self) {
            return self == Cell.Nil;
        }
        public static Cell Car(this Cell self) {
            return (self as Cell.ConsCell).Car;
        }
        public static Cell Cdr(this Cell self) {
            return (self as Cell.ConsCell).Cdr;
        }
        private static Cell ToCell(object arg) {
            if (arg is String) {
                return new Cell.ValueCell(arg as string);
            } else if (arg is Cell) {
                return arg as Cell;
            } else {
                throw new Exception();
            }
        }
        public static Cell Cons(object lhs, object rhs) {
            return new Cell.ConsCell(ToCell(lhs), ToCell(rhs));
        }
    }

}
