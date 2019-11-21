﻿using System.Collections.Generic;

namespace KKC3 {
    /// <summary>
    /// ビタビデコーダー
    /// </summary>
    public class Decoder
    {
        private FeatureFuncs FeatureFuncs { get; }
        private double Penalty { get; }

        public Decoder(FeatureFuncs featureFuncs, double penalty = 0.0) {
            FeatureFuncs = featureFuncs;
            Penalty = penalty;
        }

        /// <summary>
        /// ノード node は正解ノードか？
        /// （ノードの値がノードの位置にある教師データの値と一致しているかで判定）
        /// </summary>
        /// <param name="node"></param>
        /// <param name="gold"></param>
        /// <returns></returns>
        private static bool IsCorrectNode(Node node, IReadOnlyList<Node> gold) {
            var correctNode = gold[node.EndPos];
            return (correctNode != null && node.Word == correctNode.Word);
        }

        /// <summary>
        /// エッジ (prevNode -> node) は正解エッジか？
        /// （ノード prevNode と node がどちらも正解ノードであるかで判定）
        /// </summary>
        /// <param name="prevNode"></param>
        /// <param name="node"></param>
        /// <param name="gold"></param>
        /// <returns></returns>
        private static bool IsCorrectEdge(Node prevNode, Node node, IReadOnlyList<Node> gold) {
            return IsCorrectNode(node, gold) && IsCorrectNode(prevNode, gold);
        }

        /// <summary>
        /// ノード node のスコアを求める
        /// </summary>
        /// <param name="nodes">スコアを求める対象のノード列</param>
        /// <param name="index">ノード列の現在ノードを示す添え字</param>
        /// <param name="gold">正解を示すノード列</param>
        /// <param name="w">特徴の重み表</param>
        /// <returns></returns>
        public double GetNodeScore(IReadOnlyList<Node> nodes, int index, IReadOnlyList<Node> gold, IReadOnlyDictionary<string, double> w) {
            var score = 0.0;
            if (gold != null && IsCorrectNode(nodes[index], gold)) {
                // 構造化SVMのメインアイディアは
                //「正解のパスにペナルティ（もしくは、不正解のパスにボーナス）を与えた状態でも正しく分類できるようにしよう」
                // なので、正解のパスにペナルティ（もしくは、不正解のパスにボーナス）を課す
                score -= Penalty;
            }
            foreach (var func in FeatureFuncs.NodeFeatures) {
                var feature = func(nodes, index);
                double v;
                if (w.TryGetValue(feature, out v)) {
                    score += v;
                }
            }
            return score;
        }

        /// <summary>
        /// エッジ prevNode -> node のスコアを求める
        /// </summary>
        /// <param name="prevNode">エッジの元ノード</param>
        /// <param name="node">エッジの先ノード</param>
        /// <param name="gold">正解を示すノード列</param>
        /// <param name="w">特徴の重み表</param>
        /// <returns></returns>
        public double GetEdgeScore(Node prevNode, Node node, IReadOnlyList<Node> gold, IReadOnlyDictionary<string, double> w) {
            var score = 0.0;
            if (gold != null && IsCorrectEdge(prevNode, node, gold)) {
                // 構造化SVMのメインアイディアは
                //「正解のパスにペナルティ（もしくは、不正解のパスにボーナス）を与えた状態でも正しく分類できるようにしよう」
                // なので、正解のパスにペナルティを課す
                score -= Penalty;
            }
            foreach (var func in FeatureFuncs.EdgeFeatures) {
                var feature = func(prevNode, node);
                double v;
                if (w.TryGetValue(feature, out v)) {
                    score += v;
                }
            }
            return score;
        }

        /// <summary>
        /// ビタビアルゴリズム
        /// </summary>
        /// <param name="graph">グラフ</param>
        /// <param name="w">特徴量の重み</param>
        /// <param name="gold">教師データ列</param>
        /// <returns></returns>
        public List<Entry> Viterbi(WordLattice graph, IReadOnlyDictionary<string, double> w, IReadOnlyList<Node> gold = null) {

            //前向き
            foreach (var nodes in graph.Nodes) {
                for (var i = 0; i < nodes.Count; i++) {
                    var node = nodes[i];
                    if (node.IsBos) { continue; }
                    node.Score = -1000000.0;
                    var nodeScoreCache = GetNodeScore(nodes, i, gold, w);

                    foreach (var prevNode in graph.GetPrevs(node)) {
                        var tmpScore = prevNode.Score + GetEdgeScore(prevNode, node, gold, w) + nodeScoreCache;
                        if (tmpScore >= node.Score) {
                            node.Score = tmpScore;
                            node.Prev = prevNode;
                        }
                    }
                }
            }

            //後ろ向き
            {
                var result = new List<Entry>();
                var node = graph.Eos.Prev;
                while (!node.IsBos) {
                    result.Add(new Entry(node.Read, node.Word, node.Features));
                    node = node.Prev;
                }
                result.Reverse();
                return result;
            }
        }
    }

}
