using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace svm_fobos {
    class Program {
        static void Main(string[] args) {
            {
                var it = Mecab.Run("wikipedia00.txt").Select(x => x.Split("\t".ToArray(), 2));
                var items = new List<Tuple<string, string[]>>();
                foreach (var token in it) {
                    if (token[0] != "EOS") {
                        items.Add(Tuple.Create(token[0], token[1].Split(",".ToArray())));
                    } else {
                        foreach (var word in TermExtractor.Find(items)) {
                            Console.WriteLine(word);
                        }
                        items.Clear();
                    }
                }
            }

            {
                Console.WriteLine("Create Teaching data.");

                var wseg = new WordSegmenter();
                #region 分かち書きの学習
                {
                    Console.WriteLine("教師データを用いた分かち書きの学習を開始");
                    var result1 = wseg.Train(
                        500,
                        Mecab.Run("wikipedia00.txt")
                             .Select(Mecab.ParseLine)
                             .Select(ConvertMecabToWordSegmenter)
                             .Split(x => x.Item1 == "EOS"),
                        Mecab.Run("wikipedia01.txt")
                             .Select(Mecab.ParseLine)
                             .Select(ConvertMecabToWordSegmenter)
                             .Split(x => x.Item1 == "EOS")
                    );

                    Console.WriteLine("教師データに対する評価結果:");
                    Console.WriteLine(result1);

                    Console.WriteLine("別データに対する評価結果:");
                    var result2 = wseg.Benchmark(
                        Mecab.Run("wikipedia02.txt")
                             .Select(Mecab.ParseLine)
                             .Select(ConvertMecabToWordSegmenter)
                             .Split(x => x.Item1 == "EOS")
                             .SelectMany(WordSegmenter.CreateTeachingData)
                    );
                    Console.WriteLine(result2);
                }
                #endregion

                StructuredPerceptron sp = null;
                #region 品詞識別の学習
                {
                    Console.WriteLine("教師データを用いた品詞識別の学習を開始");
                    var teatures = new[] { "wikipedia00.txt", "wikipedia01.txt" }
                        .SelectMany(x => 
                            Mecab.Run(x)
                                 .Select(Mecab.ParseLine)
                                 .Split(y => y.Item1 == "EOS"))
                        .Select(x => x.Where(y => y.Item2.Length > 0).Select(y => Tuple.Create(y.Item1, y.Item2[0])).ToArray())
                        .Where(x => x.Length > 0)
                        .ToList();
                    sp = StructuredPerceptron.Train(teatures, 10);

                    Console.WriteLine("教師データに対する評価結果:");
                    {
                        var table = new Dictionary<Tuple<string, string>, Dictionary<Tuple<string, string>, int>>();
                        foreach (var teature in teatures) {
                            var ret = sp.Predict(teature.Select(x => x.Item1).ToArray());
                            foreach (var pair in teature.Zip(ret, Tuple.Create)) {
                                if (table.ContainsKey(pair.Item1) == false) {
                                    table[pair.Item1] = new Dictionary<Tuple<string, string>, int>();
                                }
                                if (table[pair.Item1].ContainsKey(pair.Item2) == false) {
                                    table[pair.Item1][pair.Item2] = 0;
                                }
                                table[pair.Item1][pair.Item2] += 1;
                            }
                        }

                        int total = table.Sum(x => x.Value.Sum(y => y.Value));
                        int match = table.Sum(x => x.Value.Sum(y => ((x.Key.Item1 == y.Key.Item1 && x.Key.Item2 == y.Key.Item2) ? y.Value : 0)));
                        Console.WriteLine($"Accuracy: {match}/{total} ({match * 100.0 / total}%)");
                    }

                    Console.WriteLine("別データに対する評価結果:");
                    {
                        var teature2 = Mecab.Run("wikipedia02.txt")
                            .Select(Mecab.ParseLine)
                            .Split(y => y.Item1 == "EOS")
                            .Select(x => x.Where(y => y.Item2.Length > 0).Select(y => Tuple.Create(y.Item1, y.Item2[0])).ToArray())
                        .Where(x => x.Length > 0)
                            .ToList();
                        var table = new Dictionary<Tuple<string, string>, Dictionary<Tuple<string, string>, int>>();
                        foreach (var teature in teature2) {
                            var ret = sp.Predict(teature.Select(x => x.Item1).ToArray());
                            foreach (var pair in teature.Zip(ret, Tuple.Create)) {
                                if (table.ContainsKey(pair.Item1) == false) {
                                    table[pair.Item1] = new Dictionary<Tuple<string, string>, int>();
                                }
                                if (table[pair.Item1].ContainsKey(pair.Item2) == false) {
                                    table[pair.Item1][pair.Item2] = 0;
                                }
                                table[pair.Item1][pair.Item2] += 1;
                            }
                        }

                        int total = table.Sum(x => x.Value.Sum(y => y.Value));
                        int match = table.Sum(x => x.Value.Sum(y => ((x.Key.Item1 == y.Key.Item1 && x.Key.Item2 == y.Key.Item2) ? y.Value : 0)));
                        Console.WriteLine($"Accuracy: {match}/{total} ({match * 100.0 / total}%)");
                    }
                }
                #endregion

                // SVMで分かち書きを行い、構造化パーセプトロンで品詞推定
                {
                    foreach (var line in System.IO.File.ReadLines("wikipedia02.txt")) {
                        var gold = wseg.Segmentation(line);
                        var ret = sp.Predict(gold);
                        Console.WriteLine(ret.Select(x => $"{x.Item1}/{x.Item2}").Apply(x => String.Join(" ", x)));
                    }
                }

                // 

                {
                    // 学習結果を用いて分かち書きを実行
                    Console.WriteLine($"Evaluate:");

                    // 別のwikipedia記事データからテストデータを作って評価
                    {
                        var ret = wseg.Benchmark(Mecab.Run("wikipedia02.txt").Select(Mecab.ParseLine).Split(x => x.Item1 != "EOS").SelectMany(WordSegmenter.CreateTeachingData));
                        Console.WriteLine(ret);
                    }
                }
                System.Console.ReadKey();
            }
            //{
            //    var rand = new Random();
            //    var data = LinerSVM<int>.ReadDataFromFile("news20.binary", int.Parse).OrderBy(x => rand.Next()).ToList();
            //    var tests = data.Take(data.Count / 10).ToList();
            //    var teatures = data.Skip(data.Count / 10).ToList();
            //    var svm1 = new LinerSVM<int>();
            //    for (var i = 0; i < 10; i++) {
            //        foreach (var kv in teatures) {
            //            svm1.Train(kv.Item2, kv.Item1, 0.06, 0.005);
            //        }
            //        Console.WriteLine($"RemovedFeature: {svm1.Regularize(0.005)}");
            //        Console.WriteLine(TestResult.Test(svm1, tests));
            //    }
            //}
        }

        /// <summary>
        /// Mecabのデータ形式をWordSegmenter向け形式に変換する
        /// </summary>
        /// <param name="xs"></param>
        /// <returns></returns>
        public static Tuple<string, string[]> ConvertMecabToWordSegmenter(Tuple<string, string[]> x) {
            return Tuple.Create(x.Item1, new[] { x.Item2.ElementAtOrDefault(0, "不明語"), x.Item2.ElementAtOrDefault(1, "不") });
        }

    }
}
