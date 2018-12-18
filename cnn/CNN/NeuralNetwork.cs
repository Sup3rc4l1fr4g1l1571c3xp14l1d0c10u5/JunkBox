using System.Collections.Generic;
using System.Linq;
using LibPredicate;

namespace CNN {
    public class NeuralNetwork {

        /// <summary>
        /// NNを構成する層
        /// </summary>
        public List<Layer> Layers { get; } = new List<Layer>();

        /// <summary>
        /// 入力層
        /// </summary>
        private Layer InputLayer { get { return Layers.First(); } }

        /// <summary>
        /// 出力層
        /// </summary>
        private Layer OutputLayer { get { return Layers.Last(); } }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="inputLayerSize">入力層のノード数</param>
        /// <param name="hiddenLayerSizes">隠れ層の個々のノード数</param>
        /// <param name="outputLayerSize">出力層のノード数</param>
        public NeuralNetwork(int inputLayerSize, int[] hiddenLayerSizes, int outputLayerSize) {
            // 入力層
            AddLayer(inputLayerSize);
            // 隠れ層
            hiddenLayerSizes.ForEach(AddLayer);
            // 出力層
            AddLayer(outputLayerSize);
            // 重みの初期化
            InitWeight();
        }

        /// <summary>
        /// Load処理用コンストラクタ
        /// </summary>
        private NeuralNetwork() {
        }

        /// <summary>
        /// 全レイヤーのノードの重みを初期化
        /// </summary>
        private void InitWeight() {
            Layers.ForEach(x => x.InitWeight());
        }

        /// <summary>
        /// 層を作成し隣の層と全結合
        /// </summary>
        /// <param name="numberOfNodes"></param>
        private void AddLayer(int numberOfNodes) {
            var newLayer = new Layer(Layers.Count, numberOfNodes);
            if (Layers.Count > 0) {
                Layers[Layers.Count - 1].ConnectDensely(newLayer);
            }
            Layers.Add(newLayer);
        }

        /// <summary>
        /// 入力層にデータを入力後、出力層に向けて計算を行う。（識別処理）
        /// </summary>
        /// <param name="inputData"></param>
        public NeuralNetwork CalcForward(double[] inputData) {
            InputLayer.SetInputData(inputData);
            Layers.ForEach(x => x.CalcForward());
            return this;
        }

        /// <summary>
        /// 出力層から認識結果を取得する
        /// </summary>
        /// <returns></returns>
        public int GetMaxOutput() {
            return OutputLayer.Nodes.IndexOfMax(x => x.OutputValue);
            //// 出力層のノードのうち、最大値を持つノードのインデックスを返す
            //var outputLayerNodes = OutputLayer.Nodes;
            //var max = -1;
            //var maxValue = double.MinValue;
            //for (var i = 0; i < outputLayerNodes.Length; i++) {
            //    if (maxValue < outputLayerNodes[i].OutputValue) {
            //        max = i;
            //        maxValue = outputLayerNodes[i].OutputValue;
            //    }
            //}
            //return max;
        }

        /// <summary>
        /// 重みを更新
        /// </summary>
        /// <param name="alpha"></param>
        /// <returns></returns>
        public NeuralNetwork UpdateWeight(double alpha) {
            Layers.ForEach(x => x.UpdateWeight(alpha));
            return this;
        }

        /// <summary>
        /// 誤差計算
        /// </summary>
        /// <param name="trainData"></param>
        /// <returns></returns>
        public NeuralNetwork CalcError(double[] trainData) {
            OutputLayer.Nodes.ForEach((node, i) => node.CalcError(trainData[i]));

            for (var i = Layers.Count - 2; i >= 0; i--) {
                Layers[i].Nodes.ForEach(x => x.CalcError());
            }
            return this;
        }

        /// <summary>
        /// 学習結果保存
        /// </summary>
        /// <param name="fn"></param>
        public void SaveWeight(string fn) {
            using (var fs = new System.IO.FileStream(fn, System.IO.FileMode.OpenOrCreate))
            using (var bw = new System.IO.BinaryWriter(fs)) {
                bw.Write(Layers.Count);
                Layers.ForEach((x) => bw.Write(x.Nodes.Length));
                Layers.ForEach((x) => x.Nodes.ForEach((y) => y.InputEdges.ForEach((z) => bw.Write(z.Weight))));
            }
        }

        /// <summary>
        /// 学習結果読み込み
        /// </summary>
        /// <param name="fn"></param>
        /// <returns></returns>
        public static NeuralNetwork LoadWeight(string fn) {
            using (var fw = new System.IO.FileStream(fn, System.IO.FileMode.Open))
            using (var br = new System.IO.BinaryReader(fw)) {
                var nn = new NeuralNetwork();
                var layersCount = br.ReadInt32();
                layersCount.Times().ToList().ForEach(x => nn.AddLayer(br.ReadInt32()));
                nn.Layers.ForEach((x) => x.Nodes.ForEach((y) => y.InputEdges.ForEach((z) => z.Weight = br.ReadDouble())));
                return nn;
            }
        }
    }
}