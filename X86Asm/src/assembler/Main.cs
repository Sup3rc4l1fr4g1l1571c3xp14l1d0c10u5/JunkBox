using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using X86Asm.libcoff;

namespace X86Asm {
    using X86Asm.ast;
    using X86Asm.libelf;

    public sealed class Assembler {
        public static void Main(string[] args) {
            if (System.Diagnostics.Debugger.IsAttached) {
                DebugMain(args);
            } else {
                NormalMain(args);
            }
        }

        private static void NormalMain(string[] args) {
            if (args.Length != 2) {
                Console.Error.WriteLine("Usage: X86Asm <InputFile> <OutputFile>");
                Environment.Exit(1);
            }

            using (var inputFile = File.OpenRead(args[0]))
            using (var outputFile = File.OpenWrite(args[1])) {

                Program program = parser.Parser.ParseFile(inputFile);

                {
                    uint ENTRY_POINT = 0x00100000 + 0x54;
                    var code = generator.Assembler.assemble(program, ENTRY_POINT);

                    /* pe-i386�`���̃I�u�W�F�N�g�t�@�C���𐶐� */
                    libcoff._IMAGE_FILE_HEADER fileHeader = new _IMAGE_FILE_HEADER() {
                        Machine = IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_I386,
                        NumberOfSections = 3,   // .text, .bss, .data �Ō��ߑł��ɂ��Ă���
                        TimeDateStamp = 0,
                        PointerToSymbolTable = 0xFFFFFFFF,  // ��Ŗ��߂�
                        NumberOfSymbols = 0xFFFFFFFF,  // ��Ŗ��߂�
                        SizeOfOptionalHeader = 0,   // �I�u�W�F�N�g�t�@�C���ł�0�Œ�
                        Characteristics = IMAGE_FILE_CHARACTERISTICS.IMAGE_FILE_LINE_NUMS_STRIPPED | IMAGE_FILE_CHARACTERISTICS.IMAGE_FILE_32BIT_MACHINE
                    };
                    libcoff._IMAGE_SECTION_HEADER textSectionHeader = new _IMAGE_SECTION_HEADER() {
                        Name = new char[8] { '.', 't', 'e', 'x', 't', '\0', '\0', '\0' },
                        VirtualSize = 0,
                        VirtualAddress = 0,
                        SizeOfRawData = (uint)code.Length,
                        PointerToRawData = _IMAGE_FILE_HEADER.Size + SectionHeader.TypeSize * 3,
                        PointerToRelocations = 0xFFFFFFFF,// ��Ŗ��߂�
                        PointerToLinenumbers = 0, // IMAGE_FILE_LINE_NUMS_STRIPPED�Ȃ̂�
                        NumberOfRelocations = 0xFFFF,// ��Ŗ��߂�
                        NumberOfLinenumbers = 0,// IMAGE_FILE_LINE_NUMS_STRIPPED�Ȃ̂�
                        Characteristics = (DataSectionFlags)0x60300020  // ��ŕ���
                    };
                }
                {
                    /* elf�`���̎��s�t�@�C���𐶐� */

                    /// �v���O�����̃G���g���|�C���g
                    /// ����͈ȉ��̑O���ELF�𐶐����Ă���B
                    /// �Eelf�t�@�C�����̂��x�[�X�A�h���X0x00100000�Ƀ��[�h�����Ƒz��
                    /// �E��������elf�t�@�C���̍\���� [ELF�t�@�C���w�b�_; �v���O�����Z�N�V�����w�b�_; �f�[�^�Z�O�����g] �ɂȂ��Ă���
                    /// ����ăG���g���|�C���g��0x00100000(�x�[�X�A�h���X) + 0x54 (ELF�t�@�C���w�b�_�T�C�Y+�v���O�����Z�N�V�����w�b�_�T�C�Y=�f�[�^�Z�O�����g�J�n�A�h���X�܂ł̃I�t�Z�b�g)�ɂȂ�
                    uint ENTRY_POINT = 0x00100000 + 0x54;

                    var code = generator.Assembler.assemble(program, ENTRY_POINT);


                    // ELF�t�@�C���w�b�_�����
                    ElfFile elf = new ElfFile() {
                        elfHeader = new ElfHeader() {
                            type = ObjectType.ET_EXEC,
                            entry = ENTRY_POINT,
                            shstrndx = 0
                        }
                    };

                    // �v���O�����w�b�_�𐶐�
                    ProgramHeader ph = new ProgramHeader() {
                        Type = SegmentType.PT_LOAD,
                        VAddr = ENTRY_POINT,
                        FileSz = (UInt32)code.Length,
                        MemSz = (UInt32)code.Length,
                        Flags = SegmentFlag.PF_X | SegmentFlag.PF_R | SegmentFlag.PF_W,
                        Align = 0x1000,
                    };
                    // �v���O�����w�b�_��ǉ����A�I�t�Z�b�g���X�V
                    elf.programHeaders.Add(ph);
                    ph.Offset = (UInt32)elf.DataOffset;

                    // �v���O�����w�b�_�ɑΉ�����Z�O�����g�ɐ��������@�B���ݒ�
                    elf.data = code;

                    // �������݂��s��
                    var bytes = elf.toBytes();
                    outputFile.Write(bytes, 0, bytes.Length);

                }
            }
        }

        private static void DebugMain(string[] args) {
            CoffDump.Dump(@"C:\Users\whelp\Desktop\X86Asm\test\test.o");
            NormalMain(new[] { @"C:\Users\whelp\Desktop\X86Asm\test\count.s", @"C:\Users\whelp\Desktop\X86Asm\test\count.elf" });
            System.Diagnostics.Debug.Assert(FileDiff(@"C:\Users\whelp\Desktop\X86Asm\test\count.elf", @"C:\Users\whelp\Desktop\X86Asm\test\count.elf.org"));
        }

        public static bool FileDiff(string file1, string file2) {
            byte[] binary1 = System.IO.File.ReadAllBytes(file1);
            byte[] binary2 = System.IO.File.ReadAllBytes(file2);
            return binary1.SequenceEqual(binary2);
        }

    }
}