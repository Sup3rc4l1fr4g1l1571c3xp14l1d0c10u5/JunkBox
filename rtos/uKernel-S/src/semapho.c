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
 * @brief   セマフォ
 * @author  Kazuya Fukuhara
 * @date    2010/01/07 14:08:58	作成
 * @date	2010/09/09 12:48:22	タイムシェアリングスケジューリングを追加
 * @date	2010/09/10 10:57:13	優先度付きタイムシェアリングスケジューリングを追加
 *
 * セマフォといいつつ一つしか入室を許さないので実質クリティカルセクションです。
 */

#include "semapho.h"
#include "syscall.h"

/**
 * @brief	セマフォキュー
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	作成
 *
 * 各セマフォ毎の待ち行列（優先度付きキュー）
 */
static semaphoqueue_t semaphoqueue[SEMAPHO_NUM];

/**
 * @brief	セマフォキューを初期化する
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	作成
 */
void initSemaphoQueue(void) {
	int i;
	for (i=0; i<SEMAPHO_NUM; i++) {
		semaphoqueue[i].next_task = NULL;
		semaphoqueue[i].take_task = NULL;
	}
}

/**
 * @brief			指定したタスクをセマフォキューから削除する
 * @param[in] tcb	削除するタスクのタスクポインタ
 * @author  		Kazuya Fukuhara
 * @date			2010/01/07 14:08:58	作成
 * 全セマフォ待ち要素から検索するのは判りづらい＆不毛なので再考すべし
 */
void unlinkSEMA(tcb_t *tcb) {
	int i;

	for (i=0; i<SEMAPHO_NUM; i++) {
		if (semaphoqueue[i].take_task != NULL) {
			/* セマフォ[i]を獲得しているタスクがあった */
			if (semaphoqueue[i].take_task != tcb) {
				/* セマフォ[i]を獲得しているのが指定されたタスクでない場合、 */
				/* そのセマフォの待ち行列を末尾までスキャンする */
				tcb_t *next_task = semaphoqueue[i].take_task;
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
				semaphoqueue[i].take_task = semaphoqueue[i].next_task;
				if (semaphoqueue[i].next_task != NULL) {
					semaphoqueue[i].next_task = semaphoqueue[i].next_task->next_task;
				}
			}
		}
	}
}

/**
 * @brief			セマフォを獲得する。獲得できない場合はwait状態になる。
 * @param[in] sid	獲得するセマフォのＩＤ
 * @retval SUCCESS	セマフォ獲得成功
 * @retval ERR10	エラー：獲得したいセマフォのＩＤが範囲外
 * @retval ERR31	エラー：INIT, DIAGがセマフォを獲得しようとした
 * @author  		Kazuya Fukuhara
 * @date			2010/01/07 14:08:58	作成
 */
svcresultid_t takeSEMA(const semaphoid_t sid) {

	/* 検証 */
	if (sid >= SEMAPHO_NUM-1) {
		/* 獲得したいセマフォのＩＤが範囲外 */
		return ERR10;
	} else if ((currentTCB == &(INITTASK))||(currentTCB == &(DIAGTASK))) {
		/* INIT,DIAGがセマフォを獲得しようとした */
		return ERR31;
	}

	/* 獲得 */
	if ((&(tasks[1]) <= semaphoqueue[sid].take_task) && (semaphoqueue[sid].take_task <= &(tasks[TASK_NUM-2]))) {
#if (SCHEDULER_TYPE == SCHEDULING_PRIORITY_BASE)
		/* セマフォはだれかが獲得済みなので、待ちキューに優先度（タスクＩＤ）順序で並ぶ */
		removeTaskFromReadyQueue(currentTCB);
		if ((semaphoqueue[sid].next_task == NULL)||(semaphoqueue[sid].next_task > currentTCB)) {
			/* 待ちキューへの挿入位置が先頭の場合 */
			currentTCB->next_task = semaphoqueue[sid].next_task;
			semaphoqueue[sid].next_task = currentTCB;
		} else {
			/* 待ちキューへの挿入位置が２番目以降の場合 */
			tcb_t* next_task = semaphoqueue[sid].next_task;
			while ((next_task->next_task != NULL) && (next_task->next_task < currentTCB)) {
				next_task = next_task->next_task;
			}
			currentTCB->next_task = next_task->next_task;
			next_task->next_task = currentTCB;
		}
#elif (SCHEDULER_TYPE == SCHEDULING_TIME_SHARING)
		/* セマフォはだれかが獲得済みなので、待ちキューの一番後ろに並ぶ */
		removeTaskFromReadyQueue(currentTCB);
		if (semaphoqueue[sid].next_task == NULL) {
			/* 待ちキューに要素がない場合は先頭に追加 */
			currentTCB->next_task = semaphoqueue[sid].next_task;
			semaphoqueue[sid].next_task = currentTCB;
		} else {
			/* 待ちキューに要素がある場合は末尾要素まで辿って追加 */
			tcb_t* next_task = semaphoqueue[sid].next_task;
			while (next_task->next_task != NULL) {
				next_task = next_task->next_task;
			}
			currentTCB->next_task = next_task->next_task;
			next_task->next_task = currentTCB;
		}
#elif (SCHEDULER_TYPE == SCHEDULING_PRIORITY_BASE_TIME_SHARING)
		/* セマフォはだれかが獲得済みなので、待ちキューに優先度（タスクに割り当てられた優先度）順序で並ぶ */
		removeTaskFromReadyQueue(currentTCB);
		if ((semaphoqueue[sid].next_task == NULL)||(semaphoqueue[sid].next_task->task_priolity > currentTCB->task_priolity)) {
			/* 待ちキューへの挿入位置が先頭の場合 */
			currentTCB->next_task = semaphoqueue[sid].next_task;
			semaphoqueue[sid].next_task = currentTCB;
		} else {
			/* 待ちキューへの挿入位置が２番目以降の場合 */
			tcb_t* next_task = semaphoqueue[sid].next_task;
			while ((next_task->next_task != NULL) && (next_task->next_task->task_priolity < currentTCB->task_priolity)) {
				next_task = next_task->next_task;
			}
			currentTCB->next_task = next_task->next_task;
			next_task->next_task = currentTCB;
		}
#else
#error "Scheduler type is undefined."
#endif
		currentTCB->status = WAIT_SEMA;
		rescheduling = TRUE;
		return SUCCESS;
	} else {
		/* セマフォの獲得成功 */
		semaphoqueue[sid].take_task = currentTCB;
		rescheduling = FALSE;
		return SUCCESS;
	}
}

/**
 * @brief			獲得したセマフォを放棄する。
 * @param[in] sid	放棄するセマフォのＩＤ
 * @retval SUCCESS	セマフォ放棄成功
 * @retval ERR10	エラー：放棄したいセマフォのＩＤが範囲外
 * @retval ERR12	エラー：獲得していないセマフォを放棄しようとした
 * @author  		Kazuya Fukuhara
 * @date			2010/01/07 14:08:58	作成
 */
svcresultid_t giveSEMA(semaphoid_t sid) {
	if (sid >= SEMAPHO_NUM-1) {
		/* 放棄したいセマフォのＩＤが範囲外 */
		rescheduling = FALSE;
		return ERR10;
	} else {
		if (semaphoqueue[sid].take_task != currentTCB) {
			/* 獲得していないセマフォを放棄しようとした */
			rescheduling = FALSE;
			return ERR12;
		} else {
			if (semaphoqueue[sid].next_task != NULL) {
				/* セマフォ待ちキューから次のタスクを得る */
				tcb_t* next_task = semaphoqueue[sid].next_task;
				/* セマフォを次のタスクが獲得中にする */
				semaphoqueue[sid].take_task = next_task;
				semaphoqueue[sid].next_task = next_task->next_task;
				/* 次のタスクの状態をreadyにして、readyキューに挿入する */
				next_task->status = READY;
				addTaskToReadyQueue(next_task);
				rescheduling = TRUE;
				return SUCCESS;
			} else {
				/* 次のタスクがないので、セマフォはだれも獲得していないことにする */
				semaphoqueue[sid].take_task = NULL;
				return SUCCESS;
			}
		}
	}
}

/**
 * @brief			セマフォを獲得する。獲得できない場合はエラーを戻す。
 * @param[in] sid	獲得するセマフォのＩＤ
 * @retval SUCCESS	セマフォ獲得成功
 * @retval ERR10	エラー：獲得したいセマフォのＩＤが範囲外
 * @retval ERR11	エラー：セマフォを獲得できなかった
 * @retval ERR31	エラー：INIT, DIAGがセマフォを獲得しようとした
 * @author  		Kazuya Fukuhara
 * @date    		2010/01/07 14:08:58	作成
 */
svcresultid_t tasSEMA(semaphoid_t sid) {
	if (sid >= SEMAPHO_NUM-1) {
		/* 放棄したいセマフォのＩＤが範囲外 */
		return ERR10;
	} else if ((currentTCB == &(INITTASK))||(currentTCB == &(DIAGTASK))) {
		/* INIT, DIAGがセマフォを獲得しようとした */
		return ERR31;
	} else {
		/* 獲得 */
		if (semaphoqueue[sid].take_task != NULL) {
			/* すでにセマフォは別のタスクに獲得されている */
			return ERR11;
		} else {
			/* セマフォを獲得 */
			semaphoqueue[sid].take_task = currentTCB;
			semaphoqueue[sid].next_task = NULL;
			return SUCCESS;
		}
	}

}

