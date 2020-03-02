using System;

namespace X86Asm.generator {

    using X86Asm.ast.operand;

    public abstract class OperandPattern {

        /// <summary>
        /// imm8(8bit���l)�p�^�[���N���X
        /// </summary>
        private class OperandPatternIMM8 : OperandPattern {
            public override bool matches(IOperand op) {
                return op is Label || op is ImmediateValue && ((ImmediateValue)op).IsUInt8();
            }

            public OperandPatternIMM8() : base("imm8") { }
        }
        /// <summary>
        /// imm8(8bit���l)�p�^�[��
        /// </summary>
        public static OperandPattern IMM8 = new OperandPatternIMM8();

        /// <summary>
        /// imm16(16bit���l)�p�^�[���N���X
        /// </summary>
        private class OperandPatternIMM16 : OperandPattern {
            public override bool matches(IOperand op) {
                return op is Label || op is ImmediateValue && ((ImmediateValue)op).IsUInt16();
            }
            public OperandPatternIMM16() : base("imm16") { }
        }
        /// <summary>
        /// imm16(16bit���l)�p�^�[��
        /// </summary>
        public static OperandPattern IMM16 = new OperandPatternIMM16();

        /// <summary>
        /// imm32(32bit���l)�p�^�[���N���X
        /// </summary>
        private class OperandPatternIMM32 : OperandPattern {
            public override bool matches(IOperand op) {
                return op is Label || op is ImmediateValue;
            }
            public OperandPatternIMM32() : base("imm32") { }
        }
        /// <summary>
        /// imm32(32bit���l)�p�^�[��
        /// </summary>
        public static OperandPattern IMM32 = new OperandPatternIMM32();

        /// <summary>
        /// imm8S(8bit���l�Aimm8�ƈႢ�A���x���w��̓_��)�p�^�[���N���X
        /// </summary>
        private class OperandPatternIMM8S : OperandPattern {
            public override bool matches(IOperand op) {
                return op is ImmediateValue && ((ImmediateValue)op).IsInt8();
            }
            public OperandPatternIMM8S() : base("imm8s") { }
        }
        /// <summary>
        /// imm8S(8bit���l�Aimm8�ƈႢ�A���x���w��̓_��)�p�^�[��
        /// </summary>
        public static OperandPattern IMM8S = new OperandPatternIMM8S();

        /// <summary>
        /// REL8(8�r�b�g�̑��΃I�t�Z�b�g)�p�^�[��
        /// </summary>
        private class OperandPatternREL8 : OperandPattern {
            public override bool matches(IOperand op) {
                return op is IImmediate;
            }
            public OperandPatternREL8() : base("rel8") { }
        }
        /// <summary>
        /// REL8(8�r�b�g�̑��΃I�t�Z�b�g)�p�^�[��
        /// </summary>
        public static OperandPattern REL8 = new OperandPatternREL8();

        /// <summary>
        /// REL16(16�r�b�g�̑��΃I�t�Z�b�g)�p�^�[���N���X
        /// </summary>
        private class OperandPatternREL16 : OperandPattern {
            public override bool matches(IOperand op) {
                return op is IImmediate;
            }
            public OperandPatternREL16() : base("rel16") { }
        }
        /// <summary>
        /// REL16(16�r�b�g�̑��΃I�t�Z�b�g)�p�^�[��
        /// </summary>
        public static OperandPattern REL16 = new OperandPatternREL16();

        /// <summary>
        /// REL32(32�r�b�g�̑��΃I�t�Z�b�g)�p�^�[���N���X
        /// </summary>
        private class OperandPatternREL32 : OperandPattern {
            public override bool matches(IOperand op) {
                return op is IImmediate;
            }
            public OperandPatternREL32() : base("rel32") { }
        }
        /// <summary>
        /// REL32(32�r�b�g�̑��΃I�t�Z�b�g)�p�^�[��
        /// </summary>
        public static OperandPattern REL32 = new OperandPatternREL32();

        /// <summary>
        /// immVal1(���l1�I�y�����h)�p�^�[��
        /// </summary>
        public static OperandPattern IMM_VAL_1 = new LiteralOperandPattern(new ImmediateValue(1));

        /// <summary>
        /// immVal3(���l3�I�y�����h)�p�^�[��
        /// </summary>
        public static OperandPattern IMM_VAL_3 = new LiteralOperandPattern(new ImmediateValue(3));

        /// <summary>
        /// mem�i�������I�y�����h�j�p�^�[���N���X
        /// </summary>
        private class OperandPatternMEM : OperandPattern {
            public override bool matches(IOperand op) {
                return op is Memory;
            }
            public OperandPatternMEM() : base("mem") { }
        }
        /// <summary>
        /// mem�i�������I�y�����h�j�p�^�[��
        /// </summary>
        public static OperandPattern MEM = new OperandPatternMEM();

        /// <summary>
        /// RM8�i�������I�y�����h��������8bit���W�X�^�j�p�^�[���N���X
        /// </summary>
        private class OperandPatternRM8 : OperandPattern {
            public override bool matches(IOperand op) {
                return op is Register8 || op is Memory;
            }

            public OperandPatternRM8() : base("r/m8") {
            }
        }
        /// <summary>
        /// RM8�i�������I�y�����h��������8bit���W�X�^�j�p�^�[��
        /// </summary>
        public static OperandPattern RM8 = new OperandPatternRM8();

        /// <summary>
        /// RM16�i�������I�y�����h��������16bit���W�X�^�j�p�^�[���N���X
        /// </summary>
        private class OperandPatternRM16 : OperandPattern {
            public override bool matches(IOperand op) {
                return op is Register16 || op is Memory;
            }

            public OperandPatternRM16() : base("r/m16") {
            }
        }
        /// <summary>
        /// RM16�i�������I�y�����h��������16bit���W�X�^�j�p�^�[��
        /// </summary>
        public static OperandPattern RM16 = new OperandPatternRM16();

        /// <summary>
        /// RM32�i�������I�y�����h��������32bit���W�X�^�j�p�^�[���N���X
        /// </summary>
        private class OperandPatternRM32 : OperandPattern {
            public override bool matches(IOperand op) {
                return op is Register32 || op is Memory;
            }

            public OperandPatternRM32() : base("r/m32") {
            }
        }
        /// <summary>
        /// RM32�i�������I�y�����h��������32bit���W�X�^�j�p�^�[��
        /// </summary>
        public static OperandPattern RM32 = new OperandPatternRM32();

        /// <summary>
        /// Reg8�i8bit���W�X�^�j�p�^�[���N���X
        /// </summary>
        private class OperandPatternREG8 : OperandPattern {
            public override bool matches(IOperand op) {
                return op is Register8;
            }

            public OperandPatternREG8() : base("reg8") {
            }
        }
        /// <summary>
        /// Reg8�i8bit���W�X�^�j�p�^�[��
        /// </summary>
        public static OperandPattern REG8 = new OperandPatternREG8();

        /// <summary>
        /// Reg16�i16bit���W�X�^�j�p�^�[���N���X
        /// </summary>
        private class OperandPatternREG16 : OperandPattern {
            public override bool matches(IOperand op) {
                return op is Register16;
            }

            public OperandPatternREG16() : base("reg16") {
            }
        }
        /// <summary>
        /// Reg16�i16bit���W�X�^�j�p�^�[��
        /// </summary>
        public static OperandPattern REG16 = new OperandPatternREG16();

        /// <summary>
        /// Reg32�i32bit���W�X�^�j�p�^�[���N���X
        /// </summary>
        private class OperandPatternREG32 : OperandPattern {
            public override bool matches(IOperand op) {
                return op is Register32;
            }

            public OperandPatternREG32() : base("reg32") {
            }
        }
        /// <summary>
        /// Reg32�i32bit���W�X�^�j�p�^�[��
        /// </summary>
        public static OperandPattern REG32 = new OperandPatternREG32();

        /// <summary>
        /// SREG�i�Z�O�����g���W�X�^�j�p�^�[���N���X
        /// </summary>
        private class OperandPatternSREG : OperandPattern {
            public override bool matches(IOperand op) {
                return op is SegmentRegister;
            }

            public OperandPatternSREG() : base("sreg") {
            }
        }
        /// <summary>
        /// SREG�i�Z�O�����g���W�X�^�j�p�^�[��
        /// </summary>
        public static OperandPattern SREG = new OperandPatternSREG();

        /// <summary>
        /// AL�iAL���W�X�^�j�p�^�[��
        /// </summary>
        public static OperandPattern AL = new LiteralOperandPattern(Register8.AL);

        /// <summary>
        /// AH�iAH���W�X�^�j�p�^�[��
        /// </summary>
        public static OperandPattern AH = new LiteralOperandPattern(Register8.AH);

        /// <summary>
        /// BL�iBL���W�X�^�j�p�^�[��
        /// </summary>
        public static OperandPattern BL = new LiteralOperandPattern(Register8.BL);

        /// <summary>
        /// BH�iBH���W�X�^�j�p�^�[��
        /// </summary>
        public static OperandPattern BH = new LiteralOperandPattern(Register8.BH);

        /// <summary>
        /// CL�iCL���W�X�^�j�p�^�[��
        /// </summary>
        public static OperandPattern CL = new LiteralOperandPattern(Register8.CL);

        /// <summary>
        /// CH�iCH���W�X�^�j�p�^�[��
        /// </summary>
        public static OperandPattern CH = new LiteralOperandPattern(Register8.CH);

        /// <summary>
        /// DL�iDL���W�X�^�j�p�^�[��
        /// </summary>
        public static OperandPattern DL = new LiteralOperandPattern(Register8.DL);

        /// <summary>
        /// DH�iDH���W�X�^�j�p�^�[��
        /// </summary>
        public static OperandPattern DH = new LiteralOperandPattern(Register8.DH);

        /// <summary>
        /// AX�iAX���W�X�^�j�p�^�[��
        /// </summary>
        public static OperandPattern AX = new LiteralOperandPattern(Register16.AX);

        /// <summary>
        /// BX�iBX���W�X�^�j�p�^�[��
        /// </summary>
        public static OperandPattern BX = new LiteralOperandPattern(Register16.BX);

        /// <summary>
        /// CX�iCX���W�X�^�j�p�^�[��
        /// </summary>
        public static OperandPattern CX = new LiteralOperandPattern(Register16.CX);

        /// <summary>
        /// DX�iDX���W�X�^�j�p�^�[��
        /// </summary>
        public static OperandPattern DX = new LiteralOperandPattern(Register16.DX);

        /// <summary>
        /// SP�iSP���W�X�^�j�p�^�[��
        /// </summary>
        public static OperandPattern SP = new LiteralOperandPattern(Register16.SP);

        /// <summary>
        /// BP�iBP���W�X�^�j�p�^�[��
        /// </summary>
        public static OperandPattern BP = new LiteralOperandPattern(Register16.BP);

        /// <summary>
        /// SI�iSI���W�X�^�j�p�^�[��
        /// </summary>
        public static OperandPattern SI = new LiteralOperandPattern(Register16.SI);

        /// <summary>
        /// DI�iDI���W�X�^�j�p�^�[��
        /// </summary>
        public static OperandPattern DI = new LiteralOperandPattern(Register16.DI);

        /// <summary>
        /// EAX�iEAX���W�X�^�j�p�^�[��
        /// </summary>
        public static OperandPattern EAX = new LiteralOperandPattern(Register32.EAX);

        /// <summary>
        /// EBX�iEBX���W�X�^�j�p�^�[��
        /// </summary>
        public static OperandPattern EBX = new LiteralOperandPattern(Register32.EBX);

        /// <summary>
        /// ECX�iECX���W�X�^�j�p�^�[��
        /// </summary>
        public static OperandPattern ECX = new LiteralOperandPattern(Register32.ECX);

        /// <summary>
        /// EDX�iEDX���W�X�^�j�p�^�[��
        /// </summary>
        public static OperandPattern EDX = new LiteralOperandPattern(Register32.EDX);

        /// <summary>
        /// ESP�iESP���W�X�^�j�p�^�[��
        /// </summary>
        public static OperandPattern ESP = new LiteralOperandPattern(Register32.ESP);

        /// <summary>
        /// EBP�iEBP���W�X�^�j�p�^�[��
        /// </summary>
        public static OperandPattern EBP = new LiteralOperandPattern(Register32.EBP);

        /// <summary>
        /// ESI�iESI���W�X�^�j�p�^�[��
        /// </summary>
        public static OperandPattern ESI = new LiteralOperandPattern(Register32.ESI);

        /// <summary>
        /// EDI�iEDI���W�X�^�j�p�^�[��
        /// </summary>
        public static OperandPattern EDI = new LiteralOperandPattern(Register32.EDI);

        /// <summary>
        /// CS�iCS���W�X�^�j�p�^�[��
        /// </summary>
        public static OperandPattern CS = new LiteralOperandPattern(SegmentRegister.CS);

        /// <summary>
        /// DS�iDS���W�X�^�j�p�^�[��
        /// </summary>
        public static OperandPattern DS = new LiteralOperandPattern(SegmentRegister.DS);

        /// <summary>
        /// ES�iES���W�X�^�j�p�^�[��
        /// </summary>
        public static OperandPattern ES = new LiteralOperandPattern(SegmentRegister.ES);

        /// <summary>
        /// FS�iFS���W�X�^�j�p�^�[��
        /// </summary>
        public static OperandPattern FS = new LiteralOperandPattern(SegmentRegister.FS);

        /// <summary>
        /// GS�iGS���W�X�^�j�p�^�[��
        /// </summary>
        public static OperandPattern GS = new LiteralOperandPattern(SegmentRegister.GS);

        /// <summary>
        /// SS�iSS���W�X�^�j�p�^�[��
        /// </summary>
        public static OperandPattern SS = new LiteralOperandPattern(SegmentRegister.SS);

        private string name;

        public OperandPattern(string name) {
            if (name == null) {
                throw new ArgumentNullException();
            }
            this.name = name;
        }

        public abstract bool matches(IOperand operand);


        /// <summary>
        /// Returns a string representation of this operand pattern. The format is subjected to change. </summary>
        /// <returns> a string representation of this operand pattern </returns>
        public override string ToString() {
            return name;
        }

    }

}