using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using libNLP.Extentions;

namespace libNLP {
    /// <summary>
    /// 構造化パーセプトロン
    /// </summary>
    public class StructuredPerceptron {

        /// <summary>
        /// データインタフェース
        /// </summary>
        public interface IData {
            /// <summary>
            /// 特徴列
            /// </summary>
            string[] Features { get; }
        }

        /// <summary>
        /// 入力データ
        /// </summary>
        public class InputData : IData {
            /// <summary>
            /// 特徴列
            /// </summary>
            public string[] Features { get; set; }
        }

        /// <summary>
        /// 教師データ
        /// </summary>
        public class TeatureData : IData {
            /// <summary>
            /// ラベル
            /// </summary>
            public string Label { get; set; }
            /// <summary>
            /// 特徴列
            /// </summary>
            public string[] Features { get; set; }
        }

        public interface ICalcFeatures {
            List<string> ExtractFeatures(TeatureData[] trigram, int i);
            List<string> ExtractFeatures(IData[] trigramWord, int index, string prevLabel, string currLabel);
        }

            /// <summary>
            /// 識別ラベル集合
            /// </summary>
            private HashSet<string> Labels { get; }

        /// <summary>
        ///  特徴計算
        /// </summary>
        private Dictionary<string, double> Weight { get; }

        /// <summary>
        ///  識別器の重み
        /// </summary>
        private ICalcFeatures CalcFeatures { get; }

        /// <summary>
        /// 識別器のコンストラクタ
        /// </summary>
        /// <param name="labels"></param>
        /// <param name="weight"></param>
        public StructuredPerceptron(HashSet<string> labels, Dictionary<string, double> weight, ICalcFeatures CalcFeatures) {
            this.Labels = labels;
            this.Weight = weight;
            this.CalcFeatures = CalcFeatures;
        }

        /// <summary>
        /// 特徴ベクトルと重みの内積を取る（ベクトル中の特徴点に対応する値のみ1.0それ以外は0.0とみなして内積を取る。つまり、特徴ベクトルに含まれる特徴点に対応する重みの総和を求めることに等しい）
        /// </summary>
        /// <param name="features"></param>
        /// <param name="weight"></param>
        /// <returns></returns>
        private static double InnerProduct(List<string> features, Dictionary<string, double> weight) {
            double ret = 0.0;
            foreach (var feature in features) {
                double w;
                if (weight.TryGetValue(feature, out w)) { ret += w; }
            }
            return ret;
        }

        /// <summary>
        /// 学習モデルをストリームに保存する
        /// </summary>
        /// <param name="streamWriter">保存先ストリーム</param>
        /// <param name="featureDeserializer">特徴情報のシリアライザ</param>
        public void SaveToStream(System.IO.StreamWriter streamWriter) {
            streamWriter.WriteLine(string.Join("\t", Labels));
            foreach (var weight in Weight) {
                streamWriter.Write(weight.Key);
                streamWriter.Write("\t");
                streamWriter.Write(weight.Value);
                streamWriter.WriteLine();
            }
        }

        /// <summary>
        /// ストリームから学習モデルを読み取る
        /// </summary>
        /// <param name="streamReader">読み込み元ストリーム</param>
        /// <param name="featureDeserializer">特徴情報のデシリアライザ</param>
        /// <returns></returns>
        public static StructuredPerceptron LoadFromStream(System.IO.StreamReader streamReader, ICalcFeatures calcFeatures) {
            string line;
            if ((line = streamReader.ReadLine()) == null) {
                throw new Exception("");
            }
            var labels = new HashSet<string>(line.Split("\t".ToArray()).Distinct());
            var weight = new Dictionary<string, double>();
            while ((line = streamReader.ReadLine()) != null) {
                var tokens = line.Trim().Split("\t".ToArray(), 2);
                weight[tokens[0]] = double.Parse(tokens[1]);
            }
            return new StructuredPerceptron(labels, weight, calcFeatures);
        }

        /// <summary>
        /// 重みベクトル weight の下で、入力データ列 sentence に対して最も尤もらしいラベル labels の並びを求める
        /// </summary>
        /// <param name="sentence"></param>
        /// <param name="weight"></param>
        /// <param name="labels"></param>
        /// <returns></returns>
        private static string[] ArgMax(IData[] sentence, Dictionary<string, double> weight, HashSet<string> labels, ICalcFeatures calcFeatures) {
            var bestEdge = ForwardStep(sentence, weight, labels, calcFeatures);
            return BackwardStep(sentence, bestEdge);
        }

        /// <summary>
        /// 前向きアルゴリズムで観測されたシーケンス全体の確率を元にビタビアルゴリズムで最も尤もらしい並びを求める
        /// </summary>
        /// <param name="sentence"></param>
        /// <param name="bestEdge"></param>
        /// <returns></returns>
        private static string[] BackwardStep(IData[] sentence, Dictionary<Tuple<int, string>, Tuple<int, string>> bestEdge) {
            var result = new string[sentence.Length];
            var maxIndex = sentence.Length + 1;
            var nextEdge = bestEdge[Tuple.Create(maxIndex, "EOS")];
            while (!(nextEdge.Item1 == 0 && nextEdge.Item2 == "BOS")) {
                if (nextEdge == null) {
                    throw new Exception("Cannot backtrack");
                }

                result[nextEdge.Item1 - 1] = nextEdge.Item2;
                nextEdge = bestEdge[nextEdge];
            }
            return result;
        }

        /// <summary>
        /// 重みベクトル weight の下で、入力データ列 sentence_ に前向きアルゴリズムを適用し、入力データ列の全体としてのラベル labels の確率を求める
        /// </summary>
        /// <param name="sentence">入力データ列 </param>
        /// <param name="weight">重みベクトル</param>
        /// <param name="labels">ラベル</param>
        /// <returns></returns>
        private static Dictionary<Tuple<int, string>, Tuple<int, string>> ForwardStep(IData[] sentence, Dictionary<string, double> weight, HashSet<string> labels, ICalcFeatures calcFeatures) {
            var bestScore = new Dictionary<Tuple<int, string>, double>();
            var bestEdge = new Dictionary<Tuple<int, string>, Tuple<int, string>>();
            var label_bos = "BOS";
            var label_eos = "EOS";

            bestScore[Tuple.Create(0, label_bos)] = 0.0;
            bestEdge[Tuple.Create(0, label_bos)] = null;

            var combination = ((new[] { (IEnumerable<string>)new[] { label_bos } }).Concat(Enumerable.Repeat(labels, sentence.Length)).Concat(new[] { (IEnumerable<string>)new[] { label_eos } })).EachCons(2).Select((x, i) => Tuple.Create(i, x[0], x[1]));
            foreach (var tuple in combination) {
                // ビタビ経路の前方探索（１トークン分）
                var index = tuple.Item1;
                var srcLabels = tuple.Item2;
                var dstLabels = tuple.Item3;
                foreach (var srcLabel in srcLabels) {
                    var srcState = Tuple.Create(index, srcLabel);
                    foreach (var dstLabel in dstLabels) {
                        // 入力データ列の index 番目（遷移先）の特徴値
                        var features = calcFeatures.ExtractFeatures(sentence, index, srcLabel, dstLabel);

                        // 特徴値から求めたスコア
                        var score = InnerProduct(features, weight);

                        // ビタビ経路で前の状態までの経路のスコア
                        double srcScore;
                        if (bestScore.TryGetValue(srcState, out srcScore) == false) {
                            srcScore = 0.0;
                        }

                        // ビタビ経路で、一つ手前の状態から現在の状態に遷移した場合の経路のスコア
                        var dstScore = srcScore + score;

                        // 経路のスコアが今までのスコアよりもよくなっている場合は記録
                        var dstIndex = index + 1;
                        var dstState = Tuple.Create(dstIndex, dstLabel);
                        double currentScore;
                        if (bestScore.TryGetValue(dstState, out currentScore) == false || (currentScore <= dstScore)) {
                            bestScore[dstState] = dstScore;
                            bestEdge[dstState] = srcState;
                        }
                    }
                }

            }

            return bestEdge;
        }

        /// <summary>
        /// 識別を行う
        /// </summary>
        /// <param name="gold"></param>
        /// <returns></returns>
        public string[] Predict(IData[] gold) {
            return ArgMax(gold, this.Weight, this.Labels, this.CalcFeatures);
        }

        /// <summary>
        /// ラベル付きデータから特徴列を作る
        /// </summary>
        /// <param name="sentence"></param>
        /// <returns></returns>
        private static List<string> GetFeatures(TeatureData[] sentence, ICalcFeatures calcFeatures) {
            return sentence.SelectMany((_, i) => calcFeatures.ExtractFeatures(sentence, i)).ToList();
        }

        /// <summary>
        /// 一組の入力データと教師データから特徴ベクトルの差を学習
        /// </summary>
        /// <param name="weight">重みベクトル</param>
        /// <param name="cumulativeWeight">特徴ごとの学習の重みの総和</param>
        /// <param name="teature_sentence">教師データ</param>
        /// <param name="predict_sentence">推測データ</param>
        /// <param name="n">この学習の重み</param>
        /// <param name="labels">識別ラベル集合</param>
        private static void Learn(Dictionary<string, double> weight, Dictionary<string, double> cumulativeWeight, TeatureData[] teature_sentence, TeatureData[] predict_sentence, double n, HashSet<string> labels, ICalcFeatures calcFeatures) {
            var teatureFeatures = GetFeatures(teature_sentence, calcFeatures);
            foreach (var feature in teatureFeatures) {
                double w, cw;
                if (weight.TryGetValue(feature, out w) == false) { w = 0; }
                if (cumulativeWeight.TryGetValue(feature, out cw) == false) { cw = 0; }
                weight[feature] = w + 1;   // 重みベクトル中の対応する特徴を+1
                cumulativeWeight[feature] = cw + n;   // 特徴ごとの重みの総和を+n
            }

            var predictFeatures = GetFeatures(predict_sentence, calcFeatures);
            foreach (var feature in predictFeatures) {
                double w, cw;
                if (weight.TryGetValue(feature, out w) == false) { w = 0; }
                if (cumulativeWeight.TryGetValue(feature, out cw) == false) { cw = 0; }
                weight[feature] = w - 1;   // 重みベクトル中の対応する特徴を-1
                cumulativeWeight[feature] = cw - n;   // 特徴ごとの重みの総和を-n
            }
        }

        /// <summary>
        /// 学習を行う
        /// </summary>
        /// <param name="labels"></param>
        /// <param name="teatureData"></param>
        /// <param name="epoch"></param>
        /// <returns></returns>
        public static StructuredPerceptron Train(HashSet<string> labels, List<TeatureData[]> teatureData, int epoch, ICalcFeatures calcFeatures) {

            var weight = new Dictionary<string, double>();
            var cumulativeWeight = new Dictionary<string, double>();

            var n = 1;
            for (var iter = 0; iter < epoch; iter++) {
                foreach (var teature in teatureData) {
                    var predict = ArgMax(teature, weight, labels, calcFeatures);
                    // 識別結果が教師データと不一致の場合、重みを更新
                    if (teature.Select(x => x.Label).SequenceEqual(predict) == false) {
                        Learn(weight, cumulativeWeight, teature, teature.Zip(predict, (x, y) => new TeatureData() { Features = x.Features, Label = y }).ToArray(), n, labels, calcFeatures);
                        n++;
                    }
                }
            }

            // 過去の反復で学習した重みベクトルの平均を最終的な重みにする
            var final_weight = new Dictionary<string, double>(weight);
            foreach (var kv in cumulativeWeight) {
                final_weight[kv.Key] -= kv.Value / n;
            }
            return new StructuredPerceptron(labels, final_weight, calcFeatures);
        }

        /// <summary>
        /// 構造推定のテストを行う
        /// </summary>
        /// <param name="golds"></param>
        /// <param name="predicts"></param>
        /// <returns></returns>
        public TestResult Test(List<TeatureData[]> golds) {
            var correct = 0;
            var incorrect = 0;
            for (var index = 0; index < golds.Count; index++) {
                var teatures = golds[index];
                var predicts = Predict(golds[index]);
                for (var i = 0; i < teatures.Length; i++) {
                    if (teatures[i].Label == predicts[i]) { correct++; } else { incorrect++; }
                }
            }
            return new TestResult(correct, 0, incorrect, 0);
        }
    }
    public class PosTaggingCalcFeature : StructuredPerceptron.ICalcFeatures  {
        /// <summary>
        /// トークン列中のindex番目の特徴ベクトルを取り出す
        /// </summary>
        /// <param name="sentence">トークン列</param>
        /// <param name="index">要素番号</param>
        /// <param name="prevLabel">遷移元の品詞</param>
        /// <param name="nextLabel">遷移先の品詞</param>
        /// <returns>特徴ベクトル</returns>
        /// <example>
        /// {今日/名詞 の/接続詞 天気/名詞 は/接続詞 晴れ/名詞 です/接続詞} における は の特徴ベクトルは 
        /// {transition_feature:"名詞+接続詞", emission_feature:"接続詞+は", emission_feature:"接続詞+は", emission_feature_prev:"接続詞+天気", emission_feature_next:"接続詞+晴れ"}
        /// </example>
        public  List<string> ExtractFeatures(StructuredPerceptron.TeatureData[] teatureData, int i) {
            var prevWord = (0 <= i - 1 && i - 1 < teatureData.Length) ? teatureData[i - 1].Features[0] : "";
            var prevLabel = (0 <= i - 1 && i - 1 < teatureData.Length) ? teatureData[i - 1].Label : "BOS";
            var currWord = (0 <= i + 0 && i + 0 < teatureData.Length) ? teatureData[i + 0].Features[0] : "";
            var currLabel = (0 <= i + 0 && i + 0 < teatureData.Length) ? teatureData[i + 0].Label : "";
            var nextWord = (0 <= i + 1 && i + 1 < teatureData.Length) ? teatureData[i + 1].Features[0] : "";
            //return new List<string>() {
            //    $"transition_feature:{prevLabel}+{currLabel}",
            //    $"emission_feature:{currLabel}+{currWord}",
            //    $"emission_feature_prev:{currLabel}+{prevWord}",
            //    $"emission_feature_next:{currLabel}+{nextWord}",
            //};
            return new List<string>() {
                $"w,T {currWord} {currLabel}",
                $"Len(w),T {currWord.Length} {currLabel}",

                $"Ci,T {currWord.ElementAtOrDefault(0)} {currLabel}",
                $"Ci,Ci+1,T {currWord.ElementAtOrDefault(0)} {currWord.ElementAtOrDefault(1)} {currLabel}",
                $"Cj-1,T {currWord.ElementAtOrDefault(currWord.Length-1)} {currLabel}",
                $"Cj-2,Cj-1,T {currWord.ElementAtOrDefault(currWord.Length-2)} {currWord.ElementAtOrDefault(currWord.Length-1)} {currLabel}",

                $"Cj,T {nextWord.ElementAtOrDefault(0)} {currLabel}",
                $"Cj,Cj+1,T {nextWord.ElementAtOrDefault(0)} {nextWord.ElementAtOrDefault(1)} {currLabel}",
                $"Ci-1,T {prevWord.ElementAtOrDefault(prevWord.Length-1)} {currLabel}",
                $"Ci-2,Ci-1,T {prevWord.ElementAtOrDefault(prevWord.Length-2)} {prevWord.ElementAtOrDefault(prevWord.Length-1)} {currLabel}",

            };
        }


        public List<string> ExtractFeatures(StructuredPerceptron.IData[] data, int index, string prevLabel, string currLabel) {
            var prevWord = data.ElementAtOrDefault(index - 1)?.Features[0] ?? "";
            var currWord = data.ElementAtOrDefault(index + 0)?.Features[0] ?? "";
            var nextWord = data.ElementAtOrDefault(index + 1)?.Features[0] ?? "";
            //return new List<string>() {
            //    $"transition_feature:{prevLabel}+{currLabel}",
            //    $"emission_feature:{currLabel}+{currWord}",
            //    $"emission_feature_prev:{currLabel}+{prevWord}",
            //    $"emission_feature_next:{currLabel}+{nextWord}",
            //};
            return new List<string>() {
                $"w,T {currWord} {currLabel}",
                $"Len(w),T {currWord.Length} {currLabel}",

                $"Ci,T {currWord.ElementAtOrDefault(0)} {currLabel}",
                $"Ci,Ci+1,T {currWord.ElementAtOrDefault(0)} {currWord.ElementAtOrDefault(1)} {currLabel}",
                $"Cj-1,T {currWord.ElementAtOrDefault(currWord.Length-1)} {currLabel}",
                $"Cj-2,Cj-1,T {currWord.ElementAtOrDefault(currWord.Length-2)} {currWord.ElementAtOrDefault(currWord.Length-1)} {currLabel}",

                $"Cj,T {nextWord.ElementAtOrDefault(0)} {currLabel}",
                $"Cj,Cj+1,T {nextWord.ElementAtOrDefault(0)} {nextWord.ElementAtOrDefault(1)} {currLabel}",
                $"Ci-1,T {prevWord.ElementAtOrDefault(prevWord.Length-1)} {currLabel}",
                $"Ci-2,Ci-1,T {prevWord.ElementAtOrDefault(prevWord.Length-2)} {prevWord.ElementAtOrDefault(prevWord.Length-1)} {currLabel}",

            };
        }
    }
}
