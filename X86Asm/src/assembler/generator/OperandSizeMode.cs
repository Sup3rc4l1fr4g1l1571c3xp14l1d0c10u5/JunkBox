namespace X86Asm.generator {

    /// <summary>
    /// �I�y�����h�̃T�C�Y�imovw�Ȃ�MODE16,movl�Ȃ�MODE32, jmp�Ȃ�MODELESS�j
    /// </summary>
    public enum OperandSizeMode {

        /// <summary>
        /// �I�y�����h��16bit
        /// </summary>
        MODE16,

        /// <summary>
        /// �I�y�����h��32bit
        /// </summary>
        MODE32,

        /// <summary>
        /// �I�y�����h��8bit�������̓T�C�Y���֌W
        /// </summary>
        MODELESS

    }

}