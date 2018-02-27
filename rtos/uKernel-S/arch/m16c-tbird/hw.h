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
 * @brief	M16C T-Birdハードウェア依存のコード
 * @author	Kazuya Fukuhara
 * @date	2010/09/04 19:46:15	作成
 * @date	2010/09/11 11:01:20	外部割り込みへの対応を追加
 */

#ifndef __hw_h__
#define __hw_h__

#include "../../src/syscall.h"
#include "./context.h"

/**
 * @brief	ハードウェア全般の初期化処理
 * @author	Kazuya Fukuhara
 * @date	2010/09/04 19:46:15	作成
 */
extern void initHardware(void);

/**
 * @brief			コンテキストの初期化
 * @param[in] tid	タスク番号
 * @author			Kazuya Fukuhara
 * @date			2010/09/04 19:46:15	作成
 */
extern void resetContext(taskid_t tid);

/**
 * @brief					システムコール呼び出し
 * @param[in,out] param		呼び出すシステムコールのＩＤや引数の格納された構造体へのポインタ
 * @author					Kazuya Fukuhara
 * @date					2010/09/01 21:36:49	作成
 */
extern  svcresultid_t syscall(register ptr_t param);
#pragma Parameter syscall(R0);

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
 * @date	2010/09/04 19:46:15	作成
 */
typedef enum interrupt_id_t {
	INT_BRK  = 0,		/**< BRK命令 */
	INT_INT3 = 4,		/**< IRQ:INT3 */
	INT_TIMER_B5 = 5,	/**< タイマB5 */
	INT_TIMER_B6 = 6,	/**< タイマB4 */
	INT_TIMER_B7 = 7,	/**< タイマB3 */
	INT_INT5 = 8,		/**< IRQ:INT5 */
	INT_INT4 = 9,		/**< IRQ:INT4 */
	INT_BUS = 10,		/**< バス衝突検出 */
	INT_DMA0 = 11,		/**< DMA0 */
	INT_DMA1 = 12,		/**< DMA1 */
	INT_KEYINPUT = 13,	/**< キー入力割り込み */
	INT_AD = 14,		/**< A/D変換割り込み */
	INT_UART2_TX= 15,	/**< UART2送信割り込み */
	INT_UART2_RX = 16,	/**< UART2受信割り込み */
	INT_UART0_TX = 17,	/**< UART0送信割り込み */
	INT_UART0_RX = 18,	/**< UART0受信割り込み */
	INT_UART1_TX = 19,	/**< UART1送信割り込み */
	INT_UART1_RX = 20,	/**< UART1受信割り込み */
	INT_TIMERA0 = 21,	/**< タイマA0 */
	INT_TIMERA1 = 22,	/**< タイマA1 */
	INT_TIMERA2 = 23,	/**< タイマA2 */
	INT_TIMERA3 = 24,	/**< タイマA3 */
	INT_TIMERA4 = 25,	/**< タイマA4 */
	INT_TIMERB0 = 26,	/**< タイマB0 */
	INT_TIMERB1 = 27,	/**< タイマB1 */
	INT_TIMERB2 = 28,	/**< タイマB2 */
	INT_INT0 = 29,		/**< IRQ:INT0 */
	INT_INT1 = 30,		/**< IRQ:INT1 */
	INT_INT2 = 31,		/**< IRQ:INT2 */
	INT_SWINT = 32,		/**< ソフトウェア割り込み(以降31個) */
} interrupt_id_t;

/**
 * @def		EXTINT_NUM
 * @brief	外部割り込みの個数
 * @author	Kazuya Fukuhara
 * @date	2010/09/11 11:01:20	作成
 */
#define EXTINT_NUM 32

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
 * @date	2010/09/04 19:46:15	作成
 */
#define disableInterrupt() asm("fclr I")

/**
 * @def		enableInterrupt
 * @brief	割り込みを許可する
 * @author	Kazuya Fukuhara
 * @date	2010/09/04 19:46:15	作成
 */
#define enableInterrupt() asm("fset	I")

/**
 * @def		INTCR
 * @brief	割り込みコントロールレジスタの先頭アドレス
 * @author	Kazuya Fukuhara
 * @date	2010/09/04 19:46:15	作成
 */
#define INTCR ((volatile uint8_t*)0x0040)

/**
 * @def		EnableExtInterrupt
 * @brief	特定の外部割り込み番号の割り込みを有効化する
 * @author	Kazuya Fukuhara
 * @date	2010/09/11 11:01:20	作成
 *
 * 割り込みレベルは7が最大だが、モニタデバッガがレベル7を使うため、
 * レベルを6に設定する
 */
#define EnableExtInterrupt(x) { INTCR[(x) & 63] = 6; }

/**
 * @def		DisableExtInterrupt
 * @brief	特定の外部割り込み番号の割り込みを無効化する
 * @author	Kazuya Fukuhara
 * @date	2010/09/11 11:01:20	作成
 * 
 * 割り込みレベルを0に設定する
 */
#define DisableExtInterrupt(x) { INTCR[(x) & 63] = 0; }

#endif
