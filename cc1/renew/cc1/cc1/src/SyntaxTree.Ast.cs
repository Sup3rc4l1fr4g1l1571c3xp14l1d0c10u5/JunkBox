namespace AnsiCParser.SyntaxTree {
    /// <summary>
    /// 抽象構文木基底クラス
    /// </summary>
    public abstract partial class Ast {

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
        protected Ast(LocationRange locationRange) {
            LocationRange = locationRange;
        }
    }
}
