using System;
using System.Collections.Generic;
using System.Linq;
using X86Asm.ast.operand;

namespace X86Asm.ast.statement {

    /// <summary>
    /// ディレクティブ指定分
    /// </summary>
    public class DirectiveStatement : IStatement {

        /// <summary>
        /// ラベル名
        /// </summary>
        public string Name { get; }
        public IList<IOperand> Arguments { get; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="name">ラベル名</param>
        public DirectiveStatement(string name, IList<IOperand> arguments) {
            if (name == null) { throw new ArgumentNullException(); }
            if (arguments == null) { throw new ArgumentNullException(); }
            Name = name;
            Arguments = arguments.ToArray();
        }

        /// <summary>
        /// 比較処理
        /// </summary>
        /// <param name="obj">比較対象</param>
        /// <returns>同名・同引数のディレクティブ文であれば真</returns>
        public override bool Equals(object obj) {
            if (!(obj is DirectiveStatement)) {
                return false;
            } else {
                return Name.Equals(((DirectiveStatement)obj).Name) && Arguments.SequenceEqual(((DirectiveStatement)obj).Arguments);
            }
        }

        /// <summary>
        /// このオブジェクトのハッシュ値を返す
        /// </summary>
        /// <returns>ハッシュ値</returns>
        public override int GetHashCode() {
            return Name.GetHashCode();
        }

        /// <summary>
        /// このオブジェクトの文字列表現を返す
        /// </summary>
        /// <returns>文字列表現</returns>
        public override string ToString() {
            return Name + String.Join(", ", Arguments);
        }

    }

}