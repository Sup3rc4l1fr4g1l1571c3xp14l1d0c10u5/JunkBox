using System;
using System.Linq;
using System.Numerics;

namespace MiniMAL
{
    namespace Syntax
    {
        /// <summary>
        /// パターン式
        /// </summary>
        public abstract partial class PatternExpressions
        {

            /// <summary>
            /// ワイルドカードパターン
            /// </summary>
            public class WildP : PatternExpressions
            {
                public override string ToString()
                {
                    return "_";
                }
            }

            /// <summary>
            /// 変数パターン
            /// </summary>
            public class VarP : PatternExpressions
            {
                public string Id { get; }

                public VarP(string id)
                {
                    Id = id;
                }

                public override string ToString()
                {
                    return $"{Id}";
                }
            }

            /// <summary>
            /// 整数値パターン
            /// </summary>
            public class IntP : PatternExpressions
            {
                public BigInteger Value { get; }

                public IntP(BigInteger value)
                {
                    Value = value;
                }

                public override string ToString()
                {
                    return $"{Value}";
                }
            }

            /// <summary>
            /// 文字列値パターン
            /// </summary>
            public class StrP : PatternExpressions
            {
                public string Value { get; }

                public StrP(string value)
                {
                    Value = value;
                }

                public override string ToString()
                {
                    return $"\"{Value.Replace("\"", "\\\"")}\"";
                }
            }

            /// <summary>
            /// 論理値パターン
            /// </summary>
            public class BoolP : PatternExpressions
            {
                public bool Value { get; }

                public BoolP(bool value)
                {
                    Value = value;
                }

                public override string ToString()
                {
                    return $"{Value}";
                }
            }

            /// <summary>
            /// Unitパターン
            /// </summary>
            public class UnitP : PatternExpressions
            {
                public override string ToString()
                {
                    return "()";
                }
            }

            /// <summary>
            /// consセル
            /// </summary>
            public class ConsP : PatternExpressions
            {
                public static ConsP Empty { get; } = new ConsP(null, null);
                public PatternExpressions Value { get; }
                public PatternExpressions Next { get; }

                public ConsP(PatternExpressions value, PatternExpressions next)
                {
                    Value = value;
                    Next = next;
                }

                public override string ToString()
                {
                    if (Next == Empty)
                    {
                        return $"{Value}";
                    }
                    else
                    {
                        return $"{Value} :: {Next}";
                    }
                }
            }

            /// <summary>
            /// タプルパターン
            /// </summary>
            public class TupleP : PatternExpressions
            {
                public PatternExpressions[] Members { get; }

                public TupleP(PatternExpressions[] members)
                {
                    Members = members;
                }

                public override string ToString()
                {
                    return $"({string.Join(", ", Members.Select(x => x.ToString()))})";
                }
            }
            
            /// <summary>
            /// レコードパターン
            /// </summary>
            public class RecordP : PatternExpressions
            {
                public Tuple<string,PatternExpressions>[] Members { get; }

                public RecordP(Tuple<string,PatternExpressions>[] members)
                {
                    Members = members;
                }

                public override string ToString()
                {
                    return $"{{{string.Join("; ", Members.Select(x => $"{x.Item1} = {x.Item2.ToString()}"))}}}";
                }
            }

            /// <summary>
            /// Optionパターン
            /// </summary>
            public class OptionP : PatternExpressions
            {
                public static OptionP None { get; } = new OptionP(null);

                public PatternExpressions Value { get; }

                public OptionP(PatternExpressions value)
                {
                    Value = value;
                }

                public override string ToString()
                {
                    if (this == None)
                    {
                        return "None";
                    }
                    else
                    {
                        return $"Some {Value}";
                    }
                }
            }

            /// <summary>
            /// ヴァリアントパターン
            /// </summary>
            public class VariantP : PatternExpressions
            {
                public string ConstructorName { get; }
                public PatternExpressions Body { get; }
                public int ConstructorId { get; set; }

                public VariantP(string constructorName, PatternExpressions body)
                {
                    ConstructorName = constructorName;
                    Body = body;
                }

                public override string ToString()
                {
                    return $"{ConstructorName} {Body}";
                }
            }

        }

    }
}
