using System;
using System.Collections.Generic;
using System.Linq;

namespace KKC3 {
    /// <summary>
    /// 構造化SVM学習器
    /// </summary>
    public class StructuredSupportVectorMachine {
        /// <summary>
        /// 重み（特徴⇒重みの疎行列）
        /// </summary>
        protected Dictionary<string, double> Weight { get; }

        /// <summary>
        /// 特徴量抽出
        /// </summary>
        protected FeatureFuncs FeatureFuncs { get; }

        /// <summary>
        /// 冗長出力モード
        /// </summary>
        protected bool VerboseMode { get; }

        /// <summary>
        /// 学習率
        /// </summary>
        private double LearningRate { get; }


        /// <summary>
        /// 学習用のビタビデコーダ（SVM専用）
        /// </summary>
        private Decoder LearningDecoder { get; }

        /// <summary>
        /// 識別用のビタビデコーダ
        /// </summary>
        private Decoder PredictDecoder { get; }

        /// <summary>
        /// 遅延L1正則化用の更新時間と重みの記録
        /// </summary>
        private Dictionary<string, int> LastUpdated { get; }

        /// <summary>
        /// 更新時間カウンタ
        /// </summary>
        private int UpdatedCount { get; set; }

        /// <summary>
        /// λ項（学習率）
        /// </summary>
        private double Lambda { get; }

        /// <summary>
        /// 教師データを単語構造列に変換する
        /// </summary>
        /// <param name="sentence">教師データ</param>
        /// <returns>単語構造列</returns>
        protected List<Node> ConvertToNodes(IList<Entry> sentence) {
            var ret = new List<Node>();
            var bos = new Node(0, "", "", "BOS");
            ret.Add(bos);
            var i = 0;
            var prev = bos;

            foreach (var x in sentence) {
                i += x.Read.Length;
                var node = new Node(i, x.Word, x.Read,x.Features);
                node.Prev = prev;
                ret.Add(node);
                prev = node;
            }

            var eos = new Node(i+1, "", "", "EOS");
            eos.Prev = prev;
            ret.Add(eos);

            return ret;
        }

        /// <summary>
        /// ノード node の特徴を求め、特徴の重みを更新(learningRateを加算)する
        /// </summary>
        /// <param name="nodes">スコアを求める対象のノード列</param>
        /// <param name="index">ノード列の現在ノードを示す添え字</param>
        /// <param name="learningRate">学習率（正例の場合は正の値、負例の場合は負の値）</param>
        private void UpdateNodeScore(IReadOnlyList<Node> nodes, int index, double learningRate) {
            foreach (var func in FeatureFuncs.NodeFeatures) {
                var feature = func(nodes, index);
                double value;
                if (Weight.TryGetValue(feature, out value)) {
                    value += learningRate;
                } else {
                    value = learningRate;
                }
                Weight[feature] = value;
            }
        }

        /// <summary>
        /// 辺 (prevNode -> node) の特徴を求め、特徴の重みを更新(learningRateを加算)する
        /// </summary>
        /// <param name="prevNode"></param>
        /// <param name="node"></param>
        /// <param name="learningRate">学習率（正例の場合は正の値、負例の場合は負の値）</param>
        private void UpdateEdgeScore(Node prevNode, Node node, double learningRate) {
            if (prevNode == null) { return; }
            foreach (var func in FeatureFuncs.EdgeFeatures) {
                var feature = func(prevNode, node);
                double value;
                if (Weight.TryGetValue(feature, out value)) {
                    value += learningRate;
                } else {
                    value = learningRate;
                }
                Weight[feature] = value;
            }
        }

        /// <summary>
        /// ベクトルを学習する
        /// </summary>
        /// <param name="sentence"></param>
        /// <param name="learningRate"></param>
        /// <param name="addDict"></param>
        private void UpdateParametersBody(IList<Entry> sentence, double learningRate, Action<Node> addDict) {
            var nodes = ConvertToNodes(sentence);
            Node prevNode = null;
            for (var i = 0; i < nodes.Count; i++) {
                var node = nodes[i];
                addDict(node); 
                UpdateNodeScore(nodes, i, learningRate);
                UpdateEdgeScore(prevNode, node, learningRate);
                prevNode = node;
            }
        }

        /// <summary>
        /// 教師ベクトルと結果ベクトルを基に重みを更新する
        /// </summary>
        /// <param name="sentence">教師ベクトル</param>
        /// <param name="result">結果ベクトル</param>
        /// <param name="addDict"></param>
        protected void UpdateParameters(
            IList<Entry> sentence,
            IList<Entry> result, 
            Action<Node> addDict
        ) {
            UpdateParametersBody(sentence, LearningRate, addDict);       // 正例相当
            UpdateParametersBody(result, -1 * LearningRate, addDict);    // 負例相当
        }



        public StructuredSupportVectorMachine(FeatureFuncs featureFuncs, bool verboseMode) {
            Weight = new Dictionary<string, double>();
            FeatureFuncs = featureFuncs;
            VerboseMode = verboseMode;
            LearningRate = 0.1;

            LearningDecoder = new Decoder(featureFuncs, 0.05);
            PredictDecoder = new Decoder(featureFuncs);

            LastUpdated = new Dictionary<string, int>();
            UpdatedCount = 0;
            Lambda = 1.0e-22;
        }

        /// <summary>
        /// 教師データ構造を生成
        /// </summary>
        /// <param name="sentence"></param>
        /// <returns></returns>
        private List<Node> ConvertToGoldStandard(IList<Entry> sentence) {
            var nodes = ConvertToNodes(sentence);
            var ret = Enumerable.Repeat((Node)null, nodes.Max(x => x.EndPos) + 1).ToList();
            foreach (var node in nodes) {
                ret[node.EndPos] = node;
            }
            return ret;
        }

        /// <summary>
        /// 値 a を b だけ変化させるが、 変化前後で 0 をまたぐ場合は 0 でクリップする
        /// </summary>
        /// <param name="a">基本値</param>
        /// <param name="b">変動量</param>
        /// <returns></returns>
        private static double Clip(double a, double b) {
            return Math.Sign(a) * Math.Max(Math.Abs(a) - b, 0);
        }

        /// <summary>
        /// 特徴 feature に対応する重みのL1正則化
        /// </summary>
        /// <param name="feature">特徴</param>
        private void RegularizeFeature(string feature) {
            double value;
            if (Weight.TryGetValue(feature, out value)) {
                int lastUpdated;
                if (LastUpdated.TryGetValue(feature, out lastUpdated) == false) { lastUpdated = 0; }
                var newVal = Clip(value, Lambda * (UpdatedCount - lastUpdated));
                if (Math.Abs(newVal) < 1.0e-10) {
                    Weight.Remove(feature);
                } else {
                    Weight[feature] = newVal;
                }
                LastUpdated[feature] = UpdatedCount;
            }
        }

        /// <summary>
        /// ノード node の特徴に対応する重みの正則化
        /// </summary>
        /// <param name="nodes">スコアを求める対象のノード列</param>
        /// <param name="index">ノード列の現在ノードを示す添え字</param>
        private void RegularizeNode(IReadOnlyList<Node> nodes, int index) {
            foreach (var func in FeatureFuncs.NodeFeatures) {
                var feature = func(nodes, index);
                RegularizeFeature(feature);
            }
        }

        /// <summary>
        /// 辺 prevNode -> Node の特徴に対応する重みの正則化
        /// </summary>
        /// <param name="prevNode"></param>
        /// <param name="node">ノード</param>
        private void RegularizeEdge(Node prevNode, Node node) {
            foreach (var func in FeatureFuncs.EdgeFeatures) {
                var feature = func(prevNode, node);
                RegularizeFeature(feature);
            }
        }

        /// <summary>
        /// 遅延付きL1正則化
        /// </summary>
        /// <param name="graph"></param>
        private void Regularize(WordLattice graph) {
            foreach (var nodes in graph.Nodes) {
                for (var i = 0; i < nodes.Count; i++) {
                    var node = nodes[i];
                    if (node.IsBos) { continue; }
                    RegularizeNode(nodes, i);
                    foreach (var prevNode in graph.GetPrevs(node)) {
                        RegularizeEdge(prevNode, node);
                    }
                }
            }
        }

        /// <summary>
        /// 全特徴にL1正則化を適用
        /// </summary>
        private void RegularizeAll() {
            foreach (var feature in Weight.Keys.ToList()) {
                RegularizeFeature(feature);
            }
        }

        /// <summary>
        /// 学習を行う
        /// </summary>
        /// <param name="sentence">教師データ</param>
        /// <param name="commonPrefixSearch"></param>
        /// <param name="addDict"></param>
        public void Learn(IList<Entry> sentence, Func<string, int, IEnumerable<Entry>> commonPrefixSearch, Action<Node> addDict) {
            // 読みを連結した文字列を作る
            //var str = new StringBilderString.Concat(sentence.Select(x => x.Read)); 相当
            var sb = new System.Text.StringBuilder();
            foreach (var x in sentence) {
                sb.Append(x.Read);
            }
            var str = sb.ToString();

            var graph = new WordLattice(str, commonPrefixSearch);
            Regularize(graph);  // L1正則化

            var goldStandard = ConvertToGoldStandard(sentence);
            var result = LearningDecoder.Viterbi(graph, Weight, goldStandard);

            if (!sentence.SequenceEqual(result)) {
                UpdateParameters(sentence, result, addDict);
            }
            if (VerboseMode) {
                Console.WriteLine(String.Join(" ", sentence.Select(x => x.Word)));
                Console.WriteLine(String.Join(" ", result.Select(x => x.Word)));
            }
            UpdatedCount += 1;
        }

        /// <summary>
        /// 識別を行う
        /// </summary>
        /// <param name="str"></param>
        /// <param name="commonPrefixSearch"></param>
        /// <returns></returns>
        public List<Entry> Convert(string str, Func<string, int, IEnumerable<Entry>> commonPrefixSearch) {
            var graph = new WordLattice(str, commonPrefixSearch);
            var ret = PredictDecoder.Viterbi(graph, Weight);
            //Console.Error.WriteLine(graph.ToDot());
            return ret;
        }

        /// <summary>
        /// モデルの保存
        /// </summary>
        /// <param name="filename"></param>
        public void Save(string filename) {
            RegularizeAll();
            System.IO.File.WriteAllLines(filename, Weight.Select(fv => fv.Key + "\t\t" + fv.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        }

        /// <summary>
        /// モデルの読み込み
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="featureFuncs"></param>
        /// <param name="verboseMode"></param>
        /// <returns></returns>
        public static StructuredSupportVectorMachine Load(string filename, FeatureFuncs featureFuncs, bool verboseMode) {
            var self = new StructuredSupportVectorMachine(featureFuncs, verboseMode);
            foreach (var line in System.IO.File.ReadLines(filename)) {
                var kv = line.Split(new[] { "\t\t" },2,StringSplitOptions.None);
                self.Weight[kv[0]] = double.Parse(kv[1]);
            }
            return self;
        }

    }
}