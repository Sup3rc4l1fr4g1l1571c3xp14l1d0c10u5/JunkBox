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
        /// �Z�N�V�������i�Z�N�V�����w�b�_�e�[�u���̃C���f�b�N�X�j
        /// </summary>
		public UInt32 name { get; set; }

        /// <summary>
        /// �Z�N�V�����̎��
        /// </summary>
        public SectionType Type { get; set; }

        /// <summary>
        /// �Z�N�V�����̃t���O
        /// </summary>
        public UInt32 Flags { get; set; }

        /// <summary>
        /// �Z�N�V��������������ɔz�u�����ꍇ�̃A�h���X
        /// </summary>
        public UInt32 Addr { get; set; }

        /// <summary>
        /// �Z�N�V�����̃t�@�C���擪����̃o�C�g�I�t�Z�b�g
        /// �iSHT_NOBITS ���Z�b�g����Ă���ꍇ�̓t�@�C����Ɏ��̂������Ȃ��j
        /// </summary>
        public UInt32 Offset  { get; set; }

        /// <summary>
        /// �Z�N�V�����̃T�C�Y
        /// �iSHT_NOBITS ���Z�b�g����Ă���ꍇ�̓t�@�C����Ɏ��̂������Ȃ��j
        /// </summary>
        public UInt32 Size  { get; set; }

        /// <summary>
        /// �Z�N�V�����w�b�_�e�[�u���C���f�b�N�X�����N�i�Ӗ��̓Z�N�V�����^�C�v�Ɉˑ�����j
        /// </summary>
        public UInt32 Link  { get; set; }

        /// <summary>
        /// �Z�N�V�����̒ǉ����i�Ӗ��̓Z�N�V�����^�C�v�Ɉˑ�����j
        /// </summary>
        public UInt32 Info  { get; set; }

        /// <summary>
        /// �Z�N�V�����̃A���C�����g
        /// </summary>
        public UInt32 AddrAlign { get; set; }

        /// <summary>
        /// �Z�N�V�������Œ�T�C�Y�̃G���g���̃e�[�u�������ꍇ�̃T�C�Y
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