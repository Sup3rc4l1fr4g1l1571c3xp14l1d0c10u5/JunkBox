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
 * @brief	AVRハードウェア依存のコード
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	作成
 * @date	2010/08/15 16:11:02	機種依存部分として分離
 * @date	2010/09/11 11:01:20	外部割り込みへの対応を追加
 */

#ifndef __hw_h__
#define __hw_h__

#include "../../src/syscall.h"
#include "./context.h"

/**
 * @brief	ハードウェア全般の初期化処理
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	作成
 */
extern void initHardware(void);

/**
 * @brief		コンテキストの初期化
 * @param[in]	tid タスク番号
 * @author		Kazuya Fukuhara
 * @date		2010/08/15 16:11:02	作成
 */
extern void resetContext(taskid_t tid);

/**
 * @brief					システムコール呼び出し
 * @param[in,out] param		呼び出すシステムコールのＩＤや引数の格納された構造体へのポインタ
 * @author					Kazuya Fukuhara
 * @date					2010/09/01 21:36:49	作成
 *
 * WinAVRではregister修飾子が有効であり、第１引数はレジスタ渡しで渡される。
 * また、AVRにはソフトウェア割り込みがないため、INT0割り込みをソフトウェア割り込みとして利用している。
 */
extern  svcresultid_t NAKED syscall(register ptr_t param);

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
 * @enum	interrupt_id_t
 * @typedef	enum interrupt_id_t interrupt_id_t
 * @brief	割り込み番号
 * @author	Kazuya Fukuhara
 * @date	2010/09/11 11:01:20	作成
 */
typedef enum interrupt_id_t {
	INT_RESET = 0,
	INT_INT0,
	INT_INT1,
	INT_PCINT0,
	INT_PCINT1,
	INT_PCINT2,
	INT_WDT,
	INT_TIMER2_COMPA,
	INT_TIMER2_COMPB,
	INT_TIMER2_OVF,
	INT_TIMER1_CAPT,
	INT_TIMER1_COMPA,
	INT_TIMER1_COMPB,
	INT_TIMER1_OVF,
	INT_TIMER0_COMPA,
	INT_TIMER0_COMPB,
	INT_TIMER0_OVF,
	INT_SPI_STC,
	INT_USART_RX,
	INT_USART_UDRE,
	INT_USART_TX,
	INT_ADC,
	INT_EE_READY,
	INT_ANALOG_COMP,
	INT_TWI,
	INT_SPM_READY,
	INT_MAX
} interrupt_id_t;

/**
 * @def		EXTINT_NUM
 * @brief	外部割り込みの個数
 * @author	Kazuya Fukuhara
 * @date	2010/09/11 11:01:20	作成
 */
#define EXTINT_NUM INT_MAX

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
 * @def		disableInterrupt
 * @brief	割り込みを禁止する
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	作成
 * @warning	システムコール呼び出しも不可能になる。
 */
#define disableInterrupt() cli()

/**
 * @def		enableInterrupt
 * @brief	割り込みを許可する
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	作成
 */
#define enableInterrupt() sei()

/**
 * @def		EnableExtInterrupt
 * @brief	特定の外部割り込み番号の割り込みを有効化する
 * @author	Kazuya Fukuhara
 * @date	2010/09/11 11:01:20	作成
 * @warning	未実装
 */
#define EnableExtInterrupt(x) /* AVRでは割り込みのレベルは固定なので、別途専用の処理を作成すること */

/**
 * @def		DisableExtInterrupt
 * @brief	特定の外部割り込み番号の割り込みを無効化する
 * @author	Kazuya Fukuhara
 * @date	2010/09/11 11:01:20	作成
 * @warning	未実装
 */
#define DisableExtInterrupt(x) /* AVRでは割り込みのレベルは固定なので、別途専用の処理を作成すること */

#endif
