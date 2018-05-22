namespace AnsiCParser {
    /// <summary>
    /// 構文木
    /// </summary>
    public abstract partial class SyntaxTree {

        /// <summary>
        /// 構文木の対応するソース範囲
        /// </summary>
        public LocationRange LocationRange {
            get; set;
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="locationRange"></param>
        protected SyntaxTree(LocationRange locationRange) {
            LocationRange = locationRange;
        }
    }
}
