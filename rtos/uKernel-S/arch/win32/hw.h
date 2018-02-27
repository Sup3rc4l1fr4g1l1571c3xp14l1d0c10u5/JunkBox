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
 * @date	2010/09/11 11:01:20	�O�����荞�݂ւ̑Ή���ǉ�
 */

#ifndef __hw_h__
#define __hw_h__

#include "../../src/type.h"
//#include "../../src/kernel.h"
#include "../../src/syscall.h"

/**
 * @brief   �n�[�h�E�F�A�S�ʂ̏���������
 * @author  Kazuya Fukuhara
 * @date	2010/09/01 21:36:49	�쐬
 */
extern void initHardware(void);

/**
 * @brief	���荞�݂��֎~����
 * @author	Kazuya Fukuhara
 * @date	2010/09/01 21:36:49	�쐬
 */
extern void disableInterrupt(void);

/**
 * @brief	���荞�݂�������
 * @author	Kazuya Fukuhara
 * @date	2010/09/01 21:36:49	�쐬
 */
extern void enableInterrupt(void);

/**
 * @brief			�R���e�L�X�g�̏�����
 * @param[in] tid	�^�X�N�ԍ�
 * @author			Kazuya Fukuhara
 * @date			2010/09/01 21:36:49	�쐬
 */
extern void resetContext(taskid_t tid);

/**
 * @brief					�V�X�e���R�[���Ăяo��
 * @param[in,out] param		�Ăяo���V�X�e���R�[���̂h�c������̊i�[���ꂽ�\���̂ւ̃|�C���^
 * @author					Kazuya Fukuhara
 * @date					2010/09/01 21:36:49	�쐬
 */
extern void syscall( ptr_t param );

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
 * @def		EXTINT_NUM
 * @brief	�O�����荞�݂̌�
 * @author	Kazuya Fukuhara
 * @date	2010/09/11 11:01:20	�쐬
 */
#define EXTINT_NUM 4

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
 * @def		EnableExtInterrupt
 * @brief	����̊O�����荞�ݔԍ��̊��荞�݂�L��������
 * @author	Kazuya Fukuhara
 * @date	2010/09/11 11:01:20	�쐬
 */
#define EnableExtInterrupt(x) /* Win32���z�n�[�h�E�F�A��ł͕s�v */

/**
 * @def		DisableExtInterrupt
 * @brief	����̊O�����荞�ݔԍ��̊��荞�݂𖳌�������
 * @author	Kazuya Fukuhara
 * @date	2010/09/11 11:01:20	�쐬
 */
#define DisableExtInterrupt(x) /* Win32���z�n�[�h�E�F�A��ł͕s�v */

#endif
