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
 * @brief   Win32依存のコード
 * @author  Kazuya Fukuhara
 * @date	2010/09/01 21:36:49	作成
 */

#include <process.h>
#include "../../src/arch.h"
#include "../../src/config.h"
#include "../../src/kernel.h"
#include "../../src/type.h"
#include "../../src/syscall.h"
#include "./hw.h"
#include "./virtualhw.h"
#include "./vd_kerneltimer.h"
#include "./vd_button.h"

/**
 * @brief   ハードウェア全般の初期化処理
 * @author  Kazuya Fukuhara
 * @date	2010/09/01 21:36:49	作成
 */
void initHardware(void) {
	initVirtualHardware();	/* 仮想ハードウェアの初期化 */
	initKernelTimer();		/* カーネルタイマを初期化 */
	initButtonDevice();		/* ボタンを初期化 */
	startVirtualHardware();	/* 仮想ハードウェアの実行を開始 */
	Sleep(1000);
}

/**
 * @brief	割り込みを禁止する
 * @author	Kazuya Fukuhara
 * @date	2010/09/01 21:36:49	作成
 */
void disableInterrupt(void) { 
	VHW_disableInterrupt();
}

/**
 * @brief	割り込み禁止解除
 * @author  Kazuya Fukuhara
 * @date	2010/09/01 21:36:49	作成
 */
void enableInterrupt(void) {
	VHW_enableInterrupt();
}

/**
 * @brief			コンテキストの初期化
 * @param[in] tid	タスク番号
 * @author			Kazuya Fukuhara
 * @date			2010/09/01 21:36:49	作成
 */
void resetContext(taskid_t tid) {
	context_init_t *context_init; 

	/* 初期化用コンテキストを取得し、ゼロクリア */
	context_init = (context_init_t*)&task_stack[tid][TASK_STACK_SIZE-sizeof(context_init_t)];
	ZeroMemory(context_init, sizeof(context_init_t));

	/* スタックの初期値を設定 */
	tasks[tid].stack_pointer = (uint8_t*)context_init;
	context_init->context.ebp = (register_t)&task_stack[tid][TASK_STACK_SIZE-sizeof(register_t)];

	/* コンテキストを抜けた際に自動的にexitが呼ばれるように設定 */
	context_init->exit_proc = (register_t)API_exitTASK;

	/* 開始アドレス値の設定 */
	SetReturnAddressToContext((context_t*)context_init, TaskTable[tid]);

}

/**
 * @brief					システムコール呼び出し
 * @param[in,out] param		呼び出すシステムコールのＩＤや引数の格納された構造体へのポインタ
 * @author					Kazuya Fukuhara
 * @date					2010/09/01 21:36:49	作成
 *
 * Win32ではstdcall呼び出し規約引数をスタック渡しで渡す。
 */
void syscall(ptr_t param) {

	/* 割り込み禁止 */
	disableInterrupt();

	/* 引数と戻り先を設定 */
	__asm {
		push	param
		push	L_syscall_exit	/* 戻り先アドレス */
	}
	/* コンテキストを保存 */
	SAVE_CONTEXT();

	/* カーネルスタックに切り替え */
	SET_KERNEL_STACKPOINTER();

	/* システムコール呼び出し処理に移動 */
	syscall_entry();

	/* システムコールからの戻り先 */
	__asm {
L_syscall_exit:
		add	esp,	0x04	/* 引数１個分(=4バイト)スタックを解放する */
	}
}

/**
 * @brief	発生した外部割り込みの番号
 * @author	Kazuya Fukuhara
 * @date	2010/09/11 11:01:20	作成
 */
uint8_t ExtIntID = 0;

/**
 * @brief			フック可能な外部割り込み番号か判定
 * @param[in] id	外部割り込み番号
 * @retval TRUE		フック可能な外部割り込み番号
 * @retval FALSE	フック不可能な外部割り込み番号
 * @author			Kazuya Fukuhara
 * @date			2010/09/11 11:01:20	作成
 */
bool_t is_hookable_interrupt_id(extintid_t int_id) {
	if (int_id == INTID_TIMER) {
		return FALSE;
	} else {
		return TRUE;
	}
}

