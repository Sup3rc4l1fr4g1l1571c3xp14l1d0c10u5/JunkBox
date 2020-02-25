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
        /// 構文木に対するアセンブル処理を行い、結果をStreamに書き込む
        /// </summary>
        /// <param name="program">構文木</param>
        /// <param name="outputfile">出力ストリーム</param>
        public static byte[] assemble(Program program, uint offset) {
            if (program == null) {
                throw new ArgumentNullException();
            }

            // ラベルオフセット表を生成
            IDictionary<string, uint> labelOffsets = computeLabelOffsets(program, offset);
            
            // 構文木を翻訳して機械語を書き込み
            return assembleToBytes(program, labelOffsets,offset);
        }

        /// <summary>
        /// ラベルのオフセットアドレスを算出し、ラベルオフセット表を生成する
        /// </summary>
        /// <param name="program">構文木</param>
        /// <returns></returns>
        private static IDictionary<string, uint> computeLabelOffsets(Program program, uint offset) {
            IDictionary<string, uint> result = new Dictionary<string, uint>();
            foreach (IStatement st in program.Statements) {
                if (st is InstructionStatement) {
                    // 命令語の場合は命令語の長さを算出して現在位置に加算
                    InstructionStatement ist = (InstructionStatement)st;
                    string mnemonic = ist.Mnemonic;
                    IList<IOperand> operands = ist.Operands.ToList();
                    int length = CodeGenerator.getMachineCodeLength(patterntable, mnemonic, operands);
                    offset += (uint)length;
                } else if (st is LabelStatement) {
                    // 現在位置とラベル名の対を表に記録する
                    string name = ((LabelStatement)st).Name;
                    result[name] = offset;
                }
            }
            return result;
        }

        /// <summary>
        /// 構文木から機械語を生成
        /// </summary>
        /// <param name="program"></param>
        /// <param name="labelOffsets"></param>
        /// <returns></returns>
        private static byte[] assembleToBytes(Program program, IDictionary<string, uint> labelOffsets, uint offset) {
            using (var ms = new System.IO.MemoryStream()) {
                foreach (IStatement st in program.Statements) {
                    if (st is InstructionStatement) {
                        // 命令文ならばコードを生成
                        InstructionStatement ist = (InstructionStatement)st;
                        string mnemonic = ist.Mnemonic;
                        IList<IOperand> operands = ist.Operands.ToList();
                        byte[] machinecode = CodeGenerator.makeMachineCode(patterntable, mnemonic, operands, program, labelOffsets, offset);
                        ms.Write(machinecode, 0, machinecode.Length);
                        offset += (uint)machinecode.Length;
                    } else if (st is LabelStatement) {
                        // ラベル分の場合、ラベルオフセット表と現在位置がずれていないかチェック
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