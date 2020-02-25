using System;
using System.Collections.Generic;

namespace X86Asm.ast.operand {

    /// <summary>
    /// ラベルオペランド
    /// </summary>
    public class Label : IImmediate {

        /// <summary>
        /// ラベル名
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="name">ラベル名</param>
        public Label(string name) {
            if (name == null) {
                throw new ArgumentNullException(nameof(name));
            }
            Name = name;
        }

        /// <summary>
        /// ラベルオフセットを考慮した即値オペランドの値を返す
        /// Labelクラスはラベルオフセット表が示すオフセット値に変換
        /// </summary>
        /// <param name="labelOffsets"> ラベルオフセット表 </param>
        /// <returns>ラベルに対応するオフセットを値として持つ即値</returns>
        public ImmediateValue GetValue(IDictionary<string, uint> labelOffsets) {
            return new ImmediateValue(unchecked((int)labelOffsets[Name]));
        }

        /// <summary>
        /// 比較処理
        /// </summary>
        /// <param name="obj">比較対象</param>
        /// <returns>ラベル文字列が一致すれば真</returns>
        public override bool Equals(object obj) {
            if (!(obj is Label)) {
                return false;
            } else {
                return Name.Equals(((Label)obj).Name);
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