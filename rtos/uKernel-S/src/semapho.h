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
 */

#ifndef __semapho_h__
#define __semapho_h__

#include "kernel.h"
#include "syscall.h"

/**
 * @typedef struct semaphoqueue_t semaphoqueue_t;
 * @struct  semaphoqueue_t
 * @brief   カーネルが管理するセマフォキュー構造体
 * @note    INITタスクとDIAGタスクではセマフォ使用を禁止する
 * @author  Kazuya Fukuhara
 * @date    2010/01/07 14:08:58	作成
 */
typedef struct semaphoqueue_t {
	tcb_t*	next_task;		/**<
	                         * セマフォ待ちタスクのリンクリスト
	                         * (このリストにはタスクが優先度順で並んでいる)
							 * NULLの場合は待ちタスクなし
	                         */
	tcb_t*	take_task;		/**<
							 * 今セマフォを獲得しているタスクのポインタ
							 * NULLの場合は獲得しているタスクなし
							 */
} semaphoqueue_t;

/**
 * @brief	セマフォキューを初期化する
 * @author  Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	作成
 */
extern void initSemaphoQueue(void);

/**
 * @brief			指定したタスクをセマフォキューから削除する
 * @param[in] tcb	削除するタスクのタスクポインタ
 * @author  		Kazuya Fukuhara
 * @date    		2010/01/07 14:08:58	作成
 */
extern void unlinkSEMA(tcb_t *tcb);

/**
 * @brief			セマフォを獲得する。獲得できない場合はwait状態になる。
 * @param[in] sid	獲得するセマフォのＩＤ
 * @retval SUCCESS	セマフォ獲得成功
 * @retval ERR10	エラー：獲得したいセマフォのＩＤが範囲外
 * @retval ERR31	エラー：INIT, DIAGがセマフォを獲得しようとした
 * @author  		Kazuya Fukuhara
 * @date			2010/01/07 14:08:58	作成
 */
extern svcresultid_t takeSEMA(const semaphoid_t sid);

/**
 * @brief			獲得したセマフォを放棄する。
 * @param[in] sid	放棄するセマフォのＩＤ
 * @retval SUCCESS	セマフォ放棄成功
 * @retval ERR10	エラー：放棄したいセマフォのＩＤが範囲外
 * @retval ERR12	エラー：獲得していないセマフォを放棄しようとした
 * @author  		Kazuya Fukuhara
 * @date			2010/01/07 14:08:58	作成
 */
extern svcresultid_t giveSEMA(semaphoid_t sid);

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
extern svcresultid_t tasSEMA(semaphoid_t sid);

#endif
