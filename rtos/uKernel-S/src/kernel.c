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
 * @brief   �J�[�l���R�A
 * @author  Kazuya Fukuhara
 * @date    2010/01/07 14:08:58	�쐬
 * @date	2010/08/15 16:11:02	�@��ˑ������𕪗�
 * @date	2010/09/09 12:48:22	�^�C���V�F�A�����O�X�P�W���[�����O��ǉ�
 * @date	2010/09/10 10:57:13	�D��x�t���^�C���V�F�A�����O�X�P�W���[�����O��ǉ�
 * @date	2010/09/11 11:01:20	�O�����荞�݂Ń^�X�N���N������@�\������
 *								exit/restart�V�X�e���R�[���ŃX�^�b�N�t���[����j�󂵂Ă��܂��s����C��
 */

#include "arch.h"
#include "config.h"
#include "type.h"
#include "kernel.h"
#include "syscall.h"
#include "semapho.h"
#include "message.h"


/**
 * @brief	�^�X�N�R���g���[���u���b�N�z��
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	�쐬
 */
global tcb_t	tasks[TASK_NUM];

/**
 * @brief	�^�X�N�̎��s���X�^�b�N�̈�
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	�쐬
 */
global uint8_t	task_stack[TASK_NUM][TASK_STACK_SIZE];

/**
 * @brief	���ݎ��s���̃^�X�N�̃^�X�N�R���g���[���u���b�N
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	�쐬
 */
global tcb_t	*currentTCB;

/**
 * @brief	�X�P�W���[�����O�v���t���O
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	�쐬
 */
global bool_t	rescheduling;

/**
 * @brief	ready��Ԃ̃^�X�N����ׂ�L���[
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	�쐬
 */
global readyqueue_t readyqueue;

/**
 * @brief				�w�肵��tid�����[�U�[�^�X�N�������Ă��邩���肷��B
 * @author				Kazuya Fukuhara
 * @param	[in] tid	���肷��^�X�N�h�c
 * @retval	TRUE		���[�U�[�^�X�N�ł���
 * @retval	FALSE		���[�U�[�^�X�N�ł͂Ȃ�
 * @date				2010/12/03 20:31:26	�쐬
 *
 * ���[�U�[�^�X�N�Ƃ́AINIT/DIAG���������^�X�N�̂��ƁB
 */
global bool_t isUserTask(taskid_t tid) {
	/* tid �� 0 �ł��遁INIT�ł��� 
	 * tid �� TASK_NUM-1 �ł��遁DIAG�ł���
	 * ������� 0 < tid < TASK_NUM-1 �Ȃ烆�[�U�[�^�X�N�ł���
	 */
	if ((0 < tid) && (tid < TASK_NUM-1)) {
		/* ���[�U�[�^�X�N�������Ă��� */
		return TRUE;
	} else {
		/* ���[�U�[�^�X�N�������Ă��Ȃ� */
		return FALSE;
	}
}

/**
 * @brief			�^�X�N��readyqueue�ɑ}������
 * @param [in] tcb	�}������^�X�N
 * @note			tcb�̑Ó����͌������Ȃ��̂Œ���
 * @author			Kazuya Fukuhara
 * @date			2010/01/07 14:08:58	�쐬
 * @date			2010/09/09 12:48:22	�^�C���V�F�A�����O�X�P�W���[�����O��ǉ�
 * @date			2010/09/10 10:57:13	�D��x�t���^�C���V�F�A�����O�X�P�W���[�����O��ǉ�
 */
global void addTaskToReadyQueue (tcb_t *tcb) {

	tcb_t* next_task;
	tcb_t* pT = tcb;
		
	if (pT == NULL) {
		return;
	}

	next_task = readyqueue.next_task;
#if (SCHEDULER_TYPE == SCHEDULING_PRIORITY_BASE)
	if (next_task == NULL) {
		/* readyqueue �ɉ����o�^����Ă��Ȃ��ꍇ */
		pT->next_task = readyqueue.next_task;
		readyqueue.next_task = tcb;
	} else if (next_task > tcb) {
		/* �ŏ��̗v�f�̑O�ɑ}������P�[�X */
		pT->next_task = readyqueue.next_task;
		readyqueue.next_task = tcb;
	} else {
		/* ���ȍ~�̗v�f�Ɣ�r */
		tcb_t* beforenid;
		for (;;) {
			beforenid = next_task;
			next_task = next_task->next_task;
			
			if (next_task == NULL) {
				/* �I�[�ɂɒB�����̂ŁA�I�[�ɒǉ� */
				pT->next_task = beforenid->next_task;
				beforenid->next_task = tcb;
				break;
			} else if (next_task == tcb) {
				/* ���ɓo�^�ς݁I�I */
				break;
			} else if (next_task > tcb) {
				/* �}������ʒu���������� */
				pT->next_task = beforenid->next_task;
				beforenid->next_task = tcb;
				break;
			} else {
				continue;
			}
		}
	}
#elif (SCHEDULER_TYPE == SCHEDULING_TIME_SHARING)
	if (next_task == NULL) {
		/* readyqueue �ɉ����o�^����Ă��Ȃ��ꍇ */
		pT->next_task = readyqueue.next_task;
		readyqueue.next_task = tcb;
	} else {
		/* �����ɒǉ� */
		tcb_t* beforenid;
		for (;;) {
			beforenid = next_task;
			next_task = next_task->next_task;
			
			if (next_task == NULL) {
				/* �I�[�ɂɒB�����̂ŁA�I�[�ɒǉ� */
				pT->next_task = beforenid->next_task;
				beforenid->next_task = tcb;
				break;
			} else if (next_task == tcb) {
				/* ���ɓo�^�ς݁I�I */
				break;
			} else {
				continue;
			}
		}
	}
#elif (SCHEDULER_TYPE == SCHEDULING_PRIORITY_BASE_TIME_SHARING) 
	if (next_task == NULL) {
		/* readyqueue �ɉ����o�^����Ă��Ȃ��ꍇ */
		pT->next_task = readyqueue.next_task;
		readyqueue.next_task = tcb;
	} else if (next_task->task_priolity > tcb->task_priolity) {
		/* �ŏ��̗v�f�̑O�ɑ}������P�[�X */
		pT->next_task = readyqueue.next_task;
		readyqueue.next_task = tcb;
	} else {
		/* ���ȍ~�̗v�f�Ɣ�r */
		tcb_t* beforenid;
		for (;;) {
			beforenid = next_task;
			next_task = next_task->next_task;
			
			if (next_task == NULL) {
				/* �I�[�ɂɒB�����̂ŁA�I�[�ɒǉ� */
				pT->next_task = beforenid->next_task;
				beforenid->next_task = tcb;
				break;
			} else if (next_task == tcb) {
				/* ���ɓo�^�ς݁I�I */
				break;
			} else if (next_task->task_priolity > tcb->task_priolity) {
				/* �}������ʒu���������� */
				pT->next_task = beforenid->next_task;
				beforenid->next_task = tcb;
				break;
			} else {
				continue;
			}
		}
	}
#else
#error "Scheduler type is undefined."
#endif
}

/**
 * @brief			�^�X�N�� readyqueue ���ɂ���Ύ�菜��
 * @param  [in] tcb	��菜���^�X�N
 * @retval false	�^�X�N�� readyqueue ���ɑ��݂��Ȃ������B
 * @retval true		�^�X�N�� readyqueue ���ɑ��݂����̂Ŏ�菜�����B
 * @note			tcb�̑Ó����͌������Ȃ��̂Œ���
 * @author			Kazuya Fukuhara
 * @date			2010/01/07 14:08:58	�쐬
 */
global bool_t removeTaskFromReadyQueue(tcb_t *tcb) {
	tcb_t *pid = NULL;	/* readyqueue��Ŏw�肳�ꂽ�^�X�N tcb �̂ЂƂO�̗v�f���i�[����� */

	/* �w�肳�ꂽ�^�X�N tcb �� readyqueue ���ɑ��݂��邩�T�����s�� */
	{
		tcb_t *next_task;
		for (next_task = readyqueue.next_task; next_task != tcb; next_task = next_task->next_task) {
			if (next_task == NULL) {
				return FALSE;	/* �I�[�ɒB���� */
			} else {
				pid = next_task;
			}
		}
	}

	/* �����ɓ��B�������_�ŁA�ȉ��̂��Ƃ��ۏႳ���B
	 * �Epid != null �̏ꍇ�F�w�肳�ꂽ�^�X�N tcb ��readyqueue��ɑ��݂��Ă���
	 * �Epid == null �̏ꍇ�Freadyqueue�ɂ͈���^�X�N������ł��Ȃ�
	 */

	/* readyqueue����O�� */

	if (readyqueue.next_task == tcb) {
		/* �w�肳�ꂽ�^�X�N tcb �̓L���[�̐擪�Ɉʒu���Ă���B*/
		readyqueue.next_task = tcb->next_task;	/* readyqueue.next_task �� tcb �̎��̃^�X�N��}������ */
		tcb->next_task = NULL;
	} else if (pid != NULL) {
		/*
		 * �L���[�̐擪�ł͂Ȃ��ꍇ�A�T���̃��[�v���Œ�P��͎��s����Ă���B
		 * �܂�Apid�̒l��NULL�ȊO�ɂȂ��Ă���B
		 */
		pid->next_task = tcb->next_task;
		tcb->next_task = NULL;
	} else {
		/* readyqueue�ɂ͈���^�X�N������ł��Ȃ��̂ŁA��菜�����s���K�v���Ȃ� */
	}

	return TRUE;
}

/**
 * @brief  wait��Ԃ̃^�X�N����ׂ�L���[
 * @author Kazuya Fukuhara
 * @date   2010/01/07 14:08:58	�쐬
 */
global waitqueue_t waitqueue;

/**
 * @brief			tcb�Ŏ������^�X�N��҂��s�� WaitQueue �ɒǉ�����
 * @param [in] tcb	�ǉ�����^�X�N
 * @param [in] time	�҂�����
 * @retval FALSE	�^�X�N�� WaitQueue ���ɑ��݂��Ȃ������B
 * @retval TRUE		�^�X�N�� WaitQueue ���ɑ��݂����̂Ŏ�菜�����B
 * @note			tcb�̑Ó����͌������Ȃ��̂Œ���
 * @note			time �� 0 ��n�����ꍇ�́A�҂��s��ɂ͒ǉ�����Ȃ�
 * @author			Kazuya Fukuhara
 * @date			2010/01/07 14:08:58	�쐬
 * @date			2010/09/09 12:48:22	�^�C���V�F�A�����O�X�P�W���[�����O��ǉ�
 * @date			2010/09/10 10:57:13	�D��x�t���^�C���V�F�A�����O�X�P�W���[�����O��ǉ�
 */
global void addTaskToWaitQueue(tcb_t* tcb, tick_t time) {

	tcb_t* next_task = waitqueue.next_task;
	tcb_t* pT = tcb;
	
#if (SCHEDULER_TYPE == SCHEDULING_PRIORITY_BASE)
	if (time == 0) {
		return;
	} else if (next_task == NULL) {
		/* pauseQ_head�ɉ����o�^����Ă��Ȃ��ꍇ */
		pT->next_task = waitqueue.next_task;
		pT->pause_count = time;
		waitqueue.next_task = tcb;
	} else if ((next_task->pause_count > time) || 
	           ((next_task->pause_count == time) && (next_task > tcb))) {
		/* �ŏ��̗v�f�̑O�ɑ}������P�[�X */
		pT->next_task = waitqueue.next_task;
		pT->pause_count = time;
		waitqueue.next_task = tcb;
		next_task->pause_count -= pT->pause_count;
	} else {
		/* �ŏ��̗v�f�̌��ɑ}������̂ŁA���ȍ~�̗v�f�Ɣ�r */
		tcb_t* beforenid;
		for (;;) {
			beforenid = next_task;
			time -= next_task->pause_count;
			next_task = next_task->next_task;
			
			if (next_task == NULL) {
				/* �I�[�ɂɒB�����̂ŁA�I�[�ɒǉ� */
				pT->next_task = beforenid->next_task;
				pT->pause_count = time;
				beforenid->next_task = tcb;
				break;
			} else if ((next_task->pause_count > time) || ((next_task->pause_count == time) && (next_task > tcb))) {
				/* �}������ʒu���������� */
				pT->next_task = beforenid->next_task;
				pT->pause_count = time;
				beforenid->next_task = tcb;
				next_task->pause_count -= pT->pause_count;
				break;
			} else {
				continue;
			}
		}
	}
#elif (SCHEDULER_TYPE == SCHEDULING_TIME_SHARING)
	if (time == 0) {
		return;
	} else if (next_task == NULL) {
		/* pauseQ_head�ɉ����o�^����Ă��Ȃ��ꍇ */
		pT->next_task = waitqueue.next_task;
		pT->pause_count = time;
		waitqueue.next_task = tcb;
	} else {
		/* �ŏ��̗v�f�̌��ɑ}������̂ŁA���ȍ~�̗v�f�Ɣ�r */
		tcb_t* beforenid;
		for (;;) {
			beforenid = next_task;
			time -= next_task->pause_count;
			next_task = next_task->next_task;
			
			if (next_task == NULL) {
				/* �I�[�ɂɒB�����̂ŁA�I�[�ɒǉ� */
				pT->next_task = beforenid->next_task;
				pT->pause_count = time;
				beforenid->next_task = tcb;
				break;
			} else if (next_task->pause_count > time) {
				/* �}������ʒu���������� */
				pT->next_task = beforenid->next_task;
				pT->pause_count = time;
				beforenid->next_task = tcb;
				next_task->pause_count -= pT->pause_count;
				break;
			} else {
				continue;
			}
		}
	}
#elif (SCHEDULER_TYPE == SCHEDULING_PRIORITY_BASE_TIME_SHARING)
	if (time == 0) {
		return;
	} else if (next_task == NULL) {
		/* pauseQ_head�ɉ����o�^����Ă��Ȃ��ꍇ */
		pT->next_task = waitqueue.next_task;
		pT->pause_count = time;
		waitqueue.next_task = tcb;
	} else if ((next_task->pause_count > time) || 
	           ((next_task->pause_count == time) && (next_task->task_priolity > tcb->task_priolity))) {
		/* �ŏ��̗v�f�̑O�ɑ}������P�[�X */
		pT->next_task = waitqueue.next_task;
		pT->pause_count = time;
		waitqueue.next_task = tcb;
		next_task->pause_count -= pT->pause_count;
	} else {
		/* �ŏ��̗v�f�̌��ɑ}������̂ŁA���ȍ~�̗v�f�Ɣ�r */
		tcb_t* beforenid;
		for (;;) {
			beforenid = next_task;
			time -= next_task->pause_count;
			next_task = next_task->next_task;
			
			if (next_task == NULL) {
				/* �I�[�ɂɒB�����̂ŁA�I�[�ɒǉ� */
				pT->next_task = beforenid->next_task;
				pT->pause_count = time;
				beforenid->next_task = tcb;
				break;
			} else if ((next_task->pause_count > time) || ((next_task->pause_count == time) && (next_task->task_priolity > tcb->task_priolity))) {
				/* �}������ʒu���������� */
				pT->next_task = beforenid->next_task;
				pT->pause_count = time;
				beforenid->next_task = tcb;
				next_task->pause_count -= pT->pause_count;
				break;
			} else {
				continue;
			}
		}
	}
#else
#error "Scheduler type is undefined."
#endif
}

/**
 * @brief			tcb�Ŏ������^�X�N���҂��s�� WaitQueue ���ɂ���Ύ�菜���B
 * @param[in] tcb	��菜���^�X�N
 * @retval FALSE	�^�X�N�� WaitQueue ���ɑ��݂��Ȃ������B
 * @retval TRUE		�^�X�N�� WaitQueue ���ɑ��݂����̂Ŏ�菜�����B
 * @note			tcb�̑Ó����͌������Ȃ��̂Œ���
 * @author			Kazuya Fukuhara
 * @date			2010/01/07 14:08:58	�쐬
 */
global bool_t removeTaskFromWaitQueue(tcb_t* tcb) {
	tcb_t* next_task = NULL;
	tcb_t* pid = NULL;

	if (tcb == NULL) {
		return FALSE;
	}

	/* �T�� */
	pid = NULL;
	for (next_task = waitqueue.next_task; next_task != tcb; next_task = next_task->next_task) {
		if (next_task == NULL) {
			return FALSE;	/* �I�[�ɒB���� */
		}
		pid = next_task;
	}
	
	/* �����ɓ��B�������_�ŁA�w�肳�ꂽ�^�X�N tcb �̓��X�g���ɑ��݂��Ă��邱�Ƃ��ۏ؂���� */

	/* �����ɑ����^�X�N������ꍇ�Apause_count���X�V */
	if (tcb->next_task != NULL) {
		tcb->next_task->pause_count += tcb->pause_count;
	}

	/* �L���[����O�� */
	if (waitqueue.next_task == tcb) {
		/* �L���[�̐擪�̏ꍇ */
		waitqueue.next_task = tcb->next_task;
	} else if (pid != NULL) {
		/*
		 * �L���[�̐擪�ł͂Ȃ��ꍇ�A�T���̃��[�v���Œ�P��͎��s����Ă���
		 * �܂�Apid�̒l��NULL�ȊO�ɂȂ��Ă���
		 */
		pid->next_task = tcb->next_task;
	}
	tcb->next_task = NULL;

	return TRUE;
}

/**
 * @brief			�^�X�N�R���g���[���u���b�N����^�X�NID�𓾂�
 * @param[in] tcb	ID�𓾂����^�X�N�̃^�X�N�R���g���[���u���b�N
 * @return			�^�X�NID;
 * @author			Kazuya Fukuhara
 * @date			2010/01/07 14:08:58	�쐬
 */
global taskid_t getTaskID(tcb_t* tcb) {
	return TCBtoTID(tcb);
}

/**
 * @brief               �w�肵���^�X�N�R���g���[���u���b�N��������ԂɃ��Z�b�g����
 * @param[in] taskid    ���Z�b�g�Ώۂ̃^�X�N�̃^�X�NID
 * @note                �w�肵���^�X�N�̃X�^�b�N�|�C���^���Ԃ��S�ă��Z�b�g����A�^�X�N�͏�����ԂƂȂ�B
 * @author              Kazuya Fukuhara
 * @date                2010/01/07 14:08:58	�쐬
 * @date				2010/08/15 16:11:02	�@��ˑ������𕪗�
 */
global void resetTCB(taskid_t taskid) {
	/* �R���e�L�X�g�̏����� */
	resetContext(taskid);

	/* �^�X�N�̏�Ԃ�ݒ� */
	tasks[taskid].status = DORMANT;
	tasks[taskid].pause_count = 0;

	/* �`�F�C���������� */
	tasks[taskid].next_task = NULL;

	/* ���b�Z�[�W�������� */
	tasks[taskid].message = NULL;
}

/**
 * @brief   ���ׂẴ^�X�N�R���g���[���u���b�N��������ԂɃ��Z�b�g����
 * @note    ���ׂẴ^�X�N�ɑ΂���resetTCB���Ăяo��
 * @author  Kazuya Fukuhara
 * @date    2010/01/07 14:08:58	�쐬
 */
global void initTaskControlBlocks(void) {
	taskid_t i;
	for (i=0; i<TASK_NUM; ++i) {
		resetTCB(i);
	}
}

/**
 * @brief   �҂���Ԃ̃^�X�N�̑҂����Ԃ��X�V����
 * @author  Kazuya Fukuhara
 * @date    2010/01/07 14:08:58	�쐬
 * @date	2010/09/09 12:48:22	�^�C���V�F�A�����O�X�P�W���[�����O��ǉ�
 * @date	2010/09/10 10:57:13	�D��x�t���^�C���V�F�A�����O�X�P�W���[�����O��ǉ�
 */
global void updateTick(void) {
	
#if (SCHEDULER_TYPE == SCHEDULING_TIME_SHARING) || (SCHEDULER_TYPE == SCHEDULING_PRIORITY_BASE_TIME_SHARING)
	if (currentTCB != NULL) {
		if (currentTCB->time_sharing_tick > 0) {
			currentTCB->time_sharing_tick--;
		}
	}
#endif

	if (waitqueue.next_task == NULL) {
		rescheduling = FALSE;
	} else {
		waitqueue.next_task->pause_count--;
		if (waitqueue.next_task->pause_count == 0) {
			do {
				/* ���O�� */
				tcb_t *next_task = waitqueue.next_task;
				next_task->status = READY;
				waitqueue.next_task = next_task->next_task;
				next_task->next_task = NULL;
				/* readyQueue�ɓ���� */
				addTaskToReadyQueue (next_task);
				/* �������� */
			} while ((waitqueue.next_task != NULL) && (waitqueue.next_task->pause_count == 0));
			rescheduling = TRUE;
		} else {
			rescheduling = FALSE;
		}
	}
}

/**
 * @brief   �f�B�X�p�b�`����
 * @author  Kazuya Fukuhara
 * @date    2010/01/07 14:08:58	�쐬
 * @date	2010/08/15 16:11:02	�@��ˑ������𕪗�
 */
static void dispatch(void) {
	/* ���Ɏ��s����^�X�N�̃R���e�L�X�g�𕜋A���� */
	RESTORE_CONTEXT();
	RETURN_FROM_INTERRUPT();
}

/**
 * @brief   �X�P�W���[���������֐�
 * @note    �X�P�W���[��������������B
 * @author  Kazuya Fukuhara
 * @date    2010/01/07 14:08:58	�쐬
 */
global void initScheduler(void) {

	/* readyqueue �������� */
	readyqueue.next_task = NULL;

	/* pausequeue �������� */
	waitqueue.next_task = NULL;

	/* INIT �^�X�N�̋N�� */
	INITTASK.status    = READY;
	INITTASK.next_task = NULL;
#if (SCHEDULER_TYPE == SCHEDULING_PRIORITY_BASE_TIME_SHARING)
	INITTASK.task_priolity = 0;
#endif
	addTaskToReadyQueue(&INITTASK);

	/* DIAG �^�X�N�̋N�� */
	DIAGTASK.status    = READY;
	DIAGTASK.next_task = NULL;
#if (SCHEDULER_TYPE == SCHEDULING_PRIORITY_BASE_TIME_SHARING)
	DIAGTASK.task_priolity = 0xFF;
#endif
	addTaskToReadyQueue(&DIAGTASK);

	/* �X�P�W���[�����O���K�v�Ȃ̂� rescheduling �� TRUE ��ݒ� */
	rescheduling = TRUE;

}

/**
 * @brief   �X�P�W���[���֐�
 * @note    �X�P�W���[�����O���s���AcurrentTCB�Ɏ��s���ׂ��^�X�N�̃^�X�N�R���g���[���u���b�N�̃|�C���^��ݒ肷��B
 * @author  Kazuya Fukuhara
 * @date    2010/01/07 14:08:58	�쐬
 * @date	2010/09/09 12:48:22	�^�C���V�F�A�����O�X�P�W���[�����O��ǉ�
 * @date	2010/09/10 10:57:13	�D��x�t���^�C���V�F�A�����O�X�P�W���[�����O��ǉ�
 */
#if (SCHEDULER_TYPE == SCHEDULING_PRIORITY_BASE)
global void scheduler(void) {
	if (rescheduling != FALSE) {
		/* readyqueue �̐擪���^�X�N�ɐݒ� */
		if ((readyqueue.next_task != NULL)&&(readyqueue.next_task < &tasks[TASK_NUM])) {
			currentTCB = readyqueue.next_task;
		}
		rescheduling = FALSE;
	}

	/* �f�B�X�p�b�`�������Ăяo�� */
	dispatch();
}
#elif (SCHEDULER_TYPE == SCHEDULING_TIME_SHARING)
global void scheduler(void) {
	if (currentTCB != NULL) {
		if (currentTCB->time_sharing_tick == 0) {
			/* readyqueue�̈�ԍŌ�ɑ��� */
			tcb_t *tcb = currentTCB;
			removeTaskFromReadyQueue(tcb);
			addTaskToReadyQueue(tcb);
			rescheduling = TRUE;
		}
	}

	if (rescheduling != FALSE) {
		/* readyqueue �̐擪���^�X�N�ɐݒ� */
		if ((readyqueue.next_task != NULL)&&(readyqueue.next_task < &tasks[TASK_NUM])) {
			currentTCB = readyqueue.next_task;
		}
		rescheduling = FALSE;
		assert(currentTCB != NULL);
		currentTCB->time_sharing_tick = TIME_SHARING_THRESHOLD;
	}

	/* �f�B�X�p�b�`�������Ăяo�� */
	dispatch();
}
#elif (SCHEDULER_TYPE == SCHEDULING_PRIORITY_BASE_TIME_SHARING)
global void scheduler(void) {
	tcb_t *tcb = readyqueue.next_task;
	if (currentTCB == tcb) {
		/* ���蓖�Ă�ꂽCPU���Ԃ��m�F */
		if (currentTCB->time_sharing_tick == 0) {
			/* CPU���Ԃ��g���؂����ꍇ�A����D�揇�ʂŎ��̃^�X�N������Ύ��s�����ړ����� */
			if ((currentTCB->next_task != NULL) && (currentTCB->task_priolity == currentTCB->next_task->task_priolity)) {
				removeTaskFromReadyQueue(currentTCB);
				addTaskToReadyQueue(currentTCB);
				rescheduling = TRUE;
			}
		}
	}
	if (rescheduling != FALSE) {
		if (readyqueue.next_task != NULL) {
			currentTCB = readyqueue.next_task;
			currentTCB->time_sharing_tick = TIME_SHARING_THRESHOLD;
		} else {
			currentTCB = &DIAGTASK;
			currentTCB->time_sharing_tick = 0;	/* DIAG�^�X�N�͏�Ɏ��s�����Ϗ��ł��� */
		}
		rescheduling = FALSE;
	}

	/* �f�B�X�p�b�`�������Ăяo�� */
	dispatch();
}
#else
#error "Scheduler type is undefined."
#endif

/**
 * @brief   �J�[�l���̃X�^�[�g�A�b�v�R�[�h
 * @author  Kazuya Fukuhara
 * @date    2010/01/07 14:08:58	�쐬
 * @date	2010/08/15 16:11:02	�@��ˑ������𕪗�
 */
global void startKernel(void) {

	/*
	 * �n�[�h�E�F�A��������
	 */
	initHardware();

	/*
	 * �Q�D�^�X�N�R���g���[���u���b�N������������
	 * �^�X�N�G���g���e�[�u�����Q�Ƃ��A�S�^�X�N�̃^�X�N�R���g���[���u���b�N���̃^�X�N���s�擪�A�h���X�A�����X�^�b�N�|�C���^�������l�ɐݒ肷��
	 */
	initTaskControlBlocks();

	/*
	 * �X�P�W���[��������������
	 */
	initScheduler();

	/*
	 * �Z�}�t�H������������
	 */
	initSemaphoQueue();

	/*
	 * ���b�Z�[�W������������
	 */
	initMessageQueue();

	/* �X�P�W���[�����O���s���A���Ɏ��s����^�X�N��I������ */
	scheduler();
}

/**
 * @brief	�O�����荞�݃t�b�N�\�̗v�f
 * @author	Kazuya Fukuhara
 * @date	2010/09/11 11:01:20	�쐬
 */
typedef struct {
	taskid_t	taskid;		/**< �O�����荞�݂ŋN������^�X�N�ԍ� */
#if (SCHEDULER_TYPE == SCHEDULING_PRIORITY_BASE_TIME_SHARING) 
	priolity_t	priolity;	/**< �N������^�X�N�̗D��x�i�D��x�t���^�C���V�F�A�����O�X�P�W���[�����O���ɗL���j */
#endif
} int_hook_info_t;

/**
 * @brief	�O�����荞�݃t�b�N�\
 * @author	Kazuya Fukuhara
 * @date	2010/09/11 11:01:20	�쐬
 */
static int_hook_info_t int_hook_tid[EXTINT_NUM];

/**
 * @brief					�O�����荞�݃t�b�N�\�ɏ�����ݒ肷��
 * @param[in] interrupt_id	�t�b�N����O�����荞�ݔԍ�
 * @param[in] task_id		���荞�݂ŋN������^�X�N�̔ԍ�
 * @param[in] priolity		�N������^�X�N�ɗ^�������(�D��x�t���^�C���V�F�A�����O�X�P�W���[�����O���ɗL��)
 * @author					Kazuya Fukuhara
 * @date					2010/09/11 11:01:20	�쐬
 */
global void set_int_hook_tid(extintid_t interrupt_id, taskid_t task_id
#if (SCHEDULER_TYPE == SCHEDULING_PRIORITY_BASE_TIME_SHARING) 
, priolity_t priolity
#endif
) {
	if (is_hookable_interrupt_id(interrupt_id)) {
		int_hook_tid[interrupt_id].taskid = task_id;
#if (SCHEDULER_TYPE == SCHEDULING_PRIORITY_BASE_TIME_SHARING) 
		int_hook_tid[interrupt_id].priolity = priolity;
#endif
	}
}

/**
 * @brief	�O�����荞�݂ɉ����ă^�X�N���N��
 * @author	Kazuya Fukuhara
 * @date	2010/09/11 11:01:20	�쐬
 */
global void external_interrupt_handler(void) {
	extintid_t intid = GetExtIntId();
	if (intid < EXTINT_NUM) {
		taskid_t tid = int_hook_tid[intid].taskid;
		if ((1<=tid)&&(tid<=TASK_NUM-2)) {	/* init/diag���w�肳��Ă���ꍇ�͉������Ȃ��i���S��j */
			tcb_t* task = &tasks[tid];
			if (task->status == DORMANT) {

				/* �N������^�X�N�̐ݒ� */
				task->status = READY;
				task->next_task = NULL;
				task->parameter = NULL;
#if (SCHEDULER_TYPE == SCHEDULING_PRIORITY_BASE_TIME_SHARING) 
				task->task_priolity = int_hook_tid[intid].priolity;
#endif

				/* ������ݒ� */
				SetTaskArg(GetContext(task),task->parameter);

				/* ready�L���[�ɑ}�� */
				addTaskToReadyQueue(task);

				/* �X�P�W���[�����O�v�� */
#if (SCHEDULER_TYPE == SCHEDULING_PRIORITY_BASE) 
				/* ���������D��x�̍����^�X�N���}�����ꂽ�ꍇ�ɂ̂݃X�P�W���[�����O��v�� */
				rescheduling = (currentTCB > task) ? TRUE : FALSE;
#elif (SCHEDULER_TYPE == SCHEDULING_TIME_SHARING) 
				/* �ǉ��ɔ����X�P�W���[�����O�͕s�v */
#elif (SCHEDULER_TYPE == SCHEDULING_PRIORITY_BASE_TIME_SHARING) 
				/* ���������D��x�̍����^�X�N���}�����ꂽ�ꍇ�ɂ̂݃X�P�W���[�����O��v�� */
				rescheduling = (currentTCB->task_priolity > task->task_priolity) ? TRUE : FALSE;
#endif
			}
		}
	}	
	scheduler();
} 

/**
 * @brief   �w��̈���[���N���A
 * @author  Kazuya Fukuhara
 * @date    2010/01/07 14:08:58	�쐬
 */
global void ZeroMemory(ptr_t buf, uint_t szbuf) {
	uint8_t *p = (uint8_t*)buf;
	while (szbuf--) {
		*p = 0;
		p++;
	}
}
