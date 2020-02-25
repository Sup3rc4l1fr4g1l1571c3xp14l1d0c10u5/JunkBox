namespace X86Asm.libelf {
    public enum ElfClass : byte {
        /// <summary>
        /// Invalid class
        /// </summary>
        ELFCLASSNONE = 0,

        /// <summary>
        /// 32-bit objects
        /// </summary>
        ELFCLASS32 = 1,

        /// <summary>
        /// 64-bit objects
        /// </summary>
        ELFCLASS64 = 2
    }
}