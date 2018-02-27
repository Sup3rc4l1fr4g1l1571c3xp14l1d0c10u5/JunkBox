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
 * @brief	�n�[�h�E�F�A�ˑ��̃R�[�h
 * @author  
 * @date	20xx/xx/xx xx:xx:xx	�쐬
 *
 * �ڐA�Ώۋ@��ɉ������L�q���s�����ƁB
 */

#ifndef __hw_h__
#define __hw_h__

#include "../../src/syscall.h"
#include "./context.h"

/**
 * @brief	�n�[�h�E�F�A�S�ʂ̏���������
 * @author  
 * @date	20xx/xx/xx xx:xx:xx	�쐬
 */
extern void initHardware(void);

/**
 * @brief			�R���e�L�X�g�̏�����
 * @param[in] tid	�^�X�N�ԍ�
 * @author  
 * @date	20xx/xx/xx xx:xx:xx	�쐬
 */
extern void resetContext(taskid_t tid);

/**
 * @brief				�V�X�e���R�[���Ăяo��
 * @param[in] id 		�Ăяo���V�X�e���R�[����ID
 * @param[in] param1	�V�X�e���R�[���̈����P
 * @param[in] param2	�V�X�e���R�[���̈����Q
 * @author  
 * @date	20xx/xx/xx xx:xx:xx	�쐬
 */
extern  svcresultid_t syscall(ptr_t param);

/**
 * @def		disableInterrupt
 * @brief	���荞�݂��֎~����
 * @author  
 * @date	20xx/xx/xx xx:xx:xx	�쐬
 */
#define disableInterrupt() /* ���荞�݋֎~���[�`���̌Ăяo�� */

/**
 * @def		enableInterrupt
 * @brief	���荞�݂�������
 * @author  
 * @date	20xx/xx/xx xx:xx:xx	�쐬
 */
#define enableInterrupt() /* ���荞�݋֎~�������[�`���̌Ăяo�� */

/**
 * @def		GetExtIntId
 * @brief	���������O�����荞�ݔԍ��̎擾
 * @author	
 * @date	20xx/xx/xx xx:xx:xx	�쐬
 */
#define GetExtIntId() 0 /* �O���[�o���ϐ��o�R�Ȃǂł��悢�̂Ŋ��荞�ݔԍ����擾�ł���悤�ɂ��� */

/**
 * @enum	interrupt_id_t
 * @typedef	enum interrupt_id_t interrupt_id_t
 * @brief	���荞�ݔԍ�
 * @author	
 * @date	20xx/xx/xx xx:xx:xx	�쐬
 */
typedef enum interrupt_id_t {
	/* �����Ɋ��荞�ݗv���ƑΉ�����v�f��񋓂��� */
	INT_MAX	/**< ���ɖ�肪�Ȃ��ꍇ�A���̗v�f���O�������݂̌��Ɏg���� */
} interrupt_id_t;

/**
 * @def		EXTINT_NUM
 * @brief	�O�����荞�݂̌�
 * @author	
 * @date	20xx/xx/xx xx:xx:xx	�쐬
 */
#define EXTINT_NUM INT_MAX

/**
 * @brief			�t�b�N�\�ȊO�����荞�ݔԍ�������
 * @param[in] id	�O�����荞�ݔԍ�
 * @retval TRUE		�t�b�N�\�ȊO�����荞�ݔԍ�
 * @retval FALSE	�t�b�N�s�\�ȊO�����荞�ݔԍ�
 * @author	
 * @date			20xx/xx/xx xx:xx:xx	�쐬
 */
extern bool_t is_hookable_interrupt_id(extintid_t int_id);

/**
 * @def		EnableExtInterrupt
 * @brief	����̊O�����荞�ݔԍ��̊��荞�݂�L��������
 * @author	
 * @date	20xx/xx/xx xx:xx:xx	�쐬
 * 
 * M16C�̂悤�Ƀ��W�X�^�ȂǂŊ��荞�ݗv�����ƂɗL��������ݒ�ł���ꍇ�́A�������p����R�[�h���쐬�B
 * �����łȂ��ꍇ�́A�ʓr�e�[�u���Ȃǂ��쐬���ėL��������ݒ肷��悤�ɂ��邱�ƁB
 */
#define EnableExtInterrupt(x) /*  */

/**
 * @def		DisableExtInterrupt
 * @brief	����̊O�����荞�ݔԍ��̊��荞�݂𖳌�������
 * @author	
 * @date	20xx/xx/xx xx:xx:xx	�쐬
 * 
 * M16C�̂悤�Ƀ��W�X�^�ȂǂŊ��荞�ݗv�����ƂɗL��������ݒ�ł���ꍇ�́A�������p����R�[�h���쐬�B
 * �����łȂ��ꍇ�́A�ʓr�e�[�u���Ȃǂ��쐬���ėL��������ݒ肷��悤�ɂ��邱�ƁB
 */
#define DisableExtInterrupt(x) /*  */

#endif
