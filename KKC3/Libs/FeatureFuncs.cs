using System;
using System.Collections.Generic;

namespace KKC3 {
    public struct NodeFeature : IEquatable<NodeFeature>
    {
        public NodeFeature(string first, string second, string third) {
            this.First = first;
            this.Second = second;
            this.Third = third;
            this.hashCode = first.GetHashCode() ^ second.GetHashCode() ^ third.GetHashCode();
        }

        public string First { get; }
        public string Second { get; }
        public string Third { get; }
        private int hashCode { get; }
        public override int GetHashCode() {
            return this.hashCode;
        }

        public override bool Equals(object obj) {
            if (obj == null || (obj is NodeFeature) == false) {
                return false;
            }
            return Equals((NodeFeature)obj);
        }

        public bool Equals(NodeFeature other) {
            return other.First == First && other.Second == Second && other.Third == Third;
        }

        public string Serialize() {
            return string.Join("/", new[] { First, Second, Third });
        }
    }
    public struct EdgeFeature : IEquatable<EdgeFeature>
    {
        public EdgeFeature(string first, string second) {
            this.First = first;
            this.Second = second;
            this.hashCode = first.GetHashCode() ^ second.GetHashCode();
        }

        public string First { get; }
        public string Second { get; }
        private int hashCode { get; }
        public override int GetHashCode() {
            return this.hashCode;
        }

        public override bool Equals(object obj) {
            if (obj == null || (obj is EdgeFeature) == false) {
                return false;
            }
            return Equals((EdgeFeature)obj);
        }

        public bool Equals(EdgeFeature other) {
            return other.First == First && other.Second == Second;
        }

        public string Serialize() {
            return string.Join("/", new[] { First, Second });
        }
    }

    /// <summary>
    /// ノードから特徴を一つ算出するデリゲート
    /// </summary>
    /// <param name="nodes">対象のノード列</param>
    /// <param name="index">ノード列の指定ノードを示す添え字</param>
    /// <returns></returns>
    public delegate NodeFeature NodeFeatureDelegate(IReadOnlyList<Node> nodes, int index);

    /// <summary>
    /// エッジから特徴を一つ算出するデリゲート
    /// </summary>
    /// <param name="prevNode">ノードに繋がっているひとつ前のノード</param>
    /// <param name="node">ノード</param>
    /// <returns></returns>
    public delegate EdgeFeature EdgeFeatureDelegate(Node prevNode, Node node);

    /// <summary>
    /// ノードや辺から特徴ベクトルを算出するクラス
    /// </summary>
    public class FeatureFuncs {

        public List<NodeFeatureDelegate> NodeFeatures { get; }
        public List<EdgeFeatureDelegate> EdgeFeatures { get; }
        public FeatureFuncs() {
            NodeFeatures = new List<NodeFeatureDelegate>();
            EdgeFeatures = new List<EdgeFeatureDelegate>();
        }
    }
}