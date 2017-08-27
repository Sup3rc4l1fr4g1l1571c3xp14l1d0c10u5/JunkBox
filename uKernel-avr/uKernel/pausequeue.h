#pragma once
#include "kernel.h"

/**
 * @addtogroup  一時停止タスクキュー
 */

/*@{*/

/**
 * @brief   pausequeue を初期化する
 */
extern void init_pausequeue(void);

/**
 * @brief               tcbで示されるタスクを待ち行列 pausequeue に追加する
 * @param   [in] tcb    追加するタスク
 * @param   [in] time   待ち時間
 * @note                tcbの妥当性は検査しないので注意
 * @note                time に 0 を渡した場合は、待ち行列には追加されない
 */
extern void add_task_to_pausequeue(tcb_t* tcb, msec_t time);

/**
 * @brief               tcbで示されるタスクが待ち行列 pausequeue 中にあれば取り除く。
 * @param   [in] tcb    取り除くタスク
 * @retval  false       タスクは pausequeue 中に存在しなかった。
 * @retval  true        タスクは pausequeue 中に存在したので取り除いた。
 * @note                tcbの妥当性は検査しないので注意
 */
extern bool remove_task_from_pausequeue(tcb_t* tcb);

/**
 * @brief   待ち状態のタスクの待ち時間を更新する
 */
extern void update_pausequeue(void);

/*@}*/
