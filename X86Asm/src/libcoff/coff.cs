using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace X86Asm.libcoff {
    public class CoffDump {
        public static void Dump(string path) {
            using (var br = new BinaryReader(new FileStream(path, FileMode.Open), Encoding.ASCII)) {
                libcoff._IMAGE_FILE_HEADER fileHeader = libcoff._IMAGE_FILE_HEADER.ReadFrom(br);
                Console.WriteLine("_IMAGE_FILE_HEADER:");
                Console.WriteLine($"  Machine: {fileHeader.Machine.ToString()} (0x{(UInt16)fileHeader.Machine:X4})");
                Console.WriteLine($"  NumberOfSections: {fileHeader.NumberOfSections}");
                Console.WriteLine($"  TimeDateStamp: {new DateTime(1970, 1, 1, 0, 0, 0).ToLocalTime().AddSeconds(fileHeader.TimeDateStamp).ToString("G")} (0x{fileHeader.TimeDateStamp:X8})");
                Console.WriteLine($"  PointerToSymbolTable: 0x{fileHeader.PointerToSymbolTable:X8}");
                Console.WriteLine($"  NumberOfSymbols: {fileHeader.NumberOfSymbols}");
                Console.WriteLine($"  SizeOfOptionalHeader: {fileHeader.SizeOfOptionalHeader}");
                Console.WriteLine($"  Characteristics: {fileHeader.Characteristics.ToString()} (0x{(UInt16)fileHeader.Characteristics:X4})");

                libcoff._IMAGE_SECTION_HEADER[] sectionHeaders = new _IMAGE_SECTION_HEADER[fileHeader.NumberOfSections];
                for (var i = 0; i < fileHeader.NumberOfSections; i++) {
                    sectionHeaders[i] = _IMAGE_SECTION_HEADER.ReadFrom(br);
                    Console.WriteLine($"_IMAGE_SECTION_HEADER[{i}/{fileHeader.NumberOfSections}]:");
                    Console.WriteLine($"  Name: {System.Text.Encoding.ASCII.GetString(sectionHeaders[i].Name)}");
                    Console.WriteLine($"  VirtualSize: {sectionHeaders[i].VirtualSize}");
                    Console.WriteLine($"  VirtualAddress: 0x{sectionHeaders[i].VirtualAddress:X8}");
                    Console.WriteLine($"  SizeOfRawData: {sectionHeaders[i].SizeOfRawData}");
                    Console.WriteLine($"  PointerToRawData: 0x{sectionHeaders[i].PointerToRawData:X8}");
                    Console.WriteLine($"  PointerToRelocations: 0x{sectionHeaders[i].PointerToRelocations:X8}");
                    Console.WriteLine($"  PointerToLinenumbers: 0x{sectionHeaders[i].PointerToLinenumbers:X8}");
                    Console.WriteLine($"  NumberOfRelocations: {sectionHeaders[i].NumberOfRelocations}");
                    Console.WriteLine($"  NumberOfLinenumbers: {sectionHeaders[i].NumberOfLinenumbers}");
                    Console.WriteLine($"  Characteristics: 0x{(UInt32)sectionHeaders[i].Characteristics:X8}");
                }

                for (var i = 0; i < fileHeader.NumberOfSymbols; i++) {
                    br.BaseStream.Seek(fileHeader.PointerToSymbolTable + i * _IMAGE_SYMBOL.Size, SeekOrigin.Begin);
                    Console.WriteLine($"Symbol[{i}/{fileHeader.NumberOfSymbols}]:");
                    libcoff._IMAGE_SYMBOL imageSymbol = _IMAGE_SYMBOL.ReadFrom(br);
                    if (imageSymbol.N.Short != 0) {
                        Console.WriteLine($"  ShortName: {String.Concat(imageSymbol.N.ShortName.Select(x => (char)x))} [{String.Join(", ", imageSymbol.N.ShortName.Select(x => $"0x{x:X2}").ToArray())}]");
                    } else {
                        Console.WriteLine($"  LongName: [{String.Join(", ", imageSymbol.N.LongName.Select(x => $"0x{x:X8}").ToArray())}]");
                    }
                    Console.WriteLine($"  Value: {imageSymbol.Value}");
                    Console.WriteLine($"  SectionNumber: {imageSymbol.SectionNumber}");
                    Console.WriteLine($"  Type: {imageSymbol.Type}");
                    Console.WriteLine($"  StorageClass: {imageSymbol.StorageClass}");
                    Console.WriteLine($"  NumberOfAuxSymbols: {imageSymbol.NumberOfAuxSymbols}");
                    if (imageSymbol.NumberOfAuxSymbols > 0) {
                        switch (imageSymbol.StorageClass) {
                            case _IMAGE_SYMBOL._SYMBOL_STORAGE_CLASS.C_FILE: {
                                    var str = System.Text.Encoding.ASCII.GetString(br.ReadBytes(_IMAGE_SYMBOL.Size));
                                    Console.WriteLine($"  FileName: {str}");
                                    break;
                                }
                            case _IMAGE_SYMBOL._SYMBOL_STORAGE_CLASS.C_EXT: {
                                    var tag_index = br.ReadUInt32();
                                    var size = br.ReadUInt32();
                                    var lines = br.ReadUInt32();
                                    var next_function = br.ReadUInt32();
                                    Console.WriteLine($"  TagIndex: 0x{tag_index:X8}   Size: 0x{size:X8}   Lines: 0x{lines:X8}   NextFunction: 0x{next_function:X8}");
                                    break;
                                }
                            case _IMAGE_SYMBOL._SYMBOL_STORAGE_CLASS.C_STAT: {
                                    var length = br.ReadUInt32();
                                    var relocs = br.ReadUInt32();
                                    var linenums = br.ReadUInt32();
                                    var checksum = br.ReadUInt32();
                                    Console.WriteLine($"  Section Length: 0x{length:X8}   Relocs: 0x{relocs:X8}   LineNums: 0x{linenums:X8}   CheckSum: 0x{checksum:X8}");
                                    break;
                                }
                            default: {
                                    var bytes = br.ReadBytes(_IMAGE_SYMBOL.Size * imageSymbol.NumberOfAuxSymbols);
                                    for (var j = 0; j < bytes.Length; j++) {
                                        if (j > 0 && (j % 16) == 0) {
                                            Console.WriteLine();
                                        }
                                        Console.WriteLine($"{bytes[j]:X2} ");
                                    }
                                    break;
                                }
                        }
                        i += imageSymbol.NumberOfAuxSymbols;
                    }
                }

                for (var i = 0; i < fileHeader.NumberOfSections; i++) {
                    br.BaseStream.Seek(sectionHeaders[i].PointerToRelocations, SeekOrigin.Begin);
                    Console.WriteLine($"RelocationTable[{i}]:");
                    for (var j = 0; j < (int)sectionHeaders[i].NumberOfRelocations; j++) {
                        Console.WriteLine($"  Entry[{j}/{sectionHeaders[i].NumberOfRelocations}]:");
                        libcoff._IMAGE_RELOCATION imageRelocation = libcoff._IMAGE_RELOCATION.ReadFrom(br);
                        Console.WriteLine($"    VirtualAddress: 0x{imageRelocation.VirtualAddress:X8}");
                        Console.WriteLine($"    SymbolTableIndex: 0x{imageRelocation.SymbolTableIndex:X8}");
                        Console.WriteLine($"    Type: {imageRelocation.Type} (0x{(UInt16)imageRelocation.Type:X4})");
                    }
                }

                // ロングシンボル名テーブル
                // ロングシンボル名テーブルはシンボルテーブルの直後に続きます
                // そして、最初の4バイトにテーブルサイズを持ち、続いて '\0' で終わる文字列が連なります。
                br.BaseStream.Seek(fileHeader.PointerToSymbolTable + fileHeader.NumberOfSymbols * _IMAGE_SYMBOL.Size, SeekOrigin.Begin);
                var tableSize = br.ReadUInt32() - 4U;
                var buf = new StringBuilder();
                var start = 4U;
                Console.WriteLine($"LongSymbolTable: {tableSize}byte");
                for (UInt32 offset = 0; offset < tableSize; offset++) {
                    char ch = br.ReadChar();
                    if (ch == '\0') {
                        Console.WriteLine($"  [{start:X8}] {buf.ToString()}");
                        start = offset + 1U;
                        buf.Clear();
                    } else {
                        buf.Append(ch);
                    }
                }

            }
        }
    }
}
