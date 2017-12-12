namespace AnsiCParser {
    /// <summary>
    /// 6.2.2 識別子の結合 (結合（linkage）)
    /// </summary>
    public enum LinkageKind {
        // 結合は，外部結合，内部結合及び無結合の 3 種類
        /// <summary>
        /// 指定なし
        /// </summary>
        None,

        /// <summary>
        /// 外部結合
        /// (スコープや翻訳単位に関係なく同名のものは常に同じものを指す。)
        /// </summary>
        ExternalLinkage,

        /// <summary>
        /// 内部結合
        /// (翻訳単位内で常に同じものを指す。)
        /// </summary>
        InternalLinkage,

        /// <summary>
        /// 無結合
        /// （指し示し先はすべて独立している）
        /// </summary>
        NoLinkage

    }
}