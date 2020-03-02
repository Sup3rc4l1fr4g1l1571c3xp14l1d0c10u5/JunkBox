namespace X86Asm.generator {

    /// <summary>
    /// オペランドのサイズ（movwならMODE16,movlならMODE32, jmpならMODELESS）
    /// </summary>
    public enum OperandSizeMode {

        /// <summary>
        /// オペランドは16bit
        /// </summary>
        MODE16,

        /// <summary>
        /// オペランドは32bit
        /// </summary>
        MODE32,

        /// <summary>
        /// オペランドは8bitもしくはサイズ無関係
        /// </summary>
        MODELESS

    }

}