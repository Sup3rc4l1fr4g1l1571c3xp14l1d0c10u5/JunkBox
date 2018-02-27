#include "kernel.h"
#include "semaphore.h"
#include "readyqueue.h"
#include "pausequeue.h"
#include "swi.h"
#include "message.h"
#include "kerneltimer.h"
#include "task.h"
#include "kerneldata.h"

/**
 * @brief   処理を強制停止させる。
 *
 * 割り込み禁止状態に設定した上でプログラムの処理を停止させる。
 */
__attribute__((noreturn)) void halt(void) {
    /* 割り込み禁止状態に設定 */
    cli();

	/* ブレークポイント命令で止める */
    asm volatile("break"::);
	
	/* ブレークポイント命令非対応の場合は無限ループで止める */
    for (;;);
}

/**
 * @brief   カーネル起動
 */
__attribute__((naked, noreturn)) void start_kernel(void) {

    /* 割り込み禁止状態に設定 */
    cli();

    /* ソフトウェア割り込みを初期化 */
    init_swi();

    /* カーネルタイマを初期化 */
    init_kerneltimer();

    /* 全タスクをリセット */
    for (taskid_t i = 0; i < TASK_NUM; i++) {
        reset_tcb(i);
    }

    /* セマフォを初期化 */
    init_semaphore();

    /* メッセージを初期化 */
    init_message();

    /* readyqueue を初期化 */
    init_readyqueue();

    /* pausequeue を初期化 */
    init_pausequeue();

    /* INITタスクを起動状態に設定 */
    INITTASK->status = READY;
    INITTASK->next_task = NULL;
    add_task_to_readyqueue(INITTASK);

    /* DIAGタスクを起動状態に設定 */
    DIAGTASK->status = READY;
    DIAGTASK->next_task = NULL;
    add_task_to_readyqueue(DIAGTASK);

    /* スケジューリングが必要なので request_reschedule に true を設定 */
    request_reschedule = true;

    /* スケジューリングを実行して次のタスクを選択 */
    schedule();
}

