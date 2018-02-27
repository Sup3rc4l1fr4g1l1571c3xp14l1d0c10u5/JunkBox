#include "readyqueue.h"
#include "kerneldata.h"

/**
 * @addtogroup  Readyタスクキュー
 */

/*@{*/

/**
 * @brief   readyqueue を初期化する
 */
void init_readyqueue(void) {
    readyqueue.next_task = NULL;
}

/**
 * @brief               タスクをreadyqueueに挿入する
 * @param   [in] tcb    挿入するタスク
 */
void add_task_to_readyqueue(tcb_t *tcb) {

    tcb_t* pT = tcb;

    if (pT == NULL) {
        return;
    }

    tcb_t* next_task = readyqueue.next_task;
    if (next_task == NULL) {
        /* readyqueue に何も登録されていない場合 */
        pT->next_task = readyqueue.next_task;
        readyqueue.next_task = tcb;
    } else if (next_task > tcb) {
        /* 最初の要素の前に挿入するケース */
        pT->next_task = readyqueue.next_task;
        readyqueue.next_task = tcb;
    } else {
        for (;;) {
            tcb_t *beforenid = next_task;
            next_task = next_task->next_task;

            if (next_task == NULL) {
                /* 終端にに達したので、終端に追加 */
                pT->next_task = beforenid->next_task;
                beforenid->next_task = tcb;
                break;
            } else if (next_task == tcb) {
                /* 既に登録済み！！ */
                break;
            } else if (next_task > tcb) {
                /* 挿入する位置が見つかった */
                pT->next_task = beforenid->next_task;
                beforenid->next_task = tcb;
                break;
            } else {
                continue;
            }
        }
    }
}

/**
 * @brief               タスクが readyqueue 中にあれば取り除く
 * @param   [in] tcb    取り除くタスク
 * @retval  false       タスクは readyqueue 中に存在しなかった。
 * @retval  true        タスクは readyqueue 中に存在したので取り除いた。
 * @note                tcbの妥当性は検査しないので注意
 */
bool remove_task_from_readyqueue(tcb_t *tcb) {
    tcb_t *pid = NULL;  /* readyqueue上で指定されたタスク tcb のひとつ前の要素が格納される */

    /* 指定されたタスク tcb が readyqueue 中に存在するか探索を行う */
    for (tcb_t *next_task = readyqueue.next_task; next_task != tcb; next_task = next_task->next_task) {
        if (next_task == NULL) {
            return false;   /* 終端に達した */
        } else {
            pid = next_task;
        }
    }

    /* ここに到達した時点で、以下のことが保障される。
    * ・pid != null の場合：指定されたタスク tcb はreadyqueue上に存在している
    * ・pid == null の場合：readyqueueには一つもタスクが並んでいない
    */

    /* readyqueueから外す */

    if (readyqueue.next_task == tcb) {
        /* 指定されたタスク tcb はキューの先頭に位置している。*/
        readyqueue.next_task = tcb->next_task;  /* readyqueue.next_task に tcb の次のタスクを挿入する */
        tcb->next_task = NULL;
    } else if (pid != NULL) {
        /*
        * キューの先頭ではない場合、探索のループが最低１回は実行されている。
        * つまり、pidの値はNULL以外になっている。
        */
        pid->next_task = tcb->next_task;
        tcb->next_task = NULL;
    } else {
        /* readyqueueには一つもタスクが並んでいないので、取り除きを行う必要がない */
    }

    return true;
}

/**
 * @brief   readyqueue の先頭のタスクを取得する
 * @return  readyqueue の先頭にあるタスク。全てのタスクが休止状態の場合はNULL
 */
tcb_t *dequeue_task_from_readyqueue(void) {
    return readyqueue.next_task;
}

/**
 * @brief   スケジューリング実行
 */
__attribute__((noreturn)) void schedule(void) {
    if (request_reschedule != false) {
        /* readyqueue の先頭をタスクに設定 */
        current_tcb = dequeue_task_from_readyqueue();
        request_reschedule = false;
    }
    /* 選択されたタスクのコンテキストを復帰 */
    RESTORE_CONTEXT();
    /* 割り込みから復帰 */
    asm volatile ("reti");
    for (;;);
}

/*@}*/
