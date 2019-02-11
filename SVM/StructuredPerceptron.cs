using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace svm_fobos {
    /// <summary>
    /// 構造化パーセプトロン
    /// </summary>
    public class StructuredPerceptron {
        /// <summary>
        /// 識別結果を示すラベル
        /// </summary>
        private List<string> Labels { get; }

        /// <summary>
        ///  識別器の重み
        /// </summary>
        private Dictionary<string, double> Weight { get; }

        /// <summary>
        /// 識別器のコンストラクタ
        /// </summary>
        /// <param name="labels"></param>
        /// <param name="weight"></param>
        public StructuredPerceptron(List<string> labels, Dictionary<string, double> weight) {
            this.Labels = labels;
            this.Weight = weight;
        }

        /// <summary>
        /// トークン列中のindex番目の特徴ベクトルを取り出す
        /// </summary>
        /// <param name="sentence">トークン列</param>
        /// <param name="index">要素番号</param>
        /// <param name="posPrev">遷移元の品詞</param>
        /// <param name="posNext">遷移先の品詞</param>
        /// <returns>特徴ベクトル</returns>
        /// <example>
        /// {今日/名詞 の/接続詞 天気/名詞 は/接続詞 晴れ/名詞 です/接続詞} における は の特徴ベクトルは 
        /// {transition_feature:"名詞+接続詞", emission_feature:"接続詞+は", emission_feature:"接続詞+は", emission_feature_prev:"接続詞+天気", emission_feature_next:"接続詞+晴れ"}
        /// </example>
        private static List<string> ExtractFeatures(string[] sentence, int index, string posPrev, string posNext) {
            var wordCurr = (index + 0) < sentence.Length ? sentence[index + 0] : "";
            var wordPrev = (index - 1) >= 0 ? sentence[index - 1] : "";
            var wordNext = (index + 1) < sentence.Length ? sentence[index + 1] : "";
            return new List<string>() {
                $"transition_feature:{posPrev}+{posNext}",
                $"emission_feature:{posNext}+{wordCurr}",
                $"emission_feature_prev:{posNext}+{wordPrev}",
                $"emission_feature_next:{posNext}+{wordNext}",
            };
        }

        /// <summary>
        /// 特徴ベクトルと重みの内積を取る（ベクトル中の特徴点に対応する値のみ1.0それ以外は0.0として内積を取るので、特徴ベクトルに含まれる特徴点に対応する重みの総和を求めることに等しい）
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
        /// 重みベクトル weight の下で、入力データ列 sentence_ に対して最も尤もらしいラベル labels の並びを求める
        /// </summary>
        /// <param name="sentence_"></param>
        /// <param name="weight"></param>
        /// <param name="labels"></param>
        /// <returns></returns>
        private static Tuple<string, string>[] ArgMax(string[] sentence_, Dictionary<string, double> weight, List<string> labels) {
            var bestEdge = ForwardStep(sentence_, weight, labels);
            return BackwardStep(sentence_, bestEdge);
        }

        /// <summary>
        /// 前向きアルゴリズムで観測されたシーケンス全体の確率を元にビタビアルゴリズムで最も尤もらしい並びを求める
        /// </summary>
        /// <param name="sentence"></param>
        /// <param name="best_edge"></param>
        /// <returns></returns>
        private static Tuple<string, string>[] BackwardStep(string[] sentence, Dictionary<string, string> best_edge) {
            var sentence_new = new Tuple<string, string>[sentence.Length];
            var max_idx = sentence.Length + 1;
            var next_edge = best_edge[$"{max_idx} EOS"];
            while (next_edge != "0 BOS") {
                if (next_edge == null) {
                    throw new Exception("Cannot backtrack");
                }

                var tmp = Regex.Split(next_edge, @"\s");
                var idx = int.Parse(tmp[0]);
                var pos = tmp[1];
                sentence_new[idx - 1] = Tuple.Create(sentence[idx - 1], pos);
                next_edge = best_edge[next_edge];
            }
            return sentence_new;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sentence">トークン列</param>
        /// <param name="weight">重み</param>
        /// <param name="w_idx">遷移ステップ番号</param>
        /// <param name="w_idx_prev"></param>
        /// <param name="pos_next">遷移先の品詞</param>
        /// <param name="pos_prev">遷移元の品詞</param>
        /// <param name="best_score">スコア表</param>
        /// <param name="best_edge">遷移辺</param>
        private static void ForwarsStepOne(string[] sentence, Dictionary<string, double> weight, int w_idx, string w_idx_prev, string pos_next, string pos_prev, Dictionary<string, double> best_score, Dictionary<string, string> best_edge) {
            var features = ExtractFeatures(sentence, w_idx, pos_prev, pos_next);
            double cum_score;
            if (best_score.TryGetValue(w_idx_prev, out cum_score) == false) {
                cum_score = 0.0;
            }
            var score = cum_score + InnerProduct(features, weight);
            var w_next_idx = w_idx + 1;
            var w_next_idx_next = $"{w_next_idx} {pos_next}";
            double cur_score;
            if (best_score.TryGetValue(w_next_idx_next, out cur_score) == false || (cur_score <= score)) {
                best_score[w_next_idx_next] = score;
                best_edge[w_next_idx_next] = w_idx_prev;
            }
        }

        /// <summary>
        /// 重みベクトル weight の下で、入力データ列 sentence_ に前向きアルゴリズムを適用し、入力データ列の全体としてのラベル labels の確率を求める
        /// </summary>
        /// <param name="sentence">入力データ列 </param>
        /// <param name="weight">重みベクトル</param>
        /// <param name="labels">ラベル</param>
        /// <returns></returns>
        private static Dictionary<string, string> ForwardStep(string[] sentence, Dictionary<string, double> weight, List<string> labels) {
            var best_score = new Dictionary<string, double>();
            var best_edge = new Dictionary<string, string>();
            var pos_bos = "BOS";
            var pos_eos = "EOS";

            best_score[$"0 {pos_bos}"] = 0.0;
            best_edge[$"0 {pos_bos}"] = null;

            {
                var pos_prev = pos_bos;
                var w_idx = 0;
                var w_idx_prev = $"{w_idx} {pos_prev}";
                foreach (var pos_next in labels) {
                    ForwarsStepOne(sentence, weight, w_idx, w_idx_prev, pos_next, pos_prev, best_score, best_edge);
                }
            }
            for (var w_idx = 1; w_idx < sentence.Length; w_idx++) {
                foreach (var pos_prev in labels) {
                    var w_idx_prev = $"{w_idx} {pos_prev}";
                    foreach (var pos_next in labels) {
                        ForwarsStepOne(sentence, weight, w_idx, w_idx_prev, pos_next, pos_prev, best_score, best_edge);
                    }
                }
            }
            {
                var pos_next = pos_eos;
                var w_idx = sentence.Length;
                foreach (var pos_prev in labels) {
                    var w_idx_prev = $"{w_idx} {pos_prev}";
                    ForwarsStepOne(sentence, weight, w_idx, w_idx_prev, pos_next, pos_prev, best_score, best_edge);
                }
            }
            return best_edge;
        }

        private static List<string> GetFeatures(Tuple<string, string>[] sentence) {
            var gold_features = new List<string>();
            var sentence_ = sentence.Select(x => x.Item1).ToArray();
            for (var index = 0; index < sentence.Length; index++) {
                var prev_pos = (index - 1 >= 0) ? sentence[index - 1].Item2 : "BOS";
                var next_pos = sentence[index].Item2;
                var features = ExtractFeatures(sentence_, index, prev_pos, next_pos);
                gold_features.AddRange(features);
            }
            return gold_features;
        }

        private static void Learn(Dictionary<string, double> weight, Dictionary<string, double> cum_weight, Tuple<string, string>[] sentence, Tuple<string, string>[] predict_sentence, double n, List<string> pos_labels) {
            List<string> gold_features = GetFeatures(sentence);
            List<string> predict_features = GetFeatures(predict_sentence);

            // 不正解の特徴についてのみ重みの更新を行う
            // 具体的には:
            // ・教師データに含まれる特徴の重みは+1する。
            // ・推測データに含まれる特徴の重みは-1する。
            // を行う
            // こうすると、教師データにも推測データにも含まれる特徴は変動しない
            foreach (var feature in gold_features) {
                if (weight.ContainsKey(feature) == false) { weight[feature] = 0; }
                if (cum_weight.ContainsKey(feature) == false) { cum_weight[feature] = 0; }
                weight[feature] += 1;
                cum_weight[feature] += n;
            }
            foreach (var feature in predict_features) {
                if (weight.ContainsKey(feature) == false) { weight[feature] = 0; }
                if (cum_weight.ContainsKey(feature) == false) { cum_weight[feature] = 0; }
                weight[feature] -= 1;
                cum_weight[feature] -= n;
            }
        }

        private static Dictionary<string, double> GetFinalWeight(Dictionary<string, double> weight, Dictionary<string, double> cum_weight, double n) {
            var final_weight = new Dictionary<string, double>(weight);
            foreach (var kv in cum_weight) {
                final_weight[kv.Key] -= kv.Value / n;
            }
            return final_weight;
        }

        /// <summary>
        /// 構造推定結果の正答率を求める
        /// </summary>
        /// <param name="golds"></param>
        /// <param name="predicts"></param>
        /// <returns></returns>
        public static double Accuracy(List<Tuple<string, string>[]> golds, List<Tuple<string, string>[]> predicts) {
            var correct = 0.0;
            var num = 0.0;
            for (var index = 0; index < golds.Count; index++) {
                var gold_pos_labels = golds[index].Select(x => x.Item2).ToList();
                var predict_pos_labels = predicts[index].Select(x => x.Item2).ToList();
                for (var i = 0; i < gold_pos_labels.Count; i++) {
                    if (gold_pos_labels[i] == predict_pos_labels[i]) { correct++; }
                    num++;
                }
            }
            return correct / num;
        }

        /// <summary>
        /// 識別を行う
        /// </summary>
        /// <param name="gold"></param>
        /// <returns></returns>
        public Tuple<string, string>[] Predict(string[] gold) {
            return ArgMax(gold, this.Weight, this.Labels);
        }

        /// <summary>
        /// 学習を行う
        /// </summary>
        /// <param name="labels"></param>
        /// <param name="train_data"></param>
        /// <param name="step"></param>
        /// <returns></returns>
        public static StructuredPerceptron Train(List<string> labels, List<Tuple<string, string>[]> train_data, int step) {
            var weight = new Dictionary<string, double>();
            var cum_weight = new Dictionary<string, double>();
            var n = 1;
            for (var iter = 0; iter < step; iter++) {
                foreach (var gold in train_data) {
                    var predict = ArgMax(gold.Select(x => x.Item1).ToArray(), weight, labels);
                    // 識別結果が教師データと不一致の場合、重みを更新
                    if (gold.Select(x => x.Item2).SequenceEqual(predict.Select(x => x.Item2)) == false) {
                        Learn(weight, cum_weight, gold, predict, n, labels);
                        n++;
                    }
                }
            }
            var final_weight = GetFinalWeight(weight, cum_weight, n);
            return new StructuredPerceptron(labels, final_weight);
        }
    }

}
