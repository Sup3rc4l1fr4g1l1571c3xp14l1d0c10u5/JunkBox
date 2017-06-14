using System.Linq;

namespace MiniML
{
    public static partial class MiniML
    {
        /// <summary>
        /// �p�^�[��
        /// </summary>
        public abstract class Pattern {
            /// <summary>
            /// �����萔�p�^�[��
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
            /// �^�U�萔�p�^�[��
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
            /// �ϐ��p�^�[��
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
            /// ���C���h�J�[�h�p�^�[��
            /// </summary>
            public class WildP : Pattern {
                public WildP() {}

                public override string ToString() {
                    return $"_";
                }
            }

            /// <summary>
            /// �R���X�Z���p�^�[��
            /// </summary>
            public class ConsP : Pattern {
                public Pattern Pattern { get; }
                public Pattern Next { get; }
                public static ConsP Empty { get; } = new ConsP(null,null);
                public ConsP(Pattern pattern, Pattern next)
                {
                    Pattern = pattern;
                    Next = next;
                }

                public override string ToString() {
                    if (this == Empty)
                    {
                        return $"[]";
                    } else { 
                    return $"{Pattern} :: {Next}";
                    }
                }
            }

            /// <summary>
            /// ���j�b�g�p�^�[��
            /// </summary>
            public class UnitP : Pattern {
                public UnitP() { }

                public override string ToString() {
                    return $"()";
                }
            }

            /// <summary>
            /// �^�v���p�^�[��
            /// </summary>
            public class TupleP : Pattern {
                public Pattern Pattern { get; }
                public TupleP Next { get; }
                public static TupleP Empty { get; } = new TupleP(null, null);
                public TupleP(Pattern lhs, TupleP next) {
                    Pattern = lhs;
                    Next = next;
                }

                public override string ToString() {
                    if (this == Empty) {
                        return $"[]";
                    } else {
                        return $"{Pattern} :: {Next}";
                    }
                }
            }

        }
    }
}