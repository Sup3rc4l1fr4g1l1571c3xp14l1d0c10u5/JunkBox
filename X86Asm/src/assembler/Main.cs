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

                    /* pe-i386形式のオブジェクトファイルを生成 */
                    libcoff._IMAGE_FILE_HEADER fileHeader = new _IMAGE_FILE_HEADER() {
                        Machine = IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_I386,
                        NumberOfSections = 3,   // .text, .bss, .data で決め打ちにしている
                        TimeDateStamp = 0,
                        PointerToSymbolTable = 0xFFFFFFFF,  // 後で埋める
                        NumberOfSymbols = 0xFFFFFFFF,  // 後で埋める
                        SizeOfOptionalHeader = 0,   // オブジェクトファイルでは0固定
                        Characteristics = IMAGE_FILE_CHARACTERISTICS.IMAGE_FILE_LINE_NUMS_STRIPPED | IMAGE_FILE_CHARACTERISTICS.IMAGE_FILE_32BIT_MACHINE
                    };
                    libcoff._IMAGE_SECTION_HEADER textSectionHeader = new _IMAGE_SECTION_HEADER() {
                        Name = new char[8] { '.', 't', 'e', 'x', 't', '\0', '\0', '\0' },
                        VirtualSize = 0,
                        VirtualAddress = 0,
                        SizeOfRawData = (uint)code.Length,
                        PointerToRawData = _IMAGE_FILE_HEADER.Size + SectionHeader.TypeSize * 3,
                        PointerToRelocations = 0xFFFFFFFF,// 後で埋める
                        PointerToLinenumbers = 0, // IMAGE_FILE_LINE_NUMS_STRIPPEDなので
                        NumberOfRelocations = 0xFFFF,// 後で埋める
                        NumberOfLinenumbers = 0,// IMAGE_FILE_LINE_NUMS_STRIPPEDなので
                        Characteristics = (DataSectionFlags)0x60300020  // 後で分析
                    };
                }
                {
                    /* elf形式の実行ファイルを生成 */

                    /// プログラムのエントリポイント
                    /// 今回は以下の前提でELFを生成している。
                    /// ・elfファイル自体がベースアドレス0x00100000にロードされると想定
                    /// ・生成するelfファイルの構造は [ELFファイルヘッダ; プログラムセクションヘッダ; データセグメント] になっている
                    /// よってエントリポイントは0x00100000(ベースアドレス) + 0x54 (ELFファイルヘッダサイズ+プログラムセクションヘッダサイズ=データセグメント開始アドレスまでのオフセット)になる
                    uint ENTRY_POINT = 0x00100000 + 0x54;

                    var code = generator.Assembler.assemble(program, ENTRY_POINT);


                    // ELFファイルヘッダを作る
                    ElfFile elf = new ElfFile() {
                        elfHeader = new ElfHeader() {
                            type = ObjectType.ET_EXEC,
                            entry = ENTRY_POINT,
                            shstrndx = 0
                        }
                    };

                    // プログラムヘッダを生成
                    ProgramHeader ph = new ProgramHeader() {
                        Type = SegmentType.PT_LOAD,
                        VAddr = ENTRY_POINT,
                        FileSz = (UInt32)code.Length,
                        MemSz = (UInt32)code.Length,
                        Flags = SegmentFlag.PF_X | SegmentFlag.PF_R | SegmentFlag.PF_W,
                        Align = 0x1000,
                    };
                    // プログラムヘッダを追加し、オフセットを更新
                    elf.programHeaders.Add(ph);
                    ph.Offset = (UInt32)elf.DataOffset;

                    // プログラムヘッダに対応するセグメントに生成した機械語を設定
                    elf.data = code;

                    // 書き込みを行う
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