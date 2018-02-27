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
 * @brief	Win32環境でのカーネルタイマ用の仮想デバイス
 * @author	Kazuya Fukuhara
 * @date	2010/09/01 21:36:49	作成
 */

#include "./arch.h"
#include "./virtualhw.h"
#include "./vd_kerneltimer.h"
#include "../../src/kernel.h"
#include <Windows.h>
#include <MMSystem.h>
#pragma comment(lib,"winmm.lib")

/**
 * @brief	カーネルタイマデバイスのメイン処理
 * @author	Kazuya Fukuhara
 * @date	2010/09/01 21:36:49	作成
 */
static void KernelTimerProc(void) {
	for (;;) {
		Sleep(10);
		VHW_onInterrupt(INTID_TIMER);
	}
}

/**
 * @brief	カーネルタイマ割り込みハンドラ
 * @author	Kazuya Fukuhara
 * @date	2010/09/01 21:36:49	作成
 */
void NAKED KernelTimerInterruptServiceRoutine(void) {
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
 * @note	カーネルタイマの仮想デバイスと割り込みルーチンを設定
 * @author	Kazuya Fukuhara
 * @date	2010/09/01 21:36:49	作成
 */
void initKernelTimer(void) {
	/* カーネルタイマを仮想デバイスとしてインストール */
	virtualdevice_t vd;
	vd.name = "KernelTimer";
	vd.vdsr = KernelTimerProc;

	timeBeginPeriod(1);
	VHW_installVirtualDevice(&vd);
	/* カーネルタイマが生成するINTID_TIMERイベントの割り込みルーチンを指定 */
	VHW_setInterruputServiceRoutine(INTID_TIMER, KernelTimerInterruptServiceRoutine); 
}

