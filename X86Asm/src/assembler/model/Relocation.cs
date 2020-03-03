namespace X86Asm.model {
    /// <summary>
    /// 再配置情報
    /// </summary>
    public sealed class Relocation {
        // 意味
        // この再配置情報が属するセクションのRawDataのOffsetから始まる4バイト(i386などsizeof(ptr_t)==4の環境の場合。x64なら8バイト)を
        // （リンカ/ローダが解決する際には）シンボルSymbolが示すアドレスに書き換えてほしい。
        //
        // C言語的にはこんな感じ
        // (*(uintptr_t*)&Section.RawData[Offset]) = (uintptr_t)&symbol; 
        //

        /// <summary>
        /// セクション（このセクション中にシンボルのアドレス値が書き込まれる）
        /// </summary>
        public Section Section { get; }

        /// <summary>
        /// セクション中のオフセット位置（この位置にシンボルのアドレス値が書き込まれる）
        /// </summary>
        public uint Offset { get; }

        /// <summary>
        /// 参照シンボル
        /// </summary>
        public Symbol Symbol { get; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="section"></param>
        /// <param name="offset"></param>
        /// <param name="symbol"></param>
        public Relocation(Section section, uint offset, Symbol symbol) {
            this.Section = section;
            this.Offset = offset;
            this.Symbol = symbol;
        }

        public override string ToString() {
            return $@"{Section.name} + {Offset}: {Symbol.name}";
        }

        public override bool Equals(object obj) {
            if (obj == null) { return false; }
            if (ReferenceEquals(obj, this)) { return true; }
            if (obj is Relocation) {
                var other = ((Relocation)obj);
                return this.Offset == other.Offset && this.Symbol.Equals(other.Symbol) && this.Section.Equals(other.Section);
            } else {
                return false;
            }
        }

        public override int GetHashCode() {
            return unchecked((int)Offset);
        }
    }

}

