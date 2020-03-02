using System;

namespace X86Asm.generator {

    using X86Asm.ast.operand;

    public abstract class OperandPattern {

        /// <summary>
        /// imm8(8bit即値)パターンクラス
        /// </summary>
        private class OperandPatternIMM8 : OperandPattern {
            public override bool matches(IOperand op) {
                return op is Label || op is ImmediateValue && ((ImmediateValue)op).IsUInt8();
            }

            public OperandPatternIMM8() : base("imm8") { }
        }
        /// <summary>
        /// imm8(8bit即値)パターン
        /// </summary>
        public static OperandPattern IMM8 = new OperandPatternIMM8();

        /// <summary>
        /// imm16(16bit即値)パターンクラス
        /// </summary>
        private class OperandPatternIMM16 : OperandPattern {
            public override bool matches(IOperand op) {
                return op is Label || op is ImmediateValue && ((ImmediateValue)op).IsUInt16();
            }
            public OperandPatternIMM16() : base("imm16") { }
        }
        /// <summary>
        /// imm16(16bit即値)パターン
        /// </summary>
        public static OperandPattern IMM16 = new OperandPatternIMM16();

        /// <summary>
        /// imm32(32bit即値)パターンクラス
        /// </summary>
        private class OperandPatternIMM32 : OperandPattern {
            public override bool matches(IOperand op) {
                return op is Label || op is ImmediateValue;
            }
            public OperandPatternIMM32() : base("imm32") { }
        }
        /// <summary>
        /// imm32(32bit即値)パターン
        /// </summary>
        public static OperandPattern IMM32 = new OperandPatternIMM32();

        /// <summary>
        /// imm8S(8bit即値、imm8と違い、ラベル指定はダメ)パターンクラス
        /// </summary>
        private class OperandPatternIMM8S : OperandPattern {
            public override bool matches(IOperand op) {
                return op is ImmediateValue && ((ImmediateValue)op).IsInt8();
            }
            public OperandPatternIMM8S() : base("imm8s") { }
        }
        /// <summary>
        /// imm8S(8bit即値、imm8と違い、ラベル指定はダメ)パターン
        /// </summary>
        public static OperandPattern IMM8S = new OperandPatternIMM8S();

        /// <summary>
        /// REL8(8ビットの相対オフセット)パターン
        /// </summary>
        private class OperandPatternREL8 : OperandPattern {
            public override bool matches(IOperand op) {
                return op is IImmediate;
            }
            public OperandPatternREL8() : base("rel8") { }
        }
        /// <summary>
        /// REL8(8ビットの相対オフセット)パターン
        /// </summary>
        public static OperandPattern REL8 = new OperandPatternREL8();

        /// <summary>
        /// REL16(16ビットの相対オフセット)パターンクラス
        /// </summary>
        private class OperandPatternREL16 : OperandPattern {
            public override bool matches(IOperand op) {
                return op is IImmediate;
            }
            public OperandPatternREL16() : base("rel16") { }
        }
        /// <summary>
        /// REL16(16ビットの相対オフセット)パターン
        /// </summary>
        public static OperandPattern REL16 = new OperandPatternREL16();

        /// <summary>
        /// REL32(32ビットの相対オフセット)パターンクラス
        /// </summary>
        private class OperandPatternREL32 : OperandPattern {
            public override bool matches(IOperand op) {
                return op is IImmediate;
            }
            public OperandPatternREL32() : base("rel32") { }
        }
        /// <summary>
        /// REL32(32ビットの相対オフセット)パターン
        /// </summary>
        public static OperandPattern REL32 = new OperandPatternREL32();

        /// <summary>
        /// immVal1(即値1オペランド)パターン
        /// </summary>
        public static OperandPattern IMM_VAL_1 = new LiteralOperandPattern(new ImmediateValue(1));

        /// <summary>
        /// immVal3(即値3オペランド)パターン
        /// </summary>
        public static OperandPattern IMM_VAL_3 = new LiteralOperandPattern(new ImmediateValue(3));

        /// <summary>
        /// mem（メモリオペランド）パターンクラス
        /// </summary>
        private class OperandPatternMEM : OperandPattern {
            public override bool matches(IOperand op) {
                return op is Memory;
            }
            public OperandPatternMEM() : base("mem") { }
        }
        /// <summary>
        /// mem（メモリオペランド）パターン
        /// </summary>
        public static OperandPattern MEM = new OperandPatternMEM();

        /// <summary>
        /// RM8（メモリオペランドもしくは8bitレジスタ）パターンクラス
        /// </summary>
        private class OperandPatternRM8 : OperandPattern {
            public override bool matches(IOperand op) {
                return op is Register8 || op is Memory;
            }

            public OperandPatternRM8() : base("r/m8") {
            }
        }
        /// <summary>
        /// RM8（メモリオペランドもしくは8bitレジスタ）パターン
        /// </summary>
        public static OperandPattern RM8 = new OperandPatternRM8();

        /// <summary>
        /// RM16（メモリオペランドもしくは16bitレジスタ）パターンクラス
        /// </summary>
        private class OperandPatternRM16 : OperandPattern {
            public override bool matches(IOperand op) {
                return op is Register16 || op is Memory;
            }

            public OperandPatternRM16() : base("r/m16") {
            }
        }
        /// <summary>
        /// RM16（メモリオペランドもしくは16bitレジスタ）パターン
        /// </summary>
        public static OperandPattern RM16 = new OperandPatternRM16();

        /// <summary>
        /// RM32（メモリオペランドもしくは32bitレジスタ）パターンクラス
        /// </summary>
        private class OperandPatternRM32 : OperandPattern {
            public override bool matches(IOperand op) {
                return op is Register32 || op is Memory;
            }

            public OperandPatternRM32() : base("r/m32") {
            }
        }
        /// <summary>
        /// RM32（メモリオペランドもしくは32bitレジスタ）パターン
        /// </summary>
        public static OperandPattern RM32 = new OperandPatternRM32();

        /// <summary>
        /// Reg8（8bitレジスタ）パターンクラス
        /// </summary>
        private class OperandPatternREG8 : OperandPattern {
            public override bool matches(IOperand op) {
                return op is Register8;
            }

            public OperandPatternREG8() : base("reg8") {
            }
        }
        /// <summary>
        /// Reg8（8bitレジスタ）パターン
        /// </summary>
        public static OperandPattern REG8 = new OperandPatternREG8();

        /// <summary>
        /// Reg16（16bitレジスタ）パターンクラス
        /// </summary>
        private class OperandPatternREG16 : OperandPattern {
            public override bool matches(IOperand op) {
                return op is Register16;
            }

            public OperandPatternREG16() : base("reg16") {
            }
        }
        /// <summary>
        /// Reg16（16bitレジスタ）パターン
        /// </summary>
        public static OperandPattern REG16 = new OperandPatternREG16();

        /// <summary>
        /// Reg32（32bitレジスタ）パターンクラス
        /// </summary>
        private class OperandPatternREG32 : OperandPattern {
            public override bool matches(IOperand op) {
                return op is Register32;
            }

            public OperandPatternREG32() : base("reg32") {
            }
        }
        /// <summary>
        /// Reg32（32bitレジスタ）パターン
        /// </summary>
        public static OperandPattern REG32 = new OperandPatternREG32();

        /// <summary>
        /// SREG（セグメントレジスタ）パターンクラス
        /// </summary>
        private class OperandPatternSREG : OperandPattern {
            public override bool matches(IOperand op) {
                return op is SegmentRegister;
            }

            public OperandPatternSREG() : base("sreg") {
            }
        }
        /// <summary>
        /// SREG（セグメントレジスタ）パターン
        /// </summary>
        public static OperandPattern SREG = new OperandPatternSREG();

        /// <summary>
        /// AL（ALレジスタ）パターン
        /// </summary>
        public static OperandPattern AL = new LiteralOperandPattern(Register8.AL);

        /// <summary>
        /// AH（AHレジスタ）パターン
        /// </summary>
        public static OperandPattern AH = new LiteralOperandPattern(Register8.AH);

        /// <summary>
        /// BL（BLレジスタ）パターン
        /// </summary>
        public static OperandPattern BL = new LiteralOperandPattern(Register8.BL);

        /// <summary>
        /// BH（BHレジスタ）パターン
        /// </summary>
        public static OperandPattern BH = new LiteralOperandPattern(Register8.BH);

        /// <summary>
        /// CL（CLレジスタ）パターン
        /// </summary>
        public static OperandPattern CL = new LiteralOperandPattern(Register8.CL);

        /// <summary>
        /// CH（CHレジスタ）パターン
        /// </summary>
        public static OperandPattern CH = new LiteralOperandPattern(Register8.CH);

        /// <summary>
        /// DL（DLレジスタ）パターン
        /// </summary>
        public static OperandPattern DL = new LiteralOperandPattern(Register8.DL);

        /// <summary>
        /// DH（DHレジスタ）パターン
        /// </summary>
        public static OperandPattern DH = new LiteralOperandPattern(Register8.DH);

        /// <summary>
        /// AX（AXレジスタ）パターン
        /// </summary>
        public static OperandPattern AX = new LiteralOperandPattern(Register16.AX);

        /// <summary>
        /// BX（BXレジスタ）パターン
        /// </summary>
        public static OperandPattern BX = new LiteralOperandPattern(Register16.BX);

        /// <summary>
        /// CX（CXレジスタ）パターン
        /// </summary>
        public static OperandPattern CX = new LiteralOperandPattern(Register16.CX);

        /// <summary>
        /// DX（DXレジスタ）パターン
        /// </summary>
        public static OperandPattern DX = new LiteralOperandPattern(Register16.DX);

        /// <summary>
        /// SP（SPレジスタ）パターン
        /// </summary>
        public static OperandPattern SP = new LiteralOperandPattern(Register16.SP);

        /// <summary>
        /// BP（BPレジスタ）パターン
        /// </summary>
        public static OperandPattern BP = new LiteralOperandPattern(Register16.BP);

        /// <summary>
        /// SI（SIレジスタ）パターン
        /// </summary>
        public static OperandPattern SI = new LiteralOperandPattern(Register16.SI);

        /// <summary>
        /// DI（DIレジスタ）パターン
        /// </summary>
        public static OperandPattern DI = new LiteralOperandPattern(Register16.DI);

        /// <summary>
        /// EAX（EAXレジスタ）パターン
        /// </summary>
        public static OperandPattern EAX = new LiteralOperandPattern(Register32.EAX);

        /// <summary>
        /// EBX（EBXレジスタ）パターン
        /// </summary>
        public static OperandPattern EBX = new LiteralOperandPattern(Register32.EBX);

        /// <summary>
        /// ECX（ECXレジスタ）パターン
        /// </summary>
        public static OperandPattern ECX = new LiteralOperandPattern(Register32.ECX);

        /// <summary>
        /// EDX（EDXレジスタ）パターン
        /// </summary>
        public static OperandPattern EDX = new LiteralOperandPattern(Register32.EDX);

        /// <summary>
        /// ESP（ESPレジスタ）パターン
        /// </summary>
        public static OperandPattern ESP = new LiteralOperandPattern(Register32.ESP);

        /// <summary>
        /// EBP（EBPレジスタ）パターン
        /// </summary>
        public static OperandPattern EBP = new LiteralOperandPattern(Register32.EBP);

        /// <summary>
        /// ESI（ESIレジスタ）パターン
        /// </summary>
        public static OperandPattern ESI = new LiteralOperandPattern(Register32.ESI);

        /// <summary>
        /// EDI（EDIレジスタ）パターン
        /// </summary>
        public static OperandPattern EDI = new LiteralOperandPattern(Register32.EDI);

        /// <summary>
        /// CS（CSレジスタ）パターン
        /// </summary>
        public static OperandPattern CS = new LiteralOperandPattern(SegmentRegister.CS);

        /// <summary>
        /// DS（DSレジスタ）パターン
        /// </summary>
        public static OperandPattern DS = new LiteralOperandPattern(SegmentRegister.DS);

        /// <summary>
        /// ES（ESレジスタ）パターン
        /// </summary>
        public static OperandPattern ES = new LiteralOperandPattern(SegmentRegister.ES);

        /// <summary>
        /// FS（FSレジスタ）パターン
        /// </summary>
        public static OperandPattern FS = new LiteralOperandPattern(SegmentRegister.FS);

        /// <summary>
        /// GS（GSレジスタ）パターン
        /// </summary>
        public static OperandPattern GS = new LiteralOperandPattern(SegmentRegister.GS);

        /// <summary>
        /// SS（SSレジスタ）パターン
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