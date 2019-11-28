using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KKC3 {
    public static class KanaKanji {
        public static void Run(string[] args) {
            // メモリ辞書でかな漢字変換
            if (false) {
                Dict dict;
                using (var sw = new System.IO.StreamReader("dict.tsv")) {
                    dict = Dict.Load(sw);
                }
                var featureFuncs = KKCFeatureFunc.Create();
                Func<string, int, IEnumerable<Entry>> commonPrefixSearch = (str, i) => {
                    var ret = new List<Entry>();
                    var n = Math.Min(str.Length, i + 16);
                    for (var j = i + 1; j <= n; j++) {
                        var read = str.Substring(i, j - i);
                        ret.AddRange(dict.Find(read));
                    }
                    return ret;
                };

                var svm = StructuredSupportVectorMachine.Load("learn.model", featureFuncs, true);

                for (;;) {
                    var input = Console.ReadLine();
                    var ret = svm.Convert(input, commonPrefixSearch);
                    Console.WriteLine(String.Join("\t", ret.Select(x => x.ToString())));
                }
            }

            // Trie辞書でかな漢字変換
            if (false) {
                Trie<char, string> trie = null;
                using (var sw = new System.IO.StreamReader("dict.tsv")) {
                    Dict dict;
                    dict = Dict.Load(sw);
                    var trieConstructor = new Trie<char, string>.Constructor();
                    foreach (var kv in dict) {
                        trieConstructor.Add(kv.Key.ToArray(), String.Join("\n", kv.Value.Select(x => String.Join("\t", new[] { x.Word }.Concat(x.Features)))));
                    }
                    trie = trieConstructor.Create();
                }
                System.GC.Collect();

                var featureFuncs = KKCFeatureFunc.Create();
                Func<string, int, IEnumerable<Entry>> commonPrefixSearch = (str, i) => {
                    var ret = new List<Entry>();
                    foreach (var kv in trie.CommonPrefixSearch(str.ToCharArray().Skip(i))) {
                        var read = String.Concat(kv.Item1);
                        var values = kv.Item2.Split('\n');
                        foreach (var value in values) {
                            var fields = value.Split('\t');
                            ret.Add(new Entry(read, fields[0], fields.Skip(1).ToArray()));
                        }
                    }
                    return ret;
                };

                var svm = StructuredSupportVectorMachine.Load("learn.model", featureFuncs, true);

                for (;;) {
                    var input = Console.ReadLine();
                    var ret = svm.Convert(input, commonPrefixSearch);
                    Console.WriteLine(String.Join("\t", ret.Select(x => x.ToString())));
                }
            }

            // StaticTrie辞書でかな漢字変換
            if (true) {
                var s = new System.IO.FileStream("dict.trie", System.IO.FileMode.Open);
                var trie = StaticTrie<char, string>.Load(s, (k) => Encoding.UTF8.GetChars(k).FirstOrDefault(), (v) => Encoding.UTF8.GetString(v));

                var featureFuncs = KKCFeatureFunc.Create();
                Func<string, int, IEnumerable<Entry>> commonPrefixSearch = (str, i) => {
                    var ret = new List<Entry>();
                    foreach (var kv in trie.CommonPrefixSearch(str.ToCharArray().Skip(i))) {
                        var read = String.Concat(kv.Item1);
                        var values = kv.Item2.Split('\n');
                        foreach (var value in values) {
                            var fields = value.Split('\t');
                            ret.Add(new Entry(read, fields[0], fields.Skip(1).ToArray()));
                        }
                    }
                    return ret;
                };

                var svm = StructuredSupportVectorMachine.Load("learn.model", featureFuncs, true);

                for (;;) {
                    var input = Console.ReadLine();
                    var ret = svm.Convert(input, commonPrefixSearch);
                    Console.WriteLine(String.Join("\t", ret.Select(x => x.ToString())));
                }
            }
        }
    }
}
