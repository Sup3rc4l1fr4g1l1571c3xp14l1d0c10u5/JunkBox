using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using MiniMAL.Syntax;

namespace MiniMAL
{
    namespace Interpreter
    {
        public static partial class AbstractSyntaxTreeInterpreter
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
                        return "unit";
                    }
                }

                /// <summary>
                /// ���L�V�J���N���[�W���[
                /// </summary>
                public class ProcV : ExprValue
                {
                    public string Id { get; }
                    public Expressions Body { get; }
                    public Environment<ExprValue> Env { get; private set; }

                    public ProcV(string id, Expressions body, Environment<ExprValue> env)
                    {
                        Id = id;
                        Body = body;
                        Env = env;
                    }

                    public void BackPatchEnv(Environment<ExprValue> newenv)
                    {
                        Env = newenv;
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

                    public IEnumerable<ExprValue> Values
                    {
                        get
                        {
                            for (var p = this; p != Empty; p = p.Next)
                            {
                                yield return p.Value;
                            }
                        }
                    }

                    public override string ToString()
                    {
                        return $"[{string.Join("; ", Values.Select(x => x.ToString()))}]";
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
                /// �^�v��
                /// </summary>
                public class RecordV : ExprValue
                {
                    public Tuple<string,ExprValue>[] Members { get; }

                    public RecordV(Tuple<string, ExprValue>[] membera)
                    {
                        Members = membera;
                    }

                    public override string ToString()
                    {
                        return $"{{{string.Join("; ", Members.Select(x => $"{x.Item1.ToString()}={x.Item2.ToString()}"))}}}";
                    }
                }

                /// <summary>
                /// Option
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
                        return i1.Values.SequenceEqual(i2.Values);
                    }
                    if (arg1 is TupleV && arg2 is TupleV)
                    {
                        var i1 = (TupleV)arg1;
                        var i2 = (TupleV)arg2;
                        return i1.Members.SequenceEqual(i2.Members);
                    }
                    if (arg1 is RecordV && arg2 is RecordV)
                    {
                        var i1 = (RecordV)arg1;
                        var i2 = (RecordV)arg2;
                        return i1.Members.SequenceEqual(i2.Members);
                    }
                    return false;
                }

                public override bool Equals(object obj)
                {
                    if (ReferenceEquals(null, obj)) return false;
                    if (ReferenceEquals(this, obj)) return true;
                    if (obj.GetType() != this.GetType()) return false;
                    return Equals(this,(ExprValue) obj);
                }

                public override int GetHashCode()
                {
                    return this.GetType().GetHashCode();
                }

            }
        }
    }
}