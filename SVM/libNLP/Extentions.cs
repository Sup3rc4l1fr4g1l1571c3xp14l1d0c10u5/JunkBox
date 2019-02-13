using System;
using System.Collections.Generic;
using System.Linq;

namespace libNLP.Extentions {
    /// <summary>
    /// 拡張メソッド群
    /// </summary>
    public static class Extention {
        /// <summary>
        /// self に 関数 pred を適用した結果を返します。
        /// </summary>
        /// <typeparam name="TSource">self の要素の型。</typeparam>
        /// <typeparam name="TDest"></typeparam>
        /// <param name="self"></param>
        /// <param name="pred"></param>
        /// <returns></returns>
        public static TDest Apply<TSource, TDest>(this TSource self, Func<TSource, TDest> pred) {
            return pred(self);
        }

        /// <summary>
        /// self に 手続き proc を適用し、selfを返します。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="self"></param>
        /// <param name="proc"></param>
        /// <returns></returns>
        public static T Tap<T>(this T self, Action<T> proc) {
            proc(self);
            return self;
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
        /// シーケンスを重複ありで n 要素ずつに区切った列挙子を返します。
        /// </summary>
        /// <typeparam name="TSource">self の要素の型。</typeparam>
        /// <param name="self"></param>
        /// <param name="n"></param>
        /// <returns></returns>
        public static IEnumerable<TSource[]> EachCons<TSource>(this IEnumerable<TSource> self, int n) {
            var ret = new Queue<TSource>(n);
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

        /// <summary>
        /// シーケンス中の func が真になる要素を区切り要素と見なして分割した列挙子を返します。
        /// </summary>
        /// <typeparam name="TSource">self の要素の型。</typeparam>
        /// <param name="self"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        public static IEnumerable<TSource[]> Split<TSource>(this IEnumerable<TSource> self, Func<TSource, bool> func) {
            var ret = new List<TSource>();
            if (self.Any() == false) {
                yield break;
            }
            foreach (var next in self) {
                if (func(next)) {
                    yield return ret.ToArray();
                    ret.Clear();
                } else {
                    ret.Add(next);
                }
            }
            yield return ret.ToArray();
        }

        /// <summary>
        /// シーケンス内の指定されたインデックス位置にある要素を返します。インデックスが範囲外の場合は defaultValue を返します。
        /// </summary>
        /// <typeparam name="TSource">self の要素の型。</typeparam>
        /// <param name="self">返される要素が含まれる System.Collections.Generic.IList。</param>
        /// <param name="index">取得する要素の、0 から始まるインデックス。</param>
        /// <param name="defaultValue">インデックスが範囲外の場合のデフォルト値</param>
        /// <returns>インデックスがソース シーケンスの範囲外の場合は defaultValue。それ以外の場合は、ソース シーケンスの指定した位置にある要素</returns>
        public static TSource ElementAtOrDefault<TSource>(this IList<TSource> self, int index, TSource defaultValue) {
            if (index < 0 || index >= self.Count) {
                return defaultValue;
            } else {
                return self[index];
            }
        }
    }
}
