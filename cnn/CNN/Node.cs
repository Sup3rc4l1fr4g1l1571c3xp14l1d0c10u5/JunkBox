using System;
using System.Collections.Generic;
using System.Linq;
using LibPredicate;

namespace CNN {
    public class Node {
        /// <summary>
        /// ノード名
        /// </summary>
        public Tuple<int,int> Name { get; }

        /// <summary>
        /// 入力辺
        /// </summary>
        public List<Edge> InputEdges { get; } = new List<Edge>();

        /// <summary>
        /// 出力辺
        /// </summary>
        public List<Edge> OutputEdges { get; } = new List<Edge>();

        /// <summary>
        /// 入力値の合計
        /// </summary>
        private double TotalInputValue { get; set; } = double.NaN;

        /// <summary>
        /// 出力値
        /// </summary>
        public double OutputValue { get; set; } = double.NaN;

        /// <summary>
        /// 誤差
        /// </summary>
        public double Error { get; private set; } = double.NaN;

        /// <summary>
        /// 乱数器
        /// </summary>
        private static Random Randomizer { get; } = new Random();

        /// <summary>
        /// 活性化関数
        /// </summary>
        private ActivationFunctions.IActivationFunction ActivationFunction { get; } = new ActivationFunctions.SigmoidFunction();

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="name"></param>
        public Node(Tuple<int,int> name) {
            Name = name;
        }

        /// <summary>
        /// 接続関数
        /// </summary>
        /// <param name="outputTarget"></param>
        /// <returns></returns>
        public Edge Connect(Node outputTarget) {
            var edge = new Edge() { Input = this, Output = outputTarget };
            outputTarget.InputEdges.Add(edge);
            OutputEdges.Add(edge);
            return edge;
        }

        /// <summary>
        /// 出力を計算
        /// </summary>
        public void CalcForward() {
            if (InputEdges.Count == 0) { return; }
            TotalInputValue = InputEdges.Sum((edge) => edge.Input.OutputValue * edge.Weight);
            OutputValue = ActivationFunction.Activate(TotalInputValue);
        }

        /// <summary>
        /// 重みを正規乱数で初期化
        /// </summary>
        public void InitWeight() {
            InputEdges.ForEach(edge => edge.Weight = Randomizer.NextNormal());
        }

        /// <summary>
        /// 誤差計算（出力層向け）
        /// </summary>
        public void CalcError(double t) {
            Error = t - OutputValue;
        }

        /// <summary>
        /// 誤差計算（隠れ層）
        /// </summary>
        public void CalcError() {
            Error = OutputEdges.Sum((edge) => edge.Weight * edge.Output.Error);
        }

        /// <summary>
        /// 重みの更新
        /// </summary>
        /// <param name="alpha"></param>
        public void UpdateWeight(double alpha) {
            var k = alpha * Error * ActivationFunction.Derivate(OutputValue);
            foreach (var edge in InputEdges) {
                // 調整値算出
                edge.Weight += k * edge.Input.OutputValue;
            }
        }
    }
}