namespace X86Asm.ast.operand {

    /// <summary>
    /// 8ビットレジスタオペランド
    /// </summary>
    public sealed class Register8 : Register {

        /// <summary>
        /// ALレジスタ
        /// </summary>
        public static Register8 AL { get; } = new Register8("al", 0);

        /// <summary>
        /// CLレジスタ
        /// </summary>
        public static Register8 CL { get; } = new Register8("cl", 1);

        /// <summary>
        /// DLレジスタ
        /// </summary>
        public static Register8 DL { get; } = new Register8("dl", 2);

        /// <summary>
        /// BLレジスタ
        /// </summary>
        public static Register8 BL { get; } = new Register8("bl", 3);

        /// <summary>
        /// AHレジスタ
        /// </summary>
        public static Register8 AH { get; } = new Register8("ah", 4);

        /// <summary>
        /// CHレジスタ
        /// </summary>
        public static Register8 CH { get; } = new Register8("ch", 5);

        /// <summary>
        /// DHレジスタ
        /// </summary>
        public static Register8 DH { get; } = new Register8("dh", 6);

        /// <summary>
        /// BHレジスタ
        /// </summary>
        public static Register8 BH { get; } = new Register8("bh", 7);

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="name"> レジスタ名（表示用） </param>
        /// <param name="registerNumber"> レジスタ番号（ModR/MバイトやSIBバイトで使われるため0以上8未満で指定） </param>
		private Register8(string name, uint registerNumber) : base(name, registerNumber) {}

    }

}