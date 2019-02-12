using System;
using System.Collections.Generic;
using System.Linq;
using libNLP.Extentions;

namespace libNLP {

    /// <summary>
    /// 線形サポートベクターマシン
    /// <para>
    /// パラメーター最適化にForward Backward Splitting (FOBOS) を用いている。
    /// </para>
    /// </summary>
    /// <typeparam name="TFeature">特徴を示す型</typeparam>
    public class LinerSVM<TFeature> {

        /// <summary>
        /// 学習モデルを示す重みベクトル
        /// </summary>
        private Dictionary<TFeature, double> Weight { get; }

        /// <summary>
        /// 学習モデルをストリームに保存する
        /// </summary>
        /// <param name="streamWriter">保存先ストリーム</param>
        /// <param name="featureDeserializer">特徴情報のシリアライザ</param>
        public void SaveToStream(System.IO.StreamWriter streamWriter, Func<TFeature, string> featureSerializer) {
            foreach (var weight in Weight) {
                streamWriter.Write(featureSerializer(weight.Key));
                streamWriter.Write("\t");
                streamWriter.Write(weight.Value);
            }
        }

        /// <summary>
        /// ストリームから学習モデルを読み取る
        /// </summary>
        /// <param name="streamReader">読み込み元ストリーム</param>
        /// <param name="featureDeserializer">特徴情報のデシリアライザ</param>
        /// <returns></returns>
        public static LinerSVM<TFeature> LoadFromStream(System.IO.StreamReader streamReader, Func<string, TFeature> featureDeserializer) {
            var ret = new LinerSVM<TFeature>();
            string line;
            while ((line = streamReader.ReadLine()) != null) {
                var tokens = line.Trim().Split("\t".ToArray(), 2);
                ret.Weight[featureDeserializer(tokens[0])] = double.Parse(tokens[1]);
            }
            return ret;
        }

        /// <summary>
        /// パラメータ a の値を b 分だけ 0 に近づける。
        /// 近づけて符号が変わったら 0 でクリッピングする
        /// </summary>
        /// <param name="a">パラメータ</param>
        /// <param name="b">近づける量（0以上の実数）</param>
        /// <returns></returns>
        private static double ClipByZero(double a, double b) {
            return Math.Sign(a) * Math.Max(0, Math.Abs(a) - b);
        }

        /// <summary>
        /// 重みに対してL1正則化を適用し、重みが 0 になった特徴を削除する
        /// </summary>
        /// <param name="lambda_hat">正則化の適用度（1未満の正の実数）</param>
        private int L1Regularize(double lambda_hat) {
            var keys = Weight.Keys.ToList();
            var removed = 0;
            foreach (var key in keys) {
                var weight = ClipByZero(Weight[key], lambda_hat);
                if (Math.Abs(weight) <= 0) {
                    Weight.Remove(key);
                    removed++;
                } else {
                    Weight[key] = weight;
                }
            }
            return removed;
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public LinerSVM() {
            this.Weight = new Dictionary<TFeature, double>();
        }

        /// <summary>
        /// 特徴ベクトルの内積を求める
        /// </summary>
        /// <param name="fv1">特徴ベクトル1</param>
        /// <param name="fv2">特徴ベクトル2</param>
        /// <returns>内積</returns>
        private static double DotProduct(Dictionary<TFeature, double> fv1, Dictionary<TFeature, double> fv2) {
            double m = 0;
            foreach (var kv in fv2) {
                var key = kv.Key;
                var x_i = kv.Value;
                m += x_i * fv1.GetValue(key, 0);
            }
            return m;
        }

        /// <summary>
        /// 識別を実行
        /// </summary>
        /// <param name="fv">特徴ベクトル</param>
        /// <returns></returns>
        public double Predict(Dictionary<TFeature, double> fv) {
            return DotProduct(Weight, fv);
        }

        /// <summary>
        /// 学習を実行
        /// </summary>
        /// <param name="fv">特徴ベクトル</param>
        /// <param name="y">正解ラベル(+1/-1)</param>
        /// <param name="eta">学習係数</param>
        public void Train(Dictionary<TFeature, double> fv, int y, double eta) {
            if (Predict(fv) * y < 1.0) {
                // 損失関数 l(w) が最も小さくなる方向へ重みベクトル w を更新する
                foreach (var kv in fv) {
                    var key = kv.Key;
                    var x_i = kv.Value;
                    Weight[key] = Weight.GetValue(key, 0) + y * x_i * eta;
                }
            }
        }

        /// <summary>
        /// 正則化を適用
        /// </summary>
        /// <param name="lambda_hat">正則化の適用度（1未満の正の実数）</param>
        public int Regularize(double lambda_hat) {
            // 更新した重みパラメータをできるだけ動かさずにL1正則化を行う
            return L1Regularize(lambda_hat);
        }

        /// <summary>
        /// テスト実行
        /// </summary>
        /// <param name="fvs"></param>
        /// <returns></returns>
        public TestResult Test(IEnumerable<Tuple<int, Dictionary<TFeature, double>>> fvs) {
            var truePositive = 0;
            var falsePositive = 0;
            var falseNegative = 0;
            var trueNegative = 0;
            foreach (var fv in fvs) {
                var prediction = fv.Item1 >= 0;
                var fact = this.Predict(fv.Item2) >= 0;
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
    };
}
