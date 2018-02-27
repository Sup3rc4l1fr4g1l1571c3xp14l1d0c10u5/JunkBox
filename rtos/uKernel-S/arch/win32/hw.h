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
 * @brief   Win32依存のコード
 * @author  Kazuya Fukuhara
 * @date	2010/09/01 21:36:49	作成
 * @date	2010/09/11 11:01:20	外部割り込みへの対応を追加
 */

#ifndef __hw_h__
#define __hw_h__

#include "../../src/type.h"
//#include "../../src/kernel.h"
#include "../../src/syscall.h"

/**
 * @brief   ハードウェア全般の初期化処理
 * @author  Kazuya Fukuhara
 * @date	2010/09/01 21:36:49	作成
 */
extern void initHardware(void);

/**
 * @brief	割り込みを禁止する
 * @author	Kazuya Fukuhara
 * @date	2010/09/01 21:36:49	作成
 */
extern void disableInterrupt(void);

/**
 * @brief	割り込みを許可する
 * @author	Kazuya Fukuhara
 * @date	2010/09/01 21:36:49	作成
 */
extern void enableInterrupt(void);

/**
 * @brief			コンテキストの初期化
 * @param[in] tid	タスク番号
 * @author			Kazuya Fukuhara
 * @date			2010/09/01 21:36:49	作成
 */
extern void resetContext(taskid_t tid);

/**
 * @brief					システムコール呼び出し
 * @param[in,out] param		呼び出すシステムコールのＩＤや引数の格納された構造体へのポインタ
 * @author					Kazuya Fukuhara
 * @date					2010/09/01 21:36:49	作成
 */
extern void syscall( ptr_t param );

/**
 * @brief	発生した外部割り込みの番号
 * @author	Kazuya Fukuhara
 * @date	2010/09/11 11:01:20	作成
 */
extern uint8_t ExtIntID;

/**
 * @def		GetExtIntId
 * @brief	発生した外部割り込み番号の取得
 * @author	Kazuya Fukuhara
 * @date	2010/09/11 11:01:20	作成
 */
#define GetExtIntId() ExtIntID

/**
 * @def		EXTINT_NUM
 * @brief	外部割り込みの個数
 * @author	Kazuya Fukuhara
 * @date	2010/09/11 11:01:20	作成
 */
#define EXTINT_NUM 4

/**
 * @brief			フック可能な外部割り込み番号か判定
 * @param[in] id	外部割り込み番号
 * @retval TRUE		フック可能な外部割り込み番号
 * @retval FALSE	フック不可能な外部割り込み番号
 * @author			Kazuya Fukuhara
 * @date			2010/09/11 11:01:20	作成
 */
extern bool_t is_hookable_interrupt_id(extintid_t int_id);

/**
 * @def		EnableExtInterrupt
 * @brief	特定の外部割り込み番号の割り込みを有効化する
 * @author	Kazuya Fukuhara
 * @date	2010/09/11 11:01:20	作成
 */
#define EnableExtInterrupt(x) /* Win32仮想ハードウェア上では不要 */

/**
 * @def		DisableExtInterrupt
 * @brief	特定の外部割り込み番号の割り込みを無効化する
 * @author	Kazuya Fukuhara
 * @date	2010/09/11 11:01:20	作成
 */
#define DisableExtInterrupt(x) /* Win32仮想ハードウェア上では不要 */

#endif
