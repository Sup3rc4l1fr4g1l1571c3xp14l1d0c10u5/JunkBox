using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KKC3 {
    public static class Train {
        public static void Run(string [] args) {
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

            var featureFuncs = KKCFeatureFunc.Create();
            var svm = new StructuredSupportVectorMachine(featureFuncs, false);

            for (var i = 0; i < 5; i++) {
                var words = new List<Entry>();
                Console.WriteLine($"Train Epoc={i+1}");
                var n = 0;
                foreach (var file in System.IO.Directory.EnumerateFiles(@"..\..\data\Corpus", "*.txt")) {
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
            }

            svm.Save("learn.model");
            Console.Write($"  Finish.\r");
        }
    }
}
