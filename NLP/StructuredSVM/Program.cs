using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace StructuredSVM {
    internal class Program {
        private static void Main(string[] args) {
            Learn(new[] { "train.cps", "-verbose" });
            Console.WriteLine("learn finish");
            Console.ReadKey();
            Eval(new[] { "test.cps", "-verbose" });
            Console.WriteLine("eval finish");
            Console.ReadKey();
            Test(new string[0]);
            Console.WriteLine("test finish");
            Console.ReadKey();
        }

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
            opt.Banner = "learn.rb [options] corpus_filename";
            opt.On("-model", 1, v => { modelFilename = v[0]; });
            opt.On("-dic", 1, v => { dicFilename = v[0]; });
            opt.On("-learner", 1, v => { learnerType = v[0]; });
            opt.On("-verbose", 0, v => { verboseMode = true; });
            opt.On("-iteration", 1, v => { iterationNum = int.Parse(v[0]); });

            args = opt.Parse(args);

            var dic = new Dic(dicFilename);

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


        public static Dictionary<string, double> read_weight_map(string filename, Dic dic) {
            var w = new Dictionary<string, double>();
            foreach (var line in System.IO.File.ReadLines(filename).Select(x => x.Trim('\r', '\n'))) {
                var a = line.Split(new[] { "\t\t" }, 2, StringSplitOptions.None);
                w[a[0]] = double.Parse(a[1]);
                var b = a[0].Split("\t".ToArray());
                if (b.Length == 2 && b[0][0] == 'S' && b[1][0] == 'R') {
                    var word = b[0].Substring(1);
                    var read = b[1].Substring(1);
                    dic.Add(read, word);
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
            
            var dic = new Dic(dicFilename);
            var decoder = new Decoder(featureFuncs);
            var w = read_weight_map(modelFilename, dic);

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

            Func<Node, string> nodeFeatureSurface1 = node => "S" + node.Word;
            featureFuncs.NodeFeatures.Add(nodeFeatureSurface1);

            Func<Node, string> nodeFeatureSurface2 = node => "S" + node.Word + "\tR" + node.Read;
            featureFuncs.NodeFeatures.Add(nodeFeatureSurface2);

            Func<Node, Node, string> edgeFeatureSurface = (prevNode, node) => "S" + prevNode.Word + "\tS" + node.Word;
            featureFuncs.EdgeFeatures.Add(edgeFeatureSurface);

            var modelFilename = "mk.model";
            var dicFilename = "juman.dic";
            var verboseMode = false;

            OptParse opt = new OptParse();
            opt.Banner = "test [options] corpus_filename";
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

            var dic = new Dic(dicFilename);
            var decoder = new Decoder(featureFuncs);
            var w = read_weight_map(modelFilename, dic);

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

    public class Dic {
        private Dictionary<string, HashSet<string>> Entries { get; }

        public Dic(string filename) {
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

    public class Node {
        public int EndPos { get; }
        public string Word { get; }
        public string Read { get; }
        public double Score { get; set; }
        public Node Prev { get; set; }

        public Node(string word, string read, int endPos) {
            Word = word;
            Read = read;
            EndPos = endPos;
            Score = 0.0;
            Prev = null;
        }

        public int Length {
            get { return Read.Length; }
        }

        public bool IsBos {
            get { return EndPos == 0; }
        }

        public bool IsEos {
            get { return (Read.Length == 0 && EndPos != 0); }
        }
    }

    internal class Graph {
        public List<List<Node>> Nodes { get; }
        public Node Eos { get; }
        public Graph(Dic dic, string str) {
            Nodes = Enumerable.Repeat((List<Node>)null, str.Length + 2).ToList();
            // push BOS
            var bos = new Node("", "", 0);
            Nodes[0] = new List<Node>() { bos };

            // push BOS
            Eos = new Node("", "", str.Length + 1);
            Nodes[str.Length + 1] = new List<Node>() { Eos };

            for (var i = 0; i < str.Length; i++) {
                var n = Math.Min(str.Length, i + 16);
                for (var j = i + 1; j < n; j++) {
                    var read = str.Substring(i, j - i);
                    foreach (var word in dic.Find(read)) {
                        var node = new Node(word, read, j);
                        if (Nodes[j] == null) {
                            Nodes[j] = new List<Node>();
                        }
                        Nodes[j].Add(node);
                    }
                }
                {
                    var read = str.Substring(i, 1);
                    if (read != "") {
                        // puts "put "+ Read
                        var node = new Node(read, read, i + 1);
                        if (Nodes[i + 1] == null) {
                            Nodes[i + 1] = new List<Node>();
                        }
                        Nodes[i + 1].Add(node);
                    }
                }
            }
        }

        public List<Node> GetPrevs(Node node) {
            if (node.IsEos) {
                // eosはlengthが0なので特殊な処理が必要
                var startPos = node.EndPos - 1;
                return Nodes[startPos];
            } else if (node.IsBos) {
                // bosはそれより前のノードがないので特殊な処理が必要
                return new List<Node>();
            } else {
                var startPos = node.EndPos - node.Length;
                return Nodes[startPos];
            }
        }
    }

    internal class FeatureFuncs {
        public List<Func<Node, string>> NodeFeatures { get; set; }
        public List<Func<Node, Node, string>> EdgeFeatures { get; set; }
        public FeatureFuncs() {
            NodeFeatures = new List<Func<Node, string>>();
            EdgeFeatures = new List<Func<Node, Node, string>>();
        }
    }

    internal class Decoder {
        private FeatureFuncs FeatureFuncs { get; }

        public Decoder(FeatureFuncs featureFuncs) {
            FeatureFuncs = featureFuncs;
        }

        public virtual double GetNodeScore(Node node, List<Node> gold, Dictionary<string, double> w) {
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

        public virtual double GetEdgeScore(Node prevNode, Node node, List<Node> gold, Dictionary<string, double> w) {
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

        public List<Tuple<string, string>> Viterbi(Graph graph, Dictionary<string, double> w, List<Node> gold = null) {

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

        private static bool IsCorrectNode(Node node, List<Node> gold) {
            var correctNode = gold[node.EndPos];
            return (correctNode != null && node.Word == correctNode.Word);
        }

        private static bool IsCorrectEdge(Node prevNode, Node node, List<Node> gold) {
            return IsCorrectNode(node, gold) && IsCorrectNode(prevNode, gold);
        }

        public override double GetNodeScore(Node node, List<Node> gold, Dictionary<string, double> w) {
            var score = 0.0;
            if (IsCorrectNode(node, gold)) {
                score -= Penalty;
            }

            score += base.GetNodeScore(node, gold, w);
            return score;
        }

        public override double GetEdgeScore(Node prevNode, Node node, List<Node> gold, Dictionary<string, double> w) {
            var score = 0.0;
            if (IsCorrectEdge(prevNode, node, gold)) {
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
        protected Dic Dic { get; }
        protected FeatureFuncs FeatureFuncs { get; }
        protected bool VerboseMode { get; }
        private double LearningRate { get; }

        protected AbstractLearner(Dic dic, FeatureFuncs featureFuncs, bool verboseMode) {
            Weight = new Dictionary<string, double>();
            Dic = dic;
            FeatureFuncs = featureFuncs;
            VerboseMode = verboseMode;
            LearningRate = 0.1;
        }

        public abstract void Learn(List<Tuple<string, string>> sentence);

        protected List<Node> ConvertToNodes(List<Tuple<string, string>> sentence) {
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

        private void UpdateParametersBody(List<Tuple<string, string>> sentence, double diff) {
            var nodes = ConvertToNodes(sentence);
            Node prevNode = null;
            foreach (var node in nodes) {
                UpdateNodeScore(node, diff);
                UpdateEdgeScore(prevNode, node, diff);
                prevNode = node;
            }
        }

        /// <summary>
        /// 学習を行う
        /// </summary>
        /// <param name="sentence">教師ベクトル</param>
        /// <param name="result">結果ベクトル</param>
        protected void UpdateParameters(
            List<Tuple<string, string>> sentence, 
            List<Tuple<string, string>> result
        ) {
            UpdateParametersBody(sentence, LearningRate);       // 正例相当
            UpdateParametersBody(result, -1 * LearningRate);    // 負例相当
        }

        public abstract void Save(string filename);
    }

    internal class StructuredPerceptron : AbstractLearner {
        protected Decoder Decoder { get; }

        public StructuredPerceptron(Dic dic, FeatureFuncs featureFuncs, bool verboseMode) 
            : base(dic, featureFuncs, verboseMode) {
            Decoder = new Decoder(featureFuncs);
        }
        
        public override void Learn(List<Tuple<string, string>> sentence) {
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

        public StructuredSupportVectorMachine(Dic dic, FeatureFuncs featureFuncs, bool verboseMode) : base(dic, featureFuncs, verboseMode) {

            Decoder = new StructuredSupportVectorMachineDecoder(featureFuncs);
            OriginalDecoder = new Decoder(featureFuncs);

            LastUpdated = new Dictionary<string, int>();
            UpdatedCount = 0;
            Lambda = 1.0e-22;
        }

        public List<Node> ConvertToGoldStandard(List<Tuple<string, string>> sentence) {
            var nodes = ConvertToNodes(sentence);
            var ret = Enumerable.Repeat((Node)null, nodes.Max(x => x.EndPos) + 1).ToList();
            foreach (var node in nodes) {
                ret[node.EndPos] = node;
            }
            return ret;
        }

        public static double Clip(double a, double b) {
            return Math.Sign(a) * Math.Max(Math.Abs(a) - b, 0);
        }

        public void RegularizeFeature(string feature) {
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

        public void RegularizeNode(Node node) {
            foreach (var func in FeatureFuncs.NodeFeatures) {
                var feature = func(node);
                RegularizeFeature(feature);
            }
        }

        public void RegularizeEdge(Node prevNode, Node node) {
            foreach (var func in FeatureFuncs.EdgeFeatures) {
                var feature = func(prevNode, node);
                RegularizeFeature(feature);
            }
        }


        // graphに出てくるところだけ正則化をかける
        // 雑誌のナイーブなコードでも動作速度以外は問題ないが、今回は遅すぎるのでこのテクニックを使う。
        // 詳しくは Efficient Online and Batch Learning Using Forward Backward Splitting; John Duchi, Yoram Singer; Journal of Machine Learning Research 10(Dec):2899-2934, 2009 を参照。
        public void Regularize(Graph graph) {
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
        public void regularize_all() {
            foreach (var feature in Weight.Keys.ToList()) {
                RegularizeFeature(feature);
            }
        }

        public override void Learn(List<Tuple<string, string>> sentence) {
            var str = sentence.Select(x => x.Item1).Apply(String.Concat);
            var graph = new Graph(Dic, str);
            Regularize(graph);

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
            regularize_all();
            System.IO.File.WriteAllLines(filename, Weight.Select(fv => fv.Key + "\t\t" + fv.Value.ToString(CultureInfo.InvariantCulture)));
        }

    }
}

