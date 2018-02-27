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
 * @brief	LPC1343ハードウェア依存のコード
 * @author	Kazuya Fukuhara
 * @date	2010/11/28 21:30:38	作成
 */

#include "../../src/arch.h"
#include "../../src/kernel.h"
#include "../../src/syscall.h"
#include "./context.h"
#include "./hw/cpu.h"
#include "./hw/systick.h"
#include "./hw/gpio.h"

/**
 * @brief	カーネルスタック領域
 * @author	Kazuya Fukuhara
 * @date	2010/11/28 21:30:38	作成
 */
uint8_t kernel_stack[KERNEL_STACK_SIZE];

/**
 * @brief	システムタイマ割り込み処理関数
 * @author	Kazuya Fukuhara
 * @date	2010/11/30 17:05 作成
 */
__attribute__((naked))
void SysTick_Handler(void) {
	disableInterrupt();

	/* コンテキストを保存 */
	SAVE_CONTEXT();

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
 * @date	2010/11/30 17:05 作成
 * 
 * カーネルタイマやソフトウェア割り込みを有効化するなどの初期化を行う。
 */
void initHardware(void) {
	/* 割り込みを全面的に禁止 */
	disableInterrupt();

	/* LPC1343を初期化（GPIOも含まれる） */
	CPU_Initialize();

	/* システムクロックを用いたタイマの間隔を10msに設定 */
	SYSTICK_Initialize(10);
}

/**
 * @brief			コンテキストの初期化
 * @param[in] tid	タスク番号
 * @author	Kazuya Fukuhara
 * @date	2010/11/30 17:05 作成
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

	/* Cortex-M3特有の実行時ステータスフラグ設定 */
	context->xPSR = 0x81000000;

	/* exitアドレスの設定 */
//	SetReturnAddressToContext(context, API_exitTASK);
	context->LR = (unsigned int)API_exitTASK;

	/* 開始アドレス値の設定 */
//	SetReturnAddressToContext(context, TaskTable[tid]);
	context->PC = (unsigned int)TaskTable[tid];
}

/**
 * @brief					システムコール呼び出し
 * @param[in,out] param		呼び出すシステムコールのＩＤや引数の格納された構造体へのポインタ
 * @author	Kazuya Fukuhara
 * @date	2010/11/30 17:05 作成
 * 
 * @note	SVCコールの引数部分は使わないので 0 固定とする
 */
svcresultid_t syscall(ptr_t param) {
	asm volatile ("SVC 0");
}

/**
 * @brief	ソフトウェア割り込みの捕捉
 * @author	Kazuya Fukuhara
 * @date	2010/11/30 17:05 作成
 * 
 * @note	Cortex-M3特有の制約に対応するために初期化動作と
 *          通常のシステムコール呼び出しを区別する仕掛けが施されている。
 */
__attribute__((naked))
void SVCall_Handler(void) {
	/* 割り込み発生時に r0, r1, r2, r3, r12, LR, PC, xPSR が自動で保存される */
	disableInterrupt();
	if (currentTCB == 0) {
		/* currentTCBがNULLの場合は初期化動作と見なして、startKernelに分岐 */
		__asm volatile ("b		startKernel		\r\n");
	} else {
		/* 初回起動時でない場合 */

		/* コンテキストを保存 */
		SAVE_CONTEXT();

		/* スタックポインタをカーネルスタックに書き換える */
		SET_KERNEL_STACKPOINTER();

		/* SVCall_Handler_mainに分岐 */
		__asm volatile ("b		syscall_entry	\r\n");

	}

}

/**
 * @brief	スタートアップルーチン
 * @author	Kazuya Fukuhara
 * @date	2010/11/30 17:05 作成
 * 
 * カーネルスタックへの切り替えを行い、必要な処理を行ってから、startKernel()を呼び出す。
 */
int main(void) {
	/* カーネルスタックへ切り替え */
	SET_KERNEL_STACKPOINTER();

	/* 以降、カーネルスタックが利用可能になる */

	/* カーネルの稼動開始 */
	/* Cortex-M3ではフラグレジスタなどで割り込みモードへの遷移/解除が出来ない？ようなので、
	 * サービスコール割り込みを一旦呼び出して、その中で初期化ルーチンへ遷移させる。
	 * ここは一旦NXPかARMの人に聞いてみたい。
	 */
	currentTCB = NULL;
	__asm volatile ("SVC 0");

	/* コンパイラの警告対策 */
	return 0;
}

/**
 * @brief			フック可能な外部割り込み番号か判定
 * @param[in] id	外部割り込み番号
 * @retval TRUE		フック可能な外部割り込み番号
 * @retval FALSE	フック不可能な外部割り込み番号
 * @author			Kazuya Fukuhara
 * @date			2010/11/30 17:05 作成
 * @warning			LPC1343
 */
bool_t is_hookable_interrupt_id(extintid_t int_id) {
	/* カーネルタイマや、システムコール呼び出しに使うソフトウェア割り込みなどのフックを防ぐようにする */
	/* Cortex-M3ポーティング版ではextintid_tの範囲であれば全て使える */
	if ((int_id < 0) || (int_id >= EXTINT_NUM)) {
		return FALSE;
	} else {
		return TRUE;
	}
}

