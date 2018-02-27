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
 */

#ifndef __kernel_h__
#define __kernel_h__

#include "./arch.h"
#include "./type.h"
#include "./config.h"

/**
 * @typedef	enum stateid_t stateid_t;
 * @enum	stateid_t
 * @brief	タスクの状態を示す定数
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	作成
 * @par		タスクの状態の遷移図:
 * @dot
 *   digraph state {
 *       node [ shape=box, fontname="ＳＨ Ｇ30-M", fontsize=10];
 *       edge [ fontname="ＳＨ Ｇ30-M", fontsize=10];
 *
 *       NON_EXISTS   [ label="NON_EXISTS" URL="\ref NON_EXISTS"];
 *       DORMANT      [ label="DORMANT" URL="\ref DORMANT"];
 *       READY        [ label="READY" URL="\ref READY"];
 *       PAUSE        [ label="PAUSE" URL="\ref PAUSE"];
 *       WAIT_MSG     [ label="WAIT_MSG" URL="\ref WAIT_MSG"];
 *       WAIT_SEMA    [ label="WAIT_SEMA" URL="\ref WAIT_SEMA"];
 *       WAIT_RESTART [ label="WAIT_RESTART" URL="\ref WAIT_RESTART"];
 *
 *       NON_EXISTS   -> DORMANT      [ arrowhead="open", style="solid", label="reset" ];
 *       DORMANT      -> READY        [ arrowhead="open", style="solid", label="start" ];
 *       READY        -> PAUSE        [ arrowhead="open", style="solid", label="pause" ];
 *       READY        -> DORMANT      [ arrowhead="open", style="solid", label="exit" ];
 *       PAUSE        -> READY        [ arrowhead="open", style="solid", label="resume" ];
 *       READY        -> WAIT_RESTART [ arrowhead="open", style="solid", label="restart" ];
 *       WAIT_RESTART -> READY        [ arrowhead="open", style="solid", label="" ];
 *       READY        -> WAIT_SEMA    [ arrowhead="open", style="solid", label="takeSema" ];
 *       WAIT_SEMA    -> READY        [ arrowhead="open", style="solid", label="giveSema" ];
 *       READY        -> WAIT_MSG     [ arrowhead="open", style="solid", label="waitMsg" ];
 *       WAIT_MSG     -> READY        [ arrowhead="open", style="solid", label="sendMsg" ];
 *
 *       { rank = same; NON_EXISTS }
 *       { rank = same; DORMANT }
 *       { rank = same; READY }
 *       { rank = same; PAUSE, WAIT_MSG, WAIT_SEMA, WAIT_RESTART }
 *   }
 * @enddot
 */
typedef enum stateid_t { 
	NON_EXISTS,		/**< 未初期化状態  */
	DORMANT,		/**< 休止状態：未起動、実行可能 */ 
	READY, 			/**< 実行状態：起動済み、実行可能 */
	PAUSE, 			/**< 待ち状態：起動済み、実行不可能 */
	WAIT_MSG,		/**< メッセージ待ち状態：起動済み、実行不可能 */
	WAIT_SEMA,		/**< セマフォ待ち状態：起動済み、実行不可能 */
	WAIT_RESTART,	/**< リスタート待ち状態：起動済み、実行不可能 */
} stateid_t;

/**
 * @typedef	struct tcb_t tcb_t;
 * @struct	tcb_t
 * @brief	タスクコントロールブロック
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	作成
 * @date	2010/09/09 12:48:22	タイムシェアリングスケジューリングを追加
 * @date	2010/09/10 10:57:13	優先度付きタイムシェアリングスケジューリングを追加
 */
typedef struct tcb_t {
	uint8_t*			stack_pointer;	/**< スタックポインタ */
	stateid_t			status;			/**< タスクの状態 */
	ptr_t				parameter;		/**< タスクの起動時引数 */
	uint8_t				pause_count;	/**< 待ち時間(0x00は永遠にお休み) */
	struct tcb_t*		next_task;		/**< 各種行列でのリンクリスト用 */
	struct message_t*	message;		/**< 受信したメッセージのキュー */
#if (SCHEDULER_TYPE == SCHEDULING_TIME_SHARING) || \
    (SCHEDULER_TYPE == SCHEDULING_PRIORITY_BASE_TIME_SHARING) 
	/**
	 * タイムシェアリングスケジューリング/優先度付きタイムシェアリングスケジューリング
	 * において、タスクが消費したCPU時間を数える。
	 * scheduler() で実行権が移動した際に 閾値 TIME_SHARING_THRESHOLD に初期化され、
	 * カーネルタイマ割り込みのタイミングで -1 される。
	 * 0になった場合は、スケジューリングが行われるタイミングで強制的に次のタスクに実行権が移動する。
	 */
	uint8_t				time_sharing_tick;
#endif
#if (SCHEDULER_TYPE == SCHEDULING_PRIORITY_BASE_TIME_SHARING) 
	/**
	 * 優先度付きタイムシェアリングスケジューリングにおけるタスクの優先度を格納する
	 */
	uint8_t				task_priolity;
#endif
} tcb_t;

/**
 * @def		TCBtoTID
 * @brief	タスクコントロールブロックのアドレスからタスクＩＤを得る
 * @author	Kazuya Fukuhara
 * @warning	ルネサス M3T-NC30WA などは可換が可能な場合でも除算をビットシフトに置換しない。
 *          その結果、除算関数が呼ばれ、スタックが溢れることもあるため、必要ならばビットシフトが可能な形に書き換えてもよい。
 * @date	2010/01/07 14:08:58	作成
 */
#define TCBtoTID(x) ((taskid_t)((x)-&tasks[0]))

/**
 * @brief 	タスクコントロールブロック配列
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	作成
 */
extern tcb_t	tasks[TASK_NUM];

/**
 * @brief	タスクの実行時スタック領域
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	作成
 */
extern uint8_t	task_stack[TASK_NUM][TASK_STACK_SIZE];

/**
 * @typedef	struct readyqueue_t readyqueue_t;
 * @struct	readyqueue_t
 * @brief	ready状態のタスクを並べるキュー型
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	作成
 */
typedef struct readyqueue_t {
	tcb_t*	next_task;		/**< リンクリスト用 */
} readyqueue_t;

/**
 * @brief	ready状態のタスクを並べるキュー
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	作成
 */
extern readyqueue_t readyqueue;

/**
 * @brief	指定したtidがユーザータスクを示しているか判定する。
 * @author	Kazuya Fukuhara
 * @param   tid 判定するタスクＩＤ
 * @retval	TRUE  ユーザータスクである
 * @retval	FALSE ユーザータスクではない
 * @date	2010/12/03 20:31:26	作成
 *
 * ユーザータスクとは、INIT/DIAGを除いたタスクのこと。
 */
extern bool_t isUserTask(taskid_t tid);

/**
 * @brief			タスクをreadyqueueに挿入する
 * @param[in] tcb	挿入するタスク
 * @author			Kazuya Fukuhara
 * @date			2010/01/07 14:08:58	作成
 */
extern void addTaskToReadyQueue(tcb_t *tcb);

/**
 * @brief			タスクをreadyqueueから取り除く
 * @param[in] tcb	取り除くタスク
 * @author			Kazuya Fukuhara
 * @date			2010/01/07 14:08:58	作成
 */
extern bool_t removeTaskFromReadyQueue(tcb_t *tcb);

/**
 * @brief	待ち時間が有限のwait状態のタスクを並べるキュー
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	作成
 */
typedef struct {
	tcb_t*	next_task;		/**< リンクリスト用 */
} waitqueue_t;

/**
 * @brief	wait状態のタスクを並べるキュー
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	作成
 */
extern waitqueue_t waitqueue;

/**
 * @brief			tcbで示されるタスクを待ち行列 WaitQueue に追加する
 * @param[in] tcb	追加するタスク
 * @param[in] time	待ち時間
 * @retval FALSE	タスクは WaitQueue 中に存在しなかった。
 * @retval TRUE		タスクは WaitQueue 中に存在したので取り除いた。
 * @note			tcbの妥当性は検査しないので注意
 * @note			time に 0 を渡した場合は、待ち行列には追加されない
 * @author			Kazuya Fukuhara
 * @date			2010/01/07 14:08:58	作成
 */
extern void addTaskToWaitQueue(tcb_t* tcb, uint8_t time) ;

/**
 * @brief			tcbで示されるタスクが待ち行列 WaitQueue 中にあれば取り除く。
 * @param[in] tcb	取り除くタスク
 * @retval FALSE	タスクは WaitQueue 中に存在しなかった。
 * @retval TRUE		タスクは WaitQueue 中に存在したので取り除いた。
 * @note			tcbの妥当性は検査しないので注意
 * @author			Kazuya Fukuhara
 * @date			2010/01/07 14:08:58	作成
 */
extern bool_t removeTaskFromWaitQueue(tcb_t* tcb);

/**
 * @def		INITTASK
 * @brief	初期化タスク
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	作成
 */
#define INITTASK (tasks[0])

/**
 * @def		DIAGTASK
 * @brief	Idleタスク
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	作成
 */
#define DIAGTASK (tasks[TASK_NUM-1])

/**
 * @brief	現在実行中のタスクのタスクコントロールブロック
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	作成
 */
extern tcb_t*	currentTCB;

/**
 * @brief	スケジューリング要求フラグ
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	作成
 */
extern bool_t	rescheduling;

/**
 * @typedef	taskproc_t
 * @brief	タスク開始アドレスを示す型
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	作成
 */
typedef void (TASKPROC *taskproc_t)(ptr_t arg);

/**
 * @brief	タスクの開始アドレスが格納されている配列
 * @note	ユーザー側で定義しなければならない。
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	作成
 */
extern const taskproc_t TaskTable[];

/**
 * @brief	カーネルを起動
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	作成
 */
extern void startKERNEL(void);

/**
 * @brief			タスクコントロールブロックからタスクIDを得る
 * @param[in] tcb	IDを得たいタスクのタスクコントロールブロック
 * @return			タスクID;
 * @author			Kazuya Fukuhara
 * @date			2010/01/07 14:08:58	作成
 */
extern taskid_t getTaskID(tcb_t* tcb);

/**
 * @brief				指定したタスクコントロールブロックを初期状態にリセットする
 * @param[in] taskid	リセット対象のタスクのタスクID
 * @note				指定したタスクのスタックポインタや状態が全てリセットされ、タスクは初期状態となる。
 * @author				Kazuya Fukuhara
 * @date    			2010/01/07 14:08:58	作成
 * @date				2010/08/15 16:11:02	機種依存部分を分離
 */
extern void resetTCB(taskid_t taskid);

/**
 * @brief   待ち状態のタスクの待ち時間を更新する
 * @author	Kazuya Fukuhara
 * @date    2010/01/07 14:08:58	作成
 */
extern void updateTick(void);

/**
 * @brief   スケジューラ関数
 * @note    スケジューリングを行い、currentTCBに実行すべきタスクのタスクコントロールブロックのポインタを設定する。
 * @author	Kazuya Fukuhara
 * @date    2010/01/07 14:08:58	作成
 */
extern void scheduler(void);

/**
 * @brief   カーネルのスタートアップコード
 * @author	Kazuya Fukuhara
 * @date    2010/01/07 14:08:58	作成
 * @date	2010/08/15 16:11:02	機種依存部分を分離
 */
extern void startKernel(void);

/**
 * @brief					外部割り込みフック表に情報をを設定する
 * @param[in] interrupt_id	フックする外部割り込み番号
 * @param[in] task_id		割り込みで起動するタスクの番号
 * @param[in] priolity		起動するタスクに与える引数(優先度付きタイムシェアリングスケジューリング時に有効)
 * @author					Kazuya Fukuhara
 * @date					2010/09/11 11:01:20	作成
 */
void set_int_hook_tid(extintid_t interrupt_id, taskid_t task_id
#if (SCHEDULER_TYPE == SCHEDULING_PRIORITY_BASE_TIME_SHARING) 
, priolity_t priolity
#endif
);

/**
 * @brief	外部割り込みに応じてタスクを起動
 * @author	Kazuya Fukuhara
 * @date	2010/09/11 11:01:20	作成
 */
extern void external_interrupt_handler(void);


/**
 * @brief   指定領域をゼロクリア
 * @author  Kazuya Fukuhara
 * @date    2010/01/07 14:08:58	作成
 */
extern void ZeroMemory(ptr_t buf, uint_t szbuf);

#endif
