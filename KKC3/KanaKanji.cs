using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
                Func<string, int, int, IEnumerable<Entry>> commonPrefixSearch = (str, i,len) => {
                    var ret = new List<Entry>();
                    if (len == -1) { len = 16; }
                    var n = Math.Min(str.Length, i + len);
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
                    Console.WriteLine(String.Join("\t", ret.Select(x => x.Item2.ToString())));
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
                Func<string, int, int, IEnumerable<Entry>> commonPrefixSearch = (str, i, len) => {
                    var ret = new List<Entry>();
                    foreach (var kv in trie.CommonPrefixSearch(str.ToCharArray().Skip(i))) {
                        if (len != -1) {
                            if (len == 0) { break; }
                        }
                        var read = String.Concat(kv.Item1);
                        var values = kv.Item2.Split('\n');
                        foreach (var value in values) {
                            var fields = value.Split('\t');
                            ret.Add(new Entry(read, fields[0], fields.Skip(1).ToArray()));
                        }
                        if (len > 0) { len -= 1; }
                    }
                    return ret;
                };

                var svm = StructuredSupportVectorMachine.Load("learn.model", featureFuncs, true);

                for (;;) {
                    var input = Console.ReadLine();
                    var ret = svm.Convert(input, commonPrefixSearch);
                    Console.WriteLine(String.Join("\t", ret.Select(x => x.Item2.ToString())));
                }
            }

            // StaticTrie辞書でかな漢字変換
            if (true) {

                // Trie辞書を用意
                Dict dict;
                if (System.IO.File.Exists("userdic.tsv")) {
                    using (var sw = new System.IO.StreamReader("userdic.tsv")) {
                        dict = Dict.Load(sw);
                    }
                } else {
                    dict = new Dict();
                }

                var s = new System.IO.FileStream("dict.trie", System.IO.FileMode.Open);
                var trie = StaticTrie<char, string>.Load(s, (k) => Encoding.UTF8.GetChars(k).FirstOrDefault(), (v) => Encoding.UTF8.GetString(v));

                var featureFuncs = KKCFeatureFunc.Create();
                Func<string, int, int, IEnumerable<Entry>> commonPrefixSearch = (str, i, len) => {
                    var ret = new List<Entry>();
                    var len2 = len;
                    foreach (var kv in trie.CommonPrefixSearch(str.ToCharArray().Skip(i))) {
                        if (len2 != -1) {
                            if (len2 == 0) { break; }
                        }
                        var read = String.Concat(kv.Item1);
                        var values = kv.Item2.Split('\n');
                        foreach (var value in values) {
                            var fields = value.Split('\t');
                            ret.Add(new Entry(read, fields[0], fields.Skip(1).ToArray()));
                        }
                        if (len2 > 0) { len -= 1; }
                    }

                    var str2 = (len != -1) ? str.Substring(i, len) : str.Substring(i);
                    foreach (var kv in dict.CommonPrefixSearch(str2)) {
                        ret.Add(kv);
                    }
                    return ret;
                };

                var svm = StructuredSupportVectorMachine.Load("learn.model", featureFuncs, true);

                for (;;) {
                    Console.Write("kana?>");
                    var input = Console.ReadLine();
                    var ret = svm.Convert(input, commonPrefixSearch);
                    var chunks = ret.Concat(new[] { Tuple.Create(input.Length, (Entry)null) }).EachCons(2).Select(x => Tuple.Create(x[0].Item1, x[1].Item1, x[0].Item2)).ToList();
                    for (;;) {
                        Console.WriteLine(String.Join("\t", chunks.Select((x,i) => $"[{i}:{x.Item1}] {x.Item3?.ToString() ?? input.Substring(x.Item1, x.Item2- x.Item1)}")));
                        Console.Write("cmd?> ");
                        var cmd = Console.ReadLine();
                        Match m;
                        m = Regex.Match(cmd, @"^\s*del\s+(\d+)\s*$");
                        if (m.Success) {
                            var index = int.Parse(m.Groups[1].Value);
                            if (0 <= index && index < chunks.Count) {
                                if (index == 0) {
                                    // 先頭は消せない
                                    chunks[0] = Tuple.Create(chunks[0].Item1, chunks[0].Item2, (Entry)null);
                                } else {
                                    var start = chunks[index].Item1;
                                    var end = chunks[index].Item2;
                                    chunks.RemoveAt(index);
                                    if (index > 1) {
                                        chunks[index - 1] = Tuple.Create(chunks[index - 1].Item1, end, (Entry)null);
                                    } else {
                                        chunks[0] = Tuple.Create(0, chunks[0].Item2, (Entry)null);
                                    }
                                }
                            }
                            continue;
                        }
                        m = Regex.Match(cmd, @"^\s*split\s+(\d+)\s*$");
                        if (m.Success) {
                            // 文字位置で分割する
                            var pos = int.Parse(m.Groups[1].Value);
                            var index = -1;
                            for (var i=0; i<chunks.Count; i++) {
                                if (chunks[i].Item1 <= pos && pos < chunks[i].Item2) {
                                    index = i;
                                    break;
                                }
                            }
                            if (index != -1) {

                                if (chunks[index].Item1 != pos) {
                                    var startpos = chunks[index].Item1;
                                    var endpos = chunks[index].Item2;
                                    if (startpos != pos && endpos != pos) {
                                        // 分割点以外の場合は分岐点を挿入
                                        chunks.RemoveAt(index);
                                        chunks.Insert(index + 0, Tuple.Create(startpos, pos, (Entry)null));
                                        chunks.Insert(index + 1, Tuple.Create(pos, endpos, (Entry)null));
                                    }
                                }
                            }
                            continue;
                        }
                        m = Regex.Match(cmd, @"^\s*retry\s*$");
                        if (m.Success) {
                            // 現在の文節分割を元に再変換を行う。
                            ret = svm.PartialConvert(input, chunks.Select(x => Tuple.Create(x.Item1, x.Item3)).ToList(), commonPrefixSearch);
                            chunks = ret.Concat(new[] { Tuple.Create(input.Length, (Entry)null) }).EachCons(2).Select(x => Tuple.Create(x[0].Item1, x[1].Item1, x[0].Item2)).ToList();
                            continue;
                        }
                        m = Regex.Match(cmd, @"^\s*find\s+(\d+)\s*$");
                        if (m.Success) {
                            // 文節を辞書変換する
                            var chunk = int.Parse(m.Groups[1].Value);
                            if (chunk < 0 || chunks.Count <= chunk) {
                                continue;
                            }
                            var read = input.Substring(chunks[chunk].Item1, chunks[chunk].Item2 - chunks[chunk].Item1);

                            var items = new List<string[]>();
                            var node = trie.Search(read.ToCharArray());
                            if (node != null) {
                                items.AddRange(node.Split('\n').Select(x => x.Split('\t')));
                            }
                            items.AddRange(dict.Find(read).Select(x => new[] { x.Word, x.Features[0] }));

                            for (; ; ) {
                                for (var i = 0; i < items.Count; i++) {
                                    Console.WriteLine($"[{i}] {items[i][0]}/{items[i][1]}");
                                }
                                Console.Write("select?>");
                                var select = Int32.Parse(Console.ReadLine());
                                if (0 <= select && select < items.Count) {
                                    chunks[chunk] = Tuple.Create(chunks[chunk].Item1, chunks[chunk].Item2, new Entry(read, items[select][0], items[select][1]));
                                    break;
                                }
                            }
                            continue;
                        }
                        m = Regex.Match(cmd, @"^\s*regist\s+(\d+)\s*$");
                        if (m.Success) {
                            // 文節を辞書変換する
                            var chunk = int.Parse(m.Groups[1].Value);
                            if (chunk < 0 || chunks.Count <= chunk) {
                                continue;
                            }
                            var read = input.Substring(chunks[chunk].Item1, chunks[chunk].Item2 - chunks[chunk].Item1);
                            Console.Write("word?>");
                            var word = Console.ReadLine();
                            Console.Write("feat?>");
                            var feat = Console.ReadLine();
                            if (!string.IsNullOrWhiteSpace(word) && !string.IsNullOrWhiteSpace(feat)) {
                                var newChunk = Tuple.Create(chunks[chunk].Item1, chunks[chunk].Item2, new Entry(read, word, feat));
                                chunks[chunk] = newChunk;
                            }
                            continue;
                        }
                        m = Regex.Match(cmd, @"^\s*train\s*$");
                        if (m.Success) {
                            // 現在の変換結果から学習を行って変換を完了する。
                            var teature = chunks.Select(x => x.Item3).ToList();
                            if (teature.Contains(null) == false) {
                                svm.Learn(teature, commonPrefixSearch, (node) => {
                                    var item = trie.Search(node.Read.ToArray());
                                    if (item == null || item.Split('\n').Select(x => x.Split('\t')).All(x => x[0] != node.Word)) {
                                        dict.Add(node.Read, node.Word, node.Features); 
                                    }
                                });
                                svm.Save("learn2.model");
                                using (var sw = new System.IO.StreamWriter("userdic.tsv")) {
                                    dict.Save(sw);
                                }
                                break;
                            } else {
                                continue;
                            }
                        }
                        m = Regex.Match(cmd, @"^\s*ok\s*$");
                        if (m.Success) {
                            // 変換処理を完了する
                            break;
                        }
                    }
                }
            }
        }
    }
}
