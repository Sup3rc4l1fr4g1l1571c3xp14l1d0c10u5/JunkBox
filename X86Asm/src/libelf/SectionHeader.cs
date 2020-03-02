using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using X86Asm.util;

namespace X86Asm.libelf
{
    public class SectionHeader {
        public const int TypeSize = 40;

        /// <summary>
        /// セクション名（セクションヘッダテーブルのインデックス）
        /// </summary>
		public UInt32 name { get; set; }

        /// <summary>
        /// セクションの種別
        /// </summary>
        public SectionType Type { get; set; }

        /// <summary>
        /// セクションのフラグ
        /// </summary>
        public UInt32 Flags { get; set; }

        /// <summary>
        /// セクションがメモリ上に配置される場合のアドレス
        /// </summary>
        public UInt32 Addr { get; set; }

        /// <summary>
        /// セクションのファイル先頭からのバイトオフセット
        /// （SHT_NOBITS がセットされている場合はファイル上に実体を持たない）
        /// </summary>
        public UInt32 Offset  { get; set; }

        /// <summary>
        /// セクションのサイズ
        /// （SHT_NOBITS がセットされている場合はファイル上に実体を持たない）
        /// </summary>
        public UInt32 Size  { get; set; }

        /// <summary>
        /// セクションヘッダテーブルインデックスリンク（意味はセクションタイプに依存する）
        /// </summary>
        public UInt32 Link  { get; set; }

        /// <summary>
        /// セクションの追加情報（意味はセクションタイプに依存する）
        /// </summary>
        public UInt32 Info  { get; set; }

        /// <summary>
        /// セクションのアライメント
        /// </summary>
        public UInt32 AddrAlign { get; set; }

        /// <summary>
        /// セクションが固定サイズのエントリのテーブルを持つ場合のサイズ
        /// </summary>
        public UInt32 EntSize  { get; set; }

        public void WriteTo(BinaryWriter bw) {
            bw.Write((UInt32)name);
            bw.Write((UInt32)Type);
            bw.Write((UInt32)Flags);
            bw.Write((UInt32)Addr);
            bw.Write((UInt32)Offset);
            bw.Write((UInt32)Size);
            bw.Write((UInt32)Link);
            bw.Write((UInt32)Info);
            bw.Write((UInt32)AddrAlign);
            bw.Write((UInt32)EntSize);
        }
	}
}