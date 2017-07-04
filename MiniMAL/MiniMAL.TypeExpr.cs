using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;

namespace MiniMAL {
    /// <summary>
    /// 型
    /// </summary>
    public abstract class TypeExp {
        public class TypeVar : TypeExp {
            public string Id { get; }
            public TypeVar(string id) {
                Id = id;
            }
        }

        public class IntType : TypeExp { }

        public class BoolType : TypeExp { }

        public class StrType : TypeExp { }

        public class UnitType : TypeExp { }

        public class ListType : TypeExp {
            public TypeExp Type { get; }
            public ListType(TypeExp type) {
                Type = type;
            }
        }

        public class OptionType : TypeExp {
            public TypeExp Type { get; }
            public OptionType(TypeExp type) {
                Type = type;
            }
        }

        public class TupleType : TypeExp {
            public static TupleType Tail { get; } = new TupleType(null, null);

            public TypeExp Car { get; }
            public TupleType Cdr { get; }

            public TupleType(TypeExp car, TupleType cdr) {
                Car = car;
                Cdr = cdr;
            }

        }

        public class FuncType : TypeExp {
            public TypeExp DomainType { get; }
            public TypeExp RangeType { get; }
            public FuncType(TypeExp domType, TypeExp ranType) {
                DomainType = domType;
                RangeType = ranType;
            }
        }

        public class TypeName : TypeExp {
            // type 宣言された型
            public string Name { get; }
            public TypeName(string name) {
                Name = name;
            }
        }
        public class TypeConstruct : TypeExp {
            // 型引数を伴って実体化した型
            public TypeName Base { get; }
            public TypeExp[] Params { get; }
            public TypeConstruct(TypeName @base, TypeExp[] @params) {
                Base = @base;
                Params = @params;
            }
        }
    }
}
