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
        /// アセンブリ言語で記述された命令列から機械語列のバイト長を得る
        /// </summary>
        /// <param name="table">命令パターン表</param>
        /// <param name="mnemonic"></param>
        /// <param name="operands"></param>
        /// <returns></returns>
        public static int getMachineCodeLength(InstructionPatternTable table, string mnemonic, IList<IOperand> operands) {
            // 命令パターンを得る
            InstructionPattern pat = table.match(mnemonic, operands);

            // オペランドのサイズを求めて命令長に設定
            int length = pat.opcodes.Length;
            if (pat.operandSizeMode == OperandSizeMode.MODE16) {
                length++;
            }

            // 命令がModRMを持つ形式の場合、ModRM部のバイト長を算出して、命令長に加算する
            if (pat.options.Count == 1 && pat.options[0] is ModRM) {
                length += getModRMBytesLength((ModRM)pat.options[0], operands);
            }

            // オペランドの形式に応じて命令長にサイズを加算
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
        /// ModR/Mが対象とするオペランドの書式からModRMバイト長を求める
        /// </summary>
        /// <param name="option">ModR/M情報</param>
        /// <param name="operands">オペランド</param>
        /// <returns></returns>
        private static int getModRMBytesLength(ModRM option, IList<IOperand> operands) {
            // ModR/Mが対象とするオぺランドを取り出す
            IOperand rm = operands[option.rmOperandIndex];

            if (rm is Register) {
                // オペランドがレジスタの場合、ModRMバイト長は１バイト
                return 1;
            } else if (rm is Memory) {
                // オペランドがメモリの場合、
                Memory m = (Memory)rm;
                IImmediate disp = m.Displacement;

                if (m.Base == null && m.Index == null) {
                    // ベースとインデクスを持たない場合、ModRMバイト長は５バイト
                    // つまり disp32 形式
                    return 5;
                } else if (m.Base != Register32.ESP && m.Base != Register32.EBP && m.Index == null && disp is ImmediateValue && ((ImmediateValue)disp).IsZero()) {
                    // ベースがESP/EBPレジスタ以外かつ、インデクスを持たず、ディスプレイメントが即値０の場合
                    // つまり、 (eax, ecx, edx, ebx, esi, edi) + 0 形式
                    return 1;
                } else if (m.Base != Register32.ESP && m.Index == null && disp is ImmediateValue && ((ImmediateValue)disp).IsInt8()) {
                    // ベースがESPレジスタ以外かつ、インデクスを持たず、ディスプレイメントが8bit即値の場合
                    // つまり、(eax, ecx, edx, ebx, ebp, esi, edi) + disp8 形式
                    return 2;
                } else if (m.Base != Register32.ESP && m.Index == null) {
                    // ベースがESPレジスタ以外かつ、インデクスを持たない場合で、ディスプレイメントが8bit即値や即値０出ない場合
                    // つまり、(eax, ecx, edx, ebx, ebp, esi, edi) + disp32 形式
                    return 5;
                } else {
                    // ModR/MはSIB (Scale Index Base)を指定しているため、
                    // SIBの形式に応じてModRMバイト長を求める

                    if (m.Base == null) {
                        // インデックスレジスタ * スケール + 32bit定数 の形式
                        // index * scale + disp32
                        return 6;
                    } else if (m.Base != Register32.EBP && disp is ImmediateValue && ((ImmediateValue)disp).IsZero()) {
                        // ベースレジスタ + インデックスレジスタ * スケール の形式
                        // (eax, ecx, edx, ebx, esp, esi, edi) + index * scale
                        return 2;
                    } else if (disp is ImmediateValue && ((ImmediateValue)disp).IsInt8()) {
                        // ベースレジスタ + インデックスレジスタ * スケール + 8bit定数 の形式
                        // base + index * scale + disp8
                        return 3;
                    } else {
                        // ベースレジスタ + インデックスレジスタ * スケール + 32bit定数 の形式
                        // base + index * scale + disp32
                        return 6;
                    }
                }

            } else {
                throw new Exception("ModR/Mの対象オペランドがレジスタでもメモリでもありません。");
            }
        }

        /// <summary>
        /// アセンブリ言語で記述された命令列から機械語列のバイト列を得る
        /// </summary>
        /// <param name="table"></param>
        /// <param name="mnemonic"></param>
        /// <param name="operands"></param>
        /// <param name="program"></param>
        /// <param name="labelOffsets"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public static Tuple<Tuple<Symbol,uint>[],byte[]> makeMachineCode(InstructionPatternTable table, string mnemonic, IList<IOperand> operands, Program program, IDictionary<string, Symbol> labelOffsets, Section section, uint offset) {
            // 命令パターンを得る
            InstructionPattern pat = table.match(mnemonic, operands);

            // 機械語列バッファを生成
            List<byte> result = new List<byte>();

            // 命令のオペランドサイズモードがMODE16の場合、オペランドサイズの上書きプレフィックスを追加
            if (pat.operandSizeMode == OperandSizeMode.MODE16) {
                result.Add(0x66);
            }

            byte[] opcodes = pat.opcodes;
            var relocates = new List<Tuple<Symbol, uint>>();
            // OPCode中にRegisterInOpCodeが指定されている場合、RegisterInOpCodeを処理
            if (pat.options.Count == 1 && pat.options[0] is RegisterInOpcode) {
                // 指定されたオペランドに記載されているレジスタ番号を生成する命令の末尾バイトに加算（論理和）したものにする
                RegisterInOpcode option = (RegisterInOpcode)pat.options[0];
                opcodes = opcodes.ToArray();
                opcodes[opcodes.Length - 1] += (byte)((Register)operands[option.operandIndex]).RegisterNumber;
            }

            // バッファにOPコードを追加
            result.AddRange(opcodes);

            // ModR/MとSIBが指定されている場合はModR/Mバイトを生成して追加する
            // Append ModR/M and SIB bytes if necessary
            if (pat.options.Count == 1 && pat.options[0] is ModRM) {
                var ret = makeModRMBytes((ModRM)pat.options[0], operands, program, labelOffsets);
                var symbol = ret.Item1;
                var vaddress = ret.Item2;
                var modRMBytes = ret.Item3;
                if (symbol != null) {
                    relocates.Add(Tuple.Create(symbol, vaddress + (uint)result.Count + offset));
                }
                result.AddRange(modRMBytes);
            }

            for (int i = 0; i < pat.operands.Count; i++) {
                OperandPattern slot = pat.operands[i];

                // 命令が受理する引数[i]の形式が即値オペランドの場合
                if (slot == OperandPattern.IMM8 || slot == OperandPattern.IMM8S || slot == OperandPattern.IMM16 || slot == OperandPattern.IMM32 || slot == OperandPattern.REL8 || slot == OperandPattern.REL16 || slot == OperandPattern.REL32) {

                    // ラベルオフセットを考慮してオペランドの即値表現を得る
                    ImmediateValue value = ((IImmediate)operands[i]).GetValue(labelOffsets);

                    // 命令が受理する引数の形式が即値オペランドのREL8, REL16,REL32形式の場合、
                    if (slot == OperandPattern.REL8 || slot == OperandPattern.REL16 || slot == OperandPattern.REL32) {
                        var ivalue = value.GetValue(labelOffsets);
                        if (section != ivalue.Symbol.section) {
                            throw new Exception("セクションが違うため命令相対アドレスを求められない。");
                        }
                        // 即値表現の値をセクション内絶対アドレス値から命令相対アドレス値に変換する
                        value = new ImmediateValue(ivalue.Value - (int)(offset - getMachineCodeLength(table, mnemonic, operands)));
                    }
                    if (slot == OperandPattern.IMM8) {
                        // 符号なし8ビット即値を命令列に追加
                        result.AddRange(value.To1Byte());
                    } else if (slot == OperandPattern.IMM16) {
                        // 符号なし16ビット即値を命令列に追加
                        result.AddRange(value.To2Bytes());
                    } else if (slot == OperandPattern.IMM32 || slot == OperandPattern.REL32) {
                        // 符号なし32ビット即値を命令列に追加
                        if (value.Symbol != null) {
                            relocates.Add(Tuple.Create(value.Symbol, (uint)result.Count + offset));
                        }
                        result.AddRange(value.To4Bytes());
                    } else if (slot == OperandPattern.IMM8S || slot == OperandPattern.REL8) {
                        // 符号あり8ビット即値を命令列に追加
                        if (!value.IsInt8()) {
                            throw new Exception("Not a signed 8-bit immediate operand");
                        }
                        result.AddRange(value.To1Byte());
                    } else if (slot == OperandPattern.REL16) {
                        // 符号あり16ビット即値を命令列に追加
                        if (!value.IsInt16()) {
                            throw new Exception("Not a signed 16-bit immediate operand");
                        }
                        result.AddRange(value.To2Bytes());
                    } else {
                        // 符号あり32ビット即値などは相対アドレス値として使えないのでエラー
                        throw new Exception("指定された値は即値として利用できません。");
                    }
                }
            }

            // 生成された機械語を返す
            return Tuple.Create(relocates.ToArray(),result.ToArray());
        }


        private static Tuple<Symbol,uint,byte[]> makeModRMBytes(ModRM option, IList<IOperand> operands, Program program, IDictionary<string, Symbol> labelOffsets) {
            IOperand rm = operands[option.rmOperandIndex];
            uint mod;
            uint rmvalue;
            byte[] rest;
            Symbol symbol = null;
            uint symbolOffset = 0;

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
                    if (disp.Symbol != null) {
                        symbol = disp.Symbol;
                        symbolOffset = 0;
                    }
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
                    if (disp.Symbol != null) {
                        symbol = disp.Symbol;
                        symbolOffset = 0;
                    }
                } // SIB
                else {
                    rmvalue = 4;

                    if (m.Base == null) // index*scale + disp32
                    {
                        mod = 0;
                        rest = disp.To4Bytes();
                        if (disp.Symbol != null) {
                            symbol = disp.Symbol;
                            symbolOffset = 0;
                        }

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
                        if (disp.Symbol != null) {
                            symbol = disp.Symbol;
                            symbolOffset = 0;
                        }
                    }

                    byte[] sib = makeSIBByte(m);
                    rest = sib.Concat(rest).ToArray();
                    if (symbol != null) {
                        symbolOffset = symbolOffset + (uint)sib.Length;
                    }
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

            if (symbol != null) {
                symbolOffset = symbolOffset + (uint)modrm.Length;
            }
            // Concatenate and return
            return Tuple.Create(symbol, symbolOffset, modrm.Concat(rest).ToArray());
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




        /// <summary>
        /// Not instantiable.
        /// </summary>
        private CodeGenerator() {
        }

    }

}