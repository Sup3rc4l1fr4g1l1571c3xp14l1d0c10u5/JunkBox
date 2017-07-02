using System;
using System.Linq;
using System.Numerics;
using System.Text;

namespace MiniMAL
{
    /// <summary>
    /// パターン式
    /// </summary>
    public abstract class PatternExpressions {
        /// <summary>
        /// ワイルドカードパターン
        /// </summary>
        public class WildP : PatternExpressions {
            public int Value { get; }

            public WildP() {
            }

            public override string ToString() {
                return $"{Value}";
            }
        }

        /// <summary>
        /// 変数パターン
        /// </summary>
        public class VarP : PatternExpressions {
            public string Id { get; }

            public VarP(string id) {
                Id = id;
            }

            public override string ToString() {
                return $"{Id}";
            }
        }

        /// <summary>
        /// 整数値パターン
        /// </summary>
        public class IntP : PatternExpressions {
            public BigInteger Value { get; }

            public IntP(BigInteger value) {
                Value = value;
            }

            public override string ToString() {
                return $"{Value}";
            }
        }

        /// <summary>
        /// 文字列値パターン
        /// </summary>
        public class StrP : PatternExpressions {
            public string Value { get; }

            public StrP(string value) {
                Value = value;
            }

            public override string ToString() {
                return $"\"{Value.Replace("\"", "\\\"")}\"";
            }
        }

        /// <summary>
        /// 論理値パターン
        /// </summary>
        public class BoolP : PatternExpressions {
            public bool Value { get; }

            public BoolP(bool value) {
                Value = value;
            }

            public override string ToString() {
                return $"{Value}";
            }
        }

        /// <summary>
        /// Unitパターン
        /// </summary>
        public class UnitP : PatternExpressions {
            public UnitP() {
            }

            public override string ToString() {
                return $"()";
            }
        }

        /// <summary>
        /// consセル
        /// </summary>
        public class ConsP : PatternExpressions {
            public static ConsP Empty { get; } = new ConsP(null, null);
            public PatternExpressions Value { get; }
            public PatternExpressions Next { get; }

            public ConsP(PatternExpressions value, PatternExpressions next) {
                Value = value;
                Next = next;
            }

            public override string ToString() {
                StringBuilder sb = new StringBuilder();
                if (Next == Empty) {
                    sb.Append($"{Value}");
                } else {
                    sb.Append($"{Value} :: {Next}");
                }
                return sb.ToString();
            }
        }

        /// <summary>
        /// タプルパターン
        /// </summary>
        public class TupleP : PatternExpressions {
            public static TupleP Tail { get; } = new TupleP(null, null);
            public PatternExpressions Car { get; }
            public TupleP Cdr { get; }

            public TupleP(PatternExpressions car, TupleP cdr) {
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
        /// Optionパターン
        /// </summary>
        public class OptionP: PatternExpressions {
            public static OptionP None { get; } = new OptionP(null);
            public PatternExpressions Value { get; }

            public OptionP(PatternExpressions value) {
                Value = value;
            }

            public override string ToString() {
                if (this == None)
                {
                    return $"None";
                } else
                {
                    return $"Some {Value}";
                }
            }
        }
    }
}
