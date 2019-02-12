using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using libNLP.Extentions;

namespace libNLP {
    /// <summary>
    /// テスト結果
    /// </summary>
    public class TestResult {

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="truePositive">予測が正で答えが正であるデータの件数</param>
        /// <param name="falsePositive">予測が負で答えが正であるデータの件数</param>
        /// <param name="falseNegative">予測が正で答えが負であるデータの件数</param>
        /// <param name="trueNegative">予測が負で答えが負であるデータの件数</param>
        public TestResult(int truePositive, int falsePositive, int falseNegative, int trueNegative) {
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
}
