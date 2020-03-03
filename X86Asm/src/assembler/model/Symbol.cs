namespace X86Asm.model {

    /// <summary>
    /// シンボル情報
    /// </summary>
    public sealed class Symbol {
        /// <summary>
        /// グローバルシンボル（グローバルスコープ変数など）なら真、ローカルシンボル（ファイルスコープ変数など）なら偽
        /// </summary>
        public bool global { get; set; }

        /// <summary>
        /// シンボル名
        /// </summary>
        public string name { get; set; }

        /// <summary>
        /// シンボルが宣言された（属している）セクション
        /// </summary>
        public Section section { get; set; }

        /// <summary>
        /// シンボルの宣言位置を示すオフセット
        /// </summary>
        public uint offset { get; set; }

        public override string ToString() {
            return name;
        }
    }

}
