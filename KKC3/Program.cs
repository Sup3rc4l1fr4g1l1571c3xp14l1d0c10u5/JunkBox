﻿/*
Mecab:
  mecab --node-format=%%m\t%%f[20]\t%%f[0]\n --eos-format=\n --unk-format=%%M "%~dp1%~nx1" > "%~dp1%~n1.mecabed.txt"

NHK News:
  Array.from(document.querySelectorAll('.content--summary, .content--summary-more, .content--body > .body-text')).map(x => x.innerText).join("\r\n")

*/

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KKC3 {
    class Program {
        static void Main(string[] args) {
            if (false) {
                Console.WriteLine("Create Dictionary:");
                // 単語辞書を作る
                var dict = new Dict();

                // 学習用分かち書きデータから辞書を作成
                foreach (var file in System.IO.Directory.EnumerateFiles(@"..\..\data\Corpus", "*.txt")) {
                    Console.WriteLine($"  Read File {file}");
                    foreach (var line in System.IO.File.ReadLines(file)) {
                        var items = line.Split('\t');
                        if (items.Length >= 3) {
                            dict.Add(toHiragana(items[1] == "" ? items[0] : items[1]), items[0], items[2]);
                        }
                    }
                }

                // unidicの辞書を読み取り
                foreach (var file in System.IO.Directory.EnumerateFiles(@"D:\work", "*.csv")) {
                    Console.WriteLine($"  Read File {file}");
                    foreach (var line in System.IO.File.ReadLines(file)) {
                        var items = line.Split(',');
                        if (items.Length >= 25) {
                            dict.Add(toHiragana(items[24] == "*" ? items[0] : items[24]), items[0], items[4]);
                        }
                    }
                }

                // 辞書を保存
                using (var sw = new System.IO.StreamWriter("dict.tsv")) {
                    dict.Save(sw);
                }
            }

            // 系列学習を実行
            if (false) {
                Dict dict;
                using (var sw = new System.IO.StreamReader("dict.tsv")) {
                    dict = Dict.Load(sw);
                }
                Func<string, int, IEnumerable<Entry>> commonPrefixSearch = (str, i) => {
                    var ret = new List<Entry>();
                    var n = Math.Min(str.Length, i + 16);
                    for (var j = i + 1; j <= n; j++) {
                        // 本来はCommonPrefixSearchを使う
                        var read = str.Substring(i, j - i);
                        ret.AddRange(dict.Find(read));
                    }
                    return ret;
                };

                var featureFuncs = CreateFeatureFuncs();
                var svm = new StructuredSupportVectorMachine(featureFuncs, false);

                for (var i = 0; i < 5; i++) {

                    var words = new List<Entry>();
                    foreach (var file in System.IO.Directory.EnumerateFiles(@"..\..\data\Corpus", "*.txt")) {
                        Console.WriteLine($"Read File {file}");
                        foreach (var line in System.IO.File.ReadLines(file)) {
                            var items = line.Split('\t');
                            if (String.IsNullOrWhiteSpace(line)) {
                                svm.Learn(words, commonPrefixSearch, (x) => { dict.Add(x.Read, x.Word, x.Features); });
                                words.Clear();
                            } else {
                                words.Add(new Entry(toHiragana(items[1] == "" ? items[0] : items[1]), items[0], items[2]));
                            }
                        }
                        if (words.Count != 0) {
                            svm.Learn(words, commonPrefixSearch, (x) => { dict.Add(x.Read, x.Word, x.Features); });
                            words.Clear();
                        }

                    }

                    // 検定開始
                    var gradews = new Gradews();
                    foreach (var file in System.IO.Directory.EnumerateFiles(@"..\..\data\Corpus", "*.txt")) {
                        Console.WriteLine($"Read File {file}");
                        foreach (var line in System.IO.File.ReadLines(file)) {
                            var items = line.Split('\t');
                            if (String.IsNullOrWhiteSpace(line)) {
                                var ret = svm.Convert(String.Concat(words.Select(x => x.Read)), commonPrefixSearch);
                                gradews.Comparer(String.Join(" ", words.Select(x => x.Word)), String.Join(" ", ret.Select(x => x.Word)));
                                words.Clear();
                            } else {
                                words.Add(new Entry(toHiragana(items[1] == "" ? items[0] : items[1]), items[0], items[2]));
                            }
                        }

                        if (words.Count != 0) {
                            var ret = svm.Convert(String.Concat(words.Select(x => x.Read)), commonPrefixSearch);
                            gradews.Comparer(String.Join(" ", words.Select(x => x.Word)), String.Join(" ", ret.Select(x => x.Word)));
                            words.Clear();
                        }
                    }

                    Console.WriteLine($"SentAccura: {gradews.SentAccura}");
                    Console.WriteLine($"WordPrec: {gradews.WordPrec}");
                    Console.WriteLine($"WordRec: {gradews.WordRec}");
                    Console.WriteLine($"Fmeas: {gradews.Fmeas}");
                    Console.WriteLine($"BoundAccuracy: {gradews.BoundAccuracy}");
                    Console.WriteLine();
                }

                svm.Save("learn.model");
            }

            // K交差検証を実行
            if (true) {
                Dict dict;
                using (var sw = new System.IO.StreamReader("dict.tsv")) {
                    dict = Dict.Load(sw);
                }
                Func<string, int, IEnumerable<Entry>> commonPrefixSearch = (str, i) => {
                    var ret = new List<Entry>();
                    var n = Math.Min(str.Length, i + 16);
                    for (var j = i + 1; j <= n; j++) {
                        // 本来はCommonPrefixSearchを使う
                        var read = str.Substring(i, j - i);
                        ret.AddRange(dict.Find(read));
                    }
                    return ret;
                };

                var featureFuncs = CreateFeatureFuncs();
                var svm = new StructuredSupportVectorMachine(featureFuncs, false);

                var files = System.IO.Directory.EnumerateFiles(@"..\..\data\Corpus", "*.txt").OrderBy(_ => Guid.NewGuid()).ToList();
                var fileCount = files.Count;

                var gradews = new Gradews();
                for (var i=0; i<10; i++) {
                    Console.WriteLine($"Cross Validation Phase {i}");
                    var start =    i * fileCount / 10;
                    var end   = (i+1) * fileCount / 10;
                    var testData  = files.Skip(start).Take(end-start).ToList();
                    var trainData = files.Take(start).Concat(files.Skip(end)).ToList();

                    for (var e = 0; e < 5; e++) {
                        var words = new List<Entry>();
                        var j = 0;
                        Console.WriteLine($"  Training: epoc={e}");
                        foreach (var file in files) {
                            foreach (var line in System.IO.File.ReadLines(file)) {
                                var items = line.Split('\t');
                                if (String.IsNullOrWhiteSpace(line)) {
                                    svm.Learn(words, commonPrefixSearch, (x) => { dict.Add(x.Read, x.Word, x.Features); });
                                    Console.Write($"    Data={j++}\r");
                                    words.Clear();
                                } else {
                                    words.Add(new Entry(toHiragana(items[1] == "" ? items[0] : items[1]), items[0], items[2]));
                                }
                            }
                            if (words.Count != 0) {
                                svm.Learn(words, commonPrefixSearch, (x) => { dict.Add(x.Read, x.Word, x.Features); });
                                Console.Write($"    Data={j++}\r");
                                words.Clear();
                            }

                        }
                    }
                    Console.WriteLine("");
                    // 開始
                    {
                        Console.WriteLine($"  Validation: ");
                        var j = 0;
                        var words = new List<Entry>();
                        foreach (var file in testData) {
                            //Console.WriteLine($"Read File {file}");
                            foreach (var line in System.IO.File.ReadLines(file)) {
                                var items = line.Split('\t');
                                if (String.IsNullOrWhiteSpace(line)) {
                                    var ret = svm.Convert(String.Concat(words.Select(x => x.Read)), commonPrefixSearch);
                                    gradews.Comparer(String.Join(" ", words.Select(x => x.Word)), String.Join(" ", ret.Select(x => x.Word)));
                                    Console.Write($"    Data={j++}\r");
                                    words.Clear();
                                } else {
                                    words.Add(new Entry(toHiragana(items[1] == "" ? items[0] : items[1]), items[0], items[2]));
                                }
                            }

                            if (words.Count != 0) {
                                var ret = svm.Convert(String.Concat(words.Select(x => x.Read)), commonPrefixSearch);
                                gradews.Comparer(String.Join(" ", words.Select(x => x.Word)), String.Join(" ", ret.Select(x => x.Word)));
                                Console.Write($"    Data={j++}\r");
                                words.Clear();
                            }
                        }
                    }
                    Console.WriteLine();
                    Console.WriteLine($"  SentAccura: {gradews.SentAccura}");
                    Console.WriteLine($"  WordPrec: {gradews.WordPrec}");
                    Console.WriteLine($"  WordRec: {gradews.WordRec}");
                    Console.WriteLine($"  Fmeas: {gradews.Fmeas}");
                    Console.WriteLine($"  BoundAccuracy: {gradews.BoundAccuracy}");
                    Console.WriteLine();
                }

                svm.Save("learn.model");
            }

            // 教師データの識別結果を生成
            if (true) {
                Dict dict;
                using (var sw = new System.IO.StreamReader("dict.tsv")) {
                    dict = Dict.Load(sw);
                }
                var featureFuncs = CreateFeatureFuncs();
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

                using (var ws = new System.IO.StreamWriter("result.txt")) {
                    foreach (var file in System.IO.Directory.EnumerateFiles(@"..\..\data\Corpus", "*.txt")) {
                        Console.WriteLine($"Read File {file}");
                        ws.WriteLine(file);
                        var words = new List<Entry>();
                        foreach (var line in System.IO.File.ReadLines(file)) {
                            var items = line.Split('\t');
                            if (String.IsNullOrWhiteSpace(line)) {
                                var gradews = new Gradews();
                                var ret = svm.Convert(String.Concat(words.Select(x => x.Read)), commonPrefixSearch);
                                gradews.Comparer(String.Join(" ", words.Select(x => x.Word)), String.Join(" ", ret.Select(x => x.Word)));
                                ws.WriteLine($"\tPrec:{gradews.WordPrec}\tRec:{gradews.WordRec}\tSentAccura:{gradews.SentAccura}\tBoundAccuracy:{gradews.BoundAccuracy}\tFMeas:{gradews.Fmeas}");
                                ws.WriteLine($"\tT: {String.Join(" ", words.Select(x => x.Word))}");
                                ws.WriteLine($"\tA: {String.Join(" ", ret.Select(x => x.Word))}");
                                words.Clear();
                            } else {
                                words.Add(new Entry(toHiragana(items[1] == "" ? items[0] : items[1]), items[0], items[2]));
                            }
                        }

                        if (words.Count != 0) {
                            var ret = svm.Convert(String.Concat(words.Select(x => x.Read)), commonPrefixSearch);
                            ws.WriteLine($"\tT: #{String.Join(" ", words.Select(x => x.Word))}");
                            ws.WriteLine($"\tA: #{String.Join(" ", ret.Select(x => x.Word))}");
                            words.Clear();
                        }
                    }
                }
            }

            // 辞書をDoubleArray化
            if (true) {
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
                using (var s = new System.IO.FileStream("dict.trie", System.IO.FileMode.Create)) {
                    trie.ToStaticTrie(s, (k) => Encoding.UTF8.GetBytes(new[] { k }), (v) => Encoding.UTF8.GetBytes(v));
                }
            }

            // メモリ辞書で識別
            if (false) {
                Dict dict;
                using (var sw = new System.IO.StreamReader("dict.tsv")) {
                    dict = Dict.Load(sw);
                }
                var featureFuncs = CreateFeatureFuncs();
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

            // Trie辞書で識別
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

                var featureFuncs = CreateFeatureFuncs();
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

            // StaticTrie辞書で識別
            if (true) {
                var s = new System.IO.FileStream("dict.trie", System.IO.FileMode.Open);
                var trie = StaticTrie<char, string>.Load(s, (k) => Encoding.UTF8.GetChars(k).FirstOrDefault(), (v) => Encoding.UTF8.GetString(v));

                var featureFuncs = CreateFeatureFuncs();
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

        static string toHiragana(string str) {
            // String.Concat(str.Select(x => (0x30A1 <= x && x <= 0x30F3) ? (char)(x - (0x30A1 - 0x3041)) : (char)x)) よりやや高速
            var s = str.ToCharArray();
            for (var i = 0; i < s.Length; i++) {
                var x = s[i] - 0x30A1U;
                if (x < (0x30A1U - 0x3041U)) {
                    s[i] = (char)(x - (0x30A1U - 0x3041U));
                }
            }
            return new string(s);
        }

        static FeatureFuncs CreateFeatureFuncs() {
            var featureFuncs = new FeatureFuncs();

            featureFuncs.NodeFeatures.Add((nodes, index) => "S0" + nodes[index].Word);
            featureFuncs.NodeFeatures.Add((nodes, index) => "P" + nodes[index].GetFeature(0));
            featureFuncs.NodeFeatures.Add((nodes, index) => "S0" + nodes[index].Word + "\tR0" + nodes[index].Read);
            featureFuncs.NodeFeatures.Add((nodes, index) => "S" + nodes[index].Word + "\tP" + nodes[index].GetFeature(0));
            featureFuncs.NodeFeatures.Add((nodes, index) => "S1" + ((index > 0) ? nodes[index - 1].Word : "") + "\tS0" + nodes[index].Word + "\t+R1" + ((index + 1 < nodes.Count) ? nodes[index + 1].Read : ""));
            featureFuncs.EdgeFeatures.Add((prevNode, node) => "ES" + prevNode.Word + "\tED" + node.Word);
            featureFuncs.EdgeFeatures.Add((prevNode, node) => "PS" + prevNode.GetFeature(0) + "\tPD" + node.GetFeature(0) );

            return featureFuncs;
        }
    }
}
