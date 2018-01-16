namespace AnsiCParser {
    /// <summary>
    /// 6.2.2 識別子の結合 (結合（linkage）)
    /// </summary>
    /// <remarks>
    /// 結合は，外部結合，内部結合及び無結合の 3 種類
    /// </remarks>
    public enum LinkageKind {
        /// <summary>
        /// 指定なし
        /// (便宜上作成したもので、6.2.2には登場しない)
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
        /// （指し示し先はすべて独立している。）
        /// </summary>
        NoLinkage

    }
}
