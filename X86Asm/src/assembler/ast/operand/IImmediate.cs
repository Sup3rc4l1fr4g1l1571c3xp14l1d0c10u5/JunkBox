using System;
using System.Collections.Generic;


namespace X86Asm.ast.operand {
using X86Asm.model;

    /// <summary>
    /// 即値オペランドインタフェース
    /// </summary>
    public interface IImmediate : IOperand {
        /// <summary>
        /// ラベルオフセットを考慮した即値オペランドの値を返す
        /// </summary>
        /// <param name="symbolTable"> シンボル表 </param>
        /// <returns>即値オペランドの値</returns>
        ImmediateValue GetValue(IDictionary<string, Symbol> symbolTable);
    }

}