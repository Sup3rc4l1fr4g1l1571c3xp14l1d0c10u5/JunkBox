using System;
using System.Linq;

namespace KKC3 {

    /// <summary>
    /// 辞書エントリ
    /// </summary>
    public class Entry {
        /// <summary>
        /// 読み
        /// </summary>
        public string Read { get; }

        /// <summary>
        /// 書き
        /// </summary>
        public string Word { get; }

        /// <summary>
        /// その他属性
        /// </summary>
        public string[] Features { get; }

        public Entry(string read, string word, params string[] features) {
            Word = word;
            Read = read;
            Features = features;
        }

        public override string ToString() {
            return string.Join("/", new[] { Read, Word }.Concat(Features));
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(this, obj)) { return true; }
            if (obj == null) { return false; }
            if (!(obj is Entry)) { return false; }
            var other = (Entry)obj;
            return ((Word == other.Word) && (Read == other.Read) && Features.SequenceEqual(other.Features));
        }

        public override int GetHashCode() {
            return Read.GetHashCode();
        }
    }

}
