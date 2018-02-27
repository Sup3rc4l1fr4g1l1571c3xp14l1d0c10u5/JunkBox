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

#include "message.h"
#include "syscall.h"

/**
 * @brief	���b�Z�[�W�̈�
 * @author  Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	�쐬
 */
static message_t	message_table[MESSAGE_NUM];

/**
 * @brief	�󂫃��b�Z�[�W�̈���Ǘ����郊���N���X�g
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	�쐬
 */
static message_t*	free_message_list;

/**
 * @brief	���b�Z�[�W�L���[������������
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	�쐬
 */
void initMessageQueue(void) {
	int i;
	free_message_list = &message_table[0];
	for (i=1; i<MESSAGE_NUM; i++) {
		message_table[i-1].next = &message_table[i];
	}
	message_table[MESSAGE_NUM-1].next = NULL;
}

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
svcresultid_t recvMSG(taskid_t* from, ptr_t* data) {
	message_t *p;
	if ((&INITTASK == currentTCB) || (&DIAGTASK == currentTCB)) {
		/* INITTASK��DIAGTASK��recvMSG���Ăяo���� */
		return ERR31;
	}

	/* �������Ẵ��b�Z�[�W�����݂��Ă��邩���� */
	if (currentTCB->message == NULL) {
		/* �������Ẵ��b�Z�[�W�͂Ȃ������̂Ő���I�� */
		return ERR41;
	} else {
		/* �������Ẵ��b�Z�[�W���������̂ŁA��M���� */
		p = currentTCB->message;
		currentTCB->message = p->next;
		*from = p->from;
		*data = p->data;

		/* �g���I��������b�Z�[�W�̈���J������ */
		p->from = 0;
		p->next = free_message_list;
		free_message_list = p;

		return SUCCESS;
	}
}

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
svcresultid_t sendMSG(taskid_t to, ptr_t data) {
	message_t *msg;
	message_t *p;

	if (to >= TASK_NUM) {
		/* �s���ȃ^�X�NID��to�Ɏw�肳��Ă��� */
		return ERR1;
	} else if ((to == 0)||(to == TASK_NUM-1)) {
		/* INITTASK��DIAGTASK���Ẵ��b�Z�[�W���M�͋֎~ */
		return ERR31;
	} else if ((&INITTASK == currentTCB) || (&DIAGTASK == currentTCB)) {
		/* INITTASK��DIAGTASK��sendMSG���Ăяo���� */
		return ERR31;
	}

	/* dormant��Ԃ̃^�X�N�ւ̃��b�Z�[�W���M���`�F�b�N */
	if (tasks[to].status == DORMANT) {
		return ERR40;
	}

	/* ���b�Z�[�W�X���b�g����󂫃X���b�g���擾 */
	msg = free_message_list;
	if (msg == NULL) {
		return ERR40;
	} else {
		free_message_list = free_message_list->next;
		msg->next = NULL;
	}

	/* ���M��̃��b�Z�[�W���X�g�̈�Ԍ��Ƀ��b�Z�[�W��ǉ� */
	p = tasks[to].message;
	if (p != NULL) {
		while (p->next != NULL) {
			p = p->next;
		}
		p->next = msg;
	} else {
		tasks[to].message = msg;
	}
	
	/* ���b�Z�[�W�����\�z���ď������� */
	msg->next = NULL;
	msg->from = TCBtoTID(currentTCB); 
	msg->data = data;

	/* �������A���M�悪���b�Z�[�W�҂���ԂȂ�΃^�X�N���N���� */
	if (tasks[to].status == WAIT_MSG) {
		tasks[to].status = READY;									/* �^�X�N��ready��Ԃɐݒ肷�� */
		removeTaskFromReadyQueue(&tasks[to]);						/* ���s���^�X�N�L���[����^�X�N����菜�� */
		addTaskToReadyQueue(&tasks[to]);							/* �^�X�N�����s���^�X�N�̃L���[�ɒǉ����� */
		rescheduling = (&tasks[to] < currentTCB) ? TRUE : FALSE;	/* �X�P�W���[�����O���K�v�Ȃ���s���� */
	}
	
	return SUCCESS;
}

/**
 * @brief			���b�Z�[�W����M����܂ő҂���ԂƂȂ�
 * @author  		Kazuya Fukuhara
 * @retval SUCCESS	����
 * @date			2010/01/07 14:08:58	�쐬
 */
svcresultid_t waitMSG(void) {
	if (currentTCB->message == NULL) {			/* ���b�Z�[�W����M���Ă��Ȃ��ꍇ�͑҂���Ԃɓ��� */
		currentTCB->status = WAIT_MSG;			/* �^�X�N�̏�Ԃ����b�Z�[�W�҂���Ԃɕω������� */
		removeTaskFromReadyQueue(currentTCB);	/* ���s���^�X�N�L���[����^�X�N����菜�� */
		rescheduling = TRUE;
	}
	return SUCCESS;
}

/**
 * @brief			�w�肵���^�X�N�̃��b�Z�[�W�����ׂĊJ��
 * @param[in] tcb	�^�X�N�̃^�X�N�|�C���^
 * @author  		Kazuya Fukuhara
 * @date			2010/01/07 14:08:58	�쐬
 */
void unlinkMSG(tcb_t *tcb) {
	/* �^�X�N�̃��b�Z�[�W�����ׂĊJ�� */
	if ((tcb != NULL) && (tcb->message != NULL)) {
		message_t *p = tcb->message;
		while (p->next) {
			p = p->next;
		}
		p->next = free_message_list;
		free_message_list = tcb->message;
		tcb->message = NULL;
	}
}

