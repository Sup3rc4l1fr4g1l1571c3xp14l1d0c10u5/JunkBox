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
 */

#ifndef __virtualhw_h__
#define __virtualhw_h__

/**
 * @brief	�J�[�l�����Ăяo�����荞�݃��[�`��
 * @author	Kazuya Fukuhara
 * @date	2010/09/01 21:36:49	�쐬
 */
typedef void (*interruptserviceroutine_t)(void);

/**
 * @brief	���z�f�o�C�X�̃��C������
 * @author	Kazuya Fukuhara
 * @date	2010/09/01 21:36:49	�쐬
 */
typedef void (*virtualdevice_serviceroutine_t)(void);

/**
 * @typedef	struct virtualdevice_t virtualdevice_t;
 * @struct	virtualdevice_t
 * @brief	���z�f�o�C�X�o�^�p�\����
 * @author	Kazuya Fukuhara
 * @date	2010/09/01 21:36:49	�쐬
 */
typedef struct virtualdevice_t {
	char *name;								/**< �f�o�C�X�� */
	virtualdevice_serviceroutine_t vdsr;	/**< �f�o�C�X�̃��C������ */
} virtualdevice_t;

/**
 * @brief	���z�n�[�h�E�F�A�̏�����
 * @author	Kazuya Fukuhara
 * @date	2010/09/01 21:36:49	�쐬
 */
extern void initVirtualHardware(void);

/**
 * @brief			���z�f�o�C�X�̃C���X�g�[��
 * @param[in] vd	�C���X�g�[�����鉼�z�f�o�C�X
 * @author			Kazuya Fukuhara
 * @date			2010/09/01 21:36:49	�쐬
 */
extern void VHW_installVirtualDevice(virtualdevice_t* vd);

/**
 * @brief	���z�n�[�h�E�F�A�̓�����J�n����
 * @author	Kazuya Fukuhara
 * @date	2010/09/01 21:36:49	�쐬
 * 
 * �C���X�g�[���������z�f�o�C�X�̓�����J�n����
 */
extern void startVirtualHardware(void);

/**
 * @typedef		enum vhw_interruptid_t
 * @brief		���z�n�[�h�E�F�A�̊��荞�ݔԍ�
 * @author		Kazuya Fukuhara
 * @date		2010/09/01 21:36:49	�쐬
 */
typedef enum vhw_interruptid_t {
	INTID_TIMER,	/**< �J�[�l���^�C�}�[���荞�� */
	INTID_BUTTON_DOWN,	/**< �{�^�����荞�� */
	INTID_BUTTON_UP,	/**< �{�^�����荞�� */
	INTID_BUTTON_DRAG,	/**< �{�^�����荞�� */
	INTID_MAX		/**< ���荞�ݔԍ��̍ő�l */
} vhw_interruptid_t;

/**
 * @brief			�J�[�l�����̊��荞�݃T�[�r�X���[�`�������z�n�[�h�E�F�A�̊��荞�݃x�N�^�ɐݒ�
 * @param[in] id	���z�n�[�h�E�F�A�̊��荞�ݔԍ�
 * @param[in] isr	���z�n�[�h�E�F�A�̊��荞�ݔԍ��ɑΉ�������J�[�l�����̊��荞�݃T�[�r�X���[�`��
 * @author			Kazuya Fukuhara
 * @date			2010/09/01 21:36:49	�쐬
 */
extern void VHW_setInterruputServiceRoutine(vhw_interruptid_t id, interruptserviceroutine_t isr);

/**
 * @brief			���z�n�[�h�E�F�A�̊��荞�݃x�N�^����J�[�l�����̊��荞�݃T�[�r�X���[�`�����擾
 * @param[in] id	���荞�ݔԍ�
 * @return			���荞�ݔԍ��ɑΉ����銄�荞�݃��[�`�� NULL�̏ꍇ�͖��ݒ�
 * @author			Kazuya Fukuhara
 * @date			2010/09/01 21:36:49	�쐬
 */
extern interruptserviceroutine_t VHW_getInterruputServiceRoutine(vhw_interruptid_t id);

/**
 * @brief			���z�n�[�h�E�F�A�Ɋ��荞�̔�����`�B����i�O�����{�㏈���j
 * @param[in] id	���������銄�荞�ݔԍ�
 * @author			Kazuya Fukuhara
 * @date			2010/09/01 21:36:49	�쐬
 */
extern void VHW_onInterrupt(vhw_interruptid_t id);

/**
 * @brief	���荞�݂��֎~����
 * @author	Kazuya Fukuhara
 * @date	2010/09/01 21:36:49	�쐬
 */
extern void VHW_disableInterrupt(void);

/**
 * @brief	���荞�݋֎~����
 * @author	Kazuya Fukuhara
 * @date	2010/09/01 21:36:49	�쐬
 */
extern void VHW_enableInterrupt(void);

#endif
