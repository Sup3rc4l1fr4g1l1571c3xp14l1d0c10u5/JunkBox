using System;
using System.Linq;
using System.Numerics;
using System.Text;

namespace MiniMAL
{
    /// <summary>
    /// �p�^�[����
    /// </summary>
    public abstract class PatternExpressions {
        /// <summary>
        /// ���C���h�J�[�h�p�^�[��
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
        /// �ϐ��p�^�[��
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
        /// �����l�p�^�[��
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
        /// ������l�p�^�[��
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
        /// �_���l�p�^�[��
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
        /// Unit�p�^�[��
        /// </summary>
        public class UnitP : PatternExpressions {
            public UnitP() {
            }

            public override string ToString() {
                return $"()";
            }
        }

        /// <summary>
        /// cons�Z��
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
        /// �^�v���p�^�[��
        /// </summary>
        public class TupleP : PatternExpressions {
            public PatternExpressions[] Value { get; }

            public TupleP(PatternExpressions[] value) {
                Value = value;
            }

            public override string ToString() {
                return $"({String.Join(", ", Value.Select(x => x.ToString()))})";
            }
        }

        /// <summary>
        /// Option�p�^�[��
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
