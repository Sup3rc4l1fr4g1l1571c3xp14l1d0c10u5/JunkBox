using System;

namespace X86Asm.ast.operand {

    /// <summary>
    /// レジスタオペランドの基底クラス
    /// </summary>
    public abstract class Register : IOperand {

        /// <summary>
        /// レジスタ番号（ModR/MバイトやSIBバイトで使われるため0以上8未満で指定）
        /// </summary>
        public uint RegisterNumber { get; }

        /// <summary>
        /// レジスタ名（表示用）
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="name"> レジスタ名（表示用） </param>
        /// <param name="registerNumber"> レジスタ番号（ModR/MバイトやSIBバイトで使われるため0以上8未満で指定） </param>
        protected Register(string name, uint registerNumber) {
            if (name == null) {
                throw new ArgumentNullException(nameof(name));
            }
            if (registerNumber >= 8) {
                throw new ArgumentException("不正なレジスタ番号です");
            }
            Name = name;
            RegisterNumber = registerNumber;
        }

        /// <summary>
        /// このオブジェクトの文字列表現を返す
        /// </summary>
        /// <returns>文字列表現</returns>
        public override string ToString() {
            return Name;
        }

    }

}