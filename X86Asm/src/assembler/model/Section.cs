using System.Collections.Generic;
using System.IO;

namespace X86Asm.model {

    /// <summary>
    /// セクション情報
    /// </summary>
    public sealed class Section {
        /// <summary>
        /// セクション名
        /// </summary>
        public string name { get; set; }

        /// <summary>
        /// セクション番号（セクションテーブルのインデクス番号と等しい）
        /// </summary>
        public uint index { get; set; }

        /// <summary>
        /// セクションのサイズ（データブロックのサイズと等価）
        /// </summary>
        public uint size { get { return (uint)data.Length; } }

        /// <summary>
        /// セクション内の再配置情報
        /// </summary>
        public List<Relocation> relocations { get; } = new List<Relocation>();

        /// <summary>
        /// セクションのシンボル情報
        /// </summary>
        public List<Symbol> symbols { get; } = new List<Symbol>();

        /// <summary>
        /// セクションのデータブロック
        /// </summary>
        public MemoryStream data { get; } = new MemoryStream();

        public override string ToString() {
            return name;
        }
    }
}
