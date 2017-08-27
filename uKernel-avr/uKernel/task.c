#include "task.h"
#include "kerneldata.h"
#include "systemcall.h"

/**
 * @addtogroup  タスク
 */

/*@{*/

/**
 * @brief               タスクのコンテキストを取得
 * @param   [in] tcb    コンテキストを得たいタスクのタスクコントロールブロック
 * @return              コンテキスト
 */
context_t *get_context_from_tcb(tcb_t *tcb) {
    return (context_t *)(tcb->stack_pointer + 1);
}

/**
 * @brief               指定したtidがユーザータスクを示しているか判定する。
 * @param   [in] tid    判定するタスクＩＤ
 * @retval  true        ユーザータスクである
 * @retval  false       ユーザータスクではない
 * @note                ユーザータスクとは、INIT/DIAGを除いたタスクのこと。
 */
bool is_user_task(taskid_t tid) {
    /* tid が 0 である＝INITである
     * tid が TASK_NUM-1 である＝DIAGである
     * これらより 0 < tid < TASK_NUM-1 ならユーザータスクである
     */
    if ((0 < tid) && (tid < TASK_NUM - 1)) {
        /* ユーザータスクを示している */
        return true;
    } else {
        /* ユーザータスクを示していない */
        return false;
    }
}

/**
 * @brief               タスクコントロールブロックからタスクIDを得る
 * @param   [in] tcb    IDを得たいタスクのタスクコントロールブロック
 * @return              タスクID
 */
taskid_t get_taskid_from_tcb(tcb_t *tcb) {
    return (taskid_t)(tcb - tcbs);
}

/**
 * @brief                   コンテキストに引数データを設定する
 * @param   [in] ctx        引数データを設定したいコンテキスト
 * @param   [in] argument   設定したい引数データ
 */
void set_argument_to_context(context_t* ctx, void *argument) {
    ctx->R25 = (uint8_t)(((uint16_t)argument) >> 8);
    ctx->R24 = (uint8_t)(((uint16_t)argument) & 0xFFU);
}

/**
 * @brief               コンテキストから引数データを取得する
 * @param   [in] ctx    引数データを設定したいコンテキスト
 * @return              設定したい引数データ
 */
void *get_argument_from_context(context_t* ctx) {
    return (void*)(((uintptr_t)ctx->R25 << 8) | (uintptr_t)ctx->R24);
}

/**
 * @brief               タスクコントロールブロックのリセット
 * @param   [in] taskid リセットするタスクのＩＤ
 */
void reset_tcb(taskid_t taskid) {
    const taskentry_t *entry = &task_entries[taskid];

#if false
    /* 以下の実装ではタスクの終了時に return ではなく svc_exit_task を呼ぶ必要がある。
     * svc_exit_task を呼ばずに returnしたり、returnを忘れたまま関数を終了すると、戻り先が設定されていないので暴走する 。
     */
    tcb_t *tcb = &tcbs[taskid];
    tcb->status        = DORMANT;
    tcb->stack_pointer = entry->stack_pointer;
    tcb->pause_msec    = 0;
    tcb->next_task     = NULL;
    tcb->message       = NULL;

    context_t *ctx = get_context_from_tcb(tcb);
    ctx->return_address_high = entry->start_proc.byte[1];
    ctx->return_address_low  = entry->start_proc.byte[0];
    ctx->R1 = 0x00;
#else
    /* 以下の実装ではsvc_exit_taskを呼ばずにreturnしたり、returnを忘れたまま関数を終了しても
     * 戻り先が svc_exit_task になるようにする仕掛けを仕込んでいる。
     */
    tcb_t *tcb = &tcbs[taskid];
    tcb->status        = DORMANT;
    tcb->stack_pointer = entry->stack_pointer - 3;
    tcb->pause_msec    = 0;
    tcb->next_task     = NULL;
    tcb->message       = NULL;

    /* 通常のコンテキストの末尾に戻り先アドレス領域を追加した構造体ポインタを作る */
    struct reset_context_t {
        context_t ctx;          /* 通常のコンテキスト領域 */
        address_t exit_address; /* 戻り先アドレス領域 */
    } *reset_ctx = (struct reset_context_t*)get_context_from_tcb(tcb);
    
    reset_ctx->ctx.address.byte[0] = entry->start_proc.byte[1];
    reset_ctx->ctx.address.byte[1]  = entry->start_proc.byte[0];
    reset_ctx->ctx.R1 = 0x00;
    
    address_t value_svc_exit = { .address = (taskproc_t)svc_exit_task };
    reset_ctx->exit_address.byte[0] = value_svc_exit.byte[1];
    reset_ctx->exit_address.byte[1]  = value_svc_exit.byte[0];

#endif
}

/*@}*/
