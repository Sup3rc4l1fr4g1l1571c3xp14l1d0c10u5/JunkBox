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
 * @brief	M16C T-Birdハードウェア依存のコード
 * @author	Kazuya Fukuhara
 * @date	2010/09/04 19:46:15	作成
 * @date	2010/09/11 11:01:20	外部割り込みへの対応を追加
 */

#include "../../src/arch.h"
#include "../../src/kernel.h"
#include "../../src/syscall.h"

/**
 * @def		NAKED_FUNC
 * @brief	プロローグのコードを全く生成しない関数の宣言（小手先の小技）
 * @author	Kazuya Fukuhara
 * @date	2010/09/04 19:46:15	作成
 * 
 * 割り込みやシステムコール処理などでスタックフレームを作られると困る場合に
 * プロローグのコードを全く生成しない関数を宣言するための小手先のテクニック
 */
#define NAKED_FUNC(x,y) \
static void naked_##x##_ y { \
		asm( ".glb _" #x ); \
		asm( "_" #x ":" );

/**
 * @def		TT0MR
 * @brief	タイマーA0のモードレジスタ
 * @author	Kazuya Fukuhara
 * @date	2010/09/04 19:46:15	作成
 */
#define TT0MR	(*(volatile uint8_t*)0x0396)

/**
 * @def		TA0CNT
 * @brief	タイマーA0のカウンタレジスタ(0387h:0386h番地)
 * @author	Kazuya Fukuhara
 * @date	2010/09/04 19:46:15	作成
 */
#define TA0CNT	(*(volatile uint16_t*)0x0386)

/**
 * @def		TCNTFR
 * @brief	タイマーA0のカウント開始フラグレジスタ
 * @author	Kazuya Fukuhara
 * @date	2010/09/04 19:46:15	作成
 */
#define TCNTFR	(*(volatile uint8_t*)0x0380)

/**
 * @brief	カーネルスタック領域
 * @author	Kazuya Fukuhara
 * @date	2010/09/04 19:46:15	作成
 */
uint8_t kernel_stack[KERNEL_STACK_SIZE];

/**
 * @brief	カーネルスタックの先頭ポインタ
 * @author	Kazuya Fukuhara
 * @date	2010/09/04 19:46:15	作成
 */
const uint8_t *kernel_stack_ptr = &kernel_stack[KERNEL_STACK_SIZE];

/**
 * @brief	タイマー割り込みハンドラ
 * @author	Kazuya Fukuhara
 * @date	2010/09/04 19:46:15	作成
 */
NAKED_FUNC(timer_int,(void))

	/* 割り込みを禁止する */
	disableInterrupt();	// fclr	I
	
	/* カウンタTA0(タイマA0レジスタ : 0387h:0386h番地)を1250にする。(ダウンカウントを使うため) */
	TA0CNT = 1250;	// mov.w	#1250,	TA0CNT

	/* コンテキスト退避 */
	SAVE_CONTEXT();

	/* ISPにカーネルのスタックアドレスを代入する */
	SET_KERNEL_STACKPOINTER();
	
	/* スリープ状態タスクの待ち時間を更新する */
	updateTick();

	/* スケジューリングを行い、次に実行するタスクを選択する */
	scheduler();
}

/**
 * @brief	カーネルタイマの初期化
 * @author	Kazuya Fukuhara
 * @date	2010/09/04 19:46:15	作成
 */
static void initKernelTimer(void) {
	TT0MR   = 0x40;
	TA0CNT  = 1250;	/* 1ms割り込み */
	TCNTFR  = 1;
	INTCR[INT_TIMERA0] = 6;
}

/**
 * @brief	ソフトウェア割り込みの初期化
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	作成
 */
static void initSoftwareInterrupt(void) {
	INTCR[INT_SWINT] = 6;
}

/**
 * @brief	ハードウェア全般の初期化処理
 * @author	Kazuya Fukuhara
 * @date	2010/09/04 19:46:15	作成
 */
void initHardware(void) {
	/**
	 * ソフトウェア割り込みを初期化
	 */
	initSoftwareInterrupt();

	/**
	 * カーネルタイマを初期化
	 */
	initKernelTimer();
}

/**
 * @brief			コンテキストの初期化
 * @param[in] tid	タスク番号
 * @author			Kazuya Fukuhara
 * @date			2010/09/04 19:46:15	作成
 */
void resetContext(taskid_t tid) {
	context_t* context;
	const taskproc_t StartAddress = TaskTable[tid];

	/* 初期化用コンテキストを取得し、ゼロクリア */
	context = (context_t*)&task_stack[tid][TASK_STACK_SIZE-sizeof(context_t)];
	ZeroMemory(context, (uint_t)sizeof(context_t));

	/* スタックポインタの初期値を設定 */
	tasks[tid].stack_pointer = (uint8_t*)context;

	/* コンテキストを抜けた際に自動的にexitが呼ばれるように設定 */
	context->HOOK[0] = (uint8_t)(((uint32_t)API_exitTASK      ) & 0xFF);
	context->HOOK[1] = (uint8_t)(((uint32_t)API_exitTASK >>  8) & 0xFF);
	context->HOOK[2] = (uint8_t)(((uint32_t)API_exitTASK >> 16) & 0xFF);

	/* コンテキストの設定 */
	context->R0 = 0;
	context->R1 = 0;
	context->R2 = 0;
	context->R3 = 0;
	context->A0 = 0;
	context->A1 = 0;
	context->SB = 0;
	context->FB = 0;
	context->FLG.Value = 0x40;

	/* 開始アドレス値の設定 */
	context->PC_L  = (uint8_t)(((uint32_t)StartAddress      ) & 0x0000FF);
	context->PC_M  = (uint8_t)(((uint32_t)StartAddress >>  8) & 0x0000FF);
	context->PC_H  = (uint8_t)(((uint32_t)StartAddress >> 16) & 0x0000FF);
}

/**
 * @brief	外部割り込みで発生した割り込みの番号を格納
 * @author	Kazuya Fukuhara
 * @date	2010/09/04 19:46:15	作成
 */
uint8_t ExtIntID;

/**
 * @brief	外部割り込みハンドラ
 * @author	Kazuya Fukuhara
 * @date	2010/09/04 19:46:15	作成
 */
NAKED_FUNC(external_interrupt,(void))
	/* 割り込み発生
	 * PCとFLGは自動的にスタック上に退避されている。
	 * ここに到達した時点で、ISPの値は実行中のタスクの最新のスタックポインタとなっている。
	 */

	/* コンテキスト退避 */
	SAVE_CONTEXT();

	/* カーネルスタックへ切り替え */
	SET_KERNEL_STACKPOINTER();

	/* 外部割り込み処理関数へ */
	external_interrupt_handler();	
}

/**
 * @brief					システムコール呼び出し
 * @param[in,out] param		呼び出すシステムコールのＩＤや引数の格納された構造体へのポインタ
 * @author					Kazuya Fukuhara
 * @date					2010/09/01 21:36:49	作成
 */
NAKED_FUNC(syscall, (ptr_t param) )
	asm("INT_SYSCALL .equ 32");			/* システムコール用ソフトウェア割り込み番号 */
	asm("int #INT_SYSCALL");	/* ソフトウェア割り込みでシステムコール */
	asm("rts");
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
	switch (int_id) {
		case INT_UART1_TX :	/* デバッガで使用中 */
		case INT_UART1_RX :	/* デバッガで使用中 */
		case INT_TIMERA0 :	/* カーネルタイマーで使用中 */
			return FALSE;
		default:
			if (int_id >= INT_SWINT) {	/* ソフトウェア割り込み */
				return FALSE;
			}
	}
	return TRUE;
}

/**
 * @brief	システムコール本体
 * @note	呼び出すシステムコールのIDはR0に、引数１・２はそれぞれR1とR2に格納されている必要がある。
 * @author	Kazuya Fukuhara
 * @date	2010/09/04 19:46:15	作成
 */
NAKED_FUNC(software_int,(void))
	/* 割り込みを禁止する */
	disableInterrupt();

	/* 割り込み発生
	 * PCとFLGは自動的にスタック上に退避されている。
	 * ここに到達した時点で、ISPの値は実行中のタスクの最新のスタックポインタとなっている。
	 */
	/* コンテキスト退避 */
	SAVE_CONTEXT();

	/* カーネルスタックへ切り替え */
	SET_KERNEL_STACKPOINTER();
	
	/* システムコールの種別を判定して本体に分岐する関数へ無条件分岐 */
	syscall_entry();
}

/**
 * @brief	ブートローダから呼び出されるスタートアップルーチン
 * @author	Kazuya Fukuhara
 * @date	2010/09/01 21:36:49	作成
 */
void main(void) {
	/* カーネルスタックへ切り替え */
	SET_KERNEL_STACKPOINTER();

	/* 以降、カーネルスタックが利用可能になる */

	/* カーネルの稼動開始 */
	startKernel();	
}

