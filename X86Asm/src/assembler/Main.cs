using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace X86Asm {
    using X86Asm.ast;
    using X86Asm.libelf;
    using X86Asm.libcoff;
    using X86Asm.model;

    public sealed class Assembler {
        public static void Main(string[] args) {
            if (System.Diagnostics.Debugger.IsAttached) {
                args = new[] { @"..\..\test\hello.s", @"..\..\test\hello.obj" };
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
                    List<Section> sections = new List<Section>() {
                        new Section() { name = ".text", index =0},
                        new Section() { name = ".data", index =1},
                        new Section() { name = ".bss", index =2},
                        new Section() { name = ".rdata", index =3},
                    };
                    var longSymbolTable = new List<byte>();
                    generator.Assembler.assemble(program, sections);

                    var textSection = sections.FirstOrDefault(x => x.name == ".text");
                    var dataSection = sections.FirstOrDefault(x => x.name == ".data");
                    var bssSection = sections.FirstOrDefault(x => x.name == ".bss");
                    var rdataSection = sections.FirstOrDefault(x => x.name == ".rdata");

                    var sectionSymbols = sections.SelectMany(x => x.symbols).ToList();
                    var symbols = sectionSymbols.Select(x => {
                        var n = new _SYMBOL_NAME();
                        if (x.name.Length > 8) {
                            n.Long = (uint)(longSymbolTable.Count + 4);
                            longSymbolTable.AddRange(x.name.Select(y => (byte)y));
                        } else {
                            n.ShortName = x.name.Select(y => (byte)y).Concat(Enumerable.Repeat((byte)0, 8)).Take(8).ToArray();
                        }
                        return new _IMAGE_SYMBOL() {
                            N = n,
                            Value = x.offset,
                            Type = 0,
                            StorageClass = (x.global) ? _IMAGE_SYMBOL._SYMBOL_STORAGE_CLASS.C_EXT : _IMAGE_SYMBOL._SYMBOL_STORAGE_CLASS.C_STAT,
                            SectionNumber = (short)(x.section == null ? 0 : (x.section.index + 1)),
                            NumberOfAuxSymbols = 0
                        };
                    }).ToArray();

                    var symbolTablePos = _IMAGE_FILE_HEADER.Size + SectionHeader.TypeSize * 4 + (uint)textSection.size + (uint)dataSection.size + (uint)bssSection.size + (uint)rdataSection.size;
                    var relocationTablePos = symbolTablePos + (uint)(symbols.Length * _IMAGE_SYMBOL.Size);

                    /* pe-i386形式のオブジェクトファイルを生成 */

                    libcoff._IMAGE_FILE_HEADER fileHeader = new _IMAGE_FILE_HEADER() {
                        Machine = IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_I386,
                        NumberOfSections = 4,   // .text, .data, .bss .rdata で決め打ちにしている
                        TimeDateStamp = 0,
                        PointerToSymbolTable = symbolTablePos,
                        NumberOfSymbols = (uint)(symbols.Length),
                        SizeOfOptionalHeader = 0,   // オブジェクトファイルでは0固定
                        Characteristics = IMAGE_FILE_CHARACTERISTICS.IMAGE_FILE_LINE_NUMS_STRIPPED | IMAGE_FILE_CHARACTERISTICS.IMAGE_FILE_32BIT_MACHINE
                    };
                    libcoff._IMAGE_SECTION_HEADER textSectionHeader = new _IMAGE_SECTION_HEADER() {
                        Name = new char[8] { '.', 't', 'e', 'x', 't', '\0', '\0', '\0' }.Select(x => (byte)x).ToArray(),
                        VirtualSize = 0,
                        VirtualAddress = 0,
                        SizeOfRawData = (uint)textSection.size,  //textセクションの大きさ
                        PointerToRawData = _IMAGE_FILE_HEADER.Size + SectionHeader.TypeSize *4,
                        PointerToRelocations = relocationTablePos,
                        PointerToLinenumbers = 0, // IMAGE_FILE_LINE_NUMS_STRIPPEDなので0とする
                        NumberOfRelocations = (UInt16)textSection.relocations.Count,
                        NumberOfLinenumbers = 0,// IMAGE_FILE_LINE_NUMS_STRIPPEDなので0とする
                        Characteristics = DataSectionFlags.MemoryExecute | DataSectionFlags.MemoryRead | DataSectionFlags.Align4Bytes | DataSectionFlags.ContentCode
                    };
                    libcoff._IMAGE_SECTION_HEADER dataSectionHeader = new _IMAGE_SECTION_HEADER() {
                        Name = new char[8] { '.', 'd', 'a', 't', 'a', '\0', '\0', '\0' }.Select(x => (byte)x).ToArray(),
                        VirtualSize = 0,
                        VirtualAddress = 0,
                        SizeOfRawData = (uint)dataSection.size,  // dataセクションの大きさ
                        PointerToRawData = _IMAGE_FILE_HEADER.Size + SectionHeader.TypeSize * 4 + (uint)textSection.size,
                        PointerToRelocations = (uint)(relocationTablePos + textSection.relocations.Count() * _IMAGE_RELOCATION.Size),
                        PointerToLinenumbers = 0, // IMAGE_FILE_LINE_NUMS_STRIPPEDなので0とする
                        NumberOfRelocations = (UInt16)dataSection.relocations.Count,
                        NumberOfLinenumbers = 0,// IMAGE_FILE_LINE_NUMS_STRIPPEDなので0とする
                        Characteristics = DataSectionFlags.MemoryWrite | DataSectionFlags.MemoryRead | DataSectionFlags.Align4Bytes | DataSectionFlags.ContentInitializedData
                    };
                    libcoff._IMAGE_SECTION_HEADER bssSectionHeader = new _IMAGE_SECTION_HEADER() {
                        Name = new char[8] { '.', 'b', 's', 's', '\0', '\0', '\0', '\0' }.Select(x => (byte)x).ToArray(),
                        VirtualSize = 0,
                        VirtualAddress = 0,
                        SizeOfRawData = (uint)bssSection.size,  // bssセクションの大きさ
                        PointerToRawData = _IMAGE_FILE_HEADER.Size + SectionHeader.TypeSize * 4 + (uint)textSection.size + (uint)dataSection.size,
                        PointerToRelocations = (uint)(relocationTablePos + textSection.relocations.Count() * _IMAGE_RELOCATION.Size + dataSection.relocations.Count * _IMAGE_RELOCATION.Size),
                        PointerToLinenumbers = 0, // IMAGE_FILE_LINE_NUMS_STRIPPEDなので0とする
                        NumberOfRelocations = (UInt16)bssSection.relocations.Count,
                        NumberOfLinenumbers = 0,// IMAGE_FILE_LINE_NUMS_STRIPPEDなので0とする
                        Characteristics = DataSectionFlags.MemoryRead | DataSectionFlags.MemoryWrite | DataSectionFlags.Align4Bytes | DataSectionFlags.ContentUninitializedData
                    };

                    libcoff._IMAGE_SECTION_HEADER rdataSectionHeader = new _IMAGE_SECTION_HEADER() {
                        Name = new char[8] { '.', 'r', 'd', 'a', 't', 'a', '\0', '\0' }.Select(x => (byte)x).ToArray(),
                        VirtualSize = 0,
                        VirtualAddress = 0,
                        SizeOfRawData = (uint)rdataSection.size,  // rdataセクションの大きさ
                        PointerToRawData = _IMAGE_FILE_HEADER.Size + SectionHeader.TypeSize * 4 + (uint)textSection.size + (uint)dataSection.size + (uint)bssSection.size,
                        PointerToRelocations = (uint)(relocationTablePos + textSection.relocations.Count() * _IMAGE_RELOCATION.Size + dataSection.relocations.Count * _IMAGE_RELOCATION.Size + bssSection.relocations.Count * _IMAGE_RELOCATION.Size),
                        PointerToLinenumbers = 0, // IMAGE_FILE_LINE_NUMS_STRIPPEDなので0とする
                        NumberOfRelocations = (UInt16)rdataSection.relocations.Count,
                        NumberOfLinenumbers = 0,// IMAGE_FILE_LINE_NUMS_STRIPPEDなので0とする
                        Characteristics = DataSectionFlags.MemoryRead | DataSectionFlags.Align4Bytes | DataSectionFlags.ContentUninitializedData
                    };

                    using (var bw = new BinaryWriter(outputFile, System.Text.Encoding.ASCII, true)) {
                        fileHeader.WriteTo(bw);
                        textSectionHeader.WriteTo(bw);
                        dataSectionHeader.WriteTo(bw);
                        bssSectionHeader.WriteTo(bw);
                        rdataSectionHeader.WriteTo(bw);
                        bw.Write(textSection.data.ToArray());
                        bw.Write(dataSection.data.ToArray());
                        bw.Write(bssSection.data.ToArray());
                        bw.Write(rdataSection.data.ToArray());
                        foreach (var symbol in symbols) {
                            symbol.WriteTo(bw);
                        }
                        foreach (var r in textSection.relocations) {
                            var symIndex = sectionSymbols.IndexOf(r.Symbol);
                            if (symIndex == -1) { throw new KeyNotFoundException(); }
                            var rel = new _IMAGE_RELOCATION() {
                                SymbolTableIndex = (uint)symIndex,
                                VirtualAddress = r.Offset,
                                Type = (r.Symbol.section == null ? _IMAGE_REL_I386.IMAGE_REL_I386_REL32: _IMAGE_REL_I386.IMAGE_REL_I386_DIR32),
                            };
                            rel.WriteTo(bw);
                        }
                        foreach (var r in dataSection.relocations) {
                            var symIndex = sectionSymbols.IndexOf(r.Symbol);
                            if (symIndex == -1) { throw new KeyNotFoundException(); }
                            var rel = new _IMAGE_RELOCATION() {
                                SymbolTableIndex = (uint)symIndex,
                                VirtualAddress = r.Offset,
                                Type = (r.Symbol.section == null ? _IMAGE_REL_I386.IMAGE_REL_I386_REL32 : _IMAGE_REL_I386.IMAGE_REL_I386_DIR32),
                            };
                            rel.WriteTo(bw);
                        }
                        foreach (var r in bssSection.relocations) {
                            var symIndex = sectionSymbols.IndexOf(r.Symbol);
                            if (symIndex == -1) { throw new KeyNotFoundException(); }
                            var rel = new _IMAGE_RELOCATION() {
                                SymbolTableIndex = (uint)symIndex,
                                VirtualAddress = r.Offset,
                                Type = (r.Symbol.section == null ? _IMAGE_REL_I386.IMAGE_REL_I386_REL32 : _IMAGE_REL_I386.IMAGE_REL_I386_DIR32),
                            };
                            rel.WriteTo(bw);
                        }
                        foreach (var r in rdataSection.relocations) {
                            var symIndex = sectionSymbols.IndexOf(r.Symbol);
                            if (symIndex == -1) { throw new KeyNotFoundException(); }
                            var rel = new _IMAGE_RELOCATION() {
                                SymbolTableIndex = (uint)symIndex,
                                VirtualAddress = r.Offset,
                                Type = (r.Symbol.section == null ? _IMAGE_REL_I386.IMAGE_REL_I386_REL32 : _IMAGE_REL_I386.IMAGE_REL_I386_DIR32),
                            };
                            rel.WriteTo(bw);
                        }
                        bw.Write((UInt32)longSymbolTable.Count() + 4);
                        bw.Write(longSymbolTable.ToArray());

                    }
                    return;
                }
#if false
                {
                    /* elf形式の実行ファイルを生成 */

                    /// プログラムのエントリポイント
                    /// 今回は以下の前提でELFを生成している。
                    /// ・elfファイル自体がベースアドレス0x00100000にロードされると想定
                    /// ・生成するelfファイルの構造は [ELFファイルヘッダ; プログラムセクションヘッダ; データセグメント] になっている
                    /// よってエントリポイントは0x00100000(ベースアドレス) + 0x54 (ELFファイルヘッダサイズ+プログラムセクションヘッダサイズ=データセグメント開始アドレスまでのオフセット)になる
                    uint ENTRY_POINT = 0x00100000 + 0x54;

                    Section[] sections;
                    byte[] code;
                    generator.Assembler.assemble(program, ENTRY_POINT, out code, out sections);


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
#endif
            }
        }

        private static void DebugMain(string[] args) {
            CoffDump.Dump(@"..\..\\test\magic2.obj");
            NormalMain(args);
        }

    }
}