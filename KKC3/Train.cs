using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KKC3 {
    public static class Train {
        public static void Run(string [] args) {
            string dictName = "dict.tsv";
            string modelName = "learn.model";
            string teaturePath = @"..\..\data\Corpus\*.txt";
            List<string> teatureFiles = new List<string>();

            OptionParser op = new OptionParser();
            op.Regist(
                "-dic", 1,
                (xs) => { dictName = xs[0]; },
                (xs) => { return System.IO.File.Exists(xs[0]); }
            );
            op.Regist(
                "-i", 1,
                (xs) => { teatureFiles.Add(xs[0]); }
            );
            op.Regist(
                "-o", 1,
                (xs) => { modelName = xs[0]; }
            );

            args = op.Parse(args);

            if (teatureFiles.Count == 0) {
                Console.Error.WriteLine("Input file is not setted.");
                return;
            }

            Dict dict;
            using (var sw = new System.IO.StreamReader(dictName)) {
                dict = Dict.Load(sw);
            }
            Func<string, int, int, IEnumerable<Entry>> commonPrefixSearch = (str, i, len) => {
                var ret = new List<Entry>();
                if (len == -1) { len = 16; }
                var n = Math.Min(str.Length, i + len);
                for (var j = i + 1; j <= n; j++) {
                    // 本来はCommonPrefixSearchを使う
                    var read = str.Substring(i, j - i);
                    ret.AddRange(dict.Find(read));
                }
                return ret;
            };

            var dirPart = System.IO.Path.GetDirectoryName(teaturePath);
            var filePart = System.IO.Path.GetFileName(teaturePath);

            var featureFuncs = KKCFeatureFunc.Create();
            var svm = new StructuredSupportVectorMachine(featureFuncs, false);

            for (var i = 0; i < 1; i++) {
                var words = new List<Entry>();
                Console.WriteLine($"Train Epoc={i+1}");
                var n = 0;
                foreach (var file in System.IO.Directory.EnumerateFiles(dirPart, filePart)) {
                    foreach (var line in System.IO.File.ReadLines(file)) {
                        var items = line.Split('\t');
                        if (String.IsNullOrWhiteSpace(line)) {
                            Console.Write($" step={++n}\r");
                            svm.Learn(words, commonPrefixSearch, (x) => { dict.Add(x.Read, x.Word, x.Features); });
                            words.Clear();
                        } else {
                            words.Add(new Entry(CharConv.toHiragana(items[1] == "" ? items[0] : items[1]), items[0], items[2]));
                        }
                    }
                    if (words.Count != 0) {
                        svm.Learn(words, commonPrefixSearch, (x) => { dict.Add(x.Read, x.Word, x.Features); });
                        words.Clear();
                    }

                }
                svm.RegularizeAll();
            }

            svm.Save(modelName);
            Console.Write($"  Finish.\r");
        }
    }
}
