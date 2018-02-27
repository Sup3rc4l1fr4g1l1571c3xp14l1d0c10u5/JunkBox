#pragma once
#include "kernel.h"

/**
 * @addtogroup  Readyタスクキュー
 */

/*@{*/

/**
 * @brief   readyqueue を初期化する
 */
extern void init_readyqueue(void);

/**
 * @brief               タスクをreadyqueueに挿入する
 * @param   [in] tcb    挿入するタスク
 * @note                tcbの妥当性は検査しないので注意
 */
extern void add_task_to_readyqueue(tcb_t *tcb);

/**
 * @brief               タスクが readyqueue 中にあれば取り除く
 * @param   [in] tcb    取り除くタスク
 * @retval  false       タスクは readyqueue 中に存在しなかった。
 * @retval  true        タスクは readyqueue 中に存在したので取り除いた。
 * @note                tcbの妥当性は検査しないので注意
 */
extern bool remove_task_from_readyqueue(tcb_t *tcb);

/**
 * @brief   readyqueue の先頭のタスクを取得する
 * @return  readyqueue の先頭にあるタスク。全てのタスクが休止状態の場合はNULL
 * @note    tcbの妥当性は検査しないので注意
 */
extern tcb_t *dequeue_task_from_readyqueue(void);

/**
 * @brief   スケジューリング実行
 */
__attribute__((noreturn)) extern void schedule(void);

/*@}*/
