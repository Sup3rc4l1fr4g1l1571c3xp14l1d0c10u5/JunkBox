using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace X86Asm.libcoff {
    /// <summary>
    /// 再配置情報
    /// </summary>
    public class _IMAGE_RELOCATION {

        /// <summary>
        /// 再配置が適用される項目のアドレスをセクションの先頭からのオフセットで示す
        /// </summary>
        public UInt32 VirtualAddress { get { return VirtualAddressOrRelocCount; } set { VirtualAddressOrRelocCount = value; } }

        /// <summary>
        /// セクション再配置情報数が 0xFFFF を越える場合の再配置情報数
        /// </summary>
        public UInt32 RelocCount { get { return VirtualAddressOrRelocCount; } set { VirtualAddressOrRelocCount = value; } }

        private UInt32 VirtualAddressOrRelocCount { get; set; }

        /// <summary>
        /// 再配置で置き換える値の元となるシンボルのインデックス
        /// </summary>
        public UInt32 SymbolTableIndex { get; set; }

        /// <summary>
        /// 再配置情報のタイプ
        /// </summary>
        public _IMAGE_REL_I386 Type { get; set; }

        public const int Size = 8;

        public static _IMAGE_RELOCATION ReadFrom(BinaryReader br) {
            return new _IMAGE_RELOCATION() {
                VirtualAddress = br.ReadUInt32(),
                SymbolTableIndex = br.ReadUInt32(),
                Type = (_IMAGE_REL_I386)br.ReadUInt16(),
            };
        }

        public void WriteTo(BinaryWriter bw) {
            bw.Write((UInt32)VirtualAddress);
            bw.Write((UInt32)SymbolTableIndex);
            bw.Write((UInt16)Type);
        }

    }
}
