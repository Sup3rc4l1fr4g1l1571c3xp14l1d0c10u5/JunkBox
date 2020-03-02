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
        public string name; //  セクション名
        public uint index; //  セクション番号（セクションテーブルのインデクス番号と等しい）
        public uint size;   // セクションのサイズ
                            // セクション内の再配置情報
        public class Relocation {
            public uint offset;
            public uint appliedTo;
            public Section section;
            // 意味
            // このセクションのoffsetから始まる4バイト(i386などsizeof(ptr_t)==4の環境の場合。x64なら8バイト)を
            // （リンカ/ローダが解決する際には）セクションsectionのオフセットappliedToを示すアドレスに書き換えてほしい
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
        /// 構文木に対するアセンブル処理を行い、結果をStreamに書き込む
        /// </summary>
        /// <param name="program">構文木</param>
        /// <param name="outputfile">出力ストリーム</param>
        public static bool assemble(Program program, uint offset, out byte[] code, out Section[] section) {
            if (program == null) {
                throw new ArgumentNullException();
            }

            List<Section> sections = new List<Section>();
            Dictionary<string, Symbol> labelOffsets = new Dictionary<string, Symbol>();

            // ラベルオフセット表を生成
            computeLabelOffsets(program, sections, labelOffsets);

            // 構文木を翻訳して機械語を生成
            code = assembleToBytes(program, sections, labelOffsets);
            section = sections.ToArray();
            
            return true;
        }

        /// <summary>
        /// ラベルのオフセットアドレスを算出し、ラベルオフセット表を生成する
        /// </summary>
        /// <param name="program">構文木</param>
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
                        throw new Exception("不明なディレクティブです。");
                    }
                } else if (st is InstructionStatement) {
                    if (sectionIndex == -1) {
                        throw new Exception("配置セクションが指定されていません。");
                    }
                    // 命令語の場合は命令語の長さを算出して現在位置に加算
                    InstructionStatement ist = (InstructionStatement)st;
                    string mnemonic = ist.Mnemonic;
                    IList<IOperand> operands = ist.Operands.ToList();
                    int length = CodeGenerator.getMachineCodeLength(patterntable, mnemonic, operands);
                    offset += (uint)length;
                } else if (st is LabelStatement) {
                    if (sectionIndex == -1) {
                        throw new Exception("配置セクションが指定されていません。");
                    }
                    // 現在位置とラベル名の対を表に記録する
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
        /// 構文木から機械語を生成
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
                                    throw new Exception("ラベル計算時とコード生成時でセクション名の対応がとれていない");
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
                            throw new Exception("不明なディレクティブです。");
                        }
                    } else if (st is InstructionStatement) {
                        // 命令文ならばコードを生成
                        InstructionStatement ist = (InstructionStatement)st;
                        string mnemonic = ist.Mnemonic;
                        IList<IOperand> operands = ist.Operands.ToList();
                        byte[] machinecode = CodeGenerator.makeMachineCode(patterntable, mnemonic, operands, program, labelOffsets, sections[sectionIndex], offset);
                        ms.Write(machinecode, 0, machinecode.Length);
                        offset += (uint)machinecode.Length;
                    } else if (st is LabelStatement) {
                        // ラベル文の場合、ラベルオフセット表と現在位置がずれていないかチェック
                        string name = ((LabelStatement)st).Name;
                        if (sections[sectionIndex] != labelOffsets[name].section || offset != labelOffsets[name].offset || name != labelOffsets[name].name) {
                            throw new InvalidOperationException("ラベルのオフセット情報が一致しません");
                        }
                    }
                }
                return ms.ToArray();
            }
        }
    }

}