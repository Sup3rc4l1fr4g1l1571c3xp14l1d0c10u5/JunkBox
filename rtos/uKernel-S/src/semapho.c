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
 * @date	2010/09/09 12:48:22	�^�C���V�F�A�����O�X�P�W���[�����O��ǉ�
 * @date	2010/09/10 10:57:13	�D��x�t���^�C���V�F�A�����O�X�P�W���[�����O��ǉ�
 *
 * �Z�}�t�H�Ƃ�������������������Ȃ��̂Ŏ����N���e�B�J���Z�N�V�����ł��B
 */

#include "semapho.h"
#include "syscall.h"

/**
 * @brief	�Z�}�t�H�L���[
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	�쐬
 *
 * �e�Z�}�t�H���̑҂��s��i�D��x�t���L���[�j
 */
static semaphoqueue_t semaphoqueue[SEMAPHO_NUM];

/**
 * @brief	�Z�}�t�H�L���[������������
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	�쐬
 */
void initSemaphoQueue(void) {
	int i;
	for (i=0; i<SEMAPHO_NUM; i++) {
		semaphoqueue[i].next_task = NULL;
		semaphoqueue[i].take_task = NULL;
	}
}

/**
 * @brief			�w�肵���^�X�N���Z�}�t�H�L���[����폜����
 * @param[in] tcb	�폜����^�X�N�̃^�X�N�|�C���^
 * @author  		Kazuya Fukuhara
 * @date			2010/01/07 14:08:58	�쐬
 * �S�Z�}�t�H�҂��v�f���猟������͔̂���Â炢���s�тȂ̂ōčl���ׂ�
 */
void unlinkSEMA(tcb_t *tcb) {
	int i;

	for (i=0; i<SEMAPHO_NUM; i++) {
		if (semaphoqueue[i].take_task != NULL) {
			/* �Z�}�t�H[i]���l�����Ă���^�X�N�������� */
			if (semaphoqueue[i].take_task != tcb) {
				/* �Z�}�t�H[i]���l�����Ă���̂��w�肳�ꂽ�^�X�N�łȂ��ꍇ�A */
				/* ���̃Z�}�t�H�̑҂��s��𖖔��܂ŃX�L�������� */
				tcb_t *next_task = semaphoqueue[i].take_task;
				while (next_task->next_task != NULL) {
					if (next_task->next_task == tcb) {
						/* �w�肳�ꂽ�^�X�N���Z�}�t�H�̑҂��s��ɕ���ł����̂ő҂��s�񂩂�O�� */
						next_task->next_task = tcb->next_task;
						tcb->next_task = NULL;
						break;
					} else {
						next_task = next_task->next_task;
					}
				}
			} else {
				/* �������Z�}�t�H[i]���l�����Ă���̂ŁA�Z�}�t�H�̌��������̃^�X�N�ɗ^���� */
				semaphoqueue[i].take_task = semaphoqueue[i].next_task;
				if (semaphoqueue[i].next_task != NULL) {
					semaphoqueue[i].next_task = semaphoqueue[i].next_task->next_task;
				}
			}
		}
	}
}

/**
 * @brief			�Z�}�t�H���l������B�l���ł��Ȃ��ꍇ��wait��ԂɂȂ�B
 * @param[in] sid	�l������Z�}�t�H�̂h�c
 * @retval SUCCESS	�Z�}�t�H�l������
 * @retval ERR10	�G���[�F�l���������Z�}�t�H�̂h�c���͈͊O
 * @retval ERR31	�G���[�FINIT, DIAG���Z�}�t�H���l�����悤�Ƃ���
 * @author  		Kazuya Fukuhara
 * @date			2010/01/07 14:08:58	�쐬
 */
svcresultid_t takeSEMA(const semaphoid_t sid) {

	/* ���� */
	if (sid >= SEMAPHO_NUM-1) {
		/* �l���������Z�}�t�H�̂h�c���͈͊O */
		return ERR10;
	} else if ((currentTCB == &(INITTASK))||(currentTCB == &(DIAGTASK))) {
		/* INIT,DIAG���Z�}�t�H���l�����悤�Ƃ��� */
		return ERR31;
	}

	/* �l�� */
	if ((&(tasks[1]) <= semaphoqueue[sid].take_task) && (semaphoqueue[sid].take_task <= &(tasks[TASK_NUM-2]))) {
#if (SCHEDULER_TYPE == SCHEDULING_PRIORITY_BASE)
		/* �Z�}�t�H�͂��ꂩ���l���ς݂Ȃ̂ŁA�҂��L���[�ɗD��x�i�^�X�N�h�c�j�����ŕ��� */
		removeTaskFromReadyQueue(currentTCB);
		if ((semaphoqueue[sid].next_task == NULL)||(semaphoqueue[sid].next_task > currentTCB)) {
			/* �҂��L���[�ւ̑}���ʒu���擪�̏ꍇ */
			currentTCB->next_task = semaphoqueue[sid].next_task;
			semaphoqueue[sid].next_task = currentTCB;
		} else {
			/* �҂��L���[�ւ̑}���ʒu���Q�Ԗڈȍ~�̏ꍇ */
			tcb_t* next_task = semaphoqueue[sid].next_task;
			while ((next_task->next_task != NULL) && (next_task->next_task < currentTCB)) {
				next_task = next_task->next_task;
			}
			currentTCB->next_task = next_task->next_task;
			next_task->next_task = currentTCB;
		}
#elif (SCHEDULER_TYPE == SCHEDULING_TIME_SHARING)
		/* �Z�}�t�H�͂��ꂩ���l���ς݂Ȃ̂ŁA�҂��L���[�̈�Ԍ��ɕ��� */
		removeTaskFromReadyQueue(currentTCB);
		if (semaphoqueue[sid].next_task == NULL) {
			/* �҂��L���[�ɗv�f���Ȃ��ꍇ�͐擪�ɒǉ� */
			currentTCB->next_task = semaphoqueue[sid].next_task;
			semaphoqueue[sid].next_task = currentTCB;
		} else {
			/* �҂��L���[�ɗv�f������ꍇ�͖����v�f�܂ŒH���Ēǉ� */
			tcb_t* next_task = semaphoqueue[sid].next_task;
			while (next_task->next_task != NULL) {
				next_task = next_task->next_task;
			}
			currentTCB->next_task = next_task->next_task;
			next_task->next_task = currentTCB;
		}
#elif (SCHEDULER_TYPE == SCHEDULING_PRIORITY_BASE_TIME_SHARING)
		/* �Z�}�t�H�͂��ꂩ���l���ς݂Ȃ̂ŁA�҂��L���[�ɗD��x�i�^�X�N�Ɋ��蓖�Ă�ꂽ�D��x�j�����ŕ��� */
		removeTaskFromReadyQueue(currentTCB);
		if ((semaphoqueue[sid].next_task == NULL)||(semaphoqueue[sid].next_task->task_priolity > currentTCB->task_priolity)) {
			/* �҂��L���[�ւ̑}���ʒu���擪�̏ꍇ */
			currentTCB->next_task = semaphoqueue[sid].next_task;
			semaphoqueue[sid].next_task = currentTCB;
		} else {
			/* �҂��L���[�ւ̑}���ʒu���Q�Ԗڈȍ~�̏ꍇ */
			tcb_t* next_task = semaphoqueue[sid].next_task;
			while ((next_task->next_task != NULL) && (next_task->next_task->task_priolity < currentTCB->task_priolity)) {
				next_task = next_task->next_task;
			}
			currentTCB->next_task = next_task->next_task;
			next_task->next_task = currentTCB;
		}
#else
#error "Scheduler type is undefined."
#endif
		currentTCB->status = WAIT_SEMA;
		rescheduling = TRUE;
		return SUCCESS;
	} else {
		/* �Z�}�t�H�̊l������ */
		semaphoqueue[sid].take_task = currentTCB;
		rescheduling = FALSE;
		return SUCCESS;
	}
}

/**
 * @brief			�l�������Z�}�t�H���������B
 * @param[in] sid	��������Z�}�t�H�̂h�c
 * @retval SUCCESS	�Z�}�t�H��������
 * @retval ERR10	�G���[�F�����������Z�}�t�H�̂h�c���͈͊O
 * @retval ERR12	�G���[�F�l�����Ă��Ȃ��Z�}�t�H��������悤�Ƃ���
 * @author  		Kazuya Fukuhara
 * @date			2010/01/07 14:08:58	�쐬
 */
svcresultid_t giveSEMA(semaphoid_t sid) {
	if (sid >= SEMAPHO_NUM-1) {
		/* �����������Z�}�t�H�̂h�c���͈͊O */
		rescheduling = FALSE;
		return ERR10;
	} else {
		if (semaphoqueue[sid].take_task != currentTCB) {
			/* �l�����Ă��Ȃ��Z�}�t�H��������悤�Ƃ��� */
			rescheduling = FALSE;
			return ERR12;
		} else {
			if (semaphoqueue[sid].next_task != NULL) {
				/* �Z�}�t�H�҂��L���[���玟�̃^�X�N�𓾂� */
				tcb_t* next_task = semaphoqueue[sid].next_task;
				/* �Z�}�t�H�����̃^�X�N���l�����ɂ��� */
				semaphoqueue[sid].take_task = next_task;
				semaphoqueue[sid].next_task = next_task->next_task;
				/* ���̃^�X�N�̏�Ԃ�ready�ɂ��āAready�L���[�ɑ}������ */
				next_task->status = READY;
				addTaskToReadyQueue(next_task);
				rescheduling = TRUE;
				return SUCCESS;
			} else {
				/* ���̃^�X�N���Ȃ��̂ŁA�Z�}�t�H�͂�����l�����Ă��Ȃ����Ƃɂ��� */
				semaphoqueue[sid].take_task = NULL;
				return SUCCESS;
			}
		}
	}
}

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
svcresultid_t tasSEMA(semaphoid_t sid) {
	if (sid >= SEMAPHO_NUM-1) {
		/* �����������Z�}�t�H�̂h�c���͈͊O */
		return ERR10;
	} else if ((currentTCB == &(INITTASK))||(currentTCB == &(DIAGTASK))) {
		/* INIT, DIAG���Z�}�t�H���l�����悤�Ƃ��� */
		return ERR31;
	} else {
		/* �l�� */
		if (semaphoqueue[sid].take_task != NULL) {
			/* ���łɃZ�}�t�H�͕ʂ̃^�X�N�Ɋl������Ă��� */
			return ERR11;
		} else {
			/* �Z�}�t�H���l�� */
			semaphoqueue[sid].take_task = currentTCB;
			semaphoqueue[sid].next_task = NULL;
			return SUCCESS;
		}
	}

}

