using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace X86Asm.generator {
    using X86Asm.ast;
    using X86Asm.ast.statement;
    using X86Asm.ast.operand;
    using X86Asm.model;

    public static class Assembler {

        private static InstructionPatternTable instructionPatternTable = InstructionPatternTable.MODE32_TABLE;
        private static List<DirectivePattern> directivePatternTable = DirectivePatternTable.DIRECTIVE_TABLE;

        /// <summary>
        /// 構文木に対するアセンブル処理を行う
        /// </summary>
        /// <param name="program">構文木</param>
        /// <param name="sections">セクションリスト</param>
        /// <returns></returns>
        public static bool assemble(Program program, List<Section> sections) {
            if (program == null) {
                throw new ArgumentNullException();
            }

            // シンボル表を生成
            Dictionary<string, Symbol> symbolTable = new Dictionary<string, Symbol>();

            // シンボルのアドレスを計算してシンボル表に書き込む。
            computeLabelOffsets(program, sections, symbolTable);

            // 構文木を翻訳して機械語を生成
            assembleToBytes(program, sections, symbolTable);

            return true;
        }

        /// <summary>
        /// ラベルのオフセットアドレスを算出し、シンボル表を生成する
        /// </summary>
        /// <param name="program">構文木</param>
        /// <returns></returns>
        private static void computeLabelOffsets(Program program, List<Section> sections, IDictionary<string, Symbol> symbolTable) {
            int currentSectionIndex = -1;
            uint offset = 0;
            var globalLabels = new List<string>();
            var sectionSizeTable = new Dictionary<int, uint>();
            foreach (IStatement st in program.Statements) {
                if (st is DirectiveStatement) {
                    DirectiveStatement directive = (DirectiveStatement)st;
                    var pat = directivePatternTable.FirstOrDefault(x => x.Name == directive.Name);
                    if (pat != null) {
                        pat.OnComputeLabelOffsets(directive, sections, ref currentSectionIndex, ref offset, sectionSizeTable, globalLabels);
                    } else {
                        throw new Exception("不明なディレクティブです。");
                    }
                } else if (st is InstructionStatement) {
                    if (currentSectionIndex == -1) {
                        throw new Exception("配置セクションが指定されていません。");
                    }
                    // 命令語の場合は命令語の長さを算出して現在位置に加算
                    InstructionStatement ist = (InstructionStatement)st;
                    string mnemonic = ist.Mnemonic;
                    IList<IOperand> operands = ist.Operands.ToList();
                    int length = CodeGenerator.getMachineCodeLength(instructionPatternTable, mnemonic, operands);
                    offset += (uint)length;
                } else if (st is LabelStatement) {
                    if (currentSectionIndex == -1) {
                        throw new Exception("配置セクションが指定されていません。");
                    }
                    // 現在位置とラベル名の対を表に記録する
                    string name = ((LabelStatement)st).Name;
                    symbolTable[name] = new Symbol() { section = sections[currentSectionIndex], offset = offset, name = name };
                    sections[currentSectionIndex].symbols.Add(symbolTable[name]);
                }
            }
            foreach (var label in globalLabels) {
                symbolTable[label].global = true;
            }
        }

        /// <summary>
        /// 構文木から機械語を生成
        /// </summary>
        /// <param name="program"></param>
        /// <param name="symbolTable"></param>
        /// <returns></returns>
        private static void assembleToBytes(Program program, List<Section> sections, IDictionary<string, Symbol> symbolTable) {
            var sectionSizeTable = Enumerable.Repeat(0U, sections.Count).ToArray();
            int currentSectionIndex = -1;
            uint offset = 0;
            foreach (IStatement st in program.Statements) {
                if (st is DirectiveStatement) {
                    DirectiveStatement directive = (DirectiveStatement)st;
                    var pat = directivePatternTable.FirstOrDefault(x => x.Name == directive.Name);
                    if (pat != null) {
                        // ディレクティブを処理
                        pat.OnAssembleBytes(directive, sections, ref currentSectionIndex, ref offset, sectionSizeTable, symbolTable);
                    } else {
                        throw new Exception("不明なディレクティブです。");
                    }
                } else if (st is InstructionStatement) {
                    InstructionStatement ist = (InstructionStatement)st;

                    // 命令文のニーモニックとオペランドを取り出す
                    string mnemonic = ist.Mnemonic;
                    IList<IOperand> operands = ist.Operands.ToList();
                    
                    // 機械語列を生成
                    byte[] machinecode = CodeGenerator.makeMachineCode(instructionPatternTable, mnemonic, operands, program, symbolTable, sections[currentSectionIndex], offset);

                    // 生成された機械語列をセクションのRawDataに書き込む
                    sections[currentSectionIndex].data.Write(machinecode, 0, machinecode.Length);

                    offset += (uint)machinecode.Length;
                } else if (st is LabelStatement) {
                    // ラベル文の場合、シンボル表と現在位置がずれていないかチェック
                    string name = ((LabelStatement)st).Name;
                    if (sections[currentSectionIndex] != symbolTable[name].section || offset != symbolTable[name].offset || name != symbolTable[name].name) {
                        throw new InvalidOperationException("ラベルのオフセット情報が一致しません");
                    }
                }
            }
        }
    }

}