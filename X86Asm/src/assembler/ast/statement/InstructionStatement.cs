using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace X86Asm.ast.statement {
    using X86Asm.ast.operand;

    /// <summary>
    /// アセンブリ言語命令文
    /// </summary>
    public class InstructionStatement : IStatement {

        /// <summary>
        /// 命令のニーモニック
        /// </summary>
        public string Mnemonic { get; }

        /// <summary>
        /// 命令のオペランド列
        /// </summary>
        public IReadOnlyList<IOperand> Operands { get; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="mnemonic">ニーモニック</param>
        /// <param name="operands">オペランド列</param>
        public InstructionStatement(string mnemonic, IList<IOperand> operands) {
            if (mnemonic == null) { throw new ArgumentNullException(nameof(mnemonic)); }
            if (operands == null) { throw new ArgumentNullException(nameof(operands)); }
            foreach (IOperand op in operands) {
                if (op == null) { throw new ArgumentNullException(nameof(operands)); }
            }

            Mnemonic = mnemonic;
            Operands = operands.ToList();
        }

        /// <summary>
        /// 比較処理
        /// </summary>
        /// <param name="obj">比較対象</param>
        /// <returns>同一の内容を持つ命令文であれば真</returns>
        public override bool Equals(object obj) {
            if (!(obj is InstructionStatement)) {
                return false;
            } else {
                InstructionStatement ist = (InstructionStatement)obj;
                return Mnemonic.Equals(ist.Mnemonic) && Operands.SequenceEqual(ist.Operands);
            }
        }

        /// <summary>
        /// このオブジェクトのハッシュ値を返す
        /// </summary>
        /// <returns>ハッシュ値</returns>
        public override int GetHashCode() {
            return Mnemonic.GetHashCode() + Operands.GetHashCode();
        }

        /// <summary>
        /// このオブジェクトの文字列表現を返す
        /// </summary>
        /// <returns>文字列表現</returns>
        public override string ToString() {
            StringBuilder sb = new StringBuilder();

            sb.Append(Mnemonic);

            if (Operands.Count > 0) {
                sb.Append("  ");
                sb.Append(String.Join(", ", Operands.Select(x => x.ToString())));
            }

            return sb.ToString();
        }

    }

}