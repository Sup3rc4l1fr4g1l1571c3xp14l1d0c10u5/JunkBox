using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MiniML {
    public static partial class MiniML {
        /// <summary>
        /// 環境（リンクリストで表現）
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public class Environment<T> : IEnumerable<KeyValuePair<string,T>> {

            /// <summary>
            /// キー
            /// </summary>
            public string Key { get; }

            /// <summary>
            /// 値
            /// </summary>
            public T Value { get; }

            /// <summary>
            /// 次の要素
            /// </summary>
            public Environment<T> Next { get; }

            /// <summary>
            /// 空要素
            /// </summary>
            public static Environment<T> Empty { get; } = new Environment<T>(null, default(T), null);

            public Environment(string key, T value, Environment<T> next) {
                Key = key;
                Value = value;
                Next = next;
            }

            public IEnumerator<KeyValuePair<string,T>> GetEnumerator()
            {
                var self = this;
                while (self.Next != Empty)
                {
                    yield return new KeyValuePair<string,T>(Key, Value);
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        /// <summary>
        /// 環境操作
        /// </summary>
        public static class Environment {
            public class NotBound : Exception {
                public NotBound(string s) : base(s) {
                }
            }

            public static Environment<T> Extend<T>(string key, T value, Environment<T> env) {
                return new Environment<T>(key, value, env);
            }

            /// <summary>
            /// キーに対応する値を検索
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="key">検索キー</param>
            /// <param name="env">検索環境</param>
            /// <returns>キーに対応する値。存在しない場合はNotBound例外が発生。</returns>
            public static T LookUp<T>(string key, Environment<T> env) {
                for (var e = env; e != Environment<T>.Empty; e = e.Next) {
                    if (e.Key == key) { return e.Value; }
                }
                throw new NotBound(key);
            }

            /// <summary>
            /// 既存の環境 env の値に射影関数 f を適用した新しい環境を生成
            /// </summary>
            /// <typeparam name="T1"></typeparam>
            /// <typeparam name="T2"></typeparam>
            /// <param name="f"></param>
            /// <param name="env"></param>
            /// <returns></returns>
            public static Environment<T2> Map<T1, T2>(Func<T1, T2> f, Environment<T1> env) {
                List<Tuple<string, T2>> kv = new List<Tuple<string, T2>>();
                for (var e = env; e != Environment<T1>.Empty; e = e.Next) {
                    kv.Add(Tuple.Create(e.Key, f(e.Value)));
                }
                return kv.Reverse<Tuple<string, T2>>().Aggregate(Environment<T2>.Empty, (s, x) => new Environment<T2>(x.Item1, x.Item2, s));
            }

            /// <summary>
            /// 末尾からのたたみ込みを行う
            /// </summary>
            /// <typeparam name="T1"></typeparam>
            /// <typeparam name="T2"></typeparam>
            /// <param name="f"></param>
            /// <param name="env"></param>
            /// <param name="a"></param>
            /// <returns></returns>
            public static T2 FoldRight<T1, T2>(Func<T2, T1, T2> f, Environment<T1> env, T2 a) {
                List<T1> kv = new List<T1>();
                for (var e = env; e != Environment<T1>.Empty; e = e.Next) {
                    kv.Add(e.Value);
                }
                return kv.Reverse<T1>().Aggregate(a, f);
            }
        }
    }


}