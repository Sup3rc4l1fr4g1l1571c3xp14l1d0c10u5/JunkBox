using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace X86Asm.libcoff {
    public class _SYMBOL_NAME {
        public const int Size = 8;

        /// <summary>
        /// '\0'を含まないシンボル名の文字数が8以内の場合、このメンバにシンボル名が格納される
        /// </summary>
        public byte[] ShortName { get; set; } = new byte[8];

        /// <summary>
        /// シンボル名が 8 文字を越える場合、ロングシンボル名が格納されているロングシンボル名テーブルの先頭からのオフセット
        /// </summary>
        public UInt32[] LongName {
            get { return new[] { Short, Long }; }
        }

        /// <summary>
        /// このフィールドが０ならシンボル名が 8 文字を越える
        /// </summary>
        public UInt32 Short {
            get { return BitConverter.ToUInt32(ShortName, 0); }
            set { Array.Copy(BitConverter.GetBytes(value), 0, ShortName, 0, 4); }
        }

        /// <summary>
        /// シンボル名が 8 文字を越える場合、ロングシンボル名が格納されているロングシンボル名テーブルの先頭からのオフセット
        /// </summary>
        public UInt32 Long {
            get { return BitConverter.ToUInt32(ShortName, 4); }
            set { Array.Copy(BitConverter.GetBytes(value), 0, ShortName, 4, 4); }
        }

        public static _SYMBOL_NAME ReadFrom(BinaryReader br) {
            return new _SYMBOL_NAME() {
                ShortName = br.ReadBytes(8)
            };
        }

        public void WriteTo(BinaryWriter bw) {
            bw.Write(ShortName);
        }

    }
}
