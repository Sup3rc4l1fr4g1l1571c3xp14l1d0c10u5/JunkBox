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
 * @brief   �Z�}�t�H
 * @author  Kazuya Fukuhara
 * @date    2010/01/07 14:08:58	�쐬
 */

#ifndef __semapho_h__
#define __semapho_h__

#include "kernel.h"
#include "syscall.h"

/**
 * @typedef struct semaphoqueue_t semaphoqueue_t;
 * @struct  semaphoqueue_t
 * @brief   �J�[�l�����Ǘ�����Z�}�t�H�L���[�\����
 * @note    INIT�^�X�N��DIAG�^�X�N�ł̓Z�}�t�H�g�p���֎~����
 * @author  Kazuya Fukuhara
 * @date    2010/01/07 14:08:58	�쐬
 */
typedef struct semaphoqueue_t {
	tcb_t*	next_task;		/**<
	                         * �Z�}�t�H�҂��^�X�N�̃����N���X�g
	                         * (���̃��X�g�ɂ̓^�X�N���D��x���ŕ���ł���)
							 * NULL�̏ꍇ�͑҂��^�X�N�Ȃ�
	                         */
	tcb_t*	take_task;		/**<
							 * ���Z�}�t�H���l�����Ă���^�X�N�̃|�C���^
							 * NULL�̏ꍇ�͊l�����Ă���^�X�N�Ȃ�
							 */
} semaphoqueue_t;

/**
 * @brief	�Z�}�t�H�L���[������������
 * @author  Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	�쐬
 */
extern void initSemaphoQueue(void);

/**
 * @brief			�w�肵���^�X�N���Z�}�t�H�L���[����폜����
 * @param[in] tcb	�폜����^�X�N�̃^�X�N�|�C���^
 * @author  		Kazuya Fukuhara
 * @date    		2010/01/07 14:08:58	�쐬
 */
extern void unlinkSEMA(tcb_t *tcb);

/**
 * @brief			�Z�}�t�H���l������B�l���ł��Ȃ��ꍇ��wait��ԂɂȂ�B
 * @param[in] sid	�l������Z�}�t�H�̂h�c
 * @retval SUCCESS	�Z�}�t�H�l������
 * @retval ERR10	�G���[�F�l���������Z�}�t�H�̂h�c���͈͊O
 * @retval ERR31	�G���[�FINIT, DIAG���Z�}�t�H���l�����悤�Ƃ���
 * @author  		Kazuya Fukuhara
 * @date			2010/01/07 14:08:58	�쐬
 */
extern svcresultid_t takeSEMA(const semaphoid_t sid);

/**
 * @brief			�l�������Z�}�t�H���������B
 * @param[in] sid	��������Z�}�t�H�̂h�c
 * @retval SUCCESS	�Z�}�t�H��������
 * @retval ERR10	�G���[�F�����������Z�}�t�H�̂h�c���͈͊O
 * @retval ERR12	�G���[�F�l�����Ă��Ȃ��Z�}�t�H��������悤�Ƃ���
 * @author  		Kazuya Fukuhara
 * @date			2010/01/07 14:08:58	�쐬
 */
extern svcresultid_t giveSEMA(semaphoid_t sid);

/**
 * @brief			�Z�}�t�H���l������B�l���ł��Ȃ��ꍇ�̓G���[��߂��B
 * @param[in] sid	�l������Z�}�t�H�̂h�c
 * @retval SUCCESS	�Z�}�t�H�l������
 * @retval ERR10	�G���[�F�l���������Z�}�t�H�̂h�c���͈͊O
 * @retval ERR11	�G���[�F�Z�}�t�H���l���ł��Ȃ�����
 * @retval ERR31	�G���[�FINIT, DIAG���Z�}�t�H���l�����悤�Ƃ���
 * @author  		Kazuya Fukuhara
 * @date    		2010/01/07 14:08:58	�쐬
 */
extern svcresultid_t tasSEMA(semaphoid_t sid);

#endif
