using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace X86Asm.libcoff {
    //
    // structure of COFF (.obj) file
    //
    
    //--------------------------//
    // IMAGE_FILE_HEADER        //
    //--------------------------//
    // IMAGE_SECTION_HEADER     //
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
    public enum CHARACTERISTICS : UInt16 {

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

    [StructLayout(LayoutKind.Sequential)]
    public struct IMAGE_FILE_HEADER {
        public IMAGE_FILE_MACHINE Machine;
        public UInt16 NumberOfSections;
        public UInt32 TimeDateStamp;
        public UInt32 PointerToSymbolTable;
        public UInt32 NumberOfSymbols;
        public UInt16 SizeOfOptionalHeader;
        public CHARACTERISTICS Characteristics;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct IMAGE_SECTION_HEADER {
        [FieldOffset(0)]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public char[] Name;

        [FieldOffset(8)]
        public UInt32 VirtualSize;

        [FieldOffset(12)]
        public UInt32 VirtualAddress;

        [FieldOffset(16)]
        public UInt32 SizeOfRawData;

        [FieldOffset(20)]
        public UInt32 PointerToRawData;

        [FieldOffset(24)]
        public UInt32 PointerToRelocations;

        [FieldOffset(28)]
        public UInt32 PointerToLinenumbers;

        [FieldOffset(32)]
        public UInt16 NumberOfRelocations;

        [FieldOffset(34)]
        public UInt16 NumberOfLinenumbers;

        [FieldOffset(36)]
        public DataSectionFlags Characteristics;

        public string Section {
            get { return new string(Name); }
        }
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


    [StructLayout(LayoutKind.Sequential,Pack=2)]
    public struct IMAGE_SYMBOL {
        [StructLayout(LayoutKind.Explicit)]
        public struct _SYMBOL_NAME {
            [StructLayout(LayoutKind.Sequential)]
            public struct _Name {
                public UInt32 Short;
                public UInt32 Long;
            }

            [FieldOffset(0)] [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public byte[] ShortName;

            [FieldOffset(0)] public _Name Name;

            [FieldOffset(0)] [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public UIntPtr[] LongName;
        }

        public _SYMBOL_NAME N;
        public UInt32 Value;
        public UInt16 SectionNumber;
        public UInt16 Type;
        public Byte StorageClass;
        public Byte NumberOfAuxSymbols;
    }

    [StructLayout(LayoutKind.Sequential,Pack = 2)]
    public struct RelocationEntry {
        public  Int32 VirtualAddress;   /* Reference Address */
        public  Int32 SymbolTableIndex;  /* Symbol index */
        public UInt16 Type;   /* Type of relocation */
    }
}
