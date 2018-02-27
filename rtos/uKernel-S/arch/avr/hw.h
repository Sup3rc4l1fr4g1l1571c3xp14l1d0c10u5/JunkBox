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
 * @brief	AVR�n�[�h�E�F�A�ˑ��̃R�[�h
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	�쐬
 * @date	2010/08/15 16:11:02	�@��ˑ������Ƃ��ĕ���
 * @date	2010/09/11 11:01:20	�O�����荞�݂ւ̑Ή���ǉ�
 */

#ifndef __hw_h__
#define __hw_h__

#include "../../src/syscall.h"
#include "./context.h"

/**
 * @brief	�n�[�h�E�F�A�S�ʂ̏���������
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	�쐬
 */
extern void initHardware(void);

/**
 * @brief		�R���e�L�X�g�̏�����
 * @param[in]	tid �^�X�N�ԍ�
 * @author		Kazuya Fukuhara
 * @date		2010/08/15 16:11:02	�쐬
 */
extern void resetContext(taskid_t tid);

/**
 * @brief					�V�X�e���R�[���Ăяo��
 * @param[in,out] param		�Ăяo���V�X�e���R�[���̂h�c������̊i�[���ꂽ�\���̂ւ̃|�C���^
 * @author					Kazuya Fukuhara
 * @date					2010/09/01 21:36:49	�쐬
 *
 * WinAVR�ł�register�C���q���L���ł���A��P�����̓��W�X�^�n���œn�����B
 * �܂��AAVR�ɂ̓\�t�g�E�F�A���荞�݂��Ȃ����߁AINT0���荞�݂��\�t�g�E�F�A���荞�݂Ƃ��ė��p���Ă���B
 */
extern  svcresultid_t NAKED syscall(register ptr_t param);

/**
 * @brief	���������O�����荞�݂̔ԍ�
 * @author	Kazuya Fukuhara
 * @date	2010/09/11 11:01:20	�쐬
 */
extern uint8_t ExtIntID;

/**
 * @def		GetExtIntId
 * @brief	���������O�����荞�ݔԍ��̎擾
 * @author	Kazuya Fukuhara
 * @date	2010/09/11 11:01:20	�쐬
 */
#define GetExtIntId() ExtIntID

/**
 * @enum	interrupt_id_t
 * @typedef	enum interrupt_id_t interrupt_id_t
 * @brief	���荞�ݔԍ�
 * @author	Kazuya Fukuhara
 * @date	2010/09/11 11:01:20	�쐬
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
 * @brief	�O�����荞�݂̌�
 * @author	Kazuya Fukuhara
 * @date	2010/09/11 11:01:20	�쐬
 */
#define EXTINT_NUM INT_MAX

/**
 * @brief			�t�b�N�\�ȊO�����荞�ݔԍ�������
 * @param[in] id	�O�����荞�ݔԍ�
 * @retval TRUE		�t�b�N�\�ȊO�����荞�ݔԍ�
 * @retval FALSE	�t�b�N�s�\�ȊO�����荞�ݔԍ�
 * @author			Kazuya Fukuhara
 * @date			2010/09/11 11:01:20	�쐬
 */
extern bool_t is_hookable_interrupt_id(extintid_t int_id);

/**
 * @def		disableInterrupt
 * @brief	���荞�݂��֎~����
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	�쐬
 * @warning	�V�X�e���R�[���Ăяo�����s�\�ɂȂ�B
 */
#define disableInterrupt() cli()

/**
 * @def		enableInterrupt
 * @brief	���荞�݂�������
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	�쐬
 */
#define enableInterrupt() sei()

/**
 * @def		EnableExtInterrupt
 * @brief	����̊O�����荞�ݔԍ��̊��荞�݂�L��������
 * @author	Kazuya Fukuhara
 * @date	2010/09/11 11:01:20	�쐬
 * @warning	������
 */
#define EnableExtInterrupt(x) /* AVR�ł͊��荞�݂̃��x���͌Œ�Ȃ̂ŁA�ʓr��p�̏������쐬���邱�� */

/**
 * @def		DisableExtInterrupt
 * @brief	����̊O�����荞�ݔԍ��̊��荞�݂𖳌�������
 * @author	Kazuya Fukuhara
 * @date	2010/09/11 11:01:20	�쐬
 * @warning	������
 */
#define DisableExtInterrupt(x) /* AVR�ł͊��荞�݂̃��x���͌Œ�Ȃ̂ŁA�ʓr��p�̏������쐬���邱�� */

#endif
