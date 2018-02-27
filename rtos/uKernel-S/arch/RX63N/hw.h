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
 * @brief	ハードウェア依存のコード
 * @author	Kazuya Fukuhara
 * @date	2010/11/30 17:05 作成
 *
 * 移植対象機種に応じた記述を行うこと。
 */

#ifndef __hw_h__
#define __hw_h__

#include "../../src/syscall.h"
#include "./context.h"
#include	<machine.h>

/**
 * @brief	ハードウェア全般の初期化処理
 * @author	Kazuya Fukuhara
 * @date	2010/11/30 17:05 作成
 */
extern void initHardware(void);

/**
 * @brief			コンテキストの初期化
 * @param[in] tid	タスク番号
 * @author			Kazuya Fukuhara
 * @date			2010/11/30 17:05 作成
 */
extern void resetContext(taskid_t tid);

/**
 * @brief				システムコール呼び出し
 * @param[in] id 		呼び出すシステムコールのID
 * @param[in] param1	システムコールの引数１
 * @param[in] param2	システムコールの引数２
 * @autho			r	Kazuya Fukuhara
 * @date				2010/11/30 17:05 作成
 */
extern  svcresultid_t syscall(ptr_t param);

/**
 * @def		disableInterrupt
 * @brief	割り込みを禁止する
 * @author	Kazuya Fukuhara
 * @date	2010/11/30 17:05 作成
 */
#define disableInterrupt() clrpsw_i()

/**
 * @def		enableInterrupt
 * @brief	割り込みを許可する
 * @author	Kazuya Fukuhara
 * @date	2010/11/30 17:05 作成
 */
#define enableInterrupt() setpsw_i()

/**
 * @def		GetExtIntId
 * @brief	発生した外部割り込み番号の取得
 * @author	Kazuya Fukuhara
 * @date	2010/11/30 17:05 作成
 * @warning 現時点では外部割り込み未対応なので0を返す。
 */
#define GetExtIntId() 0

/**
 * @enum	interrupt_id_t
 * @typedef	enum interrupt_id_t interrupt_id_t
 * @brief	割り込み番号
 * @author	Kazuya Fukuhara
 * @date	2010/11/30 17:05 作成
 */
typedef enum interrupt_id_t {
	DUMMY,
	INT_MAX	/**< 特に問題がない場合、この要素を外部割込みの個数に使える */
} interrupt_id_t;

/**
 * 
 */

/**
 * @def		EXTINT_NUM
 * @brief	外部割り込みの個数
 * @author	Kazuya Fukuhara
 * @date	2010/11/30 17:05 作成
 */
#define EXTINT_NUM INT_MAX

/**
 * @brief			フック可能な外部割り込み番号か判定
 * @param[in] id	外部割り込み番号
 * @retval TRUE		フック可能な外部割り込み番号
 * @retval FALSE	フック不可能な外部割り込み番号
 * @author			Kazuya Fukuhara
 * @date			2010/11/30 17:05 作成
 */
extern bool_t is_hookable_interrupt_id(extintid_t int_id);

/**
 * @def		EnableExtInterrupt
 * @brief	特定の外部割り込み番号の割り込みを有効化する
 * @author	Kazuya Fukuhara
 * @date	2010/11/30 17:05 作成
 */
#define EnableExtInterrupt(x) 

/**
 * @def		DisableExtInterrupt
 * @brief	特定の外部割り込み番号の割り込みを無効化する
 * @author	
 * @date	20xx/xx/xx xx:xx:xx	作成
 * 
 * M16Cのようにレジスタなどで割り込み要因ごとに有効無効を設定できる場合は、そちらを用いるコードを作成。
 * そうでない場合は、別途テーブルなどを作成して有効無効を設定するようにすること。
 */
#define DisableExtInterrupt(x)

#endif
