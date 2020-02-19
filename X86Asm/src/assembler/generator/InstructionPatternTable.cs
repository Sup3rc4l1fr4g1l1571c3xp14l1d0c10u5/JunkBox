using System;
using System.Collections.Generic;

namespace X86Asm.generator
{

	// OperandPattern.*;
    // OperandSizeMode.*;


	using Operand = X86Asm.operand.Operand;


	/// <summary>
	/// A set of instruction patterns.
	/// </summary>
	public class InstructionPatternTable
	{

		/// <summary>
		/// The table of instructions for x86, 32-bit mode. </summary>
		public static readonly InstructionPatternTable MODE32_TABLE;

		static InstructionPatternTable()
		{
			MODE32_TABLE = new InstructionPatternTable();
			MODE32_TABLE.add("byte", opPat(OperandPattern.IMM8), OperandSizeMode.MODELESS, new int[]{});
			MODE32_TABLE.add("word", opPat(OperandPattern.IMM16), OperandSizeMode.MODELESS, new int[]{});
			MODE32_TABLE.add("dword", opPat(OperandPattern.IMM32), OperandSizeMode.MODELESS, new int[]{});
			MODE32_TABLE.add("addb", opPat(OperandPattern.RM8, OperandPattern.REG8), OperandSizeMode.MODELESS, new int[]{0x00}, new ModRM(0, 1));
			MODE32_TABLE.add("addw", opPat(OperandPattern.RM16, OperandPattern.REG16), OperandSizeMode.MODE16, new int[]{0x01}, new ModRM(0, 1));
			MODE32_TABLE.add("addl", opPat(OperandPattern.RM32, OperandPattern.REG32), OperandSizeMode.MODE32, new int[]{0x01}, new ModRM(0, 1));
			MODE32_TABLE.add("addb", opPat(OperandPattern.REG8, OperandPattern.RM8), OperandSizeMode.MODELESS, new int[]{0x02}, new ModRM(1, 0));
			MODE32_TABLE.add("addw", opPat(OperandPattern.REG16, OperandPattern.RM16), OperandSizeMode.MODE16, new int[]{0x03}, new ModRM(1, 0));
			MODE32_TABLE.add("addl", opPat(OperandPattern.REG32, OperandPattern.RM32), OperandSizeMode.MODE32, new int[]{0x03}, new ModRM(1, 0));
			MODE32_TABLE.add("addb", opPat(OperandPattern.AL, OperandPattern.IMM8), OperandSizeMode.MODELESS, new int[]{0x04});
			MODE32_TABLE.add("addw", opPat(OperandPattern.AX, OperandPattern.IMM16), OperandSizeMode.MODE16, new int[]{0x05});
			MODE32_TABLE.add("addl", opPat(OperandPattern.EAX, OperandPattern.IMM32), OperandSizeMode.MODE32, new int[]{0x05});
			MODE32_TABLE.add("pushw", opPat(OperandPattern.ES), OperandSizeMode.MODELESS, new int[]{0x06});
			MODE32_TABLE.add("popw", opPat(OperandPattern.ES), OperandSizeMode.MODELESS, new int[]{0x07});
			MODE32_TABLE.add("orb", opPat(OperandPattern.RM8, OperandPattern.REG8), OperandSizeMode.MODELESS, new int[]{0x08}, new ModRM(0, 1));
			MODE32_TABLE.add("orw", opPat(OperandPattern.RM16, OperandPattern.REG16), OperandSizeMode.MODE16, new int[]{0x09}, new ModRM(0, 1));
			MODE32_TABLE.add("orl", opPat(OperandPattern.RM32, OperandPattern.REG32), OperandSizeMode.MODE32, new int[]{0x09}, new ModRM(0, 1));
			MODE32_TABLE.add("orb", opPat(OperandPattern.REG8, OperandPattern.RM8), OperandSizeMode.MODELESS, new int[]{0x0A}, new ModRM(1, 0));
			MODE32_TABLE.add("orw", opPat(OperandPattern.REG16, OperandPattern.RM16), OperandSizeMode.MODE16, new int[]{0x0B}, new ModRM(1, 0));
			MODE32_TABLE.add("orl", opPat(OperandPattern.REG32, OperandPattern.RM32), OperandSizeMode.MODE32, new int[]{0x0B}, new ModRM(1, 0));
			MODE32_TABLE.add("orb", opPat(OperandPattern.AL, OperandPattern.IMM8), OperandSizeMode.MODELESS, new int[]{0x0C});
			MODE32_TABLE.add("orw", opPat(OperandPattern.AX, OperandPattern.IMM16), OperandSizeMode.MODE16, new int[]{0x0D});
			MODE32_TABLE.add("orl", opPat(OperandPattern.EAX, OperandPattern.IMM32), OperandSizeMode.MODE32, new int[]{0x0D});
			MODE32_TABLE.add("pushw", opPat(OperandPattern.CS), OperandSizeMode.MODELESS, new int[]{0x0E});
			MODE32_TABLE.add("adcb", opPat(OperandPattern.RM8, OperandPattern.REG8), OperandSizeMode.MODELESS, new int[]{0x10}, new ModRM(0, 1));
			MODE32_TABLE.add("adcw", opPat(OperandPattern.RM16, OperandPattern.REG16), OperandSizeMode.MODE16, new int[]{0x11}, new ModRM(0, 1));
			MODE32_TABLE.add("adcl", opPat(OperandPattern.RM32, OperandPattern.REG32), OperandSizeMode.MODE32, new int[]{0x11}, new ModRM(0, 1));
			MODE32_TABLE.add("adcb", opPat(OperandPattern.REG8, OperandPattern.RM8), OperandSizeMode.MODELESS, new int[]{0x12}, new ModRM(1, 0));
			MODE32_TABLE.add("adcw", opPat(OperandPattern.REG16, OperandPattern.RM16), OperandSizeMode.MODE16, new int[]{0x13}, new ModRM(1, 0));
			MODE32_TABLE.add("adcl", opPat(OperandPattern.REG32, OperandPattern.RM32), OperandSizeMode.MODE32, new int[]{0x13}, new ModRM(1, 0));
			MODE32_TABLE.add("adcb", opPat(OperandPattern.AL, OperandPattern.IMM8), OperandSizeMode.MODELESS, new int[]{0x14});
			MODE32_TABLE.add("adcw", opPat(OperandPattern.AX, OperandPattern.IMM16), OperandSizeMode.MODE16, new int[]{0x15});
			MODE32_TABLE.add("adcl", opPat(OperandPattern.EAX, OperandPattern.IMM32), OperandSizeMode.MODE32, new int[]{0x15});
			MODE32_TABLE.add("pushw", opPat(OperandPattern.SS), OperandSizeMode.MODELESS, new int[]{0x16});
			MODE32_TABLE.add("popw", opPat(OperandPattern.SS), OperandSizeMode.MODELESS, new int[]{0x17});
			MODE32_TABLE.add("sbbb", opPat(OperandPattern.RM8, OperandPattern.REG8), OperandSizeMode.MODELESS, new int[]{0x18}, new ModRM(0, 1));
			MODE32_TABLE.add("sbbw", opPat(OperandPattern.RM16, OperandPattern.REG16), OperandSizeMode.MODE16, new int[]{0x19}, new ModRM(0, 1));
			MODE32_TABLE.add("sbbl", opPat(OperandPattern.RM32, OperandPattern.REG32), OperandSizeMode.MODE32, new int[]{0x19}, new ModRM(0, 1));
			MODE32_TABLE.add("sbbb", opPat(OperandPattern.REG8, OperandPattern.RM8), OperandSizeMode.MODELESS, new int[]{0x1A}, new ModRM(1, 0));
			MODE32_TABLE.add("sbbw", opPat(OperandPattern.REG16, OperandPattern.RM16), OperandSizeMode.MODE16, new int[]{0x1B}, new ModRM(1, 0));
			MODE32_TABLE.add("sbbl", opPat(OperandPattern.REG32, OperandPattern.RM32), OperandSizeMode.MODE32, new int[]{0x1B}, new ModRM(1, 0));
			MODE32_TABLE.add("sbbb", opPat(OperandPattern.AL, OperandPattern.IMM8), OperandSizeMode.MODELESS, new int[]{0x1C});
			MODE32_TABLE.add("sbbw", opPat(OperandPattern.AX, OperandPattern.IMM16), OperandSizeMode.MODE16, new int[]{0x1D});
			MODE32_TABLE.add("sbbl", opPat(OperandPattern.EAX, OperandPattern.IMM32), OperandSizeMode.MODE32, new int[]{0x1D});
			MODE32_TABLE.add("pushw", opPat(OperandPattern.DS), OperandSizeMode.MODELESS, new int[]{0x1E});
			MODE32_TABLE.add("popw", opPat(OperandPattern.DS), OperandSizeMode.MODELESS, new int[]{0x1F});
			MODE32_TABLE.add("andb", opPat(OperandPattern.RM8, OperandPattern.REG8), OperandSizeMode.MODELESS, new int[]{0x20}, new ModRM(0, 1));
			MODE32_TABLE.add("andw", opPat(OperandPattern.RM16, OperandPattern.REG16), OperandSizeMode.MODE16, new int[]{0x21}, new ModRM(0, 1));
			MODE32_TABLE.add("andl", opPat(OperandPattern.RM32, OperandPattern.REG32), OperandSizeMode.MODE32, new int[]{0x21}, new ModRM(0, 1));
			MODE32_TABLE.add("andb", opPat(OperandPattern.REG8, OperandPattern.RM8), OperandSizeMode.MODELESS, new int[]{0x22}, new ModRM(1, 0));
			MODE32_TABLE.add("andw", opPat(OperandPattern.REG16, OperandPattern.RM16), OperandSizeMode.MODE16, new int[]{0x23}, new ModRM(1, 0));
			MODE32_TABLE.add("andl", opPat(OperandPattern.REG32, OperandPattern.RM32), OperandSizeMode.MODE32, new int[]{0x23}, new ModRM(1, 0));
			MODE32_TABLE.add("andb", opPat(OperandPattern.AL, OperandPattern.IMM8), OperandSizeMode.MODELESS, new int[]{0x24});
			MODE32_TABLE.add("andw", opPat(OperandPattern.AX, OperandPattern.IMM16), OperandSizeMode.MODE16, new int[]{0x25});
			MODE32_TABLE.add("andl", opPat(OperandPattern.EAX, OperandPattern.IMM32), OperandSizeMode.MODE32, new int[]{0x25});
			MODE32_TABLE.add("daa", opPat(), OperandSizeMode.MODELESS, new int[]{0x27});
			MODE32_TABLE.add("subb", opPat(OperandPattern.RM8, OperandPattern.REG8), OperandSizeMode.MODELESS, new int[]{0x28}, new ModRM(0, 1));
			MODE32_TABLE.add("subw", opPat(OperandPattern.RM16, OperandPattern.REG16), OperandSizeMode.MODE16, new int[]{0x29}, new ModRM(0, 1));
			MODE32_TABLE.add("subl", opPat(OperandPattern.RM32, OperandPattern.REG32), OperandSizeMode.MODE32, new int[]{0x29}, new ModRM(0, 1));
			MODE32_TABLE.add("subb", opPat(OperandPattern.REG8, OperandPattern.RM8), OperandSizeMode.MODELESS, new int[]{0x2A}, new ModRM(1, 0));
			MODE32_TABLE.add("subw", opPat(OperandPattern.REG16, OperandPattern.RM16), OperandSizeMode.MODE16, new int[]{0x2B}, new ModRM(1, 0));
			MODE32_TABLE.add("subl", opPat(OperandPattern.REG32, OperandPattern.RM32), OperandSizeMode.MODE32, new int[]{0x2B}, new ModRM(1, 0));
			MODE32_TABLE.add("subb", opPat(OperandPattern.AL, OperandPattern.IMM8), OperandSizeMode.MODELESS, new int[]{0x2C});
			MODE32_TABLE.add("subw", opPat(OperandPattern.AX, OperandPattern.IMM16), OperandSizeMode.MODE16, new int[]{0x2D});
			MODE32_TABLE.add("subl", opPat(OperandPattern.EAX, OperandPattern.IMM32), OperandSizeMode.MODE32, new int[]{0x2D});
			MODE32_TABLE.add("das", opPat(), OperandSizeMode.MODELESS, new int[]{0x2F});
			MODE32_TABLE.add("xorb", opPat(OperandPattern.RM8, OperandPattern.REG8), OperandSizeMode.MODELESS, new int[]{0x30}, new ModRM(0, 1));
			MODE32_TABLE.add("xorw", opPat(OperandPattern.RM16, OperandPattern.REG16), OperandSizeMode.MODE16, new int[]{0x31}, new ModRM(0, 1));
			MODE32_TABLE.add("xorl", opPat(OperandPattern.RM32, OperandPattern.REG32), OperandSizeMode.MODE32, new int[]{0x31}, new ModRM(0, 1));
			MODE32_TABLE.add("xorb", opPat(OperandPattern.REG8, OperandPattern.RM8), OperandSizeMode.MODELESS, new int[]{0x32}, new ModRM(1, 0));
			MODE32_TABLE.add("xorw", opPat(OperandPattern.REG16, OperandPattern.RM16), OperandSizeMode.MODE16, new int[]{0x33}, new ModRM(1, 0));
			MODE32_TABLE.add("xorl", opPat(OperandPattern.REG32, OperandPattern.RM32), OperandSizeMode.MODE32, new int[]{0x33}, new ModRM(1, 0));
			MODE32_TABLE.add("xorb", opPat(OperandPattern.AL, OperandPattern.IMM8), OperandSizeMode.MODELESS, new int[]{0x34});
			MODE32_TABLE.add("xorw", opPat(OperandPattern.AX, OperandPattern.IMM16), OperandSizeMode.MODE16, new int[]{0x35});
			MODE32_TABLE.add("xorl", opPat(OperandPattern.EAX, OperandPattern.IMM32), OperandSizeMode.MODE32, new int[]{0x35});
			MODE32_TABLE.add("aaa", opPat(), OperandSizeMode.MODELESS, new int[]{0x37});
			MODE32_TABLE.add("cmpb", opPat(OperandPattern.RM8, OperandPattern.REG8), OperandSizeMode.MODELESS, new int[]{0x38}, new ModRM(0, 1));
			MODE32_TABLE.add("cmpw", opPat(OperandPattern.RM16, OperandPattern.REG16), OperandSizeMode.MODE16, new int[]{0x39}, new ModRM(0, 1));
			MODE32_TABLE.add("cmpl", opPat(OperandPattern.RM32, OperandPattern.REG32), OperandSizeMode.MODE32, new int[]{0x39}, new ModRM(0, 1));
			MODE32_TABLE.add("cmpb", opPat(OperandPattern.REG8, OperandPattern.RM8), OperandSizeMode.MODELESS, new int[]{0x3A}, new ModRM(1, 0));
			MODE32_TABLE.add("cmpw", opPat(OperandPattern.REG16, OperandPattern.RM16), OperandSizeMode.MODE16, new int[]{0x3B}, new ModRM(1, 0));
			MODE32_TABLE.add("cmpl", opPat(OperandPattern.REG32, OperandPattern.RM32), OperandSizeMode.MODE32, new int[]{0x3B}, new ModRM(1, 0));
			MODE32_TABLE.add("cmpb", opPat(OperandPattern.AL, OperandPattern.IMM8), OperandSizeMode.MODELESS, new int[]{0x3C});
			MODE32_TABLE.add("cmpw", opPat(OperandPattern.AX, OperandPattern.IMM16), OperandSizeMode.MODE16, new int[]{0x3D});
			MODE32_TABLE.add("cmpl", opPat(OperandPattern.EAX, OperandPattern.IMM32), OperandSizeMode.MODE32, new int[]{0x3D});
			MODE32_TABLE.add("aas", opPat(), OperandSizeMode.MODELESS, new int[]{0x3F});
			MODE32_TABLE.add("incw", opPat(OperandPattern.REG16), OperandSizeMode.MODE16, new int[]{0x40}, new RegisterInOpcode(0));
			MODE32_TABLE.add("incl", opPat(OperandPattern.REG32), OperandSizeMode.MODE32, new int[]{0x40}, new RegisterInOpcode(0));
			MODE32_TABLE.add("decw", opPat(OperandPattern.REG16), OperandSizeMode.MODE16, new int[]{0x48}, new RegisterInOpcode(0));
			MODE32_TABLE.add("decl", opPat(OperandPattern.REG32), OperandSizeMode.MODE32, new int[]{0x48}, new RegisterInOpcode(0));
			MODE32_TABLE.add("pushw", opPat(OperandPattern.REG16), OperandSizeMode.MODE16, new int[]{0x50}, new RegisterInOpcode(0));
			MODE32_TABLE.add("pushl", opPat(OperandPattern.REG32), OperandSizeMode.MODE32, new int[]{0x50}, new RegisterInOpcode(0));
			MODE32_TABLE.add("popw", opPat(OperandPattern.REG16), OperandSizeMode.MODE16, new int[]{0x58}, new RegisterInOpcode(0));
			MODE32_TABLE.add("popl", opPat(OperandPattern.REG32), OperandSizeMode.MODE32, new int[]{0x58}, new RegisterInOpcode(0));
			MODE32_TABLE.add("pushaw", opPat(), OperandSizeMode.MODE16, new int[]{0x60});
			MODE32_TABLE.add("pushal", opPat(), OperandSizeMode.MODE32, new int[]{0x60});
			MODE32_TABLE.add("popaw", opPat(), OperandSizeMode.MODE16, new int[]{0x61});
			MODE32_TABLE.add("popal", opPat(), OperandSizeMode.MODE32, new int[]{0x61});
			MODE32_TABLE.add("boundw", opPat(OperandPattern.REG16, OperandPattern.MEM), OperandSizeMode.MODE16, new int[]{0x62}, new ModRM(1, 0));
			MODE32_TABLE.add("boundl", opPat(OperandPattern.REG32, OperandPattern.MEM), OperandSizeMode.MODE32, new int[]{0x62}, new ModRM(1, 0));
			MODE32_TABLE.add("arpl", opPat(OperandPattern.RM16, OperandPattern.REG16), OperandSizeMode.MODELESS, new int[]{0x63}, new ModRM(0, 1));
			MODE32_TABLE.add("pushw", opPat(OperandPattern.IMM16), OperandSizeMode.MODE16, new int[]{0x68});
			MODE32_TABLE.add("pushl", opPat(OperandPattern.IMM32), OperandSizeMode.MODE32, new int[]{0x68});
			MODE32_TABLE.add("imulw", opPat(OperandPattern.REG16, OperandPattern.RM16, OperandPattern.IMM16), OperandSizeMode.MODE16, new int[]{0x69}, new ModRM(1, 0));
			MODE32_TABLE.add("imull", opPat(OperandPattern.REG32, OperandPattern.RM32, OperandPattern.IMM32), OperandSizeMode.MODE32, new int[]{0x69}, new ModRM(1, 0));
			MODE32_TABLE.add("pushb", opPat(OperandPattern.IMM8), OperandSizeMode.MODELESS, new int[]{0x6A});
			MODE32_TABLE.add("imulwb", opPat(OperandPattern.REG16, OperandPattern.RM16, OperandPattern.IMM8), OperandSizeMode.MODE16, new int[]{0x6B}, new ModRM(1, 0));
			MODE32_TABLE.add("imullb", opPat(OperandPattern.REG32, OperandPattern.RM32, OperandPattern.IMM8), OperandSizeMode.MODE32, new int[]{0x6B}, new ModRM(1, 0));
			MODE32_TABLE.add("insb", opPat(), OperandSizeMode.MODELESS, new int[]{0x6C});
			MODE32_TABLE.add("insw", opPat(), OperandSizeMode.MODE16, new int[]{0x6D});
			MODE32_TABLE.add("insl", opPat(), OperandSizeMode.MODE32, new int[]{0x6D});
			MODE32_TABLE.add("outsb", opPat(), OperandSizeMode.MODELESS, new int[]{0x6E});
			MODE32_TABLE.add("outsw", opPat(), OperandSizeMode.MODE16, new int[]{0x6F});
			MODE32_TABLE.add("outsl", opPat(), OperandSizeMode.MODE32, new int[]{0x6F});
			MODE32_TABLE.add("jo", opPat(OperandPattern.REL8), OperandSizeMode.MODELESS, new int[]{0x70});
			MODE32_TABLE.add("jno", opPat(OperandPattern.REL8), OperandSizeMode.MODELESS, new int[]{0x71});
			MODE32_TABLE.add("jb|jnae|jc", opPat(OperandPattern.REL8), OperandSizeMode.MODELESS, new int[]{0x72});
			MODE32_TABLE.add("jnb|jae|jnc", opPat(OperandPattern.REL8), OperandSizeMode.MODELESS, new int[]{0x73});
			MODE32_TABLE.add("jz|je", opPat(OperandPattern.REL8), OperandSizeMode.MODELESS, new int[]{0x74});
			MODE32_TABLE.add("jnz|jne", opPat(OperandPattern.REL8), OperandSizeMode.MODELESS, new int[]{0x75});
			MODE32_TABLE.add("jbe|jna", opPat(OperandPattern.REL8), OperandSizeMode.MODELESS, new int[]{0x76});
			MODE32_TABLE.add("jnbe|ja", opPat(OperandPattern.REL8), OperandSizeMode.MODELESS, new int[]{0x77});
			MODE32_TABLE.add("js", opPat(OperandPattern.REL8), OperandSizeMode.MODELESS, new int[]{0x78});
			MODE32_TABLE.add("jns", opPat(OperandPattern.REL8), OperandSizeMode.MODELESS, new int[]{0x79});
			MODE32_TABLE.add("jp|jpe", opPat(OperandPattern.REL8), OperandSizeMode.MODELESS, new int[]{0x7A});
			MODE32_TABLE.add("jnp|jpo", opPat(OperandPattern.REL8), OperandSizeMode.MODELESS, new int[]{0x7B});
			MODE32_TABLE.add("jl|jnge", opPat(OperandPattern.REL8), OperandSizeMode.MODELESS, new int[]{0x7C});
			MODE32_TABLE.add("jnl|jge", opPat(OperandPattern.REL8), OperandSizeMode.MODELESS, new int[]{0x7D});
			MODE32_TABLE.add("jle|jng", opPat(OperandPattern.REL8), OperandSizeMode.MODELESS, new int[]{0x7E});
			MODE32_TABLE.add("jnle|jg", opPat(OperandPattern.REL8), OperandSizeMode.MODELESS, new int[]{0x7F});
			MODE32_TABLE.add("addb", opPat(OperandPattern.RM8, OperandPattern.IMM8), OperandSizeMode.MODELESS, new int[]{0x80}, new ModRM(0, 10));
			MODE32_TABLE.add("orb", opPat(OperandPattern.RM8, OperandPattern.IMM8), OperandSizeMode.MODELESS, new int[]{0x80}, new ModRM(0, 11));
			MODE32_TABLE.add("adcb", opPat(OperandPattern.RM8, OperandPattern.IMM8), OperandSizeMode.MODELESS, new int[]{0x80}, new ModRM(0, 12));
			MODE32_TABLE.add("sbbb", opPat(OperandPattern.RM8, OperandPattern.IMM8), OperandSizeMode.MODELESS, new int[]{0x80}, new ModRM(0, 13));
			MODE32_TABLE.add("andb", opPat(OperandPattern.RM8, OperandPattern.IMM8), OperandSizeMode.MODELESS, new int[]{0x80}, new ModRM(0, 14));
			MODE32_TABLE.add("subb", opPat(OperandPattern.RM8, OperandPattern.IMM8), OperandSizeMode.MODELESS, new int[]{0x80}, new ModRM(0, 15));
			MODE32_TABLE.add("xorb", opPat(OperandPattern.RM8, OperandPattern.IMM8), OperandSizeMode.MODELESS, new int[]{0x80}, new ModRM(0, 16));
			MODE32_TABLE.add("cmpb", opPat(OperandPattern.RM8, OperandPattern.IMM8), OperandSizeMode.MODELESS, new int[]{0x80}, new ModRM(0, 17));
			MODE32_TABLE.add("addw", opPat(OperandPattern.RM16, OperandPattern.IMM16), OperandSizeMode.MODE16, new int[]{0x81}, new ModRM(0, 10));
			MODE32_TABLE.add("orw", opPat(OperandPattern.RM16, OperandPattern.IMM16), OperandSizeMode.MODE16, new int[]{0x81}, new ModRM(0, 11));
			MODE32_TABLE.add("adcw", opPat(OperandPattern.RM16, OperandPattern.IMM16), OperandSizeMode.MODE16, new int[]{0x81}, new ModRM(0, 12));
			MODE32_TABLE.add("sbbw", opPat(OperandPattern.RM16, OperandPattern.IMM16), OperandSizeMode.MODE16, new int[]{0x81}, new ModRM(0, 13));
			MODE32_TABLE.add("andw", opPat(OperandPattern.RM16, OperandPattern.IMM16), OperandSizeMode.MODE16, new int[]{0x81}, new ModRM(0, 14));
			MODE32_TABLE.add("subw", opPat(OperandPattern.RM16, OperandPattern.IMM16), OperandSizeMode.MODE16, new int[]{0x81}, new ModRM(0, 15));
			MODE32_TABLE.add("xorw", opPat(OperandPattern.RM16, OperandPattern.IMM16), OperandSizeMode.MODE16, new int[]{0x81}, new ModRM(0, 16));
			MODE32_TABLE.add("cmpw", opPat(OperandPattern.RM16, OperandPattern.IMM16), OperandSizeMode.MODE16, new int[]{0x81}, new ModRM(0, 17));
			MODE32_TABLE.add("addl", opPat(OperandPattern.RM32, OperandPattern.IMM32), OperandSizeMode.MODE32, new int[]{0x81}, new ModRM(0, 10));
			MODE32_TABLE.add("orl", opPat(OperandPattern.RM32, OperandPattern.IMM32), OperandSizeMode.MODE32, new int[]{0x81}, new ModRM(0, 11));
			MODE32_TABLE.add("adcl", opPat(OperandPattern.RM32, OperandPattern.IMM32), OperandSizeMode.MODE32, new int[]{0x81}, new ModRM(0, 12));
			MODE32_TABLE.add("sbbl", opPat(OperandPattern.RM32, OperandPattern.IMM32), OperandSizeMode.MODE32, new int[]{0x81}, new ModRM(0, 13));
			MODE32_TABLE.add("andl", opPat(OperandPattern.RM32, OperandPattern.IMM32), OperandSizeMode.MODE32, new int[]{0x81}, new ModRM(0, 14));
			MODE32_TABLE.add("subl", opPat(OperandPattern.RM32, OperandPattern.IMM32), OperandSizeMode.MODE32, new int[]{0x81}, new ModRM(0, 15));
			MODE32_TABLE.add("xorl", opPat(OperandPattern.RM32, OperandPattern.IMM32), OperandSizeMode.MODE32, new int[]{0x81}, new ModRM(0, 16));
			MODE32_TABLE.add("cmpl", opPat(OperandPattern.RM32, OperandPattern.IMM32), OperandSizeMode.MODE32, new int[]{0x81}, new ModRM(0, 17));
			MODE32_TABLE.add("addwb", opPat(OperandPattern.RM16, OperandPattern.IMM8S), OperandSizeMode.MODE16, new int[]{0x83}, new ModRM(0, 10));
			MODE32_TABLE.add("orwb", opPat(OperandPattern.RM16, OperandPattern.IMM8S), OperandSizeMode.MODE16, new int[]{0x83}, new ModRM(0, 11));
			MODE32_TABLE.add("adcwb", opPat(OperandPattern.RM16, OperandPattern.IMM8S), OperandSizeMode.MODE16, new int[]{0x83}, new ModRM(0, 12));
			MODE32_TABLE.add("sbbwb", opPat(OperandPattern.RM16, OperandPattern.IMM8S), OperandSizeMode.MODE16, new int[]{0x83}, new ModRM(0, 13));
			MODE32_TABLE.add("andwb", opPat(OperandPattern.RM16, OperandPattern.IMM8S), OperandSizeMode.MODE16, new int[]{0x83}, new ModRM(0, 14));
			MODE32_TABLE.add("subwb", opPat(OperandPattern.RM16, OperandPattern.IMM8S), OperandSizeMode.MODE16, new int[]{0x83}, new ModRM(0, 15));
			MODE32_TABLE.add("xorwb", opPat(OperandPattern.RM16, OperandPattern.IMM8S), OperandSizeMode.MODE16, new int[]{0x83}, new ModRM(0, 16));
			MODE32_TABLE.add("cmpwb", opPat(OperandPattern.RM16, OperandPattern.IMM8S), OperandSizeMode.MODE16, new int[]{0x83}, new ModRM(0, 17));
			MODE32_TABLE.add("addlb", opPat(OperandPattern.RM32, OperandPattern.IMM8S), OperandSizeMode.MODE32, new int[]{0x83}, new ModRM(0, 10));
			MODE32_TABLE.add("orlb", opPat(OperandPattern.RM32, OperandPattern.IMM8S), OperandSizeMode.MODE32, new int[]{0x83}, new ModRM(0, 11));
			MODE32_TABLE.add("adclb", opPat(OperandPattern.RM32, OperandPattern.IMM8S), OperandSizeMode.MODE32, new int[]{0x83}, new ModRM(0, 12));
			MODE32_TABLE.add("sbblb", opPat(OperandPattern.RM32, OperandPattern.IMM8S), OperandSizeMode.MODE32, new int[]{0x83}, new ModRM(0, 13));
			MODE32_TABLE.add("andlb", opPat(OperandPattern.RM32, OperandPattern.IMM8S), OperandSizeMode.MODE32, new int[]{0x83}, new ModRM(0, 14));
			MODE32_TABLE.add("sublb", opPat(OperandPattern.RM32, OperandPattern.IMM8S), OperandSizeMode.MODE32, new int[]{0x83}, new ModRM(0, 15));
			MODE32_TABLE.add("xorlb", opPat(OperandPattern.RM32, OperandPattern.IMM8S), OperandSizeMode.MODE32, new int[]{0x83}, new ModRM(0, 16));
			MODE32_TABLE.add("cmplb", opPat(OperandPattern.RM32, OperandPattern.IMM8S), OperandSizeMode.MODE32, new int[]{0x83}, new ModRM(0, 17));
			MODE32_TABLE.add("testb", opPat(OperandPattern.RM8, OperandPattern.REG8), OperandSizeMode.MODELESS, new int[]{0x84}, new ModRM(0, 1));
			MODE32_TABLE.add("testw", opPat(OperandPattern.RM16, OperandPattern.REG16), OperandSizeMode.MODE16, new int[]{0x85}, new ModRM(0, 1));
			MODE32_TABLE.add("testl", opPat(OperandPattern.RM32, OperandPattern.REG32), OperandSizeMode.MODE32, new int[]{0x85}, new ModRM(0, 1));
			MODE32_TABLE.add("xchgb", opPat(OperandPattern.RM8, OperandPattern.REG8), OperandSizeMode.MODELESS, new int[]{0x86}, new ModRM(0, 1));
			MODE32_TABLE.add("xchgw", opPat(OperandPattern.RM16, OperandPattern.REG16), OperandSizeMode.MODE16, new int[]{0x87}, new ModRM(0, 1));
			MODE32_TABLE.add("xchgl", opPat(OperandPattern.RM32, OperandPattern.REG32), OperandSizeMode.MODE32, new int[]{0x87}, new ModRM(0, 1));
			MODE32_TABLE.add("movb", opPat(OperandPattern.RM8, OperandPattern.REG8), OperandSizeMode.MODELESS, new int[]{0x88}, new ModRM(0, 1));
			MODE32_TABLE.add("movw", opPat(OperandPattern.RM16, OperandPattern.REG16), OperandSizeMode.MODE16, new int[]{0x89}, new ModRM(0, 1));
			MODE32_TABLE.add("movl", opPat(OperandPattern.RM32, OperandPattern.REG32), OperandSizeMode.MODE32, new int[]{0x89}, new ModRM(0, 1));
			MODE32_TABLE.add("movb", opPat(OperandPattern.REG8, OperandPattern.RM8), OperandSizeMode.MODELESS, new int[]{0x8A}, new ModRM(1, 0));
			MODE32_TABLE.add("movw", opPat(OperandPattern.REG16, OperandPattern.RM16), OperandSizeMode.MODE16, new int[]{0x8B}, new ModRM(1, 0));
			MODE32_TABLE.add("movl", opPat(OperandPattern.REG32, OperandPattern.RM32), OperandSizeMode.MODE32, new int[]{0x8B}, new ModRM(1, 0));
			MODE32_TABLE.add("movw", opPat(OperandPattern.RM16, OperandPattern.SREG), OperandSizeMode.MODE16, new int[]{0x8C}, new ModRM(0, 1));
			MODE32_TABLE.add("movlw", opPat(OperandPattern.RM32, OperandPattern.SREG), OperandSizeMode.MODE32, new int[]{0x8C}, new ModRM(0, 1));
			MODE32_TABLE.add("leaw", opPat(OperandPattern.REG16, OperandPattern.MEM), OperandSizeMode.MODE16, new int[]{0x8D}, new ModRM(1, 0));
			MODE32_TABLE.add("leal", opPat(OperandPattern.REG32, OperandPattern.MEM), OperandSizeMode.MODE32, new int[]{0x8D}, new ModRM(1, 0));
			MODE32_TABLE.add("movw", opPat(OperandPattern.SREG, OperandPattern.RM16), OperandSizeMode.MODE16, new int[]{0x8E}, new ModRM(1, 0));
			MODE32_TABLE.add("movwl", opPat(OperandPattern.SREG, OperandPattern.RM32), OperandSizeMode.MODE32, new int[]{0x8E}, new ModRM(1, 0));
			MODE32_TABLE.add("popw", opPat(OperandPattern.RM16), OperandSizeMode.MODE16, new int[]{0x8F}, new ModRM(0, 10));
			MODE32_TABLE.add("popl", opPat(OperandPattern.RM32), OperandSizeMode.MODE32, new int[]{0x8F}, new ModRM(0, 10));
			MODE32_TABLE.add("nop", opPat(), OperandSizeMode.MODELESS, new int[]{0x90});
			MODE32_TABLE.add("xchgw", opPat(OperandPattern.REG16, OperandPattern.AX), OperandSizeMode.MODE16, new int[]{0x90}, new RegisterInOpcode(0));
			MODE32_TABLE.add("xchgl", opPat(OperandPattern.REG32, OperandPattern.EAX), OperandSizeMode.MODE32, new int[]{0x90}, new RegisterInOpcode(0));
			MODE32_TABLE.add("cbw", opPat(), OperandSizeMode.MODE16, new int[]{0x98});
			MODE32_TABLE.add("cwde", opPat(), OperandSizeMode.MODE32, new int[]{0x98});
			MODE32_TABLE.add("cwd", opPat(), OperandSizeMode.MODE16, new int[]{0x99});
			MODE32_TABLE.add("cdq", opPat(), OperandSizeMode.MODE32, new int[]{0x99});
			MODE32_TABLE.add("pushfw", opPat(), OperandSizeMode.MODE16, new int[]{0x9C});
			MODE32_TABLE.add("pushfl", opPat(), OperandSizeMode.MODE32, new int[]{0x9C});
			MODE32_TABLE.add("popfw", opPat(), OperandSizeMode.MODE16, new int[]{0x9D});
			MODE32_TABLE.add("popfl", opPat(), OperandSizeMode.MODE32, new int[]{0x9D});
			MODE32_TABLE.add("sahf", opPat(), OperandSizeMode.MODELESS, new int[]{0x9E});
			MODE32_TABLE.add("lahf", opPat(), OperandSizeMode.MODELESS, new int[]{0x9F});
			MODE32_TABLE.add("movsb", opPat(), OperandSizeMode.MODELESS, new int[]{0xA4});
			MODE32_TABLE.add("movsw", opPat(), OperandSizeMode.MODE16, new int[]{0xA5});
			MODE32_TABLE.add("movsl", opPat(), OperandSizeMode.MODE32, new int[]{0xA5});
			MODE32_TABLE.add("cmpsb", opPat(), OperandSizeMode.MODELESS, new int[]{0xA6});
			MODE32_TABLE.add("cmpsw", opPat(), OperandSizeMode.MODE16, new int[]{0xA7});
			MODE32_TABLE.add("cmpsl", opPat(), OperandSizeMode.MODE32, new int[]{0xA8});
			MODE32_TABLE.add("testb", opPat(OperandPattern.AL, OperandPattern.IMM8), OperandSizeMode.MODELESS, new int[]{0xA8});
			MODE32_TABLE.add("testw", opPat(OperandPattern.AX, OperandPattern.IMM16), OperandSizeMode.MODE16, new int[]{0xA9});
			MODE32_TABLE.add("testl", opPat(OperandPattern.AX, OperandPattern.IMM32), OperandSizeMode.MODE32, new int[]{0xA9});
			MODE32_TABLE.add("stosb", opPat(), OperandSizeMode.MODELESS, new int[]{0xAA});
			MODE32_TABLE.add("stosw", opPat(), OperandSizeMode.MODE16, new int[]{0xAB});
			MODE32_TABLE.add("stosl", opPat(), OperandSizeMode.MODE32, new int[]{0xAB});
			MODE32_TABLE.add("lodsb", opPat(), OperandSizeMode.MODELESS, new int[]{0xAC});
			MODE32_TABLE.add("lodsw", opPat(), OperandSizeMode.MODE16, new int[]{0xAD});
			MODE32_TABLE.add("lodsl", opPat(), OperandSizeMode.MODE32, new int[]{0xAD});
			MODE32_TABLE.add("scasb", opPat(), OperandSizeMode.MODELESS, new int[]{0xAE});
			MODE32_TABLE.add("scasw", opPat(), OperandSizeMode.MODE16, new int[]{0xAF});
			MODE32_TABLE.add("scasl", opPat(), OperandSizeMode.MODE32, new int[]{0xAF});
			MODE32_TABLE.add("movb", opPat(OperandPattern.REG8, OperandPattern.IMM8), OperandSizeMode.MODELESS, new int[]{0xB0}, new RegisterInOpcode(0));
			MODE32_TABLE.add("movw", opPat(OperandPattern.REG16, OperandPattern.IMM16), OperandSizeMode.MODE16, new int[]{0xB8}, new RegisterInOpcode(0));
			MODE32_TABLE.add("movl", opPat(OperandPattern.REG32, OperandPattern.IMM32), OperandSizeMode.MODE32, new int[]{0xB8}, new RegisterInOpcode(0));
			MODE32_TABLE.add("rolb", opPat(OperandPattern.RM8, OperandPattern.IMM8), OperandSizeMode.MODELESS, new int[]{0xC0}, new ModRM(0, 10));
			MODE32_TABLE.add("rorb", opPat(OperandPattern.RM8, OperandPattern.IMM8), OperandSizeMode.MODELESS, new int[]{0xC0}, new ModRM(0, 11));
			MODE32_TABLE.add("rclb", opPat(OperandPattern.RM8, OperandPattern.IMM8), OperandSizeMode.MODELESS, new int[]{0xC0}, new ModRM(0, 12));
			MODE32_TABLE.add("rcrb", opPat(OperandPattern.RM8, OperandPattern.IMM8), OperandSizeMode.MODELESS, new int[]{0xC0}, new ModRM(0, 13));
			MODE32_TABLE.add("shlb|salb", opPat(OperandPattern.RM8, OperandPattern.IMM8), OperandSizeMode.MODELESS, new int[]{0xC0}, new ModRM(0, 14));
			MODE32_TABLE.add("shrb", opPat(OperandPattern.RM8, OperandPattern.IMM8), OperandSizeMode.MODELESS, new int[]{0xC0}, new ModRM(0, 15));
			MODE32_TABLE.add("sarb", opPat(OperandPattern.RM8, OperandPattern.IMM8), OperandSizeMode.MODELESS, new int[]{0xC0}, new ModRM(0, 17));
			MODE32_TABLE.add("rolw", opPat(OperandPattern.RM16, OperandPattern.IMM8), OperandSizeMode.MODE16, new int[]{0xC1}, new ModRM(0, 10));
			MODE32_TABLE.add("rorw", opPat(OperandPattern.RM16, OperandPattern.IMM8), OperandSizeMode.MODE16, new int[]{0xC1}, new ModRM(0, 11));
			MODE32_TABLE.add("rclw", opPat(OperandPattern.RM16, OperandPattern.IMM8), OperandSizeMode.MODE16, new int[]{0xC1}, new ModRM(0, 12));
			MODE32_TABLE.add("rcrw", opPat(OperandPattern.RM16, OperandPattern.IMM8), OperandSizeMode.MODE16, new int[]{0xC1}, new ModRM(0, 13));
			MODE32_TABLE.add("shlw|salw", opPat(OperandPattern.RM16, OperandPattern.IMM8), OperandSizeMode.MODE16, new int[]{0xC1}, new ModRM(0, 14));
			MODE32_TABLE.add("shrw", opPat(OperandPattern.RM16, OperandPattern.IMM8), OperandSizeMode.MODE16, new int[]{0xC1}, new ModRM(0, 15));
			MODE32_TABLE.add("sarw", opPat(OperandPattern.RM16, OperandPattern.IMM8), OperandSizeMode.MODE16, new int[]{0xC1}, new ModRM(0, 17));
			MODE32_TABLE.add("roll", opPat(OperandPattern.RM32, OperandPattern.IMM8), OperandSizeMode.MODE32, new int[]{0xC1}, new ModRM(0, 10));
			MODE32_TABLE.add("rorl", opPat(OperandPattern.RM32, OperandPattern.IMM8), OperandSizeMode.MODE32, new int[]{0xC1}, new ModRM(0, 11));
			MODE32_TABLE.add("rcll", opPat(OperandPattern.RM32, OperandPattern.IMM8), OperandSizeMode.MODE32, new int[]{0xC1}, new ModRM(0, 12));
			MODE32_TABLE.add("rcrl", opPat(OperandPattern.RM32, OperandPattern.IMM8), OperandSizeMode.MODE32, new int[]{0xC1}, new ModRM(0, 13));
			MODE32_TABLE.add("shll|sall", opPat(OperandPattern.RM32, OperandPattern.IMM8), OperandSizeMode.MODE32, new int[]{0xC1}, new ModRM(0, 14));
			MODE32_TABLE.add("shrl", opPat(OperandPattern.RM32, OperandPattern.IMM8), OperandSizeMode.MODE32, new int[]{0xC1}, new ModRM(0, 15));
			MODE32_TABLE.add("sarl", opPat(OperandPattern.RM32, OperandPattern.IMM8), OperandSizeMode.MODE32, new int[]{0xC1}, new ModRM(0, 17));
			MODE32_TABLE.add("ret", opPat(OperandPattern.IMM16), OperandSizeMode.MODELESS, new int[]{0xC2});
			MODE32_TABLE.add("ret", opPat(), OperandSizeMode.MODELESS, new int[]{0xC3});
			MODE32_TABLE.add("lesw", opPat(OperandPattern.REG16, OperandPattern.MEM), OperandSizeMode.MODE16, new int[]{0xC4}, new ModRM(1, 0));
			MODE32_TABLE.add("lesl", opPat(OperandPattern.REG32, OperandPattern.MEM), OperandSizeMode.MODE32, new int[]{0xC4}, new ModRM(1, 0));
			MODE32_TABLE.add("ldsw", opPat(OperandPattern.REG16, OperandPattern.MEM), OperandSizeMode.MODE16, new int[]{0xC5}, new ModRM(1, 0));
			MODE32_TABLE.add("ldsl", opPat(OperandPattern.REG32, OperandPattern.MEM), OperandSizeMode.MODE32, new int[]{0xC5}, new ModRM(1, 0));
			MODE32_TABLE.add("movb", opPat(OperandPattern.RM8, OperandPattern.IMM8), OperandSizeMode.MODELESS, new int[]{0xC6}, new ModRM(0, 10));
			MODE32_TABLE.add("movw", opPat(OperandPattern.RM16, OperandPattern.IMM16), OperandSizeMode.MODE16, new int[]{0xC7}, new ModRM(0, 10));
			MODE32_TABLE.add("movl", opPat(OperandPattern.RM32, OperandPattern.IMM32), OperandSizeMode.MODE32, new int[]{0xC7}, new ModRM(0, 10));
			MODE32_TABLE.add("enter", opPat(OperandPattern.IMM16, OperandPattern.IMM8), OperandSizeMode.MODELESS, new int[]{0xC8});
			MODE32_TABLE.add("leave", opPat(), OperandSizeMode.MODELESS, new int[]{0xC9});
			MODE32_TABLE.add("lret", opPat(OperandPattern.IMM16), OperandSizeMode.MODELESS, new int[]{0xCA});
			MODE32_TABLE.add("lret", opPat(), OperandSizeMode.MODELESS, new int[]{0xCB});
			MODE32_TABLE.add("int", opPat(OperandPattern.IMM_VAL_3), OperandSizeMode.MODELESS, new int[]{0xCC});
			MODE32_TABLE.add("int", opPat(OperandPattern.IMM8), OperandSizeMode.MODELESS, new int[]{0xCD});
			MODE32_TABLE.add("into", opPat(), OperandSizeMode.MODELESS, new int[]{0xCE});
			MODE32_TABLE.add("iretw", opPat(), OperandSizeMode.MODE16, new int[]{0xCF});
			MODE32_TABLE.add("iretl", opPat(), OperandSizeMode.MODE32, new int[]{0xCF});
			MODE32_TABLE.add("rolb", opPat(OperandPattern.RM8, OperandPattern.IMM_VAL_1), OperandSizeMode.MODELESS, new int[]{0xD0}, new ModRM(0, 10));
			MODE32_TABLE.add("rorb", opPat(OperandPattern.RM8, OperandPattern.IMM_VAL_1), OperandSizeMode.MODELESS, new int[]{0xD0}, new ModRM(0, 11));
			MODE32_TABLE.add("rclb", opPat(OperandPattern.RM8, OperandPattern.IMM_VAL_1), OperandSizeMode.MODELESS, new int[]{0xD0}, new ModRM(0, 12));
			MODE32_TABLE.add("rcrb", opPat(OperandPattern.RM8, OperandPattern.IMM_VAL_1), OperandSizeMode.MODELESS, new int[]{0xD0}, new ModRM(0, 13));
			MODE32_TABLE.add("shlb|salb", opPat(OperandPattern.RM8, OperandPattern.IMM_VAL_1), OperandSizeMode.MODELESS, new int[]{0xD0}, new ModRM(0, 14));
			MODE32_TABLE.add("shrb", opPat(OperandPattern.RM8, OperandPattern.IMM_VAL_1), OperandSizeMode.MODELESS, new int[]{0xD0}, new ModRM(0, 15));
			MODE32_TABLE.add("sarb", opPat(OperandPattern.RM8, OperandPattern.IMM_VAL_1), OperandSizeMode.MODELESS, new int[]{0xD0}, new ModRM(0, 17));
			MODE32_TABLE.add("rolw", opPat(OperandPattern.RM16, OperandPattern.IMM_VAL_1), OperandSizeMode.MODE16, new int[]{0xD1}, new ModRM(0, 10));
			MODE32_TABLE.add("rorw", opPat(OperandPattern.RM16, OperandPattern.IMM_VAL_1), OperandSizeMode.MODE16, new int[]{0xD1}, new ModRM(0, 11));
			MODE32_TABLE.add("rclw", opPat(OperandPattern.RM16, OperandPattern.IMM_VAL_1), OperandSizeMode.MODE16, new int[]{0xD1}, new ModRM(0, 12));
			MODE32_TABLE.add("rcrw", opPat(OperandPattern.RM16, OperandPattern.IMM_VAL_1), OperandSizeMode.MODE16, new int[]{0xD1}, new ModRM(0, 13));
			MODE32_TABLE.add("shlw|shlw", opPat(OperandPattern.RM16, OperandPattern.IMM_VAL_1), OperandSizeMode.MODE16, new int[]{0xD1}, new ModRM(0, 14));
			MODE32_TABLE.add("shrw", opPat(OperandPattern.RM16, OperandPattern.IMM_VAL_1), OperandSizeMode.MODE16, new int[]{0xD1}, new ModRM(0, 15));
			MODE32_TABLE.add("sarw", opPat(OperandPattern.RM16, OperandPattern.IMM_VAL_1), OperandSizeMode.MODE16, new int[]{0xD1}, new ModRM(0, 17));
			MODE32_TABLE.add("roll", opPat(OperandPattern.RM32, OperandPattern.IMM_VAL_1), OperandSizeMode.MODE32, new int[]{0xD1}, new ModRM(0, 10));
			MODE32_TABLE.add("rorl", opPat(OperandPattern.RM32, OperandPattern.IMM_VAL_1), OperandSizeMode.MODE32, new int[]{0xD1}, new ModRM(0, 11));
			MODE32_TABLE.add("rcll", opPat(OperandPattern.RM32, OperandPattern.IMM_VAL_1), OperandSizeMode.MODE32, new int[]{0xD1}, new ModRM(0, 12));
			MODE32_TABLE.add("rcrl", opPat(OperandPattern.RM32, OperandPattern.IMM_VAL_1), OperandSizeMode.MODE32, new int[]{0xD1}, new ModRM(0, 13));
			MODE32_TABLE.add("shll|sall", opPat(OperandPattern.RM32, OperandPattern.IMM_VAL_1), OperandSizeMode.MODE32, new int[]{0xD1}, new ModRM(0, 14));
			MODE32_TABLE.add("shrl", opPat(OperandPattern.RM32, OperandPattern.IMM_VAL_1), OperandSizeMode.MODE32, new int[]{0xD1}, new ModRM(0, 15));
			MODE32_TABLE.add("sarl", opPat(OperandPattern.RM32, OperandPattern.IMM_VAL_1), OperandSizeMode.MODE32, new int[]{0xD1}, new ModRM(0, 17));
			MODE32_TABLE.add("rolb", opPat(OperandPattern.RM8, OperandPattern.CL), OperandSizeMode.MODELESS, new int[]{0xD2}, new ModRM(0, 10));
			MODE32_TABLE.add("rorb", opPat(OperandPattern.RM8, OperandPattern.CL), OperandSizeMode.MODELESS, new int[]{0xD2}, new ModRM(0, 11));
			MODE32_TABLE.add("rclb", opPat(OperandPattern.RM8, OperandPattern.CL), OperandSizeMode.MODELESS, new int[]{0xD2}, new ModRM(0, 12));
			MODE32_TABLE.add("rcrb", opPat(OperandPattern.RM8, OperandPattern.CL), OperandSizeMode.MODELESS, new int[]{0xD2}, new ModRM(0, 13));
			MODE32_TABLE.add("shlb|salb", opPat(OperandPattern.RM8, OperandPattern.CL), OperandSizeMode.MODELESS, new int[]{0xD2}, new ModRM(0, 14));
			MODE32_TABLE.add("shrb", opPat(OperandPattern.RM8, OperandPattern.CL), OperandSizeMode.MODELESS, new int[]{0xD2}, new ModRM(0, 15));
			MODE32_TABLE.add("sarb", opPat(OperandPattern.RM8, OperandPattern.CL), OperandSizeMode.MODELESS, new int[]{0xD2}, new ModRM(0, 17));
			MODE32_TABLE.add("rolw", opPat(OperandPattern.RM16, OperandPattern.CL), OperandSizeMode.MODE16, new int[]{0xD3}, new ModRM(0, 10));
			MODE32_TABLE.add("rorw", opPat(OperandPattern.RM16, OperandPattern.CL), OperandSizeMode.MODE16, new int[]{0xD3}, new ModRM(0, 11));
			MODE32_TABLE.add("rclw", opPat(OperandPattern.RM16, OperandPattern.CL), OperandSizeMode.MODE16, new int[]{0xD3}, new ModRM(0, 12));
			MODE32_TABLE.add("rcrw", opPat(OperandPattern.RM16, OperandPattern.CL), OperandSizeMode.MODE16, new int[]{0xD3}, new ModRM(0, 13));
			MODE32_TABLE.add("shlw|salw", opPat(OperandPattern.RM16, OperandPattern.CL), OperandSizeMode.MODE16, new int[]{0xD3}, new ModRM(0, 14));
			MODE32_TABLE.add("shrw", opPat(OperandPattern.RM16, OperandPattern.CL), OperandSizeMode.MODE16, new int[]{0xD3}, new ModRM(0, 15));
			MODE32_TABLE.add("sarw", opPat(OperandPattern.RM16, OperandPattern.CL), OperandSizeMode.MODE16, new int[]{0xD3}, new ModRM(0, 17));
			MODE32_TABLE.add("roll", opPat(OperandPattern.RM32, OperandPattern.CL), OperandSizeMode.MODE32, new int[]{0xD3}, new ModRM(0, 10));
			MODE32_TABLE.add("rorl", opPat(OperandPattern.RM32, OperandPattern.CL), OperandSizeMode.MODE32, new int[]{0xD3}, new ModRM(0, 11));
			MODE32_TABLE.add("rcll", opPat(OperandPattern.RM32, OperandPattern.CL), OperandSizeMode.MODE32, new int[]{0xD3}, new ModRM(0, 12));
			MODE32_TABLE.add("rcrl", opPat(OperandPattern.RM32, OperandPattern.CL), OperandSizeMode.MODE32, new int[]{0xD3}, new ModRM(0, 13));
			MODE32_TABLE.add("shll|sall", opPat(OperandPattern.RM32, OperandPattern.CL), OperandSizeMode.MODE32, new int[]{0xD3}, new ModRM(0, 14));
			MODE32_TABLE.add("shrl", opPat(OperandPattern.RM32, OperandPattern.CL), OperandSizeMode.MODE32, new int[]{0xD3}, new ModRM(0, 15));
			MODE32_TABLE.add("sarl", opPat(OperandPattern.RM32, OperandPattern.CL), OperandSizeMode.MODE32, new int[]{0xD3}, new ModRM(0, 17));
			MODE32_TABLE.add("aam", opPat(), OperandSizeMode.MODELESS, new int[]{0xD4, 0x0A});
			MODE32_TABLE.add("aad", opPat(), OperandSizeMode.MODELESS, new int[]{0xD5, 0x0A});
			MODE32_TABLE.add("salc", opPat(), OperandSizeMode.MODELESS, new int[]{0xD6});
			MODE32_TABLE.add("setalc", opPat(), OperandSizeMode.MODELESS, new int[]{0xD6});
			MODE32_TABLE.add("xlat", opPat(), OperandSizeMode.MODELESS, new int[]{0xD7});
			MODE32_TABLE.add("loopnz|loopne", opPat(OperandPattern.REL8), OperandSizeMode.MODELESS, new int[]{0xE0});
			MODE32_TABLE.add("loopz|loope", opPat(OperandPattern.REL8), OperandSizeMode.MODELESS, new int[]{0xE1});
			MODE32_TABLE.add("loop", opPat(OperandPattern.REL8), OperandSizeMode.MODELESS, new int[]{0xE2});
			MODE32_TABLE.add("jecxz", opPat(OperandPattern.REL8), OperandSizeMode.MODELESS, new int[]{0xE3});
			MODE32_TABLE.add("inb", opPat(OperandPattern.AL, OperandPattern.IMM8), OperandSizeMode.MODELESS, new int[]{0xE4});
			MODE32_TABLE.add("inw", opPat(OperandPattern.AX, OperandPattern.IMM8), OperandSizeMode.MODE16, new int[]{0xE5});
			MODE32_TABLE.add("inl", opPat(OperandPattern.EAX, OperandPattern.IMM8), OperandSizeMode.MODE32, new int[]{0xE5});
			MODE32_TABLE.add("outb", opPat(OperandPattern.IMM8, OperandPattern.AL), OperandSizeMode.MODELESS, new int[]{0xE6});
			MODE32_TABLE.add("outw", opPat(OperandPattern.IMM8, OperandPattern.AX), OperandSizeMode.MODE16, new int[]{0xE7});
			MODE32_TABLE.add("outl", opPat(OperandPattern.IMM8, OperandPattern.EAX), OperandSizeMode.MODE32, new int[]{0xE7});
			MODE32_TABLE.add("call", opPat(OperandPattern.REL16), OperandSizeMode.MODE16, new int[]{0xE8});
			MODE32_TABLE.add("call", opPat(OperandPattern.REL32), OperandSizeMode.MODE32, new int[]{0xE8});
			MODE32_TABLE.add("jmp", opPat(OperandPattern.REL16), OperandSizeMode.MODE16, new int[]{0xE9});
			MODE32_TABLE.add("jmp", opPat(OperandPattern.REL32), OperandSizeMode.MODE32, new int[]{0xE9});
			MODE32_TABLE.add("ljmpw", opPat(OperandPattern.MEM), OperandSizeMode.MODE16, new int[]{0xE9});
			MODE32_TABLE.add("ljmpl", opPat(OperandPattern.MEM), OperandSizeMode.MODE32, new int[]{0xE9});
			MODE32_TABLE.add("jmp", opPat(OperandPattern.REL8), OperandSizeMode.MODELESS, new int[]{0xEB});
			MODE32_TABLE.add("inb", opPat(OperandPattern.AL, OperandPattern.DX), OperandSizeMode.MODELESS, new int[]{0xEC});
			MODE32_TABLE.add("inw", opPat(OperandPattern.AX, OperandPattern.DX), OperandSizeMode.MODE16, new int[]{0xED});
			MODE32_TABLE.add("inl", opPat(OperandPattern.EAX, OperandPattern.DX), OperandSizeMode.MODE32, new int[]{0xED});
			MODE32_TABLE.add("outb", opPat(OperandPattern.DX, OperandPattern.AL), OperandSizeMode.MODELESS, new int[]{0xEE});
			MODE32_TABLE.add("outw", opPat(OperandPattern.DX, OperandPattern.AX), OperandSizeMode.MODE16, new int[]{0xEF});
			MODE32_TABLE.add("outl", opPat(OperandPattern.DX, OperandPattern.EAX), OperandSizeMode.MODE32, new int[]{0xEF});
			MODE32_TABLE.add("lock", opPat(), OperandSizeMode.MODELESS, new int[]{0xF0});
			MODE32_TABLE.add("repnz|repne", opPat(), OperandSizeMode.MODELESS, new int[]{0xF2});
			MODE32_TABLE.add("rep|repz|repe", opPat(), OperandSizeMode.MODELESS, new int[]{0xF3});
			MODE32_TABLE.add("hlt", opPat(), OperandSizeMode.MODELESS, new int[]{0xF4});
			MODE32_TABLE.add("cmc", opPat(), OperandSizeMode.MODELESS, new int[]{0xF5});
			MODE32_TABLE.add("testb", opPat(OperandPattern.RM8, OperandPattern.IMM8), OperandSizeMode.MODELESS, new int[]{0xF6}, new ModRM(0, 10));
			MODE32_TABLE.add("notb", opPat(OperandPattern.RM8), OperandSizeMode.MODELESS, new int[]{0xF6}, new ModRM(0, 12));
			MODE32_TABLE.add("negb", opPat(OperandPattern.RM8), OperandSizeMode.MODELESS, new int[]{0xF6}, new ModRM(0, 13));
			MODE32_TABLE.add("mulb", opPat(OperandPattern.RM8), OperandSizeMode.MODELESS, new int[]{0xF6}, new ModRM(0, 14));
			MODE32_TABLE.add("imulb", opPat(OperandPattern.RM8), OperandSizeMode.MODELESS, new int[]{0xF6}, new ModRM(0, 15));
			MODE32_TABLE.add("divb", opPat(OperandPattern.RM8), OperandSizeMode.MODELESS, new int[]{0xF6}, new ModRM(0, 16));
			MODE32_TABLE.add("idivb", opPat(OperandPattern.RM8), OperandSizeMode.MODELESS, new int[]{0xF6}, new ModRM(0, 17));
			MODE32_TABLE.add("testw", opPat(OperandPattern.RM16, OperandPattern.IMM16), OperandSizeMode.MODE16, new int[]{0xF7}, new ModRM(0, 10));
			MODE32_TABLE.add("notw", opPat(OperandPattern.RM16), OperandSizeMode.MODE16, new int[]{0xF7}, new ModRM(0, 12));
			MODE32_TABLE.add("negw", opPat(OperandPattern.RM16), OperandSizeMode.MODE16, new int[]{0xF7}, new ModRM(0, 13));
			MODE32_TABLE.add("mulw", opPat(OperandPattern.RM16), OperandSizeMode.MODE16, new int[]{0xF7}, new ModRM(0, 14));
			MODE32_TABLE.add("imulw", opPat(OperandPattern.RM16), OperandSizeMode.MODE16, new int[]{0xF7}, new ModRM(0, 15));
			MODE32_TABLE.add("divw", opPat(OperandPattern.RM16), OperandSizeMode.MODE16, new int[]{0xF7}, new ModRM(0, 16));
			MODE32_TABLE.add("idivw", opPat(OperandPattern.RM16), OperandSizeMode.MODE16, new int[]{0xF7}, new ModRM(0, 17));
			MODE32_TABLE.add("testl", opPat(OperandPattern.RM32, OperandPattern.IMM32), OperandSizeMode.MODE32, new int[]{0xF7}, new ModRM(0, 10));
			MODE32_TABLE.add("notl", opPat(OperandPattern.RM32), OperandSizeMode.MODE32, new int[]{0xF7}, new ModRM(0, 12));
			MODE32_TABLE.add("negl", opPat(OperandPattern.RM32), OperandSizeMode.MODE32, new int[]{0xF7}, new ModRM(0, 13));
			MODE32_TABLE.add("mull", opPat(OperandPattern.RM32), OperandSizeMode.MODE32, new int[]{0xF7}, new ModRM(0, 14));
			MODE32_TABLE.add("imull", opPat(OperandPattern.RM32), OperandSizeMode.MODE32, new int[]{0xF7}, new ModRM(0, 15));
			MODE32_TABLE.add("divl", opPat(OperandPattern.RM32), OperandSizeMode.MODE32, new int[]{0xF7}, new ModRM(0, 16));
			MODE32_TABLE.add("idivl", opPat(OperandPattern.RM32), OperandSizeMode.MODE32, new int[]{0xF7}, new ModRM(0, 17));
			MODE32_TABLE.add("clc", opPat(), OperandSizeMode.MODELESS, new int[]{0xF8});
			MODE32_TABLE.add("stc", opPat(), OperandSizeMode.MODELESS, new int[]{0xF9});
			MODE32_TABLE.add("cli", opPat(), OperandSizeMode.MODELESS, new int[]{0xFA});
			MODE32_TABLE.add("sti", opPat(), OperandSizeMode.MODELESS, new int[]{0xFB});
			MODE32_TABLE.add("cld", opPat(), OperandSizeMode.MODELESS, new int[]{0xFC});
			MODE32_TABLE.add("std", opPat(), OperandSizeMode.MODELESS, new int[]{0xFD});
			MODE32_TABLE.add("incb", opPat(OperandPattern.RM8), OperandSizeMode.MODELESS, new int[]{0xFE}, new ModRM(0, 10));
			MODE32_TABLE.add("decb", opPat(OperandPattern.RM8), OperandSizeMode.MODELESS, new int[]{0xFE}, new ModRM(0, 11));
			MODE32_TABLE.add("incw", opPat(OperandPattern.RM16), OperandSizeMode.MODE16, new int[]{0xFF}, new ModRM(0, 10));
			MODE32_TABLE.add("decw", opPat(OperandPattern.RM16), OperandSizeMode.MODE16, new int[]{0xFF}, new ModRM(0, 11));
			MODE32_TABLE.add("callw", opPat(OperandPattern.RM16), OperandSizeMode.MODE16, new int[]{0xFF}, new ModRM(0, 12));
			MODE32_TABLE.add("lcallw", opPat(OperandPattern.MEM), OperandSizeMode.MODE16, new int[]{0xFF}, new ModRM(0, 13));
			MODE32_TABLE.add("jmpw", opPat(OperandPattern.RM16), OperandSizeMode.MODE16, new int[]{0xFF}, new ModRM(0, 14));
			MODE32_TABLE.add("ljmpw", opPat(OperandPattern.MEM), OperandSizeMode.MODE16, new int[]{0xFF}, new ModRM(0, 15));
			MODE32_TABLE.add("pushw", opPat(OperandPattern.RM16), OperandSizeMode.MODE16, new int[]{0xFF}, new ModRM(0, 16));
			MODE32_TABLE.add("incl", opPat(OperandPattern.RM32), OperandSizeMode.MODE32, new int[]{0xFF}, new ModRM(0, 10));
			MODE32_TABLE.add("decl", opPat(OperandPattern.RM32), OperandSizeMode.MODE32, new int[]{0xFF}, new ModRM(0, 11));
			MODE32_TABLE.add("calll", opPat(OperandPattern.RM32), OperandSizeMode.MODE32, new int[]{0xFF}, new ModRM(0, 12));
			MODE32_TABLE.add("lcalll", opPat(OperandPattern.MEM), OperandSizeMode.MODE32, new int[]{0xFF}, new ModRM(0, 13));
			MODE32_TABLE.add("jmpl", opPat(OperandPattern.RM32), OperandSizeMode.MODE32, new int[]{0xFF}, new ModRM(0, 14));
			MODE32_TABLE.add("ljmpl", opPat(OperandPattern.MEM), OperandSizeMode.MODE32, new int[]{0xFF}, new ModRM(0, 15));
			MODE32_TABLE.add("pushl", opPat(OperandPattern.RM32), OperandSizeMode.MODE32, new int[]{0xFF}, new ModRM(0, 16));
			MODE32_TABLE.add("invd", opPat(), OperandSizeMode.MODELESS, new int[]{0x0F, 0x08});
			MODE32_TABLE.add("wbinvd", opPat(), OperandSizeMode.MODELESS, new int[]{0x0F, 0x09});
			MODE32_TABLE.add("ud2", opPat(), OperandSizeMode.MODELESS, new int[]{0x0F, 0x0B});
			MODE32_TABLE.add("sysenter", opPat(), OperandSizeMode.MODELESS, new int[]{0x0F, 0x34});
			MODE32_TABLE.add("sysexit", opPat(), OperandSizeMode.MODELESS, new int[]{0x0F, 0x35});
			MODE32_TABLE.add("jo", opPat(OperandPattern.REL16), OperandSizeMode.MODE16, new int[]{0x0F, 0x80});
			MODE32_TABLE.add("jno", opPat(OperandPattern.REL16), OperandSizeMode.MODE16, new int[]{0x0F, 0x81});
			MODE32_TABLE.add("jb|jnae|jc", opPat(OperandPattern.REL16), OperandSizeMode.MODE16, new int[]{0x0F, 0x82});
			MODE32_TABLE.add("jnb|jae|jnc", opPat(OperandPattern.REL16), OperandSizeMode.MODE16, new int[]{0x0F, 0x83});
			MODE32_TABLE.add("jz|je", opPat(OperandPattern.REL16), OperandSizeMode.MODE16, new int[]{0x0F, 0x84});
			MODE32_TABLE.add("jnz|jne", opPat(OperandPattern.REL16), OperandSizeMode.MODE16, new int[]{0x0F, 0x85});
			MODE32_TABLE.add("jbe|jna", opPat(OperandPattern.REL16), OperandSizeMode.MODE16, new int[]{0x0F, 0x86});
			MODE32_TABLE.add("jnbe|ja", opPat(OperandPattern.REL16), OperandSizeMode.MODE16, new int[]{0x0F, 0x87});
			MODE32_TABLE.add("js", opPat(OperandPattern.REL16), OperandSizeMode.MODE16, new int[]{0x0F, 0x88});
			MODE32_TABLE.add("jns", opPat(OperandPattern.REL16), OperandSizeMode.MODE16, new int[]{0x0F, 0x89});
			MODE32_TABLE.add("jp|jpe", opPat(OperandPattern.REL16), OperandSizeMode.MODE16, new int[]{0x0F, 0x8A});
			MODE32_TABLE.add("jnp|jpo", opPat(OperandPattern.REL16), OperandSizeMode.MODE16, new int[]{0x0F, 0x8B});
			MODE32_TABLE.add("jl|jnge", opPat(OperandPattern.REL16), OperandSizeMode.MODE16, new int[]{0x0F, 0x8C});
			MODE32_TABLE.add("jnl|jge", opPat(OperandPattern.REL16), OperandSizeMode.MODE16, new int[]{0x0F, 0x8D});
			MODE32_TABLE.add("jle|jng", opPat(OperandPattern.REL16), OperandSizeMode.MODE16, new int[]{0x0F, 0x8E});
			MODE32_TABLE.add("jnle|jg", opPat(OperandPattern.REL16), OperandSizeMode.MODE16, new int[]{0x0F, 0x8F});
			MODE32_TABLE.add("jo", opPat(OperandPattern.REL32), OperandSizeMode.MODE32, new int[]{0x0F, 0x80});
			MODE32_TABLE.add("jno", opPat(OperandPattern.REL32), OperandSizeMode.MODE32, new int[]{0x0F, 0x81});
			MODE32_TABLE.add("jb|jnae|jc", opPat(OperandPattern.REL32), OperandSizeMode.MODE32, new int[]{0x0F, 0x82});
			MODE32_TABLE.add("jnb|jae|jnc", opPat(OperandPattern.REL32), OperandSizeMode.MODE32, new int[]{0x0F, 0x83});
			MODE32_TABLE.add("jz|je", opPat(OperandPattern.REL32), OperandSizeMode.MODE32, new int[]{0x0F, 0x84});
			MODE32_TABLE.add("jnz|jne", opPat(OperandPattern.REL32), OperandSizeMode.MODE32, new int[]{0x0F, 0x85});
			MODE32_TABLE.add("jbe|jna", opPat(OperandPattern.REL32), OperandSizeMode.MODE32, new int[]{0x0F, 0x86});
			MODE32_TABLE.add("jnbe|ja", opPat(OperandPattern.REL32), OperandSizeMode.MODE32, new int[]{0x0F, 0x87});
			MODE32_TABLE.add("js", opPat(OperandPattern.REL32), OperandSizeMode.MODE32, new int[]{0x0F, 0x88});
			MODE32_TABLE.add("jns", opPat(OperandPattern.REL32), OperandSizeMode.MODE32, new int[]{0x0F, 0x89});
			MODE32_TABLE.add("jp|jpe", opPat(OperandPattern.REL32), OperandSizeMode.MODE32, new int[]{0x0F, 0x8A});
			MODE32_TABLE.add("jnp|jpo", opPat(OperandPattern.REL32), OperandSizeMode.MODE32, new int[]{0x0F, 0x8B});
			MODE32_TABLE.add("jl|jnge", opPat(OperandPattern.REL32), OperandSizeMode.MODE32, new int[]{0x0F, 0x8C});
			MODE32_TABLE.add("jnl|jge", opPat(OperandPattern.REL32), OperandSizeMode.MODE32, new int[]{0x0F, 0x8D});
			MODE32_TABLE.add("jle|jng", opPat(OperandPattern.REL32), OperandSizeMode.MODE32, new int[]{0x0F, 0x8E});
			MODE32_TABLE.add("jnle|jg", opPat(OperandPattern.REL32), OperandSizeMode.MODE32, new int[]{0x0F, 0x8F});
			MODE32_TABLE.add("pushw", opPat(OperandPattern.FS), OperandSizeMode.MODELESS, new int[]{0x0F, 0xA0});
			MODE32_TABLE.add("popw", opPat(OperandPattern.FS), OperandSizeMode.MODELESS, new int[]{0x0F, 0xA1});
			MODE32_TABLE.add("cpuid", opPat(), OperandSizeMode.MODELESS, new int[]{0x0F, 0xA2});
			MODE32_TABLE.add("btw", opPat(OperandPattern.RM16, OperandPattern.REG16), OperandSizeMode.MODE16, new int[]{0x0F, 0xA3}, new ModRM(0, 1));
			MODE32_TABLE.add("btl", opPat(OperandPattern.RM32, OperandPattern.REG32), OperandSizeMode.MODE32, new int[]{0x0F, 0xA3}, new ModRM(0, 1));
			MODE32_TABLE.add("shldw", opPat(OperandPattern.RM16, OperandPattern.REG16, OperandPattern.IMM8), OperandSizeMode.MODE16, new int[]{0x0F, 0xA4}, new ModRM(0, 1));
			MODE32_TABLE.add("shldl", opPat(OperandPattern.RM32, OperandPattern.REG32, OperandPattern.IMM8), OperandSizeMode.MODE32, new int[]{0x0F, 0xA4}, new ModRM(0, 1));
			MODE32_TABLE.add("shldw", opPat(OperandPattern.RM16, OperandPattern.REG16, OperandPattern.CL), OperandSizeMode.MODE16, new int[]{0x0F, 0xA5}, new ModRM(0, 1));
			MODE32_TABLE.add("shldl", opPat(OperandPattern.RM32, OperandPattern.REG32, OperandPattern.CL), OperandSizeMode.MODE32, new int[]{0x0F, 0xA5}, new ModRM(0, 1));
			MODE32_TABLE.add("pushw", opPat(OperandPattern.GS), OperandSizeMode.MODELESS, new int[]{0x0F, 0xA8});
			MODE32_TABLE.add("popw", opPat(OperandPattern.GS), OperandSizeMode.MODELESS, new int[]{0x0F, 0xA9});
			MODE32_TABLE.add("btsw", opPat(OperandPattern.RM16, OperandPattern.REG16), OperandSizeMode.MODE16, new int[]{0x0F, 0xAB}, new ModRM(0, 1));
			MODE32_TABLE.add("btsl", opPat(OperandPattern.RM32, OperandPattern.REG32), OperandSizeMode.MODE32, new int[]{0x0F, 0xAB}, new ModRM(0, 1));
			MODE32_TABLE.add("shrdw", opPat(OperandPattern.RM16, OperandPattern.REG16, OperandPattern.IMM8), OperandSizeMode.MODE16, new int[]{0x0F, 0xAC}, new ModRM(0, 1));
			MODE32_TABLE.add("shrdl", opPat(OperandPattern.RM32, OperandPattern.REG32, OperandPattern.IMM8), OperandSizeMode.MODE32, new int[]{0x0F, 0xAC}, new ModRM(0, 1));
			MODE32_TABLE.add("shrdw", opPat(OperandPattern.RM16, OperandPattern.REG16, OperandPattern.CL), OperandSizeMode.MODE16, new int[]{0x0F, 0xAD}, new ModRM(0, 1));
			MODE32_TABLE.add("shrdl", opPat(OperandPattern.RM32, OperandPattern.REG32, OperandPattern.CL), OperandSizeMode.MODE32, new int[]{0x0F, 0xAD}, new ModRM(0, 1));
			MODE32_TABLE.add("imulw", opPat(OperandPattern.REG16, OperandPattern.RM16), OperandSizeMode.MODE16, new int[]{0x0F, 0xAF}, new ModRM(1, 0));
			MODE32_TABLE.add("imull", opPat(OperandPattern.REG32, OperandPattern.RM32), OperandSizeMode.MODE32, new int[]{0x0F, 0xAF}, new ModRM(1, 0));
			MODE32_TABLE.add("cmpxchgb", opPat(OperandPattern.RM8, OperandPattern.REG8), OperandSizeMode.MODELESS, new int[]{0x0F, 0xB0}, new ModRM(0, 1));
			MODE32_TABLE.add("cmpxchgw", opPat(OperandPattern.RM16, OperandPattern.REG16), OperandSizeMode.MODE16, new int[]{0x0F, 0xB1}, new ModRM(0, 1));
			MODE32_TABLE.add("cmpxchgl", opPat(OperandPattern.RM32, OperandPattern.REG32), OperandSizeMode.MODE32, new int[]{0x0F, 0xB1}, new ModRM(0, 1));
			MODE32_TABLE.add("lssw", opPat(OperandPattern.REG16, OperandPattern.MEM), OperandSizeMode.MODE16, new int[]{0x0F, 0xB2}, new ModRM(1, 0));
			MODE32_TABLE.add("lssl", opPat(OperandPattern.REG32, OperandPattern.MEM), OperandSizeMode.MODE32, new int[]{0x0F, 0xB2}, new ModRM(1, 0));
			MODE32_TABLE.add("btrw", opPat(OperandPattern.RM16, OperandPattern.REG16), OperandSizeMode.MODE16, new int[]{0x0F, 0xB3}, new ModRM(0, 1));
			MODE32_TABLE.add("btrl", opPat(OperandPattern.RM32, OperandPattern.REG32), OperandSizeMode.MODE32, new int[]{0x0F, 0xB3}, new ModRM(0, 1));
			MODE32_TABLE.add("lfsw", opPat(OperandPattern.REG16, OperandPattern.MEM), OperandSizeMode.MODE16, new int[]{0x0F, 0xB4}, new ModRM(1, 0));
			MODE32_TABLE.add("lfsl", opPat(OperandPattern.REG32, OperandPattern.MEM), OperandSizeMode.MODE32, new int[]{0x0F, 0xB4}, new ModRM(1, 0));
			MODE32_TABLE.add("lgsw", opPat(OperandPattern.REG16, OperandPattern.MEM), OperandSizeMode.MODE16, new int[]{0x0F, 0xB5}, new ModRM(1, 0));
			MODE32_TABLE.add("lgsl", opPat(OperandPattern.REG32, OperandPattern.MEM), OperandSizeMode.MODE32, new int[]{0x0F, 0xB5}, new ModRM(1, 0));
			MODE32_TABLE.add("movzxwb", opPat(OperandPattern.REG16, OperandPattern.RM8), OperandSizeMode.MODE16, new int[]{0x0F, 0xB6}, new ModRM(1, 0));
			MODE32_TABLE.add("movzxlb", opPat(OperandPattern.REG32, OperandPattern.RM8), OperandSizeMode.MODE32, new int[]{0x0F, 0xB6}, new ModRM(1, 0));
			MODE32_TABLE.add("movzxww", opPat(OperandPattern.REG16, OperandPattern.RM16), OperandSizeMode.MODE16, new int[]{0x0F, 0xB7}, new ModRM(1, 0));
			MODE32_TABLE.add("movzxlw", opPat(OperandPattern.REG32, OperandPattern.RM16), OperandSizeMode.MODE32, new int[]{0x0F, 0xB7}, new ModRM(1, 0));
			MODE32_TABLE.add("btw", opPat(OperandPattern.RM16, OperandPattern.IMM8), OperandSizeMode.MODE16, new int[]{0x0F, 0xBA}, new ModRM(0, 14));
			MODE32_TABLE.add("btl", opPat(OperandPattern.RM32, OperandPattern.IMM8), OperandSizeMode.MODE32, new int[]{0x0F, 0xBA}, new ModRM(0, 14));
			MODE32_TABLE.add("btsw", opPat(OperandPattern.RM16, OperandPattern.IMM8), OperandSizeMode.MODE16, new int[]{0x0F, 0xBA}, new ModRM(0, 15));
			MODE32_TABLE.add("btsl", opPat(OperandPattern.RM32, OperandPattern.IMM8), OperandSizeMode.MODE32, new int[]{0x0F, 0xBA}, new ModRM(0, 15));
			MODE32_TABLE.add("btrw", opPat(OperandPattern.RM16, OperandPattern.IMM8), OperandSizeMode.MODE16, new int[]{0x0F, 0xBA}, new ModRM(0, 16));
			MODE32_TABLE.add("btrl", opPat(OperandPattern.RM32, OperandPattern.IMM8), OperandSizeMode.MODE32, new int[]{0x0F, 0xBA}, new ModRM(0, 16));
			MODE32_TABLE.add("btcw", opPat(OperandPattern.RM16, OperandPattern.IMM8), OperandSizeMode.MODE16, new int[]{0x0F, 0xBA}, new ModRM(0, 17));
			MODE32_TABLE.add("btcl", opPat(OperandPattern.RM32, OperandPattern.IMM8), OperandSizeMode.MODE32, new int[]{0x0F, 0xBA}, new ModRM(0, 17));
			MODE32_TABLE.add("btcw", opPat(OperandPattern.RM16, OperandPattern.REG16), OperandSizeMode.MODE16, new int[]{0x0F, 0xBB}, new ModRM(0, 1));
			MODE32_TABLE.add("btcl", opPat(OperandPattern.RM32, OperandPattern.REG32), OperandSizeMode.MODE32, new int[]{0x0F, 0xBB}, new ModRM(0, 1));
			MODE32_TABLE.add("bsfw", opPat(OperandPattern.REG16, OperandPattern.RM16), OperandSizeMode.MODE16, new int[]{0x0F, 0xBC}, new ModRM(1, 0));
			MODE32_TABLE.add("bsfl", opPat(OperandPattern.REG32, OperandPattern.RM32), OperandSizeMode.MODE32, new int[]{0x0F, 0xBC}, new ModRM(1, 0));
			MODE32_TABLE.add("bsrw", opPat(OperandPattern.REG16, OperandPattern.RM16), OperandSizeMode.MODE16, new int[]{0x0F, 0xBD}, new ModRM(1, 0));
			MODE32_TABLE.add("bsrl", opPat(OperandPattern.REG32, OperandPattern.RM32), OperandSizeMode.MODE32, new int[]{0x0F, 0xBD}, new ModRM(1, 0));
			MODE32_TABLE.add("movsxwb", opPat(OperandPattern.REG16, OperandPattern.RM8), OperandSizeMode.MODE16, new int[]{0x0F, 0xBE}, new ModRM(1, 0));
			MODE32_TABLE.add("movsxlb", opPat(OperandPattern.REG32, OperandPattern.RM8), OperandSizeMode.MODE32, new int[]{0x0F, 0xBE}, new ModRM(1, 0));
			MODE32_TABLE.add("movsxww", opPat(OperandPattern.REG16, OperandPattern.RM16), OperandSizeMode.MODE16, new int[]{0x0F, 0xBF}, new ModRM(1, 0));
			MODE32_TABLE.add("movsxlw", opPat(OperandPattern.REG32, OperandPattern.RM16), OperandSizeMode.MODE32, new int[]{0x0F, 0xBF}, new ModRM(1, 0));
			MODE32_TABLE.add("xaddb", opPat(OperandPattern.RM8, OperandPattern.REG8), OperandSizeMode.MODELESS, new int[]{0x0F, 0xC0}, new ModRM(0, 1));
			MODE32_TABLE.add("xaddw", opPat(OperandPattern.RM16, OperandPattern.REG16), OperandSizeMode.MODE16, new int[]{0x0F, 0xC1}, new ModRM(0, 1));
			MODE32_TABLE.add("xaddl", opPat(OperandPattern.RM32, OperandPattern.REG32), OperandSizeMode.MODE32, new int[]{0x0F, 0xC1}, new ModRM(0, 1));
			MODE32_TABLE.add("cmpxchg8b", opPat(OperandPattern.MEM), OperandSizeMode.MODELESS, new int[]{0x0F, 0xC7}, new ModRM(0, 11));
			MODE32_TABLE.add("bswapw", opPat(OperandPattern.REG16), OperandSizeMode.MODE16, new int[]{0x0F, 0xC8}, new RegisterInOpcode(0));
			MODE32_TABLE.add("bswapl", opPat(OperandPattern.REG32), OperandSizeMode.MODE32, new int[]{0x0F, 0xC8}, new RegisterInOpcode(0));
		}



		private HashSet<InstructionPattern> patterns;

		private IDictionary<string, HashSet<InstructionPattern>> patternsByMnemonic;



		/// <summary>
		/// Constructs an empty instruction pattern table.
		/// </summary>
		private InstructionPatternTable()
		{
			patterns = new HashSet<InstructionPattern>();
			patternsByMnemonic = new Dictionary<string, HashSet<InstructionPattern>>();
		}



		/// <summary>
		/// Adds the specified instruction pattern to this table. </summary>
		/// <param name="pat"> the instruction pattern to add to this table </param>
		/// <exception cref="ArgumentNullException"> if the instruction pattern is {@code null} </exception>
		private void add(InstructionPattern pat)
		{
			if (pat == null)
			{
				throw new ArgumentNullException();
			}
			patterns.Add(pat);

			if (!patternsByMnemonic.ContainsKey(pat.mnemonic))
			{
				patternsByMnemonic[pat.mnemonic] = new HashSet<InstructionPattern>();
			}
			patternsByMnemonic[pat.mnemonic].Add(pat);
		}


		/// <summary>
		/// Adds the specified instruction pattern to this table. Each entry in the opcode array must be in the range [0x00, 0xFF]. </summary>
		/// <param name="mnemonics"> the mnemonics, separated by vertical bar (|) </param>
		/// <param name="operands"> the list of operand patterns </param>
		/// <param name="operandSizeMode"> the operand size mode </param>
		/// <param name="opcodes"> the opcodes </param>
		/// <param name="options"> the list of options </param>
		private void add(string mnemonics, OperandPattern[] operands, OperandSizeMode operandSizeMode, int[] opcodes, params InstructionOption[] options)
		{
			foreach (string mnemonic in mnemonics.Split('|'))
			{
				add(new InstructionPattern(mnemonic, operands, operandSizeMode, opcodes, options));
			}
		}


		/// <summary>
		/// A convenience method that returns the specified varargs as an array. </summary>
		/// <param name="operands"> the list of operand patterns </param>
		/// <returns> the list of operand patterns </returns>
		/// <exception cref="ArgumentNullException"> if the list of operand patterns is {@code null} </exception>
		private static OperandPattern[] opPat(params OperandPattern[] operands)
		{
			if (operands == null)
			{
				throw new ArgumentNullException();
			}
			return operands;
		}


		/// <summary>
		/// Returns an instruction pattern in this table that matches the specified mnemonic and that best matches list of operands. </summary>
		/// <param name="mnemonic"> the mnemonic </param>
		/// <param name="operands"> the list of operands </param>
		/// <returns> an instruction pattern that matches the mnemonic and operands </returns>
		public virtual InstructionPattern match(string mnemonic, IList<Operand> operands)
		{
			if (mnemonic == null || operands == null)
			{
				throw new ArgumentNullException();
			}
			if (!patternsByMnemonic.ContainsKey(mnemonic))
			{
				throw new System.ArgumentException("Invalid mnemonic: " + mnemonic);
			}

			InstructionPattern bestmatch = null;
			foreach (InstructionPattern pat in patternsByMnemonic[mnemonic])
			{
				if (matches(pat, operands) && (bestmatch == null || isBetterMatch(pat, bestmatch, operands)))
				{
					bestmatch = pat;
				}
			}

			if (bestmatch != null)
			{
				return bestmatch;
			}
			else
			{
				throw new System.ArgumentException("No match: " + mnemonic);
			}
		}


		private static bool matches(InstructionPattern pat, IList<Operand> operands)
		{
			if (pat.operands.Count != operands.Count)
			{
				return false;
			}
			for (int i = 0; i < pat.operands.Count && i < operands.Count; i++)
			{
				if (!pat.operands[i].matches(operands[i]))
				{
					return false;
				}
			}
			return true;
		}


		private static bool isBetterMatch(InstructionPattern x, InstructionPattern y, IList<Operand> operands)
		{
			bool isbetter = false;
			bool isworse = false;

			for (int i = 0; i < operands.Count; i++)
			{
				if (x.operands[i] == OperandPattern.REL8 || x.operands[i] == OperandPattern.REL16 || x.operands[i] == OperandPattern.REL32)
				{
					// Wider is better
					isbetter |= isWider(x.operands[i], y.operands[i]);
					isworse |= isWider(y.operands[i], x.operands[i]);
				}
			}

			return !isworse && isbetter;
		}


		private static bool isWider(OperandPattern x, OperandPattern y)
		{
			return getWidth(x) > getWidth(y);
		}


		private static int getWidth(OperandPattern op)
		{
			if (op == null)
			{
				throw new ArgumentNullException();
			}
			if (op == OperandPattern.REL8)
			{
				return 8;
			}
			else if (op == OperandPattern.REL16)
			{
				return 16;
			}
			else if (op == OperandPattern.REL32)
			{
				return 32;
			}
			else
			{
				throw new System.ArgumentException("Not applicable to operand");
			}
		}

	}

}