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
 * @brief   Win32�ˑ��̃R�[�h
 * @author  Kazuya Fukuhara
 * @date	2010/09/01 21:36:49	�쐬
 */

#include <process.h>
#include "../../src/arch.h"
#include "../../src/config.h"
#include "../../src/kernel.h"
#include "../../src/type.h"
#include "../../src/syscall.h"
#include "./hw.h"
#include "./virtualhw.h"
#include "./vd_kerneltimer.h"
#include "./vd_button.h"

/**
 * @brief   �n�[�h�E�F�A�S�ʂ̏���������
 * @author  Kazuya Fukuhara
 * @date	2010/09/01 21:36:49	�쐬
 */
void initHardware(void) {
	initVirtualHardware();	/* ���z�n�[�h�E�F�A�̏����� */
	initKernelTimer();		/* �J�[�l���^�C�}�������� */
	initButtonDevice();		/* �{�^���������� */
	startVirtualHardware();	/* ���z�n�[�h�E�F�A�̎��s���J�n */
	Sleep(1000);
}

/**
 * @brief	���荞�݂��֎~����
 * @author	Kazuya Fukuhara
 * @date	2010/09/01 21:36:49	�쐬
 */
void disableInterrupt(void) { 
	VHW_disableInterrupt();
}

/**
 * @brief	���荞�݋֎~����
 * @author  Kazuya Fukuhara
 * @date	2010/09/01 21:36:49	�쐬
 */
void enableInterrupt(void) {
	VHW_enableInterrupt();
}

/**
 * @brief			�R���e�L�X�g�̏�����
 * @param[in] tid	�^�X�N�ԍ�
 * @author			Kazuya Fukuhara
 * @date			2010/09/01 21:36:49	�쐬
 */
void resetContext(taskid_t tid) {
	context_init_t *context_init; 

	/* �������p�R���e�L�X�g���擾���A�[���N���A */
	context_init = (context_init_t*)&task_stack[tid][TASK_STACK_SIZE-sizeof(context_init_t)];
	ZeroMemory(context_init, sizeof(context_init_t));

	/* �X�^�b�N�̏����l��ݒ� */
	tasks[tid].stack_pointer = (uint8_t*)context_init;
	context_init->context.ebp = (register_t)&task_stack[tid][TASK_STACK_SIZE-sizeof(register_t)];

	/* �R���e�L�X�g�𔲂����ۂɎ����I��exit���Ă΂��悤�ɐݒ� */
	context_init->exit_proc = (register_t)API_exitTASK;

	/* �J�n�A�h���X�l�̐ݒ� */
	SetReturnAddressToContext((context_t*)context_init, TaskTable[tid]);

}

/**
 * @brief					�V�X�e���R�[���Ăяo��
 * @param[in,out] param		�Ăяo���V�X�e���R�[���̂h�c������̊i�[���ꂽ�\���̂ւ̃|�C���^
 * @author					Kazuya Fukuhara
 * @date					2010/09/01 21:36:49	�쐬
 *
 * Win32�ł�stdcall�Ăяo���K��������X�^�b�N�n���œn���B
 */
void syscall(ptr_t param) {

	/* ���荞�݋֎~ */
	disableInterrupt();

	/* �����Ɩ߂���ݒ� */
	__asm {
		push	param
		push	L_syscall_exit	/* �߂��A�h���X */
	}
	/* �R���e�L�X�g��ۑ� */
	SAVE_CONTEXT();

	/* �J�[�l���X�^�b�N�ɐ؂�ւ� */
	SET_KERNEL_STACKPOINTER();

	/* �V�X�e���R�[���Ăяo�������Ɉړ� */
	syscall_entry();

	/* �V�X�e���R�[������̖߂�� */
	__asm {
L_syscall_exit:
		add	esp,	0x04	/* �����P��(=4�o�C�g)�X�^�b�N��������� */
	}
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
	if (int_id == INTID_TIMER) {
		return FALSE;
	} else {
		return TRUE;
	}
}

