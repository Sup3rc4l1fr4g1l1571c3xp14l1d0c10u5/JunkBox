namespace X86Asm.ast.operand {

    /// <summary>
    /// セグメントレジスタオペランド
    /// </summary>
    public sealed class SegmentRegister : Register {

        /// <summary>
        /// ESレジスタ
        /// </summary>
        public static SegmentRegister ES { get; } = new SegmentRegister("es", 0);

        /// <summary>
        /// CSレジスタ
        /// </summary>
        public static SegmentRegister CS { get; } = new SegmentRegister("cs", 1);

        /// <summary>
        /// SSレジスタ
        /// </summary>
        public static SegmentRegister SS { get; } = new SegmentRegister("ss", 2);

        /// <summary>
        /// DSレジスタ
        /// </summary>
        public static SegmentRegister DS { get; } = new SegmentRegister("ds", 3);

        /// <summary>
        /// FSレジスタ
        /// </summary>
        public static SegmentRegister FS { get; } = new SegmentRegister("fs", 4);

        /// <summary>
        /// GSレジスタ
        /// </summary>
        public static SegmentRegister GS { get; } = new SegmentRegister("gs", 5);

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="name"> レジスタ名（表示用） </param>
        /// <param name="registerNumber"> レジスタ番号（ModR/MバイトやSIBバイトで使われるため0以上8未満で指定） </param>
		private SegmentRegister(string name, uint registerNumber) : base(name, registerNumber) { }

    }

}