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
 * @brief	M16C T-Bird�n�[�h�E�F�A�ˑ��̃R�[�h
 * @author	Kazuya Fukuhara
 * @date	2010/09/04 19:46:15	�쐬
 * @date	2010/09/11 11:01:20	�O�����荞�݂ւ̑Ή���ǉ�
 */

#ifndef __hw_h__
#define __hw_h__

#include "../../src/syscall.h"
#include "./context.h"

/**
 * @brief	�n�[�h�E�F�A�S�ʂ̏���������
 * @author	Kazuya Fukuhara
 * @date	2010/09/04 19:46:15	�쐬
 */
extern void initHardware(void);

/**
 * @brief			�R���e�L�X�g�̏�����
 * @param[in] tid	�^�X�N�ԍ�
 * @author			Kazuya Fukuhara
 * @date			2010/09/04 19:46:15	�쐬
 */
extern void resetContext(taskid_t tid);

/**
 * @brief					�V�X�e���R�[���Ăяo��
 * @param[in,out] param		�Ăяo���V�X�e���R�[���̂h�c������̊i�[���ꂽ�\���̂ւ̃|�C���^
 * @author					Kazuya Fukuhara
 * @date					2010/09/01 21:36:49	�쐬
 */
extern  svcresultid_t syscall(register ptr_t param);
#pragma Parameter syscall(R0);

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
 * @date	2010/09/04 19:46:15	�쐬
 */
typedef enum interrupt_id_t {
	INT_BRK  = 0,		/**< BRK���� */
	INT_INT3 = 4,		/**< IRQ:INT3 */
	INT_TIMER_B5 = 5,	/**< �^�C�}B5 */
	INT_TIMER_B6 = 6,	/**< �^�C�}B4 */
	INT_TIMER_B7 = 7,	/**< �^�C�}B3 */
	INT_INT5 = 8,		/**< IRQ:INT5 */
	INT_INT4 = 9,		/**< IRQ:INT4 */
	INT_BUS = 10,		/**< �o�X�Փˌ��o */
	INT_DMA0 = 11,		/**< DMA0 */
	INT_DMA1 = 12,		/**< DMA1 */
	INT_KEYINPUT = 13,	/**< �L�[���͊��荞�� */
	INT_AD = 14,		/**< A/D�ϊ����荞�� */
	INT_UART2_TX= 15,	/**< UART2���M���荞�� */
	INT_UART2_RX = 16,	/**< UART2��M���荞�� */
	INT_UART0_TX = 17,	/**< UART0���M���荞�� */
	INT_UART0_RX = 18,	/**< UART0��M���荞�� */
	INT_UART1_TX = 19,	/**< UART1���M���荞�� */
	INT_UART1_RX = 20,	/**< UART1��M���荞�� */
	INT_TIMERA0 = 21,	/**< �^�C�}A0 */
	INT_TIMERA1 = 22,	/**< �^�C�}A1 */
	INT_TIMERA2 = 23,	/**< �^�C�}A2 */
	INT_TIMERA3 = 24,	/**< �^�C�}A3 */
	INT_TIMERA4 = 25,	/**< �^�C�}A4 */
	INT_TIMERB0 = 26,	/**< �^�C�}B0 */
	INT_TIMERB1 = 27,	/**< �^�C�}B1 */
	INT_TIMERB2 = 28,	/**< �^�C�}B2 */
	INT_INT0 = 29,		/**< IRQ:INT0 */
	INT_INT1 = 30,		/**< IRQ:INT1 */
	INT_INT2 = 31,		/**< IRQ:INT2 */
	INT_SWINT = 32,		/**< �\�t�g�E�F�A���荞��(�ȍ~31��) */
} interrupt_id_t;

/**
 * @def		EXTINT_NUM
 * @brief	�O�����荞�݂̌�
 * @author	Kazuya Fukuhara
 * @date	2010/09/11 11:01:20	�쐬
 */
#define EXTINT_NUM 32

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
 * @date	2010/09/04 19:46:15	�쐬
 */
#define disableInterrupt() asm("fclr I")

/**
 * @def		enableInterrupt
 * @brief	���荞�݂�������
 * @author	Kazuya Fukuhara
 * @date	2010/09/04 19:46:15	�쐬
 */
#define enableInterrupt() asm("fset	I")

/**
 * @def		INTCR
 * @brief	���荞�݃R���g���[�����W�X�^�̐擪�A�h���X
 * @author	Kazuya Fukuhara
 * @date	2010/09/04 19:46:15	�쐬
 */
#define INTCR ((volatile uint8_t*)0x0040)

/**
 * @def		EnableExtInterrupt
 * @brief	����̊O�����荞�ݔԍ��̊��荞�݂�L��������
 * @author	Kazuya Fukuhara
 * @date	2010/09/11 11:01:20	�쐬
 *
 * ���荞�݃��x����7���ő傾���A���j�^�f�o�b�K�����x��7���g�����߁A
 * ���x����6�ɐݒ肷��
 */
#define EnableExtInterrupt(x) { INTCR[(x) & 63] = 6; }

/**
 * @def		DisableExtInterrupt
 * @brief	����̊O�����荞�ݔԍ��̊��荞�݂𖳌�������
 * @author	Kazuya Fukuhara
 * @date	2010/09/11 11:01:20	�쐬
 * 
 * ���荞�݃��x����0�ɐݒ肷��
 */
#define DisableExtInterrupt(x) { INTCR[(x) & 63] = 0; }

#endif
