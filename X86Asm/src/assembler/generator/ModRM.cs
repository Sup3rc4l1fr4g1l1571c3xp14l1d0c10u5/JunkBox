namespace X86Asm.generator {



    /// <summary>
    /// An option that specifies the generation of a ModR/M byte (and possibly a SIB byte). The reg/opcode field is either a register operand or a fixed number.
    /// </summary>
    public sealed class ModRM : InstructionOption {

        /// <summary>
        /// R/M�t�B�[���h�ƂȂ�I�y�����h�̃C���f�N�X�ԍ�
        /// </summary>
        public readonly int rmOperandIndex;

        /// <summary>
        /// reg / opcode�t�B�[���h�B
        /// �l��[ 0, 10�j�͈̔͂̏ꍇ�A�l�́A�͈�[0�A10�j�̃I�y�����h�C���f�b�N�X�Ƃ��ĉ���
        /// �l��[10, 18�j�͈̔͂̏ꍇ�A�l�́A�͈�[0�A 8�j�̃I�y�R�[�h�萔�Ƃ��ĉ���
        /// </summary>
        public readonly int regOpcodeOperandIndex;

        /// <summary>
        /// �R���X�g���N�^
        /// </summary>
        /// <param name="rmOperandIndex"> R/M�t�B�[���h�ƂȂ�I�y�����h�̃C���f�N�X�ԍ� </param>
        /// <param name="regOpOperandIndex"> reg/opcode �t�B�[���h�l </param>
        public ModRM(int rmOperandIndex, int regOpOperandIndex) {
            if (rmOperandIndex < 0 || rmOperandIndex >= 10) {
                throw new System.ArgumentException("�s���ȃI�y�����h�C���f�b�N�X�ł�");
            }
            if (regOpOperandIndex < 0 || regOpOperandIndex >= 18) {
                throw new System.ArgumentException("�s����reg/opcode�l�ł�");
            }
            this.rmOperandIndex = rmOperandIndex;
            this.regOpcodeOperandIndex = regOpOperandIndex;
        }

    }

}