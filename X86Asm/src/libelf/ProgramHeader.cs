using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using X86Asm.util;

namespace X86Asm.libelf {

    /// <summary>
    /// ELFプログラムヘッダ
    /// </summary>
	public class ProgramHeader {
        public const int TypeSize = 32;

        /// <summary>
        /// セグメントの種別
        /// </summary>
        public SegmentType Type { get; set; }

        /// <summary>
        /// セグメントのファイルオフセット
        /// </summary>
        public UInt32 Offset { get; set; }

        /// <summary>
        /// セグメントの仮想アドレス
        /// </summary>
        public UInt32 VAddr { get; set; }

        /// <summary>
        /// セグメントの物理アドレス(x86/x64では使われない)
        /// </summary>
        public UInt32 PAddr { get; set; }

        /// <summary>
        /// セグメントのファイルサイズ（ゼロの場合もあり）
        /// </summary>
        public UInt32 FileSz{ get; set; }

        /// <summary>
        /// セグメントのメモリサイズ（ゼロの場合もあり）
        /// </summary>
        public UInt32 MemSz { get; set; }

        /// <summary>
        /// セグメントの属性
        /// </summary>
        public SegmentFlag Flags { get; set; }

        /// <summary>
        /// セグメントのアライメント（ファイル、メモリ両方）
        /// </summary>
        public UInt32 Align { get; set; }

        public void WriteTo(BinaryWriter bw) {
            bw.Write((UInt32)Type);
            bw.Write((UInt32)Offset);
            bw.Write((UInt32)VAddr);
            bw.Write((UInt32)PAddr);
            bw.Write((UInt32)FileSz);
            bw.Write((UInt32)MemSz);
            bw.Write((UInt32)Flags);
            bw.Write((UInt32)Align);
        }
    }
}