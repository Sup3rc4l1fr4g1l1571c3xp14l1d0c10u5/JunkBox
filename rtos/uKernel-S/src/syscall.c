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
 * @brief   システムコール
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	作成
 * @date	2010/08/15 16:11:02	機種依存部分を分離
 * @date	2010/09/10 10:57:13	優先度付きタイムシェアリングスケジューリング用のシステムコールを追加
 * @date	2010/09/11 11:01:20	外部割り込みフック用のシステムコールを追加
 */

#include "arch.h"
#include "type.h"
#include "syscall.h"
#include "kernel.h"
#include "semapho.h"
#include "message.h"

/**
 * @enum	syscallid_t
 * @typedef	syscallid_t
 * @brief	システムコールＩＤ
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	作成
 * @date	2010/09/10 10:57:13	優先度付きタイムシェアリングスケジューリング用のシステムコールＩＤ(SVCID_setPriolity,SVCID_getPriolity)を追加
 * @date	2010/09/11 11:01:20	外部割り込みフック用のシステムコールＩＤ(SVCID_hookInterrupt)を追加
 */
typedef enum syscallid_t {
	SVCID_startTASK	 	=  0,
	SVCID_exitTASK		=  1,
	SVCID_pauseTASK		=  2,
	SVCID_resumeTASK 	=  3,
	SVCID_resetTASK	 	=  4,
	SVCID_restartTASK	=  5,
	SVCID_getTID	 	=  6,
	SVCID_takeSEMA	 	=  7,
	SVCID_giveSEMA	 	=  8,
	SVCID_tasSEMA	 	=  9,
	SVCID_hookIntTask	= 10,
	SVCID_restart		= 11,
	SVCID_sendMSG		= 12,
	SVCID_recvMSG		= 13,
	SVCID_waitMSG		= 14,
	SVCID_hookInterrupt	= 15,
#if (SCHEDULER_TYPE == SCHEDULING_PRIORITY_BASE_TIME_SHARING) 
	SVCID_setPriolity	= 16,
	SVCID_getPriolity	= 17,
#endif

} syscallid_t;

/**
 * @struct		PAR_syscall
 * @typedef		PAR_syscall
 * @brief		システムコールのパラメータを格納する構造体の基底クラス
 * @author		Kazuya Fukuhara
 * @date		2010/01/07 14:08:58	作成
 * 
 * 呼び出すシステムコールのＩＤと結果の格納に用いられるため、
 * すべてのシステムコールの引数型はこの構造体を継承して作成すること。
 */
typedef struct PAR_syscall {
	syscallid_t		id;			/**< 呼び出すシステムコールのＩＤ */
	svcresultid_t	result;		/**< システムコールの結果 */
} PAR_syscall;

/**
 * @struct		PAR_startTASK
 * @typedef		PAR_startTASK
 * @brief		SVC_startTASKのパラメータを格納する構造体
 * @author		Kazuya Fukuhara
 * @date		2010/01/07 14:08:58	作成
 * @date		2010/09/10 10:57:13	優先度付きタイムシェアリングスケジューリングで使用する引数 priolity を追加
 */
typedef struct PAR_startTASK {
	PAR_syscall		syscall;	/**< システムコールの情報が格納される */
	taskid_t		taskId;		/**< 起動するタスクの番号 */
	ptr_t			param;		/**< 起動するタスクに与える引数 */
#if (SCHEDULER_TYPE == SCHEDULING_PRIORITY_BASE_TIME_SHARING) 
	priolity_t		priolity;	/**< 起動するタスクに設定する優先順位 */
#endif
} PAR_startTASK;

/**
 * @brief			指定したタスクＩＤのタスクを起動する
 * @param[in] param	システムコール呼び出しの引数が格納された構造体へのポインタ
 * @return			システムコールの成否情報
 * @author			Kazuya Fukuhara
 * @date			2010/01/07 14:08:58	作成
 * @date			2010/08/15 16:11:02	機種依存部分を分離
 * @date			2010/09/09 12:48:22	タイムシェアリングスケジューリングの追加によるスケジューリング要求の追加
 * @date			2010/09/10 10:57:13	優先度付きタイムシェアリングスケジューリングの追加
 */
static svcresultid_t SVC_startTASK(PAR_startTASK *param) {
	tcb_t *ptcb;

#if 0
	if ((param->taskId == 0) || (param->taskId >= TASK_NUM-1)) {
#else
	if (isUserTask(param->taskId) == FALSE) {
#endif
		/* INITTASKとDIAGTASKは起動できない＝ユーザータスク以外は起動できない */
		return ERR1;
	} else {
		ptcb = &(tasks[param->taskId]);
	}

	/* 起動できるタスクは自分以外かつDORMANT状態のもの */
	if ((ptcb != currentTCB) && (ptcb->status == DORMANT)) {

		/* 起動するタスクの設定 */
		ptcb->status = READY;
		ptcb->next_task = NULL;
		ptcb->parameter = param->param;
#if (SCHEDULER_TYPE == SCHEDULING_PRIORITY_BASE_TIME_SHARING) 
		ptcb->task_priolity = param->priolity;
#endif

		/* 引数を設定 */
		SetTaskArg(GetContext(ptcb),ptcb->parameter);

		/* readyキューに挿入 */
		addTaskToReadyQueue(ptcb);

		/* スケジューリング要求 */
#if (SCHEDULER_TYPE == SCHEDULING_PRIORITY_BASE) 
		rescheduling = (currentTCB > ptcb) ? TRUE : FALSE;
#elif (SCHEDULER_TYPE == SCHEDULING_TIME_SHARING) 
		/* 追加に伴うスケジューリングは不要 */
#elif (SCHEDULER_TYPE == SCHEDULING_PRIORITY_BASE_TIME_SHARING) 
		rescheduling = (currentTCB->task_priolity > ptcb->task_priolity) ? TRUE : FALSE;
#endif
		return SUCCESS;
	} else {
		return ERR2;
	}
}

/**
 * @struct		PAR_exitTASK
 * @typedef		PAR_exitTASK
 * @brief		SVC_exitTASKのパラメータを格納する構造体
 * @author		Kazuya Fukuhara
 * @date		2010/01/07 14:08:58	作成
 */
typedef struct PAR_exitTASK {
	PAR_syscall		syscall;	/**< システムコールの情報が格納される */
} PAR_exitTASK;

/**
 * @brief			現在実行中のタスクを終了する。
 * @param[in] param	システムコール呼び出しの引数が格納された構造体へのポインタ
 * @return			システムコールの成否情報
 * @note			タスクのリセット処理も行われる。
 * @author			Kazuya Fukuhara
 * @date			2010/01/07 14:08:58	作成
 */
static svcresultid_t SVC_exitTASK(PAR_exitTASK *param) {
	(void)param;
	if (&DIAGTASK == currentTCB) {
		/* DIAGTASKは終了できない */
		return ERR1;
	} else {
		/* 現在のタスクと関連付けられているセマフォをすべて開放 */
		unlinkSEMA(currentTCB);

		/* 現在のタスクと関連付けられているメッセージをすべて開放 */
		unlinkMSG(currentTCB);

		/* ReadyQueueから現在のタスクを取り外す */
		removeTaskFromReadyQueue(currentTCB);

		/* TCBをリセット */
		resetTCB( getTaskID(currentTCB) );

		/* スケジューリング要求を設定 */
		rescheduling = TRUE;

		/* 成功 */
		return SUCCESS;
	}
}

/**
 * @struct		PAR_pauseTASK
 * @typedef		PAR_pauseTASK
 * @brief		SVC_pauseTASKのパラメータを格納する構造体
 * @author		Kazuya Fukuhara
 * @date		2010/01/07 14:08:58	作成
 */
typedef struct PAR_pauseTASK {
	PAR_syscall		syscall;	/**< システムコールの情報が格納される */
	tick_t			count;		/**< 休眠状態にする時間（tick単位） */
} PAR_pauseTASK;

/**
 * @brief			現在実行中のタスクを休眠状態にする
 * @param[in] param	システムコール呼び出しの引数が格納された構造体へのポインタ
 * @return			システムコールの成否情報
 * @note            countを0に設定すると、resumeTASKシステムコールで再開させるまで無限に休眠する。
 * @warning			長期間休眠(count > 255)には割り込みなど別の要素を使うこと。
 * @author          Kazuya Fukuhara
 * @date            2010/01/07 14:08:58	作成
 */
static svcresultid_t SVC_pauseTASK(PAR_pauseTASK *param) {
	if ((&INITTASK == currentTCB) || (&DIAGTASK == currentTCB)) {
		return ERR1;
	} else {
		currentTCB->status = PAUSE;
		currentTCB->pause_count = param->count;
		removeTaskFromReadyQueue(currentTCB);
		if (param->count != 0 ) {
			/* 無期限休止ではない場合、WaitQueueに追加する */
			addTaskToWaitQueue(currentTCB, param->count);
		}
		rescheduling = TRUE;
		return SUCCESS;
	}
}

/**
 * @struct		PAR_resumeTASK
 * @typedef		PAR_resumeTASK
 * @brief		SVC_resumeTASKのパラメータを格納する構造体
 * @author		Kazuya Fukuhara
 * @date		2010/01/07 14:08:58	作成
 */
typedef struct PAR_resumeTASK {
	PAR_syscall		syscall;	/**< システムコールの情報が格納される */
	taskid_t		taskId;		/**< 再開するタスクのタスクＩＤ */
} PAR_resumeTASK;

/**
 * @brief			指定した休眠状態のタスクを再開する
 * @param[in] param	システムコール呼び出しの引数が格納された構造体へのポインタ
 * @return			システムコールの成否情報
 * @author			Kazuya Fukuhara
 * @date			2010/01/07 14:08:58	作成
 */
static svcresultid_t SVC_resumeTASK(PAR_resumeTASK *param) {
	tcb_t* tcb;

	if (param->taskId >= TASK_NUM) {
		/* 不正なタスクIDがtaskIdに指定されている */
		return ERR1;
	} else if (param->taskId == TASK_NUM-1) {
		/* DIAGTASKはresumeできない */
		return ERR31;
	} else {
		tcb = &tasks[param->taskId];
	}

	/* 休止中のタスクか確認 */
	if (tcb->status != PAUSE) {
		/* セマフォ待ちなど、PAUSE状態ではないタスクをresumeさせた */
		rescheduling = FALSE;
		return ERR5;
	}
	
	/* 有限休眠の場合はタスクがWaitQueueに登録されているので取り除く
	 * 無限休眠の場合は登録されていない
	 */
	(void)removeTaskFromWaitQueue(tcb);


	/* タスクを再び稼動させる */
	tcb->status = READY;
	tcb->pause_count = 0;
	
	/* 引数を設定 */
	SetTaskArg(GetContext(tcb),tcb->parameter);

	addTaskToReadyQueue(currentTCB);

	/* スケジューリング要求 */
#if (SCHEDULER_TYPE == SCHEDULING_PRIORITY_BASE) 
	rescheduling = (currentTCB > tcb) ? TRUE : FALSE;
#elif (SCHEDULER_TYPE == SCHEDULING_TIME_SHARING) 
	/* スケジューリングは不要 */
#elif (SCHEDULER_TYPE == SCHEDULING_PRIORITY_BASE_TIME_SHARING) 
	rescheduling = (currentTCB->task_priolity > tcb->task_priolity) ? TRUE : FALSE;
#endif

	return SUCCESS;
}

/**
 * @struct		PAR_restartTASK 
 * @typedef		PAR_restartTASK 
 * @brief		SVC_restartTASKのパラメータを格納する構造体
 * @author		Kazuya Fukuhara
 * @date		2010/01/07 14:08:58	作成
 */
typedef struct PAR_restartTASK {
	PAR_syscall		syscall;	/**< システムコールの情報が格納される */
	tick_t			count;		/**< リスタートするまでの時間（tick単位） */
} PAR_restartTASK ;

/**
 * @brief			自タスクをリスタートする
 * @param[in] param	システムコール呼び出しの引数が格納された構造体へのポインタ
 * @return			システムコールの成否情報
 * @note            countを0に設定すること（無限休眠）はできない
 * @note            長期間休眠(count > 255)には割り込みなど別の要素を使うこと。
 * @author          Kazuya Fukuhara
 * @date            2010/01/07 14:08:58	作成
 */
static svcresultid_t SVC_restartTASK(PAR_restartTASK *param) {
	tcb_t* tcb = currentTCB;
	PAR_exitTASK dummy;
	message_t *message;
	if (param->count == 0 ) {
		/* 無期限休止は認めない */
		return ERR6;
	} else {
		/* ok */
	}

	/** 
	 * 通常のタスク終了と同様に単純にexitTASK()で終了させると、messageが開放されてしまうため、
	 * 一端messageを退避した後に、タスクをexitTASK()で終了させ、その後リスタート待ちに遷移させてから
	 * 再度messageを設定することでリスタート時もメッセージを保持することを可能とする
	 */

	/* messageを退避 */
	message = tcb->message;
	tcb->message = NULL;

	/* タスクをexitTASK()で終了する。 */
	SVC_exitTASK(&dummy);

	/* 再度messageを設定 */
	tcb->message = message;
	
	/* この時点でタスクは DORMANT 状態になっているので、WAIT_RESTART状態へ遷移させた後、WaitQueueに投入する */
	tcb->status = WAIT_RESTART;
	addTaskToWaitQueue(tcb, param->count);

	rescheduling = TRUE;
	return SUCCESS;
}

/**
 * @struct		PAR_getTID
 * @typedef		PAR_getTID
 * @brief		SVC_getTIDのパラメータを格納する構造体
 * @author		Kazuya Fukuhara
 * @date		2010/01/07 14:08:58	作成
 */
typedef struct PAR_getTID {
	PAR_syscall		syscall;	/**< システムコールの情報が格納される */
	taskid_t		taskId;		/**< システムコールによって自タスクのタスクＩＤが格納される */
} PAR_getTID;

/**
 * @brief				自タスクのタスクＩＤを取得する
 * @param[in] param		システムコール呼び出しの引数が格納された構造体へのポインタ
 * @return				システムコールの成否情報
 * @author				Kazuya Fukuhara
 * @date				2010/01/07 14:08:58	作成
 */
static svcresultid_t SVC_getTID(PAR_getTID *param) {
	param->taskId = getTaskID(currentTCB);
	return SUCCESS;
}

/**
 * @struct		PAR_takeSEMA
 * @typedef		PAR_takeSEMA
 * @brief		SVC_takeSEMAのパラメータを格納する構造体
 * @author		Kazuya Fukuhara
 * @date		2010/01/07 14:08:58	作成
 */
typedef struct PAR_takeSEMA {
	PAR_syscall		syscall;	/**< システムコールの情報が格納される */
	semaphoid_t		sid;		/**< 獲得するセマフォのＩＤ */
} PAR_takeSEMA;

/**
 * @brief			takeSEMAを呼び出すアダプタ関数
 * @param[in] param	システムコール呼び出しの引数が格納された構造体へのポインタ
 * @return			システムコールの成否情報
 * @author			Kazuya Fukuhara
 * @date			2010/01/07 14:08:58	作成
 */
static svcresultid_t SVC_takeSEMA(PAR_takeSEMA *param) {
	return takeSEMA(param->sid);
}

/**
 * @struct		PAR_giveSEMA
 * @typedef		PAR_giveSEMA
 * @brief		SVC_giveSEMAのパラメータを格納する構造体
 * @author		Kazuya Fukuhara
 * @date		2010/01/07 14:08:58	作成
 */
typedef struct PAR_giveSEMA {
	PAR_syscall		syscall;	/**< システムコールの情報が格納される */
	semaphoid_t		sid;		/**< 放棄するセマフォのＩＤ */
} PAR_giveSEMA;

/**
 * @brief			giveSEMAを呼び出すアダプタ関数
 * @param[in] param	システムコール呼び出しの引数が格納された構造体へのポインタ
 * @return			システムコールの成否情報
 * @author			Kazuya Fukuhara
 * @date			2010/01/07 14:08:58	作成
 */
static svcresultid_t SVC_giveSEMA(PAR_giveSEMA *param) {
	return giveSEMA(param->sid);
}

/**
 * @struct		PAR_tasSEMA
 * @typedef		PAR_tasSEMA
 * @brief		SVC_tasSEMAのパラメータを格納する構造体
 * @author		Kazuya Fukuhara
 * @date		2010/01/07 14:08:58	作成
 */
typedef struct PAR_tasSEMA {
	PAR_syscall		syscall;	/**< システムコールの情報が格納される */
	semaphoid_t		sid;		/**< 獲得を試みるセマフォのＩＤ */
} PAR_tasSEMA;

/**
 * @brief			tasSEMAを呼び出すアダプタ関数
 * @param[in] param	システムコール呼び出しの引数が格納された構造体へのポインタ
 * @return			システムコールの成否情報
 * @author			Kazuya Fukuhara
 * @date			2010/01/07 14:08:58	作成
 */
static svcresultid_t SVC_tasSEMA(PAR_tasSEMA *param) {
	return tasSEMA(param->sid);
}

/**
 * @struct		PAR_recvMSG
 * @typedef		PAR_recvMSG
 * @brief		SVC_recvMSGのパラメータを格納する構造体
 * @author		Kazuya Fukuhara
 * @date		2010/01/07 14:08:58	作成
 */
typedef struct PAR_recvMSG {
	PAR_syscall		syscall;	/**< システムコールの情報が格納される */
	taskid_t		from;		/**< 送信元のタスクのＩＤが格納される */
	ptr_t			data;		/**< 受信したメッセージの本文が格納される */
} PAR_recvMSG;

/**
 * @brief			recvMSGを呼び出すアダプタ関数
 * @param[in] param	システムコール呼び出しの引数が格納された構造体へのポインタ
 * @return			システムコールの成否情報
 * @author			Kazuya Fukuhara
 * @date			2010/01/07 14:08:58	作成
 */
static svcresultid_t SVC_recvMSG(PAR_recvMSG *param) {
	return recvMSG(&param->from, &param->data);
}

/**
 * @struct		PAR_sendMSG
 * @typedef		PAR_sendMSG
 * @brief		SVC_sendMSGのパラメータを格納する構造体
 * @author		Kazuya Fukuhara
 * @date		2010/01/07 14:08:58	作成
 */
typedef struct PAR_sendMSG {
	PAR_syscall		syscall;	/**< システムコールの情報が格納される */
	taskid_t		to;			/**< 送信先のタスクのＩＤ */
	ptr_t			data;		/**< 送信するメッセージの本文 */
} PAR_sendMSG;

/**
 * @brief			sendMSGを呼び出すアダプタ関数
 * @param[in] param	システムコール呼び出しの引数が格納された構造体へのポインタ
 * @return			システムコールの成否情報
 * @author			Kazuya Fukuhara
 * @date			2010/01/07 14:08:58	作成
 */
static svcresultid_t SVC_sendMSG(PAR_sendMSG *param) {
	return sendMSG(param->to, param->data);
}

/**
 * @struct		PAR_waitMSG
 * @typedef		PAR_waitMSG
 * @brief		SVC_waitMSGのパラメータを格納する構造体
 * @author		Kazuya Fukuhara
 * @date		2010/01/07 14:08:58	作成
 */
typedef struct PAR_waitMSG {
	PAR_syscall		syscall;	/**< システムコールの情報が格納される */
} PAR_waitMSG;

/**
 * @brief			waitMSGを呼び出すアダプタ関数
 * @param[in] param	システムコール呼び出しの引数が格納された構造体へのポインタ
 * @return			システムコールの成否情報
 * @author			Kazuya Fukuhara
 * @date			2010/01/07 14:08:58	作成
 */
static svcresultid_t SVC_waitMSG(PAR_waitMSG *param) {
	(void)param;
	return waitMSG();
}

/**
 * @struct		PAR_hookInterrupt 
 * @typedef		PAR_hookInterrupt 
 * @brief		SVC_hookInterruptのパラメータを格納する構造体
 * @author		Kazuya Fukuhara
 * @date		2010/09/11 11:01:20	作成
 */
typedef struct PAR_hookInterrupt  {
	PAR_syscall	syscall;		/**< システムコールの情報が格納される */
	extintid_t	interrupt_id;	/**< 外部割り込み番号 */
	taskid_t	task_id;		/**< 割り込みで起動するタスクの番号 */
#if (SCHEDULER_TYPE == SCHEDULING_PRIORITY_BASE_TIME_SHARING) 
	priolity_t	priolity;		/**< 起動するタスクに設定する優先順位 */
#endif
} PAR_hookInterrupt ;

/**
 * @brief			外部割込みにより、指定のタスクを実行する
 * @param[in] param	システムコール呼び出しの引数が格納された構造体へのポインタ
 * @return			システムコールの成否情報
 * @author			Kazuya Fukuhara
 * @date			2010/09/11 11:01:20	作成
 */
static svcresultid_t SVC_hookInterrupt(PAR_hookInterrupt *param) {
	/* 割り込み番号の有効範囲はシステムごとに違うので妥当性を検査 */
	if (is_hookable_interrupt_id(param->interrupt_id) == FALSE) {
		return ERR27;
	}

	if ((1<=param->task_id)&&(param->task_id<=TASK_NUM-2)) {
		set_int_hook_tid(param->interrupt_id, param->task_id
#if (SCHEDULER_TYPE == SCHEDULING_PRIORITY_BASE_TIME_SHARING) 
			,param->priolity
#endif
		);
		EnableExtInterrupt(param->interrupt_id);
	} else {
		set_int_hook_tid(param->interrupt_id, (taskid_t)-1
#if (SCHEDULER_TYPE == SCHEDULING_PRIORITY_BASE_TIME_SHARING) 
			,0xFE
#endif
		);
		DisableExtInterrupt(param->interrupt_id);
	}

	return SUCCESS;
}

#if (SCHEDULER_TYPE == SCHEDULING_PRIORITY_BASE_TIME_SHARING) 
/**
 * @struct		PAR_setPriolity
 * @typedef		PAR_setPriolity
 * @brief		SVC_setPriolityのパラメータを格納する構造体
 * @author		Kazuya Fukuhara
 * @date		2010/09/10 10:57:13	作成
 * @note		スケジューリングアルゴリズムが優先度付きタイムシェアリングの場合に使用可能
 */
typedef struct PAR_setPriolity {
	PAR_syscall		syscall;	/**< システムコールの情報が格納される */
	priolity_t		priolity;	/**< 自タスクに設定する優先度 */
} PAR_setPriolity;

/**
 * @brief			自タスクの優先度を変更する
 * @param[in] param	システムコール呼び出しの引数が格納された構造体へのポインタ
 * @return			システムコールの成否情報
 * @author			Kazuya Fukuhara
 * @date			2010/09/10 10:57:13	作成
 * @note			スケジューリングアルゴリズムが優先度付きタイムシェアリングの場合に使用可能
 */
static svcresultid_t SVC_setPriolity(PAR_setPriolity *param) {
	if ((&INITTASK == currentTCB) || (&DIAGTASK == currentTCB)) {
		return ERR1;
	} else {
		removeTaskFromReadyQueue(currentTCB);
		currentTCB->task_priolity = param->priolity;
		addTaskToReadyQueue(currentTCB);
		rescheduling = TRUE;
		return SUCCESS;
	}
}

/**
 * @struct		PAR_getPriolity
 * @typedef		PAR_getPriolity
 * @brief		SVC_getPriolityのパラメータを格納する構造体
 * @author		Kazuya Fukuhara
 * @date		2010/01/07 14:08:58	作成
 */
typedef struct PAR_getPriolity {
	PAR_syscall		syscall;	/**< システムコールの情報が格納される */
	priolity_t		priolity;	/**< 自タスクの優先度 */
} PAR_getPriolity;

/**
 * @brief			自タスクの優先度を取得する
 * @param[in] param	システムコール呼び出しの引数が格納された構造体へのポインタ
 * @return			システムコールの成否情報
 * @author			Kazuya Fukuhara
 * @date			2010/09/10 10:57:13	作成
 * @note			スケジューリングアルゴリズムが優先度付きタイムシェアリングの場合に使用可能
 */
static svcresultid_t SVC_getPriolity(PAR_getPriolity *param) {
	param->priolity = currentTCB->task_priolity;
	return SUCCESS;
}
#endif

/**
 * @brief	システムコール呼び出し処理
 * @note	機種依存コードが含まれる
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	作成
 * @date	2010/08/15 16:11:02	機種依存部分を分離
 * @date	2010/09/10 10:57:13	優先度付きタイムシェアリングスケジューリング用のシステムコール呼び出しを追加
 */
void syscall_entry(void) {

	context_t* context;
	PAR_syscall *par;

	/* 呼び出し元のコンテキストからシステムコールの引数を得る */
	context = GetContext(currentTCB);
	par = (PAR_syscall*)GetArgPtr(context);

	/* システムコールのIDに応じた処理 */
	switch (par->id) {
		case SVCID_startTASK	: par->result = SVC_startTASK((PAR_startTASK*)par); break;
		case SVCID_exitTASK		: /* par->result = */ SVC_exitTASK((PAR_exitTASK*)par); break;
		case SVCID_pauseTASK	: par->result = SVC_pauseTASK((PAR_pauseTASK*)par); break;
		case SVCID_resumeTASK	: par->result = SVC_resumeTASK((PAR_resumeTASK*)par); break;
		case SVCID_restartTASK	: /* par->result = */ SVC_restartTASK((PAR_restartTASK*)par); break;

		case SVCID_takeSEMA	 	: par->result = SVC_takeSEMA((PAR_takeSEMA*)par); break;
		case SVCID_giveSEMA	 	: par->result = SVC_giveSEMA((PAR_giveSEMA*)par); break;
		case SVCID_tasSEMA	 	: par->result = SVC_tasSEMA((PAR_tasSEMA*)par); break;

		case SVCID_sendMSG	 	: par->result = SVC_sendMSG((PAR_sendMSG*)par); break;
		case SVCID_recvMSG	 	: par->result = SVC_recvMSG((PAR_recvMSG*)par); break;
		case SVCID_waitMSG	 	: par->result = SVC_waitMSG((PAR_waitMSG*)par); break;

		case SVCID_getTID		: par->result = SVC_getTID((PAR_getTID*)par); break;

		case SVCID_hookInterrupt: par->result = SVC_hookInterrupt((PAR_hookInterrupt*)par); break;

#if (SCHEDULER_TYPE == SCHEDULING_PRIORITY_BASE_TIME_SHARING) 
		case SVCID_setPriolity	: par->result = SVC_setPriolity((PAR_setPriolity*)par); break;
		case SVCID_getPriolity	: par->result = SVC_getPriolity((PAR_getPriolity*)par); break;
#endif

		default					: par->result = ERR30; break;
	}

	/* スケジューリングを行い、次に実行するタスクを選択する */
	scheduler();
}

/**
 * 以降はユーザーに公開するAPI
 */

/**
 * @brief		指定したタスクＩＤのタスクを起動する
 * @param[in]	taskId 起動するタスクのタスクＩＤ
 * @param[in]	param 起動するタスクに与える引数
 * @param[in]	priolity 起動するタスクに与える引数(優先度付きタイムシェアリングスケジューリング時に有効)
 * @return		システムコールの成否情報
 * @author		Kazuya Fukuhara
 * @date		2010/01/07 14:08:58	作成
 * @date		2010/08/15 16:11:02	機種依存部分を分離
 * @date		2010/09/10 10:57:13	優先度付きタイムシェアリングスケジューリングで使用する引数 priolity を追加
 */
extern svcresultid_t API_startTASK(taskid_t taskId, ptr_t param
#if (SCHEDULER_TYPE == SCHEDULING_PRIORITY_BASE_TIME_SHARING) 
, priolity_t priolity
#endif
) {
	PAR_startTASK par;
	par.syscall.id = SVCID_startTASK;
	par.taskId = taskId;
	par.param = param;
#if (SCHEDULER_TYPE == SCHEDULING_PRIORITY_BASE_TIME_SHARING) 
	par.priolity = priolity;
#endif
	syscall( (ptr_t)&par);
	return par.syscall.result;
}

/**
 * @brief			現在実行中のタスクを終了する。
 * @return			システムコールの成否情報
 * @note			タスクのリセット処理も行われる。
 * @author			Kazuya Fukuhara
 * @date			2010/01/07 14:08:58	作成
 */
svcresultid_t API_exitTASK(void) {
	PAR_exitTASK par;
	par.syscall.id = SVCID_exitTASK;
	syscall( (ptr_t)&par );
	return par.syscall.result;
}

/**
 * @brief			現在実行中のタスクを休眠状態にする
 * @param[in] count	休眠状態にする時間（tick単位）
 * @return			システムコールの成否情報
 * @note            countを0に設定すると、resumeTASKシステムコールで再開させるまで無限に休眠する。
 * @note            長期間休眠(count > 255)には割り込みなど別の要素を使うこと。
 * @author          Kazuya Fukuhara
 * @date            2010/01/07 14:08:58	作成
 */
svcresultid_t API_pauseTASK(tick_t count) {
	PAR_pauseTASK par;
	par.syscall.id = SVCID_pauseTASK;
	par.count = count;
	syscall( (ptr_t)&par );
	return par.syscall.result;
}

/**
 * @brief				指定した休眠状態のタスクを再開中にする
 * @param[in] taskId	再開するタスクのタスクＩＤ
 * @return				システムコールの成否情報
 * @author			    Kazuya Fukuhara
 * @date			    2010/01/07 14:08:58	作成
 */
svcresultid_t API_resumeTASK(taskid_t taskId) {
	PAR_resumeTASK par;
	par.syscall.id = SVCID_resumeTASK;
	par.taskId = taskId;
	syscall( (ptr_t)&par );
	return par.syscall.result;
}

/**
 * @brief			自タスクをリスタートする
 * @param[in] count	再開するまでの時間（tick単位）
 * @return			システムコールの成否情報
 * @note            countを0に設定すると、resumeTASKシステムコールで再開させるまで無限に休眠する。
 * @note            長期間休眠(count > 255)には割り込みなど別の要素を使うこと。
 * @author          Kazuya Fukuhara
 * @date            2010/01/07 14:08:58	作成
 */
svcresultid_t API_restartTASK(tick_t count) {
	PAR_restartTASK par;
	par.syscall.id = SVCID_restartTASK;
	par.count = count;
	syscall( (ptr_t)&par );
	return par.syscall.result;
}

/**
 * @brief				自タスクのタスクＩＤを取得する
 * @param[out] pTaskID	自タスクのタスクＩＤが格納される領域
 * @return				システムコールの成否情報
 * @author				Kazuya Fukuhara
 * @date				2010/01/07 14:08:58	作成
 */
svcresultid_t API_getTID(taskid_t *pTaskID) {
	PAR_getTID par;
	par.syscall.id = SVCID_getTID;
	syscall( (ptr_t)&par );
	*pTaskID = par.taskId;
	return par.syscall.result;
}

/**
 * @brief				takeSEMAを呼び出すアダプタ関数
 * @param[in] sid		獲得するセマフォのＩＤ
 * @return				システムコールの成否情報
 * @author				Kazuya Fukuhara
 * @date				2010/01/07 14:08:58	作成
 */
svcresultid_t API_takeSEMA(semaphoid_t sid) {
	PAR_takeSEMA par;
	par.syscall.id = SVCID_takeSEMA;
	par.sid = sid;
	syscall( (ptr_t)&par );
	return par.syscall.result;
}

/**
 * @brief				giveSEMAを呼び出すアダプタ関数
 * @param[in] sid		放棄するセマフォのＩＤ
 * @return				システムコールの成否情報
 * @author				Kazuya Fukuhara
 * @date				2010/01/07 14:08:58	作成
 */
svcresultid_t API_giveSEMA(semaphoid_t sid) {
	PAR_giveSEMA par;
	par.syscall.id = SVCID_giveSEMA;
	par.sid = sid;
	syscall( (ptr_t)&par );
	return par.syscall.result;
}

/**
 * @brief				tasSEMAを呼び出すアダプタ関数
 * @param[in] sid		獲得を試みるセマフォのＩＤ
 * @return				システムコールの成否情報
 * @author				Kazuya Fukuhara
 * @date				2010/01/07 14:08:58	作成
 */
svcresultid_t API_tasSEMA(semaphoid_t sid) {
	PAR_tasSEMA par;
	par.syscall.id = SVCID_tasSEMA;
	par.sid = sid;
	syscall( (ptr_t)&par );
	return par.syscall.result;
}

/**
 * @brief				recvMSGを呼び出すアダプタ関数
 * @param[out] from		メッセージの送信タスクIDを格納する領域のポインタ
 * @param[out] data		メッセージを格納する領域のポインタ
 * @return				システムコールの成否情報
 * @author				Kazuya Fukuhara
 * @date				2010/01/07 14:08:58	作成
 */
svcresultid_t API_recvMSG(taskid_t* from, ptr_t* data) {
	PAR_recvMSG par;
	par.syscall.id = SVCID_recvMSG;
	syscall( (ptr_t)&par );
	*from = par.from;
	*data = par.data;
	return par.syscall.result;
}

/**
 * @brief				sendMSGを呼び出すアダプタ関数
 * @param[in] to		送信先のタスクのＩＤ
 * @param[in] data		送信するメッセージの本文
 * @return				システムコールの成否情報
 * @author				Kazuya Fukuhara
 * @date				2010/01/07 14:08:58	作成
 */
svcresultid_t API_sendMSG(taskid_t to, ptr_t data) {
	PAR_sendMSG par;
	par.syscall.id = SVCID_sendMSG;
	par.to = to;
	par.data = data;
	syscall( (ptr_t)&par );
	return par.syscall.result;
}

/**
 * @brief	waitMSGを呼び出すアダプタ関数
 * @return	システムコールの成否情報
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	作成
 */
svcresultid_t API_waitMSG(void) {
	PAR_waitMSG par;
	par.syscall.id = SVCID_waitMSG;
	syscall( (ptr_t)&par );
	return par.syscall.result;
}


/**
 * @brief					hookInterruptを呼び出すアダプタ関数
 * @param[in] interrupt_id	フックする外部割り込み番号
 * @param[in] task_id		割り込みで起動するタスクの番号
 * @param[in] priolity		起動するタスクに与える引数(優先度付きタイムシェアリングスケジューリング時に有効)
 * @return					システムコールの成否情報
 * @author					Kazuya Fukuhara
 * @date					2010/09/11 11:01:20	作成
 */
svcresultid_t API_hookInterrupt(extintid_t interrupt_id, taskid_t task_id
#if (SCHEDULER_TYPE == SCHEDULING_PRIORITY_BASE_TIME_SHARING) 
, priolity_t priolity
#endif
) {
	PAR_hookInterrupt par;
	par.syscall.id		= SVCID_hookInterrupt;
	par.interrupt_id	= interrupt_id;
	par.task_id			= task_id;
#if (SCHEDULER_TYPE == SCHEDULING_PRIORITY_BASE_TIME_SHARING) 
	par.priolity        = priolity;
#endif
	syscall( (ptr_t)&par );
	return par.syscall.result;
}


#if (SCHEDULER_TYPE == SCHEDULING_PRIORITY_BASE_TIME_SHARING) 
/**
 * @brief	setPriplity を呼び出すアダプタ関数
 * @param[in] priolity	変更後の優先順位
 * @return	システムコールの成否情報
 * @author	Kazuya Fukuhara
 * @date	2010/09/10 10:57:13	作成
 * @note	スケジューリングアルゴリズムが優先度付きタイムシェアリングの場合に使用可能
 */
svcresultid_t API_setPriolity(priolity_t priolity) {
	PAR_setPriolity par;
	par.syscall.id = SVCID_setPriolity;
	par.priolity = priolity;
	syscall( (ptr_t)&par );
	return par.syscall.result;
}


/**
 * @brief	getPriplity を呼び出すアダプタ関数
 * @param[out] priolity	優先順位の格納先
 * @return	システムコールの成否情報
 * @author	Kazuya Fukuhara
 * @date	2010/09/10 10:57:13	作成
 * @note	スケジューリングアルゴリズムが優先度付きタイムシェアリングの場合に使用可能
 */
svcresultid_t API_getPriolity(priolity_t *priolity) {
	PAR_setPriolity par;
	par.syscall.id = SVCID_getPriolity;
	syscall( (ptr_t)&par );
	*priolity = par.priolity;
	return par.syscall.result;
}
#endif
