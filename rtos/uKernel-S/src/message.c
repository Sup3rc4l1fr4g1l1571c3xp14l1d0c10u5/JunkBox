/*
 *   Redistribution and use in source and binary forms, with or without
 *   modification, are permitted provided that the following conditions are met:
 *
 *   * Redistributions of source code must retain the above copyright
 *     notice, this list of conditions and the following disclaimer.
 *
 *   * Redistributions in binary form must reproduce the above copyright
 *     notice, this list of conditions and the following disclaimer in
 *     the documentation and/or other materials provided with the
 *     distribution.
 *
 *   * Neither the name of the copyright holders nor the names of
 *     contributors may be used to endorse or promote products derived
 *     from this software without specific prior written permission.
 *
 *   THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 *   AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 *   IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 *   ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE
 *   LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
 *   CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
 *   SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 *   INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
 *   CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 *   ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
 *   POSSIBILITY OF SUCH DAMAGE.
 */

/**
 * @file
 * @brief   タスク間メッセージ通信
 * @author  Kazuya Fukuhara
 * @date    2010/01/07 14:08:58	作成
 */

#include "message.h"
#include "syscall.h"

/**
 * @brief	メッセージ領域
 * @author  Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	作成
 */
static message_t	message_table[MESSAGE_NUM];

/**
 * @brief	空きメッセージ領域を管理するリンクリスト
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	作成
 */
static message_t*	free_message_list;

/**
 * @brief	メッセージキューを初期化する
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	作成
 */
void initMessageQueue(void) {
	int i;
	free_message_list = &message_table[0];
	for (i=1; i<MESSAGE_NUM; i++) {
		message_table[i-1].next = &message_table[i];
	}
	message_table[MESSAGE_NUM-1].next = NULL;
}

/**
 * @brief			タスクのメッセージキューから受信したメッセージを取り出す
 * @param[out] from	送信元のタスクのＩＤ
 * @param[out] data	送信されたメッセージの本文
 * @retval SUCCESS	受信成功
 * @retval ERR31	エラー：INITTASKとDIAGTASKがrecvMSGを呼び出した
 * @retval ERR41	エラー：自分宛てのメッセージはなかった（正常終了）
 * @author  		Kazuya Fukuhara
 * @date			2010/01/07 14:08:58	作成
 */
svcresultid_t recvMSG(taskid_t* from, ptr_t* data) {
	message_t *p;
	if ((&INITTASK == currentTCB) || (&DIAGTASK == currentTCB)) {
		/* INITTASKとDIAGTASKがrecvMSGを呼び出した */
		return ERR31;
	}

	/* 自分宛てのメッセージが存在しているか判定 */
	if (currentTCB->message == NULL) {
		/* 自分宛てのメッセージはなかったので正常終了 */
		return ERR41;
	} else {
		/* 自分宛てのメッセージがあったので、受信処理 */
		p = currentTCB->message;
		currentTCB->message = p->next;
		*from = p->from;
		*data = p->data;

		/* 使い終わったメッセージ領域を開放する */
		p->from = 0;
		p->next = free_message_list;
		free_message_list = p;

		return SUCCESS;
	}
}

/**
 * @brief			指定したタスクのメッセージキューにメッセージを送信する
 * @param[in] to	送信先のタスクのＩＤ
 * @param[in] data	送信するメッセージの本文
 * @retval SUCCESS	受信成功
 * @retval ERR1		エラー：不正なタスクIDがtoに指定されている
 * @retval ERR31	エラー：INITTASKとDIAGTASKがsendMSGを呼び出した
 * @retval ERR40	エラー：メール送信失敗（dormant状態のタスクへのメッセージ送信）
 * @retval ERR41	エラー：自分宛てのメッセージはなかった（正常終了）
 * @author  		Kazuya Fukuhara
 * @date			2010/01/07 14:08:58	作成
 */
svcresultid_t sendMSG(taskid_t to, ptr_t data) {
	message_t *msg;
	message_t *p;

	if (to >= TASK_NUM) {
		/* 不正なタスクIDがtoに指定されている */
		return ERR1;
	} else if ((to == 0)||(to == TASK_NUM-1)) {
		/* INITTASKとDIAGTASK宛てのメッセージ送信は禁止 */
		return ERR31;
	} else if ((&INITTASK == currentTCB) || (&DIAGTASK == currentTCB)) {
		/* INITTASKとDIAGTASKがsendMSGを呼び出した */
		return ERR31;
	}

	/* dormant状態のタスクへのメッセージ送信をチェック */
	if (tasks[to].status == DORMANT) {
		return ERR40;
	}

	/* メッセージスロットから空きスロットを取得 */
	msg = free_message_list;
	if (msg == NULL) {
		return ERR40;
	} else {
		free_message_list = free_message_list->next;
		msg->next = NULL;
	}

	/* 送信先のメッセージリストの一番後ろにメッセージを追加 */
	p = tasks[to].message;
	if (p != NULL) {
		while (p->next != NULL) {
			p = p->next;
		}
		p->next = msg;
	} else {
		tasks[to].message = msg;
	}
	
	/* メッセージ情報を構築して書き込む */
	msg->next = NULL;
	msg->from = TCBtoTID(currentTCB); 
	msg->data = data;

	/* もしも、送信先がメッセージ待ち状態ならばタスクを起こす */
	if (tasks[to].status == WAIT_MSG) {
		tasks[to].status = READY;									/* タスクをready状態に設定する */
		removeTaskFromReadyQueue(&tasks[to]);						/* 実行中タスクキューからタスクを取り除く */
		addTaskToReadyQueue(&tasks[to]);							/* タスクを実行中タスクのキューに追加する */
		rescheduling = (&tasks[to] < currentTCB) ? TRUE : FALSE;	/* スケジューリングが必要なら実行する */
	}
	
	return SUCCESS;
}

/**
 * @brief			メッセージを受信するまで待ち状態となる
 * @author  		Kazuya Fukuhara
 * @retval SUCCESS	成功
 * @date			2010/01/07 14:08:58	作成
 */
svcresultid_t waitMSG(void) {
	if (currentTCB->message == NULL) {			/* メッセージを受信していない場合は待ち状態に入る */
		currentTCB->status = WAIT_MSG;			/* タスクの状態をメッセージ待ち状態に変化させる */
		removeTaskFromReadyQueue(currentTCB);	/* 実行中タスクキューからタスクを取り除く */
		rescheduling = TRUE;
	}
	return SUCCESS;
}

/**
 * @brief			指定したタスクのメッセージをすべて開放
 * @param[in] tcb	タスクのタスクポインタ
 * @author  		Kazuya Fukuhara
 * @date			2010/01/07 14:08:58	作成
 */
void unlinkMSG(tcb_t *tcb) {
	/* タスクのメッセージをすべて開放 */
	if ((tcb != NULL) && (tcb->message != NULL)) {
		message_t *p = tcb->message;
		while (p->next) {
			p = p->next;
		}
		p->next = free_message_list;
		free_message_list = tcb->message;
		tcb->message = NULL;
	}
}

