using System;

namespace X86Asm.ast.statement {

    /// <summary>
    /// アセンブリ言語ラベル文
    /// </summary>
    public class LabelStatement : IStatement {

        /// <summary>
        /// ラベル名
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="name">ラベル名</param>
        public LabelStatement(string name) {
            if (name == null) { throw new ArgumentNullException(); }
            Name = name;
        }

        /// <summary>
        /// 比較処理
        /// </summary>
        /// <param name="obj">比較対象</param>
        /// <returns>同名のラベル文であれば真</returns>
        public override bool Equals(object obj) {
            if (!(obj is LabelStatement)) {
                return false;
            } else {
                return Name.Equals(((LabelStatement)obj).Name);
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
            return Name;
        }

    }

}