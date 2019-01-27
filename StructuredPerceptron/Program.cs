using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace StructuredPerceptron {
    class Program {
        static void Main(string[] args) {
            var sp = new StructuredPerceptron();
            sp.main();
        }
    }

    class StructuredPerceptron {
        public class Parsed {
            public string w { get; }
            public string pos { get; set; }
            public Parsed(string w, string pos) {
                this.w = w;
                this.pos = pos;
            }
        }
        private List<Parsed> parse_line(string line) {
            var sentence = new List<Parsed>();
            var pairs = Regex.Split(line, @"\s");  // lineはutf8
            foreach (var pair in pairs) {
                var ret = Regex.Split(pair, @"_");
                if (ret.Length >= 2 && ret[0] != null && ret[1] != null) {
                    sentence.Add(new Parsed(w: ret[0], pos: ret[1]));
                }
            }
            return sentence;
        }

        private List<List<Parsed>> read_data(string filename) {
            var train_data = new List<List<Parsed>>();
            using (var sr = new System.IO.StreamReader(filename)) {
                string line = null;
                while ((line = sr.ReadLine()) != null) {
                    train_data.Add(parse_line(line.Trim()));
                }
            }
            return train_data;
        }
        private List<string> get_pos_labels(List<List<Parsed>> data) {
            return data.SelectMany(x => x.Select(y => y.pos)).Distinct().ToList();
        }

        private List<string> extract_features(List<Parsed> sentence, int index, string pos_prev, string pos_next) {
            var features = new List<string>();
            var w = index < sentence.Count ? sentence[index].w : "EOS";
            var w_prev = (index - 1) >= 0             ? sentence[index - 1].w : "";
            var w_next = (index + 1) < sentence.Count ? sentence[index + 1].w : "";
            features.Add($"transition_feature:{pos_prev}+{pos_next}");
            features.Add($"emission_feature:{pos_next}+{w}");
            features.Add($"emission_feature_prev:{pos_next}+{w_prev}");
            features.Add($"emission_feature_next:{pos_next}+{w_next}");
            return features;
        }

        private double inner_product(List<string> features, Dictionary<string, double> weight) {
            return features.Sum(x => (weight.ContainsKey(x) ? weight[x] : 0.0));
        }

        private List<Parsed> argmax(List<Parsed> sentence_, Dictionary<string, double> weight, List<string> pos_labels) {
            var sentence = sentence_.Select(x => new Parsed(w: x.w, pos: x.pos)).ToList();
            var best_edge = forward_step(sentence, weight, pos_labels);
            return backward_step(sentence, best_edge);
        }

        private List<Parsed> backward_step(List<Parsed> sentence, Dictionary<string, string> best_edge) {
            var max_idx = sentence.Count + 1;
            var next_edge = best_edge[$"{max_idx} EOS"];
            while (next_edge != "0 BOS") {
                if (next_edge == null) {
                    throw new Exception("Cannot backtrack");
                }

                var tmp = Regex.Split(next_edge, @"\s");
                var idx = int.Parse(tmp[0]);
                var pos = tmp[1];
                sentence[idx - 1].pos = pos;
                next_edge = best_edge[next_edge];
            }
            return sentence;
        }

        private new Dictionary<string, string>  forward_step(List<Parsed> sentence, Dictionary<string, double> weight, List<string> pos_labels) {
            var best_score = new Dictionary<string, double>();
            var best_edge = new Dictionary<string, string>();
            var bos = "BOS";
            best_score[$"0 {bos}"] = 0.0;
            best_edge[$"0 {bos}"] = null;
            for (var w_idx = 0; w_idx < sentence.Count; w_idx++) {
                foreach (var prev in ((w_idx == 0) ? new List<string>() { bos } : pos_labels)) {
                    foreach (var next in pos_labels) {
                        var features = extract_features(sentence, w_idx, prev, next);
                        var cum_score = w_idx == 0 ? (best_score.ContainsKey($"{w_idx} {bos}") ? best_score[$"{w_idx} {bos}"] : 0.0)
                                                   : (best_score.ContainsKey($"{w_idx} {prev}") ? best_score[$"{w_idx} {prev}"] : 0.0);
                        var score = cum_score + inner_product(features, weight);
                        var w_next_idx = w_idx + 1;
                        if ((best_score.ContainsKey($"{w_next_idx} {next}") == false) ||
                             (best_score[$"{w_next_idx} {next}"] <= score)) {
                            best_score[$"{w_next_idx} {next}"] = score;
                            best_edge[$"{w_next_idx} {next}"] = $"{w_idx} {prev}";
                        }
                    }
                }
            }
            {
                var eos = "EOS";
                var w_idx = sentence.Count;
                foreach (var prev in pos_labels) {
                    var next = eos;
                    var features = extract_features(sentence, w_idx, prev, next);
                    var score = best_score[$"{w_idx} {prev}"] + inner_product(features, weight);
                    var w_next_idx = w_idx + 1;
                    if ((best_score.ContainsKey($"{w_next_idx} {next}") == false) ||
                         (best_score[$"{w_next_idx} {next}"] <= score)) {
                        best_score[$"{w_next_idx} {next}"] = score;
                        best_edge[$"{w_next_idx} {next}"] = $"{w_idx} {prev}";
                    }
                }
            }
            return best_edge;
        }

        private new List<string> get_features(List<Parsed> sentence) {
            var gold_features = new List<string>();
            for (var index = 0; index < sentence.Count; index++) {
                var prev_pos = (index - 1 >= 0) ? sentence[index - 1].pos : "BOS";
                var next_pos = sentence[index].pos;
                var features = extract_features(sentence, index, prev_pos, next_pos);
                gold_features.AddRange(features);
            }
            return gold_features;
        }

        public void learn(Dictionary<string, double> weight, Dictionary<string, double> cum_weight, List<Parsed> sentence, List<Parsed> predict_sentence, double n , List<string> pos_labels) {
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

        Dictionary<string, double> get_final_weight(Dictionary<string, double> weight, Dictionary<string, double> cum_weight, double n) {
            var final_weight = new Dictionary<string, double>(weight);
            foreach (var kv in cum_weight) {
                final_weight[kv.Key] -= kv.Value / n;
            }
            return final_weight;
        }

        public double accuracy(List<List<Parsed>> golds, List<List<Parsed>> predicts) {
            var correct = 0.0;
            var num = 0.0;
            for (var index = 0; index < golds.Count; index++) {
                var gold_pos_labels = golds[index].Select(x => x.pos).ToList();
                var predict_pos_labels = predicts[index].Select(x => x.pos).ToList();
                for (var i = 0; i < gold_pos_labels.Count; i++) {
                    if (gold_pos_labels[i] == predict_pos_labels[i]) { correct++; }

                    num++;
                }
            }
            return correct / num;
        }

        public string pos_labels_str(List<Parsed> sentence) {
            return string.Join(", ", sentence.Select(x => x.pos));
        }

        public void main() {
            var pos_filename_train = "wiki-ja-train.word_pos";
            var train_data = read_data(pos_filename_train);
            var pos_labels = get_pos_labels(train_data);

            var pos_filename_test = "wiki-ja-test.word_pos";
            var test_data = read_data(pos_filename_test);

            var weight = new Dictionary<string, double>();
            var cum_weight = new Dictionary<string, double>();
            var n = 1;
            for (var iter = 0; iter <= 10; iter++) {
                Console.WriteLine($"Iter: {iter}");
                var rand = new Random();
                foreach (var gold in train_data.OrderBy(x => rand.Next())) {
                    var predict = argmax(gold, weight, pos_labels);
                    // 正解と一致したら重みベクトルは更新しない
                    if (pos_labels_str(gold) != pos_labels_str(predict)) {
                        learn(weight, cum_weight, gold, predict, n, pos_labels);
                        n++;
                    }
                }
                var w = get_final_weight(weight, cum_weight, n);
                var predicts = new List<List<Parsed>>();
                foreach (var gold in test_data) {
                    var predict = argmax(gold, w, pos_labels);
                    predicts.Add(predict);
                }
                Console.WriteLine(accuracy(test_data, predicts));
            }

        }
    }
}
