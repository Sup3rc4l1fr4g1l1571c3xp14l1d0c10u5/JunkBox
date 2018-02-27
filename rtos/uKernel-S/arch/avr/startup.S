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
 * @brief	uKernelのスタートアップコード
 * @note	ATmega328P向けのスタートアップルーチン
 * @note	アセンブリ言語は doxygen で解釈できないため、解説はソースコードを見よ。
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	作成
 */

; startup code
.nolist

/** @cond */

#include <avr/io.h>
#include <avr/sfr_defs.h>

.list

.section	.vectors, "ax", @progbits

	.weak	RESET_vect
	.weak	INT0_vect
	.weak	INT1_vect
	.weak	PCINT0_vect
	.weak	PCINT1_vect
	.weak	PCINT2_vect
	.weak	WDT_vect
	.weak	TIMER2_COMPA_vect
	.weak	TIMER2_COMPB_vect
	.weak	TIMER2_OVF_vect
	.weak	TIMER1_CAPT_vect
	.weak	TIMER1_COMPA_vect
	.weak	TIMER1_COMPB_vect
	.weak	TIMER1_OVF_vect
	.weak	TIMER0_COMPA_vect
	.weak	TIMER0_COMPB_vect
	.weak	TIMER0_OVF_vect
	.weak	SPI_STC_vect
	.weak	USART_RX_vect
	.weak	USART_UDRE_vect
	.weak	USART_TX_vect
	.weak	ADC_vect
	.weak	EE_READY_vect
	.weak	ANALOG_COMP_vect
	.weak	TWI_vect
	.weak	SPM_READY_vect

/**
 * @brief	割り込みベクタの定義
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	作成
 */
.global	__vectors
.func	__vectors
vectors:
	jmp	RESET_vect			/* Reset */
	jmp	INT0_vect			/* External Interrupt Request 0 */
	jmp	INT1_vect			/* External Interrupt Request 1 */
	jmp	PCINT0_vect			/* Pin Change Interrupt Request 0 */
	jmp	PCINT1_vect			/* Pin Change Interrupt Request 0 */
	jmp	PCINT2_vect			/* Pin Change Interrupt Request 1 */
	jmp	WDT_vect			/* Watchdog Time-out Interrupt */
	jmp	TIMER2_COMPA_vect	/* Timer/Counter2 Compare Match A */
	jmp	TIMER2_COMPB_vect	/* Timer/Counter2 Compare Match A */
	jmp	TIMER2_OVF_vect		/* Timer/Counter2 Overflow */
	jmp	TIMER1_CAPT_vect	/* Timer/Counter1 Capture Event */
	jmp	TIMER1_COMPA_vect	/* Timer/Counter1 Compare Match A */
	jmp	TIMER1_COMPB_vect	/* Timer/Counter1 Compare Match B */ 
	jmp	TIMER1_OVF_vect		/* Timer/Counter1 Overflow */
	jmp	TIMER0_COMPA_vect	/* TimerCounter0 Compare Match A */
	jmp	TIMER0_COMPB_vect	/* TimerCounter0 Compare Match B */
	jmp	TIMER0_OVF_vect		/* Timer/Couner0 Overflow */
	jmp	SPI_STC_vect		/* SPI Serial Transfer Complete */
	jmp	USART_RX_vect		/* USART Rx Complete */
	jmp	USART_UDRE_vect		/* USART, Data Register Empty */
	jmp	USART_TX_vect		/* USART Tx Complete */
	jmp	ADC_vect			/* ADC Conversion Complete */
	jmp	EE_READY_vect		/* EEPROM Ready */
	jmp	ANALOG_COMP_vect	/* Analog Comparator */
	jmp	TWI_vect			/* Two-wire Serial Interface */
	jmp	SPM_READY_vect		/* Store Program Memory Read */
.endfunc

.section .init0, "ax", @progbits

/**
 * @brief	リセットベクタ（スタートアップ）処理
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	作成
 */
.func	RESET_vect
RESET_vect:
	.weak	__stack
	.weak	__heap_end
	.set	__heap_end, 0

	/* r1はゼロレジスタとして使われる（WinAVRの仕様）ので、ゼロクリアする */
	clr		r1

	/* ステータスレジスタをr1(=0)でクリア */
	out		_SFR_IO_ADDR(SREG),	r1

	/* スタック関係のレジスタにカーネルスタックを設定 */
	ldi		r28,	lo8(__stack)
	ldi		r29,	hi8(__stack)
	out		_SFR_IO_ADDR(SPH),	r29
	out		_SFR_IO_ADDR(SPL),	r28

	/* 以降はカーネルスタックを利用することが可能となる */

__do_copy_data:
	/*
	 * DATA(初期値付きデータを格納しておくセクション)に初期値を設定。
	 * DATAセクションには初期値が指定されているグローバル変数や、初期値が指定されている static 変数などが配置される。 
	 */
	ldi		r26, lo8(__data_start)		/* 転送先:Xレジスタ[R27:R26]の下位を設定 */
	ldi		r27, hi8(__data_start)		/* 転送先:Xレジスタ[R27:R26]の上位を設定 */
	ldi		r30, lo8(__data_load_start)	/* 転送元:Zレジスタ[R31:R30]の下位を設定 */
	ldi		r31, hi8(__data_load_start)	/* 転送元:Zレジスタ[R31:R30]の上位を設定 */
	ldi		r17, hi8(__data_end)		/* cpc比較用の値 */
	rjmp	.L__do_copy_data_start
.L__do_copy_data_loop:
	lpm		r0, Z+						/* RO = *Z++; */
	st		X+,	r0						/* *X++ = RO; */
.L__do_copy_data_start:
	cpi		r26, lo8(__data_end)
	cpc		r27, r17
	brne	.L__do_copy_data_loop

	/**
	 * BSS(初期値なしデータが格納されるセクション)をゼロクリア
	 * BSSセクションには初期値がないグローバル変数や、初期値が指定されていない static 変数などが配置される。 
	 * 通常はゼロクリアされる。
	 */
__do_clear_bss:
	ldi		r26, lo8(__bss_start)
	ldi		r27, hi8(__bss_start)
	ldi		r17, hi8(__bss_end)
	rjmp	.L__do_clear_bss_start
.L__do_clear_bss_loop:
	st		X+, r1
.L__do_clear_bss_start:
	cpi		r26, lo8(__bss_end)
	cpc		r27, r17
	brne	.L__do_clear_bss_loop

	/* 以上でセクションの初期化が完了 */

	/* カーネルスタックはRAMの末尾からスタート */
	.set	__stack, RAMEND

	/* C言語のスタートルーチンに飛ぶ */
	call	startKernel

	/* 万が一戻ってきてしまった場合は無限ループに入る */
   .global	__halt
__halt:
	rjmp	__halt
.endfunc


/* External Interrupt Request 0 */
.func	INT0_vect
INT0_vect:
.endfunc

/* External Interrupt Request 1 */
.func	INT1_vect
INT1_vect:
.endfunc

/* Pin Change Interrupt Request 0 */
.func	PCINT0_vect
PCINT0_vect:
.endfunc

/* Pin Change Interrupt Request 0 */
.func	PCINT1_vect
PCINT1_vect:
.endfunc

/* Pin Change Interrupt Request 1 */
.func	PCINT2_vect
PCINT2_vect:
.endfunc

/* Watchdog Time-out Interrupt */
.func	WDT_vect
WDT_vect:
.endfunc

/* Timer/Counter2 Compare Match A */
.func	TIMER2_COMPA_vect
TIMER2_COMPA_vect:
.endfunc

/* Timer/Counter2 Compare Match A */
.func	TIMER2_COMPB_vect
TIMER2_COMPB_vect:
.endfunc

/* Timer/Counter2 Overflow */
.func	TIMER2_OVF_vect
TIMER2_OVF_vect:
.endfunc

/* Timer/Counter1 Capture Event */
.func	TIMER1_CAPT_vect
TIMER1_CAPT_vect:
.endfunc

/* Timer/Counter1 Compare Match A */
.func	TIMER1_COMPA_vect
TIMER1_COMPA_vect:
.endfunc

/* Timer/Counter1 Compare Match B */
.func	TIMER1_COMPB_vect
TIMER1_COMPB_vect:
.endfunc

/* Timer/Counter1 Overflow */
.func	TIMER1_OVF_vect
TIMER1_OVF_vect:
.endfunc

/* TimerCounter0 Compare Match A */
.func	TIMER0_COMPA_vect
TIMER0_COMPA_vect:
.endfunc

/* TimerCounter0 Compare Match B */
.func	TIMER0_COMPB_vect
TIMER0_COMPB_vect:
.endfunc

/* Timer/Couner0 Overflow */
.func	TIMER0_OVF_vect
TIMER0_OVF_vect:
.endfunc

/* SPI Serial Transfer Complete */
.func	SPI_STC_vect
SPI_STC_vect:
.endfunc

/* USART Rx Complete */
.func	USART_RX_vect
USART_RX_vect:
.endfunc

/* USART, Data Register Empty */
.func	USART_UDRE_vect
USART_UDRE_vect:
.endfunc

/* USART Tx Complete */
.func	USART_TX_vect
USART_TX_vect:
.endfunc

/* ADC Conversion Complete */
.func	ADC_vect
ADC_vect:
.endfunc

/* EEPROM Ready */
.func	EE_READY_vect
EE_READY_vect:
.endfunc

/* Analog Comparator */
.func	ANALOG_COMP_vect
ANALOG_COMP_vect:
.endfunc

/* Two-wire Serial Interface */
.func	TWI_vect
TWI_vect:
.endfunc

/* Store Program Memory Read */
.func	SPM_READY_vect
SPM_READY_vect:
.endfunc

.end

/** @endcond */

