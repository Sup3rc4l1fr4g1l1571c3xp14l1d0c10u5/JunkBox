using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace X86Asm.libcoff {
    /// <summary>
    /// セクションヘッダ
    /// </summary>
    public class _IMAGE_SECTION_HEADER {
        public byte[] Name { get; set; } = new byte[8];

        /// <summary>
        /// セクションの仮想サイズ
        /// obj ファイルでは使われない
        /// </summary>
        public UInt32 VirtualSize;

        /// <summary>
        /// セクションの仮想アドレス
        /// obj ファイルでは使われない
        /// </summary>
        public UInt32 VirtualAddress;

        /// <summary>
        /// セクションデータのサイズ
        /// </summary>
        public UInt32 SizeOfRawData;

        /// <summary>
        /// ファイルの先頭からセクションデータへのオフセット
        /// セクションデータが存在しない場合は0
        /// </summary>
        public UInt32 PointerToRawData;

        /// <summary>
        /// ファイルの先頭から再配置情報へのオフセット
        /// </summary>
        public UInt32 PointerToRelocations;

        /// <summary>
        /// CodeView形式のデバッグ情報で使われる行番号情報へのオフセット
        /// 使われていない場合は0が入る
        /// </summary>
        public UInt32 PointerToLinenumbers;

        /// <summary>
        /// 再配置情報の数
        /// </summary>
        public UInt16 NumberOfRelocations;

        /// <summary>
        /// 行番号情報の数
        /// </summary>
        public UInt16 NumberOfLinenumbers;

        /// <summary>
        /// セクションの特性を表すフラグの集合
        /// </summary>
        public DataSectionFlags Characteristics;

        /// <summary>
        /// _IMAGE_SECTION_HEADER構造体のバイトサイズ
        /// </summary>
        public const int Size = 40;

        public void WriteTo(BinaryWriter bw) {
            bw.Write(this.Name, 0, 8);
            bw.Write((UInt32)this.VirtualSize);
            bw.Write((UInt32)this.VirtualAddress);
            bw.Write((UInt32)this.SizeOfRawData);
            bw.Write((UInt32)this.PointerToRawData);
            bw.Write((UInt32)this.PointerToRelocations);
            bw.Write((UInt32)this.PointerToLinenumbers);
            bw.Write((UInt16)this.NumberOfRelocations);
            bw.Write((UInt16)this.NumberOfLinenumbers);
            bw.Write((UInt32)this.Characteristics);
        }

        public static _IMAGE_SECTION_HEADER ReadFrom(BinaryReader br) {
            return new _IMAGE_SECTION_HEADER() {
                Name = br.ReadBytes(8),
                VirtualSize = br.ReadUInt32(),
                VirtualAddress = br.ReadUInt32(),
                SizeOfRawData = br.ReadUInt32(),
                PointerToRawData = br.ReadUInt32(),
                PointerToRelocations = br.ReadUInt32(),
                PointerToLinenumbers = br.ReadUInt32(),
                NumberOfRelocations = br.ReadUInt16(),
                NumberOfLinenumbers = br.ReadUInt16(),
                Characteristics = (DataSectionFlags)br.ReadUInt32(),
            };
        }

    }
}
