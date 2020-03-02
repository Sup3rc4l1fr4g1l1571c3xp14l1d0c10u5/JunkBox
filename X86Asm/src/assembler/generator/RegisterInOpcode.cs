namespace X86Asm.generator {
    /// <summary>
    /// レジスタオペランドを命令中の（最後の）オペランドのバイト中にエンコードすることを指定するオプション
    /// </summary>
    public sealed class RegisterInOpcode : InstructionOption {

        /// <summary>
        /// エンコード対象のオペランドインデクス番号
        /// </summary>‘
        public readonly int operandIndex;

        /// <summary>
        /// コンストラクタ 
        /// </summary>
        public RegisterInOpcode(int operandIndex) {
            if (operandIndex < 0) {
                throw new System.ArgumentException("不正なオペランドインデクス番号です。");
            }
            this.operandIndex = operandIndex;
        }

    }

}