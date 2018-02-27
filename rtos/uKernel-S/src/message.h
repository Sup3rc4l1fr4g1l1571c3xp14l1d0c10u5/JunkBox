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
 * @brief   �^�X�N�ԃ��b�Z�[�W�ʐM
 * @author  Kazuya Fukuhara
 * @date    2010/01/07 14:08:58	�쐬
 */

#ifndef __message_h__
#define __message_h__

#include "kernel.h"
#include "syscall.h"

/**
 * @typedef struct message_t message_t;
 * @struct  message_t
 * @brief   �^�X�N�ԒʐM�ŗp���郁�b�Z�[�W�\����
 * @note    INIT�^�X�N��DIAG�^�X�N�ł̓Z�}�t�H�g�p���֎~����
 * @author  Kazuya Fukuhara
 * @date    2010/01/07 14:08:58	�쐬
 */
typedef struct message_t {
	struct message_t*	next;	/**< ���̃��b�Z�[�W�ւ̃����N */
	taskid_t			from;	/**< ���b�Z�[�W���M���̃^�X�NID */
	ptr_t				data;	/**< ���b�Z�[�W�{�� */
} message_t;

/**
 * @brief	���b�Z�[�W�L���[������������
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	�쐬
 */
extern void initMessageQueue(void);

/**
 * @brief			�^�X�N�̃��b�Z�[�W�L���[�����M�������b�Z�[�W�����o��
 * @param[out] from	���M���̃^�X�N�̂h�c
 * @param[out] data	���M���ꂽ���b�Z�[�W�̖{��
 * @retval SUCCESS	��M����
 * @retval ERR31	�G���[�FINITTASK��DIAGTASK��recvMSG���Ăяo����
 * @retval ERR41	�G���[�F�������Ẵ��b�Z�[�W�͂Ȃ������i����I���j
 * @author  		Kazuya Fukuhara
 * @date			2010/01/07 14:08:58	�쐬
 */
extern svcresultid_t recvMSG(taskid_t* from, ptr_t* data);

/**
 * @brief			�w�肵���^�X�N�̃��b�Z�[�W�L���[�Ƀ��b�Z�[�W�𑗐M����
 * @param[in] to	���M��̃^�X�N�̂h�c
 * @param[in] data	���M���郁�b�Z�[�W�̖{��
 * @retval SUCCESS	��M����
 * @retval ERR1		�G���[�F�s���ȃ^�X�NID��to�Ɏw�肳��Ă���
 * @retval ERR31	�G���[�FINITTASK��DIAGTASK��sendMSG���Ăяo����
 * @retval ERR40	�G���[�F���[�����M���s�idormant��Ԃ̃^�X�N�ւ̃��b�Z�[�W���M�j
 * @retval ERR41	�G���[�F�������Ẵ��b�Z�[�W�͂Ȃ������i����I���j
 * @author  		Kazuya Fukuhara
 * @date			2010/01/07 14:08:58	�쐬
 */
extern svcresultid_t sendMSG(taskid_t to, ptr_t data);

/**
 * @brief			���b�Z�[�W����M����܂ő҂���ԂƂȂ�
 * @author  		Kazuya Fukuhara
 * @retval SUCCESS	����
 * @date			2010/01/07 14:08:58	�쐬
 */
extern svcresultid_t waitMSG(void);

/**
 * @brief			�w�肵���^�X�N�̃��b�Z�[�W�����ׂĊJ��
 * @param[in] tcb	�^�X�N�̃^�X�N�|�C���^
 * @author  		Kazuya Fukuhara
 * @date			2010/01/07 14:08:58	�쐬
 */
extern void unlinkMSG(tcb_t *tcb);

#endif
