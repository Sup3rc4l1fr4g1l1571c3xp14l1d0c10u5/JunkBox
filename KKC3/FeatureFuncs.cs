using System.Collections.Generic;

namespace KKC3 {
    /// <summary>
    /// ノードから特徴を一つ算出するデリゲート
    /// </summary>
    /// <param name="nodes">対象のノード列</param>
    /// <param name="index">ノード列の指定ノードを示す添え字</param>
    /// <returns></returns>
    public delegate string NodeFeatures(IReadOnlyList<Node> nodes, int index);

    /// <summary>
    /// エッジから特徴を一つ算出するデリゲート
    /// </summary>
    /// <param name="prevNode">ノードに繋がっているひとつ前のノード</param>
    /// <param name="node">ノード</param>
    /// <returns></returns>
    public delegate string EdgeFeatures(Node prevNode, Node node);

    /// <summary>
    /// ノードや辺から特徴ベクトルを算出するクラス
    /// </summary>
    public class FeatureFuncs {

        public List<NodeFeatures> NodeFeatures { get; }
        public List<EdgeFeatures> EdgeFeatures { get; }
        public FeatureFuncs() {
            NodeFeatures = new List<NodeFeatures>();
            EdgeFeatures = new List<EdgeFeatures>();
        }
    }
}