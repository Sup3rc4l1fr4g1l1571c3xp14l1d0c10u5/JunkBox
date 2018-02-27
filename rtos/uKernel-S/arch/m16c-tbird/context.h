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
 * @brief	M16C T-Bird環境でのコンテキストとコンテキスト操作 
 * @author	Kazuya Fukuhara
 * @date	2010/09/04 19:46:15	作成
 */

#ifndef __CONTEXT__
#define __CONTEXT__

#include "./arch.h"
#include "../../src/type.h"


#ifdef UK_DEPRECATED
/**
 * @def		NAKED
 * @brief	関数のプロローグとエピローグを作らないようにする関数修飾子
 * @author	Kazuya Fukuhara
 * @date	2010/09/04 19:46:15	作成
 * @warning	廃止予定
 */
#define NAKED

/**
 * @def		NAKED_ISR
 * @brief	割り込み処理関数かつプロローグとエピローグを作らないようにする関数修飾子
 * @author	Kazuya Fukuhara
 * @date	2010/09/04 19:46:15	作成
 * @warning	廃止予定
 */
#define NAKED_ISR
#endif

/**
 * @typedef	union flags_t flags_t
 * @union	flags_t
 * @brief	フラグレジスタ
 * @author	Kazuya Fukuhara
 * @date	2010/09/04 19:46:15	作成
 */
typedef union flags_t {
	struct {
		uint8_t C:1;		/**< 0x01 : キャリーフラグ */
		uint8_t D:1;		/**< 0x02 : デバッグフラグ */
		uint8_t Z:1;		/**< 0x04 : ゼロフラグ */
		uint8_t S:1;		/**< 0x08 : サインフラグ */
		uint8_t B:1;		/**< 0x10 : レジスタバンク指定フラグ */
		uint8_t O:1;		/**< 0x20 : オーバフローフラグ */
		uint8_t I:1;		/**< 0x40 : 割り込み許可フラグ */
		uint8_t U:1;		/**< 0x80 : スタックポインタ指定フラグ */
	} Bits;
	uint8_t Value;
} flags_t;

/**
 * @typedef	struct context_t
 * @struct	context_t
 * @brief	コンテキスト
 * @note	Rレジスタ部とAレジスタ部を配列に書き換える予定
 * @author	Kazuya Fukuhara
 * @date	2010/09/04 19:46:15	作成
 */
typedef struct context_t {
	uint16_t	R0;
	uint16_t	R1;
	uint16_t	R2;
	uint16_t	R3;
	uint16_t	A0;
	uint16_t	A1;
	uint16_t	SB;
	uint16_t	FB;
	uint8_t		PC_L;		/**< ＰＣの最下位バイト */
	uint8_t		PC_M;		/**< ＰＣの中位バイト */
	flags_t		FLG;		/**< フラグレジスタ */
	uint8_t		PC_H;		/**< ＰＣの最上位バイト */
	uint8_t		HOOK[3];	/**< [EX] SVC_exitへのアドレス (return対策) */
} context_t;

/**
 * @def		SET_KERNEL_STACKPOINTER
 * @brief	カーネルスタックを現在のスタックポインタに設定する
 * @author	Kazuya Fukuhara
 * @date	2010/09/04 19:46:15	作成
 */
#define SET_KERNEL_STACKPOINTER()		\
asm(									\
	".GLB	_kernel_stack_ptr	\r\n"	\
	"ldc _kernel_stack_ptr, ISP	\r\n"	\
)

/**
 * @def			SAVE_CONTEXT
 * @brief		現在のコンテキストを待避
 * @attention	コンテキストはスタックに待避され、スタックポインタは currentTCB->stack_pointer に格納される。そのため、currentTCBに適切な値が設定済みであることが求められる。
 * @author		Kazuya Fukuhara
 * @date		2010/09/04 19:46:15	作成
 */
#define SAVE_CONTEXT()									\
asm (													\
	/* 汎用レジスタ R0～R3 */							\
	/* アドレスレジスタ A0～A1、SB, FB を待避 */		\
	/* スタックポインタ ISP , USP は退避しなくてよい */	\
	"pushm	R0, R1, R2, R3, A0, A1, SB, FB		\r\n"	\
														\
	/* a0 = &(currentTCB->stack_pointer) */				\
	/* stack_pointerはオフセットが0なので */			\
	/* 先頭番地をそのまま使える */						\
	".GLB	_currentTCB							\r\n"	\
	"mov.w	_currentTCB,	a0					\r\n"	\
														\
	/* ISPをTCB.stackpointerに保存する */				\
	"stc		ISP,	[a0]					\r\n"	\
);

/**
 * @def			RESTORE_CONTEXT
 * @brief		コンテキストを復帰する
 * @attention	復帰に使うスタックポインタは currentTCB->stack_pointer から読み出される。そのため、currentTCBに適切な値が設定済みであることが求められる。
 * @author		Kazuya Fukuhara
 * @date		2010/09/04 19:46:15	作成
 */
#define RESTORE_CONTEXT()							\
asm(												\
	/* ISP = currentTCB->stack_pointer */			\
	/* a0 = currentTCB */							\
	"mov.w	_currentTCB,	a0				\r\n"	\
	/* stack_pointerのオフセットは0なので */		\
	"ldc		[A0], ISP					\r\n"	\
	"popm	R0, R1, R2, R3, A0, A1, SB, FB	\r\n"	\
);

/**
 * @def		RETURN_FROM_INTERRUPT
 * @brief	割り込み処理から脱出する
 * @author	Kazuya Fukuhara
 * @date	2010/09/04 19:46:15	作成
 */
#define RETURN_FROM_INTERRUPT() asm( "reit" )

/**
 * @def   		GetContext
 * @brief		タスクコントロールブロックから実行コンテキストを取得
 * @param[in]	tcb 対象タスクコントロールブロック
 * @return		実行コンテキストを示すポインタ値
 * @author		Kazuya Fukuhara
 * @date		2010/09/04 19:46:15	作成
 */
#define GetContext(tcb) ((context_t*)((tcb)->stack_pointer))

/**
 * @def			GetArgWord
 * @brief		コンテキストからポインタ引数を取り出す
 * @param[in]	context 対象コンテキスト
 * @return		引数として渡されたポインタ値
 * @author		Kazuya Fukuhara
 * @date		2010/09/04 19:46:15	作成
 */
#define GetArgPtr(context) ((context)->R0)

/**
 * @def			SetTaskArg
 * @brief		コンテキストにタスク開始時の引数を設定
 * @param[in]	context 対象コンテキスト
 * @param[in]	arg     設定する引数
 * @author		Kazuya Fukuhara
 * @date		2010/09/04 19:46:15	作成
 */
#define SetTaskArg(context, arg) { (context)->R0 = (uint16_t)(arg); }

#endif
