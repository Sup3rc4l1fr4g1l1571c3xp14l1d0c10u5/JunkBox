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
 * @brief	Win32環境で割り込みや周辺機器を再現するための仮想ハードウェア
 * @author	Kazuya Fukuhara
 * @date	2010/09/01 21:36:49	作成
 *
 * Win32環境でタイマなどのペリフェラルや外部デバイスを再現するために仮想ハード
 * ウェアとして実装している。
 * 仮想ハードウェアでは一つのペリフェラル（デバイス）にスレッドを一つ割り当てて
 * 個別に稼働させ、割り込み要求ハンドラでカーネルスレッドに対して割り込みを行い、
 * コンテキストを書き換えることで割り込みを再現している。
 */

#include "./arch.h"
#include "./virtualhw.h"
#include "../../src/type.h"
#include <windows.h>
#include <process.h>

/**
 * @typedef	struct virtualdevice_entry_t virtualdevice_entry_t
 * @struct	virtualdevice_entry_t
 * @brief	仮想ハードウェアに登録されたデバイスの情報
 * @author	Kazuya Fukuhara
 * @date	2010/09/01 21:36:49	作成
 */
typedef struct virtualdevice_entry_t {
	char *name;	/**< デバイス名(デバイスの識別に使われる) */
	virtualdevice_serviceroutine_t vdsr;	/**< デバイスのメイン処理として実行される関数のポインタ */
	unsigned int thread_id;	/**< デバイスに割り当てたスレッドのID */
	HANDLE thread_handle;	/**< デバイスに割り当てたスレッドのハンドル */
} virtualdevice_entry_t;

/**
 * @brief	仮想ハードウェアにインストールされている仮想デバイスのテーブル
 * @author	Kazuya Fukuhara
 * @date	2010/09/01 21:36:49	作成
 */
static virtualdevice_entry_t VirtualDeviceTable[16];

/**
 * @brief			仮想デバイスのメイン処理を呼び出すスレッド関数
 * @param[in] param	実行する仮想デバイスのエントリ(virtualdevice_entry_t*型)
 * @retval == 0		引数が異常
 * @author			Kazuya Fukuhara
 * @date			2010/09/01 21:36:49	作成
 */
static unsigned int __stdcall VirtualDeviceEntryProc(void* param) {
	virtualdevice_entry_t* entry = (virtualdevice_entry_t*)param;
	if (entry) {
		entry->vdsr();
		for (;;) {
		}
	}
	return 0;
}

/**
 * @brief	仮想ハードウェアの割り込み状態を示すフラグ
 * @note	複数のスレッドから読み書きされる
 * @author	Kazuya Fukuhara
 * @date	2010/09/01 21:36:49	作成
 */
static volatile LONG flag_interrupt_lock = 1;

/**
 * @brief	カーネルスレッドのスレッドID
 * @author	Kazuya Fukuhara
 * @date	2010/09/01 21:36:49	作成
 */
static DWORD kernel_thread_id = 0;

/**
 * @brief	カーネルスレッドの「真の」ハンドル値
 * @author	Kazuya Fukuhara
 * @date	2010/09/01 21:36:49	作成
 */
static HANDLE kernel_thread_handle = 0;

/**
 * @brief	カーネルの割り込み禁止状態などを示すクリティカルセクション
 * @author	Kazuya Fukuhara
 * @date	2010/09/01 21:36:49	作成
 */
static CRITICAL_SECTION kernel_cs;

/**
 * @brief	仮想ハードウェアの初期化
 * @author	Kazuya Fukuhara
 * @date	2010/09/01 21:36:49	作成
 * 
 */
void initVirtualHardware(void) {
	int i;

	/* 割り込みフラグの初期化 */
	flag_interrupt_lock = 1;

	/* スレッドIDを取得 */
	kernel_thread_id = GetCurrentThreadId();

	/* クリティカルセクションの初期化（最初はロック状態にしておく） */
	InitializeCriticalSection(&kernel_cs);
	EnterCriticalSection(&kernel_cs);

	/* スレッドの疑似ハンドルから真のハンドルを得る */
	if (DuplicateHandle(GetCurrentProcess(), GetCurrentThread(), GetCurrentProcess(), &kernel_thread_handle, PROCESS_ALL_ACCESS, FALSE, 0) == FALSE) {
		return;
	}

	/* 仮想デバイステーブルを初期化 */
	for (i=0; i<countof(VirtualDeviceTable); i++) {
		VirtualDeviceTable[i].thread_handle = (HANDLE)-1;
		VirtualDeviceTable[i].name = "";
	}
}

/**
 * @brief			仮想デバイスのインストール
 * @param[in] vd	インストールする仮想デバイス
 * @author			Kazuya Fukuhara
 * @date			2010/09/01 21:36:49	作成
 */
void VHW_installVirtualDevice(virtualdevice_t* vd) {
	int i;
	virtualdevice_entry_t *entry = NULL;

	/* カーネルスレッドからの呼び出しのみを許可 */
	if (GetCurrentThreadId() != kernel_thread_id) {
		return;
	}

	/* 同名のデバイスが登録されている場合はインストールしない */
	for (i=0; i<countof(VirtualDeviceTable); i++) {
		if ((VirtualDeviceTable[i].name != NULL) && (strcmp(VirtualDeviceTable[i].name, vd->name) == 0)) {
			return;
		}
	}

	/* テーブルの空きを探す */
	for (i=0; i<countof(VirtualDeviceTable); i++) {
		if (VirtualDeviceTable[i].thread_handle == (HANDLE)-1) {
			entry = &VirtualDeviceTable[i];
			break;
		}
	}
	if (entry == NULL) {
		return;
	}

	/* 仮想デバイスの動作スレッドを停止状態で作る */
	entry->thread_handle = (HANDLE)_beginthreadex( NULL, 0, VirtualDeviceEntryProc, (void*)entry, CREATE_SUSPENDED, &entry->thread_id);
	if (entry->thread_handle == (HANDLE)-1) {
		return;
	}

	/* テーブルにエントリを追加する */
	entry->name = vd->name;
	entry->vdsr = vd->vdsr;

}

/**
 * @brief	仮想ハードウェアの動作を開始する
 * @note	インストールした仮想デバイスのスレッドの動作を開始する
 * @author	Kazuya Fukuhara
 * @date	2010/09/01 21:36:49	作成
 */
void startVirtualHardware(void) {
	int i;
	for (i=0; i<countof(VirtualDeviceTable); i++) {
		if (VirtualDeviceTable[i].name[0] != -1) {
			ResumeThread(VirtualDeviceTable[i].thread_handle);
		}
	}
}

/**
 * @brief	仮想ハードウェアの割り込みベクタ
 * @note	仮想ハードウェアに割り込みが発生した場合、ここに記載されている割り込みルーチンがカーネルスレッドで実行されるように仕向けられる。
 * @author	Kazuya Fukuhara
 * @date	2010/09/01 21:36:49	作成
 */
static interruptserviceroutine_t InterruptVector[INTID_MAX];

/**
 * @def		IsValideInterruptId
 * @brief	割り込み番号が妥当か検証
 * @author	Kazuya Fukuhara
 * @date	2010/09/01 21:36:49	作成
 */
#define IsValideInterruptId(x) ((INTID_TIMER <= (x))&&((x) < INTID_MAX))

/**
 * @brief			カーネル側の割り込みサービスルーチンを仮想ハードウェアの割り込みベクタに設定
 * @param[in] id	仮想ハードウェアの割り込み番号
 * @param[in] isr	仮想ハードウェアの割り込み番号に対応させるカーネル側の割り込みサービスルーチン
 * @author			Kazuya Fukuhara
 * @date			2010/09/01 21:36:49	作成
 */
void VHW_setInterruputServiceRoutine(vhw_interruptid_t id, interruptserviceroutine_t isr) {
	if (!IsValideInterruptId(id)) {
		return;
	}
	InterruptVector[id] = isr;
}

/**
 * @brief			仮想ハードウェアの割り込みベクタからカーネル側の割り込みサービスルーチンを取得
 * @param[in] id	割り込み番号
 * @return			割り込み番号に対応する割り込みルーチン NULLの場合は未設定
 * @author			Kazuya Fukuhara
 * @date			2010/09/01 21:36:49	作成
 */
interruptserviceroutine_t VHW_getInterruputServiceRoutine(vhw_interruptid_t id) {
	if (!IsValideInterruptId(id)) {
		return NULL;
	}
	return InterruptVector[id];
}

/**
 * @brief			仮想ハードウェアに割り込みを発生させる（本体）
 * @param[in] id	発生させる割り込み番号
 * @author			Kazuya Fukuhara
 * @date			2010/09/01 21:36:49	作成
 */
static bool_t VHW_onInterruptBody(vhw_interruptid_t id) {
	CONTEXT ctx;	/* 4byte アラインメントで確保されることを期待 */
	DWORD pc;
	HANDLE prc;
	BOOL ret;

	if (!IsValideInterruptId(id)) {
		return FALSE;
	}
	if (InterruptVector[id] == NULL) {
		return FALSE;
	}
	/* カーネルスレッドを一時停止する */
	if (SuspendThread(kernel_thread_handle) == -1) {
		/* サスペンド失敗（致命的）*/
		return FALSE;
	}
	/* スレッドのコンテキストを取得*/
	ctx.ContextFlags = CONTEXT_FULL;
	if (GetThreadContext( kernel_thread_handle, &ctx ) == FALSE) {
		/* 失敗（致命的） */
		goto FAILED;
	}
	pc = ctx.Eip;							/* スレッドの現在のプログラムカウンタを取得 */
	ctx.Eip = (DWORD)VHW_getInterruputServiceRoutine(id);	/*プログラムカウンタを割り込みルーチンの開始位置に設定 */

	/* スタックの値を書き換えるために、疑似ハンドルから正規のハンドルを生成する */
	if (DuplicateHandle(GetCurrentProcess(), GetCurrentProcess(), GetCurrentProcess(), &prc, PROCESS_ALL_ACCESS, FALSE, 0) == FALSE) {
		goto FAILED;
	}

	/* スタックポインタをずらす */
	ctx.Esp -= sizeof(DWORD);

	/* 空けたスタック位置に割り込みルーチンからの戻り先アドレスを書き込む */
	ret = WriteProcessMemory(prc, (LPVOID)ctx.Esp, (LPVOID)&pc, (SIZE_T)sizeof(pc), (SIZE_T*)NULL);

	/* 正規のハンドルを解放 */
	CloseHandle(prc);

	if (ret == FALSE) {
		goto FAILED;
	}

	/* スレッドのコンテキストを書き換える */
	ctx.ContextFlags = CONTEXT_FULL;
	if (SetThreadContext( kernel_thread_handle, &ctx ) == FALSE) {
		/* スレッドコンテキスト書き換え失敗 */
		goto FAILED;
	}

	/* カーネルスレッドを再開する */
	ResumeThread(kernel_thread_handle);

	return TRUE;

FAILED:
	/* カーネルスレッドを再開する */
	ResumeThread(kernel_thread_handle);

	return FALSE;
}

/**
 * @brief			仮想ハードウェアに割り込の発生を伝達する（前処理＋後処理）
 * @param[in] id	発生させる割り込み番号
 * @author			Kazuya Fukuhara
 * @date			2010/09/01 21:36:49	作成
 */
void VHW_onInterrupt(vhw_interruptid_t id) {
	while (InterlockedCompareExchange(&flag_interrupt_lock, 1, 0) != 0) {
		Sleep(0);
	}
	EnterCriticalSection(&kernel_cs);
	if (VHW_onInterruptBody(id) == FALSE) {
		VHW_enableInterrupt();
	}
}

/**
 * @brief	割り込みを禁止する
 * @author	Kazuya Fukuhara
 * @date	2010/09/01 21:36:49	作成
 */
void VHW_disableInterrupt(void) {
	while (InterlockedCompareExchange(&flag_interrupt_lock, 1, 0) != 0) {
	}
	EnterCriticalSection(&kernel_cs);
}

/**
 * @brief	割り込み禁止解除
 * @author	Kazuya Fukuhara
 * @date	2010/09/01 21:36:49	作成
 */
void VHW_enableInterrupt(void) {
	InterlockedCompareExchange(&flag_interrupt_lock, 0, 1);
	LeaveCriticalSection(&kernel_cs);
}

