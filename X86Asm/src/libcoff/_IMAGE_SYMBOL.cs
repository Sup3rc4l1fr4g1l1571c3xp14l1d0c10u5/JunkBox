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
        public _SYMBOL_NAME N { get; set; }

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
        public _SYMBOL_TYPE Type { get; set; }

        /// <summary>
        /// 記憶域クラスを示す値
        /// </summary>
        public _SYMBOL_STORAGE_CLASS StorageClass { get; set; }

        /// <summary>
        /// 補助シンボル情報の数
        /// </summary>
        public Byte NumberOfAuxSymbols { get; set; }

        public static _IMAGE_SYMBOL ReadFrom(BinaryReader br) {
            return new _IMAGE_SYMBOL() {
                N = _SYMBOL_NAME.ReadFrom(br),
                Value = br.ReadUInt32(),
                SectionNumber = br.ReadInt16(),
                Type = (_SYMBOL_TYPE)br.ReadUInt16(),
                StorageClass = (_SYMBOL_STORAGE_CLASS)br.ReadByte(),
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


        public enum _SYMBOL_TYPE : UInt16 {
            T_NULL = 0x00, // No symbol
            T_VOID = 0x01, // void function argument (not used)
            T_CHAR = 0x02, // character
            T_SHORT = 0x03, // short integer
            T_INT = 0x04, // integer
            T_LONG = 0x05, // long integer
            T_FLOAT = 0x06, // floating point
            T_DOUBLE = 0x07, // double precision float
            T_STRUCT = 0x08, // structure
            T_UNION = 0x09, // union
            T_ENUM = 0x0A, // enumeration
            T_MOE = 0x0B, // member of enumeration
            T_UCHAR = 0x0C, // unsigned character
            T_USHORT = 0x0D, // unsigned short
            T_UINT = 0x0E, // unsigned integer
            T_ULONG = 0x0F, // unsigned long
            T_LNGDBL = 0x10, // long double (special case bit pattern)
            DT_NON = 0x00, // No derived type
            DT_PTR = 0x10, // pointer to T
            DT_FCN = 0x20, // function returning T
            DT_ARY = 0x30, // array of T

        }

        public enum _SYMBOL_STORAGE_CLASS {
            C_NULL = 0, //No entry
            C_AUTO = 1, //Automatic variable
            C_EXT = 2, //External (public) symbol - this covers globals and externs
            C_STAT = 3, //static (private) symbol
            C_REG = 4, //register variable
            C_EXTDEF = 5, //External definition
            C_LABEL = 6, //label
            C_ULABEL = 7, //undefined label
            C_MOS = 8, //member of structure
            C_ARG = 9, //function argument
            C_STRTAG = 10, //structure tag
            C_MOU = 11, //member of union
            C_UNTAG = 12, //union tag
            C_TPDEF = 13, //type definition
            C_USTATIC = 14, //undefined static
            C_ENTAG = 15, //enumaration tag
            C_MOE = 16, //member of enumeration
            C_REGPARM = 17, //register parameter
            C_FIELD = 18, //bit field
            C_AUTOARG = 19, //auto argument
            C_LASTENT = 20, //dummy entry (end of block)
            C_BLOCK = 100, //".bb" or ".eb" - beginning or end of block
            C_FCN = 101, //".bf" or ".ef" - beginning or end of function
            C_EOS = 102, //end of structure
            C_FILE = 103, //file name
            C_LINE = 104, //line number, reformatted as symbol
            C_ALIAS = 105, //duplicate tag
            C_HIDDEN = 106, //ext symbol in dmert public lib
            C_EFCN = 255, //physical end of function
        }
    }
}
