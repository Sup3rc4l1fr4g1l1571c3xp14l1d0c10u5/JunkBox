// parser is https://pegjs.org/online
module X86Instruction {
/*
entries = entry*

entry = opcode:opcode __ instruction:instruction _ description:description CRLF { return { opcode:opcode, instruction:instruction, description:description }; }

opcode
  = h:opcode_heads t:opcode_tails { return {bytes:h, options:t}; }

opcode_heads
  = x:opcode_head xs:(__ y:opcode_head {return y;})* { return Array.prototype.concat([x],xs); }

opcode_head
  = hex { return Number("0x"+text()); }

opcode_tails = x:opcode_tails_? { return (x == null) ? [] : x; }

opcode_tails_
  = _ x:opcode_tail xs:(__ y:opcode_tail {return y;})*  { return Array.prototype.concat([x],xs); }

opcode_tail
  = "/r"  { return text(); }
  / "/" [0-7]  { return text(); }
  / "i" [bwd]  { return text(); }
  / "+" "i"  { return text(); }
  / "+"? "r" [bwd]  { return text(); }
  / "c" [bwdp] { return text(); }
  / "m64"      { return text(); }
  / "00"       { return text(); }
  / "01"       { return text(); }

instruction = mnemonic:mnemonic __ op:(x:op xs:(_ "," _ y:op{return y;})* { return Array.prototype.concat([x],xs); } )? { return { mnemonic:mnemonic, operands:op == null ? [] : op }; }
mnemonic = [A-Z][A-Z0-9]* { return text(); }
op
  = "r/m" ("8"/"16"/"32")  { return text(); }
  / "r" ("8"/"16"/"32")  { return text(); }
  / "rel" ("8"/"32")  { return text(); }
  / "imm" ("8"/"16"/"32")  { return text(); }
  / "moffs" ("8"/"16"/"32")  { return text(); }
  / [ABCD][LH]  { return text(); }
  / [ABCD] "X"  { return text(); }
  / "E" [ABCD] "X"  { return text(); }
  / "Sreg"  { return text(); }
  / "ptr16:32"       { return text(); }
  / "mm/m64" { return text(); }
  / "mm" { return text(); }
  / "m16:32"       { return text(); }
  / "m16&32"       { return text(); }
  / "m2byte"       { return text(); }
  / "m14/28byte"       { return text(); }
  / "m94/108byte"       { return text(); }
  / "m80bcd"       { return text(); }
  / "m" ("16"/"32"/"64") "int"       { return text(); }
  / "m" ("32"/"64"/"80") "real"      { return text(); }
  / "m" ("8"/"16"/"32")       { return text(); }
  / "m"        { return text(); }
  / "0"       { return text(); }
  / "1"       { return text(); }
  / "ST(" [0i] ")" { return text(); }
  / "DR0-DR7"  { return text(); }
  / "CR"[0234]  { return text(); }

description = (!CRLF.)* { return text(); }

hex = x:[0-9A-Z]+ &{ return /^[0-9A-F]{2}$/.exec(String.prototype.concat(...x)) != null; } { return text(); }
__ = [ \t]+
_ = [ \t]*
CRLF = _ [\r\n]+

 */

/*
37           AAA                     ASCII adjust AL after addition
D5 0A        AAD                     ASCII adjust AX before division
D4 0A        AAM                     ASCII adjust AX after multiplication
3F           AAS                     ASCII adjust AL after subtraction
14       ib  ADC AL,imm8             Add with carry 
15       id  ADC EAX,imm32           Add with carry 
80    /2 ib  ADC r/m8,imm8           Add with carry 
81    /2 id  ADC r/m32,imm32         Add with carry
83    /2 ib  ADC r/m32,imm8          Add with carry
10    /r     ADC r/m8,r8             Add with carry 
11    /r     ADC r/m32,r32           Add with carry
12    /r     ADC r8,r/m8             Add with carry 
13    /r     ADC r32,r/m32           Add with carry
04       ib  ADD AL,imm8             Add 
05       id  ADD EAX,imm32           Add 
80    /0 ib  ADD r/m8,imm8           Add 
81    /0 id  ADD r/m32,imm32         Add 
83    /0 ib  ADD r/m32,imm8          Add 
00    /r     ADD r/m8,r8             ADD 
01    /r     ADD r/m32,r32           ADD 
02    /r     ADD r8,r/m8             ADD 
03    /r     ADD r32,r/m32           ADD 
24       ib  AND AL,imm8             AND 
25       id  AND EAX,imm32           AND
80    /4 ib  AND r/m8,imm8           AND
81    /4 id  AND r/m32,imm32         AND
83    /4 ib  AND r/m32,imm8          AND
20    /r     AND r/m8,r8             AND
21    /r     AND r/m32,r32           AND
22    /r     AND r8,r/m8             AND
23    /r     AND r32,r/m32           AND
63    /r     ARPL r/m16,r16          Adjust Request Privilege Level of Sel.
62    /r     BOUND r32,m32&32        Check Array Index Against Bounds
0F BC /r     BSF r32,r/m32           Bit scan forward on r/m32
0F BD /r     BSR r32,r/m32           Bit scan reverse on r/m32
0F C8+rd     BSWAP r32               Reverses the byte order of a r32
0F A3 /r     BT r/m32,r32            Bit Test
0F BA /4 ib  BT r/m32,imm8           Bit Test
0F BB /r     BTC r/m32,r32           Bit Test and Complement
0F BA /7 ib  BTC r/m32,imm8          Bit Test and Complement
0F B3 /r     BTR r/m32,r32           Bit Test and Clear
0F BA /6 ib  BTR r/m32,imm8          Bit Test and Clear
0F AB /r     BTS r/m32,r32           Bit Test and Set
0F BA /5 ib  BTS r/m32,imm8          Bit Test and Set
E8       cd  CALL rel32              Call near, rel to n.inst
FF    /2     CALL r/m32              Call near, abs.ind.add. given in r/m32
9A       cp  CALL ptr16:32           Call far, abs.add. given in operand
FF    /3     CALL m16:32             Call far, abs.ind.add. given in m16:32
98           CBW                     Convert Byte to Word
99           CWD                     Convert Word to Doubleword
99           CDQ                     Convert Doubleword to Quadword 
F8           CLC                     Clear CF flag
FC           CLD                     Clear DF flag
FA           CLI                     Clear interrupt flag
0F 06        CLTS                    Clear Task-Switched Flag in Control Reg. Zero
F5           CMC                     Complement CF flag
0F 47 /r     CMOVA r32,r/m32         Move if above 
0F 43 /r     CMOVAE r32,r/m32        Move if above or equal 
0F 42 /r     CMOVB r32,r/m32         Move if below 
0F 46 /r     CMOVBE r32,r/m32        Move if below or equal 
0F 42 /r     CMOVC r32,r/m32         Move if carry 
0F 44 /r     CMOVE r32,r/m32         Move if equal 
0F 4F /r     CMOVG r32,r/m32         Move if greater 
0F 4D /r     CMOVGE r32,r/m32        Move if greater or equal 
0F 4C /r     CMOVL r32,r/m32         Move if less 
0F 4E /r     CMOVLE r32,r/m32        Move if less or equal 
0F 46 /r     CMOVNA r32,r/m32        Move if not above 
0F 42 /r     CMOVNAE r32,r/m32       Move if not above or equal 
0F 43 /r     CMOVNB r32,r/m32        Move if not below 
0F 47 /r     CMOVNBE r32,r/m32       Move if not below or equal 
0F 43 /r     CMOVNC r32,r/m32        Move if not carry 
0F 45 /r     CMOVNE r32,r/m32        Move if not equal 
0F 4E /r     CMOVNG r32,r/m32        Move if not greater 
0F 4C /r     CMOVNGE r32,r/m32       Move if not greater or equal 
0F 4D /r     CMOVNL r32,r/m32        Move if not less 
0F 4F /r     CMOVNLE r32,r/m32       Move if not less or equal 
0F 41 /r     CMOVNO r32,r/m32        Move if not overflow 
0F 4B /r     CMOVNP r32,r/m32        Move if not parity 
0F 49 /r     CMOVNS r32,r/m32        Move if not sign 
0F 45 /r     CMOVNZ r32,r/m32        Move if not zero 
0F 40 /r     CMOVO r32,r/m32         Move if overflow 
0F 4A /r     CMOVP r32,r/m32         Move if parity 
0F 4A /r     CMOVPE r32,r/m32        Move if parity even 
0F 4B /r     CMOVPO r32,r/m32        Move if parity odd 
0F 48 /r     CMOVS r32,r/m32         Move if sign 
0F 44 /r     CMOVZ r32,r/m32         Move if zero 
3C       ib  CMP AL,imm8             Compare 
3D       id  CMP EAX,imm32           Compare 
80    /7 ib  CMP r/m8,imm8           Compare 
81    /7 id  CMP r/m32,imm32         Compare 
83    /7 ib  CMP r/m32,imm8          Compare 
38    /r     CMP r/m8,r8             Compare 
39    /r     CMP r/m32,r32           Compare 
3A    /r     CMP r8,r/m8             Compare 
3B    /r     CMP r32,r/m32           Compare 
A6           CMPSB                   Compare byte at DS:(E)SI with ES:(E)DI 
A7           CMPSD                   Compare dw   at DS:(E)SI with ES:(E)DI
0F B0 /r     CMPXCHG r/m8,r8         Compare and Exchange
0F B1 /r     CMPXCHG r/m32,r32       Compare and Exchange
0F C7 /1 m64 CMPXCHG8B m64           Compare and Exchange
0F A2        CPUID                   EAX := Processor id.info.
27           DAA                     Decimal adjust AL after addition
2F           DAS                     Decimal adjust AL after subtraction
FE    /1     DEC r/m8                Decrement r/m8 by 1
FF    /1     DEC r/m32               Decrement r/m32 by 1
48+rd        DEC r32                 Decrement r32 by 1
F6    /6     DIV r/m8                Unsigned divide AX by r/m8
F7    /6     DIV r/m16               Unsigned divide DX:AX by r/m16
F7    /6     DIV r/m32               Unsigned divide EDX:EAX by r/m32 
0F 77        EMMS                    Set the FP tag word to empty
C8     iw 00 ENTER imm16,0           Create a stack frame for a procedure
C8     iw 01 ENTER imm16,1           Create a nested stack frame for a proc.
C8     iw ib ENTER imm16,imm8        Create a nested stack frame for a proc.
D9 F0        F2XM1                   Replace ST(0) with 2**ST(0) - 1
D9 E1        FABS                    Replace ST(0) with its absolute value
D8    /0     FADD m32real            Add m32real to ST(0) and s.r. in ST(0)
DC    /0     FADD m64real            Add m64real to ST(0) and s.r.in ST(0)
D8 C0+i      FADD ST(0),ST(i)        Add ST(0) to ST(i) and s.r.in ST(0)
DC C0+i      FADD ST(i),ST(0)        Add ST(i) to ST(0) and s.r. in ST(i)
DE C0+i      FADDP ST(i),ST(0)       Add ST(0) to ST(i), s.r.in ST(i),pop r.stack
DE C1        FADDP                   Add ST(0) to ST(1), s.r.in ST(1),pop r.stack
DA    /0     FIADD m32int            Add m32int to ST(0) and s.r.in ST(0)
DE    /0     FIADD m16int            Add m16int to ST(0) and s.r.in ST(0)
DF    /4     FBLD m80bcd             Convert m80BCD to real and push 
DF    /6     FBSTP m80bcd            Store ST(0) in m80bcd and pop ST(0)
D9 E0        FCHS                    Complements sign of ST(0)
9B DB E2     FCLEX                   Clear f.e.f. after checking for ..
DB E2        FNCLEX                  Clear f.e.f. without checking for ..
DA C0+i      FCMOVB ST(0),ST(i)      Move if below 
DA C8+i      FCMOVE ST(0),ST(i)      Move if equal 
DA D0+i      FCMOVBE ST(0),ST(i)     Move if below or equal 
DA D8+i      FCMOVU ST(0),ST(i)      Move if unordered 
DB C0+i      FCMOVNB ST(0),ST(i)     Move if not below 
DB C8+i      FCMOVNE ST(0),ST(i)     Move if not equal 
DB D0+i      FCMOVNBE ST(0),ST(i)    Move if not below or equal 
DB D8+i      FCMOVNU ST(0),ST(i)     Move if not unordered 
D8    /2     FCOM m32real            Compare ST(0) with m32real.
DC    /2     FCOM m64real            Compare ST(0) with m64real.
D8 D0+i      FCOM ST(i)              Compare ST(0) with ST(i).
D8 D1        FCOM                    Compare ST(0) with ST(1).
D8    /3     FCOMP m32real           Compare ST(0) with m32real,pop r.stack.
DC    /3     FCOMP m64real           Compare ST(0) with m64real,pop r.stack.
D8 D8+i      FCOMP ST(i)             Compare ST(0) with ST(i), pop 
D8 D9        FCOMP                   Compare ST(0) with ST(1), pop 
DE D9        FCOMPP                  Compare ST(0) with ST(1), pop pop
DB F0+i      FCOMI ST,ST(i)          Compare ST(0) with ST(i), set status flags
DF F0+i      FCOMIP ST,ST(i)         Compare ST(0) with ST(i), set s.f. ,pop 
DB E8+i      FUCOMI ST,ST(i)         Compare ST(0) with ST(i), check o.v.set s.f.
DF E8+i      FUCOMIP ST,ST(i)        Compare ST(0) with ST(i), check ovssf pop 
D9 FF        FCOS                    Replace ST(0) with its cosine
D9 F6        FDECSTP                 Decrement TOP field in FPU status word.
D8    /6     FDIV m32real            Divide ST(0) by m32real and s.r.in ST(0)
DC    /6     FDIV m64real            Divide ST(0) by m64real and s.r.in ST(0)
D8 F0+i      FDIV ST(0),ST(i)        Divide ST(0) by ST(i) and s.r.in ST(0)
DC F8+i      FDIV ST(i),ST(0)        Divide ST(i) by ST(0) and s.r.in ST(i)
DE F8+i      FDIVP ST(i),ST(0)       Divide ST(i) by ST(0), s.r.in ST(i) pop 
DE F9        FDIVP                   Divide ST(1) by ST(0), s.r.in ST(1) pop 
DA    /6     FIDIV m32int            Divide ST(0) by m32int and s.r.in ST(0)
DE    /6     FIDIV m16int            Divide ST(0) by m64int and s.r.in ST(0)
D8    /7     FDIVR m32real           Divide m32real by ST(0) and s.r.in ST(0)
DC    /7     FDIVR m64real           Divide m64real by ST(0) and s.r.in ST(0)
D8 F8+i      FDIVR ST(0),ST(i)       Divide ST(i) by ST(0) and s.r.in ST(0)
DC F0+i      FDIVR ST(i),ST(0)       Divide ST(0) by ST(i) and s.r.in ST(i)
DE F0+i      FDIVRP ST(i),ST(0)      Divide ST(0) by ST(i), s.r.in ST(i) pop 
DE F1        FDIVRP                  Divide ST(0) by ST(1), s.r.in ST(1) pop 
DA    /7     FIDIVR m32int           Divide m32int by ST(0) and s.r.in ST(0)
DE    /7     FIDIVR m16int           Divide m64int by ST(0) and s.r.in ST(0)
DD C0+i      FFREE ST(i)             Sets tag for ST(i) to empty
DE    /2     FICOM m16int            Compare ST(0) with m16int
DA    /2     FICOM m32int            Compare ST(0) with m32int
DE    /3     FICOMP m16int           Compare ST(0) with m16int and pop 
DA    /3     FICOMP m32int           Compare ST(0) with m32int and pop 
DF    /0     FILD m16int             Push m16int 
DB    /0     FILD m32int             Push m32int 
DF    /5     FILD m64int             Push m64int 
D9 F7        FINCSTP                 Increment the TOP field FPU status r.
9B DB E3     FINIT                   Initialize FPU after ...
DB E3        FNINIT                  Initialize FPU without ...
DF    /2     FIST m16int             Store ST(0) in m16int
DB    /2     FIST m32int             Store ST(0) in m32int
DF    /3     FISTP m16int            Store ST(0) in m16int and pop 
DB    /3     FISTP m32int            Store ST(0) in m32int and pop 
DF    /7     FISTP m64int            Store ST(0) in m64int and pop 
D9    /0     FLD m32real             Push m32real 
DD    /0     FLD m64real             Push m64real 
DB    /5     FLD m80real             Push m80real 
D9 C0+i      FLD ST(i)               Push ST(i) 
D9 E8        FLD1                    Push +1.0 
D9 E9        FLDL2T                  Push log2 10 
D9 EA        FLDL2E                  Push log2 e 
D9 EB        FLDPI                   Push pi 
D9 EC        FLDLG2                  Push log10 2 
D9 ED        FLDLN2                  Push loge 2 
D9 EE        FLDZ                    Push +0.0 
D9    /5     FLDCW m2byte            Load FPU control word from m2byte
D9    /4     FLDENV m14/28byte       Load FPU environment from m14/m28
D8    /1     FMUL m32real            Multiply ST(0) by m32real and s.r.in ST(0)
DC    /1     FMUL m64real            Multiply ST(0) by m64real and s.r.in ST(0)
D8 C8+i      FMUL ST(0),ST(i)        Multiply ST(0) by ST(i) and s.r.in ST(0)
DC C8+i      FMUL ST(i),ST(0)        Multiply ST(i) by ST(0) and s.r.in ST(i)
DE C8+i      FMULP ST(i),ST(0)       Multiply ST(i) by ST(0), s.r.in ST(i) pop 
DE C9        FMULP                   Multiply ST(1) by ST(0), s.r.in ST(1) pop 
DA    /1     FIMUL m32int            Multiply ST(0) by m32int and s.r.in ST(0)
DE    /1     FIMUL m16int            Multiply ST(0) by m16int and s.r.in ST(0)
D9 D0        FNOP                    No operation is performed
D9 F3        FPATAN                  Repalces ST(1) with arctan(ST(1)/ST(0)) pop 
D9 F8        FPREM                   Replaces ST(0) with rem (ST(0)/ST(1))
D9 F5        FPREM1                  Replaces ST(0) with IEEE rem(ST(0)/ST(1))
D9 F2        FPTAN                   Replaces ST(0) with its tangent push 1.0
D9 FC        FRNDINT                 Round ST(0) to an integer
DD    /4     FRSTOR m94/108byte      Load FPU status from m94 or m108 byte
9B DD /6     FSAVE m94/108byte       Store FPU status to m94 or m108
DD    /6     FNSAVE m94/108byte      Store FPU environment to m94 or m108
D9 FD        FSCALE                  Scale ST(0) by ST(1)
D9 FE        FSIN                    Replace ST(0) with its sine
D9 FB        FSINCOS                 Compute sine and consine of ST(0) s push c
D9 FA        FSQRT                   square root of ST(0)
D9    /2     FST m32real             Copy ST(0) to m32real
DD    /2     FST m64real             Copy ST(0) to m64real
DD D0+i      FST ST(i)               Copy ST(0) to ST(i)
D9    /3     FSTP m32real            Copy ST(0) to m32real and pop 
DD    /3     FSTP m64real            Copy ST(0) to m64real and pop 
DB    /7     FSTP m80real            Copy ST(0) to m80real and pop 
DD D8+i      FSTP ST(i)              Copy ST(0) to ST(i) and pop 
9B D9 /7     FSTCW m2byte            Store FPU control word
D9    /7     FNSTCW m2byte           Store FPU control word without
9B D9 /6     FSTENV m14/28byte       Store FPU environment
D9    /6     FNSTENV m14/28byte      Store FPU env without
9B DD /7     FSTSW m2byte            Store FPU status word at m2byte after 
9B DF E0     FSTSW AX                Store FPU status word in AX  after 
DD    /7     FNSTSW m2byte           Store FPU status word at m2byte without 
DF E0        FNSTSW AX               Store FPU status word in AX without 
D8    /4     FSUB m32real            Sub m32real from ST(0) and s.r.in ST(0)
DC    /4     FSUB m64real            Sub m64real from ST(0) and s.r.in ST(0)
D8 E0+i      FSUB ST(0),ST(i)        Sub ST(i) from ST(0) and s.r.in ST(0)
DC E8+i      FSUB ST(i),ST(0)        Sub ST(0) from ST(i) and s.r.in ST(i)
DE E8+i      FSUBP ST(i),ST(0)       Sub ST(0) from ST(i), s.r.in ST(i) pop
DE E9        FSUBP                   Sub ST(0) from ST(1), s.r.in ST(1) pop 
DA    /4     FISUB m32int            Sub m32int from ST(0) and s.r.in ST(0)
DE    /4     FISUB m16int            Sub m16int from ST(0) and s.r.in ST(0)
D8    /5     FSUBR m32real           Sub ST(0) from m32real and s.r.in ST(0)
DC    /5     FSUBR m64real           Sub ST(0) from m64real and s.r.in ST(0)
D8 E8+i      FSUBR ST(0),ST(i)       Sub ST(0) from ST(i) and s.r.in ST(0)
DC E0+i      FSUBR ST(i),ST(0)       Sub ST(i) from ST(0) and s.r.in ST(i)
DE E0+i      FSUBRP ST(i),ST(0)      Sub ST(i) from ST(0), s.r. in ST(i) pop 
DE E1        FSUBRP                  Sub ST(1) from ST(0), s.r.in ST(1) pop 
DA    /5     FISUBR m32int           Sub ST(0) from m32int and s.r.in ST(0)
DE    /5     FISUBR m16int           Sub ST(0) from m16int and s.r.in ST(0)
D9 E4        FTST                    Compare ST(0) with 0.0
DD E0+i      FUCOM ST(i)             Compare ST(0) with ST(i)
DD E1        FUCOM                   Compare ST(0) with ST(1)
DD E8+i      FUCOMP ST(i)            Compare ST(0) with ST(i) and pop 
DD E9        FUCOMP                  Compare ST(0) with ST(1) and pop 
DA E9        FUCOMPP                 Compare ST(0) with ST(1) and pop pop
D9 E5        FXAM                    Classify value or number in ST(0)
D9 C8+i      FXCH ST(i)              Exchange ST(0) and ST(i)
D9 C9        FXCH                    Exchange ST(0) and ST(1)
D9 F4        FXTRACT                 Seperate value in ST(0) exp. and sig.
D9 F1        FYL2X                   Replace ST(1) with ST(1)*log2ST(0) and pop
D9 F9        FYL2XP1                 Replace ST(1) with ST(1)*log2(ST(0)+1) pop
F4           HLT                     Halt
F6    /7     IDIV r/m8               Divide   
F7    /7     IDIV r/m32              Divide  
F6    /5     IMUL r/m8               Multiply
F7    /5     IMUL r/m32              Multiply
0F AF /r     IMUL r32,r/m32          Multiply
6B    /r ib  IMUL r32,r/m32,imm8     Multiply
6B    /r ib  IMUL r32,imm8           Multiply
69    /r id  IMUL r32,r/m32,imm32    Multiply
69    /r id  IMUL r32,imm32          Multiply
E4       ib  IN AL,imm8              Input byte from imm8 I/O port address into AL
E5       ib  IN EAX,imm8             Input byte from imm8 I/O port address into EAX
EC           IN AL,DX                Input byte from I/O port in DX into AL
ED           IN EAX,DX               Input doubleword from I/O port in DX into EAX
FE    /0     INC r/m8                Increment 1
FF    /0     INC r/m32               Increment 1
40+rd        INC r32                 Increment register by 1
6C           INS m8                  Input byte from I/O(DX) into  ES:(E)DI
6D           INS m32                 Input dw from I/O(DX) into ES:(E)DI
CC           INT 3                   Interrupt 3--trap to debugger
CD       ib  INT imm8                Interrupt vector number (imm8)
CE           INTO                    Interrupt 4--if overflow flag is 1
0F 08        INVD                    Flush internal caches
0F 01 /7     INVLPG m                Invalidate TLB Entry for page (m)
CF           IRETD                   Interrupt return(32)
77       cb  JA rel8                 Jump short if above 
73       cb  JAE rel8                Jump short if above or equal 
76       cb  JBE rel8                Jump short if below or equal 
72       cb  JC rel8                 Jump short if carry 
E3       cb  JECXZ rel8              Jump short if ECX register is 0
74       cb  JE rel8                 Jump short if equal 
7F       cb  JG rel8                 Jump short if greater 
7D       cb  JGE rel8                Jump short if greater or equal 
7C       cb  JL rel8                 Jump short if less 
7E       cb  JLE rel8                Jump short if less or equal 
75       cb  JNE rel8                Jump short if not equal 
71       cb  JNO rel8                Jump short if not overflow 
79       cb  JNS rel8                Jump short if not sign 
70       cb  JO rel8                 Jump short if overflow 
7A       cb  JPE rel8                Jump short if parity even 
7B       cb  JPO rel8                Jump short if parity odd 
78       cb  JS rel8                 Jump short if sign 
0F 87    cd  JA rel32                Jump near if above 
0F 83    cd  JAE rel32               Jump near if above or equal 
0F 82    cd  JB rel32                Jump near if below 
0F 86    cd  JBE rel32               Jump near if below or equal 
0F 84    cd  JE rel32                Jump near if equal 
0F 8F    cd  JG rel32                Jump near if greater 
0F 8D    cd  JGE rel32               Jump near if greater or equal 
0F 8C    cd  JL rel32                Jump near if less 
0F 8E    cd  JLE rel32               Jump near if less or equal 
0F 85    cd  JNE rel32               Jump near if not equal 
0F 81    cd  JNO rel32               Jump near if not overflow 
0F 89    cd  JNS rel32               Jump near if not sign 
0F 80    cd  JO rel32                Jump near if overflow 
0F 8A    cd  JPE rel32               Jump near if parity even 
0F 8B    cd  JPO rel32               Jump near if parity odd 
0F 88    cd  JS rel32                Jump near if sign 
EB       cb  JMP rel8                Jump short, relative, 
E9       cd  JMP rel32               Jump near, relative, 
FF    /4     JMP r/m32               Jump near, abs.ind.in r/m32
EA       cp  JMP ptr16:32            Jump far, abs.add given in operand
FF    /r     JMP m16:32              Jump far, abs.ind.in m16:32
9F           LAHF                    Load Status Flags into AH 
0F 02 /r     LAR r32,r/m32           Load Access Rights Byte     
C5    /r     LDS r32,m16:32          Load DS:r32 with far ptr
8D    /r     LEA r32,m               Load effective address  
C9           LEAVE                   Set ESP to EBP, then pop EBP
C4    /r     LES r32,m16:32          Load ES:r32 with far ptr 
0F B4 /r     LFS r32,m16:32          Load FS:r32 with far ptr
0F B5 /r     LGS r32,m16:32          Load GS:r32 with far ptr
0F 01 /2     LGDT m16&32             Load m into GDTR
0F 01 /3     LIDT m16&32             Load m into IDTR
0F 00 /2     LLDT r/m16              Load segment selector r/m16 into LDTR
0F 01 /6     LMSW r/m16              Load r/m16 in machine status word of CR0
F0           LOCK                    Asserts LOCK signal for duration ..
AC           LODS m8                 Load byte at address DS:(E)SI into AL
AD           LODS m32                Load dword at address DS:(E)SI into EAX
E2       cb  LOOP rel8               Dec count;jump if count # 0
E1       cb  LOOPE rel8              Dec count;jump if count # 0 and ZF=1
E1       cb  LOOPZ rel8              Dec count;jump if count # 0 and ZF=1
E0       cb  LOOPNE rel8             Dec count;jump if count # 0 and ZF=0
E0       cb  LOOPNZ rel8             Dec count;jump if count # 0 and ZF=0
0F 03 /r     LSL r16,r/m16           Load Segment Limit
0F 03 /r     LSL r32,r/m32           Load Segment Limit
0F B2 /r     LSS r32,m16:32          Load SS:r32 with far ptr
0F 00 /3     LTR r/m16               Load Task Register
88    /r     MOV r/m8,r8             Move 
89    /r     MOV r/m32,r32           Move 
8A    /r     MOV r8,r/m8             Move 
8B    /r     MOV r32,r/m32           Move 
8C    /r     MOV r/m16,Sreg          Move segment register to r/m16
8E    /r     MOV Sreg,r/m16          Move r/m16 to segment register
A0           MOV AL, moffs8          Move byte at ( seg:offset) to AL
A1           MOV AX, moffs16         Move word at ( seg:offset) to AX
A1           MOV EAX, moffs32        Move dword at ( seg:offset) to EAX
A2           MOV moffs8,AL           Move AL to ( seg:offset)
A3           MOV moffs16,AX          Move AX to ( seg:offset)
A3           MOV moffs32,EAX         Move EAX to ( seg:offset)
B0+rb        MOV r8,imm8             Move imm8 to r8
B8+rd        MOV r32,imm32           Move imm32 to r32
C6    /0 ib  MOV r/m8,imm8           Move imm8 to r/m8
C7    /0 id  MOV r/m32,imm32         Move imm32 to r/m32
0F 22 /r     MOV CR0, r32            Move r32 to CR0
0F 22 /r     MOV CR2, r32            Move r32 to CR2
0F 22 /r     MOV CR3, r32            Move r32 to CR3
0F 22 /r     MOV CR4, r32            Move r32 to CR4
0F 20 /r     MOV r32,CR0             Move CR0 to r32
0F 20 /r     MOV r32,CR2             Move CR2 to r32
0F 20 /r     MOV r32,CR3             Move CR3 to r32
0F 20 /r     MOV r32,CR4             Move CR4 to r32
0F 21 /r     MOV r32,DR0-DR7         Move debug register to r32
0F 23 /r     MOV DR0-DR7,r32         Move r32 to debug register
0F 6E /r     MOVD mm,r/m32           Move doubleword from r/m32 to mm
0F 7E /r     MOVD r/m32,mm           Move doubleword from mm to r/m32
0F 6F /r     MOVQ mm,mm/m64          Move quadword from mm/m64 to mm
0F 7F /r     MOVQ mm/m64,mm          Move quadword from mm to mm/m64
A4           MOVS m8,m8              Move byte at DS:(E)SI to  ES:(E)DI
A5           MOVS m32,m32            Move dword at DS:(E)SI to  ES:(E)DI
0F BE /r     MOVSX r32,r/m8          Move byte to doubleword, sign-extension
0F BF /r     MOVSX r32,r/m16         Move word to doubleword, sign-extension
0F B6 /r     MOVZX r32,r/m8          Move byte to doubleword, zero-extension
0F B7 /r     MOVZX r32,r/m16         Move word to doubleword, zero-extension
F6    /4     MUL r/m8                Unsigned multiply 
F7    /4     MUL r/m32               Unsigned multiply 
F6    /3     NEG r/m8                Two's complement negate r/m8
F7    /3     NEG r/m32               Two's complement negate r/m32
90           NOP                     No operation
F6    /2     NOT r/m8                Reverse each bit of r/m8
F7    /2     NOT r/m32               Reverse each bit of r/m32
0C       ib  OR AL,imm8              OR
0D       id  OR EAX,imm32            OR 
80    /1 ib  OR r/m8,imm8            OR 
81    /1 id  OR r/m32,imm32          OR 
83    /1 ib  OR r/m32,imm8           OR 
08    /r     OR r/m8,r8              OR 
09    /r     OR r/m32,r32            OR 
0A    /r     OR r8,r/m8              OR 
0B    /r     OR r32,r/m32            OR 
E6       ib  OUT imm8,AL             Output byte in AL to I/O(imm8)
E7       ib  OUT imm8,EAX            Output dword in EAX to I/O(imm8)
EE           OUT DX,AL               Output byte in AL to I/O(DX)
EF           OUT DX,EAX              Output dword in EAX to I/O(DX)
6E           OUTS DX,m8              Output byte from DS:(E)SI to I/O(DX)
6F           OUTS DX,m32             Output dword from DS:(E)SI to I/O (DX)
0F 63 /r     PACKSSWB mm,mm/m64      Pack with Signed Saturation
0F 6B /r     PACKSSDW mm,mm/m64      Pack with Signed Saturation
0F 67 /r     PACKUSWB mm,mm/m64      Pack with Unsigned Saturation
0F FC /r     PADDB mm,mm/m64         Add packed bytes 
0F FD /r     PADDW mm,mm/m64         Add packed words 
0F FE /r     PADDD mm,mm/m64         Add packed dwords 
0F EC /r     PADDSB mm,mm/m64        Add signed packed bytes 
0F ED /r     PADDSW mm,mm/m64        Add signed packed words 
0F DC /r     PADDUSB mm,mm/m64       Add unsigned pkd bytes 
0F DD /r     PADDUSW mm,mm/m64       Add unsigned pkd words 
0F DB /r     PAND mm,mm/m64          AND quadword from .. to ..
0F DF /r     PANDN mm,mm/m64         And qword from .. to NOT qw in mm
0F 74 /r     PCMPEQB mm,mm/m64       Packed Compare for Equal
0F 75 /r     PCMPEQW mm,mm/m64       Packed Compare for Equal
0F 76 /r     PCMPEQD mm,mm/m64       Packed Compare for Equal
0F 64 /r     PCMPGTB mm,mm/m64       Packed Compare for GT
0F 65 /r     PCMPGTW mm,mm/m64       Packed Compare for GT
0F 66 /r     PCMPGTD mm,mm/m64       Packed Compare for GT
0F F5 /r     PMADDWD mm,mm/m64       Packed Multiply and Add
0F E5 /r     PMULHW mm,mm/m64        Packed Multiply High
0F D5 /r     PMULLW mm,mm/m64        Packed Multiply Low
8F    /0     POP m32                 Pop m32
58+rd        POP r32                 Pop r32
1F           POP DS                  Pop DS
07           POP ES                  Pop ES
17           POP SS                  Pop SS
0F A1        POP FS                  Pop FS
0F A9        POP GS                  Pop GS
61           POPAD                   Pop EDI,... and EAX
9D           POPFD                   Pop Stack into EFLAGS Register
0F EB /r     POR mm,mm/m64           OR qword from .. to mm
0F F1 /r     PSLLW mm,mm/m64         Packed Shift Left Logical
0F 71 /6 ib  PSLLW mm,imm8           Packed Shift Left Logical
0F F2 /r     PSLLD mm,mm/m64         Packed Shift Left Logical
0F 72 /6 ib  PSLLD mm,imm8           Packed Shift Left Logical
0F F3 /r     PSLLQ mm,mm/m64         Packed Shift Left Logical
0F 73 /6 ib  PSLLQ mm,imm8           Packed Shift Left Logical
0F E1 /r     PSRAW mm,mm/m64         Packed Shift Right Arithmetic
0F 71 /4 ib  PSRAW mm,imm8           Packed Shift Right Arithmetic
0F E2 /r     PSRAD mm,mm/m64         Packed Shift Right Arithmetic
0F 72 /4 ib  PSRAD mm,imm8           Packed Shift Right Arithmetic
0F D1 /r     PSRLW mm,mm/m64         Packed Shift Right Logical 
0F 71 /2 ib  PSRLW mm,imm8           Packed Shift Right Logical 
0F D2 /r     PSRLD mm,mm/m64         Packed Shift Right Logical 
0F 72 /2 ib  PSRLD mm,imm8           Packed Shift Right Logical 
0F D3 /r     PSRLQ mm,mm/m64         Packed Shift Right Logical 
0F 73 /2 ib  PSRLQ mm,imm8           Packed Shift Right Logical 
0F F8 /r     PSUBB mm,mm/m64         Packed Subtract
0F F9 /r     PSUBW mm,mm/m64         Packed Subtract
0F FA /r     PSUBD mm,mm/m64         Packed Subtract
0F E8 /r     PSUBSB mm,mm/m64        Packed Subtract with Saturation
0F E9 /r     PSUBSW mm,mm/m64        Packed Subtract with Saturation
0F D8 /r     PSUBUSB mm,mm/m64       Packed Subtract Unsigned with S.
0F D9 /r     PSUBUSW mm,mm/m64       Packed Subtract Unsigned with S.
0F 68 /r     PUNPCKHBW mm,mm/m64     Unpack High Packed Data
0F 69 /r     PUNPCKHWD mm,mm/m64     Unpack High Packed Data
0F 6A /r     PUNPCKHDQ mm,mm/m64     Unpack High Packed Data
0F 60 /r     PUNPCKLBW mm,mm/m64     Unpack Low Packed Data
0F 61 /r     PUNPCKLWD mm,mm/m64     Unpack Low Packed Data
0F 62 /r     PUNPCKLDQ mm,mm/m64     Unpack Low Packed Data
FF    /6     PUSH r/m32              Push r/m32
50+rd        PUSH r32                Push r32
6A       ib  PUSH imm8               Push imm8
68       id  PUSH imm32              Push imm32
0E           PUSH CS                 Push CS
16           PUSH SS                 Push SS
1E           PUSH DS                 Push DS
06           PUSH ES                 Push ES
0F A0        PUSH FS                 Push FS
0F A8        PUSH GS                 Push GS
60           PUSHAD                  Push All g-regs
9C           PUSHFD                  Push EFLAGS
0F EF /r     PXOR mm,mm/m64          XOR qword
D0    /2     RCL r/m8,1              Rotate 9 bits left once
D2    /2     RCL r/m8,CL             Rotate 9 bits left CL times
C0    /2 ib  RCL r/m8,imm8           Rotate 9 bits left imm8 times
D1    /2     RCL r/m32,1             Rotate 33 bits left once
D3    /2     RCL r/m32,CL            Rotate 33 bits left CL times
C1    /2 ib  RCL r/m32,imm8          Rotate 33 bits left imm8 times
D0    /3     RCR r/m8,1              Rotate 9 bits right once
D2    /3     RCR r/m8,CL             Rotate 9 bits right CL times
C0    /3 ib  RCR r/m8,imm8           Rotate 9 bits right imm8 times
D1    /3     RCR r/m32,1             Rotate 33 bits right once
D3    /3     RCR r/m32,CL            Rotate 33 bits right CL times
C1    /3 ib  RCR r/m32,imm8          Rotate 33 bits right imm8 times
D0    /0     ROL r/m8,1              Rotate 8 bits r/m8 left once
D2    /0     ROL r/m8,CL             Rotate 8 bits r/m8 left CL times
C0    /0 ib  ROL r/m8,imm8           Rotate 8 bits r/m8 left imm8 times
D1    /0     ROL r/m32,1             Rotate 32 bits r/m32 left once
D3    /0     ROL r/m32,CL            Rotate 32 bits r/m32 left CL times
C1    /0 ib  ROL r/m32,imm8          Rotate 32 bits r/m32 left imm8 times
D0    /1     ROR r/m8,1              Rotate 8 bits r/m8 right once
D2    /1     ROR r/m8,CL             Rotate 8 bits r/m8 right CL times
C0    /1 ib  ROR r/m8,imm8           Rotate 8 bits r/m16 right imm8 times
D1    /1     ROR r/m32,1             Rotate 32 bits r/m32 right once
D3    /1     ROR r/m32,CL            Rotate 32 bits r/m32 right CL times
C1    /1 ib  ROR r/m32,imm8          Rotate 32 bits r/m32 right imm8 times
0F 32        RDMSR                   Read from Model Specific Register
0F 33        RDPMC                   Read Performance-Monitoring counters
0F 31        RDTSC                   Read Time-Stamp Counter
F3 6C        REP INS m8,DX           Input ECX bytes from port DX into ES:[(E)DI]
F3 6D        REP INS m32,DX          Input ECX dwords from port DX into ES:[(E)DI]
F3 A4        REP MOVS m8,m8          Move ECX bytes from DS:[(E)SI] to ES:[(E)DI]
F3 A5        REP MOVS m32,m32        Move ECX dwords from DS:[(E)SI] to ES:[(E)DI]
F3 6E        REP OUTS DX,m8          Output ECX bytes from DS:[(E)SI] to port DX
F3 6F        REP OUTS DX,m32         Output ECX dwords from DS:[(E)SI] to port DX
F3 AC        REP LODS AL             Load ECX bytes from DS:[(E)SI] to AL
F3 AD        REP LODS EAX            Load ECX dwords from DS:[(E)SI] to EAX
F3 AA        REP STOS m8             Fill ECX bytes at ES:[(E)DI] with AL
F3 AB        REP STOS m32            Fill ECX dwords at ES:[(E)DI] with EAX
F3 A6        REPE CMPS m8,m8         Find nonmatching bytes in m and m
F3 A7        REPE CMPS m32,m32       Find nonmatching dwords in m and m
F3 AE        REPE SCAS m8            Find non-AL byte starting at 
F3 AF        REPE SCAS m32           Find non-EAX dword starting at 
F2 A6        REPNE CMPS m8,m8        Find matching bytes in m and m
F2 A7        REPNE CMPS m32,m32      Find matching dwords in m and m
F2 AE        REPNE SCAS m8           Find AL, starting at ES:[(E)DI]
F2 AF        REPNE SCAS m32          Find EAX, starting at ES:[(E)DI]
C3           RET                     Near return 
CB           RET                     Far return 
C2       iw  RET imm16               Near return, pop imm16 bytes from stack
CA       iw  RET imm16               Far return, pop imm16 bytes from stack
0F AA        RSM                     Resume from System Management
9E           SAHF                    Store AH into Flags
D0    /4     SAL r/m8,1              Shift Arithmetic Left
D2    /4     SAL r/m8,CL             Shift Arithmetic Left
C0    /4 ib  SAL r/m8,imm8           Shift Arithmetic Left
D1    /4     SAL r/m32,1             Shift Arithmetic Left
D3    /4     SAL r/m32,CL            Shift Arithmetic Left
C1    /4 ib  SAL r/m32,imm8          Shift Arithmetic Left
D0    /7     SAR r/m8,1              Shift Arithmetic Right
D2    /7     SAR r/m8,CL             Shift Arithmetic Right
C0    /7 ib  SAR r/m8,imm8           Shift Arithmetic Right
D1    /7     SAR r/m32,1             Shift Arithmetic Right
D3    /7     SAR r/m32,CL            Shift Arithmetic Right
C1    /7 ib  SAR r/m32,imm8          Shift Arithmetic Right
D0    /4     SHL r/m8,1              Shift Logical Left
D2    /4     SHL r/m8,CL             Shift Logical Left
C0    /4 ib  SHL r/m8,imm8           Shift Logical Left
D1    /4     SHL r/m32,1             Shift Logical Left
D3    /4     SHL r/m32,CL            Shift Logical Left
C1    /4 ib  SHL r/m32,imm8          Shift Logical Left
D0    /5     SHR r/m8,1              Shift Logical Right
D2    /5     SHR r/m8,CL             Shift Logical Right
C0    /5 ib  SHR r/m8,imm8           Shift Logical Right
D1    /5     SHR r/m32,1             Shift Logical Right
D3    /5     SHR r/m32,CL            Shift Logical Right
C1    /5 ib  SHR r/m32,imm8          Shift Logical Right
1C       ib  SBB AL,imm8             Subtract with borrow 
1D       id  SBB EAX,imm32           Subtract with borrow 
80    /3 ib  SBB r/m8,imm8           Subtract with borrow 
81    /3 id  SBB r/m32,imm32         Subtract with borrow 
83    /3 ib  SBB r/m32,imm8          Subtract with borrow 
18    /r     SBB r/m8,r8             Subtract with borrow 
19    /r     SBB r/m32,r32           Subtract with borrow 
1A    /r     SBB r8,r/m8             Subtract with borrow 
1B    /r     SBB r32,r/m32           Subtract with borrow 
AE           SCAS m8                 Scan String 
AF           SCAS m32                Scan String
0F 97 /r     SETA r/m8               Set byte if above 
0F 93 /r     SETAE r/m8              Set byte if above or equal
0F 92 /r     SETB r/m8               Set byte if below 
0F 96 /r     SETBE r/m8              Set byte if below or equal 
0F 94 /r     SETE r/m8               Set byte if equal 
0F 9F /r     SETG r/m8               Set byte if greater 
0F 9D /r     SETGE r/m8              Set byte if greater or equal
0F 9C /r     SETL r/m8               Set byte if less 
0F 9E /r     SETLE r/m8              Set byte if less or equal 
0F 95 /r     SETNE r/m8              Set byte if not equal 
0F 91 /r     SETNO r/m8              Set byte if not overflow 
0F 99 /r     SETNS r/m8              Set byte if not sign 
0F 90 /r     SETO r/m8               Set byte if overflow 
0F 9A /r     SETPE r/m8              Set byte if parity even 
0F 9B /r     SETPO r/m8              Set byte if parity odd 
0F 98 /r     SETS r/m8               Set byte if sign 
0F 01 /0     SGDT m                  Store GDTR to m
0F 01 /1     SIDT m                  Store IDTR to m
0F A4 /r ib  SHLD r/m32,r32,imm8     Double Precision Shift Left
0F A5 /r     SHLD r/m32,r32,CL       Double Precision Shift Left
0F AC /r ib  SHRD r/m32,r32,imm8     Double Precision Shift Right
0F AD /r     SHRD r/m32,r32,CL       Double Precision Shift Right
0F 00 /0     SLDT r/m32              Store Local Descriptor Table Register
0F 01 /4     SMSW r/m32              Store Machine Status Word
F9           STC                     Set Carry Flag
FD           STD                     Set Direction Flag
FB           STI                     Set Interrup Flag
AA           STOS m8                 Store String
AB           STOS m32                Store String
0F 00 /1     STR r/m16               Store Task Register
2C       ib  SUB AL,imm8             Subtract 
2D       id  SUB EAX,imm32           Subtract 
80    /5 ib  SUB r/m8,imm8           Subtract 
81    /5 id  SUB r/m32,imm32         Subtract 
83    /5 ib  SUB r/m32,imm8          Subtract 
28    /r     SUB r/m8,r8             Subtract 
29    /r     SUB r/m32,r32           Subtract 
2A    /r     SUB r8,r/m8             Subtract 
2B    /r     SUB r32,r/m32           Subtract 
A8       ib  TEST AL,imm8            Logical Compare
A9       id  TEST EAX,imm32          Logical Compare
F6    /0 ib  TEST r/m8,imm8          Logical Compare
F7    /0 id  TEST r/m32,imm32        Logical Compare
84    /r     TEST r/m8,r8            Logical Compare
85    /r     TEST r/m16,r16          Logical Compare
85    /r     TEST r/m32,r32          Logical Compare
0F 0B        UD2                     Undifined Instruction
0F 00 /4     VERR r/m16              Verify a Segment for Reading
0F 00 /5     VERW r/m16              Verify a Segment for Writing
9B           WAIT                    Wait
9B           FWAIT                   Wait
0F 09        WBINVD                  Write Back and Invalidate Cache
0F 30        WRMSR                   Write to Model Specific Register
0F C0 /r     XADD r/m8,r8            Exchange and Add
0F C1 /r     XADD r/m16,r16          Exchange and Add
0F C1 /r     XADD r/m32,r32          Exchange and Add
90+rd        XCHG EAX,r32            Exchange r32 with EAX
90+rd        XCHG r32,EAX            Exchange EAX with r32
86    /r     XCHG r/m8,r8            Exchange byte 
86    /r     XCHG r8,r/m8            Exchange byte 
87    /r     XCHG r/m32,r32          Exchange doubleword 
87    /r     XCHG r32,r/m32          Exchange doubleword 
D7           XLAT m8                 Table Look-up Translation
34       ib  XOR AL,imm8             Logical Exclusive OR
35       id  XOR EAX,imm32           Logical Exclusive OR
80    /6 ib  XOR r/m8,imm8           Logical Exclusive OR
81    /6 id  XOR r/m32,imm32         Logical Exclusive OR
83    /6 ib  XOR r/m32,imm8          Logical Exclusive OR
30    /r     XOR r/m8,r8             Logical Exclusive OR
31    /r     XOR r/m32,r32           Logical Exclusive OR
32    /r     XOR r8,r/m8             Logical Exclusive OR
33    /r     XOR r32,r/m32           Logical Exclusive OR
*/
    export type OpCodeOption = ("ib" | "iw" | "id" | "rb" | "rw" | "rd" | "+rb" | "+rw" | "+rd" | "cb" | "cw" | "cd" | "cp" | "+i" | "/0" | "/1" | "/2" | "/3" | "/4" | "/5" | "/6" | "/7" | "/r" | "+" | "00" | "01" | "m64");
    export type InstructionOperand = ("AL" | "AX" | "EAX" | "CL" | "DX" | "imm8" | "imm16" | "imm32" | "r/m8" | "r/m16" | "r/m32" | "r8" | "r16" | "r32" | "Sreg" | "moffs8" | "moffs16" | "moffs32" | "rel8" | "rel32" | "ptr16:32" | "m16:32" | "m16&32" | "0" | "1" | "ST(i)" | "ST(0)" | "mm/m64" | "mm" | "m94/108byte" | "m14/28byte" | "m" | "m32real" | "m64real" | "m80real" | "m16int" | "m32int" | "m64int" | "m80bcd" | "m8" | "m32" | "m2byte" | "DR0-DR7" | "CR0" | "CR2" | "CR3" | "CR4");

    export interface Define {
        opcode: { bytes: number[], options: OpCodeOption[] };
        instruction: { mnemonic: string, operands: InstructionOperand[] };
        description: string;
    }

    const defineTable: Define[] = [
        {
            "opcode": {
                "bytes": [
                    55
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "AAA",
                "operands": []
            },
            "description": "ASCII adjust AL after addition"
        },
        {
            "opcode": {
                "bytes": [
                    213,
                    10
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "AAD",
                "operands": []
            },
            "description": "ASCII adjust AX before division"
        },
        {
            "opcode": {
                "bytes": [
                    212,
                    10
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "AAM",
                "operands": []
            },
            "description": "ASCII adjust AX after multiplication"
        },
        {
            "opcode": {
                "bytes": [
                    63
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "AAS",
                "operands": []
            },
            "description": "ASCII adjust AL after subtraction"
        },
        {
            "opcode": {
                "bytes": [
                    20
                ],
                "options": [
                    "ib"
                ]
            },
            "instruction": {
                "mnemonic": "ADC",
                "operands": [
                    "AL",
                    "imm8"
                ]
            },
            "description": "Add with carry"
        },
        {
            "opcode": {
                "bytes": [
                    21
                ],
                "options": [
                    "id"
                ]
            },
            "instruction": {
                "mnemonic": "ADC",
                "operands": [
                    "EAX",
                    "imm32"
                ]
            },
            "description": "Add with carry"
        },
        {
            "opcode": {
                "bytes": [
                    128
                ],
                "options": [
                    "/2",
                    "ib"
                ]
            },
            "instruction": {
                "mnemonic": "ADC",
                "operands": [
                    "r/m8",
                    "imm8"
                ]
            },
            "description": "Add with carry"
        },
        {
            "opcode": {
                "bytes": [
                    129
                ],
                "options": [
                    "/2",
                    "id"
                ]
            },
            "instruction": {
                "mnemonic": "ADC",
                "operands": [
                    "r/m32",
                    "imm32"
                ]
            },
            "description": "Add with carry"
        },
        {
            "opcode": {
                "bytes": [
                    131
                ],
                "options": [
                    "/2",
                    "ib"
                ]
            },
            "instruction": {
                "mnemonic": "ADC",
                "operands": [
                    "r/m32",
                    "imm8"
                ]
            },
            "description": "Add with carry"
        },
        {
            "opcode": {
                "bytes": [
                    16
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "ADC",
                "operands": [
                    "r/m8",
                    "r8"
                ]
            },
            "description": "Add with carry"
        },
        {
            "opcode": {
                "bytes": [
                    17
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "ADC",
                "operands": [
                    "r/m32",
                    "r32"
                ]
            },
            "description": "Add with carry"
        },
        {
            "opcode": {
                "bytes": [
                    18
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "ADC",
                "operands": [
                    "r8",
                    "r/m8"
                ]
            },
            "description": "Add with carry"
        },
        {
            "opcode": {
                "bytes": [
                    19
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "ADC",
                "operands": [
                    "r32",
                    "r/m32"
                ]
            },
            "description": "Add with carry"
        },
        {
            "opcode": {
                "bytes": [
                    4
                ],
                "options": [
                    "ib"
                ]
            },
            "instruction": {
                "mnemonic": "ADD",
                "operands": [
                    "AL",
                    "imm8"
                ]
            },
            "description": "Add"
        },
        {
            "opcode": {
                "bytes": [
                    5
                ],
                "options": [
                    "id"
                ]
            },
            "instruction": {
                "mnemonic": "ADD",
                "operands": [
                    "EAX",
                    "imm32"
                ]
            },
            "description": "Add"
        },
        {
            "opcode": {
                "bytes": [
                    128
                ],
                "options": [
                    "/0",
                    "ib"
                ]
            },
            "instruction": {
                "mnemonic": "ADD",
                "operands": [
                    "r/m8",
                    "imm8"
                ]
            },
            "description": "Add"
        },
        {
            "opcode": {
                "bytes": [
                    129
                ],
                "options": [
                    "/0",
                    "id"
                ]
            },
            "instruction": {
                "mnemonic": "ADD",
                "operands": [
                    "r/m32",
                    "imm32"
                ]
            },
            "description": "Add"
        },
        {
            "opcode": {
                "bytes": [
                    131
                ],
                "options": [
                    "/0",
                    "ib"
                ]
            },
            "instruction": {
                "mnemonic": "ADD",
                "operands": [
                    "r/m32",
                    "imm8"
                ]
            },
            "description": "Add"
        },
        {
            "opcode": {
                "bytes": [
                    0
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "ADD",
                "operands": [
                    "r/m8",
                    "r8"
                ]
            },
            "description": "ADD"
        },
        {
            "opcode": {
                "bytes": [
                    1
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "ADD",
                "operands": [
                    "r/m32",
                    "r32"
                ]
            },
            "description": "ADD"
        },
        {
            "opcode": {
                "bytes": [
                    2
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "ADD",
                "operands": [
                    "r8",
                    "r/m8"
                ]
            },
            "description": "ADD"
        },
        {
            "opcode": {
                "bytes": [
                    3
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "ADD",
                "operands": [
                    "r32",
                    "r/m32"
                ]
            },
            "description": "ADD"
        },
        {
            "opcode": {
                "bytes": [
                    36
                ],
                "options": [
                    "ib"
                ]
            },
            "instruction": {
                "mnemonic": "AND",
                "operands": [
                    "AL",
                    "imm8"
                ]
            },
            "description": "AND"
        },
        {
            "opcode": {
                "bytes": [
                    37
                ],
                "options": [
                    "id"
                ]
            },
            "instruction": {
                "mnemonic": "AND",
                "operands": [
                    "EAX",
                    "imm32"
                ]
            },
            "description": "AND"
        },
        {
            "opcode": {
                "bytes": [
                    128
                ],
                "options": [
                    "/4",
                    "ib"
                ]
            },
            "instruction": {
                "mnemonic": "AND",
                "operands": [
                    "r/m8",
                    "imm8"
                ]
            },
            "description": "AND"
        },
        {
            "opcode": {
                "bytes": [
                    129
                ],
                "options": [
                    "/4",
                    "id"
                ]
            },
            "instruction": {
                "mnemonic": "AND",
                "operands": [
                    "r/m32",
                    "imm32"
                ]
            },
            "description": "AND"
        },
        {
            "opcode": {
                "bytes": [
                    131
                ],
                "options": [
                    "/4",
                    "ib"
                ]
            },
            "instruction": {
                "mnemonic": "AND",
                "operands": [
                    "r/m32",
                    "imm8"
                ]
            },
            "description": "AND"
        },
        {
            "opcode": {
                "bytes": [
                    32
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "AND",
                "operands": [
                    "r/m8",
                    "r8"
                ]
            },
            "description": "AND"
        },
        {
            "opcode": {
                "bytes": [
                    33
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "AND",
                "operands": [
                    "r/m32",
                    "r32"
                ]
            },
            "description": "AND"
        },
        {
            "opcode": {
                "bytes": [
                    34
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "AND",
                "operands": [
                    "r8",
                    "r/m8"
                ]
            },
            "description": "AND"
        },
        {
            "opcode": {
                "bytes": [
                    35
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "AND",
                "operands": [
                    "r32",
                    "r/m32"
                ]
            },
            "description": "AND"
        },
        {
            "opcode": {
                "bytes": [
                    99
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "ARPL",
                "operands": [
                    "r/m16",
                    "r16"
                ]
            },
            "description": "Adjust Request Privilege Level of Sel."
        },
        {
            "opcode": {
                "bytes": [
                    98
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "BOUND",
                "operands": [
                    "r32",
                    "m32"
                ]
            },
            "description": "&32        Check Array Index Against Bounds"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    188
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "BSF",
                "operands": [
                    "r32",
                    "r/m32"
                ]
            },
            "description": "Bit scan forward on r/m32"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    189
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "BSR",
                "operands": [
                    "r32",
                    "r/m32"
                ]
            },
            "description": "Bit scan reverse on r/m32"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    200
                ],
                "options": [
                    "+rd"
                ]
            },
            "instruction": {
                "mnemonic": "BSWAP",
                "operands": [
                    "r32"
                ]
            },
            "description": "Reverses the byte order of a r32"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    163
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "BT",
                "operands": [
                    "r/m32",
                    "r32"
                ]
            },
            "description": "Bit Test"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    186
                ],
                "options": [
                    "/4",
                    "ib"
                ]
            },
            "instruction": {
                "mnemonic": "BT",
                "operands": [
                    "r/m32",
                    "imm8"
                ]
            },
            "description": "Bit Test"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    187
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "BTC",
                "operands": [
                    "r/m32",
                    "r32"
                ]
            },
            "description": "Bit Test and Complement"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    186
                ],
                "options": [
                    "/7",
                    "ib"
                ]
            },
            "instruction": {
                "mnemonic": "BTC",
                "operands": [
                    "r/m32",
                    "imm8"
                ]
            },
            "description": "Bit Test and Complement"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    179
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "BTR",
                "operands": [
                    "r/m32",
                    "r32"
                ]
            },
            "description": "Bit Test and Clear"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    186
                ],
                "options": [
                    "/6",
                    "ib"
                ]
            },
            "instruction": {
                "mnemonic": "BTR",
                "operands": [
                    "r/m32",
                    "imm8"
                ]
            },
            "description": "Bit Test and Clear"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    171
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "BTS",
                "operands": [
                    "r/m32",
                    "r32"
                ]
            },
            "description": "Bit Test and Set"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    186
                ],
                "options": [
                    "/5",
                    "ib"
                ]
            },
            "instruction": {
                "mnemonic": "BTS",
                "operands": [
                    "r/m32",
                    "imm8"
                ]
            },
            "description": "Bit Test and Set"
        },
        {
            "opcode": {
                "bytes": [
                    232
                ],
                "options": [
                    "cd"
                ]
            },
            "instruction": {
                "mnemonic": "CALL",
                "operands": [
                    "rel32"
                ]
            },
            "description": "Call near, rel to n.inst"
        },
        {
            "opcode": {
                "bytes": [
                    255
                ],
                "options": [
                    "/2"
                ]
            },
            "instruction": {
                "mnemonic": "CALL",
                "operands": [
                    "r/m32"
                ]
            },
            "description": "Call near, abs.ind.add. given in r/m32"
        },
        {
            "opcode": {
                "bytes": [
                    154
                ],
                "options": [
                    "cp"
                ]
            },
            "instruction": {
                "mnemonic": "CALL",
                "operands": [
                    "ptr16:32"
                ]
            },
            "description": "Call far, abs.add. given in operand"
        },
        {
            "opcode": {
                "bytes": [
                    255
                ],
                "options": [
                    "/3"
                ]
            },
            "instruction": {
                "mnemonic": "CALL",
                "operands": [
                    "m16:32"
                ]
            },
            "description": "Call far, abs.ind.add. given in m16:32"
        },
        {
            "opcode": {
                "bytes": [
                    152
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "CBW",
                "operands": []
            },
            "description": "Convert Byte to Word"
        },
        {
            "opcode": {
                "bytes": [
                    153
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "CWD",
                "operands": []
            },
            "description": "Convert Word to Doubleword"
        },
        {
            "opcode": {
                "bytes": [
                    153
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "CDQ",
                "operands": []
            },
            "description": "Convert Doubleword to Quadword"
        },
        {
            "opcode": {
                "bytes": [
                    248
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "CLC",
                "operands": []
            },
            "description": "Clear CF flag"
        },
        {
            "opcode": {
                "bytes": [
                    252
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "CLD",
                "operands": []
            },
            "description": "Clear DF flag"
        },
        {
            "opcode": {
                "bytes": [
                    250
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "CLI",
                "operands": []
            },
            "description": "Clear interrupt flag"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    6
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "CLTS",
                "operands": []
            },
            "description": "Clear Task-Switched Flag in Control Reg. Zero"
        },
        {
            "opcode": {
                "bytes": [
                    245
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "CMC",
                "operands": []
            },
            "description": "Complement CF flag"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    71
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "CMOVA",
                "operands": [
                    "r32",
                    "r/m32"
                ]
            },
            "description": "Move if above"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    67
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "CMOVAE",
                "operands": [
                    "r32",
                    "r/m32"
                ]
            },
            "description": "Move if above or equal"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    66
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "CMOVB",
                "operands": [
                    "r32",
                    "r/m32"
                ]
            },
            "description": "Move if below"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    70
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "CMOVBE",
                "operands": [
                    "r32",
                    "r/m32"
                ]
            },
            "description": "Move if below or equal"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    66
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "CMOVC",
                "operands": [
                    "r32",
                    "r/m32"
                ]
            },
            "description": "Move if carry"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    68
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "CMOVE",
                "operands": [
                    "r32",
                    "r/m32"
                ]
            },
            "description": "Move if equal"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    79
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "CMOVG",
                "operands": [
                    "r32",
                    "r/m32"
                ]
            },
            "description": "Move if greater"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    77
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "CMOVGE",
                "operands": [
                    "r32",
                    "r/m32"
                ]
            },
            "description": "Move if greater or equal"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    76
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "CMOVL",
                "operands": [
                    "r32",
                    "r/m32"
                ]
            },
            "description": "Move if less"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    78
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "CMOVLE",
                "operands": [
                    "r32",
                    "r/m32"
                ]
            },
            "description": "Move if less or equal"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    70
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "CMOVNA",
                "operands": [
                    "r32",
                    "r/m32"
                ]
            },
            "description": "Move if not above"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    66
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "CMOVNAE",
                "operands": [
                    "r32",
                    "r/m32"
                ]
            },
            "description": "Move if not above or equal"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    67
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "CMOVNB",
                "operands": [
                    "r32",
                    "r/m32"
                ]
            },
            "description": "Move if not below"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    71
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "CMOVNBE",
                "operands": [
                    "r32",
                    "r/m32"
                ]
            },
            "description": "Move if not below or equal"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    67
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "CMOVNC",
                "operands": [
                    "r32",
                    "r/m32"
                ]
            },
            "description": "Move if not carry"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    69
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "CMOVNE",
                "operands": [
                    "r32",
                    "r/m32"
                ]
            },
            "description": "Move if not equal"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    78
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "CMOVNG",
                "operands": [
                    "r32",
                    "r/m32"
                ]
            },
            "description": "Move if not greater"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    76
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "CMOVNGE",
                "operands": [
                    "r32",
                    "r/m32"
                ]
            },
            "description": "Move if not greater or equal"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    77
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "CMOVNL",
                "operands": [
                    "r32",
                    "r/m32"
                ]
            },
            "description": "Move if not less"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    79
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "CMOVNLE",
                "operands": [
                    "r32",
                    "r/m32"
                ]
            },
            "description": "Move if not less or equal"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    65
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "CMOVNO",
                "operands": [
                    "r32",
                    "r/m32"
                ]
            },
            "description": "Move if not overflow"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    75
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "CMOVNP",
                "operands": [
                    "r32",
                    "r/m32"
                ]
            },
            "description": "Move if not parity"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    73
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "CMOVNS",
                "operands": [
                    "r32",
                    "r/m32"
                ]
            },
            "description": "Move if not sign"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    69
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "CMOVNZ",
                "operands": [
                    "r32",
                    "r/m32"
                ]
            },
            "description": "Move if not zero"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    64
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "CMOVO",
                "operands": [
                    "r32",
                    "r/m32"
                ]
            },
            "description": "Move if overflow"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    74
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "CMOVP",
                "operands": [
                    "r32",
                    "r/m32"
                ]
            },
            "description": "Move if parity"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    74
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "CMOVPE",
                "operands": [
                    "r32",
                    "r/m32"
                ]
            },
            "description": "Move if parity even"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    75
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "CMOVPO",
                "operands": [
                    "r32",
                    "r/m32"
                ]
            },
            "description": "Move if parity odd"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    72
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "CMOVS",
                "operands": [
                    "r32",
                    "r/m32"
                ]
            },
            "description": "Move if sign"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    68
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "CMOVZ",
                "operands": [
                    "r32",
                    "r/m32"
                ]
            },
            "description": "Move if zero"
        },
        {
            "opcode": {
                "bytes": [
                    60
                ],
                "options": [
                    "ib"
                ]
            },
            "instruction": {
                "mnemonic": "CMP",
                "operands": [
                    "AL",
                    "imm8"
                ]
            },
            "description": "Compare"
        },
        {
            "opcode": {
                "bytes": [
                    61
                ],
                "options": [
                    "id"
                ]
            },
            "instruction": {
                "mnemonic": "CMP",
                "operands": [
                    "EAX",
                    "imm32"
                ]
            },
            "description": "Compare"
        },
        {
            "opcode": {
                "bytes": [
                    128
                ],
                "options": [
                    "/7",
                    "ib"
                ]
            },
            "instruction": {
                "mnemonic": "CMP",
                "operands": [
                    "r/m8",
                    "imm8"
                ]
            },
            "description": "Compare"
        },
        {
            "opcode": {
                "bytes": [
                    129
                ],
                "options": [
                    "/7",
                    "id"
                ]
            },
            "instruction": {
                "mnemonic": "CMP",
                "operands": [
                    "r/m32",
                    "imm32"
                ]
            },
            "description": "Compare"
        },
        {
            "opcode": {
                "bytes": [
                    131
                ],
                "options": [
                    "/7",
                    "ib"
                ]
            },
            "instruction": {
                "mnemonic": "CMP",
                "operands": [
                    "r/m32",
                    "imm8"
                ]
            },
            "description": "Compare"
        },
        {
            "opcode": {
                "bytes": [
                    56
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "CMP",
                "operands": [
                    "r/m8",
                    "r8"
                ]
            },
            "description": "Compare"
        },
        {
            "opcode": {
                "bytes": [
                    57
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "CMP",
                "operands": [
                    "r/m32",
                    "r32"
                ]
            },
            "description": "Compare"
        },
        {
            "opcode": {
                "bytes": [
                    58
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "CMP",
                "operands": [
                    "r8",
                    "r/m8"
                ]
            },
            "description": "Compare"
        },
        {
            "opcode": {
                "bytes": [
                    59
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "CMP",
                "operands": [
                    "r32",
                    "r/m32"
                ]
            },
            "description": "Compare"
        },
        {
            "opcode": {
                "bytes": [
                    166
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "CMPSB",
                "operands": []
            },
            "description": "Compare byte at DS:(E)SI with ES:(E)DI"
        },
        {
            "opcode": {
                "bytes": [
                    167
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "CMPSD",
                "operands": []
            },
            "description": "Compare dw   at DS:(E)SI with ES:(E)DI"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    176
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "CMPXCHG",
                "operands": [
                    "r/m8",
                    "r8"
                ]
            },
            "description": "Compare and Exchange"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    177
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "CMPXCHG",
                "operands": [
                    "r/m32",
                    "r32"
                ]
            },
            "description": "Compare and Exchange"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    199
                ],
                "options": [
                    "/1",
                    "m64"
                ]
            },
            "instruction": {
                "mnemonic": "CMPXCHG8B",
                "operands": [
                    "m"
                ]
            },
            "description": "64           Compare and Exchange"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    162
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "CPUID",
                "operands": [
                    "EAX"
                ]
            },
            "description": ":= Processor id.info."
        },
        {
            "opcode": {
                "bytes": [
                    39
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "DAA",
                "operands": []
            },
            "description": "Decimal adjust AL after addition"
        },
        {
            "opcode": {
                "bytes": [
                    47
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "DAS",
                "operands": []
            },
            "description": "Decimal adjust AL after subtraction"
        },
        {
            "opcode": {
                "bytes": [
                    254
                ],
                "options": [
                    "/1"
                ]
            },
            "instruction": {
                "mnemonic": "DEC",
                "operands": [
                    "r/m8"
                ]
            },
            "description": "Decrement r/m8 by 1"
        },
        {
            "opcode": {
                "bytes": [
                    255
                ],
                "options": [
                    "/1"
                ]
            },
            "instruction": {
                "mnemonic": "DEC",
                "operands": [
                    "r/m32"
                ]
            },
            "description": "Decrement r/m32 by 1"
        },
        {
            "opcode": {
                "bytes": [
                    72
                ],
                "options": [
                    "+rd"
                ]
            },
            "instruction": {
                "mnemonic": "DEC",
                "operands": [
                    "r32"
                ]
            },
            "description": "Decrement r32 by 1"
        },
        {
            "opcode": {
                "bytes": [
                    246
                ],
                "options": [
                    "/6"
                ]
            },
            "instruction": {
                "mnemonic": "DIV",
                "operands": [
                    "r/m8"
                ]
            },
            "description": "Unsigned divide AX by r/m8"
        },
        {
            "opcode": {
                "bytes": [
                    247
                ],
                "options": [
                    "/6"
                ]
            },
            "instruction": {
                "mnemonic": "DIV",
                "operands": [
                    "r/m16"
                ]
            },
            "description": "Unsigned divide DX:AX by r/m16"
        },
        {
            "opcode": {
                "bytes": [
                    247
                ],
                "options": [
                    "/6"
                ]
            },
            "instruction": {
                "mnemonic": "DIV",
                "operands": [
                    "r/m32"
                ]
            },
            "description": "Unsigned divide EDX:EAX by r/m32"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    119
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "EMMS",
                "operands": []
            },
            "description": "Set the FP tag word to empty"
        },
        {
            "opcode": {
                "bytes": [
                    200
                ],
                "options": [
                    "iw",
                    "00"
                ]
            },
            "instruction": {
                "mnemonic": "ENTER",
                "operands": [
                    "imm16",
                    "0"
                ]
            },
            "description": "Create a stack frame for a procedure"
        },
        {
            "opcode": {
                "bytes": [
                    200
                ],
                "options": [
                    "iw",
                    "01"
                ]
            },
            "instruction": {
                "mnemonic": "ENTER",
                "operands": [
                    "imm16",
                    "1"
                ]
            },
            "description": "Create a nested stack frame for a proc."
        },
        {
            "opcode": {
                "bytes": [
                    200
                ],
                "options": [
                    "iw",
                    "ib"
                ]
            },
            "instruction": {
                "mnemonic": "ENTER",
                "operands": [
                    "imm16",
                    "imm8"
                ]
            },
            "description": "Create a nested stack frame for a proc."
        },
        {
            "opcode": {
                "bytes": [
                    217,
                    240
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "F2XM1",
                "operands": []
            },
            "description": "Replace ST(0) with 2**ST(0) - 1"
        },
        {
            "opcode": {
                "bytes": [
                    217,
                    225
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "FABS",
                "operands": []
            },
            "description": "Replace ST(0) with its absolute value"
        },
        {
            "opcode": {
                "bytes": [
                    216
                ],
                "options": [
                    "/0"
                ]
            },
            "instruction": {
                "mnemonic": "FADD",
                "operands": [
                    "m32real"
                ]
            },
            "description": "Add m32real to ST(0) and s.r. in ST(0)"
        },
        {
            "opcode": {
                "bytes": [
                    220
                ],
                "options": [
                    "/0"
                ]
            },
            "instruction": {
                "mnemonic": "FADD",
                "operands": [
                    "m64real"
                ]
            },
            "description": "Add m64real to ST(0) and s.r.in ST(0)"
        },
        {
            "opcode": {
                "bytes": [
                    216,
                    192
                ],
                "options": [
                    "+i"
                ]
            },
            "instruction": {
                "mnemonic": "FADD",
                "operands": [
                    "ST(0)",
                    "ST(i)"
                ]
            },
            "description": "Add ST(0) to ST(i) and s.r.in ST(0)"
        },
        {
            "opcode": {
                "bytes": [
                    220,
                    192
                ],
                "options": [
                    "+i"
                ]
            },
            "instruction": {
                "mnemonic": "FADD",
                "operands": [
                    "ST(i)",
                    "ST(0)"
                ]
            },
            "description": "Add ST(i) to ST(0) and s.r. in ST(i)"
        },
        {
            "opcode": {
                "bytes": [
                    222,
                    192
                ],
                "options": [
                    "+i"
                ]
            },
            "instruction": {
                "mnemonic": "FADDP",
                "operands": [
                    "ST(i)",
                    "ST(0)"
                ]
            },
            "description": "Add ST(0) to ST(i), s.r.in ST(i),pop r.stack"
        },
        {
            "opcode": {
                "bytes": [
                    222,
                    193
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "FADDP",
                "operands": []
            },
            "description": "Add ST(0) to ST(1), s.r.in ST(1),pop r.stack"
        },
        {
            "opcode": {
                "bytes": [
                    218
                ],
                "options": [
                    "/0"
                ]
            },
            "instruction": {
                "mnemonic": "FIADD",
                "operands": [
                    "m32int"
                ]
            },
            "description": "Add m32int to ST(0) and s.r.in ST(0)"
        },
        {
            "opcode": {
                "bytes": [
                    222
                ],
                "options": [
                    "/0"
                ]
            },
            "instruction": {
                "mnemonic": "FIADD",
                "operands": [
                    "m16int"
                ]
            },
            "description": "Add m16int to ST(0) and s.r.in ST(0)"
        },
        {
            "opcode": {
                "bytes": [
                    223
                ],
                "options": [
                    "/4"
                ]
            },
            "instruction": {
                "mnemonic": "FBLD",
                "operands": [
                    "m80bcd"
                ]
            },
            "description": "Convert m80BCD to real and push"
        },
        {
            "opcode": {
                "bytes": [
                    223
                ],
                "options": [
                    "/6"
                ]
            },
            "instruction": {
                "mnemonic": "FBSTP",
                "operands": [
                    "m80bcd"
                ]
            },
            "description": "Store ST(0) in m80bcd and pop ST(0)"
        },
        {
            "opcode": {
                "bytes": [
                    217,
                    224
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "FCHS",
                "operands": []
            },
            "description": "Complements sign of ST(0)"
        },
        {
            "opcode": {
                "bytes": [
                    155,
                    219,
                    226
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "FCLEX",
                "operands": []
            },
            "description": "Clear f.e.f. after checking for .."
        },
        {
            "opcode": {
                "bytes": [
                    219,
                    226
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "FNCLEX",
                "operands": []
            },
            "description": "Clear f.e.f. without checking for .."
        },
        {
            "opcode": {
                "bytes": [
                    218,
                    192
                ],
                "options": [
                    "+i"
                ]
            },
            "instruction": {
                "mnemonic": "FCMOVB",
                "operands": [
                    "ST(0)",
                    "ST(i)"
                ]
            },
            "description": "Move if below"
        },
        {
            "opcode": {
                "bytes": [
                    218,
                    200
                ],
                "options": [
                    "+i"
                ]
            },
            "instruction": {
                "mnemonic": "FCMOVE",
                "operands": [
                    "ST(0)",
                    "ST(i)"
                ]
            },
            "description": "Move if equal"
        },
        {
            "opcode": {
                "bytes": [
                    218,
                    208
                ],
                "options": [
                    "+i"
                ]
            },
            "instruction": {
                "mnemonic": "FCMOVBE",
                "operands": [
                    "ST(0)",
                    "ST(i)"
                ]
            },
            "description": "Move if below or equal"
        },
        {
            "opcode": {
                "bytes": [
                    218,
                    216
                ],
                "options": [
                    "+i"
                ]
            },
            "instruction": {
                "mnemonic": "FCMOVU",
                "operands": [
                    "ST(0)",
                    "ST(i)"
                ]
            },
            "description": "Move if unordered"
        },
        {
            "opcode": {
                "bytes": [
                    219,
                    192
                ],
                "options": [
                    "+i"
                ]
            },
            "instruction": {
                "mnemonic": "FCMOVNB",
                "operands": [
                    "ST(0)",
                    "ST(i)"
                ]
            },
            "description": "Move if not below"
        },
        {
            "opcode": {
                "bytes": [
                    219,
                    200
                ],
                "options": [
                    "+i"
                ]
            },
            "instruction": {
                "mnemonic": "FCMOVNE",
                "operands": [
                    "ST(0)",
                    "ST(i)"
                ]
            },
            "description": "Move if not equal"
        },
        {
            "opcode": {
                "bytes": [
                    219,
                    208
                ],
                "options": [
                    "+i"
                ]
            },
            "instruction": {
                "mnemonic": "FCMOVNBE",
                "operands": [
                    "ST(0)",
                    "ST(i)"
                ]
            },
            "description": "Move if not below or equal"
        },
        {
            "opcode": {
                "bytes": [
                    219,
                    216
                ],
                "options": [
                    "+i"
                ]
            },
            "instruction": {
                "mnemonic": "FCMOVNU",
                "operands": [
                    "ST(0)",
                    "ST(i)"
                ]
            },
            "description": "Move if not unordered"
        },
        {
            "opcode": {
                "bytes": [
                    216
                ],
                "options": [
                    "/2"
                ]
            },
            "instruction": {
                "mnemonic": "FCOM",
                "operands": [
                    "m32real"
                ]
            },
            "description": "Compare ST(0) with m32real."
        },
        {
            "opcode": {
                "bytes": [
                    220
                ],
                "options": [
                    "/2"
                ]
            },
            "instruction": {
                "mnemonic": "FCOM",
                "operands": [
                    "m64real"
                ]
            },
            "description": "Compare ST(0) with m64real."
        },
        {
            "opcode": {
                "bytes": [
                    216,
                    208
                ],
                "options": [
                    "+i"
                ]
            },
            "instruction": {
                "mnemonic": "FCOM",
                "operands": [
                    "ST(i)"
                ]
            },
            "description": "Compare ST(0) with ST(i)."
        },
        {
            "opcode": {
                "bytes": [
                    216,
                    209
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "FCOM",
                "operands": []
            },
            "description": "Compare ST(0) with ST(1)."
        },
        {
            "opcode": {
                "bytes": [
                    216
                ],
                "options": [
                    "/3"
                ]
            },
            "instruction": {
                "mnemonic": "FCOMP",
                "operands": [
                    "m32real"
                ]
            },
            "description": "Compare ST(0) with m32real,pop r.stack."
        },
        {
            "opcode": {
                "bytes": [
                    220
                ],
                "options": [
                    "/3"
                ]
            },
            "instruction": {
                "mnemonic": "FCOMP",
                "operands": [
                    "m64real"
                ]
            },
            "description": "Compare ST(0) with m64real,pop r.stack."
        },
        {
            "opcode": {
                "bytes": [
                    216,
                    216
                ],
                "options": [
                    "+i"
                ]
            },
            "instruction": {
                "mnemonic": "FCOMP",
                "operands": [
                    "ST(i)"
                ]
            },
            "description": "Compare ST(0) with ST(i), pop"
        },
        {
            "opcode": {
                "bytes": [
                    216,
                    217
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "FCOMP",
                "operands": []
            },
            "description": "Compare ST(0) with ST(1), pop"
        },
        {
            "opcode": {
                "bytes": [
                    222,
                    217
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "FCOMPP",
                "operands": []
            },
            "description": "Compare ST(0) with ST(1), pop pop"
        },
        {
            "opcode": {
                "bytes": [
                    219,
                    240
                ],
                "options": [
                    "+i"
                ]
            },
            "instruction": {
                "mnemonic": "FCOMI",
                "operands": []
            },
            "description": "ST,ST(i)          Compare ST(0) with ST(i), set status flags"
        },
        {
            "opcode": {
                "bytes": [
                    223,
                    240
                ],
                "options": [
                    "+i"
                ]
            },
            "instruction": {
                "mnemonic": "FCOMIP",
                "operands": []
            },
            "description": "ST,ST(i)         Compare ST(0) with ST(i), set s.f. ,pop"
        },
        {
            "opcode": {
                "bytes": [
                    219,
                    232
                ],
                "options": [
                    "+i"
                ]
            },
            "instruction": {
                "mnemonic": "FUCOMI",
                "operands": []
            },
            "description": "ST,ST(i)         Compare ST(0) with ST(i), check o.v.set s.f."
        },
        {
            "opcode": {
                "bytes": [
                    223,
                    232
                ],
                "options": [
                    "+i"
                ]
            },
            "instruction": {
                "mnemonic": "FUCOMIP",
                "operands": []
            },
            "description": "ST,ST(i)        Compare ST(0) with ST(i), check ovssf pop"
        },
        {
            "opcode": {
                "bytes": [
                    217,
                    255
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "FCOS",
                "operands": []
            },
            "description": "Replace ST(0) with its cosine"
        },
        {
            "opcode": {
                "bytes": [
                    217,
                    246
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "FDECSTP",
                "operands": []
            },
            "description": "Decrement TOP field in FPU status word."
        },
        {
            "opcode": {
                "bytes": [
                    216
                ],
                "options": [
                    "/6"
                ]
            },
            "instruction": {
                "mnemonic": "FDIV",
                "operands": [
                    "m32real"
                ]
            },
            "description": "Divide ST(0) by m32real and s.r.in ST(0)"
        },
        {
            "opcode": {
                "bytes": [
                    220
                ],
                "options": [
                    "/6"
                ]
            },
            "instruction": {
                "mnemonic": "FDIV",
                "operands": [
                    "m64real"
                ]
            },
            "description": "Divide ST(0) by m64real and s.r.in ST(0)"
        },
        {
            "opcode": {
                "bytes": [
                    216,
                    240
                ],
                "options": [
                    "+i"
                ]
            },
            "instruction": {
                "mnemonic": "FDIV",
                "operands": [
                    "ST(0)",
                    "ST(i)"
                ]
            },
            "description": "Divide ST(0) by ST(i) and s.r.in ST(0)"
        },
        {
            "opcode": {
                "bytes": [
                    220,
                    248
                ],
                "options": [
                    "+i"
                ]
            },
            "instruction": {
                "mnemonic": "FDIV",
                "operands": [
                    "ST(i)",
                    "ST(0)"
                ]
            },
            "description": "Divide ST(i) by ST(0) and s.r.in ST(i)"
        },
        {
            "opcode": {
                "bytes": [
                    222,
                    248
                ],
                "options": [
                    "+i"
                ]
            },
            "instruction": {
                "mnemonic": "FDIVP",
                "operands": [
                    "ST(i)",
                    "ST(0)"
                ]
            },
            "description": "Divide ST(i) by ST(0), s.r.in ST(i) pop"
        },
        {
            "opcode": {
                "bytes": [
                    222,
                    249
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "FDIVP",
                "operands": []
            },
            "description": "Divide ST(1) by ST(0), s.r.in ST(1) pop"
        },
        {
            "opcode": {
                "bytes": [
                    218
                ],
                "options": [
                    "/6"
                ]
            },
            "instruction": {
                "mnemonic": "FIDIV",
                "operands": [
                    "m32int"
                ]
            },
            "description": "Divide ST(0) by m32int and s.r.in ST(0)"
        },
        {
            "opcode": {
                "bytes": [
                    222
                ],
                "options": [
                    "/6"
                ]
            },
            "instruction": {
                "mnemonic": "FIDIV",
                "operands": [
                    "m16int"
                ]
            },
            "description": "Divide ST(0) by m64int and s.r.in ST(0)"
        },
        {
            "opcode": {
                "bytes": [
                    216
                ],
                "options": [
                    "/7"
                ]
            },
            "instruction": {
                "mnemonic": "FDIVR",
                "operands": [
                    "m32real"
                ]
            },
            "description": "Divide m32real by ST(0) and s.r.in ST(0)"
        },
        {
            "opcode": {
                "bytes": [
                    220
                ],
                "options": [
                    "/7"
                ]
            },
            "instruction": {
                "mnemonic": "FDIVR",
                "operands": [
                    "m64real"
                ]
            },
            "description": "Divide m64real by ST(0) and s.r.in ST(0)"
        },
        {
            "opcode": {
                "bytes": [
                    216,
                    248
                ],
                "options": [
                    "+i"
                ]
            },
            "instruction": {
                "mnemonic": "FDIVR",
                "operands": [
                    "ST(0)",
                    "ST(i)"
                ]
            },
            "description": "Divide ST(i) by ST(0) and s.r.in ST(0)"
        },
        {
            "opcode": {
                "bytes": [
                    220,
                    240
                ],
                "options": [
                    "+i"
                ]
            },
            "instruction": {
                "mnemonic": "FDIVR",
                "operands": [
                    "ST(i)",
                    "ST(0)"
                ]
            },
            "description": "Divide ST(0) by ST(i) and s.r.in ST(i)"
        },
        {
            "opcode": {
                "bytes": [
                    222,
                    240
                ],
                "options": [
                    "+i"
                ]
            },
            "instruction": {
                "mnemonic": "FDIVRP",
                "operands": [
                    "ST(i)",
                    "ST(0)"
                ]
            },
            "description": "Divide ST(0) by ST(i), s.r.in ST(i) pop"
        },
        {
            "opcode": {
                "bytes": [
                    222,
                    241
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "FDIVRP",
                "operands": []
            },
            "description": "Divide ST(0) by ST(1), s.r.in ST(1) pop"
        },
        {
            "opcode": {
                "bytes": [
                    218
                ],
                "options": [
                    "/7"
                ]
            },
            "instruction": {
                "mnemonic": "FIDIVR",
                "operands": [
                    "m32int"
                ]
            },
            "description": "Divide m32int by ST(0) and s.r.in ST(0)"
        },
        {
            "opcode": {
                "bytes": [
                    222
                ],
                "options": [
                    "/7"
                ]
            },
            "instruction": {
                "mnemonic": "FIDIVR",
                "operands": [
                    "m16int"
                ]
            },
            "description": "Divide m64int by ST(0) and s.r.in ST(0)"
        },
        {
            "opcode": {
                "bytes": [
                    221,
                    192
                ],
                "options": [
                    "+i"
                ]
            },
            "instruction": {
                "mnemonic": "FFREE",
                "operands": [
                    "ST(i)"
                ]
            },
            "description": "Sets tag for ST(i) to empty"
        },
        {
            "opcode": {
                "bytes": [
                    222
                ],
                "options": [
                    "/2"
                ]
            },
            "instruction": {
                "mnemonic": "FICOM",
                "operands": [
                    "m16int"
                ]
            },
            "description": "Compare ST(0) with m16int"
        },
        {
            "opcode": {
                "bytes": [
                    218
                ],
                "options": [
                    "/2"
                ]
            },
            "instruction": {
                "mnemonic": "FICOM",
                "operands": [
                    "m32int"
                ]
            },
            "description": "Compare ST(0) with m32int"
        },
        {
            "opcode": {
                "bytes": [
                    222
                ],
                "options": [
                    "/3"
                ]
            },
            "instruction": {
                "mnemonic": "FICOMP",
                "operands": [
                    "m16int"
                ]
            },
            "description": "Compare ST(0) with m16int and pop"
        },
        {
            "opcode": {
                "bytes": [
                    218
                ],
                "options": [
                    "/3"
                ]
            },
            "instruction": {
                "mnemonic": "FICOMP",
                "operands": [
                    "m32int"
                ]
            },
            "description": "Compare ST(0) with m32int and pop"
        },
        {
            "opcode": {
                "bytes": [
                    223
                ],
                "options": [
                    "/0"
                ]
            },
            "instruction": {
                "mnemonic": "FILD",
                "operands": [
                    "m16int"
                ]
            },
            "description": "Push m16int"
        },
        {
            "opcode": {
                "bytes": [
                    219
                ],
                "options": [
                    "/0"
                ]
            },
            "instruction": {
                "mnemonic": "FILD",
                "operands": [
                    "m32int"
                ]
            },
            "description": "Push m32int"
        },
        {
            "opcode": {
                "bytes": [
                    223
                ],
                "options": [
                    "/5"
                ]
            },
            "instruction": {
                "mnemonic": "FILD",
                "operands": [
                    "m64int"
                ]
            },
            "description": "Push m64int"
        },
        {
            "opcode": {
                "bytes": [
                    217,
                    247
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "FINCSTP",
                "operands": []
            },
            "description": "Increment the TOP field FPU status r."
        },
        {
            "opcode": {
                "bytes": [
                    155,
                    219,
                    227
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "FINIT",
                "operands": []
            },
            "description": "Initialize FPU after ..."
        },
        {
            "opcode": {
                "bytes": [
                    219,
                    227
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "FNINIT",
                "operands": []
            },
            "description": "Initialize FPU without ..."
        },
        {
            "opcode": {
                "bytes": [
                    223
                ],
                "options": [
                    "/2"
                ]
            },
            "instruction": {
                "mnemonic": "FIST",
                "operands": [
                    "m16int"
                ]
            },
            "description": "Store ST(0) in m16int"
        },
        {
            "opcode": {
                "bytes": [
                    219
                ],
                "options": [
                    "/2"
                ]
            },
            "instruction": {
                "mnemonic": "FIST",
                "operands": [
                    "m32int"
                ]
            },
            "description": "Store ST(0) in m32int"
        },
        {
            "opcode": {
                "bytes": [
                    223
                ],
                "options": [
                    "/3"
                ]
            },
            "instruction": {
                "mnemonic": "FISTP",
                "operands": [
                    "m16int"
                ]
            },
            "description": "Store ST(0) in m16int and pop"
        },
        {
            "opcode": {
                "bytes": [
                    219
                ],
                "options": [
                    "/3"
                ]
            },
            "instruction": {
                "mnemonic": "FISTP",
                "operands": [
                    "m32int"
                ]
            },
            "description": "Store ST(0) in m32int and pop"
        },
        {
            "opcode": {
                "bytes": [
                    223
                ],
                "options": [
                    "/7"
                ]
            },
            "instruction": {
                "mnemonic": "FISTP",
                "operands": [
                    "m64int"
                ]
            },
            "description": "Store ST(0) in m64int and pop"
        },
        {
            "opcode": {
                "bytes": [
                    217
                ],
                "options": [
                    "/0"
                ]
            },
            "instruction": {
                "mnemonic": "FLD",
                "operands": [
                    "m32real"
                ]
            },
            "description": "Push m32real"
        },
        {
            "opcode": {
                "bytes": [
                    221
                ],
                "options": [
                    "/0"
                ]
            },
            "instruction": {
                "mnemonic": "FLD",
                "operands": [
                    "m64real"
                ]
            },
            "description": "Push m64real"
        },
        {
            "opcode": {
                "bytes": [
                    219
                ],
                "options": [
                    "/5"
                ]
            },
            "instruction": {
                "mnemonic": "FLD",
                "operands": [
                    "m80real"
                ]
            },
            "description": "Push m80real"
        },
        {
            "opcode": {
                "bytes": [
                    217,
                    192
                ],
                "options": [
                    "+i"
                ]
            },
            "instruction": {
                "mnemonic": "FLD",
                "operands": [
                    "ST(i)"
                ]
            },
            "description": "Push ST(i)"
        },
        {
            "opcode": {
                "bytes": [
                    217,
                    232
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "FLD1",
                "operands": []
            },
            "description": "Push +1.0"
        },
        {
            "opcode": {
                "bytes": [
                    217,
                    233
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "FLDL2T",
                "operands": []
            },
            "description": "Push log2 10"
        },
        {
            "opcode": {
                "bytes": [
                    217,
                    234
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "FLDL2E",
                "operands": []
            },
            "description": "Push log2 e"
        },
        {
            "opcode": {
                "bytes": [
                    217,
                    235
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "FLDPI",
                "operands": []
            },
            "description": "Push pi"
        },
        {
            "opcode": {
                "bytes": [
                    217,
                    236
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "FLDLG2",
                "operands": []
            },
            "description": "Push log10 2"
        },
        {
            "opcode": {
                "bytes": [
                    217,
                    237
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "FLDLN2",
                "operands": []
            },
            "description": "Push loge 2"
        },
        {
            "opcode": {
                "bytes": [
                    217,
                    238
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "FLDZ",
                "operands": []
            },
            "description": "Push +0.0"
        },
        {
            "opcode": {
                "bytes": [
                    217
                ],
                "options": [
                    "/5"
                ]
            },
            "instruction": {
                "mnemonic": "FLDCW",
                "operands": [
                    "m2byte"
                ]
            },
            "description": "Load FPU control word from m2byte"
        },
        {
            "opcode": {
                "bytes": [
                    217
                ],
                "options": [
                    "/4"
                ]
            },
            "instruction": {
                "mnemonic": "FLDENV",
                "operands": [
                    "m14/28byte"
                ]
            },
            "description": "Load FPU environment from m14/m28"
        },
        {
            "opcode": {
                "bytes": [
                    216
                ],
                "options": [
                    "/1"
                ]
            },
            "instruction": {
                "mnemonic": "FMUL",
                "operands": [
                    "m32real"
                ]
            },
            "description": "Multiply ST(0) by m32real and s.r.in ST(0)"
        },
        {
            "opcode": {
                "bytes": [
                    220
                ],
                "options": [
                    "/1"
                ]
            },
            "instruction": {
                "mnemonic": "FMUL",
                "operands": [
                    "m64real"
                ]
            },
            "description": "Multiply ST(0) by m64real and s.r.in ST(0)"
        },
        {
            "opcode": {
                "bytes": [
                    216,
                    200
                ],
                "options": [
                    "+i"
                ]
            },
            "instruction": {
                "mnemonic": "FMUL",
                "operands": [
                    "ST(0)",
                    "ST(i)"
                ]
            },
            "description": "Multiply ST(0) by ST(i) and s.r.in ST(0)"
        },
        {
            "opcode": {
                "bytes": [
                    220,
                    200
                ],
                "options": [
                    "+i"
                ]
            },
            "instruction": {
                "mnemonic": "FMUL",
                "operands": [
                    "ST(i)",
                    "ST(0)"
                ]
            },
            "description": "Multiply ST(i) by ST(0) and s.r.in ST(i)"
        },
        {
            "opcode": {
                "bytes": [
                    222,
                    200
                ],
                "options": [
                    "+i"
                ]
            },
            "instruction": {
                "mnemonic": "FMULP",
                "operands": [
                    "ST(i)",
                    "ST(0)"
                ]
            },
            "description": "Multiply ST(i) by ST(0), s.r.in ST(i) pop"
        },
        {
            "opcode": {
                "bytes": [
                    222,
                    201
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "FMULP",
                "operands": []
            },
            "description": "Multiply ST(1) by ST(0), s.r.in ST(1) pop"
        },
        {
            "opcode": {
                "bytes": [
                    218
                ],
                "options": [
                    "/1"
                ]
            },
            "instruction": {
                "mnemonic": "FIMUL",
                "operands": [
                    "m32int"
                ]
            },
            "description": "Multiply ST(0) by m32int and s.r.in ST(0)"
        },
        {
            "opcode": {
                "bytes": [
                    222
                ],
                "options": [
                    "/1"
                ]
            },
            "instruction": {
                "mnemonic": "FIMUL",
                "operands": [
                    "m16int"
                ]
            },
            "description": "Multiply ST(0) by m16int and s.r.in ST(0)"
        },
        {
            "opcode": {
                "bytes": [
                    217,
                    208
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "FNOP",
                "operands": []
            },
            "description": "No operation is performed"
        },
        {
            "opcode": {
                "bytes": [
                    217,
                    243
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "FPATAN",
                "operands": []
            },
            "description": "Repalces ST(1) with arctan(ST(1)/ST(0)) pop"
        },
        {
            "opcode": {
                "bytes": [
                    217,
                    248
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "FPREM",
                "operands": []
            },
            "description": "Replaces ST(0) with rem (ST(0)/ST(1))"
        },
        {
            "opcode": {
                "bytes": [
                    217,
                    245
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "FPREM1",
                "operands": []
            },
            "description": "Replaces ST(0) with IEEE rem(ST(0)/ST(1))"
        },
        {
            "opcode": {
                "bytes": [
                    217,
                    242
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "FPTAN",
                "operands": []
            },
            "description": "Replaces ST(0) with its tangent push 1.0"
        },
        {
            "opcode": {
                "bytes": [
                    217,
                    252
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "FRNDINT",
                "operands": []
            },
            "description": "Round ST(0) to an integer"
        },
        {
            "opcode": {
                "bytes": [
                    221
                ],
                "options": [
                    "/4"
                ]
            },
            "instruction": {
                "mnemonic": "FRSTOR",
                "operands": [
                    "m94/108byte"
                ]
            },
            "description": "Load FPU status from m94 or m108 byte"
        },
        {
            "opcode": {
                "bytes": [
                    155,
                    221
                ],
                "options": [
                    "/6"
                ]
            },
            "instruction": {
                "mnemonic": "FSAVE",
                "operands": [
                    "m94/108byte"
                ]
            },
            "description": "Store FPU status to m94 or m108"
        },
        {
            "opcode": {
                "bytes": [
                    221
                ],
                "options": [
                    "/6"
                ]
            },
            "instruction": {
                "mnemonic": "FNSAVE",
                "operands": [
                    "m94/108byte"
                ]
            },
            "description": "Store FPU environment to m94 or m108"
        },
        {
            "opcode": {
                "bytes": [
                    217,
                    253
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "FSCALE",
                "operands": []
            },
            "description": "Scale ST(0) by ST(1)"
        },
        {
            "opcode": {
                "bytes": [
                    217,
                    254
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "FSIN",
                "operands": []
            },
            "description": "Replace ST(0) with its sine"
        },
        {
            "opcode": {
                "bytes": [
                    217,
                    251
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "FSINCOS",
                "operands": []
            },
            "description": "Compute sine and consine of ST(0) s push c"
        },
        {
            "opcode": {
                "bytes": [
                    217,
                    250
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "FSQRT",
                "operands": []
            },
            "description": "square root of ST(0)"
        },
        {
            "opcode": {
                "bytes": [
                    217
                ],
                "options": [
                    "/2"
                ]
            },
            "instruction": {
                "mnemonic": "FST",
                "operands": [
                    "m32real"
                ]
            },
            "description": "Copy ST(0) to m32real"
        },
        {
            "opcode": {
                "bytes": [
                    221
                ],
                "options": [
                    "/2"
                ]
            },
            "instruction": {
                "mnemonic": "FST",
                "operands": [
                    "m64real"
                ]
            },
            "description": "Copy ST(0) to m64real"
        },
        {
            "opcode": {
                "bytes": [
                    221,
                    208
                ],
                "options": [
                    "+i"
                ]
            },
            "instruction": {
                "mnemonic": "FST",
                "operands": [
                    "ST(i)"
                ]
            },
            "description": "Copy ST(0) to ST(i)"
        },
        {
            "opcode": {
                "bytes": [
                    217
                ],
                "options": [
                    "/3"
                ]
            },
            "instruction": {
                "mnemonic": "FSTP",
                "operands": [
                    "m32real"
                ]
            },
            "description": "Copy ST(0) to m32real and pop"
        },
        {
            "opcode": {
                "bytes": [
                    221
                ],
                "options": [
                    "/3"
                ]
            },
            "instruction": {
                "mnemonic": "FSTP",
                "operands": [
                    "m64real"
                ]
            },
            "description": "Copy ST(0) to m64real and pop"
        },
        {
            "opcode": {
                "bytes": [
                    219
                ],
                "options": [
                    "/7"
                ]
            },
            "instruction": {
                "mnemonic": "FSTP",
                "operands": [
                    "m80real"
                ]
            },
            "description": "Copy ST(0) to m80real and pop"
        },
        {
            "opcode": {
                "bytes": [
                    221,
                    216
                ],
                "options": [
                    "+i"
                ]
            },
            "instruction": {
                "mnemonic": "FSTP",
                "operands": [
                    "ST(i)"
                ]
            },
            "description": "Copy ST(0) to ST(i) and pop"
        },
        {
            "opcode": {
                "bytes": [
                    155,
                    217
                ],
                "options": [
                    "/7"
                ]
            },
            "instruction": {
                "mnemonic": "FSTCW",
                "operands": [
                    "m2byte"
                ]
            },
            "description": "Store FPU control word"
        },
        {
            "opcode": {
                "bytes": [
                    217
                ],
                "options": [
                    "/7"
                ]
            },
            "instruction": {
                "mnemonic": "FNSTCW",
                "operands": [
                    "m2byte"
                ]
            },
            "description": "Store FPU control word without"
        },
        {
            "opcode": {
                "bytes": [
                    155,
                    217
                ],
                "options": [
                    "/6"
                ]
            },
            "instruction": {
                "mnemonic": "FSTENV",
                "operands": [
                    "m14/28byte"
                ]
            },
            "description": "Store FPU environment"
        },
        {
            "opcode": {
                "bytes": [
                    217
                ],
                "options": [
                    "/6"
                ]
            },
            "instruction": {
                "mnemonic": "FNSTENV",
                "operands": [
                    "m14/28byte"
                ]
            },
            "description": "Store FPU env without"
        },
        {
            "opcode": {
                "bytes": [
                    155,
                    221
                ],
                "options": [
                    "/7"
                ]
            },
            "instruction": {
                "mnemonic": "FSTSW",
                "operands": [
                    "m2byte"
                ]
            },
            "description": "Store FPU status word at m2byte after"
        },
        {
            "opcode": {
                "bytes": [
                    155,
                    223,
                    224
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "FSTSW",
                "operands": [
                    "AX"
                ]
            },
            "description": "Store FPU status word in AX  after"
        },
        {
            "opcode": {
                "bytes": [
                    221
                ],
                "options": [
                    "/7"
                ]
            },
            "instruction": {
                "mnemonic": "FNSTSW",
                "operands": [
                    "m2byte"
                ]
            },
            "description": "Store FPU status word at m2byte without"
        },
        {
            "opcode": {
                "bytes": [
                    223,
                    224
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "FNSTSW",
                "operands": [
                    "AX"
                ]
            },
            "description": "Store FPU status word in AX without"
        },
        {
            "opcode": {
                "bytes": [
                    216
                ],
                "options": [
                    "/4"
                ]
            },
            "instruction": {
                "mnemonic": "FSUB",
                "operands": [
                    "m32real"
                ]
            },
            "description": "Sub m32real from ST(0) and s.r.in ST(0)"
        },
        {
            "opcode": {
                "bytes": [
                    220
                ],
                "options": [
                    "/4"
                ]
            },
            "instruction": {
                "mnemonic": "FSUB",
                "operands": [
                    "m64real"
                ]
            },
            "description": "Sub m64real from ST(0) and s.r.in ST(0)"
        },
        {
            "opcode": {
                "bytes": [
                    216,
                    224
                ],
                "options": [
                    "+i"
                ]
            },
            "instruction": {
                "mnemonic": "FSUB",
                "operands": [
                    "ST(0)",
                    "ST(i)"
                ]
            },
            "description": "Sub ST(i) from ST(0) and s.r.in ST(0)"
        },
        {
            "opcode": {
                "bytes": [
                    220,
                    232
                ],
                "options": [
                    "+i"
                ]
            },
            "instruction": {
                "mnemonic": "FSUB",
                "operands": [
                    "ST(i)",
                    "ST(0)"
                ]
            },
            "description": "Sub ST(0) from ST(i) and s.r.in ST(i)"
        },
        {
            "opcode": {
                "bytes": [
                    222,
                    232
                ],
                "options": [
                    "+i"
                ]
            },
            "instruction": {
                "mnemonic": "FSUBP",
                "operands": [
                    "ST(i)",
                    "ST(0)"
                ]
            },
            "description": "Sub ST(0) from ST(i), s.r.in ST(i) pop"
        },
        {
            "opcode": {
                "bytes": [
                    222,
                    233
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "FSUBP",
                "operands": []
            },
            "description": "Sub ST(0) from ST(1), s.r.in ST(1) pop"
        },
        {
            "opcode": {
                "bytes": [
                    218
                ],
                "options": [
                    "/4"
                ]
            },
            "instruction": {
                "mnemonic": "FISUB",
                "operands": [
                    "m32int"
                ]
            },
            "description": "Sub m32int from ST(0) and s.r.in ST(0)"
        },
        {
            "opcode": {
                "bytes": [
                    222
                ],
                "options": [
                    "/4"
                ]
            },
            "instruction": {
                "mnemonic": "FISUB",
                "operands": [
                    "m16int"
                ]
            },
            "description": "Sub m16int from ST(0) and s.r.in ST(0)"
        },
        {
            "opcode": {
                "bytes": [
                    216
                ],
                "options": [
                    "/5"
                ]
            },
            "instruction": {
                "mnemonic": "FSUBR",
                "operands": [
                    "m32real"
                ]
            },
            "description": "Sub ST(0) from m32real and s.r.in ST(0)"
        },
        {
            "opcode": {
                "bytes": [
                    220
                ],
                "options": [
                    "/5"
                ]
            },
            "instruction": {
                "mnemonic": "FSUBR",
                "operands": [
                    "m64real"
                ]
            },
            "description": "Sub ST(0) from m64real and s.r.in ST(0)"
        },
        {
            "opcode": {
                "bytes": [
                    216,
                    232
                ],
                "options": [
                    "+i"
                ]
            },
            "instruction": {
                "mnemonic": "FSUBR",
                "operands": [
                    "ST(0)",
                    "ST(i)"
                ]
            },
            "description": "Sub ST(0) from ST(i) and s.r.in ST(0)"
        },
        {
            "opcode": {
                "bytes": [
                    220,
                    224
                ],
                "options": [
                    "+i"
                ]
            },
            "instruction": {
                "mnemonic": "FSUBR",
                "operands": [
                    "ST(i)",
                    "ST(0)"
                ]
            },
            "description": "Sub ST(i) from ST(0) and s.r.in ST(i)"
        },
        {
            "opcode": {
                "bytes": [
                    222,
                    224
                ],
                "options": [
                    "+i"
                ]
            },
            "instruction": {
                "mnemonic": "FSUBRP",
                "operands": [
                    "ST(i)",
                    "ST(0)"
                ]
            },
            "description": "Sub ST(i) from ST(0), s.r. in ST(i) pop"
        },
        {
            "opcode": {
                "bytes": [
                    222,
                    225
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "FSUBRP",
                "operands": []
            },
            "description": "Sub ST(1) from ST(0), s.r.in ST(1) pop"
        },
        {
            "opcode": {
                "bytes": [
                    218
                ],
                "options": [
                    "/5"
                ]
            },
            "instruction": {
                "mnemonic": "FISUBR",
                "operands": [
                    "m32int"
                ]
            },
            "description": "Sub ST(0) from m32int and s.r.in ST(0)"
        },
        {
            "opcode": {
                "bytes": [
                    222
                ],
                "options": [
                    "/5"
                ]
            },
            "instruction": {
                "mnemonic": "FISUBR",
                "operands": [
                    "m16int"
                ]
            },
            "description": "Sub ST(0) from m16int and s.r.in ST(0)"
        },
        {
            "opcode": {
                "bytes": [
                    217,
                    228
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "FTST",
                "operands": []
            },
            "description": "Compare ST(0) with 0.0"
        },
        {
            "opcode": {
                "bytes": [
                    221,
                    224
                ],
                "options": [
                    "+i"
                ]
            },
            "instruction": {
                "mnemonic": "FUCOM",
                "operands": [
                    "ST(i)"
                ]
            },
            "description": "Compare ST(0) with ST(i)"
        },
        {
            "opcode": {
                "bytes": [
                    221,
                    225
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "FUCOM",
                "operands": []
            },
            "description": "Compare ST(0) with ST(1)"
        },
        {
            "opcode": {
                "bytes": [
                    221,
                    232
                ],
                "options": [
                    "+i"
                ]
            },
            "instruction": {
                "mnemonic": "FUCOMP",
                "operands": [
                    "ST(i)"
                ]
            },
            "description": "Compare ST(0) with ST(i) and pop"
        },
        {
            "opcode": {
                "bytes": [
                    221,
                    233
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "FUCOMP",
                "operands": []
            },
            "description": "Compare ST(0) with ST(1) and pop"
        },
        {
            "opcode": {
                "bytes": [
                    218,
                    233
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "FUCOMPP",
                "operands": []
            },
            "description": "Compare ST(0) with ST(1) and pop pop"
        },
        {
            "opcode": {
                "bytes": [
                    217,
                    229
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "FXAM",
                "operands": []
            },
            "description": "Classify value or number in ST(0)"
        },
        {
            "opcode": {
                "bytes": [
                    217,
                    200
                ],
                "options": [
                    "+i"
                ]
            },
            "instruction": {
                "mnemonic": "FXCH",
                "operands": [
                    "ST(i)"
                ]
            },
            "description": "Exchange ST(0) and ST(i)"
        },
        {
            "opcode": {
                "bytes": [
                    217,
                    201
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "FXCH",
                "operands": []
            },
            "description": "Exchange ST(0) and ST(1)"
        },
        {
            "opcode": {
                "bytes": [
                    217,
                    244
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "FXTRACT",
                "operands": []
            },
            "description": "Seperate value in ST(0) exp. and sig."
        },
        {
            "opcode": {
                "bytes": [
                    217,
                    241
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "FYL2X",
                "operands": []
            },
            "description": "Replace ST(1) with ST(1)*log2ST(0) and pop"
        },
        {
            "opcode": {
                "bytes": [
                    217,
                    249
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "FYL2XP1",
                "operands": []
            },
            "description": "Replace ST(1) with ST(1)*log2(ST(0)+1) pop"
        },
        {
            "opcode": {
                "bytes": [
                    244
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "HLT",
                "operands": []
            },
            "description": "Halt"
        },
        {
            "opcode": {
                "bytes": [
                    246
                ],
                "options": [
                    "/7"
                ]
            },
            "instruction": {
                "mnemonic": "IDIV",
                "operands": [
                    "r/m8"
                ]
            },
            "description": "Divide"
        },
        {
            "opcode": {
                "bytes": [
                    247
                ],
                "options": [
                    "/7"
                ]
            },
            "instruction": {
                "mnemonic": "IDIV",
                "operands": [
                    "r/m32"
                ]
            },
            "description": "Divide"
        },
        {
            "opcode": {
                "bytes": [
                    246
                ],
                "options": [
                    "/5"
                ]
            },
            "instruction": {
                "mnemonic": "IMUL",
                "operands": [
                    "r/m8"
                ]
            },
            "description": "Multiply"
        },
        {
            "opcode": {
                "bytes": [
                    247
                ],
                "options": [
                    "/5"
                ]
            },
            "instruction": {
                "mnemonic": "IMUL",
                "operands": [
                    "r/m32"
                ]
            },
            "description": "Multiply"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    175
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "IMUL",
                "operands": [
                    "r32",
                    "r/m32"
                ]
            },
            "description": "Multiply"
        },
        {
            "opcode": {
                "bytes": [
                    107
                ],
                "options": [
                    "/r",
                    "ib"
                ]
            },
            "instruction": {
                "mnemonic": "IMUL",
                "operands": [
                    "r32",
                    "r/m32",
                    "imm8"
                ]
            },
            "description": "Multiply"
        },
        {
            "opcode": {
                "bytes": [
                    107
                ],
                "options": [
                    "/r",
                    "ib"
                ]
            },
            "instruction": {
                "mnemonic": "IMUL",
                "operands": [
                    "r32",
                    "imm8"
                ]
            },
            "description": "Multiply"
        },
        {
            "opcode": {
                "bytes": [
                    105
                ],
                "options": [
                    "/r",
                    "id"
                ]
            },
            "instruction": {
                "mnemonic": "IMUL",
                "operands": [
                    "r32",
                    "r/m32",
                    "imm32"
                ]
            },
            "description": "Multiply"
        },
        {
            "opcode": {
                "bytes": [
                    105
                ],
                "options": [
                    "/r",
                    "id"
                ]
            },
            "instruction": {
                "mnemonic": "IMUL",
                "operands": [
                    "r32",
                    "imm32"
                ]
            },
            "description": "Multiply"
        },
        {
            "opcode": {
                "bytes": [
                    228
                ],
                "options": [
                    "ib"
                ]
            },
            "instruction": {
                "mnemonic": "IN",
                "operands": [
                    "AL",
                    "imm8"
                ]
            },
            "description": "Input byte from imm8 I/O port address into AL"
        },
        {
            "opcode": {
                "bytes": [
                    229
                ],
                "options": [
                    "ib"
                ]
            },
            "instruction": {
                "mnemonic": "IN",
                "operands": [
                    "EAX",
                    "imm8"
                ]
            },
            "description": "Input byte from imm8 I/O port address into EAX"
        },
        {
            "opcode": {
                "bytes": [
                    236
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "IN",
                "operands": [
                    "AL",
                    "DX"
                ]
            },
            "description": "Input byte from I/O port in DX into AL"
        },
        {
            "opcode": {
                "bytes": [
                    237
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "IN",
                "operands": [
                    "EAX",
                    "DX"
                ]
            },
            "description": "Input doubleword from I/O port in DX into EAX"
        },
        {
            "opcode": {
                "bytes": [
                    254
                ],
                "options": [
                    "/0"
                ]
            },
            "instruction": {
                "mnemonic": "INC",
                "operands": [
                    "r/m8"
                ]
            },
            "description": "Increment 1"
        },
        {
            "opcode": {
                "bytes": [
                    255
                ],
                "options": [
                    "/0"
                ]
            },
            "instruction": {
                "mnemonic": "INC",
                "operands": [
                    "r/m32"
                ]
            },
            "description": "Increment 1"
        },
        {
            "opcode": {
                "bytes": [
                    64
                ],
                "options": [
                    "+rd"
                ]
            },
            "instruction": {
                "mnemonic": "INC",
                "operands": [
                    "r32"
                ]
            },
            "description": "Increment register by 1"
        },
        {
            "opcode": {
                "bytes": [
                    108
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "INS",
                "operands": [
                    "m8"
                ]
            },
            "description": "Input byte from I/O(DX) into  ES:(E)DI"
        },
        {
            "opcode": {
                "bytes": [
                    109
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "INS",
                "operands": [
                    "m32"
                ]
            },
            "description": "Input dw from I/O(DX) into ES:(E)DI"
        },
        {
            "opcode": {
                "bytes": [
                    204
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "INT",
                "operands": []
            },
            "description": "3                   Interrupt 3--trap to debugger"
        },
        {
            "opcode": {
                "bytes": [
                    205
                ],
                "options": [
                    "ib"
                ]
            },
            "instruction": {
                "mnemonic": "INT",
                "operands": [
                    "imm8"
                ]
            },
            "description": "Interrupt vector number (imm8)"
        },
        {
            "opcode": {
                "bytes": [
                    206
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "INTO",
                "operands": []
            },
            "description": "Interrupt 4--if overflow flag is 1"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    8
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "INVD",
                "operands": []
            },
            "description": "Flush internal caches"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    1
                ],
                "options": [
                    "/7"
                ]
            },
            "instruction": {
                "mnemonic": "INVLPG",
                "operands": [
                    "m"
                ]
            },
            "description": "Invalidate TLB Entry for page (m)"
        },
        {
            "opcode": {
                "bytes": [
                    207
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "IRETD",
                "operands": []
            },
            "description": "Interrupt return(32)"
        },
        {
            "opcode": {
                "bytes": [
                    119
                ],
                "options": [
                    "cb"
                ]
            },
            "instruction": {
                "mnemonic": "JA",
                "operands": [
                    "rel8"
                ]
            },
            "description": "Jump short if above"
        },
        {
            "opcode": {
                "bytes": [
                    115
                ],
                "options": [
                    "cb"
                ]
            },
            "instruction": {
                "mnemonic": "JAE",
                "operands": [
                    "rel8"
                ]
            },
            "description": "Jump short if above or equal"
        },
        {
            "opcode": {
                "bytes": [
                    118
                ],
                "options": [
                    "cb"
                ]
            },
            "instruction": {
                "mnemonic": "JBE",
                "operands": [
                    "rel8"
                ]
            },
            "description": "Jump short if below or equal"
        },
        {
            "opcode": {
                "bytes": [
                    114
                ],
                "options": [
                    "cb"
                ]
            },
            "instruction": {
                "mnemonic": "JC",
                "operands": [
                    "rel8"
                ]
            },
            "description": "Jump short if carry"
        },
        {
            "opcode": {
                "bytes": [
                    227
                ],
                "options": [
                    "cb"
                ]
            },
            "instruction": {
                "mnemonic": "JECXZ",
                "operands": [
                    "rel8"
                ]
            },
            "description": "Jump short if ECX register is 0"
        },
        {
            "opcode": {
                "bytes": [
                    116
                ],
                "options": [
                    "cb"
                ]
            },
            "instruction": {
                "mnemonic": "JE",
                "operands": [
                    "rel8"
                ]
            },
            "description": "Jump short if equal"
        },
        {
            "opcode": {
                "bytes": [
                    127
                ],
                "options": [
                    "cb"
                ]
            },
            "instruction": {
                "mnemonic": "JG",
                "operands": [
                    "rel8"
                ]
            },
            "description": "Jump short if greater"
        },
        {
            "opcode": {
                "bytes": [
                    125
                ],
                "options": [
                    "cb"
                ]
            },
            "instruction": {
                "mnemonic": "JGE",
                "operands": [
                    "rel8"
                ]
            },
            "description": "Jump short if greater or equal"
        },
        {
            "opcode": {
                "bytes": [
                    124
                ],
                "options": [
                    "cb"
                ]
            },
            "instruction": {
                "mnemonic": "JL",
                "operands": [
                    "rel8"
                ]
            },
            "description": "Jump short if less"
        },
        {
            "opcode": {
                "bytes": [
                    126
                ],
                "options": [
                    "cb"
                ]
            },
            "instruction": {
                "mnemonic": "JLE",
                "operands": [
                    "rel8"
                ]
            },
            "description": "Jump short if less or equal"
        },
        {
            "opcode": {
                "bytes": [
                    117
                ],
                "options": [
                    "cb"
                ]
            },
            "instruction": {
                "mnemonic": "JNE",
                "operands": [
                    "rel8"
                ]
            },
            "description": "Jump short if not equal"
        },
        {
            "opcode": {
                "bytes": [
                    113
                ],
                "options": [
                    "cb"
                ]
            },
            "instruction": {
                "mnemonic": "JNO",
                "operands": [
                    "rel8"
                ]
            },
            "description": "Jump short if not overflow"
        },
        {
            "opcode": {
                "bytes": [
                    121
                ],
                "options": [
                    "cb"
                ]
            },
            "instruction": {
                "mnemonic": "JNS",
                "operands": [
                    "rel8"
                ]
            },
            "description": "Jump short if not sign"
        },
        {
            "opcode": {
                "bytes": [
                    112
                ],
                "options": [
                    "cb"
                ]
            },
            "instruction": {
                "mnemonic": "JO",
                "operands": [
                    "rel8"
                ]
            },
            "description": "Jump short if overflow"
        },
        {
            "opcode": {
                "bytes": [
                    122
                ],
                "options": [
                    "cb"
                ]
            },
            "instruction": {
                "mnemonic": "JPE",
                "operands": [
                    "rel8"
                ]
            },
            "description": "Jump short if parity even"
        },
        {
            "opcode": {
                "bytes": [
                    123
                ],
                "options": [
                    "cb"
                ]
            },
            "instruction": {
                "mnemonic": "JPO",
                "operands": [
                    "rel8"
                ]
            },
            "description": "Jump short if parity odd"
        },
        {
            "opcode": {
                "bytes": [
                    120
                ],
                "options": [
                    "cb"
                ]
            },
            "instruction": {
                "mnemonic": "JS",
                "operands": [
                    "rel8"
                ]
            },
            "description": "Jump short if sign"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    135
                ],
                "options": [
                    "cd"
                ]
            },
            "instruction": {
                "mnemonic": "JA",
                "operands": [
                    "rel32"
                ]
            },
            "description": "Jump near if above"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    131
                ],
                "options": [
                    "cd"
                ]
            },
            "instruction": {
                "mnemonic": "JAE",
                "operands": [
                    "rel32"
                ]
            },
            "description": "Jump near if above or equal"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    130
                ],
                "options": [
                    "cd"
                ]
            },
            "instruction": {
                "mnemonic": "JB",
                "operands": [
                    "rel32"
                ]
            },
            "description": "Jump near if below"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    134
                ],
                "options": [
                    "cd"
                ]
            },
            "instruction": {
                "mnemonic": "JBE",
                "operands": [
                    "rel32"
                ]
            },
            "description": "Jump near if below or equal"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    132
                ],
                "options": [
                    "cd"
                ]
            },
            "instruction": {
                "mnemonic": "JE",
                "operands": [
                    "rel32"
                ]
            },
            "description": "Jump near if equal"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    143
                ],
                "options": [
                    "cd"
                ]
            },
            "instruction": {
                "mnemonic": "JG",
                "operands": [
                    "rel32"
                ]
            },
            "description": "Jump near if greater"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    141
                ],
                "options": [
                    "cd"
                ]
            },
            "instruction": {
                "mnemonic": "JGE",
                "operands": [
                    "rel32"
                ]
            },
            "description": "Jump near if greater or equal"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    140
                ],
                "options": [
                    "cd"
                ]
            },
            "instruction": {
                "mnemonic": "JL",
                "operands": [
                    "rel32"
                ]
            },
            "description": "Jump near if less"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    142
                ],
                "options": [
                    "cd"
                ]
            },
            "instruction": {
                "mnemonic": "JLE",
                "operands": [
                    "rel32"
                ]
            },
            "description": "Jump near if less or equal"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    133
                ],
                "options": [
                    "cd"
                ]
            },
            "instruction": {
                "mnemonic": "JNE",
                "operands": [
                    "rel32"
                ]
            },
            "description": "Jump near if not equal"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    129
                ],
                "options": [
                    "cd"
                ]
            },
            "instruction": {
                "mnemonic": "JNO",
                "operands": [
                    "rel32"
                ]
            },
            "description": "Jump near if not overflow"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    137
                ],
                "options": [
                    "cd"
                ]
            },
            "instruction": {
                "mnemonic": "JNS",
                "operands": [
                    "rel32"
                ]
            },
            "description": "Jump near if not sign"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    128
                ],
                "options": [
                    "cd"
                ]
            },
            "instruction": {
                "mnemonic": "JO",
                "operands": [
                    "rel32"
                ]
            },
            "description": "Jump near if overflow"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    138
                ],
                "options": [
                    "cd"
                ]
            },
            "instruction": {
                "mnemonic": "JPE",
                "operands": [
                    "rel32"
                ]
            },
            "description": "Jump near if parity even"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    139
                ],
                "options": [
                    "cd"
                ]
            },
            "instruction": {
                "mnemonic": "JPO",
                "operands": [
                    "rel32"
                ]
            },
            "description": "Jump near if parity odd"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    136
                ],
                "options": [
                    "cd"
                ]
            },
            "instruction": {
                "mnemonic": "JS",
                "operands": [
                    "rel32"
                ]
            },
            "description": "Jump near if sign"
        },
        {
            "opcode": {
                "bytes": [
                    235
                ],
                "options": [
                    "cb"
                ]
            },
            "instruction": {
                "mnemonic": "JMP",
                "operands": [
                    "rel8"
                ]
            },
            "description": "Jump short, relative,"
        },
        {
            "opcode": {
                "bytes": [
                    233
                ],
                "options": [
                    "cd"
                ]
            },
            "instruction": {
                "mnemonic": "JMP",
                "operands": [
                    "rel32"
                ]
            },
            "description": "Jump near, relative,"
        },
        {
            "opcode": {
                "bytes": [
                    255
                ],
                "options": [
                    "/4"
                ]
            },
            "instruction": {
                "mnemonic": "JMP",
                "operands": [
                    "r/m32"
                ]
            },
            "description": "Jump near, abs.ind.in r/m32"
        },
        {
            "opcode": {
                "bytes": [
                    234
                ],
                "options": [
                    "cp"
                ]
            },
            "instruction": {
                "mnemonic": "JMP",
                "operands": [
                    "ptr16:32"
                ]
            },
            "description": "Jump far, abs.add given in operand"
        },
        {
            "opcode": {
                "bytes": [
                    255
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "JMP",
                "operands": [
                    "m16:32"
                ]
            },
            "description": "Jump far, abs.ind.in m16:32"
        },
        {
            "opcode": {
                "bytes": [
                    159
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "LAHF",
                "operands": []
            },
            "description": "Load Status Flags into AH"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    2
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "LAR",
                "operands": [
                    "r32",
                    "r/m32"
                ]
            },
            "description": "Load Access Rights Byte"
        },
        {
            "opcode": {
                "bytes": [
                    197
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "LDS",
                "operands": [
                    "r32",
                    "m16:32"
                ]
            },
            "description": "Load DS:r32 with far ptr"
        },
        {
            "opcode": {
                "bytes": [
                    141
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "LEA",
                "operands": [
                    "r32",
                    "m"
                ]
            },
            "description": "Load effective address"
        },
        {
            "opcode": {
                "bytes": [
                    201
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "LEAVE",
                "operands": []
            },
            "description": "Set ESP to EBP, then pop EBP"
        },
        {
            "opcode": {
                "bytes": [
                    196
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "LES",
                "operands": [
                    "r32",
                    "m16:32"
                ]
            },
            "description": "Load ES:r32 with far ptr"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    180
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "LFS",
                "operands": [
                    "r32",
                    "m16:32"
                ]
            },
            "description": "Load FS:r32 with far ptr"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    181
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "LGS",
                "operands": [
                    "r32",
                    "m16:32"
                ]
            },
            "description": "Load GS:r32 with far ptr"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    1
                ],
                "options": [
                    "/2"
                ]
            },
            "instruction": {
                "mnemonic": "LGDT",
                "operands": [
                    "m16&32"
                ]
            },
            "description": "Load m into GDTR"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    1
                ],
                "options": [
                    "/3"
                ]
            },
            "instruction": {
                "mnemonic": "LIDT",
                "operands": [
                    "m16&32"
                ]
            },
            "description": "Load m into IDTR"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    0
                ],
                "options": [
                    "/2"
                ]
            },
            "instruction": {
                "mnemonic": "LLDT",
                "operands": [
                    "r/m16"
                ]
            },
            "description": "Load segment selector r/m16 into LDTR"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    1
                ],
                "options": [
                    "/6"
                ]
            },
            "instruction": {
                "mnemonic": "LMSW",
                "operands": [
                    "r/m16"
                ]
            },
            "description": "Load r/m16 in machine status word of CR0"
        },
        {
            "opcode": {
                "bytes": [
                    240
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "LOCK",
                "operands": []
            },
            "description": "Asserts LOCK signal for duration .."
        },
        {
            "opcode": {
                "bytes": [
                    172
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "LODS",
                "operands": [
                    "m8"
                ]
            },
            "description": "Load byte at address DS:(E)SI into AL"
        },
        {
            "opcode": {
                "bytes": [
                    173
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "LODS",
                "operands": [
                    "m32"
                ]
            },
            "description": "Load dword at address DS:(E)SI into EAX"
        },
        {
            "opcode": {
                "bytes": [
                    226
                ],
                "options": [
                    "cb"
                ]
            },
            "instruction": {
                "mnemonic": "LOOP",
                "operands": [
                    "rel8"
                ]
            },
            "description": "Dec count;jump if count # 0"
        },
        {
            "opcode": {
                "bytes": [
                    225
                ],
                "options": [
                    "cb"
                ]
            },
            "instruction": {
                "mnemonic": "LOOPE",
                "operands": [
                    "rel8"
                ]
            },
            "description": "Dec count;jump if count # 0 and ZF=1"
        },
        {
            "opcode": {
                "bytes": [
                    225
                ],
                "options": [
                    "cb"
                ]
            },
            "instruction": {
                "mnemonic": "LOOPZ",
                "operands": [
                    "rel8"
                ]
            },
            "description": "Dec count;jump if count # 0 and ZF=1"
        },
        {
            "opcode": {
                "bytes": [
                    224
                ],
                "options": [
                    "cb"
                ]
            },
            "instruction": {
                "mnemonic": "LOOPNE",
                "operands": [
                    "rel8"
                ]
            },
            "description": "Dec count;jump if count # 0 and ZF=0"
        },
        {
            "opcode": {
                "bytes": [
                    224
                ],
                "options": [
                    "cb"
                ]
            },
            "instruction": {
                "mnemonic": "LOOPNZ",
                "operands": [
                    "rel8"
                ]
            },
            "description": "Dec count;jump if count # 0 and ZF=0"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    3
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "LSL",
                "operands": [
                    "r16",
                    "r/m16"
                ]
            },
            "description": "Load Segment Limit"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    3
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "LSL",
                "operands": [
                    "r32",
                    "r/m32"
                ]
            },
            "description": "Load Segment Limit"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    178
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "LSS",
                "operands": [
                    "r32",
                    "m16:32"
                ]
            },
            "description": "Load SS:r32 with far ptr"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    0
                ],
                "options": [
                    "/3"
                ]
            },
            "instruction": {
                "mnemonic": "LTR",
                "operands": [
                    "r/m16"
                ]
            },
            "description": "Load Task Register"
        },
        {
            "opcode": {
                "bytes": [
                    136
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "MOV",
                "operands": [
                    "r/m8",
                    "r8"
                ]
            },
            "description": "Move"
        },
        {
            "opcode": {
                "bytes": [
                    137
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "MOV",
                "operands": [
                    "r/m32",
                    "r32"
                ]
            },
            "description": "Move"
        },
        {
            "opcode": {
                "bytes": [
                    138
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "MOV",
                "operands": [
                    "r8",
                    "r/m8"
                ]
            },
            "description": "Move"
        },
        {
            "opcode": {
                "bytes": [
                    139
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "MOV",
                "operands": [
                    "r32",
                    "r/m32"
                ]
            },
            "description": "Move"
        },
        {
            "opcode": {
                "bytes": [
                    140
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "MOV",
                "operands": [
                    "r/m16",
                    "Sreg"
                ]
            },
            "description": "Move segment register to r/m16"
        },
        {
            "opcode": {
                "bytes": [
                    142
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "MOV",
                "operands": [
                    "Sreg",
                    "r/m16"
                ]
            },
            "description": "Move r/m16 to segment register"
        },
        {
            "opcode": {
                "bytes": [
                    160
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "MOV",
                "operands": [
                    "AL",
                    "moffs8"
                ]
            },
            "description": "Move byte at ( seg:offset) to AL"
        },
        {
            "opcode": {
                "bytes": [
                    161
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "MOV",
                "operands": [
                    "AX",
                    "moffs16"
                ]
            },
            "description": "Move word at ( seg:offset) to AX"
        },
        {
            "opcode": {
                "bytes": [
                    161
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "MOV",
                "operands": [
                    "EAX",
                    "moffs32"
                ]
            },
            "description": "Move dword at ( seg:offset) to EAX"
        },
        {
            "opcode": {
                "bytes": [
                    162
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "MOV",
                "operands": [
                    "moffs8",
                    "AL"
                ]
            },
            "description": "Move AL to ( seg:offset)"
        },
        {
            "opcode": {
                "bytes": [
                    163
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "MOV",
                "operands": [
                    "moffs16",
                    "AX"
                ]
            },
            "description": "Move AX to ( seg:offset)"
        },
        {
            "opcode": {
                "bytes": [
                    163
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "MOV",
                "operands": [
                    "moffs32",
                    "EAX"
                ]
            },
            "description": "Move EAX to ( seg:offset)"
        },
        {
            "opcode": {
                "bytes": [
                    176
                ],
                "options": [
                    "+rb"
                ]
            },
            "instruction": {
                "mnemonic": "MOV",
                "operands": [
                    "r8",
                    "imm8"
                ]
            },
            "description": "Move imm8 to r8"
        },
        {
            "opcode": {
                "bytes": [
                    184
                ],
                "options": [
                    "+rd"
                ]
            },
            "instruction": {
                "mnemonic": "MOV",
                "operands": [
                    "r32",
                    "imm32"
                ]
            },
            "description": "Move imm32 to r32"
        },
        {
            "opcode": {
                "bytes": [
                    198
                ],
                "options": [
                    "/0",
                    "ib"
                ]
            },
            "instruction": {
                "mnemonic": "MOV",
                "operands": [
                    "r/m8",
                    "imm8"
                ]
            },
            "description": "Move imm8 to r/m8"
        },
        {
            "opcode": {
                "bytes": [
                    199
                ],
                "options": [
                    "/0",
                    "id"
                ]
            },
            "instruction": {
                "mnemonic": "MOV",
                "operands": [
                    "r/m32",
                    "imm32"
                ]
            },
            "description": "Move imm32 to r/m32"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    34
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "MOV",
                "operands": [
                    "CR0",
                    "r32"
                ]
            },
            "description": "Move r32 to CR0"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    34
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "MOV",
                "operands": [
                    "CR2",
                    "r32"
                ]
            },
            "description": "Move r32 to CR2"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    34
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "MOV",
                "operands": [
                    "CR3",
                    "r32"
                ]
            },
            "description": "Move r32 to CR3"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    34
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "MOV",
                "operands": [
                    "CR4",
                    "r32"
                ]
            },
            "description": "Move r32 to CR4"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    32
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "MOV",
                "operands": [
                    "r32",
                    "CR0"
                ]
            },
            "description": "Move CR0 to r32"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    32
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "MOV",
                "operands": [
                    "r32",
                    "CR2"
                ]
            },
            "description": "Move CR2 to r32"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    32
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "MOV",
                "operands": [
                    "r32",
                    "CR3"
                ]
            },
            "description": "Move CR3 to r32"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    32
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "MOV",
                "operands": [
                    "r32",
                    "CR4"
                ]
            },
            "description": "Move CR4 to r32"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    33
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "MOV",
                "operands": [
                    "r32",
                    "DR0-DR7"
                ]
            },
            "description": "Move debug register to r32"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    35
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "MOV",
                "operands": [
                    "DR0-DR7",
                    "r32"
                ]
            },
            "description": "Move r32 to debug register"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    110
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "MOVD",
                "operands": [
                    "mm",
                    "r/m32"
                ]
            },
            "description": "Move doubleword from r/m32 to mm"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    126
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "MOVD",
                "operands": [
                    "r/m32",
                    "mm"
                ]
            },
            "description": "Move doubleword from mm to r/m32"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    111
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "MOVQ",
                "operands": [
                    "mm",
                    "mm/m64"
                ]
            },
            "description": "Move quadword from mm/m64 to mm"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    127
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "MOVQ",
                "operands": [
                    "mm/m64",
                    "mm"
                ]
            },
            "description": "Move quadword from mm to mm/m64"
        },
        {
            "opcode": {
                "bytes": [
                    164
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "MOVS",
                "operands": [
                    "m8",
                    "m8"
                ]
            },
            "description": "Move byte at DS:(E)SI to  ES:(E)DI"
        },
        {
            "opcode": {
                "bytes": [
                    165
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "MOVS",
                "operands": [
                    "m32",
                    "m32"
                ]
            },
            "description": "Move dword at DS:(E)SI to  ES:(E)DI"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    190
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "MOVSX",
                "operands": [
                    "r32",
                    "r/m8"
                ]
            },
            "description": "Move byte to doubleword, sign-extension"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    191
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "MOVSX",
                "operands": [
                    "r32",
                    "r/m16"
                ]
            },
            "description": "Move word to doubleword, sign-extension"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    182
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "MOVZX",
                "operands": [
                    "r32",
                    "r/m8"
                ]
            },
            "description": "Move byte to doubleword, zero-extension"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    183
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "MOVZX",
                "operands": [
                    "r32",
                    "r/m16"
                ]
            },
            "description": "Move word to doubleword, zero-extension"
        },
        {
            "opcode": {
                "bytes": [
                    246
                ],
                "options": [
                    "/4"
                ]
            },
            "instruction": {
                "mnemonic": "MUL",
                "operands": [
                    "r/m8"
                ]
            },
            "description": "Unsigned multiply"
        },
        {
            "opcode": {
                "bytes": [
                    247
                ],
                "options": [
                    "/4"
                ]
            },
            "instruction": {
                "mnemonic": "MUL",
                "operands": [
                    "r/m32"
                ]
            },
            "description": "Unsigned multiply"
        },
        {
            "opcode": {
                "bytes": [
                    246
                ],
                "options": [
                    "/3"
                ]
            },
            "instruction": {
                "mnemonic": "NEG",
                "operands": [
                    "r/m8"
                ]
            },
            "description": "Two's complement negate r/m8"
        },
        {
            "opcode": {
                "bytes": [
                    247
                ],
                "options": [
                    "/3"
                ]
            },
            "instruction": {
                "mnemonic": "NEG",
                "operands": [
                    "r/m32"
                ]
            },
            "description": "Two's complement negate r/m32"
        },
        {
            "opcode": {
                "bytes": [
                    144
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "NOP",
                "operands": []
            },
            "description": "No operation"
        },
        {
            "opcode": {
                "bytes": [
                    246
                ],
                "options": [
                    "/2"
                ]
            },
            "instruction": {
                "mnemonic": "NOT",
                "operands": [
                    "r/m8"
                ]
            },
            "description": "Reverse each bit of r/m8"
        },
        {
            "opcode": {
                "bytes": [
                    247
                ],
                "options": [
                    "/2"
                ]
            },
            "instruction": {
                "mnemonic": "NOT",
                "operands": [
                    "r/m32"
                ]
            },
            "description": "Reverse each bit of r/m32"
        },
        {
            "opcode": {
                "bytes": [
                    12
                ],
                "options": [
                    "ib"
                ]
            },
            "instruction": {
                "mnemonic": "OR",
                "operands": [
                    "AL",
                    "imm8"
                ]
            },
            "description": "OR"
        },
        {
            "opcode": {
                "bytes": [
                    13
                ],
                "options": [
                    "id"
                ]
            },
            "instruction": {
                "mnemonic": "OR",
                "operands": [
                    "EAX",
                    "imm32"
                ]
            },
            "description": "OR"
        },
        {
            "opcode": {
                "bytes": [
                    128
                ],
                "options": [
                    "/1",
                    "ib"
                ]
            },
            "instruction": {
                "mnemonic": "OR",
                "operands": [
                    "r/m8",
                    "imm8"
                ]
            },
            "description": "OR"
        },
        {
            "opcode": {
                "bytes": [
                    129
                ],
                "options": [
                    "/1",
                    "id"
                ]
            },
            "instruction": {
                "mnemonic": "OR",
                "operands": [
                    "r/m32",
                    "imm32"
                ]
            },
            "description": "OR"
        },
        {
            "opcode": {
                "bytes": [
                    131
                ],
                "options": [
                    "/1",
                    "ib"
                ]
            },
            "instruction": {
                "mnemonic": "OR",
                "operands": [
                    "r/m32",
                    "imm8"
                ]
            },
            "description": "OR"
        },
        {
            "opcode": {
                "bytes": [
                    8
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "OR",
                "operands": [
                    "r/m8",
                    "r8"
                ]
            },
            "description": "OR"
        },
        {
            "opcode": {
                "bytes": [
                    9
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "OR",
                "operands": [
                    "r/m32",
                    "r32"
                ]
            },
            "description": "OR"
        },
        {
            "opcode": {
                "bytes": [
                    10
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "OR",
                "operands": [
                    "r8",
                    "r/m8"
                ]
            },
            "description": "OR"
        },
        {
            "opcode": {
                "bytes": [
                    11
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "OR",
                "operands": [
                    "r32",
                    "r/m32"
                ]
            },
            "description": "OR"
        },
        {
            "opcode": {
                "bytes": [
                    230
                ],
                "options": [
                    "ib"
                ]
            },
            "instruction": {
                "mnemonic": "OUT",
                "operands": [
                    "imm8",
                    "AL"
                ]
            },
            "description": "Output byte in AL to I/O(imm8)"
        },
        {
            "opcode": {
                "bytes": [
                    231
                ],
                "options": [
                    "ib"
                ]
            },
            "instruction": {
                "mnemonic": "OUT",
                "operands": [
                    "imm8",
                    "EAX"
                ]
            },
            "description": "Output dword in EAX to I/O(imm8)"
        },
        {
            "opcode": {
                "bytes": [
                    238
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "OUT",
                "operands": [
                    "DX",
                    "AL"
                ]
            },
            "description": "Output byte in AL to I/O(DX)"
        },
        {
            "opcode": {
                "bytes": [
                    239
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "OUT",
                "operands": [
                    "DX",
                    "EAX"
                ]
            },
            "description": "Output dword in EAX to I/O(DX)"
        },
        {
            "opcode": {
                "bytes": [
                    110
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "OUTS",
                "operands": [
                    "DX",
                    "m8"
                ]
            },
            "description": "Output byte from DS:(E)SI to I/O(DX)"
        },
        {
            "opcode": {
                "bytes": [
                    111
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "OUTS",
                "operands": [
                    "DX",
                    "m32"
                ]
            },
            "description": "Output dword from DS:(E)SI to I/O (DX)"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    99
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "PACKSSWB",
                "operands": [
                    "mm",
                    "mm/m64"
                ]
            },
            "description": "Pack with Signed Saturation"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    107
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "PACKSSDW",
                "operands": [
                    "mm",
                    "mm/m64"
                ]
            },
            "description": "Pack with Signed Saturation"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    103
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "PACKUSWB",
                "operands": [
                    "mm",
                    "mm/m64"
                ]
            },
            "description": "Pack with Unsigned Saturation"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    252
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "PADDB",
                "operands": [
                    "mm",
                    "mm/m64"
                ]
            },
            "description": "Add packed bytes"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    253
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "PADDW",
                "operands": [
                    "mm",
                    "mm/m64"
                ]
            },
            "description": "Add packed words"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    254
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "PADDD",
                "operands": [
                    "mm",
                    "mm/m64"
                ]
            },
            "description": "Add packed dwords"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    236
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "PADDSB",
                "operands": [
                    "mm",
                    "mm/m64"
                ]
            },
            "description": "Add signed packed bytes"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    237
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "PADDSW",
                "operands": [
                    "mm",
                    "mm/m64"
                ]
            },
            "description": "Add signed packed words"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    220
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "PADDUSB",
                "operands": [
                    "mm",
                    "mm/m64"
                ]
            },
            "description": "Add unsigned pkd bytes"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    221
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "PADDUSW",
                "operands": [
                    "mm",
                    "mm/m64"
                ]
            },
            "description": "Add unsigned pkd words"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    219
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "PAND",
                "operands": [
                    "mm",
                    "mm/m64"
                ]
            },
            "description": "AND quadword from .. to .."
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    223
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "PANDN",
                "operands": [
                    "mm",
                    "mm/m64"
                ]
            },
            "description": "And qword from .. to NOT qw in mm"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    116
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "PCMPEQB",
                "operands": [
                    "mm",
                    "mm/m64"
                ]
            },
            "description": "Packed Compare for Equal"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    117
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "PCMPEQW",
                "operands": [
                    "mm",
                    "mm/m64"
                ]
            },
            "description": "Packed Compare for Equal"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    118
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "PCMPEQD",
                "operands": [
                    "mm",
                    "mm/m64"
                ]
            },
            "description": "Packed Compare for Equal"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    100
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "PCMPGTB",
                "operands": [
                    "mm",
                    "mm/m64"
                ]
            },
            "description": "Packed Compare for GT"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    101
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "PCMPGTW",
                "operands": [
                    "mm",
                    "mm/m64"
                ]
            },
            "description": "Packed Compare for GT"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    102
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "PCMPGTD",
                "operands": [
                    "mm",
                    "mm/m64"
                ]
            },
            "description": "Packed Compare for GT"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    245
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "PMADDWD",
                "operands": [
                    "mm",
                    "mm/m64"
                ]
            },
            "description": "Packed Multiply and Add"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    229
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "PMULHW",
                "operands": [
                    "mm",
                    "mm/m64"
                ]
            },
            "description": "Packed Multiply High"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    213
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "PMULLW",
                "operands": [
                    "mm",
                    "mm/m64"
                ]
            },
            "description": "Packed Multiply Low"
        },
        {
            "opcode": {
                "bytes": [
                    143
                ],
                "options": [
                    "/0"
                ]
            },
            "instruction": {
                "mnemonic": "POP",
                "operands": [
                    "m32"
                ]
            },
            "description": "Pop m32"
        },
        {
            "opcode": {
                "bytes": [
                    88
                ],
                "options": [
                    "+rd"
                ]
            },
            "instruction": {
                "mnemonic": "POP",
                "operands": [
                    "r32"
                ]
            },
            "description": "Pop r32"
        },
        {
            "opcode": {
                "bytes": [
                    31
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "POP",
                "operands": []
            },
            "description": "DS                  Pop DS"
        },
        {
            "opcode": {
                "bytes": [
                    7
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "POP",
                "operands": []
            },
            "description": "ES                  Pop ES"
        },
        {
            "opcode": {
                "bytes": [
                    23
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "POP",
                "operands": []
            },
            "description": "SS                  Pop SS"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    161
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "POP",
                "operands": []
            },
            "description": "FS                  Pop FS"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    169
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "POP",
                "operands": []
            },
            "description": "GS                  Pop GS"
        },
        {
            "opcode": {
                "bytes": [
                    97
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "POPAD",
                "operands": []
            },
            "description": "Pop EDI,... and EAX"
        },
        {
            "opcode": {
                "bytes": [
                    157
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "POPFD",
                "operands": []
            },
            "description": "Pop Stack into EFLAGS Register"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    235
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "POR",
                "operands": [
                    "mm",
                    "mm/m64"
                ]
            },
            "description": "OR qword from .. to mm"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    241
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "PSLLW",
                "operands": [
                    "mm",
                    "mm/m64"
                ]
            },
            "description": "Packed Shift Left Logical"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    113
                ],
                "options": [
                    "/6",
                    "ib"
                ]
            },
            "instruction": {
                "mnemonic": "PSLLW",
                "operands": [
                    "mm",
                    "imm8"
                ]
            },
            "description": "Packed Shift Left Logical"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    242
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "PSLLD",
                "operands": [
                    "mm",
                    "mm/m64"
                ]
            },
            "description": "Packed Shift Left Logical"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    114
                ],
                "options": [
                    "/6",
                    "ib"
                ]
            },
            "instruction": {
                "mnemonic": "PSLLD",
                "operands": [
                    "mm",
                    "imm8"
                ]
            },
            "description": "Packed Shift Left Logical"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    243
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "PSLLQ",
                "operands": [
                    "mm",
                    "mm/m64"
                ]
            },
            "description": "Packed Shift Left Logical"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    115
                ],
                "options": [
                    "/6",
                    "ib"
                ]
            },
            "instruction": {
                "mnemonic": "PSLLQ",
                "operands": [
                    "mm",
                    "imm8"
                ]
            },
            "description": "Packed Shift Left Logical"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    225
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "PSRAW",
                "operands": [
                    "mm",
                    "mm/m64"
                ]
            },
            "description": "Packed Shift Right Arithmetic"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    113
                ],
                "options": [
                    "/4",
                    "ib"
                ]
            },
            "instruction": {
                "mnemonic": "PSRAW",
                "operands": [
                    "mm",
                    "imm8"
                ]
            },
            "description": "Packed Shift Right Arithmetic"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    226
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "PSRAD",
                "operands": [
                    "mm",
                    "mm/m64"
                ]
            },
            "description": "Packed Shift Right Arithmetic"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    114
                ],
                "options": [
                    "/4",
                    "ib"
                ]
            },
            "instruction": {
                "mnemonic": "PSRAD",
                "operands": [
                    "mm",
                    "imm8"
                ]
            },
            "description": "Packed Shift Right Arithmetic"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    209
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "PSRLW",
                "operands": [
                    "mm",
                    "mm/m64"
                ]
            },
            "description": "Packed Shift Right Logical"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    113
                ],
                "options": [
                    "/2",
                    "ib"
                ]
            },
            "instruction": {
                "mnemonic": "PSRLW",
                "operands": [
                    "mm",
                    "imm8"
                ]
            },
            "description": "Packed Shift Right Logical"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    210
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "PSRLD",
                "operands": [
                    "mm",
                    "mm/m64"
                ]
            },
            "description": "Packed Shift Right Logical"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    114
                ],
                "options": [
                    "/2",
                    "ib"
                ]
            },
            "instruction": {
                "mnemonic": "PSRLD",
                "operands": [
                    "mm",
                    "imm8"
                ]
            },
            "description": "Packed Shift Right Logical"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    211
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "PSRLQ",
                "operands": [
                    "mm",
                    "mm/m64"
                ]
            },
            "description": "Packed Shift Right Logical"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    115
                ],
                "options": [
                    "/2",
                    "ib"
                ]
            },
            "instruction": {
                "mnemonic": "PSRLQ",
                "operands": [
                    "mm",
                    "imm8"
                ]
            },
            "description": "Packed Shift Right Logical"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    248
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "PSUBB",
                "operands": [
                    "mm",
                    "mm/m64"
                ]
            },
            "description": "Packed Subtract"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    249
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "PSUBW",
                "operands": [
                    "mm",
                    "mm/m64"
                ]
            },
            "description": "Packed Subtract"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    250
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "PSUBD",
                "operands": [
                    "mm",
                    "mm/m64"
                ]
            },
            "description": "Packed Subtract"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    232
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "PSUBSB",
                "operands": [
                    "mm",
                    "mm/m64"
                ]
            },
            "description": "Packed Subtract with Saturation"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    233
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "PSUBSW",
                "operands": [
                    "mm",
                    "mm/m64"
                ]
            },
            "description": "Packed Subtract with Saturation"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    216
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "PSUBUSB",
                "operands": [
                    "mm",
                    "mm/m64"
                ]
            },
            "description": "Packed Subtract Unsigned with S."
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    217
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "PSUBUSW",
                "operands": [
                    "mm",
                    "mm/m64"
                ]
            },
            "description": "Packed Subtract Unsigned with S."
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    104
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "PUNPCKHBW",
                "operands": [
                    "mm",
                    "mm/m64"
                ]
            },
            "description": "Unpack High Packed Data"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    105
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "PUNPCKHWD",
                "operands": [
                    "mm",
                    "mm/m64"
                ]
            },
            "description": "Unpack High Packed Data"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    106
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "PUNPCKHDQ",
                "operands": [
                    "mm",
                    "mm/m64"
                ]
            },
            "description": "Unpack High Packed Data"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    96
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "PUNPCKLBW",
                "operands": [
                    "mm",
                    "mm/m64"
                ]
            },
            "description": "Unpack Low Packed Data"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    97
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "PUNPCKLWD",
                "operands": [
                    "mm",
                    "mm/m64"
                ]
            },
            "description": "Unpack Low Packed Data"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    98
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "PUNPCKLDQ",
                "operands": [
                    "mm",
                    "mm/m64"
                ]
            },
            "description": "Unpack Low Packed Data"
        },
        {
            "opcode": {
                "bytes": [
                    255
                ],
                "options": [
                    "/6"
                ]
            },
            "instruction": {
                "mnemonic": "PUSH",
                "operands": [
                    "r/m32"
                ]
            },
            "description": "Push r/m32"
        },
        {
            "opcode": {
                "bytes": [
                    80
                ],
                "options": [
                    "+rd"
                ]
            },
            "instruction": {
                "mnemonic": "PUSH",
                "operands": [
                    "r32"
                ]
            },
            "description": "Push r32"
        },
        {
            "opcode": {
                "bytes": [
                    106
                ],
                "options": [
                    "ib"
                ]
            },
            "instruction": {
                "mnemonic": "PUSH",
                "operands": [
                    "imm8"
                ]
            },
            "description": "Push imm8"
        },
        {
            "opcode": {
                "bytes": [
                    104
                ],
                "options": [
                    "id"
                ]
            },
            "instruction": {
                "mnemonic": "PUSH",
                "operands": [
                    "imm32"
                ]
            },
            "description": "Push imm32"
        },
        {
            "opcode": {
                "bytes": [
                    14
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "PUSH",
                "operands": []
            },
            "description": "CS                 Push CS"
        },
        {
            "opcode": {
                "bytes": [
                    22
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "PUSH",
                "operands": []
            },
            "description": "SS                 Push SS"
        },
        {
            "opcode": {
                "bytes": [
                    30
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "PUSH",
                "operands": []
            },
            "description": "DS                 Push DS"
        },
        {
            "opcode": {
                "bytes": [
                    6
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "PUSH",
                "operands": []
            },
            "description": "ES                 Push ES"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    160
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "PUSH",
                "operands": []
            },
            "description": "FS                 Push FS"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    168
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "PUSH",
                "operands": []
            },
            "description": "GS                 Push GS"
        },
        {
            "opcode": {
                "bytes": [
                    96
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "PUSHAD",
                "operands": []
            },
            "description": "Push All g-regs"
        },
        {
            "opcode": {
                "bytes": [
                    156
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "PUSHFD",
                "operands": []
            },
            "description": "Push EFLAGS"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    239
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "PXOR",
                "operands": [
                    "mm",
                    "mm/m64"
                ]
            },
            "description": "XOR qword"
        },
        {
            "opcode": {
                "bytes": [
                    208
                ],
                "options": [
                    "/2"
                ]
            },
            "instruction": {
                "mnemonic": "RCL",
                "operands": [
                    "r/m8",
                    "1"
                ]
            },
            "description": "Rotate 9 bits left once"
        },
        {
            "opcode": {
                "bytes": [
                    210
                ],
                "options": [
                    "/2"
                ]
            },
            "instruction": {
                "mnemonic": "RCL",
                "operands": [
                    "r/m8",
                    "CL"
                ]
            },
            "description": "Rotate 9 bits left CL times"
        },
        {
            "opcode": {
                "bytes": [
                    192
                ],
                "options": [
                    "/2",
                    "ib"
                ]
            },
            "instruction": {
                "mnemonic": "RCL",
                "operands": [
                    "r/m8",
                    "imm8"
                ]
            },
            "description": "Rotate 9 bits left imm8 times"
        },
        {
            "opcode": {
                "bytes": [
                    209
                ],
                "options": [
                    "/2"
                ]
            },
            "instruction": {
                "mnemonic": "RCL",
                "operands": [
                    "r/m32",
                    "1"
                ]
            },
            "description": "Rotate 33 bits left once"
        },
        {
            "opcode": {
                "bytes": [
                    211
                ],
                "options": [
                    "/2"
                ]
            },
            "instruction": {
                "mnemonic": "RCL",
                "operands": [
                    "r/m32",
                    "CL"
                ]
            },
            "description": "Rotate 33 bits left CL times"
        },
        {
            "opcode": {
                "bytes": [
                    193
                ],
                "options": [
                    "/2",
                    "ib"
                ]
            },
            "instruction": {
                "mnemonic": "RCL",
                "operands": [
                    "r/m32",
                    "imm8"
                ]
            },
            "description": "Rotate 33 bits left imm8 times"
        },
        {
            "opcode": {
                "bytes": [
                    208
                ],
                "options": [
                    "/3"
                ]
            },
            "instruction": {
                "mnemonic": "RCR",
                "operands": [
                    "r/m8",
                    "1"
                ]
            },
            "description": "Rotate 9 bits right once"
        },
        {
            "opcode": {
                "bytes": [
                    210
                ],
                "options": [
                    "/3"
                ]
            },
            "instruction": {
                "mnemonic": "RCR",
                "operands": [
                    "r/m8",
                    "CL"
                ]
            },
            "description": "Rotate 9 bits right CL times"
        },
        {
            "opcode": {
                "bytes": [
                    192
                ],
                "options": [
                    "/3",
                    "ib"
                ]
            },
            "instruction": {
                "mnemonic": "RCR",
                "operands": [
                    "r/m8",
                    "imm8"
                ]
            },
            "description": "Rotate 9 bits right imm8 times"
        },
        {
            "opcode": {
                "bytes": [
                    209
                ],
                "options": [
                    "/3"
                ]
            },
            "instruction": {
                "mnemonic": "RCR",
                "operands": [
                    "r/m32",
                    "1"
                ]
            },
            "description": "Rotate 33 bits right once"
        },
        {
            "opcode": {
                "bytes": [
                    211
                ],
                "options": [
                    "/3"
                ]
            },
            "instruction": {
                "mnemonic": "RCR",
                "operands": [
                    "r/m32",
                    "CL"
                ]
            },
            "description": "Rotate 33 bits right CL times"
        },
        {
            "opcode": {
                "bytes": [
                    193
                ],
                "options": [
                    "/3",
                    "ib"
                ]
            },
            "instruction": {
                "mnemonic": "RCR",
                "operands": [
                    "r/m32",
                    "imm8"
                ]
            },
            "description": "Rotate 33 bits right imm8 times"
        },
        {
            "opcode": {
                "bytes": [
                    208
                ],
                "options": [
                    "/0"
                ]
            },
            "instruction": {
                "mnemonic": "ROL",
                "operands": [
                    "r/m8",
                    "1"
                ]
            },
            "description": "Rotate 8 bits r/m8 left once"
        },
        {
            "opcode": {
                "bytes": [
                    210
                ],
                "options": [
                    "/0"
                ]
            },
            "instruction": {
                "mnemonic": "ROL",
                "operands": [
                    "r/m8",
                    "CL"
                ]
            },
            "description": "Rotate 8 bits r/m8 left CL times"
        },
        {
            "opcode": {
                "bytes": [
                    192
                ],
                "options": [
                    "/0",
                    "ib"
                ]
            },
            "instruction": {
                "mnemonic": "ROL",
                "operands": [
                    "r/m8",
                    "imm8"
                ]
            },
            "description": "Rotate 8 bits r/m8 left imm8 times"
        },
        {
            "opcode": {
                "bytes": [
                    209
                ],
                "options": [
                    "/0"
                ]
            },
            "instruction": {
                "mnemonic": "ROL",
                "operands": [
                    "r/m32",
                    "1"
                ]
            },
            "description": "Rotate 32 bits r/m32 left once"
        },
        {
            "opcode": {
                "bytes": [
                    211
                ],
                "options": [
                    "/0"
                ]
            },
            "instruction": {
                "mnemonic": "ROL",
                "operands": [
                    "r/m32",
                    "CL"
                ]
            },
            "description": "Rotate 32 bits r/m32 left CL times"
        },
        {
            "opcode": {
                "bytes": [
                    193
                ],
                "options": [
                    "/0",
                    "ib"
                ]
            },
            "instruction": {
                "mnemonic": "ROL",
                "operands": [
                    "r/m32",
                    "imm8"
                ]
            },
            "description": "Rotate 32 bits r/m32 left imm8 times"
        },
        {
            "opcode": {
                "bytes": [
                    208
                ],
                "options": [
                    "/1"
                ]
            },
            "instruction": {
                "mnemonic": "ROR",
                "operands": [
                    "r/m8",
                    "1"
                ]
            },
            "description": "Rotate 8 bits r/m8 right once"
        },
        {
            "opcode": {
                "bytes": [
                    210
                ],
                "options": [
                    "/1"
                ]
            },
            "instruction": {
                "mnemonic": "ROR",
                "operands": [
                    "r/m8",
                    "CL"
                ]
            },
            "description": "Rotate 8 bits r/m8 right CL times"
        },
        {
            "opcode": {
                "bytes": [
                    192
                ],
                "options": [
                    "/1",
                    "ib"
                ]
            },
            "instruction": {
                "mnemonic": "ROR",
                "operands": [
                    "r/m8",
                    "imm8"
                ]
            },
            "description": "Rotate 8 bits r/m16 right imm8 times"
        },
        {
            "opcode": {
                "bytes": [
                    209
                ],
                "options": [
                    "/1"
                ]
            },
            "instruction": {
                "mnemonic": "ROR",
                "operands": [
                    "r/m32",
                    "1"
                ]
            },
            "description": "Rotate 32 bits r/m32 right once"
        },
        {
            "opcode": {
                "bytes": [
                    211
                ],
                "options": [
                    "/1"
                ]
            },
            "instruction": {
                "mnemonic": "ROR",
                "operands": [
                    "r/m32",
                    "CL"
                ]
            },
            "description": "Rotate 32 bits r/m32 right CL times"
        },
        {
            "opcode": {
                "bytes": [
                    193
                ],
                "options": [
                    "/1",
                    "ib"
                ]
            },
            "instruction": {
                "mnemonic": "ROR",
                "operands": [
                    "r/m32",
                    "imm8"
                ]
            },
            "description": "Rotate 32 bits r/m32 right imm8 times"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    50
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "RDMSR",
                "operands": []
            },
            "description": "Read from Model Specific Register"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    51
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "RDPMC",
                "operands": []
            },
            "description": "Read Performance-Monitoring counters"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    49
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "RDTSC",
                "operands": []
            },
            "description": "Read Time-Stamp Counter"
        },
        {
            "opcode": {
                "bytes": [
                    243,
                    108
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "REP",
                "operands": []
            },
            "description": "INS m8,DX           Input ECX bytes from port DX into ES:[(E)DI]"
        },
        {
            "opcode": {
                "bytes": [
                    243,
                    109
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "REP",
                "operands": []
            },
            "description": "INS m32,DX          Input ECX dwords from port DX into ES:[(E)DI]"
        },
        {
            "opcode": {
                "bytes": [
                    243,
                    164
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "REP",
                "operands": []
            },
            "description": "MOVS m8,m8          Move ECX bytes from DS:[(E)SI] to ES:[(E)DI]"
        },
        {
            "opcode": {
                "bytes": [
                    243,
                    165
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "REP",
                "operands": []
            },
            "description": "MOVS m32,m32        Move ECX dwords from DS:[(E)SI] to ES:[(E)DI]"
        },
        {
            "opcode": {
                "bytes": [
                    243,
                    110
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "REP",
                "operands": []
            },
            "description": "OUTS DX,m8          Output ECX bytes from DS:[(E)SI] to port DX"
        },
        {
            "opcode": {
                "bytes": [
                    243,
                    111
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "REP",
                "operands": []
            },
            "description": "OUTS DX,m32         Output ECX dwords from DS:[(E)SI] to port DX"
        },
        {
            "opcode": {
                "bytes": [
                    243,
                    172
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "REP",
                "operands": []
            },
            "description": "LODS AL             Load ECX bytes from DS:[(E)SI] to AL"
        },
        {
            "opcode": {
                "bytes": [
                    243,
                    173
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "REP",
                "operands": []
            },
            "description": "LODS EAX            Load ECX dwords from DS:[(E)SI] to EAX"
        },
        {
            "opcode": {
                "bytes": [
                    243,
                    170
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "REP",
                "operands": []
            },
            "description": "STOS m8             Fill ECX bytes at ES:[(E)DI] with AL"
        },
        {
            "opcode": {
                "bytes": [
                    243,
                    171
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "REP",
                "operands": []
            },
            "description": "STOS m32            Fill ECX dwords at ES:[(E)DI] with EAX"
        },
        {
            "opcode": {
                "bytes": [
                    243,
                    166
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "REPE",
                "operands": []
            },
            "description": "CMPS m8,m8         Find nonmatching bytes in m and m"
        },
        {
            "opcode": {
                "bytes": [
                    243,
                    167
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "REPE",
                "operands": []
            },
            "description": "CMPS m32,m32       Find nonmatching dwords in m and m"
        },
        {
            "opcode": {
                "bytes": [
                    243,
                    174
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "REPE",
                "operands": []
            },
            "description": "SCAS m8            Find non-AL byte starting at"
        },
        {
            "opcode": {
                "bytes": [
                    243,
                    175
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "REPE",
                "operands": []
            },
            "description": "SCAS m32           Find non-EAX dword starting at"
        },
        {
            "opcode": {
                "bytes": [
                    242,
                    166
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "REPNE",
                "operands": []
            },
            "description": "CMPS m8,m8        Find matching bytes in m and m"
        },
        {
            "opcode": {
                "bytes": [
                    242,
                    167
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "REPNE",
                "operands": []
            },
            "description": "CMPS m32,m32      Find matching dwords in m and m"
        },
        {
            "opcode": {
                "bytes": [
                    242,
                    174
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "REPNE",
                "operands": []
            },
            "description": "SCAS m8           Find AL, starting at ES:[(E)DI]"
        },
        {
            "opcode": {
                "bytes": [
                    242,
                    175
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "REPNE",
                "operands": []
            },
            "description": "SCAS m32          Find EAX, starting at ES:[(E)DI]"
        },
        {
            "opcode": {
                "bytes": [
                    195
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "RET",
                "operands": []
            },
            "description": "Near return"
        },
        {
            "opcode": {
                "bytes": [
                    203
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "RET",
                "operands": []
            },
            "description": "Far return"
        },
        {
            "opcode": {
                "bytes": [
                    194
                ],
                "options": [
                    "iw"
                ]
            },
            "instruction": {
                "mnemonic": "RET",
                "operands": [
                    "imm16"
                ]
            },
            "description": "Near return, pop imm16 bytes from stack"
        },
        {
            "opcode": {
                "bytes": [
                    202
                ],
                "options": [
                    "iw"
                ]
            },
            "instruction": {
                "mnemonic": "RET",
                "operands": [
                    "imm16"
                ]
            },
            "description": "Far return, pop imm16 bytes from stack"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    170
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "RSM",
                "operands": []
            },
            "description": "Resume from System Management"
        },
        {
            "opcode": {
                "bytes": [
                    158
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "SAHF",
                "operands": []
            },
            "description": "Store AH into Flags"
        },
        {
            "opcode": {
                "bytes": [
                    208
                ],
                "options": [
                    "/4"
                ]
            },
            "instruction": {
                "mnemonic": "SAL",
                "operands": [
                    "r/m8",
                    "1"
                ]
            },
            "description": "Shift Arithmetic Left"
        },
        {
            "opcode": {
                "bytes": [
                    210
                ],
                "options": [
                    "/4"
                ]
            },
            "instruction": {
                "mnemonic": "SAL",
                "operands": [
                    "r/m8",
                    "CL"
                ]
            },
            "description": "Shift Arithmetic Left"
        },
        {
            "opcode": {
                "bytes": [
                    192
                ],
                "options": [
                    "/4",
                    "ib"
                ]
            },
            "instruction": {
                "mnemonic": "SAL",
                "operands": [
                    "r/m8",
                    "imm8"
                ]
            },
            "description": "Shift Arithmetic Left"
        },
        {
            "opcode": {
                "bytes": [
                    209
                ],
                "options": [
                    "/4"
                ]
            },
            "instruction": {
                "mnemonic": "SAL",
                "operands": [
                    "r/m32",
                    "1"
                ]
            },
            "description": "Shift Arithmetic Left"
        },
        {
            "opcode": {
                "bytes": [
                    211
                ],
                "options": [
                    "/4"
                ]
            },
            "instruction": {
                "mnemonic": "SAL",
                "operands": [
                    "r/m32",
                    "CL"
                ]
            },
            "description": "Shift Arithmetic Left"
        },
        {
            "opcode": {
                "bytes": [
                    193
                ],
                "options": [
                    "/4",
                    "ib"
                ]
            },
            "instruction": {
                "mnemonic": "SAL",
                "operands": [
                    "r/m32",
                    "imm8"
                ]
            },
            "description": "Shift Arithmetic Left"
        },
        {
            "opcode": {
                "bytes": [
                    208
                ],
                "options": [
                    "/7"
                ]
            },
            "instruction": {
                "mnemonic": "SAR",
                "operands": [
                    "r/m8",
                    "1"
                ]
            },
            "description": "Shift Arithmetic Right"
        },
        {
            "opcode": {
                "bytes": [
                    210
                ],
                "options": [
                    "/7"
                ]
            },
            "instruction": {
                "mnemonic": "SAR",
                "operands": [
                    "r/m8",
                    "CL"
                ]
            },
            "description": "Shift Arithmetic Right"
        },
        {
            "opcode": {
                "bytes": [
                    192
                ],
                "options": [
                    "/7",
                    "ib"
                ]
            },
            "instruction": {
                "mnemonic": "SAR",
                "operands": [
                    "r/m8",
                    "imm8"
                ]
            },
            "description": "Shift Arithmetic Right"
        },
        {
            "opcode": {
                "bytes": [
                    209
                ],
                "options": [
                    "/7"
                ]
            },
            "instruction": {
                "mnemonic": "SAR",
                "operands": [
                    "r/m32",
                    "1"
                ]
            },
            "description": "Shift Arithmetic Right"
        },
        {
            "opcode": {
                "bytes": [
                    211
                ],
                "options": [
                    "/7"
                ]
            },
            "instruction": {
                "mnemonic": "SAR",
                "operands": [
                    "r/m32",
                    "CL"
                ]
            },
            "description": "Shift Arithmetic Right"
        },
        {
            "opcode": {
                "bytes": [
                    193
                ],
                "options": [
                    "/7",
                    "ib"
                ]
            },
            "instruction": {
                "mnemonic": "SAR",
                "operands": [
                    "r/m32",
                    "imm8"
                ]
            },
            "description": "Shift Arithmetic Right"
        },
        {
            "opcode": {
                "bytes": [
                    208
                ],
                "options": [
                    "/4"
                ]
            },
            "instruction": {
                "mnemonic": "SHL",
                "operands": [
                    "r/m8",
                    "1"
                ]
            },
            "description": "Shift Logical Left"
        },
        {
            "opcode": {
                "bytes": [
                    210
                ],
                "options": [
                    "/4"
                ]
            },
            "instruction": {
                "mnemonic": "SHL",
                "operands": [
                    "r/m8",
                    "CL"
                ]
            },
            "description": "Shift Logical Left"
        },
        {
            "opcode": {
                "bytes": [
                    192
                ],
                "options": [
                    "/4",
                    "ib"
                ]
            },
            "instruction": {
                "mnemonic": "SHL",
                "operands": [
                    "r/m8",
                    "imm8"
                ]
            },
            "description": "Shift Logical Left"
        },
        {
            "opcode": {
                "bytes": [
                    209
                ],
                "options": [
                    "/4"
                ]
            },
            "instruction": {
                "mnemonic": "SHL",
                "operands": [
                    "r/m32",
                    "1"
                ]
            },
            "description": "Shift Logical Left"
        },
        {
            "opcode": {
                "bytes": [
                    211
                ],
                "options": [
                    "/4"
                ]
            },
            "instruction": {
                "mnemonic": "SHL",
                "operands": [
                    "r/m32",
                    "CL"
                ]
            },
            "description": "Shift Logical Left"
        },
        {
            "opcode": {
                "bytes": [
                    193
                ],
                "options": [
                    "/4",
                    "ib"
                ]
            },
            "instruction": {
                "mnemonic": "SHL",
                "operands": [
                    "r/m32",
                    "imm8"
                ]
            },
            "description": "Shift Logical Left"
        },
        {
            "opcode": {
                "bytes": [
                    208
                ],
                "options": [
                    "/5"
                ]
            },
            "instruction": {
                "mnemonic": "SHR",
                "operands": [
                    "r/m8",
                    "1"
                ]
            },
            "description": "Shift Logical Right"
        },
        {
            "opcode": {
                "bytes": [
                    210
                ],
                "options": [
                    "/5"
                ]
            },
            "instruction": {
                "mnemonic": "SHR",
                "operands": [
                    "r/m8",
                    "CL"
                ]
            },
            "description": "Shift Logical Right"
        },
        {
            "opcode": {
                "bytes": [
                    192
                ],
                "options": [
                    "/5",
                    "ib"
                ]
            },
            "instruction": {
                "mnemonic": "SHR",
                "operands": [
                    "r/m8",
                    "imm8"
                ]
            },
            "description": "Shift Logical Right"
        },
        {
            "opcode": {
                "bytes": [
                    209
                ],
                "options": [
                    "/5"
                ]
            },
            "instruction": {
                "mnemonic": "SHR",
                "operands": [
                    "r/m32",
                    "1"
                ]
            },
            "description": "Shift Logical Right"
        },
        {
            "opcode": {
                "bytes": [
                    211
                ],
                "options": [
                    "/5"
                ]
            },
            "instruction": {
                "mnemonic": "SHR",
                "operands": [
                    "r/m32",
                    "CL"
                ]
            },
            "description": "Shift Logical Right"
        },
        {
            "opcode": {
                "bytes": [
                    193
                ],
                "options": [
                    "/5",
                    "ib"
                ]
            },
            "instruction": {
                "mnemonic": "SHR",
                "operands": [
                    "r/m32",
                    "imm8"
                ]
            },
            "description": "Shift Logical Right"
        },
        {
            "opcode": {
                "bytes": [
                    28
                ],
                "options": [
                    "ib"
                ]
            },
            "instruction": {
                "mnemonic": "SBB",
                "operands": [
                    "AL",
                    "imm8"
                ]
            },
            "description": "Subtract with borrow"
        },
        {
            "opcode": {
                "bytes": [
                    29
                ],
                "options": [
                    "id"
                ]
            },
            "instruction": {
                "mnemonic": "SBB",
                "operands": [
                    "EAX",
                    "imm32"
                ]
            },
            "description": "Subtract with borrow"
        },
        {
            "opcode": {
                "bytes": [
                    128
                ],
                "options": [
                    "/3",
                    "ib"
                ]
            },
            "instruction": {
                "mnemonic": "SBB",
                "operands": [
                    "r/m8",
                    "imm8"
                ]
            },
            "description": "Subtract with borrow"
        },
        {
            "opcode": {
                "bytes": [
                    129
                ],
                "options": [
                    "/3",
                    "id"
                ]
            },
            "instruction": {
                "mnemonic": "SBB",
                "operands": [
                    "r/m32",
                    "imm32"
                ]
            },
            "description": "Subtract with borrow"
        },
        {
            "opcode": {
                "bytes": [
                    131
                ],
                "options": [
                    "/3",
                    "ib"
                ]
            },
            "instruction": {
                "mnemonic": "SBB",
                "operands": [
                    "r/m32",
                    "imm8"
                ]
            },
            "description": "Subtract with borrow"
        },
        {
            "opcode": {
                "bytes": [
                    24
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "SBB",
                "operands": [
                    "r/m8",
                    "r8"
                ]
            },
            "description": "Subtract with borrow"
        },
        {
            "opcode": {
                "bytes": [
                    25
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "SBB",
                "operands": [
                    "r/m32",
                    "r32"
                ]
            },
            "description": "Subtract with borrow"
        },
        {
            "opcode": {
                "bytes": [
                    26
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "SBB",
                "operands": [
                    "r8",
                    "r/m8"
                ]
            },
            "description": "Subtract with borrow"
        },
        {
            "opcode": {
                "bytes": [
                    27
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "SBB",
                "operands": [
                    "r32",
                    "r/m32"
                ]
            },
            "description": "Subtract with borrow"
        },
        {
            "opcode": {
                "bytes": [
                    174
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "SCAS",
                "operands": [
                    "m8"
                ]
            },
            "description": "Scan String"
        },
        {
            "opcode": {
                "bytes": [
                    175
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "SCAS",
                "operands": [
                    "m32"
                ]
            },
            "description": "Scan String"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    151
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "SETA",
                "operands": [
                    "r/m8"
                ]
            },
            "description": "Set byte if above"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    147
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "SETAE",
                "operands": [
                    "r/m8"
                ]
            },
            "description": "Set byte if above or equal"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    146
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "SETB",
                "operands": [
                    "r/m8"
                ]
            },
            "description": "Set byte if below"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    150
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "SETBE",
                "operands": [
                    "r/m8"
                ]
            },
            "description": "Set byte if below or equal"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    148
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "SETE",
                "operands": [
                    "r/m8"
                ]
            },
            "description": "Set byte if equal"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    159
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "SETG",
                "operands": [
                    "r/m8"
                ]
            },
            "description": "Set byte if greater"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    157
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "SETGE",
                "operands": [
                    "r/m8"
                ]
            },
            "description": "Set byte if greater or equal"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    156
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "SETL",
                "operands": [
                    "r/m8"
                ]
            },
            "description": "Set byte if less"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    158
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "SETLE",
                "operands": [
                    "r/m8"
                ]
            },
            "description": "Set byte if less or equal"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    149
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "SETNE",
                "operands": [
                    "r/m8"
                ]
            },
            "description": "Set byte if not equal"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    145
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "SETNO",
                "operands": [
                    "r/m8"
                ]
            },
            "description": "Set byte if not overflow"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    153
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "SETNS",
                "operands": [
                    "r/m8"
                ]
            },
            "description": "Set byte if not sign"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    144
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "SETO",
                "operands": [
                    "r/m8"
                ]
            },
            "description": "Set byte if overflow"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    154
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "SETPE",
                "operands": [
                    "r/m8"
                ]
            },
            "description": "Set byte if parity even"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    155
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "SETPO",
                "operands": [
                    "r/m8"
                ]
            },
            "description": "Set byte if parity odd"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    152
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "SETS",
                "operands": [
                    "r/m8"
                ]
            },
            "description": "Set byte if sign"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    1
                ],
                "options": [
                    "/0"
                ]
            },
            "instruction": {
                "mnemonic": "SGDT",
                "operands": [
                    "m"
                ]
            },
            "description": "Store GDTR to m"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    1
                ],
                "options": [
                    "/1"
                ]
            },
            "instruction": {
                "mnemonic": "SIDT",
                "operands": [
                    "m"
                ]
            },
            "description": "Store IDTR to m"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    164
                ],
                "options": [
                    "/r",
                    "ib"
                ]
            },
            "instruction": {
                "mnemonic": "SHLD",
                "operands": [
                    "r/m32",
                    "r32",
                    "imm8"
                ]
            },
            "description": "Double Precision Shift Left"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    165
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "SHLD",
                "operands": [
                    "r/m32",
                    "r32",
                    "CL"
                ]
            },
            "description": "Double Precision Shift Left"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    172
                ],
                "options": [
                    "/r",
                    "ib"
                ]
            },
            "instruction": {
                "mnemonic": "SHRD",
                "operands": [
                    "r/m32",
                    "r32",
                    "imm8"
                ]
            },
            "description": "Double Precision Shift Right"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    173
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "SHRD",
                "operands": [
                    "r/m32",
                    "r32",
                    "CL"
                ]
            },
            "description": "Double Precision Shift Right"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    0
                ],
                "options": [
                    "/0"
                ]
            },
            "instruction": {
                "mnemonic": "SLDT",
                "operands": [
                    "r/m32"
                ]
            },
            "description": "Store Local Descriptor Table Register"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    1
                ],
                "options": [
                    "/4"
                ]
            },
            "instruction": {
                "mnemonic": "SMSW",
                "operands": [
                    "r/m32"
                ]
            },
            "description": "Store Machine Status Word"
        },
        {
            "opcode": {
                "bytes": [
                    249
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "STC",
                "operands": []
            },
            "description": "Set Carry Flag"
        },
        {
            "opcode": {
                "bytes": [
                    253
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "STD",
                "operands": []
            },
            "description": "Set Direction Flag"
        },
        {
            "opcode": {
                "bytes": [
                    251
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "STI",
                "operands": []
            },
            "description": "Set Interrup Flag"
        },
        {
            "opcode": {
                "bytes": [
                    170
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "STOS",
                "operands": [
                    "m8"
                ]
            },
            "description": "Store String"
        },
        {
            "opcode": {
                "bytes": [
                    171
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "STOS",
                "operands": [
                    "m32"
                ]
            },
            "description": "Store String"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    0
                ],
                "options": [
                    "/1"
                ]
            },
            "instruction": {
                "mnemonic": "STR",
                "operands": [
                    "r/m16"
                ]
            },
            "description": "Store Task Register"
        },
        {
            "opcode": {
                "bytes": [
                    44
                ],
                "options": [
                    "ib"
                ]
            },
            "instruction": {
                "mnemonic": "SUB",
                "operands": [
                    "AL",
                    "imm8"
                ]
            },
            "description": "Subtract"
        },
        {
            "opcode": {
                "bytes": [
                    45
                ],
                "options": [
                    "id"
                ]
            },
            "instruction": {
                "mnemonic": "SUB",
                "operands": [
                    "EAX",
                    "imm32"
                ]
            },
            "description": "Subtract"
        },
        {
            "opcode": {
                "bytes": [
                    128
                ],
                "options": [
                    "/5",
                    "ib"
                ]
            },
            "instruction": {
                "mnemonic": "SUB",
                "operands": [
                    "r/m8",
                    "imm8"
                ]
            },
            "description": "Subtract"
        },
        {
            "opcode": {
                "bytes": [
                    129
                ],
                "options": [
                    "/5",
                    "id"
                ]
            },
            "instruction": {
                "mnemonic": "SUB",
                "operands": [
                    "r/m32",
                    "imm32"
                ]
            },
            "description": "Subtract"
        },
        {
            "opcode": {
                "bytes": [
                    131
                ],
                "options": [
                    "/5",
                    "ib"
                ]
            },
            "instruction": {
                "mnemonic": "SUB",
                "operands": [
                    "r/m32",
                    "imm8"
                ]
            },
            "description": "Subtract"
        },
        {
            "opcode": {
                "bytes": [
                    40
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "SUB",
                "operands": [
                    "r/m8",
                    "r8"
                ]
            },
            "description": "Subtract"
        },
        {
            "opcode": {
                "bytes": [
                    41
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "SUB",
                "operands": [
                    "r/m32",
                    "r32"
                ]
            },
            "description": "Subtract"
        },
        {
            "opcode": {
                "bytes": [
                    42
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "SUB",
                "operands": [
                    "r8",
                    "r/m8"
                ]
            },
            "description": "Subtract"
        },
        {
            "opcode": {
                "bytes": [
                    43
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "SUB",
                "operands": [
                    "r32",
                    "r/m32"
                ]
            },
            "description": "Subtract"
        },
        {
            "opcode": {
                "bytes": [
                    168
                ],
                "options": [
                    "ib"
                ]
            },
            "instruction": {
                "mnemonic": "TEST",
                "operands": [
                    "AL",
                    "imm8"
                ]
            },
            "description": "Logical Compare"
        },
        {
            "opcode": {
                "bytes": [
                    169
                ],
                "options": [
                    "id"
                ]
            },
            "instruction": {
                "mnemonic": "TEST",
                "operands": [
                    "EAX",
                    "imm32"
                ]
            },
            "description": "Logical Compare"
        },
        {
            "opcode": {
                "bytes": [
                    246
                ],
                "options": [
                    "/0",
                    "ib"
                ]
            },
            "instruction": {
                "mnemonic": "TEST",
                "operands": [
                    "r/m8",
                    "imm8"
                ]
            },
            "description": "Logical Compare"
        },
        {
            "opcode": {
                "bytes": [
                    247
                ],
                "options": [
                    "/0",
                    "id"
                ]
            },
            "instruction": {
                "mnemonic": "TEST",
                "operands": [
                    "r/m32",
                    "imm32"
                ]
            },
            "description": "Logical Compare"
        },
        {
            "opcode": {
                "bytes": [
                    132
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "TEST",
                "operands": [
                    "r/m8",
                    "r8"
                ]
            },
            "description": "Logical Compare"
        },
        {
            "opcode": {
                "bytes": [
                    133
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "TEST",
                "operands": [
                    "r/m16",
                    "r16"
                ]
            },
            "description": "Logical Compare"
        },
        {
            "opcode": {
                "bytes": [
                    133
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "TEST",
                "operands": [
                    "r/m32",
                    "r32"
                ]
            },
            "description": "Logical Compare"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    11
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "UD2",
                "operands": []
            },
            "description": "Undifined Instruction"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    0
                ],
                "options": [
                    "/4"
                ]
            },
            "instruction": {
                "mnemonic": "VERR",
                "operands": [
                    "r/m16"
                ]
            },
            "description": "Verify a Segment for Reading"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    0
                ],
                "options": [
                    "/5"
                ]
            },
            "instruction": {
                "mnemonic": "VERW",
                "operands": [
                    "r/m16"
                ]
            },
            "description": "Verify a Segment for Writing"
        },
        {
            "opcode": {
                "bytes": [
                    155
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "WAIT",
                "operands": []
            },
            "description": "Wait"
        },
        {
            "opcode": {
                "bytes": [
                    155
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "FWAIT",
                "operands": []
            },
            "description": "Wait"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    9
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "WBINVD",
                "operands": []
            },
            "description": "Write Back and Invalidate Cache"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    48
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "WRMSR",
                "operands": []
            },
            "description": "Write to Model Specific Register"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    192
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "XADD",
                "operands": [
                    "r/m8",
                    "r8"
                ]
            },
            "description": "Exchange and Add"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    193
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "XADD",
                "operands": [
                    "r/m16",
                    "r16"
                ]
            },
            "description": "Exchange and Add"
        },
        {
            "opcode": {
                "bytes": [
                    15,
                    193
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "XADD",
                "operands": [
                    "r/m32",
                    "r32"
                ]
            },
            "description": "Exchange and Add"
        },
        {
            "opcode": {
                "bytes": [
                    144
                ],
                "options": [
                    "+rd"
                ]
            },
            "instruction": {
                "mnemonic": "XCHG",
                "operands": [
                    "EAX",
                    "r32"
                ]
            },
            "description": "Exchange r32 with EAX"
        },
        {
            "opcode": {
                "bytes": [
                    144
                ],
                "options": [
                    "+rd"
                ]
            },
            "instruction": {
                "mnemonic": "XCHG",
                "operands": [
                    "r32",
                    "EAX"
                ]
            },
            "description": "Exchange EAX with r32"
        },
        {
            "opcode": {
                "bytes": [
                    134
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "XCHG",
                "operands": [
                    "r/m8",
                    "r8"
                ]
            },
            "description": "Exchange byte"
        },
        {
            "opcode": {
                "bytes": [
                    134
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "XCHG",
                "operands": [
                    "r8",
                    "r/m8"
                ]
            },
            "description": "Exchange byte"
        },
        {
            "opcode": {
                "bytes": [
                    135
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "XCHG",
                "operands": [
                    "r/m32",
                    "r32"
                ]
            },
            "description": "Exchange doubleword"
        },
        {
            "opcode": {
                "bytes": [
                    135
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "XCHG",
                "operands": [
                    "r32",
                    "r/m32"
                ]
            },
            "description": "Exchange doubleword"
        },
        {
            "opcode": {
                "bytes": [
                    215
                ],
                "options": []
            },
            "instruction": {
                "mnemonic": "XLAT",
                "operands": [
                    "m8"
                ]
            },
            "description": "Table Look-up Translation"
        },
        {
            "opcode": {
                "bytes": [
                    52
                ],
                "options": [
                    "ib"
                ]
            },
            "instruction": {
                "mnemonic": "XOR",
                "operands": [
                    "AL",
                    "imm8"
                ]
            },
            "description": "Logical Exclusive OR"
        },
        {
            "opcode": {
                "bytes": [
                    53
                ],
                "options": [
                    "id"
                ]
            },
            "instruction": {
                "mnemonic": "XOR",
                "operands": [
                    "EAX",
                    "imm32"
                ]
            },
            "description": "Logical Exclusive OR"
        },
        {
            "opcode": {
                "bytes": [
                    128
                ],
                "options": [
                    "/6",
                    "ib"
                ]
            },
            "instruction": {
                "mnemonic": "XOR",
                "operands": [
                    "r/m8",
                    "imm8"
                ]
            },
            "description": "Logical Exclusive OR"
        },
        {
            "opcode": {
                "bytes": [
                    129
                ],
                "options": [
                    "/6",
                    "id"
                ]
            },
            "instruction": {
                "mnemonic": "XOR",
                "operands": [
                    "r/m32",
                    "imm32"
                ]
            },
            "description": "Logical Exclusive OR"
        },
        {
            "opcode": {
                "bytes": [
                    131
                ],
                "options": [
                    "/6",
                    "ib"
                ]
            },
            "instruction": {
                "mnemonic": "XOR",
                "operands": [
                    "r/m32",
                    "imm8"
                ]
            },
            "description": "Logical Exclusive OR"
        },
        {
            "opcode": {
                "bytes": [
                    48
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "XOR",
                "operands": [
                    "r/m8",
                    "r8"
                ]
            },
            "description": "Logical Exclusive OR"
        },
        {
            "opcode": {
                "bytes": [
                    49
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "XOR",
                "operands": [
                    "r/m32",
                    "r32"
                ]
            },
            "description": "Logical Exclusive OR"
        },
        {
            "opcode": {
                "bytes": [
                    50
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "XOR",
                "operands": [
                    "r8",
                    "r/m8"
                ]
            },
            "description": "Logical Exclusive OR"
        },
        {
            "opcode": {
                "bytes": [
                    51
                ],
                "options": [
                    "/r"
                ]
            },
            "instruction": {
                "mnemonic": "XOR",
                "operands": [
                    "r32",
                    "r/m32"
                ]
            },
            "description": "Logical Exclusive OR"
        }
    ];

    export function find(x: ((x: Define) => boolean)): Define {
        return defineTable.find(x);
    }
    export function findAll(x: ((x: Define) => boolean)): Define[] {
        return defineTable.filter(x);
    }
}

module AssemblyParser {
/*
    const grammer = `
syntaxis
  = s:syntax* { return s.filter(x => x != null); }

syntax
 = _ label:label _ ':' { return { type:"labeldef", label:label }; }
 / _ directive:directive value:((!CRLF.)* { return text(); }) CRLF { return { type: "directive", name: directive, value:value } }
 / _ mnemonic:mnemonic operands:(__ o:operands  { return o; })? CRLF { return { type:"statement", mnemonic:mnemonic, operands:(operands == null) ? [] : operands } }
 / _ CRLF { return null; }

directive = "." IDENT { return text(); }
label = IDENT
mnemonic = IDENT

operands
  = x:operand xs:( _ ',' _ y:operands { return y; })* { return Array.concat([x],...xs); }
  / a:indirect_address { return [a]; }

operand
  = '$' v:HEX { return { type:"literal", value: Number(v) } }
  / '$' v:DIGITS { return { type:"literal", value: Number(v) } }
  / '$' label:label { return { type:"labeladdress", label:label }; }
  / v:register { return v }
  / m:memory { return m; }

register
  = reg8 { return { type:"register", name: text() }; }
  / reg16 { return { type:"register", name: text() }; }
  / reg32 { return { type:"register", name: text() }; }

reg8 = '%' [abcd] [lh] { return text(); }

reg16 = '%' [abcd] "x" { return text(); }
      / '%' [bs] "p" { return text(); }
      / '%' [sd] "i" { return text(); }

reg32 = '%' 'e' [abcd] "x" { return text(); }
      / '%' 'e' [bs] "p" { return text(); }
      / '%' 'e' [sd] "i" { return text(); }

memory
  = seg:segment_override _ ':' _ addr:addressing { addr.segment = seg; return addr; }
  /                              addr:addressing {                     return addr; }

segment_override
  = sreg:sreg { return { type:"sreg", name:sreg }; }
 
sreg = '%' [cdefgs] "s" { return text(); }

addressing
  = disp:displacement _ '(' _ base:base _ is:is? ')' { return { type:"memory", segment:null, displacement:disp, base:base, index:(is == null) ? null : is[0], scale:(is == null) ? null : is[1] }; }
  /                     '(' _ base:base _ is:is? ')' { return { type:"memory", segment:null, displacement:null, base:base, index:(is == null) ? null : is[0], scale:(is == null) ? null : is[1] }; }
  / disp:displacement _ '(' _           _ is:is  ')' { return { type:"memory", segment:null, displacement:disp, base:null, index:is[0], scale:is[1] }; }
  /                     '(' _           _ is:is  ')' { return { type:"memory", segment:null, displacement:null, base:null, index:is[0], scale:is[1] }; }
  / disp:displacement                                { return { type:"memory", segment:null, displacement:disp, base:null, index:null, scale:null }; }

displacement
  = v:HEX { return { type:"literal", value: Number(v) } }
  / v:DIGITS { return { type:"literal", value: Number(v) } }
  / label:label { return { type:"labelref", label:label }; }

base = register

is = ',' _ index:index _ ',' _ scale:scale _ { return [index,scale]; } 
   / ',' _ index:index                     _ { return [index,1]; } 

index = register

scale = [1248] { return Number(text()); }

indirect_address
  = '*' _ d:displacement { return { type: "indirect_address", target: d } }
  / '*' _ register:register { return { type: "indirect_address", target: register } }
  / '*' _ '(' _ register:register _ ')' { return { type: "double_indirect_address", target: register } }

DIGITS "digits" = "-"? [0-9]+ { return text(); }

HEX "hex" = "0x" [0-9A-Fa-f]+ { return text(); }

IDENT "ident" = [A-Za-z_][A-Za-z0-9_]* { return text(); }

_ = [ \t]*
__ = [ \t]+
CRLF = _ [\r\n]+
     / _ '#' (![\r\n].)* [\r\n]+ 

`;
    export const example = `
	.file	"test_magic.c"
	.text
	.def	___main;	.scl	2;	.type	32;	.endef
	.section .rdata,"dr"
LC0:
	.ascii "magic=%x\12\0"
	.text
	.globl	_main
	.def	_main;	.scl	2;	.type	32;	.endef
_main:
LFB12:
	.cfi_startproc
	pushl	%ebp
	.cfi_def_cfa_offset 8
	.cfi_offset 5, -8
	movl	%esp, %ebp
	.cfi_def_cfa_register 5
	andl	$-16, %esp
	subl	$32, %esp
	call	___main
	call	_magic
	movl	%eax, 28(%esp)
	movl	28(%esp), %eax
	movl	%eax, 4(%esp)
	movl	$LC0, (%esp)
	call	_printf
	call	_magic
	movl	%eax, 28(%esp)
	movl	28(%esp), %eax
	movl	%eax, 4(%esp)
	movl	$LC0, (%esp)
	call	_printf
	call	_magic
	movl	%eax, 28(%esp)
	movl	28(%esp), %eax
	movl	%eax, 4(%esp)
	movl	$LC0, (%esp)
	call	_printf
	movl	$0, %eax
	leave
	.cfi_restore 5
	.cfi_def_cfa 4, 4
	ret
	.cfi_endproc
LFE12:
	.ident	"GCC: (GNU) 7.4.0"
	.def	_magic;	.scl	2;	.type	32;	.endef
	.def	_printf;	.scl	2;	.type	32;	.endef

`;
*/

    export type Ast = Directive | LabelDef | Statement;

    export interface Directive {
        type: "directive";
        name: string;
        value: string;
    }
    export interface LabelDef {
        type: "labeldef";
        label: string;
    }
    export interface Statement {
        type: "statement";
        mnemonic: string;
        operands: Operand[];
    }
    export type Operand = RegisterOperand | LiteralOperand | MemoryOperand | IndirectAddressOperand | LabelAddressOperand

    type Reg8 = '%al' | '%cl' | '%dl' | '%bl' | '%ah' | '%ch' | '%dh' | '%bh';
    type Reg16 = '%ax' | '%cx' | '%dx' | '%bx' | '%sp' | '%bp' | '%si' | '%di';
    type Reg32 = '%eax' | '%ecx' | '%edx' | '%ebx' | '%esp' | '%ebp' | '%esi' | '%edi';
    export type Registers = Reg8 | Reg16 | Reg32;

    export interface RegisterOperand {
        type: "register";
        name: Registers;
    }

    export interface LiteralOperand {
        type: "literal";
        value: number;
    }
    export interface MemoryOperand {
        type: "memory";
        segment: SegmentRegisterOperand | null;
        displacement: LabelRefOperand | LiteralOperand | null;
        base: RegisterOperand | null;
        index: RegisterOperand | null;
        scale: 1|2|4|8|null;
    }
    export interface LabelAddressOperand {
        type: "labeladdress";
        label: string;
    }
    export interface LabelRefOperand {
        type: "labelref";
        label: string;
    }

    export interface IndirectAddressOperand {
        type: "indirect_address";
        target: LabelRefOperand | LiteralOperand | RegisterOperand;
    }
    export interface DoubleIndirectAddressOperand {
        type: "double_indirect_address";
        target: RegisterOperand;
    }

    export type SegmentRegisterOperand = { type: "sreg", name: "cs" | "ds" | "es" | "fs" | "gs" | "ss" };

    export const ast: Ast[] = [
        {
            "type": "directive",
            "name": ".file",
            "value": "	\"test_magic.c\""
        },
        {
            "type": "directive",
            "name": ".text",
            "value": ""
        },
        {
            "type": "directive",
            "name": ".def",
            "value": "	___main;	.scl	2;	.type	32;	.endef"
        },
        {
            "type": "directive",
            "name": ".section",
            "value": " .rdata,\"dr\""
        },
        {
            "type": "labeldef",
            "label": "LC0"
        },
        {
            "type": "directive",
            "name": ".ascii",
            "value": " \"magic=%x\12\0\""
        },
        {
            "type": "directive",
            "name": ".text",
            "value": ""
        },
        {
            "type": "directive",
            "name": ".globl",
            "value": "	_main"
        },
        {
            "type": "directive",
            "name": ".def",
            "value": "	_main;	.scl	2;	.type	32;	.endef"
        },
        {
            "type": "labeldef",
            "label": "_main"
        },
        {
            "type": "labeldef",
            "label": "LFB12"
        },
        {
            "type": "directive",
            "name": ".cfi_startproc",
            "value": ""
        },
        {
            "type": "statement",
            "mnemonic": "pushl",
            "operands": [
                {
                    "type": "register",
                    "name": "%ebp"
                }
            ]
        },
        {
            "type": "directive",
            "name": ".cfi_def_cfa_offset",
            "value": " 8"
        },
        {
            "type": "directive",
            "name": ".cfi_offset",
            "value": " 5, -8"
        },
        {
            "type": "statement",
            "mnemonic": "movl",
            "operands": [
                {
                    "type": "register",
                    "name": "%esp"
                },
                {
                    "type": "register",
                    "name": "%ebp"
                }
            ]
        },
        {
            "type": "directive",
            "name": ".cfi_def_cfa_register",
            "value": " 5"
        },
        {
            "type": "statement",
            "mnemonic": "andl",
            "operands": [
                {
                    "type": "literal",
                    "value": -16
                },
                {
                    "type": "register",
                    "name": "%esp"
                }
            ]
        },
        {
            "type": "statement",
            "mnemonic": "subl",
            "operands": [
                {
                    "type": "literal",
                    "value": 32
                },
                {
                    "type": "register",
                    "name": "%esp"
                }
            ]
        },
        {
            "type": "statement",
            "mnemonic": "call",
            "operands": [
                {
                    "type": "memory",
                    "segment": null,
                    "displacement": {
                        "type": "labelref",
                        "label": "___main"
                    },
                    "base": null,
                    "index": null,
                    "scale": null
                }
            ]
        },
        {
            "type": "statement",
            "mnemonic": "call",
            "operands": [
                {
                    "type": "memory",
                    "segment": null,
                    "displacement": {
                        "type": "labelref",
                        "label": "_magic"
                    },
                    "base": null,
                    "index": null,
                    "scale": null
                }
            ]
        },
        {
            "type": "statement",
            "mnemonic": "movl",
            "operands": [
                {
                    "type": "register",
                    "name": "%eax"
                },
                {
                    "type": "memory",
                    "segment": null,
                    "displacement": {
                        "type": "literal",
                        "value": 28
                    },
                    "base": {
                        "type": "register",
                        "name": "%esp"
                    },
                    "index": null,
                    "scale": null
                }
            ]
        },
        {
            "type": "statement",
            "mnemonic": "movl",
            "operands": [
                {
                    "type": "memory",
                    "segment": null,
                    "displacement": {
                        "type": "literal",
                        "value": 28
                    },
                    "base": {
                        "type": "register",
                        "name": "%esp"
                    },
                    "index": null,
                    "scale": null
                },
                {
                    "type": "register",
                    "name": "%eax"
                }
            ]
        },
        {
            "type": "statement",
            "mnemonic": "movl",
            "operands": [
                {
                    "type": "register",
                    "name": "%eax"
                },
                {
                    "type": "memory",
                    "segment": null,
                    "displacement": {
                        "type": "literal",
                        "value": 4
                    },
                    "base": {
                        "type": "register",
                        "name": "%esp"
                    },
                    "index": null,
                    "scale": null
                }
            ]
        },
        {
            "type": "statement",
            "mnemonic": "movl",
            "operands": [
                {
                    "type": "labeladdress",
                    "label": "LC0"
                },
                {
                    "type": "memory",
                    "segment": null,
                    "displacement": null,
                    "base": {
                        "type": "register",
                        "name": "%esp"
                    },
                    "index": null,
                    "scale": null
                }
            ]
        },
        {
            "type": "statement",
            "mnemonic": "call",
            "operands": [
                {
                    "type": "memory",
                    "segment": null,
                    "displacement": {
                        "type": "labelref",
                        "label": "_printf"
                    },
                    "base": null,
                    "index": null,
                    "scale": null
                }
            ]
        },
        {
            "type": "statement",
            "mnemonic": "call",
            "operands": [
                {
                    "type": "memory",
                    "segment": null,
                    "displacement": {
                        "type": "labelref",
                        "label": "_magic"
                    },
                    "base": null,
                    "index": null,
                    "scale": null
                }
            ]
        },
        {
            "type": "statement",
            "mnemonic": "movl",
            "operands": [
                {
                    "type": "register",
                    "name": "%eax"
                },
                {
                    "type": "memory",
                    "segment": null,
                    "displacement": {
                        "type": "literal",
                        "value": 28
                    },
                    "base": {
                        "type": "register",
                        "name": "%esp"
                    },
                    "index": null,
                    "scale": null
                }
            ]
        },
        {
            "type": "statement",
            "mnemonic": "movl",
            "operands": [
                {
                    "type": "memory",
                    "segment": null,
                    "displacement": {
                        "type": "literal",
                        "value": 28
                    },
                    "base": {
                        "type": "register",
                        "name": "%esp"
                    },
                    "index": null,
                    "scale": null
                },
                {
                    "type": "register",
                    "name": "%eax"
                }
            ]
        },
        {
            "type": "statement",
            "mnemonic": "movl",
            "operands": [
                {
                    "type": "register",
                    "name": "%eax"
                },
                {
                    "type": "memory",
                    "segment": null,
                    "displacement": {
                        "type": "literal",
                        "value": 4
                    },
                    "base": {
                        "type": "register",
                        "name": "%esp"
                    },
                    "index": null,
                    "scale": null
                }
            ]
        },
        {
            "type": "statement",
            "mnemonic": "movl",
            "operands": [
                {
                    "type": "labeladdress",
                    "label": "LC0"
                },
                {
                    "type": "memory",
                    "segment": null,
                    "displacement": null,
                    "base": {
                        "type": "register",
                        "name": "%esp"
                    },
                    "index": null,
                    "scale": null
                }
            ]
        },
        {
            "type": "statement",
            "mnemonic": "call",
            "operands": [
                {
                    "type": "memory",
                    "segment": null,
                    "displacement": {
                        "type": "labelref",
                        "label": "_printf"
                    },
                    "base": null,
                    "index": null,
                    "scale": null
                }
            ]
        },
        {
            "type": "statement",
            "mnemonic": "call",
            "operands": [
                {
                    "type": "memory",
                    "segment": null,
                    "displacement": {
                        "type": "labelref",
                        "label": "_magic"
                    },
                    "base": null,
                    "index": null,
                    "scale": null
                }
            ]
        },
        {
            "type": "statement",
            "mnemonic": "movl",
            "operands": [
                {
                    "type": "register",
                    "name": "%eax"
                },
                {
                    "type": "memory",
                    "segment": null,
                    "displacement": {
                        "type": "literal",
                        "value": 28
                    },
                    "base": {
                        "type": "register",
                        "name": "%esp"
                    },
                    "index": null,
                    "scale": null
                }
            ]
        },
        {
            "type": "statement",
            "mnemonic": "movl",
            "operands": [
                {
                    "type": "memory",
                    "segment": null,
                    "displacement": {
                        "type": "literal",
                        "value": 28
                    },
                    "base": {
                        "type": "register",
                        "name": "%esp"
                    },
                    "index": null,
                    "scale": null
                },
                {
                    "type": "register",
                    "name": "%eax"
                }
            ]
        },
        {
            "type": "statement",
            "mnemonic": "movl",
            "operands": [
                {
                    "type": "register",
                    "name": "%eax"
                },
                {
                    "type": "memory",
                    "segment": null,
                    "displacement": {
                        "type": "literal",
                        "value": 4
                    },
                    "base": {
                        "type": "register",
                        "name": "%esp"
                    },
                    "index": null,
                    "scale": null
                }
            ]
        },
        {
            "type": "statement",
            "mnemonic": "movl",
            "operands": [
                {
                    "type": "labeladdress",
                    "label": "LC0"
                },
                {
                    "type": "memory",
                    "segment": null,
                    "displacement": null,
                    "base": {
                        "type": "register",
                        "name": "%esp"
                    },
                    "index": null,
                    "scale": null
                }
            ]
        },
        {
            "type": "statement",
            "mnemonic": "call",
            "operands": [
                {
                    "type": "memory",
                    "segment": null,
                    "displacement": {
                        "type": "labelref",
                        "label": "_printf"
                    },
                    "base": null,
                    "index": null,
                    "scale": null
                }
            ]
        },
        {
            "type": "statement",
            "mnemonic": "movl",
            "operands": [
                {
                    "type": "literal",
                    "value": 0
                },
                {
                    "type": "register",
                    "name": "%eax"
                }
            ]
        },
        {
            "type": "statement",
            "mnemonic": "leave",
            "operands": []
        },
        {
            "type": "directive",
            "name": ".cfi_restore",
            "value": " 5"
        },
        {
            "type": "directive",
            "name": ".cfi_def_cfa",
            "value": " 4, 4"
        },
        {
            "type": "statement",
            "mnemonic": "ret",
            "operands": []
        },
        {
            "type": "directive",
            "name": ".cfi_endproc",
            "value": ""
        },
        {
            "type": "labeldef",
            "label": "LFE12"
        },
        {
            "type": "directive",
            "name": ".ident",
            "value": "	\"GCC: (GNU) 7.4.0\""
        },
        {
            "type": "directive",
            "name": ".def",
            "value": "	_magic;	.scl	2;	.type	32;	.endef"
        },
        {
            "type": "directive",
            "name": ".def",
            "value": "	_printf;	.scl	2;	.type	32;	.endef"
        }
    ];
}

window.onload = () => {

    function RegNameToId(name: AssemblyParser.Registers): (0 | 1 | 2 | 3 | 4 | 5 | 6 | 7) {
        switch (name) {
            // Reg8
            case "%al": return 0;
            case "%cl": return 1;
            case "%dl": return 2;
            case "%bl": return 3;
            case "%ah": return 4;
            case "%ch": return 5;
            case "%dh": return 6;
            case "%bh": return 7;
            // Reg16
            case "%ax": return 0;
            case "%cx": return 1;
            case "%dx": return 2;
            case "%bx": return 3;
            case "%sp": return 4;
            case "%bp": return 5;
            case "%si": return 6;
            case "%di": return 7;
            // Reg32
            case "%eax": return 0;
            case "%ecx": return 1;
            case "%edx": return 2;
            case "%ebx": return 3;
            case "%esp": return 4;
            case "%ebp": return 5;
            case "%esi": return 6;
            case "%edi": return 7;
        }
    }

    function compile(inst: AssemblyParser.Statement) : number[] {
    function IsMatch(option: X86Instruction.InstructionOperand, op: AssemblyParser.Operand) {
                switch (option) {
                    case "AL": 
                        return (op.type === "register" && op.name === "%al");
                    case "AX": 
                        return (op.type === "register" && op.name === "%ax");
                    case "CL": 
                        return (op.type === "register" && op.name === "%cl");
                    case "DX": 
                        return (op.type === "register" && op.name === "%dx");
                    case "EAX": 
                        return (op.type === "register" && op.name === "%eax");
                    case "imm8": 
                        return (op.type === "literal" && isByte(op.value));
                    case "imm16": 
                        return (op.type === "literal" && isWord(op.value));
                    case "imm32": 
                        return (op.type === "literal" && isDword(op.value))
                            || (op.type === "labeladdress");
                    case "r/m8":
                        return (op.type === "register" && (/^%[abcd][lh]$/.exec(op.name) != null))
                            || (op.type === "memory");
                    case "r/m16" :
                        return (op.type === "register" && (/^%([abcd]x|sp|bp|si|di)$/.exec(op.name) != null))
                            || (op.type === "memory");
                    case "r/m32":
                        return (op.type === "register" && (/^%(e[abcd]x|esp|ebp|esi|edi)$/.exec(op.name) != null))
                            || (op.type === "memory");
                    case "r8":
                        return (op.type === "register" && (/^%[abcd][lh]$/.exec(op.name) != null));
                    case "r16":
                        return (op.type === "register" && (/^%([abcd]x|sp|bp|si|di)$/.exec(op.name) != null));
                    case "r32":
                        return (op.type === "register" && (/^%(e[abcd]x|esp|ebp|esi|edi)$/.exec(op.name) != null));
                    case "Sreg":
                        return (op.type === "register" && (/^%([cdefgs]s)$/.exec(op.name) != null));
                    case "moffs8":
                    case "moffs16":
                    case "moffs32":
                        // TODO: 上位をseg, 下位をoffsetとみなす。文法上の表記を調べてから実装。 
                        return false;
                    case "rel8":
                    case "rel32":
                    case "ptr16:32":
                    case "m16:32":
                    case "m16&32":
                    case "0":
                    case "1":
                    case "ST(i)":
                    case "ST(0)":
                    case "mm/m64":
                    case "mm":
                    case "m94/108byte":
                    case "m14/28byte":
                    case "m":
                    case "m32real":
                    case "m64real":
                    case "m80real":
                    case "m16int":
                    case "m32int":
                    case "m64int":
                    case "m80bcd":
                    case "m8":
                    case "m32":
                    case "m2byte":
                    case "DR0-DR7":
                    case "CR0":
                    case "CR2":
                    case "CR3":
                    case "CR4":
                        // TODO: こいつらも詳細を調べてから実装
                        return false;

                    default:
                    return false;
                }

        }

        const instPats: X86Instruction.Define[] = X86Instruction.findAll(x => x.instruction.mnemonic.toLowerCase() == inst.mnemonic.toLowerCase());
        const instPat: X86Instruction.Define= instPats.find(x => x.instruction.operands.every((y, i) => IsMatch(y, inst.operands[i])));
        if (instPat == null) {
            console.log("not found pattern.", inst, instPats);
            return null;
        }

        function hasROpPat(def: X86Instruction.Define): boolean {
            return def.opcode.options.indexOf('/r') != -1;
        }

        function getRegArgPatIndex(def: X86Instruction.Define): number {
            return def.instruction.operands.findIndex(x => /^r(8|16|32)$/.exec(x) != null);
        }

        function getRConstOpPatValue(def: X86Instruction.Define): string {
            return def.opcode.options.find(x => /\/[0-7]$/.exec(x) != null);
        }

        function getImmOpPatValue(def: X86Instruction.Define): string {
            return def.opcode.options.find(x => /i[bwd]$/.exec(x) != null);
        }

        function hasRMArgPat(def: X86Instruction.Define): number {
            return def.instruction.operands.findIndex(x => /^r\/m(8|16|32)$/.exec(x) != null);
        }

        function hasImmArgPat(def: X86Instruction.Define): number {
            return def.instruction.operands.findIndex(x => /^imm(8|16|32)$/.exec(x) != null);
        }

        function isByte(value: number) {
            return (0 <= value && value <= 255);
        }

        function isWord(value: number) {
            return (0 <= value && value <= 65535);
        }

        function isDword(value: number) {
            return (0 <= value && value <= 4294967295);
        }

        function uint32ToBytes(value: number): number[] {
            const ab = new ArrayBuffer(4);
            const view = new DataView(ab, 0, 4);
            view.setUint32(0, value, true);
            return [...new Uint8Array(ab)];
        }

        function uint16ToBytes(value: number): number[] {
            const ab = new ArrayBuffer(2);
            const view = new DataView(ab, 0, 2);
            view.setUint16(0, value, true);
            return [...new Uint8Array(ab)];
        }

        function uint8ToBytes(value: number): number[] {
            const ab = new ArrayBuffer(1);
            const view = new DataView(ab, 0, 1);
            view.setUint8(0, value);
            return [...new Uint8Array(ab)];
        }

        //
        // パターンに基づく命令解析
        //

        let mod = null;
        let regop = null;
        let rm = null;
        let s = null;
        let i = null;
        let b = null;
        let disp: AssemblyParser.LabelRefOperand | AssemblyParser.LiteralOperand | null = null;
        let dispsize = 0;
        let imm = null;


        {
            // optionを解析してreg/opを定める。

            let tmp: string = null;
            if (hasROpPat(instPat)) {
                // option に /r が指定されている命令は、regopにレジスタを格納
                const regPatIndex = getRegArgPatIndex(instPat);
                if (regPatIndex >= 0 && inst.operands[regPatIndex].type === "register") {
                    regop = RegNameToId((<AssemblyParser.RegisterOperand>inst.operands[regPatIndex]).name);
                } else {
                    throw new Error("/rが指定されている命令なのにregopを引数に持たない。")
                }
            } else if ((tmp = getRConstOpPatValue(instPat)) != null) {
                // option に /digit が指定されている命令は、regopに命令拡張コードを格納
                const rConst = Number(tmp.substr(1, 1));
                regop = rConst; // regopをopとして使う。
            }
        }

        {
            // r/m を引数パターンに持つ命令の場合、r/mをmodr/mとSIBにエンコードする
            const rmArgId = hasRMArgPat(instPat);
            if (rmArgId >= 0) {
                const rmArg = inst.operands[rmArgId];
                switch (rmArg.type) {
                    case 'register':
                    {
                        mod = 3;
                        rm = RegNameToId(rmArg.name);
                        break;
                    }
                    case 'memory':
                    {
                        // i386のメモリ指定形式は [Base] + [Index * Scale] + Displacement
                        // Baseにはesp以外のレジスタ、Indexにはebp以外のレジスタ、Scaleは1,2,4,8のいずれか、Displacementは8/32bit即値が指定できる。
                        // BaseとDisplacementは最低どちらか一つの指定が必要、IndexとScaleは二つ一組で省略可能。
                        // よって扱えるアドレッシングモードは限られる。
                        if (rmArg.base != null && rmArg.index == null && rmArg.displacement == null) {
                            // Baseのみを指定
                            mod = 3;
                            rm = RegNameToId(rmArg.base.name);
                            disp = null;
                            dispsize = 0;
                        } else if (rmArg.base == null && rmArg.index == null && rmArg.displacement != null) {
                            // Displacementのみを指定
                            mod = 0;
                            rm = 5;
                            disp = rmArg.displacement;
                            dispsize = 32;
                        } else if (rmArg.base == null && rmArg.index == null && rmArg.displacement != null) {
                            // BaseとDisplacementのみを指定
                            if (rmArg.displacement.type === "literal" && isByte(rmArg.displacement.value)) {
                                // [Base]+disp8なので、mod=01, rm=ベースレジスタの番号
                                mod = 1;
                                rm = RegNameToId(rmArg.base.name);
                                disp = rmArg.displacement;
                                dispsize = 8;
                            } else {
                                // [Base]+disp32なので、mod=10, rm=ベースレジスタの番号
                                mod = 2;
                                rm = RegNameToId(rmArg.base.name);
                                disp = rmArg.displacement;
                                dispsize = 32;
                            }
                        } else if (rmArg.base != null && rmArg.index != null && rmArg.displacement == null) {
                            // BaseとIndexとScaleのみを指定
                            // modを01b、rmを04にすると、[scaled*index] + disp8(0) + [Base]
                            mod = 1;
                            rm = 4;
                            s = ~~Math.log2(rmArg.scale);
                            i = RegNameToId(rmArg.index.name);
                            b = RegNameToId(rmArg.base.name);
                            disp = { type: "literal", value: 0 }; // imm8の0が指定されていると仮定
                            dispsize = 8;
                        } else if (rmArg.base == null && rmArg.index != null && rmArg.displacement != null) {
                            // DisplacementとIndexとScaleのみを指定。
                            // modを00b, bを5にすると、ベース無しのdisp32を意味する。
                            mod = 0; // ベース無しのDisp32
                            rm = 5;
                            s = ~~Math.log2(rmArg.scale);
                            i = RegNameToId(rmArg.index.name);
                            b = 5;
                            disp = rmArg.displacement;
                            dispsize = 32;
                        } else if (rmArg.base == null && rmArg.index != null && rmArg.displacement != null) {
                            // BaseとDisplacementとIndexとScaleを指定
                            if (rmArg.displacement.type === "literal" && isByte(rmArg.displacement.value)) {
                                // [scaled*index] + disp8 + [Base]なので、mod=01, rm=100, スケーリングありなので、s=log2(scale), i=インデックスレジスタ番号, bはベースレジスタの番号
                                mod = 1;
                                rm = 4;
                                s = ~~Math.log2(rmArg.scale);
                                i = RegNameToId(rmArg.index.name);
                                b = RegNameToId(rmArg.base.name);
                                disp = rmArg.displacement;
                                dispsize = 8;

                            } else {
                                // [scaled*index] + disp32 + [Base]なので、mod=10, rm=100, スケーリングありなので、s=log2(scale), i=インデックスレジスタ番号, bはベースレジスタの番号
                                mod = 2;
                                rm = 4;
                                s = ~~Math.log2(rmArg.scale);
                                i = RegNameToId(rmArg.index.name);
                                b = RegNameToId(rmArg.base.name);
                                disp = rmArg.displacement;
                                dispsize = 32;
                            }

                        } else {
                            throw new Error();
                        }
                        break;
                    }
                    default:
                    {
                        throw new Error("");
                    }
                }
            }
        }
        {
            // ib/iw/idを引数オプションに持つ命令の場合、オペランド中のimmをimmへコピー
            const immArgPat = getImmOpPatValue(instPat);
            if (immArgPat != null) {
                const immId = hasImmArgPat(instPat);
                if (immId >= 0) {
                    const immArg = inst.operands[immId];
                    console.log(immId, inst, instPat);
                    if (immArgPat === 'ib' && immArg.type === 'literal' && isByte(immArg.value)) {
                        imm = uint8ToBytes(immArg.value);
                    } else if (immArgPat === 'iw' && immArg.type === 'literal' && isWord(immArg.value)) {
                        imm = uint16ToBytes(immArg.value);
                    } else if (immArgPat === 'id' && immArg.type === 'literal' && isDword(immArg.value)) {
                        imm = uint32ToBytes(immArg.value);
                    } else if (immArgPat === 'id' && immArg.type === 'labeladdress') {
                        imm = uint32ToBytes(0); // 再配置対象
                    } else {
                        throw new Error();
                    }
                }
            }

        }

        let bytes = Array.prototype.concat(instPat.opcode.bytes);
        console.log({ op: bytes, modrm: { mod: mod, regop: regop, rm: rm }, sib: { s: s, i: i, b: b }, disp: disp });
        if (mod != null && regop != null && rm != null) {
            bytes = Array.prototype.concat(bytes, [(mod << 6) | (regop << 3) | (rm)]);
        }
        if (s != null && i != null && b != null) {
            bytes = Array.prototype.concat(bytes, [(s << 6) | (i << 3) | (b)]);
        }
        if (disp != null) {
            if (dispsize === 8 && disp.type === 'literal' && isByte(disp.value)) {
                bytes = Array.prototype.concat(bytes, uint8ToBytes(disp.value));
            } else if (dispsize === 32 && disp.type === 'literal' && isDword(disp.value)) {
                bytes = Array.prototype.concat(bytes, uint32ToBytes(disp.value));
            } else if (dispsize === 32 && disp.type === 'labelref') {
                bytes = Array.prototype.concat(bytes, uint32ToBytes(0)); // 再配置対象
            } else {
                throw new Error();
            }
        }
        if (imm != null) {
            bytes = Array.prototype.concat(bytes, imm);
        }
        console.log(bytes.map(x => ("0" + x.toString(16)).substr(-2, 2)));
        return bytes;
    }

    // 構文解析の結果こんなのが得られる
    const inst: AssemblyParser.Statement = {
        /*
            mnemonic: 'add',
            mnemonic: [
              {type:'reg', name:'%ebx' },
              {
                type:'memory', 
                base: null,
                index: null,
                scale: null,
                disp: { type: 'u32', value: 0 },
              },
            ]
        */
        type: "statement",
        mnemonic: 'adc',
        operands: [
            {
                type: 'memory',
                segment:null,
                base: null,
                index: null,
                scale: null,
                displacement: { type: 'literal', value: 0xDEADBEEF },
            },
            { type: 'literal', value: 0x12345678 }
        ]
    };

    for (const i of AssemblyParser.ast) {
        if (i.type === 'statement') {
            compile(i);
        }
    }
    /*
      addl 0x00000000, %ebx ->  03 1d 00 00 00 00 
      adcl $0x12345678, 0xDEADBEEF ->  81 15 ef be ad de 78 56 34 12
    */


};