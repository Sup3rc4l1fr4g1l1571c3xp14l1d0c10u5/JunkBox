using System;

namespace X86Asm.generator
{

	using Immediate = X86Asm.operand.Immediate;
	using ImmediateValue = X86Asm.operand.ImmediateValue;
	using Label = X86Asm.operand.Label;
	using Memory = X86Asm.operand.Memory;
	using Operand = X86Asm.operand.Operand;
	using Register16 = X86Asm.operand.Register16;
	using Register32 = X86Asm.operand.Register32;
	using Register8 = X86Asm.operand.Register8;
	using SegmentRegister = X86Asm.operand.SegmentRegister;


	public abstract class OperandPattern {

		public static OperandPattern IMM8 = new OperandPatternIMM8();

		private class OperandPatternIMM8 : OperandPattern
		{
			public override bool matches(Operand op)
			{
				return op is Label || op is ImmediateValue && ((ImmediateValue)op).is8Bit();
			}

            public OperandPatternIMM8() : base("imm8") {}
        }
		public static OperandPattern IMM16 = new OperandPatternIMM16();

		private class OperandPatternIMM16 : OperandPattern
		{
			public override bool matches(Operand op)
			{
				return op is Label || op is ImmediateValue && ((ImmediateValue)op).is16Bit();
			}
            public OperandPatternIMM16() : base("imm16") {}
		}
		public static OperandPattern IMM32 = new OperandPatternIMM32();

		private class OperandPatternIMM32 : OperandPattern
		{
			public override bool matches(Operand op)
			{
				return op is Label || op is ImmediateValue;
			}
            public OperandPatternIMM32() : base("imm32") {}
		}

		public static OperandPattern IMM8S = new OperandPatternIMM8S();

		private class OperandPatternIMM8S : OperandPattern
		{
			public override bool matches(Operand op)
			{
				return op is ImmediateValue && ((ImmediateValue)op).Signed8Bit;
			}
            public OperandPatternIMM8S() : base("imm8s") {}
		}

		public static OperandPattern REL8 = new OperandPatternREL8();

		private class OperandPatternREL8 : OperandPattern
		{
			public override bool matches(Operand op)
			{
				return op is Immediate;
			}
            public OperandPatternREL8() : base("rel8") {}
		}
		public static OperandPattern REL16 = new OperandPatternREL16();

		private class OperandPatternREL16 : OperandPattern
		{
			public override bool matches(Operand op)
			{
				return op is Immediate;
			}
            public OperandPatternREL16() : base("rel16") {}
		}
		public static OperandPattern REL32 = new OperandPatternREL32();

		private class OperandPatternREL32 : OperandPattern
		{
			public override bool matches(Operand op)
			{
				return op is Immediate;
			}
            public OperandPatternREL32() : base("rel32") {}
		}

		public static OperandPattern IMM_VAL_1 = new LiteralOperandPattern(new ImmediateValue(1));
		public static OperandPattern IMM_VAL_3 = new LiteralOperandPattern(new ImmediateValue(3));

		public static OperandPattern MEM = new OperandPatternRM8();

		private class OperandPatternRM8 : OperandPattern
		{
			public override bool matches(Operand op)
			{
				return op is Memory;
			}
            public OperandPatternRM8() : base("mem") {}
		}

		public static OperandPattern RM8 = new OperandPatternAnonymousInnerClassHelper9();

		private class OperandPatternAnonymousInnerClassHelper9 : OperandPattern
		{
			public override bool matches(Operand op)
			{
				return op is Register8 || op is Memory;
			}

            public OperandPatternAnonymousInnerClassHelper9() : base("r/m8") {
            }
        }
		public static OperandPattern RM16 = new OperandPatternRM16();

		private class OperandPatternRM16 : OperandPattern
		{
			public override bool matches(Operand op)
			{
				return op is Register16 || op is Memory;
			}

            public OperandPatternRM16() : base("r/m16") {
            }
        }
		public static OperandPattern RM32 = new OperandPatternRM32();

		private class OperandPatternRM32 : OperandPattern
		{
			public override bool matches(Operand op)
			{
				return op is Register32 || op is Memory;
			}

            public OperandPatternRM32() : base("r/m32") {
            }
        }

		public static OperandPattern REG8 = new OperandPatternREG8();

		private class OperandPatternREG8 : OperandPattern
		{
			public override bool matches(Operand op)
			{
				return op is Register8;
			}

            public OperandPatternREG8() : base("reg8") {
            }
        }
		public static OperandPattern REG16 = new OperandPatternREG16();

		private class OperandPatternREG16 : OperandPattern
		{
			public override bool matches(Operand op)
			{
				return op is Register16;
			}

            public OperandPatternREG16() : base("reg16") {
            }
        }
		public static OperandPattern REG32 = new OperandPatternREG32();

		private class OperandPatternREG32 : OperandPattern
		{
			public override bool matches(Operand op)
			{
				return op is Register32;
			}

            public OperandPatternREG32() : base("reg32") {
            }
        }
		public static OperandPattern SREG = new OperandPatternSREG();

		private class OperandPatternSREG : OperandPattern
		{
			public override bool matches(Operand op) {
				return op is SegmentRegister;
			}

            public OperandPatternSREG() : base("sreg") {
            }
        }

		public static OperandPattern AL = new LiteralOperandPattern(Register8.AL);
		public static OperandPattern AH = new LiteralOperandPattern(Register8.AH);
		public static OperandPattern BL = new LiteralOperandPattern(Register8.BL);
		public static OperandPattern BH = new LiteralOperandPattern(Register8.BH);
		public static OperandPattern CL = new LiteralOperandPattern(Register8.CL);
		public static OperandPattern CH = new LiteralOperandPattern(Register8.CH);
		public static OperandPattern DL = new LiteralOperandPattern(Register8.DL);
		public static OperandPattern DH = new LiteralOperandPattern(Register8.DH);

		public static OperandPattern AX = new LiteralOperandPattern(Register16.AX);
		public static OperandPattern BX = new LiteralOperandPattern(Register16.BX);
		public static OperandPattern CX = new LiteralOperandPattern(Register16.CX);
		public static OperandPattern DX = new LiteralOperandPattern(Register16.DX);
		public static OperandPattern SP = new LiteralOperandPattern(Register16.SP);
		public static OperandPattern BP = new LiteralOperandPattern(Register16.BP);
		public static OperandPattern SI = new LiteralOperandPattern(Register16.SI);
		public static OperandPattern DI = new LiteralOperandPattern(Register16.DI);

		public static OperandPattern EAX = new LiteralOperandPattern(Register32.EAX);
		public static OperandPattern EBX = new LiteralOperandPattern(Register32.EBX);
		public static OperandPattern ECX = new LiteralOperandPattern(Register32.ECX);
		public static OperandPattern EDX = new LiteralOperandPattern(Register32.EDX);
		public static OperandPattern ESP = new LiteralOperandPattern(Register32.ESP);
		public static OperandPattern EBP = new LiteralOperandPattern(Register32.EBP);
		public static OperandPattern ESI = new LiteralOperandPattern(Register32.ESI);
		public static OperandPattern EDI = new LiteralOperandPattern(Register32.EDI);

		public static OperandPattern CS = new LiteralOperandPattern(SegmentRegister.CS);
		public static OperandPattern DS = new LiteralOperandPattern(SegmentRegister.DS);
		public static OperandPattern ES = new LiteralOperandPattern(SegmentRegister.ES);
		public static OperandPattern FS = new LiteralOperandPattern(SegmentRegister.FS);
		public static OperandPattern GS = new LiteralOperandPattern(SegmentRegister.GS);
		public static OperandPattern SS = new LiteralOperandPattern(SegmentRegister.SS);



		private string name;



		public OperandPattern(string name)
		{
			if (name == null)
			{
				throw new ArgumentNullException();
			}
			this.name = name;
		}



		public abstract bool matches(Operand operand);


		/// <summary>
		/// Returns a string representation of this operand pattern. The format is subjected to change. </summary>
		/// <returns> a string representation of this operand pattern </returns>
		public override string ToString()
		{
			return name;
		}

	}

}