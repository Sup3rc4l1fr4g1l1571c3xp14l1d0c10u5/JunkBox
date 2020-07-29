using System;
using System.Collections.Generic;

namespace AnsiCParser {
    public static class Ext {
        private class LambdaEqualityComparer<TSource> : IEqualityComparer<TSource> {

            public LambdaEqualityComparer(Func<TSource, TSource, bool> comparer) {
                this.Comparer = comparer;
            }

            private Func<TSource, TSource, bool> Comparer { get; }

            public bool Equals(TSource x, TSource y) {
                return Comparer(x, y);
            }

            public int GetHashCode(TSource obj) {
                return 0;
            }
        }

        public static IEnumerable<TSource> Except<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second, Func<TSource, TSource, bool> comparer) {
            return System.Linq.Enumerable.Except<TSource>(first, second, new LambdaEqualityComparer<TSource>(comparer));
        }

    }


}
