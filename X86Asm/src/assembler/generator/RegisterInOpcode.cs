namespace X86Asm.generator {
    /// <summary>
    /// ���W�X�^�I�y�����h�𖽗ߒ��́i�Ō�́j�I�y�����h�̃o�C�g���ɃG���R�[�h���邱�Ƃ��w�肷��I�v�V����
    /// </summary>
    public sealed class RegisterInOpcode : InstructionOption {

        /// <summary>
        /// �G���R�[�h�Ώۂ̃I�y�����h�C���f�N�X�ԍ�
        /// </summary>�e
        public readonly int operandIndex;

        /// <summary>
        /// �R���X�g���N�^ 
        /// </summary>
        public RegisterInOpcode(int operandIndex) {
            if (operandIndex < 0) {
                throw new System.ArgumentException("�s���ȃI�y�����h�C���f�N�X�ԍ��ł��B");
            }
            this.operandIndex = operandIndex;
        }

    }

}