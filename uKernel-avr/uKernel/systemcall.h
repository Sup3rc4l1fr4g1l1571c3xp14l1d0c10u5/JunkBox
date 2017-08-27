#pragma once
#include "kernel.h"

/**
 * @addtogroup  システムコール
 */

/*@{*/

/**
 * @brief                   指定したタスクを開始する
 * @param   [in] taskid     開始するタスクのID
 * @param   [in] argument   タスクに渡す引数
 * @retval  SUCCESS         指定したタスクの開始に成功
 * @retval  ERR1            ユーザータスク以外、もしくは呼び出し元自身が指定された
 * @retval  ERR2            DORMANT状態以外のタスクを起動しようとした
 */
extern svcresultid_t svc_start_task(taskid_t taskid, void* argument);

/**
 * @brief               自タスクを終了する
 * @retval  SUCCESS     自タスクの終了に成功
 * @retval  ERR3        ready 状態でないタスクを exit させようとした
 * @retval  ERR31       DIAGタスクが par->taskid に指定されている
 */
extern svcresultid_t svc_exit_task(void);

/**
 * @brief               自タスクを一時停止する
 * @param   [in] msec   停止時間(0は無期限の停止)
 * @retval  SUCCESS     自タスクの一時停止に成功
 * @retval  ERR4        ready 状態でないタスクを pause させようとした
 * @retval  ERR31       INITタスクもしくはDIAGタスクががpar->taskidに指定されている
 */
extern svcresultid_t svc_pause_task(msec_t msec);

/**
 * @brief               PAUSE状態のタスクを再開する
 * @param   [in] taskid システムコール呼び出しの引数が格納された構造体へのポインタ
 * @retval  SUCCESS     指定されたタスクの再開に成功
 * @retval  ERR1        不正なタスクIDがtaskidに指定されている
 * @retval  ERR5        taskid に指定されたタスクはPAUSE状態ではない
 * @retval  ERR31       DIAGタスクが taskid に指定されている
 * @note                セマフォ待ち状態などPAUSE状態ではないタスクはresumeTASKできない
 */
extern svcresultid_t svc_resume_task(taskid_t taskid);

/**
 * @brief               自タスクをリスタートする
 * @param   [in] msec   再開するまでの時間（tick単位）
 * @retval  SUCCESS     リスタート成功
 * @retval  ERR6        msecに0が指定された
 * @note                msecを0に設定すること（無限休眠）はできない
 * @note                長期間休眠(msec > 255)には割り込みなど別の要素を使うこと。
 */
extern svcresultid_t svc_restart_task(msec_t msec);

/**
 * @brief               自タスクのタスクＩＤを取得する
 * @param   [out] ptid  自タスクのタスクＩＤが格納される領域
 * @retval  SUCCESS     タスクＩＤ取得獲得成功
 */
extern svcresultid_t svc_get_taskid(taskid_t *ptid);

/**
 * @brief               セマフォを獲得する。獲得できない場合はwait状態になる。
 * @param   [in] sid    獲得するセマフォのＩＤ
 * @retval  SUCCESS     セマフォ獲得成功
 * @retval  ERR10       エラー：獲得したいセマフォのＩＤが範囲外
 * @retval  ERR31       エラー：INIT, DIAGがセマフォを獲得しようとした
 */
extern svcresultid_t svc_take_semaphore(semaphoreid_t sid);

/**
 * @brief               獲得したセマフォを放棄する
 * @param   [in] sid    放棄するセマフォのＩＤ
 * @retval  SUCCESS     セマフォ放棄成功
 * @retval  ERR10       エラー：放棄したいセマフォのＩＤが範囲外
 * @retval  ERR12       エラー：獲得していないセマフォを放棄しようとした
 */
extern svcresultid_t svc_give_semaphore(semaphoreid_t sid);

/**
 * @brief               セマフォの獲得を試みる。
 * @param   [in] sid    獲得を試みるセマフォのＩＤ
 * @retval  SUCCESS     セマフォ獲得成功
 * @retval  ERR10       エラー：獲得したいセマフォのＩＤが範囲外
 * @retval  ERR11       エラー：セマフォを獲得できなかった
 * @retval  ERR31       エラー：INIT, DIAGがセマフォを獲得しようとした
 * @note                takeSEMAと違い獲得に失敗した場合は制御が戻る
 */
extern svcresultid_t svc_test_and_set_semaphore(semaphoreid_t sid);

/**
 * @brief               タスクのメッセージキューから受信したメッセージを取り出す
 * @param   [out] from  送信元のタスクのＩＤ
 * @param   [out] data  送信されたメッセージの本文
 * @retval  SUCCESS     受信成功
 * @retval  ERR31       エラー：INITTASKとDIAGTASKがrecvMSGを呼び出した
 * @retval  ERR42       エラー：自分宛てのメッセージはなかった（正常終了）
 */
extern svcresultid_t svc_recive_message(taskid_t* from, void ** data);

/**
 * @brief                   指定したタスクのメッセージキューにメッセージを送信する
 * @param   [in]    to      送信先のタスクのＩＤ
 * @param   [in]    data    送信するメッセージの本文
 * @retval  SUCCESS         受信成功
 * @retval  ERR1            エラー：不正なタスクIDがtoに指定されている
 * @retval  ERR31           エラー：INITTASKとDIAGTASKがsendMSGを呼び出した
 * @retval  ERR40           エラー：dormant状態のタスクへのメッセージ送信
 * @retval  ERR41           エラー：メッセージの空きスロットが無かった
 * @retval  ERR42           エラー：自分宛てのメッセージはなかった（正常終了）
 */
extern svcresultid_t svc_send_message(taskid_t to, void *data);

/**
 * @brief               メッセージを受信するまで待ち状態となる
 * @retval  SUCCESS     メッセージ受信成功
 * @retval  ERR31       エラー：INITTASKとDIAGTASKがwaitMSGを呼び出した
 */
extern svcresultid_t svc_wait_message(void);

/**
 * @brief                       外部割り込み発生時に指定したタスクを起動する
 * @param   [in] interrupt_id   割り込み番号
 * @param   [in] taskid         割り込みで起動するタスクのID
 * @param   [in] argument       割り込みで起動するタスクのパラメータ
 * @retval  SUCCESS             指定した割り込みのフックに成功
 * @retval  ERR1                ユーザータスク以外が指定された
 * @retval  ERR27               利用できない外部割り込みが指定された
 */
extern svcresultid_t svc_hook_extint(extintid_t interrupt_id, taskid_t taskid, void *argument);

/**
 * @brief                       外部割り込み発生時のタスク起動を解除する
 * @param   [in] interrupt_id   割り込み番号
 * @retval  SUCCESS             指定した割り込みのタスク起動解除に成功
 * @retval  ERR27               利用できない外部割り込みが指定された
 */
extern svcresultid_t svc_unhook_extint(extintid_t interrupt_id);

/**
 * @brief システムコール処理
 */
__attribute__((noreturn))
extern void syscall_entry(void);

/*@}*/
