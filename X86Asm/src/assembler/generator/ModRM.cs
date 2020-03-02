namespace X86Asm.generator {



    /// <summary>
    /// An option that specifies the generation of a ModR/M byte (and possibly a SIB byte). The reg/opcode field is either a register operand or a fixed number.
    /// </summary>
    public sealed class ModRM : InstructionOption {

        /// <summary>
        /// R/Mフィールドとなるオペランドのインデクス番号
        /// </summary>
        public readonly int rmOperandIndex;

        /// <summary>
        /// reg / opcodeフィールド。
        /// 値が[ 0, 10）の範囲の場合、値は、範囲[0、10）のオペランドインデックスとして解釈
        /// 値が[10, 18）の範囲の場合、値は、範囲[0、 8）のオペコード定数として解釈
        /// </summary>
        public readonly int regOpcodeOperandIndex;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="rmOperandIndex"> R/Mフィールドとなるオペランドのインデクス番号 </param>
        /// <param name="regOpOperandIndex"> reg/opcode フィールド値 </param>
        public ModRM(int rmOperandIndex, int regOpOperandIndex) {
            if (rmOperandIndex < 0 || rmOperandIndex >= 10) {
                throw new System.ArgumentException("不正なオペランドインデックスです");
            }
            if (regOpOperandIndex < 0 || regOpOperandIndex >= 18) {
                throw new System.ArgumentException("不正なreg/opcode値です");
            }
            this.rmOperandIndex = rmOperandIndex;
            this.regOpcodeOperandIndex = regOpOperandIndex;
        }

    }

}