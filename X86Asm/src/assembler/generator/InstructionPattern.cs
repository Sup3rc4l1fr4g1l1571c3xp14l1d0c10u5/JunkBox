using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace X86Asm.generator {


    /// <summary>
    /// 命令パターンクラス
    /// </summary>
    public sealed class InstructionPattern {

        /// <summary>
        /// ニーモニックと合致する正規表現パターン。
        /// </summary>
        private static Regex MNEMONIC_PATTERN { get; } = new Regex("[a-z][a-z0-9]*");

        /// <summary>
        /// ニーモニック
        /// </summary>
        public string mnemonic { get; }

        /// <summary>
        /// オペランドのパターン
        /// </summary>
        public IReadOnlyList<OperandPattern> operands { get; }

        /// <summary>
        /// オペランドサイズモード
        /// </summary>
        public OperandSizeMode operandSizeMode { get; }

        /// <summary>
        /// 命令パターンに付与されたオプション情報
        /// </summary>
        public IReadOnlyList<InstructionOption> options { get; }


        /// <summary>
        /// 命令パターンに対応する命令コードのテンプレート（一部命令はこのバイト列をベースに少し手を入れたコードを生成する）
        /// </summary>
        public byte[] opcodes { get; }



        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="mnemonic"> ニーモニック</param>
        /// <param name="operands"> オペランドのパターン </param>
        /// <param name="operandSizeMode"> オペランドサイズモード </param>
        /// <param name="opcodes"> 命令コードテンプレート </param>
        /// <param name="options"> 命令パターンに付与されたオプション情報 </param>
        /// <exception cref="ArgumentNullException"> if any argument is {@code null} </exception>
        public InstructionPattern(string mnemonic, OperandPattern[] operands, OperandSizeMode operandSizeMode, byte[] opcodes, params InstructionOption[] options) {
            if (mnemonic == null || operands == null /*|| operandSizeMode == null */|| opcodes == null || options == null) {
                throw new ArgumentNullException();
            }

            if (!MNEMONIC_PATTERN.IsMatch(mnemonic)) {
                throw new System.ArgumentException("未定義のニーモニックです");
            }

            if (operands.Length > 10) {
                throw new System.ArgumentException("オペランドの数が多すぎます");
            }

            if (options.Length > 1) {
                throw new System.ArgumentException("オプションは2個以上指定できません");
            }
            if (options.Length == 1) {
                // オプションが指定されている場合は、オプションと引数の妥当性チェックを行う
                InstructionOption option = options[0];
                if (option is RegisterInOpcode) {
                    checkOption((RegisterInOpcode)option, operands);
                } else if (option is ModRM) {
                    checkOption((ModRM)option, operands);
                } else {
                    throw new Exception("対応していない命令オプションです");
                }
            }

            this.mnemonic = mnemonic;
            this.operandSizeMode = operandSizeMode;
            this.opcodes = opcodes.ToArray();
            this.operands = operands.ToList();
            this.options = options.ToList();
        }

        /// <summary>
        /// このオブジェクトの文字列表現を返す
        /// </summary>
        /// <returns>文字列表現</returns>
        public override string ToString() {
            StringBuilder sb = new StringBuilder();

            sb.Append(mnemonic);

            if (operands.Count > 0) {
                sb.Append("  ");
                bool initial = true;
                foreach (OperandPattern op in operands) {
                    if (initial) {
                        initial = false;
                    } else {
                        sb.Append(", ");
                    }
                    sb.Append(op);
                }
            }

            return sb.ToString();
        }


        /// <summary>
        /// RegisterInOpcodeオプションが指定されている場合の引数チェックを行う
        /// </summary>
        /// <param name="option"></param>
        /// <param name="operands"></param>
        private static void checkOption(RegisterInOpcode option, OperandPattern[] operands) {
            if (option.operandIndex >= operands.Length) {
                throw new System.IndexOutOfRangeException("オペランドの個数が、RegisterInOpcode.operandIndex以下です。");
            }
            if (!isRegisterPattern(operands[option.operandIndex])) {
                throw new System.ArgumentException("RegisterInOpcode.operandIndexで指定されたオペランドがレジスタではありません。");
            }
        }

        /// <summary>
        /// ModRMオプションが指定されている場合の引数チェックを行う
        /// </summary>
        /// <param name="option"></param>
        /// <param name="operands"></param>
        private static void checkOption(ModRM option, OperandPattern[] operands) {
            if (option.rmOperandIndex >= operands.Length) {
                throw new System.IndexOutOfRangeException("オペランドの個数が、ModRM.rmOperandIndex以下です。");
            }
            if (!isRegisterMemoryPattern(operands[option.rmOperandIndex])) {
                throw new System.ArgumentException("ModRM.rmOperandIndexで指定されたオペランドがレジスタもしくはメモリではありません。");
            }

            if (option.regOpcodeOperandIndex >= 10 && option.regOpcodeOperandIndex < 18) {
                // 10～17は0～7のオペコード定数として解釈されるので問題なし。
            } else if (option.regOpcodeOperandIndex >= 18) {
                throw new System.ArgumentException("ModRM.regOpcodeOperandIndexに不正な値が指定されています");
            } else if (option.regOpcodeOperandIndex >= operands.Length) {
                throw new System.IndexOutOfRangeException("オペランドの個数が、ModRM.regOpcodeOperandIndex以下です。");
            } else if (!isRegisterPattern(operands[option.regOpcodeOperandIndex])) {
                throw new System.ArgumentException("ModRM.regOpcodeOperandIndexで指定されたオペランドがレジスタではありません。");
            }
        }

        /// <summary>
        /// オペランドのパターンがメモリオペランドもしくは8/16/32bitレジスタパターンならば真
        /// </summary>
        /// <param name="pat"></param>
        /// <returns></returns>
        private static bool isRegisterMemoryPattern(OperandPattern pat) {
            return pat == OperandPattern.RM8 || pat == OperandPattern.RM16 || pat == OperandPattern.RM32 || pat == OperandPattern.MEM;
        }

        /// <summary>
        /// オペランドのパターンが8/16/32bitレジスタパターンもしくはセグメントレジスタパターンならば真
        /// </summary>
        /// <param name="pat"></param>
        /// <returns></returns>
        private static bool isRegisterPattern(OperandPattern pat) {
            return pat == OperandPattern.REG8 || pat == OperandPattern.REG16 || pat == OperandPattern.REG32 || pat == OperandPattern.SREG;
        }

    }

}