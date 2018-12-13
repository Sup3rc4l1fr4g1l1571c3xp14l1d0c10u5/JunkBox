namespace CNN {
    public class Edge {
        /// <summary>
        /// 入力側ノード
        /// </summary>
        public Node Input { get; set; }
        
        /// <summary>
        /// 出力側ノード
        /// </summary>
        public Node Output { get; set; }

        /// <summary>
        /// 重み
        /// </summary>
        public double Weight { get; set; }
    }
}