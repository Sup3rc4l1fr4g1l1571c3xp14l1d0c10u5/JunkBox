using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KKC3 {
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
        /// <param name="str">単語ラティスを作る文字列</param>
        /// <param name="commonPrefixSearch"></param>
        public WordLattice(string str, Func<string,int,IEnumerable<Entry>> commonPrefixSearch ) {
            var nodes = Enumerable.Range(0, str.Length + 2).Select(_ => new List<Node>()).ToArray();

            // BOSを単語ラティスの先頭に設定
            var bos = new Node(0, "", "", "BOS");
            nodes[0].Add(bos);

            // EOSを単語ラティスの末尾に設定
            Eos = new Node(str.Length + 1, "", "", "EOS");
            nodes[str.Length + 1].Add(Eos);

            for (var i = 0; i < str.Length; i++) {
                foreach (var word in commonPrefixSearch(str,i)) {
                    var j = i + word.Read.Length;
                    var node = new Node(j, word.Word, word.Read, word.Features);
                    nodes[j].Add(node);
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
}