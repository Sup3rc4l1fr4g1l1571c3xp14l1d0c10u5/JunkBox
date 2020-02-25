namespace X86Asm.ast.operand {

    /// <summary>
    /// 32ビットレジスタオペランド
    /// </summary>
    public sealed class Register32 : Register {

        /// <summary>
        /// EAXレジスタ
        /// </summary>
        public static Register32 EAX { get; } = new Register32("eax", 0);

        /// <summary>
        /// ECXレジスタ
        /// </summary>
        public static Register32 ECX { get; } = new Register32("ecx", 1);

        /// <summary>
        /// EDXレジスタ
        /// </summary>
        public static Register32 EDX { get; } = new Register32("edx", 2);

        /// <summary>
        /// EBXレジスタ
        /// </summary>
        public static Register32 EBX { get; } = new Register32("ebx", 3);

        /// <summary>
        /// ESPレジスタ
        /// </summary>
        public static Register32 ESP { get; } = new Register32("esp", 4);

        /// <summary>
        /// EBPレジスタ
        /// </summary>
        public static Register32 EBP { get; } = new Register32("ebp", 5);

        /// <summary>
        /// ESIレジスタ
        /// </summary>
        public static Register32 ESI { get; } = new Register32("esi", 6);

        /// <summary>
        /// EDIレジスタ
        /// </summary>
        public static Register32 EDI { get; } = new Register32("edi", 7);

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="name"> レジスタ名（表示用） </param>
        /// <param name="registerNumber"> レジスタ番号（ModR/MバイトやSIBバイトで使われるため0以上8未満で指定） </param>
        private Register32(string name, uint registerNumber) : base(name, registerNumber) { }

    }

}