using System;

namespace X86Asm.libelf {
    [Flags]
    public enum SectionFlag : UInt32 {
        SHF_WRITE	= 0x1,
        SHF_ALLOC	= 0x2,
        SHF_EXECINSTR	= 0x4,
    }
}