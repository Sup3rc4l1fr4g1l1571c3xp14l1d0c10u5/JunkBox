using System;
using System.Collections.Generic;
using X86Asm.generator;

namespace X86Asm.ast.operand {

    /// <summary>
    /// 即値オペランドインタフェース
    /// </summary>
    public interface IImmediate : IOperand {
        /// <summary>
        /// ラベルオフセットを考慮した即値オペランドの値を返す
        /// </summary>
        /// <param name="labelOffsets"> ラベルオフセット表 </param>
        /// <returns>即値オペランドの値</returns>
        ImmediateValue GetValue(IDictionary<string, Symbol> labelOffsets);
    }

}