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
 * @brief	AVRハードウェア依存のコード
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	作成
 * @date	2010/08/15 16:11:02	機種依存部分として分離
 * @date	2010/09/11 11:01:20	外部割り込みへの対応を追加
 */

#include "../../src/arch.h"
#include "../../src/kernel.h"
#include "../../src/syscall.h"

NAKED_ISR void TIMER0_COMPA_vect(void);

/**
 * @brief	タイマー割り込みハンドラ
 * @note	通常の記述（ ISR(TIMER0_COMPA_vect) ）ではプロローグとエピローグが生成されてしまうので、自前で宣言記述を行い、naked属性を付与している。
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	作成
 */
void TIMER0_COMPA_vect(void) {
	/* この時点ではどのコンテキストを使っているか不明なのでスタックを使えない */

	/* 現在のタスクのコンテキストを保存 */
	SAVE_CONTEXT();

	/* カーネルスタックに切り替え */
	SET_KERNEL_STACKPOINTER();

	/* 以降はカーネルスタックの利用が可能になったので、関数呼び出しなどが可能となる */

	/* スリープ状態タスクの待ち時間を更新する */
	updateTick();

	/* スケジューリングを行い、次に実行するタスクを選択する */
	scheduler();
}

/**
 * @brief	カーネルタイマの初期化
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	作成
 */
static void initKernelTimer(void) {
	/**
	 * タイマカウンタ0制御レジスタAを設定
	 * 0b00000010
	 *     ||||||
	 *     ||||++-----  WGM01: WGM00  0b10  CTC動作
	 *     ||++------- COM0B1:COM0B0  0b00  標準動作(OC0B切断)
	 *     ++--------- COM0A1:COM0A0  0b00  標準動作(OC0A切断)
	 */
	TCCR0A = 0b00000010;

	/**
	 * タイマカウンタ1制御レジスタBを設定
	 * FOC0A:FOC0B		 00
	 * WGM02			  0	CTC動作
	 * CS02:CS01:CS00	010	分周比を8分周に設定
	 */
	TCCR0B = 0b00000010;

	/**
	 * タイマカウンタ比較レジスタにカウンタ値を設定
	 * 分周比は8分周に設定しているので、20Mhz/8 = 2.5Mhz のカウンタとなる。
	 * 8bitタイマを使い250カウントに１回の割り込みの設定にすると
	 * 2.5Mhz/250 = 2500Khz/250 = 10Khzとなり、
	 * 0.1ms間隔でスケジューラが実行されることになる
	 */
	OCR0A = 250;

	/**
	 * タイマカウンタ0割り込みマスクレジスタ
	 * ICIE0  0
	 * OCIE0B 0
	 * OCIE0A 1 タイマカウンタ0比較A割り込み許可
	 * TOIE0  0
	 */
	TIMSK0 = 0b00000010;
}

NAKED_ISR void INT0_vect(void);

/**
 * @brief	AVRのINT0割り込みベクタに登録する処理
 * @note	通常の割り込みルーチンの宣言 ISR(INT0_vect) ではプロローグとエピローグが生成されてしまう。
 * @note	そのため、自前で宣言記述を行い、naked属性を付与している。
 * @note	また、INT0割り込みを呼び出すシステムコール関数では引数をregister属性で宣言しているため、
 * @note	システムコール関数に渡された引数はレジスタに格納されたまま呼び出されることになる。
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	作成
 */
void INT0_vect(void) {
	/* コンテキストを保存 */
	SAVE_CONTEXT();

	/* カーネルスタックに切り替え */
	SET_KERNEL_STACKPOINTER();

	/* EXT0割り込みをリセット */
	PORTD &= ~_BV(PORTD2);

	/* システムコール呼び出し処理に移動 */
	syscall_entry();
}

/**
 * @brief					システムコール呼び出し
 * @param[in,out] param		呼び出すシステムコールのＩＤや引数の格納された構造体へのポインタ
 * @author					Kazuya Fukuhara
 * @date					2010/09/01 21:36:49	作成
 *
 * WinAVRではregister修飾子が有効であり、第１引数はレジスタ渡しで渡される。
 * また、AVRにはソフトウェア割り込みがないため、INT0割り込みをソフトウェア割り込みとして利用している。
 */
svcresultid_t NAKED syscall(register ptr_t param) {
	(void)param;
	asm volatile(
		"	sbi	0x0B,	2		\n\t"
		"	ret					\n\t"
	);
	return 0;	// dummy
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
		case INT_RESET :	/* リセット割り込みはフックできない */
		case INT_INT0 :		/* INT0割り込みはシステムコール呼び出しで使うためフックできない */
		case INT_TIMER0_COMPA:	/* タイマー０はカーネルタイマーで使用中 */
		case INT_TIMER0_COMPB:	/* タイマー０はカーネルタイマーで使用中 */
		case INT_TIMER0_OVF:	/* タイマー０はカーネルタイマーで使用中 */
			return FALSE;
		default:
			break;
	}
	return TRUE;
}

/**
 * @brief	ソフトウェア割り込みの初期化
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	作成
 */
static void initSoftwareInterrupt(void) {
	/**
	 * AVRにはソフトウェア割り込みが存在しない。
	 * しかし、外部割り込みは INT0(PD2) ポートを出力に設定しても使用可能となっており、
	 * プログラムから INT0(PD2) に出力することで、外部割り込みを発生させることができる。
	 * これを利用して、外部割り込み INT0(PD2) をソフトウェア割り込みの代用とする。
	 */

	/* 外部割り込み INT0(PD2) は出力モード */
	DDRD  |=  _BV(PORTD2);
	PORTD &= ~_BV(PORTD2);

	/* 外部割り込み条件: INT0(PD2) の立ち上がりで発生 */
	EICRA |= (_BV(ISC01)|_BV(ISC00));

	/* 外部割り込みマスクレジスタ: INT0(PD2) の割り込みを許可 */
	EIMSK |= _BV(INT0);
}

/**
 * @brief	ハードウェア全般の初期化処理
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	作成
 */
void initHardware(void) {
	/*
	 * 割り込み禁止状態に設定
	 */
	cli();

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
 * @date			2010/08/15 16:11:02	作成
 */
void resetContext(taskid_t tid) {
	context_t* context;
	
	/* スタックポインタの初期値を設定 */
	tasks[tid].stack_pointer = &task_stack[tid][TASK_STACK_SIZE-sizeof(context_t)-3];

	/* 初期化用コンテキストを取得し、ゼロクリア */
	context = (context_t*)&task_stack[tid][TASK_STACK_SIZE-sizeof(context_t)];
	ZeroMemory(context, sizeof(context_t));

	/* exitアドレスの設定 */
	SetReturnAddressToContext(context, API_exitTASK);

	/* コンテキストの設定 */
	context = ((context_t*)&task_stack[tid][TASK_STACK_SIZE-sizeof(context_t)-2]);

	/* 開始アドレス値の設定 */
	SetReturnAddressToContext(context, TaskTable[tid]);
}

