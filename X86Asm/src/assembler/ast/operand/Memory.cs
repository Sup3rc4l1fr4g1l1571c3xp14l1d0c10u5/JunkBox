using System;
using System.Text;

namespace X86Asm.ast.operand {


    /// <summary>
    /// メモリオペランド
    /// 表現形式は Base + Index * Scale + Displacement 
    /// (base: Register|null, index:Register|null, scale:1|2|4|8, displacement: IImmediate)
    /// </summary>
    public sealed class Memory : IOperand {

        /// <summary>
        /// ベースレジスタ（null許容）
        /// </summary>
        public Register32 Base { get; }

        /// <summary>
        /// インデックスレジスタ（null許容だが、ESPレジスタは指定できない）</summary>
        public Register32 Index { get; }

        /// <summary>
        /// インデックススケール（1, 2, 4, 8のいずれか） 
        /// </summary>
        public int Scale { get; }

        /// <summary>
        /// ディスプレーメント（null禁止）
        /// </summary>
        public IImmediate Displacement { get; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="base"> ベースレジスタ（null許容） </param>
        /// <param name="index">インデックスレジスタ（null許容だが、ESPレジスタは指定できない） </param>
        /// <param name="scale"> インデックススケール（1, 2, 4, 8のいずれか）  </param>
        /// <param name="displacement"> ディスプレーメント（null禁止）</param>
        public Memory(Register32 @base, Register32 index, int scale, IImmediate displacement) {
            if (index == Register32.ESP) {
                throw new ArgumentException("インデックスレジスタにESPレジスタは指定できない。");
            }
            if (scale != 1 && scale != 2 && scale != 4 && scale != 8) {
                throw new ArgumentException("インデックススケールは 1, 2, 4, 8 のいずれかに限る。");
            }
            if (displacement == null) {
                throw new ArgumentNullException(nameof(displacement));
            }

            Base = @base;
            Index = index;
            Scale = scale;
            Displacement = displacement;
        }

        /// <summary>
        /// 比較処理
        /// </summary>
        /// <param name="obj">比較対象</param>
        /// <returns>ベース、インデクス、スケール、ディスプレーメントが全部一致すれば真</returns>
		public override bool Equals(object obj) {
            if (!(obj is Memory)) {
                return false;
            } else {
                Memory other = (Memory)obj;
                return Base == other.Base && Index == other.Index && Scale == other.Scale && Displacement.Equals(other.Displacement);
            }
        }

        /// <summary>
        /// このオブジェクトのハッシュ値を返す
        /// </summary>
        /// <returns>ハッシュ値</returns>
        public override int GetHashCode() {
            return Base.GetHashCode() + Index.GetHashCode() + Scale + Displacement.GetHashCode();
        }

        /// <summary>
        /// このオブジェクトの文字列表現を返す
        /// </summary>
        /// <returns>文字列表現</returns>
        public override string ToString() {
            StringBuilder sb = new StringBuilder();
            sb.Append("[");
            bool hasTerm = false;

            if (Base != null) {
                hasTerm = true;
                sb.Append(Base);
            }

            if (Index != null) {
                if (hasTerm) {
                    sb.Append("+");
                } else {
                    hasTerm = true;
                }
                sb.Append(Index);
                if (Scale != 1) {
                    sb.Append("*").Append(Scale);
                }
            }

            if (Displacement is Label || Displacement is ImmediateValue && ((ImmediateValue)Displacement).Value != 0 || !hasTerm) {
                if (hasTerm) {
                    sb.Append("+");
                }
                sb.Append(Displacement);
            }

            sb.Append("]");
            return sb.ToString();
        }

    }

}