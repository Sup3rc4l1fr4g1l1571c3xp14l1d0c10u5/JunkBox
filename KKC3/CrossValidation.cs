using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KKC3 {
    public static class CrossValidation {
        public static void Run(string[] args) {
            Dict dict;
            using (var sw = new System.IO.StreamReader("dict.tsv")) {
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

            var featureFuncs = KKCFeatureFunc.Create();
            var svm = new StructuredSupportVectorMachine(featureFuncs, false);

            var files = System.IO.Directory.EnumerateFiles(@"..\..\data\Corpus", "*.txt").OrderBy(_ => Guid.NewGuid()).ToList();
            var fileCount = files.Count;

            var gradews = new Gradews();
            for (var i = 0; i < 10; i++) {
                Console.WriteLine($"Cross Validation Phase {i}");
                var start = i * fileCount / 10;
                var end = (i + 1) * fileCount / 10;
                var testData = files.Skip(start).Take(end - start).ToList();
                var trainData = files.Take(start).Concat(files.Skip(end)).ToList();

                for (var e = 0; e < 1; e++) {
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
                                words.Add(new Entry(CharConv.toHiragana(items[1] == "" ? items[0] : items[1]), items[0], items[2]));
                            }
                        }
                        if (words.Count != 0) {
                            svm.Learn(words, commonPrefixSearch, (x) => { dict.Add(x.Read, x.Word, x.Features); });
                            Console.Write($"    Data={j++}\r");
                            words.Clear();
                        }

                    }
                    svm.RegularizeAll();
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
                                gradews.Comparer(String.Join(" ", words.Select(x => x.Word)), String.Join(" ", ret.Select(x => x.Item2.Word)));
                                Console.Write($"    Data={j++}\r");
                                words.Clear();
                            } else {
                                words.Add(new Entry(CharConv.toHiragana(items[1] == "" ? items[0] : items[1]), items[0], items[2]));
                            }
                        }

                        if (words.Count != 0) {
                            var ret = svm.Convert(String.Concat(words.Select(x => x.Read)), commonPrefixSearch);
                            gradews.Comparer(String.Join(" ", words.Select(x => x.Word)), String.Join(" ", ret.Select(x => x.Item2.Word)));
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
        }
    }
}
