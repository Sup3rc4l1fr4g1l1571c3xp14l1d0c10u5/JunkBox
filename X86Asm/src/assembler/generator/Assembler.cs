using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using X86Asm.libelf;

namespace X86Asm.generator {
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
    using ast.operand;

    public class Section {
        public string name; //  セクション名
        public uint index; //  セクション番号（セクションテーブルのインデクス番号と等しい）
        public uint size;   // セクションのサイズ
                            // セクション内の再配置情報
        public class Relocation {
            public uint VirtualAddress;
            public Symbol SymbolTableIndex;
            //public Section section;
            // 意味
            // このセクションのVirtualAddressから始まる4バイト(i386などsizeof(ptr_t)==4の環境の場合。x64なら8バイト)を
            // （リンカ/ローダが解決する際には）シンボルSymbolTableIndexが示すアドレスに書き換えてほしい。
        }
        public List<Relocation> relocations = new List<Section.Relocation>();
        public List<Symbol> symbols = new List<Symbol>();
        public MemoryStream data = new MemoryStream();

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
        public static bool assemble(Program program, uint offset, List<Section> sections) {
            if (program == null) {
                throw new ArgumentNullException();
            }

            //List<Section> sections = new List<Section>();
            Dictionary<string, Symbol> labelOffsets = new Dictionary<string, Symbol>();

            // ラベルオフセット表を生成
            computeLabelOffsets(program, sections, labelOffsets);

            // 構文木を翻訳して機械語を生成
            assembleToBytes(program, sections, labelOffsets);

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
                    if ((directive.Name == ".text" || directive.Name == ".data") && directive.Arguments.Count == 0) {
                        if (sectionIndex != -1) {
                            sections[sectionIndex].size = offset;
                        }
                        sectionIndex = sections.FindIndex(x => x.name == directive.Name);
                        if (sectionIndex == -1) {
                            sectionIndex = sections.Count();
                            sections.Add(new Section() { name = directive.Name, size = 0, index = (uint)sectionIndex });
                        }
                        offset = sections[sectionIndex].size;
                    } else if (directive.Name == ".globl" && directive.Arguments.Count == 1 && directive.Arguments[0] is ast.operand.Label) {
                        globalLabels.Add(((ast.operand.Label)directive.Arguments[0]).Name);
                    } else if (directive.Name == ".db") {
                        offset += (uint)directive.Arguments.Count * 1;
                    } else if (directive.Name == ".dw") {
                        offset += (uint)directive.Arguments.Count * 2;
                    } else if (directive.Name == ".dd") {
                        offset += (uint)directive.Arguments.Count * 4;
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
        private static void assembleToBytes(Program program, List<Section> sections, IDictionary<string, Symbol> labelOffsets) {
            var workSectionSize = Enumerable.Repeat(0U, sections.Count).ToArray();
            int sectionIndex = -1;
            uint offset = 0;
            foreach (IStatement st in program.Statements) {
                if (st is DirectiveStatement) {
                    DirectiveStatement directive = (DirectiveStatement)st;
                    if ((directive.Name == ".text" || directive.Name == ".data") && directive.Arguments.Count == 0) {
                        if (sectionIndex != -1) {
                            workSectionSize[sectionIndex] = offset;
                        }
                        sectionIndex = sections.FindIndex(x => x.name == directive.Name);
                        if (sectionIndex == -1) {
                            throw new Exception("辻褄が合わない");
                        }
                        offset = workSectionSize[sectionIndex];
                    } else if (directive.Name == ".globl" && directive.Arguments.Count == 1 && directive.Arguments[0] is ast.operand.Label) {
                        // skip
                    } else if (directive.Name == ".db") {
                        sections[sectionIndex].data.Write(directive.Arguments.SelectMany(x => ((IImmediate)x).GetValue(labelOffsets).To1Byte()).ToArray(), 0, directive.Arguments.Count * 1);
                        offset += (uint)directive.Arguments.Count * 1;
                    } else if (directive.Name == ".dw") {
                        sections[sectionIndex].data.Write(directive.Arguments.SelectMany(x => ((IImmediate)x).GetValue(labelOffsets).To2Bytes()).ToArray(), 0, directive.Arguments.Count * 2);
                        offset += (uint)directive.Arguments.Count * 2;
                    } else if (directive.Name == ".dd") {
                        sections[sectionIndex].data.Write(directive.Arguments.SelectMany(x => ((IImmediate)x).GetValue(labelOffsets).To4Bytes()).ToArray(), 0, directive.Arguments.Count * 4);
                        offset += (uint)directive.Arguments.Count * 4;
                    } else {
                        throw new Exception("不明なディレクティブです。");
                    }
                } else if (st is InstructionStatement) {
                    // 命令文ならばコードを生成
                    InstructionStatement ist = (InstructionStatement)st;
                    string mnemonic = ist.Mnemonic;
                    IList<IOperand> operands = ist.Operands.ToList();
                    var ret = CodeGenerator.makeMachineCode(patterntable, mnemonic, operands, program, labelOffsets, sections[sectionIndex], offset);
                    var relocations = ret.Item1;
                    byte[] machinecode = ret.Item2;
                    foreach (var rel in relocations) {
                        sections[sectionIndex].relocations.Add(new Section.Relocation() {
                            VirtualAddress = rel.Item2,
                            SymbolTableIndex = rel.Item1,
                        });
                    }
                    sections[sectionIndex].data.Write(machinecode, 0, machinecode.Length);
                    offset += (uint)machinecode.Length;
                } else if (st is LabelStatement) {
                    // ラベル文の場合、ラベルオフセット表と現在位置がずれていないかチェック
                    string name = ((LabelStatement)st).Name;
                    if (sections[sectionIndex] != labelOffsets[name].section || offset != labelOffsets[name].offset || name != labelOffsets[name].name) {
                        throw new InvalidOperationException("ラベルのオフセット情報が一致しません");
                    }
                    //sections[sectionIndex].relocations.Add(new Section.Relocation() {
                    //    VirtualAddress = offset,
                    //    SymbolTableIndex = labelOffsets[name],
                    //});

                }
            }
        }
    }

}