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
 * @brief	Win32環境でのコンテキストとコンテキスト操作
 * @author	Kazuya Fukuhara
 * @date	2010/09/01 21:36:49	作成
 */

#ifndef __CONTEXT_H__
#define __CONTEXT_H__

#include "./arch.h"
#include "../../src/type.h"

/**
 * @def		NAKED
 * @brief	関数のプロローグとエピローグを作らないようにする関数修飾子
 * @author	Kazuya Fukuhara
 * @date	2010/09/01 21:36:49	作成
 */
#define NAKED __declspec(naked)

/**
 * @def		SET_KERNEL_STACKPOINTER
 * @brief	カーネルスタックを現在のスタックポインタに設定する
 * @author	Kazuya Fukuhara
 * @date	2010/09/01 21:36:49	作成
 */
#define SET_KERNEL_STACKPOINTER() \
	__asm{	mov esp, DWORD PTR [kernel_stack_ptr]	}\
	__asm{	mov ebp, esp							}

/**
 * @def			SAVE_CONTEXT
 * @brief		現在のコンテキストを待避
 * @attention	コンテキストはスタックに待避され、スタックポインタは currentTCB->stack_pointer に格納される。そのため、currentTCBに適切な値が設定済みであることが求められる。
 * @author		Kazuya Fukuhara
 * @date		2010/09/01 21:36:49	作成
 * @note        浮動小数点数レジスタや MMX レジスタ等の退避は行っていない。
 */
#define SAVE_CONTEXT() {					\
	__asm{	push esi						}\
	__asm{	push edi						}\
	__asm{	push ebp						}\
	__asm{	push edx						}\
	__asm{	push ecx						}\
	__asm{	push ebx						}\
	__asm{	push eax						}\
	__asm{	pushfd							}\
	__asm{	mov	eax, [currentTCB]			}\
	__asm{	mov DWORD PTR [eax+0x00], esp	}\
}

/**
 * @def			RESTORE_CONTEXT
 * @brief		コンテキストを復帰する
 * @attention	復帰に使うスタックポインタは currentTCB->stack_pointer から読み出される。そのため、currentTCBに適切な値が設定済みであることが求められる。
 * @author		Kazuya Fukuhara
 * @date		2010/09/01 21:36:49	作成
 * @note        浮動小数点数レジスタや MMX レジスタ等の復帰は行っていない。
 */
#define RESTORE_CONTEXT() {					\
	__asm{	mov	eax, [currentTCB]			}\
	__asm{	mov esp, DWORD PTR [eax+0x00]	}\
	__asm{	popfd							}\
	__asm{	pop eax							}\
	__asm{	pop ebx							}\
	__asm{	pop ecx							}\
	__asm{	pop edx							}\
	__asm{	pop ebp							}\
	__asm{	pop edi							}\
	__asm{	pop esi							}\
}

/**
 * @def		RETURN_FROM_INTERRUPT
 * @brief	割り込み処理から脱出する
 * @author	Kazuya Fukuhara
 * @date	2010/09/01 21:36:49	作成
 */
#define RETURN_FROM_INTERRUPT() {	\
	__asm { push eax }				\
	enableInterrupt();				\
	__asm { pop eax }				\
	__asm { ret };					\
}

/**
 * @brief	__stacl
 * @brief	カーネルスタックへのポインタ
 * @author	Kazuya Fukuhara
 * @date	2010/09/01 21:36:49	作成
 */
const extern void* kernel_stack_ptr;

/**
 * @typedef	register_t
 * @brief	32bitレジスタを示す型
 * @author	Kazuya Fukuhara
 * @date	2010/09/01 21:36:49	作成
 */
typedef unsigned long register_t;

/**
 * @typedef	struct context_t context_t;
 * @struct	context_t
 * @brief	実行コンテキスト
 * @author	Kazuya Fukuhara
 * @date	2010/09/01 21:36:49	作成
 */
typedef struct context_t {
	register_t flag;
	register_t eax;
	register_t ebx;
	register_t ecx;
	register_t edx;
	register_t ebp;
	register_t edi;
	register_t esi;
	register_t ret;
} context_t;

/**
 * @typedef	struct context_init_t context_init_t;
 * @struct	context_init_t
 * @brief	タスクの初期設定時に使用するコンテキスト
 * @author	Kazuya Fukuhara
 * @date	2010/09/01 21:36:49	作成
 * @par		コンテキスト詳細
 * @htmlonly
<table border="1">
<tr><td><code>+0x00</code></td><td><code>flag</code></td><td rowspan="9"><code>context_t</code></td></tr>
<tr><td><code>+0x04</code></td><td><code>eax</code></td></tr>
<tr><td><code>+0x08</code></td><td><code>ebx</code></td></tr>
<tr><td><code>+0x0C</code></td><td><code>ecx</code></td></tr>
<tr><td><code>+0x10</code></td><td><code>edx</code></td></tr>
<tr><td><code>+0x14</code></td><td><code>ebp</code></td></tr>
<tr><td><code>+0x18</code></td><td><code>edi</code></td></tr>
<tr><td><code>+0x1C</code></td><td><code>esi</code></td></tr>
<tr><td><code>+0x20</code></td><td><code>ret</code></td></tr>
<tr><td><code>+0x24</code></td><td><code>exit_proc</code></td><td><code>タスク終了時に呼び出されるアドレス</code></td></tr>
<tr><td><code>+0x28</code></td><td><code>arg</code></td><td><code>タスク開始時の引数</code></td></tr>
</table>
 * @endhtmlonly
 */
typedef struct context_init_t {
	context_t context;
	register_t exit_proc;
	register_t arg;
} context_init_t;

/**
 * @def			SetReturnAddressToContext
 * @brief		コンテキストの戻り先アドレスを設定する
 * @param[in]	context 対象コンテキスト
 * @param[in]	address 設定する戻り先アドレス
 * @author		Kazuya Fukuhara
 * @date		2010/09/01 21:36:49	作成
 */
#define SetReturnAddressToContext(context, address) {	\
	(context)->ret = (register_t)(address);				\
}

/**
 * @def   		GetContext
 * @brief		タスクコントロールブロックから実行コンテキストを取得
 * @param[in]	tcb 対象タスクコントロールブロック
 * @return		実行コンテキストを示すポインタ値
 * @author		Kazuya Fukuhara
 * @date		2010/09/01 21:36:49	作成
 */
#define GetContext(tcb) ((context_t*)((tcb)->stack_pointer))

/**
 * @def			GetArgWord
 * @brief		コンテキストからポインタ引数を取り出す
 * @param[in]	context 対象コンテキスト
 * @return		引数として渡されたポインタ値
 * @author		Kazuya Fukuhara
 * @date		2010/09/01 21:36:49	作成
 */
#define GetArgPtr(context) (ptr_t)(((uint32_t*)(((uint8_t*)(context))+sizeof(context_t)))[0])

/**
 * @def			SetTaskArg
 * @brief		コンテキストにタスク開始時の引数を設定
 * @param[in]	context 対象コンテキスト
 * @param[in]	arg     設定する引数
 * @author		Kazuya Fukuhara
 * @date		2010/09/01 21:36:49	作成
 */
#define SetTaskArg(context, argument) {\
	context_init_t* context_init = (context_init_t*)(context);\
	context_init->arg = (register_t)(argument);\
}

#endif
