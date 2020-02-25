using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using X86Asm.util;

namespace X86Asm.libelf {

    /// <summary>
    /// ELFファイル
    /// </summary>
    public sealed class ElfFile {

        /// <summary>
        /// ELFファイルヘッダ
        /// </summary>
        public ElfHeader elfHeader { get; set; }

        /// <summary>
        /// プログラムヘッダ
        /// </summary>
        public IList<ProgramHeader> programHeaders { get; }

        /// <summary>
        /// セクションヘッダ
        /// </summary>
        public IList<SectionHeader> sectionHeaders { get; }

        /// <summary>
        /// データセグメント（今回の形式ではプログラムヘッダとセクションヘッダの直後にプログラムヘッダに対応するデータセグメントを配置している）
        /// </summary>
        public byte[] data { get; set; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public ElfFile() {
            programHeaders = new List<ProgramHeader>();
            sectionHeaders = new List<SectionHeader>();
        }

        /// <summary>
        /// データまでのオフセット
        /// </summary>
        public int DataOffset {
            get {
                int result = 0;
                result += ElfHeader.Size;
                result += programHeaders.Count * ProgramHeader.TypeSize;
                result += sectionHeaders.Count * SectionHeader.TypeSize;
                return result;
            }
        }

        /// <summary>
        /// バイト列取得
        /// </summary>
        /// <returns></returns>
        public byte[] toBytes() {
            using (var ms = new MemoryStream()) 
            using (var bw = new BinaryWriter(ms)) {
                elfHeader.WriteTo(bw, (ushort)programHeaders.Count, (ushort)sectionHeaders.Count);
                foreach (ProgramHeader ph in programHeaders) {
                    ph.WriteTo(bw);
                }
                foreach (SectionHeader sh in sectionHeaders) {
                    sh.WriteTo(bw);
                }
                bw.Write(data);

                return ms.ToArray();
            }
        }

    }

}