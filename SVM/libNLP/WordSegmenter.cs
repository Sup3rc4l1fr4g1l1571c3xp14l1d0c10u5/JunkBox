using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using libNLP.Extentions;

namespace libNLP {

    /// <summary>
    /// SVMによる日本語分かち書き
    /// </summary>
    public class WordSegmenter {

        /// <summary>
        /// 分類器
        /// </summary>
        private LinerSVM<string> svm;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public WordSegmenter() {
            svm = new LinerSVM<string>();
        }

        /// <summary>
        /// 学習データセットを用いて学習を実行
        /// </summary>
        /// <param name="epoch">学習回数</param>
        /// <param name="inputs">学習データセット</param>
        /// <returns>学習データセットに対するモデルの評価結果</returns>
        public TestResult Train(int epoch, params IEnumerable<Tuple<int, Dictionary<string, double>>>[] inputs) {
            List<Tuple<int, Dictionary<string, double>>> teatures = inputs.SelectMany(x => x).ToList();
            for (var i = 0; i < epoch; i++) {
                foreach (var kv in teatures) {
                    svm.Train(kv.Item2, kv.Item1, 0.06);
                }
                svm.Regularize(0.005);
            }
            return Benchmark(teatures);
        }

        /// <summary>
        /// 学習したモデルを用いて分かち書きを実行
        /// </summary>
        /// <param name="line">対象文字列</param>
        /// <returns>分かち書き結果</returns>
        public string[] Segmentation(string line) {
            var fv = CreateFeatureVector(line).ToList();
            var splitPoints = fv.Select((x, i) => Tuple.Create(svm.Predict(x), i))
                                    .Where(x => x.Item1 >= 0)
                                    .Select(x => x.Item2 + 1)
                                    .Apply(x => new[] { 0 }.Concat(x).Concat(new[] { line.Length }).EachCons(2).Select(y => Tuple.Create(y[0], y[1] - y[0]))).ToArray();
            return splitPoints.Where(x => x.Item2 > 0).Select(x => line.Substring(x.Item1, x.Item2)).ToArray();
        }

        /// <summary>
        /// 与えたデータセットを用いてモデルの評価を行う。
        /// </summary>
        /// <param name="datasets">教師付きデータセット</param>
        /// <returns>モデルの評価結果</returns>
        public TestResult Benchmark(IEnumerable<Tuple<int, Dictionary<string, double>>> datasets) {
            return svm.Test(datasets);
        }

        /// <summary>
        /// 教師データ生成
        /// </summary>
        public static IEnumerable<Tuple<int, Dictionary<string, double>>> CreateTeachingData(IEnumerable<string> inputs) {
            foreach (var line in CreateTrainData(inputs)) {
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
                            new Dictionary<string, double>()
                                .Apply(x => AppendFeatures(x, -1, 3, words[n]))
                                .Apply(x => AppendFeatures(x, 0, 3, words[m]))
                                .Apply(x => AppendFeatures(x, 1, 3, words[l]))
                        );
                }

            }
        }

        /// <summary>
        /// 特徴ベクトル生成
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static IEnumerable<Dictionary<string, double>> CreateFeatureVector(string str) {
            var line = "\u0001" + str + "\uFFFE";
            for (var i = 1; i < line.Length - 1; i++) {
                yield return new Dictionary<string, double>()
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

        private static Dictionary<string, double> AppendFeatures(Dictionary<string, double> fv, int nGramIndex, int nGram, char v) {
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

        public static IEnumerable<Tuple<string, string, string>[]> CreateTrainData(IEnumerable<string> inputs) {
            var temp = new List<Tuple<string, string, string>>();
            foreach (var x in inputs) {
                var kv = x.Split("\t".ToArray(), 2);
                if (kv[0] == "EOS") {
                    if (temp.Any()) {
                        yield return temp.ToArray();
                        temp.Clear();
                    }
                } else {
                    var m = kv[0];
                    var f = kv[1].Split(",".ToArray());
                    var f0 = f.ElementAtOrDefault(0, "不明語");
                    var f12 = f.ElementAtOrDefault(12, "不");
                    temp.Add(Tuple.Create(m, f0, f12));
                }
            }
        }
        /// <summary>
        /// 学習モデルを読み込む
        /// </summary>
        /// <param name="modelPath">学習モデルファイル</param>
        /// <returns></returns>
        public static WordSegmenter Load(string modelPath) {
            using (var streamReader = new System.IO.StreamReader(modelPath)) {
                var self = new WordSegmenter();
                self.svm = LinerSVM<string>.LoadFromStream(streamReader, x => x);
                return self;
            }
        }

        /// <summary>
        /// 学習結果を保存する
        /// </summary>
        /// <param name="modelPath">学習モデルファイル</param>
        public void Save(string modelPath) {
            using (var streamWriter = new System.IO.StreamWriter(modelPath)) {
                svm.SaveToStream(streamWriter, x => x);
            }
        }

    }
}
