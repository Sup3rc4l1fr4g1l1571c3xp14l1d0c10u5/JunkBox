namespace X86Asm.libelf {
    public enum ElfOSABI : byte {
        /// <summary>
        ///  UNIX System V ABI 
        /// </summary>
        ELFOSABI_NONE = 0,
        /// <summary>
        ///  Alias.  
        /// </summary>
        ELFOSABI_SYSV = 0,
        /// <summary>
        ///  HP-UX 
        /// </summary>
        ELFOSABI_HPUX = 1,
        /// <summary>
        ///  NetBSD.  
        /// </summary>
        ELFOSABI_NETBSD = 2,
        /// <summary>
        ///  Object uses GNU ELF extensions.  
        /// </summary>
        ELFOSABI_GNU = 3,
        /// <summary>
        ///  Compatibility alias.  
        /// </summary>
        ELFOSABI_LINUX = ELFOSABI_GNU,
        /// <summary>
        ///  Sun Solaris.  
        /// </summary>
        ELFOSABI_SOLARIS = 6,
        /// <summary>
        ///  IBM AIX.  
        /// </summary>
        ELFOSABI_AIX = 7,
        /// <summary>
        ///  SGI Irix.  
        /// </summary>
        ELFOSABI_IRIX = 8,
        /// <summary>
        ///  FreeBSD.  
        /// </summary>
        ELFOSABI_FREEBSD = 9,
        /// <summary>
        ///  Compaq TRU64 UNIX.  
        /// </summary>
        ELFOSABI_TRU64 = 10,
        /// <summary>
        ///  Novell Modesto.  
        /// </summary>
        ELFOSABI_MODESTO = 11,
        /// <summary>
        ///  OpenBSD.  
        /// </summary>
        ELFOSABI_OPENBSD = 12,
        /// <summary>
        ///  ARM EABI 
        /// </summary>
        ELFOSABI_ARM_AEABI = 64,
        /// <summary>
        ///  ARM 
        /// </summary>
        ELFOSABI_ARM = 97,
        /// <summary>
        ///  Standalone (embedded) application 
        /// </summary>
        ELFOSABI_STANDALONE = 255,

    }
}