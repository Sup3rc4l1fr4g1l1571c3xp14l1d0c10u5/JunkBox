using System;
using System.IO;
using System.Runtime.InteropServices;
using X86Asm.util;

namespace X86Asm.libelf {


    /// <summary>
    /// ELF�w�b�_
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
        /// �I�u�W�F�N�g�t�@�C���̎��
        /// </summary>
        public ObjectType type { get; set; }

        /// <summary>
        /// �A�[�L�e�N�`��
        /// </summary>
        public ElfMachine machine  { get; set; } = ElfMachine.EM_386;

        /// <summary>
        /// �o�[�W����
        /// </summary>
        public ElfVersion version  { get; set; } = ElfVersion.EV_CURRENT;

        /// <summary>
        /// �G���g���|�C���g�i���z�A�h���X�j
        /// </summary>
        public uint entry { get; set; } 

        /// <summary>
        /// �t���O�ix86��X64�ł͎g���Ă��Ȃ��j
        /// </summary>
        public uint flags { get; set; } = 0;

        /// <summary>
        /// ELF�w�b�_�̃T�C�Y
        /// </summary>
        public short ehsize { get; set; } = Size;

        /// <summary>
        /// �Z�N�V�����w�b�_�̖��O�����镶����e�[�u���̃C���f�N�X
        /// </summary>
        public short shstrndx { get; set; } 

        /// <summary>
        /// �o�C�i���X�g���[���ւ̏�������
        /// </summary>
        /// <param name="bw"></param>
        /// <param name="phnum"></param>
        /// <param name="shnum"></param>
        public void WriteTo(BinaryWriter bw, ushort phnum, ushort shnum) {
            uint phoff = Size;
            uint shoff = phoff + phnum * (uint)ProgramHeader.TypeSize;

            // e_ident �̏�������
            ident.WriteTo(bw);
            bw.Write((UInt16)type);
            bw.Write((UInt16)machine);
            bw.Write((UInt32)version);
            bw.Write((UInt32)entry);
            // �v���O�����w�b�_�e�[�u���ւ̃I�t�Z�b�g
            if (phnum != 0) {
                bw.Write((UInt32)phoff);
            } else {
                bw.Write((UInt32)0);
            }
            // �Z�N�V�����w�b�_�e�[�u���ւ̃I�t�Z�b�g
            if (shnum != 0) {
                bw.Write((UInt32)shoff);
            } else {
                bw.Write((UInt32)0);
            }
            // �t���O�i�v���Z�b�T��`�j
            bw.Write((UInt32)flags);
            // ELF�w�b�_�̃T�C�Y
            bw.Write((UInt16)ehsize);
            // �v���O�����w�b�_�̃T�C�Y
            bw.Write((UInt16)ProgramHeader.TypeSize);
            // �v���O�����w�b�_�̌�
            bw.Write((UInt16)phnum);
            // �Z�N�V�����w�b�_�̃T�C�Y
            bw.Write((UInt16)SectionHeader.TypeSize);
            // �Z�N�V�����w�b�_�̌�
            bw.Write((UInt16)shnum);
            // �Z�N�V�����w�b�_�̖��O�����镶����e�[�u���̃C���f�N�X
            bw.Write((UInt16)shstrndx);
        }

    }
}