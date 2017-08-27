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
 * @brief   強制停止
 */
__attribute__((noreturn)) void halt(void) {
    asm volatile("break"::);
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

    /* INITタスクを起動 */
    INITTASK->status = READY;
    INITTASK->next_task = NULL;
    add_task_to_readyqueue(INITTASK);

    /* DIAGタスクを起動 */
    DIAGTASK->status = READY;
    DIAGTASK->next_task = NULL;
    add_task_to_readyqueue(DIAGTASK);

    /* スケジューリングが必要なので request_reschedule に true を設定 */
    request_reschedule = true;

    /* スケジューリングを実行して次のタスクを選択 */
    schedule();
}

