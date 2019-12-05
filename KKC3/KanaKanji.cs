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
                var s = new System.IO.FileStream("dict.trie", System.IO.FileMode.Open);
                var trie = StaticTrie<char, string>.Load(s, (k) => Encoding.UTF8.GetChars(k).FirstOrDefault(), (v) => Encoding.UTF8.GetString(v));

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
                            var chunk = int.Parse(m.Groups[1].Value);
                            var start = chunks[chunk].Item1;
                            var end = chunks[chunk].Item2;
                            chunks.RemoveAt(chunk);
                            if (chunk > 1) {
                                chunks[chunk - 1] = Tuple.Create(chunks[chunk - 1].Item1, end, (Entry)null);
                            }else {
                                chunks[0] = Tuple.Create(0, chunks[0].Item2, (Entry)null);
                            }
                            continue;
                        }
                        m = Regex.Match(cmd, @"^\s*split\s+(\d+)\s*$");
                        if (m.Success) {
                            var pos = int.Parse(m.Groups[1].Value);
                            var index = -1;
                            for (var i=0; i<chunks.Count-1; i++) {
                                if (chunks[i].Item1 <= pos && pos < chunks[i+1].Item1) {
                                    index = i;
                                    break;
                                }
                            }

                            if (chunks[index].Item1 != pos) {
                                var startpos = chunks[index].Item1;
                                var endpos = chunks[index].Item2;
                                chunks.RemoveAt(index);
                                chunks.Insert(index + 0, Tuple.Create(startpos, pos, (Entry)null));
                                chunks.Insert(index + 1, Tuple.Create(pos, endpos, (Entry)null));
                            }
                            continue;
                        }
                        m = Regex.Match(cmd, @"^\s*retry\s*$");
                        if (m.Success) {
                            ret = svm.PartialConvert(input, chunks.Select(x => Tuple.Create(x.Item1, x.Item3)).ToList(), commonPrefixSearch);
                            chunks = ret.Concat(new[] { Tuple.Create(input.Length, (Entry)null) }).EachCons(2).Select(x => Tuple.Create(x[0].Item1, x[1].Item1, x[0].Item2)).ToList();
                            continue;
                        }
                        m = Regex.Match(cmd, @"^\s*find\s+(\d+)\s*$");
                        if (m.Success) {
                            var chunk = int.Parse(m.Groups[1].Value);
                            var read = input.Substring(chunks[chunk].Item1, chunks[chunk].Item2 - chunks[chunk].Item1);
                            var node = trie.Search(read.ToCharArray());
                            if (node != null) {
                                for (; ; ) {
                                    var items = node.Split('\n');
                                    for (var i=0;i<items.Length; i++) {
                                        Console.WriteLine($"[{i}] {items[i].Split('\t')[0]}");
                                    }
                                    Console.WriteLine("select?>");
                                    var select = Int32.Parse(Console.ReadLine());
                                    if (0 <= select && select < items.Length) {
                                        var fields = items[select].Split('\t');
                                        chunks[chunk] = Tuple.Create(chunks[chunk].Item1, chunks[chunk].Item2, new Entry(read, fields[0], fields[1]));
                                        break;
                                    }
                                }
                            }
                            continue;
                        }
                        m = Regex.Match(cmd, @"^\s*train\s*$");
                        if (m.Success) {
                            var teature = chunks.Select(x => x.Item3).ToList();
                            if (teature.Contains(null) == false) {
                                svm.Learn(teature, commonPrefixSearch, (node) => { });
                                break;
                            } else {
                                continue;
                            }
                        }
                        m = Regex.Match(cmd, @"^\s*ok\s*$");
                        if (m.Success) {
                            break;
                        }
                    }
                }
            }
        }
    }
}
