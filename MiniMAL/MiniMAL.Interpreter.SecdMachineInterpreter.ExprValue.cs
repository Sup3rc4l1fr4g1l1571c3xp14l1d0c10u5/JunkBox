using System;
using System.Linq;
using System.Numerics;
using System.Text;

namespace MiniMAL
{
    namespace Interpreter
    {
        public static partial class SecdMachineInterpreter
        {
            /// <summary>
            /// �]���l
            /// </summary>
            public abstract class ExprValue
            {
                /// <summary>
                /// �����l
                /// </summary>
                public class IntV : ExprValue
                {
                    public BigInteger Value { get; }

                    public IntV(BigInteger value)
                    {
                        Value = value;
                    }

                    public override string ToString()
                    {
                        return $"{Value}";
                    }
                }

                /// <summary>
                /// ������l
                /// </summary>
                public class StrV : ExprValue
                {
                    public string Value { get; }

                    public StrV(string value)
                    {
                        Value = value;
                    }

                    public override string ToString()
                    {
                        return $"\"{Value.Replace("\"", "\\\"")}\"";
                    }
                }

                /// <summary>
                /// �_���l
                /// </summary>
                public class BoolV : ExprValue
                {
                    public bool Value { get; }

                    public BoolV(bool value)
                    {
                        Value = value;
                    }

                    public override string ToString()
                    {
                        return $"{Value}";
                    }
                }

                /// <summary>
                /// Unit�l
                /// </summary>
                public class UnitV : ExprValue
                {
                    public override string ToString()
                    {
                        return "()";
                    }
                }

                /// <summary>
                /// ���L�V�J���N���[�W���[
                /// </summary>
                public class ProcV : ExprValue
                {
                    public string Id { get; }
                    public LinkedList<Instructions> Body { get; }
                    public LinkedList<LinkedList<ExprValue>> Env { get; }

                    public ProcV(string id, LinkedList<Instructions> body, LinkedList<LinkedList<ExprValue>> env)
                    {
                        Id = id;
                        Body = body;
                        Env = env;
                    }

                    public override string ToString()
                    {
                        return "<fun>";
                    }
                }

                /// <summary>
                /// �r���g�C���N���[�W���[
                /// </summary>
                public class BProcV : ExprValue
                {
                    public Func<ExprValue, ExprValue> Proc { get; }

                    public BProcV(Func<ExprValue, ExprValue> proc)
                    {
                        Proc = proc;
                    }

                    public override string ToString()
                    {
                        return "<bproc>";
                    }
                }

                /// <summary>
                /// ���X�g
                /// </summary>
                public class ListV : ExprValue
                {
                    public static ListV Empty { get; } = new ListV(null, null);
                    public ExprValue Value { get; }
                    public ListV Next { get; }

                    public ListV(ExprValue value, ListV next)
                    {
                        Value = value;
                        Next = next;
                    }

                    public override string ToString()
                    {
                        StringBuilder sb = new StringBuilder();
                        sb.Append("[");
                        if (this != Empty)
                        {
                            sb.Append($"{Value}");
                            for (var p = Next; p != Empty; p = p.Next)
                            {
                                sb.Append($"; {p.Value}");
                            }
                        }
                        sb.Append("]");
                        return sb.ToString();
                    }
                }

                /// <summary>
                /// �^�v��
                /// </summary>
                public class TupleV : ExprValue
                {
                    public ExprValue[] Members { get; }

                    public TupleV(ExprValue[] membera)
                    {
                        Members = membera;
                    }

                    public override string ToString()
                    {
                        return $"({string.Join(", ", Members.Select(x => x.ToString()))})";
                    }
                }

                /// <summary>
                /// Option�l
                /// </summary>
                public class OptionV : ExprValue
                {
                    public static OptionV None { get; } = new OptionV(null);
                    public ExprValue Value { get; }

                    public OptionV(ExprValue value)
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
                /// ��r
                /// </summary>
                /// <param name="arg1"></param>
                /// <param name="arg2"></param>
                /// <returns></returns>
                public static bool Equals(ExprValue arg1, ExprValue arg2)
                {
                    if (arg1 is IntV && arg2 is IntV)
                    {
                        var i1 = ((IntV)arg1).Value;
                        var i2 = ((IntV)arg2).Value;
                        return i1 == i2;
                    }
                    if (arg1 is StrV && arg2 is StrV)
                    {
                        var i1 = ((StrV)arg1).Value;
                        var i2 = ((StrV)arg2).Value;
                        return i1 == i2;
                    }
                    if (arg1 is BoolV && arg2 is BoolV)
                    {
                        var i1 = ((BoolV)arg1).Value;
                        var i2 = ((BoolV)arg2).Value;
                        return i1 == i2;
                    }
                    if (arg1 is UnitV && arg2 is UnitV)
                    {
                        return true;
                    }
                    if (arg1 is OptionV && arg2 is OptionV)
                    {
                        if (arg1 == OptionV.None || arg2 == OptionV.None)
                        {
                            return arg2 == OptionV.None && arg1 == OptionV.None;
                        }
                        else
                        {
                            var i1 = (OptionV)arg1;
                            var i2 = (OptionV)arg2;
                            return Equals(i1.Value, i2.Value);
                        }
                    }
                    if (arg1 is ListV && arg2 is ListV)
                    {
                        var i1 = (ListV)arg1;
                        var i2 = (ListV)arg2;
                        while (i1 != ListV.Empty && i2 != ListV.Empty)
                        {
                            if (!Equals(i1.Value, i2.Value))
                            {
                                return false;
                            }
                            i1 = i1.Next;
                            i2 = i2.Next;
                        }
                        return i1 != ListV.Empty && i2 != ListV.Empty;
                    }
                    if (arg1 is TupleV && arg2 is TupleV)
                    {
                        var i1 = (TupleV)arg1;
                        var i2 = (TupleV)arg2;
                        return i1.Members.SequenceEqual(i2.Members);
                    }
                    return false;
                }

            }
        }
    }
}