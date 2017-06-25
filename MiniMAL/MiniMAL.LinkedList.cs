using System;
using System.Linq;
using System.Text;

namespace MiniMAL
{
    /// <summary>
    /// 連結リスト
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class LinkedList<T> {
        public T Value { get; private set; }
        public LinkedList<T> Next { get; }
        public static LinkedList<T> Empty { get; } = new LinkedList<T>(default(T), null);

        public LinkedList(T value, LinkedList<T> next) {
            Value = value;
            Next = next;
        }

        public override string ToString() {
            var sb = new StringBuilder();
            var it = this;
            sb.Append("[");
            if (it != Empty) {
                sb.Append(it.Value);
                it = it.Next;
                while (it != Empty) {
                    sb.Append("; " + it.Value);
                    it = it.Next;
                }
            }
            sb.Append("]");
            return sb.ToString();
        }

        public void Replace(T value) {
            Value = value;
        }

    }

    /// <summary>
    /// 連結リスト操作
    /// </summary>
    public static class LinkedList {
        public static LinkedList<T> Extend<T>(T v, LinkedList<T> next) {
            return new LinkedList<T>(v, next);
        }
        public static T First<T>(Func<T, bool> f, LinkedList<T> env) {
            for (var e = env; e != LinkedList<T>.Empty; e = e.Next) {
                if (f(e.Value)) { return e.Value; }
            }
            return default(T);
        }
        public static int FirstIndex<T>(Func<T, bool> f, LinkedList<T> env) {
            var i = 0;
            for (var e = env; e != LinkedList<T>.Empty; e = e.Next) {
                if (f(e.Value)) { return i; }
                i += 1;
            }
            return -1;
        }
        public static LinkedList<T> Reverse<T>(LinkedList<T> list) {
            var ret = LinkedList<T>.Empty;
            for (var e = list; e != LinkedList<T>.Empty; e = e.Next) {
                ret = Extend(e.Value, ret);
            }
            return ret;
        }
        public static LinkedList<T2> Map<T1, T2>(Func<T1, T2> f, LinkedList<T1> list) {
            var ret = LinkedList<T2>.Empty;
            for (var e = list; e != LinkedList<T1>.Empty; e = e.Next) {
                ret = Extend(f(e.Value), ret);
            }
            return Reverse(ret);
        }

        public static T2 FoldRight<T1, T2>(Func<T2, T1, T2> f, LinkedList<T1> env, T2 a) {
            var kv = Reverse(env);
            var ret = a;
            for (var e = kv; e != LinkedList<T1>.Empty; e = e.Next) {
                ret = f(ret, e.Value);
            }
            return ret;
        }

        public static T At<T>(LinkedList<T> list, int num) {
            while (num > 0) {
                if (list == LinkedList<T>.Empty) {
                    throw new ArgumentOutOfRangeException();
                }
                list = list.Next;
                num--;
            }
            return list.Value;
        }

        public static LinkedList<T> Create<T>(params T[] values)
        {
            return values.Reverse().Aggregate(LinkedList<T>.Empty, (s, x) => Extend(x, s));
        }

        public static LinkedList<T> Concat<T>(params LinkedList<T>[] l)
        {
            return l.Reverse().Aggregate(LinkedList<T>.Empty, (s, x) => FoldRight((ss, xx) => Extend(xx, ss), x, s));
        }
    }

}
