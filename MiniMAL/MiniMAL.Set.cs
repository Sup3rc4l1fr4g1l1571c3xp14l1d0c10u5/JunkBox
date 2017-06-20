using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Schema;

namespace MiniMAL
{
    /// <summary>
    /// èWçá
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Set<T> {
        public T Value { get; }
        public Set<T> Next { get; }
        public static Set<T> Empty { get; } = new Set<T>(default(T),null);

        public Set(T value, Set<T> next)
        {
            Value = value;
            Next = next;
        }
    }

    public static class Set
    {
        public static Set<T> singleton<T>(T x) {
            return new Set<T>(x, Set<T>.Empty);
        }
        public static Set<T> from_list<T>(LinkedList<T> xs)
        {
            return LinkedList.FoldRight((s, x) => insert(x, s), xs, Set<T>.Empty);
        }

        public static LinkedList<T> to_list<T>(Set<T> xs) {
            return Set.Fold((s, x) => LinkedList.Extend(x, s), xs, LinkedList<T>.Empty);
        }

        public static Set<T> insert<T>(T x, Set<T> xs) {
            var rest = xs;
            while (rest != Set<T>.Empty) {
                if (rest.Value.Equals(x)) {
                    return xs;
                }
                rest = rest.Next;
            }
            return new Set<T>(x, xs);
        }
        public static Set<T> union<T>(Set<T> xs, Set<T> ys) {
            return Set.Fold((s, x) => Set.insert(x, s), xs, ys);
        }
        public static Set<T> remove<T>(T x, Set<T> xs) {
            var ret = Set<T>.Empty;
            while (xs != Set<T>.Empty) {
                if (!xs.Value.Equals(x)) {
                    ret = new Set<T>(xs.Value, ret);
                }
                xs = xs.Next;
            }
            return ret;
        }
        public static T2 Fold<T1, T2>(Func<T2, T1, T2> func, Set<T1> xs, T2 seed) {
            var ret = seed;
            while (xs != Set<T1>.Empty) {
                seed = func(seed, xs.Value);
                xs = xs.Next;
            }
            return ret;
        }

        public static Set<T> diff<T>(Set<T> xs, Set<T> ys) {
            return Set.Fold((s, x) => Set.remove(x, s), xs, ys);
        }

        public static T member<T>(T x, Set<T> xs) {
            var ret = Set<T>.Empty;
            while (xs != Set<T>.Empty) {
                if (xs.Value.Equals(x)) {
                    return xs.Value;
                }
                xs = xs.Next;
            }
            return default(T);
        }
        public static Set<T2> map<T1, T2>(Func<T1, T2> func, Set<T1> xs) {
            var ret = Set<T2>.Empty;
            while (xs != Set<T1>.Empty) {
                ret = insert(func(xs.Value),ret);
                xs = xs.Next;
            }
            return ret;
        }


        public static Set<T> bigunion<T>(Set<Set<T>> xs) {
            var ret = Set<T>.Empty;
            while (xs != Set<Set<T>>.Empty) {
                ret = union(xs.Value, ret);
                xs = xs.Next;
            }
            return ret;
        }
    }

}
