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
                args = new[] { @"C:\Users\whelp\Desktop\X86Asm\test\magic.s", @"C:\Users\whelp\Desktop\X86Asm\test\magic.obj" };
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
                    var data = new byte[0];
                    var bssSIze = 0;
                    var symbols = new List<_IMAGE_SYMBOL>() {
                        new _IMAGE_SYMBOL() {
                            N = new _SYMBOL_NAME() { ShortName = new byte[8] { (byte)'_', (byte)'m', (byte)'a', (byte)'g', (byte)'i', (byte)'c', 0,0 } },
                            Value = 0,
                            SectionNumber = 1,  // .textセクションの番号を１起点で
                            Type = _IMAGE_SYMBOL._SYMBOL_TYPE.DT_FCN,   // 型は関数
                            StorageClass = _IMAGE_SYMBOL._SYMBOL_STORAGE_CLASS.C_EXT,   // 外部結合
                            NumberOfAuxSymbols = 0
                        }
                    };

                    var symbolTablePos = _IMAGE_FILE_HEADER.Size + SectionHeader.TypeSize * 3 + (uint)code.Length + (uint)data.Length + (uint)bssSIze;
                    var relocationTablePos = symbolTablePos + (uint)(symbols.Count * _IMAGE_SYMBOL.Size);
                    var longSymbolTable = new List<byte>();
                    
                    /* pe-i386形式のオブジェクトファイルを生成 */
                    libcoff._IMAGE_FILE_HEADER fileHeader = new _IMAGE_FILE_HEADER() {
                        Machine = IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_I386,
                        NumberOfSections = 3,   // .text, .bss, .data で決め打ちにしている
                        TimeDateStamp = 0,
                        PointerToSymbolTable = symbolTablePos,
                        NumberOfSymbols = (uint)(symbols.Count),
                        SizeOfOptionalHeader = 0,   // オブジェクトファイルでは0固定
                        Characteristics = IMAGE_FILE_CHARACTERISTICS.IMAGE_FILE_LINE_NUMS_STRIPPED | IMAGE_FILE_CHARACTERISTICS.IMAGE_FILE_32BIT_MACHINE
                    };
                    List<libcoff._IMAGE_RELOCATION> textSectionRelocations = new List<libcoff._IMAGE_RELOCATION> {
                    };
                    libcoff._IMAGE_SECTION_HEADER textSectionHeader = new _IMAGE_SECTION_HEADER() {
                        Name = new char[8] { '.', 't', 'e', 'x', 't', '\0', '\0', '\0' },
                        VirtualSize = 0,
                        VirtualAddress = 0,
                        SizeOfRawData = (uint)code.Length,
                        PointerToRawData = _IMAGE_FILE_HEADER.Size + SectionHeader.TypeSize * 3,
                        PointerToRelocations = relocationTablePos,
                        PointerToLinenumbers = 0, // IMAGE_FILE_LINE_NUMS_STRIPPEDなので0とする
                        NumberOfRelocations = (UInt16)textSectionRelocations.Count,
                        NumberOfLinenumbers = 0,// IMAGE_FILE_LINE_NUMS_STRIPPEDなので0とする
                        Characteristics = DataSectionFlags.MemoryExecute | DataSectionFlags.MemoryRead | DataSectionFlags.Align4Bytes | DataSectionFlags.ContentCode
                    };
                    List<libcoff._IMAGE_RELOCATION> dataSectionRelocations = new List<libcoff._IMAGE_RELOCATION> {
                    };
                    libcoff._IMAGE_SECTION_HEADER dataSectionHeader = new _IMAGE_SECTION_HEADER() {
                        Name = new char[8] { '.', 'd', 'a', 't', 'a', '\0', '\0', '\0' },
                        VirtualSize = 0,
                        VirtualAddress = 0,
                        SizeOfRawData = (uint)data.Length,  // データセクションの大きさ
                        PointerToRawData = _IMAGE_FILE_HEADER.Size + SectionHeader.TypeSize * 3 + (uint)code.Length,
                        PointerToRelocations = (uint)(relocationTablePos + textSectionRelocations.Count() * _IMAGE_RELOCATION.Size),
                        PointerToLinenumbers = 0, // IMAGE_FILE_LINE_NUMS_STRIPPEDなので0とする
                        NumberOfRelocations = (UInt16)dataSectionRelocations.Count(),
                        NumberOfLinenumbers = 0,// IMAGE_FILE_LINE_NUMS_STRIPPEDなので0とする
                        Characteristics = DataSectionFlags.MemoryWrite | DataSectionFlags.MemoryRead | DataSectionFlags.Align4Bytes | DataSectionFlags.ContentInitializedData
                    };
                    List<libcoff._IMAGE_RELOCATION> bssSectionRelocations = new List<libcoff._IMAGE_RELOCATION> {
                    };
                    libcoff._IMAGE_SECTION_HEADER bssSectionHeader = new _IMAGE_SECTION_HEADER() {
                        Name = new char[8] { '.', 'b', 's', 's', '\0', '\0', '\0', '\0' },
                        VirtualSize = 0,
                        VirtualAddress = 0,
                        SizeOfRawData = (uint)bssSIze,  // BSSセクションの大きさ
                        PointerToRawData = _IMAGE_FILE_HEADER.Size + SectionHeader.TypeSize * 3 + (uint)code.Length + (uint)data.Length,
                        PointerToRelocations = (uint)(relocationTablePos + textSectionRelocations.Count() * _IMAGE_RELOCATION.Size + dataSectionRelocations.Count() * _IMAGE_RELOCATION.Size),
                        PointerToLinenumbers = 0, // IMAGE_FILE_LINE_NUMS_STRIPPEDなので0とする
                        NumberOfRelocations = (UInt16)bssSectionRelocations.Count(),
                        NumberOfLinenumbers = 0,// IMAGE_FILE_LINE_NUMS_STRIPPEDなので0とする
                        Characteristics = DataSectionFlags.MemoryExecute | DataSectionFlags.MemoryRead | DataSectionFlags.Align4Bytes | DataSectionFlags.ContentUninitializedData
                    };

                    using (var bw = new BinaryWriter(outputFile)) {
                        fileHeader.WriteTo(bw);
                        bw.WriteTo(textSectionHeader);
                        bw.WriteTo(dataSectionHeader);
                        bw.WriteTo(bssSectionHeader);
                        bw.Write(code.ToArray());
                        foreach (var symbol in symbols) {
                            symbol.WriteTo(bw);
                        }
                        foreach (var rel in textSectionRelocations) {
                            rel.WriteTo(bw);
                        }
                        foreach (var rel in dataSectionRelocations) {
                            rel.WriteTo(bw);
                        }
                        foreach (var rel in bssSectionRelocations) {
                            rel.WriteTo(bw);
                        }
                        bw.Write((UInt32)longSymbolTable.Count());
                        bw.Write(longSymbolTable.ToArray());
                        
                    }
                    return;
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
            NormalMain(new[] { @"C:\Users\whelp\Desktop\X86Asm\test\magic.s", @"C:\Users\whelp\Desktop\X86Asm\test\magic.obj" });
            System.Diagnostics.Debug.Assert(FileDiff(@"C:\Users\whelp\Desktop\X86Asm\test\count.elf", @"C:\Users\whelp\Desktop\X86Asm\test\count.elf.org"));
        }

        public static bool FileDiff(string file1, string file2) {
            byte[] binary1 = System.IO.File.ReadAllBytes(file1);
            byte[] binary2 = System.IO.File.ReadAllBytes(file2);
            return binary1.SequenceEqual(binary2);
        }

    }
}