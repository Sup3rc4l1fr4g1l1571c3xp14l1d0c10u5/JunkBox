namespace CSCPP {
    /// <summary>
    /// 文字
    /// </summary>
    public struct Char {
        /// <summary>
        /// 位置情報
        /// </summary>
        public Position Position { get; }

        /// <summary>
        /// 文字コード
        /// </summary>
        public int Value { get; }
        
        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="position">位置情報</param>
        /// <param name="value">文字コード</param>
        public Char(Position position, int value) {
            Position = position;
            Value = value;
        }

        public bool IsEof() {
            return Value == -1;
        }
 
        public override string ToString() {
            return $"{(char)Value}";
        }
    }
}