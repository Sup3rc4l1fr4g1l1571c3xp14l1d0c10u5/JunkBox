using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using X86Asm.util;

namespace X86Asm.libelf {

    /// <summary>
    /// ELF�t�@�C��
    /// </summary>
    public sealed class ElfFile {

        /// <summary>
        /// ELF�t�@�C���w�b�_
        /// </summary>
        public ElfHeader elfHeader { get; set; }

        /// <summary>
        /// �v���O�����w�b�_
        /// </summary>
        public IList<ProgramHeader> programHeaders { get; }

        /// <summary>
        /// �Z�N�V�����w�b�_
        /// </summary>
        public IList<SectionHeader> sectionHeaders { get; }

        /// <summary>
        /// �f�[�^�Z�O�����g�i����̌`���ł̓v���O�����w�b�_�ƃZ�N�V�����w�b�_�̒���Ƀv���O�����w�b�_�ɑΉ�����f�[�^�Z�O�����g��z�u���Ă���j
        /// </summary>
        public byte[] data { get; set; }

        /// <summary>
        /// �R���X�g���N�^
        /// </summary>
        public ElfFile() {
            programHeaders = new List<ProgramHeader>();
            sectionHeaders = new List<SectionHeader>();
        }

        /// <summary>
        /// �f�[�^�܂ł̃I�t�Z�b�g
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
        /// �o�C�g��擾
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