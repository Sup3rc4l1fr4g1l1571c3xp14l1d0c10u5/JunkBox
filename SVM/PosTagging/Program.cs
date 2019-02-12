using System;
using System.Data;
using System.Linq;
using libNLP;
using libNLP.Extentions;

namespace PosTagging {
    class Program {
        static void Main(string[] args) {
            {
                var items = Mecab.Run("wikipedia02.txt").Select(Mecab.ParseLine).Split(x => x.Item1 == "EOS");
                var tex1 = new StaticTermExtractor();
                var tex2 = new AdaptiveTermExtractor();
                tex2.Learn(
                    500,
                    (new string[] {
                        "オートマチックトランスミッション","automatic transmission", "AT", "MT",
                        "自動変速機", "車速", "回転速度", "変速比","クラッチペダル"
                    })
                    .Apply(x => Mecab.Run("", String.Join(Environment.NewLine, x)))
                    .Select(x => Mecab.ParseLine(x))
                    .Split(x => x.Item1 == "EOS")
                );
                foreach (var item in items) {
                    Console.WriteLine("静的:");
                    tex1.Extract(item).Select(x => String.Join(" ", x)).Apply(x => String.Join("|", x)).Tap(Console.WriteLine);
                    Console.WriteLine("適応型:");
                    tex2.Extract(item).Select(x => String.Join(" ", x)).Apply(x => String.Join("|", x)).Tap(Console.WriteLine);
                }
            }

            {
                // wikipedia記事データから分かち書き用の教師データを作る。
                Console.WriteLine("Create Teaching data.");

                // 教師データを用いて線形SVMで分かち書きを学習
                Console.WriteLine("Learning SVM: Start");
                var wseg = new WordSegmenter();
                var result = wseg.Train(500, Mecab.Run("wikipedia01.txt").Apply(WordSegmenter.CreateTeachingData));
                // 学習結果を表示
                Console.WriteLine(result);

                // 教師データを用いて構造化パーセプトロンで品詞を学習
                Console.WriteLine("Learning Structured perceptron: Start");
                StructuredPerceptron sp = null;
                {
                    var teatures = new[] { "wikipedia01.txt" }.SelectMany(x => WordSegmenter.CreateTrainData(Mecab.Run(x))).Select(x => x.Select(y => Tuple.Create(y.Item1, y.Item2)).ToArray()).ToList();
                    sp = StructuredPerceptron.Train(
                        teatures.SelectMany(x => x.Select(y => y.Item2)).ToList(),
                        teatures,
                        10
                    );

                    Console.WriteLine($"Learning Structured perceptron: Finish.");

                    // 学習結果を評価
                    //Console.WriteLine(TestResult.Test(svm1, teatures));
                }

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
                        var ret = wseg.Benchmark(WordSegmenter.CreateTeachingData(Mecab.Run("wikipedia02.txt")));
                        Console.WriteLine(ret);
                    }
                }
                System.Console.ReadKey();
            }
            //{
            //    var rand = new Random();
            //    var data = SVMLight.ReadDataFromFile<int>("news20.binary", int.Parse).OrderBy(x => rand.Next()).ToList();
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

        private void Usage() {
            Console.WriteLine("NLP");
        }

    }
}

