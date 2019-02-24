using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

// GoldStandard: 教師データ、もしくは、正しいラベルのついたデータ

namespace StructuredSVM {
    internal class Program {
        private static void Main(string[] args) {

            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            if (true) {
                sw.Reset();
                Console.WriteLine("learn start.");
                sw.Start();
                Learn(new[] { "train.cps", "-verbose" });
                sw.Stop();
                Console.WriteLine($"learn finish. ({sw.ElapsedMilliseconds}ms)");
            }
            if (true) {
                sw.Reset();
                Console.WriteLine("eval start.");
                sw.Start();
                Eval(new[] { "test.cps", "-verbose" });
                sw.Stop();
                Console.WriteLine($"eval finish. ({sw.ElapsedMilliseconds}ms)");
            }

            if (true) {
                Test(new string[0]);
                Console.WriteLine("test finish");
                Console.ReadKey();
            }
        }

        /// <summary>
        /// OptParserっぽいもの
        /// </summary>
        private class OptParse {
            private Dictionary<string, Tuple<int, Action<string[]>>> Entries { get; }
            public OptParse() {
                Entries = new Dictionary<string, Tuple<int, Action<string[]>>>();
            }

            public string Banner { get; set; }

            public string Help() {
                var sb = new StringBuilder();
                sb.AppendLine(Banner);
                foreach (var kv in Entries) {
                    sb.AppendLine($"{kv.Key} {Enumerable.Repeat("<arg>", kv.Value.Item1).Apply(x => string.Join(" ",x))}");
                }
                return sb.ToString();
            }

            public void On(string key, int argc, Action<string[]> predicate) {
                Entries[key] = Tuple.Create(argc, predicate);
            }

            public string[] Parse(string[] args) {
                var i = 0;
                while (i < args.Length) {
                    var key = args[i];
                    Tuple<int, Action<string[]>> value;

                    if (!Entries.TryGetValue(key, out value)) {
                        break;
                    }
                    if (i + 1 + value.Item1 > args.Length) {
                        throw new Exception("");
                    }
                    var v = args.Skip(i + 1).Take(value.Item1).ToArray();
                    value.Item2(v);
                    i += 1 + value.Item1;
                }
                return args.Skip(i).ToArray();
            }
        }

        private static void Learn(string[] args) {
            var featureFuncs = new FeatureFuncs();
            featureFuncs.NodeFeatures.Add(node => "S" + node.Word);
            featureFuncs.NodeFeatures.Add(node => "S" + node.Word + "\tR" + node.Read);
            featureFuncs.EdgeFeatures.Add((prevNode, node) => "S" + prevNode.Word + "\tS" + node.Word);

            var modelFilename = "mk.model";
            var dicFilename = "juman.dic";
            var learnerType = "ssvm";
            var verboseMode = false;
            var iterationNum = 1;

            OptParse opt = new OptParse();
            opt.Banner = "learn [options] corpus_filename";
            opt.On("-model", 1, v => { modelFilename = v[0]; });
            opt.On("-dic", 1, v => { dicFilename = v[0]; });
            opt.On("-learner", 1, v => { learnerType = v[0]; });
            opt.On("-verbose", 0, v => { verboseMode = true; });
            opt.On("-iteration", 1, v => { iterationNum = int.Parse(v[0]); });

            args = opt.Parse(args);

            var dic = new JumanDic(dicFilename);

            AbstractLearner abstractLearner = null;
            switch (learnerType) {
                case "ssvm": abstractLearner = new StructuredSupportVectorMachine(dic, featureFuncs, verboseMode); break;
                case "learner": abstractLearner = new StructuredPerceptron(dic, featureFuncs, verboseMode); break;
                default:
                    Console.Error.WriteLine("learner must be 'ssvm' or 'sperceptron'.");
                    Environment.Exit(-1);
                    break;
            }

            var corpusFilename = args.FirstOrDefault();
            if (corpusFilename == null) {
                Console.Error.WriteLine("corpus filename not found");
                Environment.Exit(-1);
                return;
            }

            for (var i = 1; i <= iterationNum; i++) {
                foreach (var line in System.IO.File.ReadLines(corpusFilename).Where(x => !String.IsNullOrEmpty(x))) {
                    var s = line.Trim('\r', '\n');
                    if (verboseMode) {
                        Console.WriteLine(s);
                    }
                    var sentence = s.Split(" ".ToArray()).Select(x => x.Split("/".ToArray(), 2, StringSplitOptions.None).Apply(y => Tuple.Create(y[0], y[1]))).ToList();
                    abstractLearner.Learn(sentence);
                }
            }

            abstractLearner.Save(modelFilename);

        }

        public static Dictionary<string, double> ReadWeightMap(string filename, JumanDic jumanDic) {
            var w = new Dictionary<string, double>();
            foreach (var line in System.IO.File.ReadLines(filename).Select(x => x.Trim('\r', '\n'))) {
                var a = line.Split(new[] { "\t\t" }, 2, StringSplitOptions.None);
                w[a[0]] = double.Parse(a[1]);
                var b = a[0].Split("\t".ToArray());
                if (b.Length == 2 && b[0][0] == 'S' && b[1][0] == 'R') {
                    var word = b[0].Substring(1);
                    var read = b[1].Substring(1);
                    jumanDic.Add(read, word);
                }
            }
            return w;
        }

        private static void Test(string[] args) {
            var featureFuncs = new FeatureFuncs();

            featureFuncs.NodeFeatures.Add(node => "S" + node.Word);
            featureFuncs.NodeFeatures.Add(node => "S" + node.Word + "\tR" + node.Read);
            featureFuncs.EdgeFeatures.Add((prevNode, node) => "S" + prevNode.Word + "\tS" + node.Word);

            var modelFilename = "mk.model";
            var dicFilename = "juman.dic";

            OptParse opt = new OptParse();
            opt.Banner = "test [options] corpus_filename";
            opt.On("-model", 1, v => { modelFilename = v[0]; });
            opt.On("-dic", 1, v => { dicFilename = v[0]; });

            opt.Parse(args);
            
            var dic = new JumanDic(dicFilename);
            var decoder = new Decoder(featureFuncs);
            var w = ReadWeightMap(modelFilename, dic);

            string line;
            while ((line = Console.ReadLine()) != null) {
                line = line.Trim('\r', '\n');
                var str = line;
                var graph = new Graph(dic, str);
                var result = decoder.Viterbi(graph, w);
                result.Select(x => x.Item1).Apply(x => String.Join(" ", x)).Apply(Console.WriteLine);
            }
        }

        private static T Max<T>(params T[] args) {
            return args.Max();
        }

        private static int CalcLcs(string str1, string str2) {
            var m = str1.Length;
            var n = str2.Length;
            var table = Enumerable.Range(0, n + 1).Select(x => Enumerable.Repeat(0, m + 1).ToArray()).ToArray();

            for (var j = 1; j <= n; j++) {
                for (var i = 1; i <= m; i++) {
                    var same = (str1[i - 1] == str2[j - 1]) ? 1 : 0;
                    table[j][i] = Max(table[j - 1][i - 1] + same, table[j - 1][i], table[j][i - 1]);
                }
            }

            return table[n][m];
        }

        private static void Eval(string[] args) {
            var featureFuncs = new FeatureFuncs();
            featureFuncs.NodeFeatures.Add(node => "S" + node.Word);
            featureFuncs.NodeFeatures.Add(node => "S" + node.Word + "\tR" + node.Read);
            featureFuncs.EdgeFeatures.Add((prevNode, node) => "S" + prevNode.Word + "\tS" + node.Word);

            var modelFilename = "mk.model";
            var dicFilename = "juman.dic";
            var verboseMode = false;

            OptParse opt = new OptParse();
            opt.Banner = "eval [options] corpus_filename";
            opt.On("-model", 1, v => { modelFilename = v[0]; });
            opt.On("-dic", 1, v => { dicFilename = v[0]; });
            opt.On("-verbose", 0, v => { verboseMode = true; });

            args = opt.Parse(args);

            var corpusFilename = args.FirstOrDefault();
            if (corpusFilename == null) {
                Console.Error.WriteLine("corpus filename not found");
                Environment.Exit(-1);
                return;
            }

            var dic = new JumanDic(dicFilename);
            var decoder = new Decoder(featureFuncs);
            var w = ReadWeightMap(modelFilename, dic);

            var lcsSum = 0;
            var sysSum = 0;
            var cpsSum = 0;

            foreach (var line in System.IO.File.ReadLines(corpusFilename).Where(x => !String.IsNullOrEmpty(x)).Select(x => x.Trim('\r', '\n'))) {
                if (verboseMode) {
                    Console.WriteLine(line);
                }
                var sentence = line.Split(" ".ToArray()).Select(x => x.Split("/".ToArray(), 2)).Select(x => Tuple.Create(x[0], x[1])).ToList();
                var str = sentence.Select(x => x.Item1).Apply(string.Concat);
                var graph = new Graph(dic, str);
                var result = decoder.Viterbi(graph, w);

                var gold = sentence.Select(x => x.Item1).Apply(string.Concat);
                var ret = result.Select(x => x.Item1).Apply(string.Concat);
                var lcs = CalcLcs(gold, ret);
                lcsSum += lcs;
                cpsSum += gold.Length;
                sysSum += ret.Length;
                if (verboseMode) {
                    result.Select(x => x.Item1).Apply(x => string.Join(" ", x)).Apply(Console.WriteLine);
                    Console.WriteLine();
                }
            }

            Console.WriteLine($"lcs/sys(precision):{ (float)lcsSum / sysSum * 100}");
            Console.WriteLine($"lcs/cps(recall):{(float)lcsSum / cpsSum * 100}");
        }

    }

    public static class Ext {
        public static T2 Apply<T1, T2>(this T1 self, Func<T1, T2> predicate) => predicate(self);
        public static T1 Apply<T1>(this T1 self, Action<T1> predicate) { predicate(self); return self; }
    }

    /// <summary>
    /// 辞書
    /// </summary>
    public class JumanDic {
        private Dictionary<string, HashSet<string>> Entries { get; }

        public JumanDic(string filename) {
            Entries = new Dictionary<string, HashSet<string>>();

            var lines = System.IO.File.ReadLines(filename)
                                      .Select(x => x.Split("\t".ToArray(), StringSplitOptions.None))
                                      .Where(x => x.Length >= 2);
            foreach (var line in lines) {
                Add(line[0], line[1]);
            }
        }

        public void Add(string read, string word) {
            HashSet<string> value;
            if (Entries.TryGetValue(read, out value)) {
                if (value.Contains(word) == false) {
                    value.Add(word);
                }
            } else {
                Entries[read] = new HashSet<string>() { word };
            }
        }

        public List<string[]> CommonPrefixSearch(string str, int max) {
            var result = new List<string[]>();
            var limit = Math.Max(str.Length, max);
            for (var i = 1; i <= limit; i++) {
                var read = str.Substring(0, i);
                foreach (var word in Entries[read]) {
                    result.Add(new[] {read, word});
                }
            }
            return result;
        }

        public HashSet<string> Find(string str) {
            HashSet<string> value;
            if (Entries.TryGetValue(str, out value)) {
                return value;
            } else {
                return new HashSet<string>();
            }
        }
    }

    /// <summary>
    /// ラティス構造のノード
    /// </summary>
    public class Node {
        /// <summary>
        /// ノードの終端位置
        /// </summary>
        public int EndPos { get; }
        public string Word { get; }
        public string Read { get; }
        public double Score { get; set; }
        public Node Prev { get; set; }

        public override string ToString() {
            return $"<Node Pos='{EndPos - Length}' Read='{Read}' Word='{Word}' Score='{Score}' Prev='{Prev?.EndPos ?? -1}' />";
        }

        public Node(string word, string read, int endPos) {
            Word = word;
            Read = read;
            EndPos = endPos;
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
    }

    /// <summary>
    /// ラティス構造（グラフ）
    /// </summary>
    public class Graph {
        public IReadOnlyList<Node>[] Nodes { get; }
        public Node Eos { get; }
        public Graph(JumanDic dic, string str) {
            var nodes = Enumerable.Repeat((List<Node>)null, str.Length + 2).Select(x => new List<Node>()).ToArray();

            // push BOS
            var bos = new Node("", "", 0);
            nodes[0].Add(bos);

            // push EOS
            Eos = new Node("", "", str.Length + 1);
            nodes[str.Length + 1].Add(Eos);

            for (var i = 0; i < str.Length; i++) {
                var n = Math.Min(str.Length, i + 16);
                // 1～16gramでスキャンして辞書に登録されている候補をグラフに入れる
                for (var j = i + 1; j <= n; j++) {
                    // 本来はCommonPrefixSearchを使う
                    var read = str.Substring(i, j - i);
                    foreach (var word in dic.Find(read)) {
                        var node = new Node(word, read, j);
                        nodes[j].Add(node);
                    }
                }
                {
                    // 無変換に対応する候補を入れる
                    var read = str.Substring(i, 1);
                    if (read != "") {
                        // puts "put "+ Read
                        var node = new Node(read, read, i + 1);
                        nodes[i + 1].Add(node);
                    }
                }
            }
            Nodes = nodes;
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
                    sb.AppendLine("\t\t" + node.ToString());
                }
                sb.AppendLine("\t</Nodes>");
            }
            sb.AppendLine("</Graph>");
            return sb.ToString();
        }
    }

    /// <summary>
    /// ノードから特徴を一つ算出するデリゲート
    /// </summary>
    /// <param name="node">ノード</param>
    /// <returns></returns>
    public delegate string NodeFeatures(Node node);

    /// <summary>
    /// エッジから特徴を一つ算出するデリゲート
    /// </summary>
    /// <param name="prevNode">ノードに繋がっているひとつ前のノード</param>
    /// <param name="node">ノード</param>
    /// <returns></returns>
    public delegate string EdgeFeatures(Node prevNode,Node node);

    /// <summary>
    /// ノードから特徴ベクトルを算出するクラス
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
    /// ビタビデコーダークラス
    /// </summary>
    public class Decoder {
        private FeatureFuncs FeatureFuncs { get; }

        public Decoder(FeatureFuncs featureFuncs) {
            FeatureFuncs = featureFuncs;
        }

        /// <summary>
        /// ノード node のスコアを求める
        /// </summary>
        /// <param name="node">スコアを求める対象のノード</param>
        /// <param name="gold">正解を示すノード列</param>
        /// <param name="w">特徴の重み表</param>
        /// <returns></returns>
        public virtual double GetNodeScore(Node node, IReadOnlyList<Node> gold, IReadOnlyDictionary<string, double> w) {
            var score = 0.0;
            foreach (var func in FeatureFuncs.NodeFeatures) {
                var feature = func(node);
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
        public virtual double GetEdgeScore(Node prevNode, Node node, IReadOnlyList<Node> gold, IReadOnlyDictionary<string, double> w) {
            var score = 0.0;
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
        /// <param name="gold"></param>
        /// <returns></returns>
        public List<Tuple<string, string>> Viterbi(Graph graph, IReadOnlyDictionary<string, double> w, IReadOnlyList<Node> gold = null) {

            foreach (var nodes in graph.Nodes) {
                foreach (var node in nodes) {
                    if (node.IsBos) { continue; }
                    node.Score = -1000000.0;
                    var nodeScoreCache = GetNodeScore(node, gold, w);

                    foreach (var prevNode in graph.GetPrevs(node)) {
                        var tmpScore = prevNode.Score + GetEdgeScore(prevNode, node, gold, w) + nodeScoreCache;
                        if (tmpScore >= node.Score) {
                            node.Score = tmpScore;
                            node.Prev = prevNode;
                        }
                    }
                }
            }
            {
                var result = new List<Tuple<string, string>>();
                var node = graph.Eos.Prev;
                while (!node.IsBos) {
                    // puts node.to_s + node.Word + "\t" + node.EndPos.to_s
                    // puts result
                    result.Add(Tuple.Create(node.Word, node.Read));
                    node = node.Prev;
                }
                result.Reverse();
                return result;
            }
        }


    }

    internal class StructuredSupportVectorMachineDecoder : Decoder {
        private double Penalty { get; }

        public StructuredSupportVectorMachineDecoder(FeatureFuncs featureFuncs) : base(featureFuncs) {
            Penalty = 0.05;
        }

        /// <summary>
        /// ノード node の値と教師データの値が一致しているか？
        /// </summary>
        /// <param name="node"></param>
        /// <param name="gold"></param>
        /// <returns></returns>
        private static bool IsCorrectNode(Node node, IReadOnlyList<Node> gold) {
            var correctNode = gold[node.EndPos];
            return (correctNode != null && node.Word == correctNode.Word);
        }

        /// <summary>
        /// prevNodeとnodeの間のエッジが教師データと一致しているかを判定
        /// （ノード prevNode と node の値がそれぞれ教師データと一致しているかどうかをもって結果としている）
        /// </summary>
        /// <param name="prevNode"></param>
        /// <param name="node"></param>
        /// <param name="gold"></param>
        /// <returns></returns>
        private static bool IsCorrectEdge(Node prevNode, Node node, IReadOnlyList<Node> gold) {
            return IsCorrectNode(node, gold) && IsCorrectNode(prevNode, gold);
        }

        public override double GetNodeScore(Node node, IReadOnlyList<Node> gold, IReadOnlyDictionary<string, double> w) {
            var score = 0.0;
            if (IsCorrectNode(node, gold)) {
                // スコアが大きい＝尤もらしさが高いのはずなのに
                // 正しく分類できている場合にスコアからペナルティを減算するのはなぜ？
                score -= Penalty;
            }

            score += base.GetNodeScore(node, gold, w);
            return score;
        }

        public override double GetEdgeScore(Node prevNode, Node node, IReadOnlyList<Node> gold, IReadOnlyDictionary<string, double> w) {
            var score = 0.0;
            if (IsCorrectEdge(prevNode, node, gold)) {
                // スコアが大きい＝尤もらしさが高いのはずなのに
                // 正しく分類できている場合にスコアからペナルティを減算するのはなぜ？
                score -= Penalty;
            }

            score += base.GetEdgeScore(prevNode, node, gold, w);
            return score;
        }

    }

    /// <summary>
    /// 学習器
    /// </summary>
    internal abstract class AbstractLearner {
        protected Dictionary<string, double> Weight { get; }
        protected JumanDic Dic { get; }
        protected FeatureFuncs FeatureFuncs { get; }
        protected bool VerboseMode { get; }
        private double LearningRate { get; }

        protected AbstractLearner(JumanDic dic, FeatureFuncs featureFuncs, bool verboseMode) {
            Weight = new Dictionary<string, double>();
            Dic = dic;
            FeatureFuncs = featureFuncs;
            VerboseMode = verboseMode;
            LearningRate = 0.1;
        }

        public abstract void Learn(IList<Tuple<string, string>> sentence);

        protected List<Node> ConvertToNodes(IList<Tuple<string, string>> sentence) {
            var ret = new List<Node>();
            var bos = new Node("", "", 0);
            ret.Add(bos);
            var i = 0;
            var prev = bos;

            foreach (var x in sentence) {
                i += x.Item2.Length;
                var node = new Node(x.Item1, x.Item2, i);
                node.Prev = prev;
                ret.Add(node);
                prev = node;
            }

            var eos = new Node("", "", i + 1);
            eos.Prev = prev;
            ret.Add(eos);

            return ret;
        }

        public abstract List<Tuple<string, string>> Convert(string str);

        private void UpdateNodeScore(Node node, double diff) {
            Dic.Add(node.Read, node.Word);
            foreach (var func in FeatureFuncs.NodeFeatures) {
                var feature = func(node);
                if (Weight.ContainsKey(feature)) {
                    Weight[feature] += diff;
                } else {
                    Weight[feature] = diff;
                }
            }
        }

        private void UpdateEdgeScore(Node prevNode, Node node, double diff) {
            if (prevNode == null) { return; }
            foreach (var func in FeatureFuncs.EdgeFeatures) {
                var feature = func(prevNode, node);
                if (Weight.ContainsKey(feature)) {
                    Weight[feature] += diff;
                } else {
                    Weight[feature] = diff;
                }
            }
        }

        private void UpdateParametersBody(IList<Tuple<string, string>> sentence, double diff) {
            var nodes = ConvertToNodes(sentence);
            Node prevNode = null;
            foreach (var node in nodes) {
                UpdateNodeScore(node, diff);
                UpdateEdgeScore(prevNode, node, diff);
                prevNode = node;
            }
        }

        /// <summary>
        /// 教師ベクトルと結果ベクトルを基に重みを更新する
        /// </summary>
        /// <param name="sentence">教師ベクトル</param>
        /// <param name="result">結果ベクトル</param>
        protected void UpdateParameters(
            IList<Tuple<string, string>> sentence,
            IList<Tuple<string, string>> result
        ) {
            UpdateParametersBody(sentence, LearningRate);       // 正例相当
            UpdateParametersBody(result, -1 * LearningRate);    // 負例相当
        }

        public abstract void Save(string filename);
    }

    internal class StructuredPerceptron : AbstractLearner {
        protected Decoder Decoder { get; }

        public StructuredPerceptron(JumanDic dic, FeatureFuncs featureFuncs, bool verboseMode) 
            : base(dic, featureFuncs, verboseMode) {
            Decoder = new Decoder(featureFuncs);
        }
        
        /// <summary>
        /// 学習
        /// </summary>
        /// <param name="sentence"></param>
        public override void Learn(IList<Tuple<string, string>> sentence) {
            var str = sentence.Select(x => x.Item2).Apply(string.Concat);
            var graph = new Graph(Dic, str);

            var result = Decoder.Viterbi(graph, Weight);

            if (!sentence.SequenceEqual(result)) {
                UpdateParameters(sentence, result);
            }
            if (VerboseMode) {
                // puts sentence.map{|x|x[0]}.join(" ")
                result.Select(x => x.Item1).Apply(x => string.Join(" ", x)).Apply(Console.WriteLine);
            }
        }

        public override List<Tuple<string, string>> Convert(string str) {
            var graph = new Graph(Dic, str);
            return Decoder.Viterbi(graph, Weight);
        }

        public override void Save(string filename) {
            System.IO.File.WriteAllLines(filename, Weight.Select(fv => fv.Key + "\t\t" + fv.Value.ToString(CultureInfo.InvariantCulture)));
        }
    }

    internal class StructuredSupportVectorMachine : AbstractLearner {
        private StructuredSupportVectorMachineDecoder Decoder{ get; }
        private Decoder OriginalDecoder{ get; }
        private Dictionary<string, int> LastUpdated{ get; }
        private int UpdatedCount{ get; set; }
        private double Lambda { get; }

        public StructuredSupportVectorMachine(JumanDic dic, FeatureFuncs featureFuncs, bool verboseMode) : base(dic, featureFuncs, verboseMode) {

            Decoder = new StructuredSupportVectorMachineDecoder(featureFuncs);
            OriginalDecoder = new Decoder(featureFuncs);

            LastUpdated = new Dictionary<string, int>();
            UpdatedCount = 0;
            Lambda = 1.0e-22;
        }

        private List<Node> ConvertToGoldStandard(IList<Tuple<string, string>> sentence) {
            var nodes = ConvertToNodes(sentence);
            var ret = Enumerable.Repeat((Node)null, nodes.Max(x => x.EndPos) + 1).ToList();
            foreach (var node in nodes) {
                ret[node.EndPos] = node;
            }
            return ret;
        }

        private static double Clip(double a, double b) {
            return Math.Sign(a) * Math.Max(Math.Abs(a) - b, 0);
        }

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

        private void RegularizeNode(Node node) {
            foreach (var func in FeatureFuncs.NodeFeatures) {
                var feature = func(node);
                RegularizeFeature(feature);
            }
        }

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
        private void Regularize(Graph graph) {
            foreach (var nodes in graph.Nodes) {
                foreach (var node in nodes) {
                    if (node.IsBos) { continue; }
                    RegularizeNode(node);
                    foreach (var prevNode in graph.GetPrevs(node)) {
                        RegularizeEdge(prevNode, node);
                    }
                }
            }
        }

        // ファイルにパラメーターを保存する前に正則化をかけるために使う
        private void RegularizeAll() {
            foreach (var feature in Weight.Keys.ToList()) {
                RegularizeFeature(feature);
            }
        }

        /// <summary>
        /// 学習する
        /// </summary>
        /// <param name="sentence"></param>
        public override void Learn(IList<Tuple<string, string>> sentence) {
            var str = sentence.Select(x => x.Item2).Apply(String.Concat);
            var graph = new Graph(Dic, str);
            Regularize(graph);  // L1正則化

            var goldStandard = ConvertToGoldStandard(sentence);
            var result = Decoder.Viterbi(graph, Weight, goldStandard);

            if (!sentence.SequenceEqual(result)) {
                UpdateParameters(sentence, result);
            }
            if (VerboseMode) {
                sentence.Select(x => x.Item1).Apply(x => String.Join(" ", x)).Apply(Console.WriteLine);
                result.Select(x => x.Item1).Apply(x => String.Join(" ", x)).Apply(Console.WriteLine);
            }
            UpdatedCount += 1;
        }

        public override List<Tuple<string, string>> Convert(string str) {
            var graph = new Graph(Dic, str);
            return OriginalDecoder.Viterbi(graph, Weight);
        }

        public override void Save(string filename) {
            RegularizeAll();
            System.IO.File.WriteAllLines(filename, Weight.Select(fv => fv.Key + "\t\t" + fv.Value.ToString(CultureInfo.InvariantCulture)));
        }

    }
}

