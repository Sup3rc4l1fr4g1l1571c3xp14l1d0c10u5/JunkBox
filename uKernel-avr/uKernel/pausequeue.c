#include "pausequeue.h"
#include "readyqueue.h"
#include "kerneldata.h"

/**
 * @addtogroup  一時停止タスクキュー
 */

/*@{*/

/**
 * @brief   pausequeue を初期化する
 */
void init_pausequeue(void) {
    pausequeue.next_task = NULL;
}

/**
 * @brief               tcbで示されるタスクを待ち行列 pausequeue に追加する
 * @param   [in] tcb    追加するタスク
 * @param   [in] time   待ち時間(tick単位)
 * @note                tcbの妥当性は検査しないので注意
 * @note                time に 0 を渡した場合は、待ち行列には追加されない
 */
void add_task_to_pausequeue(tcb_t* tcb, msec_t time) {

    tcb_t* next_task = pausequeue.next_task;
    tcb_t* pT = tcb;

    if (time == 0) {
        return;
    } else if (next_task == NULL) {
        /* pausequeueに何も登録されていない場合 */
        pT->next_task = pausequeue.next_task;
        pT->pause_msec = time;
        pausequeue.next_task = tcb;
    } else if ((next_task->pause_msec > time) ||
        ((next_task->pause_msec == time) && (next_task > tcb))) {
        /* 最初の要素の前に挿入するケース */
        pT->next_task = pausequeue.next_task;
        pT->pause_msec = time;
        pausequeue.next_task = tcb;
        next_task->pause_msec -= pT->pause_msec;
    } else {
        /* 最初の要素の後ろに挿入するので、次以降の要素と比較 */
        for (;;) {
            tcb_t* beforenid = next_task;
            time -= next_task->pause_msec;
            next_task = next_task->next_task;

            if (next_task == NULL) {
                /* 終端にに達したので、終端に追加 */
                pT->next_task = beforenid->next_task;
                pT->pause_msec = time;
                beforenid->next_task = tcb;
                break;
            } else if ((next_task->pause_msec > time) || ((next_task->pause_msec == time) && (next_task > tcb))) {
                /* 挿入する位置が見つかった */
                pT->next_task = beforenid->next_task;
                pT->pause_msec = time;
                beforenid->next_task = tcb;
                next_task->pause_msec -= pT->pause_msec;
                break;
            } else {
                continue;
            }
        }
    }
}

/**
 * @brief               tcbで示されるタスクが待ち行列 pausequeue 中にあれば取り除く。
 * @param   [in] tcb    取り除くタスク
 * @retval  false       タスクは pausequeue 中に存在しなかった。
 * @retval  true        タスクは pausequeue 中に存在したので取り除いた。
 * @note                tcbの妥当性は検査しないので注意
 */
bool remove_task_from_pausequeue(tcb_t* tcb) {

    if (tcb == NULL) {
        return false;
    }

    /* 探索 */
    tcb_t* pid = NULL;
    for (tcb_t* next_task = pausequeue.next_task; next_task != tcb; next_task = next_task->next_task) {
        if (next_task == NULL) {
            return false;   /* 終端に達した */
        }
        pid = next_task;
    }

    /* ここに到達した時点で、指定されたタスク tcb はリスト中に存在していることが保証される */

    /* 自分に続くタスクがある場合、pause_countを更新 */
    if (tcb->next_task != NULL) {
        tcb->next_task->pause_msec += tcb->pause_msec;
    }

    /* キューから外す */
    if (pausequeue.next_task == tcb) {
        /* キューの先頭の場合 */
        pausequeue.next_task = tcb->next_task;
    } else if (pid != NULL) {
        /*
         * キューの先頭ではない場合、探索のループが最低１回は実行されている
         * つまり、pidの値はNULL以外になっている
         */
        pid->next_task = tcb->next_task;
    }
    tcb->next_task = NULL;

    return true;
}

/**
 * @brief   待ち状態のタスクの待ち時間を更新する。
 *           待ち時間が経過したタスクはpausequeueから取り出し、readyqueueに挿入する。
 */
void update_pausequeue(void) {

    if (pausequeue.next_task == NULL) {
        request_reschedule = false;
    } else {
        pausequeue.next_task->pause_msec--;
        if (pausequeue.next_task->pause_msec == 0) {
            do {
                /* 取り外す */
                tcb_t *next_task = pausequeue.next_task;
                next_task->status = READY;
                pausequeue.next_task = next_task->next_task;
                next_task->next_task = NULL;
                /* readyqueueに入れる */
                add_task_to_readyqueue(next_task);
                /* 次を見る */
            } while ((pausequeue.next_task != NULL) && (pausequeue.next_task->pause_msec == 0));
            request_reschedule = true;
        } else {
            request_reschedule = false;
        }
    }
}

/*@}*/
