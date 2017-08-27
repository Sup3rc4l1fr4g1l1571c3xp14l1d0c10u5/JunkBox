#include "semaphore.h"
#include "readyqueue.h"
#include "kerneldata.h"

/**
 * @addtogroup  セマフォ
 */

/*@{*/

/**
 * @brief   セマフォキューを初期化する
 */
void init_semaphore(void) {
    for (int i = 0; i<SEMAPHORE_NUM; i++) {
        semaphorequeue[i].next_task = NULL;
        semaphorequeue[i].take_task = NULL;
    }
}

/**
 * @brief               指定したタスクをセマフォキューから削除する
 * @param   [in] tcb    削除するタスクのタスクポインタ
 */
void unlink_semaphore(tcb_t *tcb) {
    for (int i = 0; i<SEMAPHORE_NUM; i++) {
        if (semaphorequeue[i].take_task != NULL) {
            /* セマフォ[i]を獲得しているタスクがあった */
            if (semaphorequeue[i].take_task != tcb) {
                /* セマフォ[i]を獲得しているのが指定されたタスクでない場合、 */
                /* そのセマフォの待ち行列を末尾までスキャンする */
                tcb_t *next_task = semaphorequeue[i].take_task;
                while (next_task->next_task != NULL) {
                    if (next_task->next_task == tcb) {
                        /* 指定されたタスクがセマフォの待ち行列に並んでいたので待ち行列から外す */
                        next_task->next_task = tcb->next_task;
                        tcb->next_task = NULL;
                        break;
                    } else {
                        next_task = next_task->next_task;
                    }
                }
            } else {
                /* 自分がセマフォ[i]を獲得しているので、セマフォの権利を次のタスクに与える */
                semaphorequeue[i].take_task = semaphorequeue[i].next_task;
                if (semaphorequeue[i].next_task != NULL) {
                    semaphorequeue[i].next_task = semaphorequeue[i].next_task->next_task;
                }
            }
        }
    }
}

/**
 * @brief               セマフォを獲得する。獲得できない場合はwait状態になる。
 * @param   [in] sid    獲得するセマフォのＩＤ
 * @retval  SUCCESS     セマフォ獲得成功
 * @retval  ERR10       エラー：獲得したいセマフォのＩＤが範囲外
 * @retval  ERR31       エラー：INIT, DIAGがセマフォを獲得しようとした
 */
svcresultid_t take_semaphore(const semaphoreid_t sid) {

    /* 検証 */
    if (sid >= SEMAPHORE_NUM - 1) {
        /* 獲得したいセマフォのＩＤが範囲外 */
        return ERR10;
    } else if ((current_tcb == INITTASK) || (current_tcb == DIAGTASK)) {
        /* INIT,DIAGがセマフォを獲得しようとした */
        return ERR31;
    }

    /* セマフォを獲得 */
    if ((INITTASK <= semaphorequeue[sid].take_task) && (semaphorequeue[sid].take_task <= DIAGTASK)) {
        /* セマフォはだれかが獲得済みなので、待ちキューに優先度（タスクＩＤ）順序で並ぶ */
        remove_task_from_readyqueue(current_tcb);
        if ((semaphorequeue[sid].next_task == NULL) || (semaphorequeue[sid].next_task > current_tcb)) {
            /* 待ちキューへの挿入位置が先頭の場合 */
            current_tcb->next_task = semaphorequeue[sid].next_task;
            semaphorequeue[sid].next_task = current_tcb;
        } else {
            /* 待ちキューへの挿入位置が２番目以降の場合 */
            tcb_t* next_task = semaphorequeue[sid].next_task;
            while ((next_task->next_task != NULL) && (next_task->next_task < current_tcb)) {
                next_task = next_task->next_task;
            }
            current_tcb->next_task = next_task->next_task;
            next_task->next_task = current_tcb;
        }
        current_tcb->status = WAIT_SEMA;
        request_reschedule = true;
        return SUCCESS;
    } else {
        /* セマフォの獲得成功 */
        semaphorequeue[sid].take_task = current_tcb;
        request_reschedule = false;
        return SUCCESS;
    }
}

/**
 * @brief               獲得したセマフォを放棄する。
 * @param   [in] sid    放棄するセマフォのＩＤ
 * @retval  SUCCESS     セマフォ放棄成功
 * @retval  ERR10       エラー：放棄したいセマフォのＩＤが範囲外
 * @retval  ERR12       エラー：獲得していないセマフォを放棄しようとした
 */
svcresultid_t give_semaphore(semaphoreid_t sid) {
    if (sid >= SEMAPHORE_NUM - 1) {
        /* 放棄したいセマフォのＩＤが範囲外 */
        request_reschedule = false;
        return ERR10;
    } else {
        if (semaphorequeue[sid].take_task != current_tcb) {
            /* 獲得していないセマフォを放棄しようとした */
            request_reschedule = false;
            return ERR12;
        } else {
            if (semaphorequeue[sid].next_task != NULL) {
                /* セマフォ待ちキューから次のタスクを得る */
                tcb_t* next_task = semaphorequeue[sid].next_task;
                /* セマフォを次のタスクが獲得中にする */
                semaphorequeue[sid].take_task = next_task;
                semaphorequeue[sid].next_task = next_task->next_task;
                /* 次のタスクの状態をreadyにして、readyキューに挿入する */
                next_task->status = READY;
                add_task_to_readyqueue(next_task);
                request_reschedule = true;
                return SUCCESS;
            } else {
                /* 次のタスクがないので、セマフォはだれも獲得していないことにする */
                semaphorequeue[sid].take_task = NULL;
                return SUCCESS;
            }
        }
    }
}

/**
 * @brief               セマフォを獲得する。獲得できない場合はエラーを戻す。
 * @param   [in] sid    獲得するセマフォのＩＤ
 * @retval  SUCCESS     セマフォ獲得成功
 * @retval  ERR10       エラー：獲得したいセマフォのＩＤが範囲外
 * @retval  ERR11       エラー：セマフォを獲得できなかった
 * @retval  ERR31       エラー：INIT, DIAGがセマフォを獲得しようとした
 */
svcresultid_t test_and_set_semaphore(semaphoreid_t sid) {
    if (sid >= SEMAPHORE_NUM - 1) {
        /* 放棄したいセマフォのＩＤが範囲外 */
        return ERR10;
    } else if ((current_tcb == INITTASK) || (current_tcb == DIAGTASK)) {
        /* INIT, DIAGがセマフォを獲得しようとした */
        return ERR31;
    } else {
        /* 獲得 */
        if (semaphorequeue[sid].take_task != NULL) {
            /* すでにセマフォは別のタスクに獲得されている */
            return ERR11;
        } else {
            /* セマフォを獲得 */
            semaphorequeue[sid].take_task = current_tcb;
            semaphorequeue[sid].next_task = NULL;
            return SUCCESS;
        }
    }
}

/*@}*/
