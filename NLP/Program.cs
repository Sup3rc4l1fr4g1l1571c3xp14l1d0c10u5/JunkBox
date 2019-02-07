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
                var it = Mecab.RunMecab("wikipedia00.txt").Select(x => x.Split("\t".ToArray(), 2));
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
                // wikipedia記事データから分かち書き用の教師データを作る。
                Console.WriteLine("Create Teaching data.");

                // 教師データを用いて線形SVMで分かち書きを学習
                Console.WriteLine("Learning SVM: Start");
                var wseg = new WordSegmenter();
                var result = wseg.TrainFromFiles(500, "wikipedia00.txt", "wikipedia01.txt");
                // 学習結果を表示
                Console.WriteLine(result);

                // 教師データを用いて構造化パーセプトロンで品詞を学習
                Console.WriteLine("Learning Structured perceptron: Start");
                StructuredPerceptron sp = null;
                {
                    var teatures = new[] { "wikipedia00.txt", "wikipedia01.txt" }.SelectMany(x => Mecab.CreateTrainDataUsingMecabUnidic(x)).Select(x => x.Select(y => Tuple.Create(y.Item1, y.Item2)).ToArray()).ToList();
                    sp = StructuredPerceptron.Train(teatures, 10);

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
                        var ret = wseg.Benchmark(WordSegmenter.CreateTeachingDataFromFile("wikipedia02.txt").ToList());
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
    }

    /// <summary>
    /// 分かち書きクラス
    /// </summary>
    internal class WordSegmenter {
        private LinerSVM<string> svm { get; }
        public WordSegmenter() {
            svm = new LinerSVM<string>();
        }


        /// <summary>
        /// ファイルから教師データを読み取って学習を実行
        /// </summary>
        /// <param name="file"></param>
        /// <param name="epoc"></param>
        /// <returns></returns>
        public TestResult TrainFromFiles(int epoc, params string[] files) {
            List<Tuple<int, Dictionary<string, double>>> teatures = files.SelectMany(x => CreateTeachingDataFromFile(x)).ToList();
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
        /// <param name="datas"></param>
        /// <returns></returns>
        public TestResult Benchmark(List<Tuple<int, Dictionary<string, double>>> datas) {
            return TestResult.Test(svm, datas);
        }

        /// <summary>
        /// 教師データ生成
        /// </summary>
        public static IEnumerable<Tuple<int, Dictionary<string, double>>> CreateTeachingDataFromFile(string file) {
            foreach (var line in Mecab.CreateTrainDataUsingMecabUnidic(file)) {
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
    }

    /// <summary>
    /// テスト結果
    /// </summary>
    public class TestResult {
        /// <summary>
        /// テスト実行
        /// </summary>
        /// <typeparam name="TFeature"></typeparam>
        /// <param name="svm"></param>
        /// <param name="fvs"></param>
        /// <returns></returns>
        public static TestResult Test<TFeature>(LinerSVM<TFeature> svm, List<Tuple<int, Dictionary<TFeature, double>>> fvs) {
            var truePositive = 0;
            var falsePositive = 0;
            var falseNegative = 0;
            var trueNegative = 0;
            foreach (var fv in fvs) {
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
        /// コンストラクタ
        /// </summary>
        /// <param name="truePositive"></param>
        /// <param name="falsePositive"></param>
        /// <param name="falseNegative"></param>
        /// <param name="trueNegative"></param>
        private TestResult(int truePositive, int falsePositive, int falseNegative, int trueNegative) {
            this.TruePositive = truePositive;
            this.FalsePositive = falsePositive;
            this.FalseNegative = falseNegative;
            this.TrueNegative = trueNegative;
        }

        /// <summary>
        /// 正解率(予測結果全体と、答えがどれぐらい一致しているか)
        /// </summary>
        public double Accuracy => (double)(TruePositive + TrueNegative) / (TruePositive + FalsePositive + FalseNegative + TrueNegative);

        /// <summary>
        /// 適合率(予測を正と判断した中で、答えも正のもの)
        /// </summary>
        public double Precision => (double)(TruePositive) / (TruePositive + FalsePositive);

        /// <summary>
        /// 再現率(答えが正の中で、予測が正とされたもの)
        /// </summary>
        public double Recall => (double)(TruePositive) / (TruePositive + FalseNegative);

        /// <summary>
        /// F値(PresicionとRecallの調和平均で、予測精度の評価指標)
        /// </summary>
        public double FMeasure => (2 * Recall * Precision) / (Recall + Precision);

        /// <summary>
        /// 全テストデータの件数
        /// </summary>
        public int Total => TruePositive + FalsePositive + FalseNegative + TrueNegative;

        /// <summary>
        /// 予測が正で答えが正であるデータの件数
        /// </summary>
        public int TruePositive { get; }

        /// <summary>
        /// 予測が負で答えが正であるデータの件数
        /// </summary>
        public int FalsePositive { get; }

        /// <summary>
        /// 予測が正で答えが負であるデータの件数
        /// </summary>
        public int FalseNegative { get; }

        /// <summary>
        /// 予測が負で答えが負であるデータの件数
        /// </summary>
        public int TrueNegative { get; }

        /// <summary>
        /// レポート文字列を生成
        /// </summary>
        /// <returns></returns>
        public override string ToString() {
            var sb = new StringBuilder();
            sb.AppendLine($"Total: {Total}");
            sb.AppendLine($"  TP/FP/FN/TN: {TruePositive}/{FalsePositive}/{FalseNegative}/{TrueNegative}");
            sb.AppendLine($"  Accuracy   : {Accuracy}");
            sb.AppendLine($"  Precision  : {Precision}");
            sb.AppendLine($"  Recall     : {Recall}");
            sb.AppendLine($"  F-Measure  : {FMeasure}");
            return sb.ToString();
        }

    }

    /// <summary>
    /// 構造化パーセプトロン
    /// </summary>
    public class StructuredPerceptron {
        private List<string> partsOfSpeechLabels;
        private Dictionary<string, double> final_weight;

        public StructuredPerceptron(List<string> partsOfSpeechLabels, Dictionary<string, double> final_weight) {
            this.partsOfSpeechLabels = partsOfSpeechLabels;
            this.final_weight = final_weight;
        }

        internal static List<string> GetPartsOfSpeechLabels(List<Tuple<string, string>[]> data) {
            return data.SelectMany(x => x.Select(y => y.Item2)).Distinct().ToList();

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
            var w = (index + 0) < sentence.Length ? sentence[index + 0] : "EOS";
            var w_prev = (index - 1) >= 0 ? sentence[index - 1] : "";
            var w_next = (index + 1) < sentence.Length ? sentence[index + 1] : "";
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

        private static Tuple<string, string>[] argmax(string[] sentence_, Dictionary<string, double> weight, List<string> partsOfSpeechLabels) {
            var best_edge = forward_step(sentence_, weight, partsOfSpeechLabels);
            return backward_step(sentence_, best_edge);
        }

        private static Tuple<string, string>[] backward_step(string[] sentence, Dictionary<string, string> best_edge) {
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
        /// <param name="next">遷移先の品詞</param>
        /// <param name="prev">遷移元の品詞</param>
        /// <param name="best_score">スコア表</param>
        /// <param name="best_edge">遷移辺</param>
        private static void forwars_step_one(string[] sentence, Dictionary<string, double> weight, int w_idx, string w_idx_prev, string next, string prev, Dictionary<string, double> best_score, Dictionary<string, string> best_edge) {
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
        private static Dictionary<string, string> forward_step(string[] sentence, Dictionary<string, double> weight, List<string> partsOfSpeechLabels) {
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
            for (var w_idx = 1; w_idx < sentence.Length; w_idx++) {
                foreach (var prev in partsOfSpeechLabels) {
                    var w_idx_prev = $"{w_idx} {prev}";
                    foreach (var next in partsOfSpeechLabels) {
                        forwars_step_one(sentence, weight, w_idx, w_idx_prev, next, prev, best_score, best_edge);
                    }
                }
            }
            {
                var next = eos;
                var w_idx = sentence.Length;
                foreach (var prev in partsOfSpeechLabels) {
                    var w_idx_prev = $"{w_idx} {prev}";
                    forwars_step_one(sentence, weight, w_idx, w_idx_prev, next, prev, best_score, best_edge);
                }
            }
            return best_edge;
        }

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

        public static void learn(Dictionary<string, double> weight, Dictionary<string, double> cum_weight, Tuple<string, string>[] sentence, Tuple<string, string>[] predict_sentence, double n, List<string> pos_labels) {
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

        public static string pos_labels_str(Tuple<string, string>[] sentence) {
            return string.Join(", ", sentence.Select(x => x.Item2));
        }

        public Tuple<string, string>[] Predict(string[] gold) {
            return argmax(gold, this.final_weight, this.partsOfSpeechLabels);
        }
        public static StructuredPerceptron Train(List<Tuple<string, string>[]> train_data, int step) {
            var partsOfSpeechLabels = StructuredPerceptron.GetPartsOfSpeechLabels(train_data);
            var weight = new Dictionary<string, double>();
            var cum_weight = new Dictionary<string, double>();
            var n = 1;
            for (var iter = 0; iter < step; iter++) {
                foreach (var gold in train_data) {
                    var predict = argmax(gold.Select(x => x.Item1).ToArray(), weight, partsOfSpeechLabels);
                    // 正解と一致したら重みベクトルは更新しない
                    if (pos_labels_str(gold) != pos_labels_str(predict)) {
                        learn(weight, cum_weight, gold, predict, n, partsOfSpeechLabels);
                        n++;
                    }
                }
            }
            var final_weight = get_final_weight(weight, cum_weight, n);
            return new StructuredPerceptron(partsOfSpeechLabels, final_weight);
        }
    }

    /// <summary>
    /// 専門用語抽出
    /// </summary>
    public static class TermExtractor {
        /// <summary>
        /// 形態素解析結果から複合語を返す
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static List<string> Find(List<Tuple<string, string[]>> data) {
            var cmp_nouns = new List<string>();
            var must = false;  // 次の語が名詞でなければならない場合は真
            var terms = new List<string>(); // 複合語リスト作成用の作業用配列
                                            // 単名詞の連結処理
            foreach (var noun_value in data) {
                var noun = noun_value.Item1;
                var value = noun_value.Item2;
                var part_of_speach = value[0];
                var cl_1 = value[1];
                var cl_2 = value[2];
                if ((part_of_speach == "名詞" && cl_1 == "一般") ||
                        (part_of_speach == "名詞" && cl_1 == "接尾" && cl_2 == "一般") ||
                        (part_of_speach == "名詞" && cl_1 == "接尾" && cl_2 == "サ変接続") ||
                        (part_of_speach == "名詞" && cl_1 == "固有名詞") ||
                        (part_of_speach == "記号" && cl_1 == "アルファベット") ||
                        (part_of_speach == "名詞" && cl_1 == "サ変接続" && !Regex.IsMatch(@"[!\""#$%&'\(\)*+,-./{\|}:;<>\[\]\?!]$", noun))) {
                    terms.Add(noun);
                    must = false;
                } else if (
                            (part_of_speach == "名詞" && cl_1 == "形容動詞語幹") ||
                            (part_of_speach == "名詞" && cl_1 == "ナイ形容詞語幹")) {
                    terms.Add(noun);
                    must = true;
                } else if (part_of_speach == "名詞" && cl_1 == "接尾" && cl_2 == "形容動詞語幹") {
                    terms.Add(noun);
                    must = true;
                } else if (part_of_speach == "動詞") {
                    terms.Clear();
                } else {
                    if (!must) {
                        _increase(cmp_nouns, terms);
                    }
                    must = false;
                    terms.Clear();
                }
            }

            if (!must) {
                _increase(cmp_nouns, terms);
            }
            return cmp_nouns;

        }
        private static HashSet<string> SETSUBI = new HashSet<string>() { "など", "ら", "上", "内", "型", "間", "中", "毎" };
        private static void _increase(List<string> cmp_nouns, List<string> terms) {
            // 専門用語リストへ、整形して追加
            // 語頭の不要な語の削除
            if (terms.Count > 1) {
                if (terms[0] == "本") {
                    terms.RemoveAt(0);
                }
            }
            if (terms.Any()) {
                // 語尾の余分な語の削除
                var end = terms.Last();
                if (SETSUBI.Contains(end) || Regex.IsMatch(@"\s+$", end)) {
                    terms.RemoveAt(terms.Count - 1);
                }
            }
            if (terms.Any()) {
                var cmp_noun = string.Join(" ", terms);
                cmp_nouns.Add(cmp_noun);
                terms.Clear();
            }
        }
    }
}
