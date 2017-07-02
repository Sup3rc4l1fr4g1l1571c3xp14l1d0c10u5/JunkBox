using System;
using System.Collections.Generic;
using System.Linq;

namespace MiniMAL
{
    /// <summary>
    /// 集合
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

        public override string ToString() {
            var items = new List<T>();
            for (var it = this; it != Empty; it = it.Next) {
                items.Add(it.Value);
            }
            return $"[{string.Join("; ", items.Select(x => x.ToString()))}]";
        }
    }

    public static class Set
    {
        public static Set<T> Singleton<T>(T x) {
            return new Set<T>(x, Set<T>.Empty);
        }

        public static Set<T> FromLinkedList<T>(LinkedList<T> xs)
        {
            return LinkedList.FoldRight((s, x) => Insert(x, s), xs, Set<T>.Empty);
        }

        public static LinkedList<T> ToLinkedList<T>(Set<T> xs) {
            return Set.Fold((s, x) => LinkedList.Extend(x, s), LinkedList<T>.Empty, xs);
        }

        public static Set<T> Insert<T>(T x, Set<T> xs) {
            var rest = xs;
            while (rest != Set<T>.Empty) {
                if (rest.Value.Equals(x)) {
                    return xs;
                }
                rest = rest.Next;
            }
            return new Set<T>(x, xs);
        }

        public static Set<T> Union<T>(Set<T> xs, Set<T> ys) {
            return Fold((s, x) => Insert(x, s), xs, ys);
        }

        public static Set<T> Remove<T>(T x, Set<T> xs) {
            var ret = Set<T>.Empty;
            while (xs != Set<T>.Empty) {
                if (!xs.Value.Equals(x)) {
                    ret = new Set<T>(xs.Value, ret);
                }
                xs = xs.Next;
            }
            return ret;
        }

        public static T2 Fold<T1, T2>(Func<T2, T1, T2> func, T2 seed, Set<T1> xs) {
            var ret = seed;
            while (xs != Set<T1>.Empty) {
                ret  = func(ret, xs.Value);
                xs = xs.Next;
            }
            return ret;
        }

        public static Set<T> Diff<T>(Set<T> xs, Set<T> ys) {
            return Fold((s, x) => Remove(x, s), xs, ys);
        }

        public static int Count<T>(Set<T> set) {
            return Fold((s, x) => s + 1, 0, set);
        }

        public static bool Member<T>(T x, Set<T> xs) {
            while (xs != Set<T>.Empty) {
                if (xs.Value.Equals(x)) {
                    return true;
                }
                xs = xs.Next;
            }
            return false;
        }

        public static Set<T2> Map<T1, T2>(Func<T1, T2> func, Set<T1> xs) {
            var ret = Set<T2>.Empty;
            while (xs != Set<T1>.Empty) {
                ret = Insert(func(xs.Value),ret);
                xs = xs.Next;
            }
            return ret;
        }

    }

}
