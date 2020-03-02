using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace X86Asm.libcoff {
    /// <summary>
    /// COFF ファイルヘッダ
    /// </summary>

    public class  _IMAGE_FILE_HEADER {
        /// <summary>
        /// COFFファイルが対象とするアーキテクチャ
        /// </summary>
        public IMAGE_FILE_MACHINE Machine { get; set; }

        /// <summary>
        /// セクション数
        /// Machine が IMAGE_FILE_MACHINE_UNKNOWN で、セクション数が IMPORT_OBJECT_HDR_SIG2(0xffff) の場合はインポート用の擬似COFFファイル
        /// </summary>
        public UInt16 NumberOfSections { get; set; }

        /// <summary>
        /// ファイルのタイムスタンプ
        /// </summary>
        public UInt32 TimeDateStamp { get; set; }

        /// <summary>
        /// ファイルの先頭からシンボルテーブルへのオフセット
        /// </summary>
        public UInt32 PointerToSymbolTable { get; set; }

        /// <summary>
        /// このファイルに含まれるシンボル数
        /// </summary>
        public UInt32 NumberOfSymbols { get; set; }

        /// <summary>
        /// オプショナルヘッダのサイズ
        /// obj ファイルでは通常オプショナルヘッダを持たないので 0 
        /// </summary>
        public UInt16 SizeOfOptionalHeader { get; set; }

        /// <summary>
        /// ファイルの特性を表すフラグの集合
        /// </summary>
        public IMAGE_FILE_CHARACTERISTICS Characteristics { get; set; }

        public const int Size = 20;

        public static _IMAGE_FILE_HEADER ReadFrom(BinaryReader br) {
            return new _IMAGE_FILE_HEADER() {
                Machine = (IMAGE_FILE_MACHINE)br.ReadUInt16(),
                NumberOfSections = br.ReadUInt16(),
                TimeDateStamp = br.ReadUInt32(),
                PointerToSymbolTable = br.ReadUInt32(),
                NumberOfSymbols = br.ReadUInt32(),
                SizeOfOptionalHeader = br.ReadUInt16(),
                Characteristics = (IMAGE_FILE_CHARACTERISTICS)br.ReadUInt16(),
            };
        }

        public void WriteTo(BinaryWriter bw) {
            bw.Write((UInt16)Machine);
            bw.Write((UInt16)NumberOfSections);
            bw.Write((UInt32)TimeDateStamp);
            bw.Write((UInt32)PointerToSymbolTable);
            bw.Write((UInt32)NumberOfSymbols);
            bw.Write((UInt16)SizeOfOptionalHeader);
            bw.Write((UInt16)Characteristics);
        }
    }
}
