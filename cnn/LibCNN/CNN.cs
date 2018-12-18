using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics.LinearAlgebra.Double;
using MathNet.Numerics.LinearAlgebra;
using LibPredicate;

namespace LibCNN
{
    public class CNN
    {
        /// <summary>
        /// 各レイヤーのノード数
        /// </summary>
        private int[] Layers { get; }

        /// <summary>
        /// 各ノードの出力（行列形式）
        /// </summary>
        private Matrix<double>[] Outputs { get; }

        /// <summary>
        /// 各ノードの出力との誤差（行列形式）
        /// </summary>
        private Matrix<double>[] Errors { get; }

        /// <summary>
        /// 辺の重み（行列形式）
        /// </summary>
        private Matrix<double>[] Weights { get; }

        /// <summary>
        /// 出力層の値
        /// </summary>
        public double[] Output { get { return Outputs.Last().Row(0).ToArray(); } }

        /// <summary>
        /// 活性化関数
        /// </summary>
        private static ActivationFunctions.IActivationFunction activationFunction = new ActivationFunctions.SigmoidFunction();

        /// <summary>
        /// 単純ニューラルネットワークの実装
        /// </summary>
        /// <param name="input"></param>
        /// <param name="output"></param>
        /// <param name="hidden"></param>
        public CNN(int input, int output, params int[] hidden)
        {
            Layers = new[] { input }.Concat(hidden).Concat(new[] { output }).ToArray();
            Outputs = Layers.Select(x => (Matrix<double>)DenseMatrix.CreateDiagonal(1, x, 0)).ToArray();
            Errors = Layers.Select(x => (Matrix<double>)DenseMatrix.CreateDiagonal(1, x, 0)).ToArray();
            var rand = new Random();
            Weights = Layers.each_cons(2).Select((x) => x.ToArray()).Select(x => (Matrix<double>)DenseMatrix.Create(x[0], x[1], (a, b) => rand.NextNormal())).ToArray();
        }

        /// <summary>
        /// 単純ニューラルネットワークの実装
        /// </summary>
        /// <param name="layers"></param>
        /// <param name="matrices"></param>
        private CNN(int[] layers, IEnumerable<double[][]> matrices)
        {
            Layers = layers.ToArray();
            Outputs = Layers.Select(x => (Matrix<double>)DenseMatrix.CreateDiagonal(1, x, 0)).ToArray();
            Errors = Layers.Select(x => (Matrix<double>)DenseMatrix.CreateDiagonal(1, x, 0)).ToArray();
            Weights = matrices.Select(x => DenseMatrix.OfRowArrays(x).Transpose()).ToArray();
        }

        /// <summary>
        /// 識別を実行
        /// </summary>
        /// <param name="data"></param>
        public CNN Predict(double[] data)
        {
            Outputs[0].SetRow(0, data);
            for (var i = 1; i < Layers.Length; i++)
            {
                var tmp = (Outputs[i - 1] * Weights[i - 1]);
                tmp.SetRow(0, activationFunction.Activate(tmp.Row(0).ToArray()));
                Outputs[i] = tmp;
            }
            return this;
        }

        /// <summary>
        /// 学習を実行
        /// </summary>
        /// <param name="data">教師データ</param>
        /// <param name="label">ラベルベクトル</param>
        /// <param name="alpha">学習率</param>
        /// <returns></returns>
        public CNN Train(double[] data, double[] label, double alpha)
        {
            // 識別を実行
            Predict(data);

            // 各層の誤差を算出
            var labelVector = DenseMatrix.Build.Dense(1, Layers.Last(), (r, c) => label[c]);
            Errors[Layers.Length - 1] = (labelVector - Outputs[Layers.Length - 1]).Transpose();
            for (var i = Layers.Length - 2; i >= 0; i--)
            {
                Errors[i] = Weights[i] * Errors[i + 1];
            }

            // 重みの更新
            for (var i = 0; i < Layers.Length - 1; i++)
            {
                var tmp = Outputs[i + 1].Row(0).ToArray().Apply(activationFunction.Derivate).Apply(x => DenseMatrix.OfRowArrays(x));
                Weights[i] += alpha * Outputs[i].Transpose() * Errors[i + 1].Transpose().PointwiseMultiply(tmp);
            }
            return this;
        }

        /// <summary>
        /// 学習したネットワークを保存
        /// </summary>
        /// <param name="fn"></param>
        /// <returns></returns>
        public CNN Save(string fn)
        {
            using (var fw = new System.IO.FileStream(fn, System.IO.FileMode.Open))
            using (var bw = new System.IO.BinaryWriter(fw))
            {
                bw.Write(Layers.Length);
                Layers.ForEach(bw.Write);
                Weights.ForEach(x => x.Enumerate().ToArray().ForEach(bw.Write));
            }
            return this;
        }

        /// <summary>
        /// ネットワークを読み込んでインスタンス作成
        /// </summary>
        /// <param name="fn"></param>
        /// <returns></returns>
        public static CNN Load(string fn)
        {
            using (var fw = new System.IO.FileStream(fn, System.IO.FileMode.Open))
            using (var br = new System.IO.BinaryReader(fw))
            {
                var layersCount = br.ReadInt32();
                var layerSizes = layersCount.Times().Select(x => br.ReadInt32()).ToArray();
                var matrices = layerSizes.each_cons(2).Select(x => x.ToArray()).Select((x) => x[1].Times().Select(_ => x[0].Times().Select(__ => br.ReadDouble()).ToArray()).ToArray());
                return new CNN(layerSizes, matrices);
            }
        }

    }

}
