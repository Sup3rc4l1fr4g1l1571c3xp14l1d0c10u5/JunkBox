namespace X86Asm.libelf {

    /// <summary>
    /// オブジェクトファイルタイプ
    /// </summary>
    public enum ObjectType : ushort {
        /// <summary>
        /// 未知のタイプ
        /// </summary>
        ET_NONE = 0,
        /// <summary>
        /// 再配置可能なファイル
        /// </summary>
        ET_REL = 1,
        /// <summary>
        /// 実行可能ファイル
        /// </summary>
        ET_EXEC = 2,
        /// <summary>
        /// 共有オブジェクト
        /// </summary>
        ET_DYN = 3,
        /// <summary>
        /// コアファイル
        /// </summary>
        ET_CORE = 4
    }

}