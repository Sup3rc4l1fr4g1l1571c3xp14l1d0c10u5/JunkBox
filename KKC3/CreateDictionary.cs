using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KKC3 {
    public class CreateDictionary {
        public static void Run(string[] args) {
            string dstDictName = "dict.tsv";
            string srcDictName = null;
            List < Tuple<string, string>> inputFiles = new List<Tuple<string,string>>();

            OptionParser op = new OptionParser();
            op.Regist(
                "-o", 1, 
                (xs) => { dstDictName = xs[0]; }
            );
            op.Regist(
                "-d",  1,
                (xs) => { srcDictName = xs[0]; },
                (xs) => { return System.IO.File.Exists(xs[0]); }
            );
            op.Regist(
                "-i", 2, 
                (xs) => { inputFiles.Add(Tuple.Create(xs[0], xs[1])); },
                (xs) => {
                    switch (xs[0]) {
                        case "tsv":
                        case "unidic":
                            return true;
                        default:
                            return false;
                    }
                }
            );

            if (inputFiles.Count == 0) {
                Console.Error.WriteLine("Input file is not setted.");
                return;
            }

            // 辞書を生成
            Dict dict = null;
            if (srcDictName == null) {
                Console.WriteLine("Create New Dictionary.");
                dict = new Dict();
            } else {
                Console.WriteLine($"Load dictionary from {srcDictName}.");
                using (var sw = new System.IO.StreamReader(srcDictName)) {
                    dict = Dict.Load(sw);
                }
            }

            //　学習実行
            foreach (var inputFile in inputFiles) {
                switch (inputFile.Item1) {
                    case "tsv": {
                        var dirPart = System.IO.Path.GetDirectoryName(inputFile.Item2);
                        var filePart = System.IO.Path.GetFileName(inputFile.Item2);
                        foreach (var file in System.IO.Directory.EnumerateFiles(dirPart, filePart)) {
                            Console.WriteLine($"  Read TSV file: {file}");
                            foreach (var line in System.IO.File.ReadLines(file)) {
                                var items = line.Split('\t');
                                if (items.Length >= 3) {
                                    dict.Add(CharConv.toHiragana(items[1] == "" ? items[0] : items[1]), items[0], items[2]);
                                }
                            }
                        }
                        break;
                    }
                    case "unidic": {
                        var dirPart = System.IO.Path.GetDirectoryName(inputFile.Item2);
                        var filePart = System.IO.Path.GetFileName(inputFile.Item2);
                        foreach (var file in System.IO.Directory.EnumerateFiles(dirPart, filePart)) {
                            Console.WriteLine($"  Read Unidic file: {file}");
                            foreach (var line in System.IO.File.ReadLines(file)) {
                                var items = line.Split(',');
                                if (items.Length >= 25) {
                                    dict.Add(CharConv.toHiragana(items[24] == "*" ? items[0] : items[24]), items[0], items[4]);
                                }
                            }
                        }
                        break;
                    }
                    default:
                        throw new Exception();
                }
            }
            //// 学習用分かち書きデータから辞書を作成
            //foreach (var file in System.IO.Directory.EnumerateFiles(@"..\..\data\Corpus", "*.txt")) {
            //    Console.WriteLine($"  Read File {file}");
            //    foreach (var line in System.IO.File.ReadLines(file)) {
            //        var items = line.Split('\t');
            //        if (items.Length >= 3) {
            //            dict.Add(CharConv.toHiragana(items[1] == "" ? items[0] : items[1]), items[0], items[2]);
            //        }
            //    }
            //}

            //// unidicの辞書を読み取り
            //foreach (var file in System.IO.Directory.EnumerateFiles(@"C:\mecab\lib\mecab\dic\unidic", "*.csv")) {
            //    Console.WriteLine($"  Read File {file}");
            //    foreach (var line in System.IO.File.ReadLines(file)) {
            //        var items = line.Split(',');
            //        if (items.Length >= 25) {
            //            dict.Add(CharConv.toHiragana(items[24] == "*" ? items[0] : items[24]), items[0], items[4]);
            //        }
            //    }
            //}

            // 辞書を保存
            using (var sw = new System.IO.StreamWriter(dstDictName)) {
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
