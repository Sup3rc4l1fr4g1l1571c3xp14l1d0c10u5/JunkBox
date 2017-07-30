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
                public Tuple<bool, string, TypeExpressions>[] Members { get; }

                public RecordType(Tuple<bool, string, TypeExpressions>[] members)
                {
                    Members = members;
                }
                public override string ToString()
                {
                    return $"{{{string.Join("; ", Members.Select(x => $"{(x.Item1?"mutable ":"")}{x.Item2} : {x.Item3.ToString()}"))}}}";
                }

            }

            public class VariantType : TypeExpressions
            {
                public Tuple<string, TypeExpressions>[] Members { get; }

                public VariantType(Tuple<string, TypeExpressions>[] members)
                {
                    Members = members;
                }
                public override string ToString()
                {
                    return $"{string.Join(" | ", Members.Select(x => $"{x.Item1} of {x.Item2.ToString()}"))}}}";
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

            public static Func<TypeExpressions, TResult> Match<TResult>(
                Func<TypeVar, TResult> TypeVar,
                Func<IntType, TResult> IntType,
                Func<BoolType, TResult> BoolType,
                Func<StrType, TResult> StrType,
                Func<UnitType, TResult> UnitType,
                Func<ListType, TResult> ListType,
                Func<OptionType, TResult> OptionType,
                Func<TupleType, TResult> TupleType,
                Func<RecordType, TResult> RecordType,
                Func<VariantType, TResult> VariantType,
                Func<FuncType, TResult> FuncType,
                Func<TypeName, TResult> TypeName,
                Func<TypeConstruct, TResult> TypeConstruct,
                Func<TypeExpressions, TResult> Other
            )
            {
                return (obj) =>
                {
                    if (obj is TypeVar) { return TypeVar((TypeVar)obj); }
                    if (obj is IntType) { return IntType((IntType)obj); }
                    if (obj is BoolType) { return BoolType((BoolType)obj); }
                    if (obj is StrType) { return StrType((StrType)obj); }
                    if (obj is UnitType) { return UnitType((UnitType)obj); }
                    if (obj is ListType) { return ListType((ListType)obj); }
                    if (obj is OptionType) { return OptionType((OptionType)obj); }
                    if (obj is TupleType) { return TupleType((TupleType)obj); }
                    if (obj is RecordType) { return RecordType((RecordType)obj); }
                    if (obj is VariantType) { return VariantType((VariantType)obj); }
                    if (obj is FuncType) { return FuncType((FuncType)obj); }
                    if (obj is TypeName) { return TypeName((TypeName)obj); }
                    if (obj is TypeConstruct) { return TypeConstruct((TypeConstruct)obj); }
                    return Other(obj);
                };
            }
        }
    }
}
