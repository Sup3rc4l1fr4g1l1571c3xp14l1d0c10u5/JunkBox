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
 * @brief	システムコール
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	作成
 * @date	2010/08/15 16:11:02	機種依存部分を分離
 * @date	2010/09/10 10:57:13	優先度付きタイムシェアリングスケジューリング用のシステムコールを追加
 * @date	2010/09/11 11:01:20	外部割り込みフック用のシステムコールを追加
 */

#ifndef __syscall_h__
#define __syscall_h__

#include "type.h"

/**
 * @enum	svcresultid_t
 * @brief	システムコールの呼び出し結果
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	作成
 */
typedef enum svcresultid_t {
	SUCCESS	= 0,	/**< 正常終了 */
	ERR1	= 1,	/**< タスクＩＤが不正  */
	ERR2	= 2,	/**< DORMANT状態でないタスクを起動した */
	ERR3	= 3,	/**< [予約] */
	ERR4	= 4,	/**< [予約] */
	ERR5	= 5,	/**< wait 状態でないタスクを resume させようとした */
	ERR6	= 6,	/**< Pause指定時間が不正(0) */
	ERR7	= 7,	/**< [予約] */
	ERR8	= 8,	/**< [予約] */
	ERR9	= 9,	/**< [予約] */
	ERR10	= 10,	/**< セマフォＩＤが不正  */
	ERR11	= 11,	/**< 指定したセマフォが獲得できない */
	ERR12	= 12,	/**< 指定したセマフォを開放できない */
	ERR13	= 13,	/**< [予約] */
	ERR14	= 14,	/**< [予約] */
	ERR15	= 15,	/**< [予約] */
	ERR16	= 16,	/**< [予約] */
	ERR17	= 17,	/**< [予約] */
	ERR18	= 18,	/**< [予約] */
	ERR19	= 19,	/**< [予約] */
	ERR20	= 20,	/**< 自タスクを reset しようとした */
	ERR21	= 21,	/**< [予約] */
	ERR22	= 22,	/**< [予約] */
	ERR23	= 23,	/**< [予約] */
	ERR24	= 24,	/**< [予約] */
	ERR25	= 25,	/**< [予約] */
	ERR26	= 26,	/**< [予約] */
	ERR27	= 27,	/**< 不正な割込み番号  */
	ERR28	= 28,	/**< [予約] */
	ERR29	= 29,	/**< [予約] */
	ERR30	= 30,	/**< 不正なシステムコール呼び出し */
	ERR31	= 31,	/**< INIT/DIAGからは呼び出せないシステムコールを呼び出した */
	ERR40	= 40,	/**< メール送信失敗 */
	ERR41	= 41,	/**< メールボックスは空 */
	ERR42	= 42,	/**< メール取得失敗 */
} svcresultid_t;

/**
 * @brief			システムコール呼び出し処理
 * @note			機種依存コードが含まれる
 * @author			Kazuya Fukuhara
 * @date			2010/01/07 14:08:58	作成
 * @date			2010/08/15 16:11:02	機種依存部分を分離
 */
extern void syscall_entry(void);

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
);

/**
 * @brief		現在実行中のタスクを終了する。
 * @return		システムコールの成否情報
 * @note		タスクのリセット処理も行われる。
 * @author		Kazuya Fukuhara
 * @date		2010/01/07 14:08:58	作成
 */
extern svcresultid_t API_exitTASK(void);

/**
 * @brief			現在実行中のタスクを休眠状態にする
 * @param[in] count	休眠状態にする時間（tick単位）
 * @return			システムコールの成否情報
 * @note            countを0に設定すると、resumeTASKシステムコールで再開させるまで無限に休眠する。
 * @note            長期間休眠(count > 255)には割り込みなど別の要素を使うこと。
 * @author          Kazuya Fukuhara
 * @date            2010/01/07 14:08:58	作成
 */
extern svcresultid_t API_pauseTASK(tick_t count);

/**
 * @brief				指定した休眠状態のタスクを再開中にする
 * @param[in] taskId	再開するタスクのタスクＩＤ
 * @return				システムコールの成否情報
 * @author				Kazuya Fukuhara
 * @date				2010/01/07 14:08:58	作成
 */
extern svcresultid_t API_resumeTASK(taskid_t taskId);

/**
 * @brief			自タスクをリスタートする
 * @param[in] count	再開するまでの時間（tick単位）
 * @return			システムコールの成否情報
 * @note            countを0に設定すると、resumeTASKシステムコールで再開させるまで無限に休眠する。
 * @note            長期間休眠(count > 255)には割り込みなど別の要素を使うこと。
 * @author          Kazuya Fukuhara
 * @date            2010/01/07 14:08:58	作成
 */
extern svcresultid_t API_restartTASK(tick_t count);

/**
 * @brief				自タスクのタスクＩＤを取得する
 * @param[out] pTaskID	自タスクのタスクＩＤが格納される領域
 * @return				システムコールの成否情報
 * @author				Kazuya Fukuhara
 * @date				2010/01/07 14:08:58	作成
 */
extern svcresultid_t API_getTID(taskid_t *pTaskID);

/**
 * @brief			takeSEMAを呼び出すアダプタ関数
 * @param[in] sid	獲得するセマフォのＩＤ
 * @return			システムコールの成否情報
 * @author			Kazuya Fukuhara
 * @date			2010/01/07 14:08:58	作成
 */
extern svcresultid_t API_takeSEMA(semaphoid_t sid);

/**
 * @brief			giveSEMAを呼び出すアダプタ関数
 * @param[in] sid	放棄するセマフォのＩＤ
 * @return			システムコールの成否情報
 * @author			Kazuya Fukuhara
 * @date			2010/01/07 14:08:58	作成
 */
extern svcresultid_t API_giveSEMA(semaphoid_t sid);

/**
 * @brief			tasSEMAを呼び出すアダプタ関数
 * @param[in] sid	獲得を試みるセマフォのＩＤ
 * @return			システムコールの成否情報
 * @author			Kazuya Fukuhara
 * @date			2010/01/07 14:08:58	作成
 */
extern svcresultid_t API_tasSEMA(semaphoid_t sid);

/**
 * @brief			recvMSGを呼び出すアダプタ関数
 * @param[out] from	メッセージの送信タスクIDを格納する領域のポインタ
 * @param[out] data	メッセージを格納する領域のポインタ
 * @return			システムコールの成否情報
 * @author			Kazuya Fukuhara
 * @date			2010/01/07 14:08:58	作成
 */
extern svcresultid_t API_recvMSG(taskid_t* from, ptr_t* data);

/**
 * @brief			sendMSGを呼び出すアダプタ関数
 * @param[in] to	送信先のタスクのＩＤ
 * @param[in] data	送信するメッセージの本文
 * @return			システムコールの成否情報
 * @author			Kazuya Fukuhara
 * @date			2010/01/07 14:08:58	作成
 */
extern svcresultid_t API_sendMSG(taskid_t to, ptr_t data);

/**
 * @brief		waitMSGを呼び出すアダプタ関数
 * @return		システムコールの成否情報
 * @author		Kazuya Fukuhara
 * @date		2010/01/07 14:08:58	作成
 */
extern svcresultid_t API_waitMSG(void);

/**
 * @brief					hookInterruptを呼び出すアダプタ関数
 * @param[in] interrupt_id	フックする外部割り込み番号
 * @param[in] task_id		割り込みで起動するタスクの番号
 * @param[in] priolity		起動するタスクに与える引数(優先度付きタイムシェアリングスケジューリング時に有効)
 * @return					システムコールの成否情報
 * @author					Kazuya Fukuhara
 * @date					2010/09/11 11:01:20	作成
 */
extern svcresultid_t API_hookInterrupt(extintid_t interrupt_id, taskid_t task_id
#if (SCHEDULER_TYPE == SCHEDULING_PRIORITY_BASE_TIME_SHARING) 
, priolity_t priolity
#endif
);

#if (SCHEDULER_TYPE == SCHEDULING_PRIORITY_BASE_TIME_SHARING) 
/**
 * @brief	setPriplity を呼び出すアダプタ関数
 * @param[in] priolity	変更後の優先順位
 * @return	システムコールの成否情報
 * @author	Kazuya Fukuhara
 * @date	2010/09/10 10:57:13	作成
 * @note	スケジューリングアルゴリズムが優先度付きタイムシェアリングの場合に使用可能
 */
extern svcresultid_t API_setPriolity(priolity_t priolity);

/**
 * @brief	getPriplity を呼び出すアダプタ関数
 * @param[out] priolity	優先順位の格納先
 * @return	システムコールの成否情報
 * @author	Kazuya Fukuhara
 * @date	2010/09/10 10:57:13	作成
 * @note	スケジューリングアルゴリズムが優先度付きタイムシェアリングの場合に使用可能
 */
extern svcresultid_t API_getPriolity(priolity_t *priolity);
#endif

#endif
