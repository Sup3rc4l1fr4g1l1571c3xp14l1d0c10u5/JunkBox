using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using X86Asm.libelf;

namespace X86Asm.generator
{


    using InstructionStatement = X86Asm.ast.statement.InstructionStatement;
    using LabelStatement = X86Asm.ast.statement.LabelStatement;
    using Program = X86Asm.ast.Program;
    using IStatement = X86Asm.ast.statement.IStatement;
    using IOperand = X86Asm.ast.operand.IOperand;
    using ElfFile = X86Asm.libelf.ElfFile;
    using ElfHeader = X86Asm.libelf.ElfHeader;
    using ObjectType = X86Asm.libelf.ObjectType;
    using ProgramHeader = X86Asm.libelf.ProgramHeader;
    using SegmentType = X86Asm.libelf.SegmentType;

    public static class Assembler {

        private static InstructionPatternTable patterntable = InstructionPatternTable.MODE32_TABLE;

        /// <summary>
        /// �\���؂ɑ΂���A�Z���u���������s���A���ʂ�Stream�ɏ�������
        /// </summary>
        /// <param name="program">�\����</param>
        /// <param name="outputfile">�o�̓X�g���[��</param>
        public static byte[] assemble(Program program, uint offset) {
            if (program == null) {
                throw new ArgumentNullException();
            }

            // ���x���I�t�Z�b�g�\�𐶐�
            IDictionary<string, uint> labelOffsets = computeLabelOffsets(program, offset);
            
            // �\���؂�|�󂵂ċ@�B�����������
            return assembleToBytes(program, labelOffsets,offset);
        }

        /// <summary>
        /// ���x���̃I�t�Z�b�g�A�h���X���Z�o���A���x���I�t�Z�b�g�\�𐶐�����
        /// </summary>
        /// <param name="program">�\����</param>
        /// <returns></returns>
        private static IDictionary<string, uint> computeLabelOffsets(Program program, uint offset) {
            IDictionary<string, uint> result = new Dictionary<string, uint>();
            foreach (IStatement st in program.Statements) {
                if (st is InstructionStatement) {
                    // ���ߌ�̏ꍇ�͖��ߌ�̒������Z�o���Č��݈ʒu�ɉ��Z
                    InstructionStatement ist = (InstructionStatement)st;
                    string mnemonic = ist.Mnemonic;
                    IList<IOperand> operands = ist.Operands.ToList();
                    int length = CodeGenerator.getMachineCodeLength(patterntable, mnemonic, operands);
                    offset += (uint)length;
                } else if (st is LabelStatement) {
                    // ���݈ʒu�ƃ��x�����̑΂�\�ɋL�^����
                    string name = ((LabelStatement)st).Name;
                    result[name] = offset;
                }
            }
            return result;
        }

        /// <summary>
        /// �\���؂���@�B��𐶐�
        /// </summary>
        /// <param name="program"></param>
        /// <param name="labelOffsets"></param>
        /// <returns></returns>
        private static byte[] assembleToBytes(Program program, IDictionary<string, uint> labelOffsets, uint offset) {
            using (var ms = new System.IO.MemoryStream()) {
                foreach (IStatement st in program.Statements) {
                    if (st is InstructionStatement) {
                        // ���ߕ��Ȃ�΃R�[�h�𐶐�
                        InstructionStatement ist = (InstructionStatement)st;
                        string mnemonic = ist.Mnemonic;
                        IList<IOperand> operands = ist.Operands.ToList();
                        byte[] machinecode = CodeGenerator.makeMachineCode(patterntable, mnemonic, operands, program, labelOffsets, offset);
                        ms.Write(machinecode, 0, machinecode.Length);
                        offset += (uint)machinecode.Length;
                    } else if (st is LabelStatement) {
                        // ���x�����̏ꍇ�A���x���I�t�Z�b�g�\�ƌ��݈ʒu������Ă��Ȃ����`�F�b�N
                        string name = ((LabelStatement)st).Name;
                        if (offset != labelOffsets[name]) {
                            throw new InvalidOperationException("Label offset mismatch");
                        }
                    }
                }
                return ms.ToArray();
            }
        }
    }

}