using System;
using System.Collections;
using System.Collections.Generic;
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

    /// <summary>
    /// 辞書
    /// </summary>
    public class Dict : IEnumerable<KeyValuePair<string, List<Entry>>> {


        private Dictionary<string, List<Entry>> Items { get; } = new Dictionary<string, List<Entry>>();

        public Dict Add(string read, string word, params string[] features) {
            List<Entry> entries;
            if (Items.TryGetValue(read, out entries) == false) {
                entries = new List<Entry>();
                Items.Add(read, entries);
            }
            var e = new Entry(read, word, features);
            if (entries.Any(x => Equals(x, e)) == false) {
                entries.Add(e);
            }

            return this;
        }

        public void Save(System.IO.TextWriter sw) {
            foreach (var item in Items) {

                foreach (var e in item.Value) {
                    sw.WriteLine(String.Join("\t", new[] { e.Read, e.Word }.Concat(e.Features)));
                }
            }
        }

        public static Dict Load(System.IO.TextReader sw) {
            var d = new Dict();
            string line;
            while ((line = sw.ReadLine()) != null) {
                var items = line.Split('\t');
                d.Add(items[0], items[1], items.Skip(2).ToArray());
            }
            return d;
        }

        public IEnumerable<Entry> Find(string key) {
            List<Entry> ret;
            if (Items.TryGetValue(key, out ret)) {
                foreach (var e in ret) {
                    yield return e;
                }
            }
        }

        public IEnumerator<KeyValuePair<string, List<Entry>>> GetEnumerator() {
            foreach (var kv in Items) {
                yield return kv;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }
    }

}
