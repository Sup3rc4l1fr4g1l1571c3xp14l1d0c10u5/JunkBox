using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace X86Asm.libcoff {
    //
    // structure of COFF (.obj) file
    //
    
    //--------------------------//
    // _IMAGE_FILE_HEADER        //
    //--------------------------//
    // _IMAGE_SECTION_HEADER     //
    //  * num sections          //
    //--------------------------//
    //                          //
    //                          //
    //                          //
    // section data             //
    //  * num sections          //
    //                          //
    //                          //
    //--------------------------//
    // IMAGE_SYMBOL             //
    //  * num symbols           //
    //--------------------------//
    // string table             //
    //--------------------------//
    public enum IMAGE_FILE_MACHINE : UInt16 {
        /// <summary>
        /// The contents of this field are assumed to be applicable to any machine type
        /// </summary>
        IMAGE_FILE_MACHINE_UNKNOWN = 0x0,

        /// <summary>
        /// Matsushita AM33
        /// </summary>
        IMAGE_FILE_MACHINE_AM33 = 0x1d3,

        /// <summary>
        /// x64
        /// </summary>
        IMAGE_FILE_MACHINE_AMD64 = 0x8664,

        /// <summary>
        /// ARM little endian
        /// </summary>
        IMAGE_FILE_MACHINE_ARM = 0x1c0,

        /// <summary>
        /// ARM64 little endian
        /// </summary>
        IMAGE_FILE_MACHINE_ARM64 = 0xaa64,

        /// <summary>
        /// ARM Thumb-2 little endian
        /// </summary>
        IMAGE_FILE_MACHINE_ARMNT = 0x1c4,

        /// <summary>
        /// EFI byte code
        /// </summary>
        IMAGE_FILE_MACHINE_EBC = 0xebc,

        /// <summary>
        /// Intel 386 or later processors and compatible processors
        /// </summary>
        IMAGE_FILE_MACHINE_I386 = 0x14c,

        /// <summary>
        /// Intel Itanium processor family
        /// </summary>
        IMAGE_FILE_MACHINE_IA64 = 0x200,

        /// <summary>
        /// Mitsubishi M32R little endian
        /// </summary>
        IMAGE_FILE_MACHINE_M32R = 0x9041,

        /// <summary>
        /// MIPS16	
        /// </summary>
        IMAGE_FILE_MACHINE_MIPS16 = 0x266,

        /// <summary>
        /// MIPS with FPU
        /// </summary>
        IMAGE_FILE_MACHINE_MIPSFPU = 0x366,

        /// <summary>
        /// MIPS16 with FPU
        /// </summary>
        IMAGE_FILE_MACHINE_MIPSFPU16 = 0x466,

        /// <summary>
        /// Power PC little endian
        /// </summary>
        IMAGE_FILE_MACHINE_POWERPC = 0x1f0,

        /// <summary>
        /// Power PC with floating point support
        /// </summary>
        IMAGE_FILE_MACHINE_POWERPCFP = 0x1f1,

        /// <summary>
        /// MIPS little endian
        /// </summary>
        IMAGE_FILE_MACHINE_R4000 = 0x166,

        /// <summary>
        /// RISC-V 32-bit address space
        /// </summary>
        IMAGE_FILE_MACHINE_RISCV32 = 0x5032,

        /// <summary>
        /// RISC-V 64-bit address space
        /// </summary>
        IMAGE_FILE_MACHINE_RISCV64 = 0x5064,

        /// <summary>
        /// RISC-V 128-bit address space
        /// </summary>
        IMAGE_FILE_MACHINE_RISCV128 = 0x5128,

        /// <summary>
        /// Hitachi SH3
        /// </summary>
        IMAGE_FILE_MACHINE_SH3 = 0x1a2,

        /// <summary>
        /// Hitachi SH3 DSP
        /// </summary>
        IMAGE_FILE_MACHINE_SH3DSP = 0x1a3,

        /// <summary>
        /// Hitachi SH4
        /// </summary>
        IMAGE_FILE_MACHINE_SH4 = 0x1a6,

        /// <summary>
        /// Hitachi SH5
        /// </summary>
        IMAGE_FILE_MACHINE_SH5 = 0x1a8,

        /// <summary>
        /// Thumb
        /// </summary>
        IMAGE_FILE_MACHINE_THUMB = 0x1c2,

        /// <summary>
        /// MIPS little-endian WCE v2
        /// </summary>
        IMAGE_FILE_MACHINE_WCEMIPSV2 = 0x169,
    }

    [Flags]
    public enum IMAGE_FILE_CHARACTERISTICS : UInt16 {

        /// <summary>
        /// Image only, Windows CE, and Microsoft Windows NT and later. This indicates that the file does not contain base relocations and must therefore be loaded at its preferred base address. If the base address is not available, the loader reports an error. The default behavior of the linker is to strip base relocations from executable (EXE) files.
        /// </summary>
        IMAGE_FILE_RELOCS_STRIPPED = 0x0001,

        /// <summary>
        /// Image only. This indicates that the image file is valid and can be run. If this flag is not set, it indicates a linker error.
        /// </summary>
        IMAGE_FILE_EXECUTABLE_IMAGE = 0x0002,

        /// <summary>
        /// COFF line numbers have been removed. This flag is deprecated and should be zero.
        /// </summary>
        IMAGE_FILE_LINE_NUMS_STRIPPED = 0x0004,

        /// <summary>
        /// COFF symbol table entries for local symbols have been removed. This flag is deprecated and should be zero.
        /// </summary>
        IMAGE_FILE_LOCAL_SYMS_STRIPPED = 0x0008,

        /// <summary>
        /// Obsolete. Aggressively trim working set. This flag is deprecated for Windows 2000 and later and must be zero.
        /// </summary>
        IMAGE_FILE_AGGRESSIVE_WS_TRIM = 0x0010,

        /// <summary>
        /// Application can handle > 2-GB addresses.
        /// </summary>
        IMAGE_FILE_LARGE_ADDRESS_AWARE = 0x0020,

        /// <summary>
        /// This flag is reserved for future use.
        /// </summary>
        reserved = 0x0040,

        /// <summary>
        /// Little endian: the least significant bit (LSB) precedes the most significant bit (MSB) in memory. This flag is deprecated and should be zero.
        /// </summary>
        IMAGE_FILE_BYTES_REVERSED_LO = 0x0080,

        /// <summary>
        /// Machine is based on a 32-bit-word architecture.
        /// </summary>
        IMAGE_FILE_32BIT_MACHINE = 0x0100,

        /// <summary>
        /// Debugging information is removed from the image file.
        /// </summary>
        IMAGE_FILE_DEBUG_STRIPPED = 0x0200,

        /// <summary>
        /// If the image is on removable media, fully load it and copy it to the swap file.
        /// </summary>
        IMAGE_FILE_REMOVABLE_RUN_FROM_SWAP = 0x0400,

        /// <summary>
        /// If the image is on network media, fully load it and copy it to the swap file.
        /// </summary>
        IMAGE_FILE_NET_RUN_FROM_SWAP = 0x0800,

        /// <summary>
        /// The image file is a system file, not a user program.
        /// </summary>
        IMAGE_FILE_SYSTEM = 0x1000,

        /// <summary>
        /// The image file is a dynamic-link library (DLL). Such files are considered executable files for almost all purposes, although they cannot be directly run.
        /// </summary>
        IMAGE_FILE_DLL = 0x2000,

        /// <summary>
        /// The file should be run only on a uniprocessor machine.
        /// </summary>
        IMAGE_FILE_UP_SYSTEM_ONLY = 0x4000,

        /// <summary>
        /// Big endian: the MSB precedes the LSB in memory. This flag is deprecated and should be zero.
        /// </summary>
        IMAGE_FILE_BYTES_REVERSED_HI = 0x8000,

    }

    /// <summary>
    /// COFF ファイルヘッダ
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct _IMAGE_FILE_HEADER {
        /// <summary>
        /// COFFファイルが対象とするアーキテクチャ
        /// </summary>
        public IMAGE_FILE_MACHINE Machine;

        /// <summary>
        /// セクション数
        /// Machine が IMAGE_FILE_MACHINE_UNKNOWN で、セクション数が IMPORT_OBJECT_HDR_SIG2(0xffff) の場合はインポート用の擬似COFFファイル
        /// </summary>
        public UInt16 NumberOfSections;
        
        /// <summary>
        /// ファイルのタイムスタンプ
        /// </summary>
        public UInt32 TimeDateStamp;

        /// <summary>
        /// ファイルの先頭からシンボルテーブルへのオフセット
        /// </summary>
        public UInt32 PointerToSymbolTable;

        /// <summary>
        /// このファイルに含まれるシンボル数
        /// </summary>
        public UInt32 NumberOfSymbols;

        /// <summary>
        /// オプショナルヘッダのサイズ
        /// obj ファイルでは通常オプショナルヘッダを持たないので 0 
        /// </summary>
        public UInt16 SizeOfOptionalHeader;

        /// <summary>
        /// ファイルの特性を表すフラグの集合
        /// </summary>
        public IMAGE_FILE_CHARACTERISTICS Characteristics;
    }

    /// <summary>
    /// セクションヘッダ
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct _IMAGE_SECTION_HEADER {
        [FieldOffset(0)]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public char[] Name;

        /// <summary>
        /// セクションの仮想サイズ
        /// obj ファイルでは使われない
        /// </summary>
        [FieldOffset(8)]
        public UInt32 VirtualSize;

        /// <summary>
        /// セクションの仮想アドレス
        /// obj ファイルでは使われない
        /// </summary>
        [FieldOffset(12)]
        public UInt32 VirtualAddress;

        /// <summary>
        /// セクションデータのサイズ
        /// </summary>
        [FieldOffset(16)]
        public UInt32 SizeOfRawData;

        /// <summary>
        /// ファイルの先頭からセクションデータへのオフセット
        /// セクションデータが存在しない場合は0
        /// </summary>
        [FieldOffset(20)]
        public UInt32 PointerToRawData;

        /// <summary>
        /// ファイルの先頭から再配置情報へのオフセット
        /// </summary>
        [FieldOffset(24)]
        public UInt32 PointerToRelocations;

        /// <summary>
        /// CodeView形式のデバッグ情報で使われる行番号情報へのオフセット
        /// 使われていない場合は0が入る
        /// </summary>
        [FieldOffset(28)]
        public UInt32 PointerToLinenumbers;

        /// <summary>
        /// 再配置情報の数
        /// </summary>
        [FieldOffset(32)]
        public UInt16 NumberOfRelocations;

        /// <summary>
        /// 行番号情報の数
        /// </summary>
        [FieldOffset(34)]
        public UInt16 NumberOfLinenumbers;

        /// <summary>
        /// セクションの特性を表すフラグの集合
        /// </summary>
        [FieldOffset(36)]
        public DataSectionFlags Characteristics;

    }

    [Flags]
    public enum DataSectionFlags : uint {
        /// <summary>
        /// Reserved for future use.
        /// </summary>
        TypeReg = 0x00000000,
        /// <summary>
        /// Reserved for future use.
        /// </summary>
        TypeDsect = 0x00000001,
        /// <summary>
        /// Reserved for future use.
        /// </summary>
        TypeNoLoad = 0x00000002,
        /// <summary>
        /// Reserved for future use.
        /// </summary>
        TypeGroup = 0x00000004,
        /// <summary>
        /// The section should not be padded to the next boundary. This flag is obsolete and is replaced by IMAGE_SCN_ALIGN_1BYTES. This is valid only for object files.
        /// </summary>
        TypeNoPadded = 0x00000008,
        /// <summary>
        /// Reserved for future use.
        /// </summary>
        TypeCopy = 0x00000010,
        /// <summary>
        /// The section contains executable code.
        /// </summary>
        ContentCode = 0x00000020,
        /// <summary>
        /// The section contains initialized data.
        /// </summary>
        ContentInitializedData = 0x00000040,
        /// <summary>
        /// The section contains uninitialized data.
        /// </summary>
        ContentUninitializedData = 0x00000080,
        /// <summary>
        /// Reserved for future use.
        /// </summary>
        LinkOther = 0x00000100,
        /// <summary>
        /// The section contains comments or other information. The .drectve section has this type. This is valid for object files only.
        /// </summary>
        LinkInfo = 0x00000200,
        /// <summary>
        /// Reserved for future use.
        /// </summary>
        TypeOver = 0x00000400,
        /// <summary>
        /// The section will not become part of the image. This is valid only for object files.
        /// </summary>
        LinkRemove = 0x00000800,
        /// <summary>
        /// The section contains COMDAT data. For more information, see section 5.5.6, COMDAT Sections (Object Only). This is valid only for object files.
        /// </summary>
        LinkComDat = 0x00001000,
        /// <summary>
        /// Reset speculative exceptions handling bits in the TLB entries for this section.
        /// </summary>
        NoDeferSpecExceptions = 0x00004000,
        /// <summary>
        /// The section contains data referenced through the global pointer (GP).
        /// </summary>
        RelativeGP = 0x00008000,
        /// <summary>
        /// Reserved for future use.
        /// </summary>
        MemPurgeable = 0x00020000,
        /// <summary>
        /// Reserved for future use.
        /// </summary>
        Memory16Bit = 0x00020000,
        /// <summary>
        /// Reserved for future use.
        /// </summary>
        MemoryLocked = 0x00040000,
        /// <summary>
        /// Reserved for future use.
        /// </summary>
        MemoryPreload = 0x00080000,
        /// <summary>
        /// Align data on a 1-byte boundary. Valid only for object files.
        /// </summary>
        Align1Bytes = 0x00100000,
        /// <summary>
        /// Align data on a 2-byte boundary. Valid only for object files.
        /// </summary>
        Align2Bytes = 0x00200000,
        /// <summary>
        /// Align data on a 4-byte boundary. Valid only for object files.
        /// </summary>
        Align4Bytes = 0x00300000,
        /// <summary>
        /// Align data on an 8-byte boundary. Valid only for object files.
        /// </summary>
        Align8Bytes = 0x00400000,
        /// <summary>
        /// Align data on a 16-byte boundary. Valid only for object files.
        /// </summary>
        Align16Bytes = 0x00500000,
        /// <summary>
        /// Align data on a 32-byte boundary. Valid only for object files.
        /// </summary>
        Align32Bytes = 0x00600000,
        /// <summary>
        /// Align data on a 64-byte boundary. Valid only for object files.
        /// </summary>
        Align64Bytes = 0x00700000,
        /// <summary>
        /// Align data on a 128-byte boundary. Valid only for object files.
        /// </summary>
        Align128Bytes = 0x00800000,
        /// <summary>
        /// Align data on a 256-byte boundary. Valid only for object files.
        /// </summary>
        Align256Bytes = 0x00900000,
        /// <summary>
        /// Align data on a 512-byte boundary. Valid only for object files.
        /// </summary>
        Align512Bytes = 0x00A00000,
        /// <summary>
        /// Align data on a 1024-byte boundary. Valid only for object files.
        /// </summary>
        Align1024Bytes = 0x00B00000,
        /// <summary>
        /// Align data on a 2048-byte boundary. Valid only for object files.
        /// </summary>
        Align2048Bytes = 0x00C00000,
        /// <summary>
        /// Align data on a 4096-byte boundary. Valid only for object files.
        /// </summary>
        Align4096Bytes = 0x00D00000,
        /// <summary>
        /// Align data on an 8192-byte boundary. Valid only for object files.
        /// </summary>
        Align8192Bytes = 0x00E00000,
        /// <summary>
        /// The section contains extended relocations.
        /// </summary>
        LinkExtendedRelocationOverflow = 0x01000000,
        /// <summary>
        /// The section can be discarded as needed.
        /// </summary>
        MemoryDiscardable = 0x02000000,
        /// <summary>
        /// The section cannot be cached.
        /// </summary>
        MemoryNotCached = 0x04000000,
        /// <summary>
        /// The section is not pageable.
        /// </summary>
        MemoryNotPaged = 0x08000000,
        /// <summary>
        /// The section can be shared in memory.
        /// </summary>
        MemoryShared = 0x10000000,
        /// <summary>
        /// The section can be executed as code.
        /// </summary>
        MemoryExecute = 0x20000000,
        /// <summary>
        /// The section can be read.
        /// </summary>
        MemoryRead = 0x40000000,
        /// <summary>
        /// The section can be written to.
        /// </summary>
        MemoryWrite = 0x80000000
    }

    /// <summary>
    /// シンボル情報
    /// </summary>
    [StructLayout(LayoutKind.Sequential,Pack=1)]
    public struct _IMAGE_SYMBOL {
        [StructLayout(LayoutKind.Explicit)]
        public struct _SYMBOL_NAME {
            /// <summary>
            /// '\0'を含まないシンボル名の文字数が8以内の場合、このメンバにシンボル名が格納される
            /// </summary>
            [FieldOffset(0)] [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public byte[] ShortName;

            /// <summary>
            /// シンボル名が 8 文字を越える場合、ロングシンボル名が格納されているロングシンボル名テーブルの先頭からのオフセット
            /// </summary>
            [FieldOffset(0)] [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public UInt32[] LongName;

            /// <summary>
            /// このフィールドが０ならシンボル名が 8 文字を越える
            /// </summary>
            public UInt32 Short { get { return LongName[0]; } }

            /// <summary>
            /// シンボル名が 8 文字を越える場合、ロングシンボル名が格納されているロングシンボル名テーブルの先頭からのオフセット
            /// </summary>
            public UInt32 Long { get { return LongName[1]; } }
        }

        /// <summary>
        /// シンボル名情報
        /// </summary>
        public _SYMBOL_NAME N;

        /// <summary>
        /// シンボルの種類によって意味が変わる値
        /// </summary>
        public UInt32 Value;

        /// <summary>
        /// このシンボルがどのセクションに定義されているかを示す番号（1-based の セクションテーブルのインデックス）
        /// IMAGE_SYM_UNDEFINED (=0) の場合は セクションに属さないデータや別のどこかで定義されている外部シンボルへの参照を 意味する
        /// 特別な意味を持つ負数が入る場合もある
        /// </summary>
        public Int16 SectionNumber;

        /// <summary>
        /// 上位バイト、下位バイトでシンボルの型を示す。
        /// </summary>
        public UInt16 Type;

        /// <summary>
        /// 記憶域クラスを示す値
        /// </summary>
        public Byte StorageClass;

        /// <summary>
        /// 補助シンボル情報の数
        /// </summary>
        public Byte NumberOfAuxSymbols;
    }

    /// <summary>
    /// 再配置情報
    /// </summary>
    [StructLayout(LayoutKind.Explicit,Pack = 2)]
    public struct _IMAGE_RELOCATION {
        /// <summary>
        /// 再配置しなければいけない位置のセクションデータの先頭からのオフセット
        /// </summary>
        [FieldOffset(0)]
        public UInt32 VirtualAddress;
        /// <summary>
        /// セクション再配置情報数が 0xFFFF を越える場合の再配置情報数
        /// </summary>
        [FieldOffset(0)]
        public UInt32 RelocCount;
        
        /// <summary>
        /// 再配置で置き換える値の元となるシンボルのインデックス
        /// </summary>
        [FieldOffset(4)]
        public UInt32 SymbolTableIndex; 

        /// <summary>
        /// 再配置情報のタイプ
        /// </summary>
        [FieldOffset(8)]
        public UInt16 Type;
    }

    public class CoffDump {
        public static void Dump(string path) {
            using (var file = new FileStream(path, FileMode.Open)) 
            using (var br = new BinaryReader(file,Encoding.ASCII, false)) {
                libcoff._IMAGE_FILE_HEADER fileHeader = br.ReadFrom<_IMAGE_FILE_HEADER>();
                Console.WriteLine("_IMAGE_FILE_HEADER:");
                Console.WriteLine($"  Machine: {fileHeader.Machine.ToString()} (0x{(UInt16)fileHeader.Machine:X4})");
                Console.WriteLine($"  NumberOfSections: {fileHeader.NumberOfSections}");
                Console.WriteLine($"  TimeDateStamp: {new DateTime(1970,1,1,0,0,0).ToLocalTime().AddSeconds(fileHeader.TimeDateStamp).ToString("G")} (0x{fileHeader.TimeDateStamp:X8})");
                Console.WriteLine($"  PointerToSymbolTable: 0x{fileHeader.PointerToSymbolTable:X8}");
                Console.WriteLine($"  NumberOfSymbols: {fileHeader.NumberOfSymbols}");
                Console.WriteLine($"  SizeOfOptionalHeader: {fileHeader.SizeOfOptionalHeader}");
                Console.WriteLine($"  Characteristics: {fileHeader.Characteristics.ToString()} (0x{(UInt16)fileHeader.Characteristics:X4})");

                libcoff._IMAGE_SECTION_HEADER[] sectionHeaders = new _IMAGE_SECTION_HEADER[fileHeader.NumberOfSections];
                for (var i = 0; i < fileHeader.NumberOfSections; i++) {
                    sectionHeaders[i] = br.ReadFrom<_IMAGE_SECTION_HEADER>();
                    Console.WriteLine($"_IMAGE_SECTION_HEADER[{i}/{fileHeader.NumberOfSections}]:");
                    Console.WriteLine($"  Name: {new String(sectionHeaders[i].Name,0,8)}");
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

                br.BaseStream.Seek(fileHeader.PointerToSymbolTable, SeekOrigin.Begin);
                for (var i = 0; i < fileHeader.NumberOfSymbols; i++) {
                    Console.WriteLine($"Symbol[{i}/{fileHeader.NumberOfSymbols}]:");
                    libcoff._IMAGE_SYMBOL imageSymbol = br.ReadFrom<_IMAGE_SYMBOL>();
                    Console.WriteLine($"  ShortName: [{String.Join(", ", imageSymbol.N.ShortName.Select(x => $"0x{x:X2}").ToArray())}]");
                    Console.WriteLine($"  LongName: [{String.Join(", ", imageSymbol.N.LongName.Select(x => $"0x{x:X8}").ToArray())}]");
                    Console.WriteLine($"  Value: {imageSymbol.Value}");
                    Console.WriteLine($"  SectionNumber: {imageSymbol.SectionNumber}");
                    Console.WriteLine($"  Type: {imageSymbol.Type}");
                    Console.WriteLine($"  StorageClass: {imageSymbol.StorageClass}");
                    Console.WriteLine($"  NumberOfAuxSymbols: {imageSymbol.NumberOfAuxSymbols}");
                }

                for (var i = 0; i < fileHeader.NumberOfSections; i++) {
                    br.BaseStream.Seek(sectionHeaders[i].PointerToRelocations,SeekOrigin.Begin);
                    Console.WriteLine($"RelocationTable[{i}]:");
                    for (var j = 0; j < (int) sectionHeaders[i].NumberOfRelocations; j++) {
                        Console.WriteLine($"  Entry[{j}/{sectionHeaders[i].NumberOfRelocations}]:");
                        libcoff._IMAGE_RELOCATION imageRelocation = br.ReadFrom<_IMAGE_RELOCATION>();
                        Console.WriteLine($"    VirtualAddress: 0x{imageRelocation.VirtualAddress:X8}");
                        Console.WriteLine($"    SymbolTableIndex: 0x{imageRelocation.SymbolTableIndex:X8}");
                        Console.WriteLine($"    Type: 0x{imageRelocation.Type:X4}");
                    }
                }

                // ロングシンボル名テーブル
                // ロングシンボル名テーブルはシンボルテーブルの直後に続きます
                // そして、最初の4バイトにテーブルサイズを持ち、続いて '\0' で終わる文字列が連なります。
                br.BaseStream.Seek(fileHeader.PointerToSymbolTable + fileHeader.NumberOfSymbols * Marshal.SizeOf<_IMAGE_SYMBOL>(), SeekOrigin.Begin);
                var tableSize = br.ReadUInt32()-4;
                var buf = new StringBuilder();
                for (UInt32 offset = 0; offset < tableSize; offset++) {
                    char ch = br.ReadChar();
                    if (ch == '\0') {
                        Console.WriteLine($"[{offset:X8}] {buf.ToString()}");
                        buf.Clear();
                    }
                    else {
                        buf.Append(ch);
                    }
                }

            }

        }
    }
    static class BinaryReadWriteExt {
        public static void WriteTo<TStruct>(this BinaryWriter writer, TStruct s) where TStruct : struct
        {
            var size = Marshal.SizeOf(typeof(TStruct));
            var buffer = new byte[size];
            var ptr = IntPtr.Zero;

            try {
                ptr = Marshal.AllocHGlobal(size);

                Marshal.StructureToPtr(s, ptr, false);

                Marshal.Copy(ptr, buffer, 0, size);
            }
            finally {
                if (ptr != IntPtr.Zero)
                    Marshal.FreeHGlobal(ptr);
            }

            writer.Write(buffer);
        }

        public static TStruct ReadFrom<TStruct>(this BinaryReader reader) where TStruct : struct
        {
            var size = Marshal.SizeOf(typeof(TStruct));
            var ptr = IntPtr.Zero;

            try {
                ptr = Marshal.AllocHGlobal(size);

                Marshal.Copy(reader.ReadBytes(size), 0, ptr, size);

                return (TStruct)Marshal.PtrToStructure(ptr, typeof(TStruct));
            }
            finally {
                if (ptr != IntPtr.Zero)
                    Marshal.FreeHGlobal(ptr);
            }
        }
    }
}
