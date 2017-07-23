using System;
using System.Collections.Generic;
using System.Linq;

namespace MiniMAL {

    /// <summary>
    /// 環境
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Environment<T> { 

        /// <summary>
        /// 束縛名
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// 値
        /// </summary>
        public T Value { get; }

        /// <summary>
        /// 次の要素
        /// </summary>
        public Environment<T> Next { get; }

        /// <summary>
        /// 終端を意味する空要素
        /// </summary>
        public static Environment<T> Empty { get; } = new Environment<T>(null, default(T), null);

        public Environment(string id, T value, Environment<T> next) {
            Id = id;
            Value = value;
            Next = next;
        }
    }

    /// <summary>
    /// 環境操作
    /// </summary>
    public static class Environment {

        public static Environment<T> Extend<T>(string x, T v, Environment<T> env) {
            return new Environment<T>(x, v, env);
        }

        public static T LookUp<T>(string x, Environment<T> env) {
            for (var e = env; e != Environment<T>.Empty; e = e.Next) {
                if (e.Id == x) { return e.Value; }
            }
            throw new Exception.NotBound(x);
        }

        public static bool Contains<T>(string x, Environment<T> env) {
            for (var e = env; e != Environment<T>.Empty; e = e.Next) {
                if (e.Id == x) { return true; }
            }
            return false;
        }

        public static Environment<T2> Map<T1, T2>(Func<T1, T2> f, Environment<T1> env) {
            List<Tuple<string, T2>> kv = new List<Tuple<string, T2>>();
            for (var e = env; e != Environment<T1>.Empty; e = e.Next) {
                kv.Add(Tuple.Create(e.Id, f(e.Value)));
            }
            return kv.Reverse<Tuple<string, T2>>().Aggregate(Environment<T2>.Empty, (s, x) => new Environment<T2>(x.Item1, x.Item2, s));
        }

        public static T2 FoldLeft<T1, T2>(Func<T2, T1, T2> f, Environment<T1> env, T2 a) {
            List<T1> kv = new List<T1>();
            for (var e = env; e != Environment<T1>.Empty; e = e.Next) {
                kv.Add(e.Value);
            }
            return kv.Aggregate(a, f);
        }

        public static T2 FoldRight<T1, T2>(Func<T2, T1, T2> f, Environment<T1> env, T2 a) {
            List<T1> kv = new List<T1>();
            for (var e = env; e != Environment<T1>.Empty; e = e.Next) {
                kv.Add(e.Value);
            }
            return kv.Reverse<T1>().Aggregate(a, f);
        }

        public static LinkedList<string> Keys<T>(Environment<T> env) {
            List<string> kv = new List<string>();
            for (var e = env; e != Environment<T>.Empty; e = e.Next) {
                kv.Add(e.Id);
            }
            return kv.Reverse<string>().Aggregate(LinkedList<string>.Empty, (s, x) => LinkedList.Extend(x, s));
        }

        public static LinkedList<T> Values<T>(Environment<T> env) {
            List<T> kv = new List<T>();
            for (var e = env; e != Environment<T>.Empty; e = e.Next) {
                kv.Add(e.Value);
            }
            return kv.Reverse<T>().Aggregate(LinkedList<T>.Empty, (s, x) => LinkedList.Extend(x, s));
        }

    }

}
