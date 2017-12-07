using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AnsiCParser {
    /// <summary>
    /// S���p�̃Z��
    /// </summary>
    public abstract class Cell {

        /// <summary>
        /// ��Z��
        /// </summary>
        public static Cell Nil { get; } = new ConsCell();

        /// <summary>
        /// �R���X�Z��
        /// </summary>
        public class ConsCell : Cell {
            public Cell Car {
                get;
            }
            public Cell Cdr {
                get;
            }
            public ConsCell(Cell car = null, Cell cdr = null) {
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
        /// ������l�Z��
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
        /// ���X�g�쐬
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static Cell Create(params object[] args) {
            var chain = Nil;
            foreach (var arg in args.Reverse()) {
                if (arg is String) {
                    chain = new ConsCell(new ValueCell(arg as string), chain);
                } else if (arg is Cell) {
                    chain = new ConsCell(arg as Cell, chain);
                } else {
                    throw new Exception();
                }
            }
            return chain;
        }


        /// <summary>
        /// S���̐��`�o��
        /// </summary>
        private static class PrettyPrinter {
            private static void PrintINdent(StringBuilder sb, int lebel) {
                sb.Append(String.Concat(Enumerable.Repeat("  ", lebel)));
            }
            private static void PrintOpenParen(StringBuilder sb) { sb.Append("("); }
            private static void PrintCloseParen(StringBuilder sb) { sb.Append(")"); }
            private static void PrintAtom(StringBuilder sb, Cell e, bool prefix) { sb.Append(prefix ? " " : "").Append((e as ValueCell)?.Value ?? ""); }

            private static void PrintList(StringBuilder sb, Cell s, int lebel, bool prefix) {
                if (prefix) {
                    PrintINdent(sb,lebel);
                }
                PrintOpenParen(sb);
                prefix = false;
                for (; ; ) {
                    if (s == Nil) {
                        PrintCloseParen(sb);
                        break;
                    } else if (s is ConsCell) {
                        var e = (s as ConsCell).Car;
                        if (e is ConsCell) {
                            if (prefix) {
                                sb.AppendLine();
                            }
                            PrintList(sb,e as ConsCell, lebel + 1, prefix);
                        } else {
                            PrintAtom(sb,e, prefix);
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
                    PrintList(sb,cell, 0, false);
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
}