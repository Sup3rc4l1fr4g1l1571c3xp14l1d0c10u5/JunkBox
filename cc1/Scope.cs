using System;
using System.Collections.Generic;

namespace AnsiCParser {
    /// <summary>
    /// 名前空間
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    public class Scope<TValue> {
        /// <summary>
        /// 空のスコープ
        /// </summary>
        public static Scope<TValue> Empty { get; } = new Scope<TValue>();

        /// <summary>
        /// 親スコープ
        /// </summary>
        public Scope<TValue> Parent { get; } = Empty;

        /// <summary>
        /// 登録されている要素
        /// </summary>
        private readonly List<Tuple<string, TValue>> _entries = new List<Tuple<string, TValue>>();

        private Scope() {
        }

        protected Scope(Scope<TValue> parent) {
            Parent = parent;
        }

        /// <summary>
        /// 新しいスコープを作って返す。
        /// </summary>
        /// <returns></returns>
        public Scope<TValue> Extend() {
            return new Scope<TValue>(this);
        }

        /// <summary>
        /// 現在のスコープに要素を登録する
        /// </summary>
        /// <param name="ident"></param>
        /// <param name="value"></param>
        public void Add(string ident, TValue value) {
            _entries.Add(Tuple.Create(ident, value));
        }

        /// <summary>
        /// 指定した名前の要素が存在するか調べる。
        /// </summary>
        /// <param name="ident"></param>
        /// <returns></returns>
        public bool ContainsKey(string ident) {
            var it = this;
            while (it != null) {
                if (it._entries.FindLast(x => x.Item1 == ident) != null) {
                    return true;
                }
                it = it.Parent;
            }
            return false;
        }


        /// <summary>
        /// 指定した名前の要素が存在するなら取得する。
        /// </summary>
        /// <param name="ident"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool TryGetValue(string ident, out TValue value) {
            var it = this;
            while (it != null) {
                var val = it._entries.FindLast(x => x.Item1 == ident);
                if (val != null) {
                    value = val.Item2;
                    return true;
                }
                it = it.Parent;
            }
            value = default(TValue);
            return false;
        }

        /// <summary>
        /// 指定した名前の要素が存在するなら取得する。さらに、現在のスコープにあるかどうかも調べる。
        /// </summary>
        /// <param name="ident"></param>
        /// <param name="value"></param>
        /// <param name="isCurrent"></param>
        /// <returns></returns>
        public bool TryGetValue(string ident, out TValue value, out bool isCurrent) {
            var it = this;
            isCurrent = true;
            while (it != null) {
                var val = it._entries.FindLast(x => x.Item1 == ident);
                if (val != null) {
                    value = val.Item2;
                    return true;
                }
                it = it.Parent;
                isCurrent = false;
            }
            value = default(TValue);
            return false;
        }

    }
}