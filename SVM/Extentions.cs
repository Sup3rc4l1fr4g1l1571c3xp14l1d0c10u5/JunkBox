using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace svm_fobos {
    /// <summary>
    /// 拡張メソッド
    /// </summary>
    internal static class Extention {
        /// <summary>
        /// self に 関数 pred を適用した結果を返します。
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <param name="self"></param>
        /// <param name="pred"></param>
        /// <returns></returns>
        public static T2 Apply<T1, T2>(this T1 self, Func<T1, T2> pred) {
            return pred(self);
        }

        /// <summary>
        /// 辞書型から指定したキーを持つ要素を取得し返します。指定したキーを持つ要素が見つからない場合は代用値を返します。
        /// </summary>
        /// <typeparam name="TKey">ディクショナリ内のキーの型</typeparam>
        /// <typeparam name="TValue">ディクショナリ内の値の型</typeparam>
        /// <param name="self">辞書型</param>
        /// <param name="key">取得または設定する要素のキー</param>
        /// <param name="defaultValue">指定したキーを持つ要素が見つからない場合の代用値</param>
        /// <returns>キーが見つかった場合は、指定したキーに関連付けられている値。それ以外の場合は defaultValue パラメーターで指定されている代用値。</returns>
        public static TValue GetValue<TKey, TValue>(this IDictionary<TKey, TValue> self, TKey key, TValue defaultValue) {
            TValue value;
            if (self.TryGetValue(key, out value)) {
                return value;
            } else {
                return defaultValue;
            }
        }

        /// <summary>
        /// 要素を重複ありで n 要素ずつに区切った列挙子を返します。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="self"></param>
        /// <param name="n"></param>
        /// <returns></returns>
        public static IEnumerable<T[]> EachCons<T>(this IEnumerable<T> self, int n) {
            var ret = new Queue<T>(n);
            foreach (var next in self) {
                if (ret.Count == n) {
                    yield return ret.ToArray();
                    ret.Dequeue();
                }
                ret.Enqueue(next);
            }
            if (ret.Count == n) {
                yield return ret.ToArray();
            }
        }

        public static IEnumerable<T[]> Split<T>(this IEnumerable<T> self, Func<T,bool> func) {
            var ret = new List<T>();
            foreach (var item in self) {
                if (func(item)) {
                    yield return ret.ToArray();
                    ret.Clear();
                }  else {
                    ret.Add(item);
                }
            }
            yield return ret.ToArray();
        }

        public static T ElementAtOrDefault<T>(this IEnumerable<T> self, int index, T defaultValue) {
            using (var it = self.GetEnumerator()) {
                while (index >= 0 && it.MoveNext()) {
                    if (index == 0) {
                        return it.Current;
                    } else {
                        index--;
                    }
                }
                return defaultValue;
            }
        }
    }
}
