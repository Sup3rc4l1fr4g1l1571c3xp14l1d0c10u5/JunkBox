using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace svm_fobos {
    /// <summary>
    /// オンライン学習可能な線形サポートベクターマシン（実装はFOBOS）
    /// </summary>
    /// <typeparam name="TFeature">特徴を示す型</typeparam>
    public class LinerSVM<TFeature> {

        /// <summary>
        /// 重みベクトル
        /// </summary>
        private Dictionary<TFeature, double> Weight { get; }

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
        /// 重みに対してL1正則化を適用し、重みが０になった特徴を削除する
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

    };
}
