using System;
using System.Collections.Generic;
using System.Linq;

namespace X86Asm.generator {
    using Program = X86Asm.ast.Program;
    using IImmediate = X86Asm.ast.operand.IImmediate;
    using ImmediateValue = X86Asm.ast.operand.ImmediateValue;
    using Memory = X86Asm.ast.operand.Memory;
    using IOperand = X86Asm.ast.operand.IOperand;
    using Register = X86Asm.ast.operand.Register;
    using Register32 = X86Asm.ast.operand.Register32;

    public sealed class CodeGenerator {

        /// <summary>
        /// �A�Z���u������ŋL�q���ꂽ���ߗ񂩂�@�B���̃o�C�g���𓾂�
        /// </summary>
        /// <param name="table">���߃p�^�[���\</param>
        /// <param name="mnemonic"></param>
        /// <param name="operands"></param>
        /// <returns></returns>
        public static int getMachineCodeLength(InstructionPatternTable table, string mnemonic, IList<IOperand> operands) {
            // ���߃p�^�[���𓾂�
            InstructionPattern pat = table.match(mnemonic, operands);

            // �I�y�����h�̃T�C�Y�����߂Ė��ߒ��ɐݒ�
            int length = pat.opcodes.Length;
            if (pat.operandSizeMode == OperandSizeMode.MODE16) {
                length++;
            }

            // ���߂�ModRM�����`���̏ꍇ�AModRM���̃o�C�g�����Z�o���āA���ߒ��ɉ��Z����
            if (pat.options.Count == 1 && pat.options[0] is ModRM) {
                length += getModRMBytesLength((ModRM)pat.options[0], operands);
            }

            // �I�y�����h�̌`���ɉ����Ė��ߒ��ɃT�C�Y�����Z
            for (int i = 0; i < pat.operands.Count; i++) {
                OperandPattern slot = pat.operands[i];
                if (slot == OperandPattern.IMM8 || slot == OperandPattern.IMM8S || slot == OperandPattern.REL8) {
                    length += 1;
                } else if (slot == OperandPattern.IMM16 || slot == OperandPattern.REL16) {
                    length += 2;
                } else if (slot == OperandPattern.IMM32 || slot == OperandPattern.REL32) {
                    length += 4;
                }
            }

            return length;
        }


        /// <summary>
        /// ModR/M���ΏۂƂ���I�y�����h�̏�������ModRM�o�C�g�������߂�
        /// </summary>
        /// <param name="option">ModR/M���</param>
        /// <param name="operands">�I�y�����h</param>
        /// <returns></returns>
        private static int getModRMBytesLength(ModRM option, IList<IOperand> operands) {
            // ModR/M���ΏۂƂ���I�؃����h�����o��
            IOperand rm = operands[option.rmOperandIndex];

            if (rm is Register) {
                // �I�y�����h�����W�X�^�̏ꍇ�AModRM�o�C�g���͂P�o�C�g
                return 1;
            } else if (rm is Memory) {
                // �I�y�����h���������̏ꍇ�A
                Memory m = (Memory)rm;
                IImmediate disp = m.Displacement;

                if (m.Base == null && m.Index == null) {
                    // �x�[�X�ƃC���f�N�X�������Ȃ��ꍇ�AModRM�o�C�g���͂T�o�C�g
                    // �܂� disp32 �`��
                    return 5;
                } else if (m.Base != Register32.ESP && m.Base != Register32.EBP && m.Index == null && disp is ImmediateValue && ((ImmediateValue)disp).IsZero()) {
                    // �x�[�X��ESP/EBP���W�X�^�ȊO���A�C���f�N�X���������A�f�B�X�v���C�����g�����l�O�̏ꍇ
                    // �܂�A (eax, ecx, edx, ebx, esi, edi) + 0 �`��
                    return 1;
                } else if (m.Base != Register32.ESP && m.Index == null && disp is ImmediateValue && ((ImmediateValue)disp).IsInt8()) {
                    // �x�[�X��ESP���W�X�^�ȊO���A�C���f�N�X���������A�f�B�X�v���C�����g��8bit���l�̏ꍇ
                    // �܂�A(eax, ecx, edx, ebx, ebp, esi, edi) + disp8 �`��
                    return 2;
                } else if (m.Base != Register32.ESP && m.Index == null) {
                    // �x�[�X��ESP���W�X�^�ȊO���A�C���f�N�X�������Ȃ��ꍇ�ŁA�f�B�X�v���C�����g��8bit���l�⑦�l�O�o�Ȃ��ꍇ
                    // �܂�A(eax, ecx, edx, ebx, ebp, esi, edi) + disp32 �`��
                    return 5;
                } else {
                    // ModR/M��SIB (Scale Index Base)���w�肵�Ă��邽�߁A
                    // SIB�̌`���ɉ�����ModRM�o�C�g�������߂�

                    if (m.Base == null) {
                        // �C���f�b�N�X���W�X�^ * �X�P�[�� + 32bit�萔 �̌`��
                        // index * scale + disp32
                        return 6;
                    } else if (m.Base != Register32.EBP && disp is ImmediateValue && ((ImmediateValue)disp).IsZero()) {
                        // �x�[�X���W�X�^ + �C���f�b�N�X���W�X�^ * �X�P�[�� �̌`��
                        // (eax, ecx, edx, ebx, esp, esi, edi) + index * scale
                        return 2;
                    } else if (disp is ImmediateValue && ((ImmediateValue)disp).IsInt8()) {
                        // �x�[�X���W�X�^ + �C���f�b�N�X���W�X�^ * �X�P�[�� + 8bit�萔 �̌`��
                        // base + index * scale + disp8
                        return 3;
                    } else {
                        // �x�[�X���W�X�^ + �C���f�b�N�X���W�X�^ * �X�P�[�� + 32bit�萔 �̌`��
                        // base + index * scale + disp32
                        return 6;
                    }
                }

            } else {
                throw new Exception("ModR/M�̑ΏۃI�y�����h�����W�X�^�ł��������ł�����܂���B");
            }
        }

        /// <summary>
        /// �A�Z���u������ŋL�q���ꂽ���ߗ񂩂�@�B���̃o�C�g��𓾂�
        /// </summary>
        /// <param name="table"></param>
        /// <param name="mnemonic"></param>
        /// <param name="operands"></param>
        /// <param name="program"></param>
        /// <param name="labelOffsets"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public static byte[] makeMachineCode(InstructionPatternTable table, string mnemonic, IList<IOperand> operands, Program program, IDictionary<string, Tuple<Section,uint>> labelOffsets, Section section, uint offset) {
            // ���߃p�^�[���𓾂�
            InstructionPattern pat = table.match(mnemonic, operands);

            // �@�B���o�b�t�@�𐶐�
            List<byte> result = new List<byte>();

            // ���߂̃I�y�����h�T�C�Y���[�h��MODE16�̏ꍇ�A�I�y�����h�T�C�Y�̏㏑���v���t�B�b�N�X��ǉ�
            if (pat.operandSizeMode == OperandSizeMode.MODE16) {
                result.Add(0x66);
            }

            byte[] opcodes = pat.opcodes;

            // OPCode����RegisterInOpCode���w�肳��Ă���ꍇ�ARegisterInOpCode������
            if (pat.options.Count == 1 && pat.options[0] is RegisterInOpcode) {
                // �w�肳�ꂽ�I�y�����h�ɋL�ڂ���Ă��郌�W�X�^�ԍ��𐶐����閽�߂̖����o�C�g�ɉ��Z�i�_���a�j�������̂ɂ���
                RegisterInOpcode option = (RegisterInOpcode)pat.options[0];
                opcodes = opcodes.ToArray();
                opcodes[opcodes.Length - 1] += (byte)((Register)operands[option.operandIndex]).RegisterNumber;
            }

            // �o�b�t�@��OP�R�[�h��ǉ�
            result.AddRange(opcodes);

            // ModR/M��SIB���w�肳��Ă���ꍇ��ModR/M�o�C�g�𐶐����Ēǉ�����
            // Append ModR/M and SIB bytes if necessary
            if (pat.options.Count == 1 && pat.options[0] is ModRM) {
                var modRMBytes = makeModRMBytes((ModRM)pat.options[0], operands, program, labelOffsets, section, offset);
                result.AddRange(modRMBytes);
            }

            for (int i = 0; i < pat.operands.Count; i++) {
                OperandPattern slot = pat.operands[i];

                // ���߂��󗝂������[i]�̌`�������l�I�y�����h�̏ꍇ
                if (slot == OperandPattern.IMM8 || slot == OperandPattern.IMM8S || slot == OperandPattern.IMM16 || slot == OperandPattern.IMM32 || slot == OperandPattern.REL8 || slot == OperandPattern.REL16 || slot == OperandPattern.REL32) {

                    // ���x���I�t�Z�b�g���l�����ăI�y�����h�̑��l�\���𓾂�
                    ImmediateValue value = ((IImmediate)operands[i]).GetValue(labelOffsets);

                    // ���߂��󗝂�������̌`�������l�I�y�����h��REL8, REL16,REL32�`���̏ꍇ�A
                    if (slot == OperandPattern.REL8 || slot == OperandPattern.REL16 || slot == OperandPattern.REL32) {
                        // ���l�\���̒l���Z�N�V��������΃A�h���X�l���疽�ߑ��΃A�h���X�l�ɕϊ�����
                        value = new ImmediateValue(value.GetValue(labelOffsets).Value - (int)(offset - getMachineCodeLength(table, mnemonic, operands)));
                    }

                    if (slot == OperandPattern.IMM8) {
                        // �����Ȃ�8�r�b�g���l�𖽗ߗ�ɒǉ�
                        result.AddRange(value.To1Byte());
                    } else if (slot == OperandPattern.IMM16) {
                        // �����Ȃ�16�r�b�g���l�𖽗ߗ�ɒǉ�
                        result.AddRange(value.To2Bytes());
                    } else if (slot == OperandPattern.IMM32 || slot == OperandPattern.REL32) {
                        // �����Ȃ�32�r�b�g���l�𖽗ߗ�ɒǉ�
                        result.AddRange(value.To4Bytes());
                    } else if (slot == OperandPattern.IMM8S || slot == OperandPattern.REL8) {
                        // ��������8�r�b�g���l�𖽗ߗ�ɒǉ�
                        if (!value.IsInt8()) {
                            throw new Exception("Not a signed 8-bit immediate operand");
                        }
                        result.AddRange(value.To1Byte());
                    } else if (slot == OperandPattern.REL16) {
                        // ��������16�r�b�g���l�𖽗ߗ�ɒǉ�
                        if (!value.IsInt16()) {
                            throw new Exception("Not a signed 16-bit immediate operand");
                        }
                        result.AddRange(value.To2Bytes());
                    } else {
                        // ��������32�r�b�g���l�Ȃǂ͑��΃A�h���X�l�Ƃ��Ďg���Ȃ��̂ŃG���[
                        throw new Exception("�w�肳�ꂽ�l�͑��l�Ƃ��ė��p�ł��܂���B");
                    }
                }
            }

            // �������ꂽ�@�B���Ԃ�
            return result.ToArray();
        }


        private static byte[] makeModRMBytes(ModRM option, IList<IOperand> operands, Program program, IDictionary<string, uint> labelOffsets) {
            IOperand rm = operands[option.rmOperandIndex];
            uint mod;
            uint rmvalue;
            byte[] rest;

            if (rm is Register) {
                mod = 3;
                rmvalue = ((Register)rm).RegisterNumber;
                rest = new byte[0];

            } else if (rm is Memory) {
                Memory m = (Memory)rm;
                ImmediateValue disp = m.Displacement.GetValue(labelOffsets);

                if (m.Base == null && m.Index == null) // disp32
                {
                    mod = 0;
                    rmvalue = 5;
                    rest = disp.To4Bytes();

                } // eax, ecx, edx, ebx, esi, edi
                else if (m.Base != Register32.ESP && m.Base != Register32.EBP && m.Index == null && m.Displacement is ImmediateValue && disp.IsZero()) {
                    mod = 0;
                    rmvalue = m.Base.RegisterNumber;
                    rest = new byte[0];

                } // (eax, ecx, edx, ebx, ebp, esi, edi) + disp8
                else if (m.Base != Register32.ESP && m.Index == null && m.Displacement is ImmediateValue && disp.IsInt8()) {
                    mod = 1;
                    rmvalue = m.Base.RegisterNumber;
                    rest = disp.To1Byte();

                } // (eax, ecx, edx, ebx, ebp, esi, edi) + disp32
                else if (m.Base != Register32.ESP && m.Index == null) {
                    mod = 2;
                    rmvalue = m.Base.RegisterNumber;
                    rest = disp.To4Bytes();

                } // SIB
                else {
                    rmvalue = 4;

                    if (m.Base == null) // index*scale + disp32
                    {
                        mod = 0;
                        rest = disp.To4Bytes();

                    } // (eax, ecx, edx, ebx, esp, esi, edi) + index*scale
                    else if (m.Base != Register32.EBP && m.Displacement is ImmediateValue && disp.IsZero()) {
                        mod = 0;
                        rest = new byte[0];

                    } // base + index*scale + disp8
                    else if (m.Displacement is ImmediateValue && disp.IsInt8()) {
                        mod = 1;
                        rest = disp.To1Byte();

                    } // base + index*scale + disp32
                    else {
                        mod = 2;
                        rest = disp.To4Bytes();
                    }

                    byte[] sib = makeSIBByte(m);
                    rest = concatenate(sib, rest);
                }

            } else {
                throw new Exception("Not a register or memory operand");
            }

            // Set reg/op value
            uint regopvalue;
            if (option.regOpcodeOperandIndex < 10) {
                Register regop = (Register)operands[option.regOpcodeOperandIndex];
                regopvalue = regop.RegisterNumber;
            } else {
                regopvalue = (uint)(option.regOpcodeOperandIndex - 10);
            }

            // Make ModR/M byte
            byte[] modrm = makeModRMByte(mod, regopvalue, rmvalue);

            // Concatenate and return
            return concatenate(modrm, rest);
        }


        private static byte[] makeModRMByte(uint mod, uint regop, uint rm) {
            if (mod < 0 || mod >= 4 || regop < 0 || regop >= 8 || rm < 0 || rm >= 8) {
                throw new Exception("Invalid ModR/M fields");
            }
            return new byte[] { (byte)(mod << 6 | regop << 3 | rm << 0) };
        }


        private static byte[] makeSIBByte(Memory mem) {
            uint scale = getScaleNumber(mem.Scale);
            uint index = getIndexNumber(mem.Index);
            uint @base = getBaseNumber(mem.Base);
            return new byte[] { (byte)(scale << 6 | index << 3 | @base << 0) };
        }


        private static uint getBaseNumber(Register32 @base) {
            if (@base != null) {
                return @base.RegisterNumber;
            } else {
                return 5;
            }
        }


        private static uint getIndexNumber(Register32 index) {
            if (index == Register32.ESP) {
                throw new System.ArgumentException("ESP register not allowed");
            }
            if (index != null) {
                return index.RegisterNumber;
            } else {
                return 4;
            }
        }


        private static uint getScaleNumber(int scale) {
            switch (scale) {
                case 1:
                    return 0;
                case 2:
                    return 1;
                case 4:
                    return 2;
                case 8:
                    return 3;
                default:
                    throw new ArgumentOutOfRangeException(nameof(scale), "Invalid scale");
            }
        }


        private static byte[] concatenate(params byte[][] arrays) {
            int totalLength = 0;
            foreach (byte[] b in arrays) {
                totalLength += b.Length;
            }

            byte[] result = new byte[totalLength];
            int offset = 0;
            foreach (byte[] b in arrays) {
                Array.Copy(b, 0, result, offset, b.Length);
                offset += b.Length;
            }
            return result;
        }



        /// <summary>
        /// Not instantiable.
        /// </summary>
        private CodeGenerator() {
        }

    }

}