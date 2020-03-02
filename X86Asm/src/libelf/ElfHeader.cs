using System;
using System.IO;
using System.Runtime.InteropServices;
using X86Asm.util;

namespace X86Asm.libelf {


    /// <summary>
    /// ELFヘッダ
    /// </summary>
    public sealed class ElfHeader {
        public const int Size = 52;

        /// <summary>
        /// 
        /// </summary>
        private static ElfIdent ident = new ElfIdent() {
            EI_MAG0 = 0x7F, 
            EI_MAG1 = (byte)'E', 
            EI_MAG2 = (byte)'L', 
            EI_MAG3 = (byte)'F', 
            EI_CLASS = ElfClass.ELFCLASS32, 
            EI_DATA = ElfData.ELFDATA2LSB, 
            EI_VERSION = ElfVersion.EV_CURRENT, 
            EI_OSABI = ElfOSABI.ELFOSABI_NONE, 
            EI_ABIVERSION = 0, 
        };

        /// <summary>
        /// オブジェクトファイルの種類
        /// </summary>
        public ObjectType type { get; set; }

        /// <summary>
        /// アーキテクチャ
        /// </summary>
        public ElfMachine machine  { get; set; } = ElfMachine.EM_386;

        /// <summary>
        /// バージョン
        /// </summary>
        public ElfVersion version  { get; set; } = ElfVersion.EV_CURRENT;

        /// <summary>
        /// エントリポイント（仮想アドレス）
        /// </summary>
        public uint entry { get; set; } 

        /// <summary>
        /// フラグ（x86やX64では使われていない）
        /// </summary>
        public uint flags { get; set; } = 0;

        /// <summary>
        /// ELFヘッダのサイズ
        /// </summary>
        public short ehsize { get; set; } = Size;

        /// <summary>
        /// セクションヘッダの名前がある文字列テーブルのインデクス
        /// </summary>
        public short shstrndx { get; set; } 

        /// <summary>
        /// バイナリストリームへの書き込み
        /// </summary>
        /// <param name="bw"></param>
        /// <param name="phnum"></param>
        /// <param name="shnum"></param>
        public void WriteTo(BinaryWriter bw, ushort phnum, ushort shnum) {
            uint phoff = Size;
            uint shoff = phoff + phnum * (uint)ProgramHeader.TypeSize;

            // e_ident の書き込み
            ident.WriteTo(bw);
            bw.Write((UInt16)type);
            bw.Write((UInt16)machine);
            bw.Write((UInt32)version);
            bw.Write((UInt32)entry);
            // プログラムヘッダテーブルへのオフセット
            if (phnum != 0) {
                bw.Write((UInt32)phoff);
            } else {
                bw.Write((UInt32)0);
            }
            // セクションヘッダテーブルへのオフセット
            if (shnum != 0) {
                bw.Write((UInt32)shoff);
            } else {
                bw.Write((UInt32)0);
            }
            // フラグ（プロセッサ定義）
            bw.Write((UInt32)flags);
            // ELFヘッダのサイズ
            bw.Write((UInt16)ehsize);
            // プログラムヘッダのサイズ
            bw.Write((UInt16)ProgramHeader.TypeSize);
            // プログラムヘッダの個数
            bw.Write((UInt16)phnum);
            // セクションヘッダのサイズ
            bw.Write((UInt16)SectionHeader.TypeSize);
            // セクションヘッダの個数
            bw.Write((UInt16)shnum);
            // セクションヘッダの名前がある文字列テーブルのインデクス
            bw.Write((UInt16)shstrndx);
        }

    }
}