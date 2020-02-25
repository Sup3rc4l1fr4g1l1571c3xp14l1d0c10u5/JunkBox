namespace X86Asm.parser {

    /// <summary>
    /// 字句解析器インタフェース
    /// </summary>
    public interface ITokenizer {
        /// <summary>
        /// 次のトークンを読み取り、結果を返す
        /// </summary>
        /// <returns>読み取り結果を表すトークン</returns>
        Token Next();
    }
}