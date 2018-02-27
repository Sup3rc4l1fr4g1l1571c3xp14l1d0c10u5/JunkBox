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
 * @brief	�R���e�L�X�g�ƃR���e�L�X�g����
 * @author  
 * @date	20xx/xx/xx xx:xx:xx	�쐬
 *
 * �ڐA�Ώۋ@��ɉ������L�q���s�����ƁB
 */

#ifndef __CONTEXT_H__
#define __CONTEXT_H__

#include "./arch.h"
#include "../../src/type.h"

/**
 * @def		SET_KERNEL_STACKPOINTER
 * @brief	�J�[�l���X�^�b�N�����݂̃X�^�b�N�|�C���^�ɐݒ肷��
 * @author	
 * @date	20xx/xx/xx xx:xx:xx	�쐬
 *
 * �����̏ꍇ�A�C�����C���A�Z���u����p���ċL�q����
 */
#define SET_KERNEL_STACKPOINTER()

/**
 * @def			SAVE_CONTEXT
 * @brief		���݂̃R���e�L�X�g��Ҕ�
 * @attention	�R���e�L�X�g�̓X�^�b�N�ɑҔ�����A�X�^�b�N�|�C���^�� currentTCB->stack_pointer �Ɋi�[�����B���̂��߁AcurrentTCB�ɓK�؂Ȓl���ݒ�ς݂ł��邱�Ƃ����߂���B
 * @author		
 * @date		20xx/xx/xx xx:xx:xx	�쐬
 *
 * �����̏ꍇ�A�C�����C���A�Z���u����p���ċL�q����
 */
#define SAVE_CONTEXT()

/**
 * @def			RESTORE_CONTEXT
 * @brief		�R���e�L�X�g�𕜋A����
 * @attention	���A�Ɏg���X�^�b�N�|�C���^�� currentTCB->stack_pointer ����ǂݏo�����B���̂��߁AcurrentTCB�ɓK�؂Ȓl���ݒ�ς݂ł��邱�Ƃ����߂���B
 * @author		
 * @date		20xx/xx/xx xx:xx:xx	�쐬
 *
 * �����̏ꍇ�A�C�����C���A�Z���u����p���ċL�q����
 */
#define RESTORE_CONTEXT()

/**
 * @def		RETURN_FROM_INTERRUPT
 * @brief	���荞�ݏ�������E�o����
 * @author  
 * @date	20xx/xx/xx xx:xx:xx	�쐬
 *
 * �����̏ꍇ�A�C�����C���A�Z���u����p���ċL�q����
 */
#define RETURN_FROM_INTERRUPT()

/**
 * @typedef	struct context_t context_t;
 * @struct	context_t
 * @brief	�X�^�b�N�ɕۑ��������s�R���e�L�X�g�i�X�^�b�N�t���[���j
 * @author	
 * @date	20xx/xx/xx xx:xx:xx	�쐬
 *
 * �����̏ꍇ�́A�ėp���W�X�^�A�X�e�[�^�X���W�X�^�A�߂��A�h���X���i�[�����
 */
typedef struct context_t {
	int dummy; /**< �e�A�[�L�e�N�`���ɏ]���Ď������s���Ă������� */
} context_t;

/**
 * @def			SetReturnAddressToContext
 * @brief		�R���e�L�X�g�̖߂��A�h���X��ݒ肷��
 * @param[in]	context �ΏۃR���e�L�X�g
 * @param[in]	address �ݒ肷��߂��A�h���X
 * @author		
 * @date		20xx/xx/xx xx:xx:xx	�쐬
 */
#define SetReturnAddressToContext(context, address)

/**
 * @def			GetContext
 * @brief		�^�X�N�R���g���[���u���b�N������s�R���e�L�X�g���擾
 * @param[in]	tcb �Ώۃ^�X�N�R���g���[���u���b�N
 * @return		���s�R���e�L�X�g�������|�C���^�l
 * @author	
 * @date		20xx/xx/xx xx:xx:xx	�쐬
 */
#define GetContext(tcb) NULL

/**
 * @def			GetArgWord
 * @brief		�R���e�L�X�g����|�C���^���������o��
 * @param[in]	context �ΏۃR���e�L�X�g
 * @return		�����Ƃ��ēn���ꂽ�|�C���^�l
 * @author		
 * @date		20xx/xx/xx xx:xx:xx	�쐬
 */
#define GetArgPtr(context) NULL

/**
 * @def			SetTaskArg
 * @brief		�R���e�L�X�g�Ƀ^�X�N�J�n���̈�����ݒ�
 * @param[in]	context �ΏۃR���e�L�X�g
 * @param[in]	arg     �ݒ肷�����
 * @author		
 * @date		20xx/xx/xx xx:xx:xx	�쐬
 */
#define SetTaskArg(context, arg) 

#endif

