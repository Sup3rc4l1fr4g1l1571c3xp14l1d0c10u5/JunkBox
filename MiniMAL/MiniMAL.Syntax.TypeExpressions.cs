using System;

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
                public string Id { get; }

                public TypeVar(string id)
                {
                    Id = id;
                }
            }

            public class IntType : TypeExpressions
            {
            }

            public class BoolType : TypeExpressions
            {
            }

            public class StrType : TypeExpressions
            {
            }

            public class UnitType : TypeExpressions
            {
            }

            public class ListType : TypeExpressions
            {
                public TypeExpressions Type { get; }

                public ListType(TypeExpressions type)
                {
                    Type = type;
                }
            }

            public class OptionType : TypeExpressions
            {
                public TypeExpressions Type { get; }

                public OptionType(TypeExpressions type)
                {
                    Type = type;
                }
            }

            public class TupleType : TypeExpressions
            {

                public TypeExpressions[] Members { get; }

                public TupleType(TypeExpressions[] members)
                {
                    Members = members;
                }

            }

            public class RecordType : TypeExpressions
            {
                public Tuple<string, TypeExpressions>[] Members { get; }

                public RecordType(Tuple<string, TypeExpressions>[] members)
                {
                    Members = members;
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
            }

            public class TypeName : TypeExpressions
            {
                // type 宣言された型
                public string Name { get; }

                public TypeName(string name)
                {
                    Name = name;
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
            }
        }
    }
}
