using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        /// 推定したいラベル
        /// </summary>
        private HashSet<string> partsOfSpeechLabels;

        /// <summary>
        /// パーセプトロンの重みベクトル
        /// </summary>
        private Dictionary<string, double> final_weight;

        public StructuredPerceptron(HashSet<string> partsOfSpeechLabels, Dictionary<string, double> final_weight) {
            this.partsOfSpeechLabels = partsOfSpeechLabels;
            this.final_weight = final_weight;
        }

        internal static HashSet<string> GetPartsOfSpeechLabels(List<Tuple<string, string>[]> data) {
            return new HashSet<string>(data.SelectMany(x => x.Select(y => y.Item2)).Distinct());

        }

        /// <summary>
        /// トークン列中のindex番目の特徴ベクトルを取り出す
        /// </summary>
        /// <param name="sentence">トークン列</param>
        /// <param name="index">要素番号</param>
        /// <param name="pos_prev">遷移元の品詞</param>
        /// <param name="pos_next">遷移先の品詞</param>
        /// <returns>特徴ベクトル</returns>
        /// <example>
        /// {今日/名詞 の/接続詞 天気/名詞 は/接続詞 晴れ/名詞 です/接続詞} における は の特徴ベクトルは 
        /// {transition_feature:"名詞+接続詞", emission_feature:"接続詞+は", emission_feature:"接続詞+は", emission_feature_prev:"接続詞+天気", emission_feature_next:"接続詞+晴れ"}
        /// </example>
        private static List<string> extract_features(string[] sentence, int index, string pos_prev, string pos_next) {
            var w      = sentence.ElementAtOrDefault(index + 0, "EOS");
            var w_prev = sentence.ElementAtOrDefault(index - 1, "");
            var w_next = sentence.ElementAtOrDefault(index + 1, "");
            return new List<string>() {
                $"transition_feature:{pos_prev}+{pos_next}",
                $"emission_feature:{pos_next}+{w}",
                $"emission_feature_prev:{pos_next}+{w_prev}",
                $"emission_feature_next:{pos_next}+{w_next}",
            };
        }

        private static double inner_product(List<string> features, Dictionary<string, double> weight) {
            double ret = 0.0;
            foreach (var feature in features) {
                double w;
                if (weight.TryGetValue(feature, out w)) { ret += w; }
            }
            return ret;
        }

        private static Tuple<string, string>[] argmax(string[] sentence_, Dictionary<string, double> weight, HashSet<string> partsOfSpeechLabels) {
            var best_edge = forward_step(sentence_, weight, partsOfSpeechLabels);
            return backward_step(sentence_, best_edge);
        }

        private static Tuple<string, string>[] backward_step(string[] sentence, Dictionary<string, Tuple<string,double>> best) {
            var sentence_new = new Tuple<string, string>[sentence.Length];
            var max_idx = sentence.Length + 1;
            var next_edge = best[$"{max_idx} EOS"].Item1;
            while (next_edge != "0 BOS") {
                if (next_edge == null) {
                    throw new Exception("Cannot backtrack");
                }

                var tmp = next_edge.Split(" ".ToArray(), 2);
                var idx = int.Parse(tmp[0]);
                var pos = tmp[1];
                sentence_new[idx - 1] = Tuple.Create(sentence[idx - 1], pos);
                next_edge = best[next_edge].Item1;
            }
            return sentence_new;
        }

        /// <summary>
        /// ビタビ探索の前向きステップ一つ
        /// </summary>
        /// <param name="sentence">トークン列</param>
        /// <param name="weight">重み</param>
        /// <param name="w_idx">遷移ステップ番号</param>
        /// <param name="w_idx_prev"></param>
        /// <param name="pos_next">遷移先の品詞</param>
        /// <param name="pos_prev">遷移元の品詞</param>
        /// <param name="best">ビタビ経路表</param>
        private static void forwars_step_one(
            string[] sentence, 
            Dictionary<string, double> weight, 
            int w_idx, 
            string w_idx_prev, 
            string pos_next, 
            string pos_prev, 
            Dictionary<string, Tuple<string,double>> best
        ) {
            // 特徴を抽出
            var features = extract_features(sentence, w_idx, pos_prev, pos_next);

            // 遷移前の状態のスコアを得る
            Tuple<string, double> cum;
            if (best.TryGetValue(w_idx_prev, out cum) == false) {
                cum = Tuple.Create("",0.0);
            }

            // 遷移後の状態名とスコアを求める
            var score = cum.Item2 + inner_product(features, weight);
            var w_next_idx = w_idx + 1;
            var w_next_idx_next = $"{w_next_idx} {pos_next}";

            // 遷移後のスコアが遷移前のスコアよりも良い場合は遷移後から遷移前への辺のスコアを更新
            Tuple<string, double> cur;
            if (best.TryGetValue(w_next_idx_next, out cur) == false || (cur.Item2 <= score)) {
                best[w_next_idx_next] = Tuple.Create(w_idx_prev,score);
            }
        }


        /// <summary>
        /// トークン列に対するビタビ探索の前向きステップ
        /// </summary>
        /// <param name="sentence">トークン列</param>
        /// <param name="weight">重み</param>
        /// <param name="partsOfSpeechLabels">品詞表</param>
        /// <returns></returns>
        private static Dictionary<string, Tuple<string, double>> forward_step(string[] sentence, Dictionary<string, double> weight, HashSet<string> partsOfSpeechLabels) {
            var best = new Dictionary<string, Tuple<string,double>>();
            var bos = "BOS";
            var eos = "EOS";

            best[$"0 {bos}"] = Tuple.Create((string)null,0.0);

            {
                var prev = bos;
                var w_idx = 0;
                var w_idx_prev = $"{w_idx} {prev}";
                foreach (var next in partsOfSpeechLabels) {
                    forwars_step_one(sentence, weight, w_idx, w_idx_prev, next, prev, best);
                }
            }
            for (var w_idx = 1; w_idx < sentence.Length; w_idx++) {
                foreach (var prev in partsOfSpeechLabels) {
                    var w_idx_prev = $"{w_idx} {prev}";
                    foreach (var next in partsOfSpeechLabels) {
                        forwars_step_one(sentence, weight, w_idx, w_idx_prev, next, prev, best);
                    }
                }
            }
            {
                var next = eos;
                var w_idx = sentence.Length;
                foreach (var prev in partsOfSpeechLabels) {
                    var w_idx_prev = $"{w_idx} {prev}";
                    forwars_step_one(sentence, weight, w_idx, w_idx_prev, next, prev, best);
                }
            }
            return best;
        }

        /// <summary>
        /// 入力データから特徴ベクトルを生成する
        /// </summary>
        /// <param name="sentence"></param>
        /// <returns></returns>
        private static List<string> get_features(Tuple<string, string>[] sentence) {
            var gold_features = new List<string>();
            var sentence_ = sentence.Select(x => x.Item1).ToArray();
            for (var index = 0; index < sentence.Length; index++) {
                var prev_pos = (index - 1 >= 0) ? sentence[index - 1].Item2 : "BOS";
                var next_pos = sentence[index].Item2;
                var features = extract_features(sentence_, index, prev_pos, next_pos);
                gold_features.AddRange(features);
            }
            return gold_features;
        }

        /// <summary>
        /// 学習を行う
        /// </summary>
        /// <param name="weight">重み行列</param>
        /// <param name="cum_weight">平均化用の重みを保持する重み行列</param>
        /// <param name="sentence">教師データ</param>
        /// <param name="predict_sentence">推測結果</param>
        /// <param name="n">この学習の重み</param>
        /// <param name="partsOfSpeechLabels">ラベル表</param>
        public static void learn(
            Dictionary<string, double> weight, 
            Dictionary<string, double> cum_weight, 
            Tuple<string, string>[] sentence, 
            Tuple<string, string>[] predict_sentence, 
            double n, 
            HashSet<string> partsOfSpeechLabels
        ) {
            // 特徴列を得る
            List<string> gold_features = get_features(sentence);
            List<string> predict_features = get_features(predict_sentence);

            // 教師データの特徴列に含まれる要素の重みを正の方向に移動
            foreach (var gold_feature in gold_features) {
                if (weight.ContainsKey(gold_feature) == false) { weight[gold_feature] = 0; }
                if (cum_weight.ContainsKey(gold_feature) == false) { cum_weight[gold_feature] = 0; }
                weight[gold_feature]     += 1;
                cum_weight[gold_feature] += n;
            }
            // 推測結果の特徴列に含まれる要素の重みを負の方向に移動
            foreach (var predict_feature in predict_features) {
                if (weight.ContainsKey(predict_feature) == false) { weight[predict_feature] = 0; }
                if (cum_weight.ContainsKey(predict_feature) == false) { cum_weight[predict_feature] = 0; }
                weight[predict_feature] -= 1;
                cum_weight[predict_feature] -= n;
            }
        }

    /// <summary>
    /// 重みの平均値を求める
    /// </summary>
    /// <param name="weight"></param>
    /// <param name="cum_weight"></param>
    /// <param name="n">学習回数</param>
    /// <returns></returns>
        private static Dictionary<string, double> get_final_weight(Dictionary<string, double> weight, Dictionary<string, double> cum_weight, double n) {
            var final_weight = new Dictionary<string, double>(weight);
            foreach (var kv in cum_weight) {
                final_weight[kv.Key] -= kv.Value / n;
            }
            return final_weight;
        }

        public static double accuracy(List<Tuple<string, string>[]> golds, List<Tuple<string, string>[]> predicts) {
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
        /// 入力ベクトルに対する構造推定を行う
        /// </summary>
        /// <param name="gold"></param>
        /// <returns></returns>
        public Tuple<string, string>[] Predict(string[] gold) {
            return argmax(gold, this.final_weight, this.partsOfSpeechLabels);
        }

        /// <summary>
        /// 入力教師データ列から構造を学習する
        /// </summary>
        /// <param name="train_data">教師データ</param>
        /// <param name="step">学習回数</param>
        /// <returns></returns>
        public static StructuredPerceptron Train(List<Tuple<string, string>[]> train_data, int step) {
            
            // 教師データ列から構造に割り当てるラベルの集合を作る
            var partsOfSpeechLabels = GetPartsOfSpeechLabels(train_data);

            // 重みベクトル
            var weight = new Dictionary<string, double>();

            // 平均化用の重みベクトル
            var cum_weight = new Dictionary<string, double>();

            var n = 1;
            for (var iter = 0; iter < step; iter++) {
                // 学習を１ステップ実行
                foreach (var gold in train_data) {
                    var predict = argmax(gold.Select(x => x.Item1).ToArray(), weight, partsOfSpeechLabels);
                    
                    // 識別結果と教師データのラベル推定結果が一致したら重みベクトルは更新しない
                    if (gold.Select(x => x.Item2).SequenceEqual(predict.Select(x => x.Item2)) == false) {
                        learn(weight, cum_weight, gold, predict, n, partsOfSpeechLabels);
                        n++;
                    }
                }
            }
            var final_weight = get_final_weight(weight, cum_weight, n);
            return new StructuredPerceptron(partsOfSpeechLabels, final_weight);
        }
    }

}
