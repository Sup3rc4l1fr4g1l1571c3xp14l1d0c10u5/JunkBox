using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using X86Asm.util;

namespace X86Asm.libelf {

    /// <summary>
    /// ELF�v���O�����w�b�_
    /// </summary>
	public class ProgramHeader {
        public const int TypeSize = 32;

        /// <summary>
        /// �Z�O�����g�̎��
        /// </summary>
        public SegmentType Type { get; set; }

        /// <summary>
        /// �Z�O�����g�̃t�@�C���I�t�Z�b�g
        /// </summary>
        public UInt32 Offset { get; set; }

        /// <summary>
        /// �Z�O�����g�̉��z�A�h���X
        /// </summary>
        public UInt32 VAddr { get; set; }

        /// <summary>
        /// �Z�O�����g�̕����A�h���X(x86/x64�ł͎g���Ȃ�)
        /// </summary>
        public UInt32 PAddr { get; set; }

        /// <summary>
        /// �Z�O�����g�̃t�@�C���T�C�Y�i�[���̏ꍇ������j
        /// </summary>
        public UInt32 FileSz{ get; set; }

        /// <summary>
        /// �Z�O�����g�̃������T�C�Y�i�[���̏ꍇ������j
        /// </summary>
        public UInt32 MemSz { get; set; }

        /// <summary>
        /// �Z�O�����g�̑���
        /// </summary>
        public SegmentFlag Flags { get; set; }

        /// <summary>
        /// �Z�O�����g�̃A���C�����g�i�t�@�C���A�����������j
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