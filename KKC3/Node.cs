namespace KKC3 {
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
        /// <param name="features"></param>
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
            if (0 <= i && i < Features.Length) {
                return Features[i];
            } else {
                return defaultValue;
            }
        }
    }
}