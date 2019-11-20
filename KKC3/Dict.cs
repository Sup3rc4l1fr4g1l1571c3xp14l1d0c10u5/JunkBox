﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KKC3 {


    /// <summary>
    /// 辞書
    /// </summary>
    public class Dict {

        /// <summary>
        /// 辞書エントリ
        /// </summary>
        public class Entry {
            /// <summary>
            /// 読み
            /// </summary>
            public string read { get; }

            /// <summary>
            /// 書き
            /// </summary>
            public string word { get; }

            /// <summary>
            /// その他属性
            /// </summary>
            public string[] features { get; }

            public Entry(string read, string word, params string[] features) {
                this.word = word;
                this.read = read;
                this.features = features;
            }

            public override string ToString() {
                return string.Join("/", this.read, this.word, this.features);
            }

            public override bool Equals(object obj) {
                if (Object.ReferenceEquals(this, obj)) { return true; }
                if (obj == null) { return false; }
                if (!(obj is Entry)) { return false; }
                var other = (Entry)obj;
                return ((this.word == other.word) && (this.read == other.read) && this.features.SequenceEqual(other.features));
            }

            public override int GetHashCode() {
                return this.read.GetHashCode();
            }
        }

        private Dictionary<string, List<Entry>> items { get; } = new Dictionary<string, List<Entry>>();

        public Dict Add(string read, string word, params string[] features) {
            List<Entry> entries;
            if (this.items.TryGetValue(read, out entries) == false) {
                entries = new List<Entry>();
                this.items.Add(read, entries);
            }
            var e = new Entry(read, word, features);
            if (entries.Any(x => Entry.Equals(x, e)) == false) {
                entries.Add(e);
            }

            return this;
        }

        public void Save(System.IO.TextWriter sw) {
            foreach (var item in items) {

                foreach (var e in item.Value) {
                    sw.WriteLine(String.Join("\t", new[] { e.read, e.word }.Concat(e.features)));
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
            if (this.items.TryGetValue(key, out ret)) {
                foreach (var e in ret) {
                    yield return e;
                }
            }
        }
    }

}
