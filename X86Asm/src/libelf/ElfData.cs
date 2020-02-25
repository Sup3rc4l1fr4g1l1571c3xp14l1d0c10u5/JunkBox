namespace X86Asm.libelf {
    public enum ElfData : byte {
        /// <summary>
        /// Invalid data encoding
        /// </summary>
        ELFDATANONE = 0,
        
        /// <summary>
        /// 2's complement, little endian
        /// </summary>
        ELFDATA2LSB = 1,

        /// <summary>
        /// 2's complement, big endian
        /// </summary>
        ELFDATA2MSB = 2
    }
}