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
 * @brief   カーネルコア
 * @author  Kazuya Fukuhara
 * @date    2010/01/07 14:08:58	作成
 * @date	2010/08/15 16:11:02	機種依存部分を分離
 * @date	2010/09/09 12:48:22	タイムシェアリングスケジューリングを追加
 * @date	2010/09/10 10:57:13	優先度付きタイムシェアリングスケジューリングを追加
 * @date	2010/09/11 11:01:20	外部割り込みでタスクを起動する機能を実装
 *								exit/restartシステムコールでスタックフレームを破壊してしまう不具合を修正
 */

#include "arch.h"
#include "config.h"
#include "type.h"
#include "kernel.h"
#include "syscall.h"
#include "semapho.h"
#include "message.h"


/**
 * @brief	タスクコントロールブロック配列
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	作成
 */
global tcb_t	tasks[TASK_NUM];

/**
 * @brief	タスクの実行時スタック領域
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	作成
 */
global uint8_t	task_stack[TASK_NUM][TASK_STACK_SIZE];

/**
 * @brief	現在実行中のタスクのタスクコントロールブロック
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	作成
 */
global tcb_t	*currentTCB;

/**
 * @brief	スケジューリング要求フラグ
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	作成
 */
global bool_t	rescheduling;

/**
 * @brief	ready状態のタスクを並べるキュー
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	作成
 */
global readyqueue_t readyqueue;

/**
 * @brief				指定したtidがユーザータスクを示しているか判定する。
 * @author				Kazuya Fukuhara
 * @param	[in] tid	判定するタスクＩＤ
 * @retval	TRUE		ユーザータスクである
 * @retval	FALSE		ユーザータスクではない
 * @date				2010/12/03 20:31:26	作成
 *
 * ユーザータスクとは、INIT/DIAGを除いたタスクのこと。
 */
global bool_t isUserTask(taskid_t tid) {
	/* tid が 0 である＝INITである 
	 * tid が TASK_NUM-1 である＝DIAGである
	 * これらより 0 < tid < TASK_NUM-1 ならユーザータスクである
	 */
	if ((0 < tid) && (tid < TASK_NUM-1)) {
		/* ユーザータスクを示している */
		return TRUE;
	} else {
		/* ユーザータスクを示していない */
		return FALSE;
	}
}

/**
 * @brief			タスクをreadyqueueに挿入する
 * @param [in] tcb	挿入するタスク
 * @note			tcbの妥当性は検査しないので注意
 * @author			Kazuya Fukuhara
 * @date			2010/01/07 14:08:58	作成
 * @date			2010/09/09 12:48:22	タイムシェアリングスケジューリングを追加
 * @date			2010/09/10 10:57:13	優先度付きタイムシェアリングスケジューリングを追加
 */
global void addTaskToReadyQueue (tcb_t *tcb) {

	tcb_t* next_task;
	tcb_t* pT = tcb;
		
	if (pT == NULL) {
		return;
	}

	next_task = readyqueue.next_task;
#if (SCHEDULER_TYPE == SCHEDULING_PRIORITY_BASE)
	if (next_task == NULL) {
		/* readyqueue に何も登録されていない場合 */
		pT->next_task = readyqueue.next_task;
		readyqueue.next_task = tcb;
	} else if (next_task > tcb) {
		/* 最初の要素の前に挿入するケース */
		pT->next_task = readyqueue.next_task;
		readyqueue.next_task = tcb;
	} else {
		/* 次以降の要素と比較 */
		tcb_t* beforenid;
		for (;;) {
			beforenid = next_task;
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
#elif (SCHEDULER_TYPE == SCHEDULING_TIME_SHARING)
	if (next_task == NULL) {
		/* readyqueue に何も登録されていない場合 */
		pT->next_task = readyqueue.next_task;
		readyqueue.next_task = tcb;
	} else {
		/* 末尾に追加 */
		tcb_t* beforenid;
		for (;;) {
			beforenid = next_task;
			next_task = next_task->next_task;
			
			if (next_task == NULL) {
				/* 終端にに達したので、終端に追加 */
				pT->next_task = beforenid->next_task;
				beforenid->next_task = tcb;
				break;
			} else if (next_task == tcb) {
				/* 既に登録済み！！ */
				break;
			} else {
				continue;
			}
		}
	}
#elif (SCHEDULER_TYPE == SCHEDULING_PRIORITY_BASE_TIME_SHARING) 
	if (next_task == NULL) {
		/* readyqueue に何も登録されていない場合 */
		pT->next_task = readyqueue.next_task;
		readyqueue.next_task = tcb;
	} else if (next_task->task_priolity > tcb->task_priolity) {
		/* 最初の要素の前に挿入するケース */
		pT->next_task = readyqueue.next_task;
		readyqueue.next_task = tcb;
	} else {
		/* 次以降の要素と比較 */
		tcb_t* beforenid;
		for (;;) {
			beforenid = next_task;
			next_task = next_task->next_task;
			
			if (next_task == NULL) {
				/* 終端にに達したので、終端に追加 */
				pT->next_task = beforenid->next_task;
				beforenid->next_task = tcb;
				break;
			} else if (next_task == tcb) {
				/* 既に登録済み！！ */
				break;
			} else if (next_task->task_priolity > tcb->task_priolity) {
				/* 挿入する位置が見つかった */
				pT->next_task = beforenid->next_task;
				beforenid->next_task = tcb;
				break;
			} else {
				continue;
			}
		}
	}
#else
#error "Scheduler type is undefined."
#endif
}

/**
 * @brief			タスクが readyqueue 中にあれば取り除く
 * @param  [in] tcb	取り除くタスク
 * @retval false	タスクは readyqueue 中に存在しなかった。
 * @retval true		タスクは readyqueue 中に存在したので取り除いた。
 * @note			tcbの妥当性は検査しないので注意
 * @author			Kazuya Fukuhara
 * @date			2010/01/07 14:08:58	作成
 */
global bool_t removeTaskFromReadyQueue(tcb_t *tcb) {
	tcb_t *pid = NULL;	/* readyqueue上で指定されたタスク tcb のひとつ前の要素が格納される */

	/* 指定されたタスク tcb が readyqueue 中に存在するか探索を行う */
	{
		tcb_t *next_task;
		for (next_task = readyqueue.next_task; next_task != tcb; next_task = next_task->next_task) {
			if (next_task == NULL) {
				return FALSE;	/* 終端に達した */
			} else {
				pid = next_task;
			}
		}
	}

	/* ここに到達した時点で、以下のことが保障される。
	 * ・pid != null の場合：指定されたタスク tcb はreadyqueue上に存在している
	 * ・pid == null の場合：readyqueueには一つもタスクが並んでいない
	 */

	/* readyqueueから外す */

	if (readyqueue.next_task == tcb) {
		/* 指定されたタスク tcb はキューの先頭に位置している。*/
		readyqueue.next_task = tcb->next_task;	/* readyqueue.next_task に tcb の次のタスクを挿入する */
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

	return TRUE;
}

/**
 * @brief  wait状態のタスクを並べるキュー
 * @author Kazuya Fukuhara
 * @date   2010/01/07 14:08:58	作成
 */
global waitqueue_t waitqueue;

/**
 * @brief			tcbで示されるタスクを待ち行列 WaitQueue に追加する
 * @param [in] tcb	追加するタスク
 * @param [in] time	待ち時間
 * @retval FALSE	タスクは WaitQueue 中に存在しなかった。
 * @retval TRUE		タスクは WaitQueue 中に存在したので取り除いた。
 * @note			tcbの妥当性は検査しないので注意
 * @note			time に 0 を渡した場合は、待ち行列には追加されない
 * @author			Kazuya Fukuhara
 * @date			2010/01/07 14:08:58	作成
 * @date			2010/09/09 12:48:22	タイムシェアリングスケジューリングを追加
 * @date			2010/09/10 10:57:13	優先度付きタイムシェアリングスケジューリングを追加
 */
global void addTaskToWaitQueue(tcb_t* tcb, tick_t time) {

	tcb_t* next_task = waitqueue.next_task;
	tcb_t* pT = tcb;
	
#if (SCHEDULER_TYPE == SCHEDULING_PRIORITY_BASE)
	if (time == 0) {
		return;
	} else if (next_task == NULL) {
		/* pauseQ_headに何も登録されていない場合 */
		pT->next_task = waitqueue.next_task;
		pT->pause_count = time;
		waitqueue.next_task = tcb;
	} else if ((next_task->pause_count > time) || 
	           ((next_task->pause_count == time) && (next_task > tcb))) {
		/* 最初の要素の前に挿入するケース */
		pT->next_task = waitqueue.next_task;
		pT->pause_count = time;
		waitqueue.next_task = tcb;
		next_task->pause_count -= pT->pause_count;
	} else {
		/* 最初の要素の後ろに挿入するので、次以降の要素と比較 */
		tcb_t* beforenid;
		for (;;) {
			beforenid = next_task;
			time -= next_task->pause_count;
			next_task = next_task->next_task;
			
			if (next_task == NULL) {
				/* 終端にに達したので、終端に追加 */
				pT->next_task = beforenid->next_task;
				pT->pause_count = time;
				beforenid->next_task = tcb;
				break;
			} else if ((next_task->pause_count > time) || ((next_task->pause_count == time) && (next_task > tcb))) {
				/* 挿入する位置が見つかった */
				pT->next_task = beforenid->next_task;
				pT->pause_count = time;
				beforenid->next_task = tcb;
				next_task->pause_count -= pT->pause_count;
				break;
			} else {
				continue;
			}
		}
	}
#elif (SCHEDULER_TYPE == SCHEDULING_TIME_SHARING)
	if (time == 0) {
		return;
	} else if (next_task == NULL) {
		/* pauseQ_headに何も登録されていない場合 */
		pT->next_task = waitqueue.next_task;
		pT->pause_count = time;
		waitqueue.next_task = tcb;
	} else {
		/* 最初の要素の後ろに挿入するので、次以降の要素と比較 */
		tcb_t* beforenid;
		for (;;) {
			beforenid = next_task;
			time -= next_task->pause_count;
			next_task = next_task->next_task;
			
			if (next_task == NULL) {
				/* 終端にに達したので、終端に追加 */
				pT->next_task = beforenid->next_task;
				pT->pause_count = time;
				beforenid->next_task = tcb;
				break;
			} else if (next_task->pause_count > time) {
				/* 挿入する位置が見つかった */
				pT->next_task = beforenid->next_task;
				pT->pause_count = time;
				beforenid->next_task = tcb;
				next_task->pause_count -= pT->pause_count;
				break;
			} else {
				continue;
			}
		}
	}
#elif (SCHEDULER_TYPE == SCHEDULING_PRIORITY_BASE_TIME_SHARING)
	if (time == 0) {
		return;
	} else if (next_task == NULL) {
		/* pauseQ_headに何も登録されていない場合 */
		pT->next_task = waitqueue.next_task;
		pT->pause_count = time;
		waitqueue.next_task = tcb;
	} else if ((next_task->pause_count > time) || 
	           ((next_task->pause_count == time) && (next_task->task_priolity > tcb->task_priolity))) {
		/* 最初の要素の前に挿入するケース */
		pT->next_task = waitqueue.next_task;
		pT->pause_count = time;
		waitqueue.next_task = tcb;
		next_task->pause_count -= pT->pause_count;
	} else {
		/* 最初の要素の後ろに挿入するので、次以降の要素と比較 */
		tcb_t* beforenid;
		for (;;) {
			beforenid = next_task;
			time -= next_task->pause_count;
			next_task = next_task->next_task;
			
			if (next_task == NULL) {
				/* 終端にに達したので、終端に追加 */
				pT->next_task = beforenid->next_task;
				pT->pause_count = time;
				beforenid->next_task = tcb;
				break;
			} else if ((next_task->pause_count > time) || ((next_task->pause_count == time) && (next_task->task_priolity > tcb->task_priolity))) {
				/* 挿入する位置が見つかった */
				pT->next_task = beforenid->next_task;
				pT->pause_count = time;
				beforenid->next_task = tcb;
				next_task->pause_count -= pT->pause_count;
				break;
			} else {
				continue;
			}
		}
	}
#else
#error "Scheduler type is undefined."
#endif
}

/**
 * @brief			tcbで示されるタスクが待ち行列 WaitQueue 中にあれば取り除く。
 * @param[in] tcb	取り除くタスク
 * @retval FALSE	タスクは WaitQueue 中に存在しなかった。
 * @retval TRUE		タスクは WaitQueue 中に存在したので取り除いた。
 * @note			tcbの妥当性は検査しないので注意
 * @author			Kazuya Fukuhara
 * @date			2010/01/07 14:08:58	作成
 */
global bool_t removeTaskFromWaitQueue(tcb_t* tcb) {
	tcb_t* next_task = NULL;
	tcb_t* pid = NULL;

	if (tcb == NULL) {
		return FALSE;
	}

	/* 探索 */
	pid = NULL;
	for (next_task = waitqueue.next_task; next_task != tcb; next_task = next_task->next_task) {
		if (next_task == NULL) {
			return FALSE;	/* 終端に達した */
		}
		pid = next_task;
	}
	
	/* ここに到達した時点で、指定されたタスク tcb はリスト中に存在していることが保証される */

	/* 自分に続くタスクがある場合、pause_countを更新 */
	if (tcb->next_task != NULL) {
		tcb->next_task->pause_count += tcb->pause_count;
	}

	/* キューから外す */
	if (waitqueue.next_task == tcb) {
		/* キューの先頭の場合 */
		waitqueue.next_task = tcb->next_task;
	} else if (pid != NULL) {
		/*
		 * キューの先頭ではない場合、探索のループが最低１回は実行されている
		 * つまり、pidの値はNULL以外になっている
		 */
		pid->next_task = tcb->next_task;
	}
	tcb->next_task = NULL;

	return TRUE;
}

/**
 * @brief			タスクコントロールブロックからタスクIDを得る
 * @param[in] tcb	IDを得たいタスクのタスクコントロールブロック
 * @return			タスクID;
 * @author			Kazuya Fukuhara
 * @date			2010/01/07 14:08:58	作成
 */
global taskid_t getTaskID(tcb_t* tcb) {
	return TCBtoTID(tcb);
}

/**
 * @brief               指定したタスクコントロールブロックを初期状態にリセットする
 * @param[in] taskid    リセット対象のタスクのタスクID
 * @note                指定したタスクのスタックポインタや状態が全てリセットされ、タスクは初期状態となる。
 * @author              Kazuya Fukuhara
 * @date                2010/01/07 14:08:58	作成
 * @date				2010/08/15 16:11:02	機種依存部分を分離
 */
global void resetTCB(taskid_t taskid) {
	/* コンテキストの初期化 */
	resetContext(taskid);

	/* タスクの状態を設定 */
	tasks[taskid].status = DORMANT;
	tasks[taskid].pause_count = 0;

	/* チェインを初期化 */
	tasks[taskid].next_task = NULL;

	/* メッセージを初期化 */
	tasks[taskid].message = NULL;
}

/**
 * @brief   すべてのタスクコントロールブロックを初期状態にリセットする
 * @note    すべてのタスクに対してresetTCBを呼び出す
 * @author  Kazuya Fukuhara
 * @date    2010/01/07 14:08:58	作成
 */
global void initTaskControlBlocks(void) {
	taskid_t i;
	for (i=0; i<TASK_NUM; ++i) {
		resetTCB(i);
	}
}

/**
 * @brief   待ち状態のタスクの待ち時間を更新する
 * @author  Kazuya Fukuhara
 * @date    2010/01/07 14:08:58	作成
 * @date	2010/09/09 12:48:22	タイムシェアリングスケジューリングを追加
 * @date	2010/09/10 10:57:13	優先度付きタイムシェアリングスケジューリングを追加
 */
global void updateTick(void) {
	
#if (SCHEDULER_TYPE == SCHEDULING_TIME_SHARING) || (SCHEDULER_TYPE == SCHEDULING_PRIORITY_BASE_TIME_SHARING)
	if (currentTCB != NULL) {
		if (currentTCB->time_sharing_tick > 0) {
			currentTCB->time_sharing_tick--;
		}
	}
#endif

	if (waitqueue.next_task == NULL) {
		rescheduling = FALSE;
	} else {
		waitqueue.next_task->pause_count--;
		if (waitqueue.next_task->pause_count == 0) {
			do {
				/* 取り外す */
				tcb_t *next_task = waitqueue.next_task;
				next_task->status = READY;
				waitqueue.next_task = next_task->next_task;
				next_task->next_task = NULL;
				/* readyQueueに入れる */
				addTaskToReadyQueue (next_task);
				/* 次を見る */
			} while ((waitqueue.next_task != NULL) && (waitqueue.next_task->pause_count == 0));
			rescheduling = TRUE;
		} else {
			rescheduling = FALSE;
		}
	}
}

/**
 * @brief   ディスパッチ処理
 * @author  Kazuya Fukuhara
 * @date    2010/01/07 14:08:58	作成
 * @date	2010/08/15 16:11:02	機種依存部分を分離
 */
static void dispatch(void) {
	/* 次に実行するタスクのコンテキストを復帰する */
	RESTORE_CONTEXT();
	RETURN_FROM_INTERRUPT();
}

/**
 * @brief   スケジューラ初期化関数
 * @note    スケジューラを初期化する。
 * @author  Kazuya Fukuhara
 * @date    2010/01/07 14:08:58	作成
 */
global void initScheduler(void) {

	/* readyqueue を初期化 */
	readyqueue.next_task = NULL;

	/* pausequeue を初期化 */
	waitqueue.next_task = NULL;

	/* INIT タスクの起動 */
	INITTASK.status    = READY;
	INITTASK.next_task = NULL;
#if (SCHEDULER_TYPE == SCHEDULING_PRIORITY_BASE_TIME_SHARING)
	INITTASK.task_priolity = 0;
#endif
	addTaskToReadyQueue(&INITTASK);

	/* DIAG タスクの起動 */
	DIAGTASK.status    = READY;
	DIAGTASK.next_task = NULL;
#if (SCHEDULER_TYPE == SCHEDULING_PRIORITY_BASE_TIME_SHARING)
	DIAGTASK.task_priolity = 0xFF;
#endif
	addTaskToReadyQueue(&DIAGTASK);

	/* スケジューリングが必要なので rescheduling に TRUE を設定 */
	rescheduling = TRUE;

}

/**
 * @brief   スケジューラ関数
 * @note    スケジューリングを行い、currentTCBに実行すべきタスクのタスクコントロールブロックのポインタを設定する。
 * @author  Kazuya Fukuhara
 * @date    2010/01/07 14:08:58	作成
 * @date	2010/09/09 12:48:22	タイムシェアリングスケジューリングを追加
 * @date	2010/09/10 10:57:13	優先度付きタイムシェアリングスケジューリングを追加
 */
#if (SCHEDULER_TYPE == SCHEDULING_PRIORITY_BASE)
global void scheduler(void) {
	if (rescheduling != FALSE) {
		/* readyqueue の先頭をタスクに設定 */
		if ((readyqueue.next_task != NULL)&&(readyqueue.next_task < &tasks[TASK_NUM])) {
			currentTCB = readyqueue.next_task;
		}
		rescheduling = FALSE;
	}

	/* ディスパッチ処理を呼び出す */
	dispatch();
}
#elif (SCHEDULER_TYPE == SCHEDULING_TIME_SHARING)
global void scheduler(void) {
	if (currentTCB != NULL) {
		if (currentTCB->time_sharing_tick == 0) {
			/* readyqueueの一番最後に送る */
			tcb_t *tcb = currentTCB;
			removeTaskFromReadyQueue(tcb);
			addTaskToReadyQueue(tcb);
			rescheduling = TRUE;
		}
	}

	if (rescheduling != FALSE) {
		/* readyqueue の先頭をタスクに設定 */
		if ((readyqueue.next_task != NULL)&&(readyqueue.next_task < &tasks[TASK_NUM])) {
			currentTCB = readyqueue.next_task;
		}
		rescheduling = FALSE;
		assert(currentTCB != NULL);
		currentTCB->time_sharing_tick = TIME_SHARING_THRESHOLD;
	}

	/* ディスパッチ処理を呼び出す */
	dispatch();
}
#elif (SCHEDULER_TYPE == SCHEDULING_PRIORITY_BASE_TIME_SHARING)
global void scheduler(void) {
	tcb_t *tcb = readyqueue.next_task;
	if (currentTCB == tcb) {
		/* 割り当てられたCPU時間を確認 */
		if (currentTCB->time_sharing_tick == 0) {
			/* CPU時間を使い切った場合、同一優先順位で次のタスクがあれば実行権が移動する */
			if ((currentTCB->next_task != NULL) && (currentTCB->task_priolity == currentTCB->next_task->task_priolity)) {
				removeTaskFromReadyQueue(currentTCB);
				addTaskToReadyQueue(currentTCB);
				rescheduling = TRUE;
			}
		}
	}
	if (rescheduling != FALSE) {
		if (readyqueue.next_task != NULL) {
			currentTCB = readyqueue.next_task;
			currentTCB->time_sharing_tick = TIME_SHARING_THRESHOLD;
		} else {
			currentTCB = &DIAGTASK;
			currentTCB->time_sharing_tick = 0;	/* DIAGタスクは常に実行権を委譲できる */
		}
		rescheduling = FALSE;
	}

	/* ディスパッチ処理を呼び出す */
	dispatch();
}
#else
#error "Scheduler type is undefined."
#endif

/**
 * @brief   カーネルのスタートアップコード
 * @author  Kazuya Fukuhara
 * @date    2010/01/07 14:08:58	作成
 * @date	2010/08/15 16:11:02	機種依存部分を分離
 */
global void startKernel(void) {

	/*
	 * ハードウェアを初期化
	 */
	initHardware();

	/*
	 * ２．タスクコントロールブロックを初期化する
	 * タスクエントリテーブルを参照し、全タスクのタスクコントロールブロック中のタスク実行先頭アドレス、初期スタックポインタを初期値に設定する
	 */
	initTaskControlBlocks();

	/*
	 * スケジューラを初期化する
	 */
	initScheduler();

	/*
	 * セマフォを初期化する
	 */
	initSemaphoQueue();

	/*
	 * メッセージを初期化する
	 */
	initMessageQueue();

	/* スケジューリングを行い、次に実行するタスクを選択する */
	scheduler();
}

/**
 * @brief	外部割り込みフック表の要素
 * @author	Kazuya Fukuhara
 * @date	2010/09/11 11:01:20	作成
 */
typedef struct {
	taskid_t	taskid;		/**< 外部割り込みで起動するタスク番号 */
#if (SCHEDULER_TYPE == SCHEDULING_PRIORITY_BASE_TIME_SHARING) 
	priolity_t	priolity;	/**< 起動するタスクの優先度（優先度付きタイムシェアリングスケジューリング時に有効） */
#endif
} int_hook_info_t;

/**
 * @brief	外部割り込みフック表
 * @author	Kazuya Fukuhara
 * @date	2010/09/11 11:01:20	作成
 */
static int_hook_info_t int_hook_tid[EXTINT_NUM];

/**
 * @brief					外部割り込みフック表に情報をを設定する
 * @param[in] interrupt_id	フックする外部割り込み番号
 * @param[in] task_id		割り込みで起動するタスクの番号
 * @param[in] priolity		起動するタスクに与える引数(優先度付きタイムシェアリングスケジューリング時に有効)
 * @author					Kazuya Fukuhara
 * @date					2010/09/11 11:01:20	作成
 */
global void set_int_hook_tid(extintid_t interrupt_id, taskid_t task_id
#if (SCHEDULER_TYPE == SCHEDULING_PRIORITY_BASE_TIME_SHARING) 
, priolity_t priolity
#endif
) {
	if (is_hookable_interrupt_id(interrupt_id)) {
		int_hook_tid[interrupt_id].taskid = task_id;
#if (SCHEDULER_TYPE == SCHEDULING_PRIORITY_BASE_TIME_SHARING) 
		int_hook_tid[interrupt_id].priolity = priolity;
#endif
	}
}

/**
 * @brief	外部割り込みに応じてタスクを起動
 * @author	Kazuya Fukuhara
 * @date	2010/09/11 11:01:20	作成
 */
global void external_interrupt_handler(void) {
	extintid_t intid = GetExtIntId();
	if (intid < EXTINT_NUM) {
		taskid_t tid = int_hook_tid[intid].taskid;
		if ((1<=tid)&&(tid<=TASK_NUM-2)) {	/* init/diagが指定されている場合は何もしない（安全策） */
			tcb_t* task = &tasks[tid];
			if (task->status == DORMANT) {

				/* 起動するタスクの設定 */
				task->status = READY;
				task->next_task = NULL;
				task->parameter = NULL;
#if (SCHEDULER_TYPE == SCHEDULING_PRIORITY_BASE_TIME_SHARING) 
				task->task_priolity = int_hook_tid[intid].priolity;
#endif

				/* 引数を設定 */
				SetTaskArg(GetContext(task),task->parameter);

				/* readyキューに挿入 */
				addTaskToReadyQueue(task);

				/* スケジューリング要求 */
#if (SCHEDULER_TYPE == SCHEDULING_PRIORITY_BASE) 
				/* 自分よりも優先度の高いタスクが挿入された場合にのみスケジューリングを要求 */
				rescheduling = (currentTCB > task) ? TRUE : FALSE;
#elif (SCHEDULER_TYPE == SCHEDULING_TIME_SHARING) 
				/* 追加に伴うスケジューリングは不要 */
#elif (SCHEDULER_TYPE == SCHEDULING_PRIORITY_BASE_TIME_SHARING) 
				/* 自分よりも優先度の高いタスクが挿入された場合にのみスケジューリングを要求 */
				rescheduling = (currentTCB->task_priolity > task->task_priolity) ? TRUE : FALSE;
#endif
			}
		}
	}	
	scheduler();
} 

/**
 * @brief   指定領域をゼロクリア
 * @author  Kazuya Fukuhara
 * @date    2010/01/07 14:08:58	作成
 */
global void ZeroMemory(ptr_t buf, uint_t szbuf) {
	uint8_t *p = (uint8_t*)buf;
	while (szbuf--) {
		*p = 0;
		p++;
	}
}
