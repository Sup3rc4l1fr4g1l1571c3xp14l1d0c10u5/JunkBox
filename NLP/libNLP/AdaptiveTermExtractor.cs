using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using libNLP.Extentions;

namespace libNLP {
    /// <summary>
    /// 機械学習による適応型の専門用語（複合語）抽出
    /// <para>
    /// 専門用語辞書から特徴ベクトルを生成して機械学習させて作成したモデルを用いて専門用語（複合語）を抽出する。
    /// 学習セットや特徴ベクトルの作り方に強く依存する。
    /// </para>
    /// </summary>
    public class AdaptiveTermExtractor {
        /// <summary>
        /// 学習器
        /// </summary>
        private LinerSVM<string> svm;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public AdaptiveTermExtractor() {
            this.svm = new LinerSVM<string>();
        }

        /// <summary>
        /// 教師データを元に学習を行う
        /// </summary>
        /// <param name="epoch">学習回数</param>
        /// <param name="positive">教師データ</param>
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
            fvs1.Add(CreateTeature(-1, new string[][] { (string[])null, new string[] { ":", "補助記号" }, (string[])null }));
            fvs1.Add(CreateTeature(-1, new string[][] { (string[])null, new string[] { "（", "補助記号" }, (string[])null }));
            fvs1.Add(CreateTeature(-1, new string[][] { (string[])null, new string[] { "）", "補助記号" }, (string[])null }));
            fvs1.Add(CreateTeature(-1, new string[][] { (string[])null, new string[] { "、", "補助記号" }, (string[])null }));
            fvs1.Add(CreateTeature(-1, new string[][] { (string[])null, new string[] { "。", "補助記号" }, (string[])null }));
            fvs1.Add(CreateTeature(-1, new string[][] { (string[])null, new string[] { "や", "助詞" }, (string[])null }));
            fvs1.Add(CreateTeature(-1, new string[][] { (string[])null, new string[] { "の", "助詞" }, (string[])null }));
            fvs1.Add(CreateTeature(-1, new string[][] { (string[])null, new string[] { "を", "助詞" }, (string[])null }));

            for (var i = 0; i < epoch; i++) {
                foreach (var fv in fvs1) {
                    svm.Train(fv.Item2, fv.Item1, 0.06);
                }
                svm.Regularize(0.005);
                Console.WriteLine(svm.Test(fvs1));
            }
        }

        /// <summary>
        /// 教師ベクトルを生成
        /// </summary>
        /// <param name="label">ラベル(+1|-1)</param>
        /// <param name="pair">trigram</param>
        /// <returns>教師ベクトル</returns>
        private static Tuple<int, Dictionary<string, double>> CreateTeature(int label, string[][] pair) {
            return CreateTeature(label, pair[0], pair[1], pair[2]);
        }

        /// <summary>
        /// 教師ベクトルを生成
        /// </summary>
        /// <param name="label">ラベル(+1|-1)</param>
        /// <param name="first">trigramの第1要素</param>
        /// <param name="second">trigramの第2要素</param>
        /// <param name="third">trigramの第3要素</param>
        /// <returns>教師ベクトル</returns>
        private static Tuple<int, Dictionary<string, double>> CreateTeature(int label, string[] first, string[] second, string[] third) {

            return Tuple.Create(label, CreateFeature(first, second, third));
        }

        /// <summary>
        /// 特徴ベクトルを生成
        /// </summary>
        /// <param name="pair">trigram</param>
        /// <returns></returns>
        private static Dictionary<string, double> CreateFeature(string[][] pair) {
            return CreateFeature(pair[0], pair[1], pair[2]);
        }

        /// <summary>
        /// 特徴ベクトルを生成
        /// </summary>
        /// <param name="first">trigramの第1要素</param>
        /// <param name="second">trigramの第2要素</param>
        /// <param name="third">trigramの第3要素</param>
        /// <returns></returns>
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
                var ret = svm.Predict(fv);
                if (ret > 0) {
                    bug.Add(pair[1][0]);
                } else {
                    bug.Add(null);
                }
                idx++;
            }
            return bug.Split(x => x == null).Where(x => x.Any()).ToList();
        }

        /// <summary>
        /// 学習モデルを読み込む
        /// </summary>
        /// <param name="modelPath">学習モデルファイル</param>
        /// <returns></returns>
        public static AdaptiveTermExtractor Load(string modelPath) {
            using (var streamReader = new System.IO.StreamReader(modelPath)) {
                var self = new AdaptiveTermExtractor();
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
