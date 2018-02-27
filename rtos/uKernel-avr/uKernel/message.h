#pragma once
#include "kernel.h"

/**
* @addtogroup メッセージ
*/

/*@{*/

/**
 * @brief   メッセージキューを初期化する
 */
extern void init_message(void);

/**
 * @brief               タスクのメッセージキューから受信したメッセージを取り出す
 * @param   [out] from  送信元のタスクのＩＤ
 * @param   [out] data  送信されたメッセージの本文
 * @retval  SUCCESS     受信成功
 * @retval  ERR31       エラー：INITTASKとDIAGTASKがrecive_messageを呼び出した
 * @retval  ERR42       エラー：自分宛てのメッセージはなかった（正常終了）
 */
extern svcresultid_t recive_message(taskid_t* from, void** data);

/**
 * @brief                   指定したタスクのメッセージキューにメッセージを送信する
 * @param   [in]    to      送信先のタスクのＩＤ
 * @param   [in]    data    送信するメッセージの本文
 * @retval  SUCCESS         受信成功
 * @retval  ERR1            エラー：不正なタスクIDがtoに指定されている
 * @retval  ERR31           エラー：INITTASKとDIAGTASKがsend_messageを呼び出した
 * @retval  ERR40           エラー：dormant状態のタスクへのメッセージ送信
 * @retval  ERR41           エラー：メッセージの空きスロットが無かった
 * @retval  ERR42           エラー：自分宛てのメッセージはなかった（正常終了）
 */
extern svcresultid_t send_message(taskid_t to, void* data);

/**
 * @brief           メッセージを受信するまで待ち状態となる
 * @retval  SUCCESS 成功
 * @retval  ERR31   エラー：INITTASKとDIAGTASKがwait_messageを呼び出した
 */
extern svcresultid_t wait_message(void);

/**
 * @brief               指定したタスクのメッセージをすべて開放
 * @param   [in] tcb    タスクのタスクポインタ
 */
extern void unlink_message(tcb_t *tcb);

/*@}*/
