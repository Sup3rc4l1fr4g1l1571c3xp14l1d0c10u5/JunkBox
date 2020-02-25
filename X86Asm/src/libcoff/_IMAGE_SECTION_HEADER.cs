using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace X86Asm.libcoff {
    /// <summary>
    /// セクションヘッダ
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct _IMAGE_SECTION_HEADER {
        [FieldOffset(0)]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public char[] Name;

        /// <summary>
        /// セクションの仮想サイズ
        /// obj ファイルでは使われない
        /// </summary>
        [FieldOffset(8)]
        public UInt32 VirtualSize;

        /// <summary>
        /// セクションの仮想アドレス
        /// obj ファイルでは使われない
        /// </summary>
        [FieldOffset(12)]
        public UInt32 VirtualAddress;

        /// <summary>
        /// セクションデータのサイズ
        /// </summary>
        [FieldOffset(16)]
        public UInt32 SizeOfRawData;

        /// <summary>
        /// ファイルの先頭からセクションデータへのオフセット
        /// セクションデータが存在しない場合は0
        /// </summary>
        [FieldOffset(20)]
        public UInt32 PointerToRawData;

        /// <summary>
        /// ファイルの先頭から再配置情報へのオフセット
        /// </summary>
        [FieldOffset(24)]
        public UInt32 PointerToRelocations;

        /// <summary>
        /// CodeView形式のデバッグ情報で使われる行番号情報へのオフセット
        /// 使われていない場合は0が入る
        /// </summary>
        [FieldOffset(28)]
        public UInt32 PointerToLinenumbers;

        /// <summary>
        /// 再配置情報の数
        /// </summary>
        [FieldOffset(32)]
        public UInt16 NumberOfRelocations;

        /// <summary>
        /// 行番号情報の数
        /// </summary>
        [FieldOffset(34)]
        public UInt16 NumberOfLinenumbers;

        /// <summary>
        /// セクションの特性を表すフラグの集合
        /// </summary>
        [FieldOffset(36)]
        public DataSectionFlags Characteristics;

    }
}
