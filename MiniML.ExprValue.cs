using System.Linq;

namespace MiniML
{
    public static partial class MiniML
    {
        /// <summary>
        /// 評価値
        /// </summary>
        public abstract class ExprValue {
            /// <summary>
            /// 整数値
            /// </summary>
            public class IntV : ExprValue {
                public int Value { get; }

                public IntV(int value) {
                    Value = value;
                }

                public override string ToString() {
                    return $"{Value}";
                }
            }

            /// <summary>
            /// 真偽値
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
            /// コンスセル値
            /// </summary>
            public class ConsV : ExprValue {
                public ExprValue Value { get; }
                public ExprValue Next { get; }
                public static ConsV Empty { get; } = new ConsV(null,null);
                public ConsV(ExprValue value, ExprValue next) {
                    Value = value;
                    Next = next;
                }

                public override string ToString() {
                    if (this == Empty)
                    {
                        return $"[]";
                    }
                    else
                    {
                        return $"({Value} :: {Next})";
                    }
                }
            }

            /// <summary>
            /// ユニット値
            /// </summary>
            public class UnitV : ExprValue {
                public UnitV() {
                }

                public override string ToString() {
                    return $"()";
                }
            }

            /// <summary>
            /// タプル値
            /// </summary>
            public class TupleV : ExprValue {
                public ExprValue[] Values { get; }

                public TupleV(ExprValue[] values) {
                    Values = values;
                }

                public override string ToString() {
                    return $"({string.Join(" ", Values.Select(x => x.ToString()))})";
                }
            }

            /// <summary>
            /// レキシカルクロージャー
            /// </summary>
            public class ProcV : ExprValue {
                public string Id { get; }
                public Syntax Body { get; }
                public Environment<ExprValue> Env { get; private set; }

                public ProcV(string id, Syntax body, Environment<ExprValue> env) {
                    Id = id;
                    Body = body;
                    Env = env;
                }

                public override string ToString() {
                    return $"<fun>";
                }

                public void BackPatchEnv(Environment<ExprValue> newenv)
                {
                    this.Env = newenv;
                }
            }

            /// <summary>
            /// ダイナミッククロージャー
            /// </summary>
            public class DProcV : ExprValue {
                public string Id { get; }
                public Syntax Body { get; }

                public DProcV(string id, Syntax body) {
                    Id = id;
                    Body = body;
                }

                public override string ToString() {
                    return $"<dfun>";
                }
            }

        }
    }
}