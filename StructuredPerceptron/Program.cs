using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace StructuredPerceptron {
    class Program {
        static void Main(string[] args) {
            StructuredPerceptron.main();
            //Console.ReadLine();
        }
    }

    class StructuredPerceptron {
        public class Token {
            /// <summary>
            /// 単語
            /// </summary>
            public string Word { get; }
            /// <summary>
            /// 品詞
            /// </summary>
            public string PartsOfSpeech { get; }
            /// <summary>
            /// コンストラクタ
            /// </summary>
            /// <param name="word">単語</param>
            /// <param name="partsOfSpeech">品詞</param>
            public Token(string word, string partsOfSpeech) {
                Word = word;
                PartsOfSpeech = partsOfSpeech;
            }
        }

        private static List<Token> ParseLine(string line) {
            var sentence = new List<Token>();
            var pairs = Regex.Split(line, @"\s");
            foreach (var pair in pairs) {
                var ret = Regex.Split(pair, @"_");
                if (ret.Length >= 2 && ret[0] != null && ret[1] != null) {
                    sentence.Add(new Token(word: ret[0], partsOfSpeech: ret[1]));
                }
            }
            return sentence;
        }

        private static List<List<Token>> ReadData(string filename) {
            var train_data = new List<List<Token>>();
            using (var sr = new System.IO.StreamReader(filename)) {
                string line = null;
                while ((line = sr.ReadLine()) != null) {
                    train_data.Add(ParseLine(line.Trim()));
                }
            }
            return train_data;
        }
        /// <summary>
        /// 全トークン列から品詞を取り出す
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private static List<string> GetPartsOfSpeechLabels(List<List<Token>> data) {
            return data.SelectMany(x => x.Select(y => y.PartsOfSpeech)).Distinct().ToList();
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
        private static List<string> extract_features(List<Token> sentence, int index, string pos_prev, string pos_next) {
            var w      = (index + 0) < sentence.Count ? sentence[index + 0].Word : "EOS";
            var w_prev = (index - 1) >= 0             ? sentence[index - 1].Word : "";
            var w_next = (index + 1) < sentence.Count ? sentence[index + 1].Word : "";
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

        private static List<Token> argmax(List<Token> sentence_, Dictionary<string, double> weight, List<string> partsOfSpeechLabels) {
            var sentence = sentence_.Select(x => new Token(word: x.Word, partsOfSpeech: x.PartsOfSpeech)).ToList();
            var best_edge = forward_step(sentence, weight, partsOfSpeechLabels);
            return backward_step(sentence, best_edge);
        }

        private static List<Token> backward_step(List<Token> sentence, Dictionary<string, string> best_edge) {
            var max_idx = sentence.Count + 1;
            var next_edge = best_edge[$"{max_idx} EOS"];
            while (next_edge != "0 BOS") {
                if (next_edge == null) {
                    throw new Exception("Cannot backtrack");
                }

                var tmp = Regex.Split(next_edge, @"\s");
                var idx = int.Parse(tmp[0]);
                var pos = tmp[1];
                sentence[idx - 1] = new Token(sentence[idx - 1].Word, pos);
                next_edge = best_edge[next_edge];
            }
            return sentence;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sentence">トークン列</param>
        /// <param name="weight">重み</param>
        /// <param name="w_idx">遷移ステップ番号</param>
        /// <param name="w_idx_prev"></param>
        /// <param name="next">遷移先の品詞</param>
        /// <param name="prev">遷移元の品詞</param>
        /// <param name="best_score">スコア表</param>
        /// <param name="best_edge">遷移辺</param>
        private static void forwars_step_one(List<Token> sentence, Dictionary<string, double> weight, int w_idx, string w_idx_prev, string next, string prev, Dictionary<string, double> best_score, Dictionary<string, string> best_edge) {
            var features = extract_features(sentence, w_idx, prev, next);
            double cum_score;
            if (best_score.TryGetValue(w_idx_prev, out cum_score) == false) {
                cum_score = 0.0;
            }
            var score = cum_score + inner_product(features, weight);
            var w_next_idx = w_idx + 1;
            var w_next_idx_next = $"{w_next_idx} {next}";
            double cur_score;
            if (best_score.TryGetValue(w_next_idx_next, out cur_score) == false || (cur_score <= score)) {
                best_score[w_next_idx_next] = score;
                best_edge[w_next_idx_next] = w_idx_prev;
            }
        }

        /// <summary>
        /// トークン列に対するビタビ探索の前向きステップ
        /// </summary>
        /// <param name="sentence">トークン列</param>
        /// <param name="weight">重み</param>
        /// <param name="partsOfSpeechLabels">品詞表</param>
        /// <returns></returns>
        private static Dictionary<string, string>  forward_step(List<Token> sentence, Dictionary<string, double> weight, List<string> partsOfSpeechLabels) {
            var best_score = new Dictionary<string, double>();
            var best_edge = new Dictionary<string, string>();
            var bos = "BOS";
            var eos = "EOS";

            best_score[$"0 {bos}"] = 0.0;
            best_edge[$"0 {bos}"] = null;

            {
                var prev = bos;
                var w_idx = 0;
                var w_idx_prev = $"{w_idx} {prev}";
                foreach (var next in partsOfSpeechLabels) {
                    forwars_step_one(sentence, weight, w_idx, w_idx_prev, next, prev, best_score, best_edge);
                }
            }
            for (var w_idx = 1; w_idx < sentence.Count; w_idx++) {
                foreach (var prev in partsOfSpeechLabels) {
                    var w_idx_prev = $"{w_idx} {prev}";
                    foreach (var next in partsOfSpeechLabels) {
                        forwars_step_one(sentence, weight, w_idx, w_idx_prev, next, prev, best_score, best_edge);
                    }
                }
            }
            {
                var next = eos;
                var w_idx = sentence.Count;
                foreach (var prev in partsOfSpeechLabels) {
                    var w_idx_prev = $"{w_idx} {prev}";
                    forwars_step_one(sentence, weight, w_idx, w_idx_prev, next, prev, best_score, best_edge);
                }
            }
            return best_edge;
        }

        private static List<string> get_features(List<Token> sentence) {
            var gold_features = new List<string>();
            for (var index = 0; index < sentence.Count; index++) {
                var prev_pos = (index - 1 >= 0) ? sentence[index - 1].PartsOfSpeech : "BOS";
                var next_pos = sentence[index].PartsOfSpeech;
                var features = extract_features(sentence, index, prev_pos, next_pos);
                gold_features.AddRange(features);
            }
            return gold_features;
        }

        public static void learn(Dictionary<string, double> weight, Dictionary<string, double> cum_weight, List<Token> sentence, List<Token> predict_sentence, double n , List<string> pos_labels) {
            List<string> gold_features = get_features(sentence);
            List<string> predict_features = get_features(predict_sentence);

            // update weight
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

        private static Dictionary<string, double> get_final_weight(Dictionary<string, double> weight, Dictionary<string, double> cum_weight, double n) {
            var final_weight = new Dictionary<string, double>(weight);
            foreach (var kv in cum_weight) {
                final_weight[kv.Key] -= kv.Value / n;
            }
            return final_weight;
        }

        public static double accuracy(List<List<Token>> golds, List<List<Token>> predicts) {
            var correct = 0.0;
            var num = 0.0;
            for (var index = 0; index < golds.Count; index++) {
                var gold_pos_labels = golds[index].Select(x => x.PartsOfSpeech).ToList();
                var predict_pos_labels = predicts[index].Select(x => x.PartsOfSpeech).ToList();
                for (var i = 0; i < gold_pos_labels.Count; i++) {
                    if (gold_pos_labels[i] == predict_pos_labels[i]) { correct++; }
                    num++;
                }
            }
            return correct / num;
        }

        public static string pos_labels_str(List<Token> sentence) {
            return string.Join(", ", sentence.Select(x => x.PartsOfSpeech));
        }

        public static void main() {
            var pos_filename_train = "wiki-ja-train.word_pos";
            var train_data = ReadData(pos_filename_train);
            var partsOfSpeechLabels = GetPartsOfSpeechLabels(train_data);

            var pos_filename_test = "wiki-ja-test.word_pos";
            var test_data = ReadData(pos_filename_test);

            var weight = new Dictionary<string, double>();
            var cum_weight = new Dictionary<string, double>();
            var n = 1;
            for (var iter = 0; iter <= 10; iter++) {
                Console.WriteLine($"Iter: {iter}");
                var rand = new Random();
                foreach (var gold in train_data.OrderBy(x => rand.Next())) {
                    var predict = argmax(gold, weight, partsOfSpeechLabels);
                    // 正解と一致したら重みベクトルは更新しない
                    if (pos_labels_str(gold) != pos_labels_str(predict)) {
                        learn(weight, cum_weight, gold, predict, n, partsOfSpeechLabels);
                        n++;
                    }
                }
                var w = get_final_weight(weight, cum_weight, n);
                var predicts = new List<List<Token>>();
                foreach (var gold in test_data) {
                    var predict = argmax(gold, w, partsOfSpeechLabels);
                    predicts.Add(predict);
                }
                Console.WriteLine(accuracy(test_data, predicts));
            }

            // save weight
            using (var tw = new System.IO.StreamWriter("model.csv")) {
                foreach (var key in weight.Keys) {
                    tw.Write(key);
                    tw.Write('\t');
                    tw.Write(weight[key]);
                    tw.Write('\t');
                    tw.Write(cum_weight[key]);
                    tw.WriteLine();
                }
            }
            // output
            {
                var w = get_final_weight(weight, cum_weight, n);
                foreach (var gold in test_data) {
                    var predict = argmax(gold, w, partsOfSpeechLabels);
                    foreach (var ret in predict.Zip(gold, Tuple.Create)) {
                        var diff = ret.Item1.PartsOfSpeech == ret.Item2.PartsOfSpeech ? "+" : "-";
                        Console.WriteLine($"{diff}: {ret.Item1.Word}/{ret.Item1.PartsOfSpeech}/{ret.Item2.PartsOfSpeech}");
                    }
                }
            }

        }
    }
}
