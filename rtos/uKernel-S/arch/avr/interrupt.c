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
 * @brief	AVRハードウェアの外部割り込みを捕捉する
 * @author	Kazuya Fukuhara
 * @date	2010/09/11 11:01:20	作成
 */

#include "../../src/arch.h"
#include "../../src/kernel.h"
#include "../../src/syscall.h"

/**
 * @brief	コンテキストをカーネル側に切り替えた後に、外部割込みをカーネルに通達する
 * @author	Kazuya Fukuhara
 * @date	2010/09/11 11:01:20	作成
 */
void NAKED notice_interrupt_to_kernel(void) {
	/* 現在のタスクのコンテキストを保存 */
	SAVE_CONTEXT();

	/* カーネルスタックに切り替え */
	SET_KERNEL_STACKPOINTER();

	/* 以降はカーネルスタックの利用が可能になったので、関数呼び出しなどが可能となる */

	/* カーネルの外部割込み処理ハンドラを呼び出す（戻ってこない） */
	external_interrupt_handler();
}

/**
 * @def		EXTINT_CAPTURE
 * @brief	外部割り込みを捕捉するコードを生成するマクロ
 * @author	Kazuya Fukuhara
 * @date	2010/09/11 11:01:20	作成
 */
#define EXTINT_CAPTURE(x)											\
NAKED_ISR void x##_vect(void);										\
																	\
void x##_vect(void) {												\
	asm("PUSH	R24							");						\
	asm("LDI	R24, %0						" : : "M" (INT_##x) );	\
	asm("STS	ExtIntID, R24				");						\
	asm("POP	R24							");						\
	asm("JMP	notice_interrupt_to_kernel	");						\
}																	\


/* 各外部割り込みを捕捉するためのコードを生成 */

/* EXTINT_CAPTURE(RESET) は利用禁止 */
/* EXTINT_CAPTURE(INT0) はソフトウェア割り込みで利用済み */
EXTINT_CAPTURE(INT1)
EXTINT_CAPTURE(PCINT0)
EXTINT_CAPTURE(PCINT1)
EXTINT_CAPTURE(PCINT2)
EXTINT_CAPTURE(WDT)
EXTINT_CAPTURE(TIMER2_COMPA)
EXTINT_CAPTURE(TIMER2_COMPB)
EXTINT_CAPTURE(TIMER2_OVF)
EXTINT_CAPTURE(TIMER1_CAPT)
EXTINT_CAPTURE(TIMER1_COMPA)
EXTINT_CAPTURE(TIMER1_COMPB)
EXTINT_CAPTURE(TIMER1_OVF)
/* EXTINT_CAPTURE(TIMER0_COMPA) はカーネルタイマ割り込みとして利用済み */
/* EXTINT_CAPTURE(TIMER0_COMPB) はTIMER0がカーネルタイマとして利用されているので利用禁止 */
/* EXTINT_CAPTURE(TIMER0_OVF) はTIMER0がカーネルタイマとして利用されているので利用禁止 */
EXTINT_CAPTURE(SPI_STC)
EXTINT_CAPTURE(USART_RX)
EXTINT_CAPTURE(USART_UDRE)
EXTINT_CAPTURE(USART_TX)
EXTINT_CAPTURE(ADC)
EXTINT_CAPTURE(EE_READY)
EXTINT_CAPTURE(ANALOG_COMP)
EXTINT_CAPTURE(TWI)
EXTINT_CAPTURE(SPM_READY)

