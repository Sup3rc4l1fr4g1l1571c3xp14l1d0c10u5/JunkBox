/*
Mecab:
  mecab --node-format=%%m\t%%f[20]\t%%f[0]\n --eos-format=\n --unk-format=%%M "%~dp1%~nx1" > "%~dp1%~n1.mecabed.txt"

NHK News:
  Array.from(document.querySelectorAll('.content--summary, .content--summary-more, .content--body > .body-text')).map(x => x.innerText).join("\r\n")

*/

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KKC3 {
    class Program {
        static void Main(string[] args) {
            if (true) {
                // 単語辞書を作る
                var dict = new Dict();

                // 学習用分かち書きデータから辞書を作成
                foreach (var file in System.IO.Directory.EnumerateFiles(@".\data\Corpus", "*.txt")) {
                    Console.WriteLine($"Read File {file}");
                    foreach (var line in System.IO.File.ReadLines(file)) {
                        var items = line.Split('\t');
                        if (items.Length >= 3) {
                            dict.Add(toHiragana(items[1] == "" ? items[0] : items[1]), items[0], items[2]);
                        }
                    }
                }

                // 辞書を保存
                using (var sw = new System.IO.StreamWriter("dict.tsv")) {
                    dict.Save(sw);
                }
            }

            // 系列学習を実行
            if (true) {
                Dict dict;
                using (var sw = new System.IO.StreamReader("dict.tsv")) {
                    dict = Dict.Load(sw);
                }
                var featureFuncs = CreateFeatureFuncs();
                var svm = new StructuredSupportVectorMachine(dict, featureFuncs, false);

                for (var i = 0; i < 5; i++) {
                
                    var words = new List<Dict.Entry>();
                    foreach (var file in System.IO.Directory.EnumerateFiles(@".\data\Corpus", "*.txt")) {
                        Console.WriteLine($"Read File {file}");
                        foreach (var line in System.IO.File.ReadLines(file)) {
                            var items = line.Split('\t');
                            if (String.IsNullOrWhiteSpace(line)) {
                                svm.Learn(words);
                                words.Clear();
                            } else {
                                words.Add(new Dict.Entry(toHiragana(items[1] == "" ? items[0] : items[1]), items[0], items[2]));
                            }
                        }
                        if (words.Count != 0) {
                            svm.Learn(words);
                            words.Clear();
                        }

                    }

                    // 検定開始
                    var gradews = new Gradews();
                    foreach (var file in System.IO.Directory.EnumerateFiles(@".\data\Corpus", "*.txt")) {
                        Console.WriteLine($"Read File {file}");
                        foreach (var line in System.IO.File.ReadLines(file)) {
                            var items = line.Split('\t');
                            if (String.IsNullOrWhiteSpace(line)) {
                                var ret = svm.Convert(String.Concat(words.Select(x => x.read)));
                                gradews.Comparer(String.Join(" ", words.Select(x => x.word)), String.Join(" ", ret.Select(x => x.word)));
                                words.Clear();
                            }
                            else {
                                words.Add(new Dict.Entry(toHiragana(items[1] == "" ? items[0] : items[1]), items[0], items[2]));
                            }
                        }

                        if (words.Count != 0) {
                            var ret = svm.Convert(String.Concat(words.Select(x => x.read)));
                            gradews.Comparer(String.Join(" ", words.Select(x => x.word)), String.Join(" ", ret.Select(x => x.word)));
                            words.Clear();
                        }
                    }

                    Console.WriteLine($"SentAccura: {gradews.SentAccura}");
                    Console.WriteLine($"WordPrec: {gradews.WordPrec}");
                    Console.WriteLine($"WordRec: {gradews.WordRec}");
                    Console.WriteLine($"Fmeas: {gradews.Fmeas}");
                    Console.WriteLine($"BoundAccuracy: {gradews.BoundAccuracy}");
                    Console.WriteLine();
                }

                svm.Save("learn.model");
            }
            {
                Dict dict;
                using (var sw = new System.IO.StreamReader("dict.tsv")) {
                    dict = Dict.Load(sw);
                }
                var featureFuncs = CreateFeatureFuncs();

                var svm = StructuredSupportVectorMachine.Load("learn.model", dict, featureFuncs, true);

                for (;;) {
                    var input = Console.ReadLine();
                    var ret = svm.Convert(input);
                    Console.WriteLine(String.Join("\t", ret.Select(x => x.ToString())));
                }
            }

        }

        static string toHiragana(string str) {
            return String.Concat(str.Select(x => (0x30A1 <= x && x <= 0x30F3) ? (char)(x - (0x30A1 - 0x3041)) : (char)x));
        }

        static FeatureFuncs CreateFeatureFuncs() {
            var featureFuncs = new FeatureFuncs();

            featureFuncs.NodeFeatures.Add((nodes, index) => "S0" + nodes[index].Word);
            //featureFuncs.NodeFeatures.Add((nodes, index) => "P" + nodes[index].GetFeature(0));
            featureFuncs.NodeFeatures.Add((nodes, index) => "S0" + nodes[index].Word + "\tR0" + nodes[index].Read);
            //featureFuncs.NodeFeatures.Add((nodes, index) => "S" + nodes[index].Word + "\tP" + nodes[index].GetFeature(0));
            featureFuncs.NodeFeatures.Add((nodes, index) => "S1" + ((index > 0) ? nodes[index - 1].Word : "") + "\tS0" + nodes[index].Word + "\t+R1" + ((index + 1 < nodes.Count) ? nodes[index + 1].Read : ""));
            //featureFuncs.NodeFeatures.Add((nodes, index) => "-P" + ((index > 0) ? nodes[index - 1].GetFeature(0) : "") + "\tS" + nodes[index].Word + "\t+P" + ((index + 1 < nodes.Count) ? nodes[index + 1].Read : ""));
            featureFuncs.EdgeFeatures.Add((prevNode, node) => "ES" + prevNode.Word + "\tED" + node.Word);
            //featureFuncs.EdgeFeatures.Add((prevNode, node) => "ES" + prevNode.Word + "\tEP" + node.GetFeature(0) );

            return featureFuncs;
        }
    }

    /// <summary>
    /// 単語構造（単語ラティスのノード）
    /// </summary>
    public class Node {
        /// <summary>
        /// 単語の末尾位置
        /// </summary>
        public int EndPos { get; }

        /// <summary>
        /// 変換後の単語
        /// </summary>
        public string Word { get; }

        /// <summary>
        /// 変換前の読み
        /// </summary>
        public string Read { get; }

        /// <summary>
        /// 単語の特徴
        /// </summary>
        public string[] Features { get; }
        
        /// <summary>
        /// スコア
        /// </summary>
        public double Score { get; set; }

        /// <summary>
        /// このノードが接続されているひとつ前のノード
        /// </summary>
        public Node Prev { get; set; }

        public override string ToString() {
            //return $"<Node Pos='{EndPos - Length}' Read='{Read}' Word='{Word}' Pos1='{Pos1}' Pos2='{Pos2}' Score='{Score}' Prev='{Prev?.EndPos ?? -1}' />";
            return $"<Node Pos='{EndPos - Length}' Read='{Read}' Word='{Word}' Score='{Score}' Prev='{Prev?.EndPos ?? -1}' />";
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="endPos">単語の末尾位置</param>
        /// <param name="word">変換後の単語（系列ラベリングにおけるラベル）</param>
        /// <param name="read">変換前の読み（系列ラベリングにおける値）</param>
        public Node(int endPos, string word, string read, params string[] features) {
            EndPos = endPos;
            Word = word;
            Read = read;
            Features = features;
            Score = 0.0;
            Prev = null;
        }

        /// <summary>
        /// 受理する文字列の長さ
        /// </summary>
        public int Length {
            get { return Read.Length; }
        }

        /// <summary>
        /// グラフの先頭ノードであるか？
        /// </summary>
        public bool IsBos {
            get { return EndPos == 0; }
        }

        /// <summary>
        /// グラフの終端ノードであるか？
        /// </summary>
        public bool IsEos {
            get { return (Read.Length == 0 && EndPos != 0); }
        }

        public string GetFeature(int i, string defaultValue = "") {
            if (0 <= i && i < this.Features.Length) {
                return this.Features[i];
            } else {
                return defaultValue;
            }
        }
    }

    /// <summary>
    /// 単語ラティス
    /// </summary>
    public class WordLattice {
        /// <summary>
        /// 単語構造列
        /// </summary>
        public IReadOnlyList<Node>[] Nodes { get; }

        /// <summary>
        /// グラフの末尾
        /// </summary>
        public Node Eos { get; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="dic">辞書</param>
        /// <param name="str">単語ラティスを作る文字列</param>
        public WordLattice(Dict dic, string str) {
            var nodes = Enumerable.Range(0, str.Length + 2).Select(_ => new List<Node>()).ToArray();

            // BOSを単語ラティスの先頭に設定
            var bos = new Node(0, "", "", "BOS");
            nodes[0].Add(bos);

            // EOSを単語ラティスの末尾に設定
            Eos = new Node(str.Length + 1, "", "", "EOS");
            nodes[str.Length + 1].Add(Eos);

            for (var i = 0; i < str.Length; i++) {
                var n = Math.Min(str.Length, i + 16);
                // 文字列を1～16gramでスキャンして辞書に登録されている候補をグラフに入れる
                for (var j = i + 1; j <= n; j++) {
                    // 本来はCommonPrefixSearchを使う
                    var read = str.Substring(i, j - i);
                    foreach (var word in dic.Find(read)) {
                        var node = new Node(j, word.word, word.read, word.features);
                        nodes[j].Add(node);
                    }
                }
                {
                    // 無変換に対応する候補を入れる
                    var read = str.Substring(i, 1);
                    if (read != "") {
                        var node = new Node(i + 1, read, read, "");
                        nodes[i + 1].Add(node);
                    }
                }
            }
            Nodes = nodes.Cast<IReadOnlyList<Node>>().ToArray();
        }

        /// <summary>
        /// ノード node の前のノード集合を得る
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public IReadOnlyList<Node> GetPrevs(Node node) {
            if (node.IsEos) {
                // Eosの場合は最後のノード集合列
                var startPos = node.EndPos - 1;
                return Nodes[startPos];
            } else if (node.IsBos) {
                // Bosの場合は空集合
                return new List<Node>();
            } else {
                // それ以外はノードの終端
                var startPos = node.EndPos - node.Length;
                return Nodes[startPos];
            }
        }

        public override string ToString() {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<Graph>");
            foreach (var nodes in Nodes) {
                sb.AppendLine("\t<Nodes>");
                foreach (var node in nodes) {
                    sb.AppendLine("\t\t" + node);
                }
                sb.AppendLine("\t</Nodes>");
            }
            sb.AppendLine("</Graph>");
            return sb.ToString();
        }

        public string ToDot() {
            StringBuilder sb = new StringBuilder();
            var table = Enumerable.Range(0, Nodes.Length).Select(x => new List<string>()).ToList();
            var nodeTable = new Dictionary<Node, string>();

            sb.AppendLine("digraph G {");
            sb.AppendLine("graph [rankdir = LR; concentrate = true];");
            sb.AppendLine("node[shape = record];");
            for (var x = 0; x < Nodes.Length; x++) {
                var nodes = Nodes[x];
                sb.AppendLine($"N{x} [label=\"{nodes.Last().Read}\"];");
            }
            
            sb.AppendLine($"{String.Join(" -> ", Enumerable.Range(0, Nodes.Length).Select(x => $"N{x}"))};");

            for (var x = 1; x < Nodes.Length-1; x++) {
                var nodes = Nodes[x];
                for (var y = 0; y < nodes.Count; y++) {
                    var node = nodes[y];
                    sb.AppendLine($"N{x-node.Length+1}_{x}_{y} [label=\"{{{node.Read}|{node.Word}|{node.Score}}}\"];");
                    table[x - node.Length+1].Add($"N{x-node.Length+1}_{x}_{y}");
                    nodeTable.Add(node, $"N{x - node.Length + 1}_{x}_{y}");
                    if (nodeTable.ContainsKey(node.Prev)) {
                        sb.AppendLine($"{nodeTable[node.Prev]} -> N{x - node.Length + 1}_{x}_{y};");
                    }
                }
            }
            for (var i = 0; i < table.Count; i++) {
                sb.AppendLine($"{{ rank = same; N{i} {String.Join(" ", table[i]) } }}");
            }
            sb.AppendLine("}");
            return sb.ToString();
        }

    }

    /// <summary>
    /// ノードから特徴を一つ算出するデリゲート
    /// </summary>
    /// <param name="node">ノード列</param>
    /// <param name="index">現在のノード</param>
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

    /// <summary>
    /// 構造化SVM学習器
    /// </summary>
    public class StructuredSupportVectorMachine {
        /// <summary>
        /// 重み（特徴⇒重みの疎行列）
        /// </summary>
        protected Dictionary<string, double> Weight { get; }

        /// <summary>
        /// 辞書
        /// </summary>
        protected Dict Dic { get; }

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
        protected List<Node> ConvertToNodes(IList<Dict.Entry> sentence) {
            var ret = new List<Node>();
            var bos = new Node(0, "", "", "BOS");
            ret.Add(bos);
            var i = 0;
            var prev = bos;

            foreach (var x in sentence) {
                i += x.read.Length;
                var node = new Node(i, x.word, x.read,x.features);
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
        /// <param name="node"></param>
        /// <param name="learningRate">学習率（正例の場合は正の値、負例の場合は負の値）</param>
        private void UpdateNodeScore(IReadOnlyList<Node> nodes, int index, double learningRate) {
            foreach (var func in FeatureFuncs.NodeFeatures) {
                var feature = func(nodes, index);
                if (Weight.ContainsKey(feature)) {
                    Weight[feature] += learningRate;
                } else {
                    Weight[feature] = learningRate;
                }
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
                if (Weight.ContainsKey(feature)) {
                    Weight[feature] += learningRate;
                } else {
                    Weight[feature] = learningRate;
                }
            }
        }

        /// <summary>
        /// ベクトルを学習する
        /// </summary>
        /// <param name="sentence"></param>
        /// <param name="learningRate"></param>
        private void UpdateParametersBody(IList<Dict.Entry> sentence, double learningRate) {
            var nodes = ConvertToNodes(sentence);
            Node prevNode = null;
            for (var i = 0; i < nodes.Count; i++) {
                var node = nodes[i];
                Dic.Add(node.Read, node.Word/*, node.Pos1, node.Pos2*/);
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
        protected void UpdateParameters(
            IList<Dict.Entry> sentence,
            IList<Dict.Entry> result
        ) {
            UpdateParametersBody(sentence, LearningRate);       // 正例相当
            UpdateParametersBody(result, -1 * LearningRate);    // 負例相当
        }



        public StructuredSupportVectorMachine(Dict dic, FeatureFuncs featureFuncs, bool verboseMode) {
            Weight = new Dictionary<string, double>();
            Dic = dic;
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
        private List<Node> ConvertToGoldStandard(IList<Dict.Entry> sentence) {
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
        /// <param name="node">ノード</param>
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
        public void Learn(IList<Dict.Entry> sentence) {
            // 読みを連結した文字列を作る
            var str = String.Concat(sentence.Select(x => x.read));
            // 
            var graph = new WordLattice(Dic, str);
            Regularize(graph);  // L1正則化

            var goldStandard = ConvertToGoldStandard(sentence);
            var result = LearningDecoder.Viterbi(graph, Weight, goldStandard);

            if (!sentence.SequenceEqual(result)) {
                UpdateParameters(sentence, result);
            }
            if (VerboseMode) {
                Console.WriteLine(String.Join(" ", sentence.Select(x => x.word)));
                Console.WriteLine(String.Join(" ", result.Select(x => x.word)));
            }
            UpdatedCount += 1;
        }

        /// <summary>
        /// 識別を行う
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public List<Dict.Entry> Convert(string str) {
            var graph = new WordLattice(Dic, str);
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
        /// <param name="dic"></param>
        /// <param name="featureFuncs"></param>
        /// <param name="verboseMode"></param>
        /// <returns></returns>
        public static StructuredSupportVectorMachine Load(string filename, Dict dic, FeatureFuncs featureFuncs, bool verboseMode) {
            var self = new StructuredSupportVectorMachine(dic, featureFuncs, verboseMode);
            foreach (var line in System.IO.File.ReadLines(filename)) {
                var kv = line.Split(new[] { "\t\t" },2,StringSplitOptions.None);
                self.Weight[kv[0]] = double.Parse(kv[1]);
            }
            return self;
        }

    }


    public class Gradews {
        /// <summary>
        /// 総データ数
        /// </summary>
        private int tots;
        /// <summary>
        /// 正解数
        /// </summary>
        private int cors;
        /// <summary>
        /// 境界総数
        /// </summary>
        private int totb;
        /// <summary>
        /// 正解した境界総数
        /// </summary>
        private int corb;
        /// <summary>
        /// 教師データの単語総数
        /// </summary>
        private int refw;
        /// <summary>
        /// テストデータの単語総数
        /// </summary>
        private int testw;
        /// <summary>
        /// 区切り位置の一致総数
        /// </summary>
        private int corw;

        public Gradews() {
            reset();
        }
        public void reset() {
            tots = 0;
            cors = 0;
            totb = 0;
            corb = 0;
            refw = 0;
            testw = 0;
            corw = 0;
        }
        public void Comparer(string _ref, string _test) {
            tots++;
            if (_ref == _test) {
                cors++;
            }
            var rarr = _ref.ToCharArray().ToList();
            var tarr = _test.ToCharArray().ToList();
            var rb = new List<bool>();
            var tb = new List<bool>();
            while (rarr.Count > 0 && tarr.Count > 0) {
                var rs = (rarr[0] == ' ');
                if (rs) { rarr.RemoveAt(0); }
                rb.Add(rs);
                var ts = (tarr[0] == ' ');
                if (ts) { tarr.RemoveAt(0); }
                tb.Add(ts);

                tarr.RemoveAt(0);
                rarr.RemoveAt(0);
            }
            System.Diagnostics.Debug.Assert(rb.Count == tb.Count);

            // total boundaries
            this.totb += rb.Count;
            // correct boundaries
            Enumerable.Range(0, rb.Count).ToList().ForEach((i) => { if (rb[i] == tb[i]) { corb++; } });

            // find word counts
            var rlens = rb.ToList();
            refw += rlens.Count;
            var tlens = tb.ToList();
            testw += @tlens.Count;
            // print "$ref\n@rlens\n$test\n@tlens\n";
            // find word matches
            var rlast = 0;
            var tlast = 0;
            while (rlens.Any() && tlens.Any()) {
                if (rlast == tlast) {
                    if (rlens[0] == tlens[0]) { corw++; }
                }
                if (rlast <= tlast) {
                    rlast += rlens[0] ? 1 : 0;
                    rlens.RemoveAt(0);
                }
                if (tlast < rlast) {
                    tlast += tlens[0] ? 1 : 0;
                    tlens.RemoveAt(0);
                }
            }
        }
        /// <summary>
        /// 正解率
        /// </summary>
        public double SentAccura { get { return (double)cors / tots; } }
        /// <summary>
        /// 適合率
        /// </summary>
        public double WordPrec { get { return (double)corw / testw; } }
        /// <summary>
        /// 再現率
        /// </summary>
        public double WordRec { get { return (double)corw / refw; } }
        /// <summary>
        /// F-尺度
        /// </summary>
        public double Fmeas { get { return (2.0 * WordPrec * WordRec) / (WordPrec + WordRec); } }
        /// <summary>
        /// 区切り位置の正解率
        /// </summary>
        public double BoundAccuracy { get { return (double)corb / totb; } }
    }

}
