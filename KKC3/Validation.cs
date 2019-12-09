﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KKC3 {
    public static class Validation {
        public static void Run(string[] args) {
            Dict dict;
            using (var sw = new System.IO.StreamReader("dict.tsv")) {
                dict = Dict.Load(sw);
            }
            var featureFuncs = KKCFeatureFunc.Create();
            Func<string, int, int, IEnumerable<Entry>> commonPrefixSearch = (str, i, len) => {
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

            var words = new List<Entry>();
            var gradews = new Gradews();
            foreach (var file in System.IO.Directory.EnumerateFiles(@"..\..\data\Corpus", "*.txt")) {
                Console.WriteLine($"Read File {file}");
                foreach (var line in System.IO.File.ReadLines(file)) {
                    var items = line.Split('\t');
                    if (String.IsNullOrWhiteSpace(line)) {
                        var ret = svm.Convert(String.Concat(words.Select(x => x.Read)), commonPrefixSearch);
                        gradews.Comparer(String.Join(" ", words.Select(x => x.Word)), String.Join(" ", ret.Select(x => x.Item2.Word)));
                        words.Clear();
                    } else {
                        words.Add(new Entry(CharConv.toHiragana(items[1] == "" ? items[0] : items[1]), items[0], items[2]));
                    }
                }

                if (words.Count != 0) {
                    var ret = svm.Convert(String.Concat(words.Select(x => x.Read)), commonPrefixSearch);
                    gradews.Comparer(String.Join(" ", words.Select(x => x.Word)), String.Join(" ", ret.Select(x => x.Item2.Word)));
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
    }
}