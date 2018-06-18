namespace CSCPP {
    /// <summary>
    /// プリプロセッサの機能フラグ
    /// </summary>
    public enum Feature {
        /// <summary>
        /// ANSI trigraphs サポート有効にします。
        /// </summary>
        Trigraphs,

        /// <summary>
        /// 未定義のプリプロセッサ指令を出力に含めます。
        /// </summary>
        UnknownDirectives,

        /// <summary>
        /// 前処理結果に出力する行番号情報を GCC 形式の書式にします。
        /// </summary>
        OutputGccStyleLineDirective,

        /// <summary>
        /// ISO/IEC 9899-1999 で導入された行コメント記法を有効にします。
        /// </summary>
        LineComment,

        /// <summary>
        /// ISO/IEC 9899-1999で導入された空のマクロ実引数機能を有効にします。
        /// </summary>
        EmptyMacroArgument,

        /// <summary>
        /// ISO/IEC 9899-1999で導入された可変個引数マクロ機能を有効にします。
        /// </summary>
        VariadicMacro,

        /// <summary>
        /// ISO/IEC 9899-1999で導入された64bit型 (long long および unsigned long long) の定数サポートを有効にします。
        /// </summary>
        LongLongConstant,

        /// <summary>
        /// gcc や clang でサポートされている可変個引数マクロの末尾コンマに対する拡張を有効にします。
        /// これは ISO/IEC 9899-1999 では定義されていない非標準の拡張です。
        /// </summary>
        ExtensionForVariadicMacro,
    }
}