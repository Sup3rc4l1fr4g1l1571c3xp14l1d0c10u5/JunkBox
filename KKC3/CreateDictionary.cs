using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KKC3 {
    public class CreateDictionary {
        public static void Run(string[] args) {
            Console.WriteLine("Create Dictionary:");
            // 単語辞書を作る
            var dict = new Dict();

            // 学習用分かち書きデータから辞書を作成
            foreach (var file in System.IO.Directory.EnumerateFiles(@"..\..\data\Corpus", "*.txt")) {
                Console.WriteLine($"  Read File {file}");
                foreach (var line in System.IO.File.ReadLines(file)) {
                    var items = line.Split('\t');
                    if (items.Length >= 3) {
                        dict.Add(CharConv.toHiragana(items[1] == "" ? items[0] : items[1]), items[0], items[2]);
                    }
                }
            }

            // unidicの辞書を読み取り
            foreach (var file in System.IO.Directory.EnumerateFiles(@"d:\work", "*.csv")) {
                Console.WriteLine($"  Read File {file}");
                foreach (var line in System.IO.File.ReadLines(file)) {
                    var items = line.Split(',');
                    if (items.Length >= 25) {
                        dict.Add(CharConv.toHiragana(items[24] == "*" ? items[0] : items[24]), items[0], items[4]);
                    }
                }
            }

            // 辞書を保存
            using (var sw = new System.IO.StreamWriter("dict.tsv")) {
                dict.Save(sw);
            }

            // trie辞書を保存
            var trieConstructor = new Trie<char, string>.Constructor();
            foreach (var kv in dict) {
                trieConstructor.Add(kv.Key.ToArray(), String.Join("\n", kv.Value.Select(x => String.Join("\t", new[] { x.Word }.Concat(x.Features)))));
            }
            var trie = trieConstructor.Create();
            System.GC.Collect();
            using (var s = new System.IO.FileStream("dict.trie", System.IO.FileMode.Create)) {
                trie.ToStaticTrie(s, (k) => Encoding.UTF8.GetBytes(new[] { k }), (v) => Encoding.UTF8.GetBytes(v));
            }

        }
    }
}
