/*
 *   Redistribution and use in source and binary forms, with or without
 *   modification, are permitted provided that the following conditions are met:
 *
 *   * Redistributions of source code must retain the above copyright
 *     notice, this list of conditions and the following disclaimer.
 *
 *   * Redistributions in binary form must reproduce the above copyright
 *     notice, this list of conditions and the following disclaimer in
 *     the documentation and/or other materials provided with the
 *     distribution.
 *
 *   * Neither the name of the copyright holders nor the names of
 *     contributors may be used to endorse or promote products derived
 *     from this software without specific prior written permission.
 *
 *   THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 *   AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 *   IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 *   ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE
 *   LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
 *   CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
 *   SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 *   INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
 *   CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 *   ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
 *   POSSIBILITY OF SUCH DAMAGE.
 */

/**
 * @file
 * @brief	AVR環境でのコンテキストとコンテキスト操作
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	作成
 * @date	2010/08/15 16:11:02	機種依存部分として分離
 */


#ifndef __CONTEXT_H__
#define __CONTEXT_H__

#include "./arch.h"
#include "../../src/type.h"

/**
 * @def		NAKED
 * @brief	関数のプロローグとエピローグを作らないようにする関数修飾子
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	作成
 */
#define NAKED __attribute__ ((naked)) 

/**
 * @def		NAKED_ISR
 * @brief	割り込み処理関数かつプロローグとエピローグを作らないようにする関数修飾子
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	作成
 */
#define NAKED_ISR __attribute__ ( ( signal, naked ) ) 

/**
 * @def		SET_KERNEL_STACKPOINTER
 * @brief	カーネルスタックを現在のスタックポインタに設定する
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	作成
 */
#define SET_KERNEL_STACKPOINTER()				\
asm volatile (									\
	"ldi	r28,		lo8(__stack)	\n\t"	\
	"ldi	r29,		hi8(__stack)	\n\t"	\
	"out	__SP_H__,	r29				\n\t"	\
	"out	__SP_L__,	r28				\n\t"	\
);

/**
 * @def			SAVE_CONTEXT
 * @brief		現在のコンテキストを待避
 * @attention	コンテキストはスタックに待避され、スタックポインタは currentTCB->stack_pointer に格納される。そのため、currentTCBに適切な値が設定済みであることが求められる。
 * @author		Kazuya Fukuhara
 * @date		2010/01/07 14:08:58	作成
 */
#define SAVE_CONTEXT()						\
asm volatile (								\
	"push	r0						\n\t" 	\
	"in		r0,	__SREG__ 			\n\t" 	\
	"cli							\n\t" 	\
	"push 	r0						\n\t" 	\
	"push 	r1						\n\t" 	\
	"clr 	r1						\n\t" 	\
	"push 	r2						\n\t" 	\
	"push 	r3						\n\t" 	\
	"push 	r4						\n\t" 	\
	"push 	r5						\n\t" 	\
	"push 	r6						\n\t" 	\
	"push 	r7						\n\t" 	\
	"push 	r8						\n\t" 	\
	"push 	r9						\n\t" 	\
	"push 	r10						\n\t" 	\
	"push 	r11						\n\t" 	\
	"push 	r12						\n\t" 	\
	"push 	r13						\n\t" 	\
	"push 	r14						\n\t" 	\
	"push 	r15						\n\t" 	\
	"push 	r16						\n\t" 	\
	"push 	r17						\n\t" 	\
	"push 	r18						\n\t" 	\
	"push 	r19						\n\t" 	\
	"push 	r20						\n\t" 	\
	"push 	r21						\n\t" 	\
	"push 	r22						\n\t" 	\
	"push 	r23						\n\t" 	\
	"push 	r24						\n\t" 	\
	"push 	r25						\n\t" 	\
	"push 	r26						\n\t" 	\
	"push 	r27						\n\t" 	\
	"push 	r28						\n\t" 	\
	"push 	r29						\n\t" 	\
	"push 	r30						\n\t" 	\
	"push 	r31						\n\t" 	\
	"lds 	r26, currentTCB 		\n\t" 	\
	"lds 	r27, currentTCB + 1 	\n\t" 	\
	"in 	 r0, __SP_L__			\n\t" 	\
	"st 	 x+, r0					\n\t" 	\
	"in 	 r0, __SP_H__			\n\t" 	\
	"st 	 x+, r0					\n\t" 	\
);

/**
 * @def			RESTORE_CONTEXT
 * @brief		コンテキストを復帰する
 * @attention	復帰に使うスタックポインタは currentTCB->stack_pointer から読み出される。そのため、currentTCBに適切な値が設定済みであることが求められる。
 * @author		Kazuya Fukuhara
 * @date		2010/01/07 14:08:58	作成
 */
#define RESTORE_CONTEXT()						\
asm volatile (									\
	"lds 	r26		, currentTCB		\n\t" 	\
	"lds 	r27		, currentTCB + 1 	\n\t" 	\
	"ld 	r28		, x+				\n\t" 	\
	"out 	__SP_L__, r28			 	\n\t" 	\
	"ld 	r29		, x+ 				\n\t" 	\
	"out 	__SP_H__, r29 				\n\t" 	\
	"pop 	r31 						\n\t" 	\
	"pop 	r30 						\n\t" 	\
	"pop 	r29 						\n\t" 	\
	"pop 	r28 						\n\t" 	\
	"pop 	r27 						\n\t" 	\
	"pop 	r26 						\n\t" 	\
	"pop 	r25 						\n\t" 	\
	"pop 	r24 						\n\t" 	\
	"pop 	r23 						\n\t" 	\
	"pop 	r22 						\n\t" 	\
	"pop 	r21 						\n\t" 	\
	"pop 	r20 						\n\t" 	\
	"pop 	r19 						\n\t" 	\
	"pop 	r18 						\n\t" 	\
	"pop 	r17 						\n\t" 	\
	"pop 	r16 						\n\t" 	\
	"pop 	r15 						\n\t" 	\
	"pop 	r14 						\n\t" 	\
	"pop 	r13 						\n\t" 	\
	"pop 	r12 						\n\t" 	\
	"pop 	r11 						\n\t" 	\
	"pop 	r10 						\n\t" 	\
	"pop 	r9	 						\n\t" 	\
	"pop 	r8	 						\n\t" 	\
	"pop 	r7	 						\n\t" 	\
	"pop 	r6	 						\n\t" 	\
	"pop 	r5	 						\n\t" 	\
	"pop 	r4	 						\n\t" 	\
	"pop 	r3	 						\n\t" 	\
	"pop 	r2	 						\n\t" 	\
	"pop 	r1 							\n\t" 	\
	"pop 	r0							\n\t" 	\
	"out 	__SREG__,	r0				\n\t" 	\
	"pop 	r0							\n\t" 	\
);

/**
 * @def		RETURN_FROM_INTERRUPT
 * @brief	割り込み処理から脱出する
 * @author	Kazuya Fukuhara
 * @date	2010/08/15 16:11:02	作成
 */
#define RETURN_FROM_INTERRUPT() asm volatile ( "reti" )

/**
 * @typedef	enum CONTEXT_MEMBER_T CONTEXT_MEMBER_T;
 * @enum	CONTEXT_MEMBER_T
 * @brief	コンテキスト中のレジスタメンバアクセス用のインデックスを示す定数
 * @author	help
 * @date	2010/01/07 14:08:58	作成
 */
typedef enum CONTEXT_MEMBER_T {
	R31, R30, 
	R29, R28, R27, R26, R25, R24, R23, R22, R21, R20, 
	R19, R18, R17, R16, R15, R14, R13, R12, R11, R10, 
	R9 , R8 , R7 , R6 , R5 , R4 , R3 , R2 , R1 , 
	STATUSREG, 
	R0, 
	REG_NUM
} CONTEXT_MEMBER_T;

/**
 * @typedef	struct context_t context_t;
 * @struct	context_t
 * @brief	スタックに保存される実行コンテキスト
 * @note	AVRでの戻り先アドレスは16bit値だが、エンディアンが反転している
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	作成
 */
typedef struct context_t {
	uint8_t registers[REG_NUM];		/**< 汎用レジスタとステータスレジスタ */
	uint8_t return_address_high;	/**< 戻り先アドレスの上位8bit */
	uint8_t return_address_low;		/**< 戻り先アドレスの下位8bit */
} context_t;

/**
 * @def   		SetReturnAddressToContext
 * @brief		コンテキストの戻り先アドレスを設定する
 * @param[in]	context 対象コンテキスト
 * @param[in]	address 設定する戻り先アドレス
 * @author		Kazuya Fukuhara
 * @date		2010/01/07 14:08:58	作成
 */
#define SetReturnAddressToContext(context, arg) { \
	context_t* ctx = context; \
	union { ptr_t word; uint8_t byte[2]; } u; \
	u.word = arg; \
	ctx->return_address_high = u.byte[1]; \
	ctx->return_address_low  = u.byte[0]; \
}

/**
 * @def   		GetContext
 * @brief		タスクコントロールブロックから実行コンテキストを取得
 * @param[in]	tcb 対象タスクコントロールブロック
 * @return		実行コンテキストを示すポインタ値
 * @author		Kazuya Fukuhara
 * @date		2010/01/07 14:08:58	作成
 */
#define GetContext(tcb) ((context_t*)((tcb)->stack_pointer+1))

/**
 * @def			GetArgWord
 * @brief		コンテキストからポインタ引数を取り出す
 * @param[in]	context 対象コンテキスト
 * @return		引数として渡されたポインタ値
 * @author		Kazuya Fukuhara
 * @date		2010/01/07 14:08:58	作成
 */
#define GetArgPtr(context) (((context)->registers[R25] << 8) | ((context)->registers[R24]))

/**
 * @def			SetTaskArg
 * @brief		コンテキストにタスク開始時の引数を設定
 * @param[in]	context 対象コンテキスト
 * @param[in]	arg     設定する引数
 * @author		Kazuya Fukuhara
 * @date		2010/01/07 14:08:58	作成
 */
#define SetTaskArg(context, arg) {\
	context_t* ctx = context; \
	ptr_t param = arg; \
	uint8_t *p = (uint8_t*)&param; \
	ctx->registers[R25] = p[0]; \
	ctx->registers[R24] = p[1]; \
}

#endif

