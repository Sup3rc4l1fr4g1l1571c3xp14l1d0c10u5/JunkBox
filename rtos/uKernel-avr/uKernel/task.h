#pragma once
#include "kernel.h"

/**
 * @addtogroup  タスク
 */

/*@{*/

/**
 * @brief               タスクのコンテキストを取得
 * @param   [in] tcb    コンテキストを得たいタスクのタスクコントロールブロック
 * @return              コンテキスト
 */
extern context_t *get_context_from_tcb(tcb_t *tcb);

/**
 * @brief               指定したtidがユーザータスクを示しているか判定する。
 * @param   [in] tid    判定するタスクＩＤ
 * @retval  true        ユーザータスクである
 * @retval  false       ユーザータスクではない
 * @note                ユーザータスクとは、INIT/DIAGを除いたタスクのこと。
 */
extern bool is_user_task(taskid_t tid);

/**
 * @brief               タスクコントロールブロックからタスクIDを得る
 * @param   [in] tcb    IDを得たいタスクのタスクコントロールブロック
 * @return              タスクID
 */
extern taskid_t get_taskid_from_tcb(tcb_t *tcb);

/**
 * @brief                   コンテキストに引数データを設定する
 * @param   [in] ctx        引数データを設定したいコンテキスト
 * @param   [in] argument   設定したい引数データ
 */
extern void set_argument_to_context(context_t* ctx, void *argument);

/**
 * @brief               コンテキストから引数データを取得する
 * @param   [in] ctx    引数データを設定したいコンテキスト
 * @return              設定したい引数データ
 */
extern void *get_argument_from_context(context_t* ctx);

/**
 * @brief               タスクコントロールブロックのリセット
 * @param   [in] taskid     リセットするタスクのＩＤ
 */
extern void reset_tcb(taskid_t taskid);

/*@}*/
