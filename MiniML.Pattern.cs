using System.Linq;

namespace MiniML
{
    public static partial class MiniML
    {
        /// <summary>
        /// パターン
        /// </summary>
        public abstract class Pattern {
            /// <summary>
            /// 整数定数パターン
            /// </summary>
            public class IntP : Pattern  {
                public int Value { get; }

                public IntP(int value) {
                    Value = value;
                }

                public override string ToString() {
                    return $"{Value}";
                }
            }
            
            /// <summary>
            /// 真偽定数パターン
            /// </summary>
            public class BoolP : Pattern  {
                public bool Value { get; }

                public BoolP(bool value) {
                    Value = value;
                }

                public override string ToString() {
                    return $"{Value}";
                }
            }

            /// <summary>
            /// 変数パターン
            /// </summary>
            public class VarP : Pattern {
                public string Id { get; }

                public VarP(string id) {
                    Id = id;
                }

                public override string ToString() {
                    return $"{Id}";
                }
            }

            /// <summary>
            /// ワイルドカードパターン
            /// </summary>
            public class WildP : Pattern {
                public WildP() {}

                public override string ToString() {
                    return $"_";
                }
            }

            /// <summary>
            /// コンスセルパターン
            /// </summary>
            public class ConsP : Pattern {
                public Pattern Lhs { get; }
                public Pattern Rhs { get; }
                public static ConsP Empty { get; } = new ConsP(null,null);
                public ConsP(Pattern lhs, Pattern  rhs)
                {
                    Lhs = lhs;
                    Rhs = rhs;
                }

                public override string ToString() {
                    if (this == Empty)
                    {
                        return $"[]";
                    } else { 
                    return $"{Lhs} :: {Rhs}";
                    }
                }
            }

            /// <summary>
            /// ユニットパターン
            /// </summary>
            public class UnitP : Pattern {
                public UnitP() { }

                public override string ToString() {
                    return $"()";
                }
            }

            /// <summary>
            /// タプルパターン
            /// </summary>
            public class TupleP : Pattern {
                public TupleP(Pattern[] patterns) {
                    Patterns = patterns;
                }

                public Pattern[] Patterns { get; }

                public override string ToString() {
                    return $"({string.Join(" ", Patterns.Select(x => x.ToString()))})";
                }
            }

        }
    }
}