using System;
using System.Linq;

namespace MiniMAL
{
    namespace Syntax
    {
        /// <summary>
        /// 型式
        /// </summary>
        public abstract class TypeExpressions
        {
            public class TypeVar : TypeExpressions
            {
                private static int _counter;

                public static TypeVar Fresh()
                {
                    return new TypeVar("$"+(_counter++));
                }


                public string Id { get; }

                public TypeVar(string id)
                {
                    Id = id;
                }

                public override string ToString()
                {
                    return $"{Id}";
                }
            }

            public class IntType : TypeExpressions
            {
                public override string ToString()
                {
                    return "int";
                }
            }

            public class BoolType : TypeExpressions
            {
                public override string ToString()
                {
                    return "bool";
                }
            }

            public class StrType : TypeExpressions
            {
                public override string ToString()
                {
                    return "string";
                }
            }

            public class UnitType : TypeExpressions
            {
                public override string ToString()
                {
                    return "unit";
                }
            }

            public class ListType : TypeExpressions
            {
                public TypeExpressions Type { get; }

                public ListType(TypeExpressions type)
                {
                    Type = type;
                }
                public override string ToString()
                {
                    return $"{Type} list";
                } 
            }

            public class OptionType : TypeExpressions
            {
                public TypeExpressions Type { get; }

                public OptionType(TypeExpressions type)
                {
                    Type = type;
                }
                public override string ToString()
                {
                    return $"{Type} option";
                }
            }

            public class TupleType : TypeExpressions
            {

                public TypeExpressions[] Members { get; }

                public TupleType(TypeExpressions[] members)
                {
                    Members = members;
                }

                public override string ToString()
                {
                    return $"({string.Join(" * ", Members.Select(x => x.ToString()))})";
                }
            }

            public class RecordType : TypeExpressions
            {
                public Tuple<string, TypeExpressions>[] Members { get; }

                public RecordType(Tuple<string, TypeExpressions>[] members)
                {
                    Members = members;
                }
                public override string ToString()
                {
                    return $"({string.Join(" * ", Members.Select(x => $"{x.Item1} : {x.Item2.ToString()}"))})";
                }

            }

            public class FuncType : TypeExpressions
            {
                public TypeExpressions DomainType { get; }
                public TypeExpressions RangeType { get; }

                public FuncType(TypeExpressions domType, TypeExpressions ranType)
                {
                    DomainType = domType;
                    RangeType = ranType;
                }
                public override string ToString()
                {
                    return $"{DomainType} -> {RangeType}";
                }
            }

            public class TypeName : TypeExpressions
            {
                // type 宣言された型
                public string Name { get; }

                public TypeName(string name)
                {
                    Name = name;
                }
                public override string ToString()
                {
                    return $"{Name}";
                }
            }

            public class TypeConstruct : TypeExpressions
            {
                // 型引数を伴って実体化した型
                public TypeName Base { get; }

                public TypeExpressions[] Params { get; }

                public TypeConstruct(TypeName @base, TypeExpressions[] @params)
                {
                    Base = @base;
                    Params = @params;
                }
                public override string ToString()
                {
                    return $"({string.Join(", ", Params.Select(x => x.ToString()))}) {Base}";
                }
            }
        }
    }
}
