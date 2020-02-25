namespace X86Asm.ast.operand {

    /// <summary>
    /// 16ビットレジスタオペランド
    /// </summary>
    public sealed class Register16 : Register {

        /// <summary>
        /// AXレジスタ
        /// </summary>
        public static Register16 AX { get; } = new Register16("ax", 0);

        /// <summary>
        /// CXレジスタ
        /// </summary>
        public static Register16 CX { get; } = new Register16("cx", 1);

        /// <summary>
        /// DXレジスタ
        /// </summary>
        public static Register16 DX { get; } = new Register16("dx", 2);

        /// <summary>
        /// BXレジスタ
        /// </summary>
        public static Register16 BX { get; } = new Register16("bx", 3);

        /// <summary>
        /// SPレジスタ
        /// </summary>
        public static Register16 SP { get; } = new Register16("sp", 4);

        /// <summary>
        /// BPレジスタ
        /// </summary>
        public static Register16 BP { get; } = new Register16("bp", 5);

        /// <summary>
        /// SIレジスタ
        /// </summary>
        public static Register16 SI { get; } = new Register16("si", 6);

        /// <summary>
        /// DIレジスタ
        /// </summary>
        public static Register16 DI { get; } = new Register16("di", 7);

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="name"> レジスタ名（表示用） </param>
        /// <param name="registerNumber"> レジスタ番号（ModR/MバイトやSIBバイトで使われるため0以上8未満で指定） </param>
        private Register16(string name, uint registerNumber) : base(name, registerNumber) { }

    }

}