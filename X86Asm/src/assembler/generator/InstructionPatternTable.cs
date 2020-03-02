using System;
using System.Collections.Generic;

namespace X86Asm.generator {
    using IOperand = X86Asm.ast.operand.IOperand;


    /// <summary>
    /// 命令パターン表
    /// </summary>
    public class InstructionPatternTable {

        /// <summary>
        /// x86, 32-bit モードの命令パターン表 
        /// </summary>
        public static readonly InstructionPatternTable MODE32_TABLE;

        static InstructionPatternTable() {
            MODE32_TABLE = new InstructionPatternTable();
            MODE32_TABLE.add("byte", new[] { OperandPattern.IMM8 }, OperandSizeMode.MODELESS, new byte[] { });
            MODE32_TABLE.add("word", new[] { OperandPattern.IMM16 }, OperandSizeMode.MODELESS, new byte[] { });
            MODE32_TABLE.add("dword", new[] { OperandPattern.IMM32 }, OperandSizeMode.MODELESS, new byte[] { });
            MODE32_TABLE.add("addb", new[] { OperandPattern.RM8, OperandPattern.REG8 }, OperandSizeMode.MODELESS, new byte[] { 0x00 }, new ModRM(0, 1));
            MODE32_TABLE.add("addw", new[] { OperandPattern.RM16, OperandPattern.REG16 }, OperandSizeMode.MODE16, new byte[] { 0x01 }, new ModRM(0, 1));
            MODE32_TABLE.add("addl", new[] { OperandPattern.RM32, OperandPattern.REG32 }, OperandSizeMode.MODE32, new byte[] { 0x01 }, new ModRM(0, 1));
            MODE32_TABLE.add("addb", new[] { OperandPattern.REG8, OperandPattern.RM8 }, OperandSizeMode.MODELESS, new byte[] { 0x02 }, new ModRM(1, 0));
            MODE32_TABLE.add("addw", new[] { OperandPattern.REG16, OperandPattern.RM16 }, OperandSizeMode.MODE16, new byte[] { 0x03 }, new ModRM(1, 0));
            MODE32_TABLE.add("addl", new[] { OperandPattern.REG32, OperandPattern.RM32 }, OperandSizeMode.MODE32, new byte[] { 0x03 }, new ModRM(1, 0));
            MODE32_TABLE.add("addb", new[] { OperandPattern.AL, OperandPattern.IMM8 }, OperandSizeMode.MODELESS, new byte[] { 0x04 });
            MODE32_TABLE.add("addw", new[] { OperandPattern.AX, OperandPattern.IMM16 }, OperandSizeMode.MODE16, new byte[] { 0x05 });
            MODE32_TABLE.add("addl", new[] { OperandPattern.EAX, OperandPattern.IMM32 }, OperandSizeMode.MODE32, new byte[] { 0x05 });
            MODE32_TABLE.add("pushw", new[] { OperandPattern.ES }, OperandSizeMode.MODELESS, new byte[] { 0x06 });
            MODE32_TABLE.add("popw", new[] { OperandPattern.ES }, OperandSizeMode.MODELESS, new byte[] { 0x07 });
            MODE32_TABLE.add("orb", new[] { OperandPattern.RM8, OperandPattern.REG8 }, OperandSizeMode.MODELESS, new byte[] { 0x08 }, new ModRM(0, 1));
            MODE32_TABLE.add("orw", new[] { OperandPattern.RM16, OperandPattern.REG16 }, OperandSizeMode.MODE16, new byte[] { 0x09 }, new ModRM(0, 1));
            MODE32_TABLE.add("orl", new[] { OperandPattern.RM32, OperandPattern.REG32 }, OperandSizeMode.MODE32, new byte[] { 0x09 }, new ModRM(0, 1));
            MODE32_TABLE.add("orb", new[] { OperandPattern.REG8, OperandPattern.RM8 }, OperandSizeMode.MODELESS, new byte[] { 0x0A }, new ModRM(1, 0));
            MODE32_TABLE.add("orw", new[] { OperandPattern.REG16, OperandPattern.RM16 }, OperandSizeMode.MODE16, new byte[] { 0x0B }, new ModRM(1, 0));
            MODE32_TABLE.add("orl", new[] { OperandPattern.REG32, OperandPattern.RM32 }, OperandSizeMode.MODE32, new byte[] { 0x0B }, new ModRM(1, 0));
            MODE32_TABLE.add("orb", new[] { OperandPattern.AL, OperandPattern.IMM8 }, OperandSizeMode.MODELESS, new byte[] { 0x0C });
            MODE32_TABLE.add("orw", new[] { OperandPattern.AX, OperandPattern.IMM16 }, OperandSizeMode.MODE16, new byte[] { 0x0D });
            MODE32_TABLE.add("orl", new[] { OperandPattern.EAX, OperandPattern.IMM32 }, OperandSizeMode.MODE32, new byte[] { 0x0D });
            MODE32_TABLE.add("pushw", new[] { OperandPattern.CS }, OperandSizeMode.MODELESS, new byte[] { 0x0E });
            MODE32_TABLE.add("adcb", new[] { OperandPattern.RM8, OperandPattern.REG8 }, OperandSizeMode.MODELESS, new byte[] { 0x10 }, new ModRM(0, 1));
            MODE32_TABLE.add("adcw", new[] { OperandPattern.RM16, OperandPattern.REG16 }, OperandSizeMode.MODE16, new byte[] { 0x11 }, new ModRM(0, 1));
            MODE32_TABLE.add("adcl", new[] { OperandPattern.RM32, OperandPattern.REG32 }, OperandSizeMode.MODE32, new byte[] { 0x11 }, new ModRM(0, 1));
            MODE32_TABLE.add("adcb", new[] { OperandPattern.REG8, OperandPattern.RM8 }, OperandSizeMode.MODELESS, new byte[] { 0x12 }, new ModRM(1, 0));
            MODE32_TABLE.add("adcw", new[] { OperandPattern.REG16, OperandPattern.RM16 }, OperandSizeMode.MODE16, new byte[] { 0x13 }, new ModRM(1, 0));
            MODE32_TABLE.add("adcl", new[] { OperandPattern.REG32, OperandPattern.RM32 }, OperandSizeMode.MODE32, new byte[] { 0x13 }, new ModRM(1, 0));
            MODE32_TABLE.add("adcb", new[] { OperandPattern.AL, OperandPattern.IMM8 }, OperandSizeMode.MODELESS, new byte[] { 0x14 });
            MODE32_TABLE.add("adcw", new[] { OperandPattern.AX, OperandPattern.IMM16 }, OperandSizeMode.MODE16, new byte[] { 0x15 });
            MODE32_TABLE.add("adcl", new[] { OperandPattern.EAX, OperandPattern.IMM32 }, OperandSizeMode.MODE32, new byte[] { 0x15 });
            MODE32_TABLE.add("pushw", new[] { OperandPattern.SS }, OperandSizeMode.MODELESS, new byte[] { 0x16 });
            MODE32_TABLE.add("popw", new[] { OperandPattern.SS }, OperandSizeMode.MODELESS, new byte[] { 0x17 });
            MODE32_TABLE.add("sbbb", new[] { OperandPattern.RM8, OperandPattern.REG8 }, OperandSizeMode.MODELESS, new byte[] { 0x18 }, new ModRM(0, 1));
            MODE32_TABLE.add("sbbw", new[] { OperandPattern.RM16, OperandPattern.REG16 }, OperandSizeMode.MODE16, new byte[] { 0x19 }, new ModRM(0, 1));
            MODE32_TABLE.add("sbbl", new[] { OperandPattern.RM32, OperandPattern.REG32 }, OperandSizeMode.MODE32, new byte[] { 0x19 }, new ModRM(0, 1));
            MODE32_TABLE.add("sbbb", new[] { OperandPattern.REG8, OperandPattern.RM8 }, OperandSizeMode.MODELESS, new byte[] { 0x1A }, new ModRM(1, 0));
            MODE32_TABLE.add("sbbw", new[] { OperandPattern.REG16, OperandPattern.RM16 }, OperandSizeMode.MODE16, new byte[] { 0x1B }, new ModRM(1, 0));
            MODE32_TABLE.add("sbbl", new[] { OperandPattern.REG32, OperandPattern.RM32 }, OperandSizeMode.MODE32, new byte[] { 0x1B }, new ModRM(1, 0));
            MODE32_TABLE.add("sbbb", new[] { OperandPattern.AL, OperandPattern.IMM8 }, OperandSizeMode.MODELESS, new byte[] { 0x1C });
            MODE32_TABLE.add("sbbw", new[] { OperandPattern.AX, OperandPattern.IMM16 }, OperandSizeMode.MODE16, new byte[] { 0x1D });
            MODE32_TABLE.add("sbbl", new[] { OperandPattern.EAX, OperandPattern.IMM32 }, OperandSizeMode.MODE32, new byte[] { 0x1D });
            MODE32_TABLE.add("pushw", new[] { OperandPattern.DS }, OperandSizeMode.MODELESS, new byte[] { 0x1E });
            MODE32_TABLE.add("popw", new[] { OperandPattern.DS }, OperandSizeMode.MODELESS, new byte[] { 0x1F });
            MODE32_TABLE.add("andb", new[] { OperandPattern.RM8, OperandPattern.REG8 }, OperandSizeMode.MODELESS, new byte[] { 0x20 }, new ModRM(0, 1));
            MODE32_TABLE.add("andw", new[] { OperandPattern.RM16, OperandPattern.REG16 }, OperandSizeMode.MODE16, new byte[] { 0x21 }, new ModRM(0, 1));
            MODE32_TABLE.add("andl", new[] { OperandPattern.RM32, OperandPattern.REG32 }, OperandSizeMode.MODE32, new byte[] { 0x21 }, new ModRM(0, 1));
            MODE32_TABLE.add("andb", new[] { OperandPattern.REG8, OperandPattern.RM8 }, OperandSizeMode.MODELESS, new byte[] { 0x22 }, new ModRM(1, 0));
            MODE32_TABLE.add("andw", new[] { OperandPattern.REG16, OperandPattern.RM16 }, OperandSizeMode.MODE16, new byte[] { 0x23 }, new ModRM(1, 0));
            MODE32_TABLE.add("andl", new[] { OperandPattern.REG32, OperandPattern.RM32 }, OperandSizeMode.MODE32, new byte[] { 0x23 }, new ModRM(1, 0));
            MODE32_TABLE.add("andb", new[] { OperandPattern.AL, OperandPattern.IMM8 }, OperandSizeMode.MODELESS, new byte[] { 0x24 });
            MODE32_TABLE.add("andw", new[] { OperandPattern.AX, OperandPattern.IMM16 }, OperandSizeMode.MODE16, new byte[] { 0x25 });
            MODE32_TABLE.add("andl", new[] { OperandPattern.EAX, OperandPattern.IMM32 }, OperandSizeMode.MODE32, new byte[] { 0x25 });
            MODE32_TABLE.add("daa", new OperandPattern[0], OperandSizeMode.MODELESS, new byte[] { 0x27 });
            MODE32_TABLE.add("subb", new[] { OperandPattern.RM8, OperandPattern.REG8 }, OperandSizeMode.MODELESS, new byte[] { 0x28 }, new ModRM(0, 1));
            MODE32_TABLE.add("subw", new[] { OperandPattern.RM16, OperandPattern.REG16 }, OperandSizeMode.MODE16, new byte[] { 0x29 }, new ModRM(0, 1));
            MODE32_TABLE.add("subl", new[] { OperandPattern.RM32, OperandPattern.REG32 }, OperandSizeMode.MODE32, new byte[] { 0x29 }, new ModRM(0, 1));
            MODE32_TABLE.add("subb", new[] { OperandPattern.REG8, OperandPattern.RM8 }, OperandSizeMode.MODELESS, new byte[] { 0x2A }, new ModRM(1, 0));
            MODE32_TABLE.add("subw", new[] { OperandPattern.REG16, OperandPattern.RM16 }, OperandSizeMode.MODE16, new byte[] { 0x2B }, new ModRM(1, 0));
            MODE32_TABLE.add("subl", new[] { OperandPattern.REG32, OperandPattern.RM32 }, OperandSizeMode.MODE32, new byte[] { 0x2B }, new ModRM(1, 0));
            MODE32_TABLE.add("subb", new[] { OperandPattern.AL, OperandPattern.IMM8 }, OperandSizeMode.MODELESS, new byte[] { 0x2C });
            MODE32_TABLE.add("subw", new[] { OperandPattern.AX, OperandPattern.IMM16 }, OperandSizeMode.MODE16, new byte[] { 0x2D });
            MODE32_TABLE.add("subl", new[] { OperandPattern.EAX, OperandPattern.IMM32 }, OperandSizeMode.MODE32, new byte[] { 0x2D });
            MODE32_TABLE.add("das", new OperandPattern[0], OperandSizeMode.MODELESS, new byte[] { 0x2F });
            MODE32_TABLE.add("xorb", new[] { OperandPattern.RM8, OperandPattern.REG8 }, OperandSizeMode.MODELESS, new byte[] { 0x30 }, new ModRM(0, 1));
            MODE32_TABLE.add("xorw", new[] { OperandPattern.RM16, OperandPattern.REG16 }, OperandSizeMode.MODE16, new byte[] { 0x31 }, new ModRM(0, 1));
            MODE32_TABLE.add("xorl", new[] { OperandPattern.RM32, OperandPattern.REG32 }, OperandSizeMode.MODE32, new byte[] { 0x31 }, new ModRM(0, 1));
            MODE32_TABLE.add("xorb", new[] { OperandPattern.REG8, OperandPattern.RM8 }, OperandSizeMode.MODELESS, new byte[] { 0x32 }, new ModRM(1, 0));
            MODE32_TABLE.add("xorw", new[] { OperandPattern.REG16, OperandPattern.RM16 }, OperandSizeMode.MODE16, new byte[] { 0x33 }, new ModRM(1, 0));
            MODE32_TABLE.add("xorl", new[] { OperandPattern.REG32, OperandPattern.RM32 }, OperandSizeMode.MODE32, new byte[] { 0x33 }, new ModRM(1, 0));
            MODE32_TABLE.add("xorb", new[] { OperandPattern.AL, OperandPattern.IMM8 }, OperandSizeMode.MODELESS, new byte[] { 0x34 });
            MODE32_TABLE.add("xorw", new[] { OperandPattern.AX, OperandPattern.IMM16 }, OperandSizeMode.MODE16, new byte[] { 0x35 });
            MODE32_TABLE.add("xorl", new[] { OperandPattern.EAX, OperandPattern.IMM32 }, OperandSizeMode.MODE32, new byte[] { 0x35 });
            MODE32_TABLE.add("aaa", new OperandPattern[0], OperandSizeMode.MODELESS, new byte[] { 0x37 });
            MODE32_TABLE.add("cmpb", new[] { OperandPattern.RM8, OperandPattern.REG8 }, OperandSizeMode.MODELESS, new byte[] { 0x38 }, new ModRM(0, 1));
            MODE32_TABLE.add("cmpw", new[] { OperandPattern.RM16, OperandPattern.REG16 }, OperandSizeMode.MODE16, new byte[] { 0x39 }, new ModRM(0, 1));
            MODE32_TABLE.add("cmpl", new[] { OperandPattern.RM32, OperandPattern.REG32 }, OperandSizeMode.MODE32, new byte[] { 0x39 }, new ModRM(0, 1));
            MODE32_TABLE.add("cmpb", new[] { OperandPattern.REG8, OperandPattern.RM8 }, OperandSizeMode.MODELESS, new byte[] { 0x3A }, new ModRM(1, 0));
            MODE32_TABLE.add("cmpw", new[] { OperandPattern.REG16, OperandPattern.RM16 }, OperandSizeMode.MODE16, new byte[] { 0x3B }, new ModRM(1, 0));
            MODE32_TABLE.add("cmpl", new[] { OperandPattern.REG32, OperandPattern.RM32 }, OperandSizeMode.MODE32, new byte[] { 0x3B }, new ModRM(1, 0));
            MODE32_TABLE.add("cmpb", new[] { OperandPattern.AL, OperandPattern.IMM8 }, OperandSizeMode.MODELESS, new byte[] { 0x3C });
            MODE32_TABLE.add("cmpw", new[] { OperandPattern.AX, OperandPattern.IMM16 }, OperandSizeMode.MODE16, new byte[] { 0x3D });
            MODE32_TABLE.add("cmpl", new[] { OperandPattern.EAX, OperandPattern.IMM32 }, OperandSizeMode.MODE32, new byte[] { 0x3D });
            MODE32_TABLE.add("aas", new OperandPattern[0], OperandSizeMode.MODELESS, new byte[] { 0x3F });
            MODE32_TABLE.add("incw", new[] { OperandPattern.REG16 }, OperandSizeMode.MODE16, new byte[] { 0x40 }, new RegisterInOpcode(0));
            MODE32_TABLE.add("incl", new[] { OperandPattern.REG32 }, OperandSizeMode.MODE32, new byte[] { 0x40 }, new RegisterInOpcode(0));
            MODE32_TABLE.add("decw", new[] { OperandPattern.REG16 }, OperandSizeMode.MODE16, new byte[] { 0x48 }, new RegisterInOpcode(0));
            MODE32_TABLE.add("decl", new[] { OperandPattern.REG32 }, OperandSizeMode.MODE32, new byte[] { 0x48 }, new RegisterInOpcode(0));
            MODE32_TABLE.add("pushw", new[] { OperandPattern.REG16 }, OperandSizeMode.MODE16, new byte[] { 0x50 }, new RegisterInOpcode(0));
            MODE32_TABLE.add("pushl", new[] { OperandPattern.REG32 }, OperandSizeMode.MODE32, new byte[] { 0x50 }, new RegisterInOpcode(0));
            MODE32_TABLE.add("popw", new[] { OperandPattern.REG16 }, OperandSizeMode.MODE16, new byte[] { 0x58 }, new RegisterInOpcode(0));
            MODE32_TABLE.add("popl", new[] { OperandPattern.REG32 }, OperandSizeMode.MODE32, new byte[] { 0x58 }, new RegisterInOpcode(0));
            MODE32_TABLE.add("pushaw", new OperandPattern[0], OperandSizeMode.MODE16, new byte[] { 0x60 });
            MODE32_TABLE.add("pushal", new OperandPattern[0], OperandSizeMode.MODE32, new byte[] { 0x60 });
            MODE32_TABLE.add("popaw", new OperandPattern[0], OperandSizeMode.MODE16, new byte[] { 0x61 });
            MODE32_TABLE.add("popal", new OperandPattern[0], OperandSizeMode.MODE32, new byte[] { 0x61 });
            MODE32_TABLE.add("boundw", new[] { OperandPattern.REG16, OperandPattern.MEM }, OperandSizeMode.MODE16, new byte[] { 0x62 }, new ModRM(1, 0));
            MODE32_TABLE.add("boundl", new[] { OperandPattern.REG32, OperandPattern.MEM }, OperandSizeMode.MODE32, new byte[] { 0x62 }, new ModRM(1, 0));
            MODE32_TABLE.add("arpl", new[] { OperandPattern.RM16, OperandPattern.REG16 }, OperandSizeMode.MODELESS, new byte[] { 0x63 }, new ModRM(0, 1));
            MODE32_TABLE.add("pushw", new[] { OperandPattern.IMM16 }, OperandSizeMode.MODE16, new byte[] { 0x68 });
            MODE32_TABLE.add("pushl", new[] { OperandPattern.IMM32 }, OperandSizeMode.MODE32, new byte[] { 0x68 });
            MODE32_TABLE.add("imulw", new[] { OperandPattern.REG16, OperandPattern.RM16, OperandPattern.IMM16 }, OperandSizeMode.MODE16, new byte[] { 0x69 }, new ModRM(1, 0));
            MODE32_TABLE.add("imull", new[] { OperandPattern.REG32, OperandPattern.RM32, OperandPattern.IMM32 }, OperandSizeMode.MODE32, new byte[] { 0x69 }, new ModRM(1, 0));
            MODE32_TABLE.add("pushb", new[] { OperandPattern.IMM8 }, OperandSizeMode.MODELESS, new byte[] { 0x6A });
            MODE32_TABLE.add("imulwb", new[] { OperandPattern.REG16, OperandPattern.RM16, OperandPattern.IMM8 }, OperandSizeMode.MODE16, new byte[] { 0x6B }, new ModRM(1, 0));
            MODE32_TABLE.add("imullb", new[] { OperandPattern.REG32, OperandPattern.RM32, OperandPattern.IMM8 }, OperandSizeMode.MODE32, new byte[] { 0x6B }, new ModRM(1, 0));
            MODE32_TABLE.add("insb", new OperandPattern[0], OperandSizeMode.MODELESS, new byte[] { 0x6C });
            MODE32_TABLE.add("insw", new OperandPattern[0], OperandSizeMode.MODE16, new byte[] { 0x6D });
            MODE32_TABLE.add("insl", new OperandPattern[0], OperandSizeMode.MODE32, new byte[] { 0x6D });
            MODE32_TABLE.add("outsb", new OperandPattern[0], OperandSizeMode.MODELESS, new byte[] { 0x6E });
            MODE32_TABLE.add("outsw", new OperandPattern[0], OperandSizeMode.MODE16, new byte[] { 0x6F });
            MODE32_TABLE.add("outsl", new OperandPattern[0], OperandSizeMode.MODE32, new byte[] { 0x6F });
            MODE32_TABLE.add("jo", new[] { OperandPattern.REL8 }, OperandSizeMode.MODELESS, new byte[] { 0x70 });
            MODE32_TABLE.add("jno", new[] { OperandPattern.REL8 }, OperandSizeMode.MODELESS, new byte[] { 0x71 });
            MODE32_TABLE.add("jb|jnae|jc", new[] { OperandPattern.REL8 }, OperandSizeMode.MODELESS, new byte[] { 0x72 });
            MODE32_TABLE.add("jnb|jae|jnc", new[] { OperandPattern.REL8 }, OperandSizeMode.MODELESS, new byte[] { 0x73 });
            MODE32_TABLE.add("jz|je", new[] { OperandPattern.REL8 }, OperandSizeMode.MODELESS, new byte[] { 0x74 });
            MODE32_TABLE.add("jnz|jne", new[] { OperandPattern.REL8 }, OperandSizeMode.MODELESS, new byte[] { 0x75 });
            MODE32_TABLE.add("jbe|jna", new[] { OperandPattern.REL8 }, OperandSizeMode.MODELESS, new byte[] { 0x76 });
            MODE32_TABLE.add("jnbe|ja", new[] { OperandPattern.REL8 }, OperandSizeMode.MODELESS, new byte[] { 0x77 });
            MODE32_TABLE.add("js", new[] { OperandPattern.REL8 }, OperandSizeMode.MODELESS, new byte[] { 0x78 });
            MODE32_TABLE.add("jns", new[] { OperandPattern.REL8 }, OperandSizeMode.MODELESS, new byte[] { 0x79 });
            MODE32_TABLE.add("jp|jpe", new[] { OperandPattern.REL8 }, OperandSizeMode.MODELESS, new byte[] { 0x7A });
            MODE32_TABLE.add("jnp|jpo", new[] { OperandPattern.REL8 }, OperandSizeMode.MODELESS, new byte[] { 0x7B });
            MODE32_TABLE.add("jl|jnge", new[] { OperandPattern.REL8 }, OperandSizeMode.MODELESS, new byte[] { 0x7C });
            MODE32_TABLE.add("jnl|jge", new[] { OperandPattern.REL8 }, OperandSizeMode.MODELESS, new byte[] { 0x7D });
            MODE32_TABLE.add("jle|jng", new[] { OperandPattern.REL8 }, OperandSizeMode.MODELESS, new byte[] { 0x7E });
            MODE32_TABLE.add("jnle|jg", new[] { OperandPattern.REL8 }, OperandSizeMode.MODELESS, new byte[] { 0x7F });
            MODE32_TABLE.add("addb", new[] { OperandPattern.RM8, OperandPattern.IMM8 }, OperandSizeMode.MODELESS, new byte[] { 0x80 }, new ModRM(0, 10));
            MODE32_TABLE.add("orb", new[] { OperandPattern.RM8, OperandPattern.IMM8 }, OperandSizeMode.MODELESS, new byte[] { 0x80 }, new ModRM(0, 11));
            MODE32_TABLE.add("adcb", new[] { OperandPattern.RM8, OperandPattern.IMM8 }, OperandSizeMode.MODELESS, new byte[] { 0x80 }, new ModRM(0, 12));
            MODE32_TABLE.add("sbbb", new[] { OperandPattern.RM8, OperandPattern.IMM8 }, OperandSizeMode.MODELESS, new byte[] { 0x80 }, new ModRM(0, 13));
            MODE32_TABLE.add("andb", new[] { OperandPattern.RM8, OperandPattern.IMM8 }, OperandSizeMode.MODELESS, new byte[] { 0x80 }, new ModRM(0, 14));
            MODE32_TABLE.add("subb", new[] { OperandPattern.RM8, OperandPattern.IMM8 }, OperandSizeMode.MODELESS, new byte[] { 0x80 }, new ModRM(0, 15));
            MODE32_TABLE.add("xorb", new[] { OperandPattern.RM8, OperandPattern.IMM8 }, OperandSizeMode.MODELESS, new byte[] { 0x80 }, new ModRM(0, 16));
            MODE32_TABLE.add("cmpb", new[] { OperandPattern.RM8, OperandPattern.IMM8 }, OperandSizeMode.MODELESS, new byte[] { 0x80 }, new ModRM(0, 17));
            MODE32_TABLE.add("addw", new[] { OperandPattern.RM16, OperandPattern.IMM16 }, OperandSizeMode.MODE16, new byte[] { 0x81 }, new ModRM(0, 10));
            MODE32_TABLE.add("orw", new[] { OperandPattern.RM16, OperandPattern.IMM16 }, OperandSizeMode.MODE16, new byte[] { 0x81 }, new ModRM(0, 11));
            MODE32_TABLE.add("adcw", new[] { OperandPattern.RM16, OperandPattern.IMM16 }, OperandSizeMode.MODE16, new byte[] { 0x81 }, new ModRM(0, 12));
            MODE32_TABLE.add("sbbw", new[] { OperandPattern.RM16, OperandPattern.IMM16 }, OperandSizeMode.MODE16, new byte[] { 0x81 }, new ModRM(0, 13));
            MODE32_TABLE.add("andw", new[] { OperandPattern.RM16, OperandPattern.IMM16 }, OperandSizeMode.MODE16, new byte[] { 0x81 }, new ModRM(0, 14));
            MODE32_TABLE.add("subw", new[] { OperandPattern.RM16, OperandPattern.IMM16 }, OperandSizeMode.MODE16, new byte[] { 0x81 }, new ModRM(0, 15));
            MODE32_TABLE.add("xorw", new[] { OperandPattern.RM16, OperandPattern.IMM16 }, OperandSizeMode.MODE16, new byte[] { 0x81 }, new ModRM(0, 16));
            MODE32_TABLE.add("cmpw", new[] { OperandPattern.RM16, OperandPattern.IMM16 }, OperandSizeMode.MODE16, new byte[] { 0x81 }, new ModRM(0, 17));
            MODE32_TABLE.add("addl", new[] { OperandPattern.RM32, OperandPattern.IMM32 }, OperandSizeMode.MODE32, new byte[] { 0x81 }, new ModRM(0, 10));
            MODE32_TABLE.add("orl", new[] { OperandPattern.RM32, OperandPattern.IMM32 }, OperandSizeMode.MODE32, new byte[] { 0x81 }, new ModRM(0, 11));
            MODE32_TABLE.add("adcl", new[] { OperandPattern.RM32, OperandPattern.IMM32 }, OperandSizeMode.MODE32, new byte[] { 0x81 }, new ModRM(0, 12));
            MODE32_TABLE.add("sbbl", new[] { OperandPattern.RM32, OperandPattern.IMM32 }, OperandSizeMode.MODE32, new byte[] { 0x81 }, new ModRM(0, 13));
            MODE32_TABLE.add("andl", new[] { OperandPattern.RM32, OperandPattern.IMM32 }, OperandSizeMode.MODE32, new byte[] { 0x81 }, new ModRM(0, 14));
            MODE32_TABLE.add("subl", new[] { OperandPattern.RM32, OperandPattern.IMM32 }, OperandSizeMode.MODE32, new byte[] { 0x81 }, new ModRM(0, 15));
            MODE32_TABLE.add("xorl", new[] { OperandPattern.RM32, OperandPattern.IMM32 }, OperandSizeMode.MODE32, new byte[] { 0x81 }, new ModRM(0, 16));
            MODE32_TABLE.add("cmpl", new[] { OperandPattern.RM32, OperandPattern.IMM32 }, OperandSizeMode.MODE32, new byte[] { 0x81 }, new ModRM(0, 17));
            MODE32_TABLE.add("addwb", new[] { OperandPattern.RM16, OperandPattern.IMM8S }, OperandSizeMode.MODE16, new byte[] { 0x83 }, new ModRM(0, 10));
            MODE32_TABLE.add("orwb", new[] { OperandPattern.RM16, OperandPattern.IMM8S }, OperandSizeMode.MODE16, new byte[] { 0x83 }, new ModRM(0, 11));
            MODE32_TABLE.add("adcwb", new[] { OperandPattern.RM16, OperandPattern.IMM8S }, OperandSizeMode.MODE16, new byte[] { 0x83 }, new ModRM(0, 12));
            MODE32_TABLE.add("sbbwb", new[] { OperandPattern.RM16, OperandPattern.IMM8S }, OperandSizeMode.MODE16, new byte[] { 0x83 }, new ModRM(0, 13));
            MODE32_TABLE.add("andwb", new[] { OperandPattern.RM16, OperandPattern.IMM8S }, OperandSizeMode.MODE16, new byte[] { 0x83 }, new ModRM(0, 14));
            MODE32_TABLE.add("subwb", new[] { OperandPattern.RM16, OperandPattern.IMM8S }, OperandSizeMode.MODE16, new byte[] { 0x83 }, new ModRM(0, 15));
            MODE32_TABLE.add("xorwb", new[] { OperandPattern.RM16, OperandPattern.IMM8S }, OperandSizeMode.MODE16, new byte[] { 0x83 }, new ModRM(0, 16));
            MODE32_TABLE.add("cmpwb", new[] { OperandPattern.RM16, OperandPattern.IMM8S }, OperandSizeMode.MODE16, new byte[] { 0x83 }, new ModRM(0, 17));
            MODE32_TABLE.add("addlb", new[] { OperandPattern.RM32, OperandPattern.IMM8S }, OperandSizeMode.MODE32, new byte[] { 0x83 }, new ModRM(0, 10));
            MODE32_TABLE.add("orlb", new[] { OperandPattern.RM32, OperandPattern.IMM8S }, OperandSizeMode.MODE32, new byte[] { 0x83 }, new ModRM(0, 11));
            MODE32_TABLE.add("adclb", new[] { OperandPattern.RM32, OperandPattern.IMM8S }, OperandSizeMode.MODE32, new byte[] { 0x83 }, new ModRM(0, 12));
            MODE32_TABLE.add("sbblb", new[] { OperandPattern.RM32, OperandPattern.IMM8S }, OperandSizeMode.MODE32, new byte[] { 0x83 }, new ModRM(0, 13));
            MODE32_TABLE.add("andlb", new[] { OperandPattern.RM32, OperandPattern.IMM8S }, OperandSizeMode.MODE32, new byte[] { 0x83 }, new ModRM(0, 14));
            MODE32_TABLE.add("sublb", new[] { OperandPattern.RM32, OperandPattern.IMM8S }, OperandSizeMode.MODE32, new byte[] { 0x83 }, new ModRM(0, 15));
            MODE32_TABLE.add("xorlb", new[] { OperandPattern.RM32, OperandPattern.IMM8S }, OperandSizeMode.MODE32, new byte[] { 0x83 }, new ModRM(0, 16));
            MODE32_TABLE.add("cmplb", new[] { OperandPattern.RM32, OperandPattern.IMM8S }, OperandSizeMode.MODE32, new byte[] { 0x83 }, new ModRM(0, 17));
            MODE32_TABLE.add("testb", new[] { OperandPattern.RM8, OperandPattern.REG8 }, OperandSizeMode.MODELESS, new byte[] { 0x84 }, new ModRM(0, 1));
            MODE32_TABLE.add("testw", new[] { OperandPattern.RM16, OperandPattern.REG16 }, OperandSizeMode.MODE16, new byte[] { 0x85 }, new ModRM(0, 1));
            MODE32_TABLE.add("testl", new[] { OperandPattern.RM32, OperandPattern.REG32 }, OperandSizeMode.MODE32, new byte[] { 0x85 }, new ModRM(0, 1));
            MODE32_TABLE.add("xchgb", new[] { OperandPattern.RM8, OperandPattern.REG8 }, OperandSizeMode.MODELESS, new byte[] { 0x86 }, new ModRM(0, 1));
            MODE32_TABLE.add("xchgw", new[] { OperandPattern.RM16, OperandPattern.REG16 }, OperandSizeMode.MODE16, new byte[] { 0x87 }, new ModRM(0, 1));
            MODE32_TABLE.add("xchgl", new[] { OperandPattern.RM32, OperandPattern.REG32 }, OperandSizeMode.MODE32, new byte[] { 0x87 }, new ModRM(0, 1));
            MODE32_TABLE.add("movb", new[] { OperandPattern.RM8, OperandPattern.REG8 }, OperandSizeMode.MODELESS, new byte[] { 0x88 }, new ModRM(0, 1));
            MODE32_TABLE.add("movw", new[] { OperandPattern.RM16, OperandPattern.REG16 }, OperandSizeMode.MODE16, new byte[] { 0x89 }, new ModRM(0, 1));
            MODE32_TABLE.add("movl", new[] { OperandPattern.RM32, OperandPattern.REG32 }, OperandSizeMode.MODE32, new byte[] { 0x89 }, new ModRM(0, 1));
            MODE32_TABLE.add("movb", new[] { OperandPattern.REG8, OperandPattern.RM8 }, OperandSizeMode.MODELESS, new byte[] { 0x8A }, new ModRM(1, 0));
            MODE32_TABLE.add("movw", new[] { OperandPattern.REG16, OperandPattern.RM16 }, OperandSizeMode.MODE16, new byte[] { 0x8B }, new ModRM(1, 0));
            MODE32_TABLE.add("movl", new[] { OperandPattern.REG32, OperandPattern.RM32 }, OperandSizeMode.MODE32, new byte[] { 0x8B }, new ModRM(1, 0));
            MODE32_TABLE.add("movw", new[] { OperandPattern.RM16, OperandPattern.SREG }, OperandSizeMode.MODE16, new byte[] { 0x8C }, new ModRM(0, 1));
            MODE32_TABLE.add("movlw", new[] { OperandPattern.RM32, OperandPattern.SREG }, OperandSizeMode.MODE32, new byte[] { 0x8C }, new ModRM(0, 1));
            MODE32_TABLE.add("leaw", new[] { OperandPattern.REG16, OperandPattern.MEM }, OperandSizeMode.MODE16, new byte[] { 0x8D }, new ModRM(1, 0));
            MODE32_TABLE.add("leal", new[] { OperandPattern.REG32, OperandPattern.MEM }, OperandSizeMode.MODE32, new byte[] { 0x8D }, new ModRM(1, 0));
            MODE32_TABLE.add("movw", new[] { OperandPattern.SREG, OperandPattern.RM16 }, OperandSizeMode.MODE16, new byte[] { 0x8E }, new ModRM(1, 0));
            MODE32_TABLE.add("movwl", new[] { OperandPattern.SREG, OperandPattern.RM32 }, OperandSizeMode.MODE32, new byte[] { 0x8E }, new ModRM(1, 0));
            MODE32_TABLE.add("popw", new[] { OperandPattern.RM16 }, OperandSizeMode.MODE16, new byte[] { 0x8F }, new ModRM(0, 10));
            MODE32_TABLE.add("popl", new[] { OperandPattern.RM32 }, OperandSizeMode.MODE32, new byte[] { 0x8F }, new ModRM(0, 10));
            MODE32_TABLE.add("nop", new OperandPattern[0], OperandSizeMode.MODELESS, new byte[] { 0x90 });
            MODE32_TABLE.add("xchgw", new[] { OperandPattern.REG16, OperandPattern.AX }, OperandSizeMode.MODE16, new byte[] { 0x90 }, new RegisterInOpcode(0));
            MODE32_TABLE.add("xchgl", new[] { OperandPattern.REG32, OperandPattern.EAX }, OperandSizeMode.MODE32, new byte[] { 0x90 }, new RegisterInOpcode(0));
            MODE32_TABLE.add("cbw", new OperandPattern[0], OperandSizeMode.MODE16, new byte[] { 0x98 });
            MODE32_TABLE.add("cwde", new OperandPattern[0], OperandSizeMode.MODE32, new byte[] { 0x98 });
            MODE32_TABLE.add("cwd", new OperandPattern[0], OperandSizeMode.MODE16, new byte[] { 0x99 });
            MODE32_TABLE.add("cdq", new OperandPattern[0], OperandSizeMode.MODE32, new byte[] { 0x99 });
            MODE32_TABLE.add("pushfw", new OperandPattern[0], OperandSizeMode.MODE16, new byte[] { 0x9C });
            MODE32_TABLE.add("pushfl", new OperandPattern[0], OperandSizeMode.MODE32, new byte[] { 0x9C });
            MODE32_TABLE.add("popfw", new OperandPattern[0], OperandSizeMode.MODE16, new byte[] { 0x9D });
            MODE32_TABLE.add("popfl", new OperandPattern[0], OperandSizeMode.MODE32, new byte[] { 0x9D });
            MODE32_TABLE.add("sahf", new OperandPattern[0], OperandSizeMode.MODELESS, new byte[] { 0x9E });
            MODE32_TABLE.add("lahf", new OperandPattern[0], OperandSizeMode.MODELESS, new byte[] { 0x9F });
            MODE32_TABLE.add("movsb", new OperandPattern[0], OperandSizeMode.MODELESS, new byte[] { 0xA4 });
            MODE32_TABLE.add("movsw", new OperandPattern[0], OperandSizeMode.MODE16, new byte[] { 0xA5 });
            MODE32_TABLE.add("movsl", new OperandPattern[0], OperandSizeMode.MODE32, new byte[] { 0xA5 });
            MODE32_TABLE.add("cmpsb", new OperandPattern[0], OperandSizeMode.MODELESS, new byte[] { 0xA6 });
            MODE32_TABLE.add("cmpsw", new OperandPattern[0], OperandSizeMode.MODE16, new byte[] { 0xA7 });
            MODE32_TABLE.add("cmpsl", new OperandPattern[0], OperandSizeMode.MODE32, new byte[] { 0xA8 });
            MODE32_TABLE.add("testb", new[] { OperandPattern.AL, OperandPattern.IMM8 }, OperandSizeMode.MODELESS, new byte[] { 0xA8 });
            MODE32_TABLE.add("testw", new[] { OperandPattern.AX, OperandPattern.IMM16 }, OperandSizeMode.MODE16, new byte[] { 0xA9 });
            MODE32_TABLE.add("testl", new[] { OperandPattern.AX, OperandPattern.IMM32 }, OperandSizeMode.MODE32, new byte[] { 0xA9 });
            MODE32_TABLE.add("stosb", new OperandPattern[0], OperandSizeMode.MODELESS, new byte[] { 0xAA });
            MODE32_TABLE.add("stosw", new OperandPattern[0], OperandSizeMode.MODE16, new byte[] { 0xAB });
            MODE32_TABLE.add("stosl", new OperandPattern[0], OperandSizeMode.MODE32, new byte[] { 0xAB });
            MODE32_TABLE.add("lodsb", new OperandPattern[0], OperandSizeMode.MODELESS, new byte[] { 0xAC });
            MODE32_TABLE.add("lodsw", new OperandPattern[0], OperandSizeMode.MODE16, new byte[] { 0xAD });
            MODE32_TABLE.add("lodsl", new OperandPattern[0], OperandSizeMode.MODE32, new byte[] { 0xAD });
            MODE32_TABLE.add("scasb", new OperandPattern[0], OperandSizeMode.MODELESS, new byte[] { 0xAE });
            MODE32_TABLE.add("scasw", new OperandPattern[0], OperandSizeMode.MODE16, new byte[] { 0xAF });
            MODE32_TABLE.add("scasl", new OperandPattern[0], OperandSizeMode.MODE32, new byte[] { 0xAF });
            MODE32_TABLE.add("movb", new[] { OperandPattern.REG8, OperandPattern.IMM8 }, OperandSizeMode.MODELESS, new byte[] { 0xB0 }, new RegisterInOpcode(0));
            MODE32_TABLE.add("movw", new[] { OperandPattern.REG16, OperandPattern.IMM16 }, OperandSizeMode.MODE16, new byte[] { 0xB8 }, new RegisterInOpcode(0));
            MODE32_TABLE.add("movl", new[] { OperandPattern.REG32, OperandPattern.IMM32 }, OperandSizeMode.MODE32, new byte[] { 0xB8 }, new RegisterInOpcode(0));
            MODE32_TABLE.add("rolb", new[] { OperandPattern.RM8, OperandPattern.IMM8 }, OperandSizeMode.MODELESS, new byte[] { 0xC0 }, new ModRM(0, 10));
            MODE32_TABLE.add("rorb", new[] { OperandPattern.RM8, OperandPattern.IMM8 }, OperandSizeMode.MODELESS, new byte[] { 0xC0 }, new ModRM(0, 11));
            MODE32_TABLE.add("rclb", new[] { OperandPattern.RM8, OperandPattern.IMM8 }, OperandSizeMode.MODELESS, new byte[] { 0xC0 }, new ModRM(0, 12));
            MODE32_TABLE.add("rcrb", new[] { OperandPattern.RM8, OperandPattern.IMM8 }, OperandSizeMode.MODELESS, new byte[] { 0xC0 }, new ModRM(0, 13));
            MODE32_TABLE.add("shlb|salb", new[] { OperandPattern.RM8, OperandPattern.IMM8 }, OperandSizeMode.MODELESS, new byte[] { 0xC0 }, new ModRM(0, 14));
            MODE32_TABLE.add("shrb", new[] { OperandPattern.RM8, OperandPattern.IMM8 }, OperandSizeMode.MODELESS, new byte[] { 0xC0 }, new ModRM(0, 15));
            MODE32_TABLE.add("sarb", new[] { OperandPattern.RM8, OperandPattern.IMM8 }, OperandSizeMode.MODELESS, new byte[] { 0xC0 }, new ModRM(0, 17));
            MODE32_TABLE.add("rolw", new[] { OperandPattern.RM16, OperandPattern.IMM8 }, OperandSizeMode.MODE16, new byte[] { 0xC1 }, new ModRM(0, 10));
            MODE32_TABLE.add("rorw", new[] { OperandPattern.RM16, OperandPattern.IMM8 }, OperandSizeMode.MODE16, new byte[] { 0xC1 }, new ModRM(0, 11));
            MODE32_TABLE.add("rclw", new[] { OperandPattern.RM16, OperandPattern.IMM8 }, OperandSizeMode.MODE16, new byte[] { 0xC1 }, new ModRM(0, 12));
            MODE32_TABLE.add("rcrw", new[] { OperandPattern.RM16, OperandPattern.IMM8 }, OperandSizeMode.MODE16, new byte[] { 0xC1 }, new ModRM(0, 13));
            MODE32_TABLE.add("shlw|salw", new[] { OperandPattern.RM16, OperandPattern.IMM8 }, OperandSizeMode.MODE16, new byte[] { 0xC1 }, new ModRM(0, 14));
            MODE32_TABLE.add("shrw", new[] { OperandPattern.RM16, OperandPattern.IMM8 }, OperandSizeMode.MODE16, new byte[] { 0xC1 }, new ModRM(0, 15));
            MODE32_TABLE.add("sarw", new[] { OperandPattern.RM16, OperandPattern.IMM8 }, OperandSizeMode.MODE16, new byte[] { 0xC1 }, new ModRM(0, 17));
            MODE32_TABLE.add("roll", new[] { OperandPattern.RM32, OperandPattern.IMM8 }, OperandSizeMode.MODE32, new byte[] { 0xC1 }, new ModRM(0, 10));
            MODE32_TABLE.add("rorl", new[] { OperandPattern.RM32, OperandPattern.IMM8 }, OperandSizeMode.MODE32, new byte[] { 0xC1 }, new ModRM(0, 11));
            MODE32_TABLE.add("rcll", new[] { OperandPattern.RM32, OperandPattern.IMM8 }, OperandSizeMode.MODE32, new byte[] { 0xC1 }, new ModRM(0, 12));
            MODE32_TABLE.add("rcrl", new[] { OperandPattern.RM32, OperandPattern.IMM8 }, OperandSizeMode.MODE32, new byte[] { 0xC1 }, new ModRM(0, 13));
            MODE32_TABLE.add("shll|sall", new[] { OperandPattern.RM32, OperandPattern.IMM8 }, OperandSizeMode.MODE32, new byte[] { 0xC1 }, new ModRM(0, 14));
            MODE32_TABLE.add("shrl", new[] { OperandPattern.RM32, OperandPattern.IMM8 }, OperandSizeMode.MODE32, new byte[] { 0xC1 }, new ModRM(0, 15));
            MODE32_TABLE.add("sarl", new[] { OperandPattern.RM32, OperandPattern.IMM8 }, OperandSizeMode.MODE32, new byte[] { 0xC1 }, new ModRM(0, 17));
            MODE32_TABLE.add("ret", new[] { OperandPattern.IMM16 }, OperandSizeMode.MODELESS, new byte[] { 0xC2 });
            MODE32_TABLE.add("ret", new OperandPattern[0], OperandSizeMode.MODELESS, new byte[] { 0xC3 });
            MODE32_TABLE.add("lesw", new[] { OperandPattern.REG16, OperandPattern.MEM }, OperandSizeMode.MODE16, new byte[] { 0xC4 }, new ModRM(1, 0));
            MODE32_TABLE.add("lesl", new[] { OperandPattern.REG32, OperandPattern.MEM }, OperandSizeMode.MODE32, new byte[] { 0xC4 }, new ModRM(1, 0));
            MODE32_TABLE.add("ldsw", new[] { OperandPattern.REG16, OperandPattern.MEM }, OperandSizeMode.MODE16, new byte[] { 0xC5 }, new ModRM(1, 0));
            MODE32_TABLE.add("ldsl", new[] { OperandPattern.REG32, OperandPattern.MEM }, OperandSizeMode.MODE32, new byte[] { 0xC5 }, new ModRM(1, 0));
            MODE32_TABLE.add("movb", new[] { OperandPattern.RM8, OperandPattern.IMM8 }, OperandSizeMode.MODELESS, new byte[] { 0xC6 }, new ModRM(0, 10));
            MODE32_TABLE.add("movw", new[] { OperandPattern.RM16, OperandPattern.IMM16 }, OperandSizeMode.MODE16, new byte[] { 0xC7 }, new ModRM(0, 10));
            MODE32_TABLE.add("movl", new[] { OperandPattern.RM32, OperandPattern.IMM32 }, OperandSizeMode.MODE32, new byte[] { 0xC7 }, new ModRM(0, 10));
            MODE32_TABLE.add("enter", new[] { OperandPattern.IMM16, OperandPattern.IMM8 }, OperandSizeMode.MODELESS, new byte[] { 0xC8 });
            MODE32_TABLE.add("leave", new OperandPattern[0], OperandSizeMode.MODELESS, new byte[] { 0xC9 });
            MODE32_TABLE.add("lret", new[] { OperandPattern.IMM16 }, OperandSizeMode.MODELESS, new byte[] { 0xCA });
            MODE32_TABLE.add("lret", new OperandPattern[0], OperandSizeMode.MODELESS, new byte[] { 0xCB });
            MODE32_TABLE.add("int", new[] { OperandPattern.IMM_VAL_3 }, OperandSizeMode.MODELESS, new byte[] { 0xCC });
            MODE32_TABLE.add("int", new[] { OperandPattern.IMM8 }, OperandSizeMode.MODELESS, new byte[] { 0xCD });
            MODE32_TABLE.add("into", new OperandPattern[0], OperandSizeMode.MODELESS, new byte[] { 0xCE });
            MODE32_TABLE.add("iretw", new OperandPattern[0], OperandSizeMode.MODE16, new byte[] { 0xCF });
            MODE32_TABLE.add("iretl", new OperandPattern[0], OperandSizeMode.MODE32, new byte[] { 0xCF });
            MODE32_TABLE.add("rolb", new[] { OperandPattern.RM8, OperandPattern.IMM_VAL_1 }, OperandSizeMode.MODELESS, new byte[] { 0xD0 }, new ModRM(0, 10));
            MODE32_TABLE.add("rorb", new[] { OperandPattern.RM8, OperandPattern.IMM_VAL_1 }, OperandSizeMode.MODELESS, new byte[] { 0xD0 }, new ModRM(0, 11));
            MODE32_TABLE.add("rclb", new[] { OperandPattern.RM8, OperandPattern.IMM_VAL_1 }, OperandSizeMode.MODELESS, new byte[] { 0xD0 }, new ModRM(0, 12));
            MODE32_TABLE.add("rcrb", new[] { OperandPattern.RM8, OperandPattern.IMM_VAL_1 }, OperandSizeMode.MODELESS, new byte[] { 0xD0 }, new ModRM(0, 13));
            MODE32_TABLE.add("shlb|salb", new[] { OperandPattern.RM8, OperandPattern.IMM_VAL_1 }, OperandSizeMode.MODELESS, new byte[] { 0xD0 }, new ModRM(0, 14));
            MODE32_TABLE.add("shrb", new[] { OperandPattern.RM8, OperandPattern.IMM_VAL_1 }, OperandSizeMode.MODELESS, new byte[] { 0xD0 }, new ModRM(0, 15));
            MODE32_TABLE.add("sarb", new[] { OperandPattern.RM8, OperandPattern.IMM_VAL_1 }, OperandSizeMode.MODELESS, new byte[] { 0xD0 }, new ModRM(0, 17));
            MODE32_TABLE.add("rolw", new[] { OperandPattern.RM16, OperandPattern.IMM_VAL_1 }, OperandSizeMode.MODE16, new byte[] { 0xD1 }, new ModRM(0, 10));
            MODE32_TABLE.add("rorw", new[] { OperandPattern.RM16, OperandPattern.IMM_VAL_1 }, OperandSizeMode.MODE16, new byte[] { 0xD1 }, new ModRM(0, 11));
            MODE32_TABLE.add("rclw", new[] { OperandPattern.RM16, OperandPattern.IMM_VAL_1 }, OperandSizeMode.MODE16, new byte[] { 0xD1 }, new ModRM(0, 12));
            MODE32_TABLE.add("rcrw", new[] { OperandPattern.RM16, OperandPattern.IMM_VAL_1 }, OperandSizeMode.MODE16, new byte[] { 0xD1 }, new ModRM(0, 13));
            MODE32_TABLE.add("shlw|shlw", new[] { OperandPattern.RM16, OperandPattern.IMM_VAL_1 }, OperandSizeMode.MODE16, new byte[] { 0xD1 }, new ModRM(0, 14));
            MODE32_TABLE.add("shrw", new[] { OperandPattern.RM16, OperandPattern.IMM_VAL_1 }, OperandSizeMode.MODE16, new byte[] { 0xD1 }, new ModRM(0, 15));
            MODE32_TABLE.add("sarw", new[] { OperandPattern.RM16, OperandPattern.IMM_VAL_1 }, OperandSizeMode.MODE16, new byte[] { 0xD1 }, new ModRM(0, 17));
            MODE32_TABLE.add("roll", new[] { OperandPattern.RM32, OperandPattern.IMM_VAL_1 }, OperandSizeMode.MODE32, new byte[] { 0xD1 }, new ModRM(0, 10));
            MODE32_TABLE.add("rorl", new[] { OperandPattern.RM32, OperandPattern.IMM_VAL_1 }, OperandSizeMode.MODE32, new byte[] { 0xD1 }, new ModRM(0, 11));
            MODE32_TABLE.add("rcll", new[] { OperandPattern.RM32, OperandPattern.IMM_VAL_1 }, OperandSizeMode.MODE32, new byte[] { 0xD1 }, new ModRM(0, 12));
            MODE32_TABLE.add("rcrl", new[] { OperandPattern.RM32, OperandPattern.IMM_VAL_1 }, OperandSizeMode.MODE32, new byte[] { 0xD1 }, new ModRM(0, 13));
            MODE32_TABLE.add("shll|sall", new[] { OperandPattern.RM32, OperandPattern.IMM_VAL_1 }, OperandSizeMode.MODE32, new byte[] { 0xD1 }, new ModRM(0, 14));
            MODE32_TABLE.add("shrl", new[] { OperandPattern.RM32, OperandPattern.IMM_VAL_1 }, OperandSizeMode.MODE32, new byte[] { 0xD1 }, new ModRM(0, 15));
            MODE32_TABLE.add("sarl", new[] { OperandPattern.RM32, OperandPattern.IMM_VAL_1 }, OperandSizeMode.MODE32, new byte[] { 0xD1 }, new ModRM(0, 17));
            MODE32_TABLE.add("rolb", new[] { OperandPattern.RM8, OperandPattern.CL }, OperandSizeMode.MODELESS, new byte[] { 0xD2 }, new ModRM(0, 10));
            MODE32_TABLE.add("rorb", new[] { OperandPattern.RM8, OperandPattern.CL }, OperandSizeMode.MODELESS, new byte[] { 0xD2 }, new ModRM(0, 11));
            MODE32_TABLE.add("rclb", new[] { OperandPattern.RM8, OperandPattern.CL }, OperandSizeMode.MODELESS, new byte[] { 0xD2 }, new ModRM(0, 12));
            MODE32_TABLE.add("rcrb", new[] { OperandPattern.RM8, OperandPattern.CL }, OperandSizeMode.MODELESS, new byte[] { 0xD2 }, new ModRM(0, 13));
            MODE32_TABLE.add("shlb|salb", new[] { OperandPattern.RM8, OperandPattern.CL }, OperandSizeMode.MODELESS, new byte[] { 0xD2 }, new ModRM(0, 14));
            MODE32_TABLE.add("shrb", new[] { OperandPattern.RM8, OperandPattern.CL }, OperandSizeMode.MODELESS, new byte[] { 0xD2 }, new ModRM(0, 15));
            MODE32_TABLE.add("sarb", new[] { OperandPattern.RM8, OperandPattern.CL }, OperandSizeMode.MODELESS, new byte[] { 0xD2 }, new ModRM(0, 17));
            MODE32_TABLE.add("rolw", new[] { OperandPattern.RM16, OperandPattern.CL }, OperandSizeMode.MODE16, new byte[] { 0xD3 }, new ModRM(0, 10));
            MODE32_TABLE.add("rorw", new[] { OperandPattern.RM16, OperandPattern.CL }, OperandSizeMode.MODE16, new byte[] { 0xD3 }, new ModRM(0, 11));
            MODE32_TABLE.add("rclw", new[] { OperandPattern.RM16, OperandPattern.CL }, OperandSizeMode.MODE16, new byte[] { 0xD3 }, new ModRM(0, 12));
            MODE32_TABLE.add("rcrw", new[] { OperandPattern.RM16, OperandPattern.CL }, OperandSizeMode.MODE16, new byte[] { 0xD3 }, new ModRM(0, 13));
            MODE32_TABLE.add("shlw|salw", new[] { OperandPattern.RM16, OperandPattern.CL }, OperandSizeMode.MODE16, new byte[] { 0xD3 }, new ModRM(0, 14));
            MODE32_TABLE.add("shrw", new[] { OperandPattern.RM16, OperandPattern.CL }, OperandSizeMode.MODE16, new byte[] { 0xD3 }, new ModRM(0, 15));
            MODE32_TABLE.add("sarw", new[] { OperandPattern.RM16, OperandPattern.CL }, OperandSizeMode.MODE16, new byte[] { 0xD3 }, new ModRM(0, 17));
            MODE32_TABLE.add("roll", new[] { OperandPattern.RM32, OperandPattern.CL }, OperandSizeMode.MODE32, new byte[] { 0xD3 }, new ModRM(0, 10));
            MODE32_TABLE.add("rorl", new[] { OperandPattern.RM32, OperandPattern.CL }, OperandSizeMode.MODE32, new byte[] { 0xD3 }, new ModRM(0, 11));
            MODE32_TABLE.add("rcll", new[] { OperandPattern.RM32, OperandPattern.CL }, OperandSizeMode.MODE32, new byte[] { 0xD3 }, new ModRM(0, 12));
            MODE32_TABLE.add("rcrl", new[] { OperandPattern.RM32, OperandPattern.CL }, OperandSizeMode.MODE32, new byte[] { 0xD3 }, new ModRM(0, 13));
            MODE32_TABLE.add("shll|sall", new[] { OperandPattern.RM32, OperandPattern.CL }, OperandSizeMode.MODE32, new byte[] { 0xD3 }, new ModRM(0, 14));
            MODE32_TABLE.add("shrl", new[] { OperandPattern.RM32, OperandPattern.CL }, OperandSizeMode.MODE32, new byte[] { 0xD3 }, new ModRM(0, 15));
            MODE32_TABLE.add("sarl", new[] { OperandPattern.RM32, OperandPattern.CL }, OperandSizeMode.MODE32, new byte[] { 0xD3 }, new ModRM(0, 17));
            MODE32_TABLE.add("aam", new OperandPattern[0], OperandSizeMode.MODELESS, new byte[] { 0xD4, 0x0A });
            MODE32_TABLE.add("aad", new OperandPattern[0], OperandSizeMode.MODELESS, new byte[] { 0xD5, 0x0A });
            MODE32_TABLE.add("salc", new OperandPattern[0], OperandSizeMode.MODELESS, new byte[] { 0xD6 });
            MODE32_TABLE.add("setalc", new OperandPattern[0], OperandSizeMode.MODELESS, new byte[] { 0xD6 });
            MODE32_TABLE.add("xlat", new OperandPattern[0], OperandSizeMode.MODELESS, new byte[] { 0xD7 });
            MODE32_TABLE.add("loopnz|loopne", new[] { OperandPattern.REL8 }, OperandSizeMode.MODELESS, new byte[] { 0xE0 });
            MODE32_TABLE.add("loopz|loope", new[] { OperandPattern.REL8 }, OperandSizeMode.MODELESS, new byte[] { 0xE1 });
            MODE32_TABLE.add("loop", new[] { OperandPattern.REL8 }, OperandSizeMode.MODELESS, new byte[] { 0xE2 });
            MODE32_TABLE.add("jecxz", new[] { OperandPattern.REL8 }, OperandSizeMode.MODELESS, new byte[] { 0xE3 });
            MODE32_TABLE.add("inb", new[] { OperandPattern.AL, OperandPattern.IMM8 }, OperandSizeMode.MODELESS, new byte[] { 0xE4 });
            MODE32_TABLE.add("inw", new[] { OperandPattern.AX, OperandPattern.IMM8 }, OperandSizeMode.MODE16, new byte[] { 0xE5 });
            MODE32_TABLE.add("inl", new[] { OperandPattern.EAX, OperandPattern.IMM8 }, OperandSizeMode.MODE32, new byte[] { 0xE5 });
            MODE32_TABLE.add("outb", new[] { OperandPattern.IMM8, OperandPattern.AL }, OperandSizeMode.MODELESS, new byte[] { 0xE6 });
            MODE32_TABLE.add("outw", new[] { OperandPattern.IMM8, OperandPattern.AX }, OperandSizeMode.MODE16, new byte[] { 0xE7 });
            MODE32_TABLE.add("outl", new[] { OperandPattern.IMM8, OperandPattern.EAX }, OperandSizeMode.MODE32, new byte[] { 0xE7 });
            MODE32_TABLE.add("call", new[] { OperandPattern.REL16 }, OperandSizeMode.MODE16, new byte[] { 0xE8 });
            MODE32_TABLE.add("call", new[] { OperandPattern.REL32 }, OperandSizeMode.MODE32, new byte[] { 0xE8 });
            MODE32_TABLE.add("jmp", new[] { OperandPattern.REL16 }, OperandSizeMode.MODE16, new byte[] { 0xE9 });
            MODE32_TABLE.add("jmp", new[] { OperandPattern.REL32 }, OperandSizeMode.MODE32, new byte[] { 0xE9 });
            MODE32_TABLE.add("ljmpw", new[] { OperandPattern.MEM }, OperandSizeMode.MODE16, new byte[] { 0xE9 });
            MODE32_TABLE.add("ljmpl", new[] { OperandPattern.MEM }, OperandSizeMode.MODE32, new byte[] { 0xE9 });
            MODE32_TABLE.add("jmp", new[] { OperandPattern.REL8 }, OperandSizeMode.MODELESS, new byte[] { 0xEB });
            MODE32_TABLE.add("inb", new[] { OperandPattern.AL, OperandPattern.DX }, OperandSizeMode.MODELESS, new byte[] { 0xEC });
            MODE32_TABLE.add("inw", new[] { OperandPattern.AX, OperandPattern.DX }, OperandSizeMode.MODE16, new byte[] { 0xED });
            MODE32_TABLE.add("inl", new[] { OperandPattern.EAX, OperandPattern.DX }, OperandSizeMode.MODE32, new byte[] { 0xED });
            MODE32_TABLE.add("outb", new[] { OperandPattern.DX, OperandPattern.AL }, OperandSizeMode.MODELESS, new byte[] { 0xEE });
            MODE32_TABLE.add("outw", new[] { OperandPattern.DX, OperandPattern.AX }, OperandSizeMode.MODE16, new byte[] { 0xEF });
            MODE32_TABLE.add("outl", new[] { OperandPattern.DX, OperandPattern.EAX }, OperandSizeMode.MODE32, new byte[] { 0xEF });
            MODE32_TABLE.add("lock", new OperandPattern[0], OperandSizeMode.MODELESS, new byte[] { 0xF0 });
            MODE32_TABLE.add("repnz|repne", new OperandPattern[0], OperandSizeMode.MODELESS, new byte[] { 0xF2 });
            MODE32_TABLE.add("rep|repz|repe", new OperandPattern[0], OperandSizeMode.MODELESS, new byte[] { 0xF3 });
            MODE32_TABLE.add("hlt", new OperandPattern[0], OperandSizeMode.MODELESS, new byte[] { 0xF4 });
            MODE32_TABLE.add("cmc", new OperandPattern[0], OperandSizeMode.MODELESS, new byte[] { 0xF5 });
            MODE32_TABLE.add("testb", new[] { OperandPattern.RM8, OperandPattern.IMM8 }, OperandSizeMode.MODELESS, new byte[] { 0xF6 }, new ModRM(0, 10));
            MODE32_TABLE.add("notb", new[] { OperandPattern.RM8 }, OperandSizeMode.MODELESS, new byte[] { 0xF6 }, new ModRM(0, 12));
            MODE32_TABLE.add("negb", new[] { OperandPattern.RM8 }, OperandSizeMode.MODELESS, new byte[] { 0xF6 }, new ModRM(0, 13));
            MODE32_TABLE.add("mulb", new[] { OperandPattern.RM8 }, OperandSizeMode.MODELESS, new byte[] { 0xF6 }, new ModRM(0, 14));
            MODE32_TABLE.add("imulb", new[] { OperandPattern.RM8 }, OperandSizeMode.MODELESS, new byte[] { 0xF6 }, new ModRM(0, 15));
            MODE32_TABLE.add("divb", new[] { OperandPattern.RM8 }, OperandSizeMode.MODELESS, new byte[] { 0xF6 }, new ModRM(0, 16));
            MODE32_TABLE.add("idivb", new[] { OperandPattern.RM8 }, OperandSizeMode.MODELESS, new byte[] { 0xF6 }, new ModRM(0, 17));
            MODE32_TABLE.add("testw", new[] { OperandPattern.RM16, OperandPattern.IMM16 }, OperandSizeMode.MODE16, new byte[] { 0xF7 }, new ModRM(0, 10));
            MODE32_TABLE.add("notw", new[] { OperandPattern.RM16 }, OperandSizeMode.MODE16, new byte[] { 0xF7 }, new ModRM(0, 12));
            MODE32_TABLE.add("negw", new[] { OperandPattern.RM16 }, OperandSizeMode.MODE16, new byte[] { 0xF7 }, new ModRM(0, 13));
            MODE32_TABLE.add("mulw", new[] { OperandPattern.RM16 }, OperandSizeMode.MODE16, new byte[] { 0xF7 }, new ModRM(0, 14));
            MODE32_TABLE.add("imulw", new[] { OperandPattern.RM16 }, OperandSizeMode.MODE16, new byte[] { 0xF7 }, new ModRM(0, 15));
            MODE32_TABLE.add("divw", new[] { OperandPattern.RM16 }, OperandSizeMode.MODE16, new byte[] { 0xF7 }, new ModRM(0, 16));
            MODE32_TABLE.add("idivw", new[] { OperandPattern.RM16 }, OperandSizeMode.MODE16, new byte[] { 0xF7 }, new ModRM(0, 17));
            MODE32_TABLE.add("testl", new[] { OperandPattern.RM32, OperandPattern.IMM32 }, OperandSizeMode.MODE32, new byte[] { 0xF7 }, new ModRM(0, 10));
            MODE32_TABLE.add("notl", new[] { OperandPattern.RM32 }, OperandSizeMode.MODE32, new byte[] { 0xF7 }, new ModRM(0, 12));
            MODE32_TABLE.add("negl", new[] { OperandPattern.RM32 }, OperandSizeMode.MODE32, new byte[] { 0xF7 }, new ModRM(0, 13));
            MODE32_TABLE.add("mull", new[] { OperandPattern.RM32 }, OperandSizeMode.MODE32, new byte[] { 0xF7 }, new ModRM(0, 14));
            MODE32_TABLE.add("imull", new[] { OperandPattern.RM32 }, OperandSizeMode.MODE32, new byte[] { 0xF7 }, new ModRM(0, 15));
            MODE32_TABLE.add("divl", new[] { OperandPattern.RM32 }, OperandSizeMode.MODE32, new byte[] { 0xF7 }, new ModRM(0, 16));
            MODE32_TABLE.add("idivl", new[] { OperandPattern.RM32 }, OperandSizeMode.MODE32, new byte[] { 0xF7 }, new ModRM(0, 17));
            MODE32_TABLE.add("clc", new OperandPattern[0], OperandSizeMode.MODELESS, new byte[] { 0xF8 });
            MODE32_TABLE.add("stc", new OperandPattern[0], OperandSizeMode.MODELESS, new byte[] { 0xF9 });
            MODE32_TABLE.add("cli", new OperandPattern[0], OperandSizeMode.MODELESS, new byte[] { 0xFA });
            MODE32_TABLE.add("sti", new OperandPattern[0], OperandSizeMode.MODELESS, new byte[] { 0xFB });
            MODE32_TABLE.add("cld", new OperandPattern[0], OperandSizeMode.MODELESS, new byte[] { 0xFC });
            MODE32_TABLE.add("std", new OperandPattern[0], OperandSizeMode.MODELESS, new byte[] { 0xFD });
            MODE32_TABLE.add("incb", new[] { OperandPattern.RM8 }, OperandSizeMode.MODELESS, new byte[] { 0xFE }, new ModRM(0, 10));
            MODE32_TABLE.add("decb", new[] { OperandPattern.RM8 }, OperandSizeMode.MODELESS, new byte[] { 0xFE }, new ModRM(0, 11));
            MODE32_TABLE.add("incw", new[] { OperandPattern.RM16 }, OperandSizeMode.MODE16, new byte[] { 0xFF }, new ModRM(0, 10));
            MODE32_TABLE.add("decw", new[] { OperandPattern.RM16 }, OperandSizeMode.MODE16, new byte[] { 0xFF }, new ModRM(0, 11));
            MODE32_TABLE.add("callw", new[] { OperandPattern.RM16 }, OperandSizeMode.MODE16, new byte[] { 0xFF }, new ModRM(0, 12));
            MODE32_TABLE.add("lcallw", new[] { OperandPattern.MEM }, OperandSizeMode.MODE16, new byte[] { 0xFF }, new ModRM(0, 13));
            MODE32_TABLE.add("jmpw", new[] { OperandPattern.RM16 }, OperandSizeMode.MODE16, new byte[] { 0xFF }, new ModRM(0, 14));
            MODE32_TABLE.add("ljmpw", new[] { OperandPattern.MEM }, OperandSizeMode.MODE16, new byte[] { 0xFF }, new ModRM(0, 15));
            MODE32_TABLE.add("pushw", new[] { OperandPattern.RM16 }, OperandSizeMode.MODE16, new byte[] { 0xFF }, new ModRM(0, 16));
            MODE32_TABLE.add("incl", new[] { OperandPattern.RM32 }, OperandSizeMode.MODE32, new byte[] { 0xFF }, new ModRM(0, 10));
            MODE32_TABLE.add("decl", new[] { OperandPattern.RM32 }, OperandSizeMode.MODE32, new byte[] { 0xFF }, new ModRM(0, 11));
            MODE32_TABLE.add("calll", new[] { OperandPattern.RM32 }, OperandSizeMode.MODE32, new byte[] { 0xFF }, new ModRM(0, 12));
            MODE32_TABLE.add("lcalll", new[] { OperandPattern.MEM }, OperandSizeMode.MODE32, new byte[] { 0xFF }, new ModRM(0, 13));
            MODE32_TABLE.add("jmpl", new[] { OperandPattern.RM32 }, OperandSizeMode.MODE32, new byte[] { 0xFF }, new ModRM(0, 14));
            MODE32_TABLE.add("ljmpl", new[] { OperandPattern.MEM }, OperandSizeMode.MODE32, new byte[] { 0xFF }, new ModRM(0, 15));
            MODE32_TABLE.add("pushl", new[] { OperandPattern.RM32 }, OperandSizeMode.MODE32, new byte[] { 0xFF }, new ModRM(0, 16));
            MODE32_TABLE.add("invd", new OperandPattern[0], OperandSizeMode.MODELESS, new byte[] { 0x0F, 0x08 });
            MODE32_TABLE.add("wbinvd", new OperandPattern[0], OperandSizeMode.MODELESS, new byte[] { 0x0F, 0x09 });
            MODE32_TABLE.add("ud2", new OperandPattern[0], OperandSizeMode.MODELESS, new byte[] { 0x0F, 0x0B });
            MODE32_TABLE.add("sysenter", new OperandPattern[0], OperandSizeMode.MODELESS, new byte[] { 0x0F, 0x34 });
            MODE32_TABLE.add("sysexit", new OperandPattern[0], OperandSizeMode.MODELESS, new byte[] { 0x0F, 0x35 });
            MODE32_TABLE.add("jo", new[] { OperandPattern.REL16 }, OperandSizeMode.MODE16, new byte[] { 0x0F, 0x80 });
            MODE32_TABLE.add("jno", new[] { OperandPattern.REL16 }, OperandSizeMode.MODE16, new byte[] { 0x0F, 0x81 });
            MODE32_TABLE.add("jb|jnae|jc", new[] { OperandPattern.REL16 }, OperandSizeMode.MODE16, new byte[] { 0x0F, 0x82 });
            MODE32_TABLE.add("jnb|jae|jnc", new[] { OperandPattern.REL16 }, OperandSizeMode.MODE16, new byte[] { 0x0F, 0x83 });
            MODE32_TABLE.add("jz|je", new[] { OperandPattern.REL16 }, OperandSizeMode.MODE16, new byte[] { 0x0F, 0x84 });
            MODE32_TABLE.add("jnz|jne", new[] { OperandPattern.REL16 }, OperandSizeMode.MODE16, new byte[] { 0x0F, 0x85 });
            MODE32_TABLE.add("jbe|jna", new[] { OperandPattern.REL16 }, OperandSizeMode.MODE16, new byte[] { 0x0F, 0x86 });
            MODE32_TABLE.add("jnbe|ja", new[] { OperandPattern.REL16 }, OperandSizeMode.MODE16, new byte[] { 0x0F, 0x87 });
            MODE32_TABLE.add("js", new[] { OperandPattern.REL16 }, OperandSizeMode.MODE16, new byte[] { 0x0F, 0x88 });
            MODE32_TABLE.add("jns", new[] { OperandPattern.REL16 }, OperandSizeMode.MODE16, new byte[] { 0x0F, 0x89 });
            MODE32_TABLE.add("jp|jpe", new[] { OperandPattern.REL16 }, OperandSizeMode.MODE16, new byte[] { 0x0F, 0x8A });
            MODE32_TABLE.add("jnp|jpo", new[] { OperandPattern.REL16 }, OperandSizeMode.MODE16, new byte[] { 0x0F, 0x8B });
            MODE32_TABLE.add("jl|jnge", new[] { OperandPattern.REL16 }, OperandSizeMode.MODE16, new byte[] { 0x0F, 0x8C });
            MODE32_TABLE.add("jnl|jge", new[] { OperandPattern.REL16 }, OperandSizeMode.MODE16, new byte[] { 0x0F, 0x8D });
            MODE32_TABLE.add("jle|jng", new[] { OperandPattern.REL16 }, OperandSizeMode.MODE16, new byte[] { 0x0F, 0x8E });
            MODE32_TABLE.add("jnle|jg", new[] { OperandPattern.REL16 }, OperandSizeMode.MODE16, new byte[] { 0x0F, 0x8F });
            MODE32_TABLE.add("jo", new[] { OperandPattern.REL32 }, OperandSizeMode.MODE32, new byte[] { 0x0F, 0x80 });
            MODE32_TABLE.add("jno", new[] { OperandPattern.REL32 }, OperandSizeMode.MODE32, new byte[] { 0x0F, 0x81 });
            MODE32_TABLE.add("jb|jnae|jc", new[] { OperandPattern.REL32 }, OperandSizeMode.MODE32, new byte[] { 0x0F, 0x82 });
            MODE32_TABLE.add("jnb|jae|jnc", new[] { OperandPattern.REL32 }, OperandSizeMode.MODE32, new byte[] { 0x0F, 0x83 });
            MODE32_TABLE.add("jz|je", new[] { OperandPattern.REL32 }, OperandSizeMode.MODE32, new byte[] { 0x0F, 0x84 });
            MODE32_TABLE.add("jnz|jne", new[] { OperandPattern.REL32 }, OperandSizeMode.MODE32, new byte[] { 0x0F, 0x85 });
            MODE32_TABLE.add("jbe|jna", new[] { OperandPattern.REL32 }, OperandSizeMode.MODE32, new byte[] { 0x0F, 0x86 });
            MODE32_TABLE.add("jnbe|ja", new[] { OperandPattern.REL32 }, OperandSizeMode.MODE32, new byte[] { 0x0F, 0x87 });
            MODE32_TABLE.add("js", new[] { OperandPattern.REL32 }, OperandSizeMode.MODE32, new byte[] { 0x0F, 0x88 });
            MODE32_TABLE.add("jns", new[] { OperandPattern.REL32 }, OperandSizeMode.MODE32, new byte[] { 0x0F, 0x89 });
            MODE32_TABLE.add("jp|jpe", new[] { OperandPattern.REL32 }, OperandSizeMode.MODE32, new byte[] { 0x0F, 0x8A });
            MODE32_TABLE.add("jnp|jpo", new[] { OperandPattern.REL32 }, OperandSizeMode.MODE32, new byte[] { 0x0F, 0x8B });
            MODE32_TABLE.add("jl|jnge", new[] { OperandPattern.REL32 }, OperandSizeMode.MODE32, new byte[] { 0x0F, 0x8C });
            MODE32_TABLE.add("jnl|jge", new[] { OperandPattern.REL32 }, OperandSizeMode.MODE32, new byte[] { 0x0F, 0x8D });
            MODE32_TABLE.add("jle|jng", new[] { OperandPattern.REL32 }, OperandSizeMode.MODE32, new byte[] { 0x0F, 0x8E });
            MODE32_TABLE.add("jnle|jg", new[] { OperandPattern.REL32 }, OperandSizeMode.MODE32, new byte[] { 0x0F, 0x8F });
            MODE32_TABLE.add("pushw", new[] { OperandPattern.FS }, OperandSizeMode.MODELESS, new byte[] { 0x0F, 0xA0 });
            MODE32_TABLE.add("popw", new[] { OperandPattern.FS }, OperandSizeMode.MODELESS, new byte[] { 0x0F, 0xA1 });
            MODE32_TABLE.add("cpuid", new OperandPattern[0], OperandSizeMode.MODELESS, new byte[] { 0x0F, 0xA2 });
            MODE32_TABLE.add("btw", new[] { OperandPattern.RM16, OperandPattern.REG16 }, OperandSizeMode.MODE16, new byte[] { 0x0F, 0xA3 }, new ModRM(0, 1));
            MODE32_TABLE.add("btl", new[] { OperandPattern.RM32, OperandPattern.REG32 }, OperandSizeMode.MODE32, new byte[] { 0x0F, 0xA3 }, new ModRM(0, 1));
            MODE32_TABLE.add("shldw", new[] { OperandPattern.RM16, OperandPattern.REG16, OperandPattern.IMM8 }, OperandSizeMode.MODE16, new byte[] { 0x0F, 0xA4 }, new ModRM(0, 1));
            MODE32_TABLE.add("shldl", new[] { OperandPattern.RM32, OperandPattern.REG32, OperandPattern.IMM8 }, OperandSizeMode.MODE32, new byte[] { 0x0F, 0xA4 }, new ModRM(0, 1));
            MODE32_TABLE.add("shldw", new[] { OperandPattern.RM16, OperandPattern.REG16, OperandPattern.CL }, OperandSizeMode.MODE16, new byte[] { 0x0F, 0xA5 }, new ModRM(0, 1));
            MODE32_TABLE.add("shldl", new[] { OperandPattern.RM32, OperandPattern.REG32, OperandPattern.CL }, OperandSizeMode.MODE32, new byte[] { 0x0F, 0xA5 }, new ModRM(0, 1));
            MODE32_TABLE.add("pushw", new[] { OperandPattern.GS }, OperandSizeMode.MODELESS, new byte[] { 0x0F, 0xA8 });
            MODE32_TABLE.add("popw", new[] { OperandPattern.GS }, OperandSizeMode.MODELESS, new byte[] { 0x0F, 0xA9 });
            MODE32_TABLE.add("btsw", new[] { OperandPattern.RM16, OperandPattern.REG16 }, OperandSizeMode.MODE16, new byte[] { 0x0F, 0xAB }, new ModRM(0, 1));
            MODE32_TABLE.add("btsl", new[] { OperandPattern.RM32, OperandPattern.REG32 }, OperandSizeMode.MODE32, new byte[] { 0x0F, 0xAB }, new ModRM(0, 1));
            MODE32_TABLE.add("shrdw", new[] { OperandPattern.RM16, OperandPattern.REG16, OperandPattern.IMM8 }, OperandSizeMode.MODE16, new byte[] { 0x0F, 0xAC }, new ModRM(0, 1));
            MODE32_TABLE.add("shrdl", new[] { OperandPattern.RM32, OperandPattern.REG32, OperandPattern.IMM8 }, OperandSizeMode.MODE32, new byte[] { 0x0F, 0xAC }, new ModRM(0, 1));
            MODE32_TABLE.add("shrdw", new[] { OperandPattern.RM16, OperandPattern.REG16, OperandPattern.CL }, OperandSizeMode.MODE16, new byte[] { 0x0F, 0xAD }, new ModRM(0, 1));
            MODE32_TABLE.add("shrdl", new[] { OperandPattern.RM32, OperandPattern.REG32, OperandPattern.CL }, OperandSizeMode.MODE32, new byte[] { 0x0F, 0xAD }, new ModRM(0, 1));
            MODE32_TABLE.add("imulw", new[] { OperandPattern.REG16, OperandPattern.RM16 }, OperandSizeMode.MODE16, new byte[] { 0x0F, 0xAF }, new ModRM(1, 0));
            MODE32_TABLE.add("imull", new[] { OperandPattern.REG32, OperandPattern.RM32 }, OperandSizeMode.MODE32, new byte[] { 0x0F, 0xAF }, new ModRM(1, 0));
            MODE32_TABLE.add("cmpxchgb", new[] { OperandPattern.RM8, OperandPattern.REG8 }, OperandSizeMode.MODELESS, new byte[] { 0x0F, 0xB0 }, new ModRM(0, 1));
            MODE32_TABLE.add("cmpxchgw", new[] { OperandPattern.RM16, OperandPattern.REG16 }, OperandSizeMode.MODE16, new byte[] { 0x0F, 0xB1 }, new ModRM(0, 1));
            MODE32_TABLE.add("cmpxchgl", new[] { OperandPattern.RM32, OperandPattern.REG32 }, OperandSizeMode.MODE32, new byte[] { 0x0F, 0xB1 }, new ModRM(0, 1));
            MODE32_TABLE.add("lssw", new[] { OperandPattern.REG16, OperandPattern.MEM }, OperandSizeMode.MODE16, new byte[] { 0x0F, 0xB2 }, new ModRM(1, 0));
            MODE32_TABLE.add("lssl", new[] { OperandPattern.REG32, OperandPattern.MEM }, OperandSizeMode.MODE32, new byte[] { 0x0F, 0xB2 }, new ModRM(1, 0));
            MODE32_TABLE.add("btrw", new[] { OperandPattern.RM16, OperandPattern.REG16 }, OperandSizeMode.MODE16, new byte[] { 0x0F, 0xB3 }, new ModRM(0, 1));
            MODE32_TABLE.add("btrl", new[] { OperandPattern.RM32, OperandPattern.REG32 }, OperandSizeMode.MODE32, new byte[] { 0x0F, 0xB3 }, new ModRM(0, 1));
            MODE32_TABLE.add("lfsw", new[] { OperandPattern.REG16, OperandPattern.MEM }, OperandSizeMode.MODE16, new byte[] { 0x0F, 0xB4 }, new ModRM(1, 0));
            MODE32_TABLE.add("lfsl", new[] { OperandPattern.REG32, OperandPattern.MEM }, OperandSizeMode.MODE32, new byte[] { 0x0F, 0xB4 }, new ModRM(1, 0));
            MODE32_TABLE.add("lgsw", new[] { OperandPattern.REG16, OperandPattern.MEM }, OperandSizeMode.MODE16, new byte[] { 0x0F, 0xB5 }, new ModRM(1, 0));
            MODE32_TABLE.add("lgsl", new[] { OperandPattern.REG32, OperandPattern.MEM }, OperandSizeMode.MODE32, new byte[] { 0x0F, 0xB5 }, new ModRM(1, 0));
            MODE32_TABLE.add("movzxwb", new[] { OperandPattern.REG16, OperandPattern.RM8 }, OperandSizeMode.MODE16, new byte[] { 0x0F, 0xB6 }, new ModRM(1, 0));
            MODE32_TABLE.add("movzxlb", new[] { OperandPattern.REG32, OperandPattern.RM8 }, OperandSizeMode.MODE32, new byte[] { 0x0F, 0xB6 }, new ModRM(1, 0));
            MODE32_TABLE.add("movzxww", new[] { OperandPattern.REG16, OperandPattern.RM16 }, OperandSizeMode.MODE16, new byte[] { 0x0F, 0xB7 }, new ModRM(1, 0));
            MODE32_TABLE.add("movzxlw", new[] { OperandPattern.REG32, OperandPattern.RM16 }, OperandSizeMode.MODE32, new byte[] { 0x0F, 0xB7 }, new ModRM(1, 0));
            MODE32_TABLE.add("btw", new[] { OperandPattern.RM16, OperandPattern.IMM8 }, OperandSizeMode.MODE16, new byte[] { 0x0F, 0xBA }, new ModRM(0, 14));
            MODE32_TABLE.add("btl", new[] { OperandPattern.RM32, OperandPattern.IMM8 }, OperandSizeMode.MODE32, new byte[] { 0x0F, 0xBA }, new ModRM(0, 14));
            MODE32_TABLE.add("btsw", new[] { OperandPattern.RM16, OperandPattern.IMM8 }, OperandSizeMode.MODE16, new byte[] { 0x0F, 0xBA }, new ModRM(0, 15));
            MODE32_TABLE.add("btsl", new[] { OperandPattern.RM32, OperandPattern.IMM8 }, OperandSizeMode.MODE32, new byte[] { 0x0F, 0xBA }, new ModRM(0, 15));
            MODE32_TABLE.add("btrw", new[] { OperandPattern.RM16, OperandPattern.IMM8 }, OperandSizeMode.MODE16, new byte[] { 0x0F, 0xBA }, new ModRM(0, 16));
            MODE32_TABLE.add("btrl", new[] { OperandPattern.RM32, OperandPattern.IMM8 }, OperandSizeMode.MODE32, new byte[] { 0x0F, 0xBA }, new ModRM(0, 16));
            MODE32_TABLE.add("btcw", new[] { OperandPattern.RM16, OperandPattern.IMM8 }, OperandSizeMode.MODE16, new byte[] { 0x0F, 0xBA }, new ModRM(0, 17));
            MODE32_TABLE.add("btcl", new[] { OperandPattern.RM32, OperandPattern.IMM8 }, OperandSizeMode.MODE32, new byte[] { 0x0F, 0xBA }, new ModRM(0, 17));
            MODE32_TABLE.add("btcw", new[] { OperandPattern.RM16, OperandPattern.REG16 }, OperandSizeMode.MODE16, new byte[] { 0x0F, 0xBB }, new ModRM(0, 1));
            MODE32_TABLE.add("btcl", new[] { OperandPattern.RM32, OperandPattern.REG32 }, OperandSizeMode.MODE32, new byte[] { 0x0F, 0xBB }, new ModRM(0, 1));
            MODE32_TABLE.add("bsfw", new[] { OperandPattern.REG16, OperandPattern.RM16 }, OperandSizeMode.MODE16, new byte[] { 0x0F, 0xBC }, new ModRM(1, 0));
            MODE32_TABLE.add("bsfl", new[] { OperandPattern.REG32, OperandPattern.RM32 }, OperandSizeMode.MODE32, new byte[] { 0x0F, 0xBC }, new ModRM(1, 0));
            MODE32_TABLE.add("bsrw", new[] { OperandPattern.REG16, OperandPattern.RM16 }, OperandSizeMode.MODE16, new byte[] { 0x0F, 0xBD }, new ModRM(1, 0));
            MODE32_TABLE.add("bsrl", new[] { OperandPattern.REG32, OperandPattern.RM32 }, OperandSizeMode.MODE32, new byte[] { 0x0F, 0xBD }, new ModRM(1, 0));
            MODE32_TABLE.add("movsxwb", new[] { OperandPattern.REG16, OperandPattern.RM8 }, OperandSizeMode.MODE16, new byte[] { 0x0F, 0xBE }, new ModRM(1, 0));
            MODE32_TABLE.add("movsxlb", new[] { OperandPattern.REG32, OperandPattern.RM8 }, OperandSizeMode.MODE32, new byte[] { 0x0F, 0xBE }, new ModRM(1, 0));
            MODE32_TABLE.add("movsxww", new[] { OperandPattern.REG16, OperandPattern.RM16 }, OperandSizeMode.MODE16, new byte[] { 0x0F, 0xBF }, new ModRM(1, 0));
            MODE32_TABLE.add("movsxlw", new[] { OperandPattern.REG32, OperandPattern.RM16 }, OperandSizeMode.MODE32, new byte[] { 0x0F, 0xBF }, new ModRM(1, 0));
            MODE32_TABLE.add("xaddb", new[] { OperandPattern.RM8, OperandPattern.REG8 }, OperandSizeMode.MODELESS, new byte[] { 0x0F, 0xC0 }, new ModRM(0, 1));
            MODE32_TABLE.add("xaddw", new[] { OperandPattern.RM16, OperandPattern.REG16 }, OperandSizeMode.MODE16, new byte[] { 0x0F, 0xC1 }, new ModRM(0, 1));
            MODE32_TABLE.add("xaddl", new[] { OperandPattern.RM32, OperandPattern.REG32 }, OperandSizeMode.MODE32, new byte[] { 0x0F, 0xC1 }, new ModRM(0, 1));
            MODE32_TABLE.add("cmpxchg8b", new[] { OperandPattern.MEM }, OperandSizeMode.MODELESS, new byte[] { 0x0F, 0xC7 }, new ModRM(0, 11));
            MODE32_TABLE.add("bswapw", new[] { OperandPattern.REG16 }, OperandSizeMode.MODE16, new byte[] { 0x0F, 0xC8 }, new RegisterInOpcode(0));
            MODE32_TABLE.add("bswapl", new[] { OperandPattern.REG32 }, OperandSizeMode.MODE32, new byte[] { 0x0F, 0xC8 }, new RegisterInOpcode(0));

        }

        /// <summary>
        /// 命令パターン集合
        /// </summary>
        private HashSet<InstructionPattern> patterns;

        /// <summary>
        /// ニーモニックと対応する命令パターン集合の表
        /// </summary>
        private IDictionary<string, HashSet<InstructionPattern>> patternsByMnemonic;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        private InstructionPatternTable() {
            patterns = new HashSet<InstructionPattern>();
            patternsByMnemonic = new Dictionary<string, HashSet<InstructionPattern>>();
        }

        /// <summary>
        /// 命令パターンを追加 
        /// </summary>
        /// <param name="pat">追加したい命令パターン</param>
        private void add(InstructionPattern pat) {
            if (pat == null) {
                throw new ArgumentNullException();
            }
            patterns.Add(pat);

            if (!patternsByMnemonic.ContainsKey(pat.mnemonic)) {
                patternsByMnemonic[pat.mnemonic] = new HashSet<InstructionPattern>();
            }
            patternsByMnemonic[pat.mnemonic].Add(pat);
        }


        /// <summary>
        /// 命令パターンを追加
        /// </summary>
        /// <param name="mnemonics"> ニーモニック（|でニーモニック名を区切って複数パターン同時登録できる） </param>
        /// <param name="operands"> オペランドのパターン </param>
        /// <param name="operandSizeMode"> オペランドのサイズモード </param>
        /// <param name="opcodes"> 生成される機械語テンプレート </param>
        /// <param name="options"> 命令パターンに付属するオプション </param>
        private void add(string mnemonics, OperandPattern[] operands, OperandSizeMode operandSizeMode, byte[] opcodes, params InstructionOption[] options) {
            foreach (string mnemonic in mnemonics.Split('|')) {
                add(new InstructionPattern(mnemonic, operands, operandSizeMode, opcodes, options));
            }
        }

        /// <summary>
        /// 指定されたニーモニックとオペランド列にマッチする命令パターンを返す
        /// </summary>
        /// <param name="mnemonic"> ニーモニック </param>
        /// <param name="operands"> オペランド列 </param>
        /// <returns></returns>
        public InstructionPattern match(string mnemonic, IList<IOperand> operands) {
            if (mnemonic == null || operands == null) {
                throw new ArgumentNullException();
            }
            if (!patternsByMnemonic.ContainsKey(mnemonic)) {
                throw new System.ArgumentException("未登録のニーモニックです: " + mnemonic);
            }

            InstructionPattern bestmatch = null;
            foreach (InstructionPattern pat in patternsByMnemonic[mnemonic]) {
                if (matches(pat, operands) && (bestmatch == null || isBetterMatch(pat, bestmatch, operands))) {
                    bestmatch = pat;
                }
            }

            if (bestmatch != null) {
                return bestmatch;
            } else {
                throw new System.ArgumentException("どのパターンとも一致しません: " + mnemonic);
            }
        }

        /// <summary>
        /// 命令パターンのオペランド列と指定したオペランド列がマッチするか調べる
        /// </summary>
        /// <param name="pat"> 命令パターン </param>
        /// <param name="operands"> オペランド列 </param>
        /// <returns></returns>
        private static bool matches(InstructionPattern pat, IList<IOperand> operands) {
            if (pat.operands.Count != operands.Count) {
                return false;
            }
            for (int i = 0; i < pat.operands.Count && i < operands.Count; i++) {
                if (!pat.operands[i].matches(operands[i])) {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 指定したオペランド列に対して、命令パターンxがyより適切か調べる
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="operands"></param>
        /// <returns></returns>
        private static bool isBetterMatch(InstructionPattern x, InstructionPattern y, IList<IOperand> operands) {
            bool isbetter = false;
            bool isworse = false;

            for (int i = 0; i < operands.Count; i++) {
                if (x.operands[i] == OperandPattern.REL8 || x.operands[i] == OperandPattern.REL16 || x.operands[i] == OperandPattern.REL32) {
                    // オペランド幅がx>yならbetterを立てる
                    isbetter |= isWider(x.operands[i], y.operands[i]);
                    // オペランド幅がx<yならworseを立てる
                    isworse |= isWider(y.operands[i], x.operands[i]);
                }
            }
            // worseが立っておらず、betterが立っているときのみ x が y より良いマッチ結果と判断する
            return !isworse && isbetter;
        }


        /// <summary>
        /// 即値オペランドxのビット幅がyよりも大きいなら真
        /// </summary>
        /// <param name="op"></param>
        /// <returns></returns>
        private static bool isWider(OperandPattern x, OperandPattern y) {
            return getWidth(x) > getWidth(y);
        }

        /// <summary>
        /// 即値オペランドのビット幅を返す
        /// </summary>
        /// <param name="op"></param>
        /// <returns></returns>
        private static int getWidth(OperandPattern op) {
            if (op == null) {
                throw new ArgumentNullException();
            }
            if (op == OperandPattern.REL8) {
                return 8;
            } else if (op == OperandPattern.REL16) {
                return 16;
            } else if (op == OperandPattern.REL32) {
                return 32;
            } else {
                throw new System.ArgumentException("Not applicable to operand");
            }
        }

    }

}