using System;

namespace X86Asm.generator {

    using IOperand = X86Asm.ast.operand.IOperand;

    /// <summary>
    /// ���e�����I�y�����h�p�^�[��
    /// </summary>
    public sealed class LiteralOperandPattern : OperandPattern {

        /// <summary>
        /// �}�b�`���郊�e�����I�y�����h
        /// </summary>
        private IOperand literal;

        /// <summary>
        /// �R���X�g���N�^
        /// </summary>
        /// <param name="literal"></param>
        public LiteralOperandPattern(IOperand literal) : base(literal.ToString()) {
            this.literal = literal;
        }

        /// <summary>
        /// �}�b�`����
        /// </summary>
        /// <param name="operand"></param>
        /// <returns></returns>
        public override bool matches(IOperand operand) {
            if (operand == null) {
                throw new ArgumentNullException();
            }
            return operand.Equals(literal);
        }

    }

}