using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace svm_fobos {
    /// <summary>
    /// 専門用語抽出インタフェース
    /// </summary>
    public interface ITermExtractor {
        List<string[]> Extract(IEnumerable<Tuple<string, string[]>> line);
    }

    /// <summary>
    /// 静的なルールによる専門用語抽出
    /// 東京大学の中川先生の研究中で触れられているもの。
    /// 形態素解析の結果に対して静的なルール適用して専門用語を抽出するため、形態素解析器の出力に応じて属性値を変更しなければならない。
    /// </summary>
    public class StaticTermExtractor : ITermExtractor {
        /// <summary>
        /// 1行分の形態素解析結果から専門用語を抽出する。
        /// </summary>
        /// <param name="line">1行分の形態素解析結果</param>
        /// <returns></returns>
        public List<string[]> Extract(IEnumerable<Tuple<string, string[]>> line) {
            var cmp_nouns = new List<string[]>();
            var must = false;  // 次の語が名詞でなければならない場合は真
            var terms = new List<string>(); // 複合語リスト作成用の作業用配列
                                            // 単名詞の連結処理
            foreach (var noun_value in line) {
                //Console.WriteLine($"{noun_value.Item1}: {noun_value.Item2.Apply(x => string.Join(",", x))}");
                var noun = noun_value.Item1;
                var value = noun_value.Item2;
                var part_of_speach = value[0];
                var cl_1 = value[1];
                var cl_2 = value[2];
                if ((part_of_speach == "名詞" && cl_1 == "普通名詞") ||
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
                        Increase(cmp_nouns, terms);
                    }
                    must = false;
                    terms.Clear();
                }
            }

            if (!must) {
                Increase(cmp_nouns, terms);
            }
            return cmp_nouns;

        }

        private static HashSet<string> SETSUBI = new HashSet<string>() { "など", "ら", "上", "内", "型", "間", "中", "毎" };

        private static void Increase(List<string[]> cmp_nouns, List<string> terms) {
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
                cmp_nouns.Add(terms.ToArray());
                terms.Clear();
            }
        }
    }

    /// <summary>
    /// 機械学習による専門用語抽出
    /// 専門用語辞書を元に機械学習させた結果を用いて専門用語を抽出する。
    /// 学習セットや特徴ベクトルの作り方に強く依存する。
    /// </summary>
    public class AdaptiveTermExtractor : ITermExtractor {
        private LinerSVM<string> svm1;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public AdaptiveTermExtractor() {
            this.svm1 = new LinerSVM<string>();
        }

        /// <summary>
        /// 学習する
        /// </summary>
        /// <param name="epoch"></param>
        /// <param name="positive"></param>
        public void Learn(int epoch, IEnumerable<Tuple<string, string[]>[]> positive) {
            List<Tuple<int, Dictionary<string, double>>> fvs1 = new List<Tuple<int, Dictionary<string, double>>>();

            foreach (var line in positive.Where(x => x.Any())) {
                var features = line.Select((x, i) => new string[] { x.Item1, x.Item2[0], x.Item2[1] }).ToList();

                foreach (var pair in new string[][] { (string[])null }.Concat(features).Concat(new string[][] { (string[])null }).EachCons(3)) {
                    fvs1.Add(CreateTeature(+1, pair));
                }
                fvs1.Add(CreateTeature(-1, new string[][] { (string[])null, (string[])null, features.First() }));
                fvs1.Add(CreateTeature(-1, new string[][] { features.Last(), (string[])null, (string[])null }));
            }

            for (var i = 0; i < epoch; i++) {
                foreach (var fv in fvs1) {
                    svm1.Train(fv.Item2, fv.Item1, 0.06);
                }
                svm1.Regularize(0.005);
                Console.WriteLine(TestResult.Test(svm1, fvs1));
            }
        }

        /// <summary>
        /// 教師ベクトルを生成
        /// </summary>
        /// <param name="label"></param>
        /// <param name="pair"></param>
        /// <returns></returns>
        private static Tuple<int, Dictionary<string, double>> CreateTeature(int label, string[][] pair) {
            return CreateTeature(label, pair[0], pair[1], pair[2]);
        }
        private static Tuple<int, Dictionary<string, double>> CreateTeature(int label, string[] first, string[] second, string[] third) {

            return Tuple.Create(label, CreateFeature(first, second, third));
        }
        /// <summary>
        /// 特徴ベクトルを生成
        /// </summary>
        /// <param name="pair"></param>
        /// <returns></returns>
        private static Dictionary<string, double> CreateFeature(string[][] pair) {
            return CreateFeature(pair[0], pair[1], pair[2]);
        }
        private static Dictionary<string, double> CreateFeature(string[] first, string[] second, string[] third) {

            var ret = new Dictionary<string, double>();
            if (second != null) {
                ret[$"W0 {second[0]}"] = 1;
                ret[$"W1 {second[1]}"] = 1;
                ret[$"F0 {second[0]} {second[1]}"] = 1;
            }
            if (first != null && third != null) {
                ret[$"F1 {first[1]} {third[1]}"] = 1;
            }
            if (third != null && second != null) {
                ret[$"F2 {third[1]} {second[0]}"] = 1;
            }
            if (first != null) {
                ret[$"F3 {first[1]} {first[0]}"] = 1;
            }
            if (third != null) {
                ret[$"F4 {third[1]} {third[0]}"] = 1;
            }

            return ret;
        }

        /// <summary>
        /// 1行分の形態素解析結果から専門用語を抽出する。
        /// </summary>
        /// <param name="line">1行分の形態素解析結果</param>
        /// <returns></returns>
        public List<string[]> Extract(IEnumerable<Tuple<string, string[]>> line) {
            var features = line.Select((x, i) => new string[] { x.Item1, x.Item2[0], x.Item2[1] }).ToList();
            var idx = 0;
            var bug = new List<string>();
            foreach (var pair in new string[][] { (string[])null }.Concat(features).Concat(new string[][] { (string[])null }).EachCons(3)) {
                var fv = CreateFeature(pair);
                var ret = svm1.Predict(fv);
                if (ret > 0) {
                    bug.Add(pair[1][0]);
                } else {
                    bug.Add(null);
                }
                idx++;
            }
            return bug.Split(x => x == null).Where(x => x.Any()).ToList();
        }
    }
}
