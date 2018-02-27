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
 * @brief	コンテキストとコンテキスト操作
 * @author	Kazuya Fukuhara
 * @date	2013/03/17 23:07:09	作成
 */

#ifndef __CONTEXT_H__
#define __CONTEXT_H__

#include "./arch.h"
#include "../../src/type.h"

/**
 * @def		set_kernel_stackpointer_asm
 * @brief	カーネルスタックを現在のスタックポインタに設定するアセンブラ関数
 * @author	Kazuya Fukuhara
 * @date	2013/03/17 23:07:09	作成
 * @note	CC-RXではインラインアセンブラが使えないのでアセンブラ関数を作って使う
 */
#pragma inline_asm set_kernel_stackpointer_asm
static void set_kernel_stackpointer_asm(void *stack) {
	mov.l r1, r0
}

/**
 * @def		SET_KERNEL_STACKPOINTER
 * @brief	カーネルスタックを現在のスタックポインタに設定する
 * @author	Kazuya Fukuhara
 * @date	2013/03/17 23:07:09	作成
 */
#define SET_KERNEL_STACKPOINTER() \
	set_kernel_stackpointer_asm((void*)&kernel_stack[1024-4])

/**
 * @def			save_context_asm
 * @brief		現在のコンテキストを待避するアセンブラ関数
 * @author		Kazuya Fukuhara
 * @date	2013/03/17 23:07:09	作成
 * @note	CC-RXではインラインアセンブラが使えないのでアセンブラ関数を作って使う
 */
#pragma inline_asm save_context_asm
static void save_context_asm(void *tcb_sp) {
	pushm	r1-r14		; r1からr14を待避
	mov.l	0[r1], [r2]
	mov.l	r0, 0[r2]	; スタックポインタを保存
}

/**
 * @def			SAVE_CONTEXT
 * @brief		現在のコンテキストを待避
 * @attention	コンテキストはスタックに待避され、スタックポインタは currentTCB->stack_pointer に格納される。そのため、currentTCBに適切な値が設定済みであることが求められる。
 * @author		Kazuya Fukuhara
 * @date	2013/03/17 23:07:09	作成
 */
#define SAVE_CONTEXT() { \
	save_context_asm(&currentTCB->stack_pointer); \
}

/**
 * @def			restore_context_asm
 * @brief		コンテキストを復帰する
 * @author		Kazuya Fukuhara
 * @date	2013/03/17 23:07:09	作成
 * @note	CC-RXではインラインアセンブラが使えないのでアセンブラ関数を作って使う
 */
#pragma inline_asm restore_context_asm
static void restore_context_asm(void *tcb_sp) {
	nop;
	mov.l	r1, r0	; スタックポインタ復帰
	popm	r1-r14		; r1からr14を復帰
}

/**
 * @def			RESTORE_CONTEXT
 * @brief		コンテキストを復帰する
 * @attention	復帰に使うスタックポインタは currentTCB->stack_pointer から読み出される。そのため、currentTCBに適切な値が設定済みであることが求められる。
 * @author		Kazuya Fukuhara
 * @date	2013/03/17 23:07:09	作成
 */
#define RESTORE_CONTEXT() { \
	restore_context_asm(currentTCB->stack_pointer); \
}

/**
 * @def			return_from_interrupt_asm
 * @brief		割り込みから復帰する
 * @author		Kazuya Fukuhara
 * @date	2013/03/17 23:07:09	作成
 * @note	CC-RXではインラインアセンブラが使えないのでアセンブラ関数を作って使う
 */
#pragma inline_asm return_from_interrupt_asm
static void return_from_interrupt_asm() {
	rte	; 割り込み復帰
}

/**
 * @def		RETURN_FROM_INTERRUPT
 * @brief	割り込み処理から脱出する
 * @author	Kazuya Fukuhara
 * @date	2010/11/30 17:05 作成
 * @date	2013/03/17 23:07:09	作成
 */
#define RETURN_FROM_INTERRUPT() { \
	enableInterrupt(); \
	return_from_interrupt_asm(); \
}

/**
 * @typedef	struct context_t context_t;
 * @struct	context_t
 * @brief	スタックに保存される実行コンテキスト（スタックフレーム）
 * @author	Kazuya Fukuhara
 * @date	2013/03/17 23:07:09	作成
 */
typedef struct context_t {
	uint32_t r1, r2, r3, r4, r5, r6, r7, r8, r9, r10, r11, r12, r13, r14, PC, PSW, HOOK;
} context_t;

/**
 * @def   	SetReturnAddressToContext
 * @brief	コンテキストの戻り先アドレスを設定する
 * @param[in]	context 対象コンテキスト
 * @param[in]	address 設定する戻り先アドレス
 * @author	Kazuya Fukuhara
 * @date	2013/03/17 23:07:09	作成
 */
#define SetReturnAddressToContext(context, address) \
	(context)->PC = (uint32_t)(address);

/**
 * @def   		GetContext
 * @brief		タスクコントロールブロックから実行コンテキストを取得
 * @param[in]	tcb 対象タスクコントロールブロック
 * @return		実行コンテキストを示すポインタ値
 * @author		Kazuya Fukuhara
 * @date	2013/03/17 23:07:09	作成
 */
#define GetContext(tcb) ((context_t*)((tcb)->stack_pointer))

/**
 * @def			GetArgPtr
 * @brief		コンテキストからポインタ引数を取り出す
 * @param[in]	context 対象コンテキスト
 * @return		引数として渡されたポインタ値
 * @author		Kazuya Fukuhara
 * @date	2013/03/17 23:07:09	作成
 */
#define GetArgPtr(context) ((context)->r1)

/**
 * @def			SetTaskArg
 * @brief		コンテキストにタスク開始時の引数を設定
 * @param[in]	context 対象コンテキスト
 * @param[in]	arg     設定する引数
 * @author		Kazuya Fukuhara
 * @date	2013/03/17 23:07:09	作成
 */
#define SetTaskArg(context, arg) { (context)->r1 = (uint32_t)(arg); }

#endif

