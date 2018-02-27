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
 * @brief	Win32���ł̃J�[�l���^�C�}�p�̉��z�f�o�C�X
 * @author	Kazuya Fukuhara
 * @date	2010/09/01 21:36:49	�쐬
 */

#include "./arch.h"
#include "./virtualhw.h"
#include "./vd_kerneltimer.h"
#include "../../src/kernel.h"
#include <Windows.h>
#include <MMSystem.h>
#pragma comment(lib,"winmm.lib")

/**
 * @brief	�J�[�l���^�C�}�f�o�C�X�̃��C������
 * @author	Kazuya Fukuhara
 * @date	2010/09/01 21:36:49	�쐬
 */
static void KernelTimerProc(void) {
	for (;;) {
		Sleep(10);
		VHW_onInterrupt(INTID_TIMER);
	}
}

/**
 * @brief	�J�[�l���^�C�}���荞�݃n���h��
 * @author	Kazuya Fukuhara
 * @date	2010/09/01 21:36:49	�쐬
 */
void NAKED KernelTimerInterruptServiceRoutine(void) {
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
 * @note	�J�[�l���^�C�}�̉��z�f�o�C�X�Ɗ��荞�݃��[�`����ݒ�
 * @author	Kazuya Fukuhara
 * @date	2010/09/01 21:36:49	�쐬
 */
void initKernelTimer(void) {
	/* �J�[�l���^�C�}�����z�f�o�C�X�Ƃ��ăC���X�g�[�� */
	virtualdevice_t vd;
	vd.name = "KernelTimer";
	vd.vdsr = KernelTimerProc;

	timeBeginPeriod(1);
	VHW_installVirtualDevice(&vd);
	/* �J�[�l���^�C�}����������INTID_TIMER�C�x���g�̊��荞�݃��[�`�����w�� */
	VHW_setInterruputServiceRoutine(INTID_TIMER, KernelTimerInterruptServiceRoutine); 
}

