namespace X86Asm.libelf {

    /// <summary>
    /// �I�u�W�F�N�g�t�@�C���^�C�v
    /// </summary>
    public enum ObjectType : ushort {
        /// <summary>
        /// ���m�̃^�C�v
        /// </summary>
        ET_NONE = 0,
        /// <summary>
        /// �Ĕz�u�\�ȃt�@�C��
        /// </summary>
        ET_REL = 1,
        /// <summary>
        /// ���s�\�t�@�C��
        /// </summary>
        ET_EXEC = 2,
        /// <summary>
        /// ���L�I�u�W�F�N�g
        /// </summary>
        ET_DYN = 3,
        /// <summary>
        /// �R�A�t�@�C��
        /// </summary>
        ET_CORE = 4
    }

}