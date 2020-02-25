using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace X86Asm.libcoff {
    /// <summary>
    /// シンボル情報
    /// </summary>
    public class _IMAGE_SYMBOL {
        public const int Size = _SYMBOL_NAME.Size + 4 + 2 + 2 + 1 + 1;

        /// <summary>
        /// シンボル名情報
        /// </summary>
        public _SYMBOL_NAME N { get; private set; }

        /// <summary>
        /// シンボルの種類によって意味が変わる値
        /// </summary>
        public UInt32 Value { get; set; }

        /// <summary>
        /// このシンボルがどのセクションに定義されているかを示す番号（1-based の セクションテーブルのインデックス）
        /// IMAGE_SYM_UNDEFINED (=0) の場合は セクションに属さないデータや別のどこかで定義されている外部シンボルへの参照を 意味する
        /// 特別な意味を持つ負数が入る場合もある
        /// </summary>
        public Int16 SectionNumber { get; set; }

        /// <summary>
        /// 上位バイト、下位バイトでシンボルの型を示す。
        /// </summary>
        public UInt16 Type { get; set; }

        /// <summary>
        /// 記憶域クラスを示す値
        /// </summary>
        public Byte StorageClass { get; set; }

        /// <summary>
        /// 補助シンボル情報の数
        /// </summary>
        public Byte NumberOfAuxSymbols { get; set; }

        public static _IMAGE_SYMBOL ReadFrom(BinaryReader br) {
            return new _IMAGE_SYMBOL() {
        N = _SYMBOL_NAME.ReadFrom(br),
        Value = br.ReadUInt32(),
        SectionNumber = br.ReadInt16(),
        Type = br.ReadUInt16(),
        StorageClass = br.ReadByte(),
        NumberOfAuxSymbols = br.ReadByte()

    };
        }

        public void WriteTo(BinaryWriter bw) {
            N.WriteTo(bw);
            bw.Write((UInt32)Value);
            bw.Write((short)SectionNumber);
            bw.Write((UInt16)Type);
            bw.Write((byte)StorageClass);
            bw.Write((byte)NumberOfAuxSymbols);
        }


    }
}
