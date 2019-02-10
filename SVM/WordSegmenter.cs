using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace svm_fobos {
    // 特徴ベクトル（疎ベクトル）
    using FeatureVector = Dictionary<string, double>;

    /// <summary>
    /// 分かち書きクラス
    /// </summary>
    internal class WordSegmenter {

        /// <summary>
        /// 識別器
        /// </summary>
        private LinerSVM<string> svm { get; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public WordSegmenter() {
            svm = new LinerSVM<string>();
        }

        /// <summary>
        /// 教師データを学習
        /// </summary>
        /// <param name="epoc">学習のイテレーション回数</param>
        /// <param name="file">教師データ</param>
        /// <returns></returns>
        public TestResult Train(int epoc, params IEnumerable<Tuple<string,string[]>[]>[] inputs) {
            List<Tuple<int, FeatureVector>> teatures = inputs.SelectMany(x => x).SelectMany(CreateTeachingData).ToList();
            for (var i = 0; i < epoc; i++) {
                foreach (var kv in teatures) {
                    svm.Train(kv.Item2, kv.Item1, 0.06, 0.005);
                }
                svm.Regularize(0.005);
            }
            return Benchmark(teatures);
        }

        /// <summary>
        /// 学習結果を用いて文字列を分かち書きする
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        public string[] Segmentation(string line) {
            var fv = CreateFeatureVector(line).ToList();
            var splitPoints = fv.Select((x, i) => Tuple.Create(svm.Predict(x), i))
                                    .Where(x => x.Item1 >= 0)
                                    .Select(x => x.Item2 + 1)
                                    .Apply(x => new[] { 0 }.Concat(x).Concat(new[] { line.Length }).EachCons(2).Select(y => Tuple.Create(y[0], y[1] - y[0]))).ToArray();
            return splitPoints.Select(x => line.Substring(x.Item1, x.Item2)).ToArray();
        }

        /// <summary>
        /// ベンチマークを実行
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public TestResult Benchmark(IEnumerable<Tuple<int, FeatureVector>> data) {
                var truePositive = 0;
                var falsePositive = 0;
                var falseNegative = 0;
                var trueNegative = 0;
                foreach (var fv in data) {
                    var prediction = fv.Item1 < 0;
                    var fact = svm.Predict(fv.Item2) < 0;
                    if (prediction) {
                        if (fact) {
                            truePositive++;
                        } else {
                            falsePositive++;
                        }
                    } else {
                        if (fact) {
                            falseNegative++;
                        } else {
                            trueNegative++;
                        }
                    }
                }
                return new TestResult(truePositive, falsePositive, falseNegative, trueNegative);
        }

        /// <summary>
        /// 教師データ生成
        /// </summary>
        public static IEnumerable<Tuple<int, FeatureVector>> CreateTeachingData(IEnumerable<Tuple<string,string[]>> line) {
            var index = new List<int>();
            var lineText = line.Select(x => x.Item1).Apply(x => String.Join(" ", x));
            var words = "\u0001 " + lineText + " \uFFFE";
            for (var i = 0; i < words.Length; i++) {
                if (words[i] != ' ') {
                    index.Add(i);
                }
            }
            for (var i = 1; i < index.Count - 1; i++) {
                var n = index[i - 1];
                var m = index[i + 0];
                var l = index[i + 1];
                yield return
                    Tuple.Create(
                        (m + 1 != l) ? +1 : -1,
                        new FeatureVector()
                            .Apply(x => AppendFeatures(x, -1, 3, words[n]))
                            .Apply(x => AppendFeatures(x, 0, 3, words[m]))
                            .Apply(x => AppendFeatures(x, 1, 3, words[l]))
                    );
            }
        }

        /// <summary>
        /// 特徴ベクトル生成
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static IEnumerable<FeatureVector> CreateFeatureVector(string str) {
            var line = "\u0001" + str + "\uFFFE";
            for (var i = 1; i < line.Length - 1; i++) {
                yield return new FeatureVector()
                            .Apply(x => AppendFeatures(x, -1, 3, line[i - 1]))
                            .Apply(x => AppendFeatures(x, 0, 3, line[i + 0]))
                            .Apply(x => AppendFeatures(x, 1, 3, line[i + 1]));
            }
        }

        private static bool isKanji(char v) {
            return ("々〇〻".IndexOf(v) != -1) || ('\u3400' <= v && v <= '\u9FFF') || ('\uF900' <= v && v <= '\uFAFF') || ('\uD840' <= v && v <= '\uD87F') || ('\uDC00' <= v && v <= '\uDFFF');
        }
        private static bool isHiragana(char v) {
            return ('\u3041' <= v && v <= '\u3096');
        }
        private static bool isKatakana(char v) {
            return ('\u30A1' <= v && v <= '\u30FA');
        }
        private static bool isAlpha(char v) {
            return (char.IsLower(v) || char.IsUpper(v));
        }
        private static bool isDigit(char v) {
            return char.IsDigit(v);
        }
        private static FeatureVector AppendFeatures(FeatureVector fv, int nGramIndex, int nGram, char v) {
            var sIndex = nGramIndex.ToString("+0;-0");
            fv.Add($"{nGram}G{sIndex} {v}", 1);
            fv.Add($"D{sIndex} ", isDigit(v) ? +1 : -1);
            fv.Add($"A{sIndex} ", isAlpha(v) ? +1 : -1);
            fv.Add($"S{sIndex} ", char.IsSymbol(v) ? +1 : -1);
            fv.Add($"H{sIndex} ", isHiragana(v) ? +1 : -1);
            fv.Add($"K{sIndex} ", isKatakana(v) ? +1 : -1);
            fv.Add($"J{sIndex} ", isKanji(v) ? +1 : -1);

            return fv;
        }
    }
}
