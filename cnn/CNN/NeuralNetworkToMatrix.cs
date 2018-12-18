using System.Collections.Generic;

namespace CNN {
    public static class NeuralNetworkToMatrix {

        /// <summary>
        /// 前方向の重み行列を出力する
        /// (x方向が入力元ノード、y方向が出力先ノード)
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        public static double[][,] ToForwardAdjacencyMatrices(this NeuralNetwork self) {
            var adjacencyForwardMatrix = new List<double[,]>();
            for (var i = 0; i < self.Layers.Count - 1; i++) {
                var col = self.Layers[i].Nodes.Length;
                var row = self.Layers[i + 1].Nodes.Length;
                adjacencyForwardMatrix.Add(new double[col, row]);
            }
            for (var i = 0; i < self.Layers.Count - 1; i++) {
                foreach (var node in self.Layers[i].Nodes) {
                    var layer = node.Name.Item1;
                    var from = node.Name.Item2;
                    foreach (var edge in node.OutputEdges) {
                        var to = edge.Output.Name.Item2;
                        adjacencyForwardMatrix[layer][from, to] = edge.Weight;
                    }
                }
            }
            return adjacencyForwardMatrix.ToArray();
        }
    }
}