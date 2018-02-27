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

#include "../../src/arch.h"
#include "../../src/kernel.h"
#include "../../src/syscall.h"

/**
 * @def		NAKED_FUNC
 * @brief	�v�����[�O�̃R�[�h��S���������Ȃ��֐��̐錾�i�����̏��Z�j
 * @author	Kazuya Fukuhara
 * @date	2010/09/04 19:46:15	�쐬
 * 
 * ���荞�݂�V�X�e���R�[�������ȂǂŃX�^�b�N�t���[���������ƍ���ꍇ��
 * �v�����[�O�̃R�[�h��S���������Ȃ��֐���錾���邽�߂̏����̃e�N�j�b�N
 */
#define NAKED_FUNC(x,y) \
static void naked_##x##_ y { \
		asm( ".glb _" #x ); \
		asm( "_" #x ":" );

/**
 * @def		TT0MR
 * @brief	�^�C�}�[A0�̃��[�h���W�X�^
 * @author	Kazuya Fukuhara
 * @date	2010/09/04 19:46:15	�쐬
 */
#define TT0MR	(*(volatile uint8_t*)0x0396)

/**
 * @def		TA0CNT
 * @brief	�^�C�}�[A0�̃J�E���^���W�X�^(0387h:0386h�Ԓn)
 * @author	Kazuya Fukuhara
 * @date	2010/09/04 19:46:15	�쐬
 */
#define TA0CNT	(*(volatile uint16_t*)0x0386)

/**
 * @def		TCNTFR
 * @brief	�^�C�}�[A0�̃J�E���g�J�n�t���O���W�X�^
 * @author	Kazuya Fukuhara
 * @date	2010/09/04 19:46:15	�쐬
 */
#define TCNTFR	(*(volatile uint8_t*)0x0380)

/**
 * @brief	�J�[�l���X�^�b�N�̈�
 * @author	Kazuya Fukuhara
 * @date	2010/09/04 19:46:15	�쐬
 */
uint8_t kernel_stack[KERNEL_STACK_SIZE];

/**
 * @brief	�J�[�l���X�^�b�N�̐擪�|�C���^
 * @author	Kazuya Fukuhara
 * @date	2010/09/04 19:46:15	�쐬
 */
const uint8_t *kernel_stack_ptr = &kernel_stack[KERNEL_STACK_SIZE];

/**
 * @brief	�^�C�}�[���荞�݃n���h��
 * @author	Kazuya Fukuhara
 * @date	2010/09/04 19:46:15	�쐬
 */
NAKED_FUNC(timer_int,(void))

	/* ���荞�݂��֎~���� */
	disableInterrupt();	// fclr	I
	
	/* �J�E���^TA0(�^�C�}A0���W�X�^ : 0387h:0386h�Ԓn)��1250�ɂ���B(�_�E���J�E���g���g������) */
	TA0CNT = 1250;	// mov.w	#1250,	TA0CNT

	/* �R���e�L�X�g�ޔ� */
	SAVE_CONTEXT();

	/* ISP�ɃJ�[�l���̃X�^�b�N�A�h���X�������� */
	SET_KERNEL_STACKPOINTER();
	
	/* �X���[�v��ԃ^�X�N�̑҂����Ԃ��X�V���� */
	updateTick();

	/* �X�P�W���[�����O���s���A���Ɏ��s����^�X�N��I������ */
	scheduler();
}

/**
 * @brief	�J�[�l���^�C�}�̏�����
 * @author	Kazuya Fukuhara
 * @date	2010/09/04 19:46:15	�쐬
 */
static void initKernelTimer(void) {
	TT0MR   = 0x40;
	TA0CNT  = 1250;	/* 1ms���荞�� */
	TCNTFR  = 1;
	INTCR[INT_TIMERA0] = 6;
}

/**
 * @brief	�\�t�g�E�F�A���荞�݂̏�����
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	�쐬
 */
static void initSoftwareInterrupt(void) {
	INTCR[INT_SWINT] = 6;
}

/**
 * @brief	�n�[�h�E�F�A�S�ʂ̏���������
 * @author	Kazuya Fukuhara
 * @date	2010/09/04 19:46:15	�쐬
 */
void initHardware(void) {
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
 * @date			2010/09/04 19:46:15	�쐬
 */
void resetContext(taskid_t tid) {
	context_t* context;
	const taskproc_t StartAddress = TaskTable[tid];

	/* �������p�R���e�L�X�g���擾���A�[���N���A */
	context = (context_t*)&task_stack[tid][TASK_STACK_SIZE-sizeof(context_t)];
	ZeroMemory(context, (uint_t)sizeof(context_t));

	/* �X�^�b�N�|�C���^�̏����l��ݒ� */
	tasks[tid].stack_pointer = (uint8_t*)context;

	/* �R���e�L�X�g�𔲂����ۂɎ����I��exit���Ă΂��悤�ɐݒ� */
	context->HOOK[0] = (uint8_t)(((uint32_t)API_exitTASK      ) & 0xFF);
	context->HOOK[1] = (uint8_t)(((uint32_t)API_exitTASK >>  8) & 0xFF);
	context->HOOK[2] = (uint8_t)(((uint32_t)API_exitTASK >> 16) & 0xFF);

	/* �R���e�L�X�g�̐ݒ� */
	context->R0 = 0;
	context->R1 = 0;
	context->R2 = 0;
	context->R3 = 0;
	context->A0 = 0;
	context->A1 = 0;
	context->SB = 0;
	context->FB = 0;
	context->FLG.Value = 0x40;

	/* �J�n�A�h���X�l�̐ݒ� */
	context->PC_L  = (uint8_t)(((uint32_t)StartAddress      ) & 0x0000FF);
	context->PC_M  = (uint8_t)(((uint32_t)StartAddress >>  8) & 0x0000FF);
	context->PC_H  = (uint8_t)(((uint32_t)StartAddress >> 16) & 0x0000FF);
}

/**
 * @brief	�O�����荞�݂Ŕ����������荞�݂̔ԍ����i�[
 * @author	Kazuya Fukuhara
 * @date	2010/09/04 19:46:15	�쐬
 */
uint8_t ExtIntID;

/**
 * @brief	�O�����荞�݃n���h��
 * @author	Kazuya Fukuhara
 * @date	2010/09/04 19:46:15	�쐬
 */
NAKED_FUNC(external_interrupt,(void))
	/* ���荞�ݔ���
	 * PC��FLG�͎����I�ɃX�^�b�N��ɑޔ�����Ă���B
	 * �����ɓ��B�������_�ŁAISP�̒l�͎��s���̃^�X�N�̍ŐV�̃X�^�b�N�|�C���^�ƂȂ��Ă���B
	 */

	/* �R���e�L�X�g�ޔ� */
	SAVE_CONTEXT();

	/* �J�[�l���X�^�b�N�֐؂�ւ� */
	SET_KERNEL_STACKPOINTER();

	/* �O�����荞�ݏ����֐��� */
	external_interrupt_handler();	
}

/**
 * @brief					�V�X�e���R�[���Ăяo��
 * @param[in,out] param		�Ăяo���V�X�e���R�[���̂h�c������̊i�[���ꂽ�\���̂ւ̃|�C���^
 * @author					Kazuya Fukuhara
 * @date					2010/09/01 21:36:49	�쐬
 */
NAKED_FUNC(syscall, (ptr_t param) )
	asm("INT_SYSCALL .equ 32");			/* �V�X�e���R�[���p�\�t�g�E�F�A���荞�ݔԍ� */
	asm("int #INT_SYSCALL");	/* �\�t�g�E�F�A���荞�݂ŃV�X�e���R�[�� */
	asm("rts");
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
		case INT_UART1_TX :	/* �f�o�b�K�Ŏg�p�� */
		case INT_UART1_RX :	/* �f�o�b�K�Ŏg�p�� */
		case INT_TIMERA0 :	/* �J�[�l���^�C�}�[�Ŏg�p�� */
			return FALSE;
		default:
			if (int_id >= INT_SWINT) {	/* �\�t�g�E�F�A���荞�� */
				return FALSE;
			}
	}
	return TRUE;
}

/**
 * @brief	�V�X�e���R�[���{��
 * @note	�Ăяo���V�X�e���R�[����ID��R0�ɁA�����P�E�Q�͂��ꂼ��R1��R2�Ɋi�[����Ă���K�v������B
 * @author	Kazuya Fukuhara
 * @date	2010/09/04 19:46:15	�쐬
 */
NAKED_FUNC(software_int,(void))
	/* ���荞�݂��֎~���� */
	disableInterrupt();

	/* ���荞�ݔ���
	 * PC��FLG�͎����I�ɃX�^�b�N��ɑޔ�����Ă���B
	 * �����ɓ��B�������_�ŁAISP�̒l�͎��s���̃^�X�N�̍ŐV�̃X�^�b�N�|�C���^�ƂȂ��Ă���B
	 */
	/* �R���e�L�X�g�ޔ� */
	SAVE_CONTEXT();

	/* �J�[�l���X�^�b�N�֐؂�ւ� */
	SET_KERNEL_STACKPOINTER();
	
	/* �V�X�e���R�[���̎�ʂ𔻒肵�Ė{�̂ɕ��򂷂�֐��֖��������� */
	syscall_entry();
}

/**
 * @brief	�u�[�g���[�_����Ăяo�����X�^�[�g�A�b�v���[�`��
 * @author	Kazuya Fukuhara
 * @date	2010/09/01 21:36:49	�쐬
 */
void main(void) {
	/* �J�[�l���X�^�b�N�֐؂�ւ� */
	SET_KERNEL_STACKPOINTER();

	/* �ȍ~�A�J�[�l���X�^�b�N�����p�\�ɂȂ� */

	/* �J�[�l���̉ғ��J�n */
	startKernel();	
}

