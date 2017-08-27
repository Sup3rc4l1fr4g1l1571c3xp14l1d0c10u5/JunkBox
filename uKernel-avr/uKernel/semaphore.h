#pragma once
#include "kernel.h"

/**
 * @addtogroup  セマフォ
 */

/*@{*/

/**
 * @brief   セマフォキューを初期化する
 */
extern void init_semaphore(void);

/**
 * @brief               指定したタスクをセマフォキューから削除する
 * @param   [in] tcb    削除するタスクのタスクポインタ
 */
extern void unlink_semaphore(tcb_t *tcb);

/**
 * @brief               セマフォを獲得する。獲得できない場合はwait状態になる。
 * @param   [in] sid    獲得するセマフォのＩＤ
 * @retval  SUCCESS     セマフォ獲得成功
 * @retval  ERR10       エラー：獲得したいセマフォのＩＤが範囲外
 * @retval  ERR31       エラー：INIT, DIAGがセマフォを獲得しようとした
 */
extern svcresultid_t take_semaphore(const semaphoreid_t sid);

/**
 * @brief               セマフォを獲得する。獲得できない場合はエラーを戻す。
 * @param   [in] sid    獲得するセマフォのＩＤ
 * @retval  SUCCESS     セマフォ獲得成功
 * @retval  ERR10       エラー：獲得したいセマフォのＩＤが範囲外
 * @retval  ERR11       エラー：セマフォを獲得できなかった
 * @retval  ERR31       エラー：INIT, DIAGがセマフォを獲得しようとした
 */
extern svcresultid_t test_and_set_semaphore(semaphoreid_t sid);

/**
 * @brief               獲得したセマフォを放棄する。
 * @param   [in] sid    放棄するセマフォのＩＤ
 * @retval  SUCCESS     セマフォ放棄成功
 * @retval  ERR10       エラー：放棄したいセマフォのＩＤが範囲外
 * @retval  ERR12       エラー：獲得していないセマフォを放棄しようとした
 */
extern svcresultid_t give_semaphore(semaphoreid_t sid);

/*@}*/
