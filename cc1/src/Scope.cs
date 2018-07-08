using System;
using System.Collections;
using System.Collections.Generic;

namespace AnsiCParser {

    /// <summary>
    /// 各種名前空間
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    public class Scope<TValue> : IEnumerable<Tuple<string, TValue>> {
        /// <summary>
        /// 空のスコープ
        /// </summary>
        public static Scope<TValue> Empty { get; } = new Scope<TValue>();

        /// <summary>
        /// 親スコープ
        /// </summary>
        public Scope<TValue> Parent { get; } = Empty;

        /// <summary>
        /// スコープに登録されている要素
        /// </summary>
        private readonly List<Tuple<string, TValue>> _entries = new List<Tuple<string, TValue>>();

        /// <summary>
        /// コンストラクタ
        /// </summary>
        private Scope() {
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        protected Scope(Scope<TValue> parent) {
            Parent = parent;
        }

        /// <summary>
        /// 新しくネストしたスコープを作って返す。
        /// </summary>
        /// <returns></returns>
        public Scope<TValue> Extend() {
            return new Scope<TValue>(this);
        }

        /// <summary>
        /// スコープに要素を登録する
        /// </summary>
        /// <param name="ident"></param>
        /// <param name="value"></param>
        public void Add(string ident, TValue value) {
            _entries.Add(Tuple.Create(ident, value));
        }

        /// <summary>
        /// 指定した名前と型の要素がこのスコープもしくは上位のスコープに存在するなら取得する。
        /// </summary>
        /// <param name="ident"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool TryGetValue<T>(string ident, out T value) where T : TValue {
            bool isCurrent;
            return TryGetValue(ident, out value, out isCurrent);
        }

        /// <summary>
        /// 指定した名前の要素がこのスコープもしくは上位のスコープに存在するなら取得する。
        /// さらに、現在のスコープにあるかどうかも調べる。
        /// </summary>
        /// <param name="ident"></param>
        /// <param name="value"></param>
        /// <param name="isCurrent"></param>
        /// <returns></returns>
        public bool TryGetValue<T>(string ident, out T value, out bool isCurrent) where T : TValue {
            var it = this;
            isCurrent = true;
            while (it != Empty) {
                var val = it._entries.FindLast(x => x.Item1 == ident);
                if (val != null) {
                    if (val.Item2 is T) {
                        value = (T)(val.Item2);
                        return true;
                    } else {
                        value = default(T);
                        return false;
                    }
                }
                it = it.Parent;
                isCurrent = false;
            }
            value = default(T);
            return false;
        }

        #region IEnumerable<Tuple<string, TValue>>の実装

        public IEnumerator<Tuple<string, TValue>> GetEnumerator() {
            foreach (var entry in _entries) {
                yield return entry;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() {
            foreach (var entry in _entries) {
                yield return entry;
            }
        }
        #endregion

    }
}
