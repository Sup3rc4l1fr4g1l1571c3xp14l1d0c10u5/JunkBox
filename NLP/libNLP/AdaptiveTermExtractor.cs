using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
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
        /// 構造化パーセプトロン
        /// </summary>
        private StructuredPerceptron sp;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public AdaptiveTermExtractor() {
            this.sp = null;
        }

        /// <summary>
        /// 教師データを元に学習を行う
        /// </summary>
        /// <param name="epoch">学習回数</param>
        /// <param name="positive">教師データ</param>
        public void Learn(int epoch, IEnumerable<Tuple<string, string[]>[]> positive) {
            var dics = Mecab.Run(@"C:\Users\whelp\Desktop\新しいフォルダー\libnlp\TestData\worddic.txt").Select(Mecab.ParseLine).Split(y => y.Item1 == "EOS").ToList();
            var fvs1 = new List<StructuredPerceptron.TeatureData[]>();
            foreach (var line in positive.Where(x => x.Any())) {
                //var features = line.Select((x, i) => new string[] { x.Item1, x.Item2[0], x.Item2[1] }).ToList();
                var teatures = new List<StructuredPerceptron.TeatureData>();

                for (var i = 0; i < line.Length; ) {
                    int n = IoB(line, i, dics);
                    if (n > 0) {
                        teatures.Add(new StructuredPerceptron.TeatureData() { Label="B", Features = new [] {line[i].Item1,line[i].Item2[0]}});
                        for (var j = 1; j < n; j++) {
                            teatures.Add(new StructuredPerceptron.TeatureData() { Label="I", Features = new [] {line[i+j].Item1,line[i+j].Item2[0]}});
                        }    
                        i += n;
                    }
                    else {
                        teatures.Add(new StructuredPerceptron.TeatureData() { Label="O", Features = new [] {line[i].Item1,line[i].Item2[0]}});
                        i++;
                    }
                }
                fvs1.Add(teatures.ToArray());
            }

            sp = StructuredPerceptron.Train(new HashSet<string>() {"I", "O", "B"}, fvs1, 10, new PosTaggingCalcFeature());
            Console.WriteLine(sp.Test(fvs1));
        }

        private int IoB(Tuple<string, string[]>[] line, int i, List<Tuple<string, string[]>[]> dics) {
            for (var j=0;;j++) {
                dics = dics.Where(x => x.Length > i && line.Length > i && x[i].Item1 == line[i].Item1 && x[i].Item2[0] == line[i].Item2[0]).ToList();
                if (dics.Any()) {
                    return j;
                }
            }
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
            var l = line.ToList();
            var features = l.Select((x, i) => new StructuredPerceptron.InputData { Features = new [] {x.Item1, x.Item2[0] }}).ToArray();
            var ret = sp.Predict(features);
            return l.Zip(ret, (x, y) => new[] {x.Item1, y}).ToList();
        }

        /// <summary>
        /// 学習モデルを読み込む
        /// </summary>
        /// <param name="modelPath">学習モデルファイル</param>
        /// <returns></returns>
        public static AdaptiveTermExtractor Load(string modelPath) {
            using (var streamReader = new System.IO.StreamReader(modelPath)) {
                var self = new AdaptiveTermExtractor();
                self.sp = StructuredPerceptron.LoadFromStream(streamReader, new PosTaggingCalcFeature());
                return self;
            }
        }

        /// <summary>
        /// 学習結果を保存する
        /// </summary>
        /// <param name="modelPath">学習モデルファイル</param>
        public void Save(string modelPath) {
            using (var streamWriter = new System.IO.StreamWriter(modelPath)) {
                sp.SaveToStream(streamWriter);
            }
        }
    }
}
