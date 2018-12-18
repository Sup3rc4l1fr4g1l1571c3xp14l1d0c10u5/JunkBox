using System;
using System.Linq;
using LibPredicate;

namespace CNN {
    public class Layer {
        /// <summary>
        /// レイヤID
        /// </summary>
        public int LayerId { get; }

        /// <summary>
        /// ノード
        /// </summary>
        public Node[] Nodes { get; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="layerId"></param>
        /// <param name="numNodes"></param>
        public Layer(int layerId, int numNodes) {
            LayerId = layerId;
            Nodes = numNodes.Times().Select(id => new Node(Tuple.Create(layerId, id))).ToArray();
        }

        /// <summary>
        /// ノードを全結合させる
        /// </summary>
        /// <param name="rightLayer"></param>
        public void ConnectDensely(Layer rightLayer) {
            foreach (var node in Nodes) {
                foreach (var nextNode in rightLayer.Nodes) {
                    node.Connect(nextNode);
                }
            }
        }

        /// <summary>
        /// ノードの重みを初期化
        /// </summary>
        public void InitWeight() {
            Nodes.ForEach(x => x.InitWeight());
        }

        /// <summary>
        /// ノードにデータを入力
        /// </summary>
        public void SetInputData(double[] input) {
            Nodes.ForEach((node, i) => node.OutputValue = input[i]);
        }

        /// <summary>
        /// 出力を計算
        /// </summary>
        public void CalcForward() {
            Nodes.AsParallel().ForAll(x => x.CalcForward());
        }

        /// <summary>
        /// 重みを更新
        /// </summary>
        public void UpdateWeight(double alpha) {
            Nodes.ForEach(x => x.UpdateWeight(alpha));
        }

    }
}