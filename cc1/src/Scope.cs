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

        /// <summary>
        /// このスコープの全要素
        /// </summary>
        public IEnumerable<Tuple<string, TValue>> GetEnumertor() {
            return _entries;
        }

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
            while (it != Empty) {
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
            while (it != Empty) {
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
        /// 指定した名前と型の要素が存在するなら取得する。
        /// </summary>
        /// <param name="ident"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool TryGetValue<T>(string ident, out T value) where T : TValue {
            var it = this;
            while (it != Empty) {
                var val = it._entries.FindLast(x => x.Item1 == ident);
                if (val != null) {
                    if (val.Item2 is T) {
                        value = (T)val.Item2;
                        return true;
                    } else {
                        value = default(T);
                        return false;
                    }
                }
                it = it.Parent;
            }
            value = default(T);
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
            while (it != Empty) {
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

        public bool IsGlobalScope() {
            return Parent == Empty;
        }

        public Scope<TValue> GetGlobalScope() {
            var it = this;
            if (it == Empty) {
                throw new Exception("");
            }
            while (it.Parent != Empty) {
                it = it.Parent;
            }
            return it;
        }
    }
}
