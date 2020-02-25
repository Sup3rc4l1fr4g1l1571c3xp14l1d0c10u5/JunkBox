using System;

namespace X86Asm.generator {

    using IOperand = X86Asm.ast.operand.IOperand;

    /// <summary>
    /// リテラルオペランドパターン
    /// </summary>
    public sealed class LiteralOperandPattern : OperandPattern {

        /// <summary>
        /// マッチするリテラルオペランド
        /// </summary>
        private IOperand literal;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="literal"></param>
        public LiteralOperandPattern(IOperand literal) : base(literal.ToString()) {
            this.literal = literal;
        }

        /// <summary>
        /// マッチ判定
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