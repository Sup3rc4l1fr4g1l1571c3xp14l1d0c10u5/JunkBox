#include "message.h"
#include "readyqueue.h"
#include "task.h"
#include "kerneldata.h"

/**
 * @addtogroup メッセージ
 */

/*@{*/

/**
 * @brief   メッセージを初期化する
 */
void init_message(void) {
    free_message_list = &messages[0];
    for (int i = 1; i<MESSAGE_NUM; i++) {
        messages[i - 1].next_message = &messages[i];
    }
    messages[MESSAGE_NUM - 1].next_message = NULL;
}

/**
 * @brief               タスクのメッセージキューから受信したメッセージを取り出す
 * @param   [out] from  送信元のタスクのＩＤ
 * @param   [out] data  送信されたメッセージの本文
 * @retval  SUCCESS     受信成功
 * @retval  ERR31       エラー：INITTASKとDIAGTASKがrecive_messageを呼び出した
 * @retval  ERR42       エラー：自分宛てのメッセージはなかった（正常終了）
 */
svcresultid_t recive_message(taskid_t* from, void** data) {
    if (!is_user_task(get_taskid_from_tcb(current_tcb))) {
        /* ユーザータスク以外がrecive_messageを呼び出した */
        return ERR31;
    }

    /* 自分宛てのメッセージが存在しているか判定 */
    if (current_tcb->message == NULL) {
        /* 自分宛てのメッセージはなかったので正常終了 */
        return ERR42;
    } else {
        /* 自分宛てのメッセージがあったので、受信処理 */
        message_t *msg = current_tcb->message;
        current_tcb->message = msg->next_message;
        *from = msg->from_taskid;
        *data = msg->data;

        /* 使い終わったメッセージ領域を開放する */
        msg->from_taskid  = 0;
        msg->next_message = free_message_list;
        free_message_list = msg;

        return SUCCESS;
    }
}

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
svcresultid_t send_message(taskid_t to, void* data) {
    if (to >= TASK_NUM) {
        /* 不正なタスクIDがtoに指定されている */
        return ERR1;
    } else if (is_user_task(to) == false) {
        /* ユーザータスク以外へのメッセージ送信は禁止 */
        return ERR31;
    } else if ((INITTASK == current_tcb) || (DIAGTASK == current_tcb)) {
        /* INITTASKとDIAGTASKがsend_messageを呼び出した */
        return ERR31;
    }

    /* dormant状態のタスクへのメッセージ送信をチェック */
    if (tcbs[to].status == DORMANT) {
        return ERR40;
    }

    /* メッセージスロットから空きスロットを取得 */
    message_t *msg = free_message_list;
    if (msg == NULL) {
        return ERR41;
    } else {
        free_message_list = free_message_list->next_message;
        msg->next_message = NULL;
    }

    /* 送信先のメッセージリストの一番後ろにメッセージを追加 */
    message_t *m = tcbs[to].message;
    if (m != NULL) {
        while (m->next_message != NULL) {
            m = m->next_message;
        }
        m->next_message = msg;
    } else {
        tcbs[to].message = msg;
    }

    /* メッセージ情報を構築して書き込む */
    msg->next_message = NULL;
    msg->from_taskid = get_taskid_from_tcb(current_tcb);
    msg->data = data;

    /* もしも、送信先がメッセージ待ち状態ならばタスクを起こす */
    if (tcbs[to].status == WAIT_MSG) {
        tcbs[to].status = READY;                                        /* タスクをready状態に設定する */
        remove_task_from_readyqueue(&tcbs[to]);                         /* 実行中タスクキューからタスクを取り除く */
        add_task_to_readyqueue(&tcbs[to]);                              /* タスクを実行中タスクのキューに追加する */
        request_reschedule = (&tcbs[to] < current_tcb) ? true : false;  /* スケジューリングが必要なら実行する */
    }

    return SUCCESS;
}

/**
 * @brief           メッセージを受信するまで待ち状態となる
 * @retval  SUCCESS 成功
 * @retval  ERR31   エラー：INITTASKとDIAGTASKがwait_messageを呼び出した
 */
svcresultid_t wait_message(void) {
    if ((INITTASK == current_tcb) || (DIAGTASK == current_tcb)) {
        /* INITTASKとDIAGTASKがwait_messageを呼び出した */
        return ERR31;
    }

    if (current_tcb->message == NULL) {             /* メッセージを受信していない場合は待ち状態に入る */
        current_tcb->status = WAIT_MSG;             /* タスクの状態をメッセージ待ち状態に変化させる */
        remove_task_from_readyqueue(current_tcb);   /* 実行中タスクキューからタスクを取り除く */
        request_reschedule = true;
    }
    return SUCCESS;
}

/**
 * @brief               指定したタスクのメッセージをすべて開放
 * @param   [in] tcb    タスクのタスクポインタ
 */
void unlink_message(tcb_t *tcb) {
    if ((tcb != NULL) && (tcb->message != NULL)) {
        /* タスクのメッセージをすべて開放 */
        message_t *msg = tcb->message;
        while (msg->next_message) {
            msg = msg->next_message;
        }
        msg->next_message = free_message_list;
        free_message_list = tcb->message;
        tcb->message = NULL;
    }
}

/*@}*/
