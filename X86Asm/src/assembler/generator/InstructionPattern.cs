using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace X86Asm.generator {


    /// <summary>
    /// ���߃p�^�[���N���X
    /// </summary>
    public sealed class InstructionPattern {

        /// <summary>
        /// �j�[���j�b�N�ƍ��v���鐳�K�\���p�^�[���B
        /// </summary>
        private static Regex MNEMONIC_PATTERN { get; } = new Regex("[a-z][a-z0-9]*");

        /// <summary>
        /// �j�[���j�b�N
        /// </summary>
        public string mnemonic { get; }

        /// <summary>
        /// �I�y�����h�̃p�^�[��
        /// </summary>
        public IReadOnlyList<OperandPattern> operands { get; }

        /// <summary>
        /// �I�y�����h�T�C�Y���[�h
        /// </summary>
        public OperandSizeMode operandSizeMode { get; }

        /// <summary>
        /// ���߃p�^�[���ɕt�^���ꂽ�I�v�V�������
        /// </summary>
        public IReadOnlyList<InstructionOption> options { get; }


        /// <summary>
        /// ���߃p�^�[���ɑΉ����閽�߃R�[�h�̃e���v���[�g�i�ꕔ���߂͂��̃o�C�g����x�[�X�ɏ��������ꂽ�R�[�h�𐶐�����j
        /// </summary>
        public byte[] opcodes { get; }



        /// <summary>
        /// �R���X�g���N�^
        /// </summary>
        /// <param name="mnemonic"> �j�[���j�b�N</param>
        /// <param name="operands"> �I�y�����h�̃p�^�[�� </param>
        /// <param name="operandSizeMode"> �I�y�����h�T�C�Y���[�h </param>
        /// <param name="opcodes"> ���߃R�[�h�e���v���[�g </param>
        /// <param name="options"> ���߃p�^�[���ɕt�^���ꂽ�I�v�V������� </param>
        /// <exception cref="ArgumentNullException"> if any argument is {@code null} </exception>
        public InstructionPattern(string mnemonic, OperandPattern[] operands, OperandSizeMode operandSizeMode, byte[] opcodes, params InstructionOption[] options) {
            if (mnemonic == null || operands == null /*|| operandSizeMode == null */|| opcodes == null || options == null) {
                throw new ArgumentNullException();
            }

            if (!MNEMONIC_PATTERN.IsMatch(mnemonic)) {
                throw new System.ArgumentException("����`�̃j�[���j�b�N�ł�");
            }

            if (operands.Length > 10) {
                throw new System.ArgumentException("�I�y�����h�̐����������܂�");
            }

            if (options.Length > 1) {
                throw new System.ArgumentException("�I�v�V������2�ȏ�w��ł��܂���");
            }
            if (options.Length == 1) {
                // �I�v�V�������w�肳��Ă���ꍇ�́A�I�v�V�����ƈ����̑Ó����`�F�b�N���s��
                InstructionOption option = options[0];
                if (option is RegisterInOpcode) {
                    checkOption((RegisterInOpcode)option, operands);
                } else if (option is ModRM) {
                    checkOption((ModRM)option, operands);
                } else {
                    throw new Exception("�Ή����Ă��Ȃ����߃I�v�V�����ł�");
                }
            }

            this.mnemonic = mnemonic;
            this.operandSizeMode = operandSizeMode;
            this.opcodes = opcodes.ToArray();
            this.operands = operands.ToList();
            this.options = options.ToList();
        }

        /// <summary>
        /// ���̃I�u�W�F�N�g�̕�����\����Ԃ�
        /// </summary>
        /// <returns>������\��</returns>
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
        /// RegisterInOpcode�I�v�V�������w�肳��Ă���ꍇ�̈����`�F�b�N���s��
        /// </summary>
        /// <param name="option"></param>
        /// <param name="operands"></param>
        private static void checkOption(RegisterInOpcode option, OperandPattern[] operands) {
            if (option.operandIndex >= operands.Length) {
                throw new System.IndexOutOfRangeException("�I�y�����h�̌����ARegisterInOpcode.operandIndex�ȉ��ł��B");
            }
            if (!isRegisterPattern(operands[option.operandIndex])) {
                throw new System.ArgumentException("RegisterInOpcode.operandIndex�Ŏw�肳�ꂽ�I�y�����h�����W�X�^�ł͂���܂���B");
            }
        }

        /// <summary>
        /// ModRM�I�v�V�������w�肳��Ă���ꍇ�̈����`�F�b�N���s��
        /// </summary>
        /// <param name="option"></param>
        /// <param name="operands"></param>
        private static void checkOption(ModRM option, OperandPattern[] operands) {
            if (option.rmOperandIndex >= operands.Length) {
                throw new System.IndexOutOfRangeException("�I�y�����h�̌����AModRM.rmOperandIndex�ȉ��ł��B");
            }
            if (!isRegisterMemoryPattern(operands[option.rmOperandIndex])) {
                throw new System.ArgumentException("ModRM.rmOperandIndex�Ŏw�肳�ꂽ�I�y�����h�����W�X�^�������̓������ł͂���܂���B");
            }

            if (option.regOpcodeOperandIndex >= 10 && option.regOpcodeOperandIndex < 18) {
                // 10�`17��0�`7�̃I�y�R�[�h�萔�Ƃ��ĉ��߂����̂Ŗ��Ȃ��B
            } else if (option.regOpcodeOperandIndex >= 18) {
                throw new System.ArgumentException("ModRM.regOpcodeOperandIndex�ɕs���Ȓl���w�肳��Ă��܂�");
            } else if (option.regOpcodeOperandIndex >= operands.Length) {
                throw new System.IndexOutOfRangeException("�I�y�����h�̌����AModRM.regOpcodeOperandIndex�ȉ��ł��B");
            } else if (!isRegisterPattern(operands[option.regOpcodeOperandIndex])) {
                throw new System.ArgumentException("ModRM.regOpcodeOperandIndex�Ŏw�肳�ꂽ�I�y�����h�����W�X�^�ł͂���܂���B");
            }
        }

        /// <summary>
        /// �I�y�����h�̃p�^�[�����������I�y�����h��������8/16/32bit���W�X�^�p�^�[���Ȃ�ΐ^
        /// </summary>
        /// <param name="pat"></param>
        /// <returns></returns>
        private static bool isRegisterMemoryPattern(OperandPattern pat) {
            return pat == OperandPattern.RM8 || pat == OperandPattern.RM16 || pat == OperandPattern.RM32 || pat == OperandPattern.MEM;
        }

        /// <summary>
        /// �I�y�����h�̃p�^�[����8/16/32bit���W�X�^�p�^�[���������̓Z�O�����g���W�X�^�p�^�[���Ȃ�ΐ^
        /// </summary>
        /// <param name="pat"></param>
        /// <returns></returns>
        private static bool isRegisterPattern(OperandPattern pat) {
            return pat == OperandPattern.REG8 || pat == OperandPattern.REG16 || pat == OperandPattern.REG32 || pat == OperandPattern.SREG;
        }

    }

}