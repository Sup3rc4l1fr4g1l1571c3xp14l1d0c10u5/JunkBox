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
 * @brief	Win32���Ŋ��荞�݂���Ӌ@����Č����邽�߂̉��z�n�[�h�E�F�A
 * @author	Kazuya Fukuhara
 * @date	2010/09/01 21:36:49	�쐬
 *
 * Win32���Ń^�C�}�Ȃǂ̃y���t�F������O���f�o�C�X���Č����邽�߂ɉ��z�n�[�h
 * �E�F�A�Ƃ��Ď������Ă���B
 * ���z�n�[�h�E�F�A�ł͈�̃y���t�F�����i�f�o�C�X�j�ɃX���b�h������蓖�Ă�
 * �ʂɉғ������A���荞�ݗv���n���h���ŃJ�[�l���X���b�h�ɑ΂��Ċ��荞�݂��s���A
 * �R���e�L�X�g�����������邱�ƂŊ��荞�݂��Č����Ă���B
 */

#include "./arch.h"
#include "./virtualhw.h"
#include "../../src/type.h"
#include <windows.h>
#include <process.h>

/**
 * @typedef	struct virtualdevice_entry_t virtualdevice_entry_t
 * @struct	virtualdevice_entry_t
 * @brief	���z�n�[�h�E�F�A�ɓo�^���ꂽ�f�o�C�X�̏��
 * @author	Kazuya Fukuhara
 * @date	2010/09/01 21:36:49	�쐬
 */
typedef struct virtualdevice_entry_t {
	char *name;	/**< �f�o�C�X��(�f�o�C�X�̎��ʂɎg����) */
	virtualdevice_serviceroutine_t vdsr;	/**< �f�o�C�X�̃��C�������Ƃ��Ď��s�����֐��̃|�C���^ */
	unsigned int thread_id;	/**< �f�o�C�X�Ɋ��蓖�Ă��X���b�h��ID */
	HANDLE thread_handle;	/**< �f�o�C�X�Ɋ��蓖�Ă��X���b�h�̃n���h�� */
} virtualdevice_entry_t;

/**
 * @brief	���z�n�[�h�E�F�A�ɃC���X�g�[������Ă��鉼�z�f�o�C�X�̃e�[�u��
 * @author	Kazuya Fukuhara
 * @date	2010/09/01 21:36:49	�쐬
 */
static virtualdevice_entry_t VirtualDeviceTable[16];

/**
 * @brief			���z�f�o�C�X�̃��C���������Ăяo���X���b�h�֐�
 * @param[in] param	���s���鉼�z�f�o�C�X�̃G���g��(virtualdevice_entry_t*�^)
 * @retval == 0		�������ُ�
 * @author			Kazuya Fukuhara
 * @date			2010/09/01 21:36:49	�쐬
 */
static unsigned int __stdcall VirtualDeviceEntryProc(void* param) {
	virtualdevice_entry_t* entry = (virtualdevice_entry_t*)param;
	if (entry) {
		entry->vdsr();
		for (;;) {
		}
	}
	return 0;
}

/**
 * @brief	���z�n�[�h�E�F�A�̊��荞�ݏ�Ԃ������t���O
 * @note	�����̃X���b�h����ǂݏ��������
 * @author	Kazuya Fukuhara
 * @date	2010/09/01 21:36:49	�쐬
 */
static volatile LONG flag_interrupt_lock = 1;

/**
 * @brief	�J�[�l���X���b�h�̃X���b�hID
 * @author	Kazuya Fukuhara
 * @date	2010/09/01 21:36:49	�쐬
 */
static DWORD kernel_thread_id = 0;

/**
 * @brief	�J�[�l���X���b�h�́u�^�́v�n���h���l
 * @author	Kazuya Fukuhara
 * @date	2010/09/01 21:36:49	�쐬
 */
static HANDLE kernel_thread_handle = 0;

/**
 * @brief	�J�[�l���̊��荞�݋֎~��ԂȂǂ������N���e�B�J���Z�N�V����
 * @author	Kazuya Fukuhara
 * @date	2010/09/01 21:36:49	�쐬
 */
static CRITICAL_SECTION kernel_cs;

/**
 * @brief	���z�n�[�h�E�F�A�̏�����
 * @author	Kazuya Fukuhara
 * @date	2010/09/01 21:36:49	�쐬
 * 
 */
void initVirtualHardware(void) {
	int i;

	/* ���荞�݃t���O�̏����� */
	flag_interrupt_lock = 1;

	/* �X���b�hID���擾 */
	kernel_thread_id = GetCurrentThreadId();

	/* �N���e�B�J���Z�N�V�����̏������i�ŏ��̓��b�N��Ԃɂ��Ă����j */
	InitializeCriticalSection(&kernel_cs);
	EnterCriticalSection(&kernel_cs);

	/* �X���b�h�̋^���n���h������^�̃n���h���𓾂� */
	if (DuplicateHandle(GetCurrentProcess(), GetCurrentThread(), GetCurrentProcess(), &kernel_thread_handle, PROCESS_ALL_ACCESS, FALSE, 0) == FALSE) {
		return;
	}

	/* ���z�f�o�C�X�e�[�u���������� */
	for (i=0; i<countof(VirtualDeviceTable); i++) {
		VirtualDeviceTable[i].thread_handle = (HANDLE)-1;
		VirtualDeviceTable[i].name = "";
	}
}

/**
 * @brief			���z�f�o�C�X�̃C���X�g�[��
 * @param[in] vd	�C���X�g�[�����鉼�z�f�o�C�X
 * @author			Kazuya Fukuhara
 * @date			2010/09/01 21:36:49	�쐬
 */
void VHW_installVirtualDevice(virtualdevice_t* vd) {
	int i;
	virtualdevice_entry_t *entry = NULL;

	/* �J�[�l���X���b�h����̌Ăяo���݂̂����� */
	if (GetCurrentThreadId() != kernel_thread_id) {
		return;
	}

	/* �����̃f�o�C�X���o�^����Ă���ꍇ�̓C���X�g�[�����Ȃ� */
	for (i=0; i<countof(VirtualDeviceTable); i++) {
		if ((VirtualDeviceTable[i].name != NULL) && (strcmp(VirtualDeviceTable[i].name, vd->name) == 0)) {
			return;
		}
	}

	/* �e�[�u���̋󂫂�T�� */
	for (i=0; i<countof(VirtualDeviceTable); i++) {
		if (VirtualDeviceTable[i].thread_handle == (HANDLE)-1) {
			entry = &VirtualDeviceTable[i];
			break;
		}
	}
	if (entry == NULL) {
		return;
	}

	/* ���z�f�o�C�X�̓���X���b�h���~��Ԃō�� */
	entry->thread_handle = (HANDLE)_beginthreadex( NULL, 0, VirtualDeviceEntryProc, (void*)entry, CREATE_SUSPENDED, &entry->thread_id);
	if (entry->thread_handle == (HANDLE)-1) {
		return;
	}

	/* �e�[�u���ɃG���g����ǉ����� */
	entry->name = vd->name;
	entry->vdsr = vd->vdsr;

}

/**
 * @brief	���z�n�[�h�E�F�A�̓�����J�n����
 * @note	�C���X�g�[���������z�f�o�C�X�̃X���b�h�̓�����J�n����
 * @author	Kazuya Fukuhara
 * @date	2010/09/01 21:36:49	�쐬
 */
void startVirtualHardware(void) {
	int i;
	for (i=0; i<countof(VirtualDeviceTable); i++) {
		if (VirtualDeviceTable[i].name[0] != -1) {
			ResumeThread(VirtualDeviceTable[i].thread_handle);
		}
	}
}

/**
 * @brief	���z�n�[�h�E�F�A�̊��荞�݃x�N�^
 * @note	���z�n�[�h�E�F�A�Ɋ��荞�݂����������ꍇ�A�����ɋL�ڂ���Ă��銄�荞�݃��[�`�����J�[�l���X���b�h�Ŏ��s�����悤�Ɏd��������B
 * @author	Kazuya Fukuhara
 * @date	2010/09/01 21:36:49	�쐬
 */
static interruptserviceroutine_t InterruptVector[INTID_MAX];

/**
 * @def		IsValideInterruptId
 * @brief	���荞�ݔԍ����Ó�������
 * @author	Kazuya Fukuhara
 * @date	2010/09/01 21:36:49	�쐬
 */
#define IsValideInterruptId(x) ((INTID_TIMER <= (x))&&((x) < INTID_MAX))

/**
 * @brief			�J�[�l�����̊��荞�݃T�[�r�X���[�`�������z�n�[�h�E�F�A�̊��荞�݃x�N�^�ɐݒ�
 * @param[in] id	���z�n�[�h�E�F�A�̊��荞�ݔԍ�
 * @param[in] isr	���z�n�[�h�E�F�A�̊��荞�ݔԍ��ɑΉ�������J�[�l�����̊��荞�݃T�[�r�X���[�`��
 * @author			Kazuya Fukuhara
 * @date			2010/09/01 21:36:49	�쐬
 */
void VHW_setInterruputServiceRoutine(vhw_interruptid_t id, interruptserviceroutine_t isr) {
	if (!IsValideInterruptId(id)) {
		return;
	}
	InterruptVector[id] = isr;
}

/**
 * @brief			���z�n�[�h�E�F�A�̊��荞�݃x�N�^����J�[�l�����̊��荞�݃T�[�r�X���[�`�����擾
 * @param[in] id	���荞�ݔԍ�
 * @return			���荞�ݔԍ��ɑΉ����銄�荞�݃��[�`�� NULL�̏ꍇ�͖��ݒ�
 * @author			Kazuya Fukuhara
 * @date			2010/09/01 21:36:49	�쐬
 */
interruptserviceroutine_t VHW_getInterruputServiceRoutine(vhw_interruptid_t id) {
	if (!IsValideInterruptId(id)) {
		return NULL;
	}
	return InterruptVector[id];
}

/**
 * @brief			���z�n�[�h�E�F�A�Ɋ��荞�݂𔭐�������i�{�́j
 * @param[in] id	���������銄�荞�ݔԍ�
 * @author			Kazuya Fukuhara
 * @date			2010/09/01 21:36:49	�쐬
 */
static bool_t VHW_onInterruptBody(vhw_interruptid_t id) {
	CONTEXT ctx;	/* 4byte �A���C�������g�Ŋm�ۂ���邱�Ƃ����� */
	DWORD pc;
	HANDLE prc;
	BOOL ret;

	if (!IsValideInterruptId(id)) {
		return FALSE;
	}
	if (InterruptVector[id] == NULL) {
		return FALSE;
	}
	/* �J�[�l���X���b�h���ꎞ��~���� */
	if (SuspendThread(kernel_thread_handle) == -1) {
		/* �T�X�y���h���s�i�v���I�j*/
		return FALSE;
	}
	/* �X���b�h�̃R���e�L�X�g���擾*/
	ctx.ContextFlags = CONTEXT_FULL;
	if (GetThreadContext( kernel_thread_handle, &ctx ) == FALSE) {
		/* ���s�i�v���I�j */
		goto FAILED;
	}
	pc = ctx.Eip;							/* �X���b�h�̌��݂̃v���O�����J�E���^���擾 */
	ctx.Eip = (DWORD)VHW_getInterruputServiceRoutine(id);	/*�v���O�����J�E���^�����荞�݃��[�`���̊J�n�ʒu�ɐݒ� */

	/* �X�^�b�N�̒l�����������邽�߂ɁA�^���n���h�����琳�K�̃n���h���𐶐����� */
	if (DuplicateHandle(GetCurrentProcess(), GetCurrentProcess(), GetCurrentProcess(), &prc, PROCESS_ALL_ACCESS, FALSE, 0) == FALSE) {
		goto FAILED;
	}

	/* �X�^�b�N�|�C���^�����炷 */
	ctx.Esp -= sizeof(DWORD);

	/* �󂯂��X�^�b�N�ʒu�Ɋ��荞�݃��[�`������̖߂��A�h���X���������� */
	ret = WriteProcessMemory(prc, (LPVOID)ctx.Esp, (LPVOID)&pc, (SIZE_T)sizeof(pc), (SIZE_T*)NULL);

	/* ���K�̃n���h������� */
	CloseHandle(prc);

	if (ret == FALSE) {
		goto FAILED;
	}

	/* �X���b�h�̃R���e�L�X�g������������ */
	ctx.ContextFlags = CONTEXT_FULL;
	if (SetThreadContext( kernel_thread_handle, &ctx ) == FALSE) {
		/* �X���b�h�R���e�L�X�g�����������s */
		goto FAILED;
	}

	/* �J�[�l���X���b�h���ĊJ���� */
	ResumeThread(kernel_thread_handle);

	return TRUE;

FAILED:
	/* �J�[�l���X���b�h���ĊJ���� */
	ResumeThread(kernel_thread_handle);

	return FALSE;
}

/**
 * @brief			���z�n�[�h�E�F�A�Ɋ��荞�̔�����`�B����i�O�����{�㏈���j
 * @param[in] id	���������銄�荞�ݔԍ�
 * @author			Kazuya Fukuhara
 * @date			2010/09/01 21:36:49	�쐬
 */
void VHW_onInterrupt(vhw_interruptid_t id) {
	while (InterlockedCompareExchange(&flag_interrupt_lock, 1, 0) != 0) {
		Sleep(0);
	}
	EnterCriticalSection(&kernel_cs);
	if (VHW_onInterruptBody(id) == FALSE) {
		VHW_enableInterrupt();
	}
}

/**
 * @brief	���荞�݂��֎~����
 * @author	Kazuya Fukuhara
 * @date	2010/09/01 21:36:49	�쐬
 */
void VHW_disableInterrupt(void) {
	while (InterlockedCompareExchange(&flag_interrupt_lock, 1, 0) != 0) {
	}
	EnterCriticalSection(&kernel_cs);
}

/**
 * @brief	���荞�݋֎~����
 * @author	Kazuya Fukuhara
 * @date	2010/09/01 21:36:49	�쐬
 */
void VHW_enableInterrupt(void) {
	InterlockedCompareExchange(&flag_interrupt_lock, 0, 1);
	LeaveCriticalSection(&kernel_cs);
}

