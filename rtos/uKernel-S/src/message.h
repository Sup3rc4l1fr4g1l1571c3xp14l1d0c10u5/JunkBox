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

#ifndef __message_h__
#define __message_h__

#include "kernel.h"
#include "syscall.h"

/**
 * @typedef struct message_t message_t;
 * @struct  message_t
 * @brief   タスク間通信で用いるメッセージ構造体
 * @note    INITタスクとDIAGタスクではセマフォ使用を禁止する
 * @author  Kazuya Fukuhara
 * @date    2010/01/07 14:08:58	作成
 */
typedef struct message_t {
	struct message_t*	next;	/**< 次のメッセージへのリンク */
	taskid_t			from;	/**< メッセージ送信元のタスクID */
	ptr_t				data;	/**< メッセージ本文 */
} message_t;

/**
 * @brief	メッセージキューを初期化する
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	作成
 */
extern void initMessageQueue(void);

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
extern svcresultid_t recvMSG(taskid_t* from, ptr_t* data);

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
extern svcresultid_t sendMSG(taskid_t to, ptr_t data);

/**
 * @brief			メッセージを受信するまで待ち状態となる
 * @author  		Kazuya Fukuhara
 * @retval SUCCESS	成功
 * @date			2010/01/07 14:08:58	作成
 */
extern svcresultid_t waitMSG(void);

/**
 * @brief			指定したタスクのメッセージをすべて開放
 * @param[in] tcb	タスクのタスクポインタ
 * @author  		Kazuya Fukuhara
 * @date			2010/01/07 14:08:58	作成
 */
extern void unlinkMSG(tcb_t *tcb);

#endif
