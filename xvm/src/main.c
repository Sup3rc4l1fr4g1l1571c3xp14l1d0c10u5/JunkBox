// very tiny vm

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <stdint.h>
#include <float.h>
#include <math.h>
#include <ctype.h>
#include <stdbool.h>
#include <sys/stat.h>

typedef enum {
	Halt,
	Nop,

	Mov,	// レジスタ間コピー
	Movi,	// 即値をレジスタへロード
	Load,	// メモリからレジスタへロード
	Store,	// レジスタからメモリへストア

	Add,
	Addi,
	Sub,
	Subi,
	Mul,
	Muli,
	Div,
	Divi,
	Mod,
	Modi,

	And,
	Andi,
	Or,
	Ori,
	Xor,
	Xori,
	Not,

	Neg,

	Shl,
	Shli,
	Shr,
	Shri,
	Sal,
	Sali,
	Sar,
	Sari,

	Cmp,	// 比較
	Cmpi,	// 即値比較

	Jmp,	// 無条件ジャンプ
	Jeq,	// 等しい 					Z = 1
	Jne,	// 等しくない					Z = 0
	Jcs,	// キャリーセット				C = 1
	Jcc,	// キャリークリア				C = 0
	Jmi,	// 負 						N = 1
	Jpl,	// ゼロまたは正				N = 0
	Jvs,	// オーバーフロー			 	V = 1
	Jvc,	// オーバーフローなし		 	V = 0
	Jhi,	// ＞ 大きい (符号無し) 		(C = 0) and (Z = 0)
	Jls,	// ≦ 小さいか等しい (符号無し) 	(C = 1) or (Z =1)
	Jge,	// ≧ 大きいか等しい (符号付)	N = V
	Jlt,	// ＜ 小さい (符号付)		 	N <> V
	Jgt,	// ＞ 大きい (符号付)			(Z = 0) and (N = V)
	Jle,	// ≦ 小さいか等しい (符号付) 	(Z = 1) or (N <> V)

	Push,
	Pop,

	Call,	// 関数呼び出し
	Ret,	// 関数呼び出しからの復帰

	Cast,	// キャスト

	Swi,	// ソフトウェア割込み
} Op;

typedef enum {
	Byte = 0x00,		// .8
	Word = 0x01,		// .16
	Dword = 0x02,		// .32
	Qword = 0x03,		// .64
	Float = 0x04,		// f
	Double = 0x05,		// d
	Unsigned = 0x00,	// u.
	Signed = 0x10,		// s.
} Type;

typedef union {
	uint8_t  u8;
	uint16_t u16;
	uint32_t u32;
	uint64_t u64;
	int8_t  s8;
	int16_t s16;
	int32_t s32;
	int64_t s64;
	float   f32;
	double  f64;
	void* pointer;
} Value;

typedef enum {
	None = 0x00,
	Carry = 0x01,
	Overflow = 0x02,
	Negative = 0x04,
	Zero = 0x08,
} Flags;

typedef Value Register;

typedef enum {
	PC = 32,
	SP,
	BP,
	FLAGS
} SREG;

static Register registers[32+4];
static uint8_t* memory;

#define pc_reg registers[PC]
#define pc pc_reg.u32
#define sp_reg registers[SP]
#define sp sp_reg.u32
#define bp_reg registers[BP]
#define bp bp_reg.u32
#define flags_reg registers[FLAGS]
#define flags flags_reg.u32

#define WriteInst(mem, pc, inst) (memcpy(&mem[pc], &inst, sizeof(inst)), pc + sizeof(inst)); 

#define InstBase \
	Op op; \
	size_t sz

typedef struct {
	InstBase;
} Inst;

typedef struct {
	InstBase;
	uint8_t dest;
	uint8_t lhs;
} InstMov;

typedef struct {
	InstBase;
	uint8_t dest;
	Value value;
} InstMovi;

typedef struct {
	InstBase;
	uint8_t type;
	uint8_t dest;
	uint8_t base;
	uint32_t offset;
} InstLoadStore;

typedef struct {
	InstBase;
	uint8_t type;
	uint8_t dest;
	uint8_t lhs;
	uint8_t rhs;
} InstBinOp;

typedef struct {
	InstBase;
	uint8_t type;
	uint8_t dest;
	uint8_t lhs;
	Value value;
} InstBinOpi;

typedef struct {
	InstBase;
	uint8_t type;
	uint8_t dest;
	uint8_t lhs;
	uint8_t rhs;
} InstUnaryOp;

typedef struct {
	InstBase;
	uint8_t type;
	uint8_t lhs;
	uint8_t rhs;
} InstCmpOp;

typedef struct {
	InstBase;
	uint8_t type;
	uint8_t lhs;
	Value value;
} InstCmpiOp;

typedef struct {
	InstBase;
	uint8_t lhs;
	uint32_t offset;
} InstJmp;

typedef struct {
	InstBase;
	uint8_t lhs;
} InstPush;

typedef struct {
	InstBase;
	uint8_t dest;
} InstPop;

typedef struct {
	InstBase;
	uint8_t lhs;
	uint32_t offset;
} InstCall;


typedef struct {
	InstBase;
	uint8_t tydest;
	uint8_t dest;
	uint8_t tysrc;
	uint8_t src;
} InstCast;

typedef struct {
	InstBase;
	uint32_t code;
} InstSwi;

// 停止
#define halt() (Inst){ .op = Halt, .sz = sizeof(Inst) }
#define nop() (Inst){ .op = Nop, .sz = sizeof(Inst) }

// レジスタ←レジスタ　コピー
#define mov(dst, src) (InstMov){ .op = Mov, .sz = sizeof(InstMov), .dest = (dst), .lhs = (src) }
// レジスタ←即値　転送
#define movi(ty, r, v) (InstMovi){ .op = Movi, .sz = sizeof(InstMovi), .dest = (r), .value = (v) }
// レジスタ←メモリ[register+offset]　転送
#define load(ty, r, b, o) (InstLoadStore){ .op = Load, .sz = sizeof(InstLoadStore), .type = (ty), .dest = (r), .base = (b), .offset = (o) }
// メモリ[register+offset]←レジスタ　転送
#define store(ty, b, o, r) (InstLoadStore){ .op = Store, .sz = sizeof(InstLoadStore), .type = (ty), .dest = (r), .base = (b), .offset = (o) }

// 四則演算
#define add(ty, dst, l, r) (InstBinOp){ .op = Add, .sz = sizeof(InstBinOp), .type = (ty), .dest = (dst), .lhs = (l), .rhs = (r) }
#define addi(ty, dst, l, v) (InstBinOpi){ .op = Addi, .sz = sizeof(InstBinOpi), .type = (ty), .dest = (dst), .lhs = (l), .value = (v) }
#define sub(ty, dst, l, r) (InstBinOp){ .op = Sub, .sz = sizeof(InstBinOp), .type = (ty), .dest = (dst), .lhs = (l), .rhs = (r) }
#define subi(ty, dst, l, v) (InstBinOpi){ .op = Subi, .sz = sizeof(InstBinOpi), .type = (ty), .dest = (dst), .lhs = (l), .value = (v) }
#define mul(ty, dst, l, r) (InstBinOp){ .op = Mul, .sz = sizeof(InstBinOp), .type = (ty), .dest = (dst), .lhs = (l), .rhs = (r) }
#define muli(ty, dst, l, v) (InstBinOpi){ .op = Muli, .sz = sizeof(InstBinOpi), .type = (ty), .dest = (dst), .lhs = (l), .value = (v) }
#define div(ty, dst, l, r) (InstBinOp){ .op = Div, .sz = sizeof(InstBinOp), .type = (ty), .dest = (dst), .lhs = (l), .rhs = (r) }
#define divi(ty, dst, l, v) (InstBinOpi){ .op = Divi, .sz = sizeof(InstBinOpi), .type = (ty), .dest = (dst), .lhs = (l), .value = (v) }
#define mod(ty, dst, l, r) (InstBinOp){ .op = Mod, .sz = sizeof(InstBinOp), .type = (ty), .dest = (dst), .lhs = (l), .rhs = (r) }
#define modi(ty, dst, l, v) (InstBinOpi){ .op = Modi, .sz = sizeof(InstBinOpi), .type = (ty), .dest = (dst), .lhs = (l), .value = (v) }

// ビット演算
#define and(ty, dst, l, r) (InstBinOp){ .op = And, .sz = sizeof(InstBinOp), .type = (ty), .dest = (dst), .lhs = (l), .rhs = (r) }
#define andi(ty, dst, l, v) (InstBinOpi){ .op = Andi, .sz = sizeof(InstBinOpi), .type = (ty), .dest = (dst), .lhs = (l), .value = (v) }
#define or(ty, dst, l, r) (InstBinOp){ .op = Or, .sz = sizeof(InstBinOp), .type = (ty), .dest = (dst), .lhs = (l), .rhs = (r) }
#define ori(ty, dst, l, v) (InstBinOpi){ .op = Ori, .sz = sizeof(InstBinOpi), .type = (ty), .dest = (dst), .lhs = (l), .value = (v) }
#define xor(ty, dst, l, r) (InstBinOp){ .op = Xor, .sz = sizeof(InstBinOp), .type = (ty), .dest = (dst), .lhs = (l), .rhs = (r) }
#define xori(ty, dst, l, v) (InstBinOpi){ .op = Xori, .sz = sizeof(InstBinOpi), .type = (ty), .dest = (dst), .lhs = (l), .value = (v) }
#define not(ty, dst, l) (InstUnaryOp){ .op = Not, .sz = sizeof(InstUnaryOp), .type = (ty), .dest = (dst), .lhs = (l) }

// 符号操作
#define neg(ty, dst, l) (InstUnaryOp){ .op = Neg, .sz = sizeof(InstUnaryOp), .type = (ty), .dest = (dst), .lhs = (l) }

// シフト演算
#define shl(ty, dst, l, r) (InstBinOp){ .op = Shl, .sz = sizeof(InstBinOp), .type = (ty), .dest = (dst), .lhs = (l), .rhs = (r) }
#define shli(ty, dst, l, v) (InstBinOpi){ .op = Shl, .sz = sizeof(InstBinOpi), .type = (ty), .dest = (dst), .lhs = (l), .value = (v) }
#define shr(ty, dst, l, r) (InstBinOp){ .op = Shr, .sz = sizeof(InstBinOp), .type = (ty), .dest = (dst), .lhs = (l), .rhs = (r) }
#define shri(ty, dst, l, v) (InstBinOpi){ .op = Shr, .sz = sizeof(InstBinOpi), .type = (ty), .dest = (dst), .lhs = (l), .value = (v) }
#define sal(ty, dst, l, r) (InstBinOp){ .op = Sal, .sz = sizeof(InstBinOp), .type = (ty), .dest = (dst), .lhs = (l), .rhs = (r) }
#define sali(ty, dst, l, v) (InstBinOpi){ .op = Sal, .sz = sizeof(InstBinOpi), .type = (ty), .dest = (dst), .lhs = (l), .value = (v) }
#define sar(ty, dst, l, r) (InstBinOp){ .op = Sar, .sz = sizeof(InstBinOp), .type = (ty), .dest = (dst), .lhs = (l), .rhs = (r) }
#define sari(ty, dst, l, v) (InstBinOpi){ .op = Sar, .sz = sizeof(InstBinOpi), .type = (ty), .dest = (dst), .lhs = (l), .value = (v) }

// 比較
#define cmp(ty, l, r) (InstCmpOp){ .op = Cmp, .sz = sizeof(InstCmpOp), .type = (ty), .lhs = (l), .rhs = (r) }
#define cmpi(ty, l, v) (InstCmpiOp){ .op = Cmpi, .sz = sizeof(InstCmpiOp), .type = (ty), .lhs = (l), .value = (v) }

// 分岐・ジャンプ命令
#define jmp(r, a) (InstJmp){ .op = Jmp, .sz = sizeof(InstJmp), .lhs = r, .offset = a }
#define jeq(r, a) (InstJmp){ .op = Jeq, .sz = sizeof(InstJmp), .lhs = r, .offset = a }
#define jne(r, a) (InstJmp){ .op = Jne, .sz = sizeof(InstJmp), .lhs = r, .offset = a }
#define jcs(r, a) (InstJmp){ .op = Jcs, .sz = sizeof(InstJmp), .lhs = r, .offset = a }
#define jcc(r, a) (InstJmp){ .op = Jcc, .sz = sizeof(InstJmp), .lhs = r, .offset = a }
#define jmi(r, a) (InstJmp){ .op = Jmi, .sz = sizeof(InstJmp), .lhs = r, .offset = a }
#define jpl(r, a) (InstJmp){ .op = Jpl, .sz = sizeof(InstJmp), .lhs = r, .offset = a }
#define jvs(r, a) (InstJmp){ .op = Jvs, .sz = sizeof(InstJmp), .lhs = r, .offset = a }
#define jvc(r, a) (InstJmp){ .op = Jvc, .sz = sizeof(InstJmp), .lhs = r, .offset = a }
#define jhi(r, a) (InstJmp){ .op = Jhi, .sz = sizeof(InstJmp), .lhs = r, .offset = a }
#define jls(r, a) (InstJmp){ .op = Jls, .sz = sizeof(InstJmp), .lhs = r, .offset = a }
#define jge(r, a) (InstJmp){ .op = Jge, .sz = sizeof(InstJmp), .lhs = r, .offset = a }
#define jlt(r, a) (InstJmp){ .op = Jlt, .sz = sizeof(InstJmp), .lhs = r, .offset = a }
#define jgt(r, a) (InstJmp){ .op = Jgt, .sz = sizeof(InstJmp), .lhs = r, .offset = a }
#define jle(r, a) (InstJmp){ .op = Jle, .sz = sizeof(InstJmp), .lhs = r, .offset = a }

#define push(l) (InstPush){ .op = Push, .sz = sizeof(InstPush), .lhs = (l) }
#define pop(d) (InstPop){ .op = Pop, .sz = sizeof(InstPop), .dest = (d) }

#define call(r,a) (InstCall){ .op = Call, .sz = sizeof(InstCall), .lhs = r, .offset = a }
#define ret() (Inst){ .op = Ret, .sz = sizeof(Inst) }

// キャスト
#define cast(td, d, ts, s) (InstCast){ .op = Cast, .sz = sizeof(InstCast), .tydest = (td), .dest = (d), .tysrc = (ts), .src = (s) }


// ソフトウェア割込み
#define swi(c) (InstSwi){ .op = Swi, .sz = sizeof(InstSwi), .code = (c) }

#define Reg(n) n
#define Const(n) n

#define BinOp(p, op) \
	UIntBinOp(p, op) \
	SIntBinOp(p, op) \
	FloatBinOp(p, op) \
	DefaultBinOp(p, op) \

#define UIntBinOp(p, op) \
	case Unsigned | Byte: registers[p->dest].u8 = registers[p->lhs].u8 op registers[p->rhs].u8; break; \
	case Unsigned | Word: registers[p->dest].u16 = registers[p->lhs].u16 op registers[p->rhs].u16; break; \
	case Unsigned | Dword: registers[p->dest].u32 = registers[p->lhs].u32 op registers[p->rhs].u32; break; \
	case Unsigned | Qword: registers[p->dest].u64 = registers[p->lhs].u64 op registers[p->rhs].u64; break; \

#define SIntBinOp(p, op) \
	case Signed | Byte:registers[p->dest].s8 = registers[p->lhs].s8 op registers[p->rhs].s8; break; \
	case Signed | Word:registers[p->dest].s16 = registers[p->lhs].s16 op registers[p->rhs].s16; break; \
	case Signed | Dword: registers[p->dest].s32 = registers[p->lhs].s32 op registers[p->rhs].s32; break; \
	case Signed | Qword: registers[p->dest].s64 = registers[p->lhs].s64 op registers[p->rhs].s64; break; \

#define FloatBinOp(p, op) \
	case Float: registers[p->dest].f32 = registers[p->lhs].f32 op registers[p->rhs].f32; break; \
	case Double: registers[p->dest].f64 = registers[p->lhs].f64 op registers[p->rhs].f64; break; \

#define DefaultBinOp(p, op) \
	default: perror("bad instruction size"); exit(-1); break;

#define BinOpi(p, op) \
	UIntBinOpi(p, op) \
	SIntBinOpi(p, op) \
	FloatBinOpi(p, op) \
	DefaultBinOpi(p, op) \

#define UIntBinOpi(p, op) \
	case Unsigned | Byte: registers[p->dest].u8 = registers[p->lhs].u8 op p->value.u8; break; \
	case Unsigned | Word: registers[p->dest].u16 = registers[p->lhs].u16 op p->value.u16; break; \
	case Unsigned | Dword: registers[p->dest].u32 = registers[p->lhs].u32 op p->value.u32; break; \
	case Unsigned | Qword: registers[p->dest].u64 = registers[p->lhs].u64 op p->value.u64; break; \

#define SIntBinOpi(p, op) \
	case Signed | Byte:registers[p->dest].s8 = registers[p->lhs].s8 op p->value.s8; break; \
	case Signed | Word:registers[p->dest].s16 = registers[p->lhs].s16 op p->value.s16; break; \
	case Signed | Dword: registers[p->dest].s32 = registers[p->lhs].s32 op p->value.s32; break; \
	case Signed | Qword: registers[p->dest].s64 = registers[p->lhs].s64 op p->value.s64; break; \

#define FloatBinOpi(p, op) \
	case Float: registers[p->dest].f32 = registers[p->lhs].f32 op p->value.f32; break; \
	case Double: registers[p->dest].f64 = registers[p->lhs].f64 op p->value.f64; break; \

#define DefaultBinOpi(p, op) \
	default: perror("bad instruction size"); exit(-1); break;

#define UnaryOp(p, op) \
	UIntUnaryOp(p, op) \
	SIntUnaryOp(p, op) \
	FloatUnaryOp(p, op) \
	DefaultUnaryOp(p, op) \

#define UIntUnaryOp(p, op) \
	case Unsigned | Byte: registers[p->dest].u8 = op registers[p->lhs].u8; break; \
	case Unsigned | Word: registers[p->dest].u16 = op registers[p->lhs].u16; break; \
	case Unsigned | Dword: registers[p->dest].u32 = op registers[p->lhs].u32; break; \
	case Unsigned | Qword: registers[p->dest].u64 = op registers[p->lhs].u64; break; \

#define SIntUnaryOp(p, op) \
	case Signed | Byte:registers[p->dest].s8 = op registers[p->lhs].s8; break; \
	case Signed | Word:registers[p->dest].s16 = op registers[p->lhs].s16; break; \
	case Signed | Dword: registers[p->dest].s32 = op registers[p->lhs].s32; break; \
	case Signed | Qword: registers[p->dest].s64 = op registers[p->lhs].s64; break; \

#define FloatUnaryOp(p, op) \
	case Float: registers[p->dest].f32 = op registers[p->lhs].f32; break; \
	case Double: registers[p->dest].f64 = op registers[p->lhs].f64; break; \

#define DefaultUnaryOp(p, op) \
	default: perror("bad instruction size"); exit(-1); break;

void run() {

	for (;;) {
		Inst* i = (Inst*)&memory[pc];
		pc += i->sz;
		switch (i->op) {
		case Halt: {
			goto Exit;
		}
		case Nop: {
			break;
		}
		case Mov: {
			InstMov* p = (InstMov*)i;
			registers[p->dest] = registers[p->lhs];
			break;
		}
		case Movi: {
			InstMovi* p = (InstMovi*)i;
			registers[p->dest] = p->value;
			break;
		}
		case Load: {
			InstLoadStore* p = (InstLoadStore*)i;
			switch (p->type) {
			case Byte: memcpy(&registers[p->dest].u8, &memory[registers[p->base].u32 + p->offset], 1); break;
			case Word: memcpy(&registers[p->dest].u16, &memory[registers[p->base].u32 + p->offset], 2); break;
			case Dword: memcpy(&registers[p->dest].u32, &memory[registers[p->base].u32 + p->offset], 4); break;
			case Qword: memcpy(&registers[p->dest].u64, &memory[registers[p->base].u32 + p->offset], 8); break;
			case Float: memcpy(&registers[p->dest].f32, &memory[registers[p->base].u32 + p->offset], 4); break;
			case Double: memcpy(&registers[p->dest].f64, &memory[registers[p->base].u32 + p->offset], 8); break;
			default: perror("bad instruction size"); exit(-1); break;
			}
			break;
		}
		case Store: {
			InstLoadStore* p = (InstLoadStore*)i;
			switch (p->type) {
			case Byte: memcpy(&memory[registers[p->dest].u32 + p->offset], &registers[p->base].u8, 1); break;
			case Word: memcpy(&memory[registers[p->dest].u32 + p->offset], &registers[p->base].u16, 2); break;
			case Dword: memcpy(&memory[registers[p->dest].u32 + p->offset], &registers[p->base].u32, 4); break;
			case Qword: memcpy(&memory[registers[p->dest].u32 + p->offset], &registers[p->base].u64, 8); break;
			case Float: memcpy(&memory[registers[p->dest].u32 + p->offset], &registers[p->base].f32, 4); break;
			case Double: memcpy(&memory[registers[p->dest].u32 + p->offset], &registers[p->base].f64, 8); break;
			default: perror("bad instruction size"); exit(-1); break;
			}
			break;
		}
		case Add: {
			InstBinOp* p = (InstBinOp*)i;
			switch (p->type) {
				BinOp(p, +)
			}
			break;
		}
		case Addi: {
			InstBinOpi* p = (InstBinOpi*)i;
			switch (p->type) {
				BinOpi(p, +)
			}
			break;
		}
		case Sub: {
			InstBinOp* p = (InstBinOp*)i;
			switch (p->type) {
				BinOp(p, -)
			}
			break;
		}
		case Subi: {
			InstBinOpi* p = (InstBinOpi*)i;
			switch (p->type) {
				BinOpi(p, -)
			}
			break;
		}
		case Mul: {
			InstBinOp* p = (InstBinOp*)i;
			switch (p->type) {
				BinOp(p, *)
			}
			break;
		}
		case Muli: {
			InstBinOpi* p = (InstBinOpi*)i;
			switch (p->type) {
				BinOpi(p, *)
			}
			break;
		}
		case Div: {
			InstBinOp* p = (InstBinOp*)i;
			switch (p->type) {
				BinOp(p, / )
			}
			break;
		}
		case Divi: {
			InstBinOpi* p = (InstBinOpi*)i;
			switch (p->type) {
				BinOpi(p, / )
			}
			break;
		}
		case Mod: {
			InstBinOp* p = (InstBinOp*)i;
			switch (p->type) {
				UIntBinOp(p, %)
					SIntBinOp(p, %)
					DefaultBinOp(p, %)
			}
			break;
		}
		case Modi: {
			InstBinOpi* p = (InstBinOpi*)i;
			switch (p->type) {
				UIntBinOpi(p, %)
					SIntBinOpi(p, %)
					DefaultBinOpi(p, %)
			}
			break;
		}
		case Neg: {
			InstUnaryOp* p = (InstUnaryOp*)i;
			switch (p->type) {
				case Unsigned | Byte:registers[p->dest].s8 = - registers[p->lhs].s8; break;
				case Unsigned | Word:registers[p->dest].s16 = - registers[p->lhs].s16; break;
				case Unsigned | Dword: registers[p->dest].s32 = - registers[p->lhs].s32; break;
				case Unsigned | Qword: registers[p->dest].s64 = - registers[p->lhs].s64; break;
				SIntUnaryOp(p, -)
				FloatUnaryOp(p, -)
				DefaultUnaryOp(p, -)
			}
			break;
		}
		case Shl: {
			InstBinOp* p = (InstBinOp*)i;
			switch (p->type & 0x0F) {
				UIntBinOp(p, << )
					DefaultBinOp(p, << )
			}
			break;
		}
		case Shli: {
			InstBinOpi* p = (InstBinOpi*)i;
			switch (p->type & 0x0F) {
				UIntBinOpi(p, << )
					DefaultBinOpi(p, << )
			}
			break;
		}
		case Shr: {
			InstBinOp* p = (InstBinOp*)i;
			switch (p->type & 0x0F) {
				UIntBinOp(p, >> )
					DefaultBinOp(p, >> )
			}
			break;
		}
		case Shri: {
			InstBinOpi* p = (InstBinOpi*)i;
			switch (p->type & 0x0F) {
				UIntBinOpi(p, >> )
					DefaultBinOpi(p, >> )
			}
			break;
		}
		case Sal: {
			InstBinOp* p = (InstBinOp*)i;
			switch (p->type | 0x80) {
				SIntBinOp(p, << )
					DefaultBinOp(p, << )
			}
			break;
		}
		case Sali: {
			InstBinOpi* p = (InstBinOpi*)i;
			switch (p->type | 0x80) {
				SIntBinOpi(p, << )
					DefaultBinOpi(p, << )
			}
			break;
		}
		case Sar: {
			InstBinOp* p = (InstBinOp*)i;
			switch (p->type | 0x80) {
				SIntBinOp(p, >> )
					DefaultBinOp(p, >> )
			}
			break;
		}
		case Sari: {
			InstBinOpi* p = (InstBinOpi*)i;
			switch (p->type | 0x80) {
				SIntBinOpi(p, >> )
					DefaultBinOpi(p, >> )
			}
			break;
		}
		case Cmp: {
			InstCmpOp* p = (InstCmpOp*)i;
			switch (p->type & 0x0F) {
			case Byte: {
				uint8_t lhs = registers[p->lhs].u8;
				uint8_t rhs = registers[p->rhs].u8;
				uint8_t ret = lhs - rhs;
				flags = None;
				if (lhs < rhs) { flags |= Carry; }
				if (((lhs & 0x80U) != (rhs & 0x80U)) && ((rhs & 0x80U) == (ret & 0x80U))) { flags |= Overflow; }
				if ((ret & 0x80U) != 0x00U) { flags |= Negative; };
				if (ret == 0x00U) { flags |= Zero; };
				break;
			}
			case Word: {
				uint16_t lhs = registers[p->lhs].u16;
				uint16_t rhs = registers[p->rhs].u16;
				uint16_t ret = lhs - rhs;
				flags = None;
				if (lhs < rhs) { flags |= Carry; }
				if (((lhs & 0x8000U) != (rhs & 0x8000U)) && ((rhs & 0x8000U) == (ret & 0x8000U))) { flags |= Overflow; }
				if ((ret & 0x8000U) != 0x0000U) { flags |= Negative; };
				if (ret == 0x0000U) { flags |= Zero; };
				break;
			}
			case Dword: {
				uint32_t lhs = registers[p->lhs].u32;
				uint32_t rhs = registers[p->rhs].u32;
				uint32_t ret = lhs - rhs;
				flags = None;
				if (lhs < rhs) { flags |= Carry; }
				if (((lhs & 0x80000000U) != (rhs & 0x80000000U)) && ((rhs & 0x80000000U) == (ret & 0x80000000U))) { flags |= Overflow; }
				if ((ret & 0x80000000U) != 0x00000000U) { flags |= Negative; };
				if (ret == 0x00000000U) { flags |= Zero; };
				break;
			}
			case Qword: {
				uint64_t lhs = registers[p->lhs].u64;
				uint64_t rhs = registers[p->rhs].u64;
				uint64_t ret = lhs - rhs;
				flags = None;
				if (lhs < rhs) { flags |= Carry; }
				if (((lhs & 0x8000000000000000U) != (rhs & 0x8000000000000000U)) && ((rhs & 0x8000000000000000U) == (ret & 0x8000000000000000U))) { flags |= Overflow; }
				if ((ret & 0x8000000000000000U) != 0x0000000000000000U) { flags |= Negative; };
				if (ret == 0x0000000000000000U) { flags |= Zero; };
				break;
			}
			case Float: {
				float lhs = registers[p->lhs].f32;
				float rhs = registers[p->rhs].f32;
				float ret = lhs - rhs;
				flags = None;
				if (lhs < rhs) { flags |= Carry; }
				if (isnan(lhs) || isnan(rhs) || isnan(ret)) { flags |= Overflow; }
				if (ret < 0) { flags |= Negative; };
				if (fabsf(ret) < FLT_EPSILON) { flags |= Zero; };
				break;
			}
			case Double: {
				double lhs = registers[p->lhs].f32;
				double rhs = registers[p->rhs].f32;
				double ret = lhs - rhs;
				flags = None;
				if (lhs < rhs) { flags |= Carry; }
				if (isnan(lhs) || isnan(rhs) || isnan(ret)) { flags |= Overflow; }
				if (ret < 0) { flags |= Negative; };
				if (fabs(ret) < DBL_EPSILON) { flags |= Zero; };
				break;
			}
			default: perror("bad instruction size"); exit(-1); break;
			}
			break;
		}
		case Cmpi: {
			InstCmpiOp* p = (InstCmpiOp*)i;
			switch (p->type & 0x0F) {
			case Byte: {
				uint8_t lhs = registers[p->lhs].u8;
				uint8_t rhs = p->value.u8;
				uint8_t ret = lhs - rhs;
				flags = None;
				if (lhs < rhs) { flags |= Carry; }
				if (((lhs & 0x80U) != (rhs & 0x80U)) && ((rhs & 0x80U) == (ret & 0x80U))) { flags |= Overflow; }
				if ((ret & 0x80U) != 0x00U) { flags |= Negative; };
				if (ret == 0x00U) { flags |= Zero; };
				break;
			}
			case Word: {
				uint16_t lhs = registers[p->lhs].u16;
				uint16_t rhs = p->value.u16;
				uint16_t ret = lhs - rhs;
				flags = None;
				if (lhs < rhs) { flags |= Carry; }
				if (((lhs & 0x8000U) != (rhs & 0x8000U)) && ((rhs & 0x8000U) == (ret & 0x8000U))) { flags |= Overflow; }
				if ((ret & 0x8000U) != 0x0000U) { flags |= Negative; };
				if (ret == 0x0000U) { flags |= Zero; };
				break;
			}
			case Dword: {
				uint32_t lhs = registers[p->lhs].u32;
				uint32_t rhs = p->value.u32;
				uint32_t ret = lhs - rhs;
				flags = None;
				if (lhs < rhs) { flags |= Carry; }
				if (((lhs & 0x80000000U) != (rhs & 0x80000000U)) && ((rhs & 0x80000000U) == (ret & 0x80000000U))) { flags |= Overflow; }
				if ((ret & 0x80000000U) != 0x00000000U) { flags |= Negative; };
				if (ret == 0x00000000U) { flags |= Zero; };
				break;
			}
			case Qword: {
				uint64_t lhs = registers[p->lhs].u64;
				uint64_t rhs = p->value.u64;
				uint64_t ret = lhs - rhs;
				flags = None;
				if (lhs < rhs) { flags |= Carry; }
				if (((lhs & 0x8000000000000000U) != (rhs & 0x8000000000000000U)) && ((rhs & 0x8000000000000000U) == (ret & 0x8000000000000000U))) { flags |= Overflow; }
				if ((ret & 0x8000000000000000U) != 0x0000000000000000U) { flags |= Negative; };
				if (ret == 0x0000000000000000U) { flags |= Zero; };
				break;
			}
			case Float: {
				float lhs = registers[p->lhs].f32;
				float rhs = p->value.f32;
				float ret = lhs - rhs;
				flags = None;
				if (lhs < rhs) { flags |= Carry; }
				if (isnan(lhs) || isnan(rhs) || isnan(ret)) { flags |= Overflow; }
				if (ret < 0) { flags |= Negative; };
				if (fabsf(ret) < FLT_EPSILON) { flags |= Zero; };
				break;
			}
			case Double: {
				double lhs = registers[p->lhs].f32;
				double rhs = p->value.f32;
				double ret = lhs - rhs;
				flags = None;
				if (lhs < rhs) { flags |= Carry; }
				if (isnan(lhs) || isnan(rhs) || isnan(ret)) { flags |= Overflow; }
				if (ret < 0) { flags |= Negative; };
				if (fabs(ret) < DBL_EPSILON) { flags |= Zero; };
				break;
			}
			default: perror("bad instruction size"); exit(-1); break;
			}
			break;
		}
		case Jmp: {
			InstJmp* p = (InstJmp*)i;
			pc = registers[p->lhs].u32 + p->offset;
			break;
		}
		case Jeq: {
			InstJmp* p = (InstJmp*)i;
			if (flags & Zero) {
				pc = registers[p->lhs].u32 + p->offset;
			}
			break;
		}
		case Jne: {
			InstJmp* p = (InstJmp*)i;
			if (!(flags & Zero)) {
				pc = registers[p->lhs].u32 + p->offset;
			}
			break;
		}
		case Jcs: {
			InstJmp* p = (InstJmp*)i;
			if (flags & Carry) {
				pc = registers[p->lhs].u32 + p->offset;
			}
			break;
		}
		case Jcc: {
			InstJmp* p = (InstJmp*)i;
			if (!(flags & Carry)) {
				pc = registers[p->lhs].u32 + p->offset;
			}
			break;
		}
		case Jmi: {
			InstJmp* p = (InstJmp*)i;
			if (flags & Negative) {
				pc = registers[p->lhs].u32 + p->offset;
			}
			break;
		}
		case Jpl: {
			InstJmp* p = (InstJmp*)i;
			if (!(flags & Negative)) {
				pc = registers[p->lhs].u32 + p->offset;
			}
			break;
		}
		case Jvs: {
			InstJmp* p = (InstJmp*)i;
			if (flags & Overflow) {
				pc = registers[p->lhs].u32 + p->offset;
			}
			break;
		}
		case Jvc: {
			InstJmp* p = (InstJmp*)i;
			if (!(flags & Overflow)) {
				pc = registers[p->lhs].u32 + p->offset;
			}
			break;
		}
		case Jhi: {
			InstJmp* p = (InstJmp*)i;
			if ((flags & (Carry | Zero)) == 0) {
				pc = registers[p->lhs].u32 + p->offset;
			}
			break;
		}
		case Jls: {
			InstJmp* p = (InstJmp*)i;
			if ((flags & (Carry | Zero)) != 0) {
				pc = registers[p->lhs].u32 + p->offset;
			}
			break;
		}
		case Jge: {
			InstJmp* p = (InstJmp*)i;
			if (((flags & Negative) != 0) == ((flags & Overflow) != 0)) {
				pc = registers[p->lhs].u32 + p->offset;
			}
			break;
		}
		case Jlt: {
			InstJmp* p = (InstJmp*)i;
			if (((flags & Negative) != 0) != ((flags & Overflow) != 0)) {
				pc = registers[p->lhs].u32 + p->offset;
			}
			break;
		}
		case Jgt: {
			InstJmp* p = (InstJmp*)i;
			if ((((flags & Negative) != 0) == ((flags & Overflow) != 0)) && ((flags & Zero) != Zero)) {
				pc = registers[p->lhs].u32 + p->offset;
			}
			break;
		}
		case Jle: {
			InstJmp* p = (InstJmp*)i;
			if ((((flags & Negative) != 0) != ((flags & Overflow) != 0)) || ((flags & Zero) == Zero)) {
				pc = registers[p->lhs].u32 + p->offset;
			}
			break;
		}
		case Push: {
			InstPush* p = (InstPush*)i;
			sp -= sizeof(Register);
			memcpy(&memory[sp], &registers[p->lhs],sizeof(Register));
			break;
		}
		case Pop: {
			InstPop* p = (InstPop*)i;
			memcpy(&registers[p->dest], &memory[sp], sizeof(Register));
			sp += sizeof(Value);
			break;
		}
		case Call: {
			InstCall* p = (InstCall*)i;
			sp -= sizeof(Register) * 3;
			memcpy(&memory[sp + sizeof(Register) * 0], &pc_reg, sizeof(Register));
			memcpy(&memory[sp + sizeof(Register) * 1], &bp_reg, sizeof(Register));
			memcpy(&memory[sp + sizeof(Register) * 2], &sp_reg, sizeof(Register));
			bp = sp;
			pc = registers[p->lhs].u32 + p->offset;
			break;
		}
		case Ret: {
			Inst* p = (Inst*)i;
			uint32_t tbp = bp;
			memcpy(&pc_reg, &memory[tbp + sizeof(Register) * 0], sizeof(Register));
			memcpy(&bp_reg, &memory[tbp + sizeof(Register) * 1], sizeof(Register));
			memcpy(&sp_reg, &memory[tbp + sizeof(Register) * 2], sizeof(Register));
			break;
		}
		case Cast: {
			InstCast* p = (InstCast*)i;
#define Cons(x,y) (((x) << 8)|(y))
			switch (Cons(p->tydest, p->tysrc)) {
				case Cons(Unsigned | Byte   , Unsigned | Byte): registers[p->dest].u8 = registers[p->src].u8; break;
				case Cons(Unsigned | Word   , Unsigned | Byte): registers[p->dest].u16 = registers[p->src].u8; break;
				case Cons(Unsigned | Dword  , Unsigned | Byte): registers[p->dest].u32 = registers[p->src].u8; break;
				case Cons(Unsigned | Qword  , Unsigned | Byte): registers[p->dest].u64 = registers[p->src].u8; break;
				case Cons(Signed   | Byte   , Unsigned | Byte): registers[p->dest].s8 = registers[p->src].u8; break;
				case Cons(Signed   | Word   , Unsigned | Byte): registers[p->dest].s16 = registers[p->src].u8; break;
				case Cons(Signed   | Dword  , Unsigned | Byte): registers[p->dest].s32 = registers[p->src].u8; break;
				case Cons(Signed   | Qword  , Unsigned | Byte): registers[p->dest].s64 = registers[p->src].u8; break;
				case Cons(           Float  , Unsigned | Byte): registers[p->dest].f32 = registers[p->src].u8; break;
				case Cons(           Double , Unsigned | Byte): registers[p->dest].f64 = registers[p->src].u8; break;
				case Cons(Unsigned | Byte   , Unsigned | Word): registers[p->dest].u8 = registers[p->src].u16; break;
				case Cons(Unsigned | Word   , Unsigned | Word): registers[p->dest].u16 = registers[p->src].u16; break;
				case Cons(Unsigned | Dword  , Unsigned | Word): registers[p->dest].u32 = registers[p->src].u16; break;
				case Cons(Unsigned | Qword  , Unsigned | Word): registers[p->dest].u64 = registers[p->src].u16; break;
				case Cons(Signed   | Byte   , Unsigned | Word): registers[p->dest].s8 = registers[p->src].u16; break;
				case Cons(Signed   | Word   , Unsigned | Word): registers[p->dest].s16 = registers[p->src].u16; break;
				case Cons(Signed   | Dword  , Unsigned | Word): registers[p->dest].s32 = registers[p->src].u16; break;
				case Cons(Signed   | Qword  , Unsigned | Word): registers[p->dest].s64 = registers[p->src].u16; break;
				case Cons(           Float  , Unsigned | Word): registers[p->dest].f32 = registers[p->src].u16; break;
				case Cons(           Double , Unsigned | Word): registers[p->dest].f64 = registers[p->src].u16; break;
				case Cons(Unsigned | Byte   , Unsigned | Dword): registers[p->dest].u8 = registers[p->src].u32; break;
				case Cons(Unsigned | Word   , Unsigned | Dword): registers[p->dest].u16 = registers[p->src].u32; break;
				case Cons(Unsigned | Dword  , Unsigned | Dword): registers[p->dest].u32 = registers[p->src].u32; break;
				case Cons(Unsigned | Qword  , Unsigned | Dword): registers[p->dest].u64 = registers[p->src].u32; break;
				case Cons(Signed   | Byte   , Unsigned | Dword): registers[p->dest].s8 = registers[p->src].u32; break;
				case Cons(Signed   | Word   , Unsigned | Dword): registers[p->dest].s16 = registers[p->src].u32; break;
				case Cons(Signed   | Dword  , Unsigned | Dword): registers[p->dest].s32 = registers[p->src].u32; break;
				case Cons(Signed   | Qword  , Unsigned | Dword): registers[p->dest].s64 = registers[p->src].u32; break;
				case Cons(           Float  , Unsigned | Dword): registers[p->dest].f32 = registers[p->src].u32; break;
				case Cons(           Double , Unsigned | Dword): registers[p->dest].f64 = registers[p->src].u32; break;
				case Cons(Unsigned | Byte   , Unsigned | Qword): registers[p->dest].u8 = registers[p->src].u64; break;
				case Cons(Unsigned | Word   , Unsigned | Qword): registers[p->dest].u16 = registers[p->src].u64; break;
				case Cons(Unsigned | Dword  , Unsigned | Qword): registers[p->dest].u32 = registers[p->src].u64; break;
				case Cons(Unsigned | Qword  , Unsigned | Qword): registers[p->dest].u64 = registers[p->src].u64; break;
				case Cons(Signed   | Byte   , Unsigned | Qword): registers[p->dest].s8 = registers[p->src].u64; break;
				case Cons(Signed   | Word   , Unsigned | Qword): registers[p->dest].s16 = registers[p->src].u64; break;
				case Cons(Signed   | Dword  , Unsigned | Qword): registers[p->dest].s32 = registers[p->src].u64; break;
				case Cons(Signed   | Qword  , Unsigned | Qword): registers[p->dest].s64 = registers[p->src].u64; break;
				case Cons(           Float  , Unsigned | Qword): registers[p->dest].f32 = registers[p->src].u64; break;
				case Cons(           Double , Unsigned | Qword): registers[p->dest].f64 = registers[p->src].u64; break;
				case Cons(Unsigned | Byte   , Signed | Byte): registers[p->dest].u8 = registers[p->src].s8; break;
				case Cons(Unsigned | Word   , Signed | Byte): registers[p->dest].u16 = registers[p->src].s8; break;
				case Cons(Unsigned | Dword  , Signed | Byte): registers[p->dest].u32 = registers[p->src].s8; break;
				case Cons(Unsigned | Qword  , Signed | Byte): registers[p->dest].u64 = registers[p->src].s8; break;
				case Cons(Signed   | Byte   , Signed | Byte): registers[p->dest].s8 = registers[p->src].s8; break;
				case Cons(Signed   | Word   , Signed | Byte): registers[p->dest].s16 = registers[p->src].s8; break;
				case Cons(Signed   | Dword  , Signed | Byte): registers[p->dest].s32 = registers[p->src].s8; break;
				case Cons(Signed   | Qword  , Signed | Byte): registers[p->dest].s64 = registers[p->src].s8; break;
				case Cons(           Float  , Signed | Byte): registers[p->dest].f32 = registers[p->src].s8; break;
				case Cons(           Double , Signed | Byte): registers[p->dest].f64 = registers[p->src].s8; break;
				case Cons(Unsigned | Byte   , Signed | Word): registers[p->dest].u8 = registers[p->src].s16; break;
				case Cons(Unsigned | Word   , Signed | Word): registers[p->dest].u16 = registers[p->src].s16; break;
				case Cons(Unsigned | Dword  , Signed | Word): registers[p->dest].u32 = registers[p->src].s16; break;
				case Cons(Unsigned | Qword  , Signed | Word): registers[p->dest].u64 = registers[p->src].s16; break;
				case Cons(Signed   | Byte   , Signed | Word): registers[p->dest].s8 = registers[p->src].s16; break;
				case Cons(Signed   | Word   , Signed | Word): registers[p->dest].s16 = registers[p->src].s16; break;
				case Cons(Signed   | Dword  , Signed | Word): registers[p->dest].s32 = registers[p->src].s16; break;
				case Cons(Signed   | Qword  , Signed | Word): registers[p->dest].s64 = registers[p->src].s16; break;
				case Cons(           Float  , Signed | Word): registers[p->dest].f32 = registers[p->src].s16; break;
				case Cons(           Double , Signed | Word): registers[p->dest].f64 = registers[p->src].s16; break;
				case Cons(Unsigned | Byte   , Signed | Dword): registers[p->dest].u8 = registers[p->src].s32; break;
				case Cons(Unsigned | Word   , Signed | Dword): registers[p->dest].u16 = registers[p->src].s32; break;
				case Cons(Unsigned | Dword  , Signed | Dword): registers[p->dest].u32 = registers[p->src].s32; break;
				case Cons(Unsigned | Qword  , Signed | Dword): registers[p->dest].u64 = registers[p->src].s32; break;
				case Cons(Signed   | Byte   , Signed | Dword): registers[p->dest].s8 = registers[p->src].s32; break;
				case Cons(Signed   | Word   , Signed | Dword): registers[p->dest].s16 = registers[p->src].s32; break;
				case Cons(Signed   | Dword  , Signed | Dword): registers[p->dest].s32 = registers[p->src].s32; break;
				case Cons(Signed   | Qword  , Signed | Dword): registers[p->dest].s64 = registers[p->src].s32; break;
				case Cons(           Float  , Signed | Dword): registers[p->dest].f32 = registers[p->src].s32; break;
				case Cons(           Double , Signed | Dword): registers[p->dest].f64 = registers[p->src].s32; break;
				case Cons(Unsigned | Byte   , Signed | Qword): registers[p->dest].u8 = registers[p->src].s64; break;
				case Cons(Unsigned | Word   , Signed | Qword): registers[p->dest].u16 = registers[p->src].s64; break;
				case Cons(Unsigned | Dword  , Signed | Qword): registers[p->dest].u32 = registers[p->src].s64; break;
				case Cons(Unsigned | Qword  , Signed | Qword): registers[p->dest].u64 = registers[p->src].s64; break;
				case Cons(Signed   | Byte   , Signed | Qword): registers[p->dest].s8 = registers[p->src].s64; break;
				case Cons(Signed   | Word   , Signed | Qword): registers[p->dest].s16 = registers[p->src].s64; break;
				case Cons(Signed   | Dword  , Signed | Qword): registers[p->dest].s32 = registers[p->src].s64; break;
				case Cons(Signed   | Qword  , Signed | Qword): registers[p->dest].s64 = registers[p->src].s64; break;
				case Cons(           Float  , Signed | Qword): registers[p->dest].f32 = registers[p->src].s64; break;
				case Cons(           Double , Signed | Qword): registers[p->dest].f64 = registers[p->src].s64; break;
				case Cons(Unsigned | Byte   , Float): registers[p->dest].u8 = registers[p->src].f32; break;
				case Cons(Unsigned | Word   , Float): registers[p->dest].u16 = registers[p->src].f32; break;
				case Cons(Unsigned | Dword  , Float): registers[p->dest].u32 = registers[p->src].f32; break;
				case Cons(Unsigned | Qword  , Float): registers[p->dest].u64 = registers[p->src].f32; break;
				case Cons(Signed   | Byte   , Float): registers[p->dest].s8 = registers[p->src].f32; break;
				case Cons(Signed   | Word   , Float): registers[p->dest].s16 = registers[p->src].f32; break;
				case Cons(Signed   | Dword  , Float): registers[p->dest].s32 = registers[p->src].f32; break;
				case Cons(Signed   | Qword  , Float): registers[p->dest].s64 = registers[p->src].f32; break;
				case Cons(           Float  , Float): registers[p->dest].f32 = registers[p->src].f32; break;
				case Cons(           Double , Float): registers[p->dest].f64 = registers[p->src].f32; break;
				case Cons(Unsigned | Byte   , Double): registers[p->dest].u8 = registers[p->src].f64; break;
				case Cons(Unsigned | Word   , Double): registers[p->dest].u16 = registers[p->src].f64; break;
				case Cons(Unsigned | Dword  , Double): registers[p->dest].u32 = registers[p->src].f64; break;
				case Cons(Unsigned | Qword  , Double): registers[p->dest].u64 = registers[p->src].f64; break;
				case Cons(Signed   | Byte   , Double): registers[p->dest].s8 = registers[p->src].f64; break;
				case Cons(Signed   | Word   , Double): registers[p->dest].s16 = registers[p->src].f64; break;
				case Cons(Signed   | Dword  , Double): registers[p->dest].s32 = registers[p->src].f64; break;
				case Cons(Signed   | Qword  , Double): registers[p->dest].s64 = registers[p->src].f64; break;
				case Cons(           Float  , Double): registers[p->dest].f32 = registers[p->src].f64; break;
				case Cons(           Double , Double): registers[p->dest].f64 = registers[p->src].f64; break;
				default: perror("bad instruction arg type"); exit(-1); break; 
			}
#undef Cons
			break;
		}
		case Swi: {
			InstSwi* p = (InstSwi*)i;
			switch (p->code) {
			default:
				break;
			}
			break;
		}
		default: {
			perror("bad instruction"); exit(-1); break;
		}

		}
	}
Exit:
	return;

}

typedef struct {
	char *source;
	int  index;
	int  len;
} parse_ctx;

typedef struct {
	int start;
	int len;
} token_t;

char* strchrn(const char* s, int c) {
	char* p = strchr(s, c);
	if (p != NULL && *p == '\0') {
		return NULL;
	}
	else {
		return p;
	}
}

void skip_space(parse_ctx* ctx) {
	while (isspace(ctx->source[ctx->index])) { ctx->index++; }
}
void skip_sepspace(parse_ctx* ctx) {
	while (strchrn(" \t\v", ctx->source[ctx->index]) != NULL) { ctx->index++; }
}

void skip_sepspace1(parse_ctx* ctx) {
	if (strchrn(" \t\v", ctx->source[ctx->index]) == NULL) {
		perror("no sepspace");
		exit(-1);
	}
	while (strchrn(" \t\v", ctx->source[ctx->index]) != NULL) { ctx->index++; }
}

bool if_skip_sepspace1(parse_ctx* ctx) {
	int start = ctx->index;
	while (strchrn(" \t\v", ctx->source[ctx->index]) != NULL) { ctx->index++; }
	return ctx->index > 0;
}

void skip_crlf(parse_ctx* ctx) {
	skip_sepspace(ctx);
	if (strchrn("\r\n", ctx->source[ctx->index]) == NULL) {
		perror("no crlf");
		exit(-1);
	}
	while (strchrn("\r\n", ctx->source[ctx->index]) != NULL) { ctx->index++; }
}

char read_ch(parse_ctx* ctx) {
	return ctx->source[ctx->index++];
}

void char_is(parse_ctx* ctx, char ch) {
	if (ctx->source[ctx->index] == ch) {
		ctx->index++;
	}
	else {
		perror("no char");
		exit(-1);
	}
}

bool if_char_is(parse_ctx* ctx, char ch) {
	if (ctx->source[ctx->index] == ch) {
		ctx->index++;
		return true;
	} else {
		return false;
	}
}

bool peek_ident(parse_ctx* ctx, token_t *tok) {
	char* p = &ctx->source[ctx->index];
	if (!(isalpha(*p) || '_' == *p)) { return false; }
	while ((isalnum(*p) || '_' == *p)) { p++; }
	tok->start = ctx->index;
	tok->len = p - &ctx->source[ctx->index];

	return true;
}

void read_ident(parse_ctx* ctx, token_t *tok) {
	if (peek_ident(ctx,tok)) {
		ctx->index += tok->len;
	}
	else {
		perror("no ident"); 
		exit(-1);
	}
}

bool if_token_is(parse_ctx* ctx, token_t* tok, const char* str) {
	return (strncmp(&ctx->source[tok->start], str, tok->len) == 0);
}


struct LabelEntry {
	char* str;
	int len;
	struct Chain {
		struct Chain* next;
		uint32_t patchAddr;
		size_t   patchSize;
	} *chain;
	uint32_t address;
	struct LabelEntry* next;
};

struct LabelEntry* entries = NULL;

void parge_entries(void) {
	for (struct LabelEntry* entry = entries; entry != NULL; ) {
		struct LabelEntry* q = entry;
		if (entry->chain != NULL) {
			printf("label %.*s is referenced, but not defined.", entry->len, entry->str);
		}
		for (struct Chain* chain = entry->chain; chain != NULL; ) {
			struct Chain* p = chain;
			chain = chain->next;
			free(p);
		}
		entry = entry->next;
		free(q);
	}
}

static uint32_t ref_label(const parse_ctx* ctx, token_t* tok, uint8_t* memory, uint32_t patchAddr, size_t patchSize) {
	for (struct LabelEntry* entry = entries; entry != NULL; entry = entry->next) {
		if (entry->len == tok->len && strncmp(entry->str, &ctx->source[tok->start], tok->len) == 0) {
			if (entry->chain == NULL) {
				uint64_t u64v = entry->address;
				memcpy(&memory[patchAddr], &u64v, patchSize);
				return entry->address;
			}
			else {
				struct Chain* chain = calloc(1, sizeof(struct Chain));
				if (chain == NULL) { perror("no memory"); exit(0); }
				chain->next = entry->chain;
				chain->patchAddr = patchAddr;
				chain->patchSize = patchSize;
				entry->chain = chain;
				return 0;
			}
		}
	}
	{
		struct LabelEntry* entry = calloc(1, sizeof(struct LabelEntry));
		if (entry == NULL) { perror("no memory"); exit(0); }
		entry->str = &ctx->source[tok->start];
		entry->len = tok->len;
		entry->chain = NULL;
		entry->address = 0;
		entry->next = entries;
		entries = entry;

		struct Chain* chain = calloc(1, sizeof(struct Chain));
		if (chain == NULL) { perror("no memory"); exit(0); }
		chain->next = entry->chain;
		chain->patchAddr = patchAddr;
		chain->patchSize = patchSize;
		entry->chain = chain;
		return 0;
	}
}
static void regist_label(const parse_ctx* ctx, token_t* tok, uint8_t* memory, uint32_t address) {
	for (struct LabelEntry* entry = entries; entry != NULL; entry = entry->next) {
		if (entry->len == tok->len && strncmp(entry->str, &ctx->source[tok->start], tok->len) == 0) {
			if (entry->chain == NULL) {
				perror("duplicate label.");
				return;
			}
			else {
				uint64_t u64v = address;
				entry->address = address;
				for (struct Chain* chain = entry->chain; chain != NULL; ) {
					struct Chain* p = chain;
					memcpy(&memory[chain->patchAddr], &u64v, chain->patchSize);

					chain = chain->next;
					free(p);
				}
				entry->chain = NULL;
				return;
			}
		}
	}
	{
		struct LabelEntry* entry = calloc(1, sizeof(struct LabelEntry));
		if (entry == NULL) { perror("no memory"); exit(0); }
		entry->str = &ctx->source[tok->start];
		entry->len = tok->len;
		entry->chain = NULL;
		entry->address = address;
		entry->next = entries;
		entries = entry;

		return;
	}
}

#define mk_read_uX(x) \
uint##x##_t read_uint##x##(parse_ctx* ctx) { \
	char* ep = NULL; \
	uint64_t value = strtoull(&ctx->source[ctx->index], &ep, 0); \
	size_t len = (ep - &ctx->source[ctx->index]); \
	if (len == 0) { perror("no unsigned integer."); exit(-1); } \
	if (value > UINT##x##_MAX) { \
		printf("%.*s is too large for type u%d\n", len, &ctx->source[ctx->index],x); \
	} \
	ctx->index += len; \
	return (uint##x##_t)value; \
} \
bool if_read_uint##x##(parse_ctx* ctx, uint##x##_t* pvalue) { \
	char* ep = NULL; \
	uint64_t value = strtoull(&ctx->source[ctx->index], &ep, 0); \
	size_t len = (ep - &ctx->source[ctx->index]); \
	if (len == 0) { return false; } \
	if (value > UINT##x##_MAX) { \
		printf("%.*s is too large for type u%d\n", len, &ctx->source[ctx->index],x); \
	} \
	ctx->index += len; \
	*pvalue = (uint##x##_t)value; \
	return true; \
} \
uint##x##_t read_uint##x##_or_label(parse_ctx * ctx, uint8_t * memory, uint32_t waddr, size_t offset) { \
	token_t ident; \
	if (peek_ident(ctx, &ident)) { \
		read_ident(ctx, &ident); \
		return (uint##x##_t)ref_label(ctx, &ident, memory, waddr + offset, sizeof(uint##x##_t)); \
	} else { \
		char* ep = NULL; \
		uint64_t value = strtoull(&ctx->source[ctx->index], &ep, 0); \
		size_t len = (ep - &ctx->source[ctx->index]); \
		if (len == 0) { perror("no unsigned integer."); exit(-1); } \
		if (value > UINT##x##_MAX) { \
			printf("%.*s is too large for type u%d\n", len, &ctx->source[ctx->index],x); \
		} \
		ctx->index += len; \
		return (uint##x##_t)value; \
	} \
} \
bool if_read_uint##x##_or_label(parse_ctx * ctx, uint8_t * memory, uint32_t waddr, size_t offset, uint##x##_t* pvalue) { \
	token_t ident; \
	if (peek_ident(ctx, &ident)) { \
		read_ident(ctx, &ident); \
		*pvalue = (uint##x##_t)ref_label(ctx, &ident, memory, waddr + offset, sizeof(uint##x##_t)); \
		return true; \
	} else { \
		char* ep = NULL; \
		uint64_t value = strtoull(&ctx->source[ctx->index], &ep, 0); \
		size_t len = (ep - &ctx->source[ctx->index]); \
		if (len == 0) { return false; } \
		if (value > UINT##x##_MAX) { \
			printf("%.*s is too unsigned for type u%d\n", len, &ctx->source[ctx->index],x); \
		} \
		ctx->index += len; \
		*pvalue = (uint##x##_t)value; \
		return true; \
	} \
}

#define mk_read_sX(x) \
int##x##_t read_int##x##(parse_ctx* ctx) { \
	char* ep = NULL; \
	int64_t value = strtoll(&ctx->source[ctx->index], &ep, 0); \
	size_t len = (ep - &ctx->source[ctx->index]); \
	if (len == 0) { perror("no signed integer."); exit(-1); } \
	if (value < INT##x##_MIN || value > INT##x##_MAX) { \
		printf("%.*s is too large or too small for type u%d\n", len, &ctx->source[ctx->index],x); \
	} \
	ctx->index += len; \
	return (int##x##_t)value; \
} \
bool if_read_int##x##(parse_ctx* ctx, int##x##_t* pvalue) { \
	char* ep = NULL; \
	int64_t value = strtoll(&ctx->source[ctx->index], &ep, 0); \
	size_t len = (ep - &ctx->source[ctx->index]); \
	if (len == 0) { return false; } \
	if (value < INT##x##_MIN || value > INT##x##_MAX) { \
		printf("%.*s is too large or too small for type s%d\n", len, &ctx->source[ctx->index],x); \
	} \
	ctx->index += len; \
	*pvalue = (int##x##_t)value; \
	return true; \
} \
int##x##_t read_int##x##_or_label(parse_ctx * ctx, uint8_t * memory, uint32_t waddr, size_t offset) { \
	token_t ident; \
	if (peek_ident(ctx, &ident)) { \
		read_ident(ctx, &ident); \
		return (int##x##_t)ref_label(ctx, &ident, memory, waddr + offset, sizeof(int##x##_t)); \
	} else { \
		char* ep = NULL; \
		int64_t value = strtoll(&ctx->source[ctx->index], &ep, 0); \
		size_t len = (ep - &ctx->source[ctx->index]); \
		if (len == 0) { perror("no signed integer."); exit(-1); } \
		if (value < INT##x##_MIN || value > INT##x##_MAX) { \
			printf("%.*s is too large or too small for type s%d\n", len, &ctx->source[ctx->index],x); \
		} \
		ctx->index += len; \
		return (int##x##_t)value; \
	} \
} \
bool if_read_int##x##_or_label(parse_ctx * ctx, uint8_t * memory, uint32_t waddr, size_t offset, int##x##_t* pvalue) { \
	token_t ident; \
	if (peek_ident(ctx, &ident)) { \
		read_ident(ctx, &ident); \
		*pvalue = (int##x##_t)ref_label(ctx, &ident, memory, waddr + offset, sizeof(int##x##_t)); \
		return true; \
	} else { \
		char* ep = NULL; \
		int64_t value = strtoll(&ctx->source[ctx->index], &ep, 0); \
		size_t len = (ep - &ctx->source[ctx->index]); \
		if (len == 0) { return false; } \
		if (value < INT##x##_MIN || value > INT##x##_MAX) { \
			printf("%.*s is too large or too small for type s%d\n", len, &ctx->source[ctx->index],x); \
		} \
		ctx->index += len; \
		*pvalue = (int##x##_t)value; \
		return true; \
	} \
}

mk_read_uX(8)
mk_read_uX(16)
mk_read_uX(32)
mk_read_uX(64)
mk_read_sX(8)
mk_read_sX(16)
mk_read_sX(32)
mk_read_sX(64)

#undef mk_read_uX
#undef mk_read_sX

float_t read_f32(parse_ctx* ctx, uint8_t * memory, uint32_t waddr, size_t offset) {
	char* ep = NULL;
	float value = strtof(&ctx->source[ctx->index], &ep);
	size_t len = (ep - &ctx->source[ctx->index]);
	if (len == 0) { perror("no signed integer."); exit(-1); }
	ctx->index += len;
	return (float_t)value;
}

bool if_read_f32(parse_ctx* ctx, uint8_t* memory, uint32_t waddr, size_t offset, float_t* pvalue) {
	char* ep = NULL;
	float value = strtof(&ctx->source[ctx->index], &ep);
	size_t len = (ep - &ctx->source[ctx->index]);
	if (len == 0) { return false; }
	ctx->index += len;
	*pvalue = (float_t)value;
	return true;
}

double_t read_f64(parse_ctx* ctx, uint8_t* memory, uint32_t waddr, size_t offset) {
	char* ep = NULL;
	double value = strtof(&ctx->source[ctx->index], &ep);
	size_t len = (ep - &ctx->source[ctx->index]);
	if (len == 0) { perror("no signed integer."); exit(-1); }
	ctx->index += len;
	return (double_t)value;
}

bool if_read_f64(parse_ctx* ctx, uint8_t* memory, uint32_t waddr, size_t offset, double_t* pvalue) {
	char* ep = NULL;
	double value = strtof(&ctx->source[ctx->index], &ep);
	size_t len = (ep - &ctx->source[ctx->index]);
	if (len == 0) { return false; }
	ctx->index += len;
	*pvalue = (double_t)value;
	return true;
}

Value read_value(parse_ctx* ctx, uint8_t ty, uint8_t *memory, uint32_t waddr, size_t offset) {
	switch (ty) {
	case Unsigned | Byte   : return (Value) { .u8  = read_uint8_or_label(ctx, memory,waddr,offset) };
	case Unsigned | Word   : return (Value) { .u16 = read_uint16_or_label(ctx, memory, waddr, offset) };
	case Unsigned | Dword  : return (Value) { .u32 = read_uint32_or_label(ctx, memory, waddr, offset) };
	case Unsigned | Qword  : return (Value) { .u64 = read_uint64_or_label(ctx, memory, waddr, offset) };
	case   Signed | Byte   : return (Value) { .s8  = read_int8_or_label(ctx, memory, waddr, offset) };
	case   Signed | Word   : return (Value) { .s8  = read_int16_or_label(ctx, memory, waddr, offset) };
	case   Signed | Dword  : return (Value) { .s8  = read_int32_or_label(ctx, memory, waddr, offset) };
	case   Signed | Qword  : return (Value) { .s8  = read_int64_or_label(ctx, memory, waddr, offset) };
	case            Float  : return (Value) { .f32 = read_f32(ctx, memory, waddr, offset) };
	case            Double : return (Value) { .f64 = read_f64(ctx, memory, waddr, offset) };
	default                : perror("no type."); exit(-1);
	}
}

uint8_t read_type(parse_ctx* ctx) {
	token_t tok;
	read_ident(ctx, &tok);
	if (if_token_is(ctx, &tok, "u8")) { return Unsigned | Byte; }
	if (if_token_is(ctx, &tok, "u16")) { return Unsigned | Word; }
	if (if_token_is(ctx, &tok, "u32")) { return Unsigned | Dword; }
	if (if_token_is(ctx, &tok, "u64")) { return Unsigned | Qword; }
	if (if_token_is(ctx, &tok, "s8")) { return Signed | Byte; }
	if (if_token_is(ctx, &tok, "s16")) { return Signed | Word; }
	if (if_token_is(ctx, &tok, "s32")) { return Signed | Dword; }
	if (if_token_is(ctx, &tok, "s64")) { return Signed | Qword; }
	if (if_token_is(ctx, &tok, "f32")) { return Float; }
	if (if_token_is(ctx, &tok, "f64")) { return Double; }
	perror("no type.");
	exit(-1);
	return 0;
}

uint8_t read_normal_reg(parse_ctx* ctx) {
	char_is(ctx, '%');
	char_is(ctx, 'r');
	char* ep = NULL;
	unsigned long value = strtoul(&ctx->source[ctx->index], &ep, 10);
	size_t len = (ep - &ctx->source[ctx->index]);
	if (len == 0 || len > 2 || value >= 32) {
		perror("no register");
		exit(-1);
		return 0;
	}
	ctx->index += len;
	return (uint8_t)value;
}

uint8_t if_read_normal_reg(parse_ctx* ctx, uint8_t *v) {
	int idx = ctx->index;
	if (if_char_is(ctx, '%') == false) {
		goto fail;
	}
	if (if_char_is(ctx, 'r') == false) {
		goto fail;
	}
	char* ep = NULL;
	unsigned long value = strtoul(&ctx->source[ctx->index], &ep, 10);
	size_t len = (ep - &ctx->source[ctx->index]);
	if (len == 0 || len > 2 || value >= 32) {
		goto fail;
	}
	ctx->index += len;
	*v = (uint8_t)value;
	return true;
fail:
	ctx->index = idx;
	*v = 0;
	return false;
}


void skip_sepcomma(parse_ctx* ctx) {
	skip_sepspace(ctx);
	char_is(ctx, ',');
	skip_sepspace(ctx);
}

bool if_skip_sepcomma(parse_ctx* ctx) {
	skip_sepspace(ctx);
	if (if_char_is(ctx, ',')) {
		skip_sepspace(ctx);
		return true;
	}
	return false;
}

bool parser(const char* filename) {
	struct _stat s;
	if (_stat(filename, &s) != 0) { return false; }
	char* source = (char*)calloc(s.st_size + 1, sizeof(char));
	if (source == NULL) { return false; }
	FILE* fp;
	if (fopen_s(&fp, filename, "rb") != 0) { return false; }
	if (fread(source, s.st_size, 1, fp) != 1) {
		fclose(fp);
		free(source);
		return false;
	}
	fclose(fp);

	parse_ctx ctx = { source, 0, s.st_size };

	uint32_t code_addr = 0;
	uint32_t data_addr = 0;
	uint32_t rdata_addr = 0;
	uint32_t memory_size = 0;

	uint32_t waddr = 0;
	memory = NULL;

	enum {
		S_CODE,
		S_DATA,
		S_RDATA,
	} segmeng_mode = S_CODE;
	for (;;) {
	line_head:
		skip_space(&ctx);
		if (if_char_is(&ctx, '\0')) { break; }
		if (if_char_is(&ctx, '.')) {
			token_t tok;
			read_ident(&ctx, &tok);
			if (if_token_is(&ctx, &tok, "code")) {
				uint32_t n;
				if (if_skip_sepspace1(&ctx) && if_read_uint32(&ctx, &n)) { code_addr = n; }
				skip_crlf(&ctx);
				segmeng_mode = S_CODE;
				waddr = code_addr;
				goto line_head;
			}
			if (if_token_is(&ctx, &tok, "data")) {
				uint32_t n;
				if (if_skip_sepspace1(&ctx) && if_read_uint32(&ctx, &n)) { data_addr = n; }
				skip_crlf(&ctx);
				segmeng_mode = S_DATA;
				waddr = data_addr;
				goto line_head;
			}
			if (if_token_is(&ctx, &tok, "rdata")) {
				uint32_t n;
				if (if_skip_sepspace1(&ctx) && if_read_uint32(&ctx, &n)) { rdata_addr = n; }
				skip_crlf(&ctx);
				segmeng_mode = S_RDATA;
				waddr = rdata_addr;
				goto line_head;
			}
			if (if_token_is(&ctx, &tok, "memory")) {
				skip_sepspace1(&ctx);
				uint32_t n = read_uint32(&ctx);
				uint8_t *new_memory = (uint8_t * )calloc(n, sizeof(uint8_t));
				if (new_memory == NULL) {
					perror("cannot create memory");
					exit(-1);
					return false;
				}
				if (memory != NULL) {
					memcpy(new_memory, memory, (memory_size < n) ? memory_size : n);
					free(memory);
				}
				memory = new_memory;
				memory_size = n;

				skip_crlf(&ctx);
				goto line_head;
			}
#define read_values(ty,func) \
			for (;;) { \
				ty n = func(&ctx, memory, waddr, 0); \
				memcpy(&memory[waddr], &n, sizeof(n)); \
				waddr += sizeof(n); \
				if (if_skip_sepcomma(&ctx) == false) { \
					break; \
				} \
			}

			if (if_token_is(&ctx, &tok, "u8")) {
				skip_sepspace1(&ctx);
				read_values(uint8_t, read_uint8_or_label);
				skip_crlf(&ctx);
				goto line_head;
			}
			if (if_token_is(&ctx, &tok, "u16")) {
				skip_sepspace1(&ctx);
				read_values(uint16_t, read_uint16_or_label);
				skip_crlf(&ctx);
				goto line_head;
			}
			if (if_token_is(&ctx, &tok, "u32")) {
				skip_sepspace1(&ctx);
				read_values(uint32_t, read_uint32_or_label);
				skip_crlf(&ctx);
				goto line_head;
			}
			if (if_token_is(&ctx, &tok, "u64")) {
				skip_sepspace1(&ctx);
				read_values(uint64_t, read_uint64_or_label);
				skip_crlf(&ctx);
				goto line_head;
			}
			if (if_token_is(&ctx, &tok, "s8")) {
				skip_sepspace1(&ctx);
				read_values(int8_t, read_int8_or_label);
				skip_crlf(&ctx);
				goto line_head;
			}
			if (if_token_is(&ctx, &tok, "s16")) {
				skip_sepspace1(&ctx);
				read_values(int16_t, read_int16_or_label);
				skip_crlf(&ctx);
				goto line_head;
			}
			if (if_token_is(&ctx, &tok, "s32")) {
				skip_sepspace1(&ctx);
				read_values(int32_t, read_int32_or_label);
				skip_crlf(&ctx);
				goto line_head;
			}
			if (if_token_is(&ctx, &tok, "s64")) {
				skip_sepspace1(&ctx);
				read_values(int64_t, read_int64_or_label);
				skip_crlf(&ctx);
				goto line_head;
			}
			if (if_token_is(&ctx, &tok, "f32")) {
				skip_sepspace1(&ctx);
				read_values(float, read_f32);
				skip_crlf(&ctx);
				goto line_head;
			}
			if (if_token_is(&ctx, &tok, "f64")) {
				skip_sepspace1(&ctx);
				read_values(double, read_f64);
				skip_crlf(&ctx);
				goto line_head;
			}
			perror("unknown pramga");
			exit(-1);
		}
		{
			token_t tok;
			read_ident(&ctx, &tok);

			if (if_token_is(&ctx, &tok, "halt")) {
				skip_crlf(&ctx);
				if (memory == NULL) { perror("memory is none"); exit(-1); }
				waddr = WriteInst(memory, waddr, halt());
				goto line_head;
			}
			if (if_token_is(&ctx, &tok, "nop")) {
				skip_crlf(&ctx);
				if (memory == NULL) { perror("memory is none"); exit(-1); }
				waddr = WriteInst(memory, waddr, nop());
				goto line_head;
			}
			if (if_token_is(&ctx, &tok, "mov")) {
				skip_sepspace1(&ctx);
				uint8_t dset = read_normal_reg(&ctx);
				skip_sepcomma(&ctx);
				uint8_t lhs = read_normal_reg(&ctx);
				skip_crlf(&ctx);
				if (memory == NULL) { perror("memory is none"); exit(-1); }
				waddr = WriteInst(memory, waddr, mov(Reg(dset), Reg(lhs)));
				goto line_head;
			}
			if (if_token_is(&ctx, &tok, "movi")) {
				char_is(&ctx, '.');
				uint8_t ty = read_type(&ctx);
				skip_sepspace1(&ctx);
				uint8_t dset = read_normal_reg(&ctx);
				skip_sepcomma(&ctx);
				Value value = read_value(&ctx, ty, memory, waddr, offsetof(InstMovi, value));
				skip_crlf(&ctx);
				if (memory == NULL) { perror("memory is none"); exit(-1); }
				waddr = WriteInst(memory, waddr, movi(ty, Reg(dset), value));
				goto line_head;
			}
			if (if_token_is(&ctx, &tok, "load")) {
				char_is(&ctx, '.');
				uint8_t ty = read_type(&ctx);
				skip_sepspace1(&ctx);
				uint8_t dset = read_normal_reg(&ctx);
				skip_sepcomma(&ctx);
				uint8_t base = 0;
				int32_t offset = 0;
				if (if_read_normal_reg(&ctx, &base)) {
					if (if_skip_sepcomma(&ctx) == true) {
						offset = read_int32_or_label(&ctx, memory, waddr, offsetof(InstLoadStore,offset));
					}
				}
				else {
					offset = read_int32_or_label(&ctx, memory, waddr, offsetof(InstLoadStore, offset));
				}
				skip_crlf(&ctx);
				if (memory == NULL) { perror("memory is none"); exit(-1); }
				waddr = WriteInst(memory, waddr, load(ty, Reg(dset), Reg(base), Const(offset)));
				goto line_head;
			}

			if (if_token_is(&ctx, &tok, "store")) {
				char_is(&ctx, '.');
				uint8_t ty = read_type(&ctx);
				skip_sepspace1(&ctx);
				uint8_t base = 0;
				int32_t offset = 0;
				if (if_read_normal_reg(&ctx, &base)) {
					if (if_skip_sepcomma(&ctx) == true) {
						offset = read_int32_or_label(&ctx, memory, waddr, offsetof(InstLoadStore, offset));
					}
				}
				else {
					offset = read_int32_or_label(&ctx, memory, waddr, offsetof(InstLoadStore, offset));
				}
				skip_sepcomma(&ctx);
				uint8_t to = read_normal_reg(&ctx);
				skip_crlf(&ctx);
				if (memory == NULL) { perror("memory is none"); exit(-1); }
				waddr = WriteInst(memory, waddr, store(ty, Reg(base), Const(offset), Reg(to)));
				goto line_head;
			}

#define binop(x) \
			if (if_token_is(&ctx, &tok, #x)) { \
				char_is(&ctx, '.'); \
				uint8_t ty = read_type(&ctx); \
				skip_sepspace1(&ctx); \
				uint8_t dest = read_normal_reg(&ctx); \
				skip_sepcomma(&ctx); \
				uint8_t lhs = read_normal_reg(&ctx); \
				skip_sepcomma(&ctx); \
				uint8_t rhs = read_normal_reg(&ctx); \
				skip_crlf(&ctx); \
				if (memory == NULL) { perror("memory is none"); exit(-1); } \
				waddr = WriteInst(memory, waddr, x(ty, Reg(dest), Reg(lhs), Reg(rhs))); \
				goto line_head; \
			}
#define binopi(x) \
			if (if_token_is(&ctx, &tok, #x)) { \
				char_is(&ctx, '.'); \
				uint8_t ty = read_type(&ctx); \
				skip_sepspace1(&ctx); \
				uint8_t dest = read_normal_reg(&ctx); \
				skip_sepcomma(&ctx); \
				uint8_t lhs = read_normal_reg(&ctx); \
				skip_sepcomma(&ctx); \
				Value value = read_value(&ctx, ty, memory, waddr, offsetof(InstMovi, value)); \
				skip_crlf(&ctx); \
				if (memory == NULL) { perror("memory is none"); exit(-1); } \
				waddr = WriteInst(memory, waddr, x(ty, Reg(dest), Reg(lhs), value)); \
				goto line_head; \
			}
#define unaryop(x) \
			if (if_token_is(&ctx, &tok, #x)) { \
				char_is(&ctx, '.'); \
				uint8_t ty = read_type(&ctx); \
				skip_sepspace1(&ctx); \
				uint8_t dest = read_normal_reg(&ctx); \
				skip_sepcomma(&ctx); \
				uint8_t lhs = read_normal_reg(&ctx); \
				skip_crlf(&ctx); \
				if (memory == NULL) { perror("memory is none"); exit(-1); } \
				waddr = WriteInst(memory, waddr, x(ty, Reg(dest), Reg(lhs))); \
				goto line_head; \
			}

			binop(add);
			binopi(addi);
			binop(sub);
			binopi(subi);
			binop(mul);
			binopi(muli);
			binop(div);
			binopi(divi);
			binop(mod);
			binopi(modi);

			binop(and);
			binopi(andi);
			binop(or );
			binopi(ori);
			binop(xor);
			binopi(xori);
			unaryop(not);

			unaryop(neg);

			binop(shl);
			binopi(shli);
			binop(shr);
			binopi(shri);
			binop(sal);
			binopi(sali);
			binop(sar);
			binopi(sari);

#undef binop
#undef binopi

			if (if_token_is(&ctx, &tok, "cmp")) {
				char_is(&ctx, '.');
				uint8_t ty = read_type(&ctx);
				skip_sepspace1(&ctx);
				uint8_t lhs = read_normal_reg(&ctx);
				skip_sepcomma(&ctx);
				uint8_t rhs = read_normal_reg(&ctx);
				skip_crlf(&ctx);
				if (memory == NULL) { perror("memory is none"); exit(-1); }
				waddr = WriteInst(memory, waddr, cmp(ty, Reg(lhs), Reg(rhs)));
				goto line_head;
			}

			if (if_token_is(&ctx, &tok, "cmpi")) {
				char_is(&ctx, '.');
				uint8_t ty = read_type(&ctx);
				skip_sepspace1(&ctx);
				uint8_t lhs = read_normal_reg(&ctx);
				skip_sepcomma(&ctx);
				Value value = read_value(&ctx, ty, memory, waddr, offsetof(InstMovi, value));
				skip_crlf(&ctx);
				if (memory == NULL) { perror("memory is none"); exit(-1); }
				waddr = WriteInst(memory, waddr, cmpi(ty, Reg(lhs), value));
				goto line_head;
			}

#define jmpop(x) \
			if (if_token_is(&ctx, &tok, #x)) { \
				skip_sepspace1(&ctx); \
				uint8_t base; \
				uint32_t offset; \
				if (if_read_normal_reg(&ctx, &base)) { \
					if (if_skip_sepcomma(&ctx) == true) { \
						offset = read_int32_or_label(&ctx, memory, waddr, offsetof(InstJmp,offset)); \
					} \
				} \
				else { \
					offset = read_int32_or_label(&ctx, memory, waddr, offsetof(InstJmp,offset));; \
				} \
				skip_crlf(&ctx); \
				if (memory == NULL) { perror("memory is none"); exit(-1); } \
				waddr = WriteInst(memory, waddr, x(Reg(base), Const(offset))); \
				goto line_head; \
			}

			if (if_token_is(&ctx, &tok, "jmp")) {
				skip_sepspace1(&ctx);
				uint8_t base;
				uint32_t offset;
				if (if_read_normal_reg(&ctx, &base)) {
					if (if_skip_sepcomma(&ctx) == true) {
						offset = read_int32_or_label(&ctx, memory, waddr, offsetof(InstJmp,offset));
					}
				}
				else {
					offset = read_int32_or_label(&ctx, memory, waddr, offsetof(InstJmp,offset));;
				}
				skip_crlf(&ctx);
				if (memory == NULL) { perror("memory is none"); exit(-1); }
				waddr = WriteInst(memory, waddr, jmp(Reg(base), Const(offset)));
				goto line_head;
			}
			//jmpop(jmp);
			jmpop(jeq);
			jmpop(jne);
			jmpop(jcs);
			jmpop(jcc);
			jmpop(jmi);
			jmpop(jpl);
			jmpop(jvs);
			jmpop(jvc);
			jmpop(jhi);
			jmpop(jls);
			jmpop(jge);
			jmpop(jlt);
			jmpop(jgt);
			jmpop(jle);
#undef jmpop
			if (if_token_is(&ctx, &tok, "push")) {
				skip_sepspace1(&ctx);
				uint8_t dset = read_normal_reg(&ctx);
				skip_crlf(&ctx);
				if (memory == NULL) { perror("memory is none"); exit(-1); }
				waddr = WriteInst(memory, waddr, push(Reg(dset)));
				goto line_head;
			}
			if (if_token_is(&ctx, &tok, "pop")) {
				skip_sepspace1(&ctx);
				uint8_t dset = read_normal_reg(&ctx);
				skip_crlf(&ctx);
				if (memory == NULL) { perror("memory is none"); exit(-1); }
				waddr = WriteInst(memory, waddr, pop(Reg(dset)));
				goto line_head;
			}
			if (if_token_is(&ctx, &tok, "ret")) {
				skip_crlf(&ctx);
				if (memory == NULL) { perror("memory is none"); exit(-1); }
				waddr = WriteInst(memory, waddr, ret());
				goto line_head;
			}
			if (if_token_is(&ctx, &tok, "cast")) {
				skip_crlf(&ctx);
				if (memory == NULL) { perror("memory is none"); exit(-1); }
				skip_sepspace1(&ctx);
				uint8_t td = read_type(&ctx);
				skip_sepcomma(&ctx);
				uint8_t dest = read_normal_reg(&ctx);
				skip_sepcomma(&ctx);
				uint8_t ts = read_type(&ctx);
				skip_sepcomma(&ctx);
				uint8_t src = read_normal_reg(&ctx);
				skip_crlf(&ctx);

				waddr = WriteInst(memory, waddr, cast(td, Reg(dest), ts, Reg(src)));
				goto line_head;
			}
			if (if_token_is(&ctx, &tok, "swi")) {
				uint32_t rhs = read_uint32(&ctx);
				skip_crlf(&ctx);
				if (memory == NULL) { perror("memory is none"); exit(-1); }
				waddr = WriteInst(memory, waddr, swi(Const(rhs)));
				goto line_head;
			}
			{
				skip_sepspace(&ctx);
				if (if_char_is(&ctx, ':')) {
					regist_label(&ctx, &tok, memory, waddr);
					goto line_head;
				}
			}
		}
		perror("unknown instruction");
		exit(-1);
	}
	parge_entries();
	return true;
}

int main(void) {

	if (parser("example.asm") == false) {
		return -1;
	}

	pc = 0x0010;
	sp = bp = 1024 * 64;
	flags = None;

	run();

	free(memory);
	return 0;
}

