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
 * @brief	RX63Nハードウェア依存のコード
 * @author	Kazuya Fukuhara
 * @date	2013/03/17 23:07:09	作成
 */

#include "../../src/arch.h"
#include "../../src/kernel.h"
#include "../../src/syscall.h"
#include "./context.h"
//#include "./hw/cpu.h"
//#include "./hw/systick.h"
//#include "./hw/gpio.h"

/**
 * @brief	カーネルスタック領域
 * @author	Kazuya Fukuhara
 * @date	2013/03/17 23:07:09	作成
 */
uint8_t kernel_stack[KERNEL_STACK_SIZE];

/**
 * @brief	システムタイマ割り込み処理関数
 * @author	Kazuya Fukuhara
 * @date	2013/03/17 23:07:09	作成
 */
void Excep_CMT0_CMI0_Handler(void) {
	extern void SYSTICK_Reset();
	/* コンテキストはアセンブラ側で保存済み */

	/* タイマーリセット */
	SYSTICK_Reset();
	
	/* スタックポインタをカーネルスタックに書き換える */
	SET_KERNEL_STACKPOINTER();

	/* スリープ状態タスクの待ち時間を更新する */
	updateTick();

	/* スケジューリングを行い、次に実行するタスクを選択する */
	scheduler();
}

/**
 * @brief	ハードウェア全般の初期化処理
 * @author	Kazuya Fukuhara
 * @date	2013/03/17 23:07:09	作成
 * 
 * カーネルタイマやソフトウェア割り込みを有効化するなどの初期化を行う。
 */
void initHardware(void) {
	/* 割り込みを全面的に禁止 */
	disableInterrupt();

	/* RX63Nを初期化（GPIOも含まれる） */
	CPU_Initialize();

	/* システムクロックを用いたタイマの間隔を10msに設定 */
	SYSTICK_Initialize(10);
}

/**
 * @brief			コンテキストの初期化
 * @param[in] tid	タスク番号
 * @author	Kazuya Fukuhara
 * @date	2013/03/17 23:07:09	作成
 * 
 * 指定されたタスクのコンテキストの初期化を行う。具体的には以下の処理を行う
 * @li スタックポインタの初期値を設定
 * @li コンテキストを抜けた際に自動的にexitが呼ばれるように設定
 * @li コンテキストの初期値設定
 * @li 開始アドレス値の設定
 */
void resetContext(taskid_t tid) {

	context_t* context;
	
	/* スタックポインタの初期値を設定 */
	tasks[tid].stack_pointer = &task_stack[tid][TASK_STACK_SIZE-sizeof(context_t)];

	/* 初期化用コンテキストを取得し、ゼロクリア */
	context = (context_t*)&task_stack[tid][TASK_STACK_SIZE-sizeof(context_t)];
	ZeroMemory(context, sizeof(context_t));

	/* exitアドレスの設定 */
//	SetReturnAddressToContext(context, API_exitTASK);
	context->HOOK = (unsigned int)API_exitTASK;

	/* 開始アドレス値の設定 */
//	SetReturnAddressToContext(context, TaskTable[tid]);
	context->PC = (unsigned int)TaskTable[tid];

	/* 割り込み許可フラグを立てる */
	context->PSW = 0x00010000;
}

/**
 * @brief					システムコール呼び出しのアセンブラ処理
 * @param[in,out] param		呼び出すシステムコールのＩＤや引数の格納された構造体へのポインタ
 * @author	Kazuya Fukuhara
 * @date	2013/03/17 23:07:09	作成
 * 
 * @note	INTコールの引数部分は使わないので 0 固定とする
 */
#pragma inline_asm syscall_body
static svcresultid_t syscall_body(ptr_t param) {
	INT #0;
}

/**
 * @brief					システムコール呼び出し
 * @param[in,out] param		呼び出すシステムコールのＩＤや引数の格納された構造体へのポインタ
 * @author	Kazuya Fukuhara
 * @date	2013/03/17 23:07:09	作成
 */
svcresultid_t syscall(ptr_t param) {
	return syscall_body(param);
}

/**
 * @brief	ソフトウェア割り込みの捕捉
 * @author	Kazuya Fukuhara
 * @date	2013/03/17 23:07:09	作成
 */
void Excep_BRK_Handler(void) {
	/* コンテキストはアセンブラ側で保存済み */

	/* スタックポインタをカーネルスタックに書き換える */
	SET_KERNEL_STACKPOINTER();

	/* SVCall_Handler_mainに分岐 */
	syscall_entry();

}

/**
 * @brief	スタートアップルーチン
 * @author	Kazuya Fukuhara
 * @date	2013/03/17 23:07:09	作成
 * 
 * カーネルスタックへの切り替えを行い、必要な処理を行ってから、startKernel()を呼び出す。
 */
int main(void) {
	/* カーネルスタックへ切り替え */
	SET_KERNEL_STACKPOINTER();

	/* 以降、カーネルスタックが利用可能になる */

	startKernel();

	/* コンパイラの警告対策 */
	return 0;
}

/**
 * @brief			フック可能な外部割り込み番号か判定
 * @param[in] id	外部割り込み番号
 * @retval TRUE		フック可能な外部割り込み番号
 * @retval FALSE	フック不可能な外部割り込み番号
 * @author			Kazuya Fukuhara
 * @date	2013/03/17 23:07:09	作成
 * @warning			RX63Nでは今のところ対応させていない
 */
bool_t is_hookable_interrupt_id(extintid_t int_id) {
	/* カーネルタイマや、システムコール呼び出しに使うソフトウェア割り込みなどのフックを防ぐようにする */
	/* RX63Nでは今のところ対応させていない */
	return FALSE;
}

