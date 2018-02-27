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

#include "../../src/arch.h"
#include "../../src/kernel.h"
#include "../../src/syscall.h"

NAKED_ISR void TIMER0_COMPA_vect(void);

/**
 * @brief	�^�C�}�[���荞�݃n���h��
 * @note	�ʏ�̋L�q�i ISR(TIMER0_COMPA_vect) �j�ł̓v�����[�O�ƃG�s���[�O����������Ă��܂��̂ŁA���O�Ő錾�L�q���s���Anaked������t�^���Ă���B
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	�쐬
 */
void TIMER0_COMPA_vect(void) {
	/* ���̎��_�ł͂ǂ̃R���e�L�X�g���g���Ă��邩�s���Ȃ̂ŃX�^�b�N���g���Ȃ� */

	/* ���݂̃^�X�N�̃R���e�L�X�g��ۑ� */
	SAVE_CONTEXT();

	/* �J�[�l���X�^�b�N�ɐ؂�ւ� */
	SET_KERNEL_STACKPOINTER();

	/* �ȍ~�̓J�[�l���X�^�b�N�̗��p���\�ɂȂ����̂ŁA�֐��Ăяo���Ȃǂ��\�ƂȂ� */

	/* �X���[�v��ԃ^�X�N�̑҂����Ԃ��X�V���� */
	updateTick();

	/* �X�P�W���[�����O���s���A���Ɏ��s����^�X�N��I������ */
	scheduler();
}

/**
 * @brief	�J�[�l���^�C�}�̏�����
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	�쐬
 */
static void initKernelTimer(void) {
	/**
	 * �^�C�}�J�E���^0���䃌�W�X�^A��ݒ�
	 * 0b00000010
	 *     ||||||
	 *     ||||++-----  WGM01: WGM00  0b10  CTC����
	 *     ||++------- COM0B1:COM0B0  0b00  �W������(OC0B�ؒf)
	 *     ++--------- COM0A1:COM0A0  0b00  �W������(OC0A�ؒf)
	 */
	TCCR0A = 0b00000010;

	/**
	 * �^�C�}�J�E���^1���䃌�W�X�^B��ݒ�
	 * FOC0A:FOC0B		 00
	 * WGM02			  0	CTC����
	 * CS02:CS01:CS00	010	�������8�����ɐݒ�
	 */
	TCCR0B = 0b00000010;

	/**
	 * �^�C�}�J�E���^��r���W�X�^�ɃJ�E���^�l��ݒ�
	 * �������8�����ɐݒ肵�Ă���̂ŁA20Mhz/8 = 2.5Mhz �̃J�E���^�ƂȂ�B
	 * 8bit�^�C�}���g��250�J�E���g�ɂP��̊��荞�݂̐ݒ�ɂ����
	 * 2.5Mhz/250 = 2500Khz/250 = 10Khz�ƂȂ�A
	 * 0.1ms�Ԋu�ŃX�P�W���[�������s����邱�ƂɂȂ�
	 */
	OCR0A = 250;

	/**
	 * �^�C�}�J�E���^0���荞�݃}�X�N���W�X�^
	 * ICIE0  0
	 * OCIE0B 0
	 * OCIE0A 1 �^�C�}�J�E���^0��rA���荞�݋���
	 * TOIE0  0
	 */
	TIMSK0 = 0b00000010;
}

NAKED_ISR void INT0_vect(void);

/**
 * @brief	AVR��INT0���荞�݃x�N�^�ɓo�^���鏈��
 * @note	�ʏ�̊��荞�݃��[�`���̐錾 ISR(INT0_vect) �ł̓v�����[�O�ƃG�s���[�O����������Ă��܂��B
 * @note	���̂��߁A���O�Ő錾�L�q���s���Anaked������t�^���Ă���B
 * @note	�܂��AINT0���荞�݂��Ăяo���V�X�e���R�[���֐��ł͈�����register�����Ő錾���Ă��邽�߁A
 * @note	�V�X�e���R�[���֐��ɓn���ꂽ�����̓��W�X�^�Ɋi�[���ꂽ�܂܌Ăяo����邱�ƂɂȂ�B
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	�쐬
 */
void INT0_vect(void) {
	/* �R���e�L�X�g��ۑ� */
	SAVE_CONTEXT();

	/* �J�[�l���X�^�b�N�ɐ؂�ւ� */
	SET_KERNEL_STACKPOINTER();

	/* EXT0���荞�݂����Z�b�g */
	PORTD &= ~_BV(PORTD2);

	/* �V�X�e���R�[���Ăяo�������Ɉړ� */
	syscall_entry();
}

/**
 * @brief					�V�X�e���R�[���Ăяo��
 * @param[in,out] param		�Ăяo���V�X�e���R�[���̂h�c������̊i�[���ꂽ�\���̂ւ̃|�C���^
 * @author					Kazuya Fukuhara
 * @date					2010/09/01 21:36:49	�쐬
 *
 * WinAVR�ł�register�C���q���L���ł���A��P�����̓��W�X�^�n���œn�����B
 * �܂��AAVR�ɂ̓\�t�g�E�F�A���荞�݂��Ȃ����߁AINT0���荞�݂��\�t�g�E�F�A���荞�݂Ƃ��ė��p���Ă���B
 */
svcresultid_t NAKED syscall(register ptr_t param) {
	(void)param;
	asm volatile(
		"	sbi	0x0B,	2		\n\t"
		"	ret					\n\t"
	);
	return 0;	// dummy
}

/**
 * @brief	���������O�����荞�݂̔ԍ�
 * @author	Kazuya Fukuhara
 * @date	2010/09/11 11:01:20	�쐬
 */
uint8_t ExtIntID = 0;

/**
 * @brief			�t�b�N�\�ȊO�����荞�ݔԍ�������
 * @param[in] id	�O�����荞�ݔԍ�
 * @retval TRUE		�t�b�N�\�ȊO�����荞�ݔԍ�
 * @retval FALSE	�t�b�N�s�\�ȊO�����荞�ݔԍ�
 * @author			Kazuya Fukuhara
 * @date			2010/09/11 11:01:20	�쐬
 */
bool_t is_hookable_interrupt_id(extintid_t int_id) {
	switch (int_id) {
		case INT_RESET :	/* ���Z�b�g���荞�݂̓t�b�N�ł��Ȃ� */
		case INT_INT0 :		/* INT0���荞�݂̓V�X�e���R�[���Ăяo���Ŏg�����߃t�b�N�ł��Ȃ� */
		case INT_TIMER0_COMPA:	/* �^�C�}�[�O�̓J�[�l���^�C�}�[�Ŏg�p�� */
		case INT_TIMER0_COMPB:	/* �^�C�}�[�O�̓J�[�l���^�C�}�[�Ŏg�p�� */
		case INT_TIMER0_OVF:	/* �^�C�}�[�O�̓J�[�l���^�C�}�[�Ŏg�p�� */
			return FALSE;
		default:
			break;
	}
	return TRUE;
}

/**
 * @brief	�\�t�g�E�F�A���荞�݂̏�����
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	�쐬
 */
static void initSoftwareInterrupt(void) {
	/**
	 * AVR�ɂ̓\�t�g�E�F�A���荞�݂����݂��Ȃ��B
	 * �������A�O�����荞�݂� INT0(PD2) �|�[�g���o�͂ɐݒ肵�Ă��g�p�\�ƂȂ��Ă���A
	 * �v���O�������� INT0(PD2) �ɏo�͂��邱�ƂŁA�O�����荞�݂𔭐������邱�Ƃ��ł���B
	 * ����𗘗p���āA�O�����荞�� INT0(PD2) ���\�t�g�E�F�A���荞�݂̑�p�Ƃ���B
	 */

	/* �O�����荞�� INT0(PD2) �͏o�̓��[�h */
	DDRD  |=  _BV(PORTD2);
	PORTD &= ~_BV(PORTD2);

	/* �O�����荞�ݏ���: INT0(PD2) �̗����オ��Ŕ��� */
	EICRA |= (_BV(ISC01)|_BV(ISC00));

	/* �O�����荞�݃}�X�N���W�X�^: INT0(PD2) �̊��荞�݂����� */
	EIMSK |= _BV(INT0);
}

/**
 * @brief	�n�[�h�E�F�A�S�ʂ̏���������
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	�쐬
 */
void initHardware(void) {
	/*
	 * ���荞�݋֎~��Ԃɐݒ�
	 */
	cli();

	/**
	 * �\�t�g�E�F�A���荞�݂�������
	 */
	initSoftwareInterrupt();

	/**
	 * �J�[�l���^�C�}��������
	 */
	initKernelTimer();
}

/**
 * @brief			�R���e�L�X�g�̏�����
 * @param[in] tid	�^�X�N�ԍ�
 * @author			Kazuya Fukuhara
 * @date			2010/08/15 16:11:02	�쐬
 */
void resetContext(taskid_t tid) {
	context_t* context;
	
	/* �X�^�b�N�|�C���^�̏����l��ݒ� */
	tasks[tid].stack_pointer = &task_stack[tid][TASK_STACK_SIZE-sizeof(context_t)-3];

	/* �������p�R���e�L�X�g���擾���A�[���N���A */
	context = (context_t*)&task_stack[tid][TASK_STACK_SIZE-sizeof(context_t)];
	ZeroMemory(context, sizeof(context_t));

	/* exit�A�h���X�̐ݒ� */
	SetReturnAddressToContext(context, API_exitTASK);

	/* �R���e�L�X�g�̐ݒ� */
	context = ((context_t*)&task_stack[tid][TASK_STACK_SIZE-sizeof(context_t)-2]);

	/* �J�n�A�h���X�l�̐ݒ� */
	SetReturnAddressToContext(context, TaskTable[tid]);
}

