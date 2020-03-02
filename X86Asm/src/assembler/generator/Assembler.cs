using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using X86Asm.libelf;

namespace X86Asm.generator
{


    using InstructionStatement = X86Asm.ast.statement.InstructionStatement;
    using LabelStatement = X86Asm.ast.statement.LabelStatement;
    using DirectiveStatement = X86Asm.ast.statement.DirectiveStatement;
    using Program = X86Asm.ast.Program;
    using IStatement = X86Asm.ast.statement.IStatement;
    using IOperand = X86Asm.ast.operand.IOperand;
    using ElfFile = X86Asm.libelf.ElfFile;
    using ElfHeader = X86Asm.libelf.ElfHeader;
    using ObjectType = X86Asm.libelf.ObjectType;
    using ProgramHeader = X86Asm.libelf.ProgramHeader;
    using SegmentType = X86Asm.libelf.SegmentType;

    public class Section {
        public string name; //  �Z�N�V������
        public uint index; //  �Z�N�V�����ԍ��i�Z�N�V�����e�[�u���̃C���f�N�X�ԍ��Ɠ������j
        public uint size;   // �Z�N�V�����̃T�C�Y
                            // �Z�N�V�������̍Ĕz�u���
        public class Relocation {
            public uint offset;
            public uint appliedTo;
            public Section section;
            // �Ӗ�
            // ���̃Z�N�V������offset����n�܂�4�o�C�g(i386�Ȃ�sizeof(ptr_t)==4�̊��̏ꍇ�Bx64�Ȃ�8�o�C�g)��
            // �i�����J/���[�_����������ۂɂ́j�Z�N�V����section�̃I�t�Z�b�gappliedTo�������A�h���X�ɏ��������Ăق���
        }
        public List<Relocation> relocations;
        public List<Symbol> symbols;

        public override string ToString() {
            return name;
        }
    }

    public class Symbol {
        public bool global;
        public string name;
        public Section section;
        public uint offset;
        public override string ToString() {
            return name;
        }
    }

    public static class Assembler {

        private static InstructionPatternTable patterntable = InstructionPatternTable.MODE32_TABLE;

        /// <summary>
        /// �\���؂ɑ΂���A�Z���u���������s���A���ʂ�Stream�ɏ�������
        /// </summary>
        /// <param name="program">�\����</param>
        /// <param name="outputfile">�o�̓X�g���[��</param>
        public static bool assemble(Program program, uint offset, out byte[] code, out Section[] section) {
            if (program == null) {
                throw new ArgumentNullException();
            }

            List<Section> sections = new List<Section>();
            Dictionary<string, Symbol> labelOffsets = new Dictionary<string, Symbol>();

            // ���x���I�t�Z�b�g�\�𐶐�
            computeLabelOffsets(program, sections, labelOffsets);

            // �\���؂�|�󂵂ċ@�B��𐶐�
            code = assembleToBytes(program, sections, labelOffsets);
            section = sections.ToArray();
            
            return true;
        }

        /// <summary>
        /// ���x���̃I�t�Z�b�g�A�h���X���Z�o���A���x���I�t�Z�b�g�\�𐶐�����
        /// </summary>
        /// <param name="program">�\����</param>
        /// <returns></returns>
        private static void computeLabelOffsets(Program program, List<Section> sections, IDictionary<string, Symbol> labelOffsets) {
            int sectionIndex = -1;
            uint offset = 0;
            var globalLabels = new List<string>();
            foreach (IStatement st in program.Statements) {
                if (st is DirectiveStatement) {
                    DirectiveStatement directive = (DirectiveStatement)st;
                    if (directive.Name == ".text" && directive.Arguments.Count == 0) {
                        if (sectionIndex != -1) {
                            sections[sectionIndex].size = offset;
                        }
                        sectionIndex = sections.FindIndex(x => x.name == ".text");
                        if (sectionIndex == -1) {
                            sectionIndex = sections.Count();
                            sections.Add(new Section() { name = ".text", size = 0, index = (uint)sectionIndex, relocations = new List<Section.Relocation>(), symbols = new List<Symbol>() });
                        }
                        offset = sections[sectionIndex].size;
                    } else if (directive.Name == ".globl" && directive.Arguments.Count == 1 && directive.Arguments[0] is ast.operand.Label) {
                        globalLabels.Add(((ast.operand.Label)directive.Arguments[0]).Name);
                    } else {
                        throw new Exception("�s���ȃf�B���N�e�B�u�ł��B");
                    }
                } else if (st is InstructionStatement) {
                    if (sectionIndex == -1) {
                        throw new Exception("�z�u�Z�N�V�������w�肳��Ă��܂���B");
                    }
                    // ���ߌ�̏ꍇ�͖��ߌ�̒������Z�o���Č��݈ʒu�ɉ��Z
                    InstructionStatement ist = (InstructionStatement)st;
                    string mnemonic = ist.Mnemonic;
                    IList<IOperand> operands = ist.Operands.ToList();
                    int length = CodeGenerator.getMachineCodeLength(patterntable, mnemonic, operands);
                    offset += (uint)length;
                } else if (st is LabelStatement) {
                    if (sectionIndex == -1) {
                        throw new Exception("�z�u�Z�N�V�������w�肳��Ă��܂���B");
                    }
                    // ���݈ʒu�ƃ��x�����̑΂�\�ɋL�^����
                    string name = ((LabelStatement)st).Name;
                    labelOffsets[name] = new Symbol() { section = sections[sectionIndex], offset = offset, name = name };
                    sections[sectionIndex].symbols.Add(labelOffsets[name]);
                }
            }
            if (sectionIndex != -1) {
                sections[sectionIndex].size = offset;
            }
            foreach (var label in globalLabels) {
                labelOffsets[label].global = true;
            }
        }

        /// <summary>
        /// �\���؂���@�B��𐶐�
        /// </summary>
        /// <param name="program"></param>
        /// <param name="labelOffsets"></param>
        /// <returns></returns>
        private static byte[] assembleToBytes(Program program, List<Section> sections, IDictionary<string, Symbol> labelOffsets) {
            using (var ms = new System.IO.MemoryStream()) {
                List<Section> workSections = new List<Section>();
                int sectionIndex = -1;
                uint offset = 0;
                foreach (IStatement st in program.Statements) {
                    if (st is DirectiveStatement) {
                        DirectiveStatement directive = (DirectiveStatement)st;
                        if (directive.Name == ".text" && directive.Arguments.Count == 0) {
                            if (sectionIndex != -1) {
                                if (sections[sectionIndex].name != workSections[sectionIndex].name) {
                                    throw new Exception("���x���v�Z���ƃR�[�h�������ŃZ�N�V�������̑Ή����Ƃ�Ă��Ȃ�");
                                }
                                workSections[sectionIndex].size = offset;
                            }
                            sectionIndex = workSections.FindIndex(x => x.name == ".text");
                            if (sectionIndex == -1) {
                                sectionIndex = workSections.Count();
                                workSections.Add(new Section() { name = ".text", size = 0, index = (uint)sectionIndex, relocations = new List<Section.Relocation>() });
                            }
                            offset = workSections[sectionIndex].size;
                        } else if (directive.Name == ".globl" && directive.Arguments.Count == 1 && directive.Arguments[0] is ast.operand.Label) {
                            // skip
                        } else {
                            throw new Exception("�s���ȃf�B���N�e�B�u�ł��B");
                        }
                    } else if (st is InstructionStatement) {
                        // ���ߕ��Ȃ�΃R�[�h�𐶐�
                        InstructionStatement ist = (InstructionStatement)st;
                        string mnemonic = ist.Mnemonic;
                        IList<IOperand> operands = ist.Operands.ToList();
                        byte[] machinecode = CodeGenerator.makeMachineCode(patterntable, mnemonic, operands, program, labelOffsets, sections[sectionIndex], offset);
                        ms.Write(machinecode, 0, machinecode.Length);
                        offset += (uint)machinecode.Length;
                    } else if (st is LabelStatement) {
                        // ���x�����̏ꍇ�A���x���I�t�Z�b�g�\�ƌ��݈ʒu������Ă��Ȃ����`�F�b�N
                        string name = ((LabelStatement)st).Name;
                        if (sections[sectionIndex] != labelOffsets[name].section || offset != labelOffsets[name].offset || name != labelOffsets[name].name) {
                            throw new InvalidOperationException("���x���̃I�t�Z�b�g��񂪈�v���܂���");
                        }
                    }
                }
                return ms.ToArray();
            }
        }
    }

}