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
 * @brief	AVR�n�[�h�E�F�A�̊O�����荞�݂�ߑ�����
 * @author	Kazuya Fukuhara
 * @date	2010/09/11 11:01:20	�쐬
 */

#include "../../src/arch.h"
#include "../../src/kernel.h"
#include "../../src/syscall.h"

/**
 * @brief	�R���e�L�X�g���J�[�l�����ɐ؂�ւ�����ɁA�O�������݂��J�[�l���ɒʒB����
 * @author	Kazuya Fukuhara
 * @date	2010/09/11 11:01:20	�쐬
 */
void NAKED notice_interrupt_to_kernel(void) {
	/* ���݂̃^�X�N�̃R���e�L�X�g��ۑ� */
	SAVE_CONTEXT();

	/* �J�[�l���X�^�b�N�ɐ؂�ւ� */
	SET_KERNEL_STACKPOINTER();

	/* �ȍ~�̓J�[�l���X�^�b�N�̗��p���\�ɂȂ����̂ŁA�֐��Ăяo���Ȃǂ��\�ƂȂ� */

	/* �J�[�l���̊O�������ݏ����n���h�����Ăяo���i�߂��Ă��Ȃ��j */
	external_interrupt_handler();
}

/**
 * @def		EXTINT_CAPTURE
 * @brief	�O�����荞�݂�ߑ�����R�[�h�𐶐�����}�N��
 * @author	Kazuya Fukuhara
 * @date	2010/09/11 11:01:20	�쐬
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


/* �e�O�����荞�݂�ߑ����邽�߂̃R�[�h�𐶐� */

/* EXTINT_CAPTURE(RESET) �͗��p�֎~ */
/* EXTINT_CAPTURE(INT0) �̓\�t�g�E�F�A���荞�݂ŗ��p�ς� */
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
/* EXTINT_CAPTURE(TIMER0_COMPA) �̓J�[�l���^�C�}���荞�݂Ƃ��ė��p�ς� */
/* EXTINT_CAPTURE(TIMER0_COMPB) ��TIMER0���J�[�l���^�C�}�Ƃ��ė��p����Ă���̂ŗ��p�֎~ */
/* EXTINT_CAPTURE(TIMER0_OVF) ��TIMER0���J�[�l���^�C�}�Ƃ��ė��p����Ă���̂ŗ��p�֎~ */
EXTINT_CAPTURE(SPI_STC)
EXTINT_CAPTURE(USART_RX)
EXTINT_CAPTURE(USART_UDRE)
EXTINT_CAPTURE(USART_TX)
EXTINT_CAPTURE(ADC)
EXTINT_CAPTURE(EE_READY)
EXTINT_CAPTURE(ANALOG_COMP)
EXTINT_CAPTURE(TWI)
EXTINT_CAPTURE(SPM_READY)

