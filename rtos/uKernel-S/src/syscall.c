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
 * @brief   �V�X�e���R�[��
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	�쐬
 * @date	2010/08/15 16:11:02	�@��ˑ������𕪗�
 * @date	2010/09/10 10:57:13	�D��x�t���^�C���V�F�A�����O�X�P�W���[�����O�p�̃V�X�e���R�[����ǉ�
 * @date	2010/09/11 11:01:20	�O�����荞�݃t�b�N�p�̃V�X�e���R�[����ǉ�
 */

#include "arch.h"
#include "type.h"
#include "syscall.h"
#include "kernel.h"
#include "semapho.h"
#include "message.h"

/**
 * @enum	syscallid_t
 * @typedef	syscallid_t
 * @brief	�V�X�e���R�[���h�c
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	�쐬
 * @date	2010/09/10 10:57:13	�D��x�t���^�C���V�F�A�����O�X�P�W���[�����O�p�̃V�X�e���R�[���h�c(SVCID_setPriolity,SVCID_getPriolity)��ǉ�
 * @date	2010/09/11 11:01:20	�O�����荞�݃t�b�N�p�̃V�X�e���R�[���h�c(SVCID_hookInterrupt)��ǉ�
 */
typedef enum syscallid_t {
	SVCID_startTASK	 	=  0,
	SVCID_exitTASK		=  1,
	SVCID_pauseTASK		=  2,
	SVCID_resumeTASK 	=  3,
	SVCID_resetTASK	 	=  4,
	SVCID_restartTASK	=  5,
	SVCID_getTID	 	=  6,
	SVCID_takeSEMA	 	=  7,
	SVCID_giveSEMA	 	=  8,
	SVCID_tasSEMA	 	=  9,
	SVCID_hookIntTask	= 10,
	SVCID_restart		= 11,
	SVCID_sendMSG		= 12,
	SVCID_recvMSG		= 13,
	SVCID_waitMSG		= 14,
	SVCID_hookInterrupt	= 15,
#if (SCHEDULER_TYPE == SCHEDULING_PRIORITY_BASE_TIME_SHARING) 
	SVCID_setPriolity	= 16,
	SVCID_getPriolity	= 17,
#endif

} syscallid_t;

/**
 * @struct		PAR_syscall
 * @typedef		PAR_syscall
 * @brief		�V�X�e���R�[���̃p�����[�^���i�[����\���̂̊��N���X
 * @author		Kazuya Fukuhara
 * @date		2010/01/07 14:08:58	�쐬
 * 
 * �Ăяo���V�X�e���R�[���̂h�c�ƌ��ʂ̊i�[�ɗp�����邽�߁A
 * ���ׂẴV�X�e���R�[���̈����^�͂��̍\���̂��p�����č쐬���邱�ƁB
 */
typedef struct PAR_syscall {
	syscallid_t		id;			/**< �Ăяo���V�X�e���R�[���̂h�c */
	svcresultid_t	result;		/**< �V�X�e���R�[���̌��� */
} PAR_syscall;

/**
 * @struct		PAR_startTASK
 * @typedef		PAR_startTASK
 * @brief		SVC_startTASK�̃p�����[�^���i�[����\����
 * @author		Kazuya Fukuhara
 * @date		2010/01/07 14:08:58	�쐬
 * @date		2010/09/10 10:57:13	�D��x�t���^�C���V�F�A�����O�X�P�W���[�����O�Ŏg�p������� priolity ��ǉ�
 */
typedef struct PAR_startTASK {
	PAR_syscall		syscall;	/**< �V�X�e���R�[���̏�񂪊i�[����� */
	taskid_t		taskId;		/**< �N������^�X�N�̔ԍ� */
	ptr_t			param;		/**< �N������^�X�N�ɗ^������� */
#if (SCHEDULER_TYPE == SCHEDULING_PRIORITY_BASE_TIME_SHARING) 
	priolity_t		priolity;	/**< �N������^�X�N�ɐݒ肷��D�揇�� */
#endif
} PAR_startTASK;

/**
 * @brief			�w�肵���^�X�N�h�c�̃^�X�N���N������
 * @param[in] param	�V�X�e���R�[���Ăяo���̈������i�[���ꂽ�\���̂ւ̃|�C���^
 * @return			�V�X�e���R�[���̐��ۏ��
 * @author			Kazuya Fukuhara
 * @date			2010/01/07 14:08:58	�쐬
 * @date			2010/08/15 16:11:02	�@��ˑ������𕪗�
 * @date			2010/09/09 12:48:22	�^�C���V�F�A�����O�X�P�W���[�����O�̒ǉ��ɂ��X�P�W���[�����O�v���̒ǉ�
 * @date			2010/09/10 10:57:13	�D��x�t���^�C���V�F�A�����O�X�P�W���[�����O�̒ǉ�
 */
static svcresultid_t SVC_startTASK(PAR_startTASK *param) {
	tcb_t *ptcb;

#if 0
	if ((param->taskId == 0) || (param->taskId >= TASK_NUM-1)) {
#else
	if (isUserTask(param->taskId) == FALSE) {
#endif
		/* INITTASK��DIAGTASK�͋N���ł��Ȃ������[�U�[�^�X�N�ȊO�͋N���ł��Ȃ� */
		return ERR1;
	} else {
		ptcb = &(tasks[param->taskId]);
	}

	/* �N���ł���^�X�N�͎����ȊO����DORMANT��Ԃ̂��� */
	if ((ptcb != currentTCB) && (ptcb->status == DORMANT)) {

		/* �N������^�X�N�̐ݒ� */
		ptcb->status = READY;
		ptcb->next_task = NULL;
		ptcb->parameter = param->param;
#if (SCHEDULER_TYPE == SCHEDULING_PRIORITY_BASE_TIME_SHARING) 
		ptcb->task_priolity = param->priolity;
#endif

		/* ������ݒ� */
		SetTaskArg(GetContext(ptcb),ptcb->parameter);

		/* ready�L���[�ɑ}�� */
		addTaskToReadyQueue(ptcb);

		/* �X�P�W���[�����O�v�� */
#if (SCHEDULER_TYPE == SCHEDULING_PRIORITY_BASE) 
		rescheduling = (currentTCB > ptcb) ? TRUE : FALSE;
#elif (SCHEDULER_TYPE == SCHEDULING_TIME_SHARING) 
		/* �ǉ��ɔ����X�P�W���[�����O�͕s�v */
#elif (SCHEDULER_TYPE == SCHEDULING_PRIORITY_BASE_TIME_SHARING) 
		rescheduling = (currentTCB->task_priolity > ptcb->task_priolity) ? TRUE : FALSE;
#endif
		return SUCCESS;
	} else {
		return ERR2;
	}
}

/**
 * @struct		PAR_exitTASK
 * @typedef		PAR_exitTASK
 * @brief		SVC_exitTASK�̃p�����[�^���i�[����\����
 * @author		Kazuya Fukuhara
 * @date		2010/01/07 14:08:58	�쐬
 */
typedef struct PAR_exitTASK {
	PAR_syscall		syscall;	/**< �V�X�e���R�[���̏�񂪊i�[����� */
} PAR_exitTASK;

/**
 * @brief			���ݎ��s���̃^�X�N���I������B
 * @param[in] param	�V�X�e���R�[���Ăяo���̈������i�[���ꂽ�\���̂ւ̃|�C���^
 * @return			�V�X�e���R�[���̐��ۏ��
 * @note			�^�X�N�̃��Z�b�g�������s����B
 * @author			Kazuya Fukuhara
 * @date			2010/01/07 14:08:58	�쐬
 */
static svcresultid_t SVC_exitTASK(PAR_exitTASK *param) {
	(void)param;
	if (&DIAGTASK == currentTCB) {
		/* DIAGTASK�͏I���ł��Ȃ� */
		return ERR1;
	} else {
		/* ���݂̃^�X�N�Ɗ֘A�t�����Ă���Z�}�t�H�����ׂĊJ�� */
		unlinkSEMA(currentTCB);

		/* ���݂̃^�X�N�Ɗ֘A�t�����Ă��郁�b�Z�[�W�����ׂĊJ�� */
		unlinkMSG(currentTCB);

		/* ReadyQueue���猻�݂̃^�X�N�����O�� */
		removeTaskFromReadyQueue(currentTCB);

		/* TCB�����Z�b�g */
		resetTCB( getTaskID(currentTCB) );

		/* �X�P�W���[�����O�v����ݒ� */
		rescheduling = TRUE;

		/* ���� */
		return SUCCESS;
	}
}

/**
 * @struct		PAR_pauseTASK
 * @typedef		PAR_pauseTASK
 * @brief		SVC_pauseTASK�̃p�����[�^���i�[����\����
 * @author		Kazuya Fukuhara
 * @date		2010/01/07 14:08:58	�쐬
 */
typedef struct PAR_pauseTASK {
	PAR_syscall		syscall;	/**< �V�X�e���R�[���̏�񂪊i�[����� */
	tick_t			count;		/**< �x����Ԃɂ��鎞�ԁitick�P�ʁj */
} PAR_pauseTASK;

/**
 * @brief			���ݎ��s���̃^�X�N���x����Ԃɂ���
 * @param[in] param	�V�X�e���R�[���Ăяo���̈������i�[���ꂽ�\���̂ւ̃|�C���^
 * @return			�V�X�e���R�[���̐��ۏ��
 * @note            count��0�ɐݒ肷��ƁAresumeTASK�V�X�e���R�[���ōĊJ������܂Ŗ����ɋx������B
 * @warning			�����ԋx��(count > 255)�ɂ͊��荞�݂ȂǕʂ̗v�f���g�����ƁB
 * @author          Kazuya Fukuhara
 * @date            2010/01/07 14:08:58	�쐬
 */
static svcresultid_t SVC_pauseTASK(PAR_pauseTASK *param) {
	if ((&INITTASK == currentTCB) || (&DIAGTASK == currentTCB)) {
		return ERR1;
	} else {
		currentTCB->status = PAUSE;
		currentTCB->pause_count = param->count;
		removeTaskFromReadyQueue(currentTCB);
		if (param->count != 0 ) {
			/* �������x�~�ł͂Ȃ��ꍇ�AWaitQueue�ɒǉ����� */
			addTaskToWaitQueue(currentTCB, param->count);
		}
		rescheduling = TRUE;
		return SUCCESS;
	}
}

/**
 * @struct		PAR_resumeTASK
 * @typedef		PAR_resumeTASK
 * @brief		SVC_resumeTASK�̃p�����[�^���i�[����\����
 * @author		Kazuya Fukuhara
 * @date		2010/01/07 14:08:58	�쐬
 */
typedef struct PAR_resumeTASK {
	PAR_syscall		syscall;	/**< �V�X�e���R�[���̏�񂪊i�[����� */
	taskid_t		taskId;		/**< �ĊJ����^�X�N�̃^�X�N�h�c */
} PAR_resumeTASK;

/**
 * @brief			�w�肵���x����Ԃ̃^�X�N���ĊJ����
 * @param[in] param	�V�X�e���R�[���Ăяo���̈������i�[���ꂽ�\���̂ւ̃|�C���^
 * @return			�V�X�e���R�[���̐��ۏ��
 * @author			Kazuya Fukuhara
 * @date			2010/01/07 14:08:58	�쐬
 */
static svcresultid_t SVC_resumeTASK(PAR_resumeTASK *param) {
	tcb_t* tcb;

	if (param->taskId >= TASK_NUM) {
		/* �s���ȃ^�X�NID��taskId�Ɏw�肳��Ă��� */
		return ERR1;
	} else if (param->taskId == TASK_NUM-1) {
		/* DIAGTASK��resume�ł��Ȃ� */
		return ERR31;
	} else {
		tcb = &tasks[param->taskId];
	}

	/* �x�~���̃^�X�N���m�F */
	if (tcb->status != PAUSE) {
		/* �Z�}�t�H�҂��ȂǁAPAUSE��Ԃł͂Ȃ��^�X�N��resume������ */
		rescheduling = FALSE;
		return ERR5;
	}
	
	/* �L���x���̏ꍇ�̓^�X�N��WaitQueue�ɓo�^����Ă���̂Ŏ�菜��
	 * �����x���̏ꍇ�͓o�^����Ă��Ȃ�
	 */
	(void)removeTaskFromWaitQueue(tcb);


	/* �^�X�N���Ăщғ������� */
	tcb->status = READY;
	tcb->pause_count = 0;
	
	/* ������ݒ� */
	SetTaskArg(GetContext(tcb),tcb->parameter);

	addTaskToReadyQueue(currentTCB);

	/* �X�P�W���[�����O�v�� */
#if (SCHEDULER_TYPE == SCHEDULING_PRIORITY_BASE) 
	rescheduling = (currentTCB > tcb) ? TRUE : FALSE;
#elif (SCHEDULER_TYPE == SCHEDULING_TIME_SHARING) 
	/* �X�P�W���[�����O�͕s�v */
#elif (SCHEDULER_TYPE == SCHEDULING_PRIORITY_BASE_TIME_SHARING) 
	rescheduling = (currentTCB->task_priolity > tcb->task_priolity) ? TRUE : FALSE;
#endif

	return SUCCESS;
}

/**
 * @struct		PAR_restartTASK 
 * @typedef		PAR_restartTASK 
 * @brief		SVC_restartTASK�̃p�����[�^���i�[����\����
 * @author		Kazuya Fukuhara
 * @date		2010/01/07 14:08:58	�쐬
 */
typedef struct PAR_restartTASK {
	PAR_syscall		syscall;	/**< �V�X�e���R�[���̏�񂪊i�[����� */
	tick_t			count;		/**< ���X�^�[�g����܂ł̎��ԁitick�P�ʁj */
} PAR_restartTASK ;

/**
 * @brief			���^�X�N�����X�^�[�g����
 * @param[in] param	�V�X�e���R�[���Ăяo���̈������i�[���ꂽ�\���̂ւ̃|�C���^
 * @return			�V�X�e���R�[���̐��ۏ��
 * @note            count��0�ɐݒ肷�邱�Ɓi�����x���j�͂ł��Ȃ�
 * @note            �����ԋx��(count > 255)�ɂ͊��荞�݂ȂǕʂ̗v�f���g�����ƁB
 * @author          Kazuya Fukuhara
 * @date            2010/01/07 14:08:58	�쐬
 */
static svcresultid_t SVC_restartTASK(PAR_restartTASK *param) {
	tcb_t* tcb = currentTCB;
	PAR_exitTASK dummy;
	message_t *message;
	if (param->count == 0 ) {
		/* �������x�~�͔F�߂Ȃ� */
		return ERR6;
	} else {
		/* ok */
	}

	/** 
	 * �ʏ�̃^�X�N�I���Ɠ��l�ɒP����exitTASK()�ŏI��������ƁAmessage���J������Ă��܂����߁A
	 * ��[message��ޔ�������ɁA�^�X�N��exitTASK()�ŏI�������A���̌ナ�X�^�[�g�҂��ɑJ�ڂ����Ă���
	 * �ēxmessage��ݒ肷�邱�ƂŃ��X�^�[�g�������b�Z�[�W��ێ����邱�Ƃ��\�Ƃ���
	 */

	/* message��ޔ� */
	message = tcb->message;
	tcb->message = NULL;

	/* �^�X�N��exitTASK()�ŏI������B */
	SVC_exitTASK(&dummy);

	/* �ēxmessage��ݒ� */
	tcb->message = message;
	
	/* ���̎��_�Ń^�X�N�� DORMANT ��ԂɂȂ��Ă���̂ŁAWAIT_RESTART��Ԃ֑J�ڂ�������AWaitQueue�ɓ������� */
	tcb->status = WAIT_RESTART;
	addTaskToWaitQueue(tcb, param->count);

	rescheduling = TRUE;
	return SUCCESS;
}

/**
 * @struct		PAR_getTID
 * @typedef		PAR_getTID
 * @brief		SVC_getTID�̃p�����[�^���i�[����\����
 * @author		Kazuya Fukuhara
 * @date		2010/01/07 14:08:58	�쐬
 */
typedef struct PAR_getTID {
	PAR_syscall		syscall;	/**< �V�X�e���R�[���̏�񂪊i�[����� */
	taskid_t		taskId;		/**< �V�X�e���R�[���ɂ���Ď��^�X�N�̃^�X�N�h�c���i�[����� */
} PAR_getTID;

/**
 * @brief				���^�X�N�̃^�X�N�h�c���擾����
 * @param[in] param		�V�X�e���R�[���Ăяo���̈������i�[���ꂽ�\���̂ւ̃|�C���^
 * @return				�V�X�e���R�[���̐��ۏ��
 * @author				Kazuya Fukuhara
 * @date				2010/01/07 14:08:58	�쐬
 */
static svcresultid_t SVC_getTID(PAR_getTID *param) {
	param->taskId = getTaskID(currentTCB);
	return SUCCESS;
}

/**
 * @struct		PAR_takeSEMA
 * @typedef		PAR_takeSEMA
 * @brief		SVC_takeSEMA�̃p�����[�^���i�[����\����
 * @author		Kazuya Fukuhara
 * @date		2010/01/07 14:08:58	�쐬
 */
typedef struct PAR_takeSEMA {
	PAR_syscall		syscall;	/**< �V�X�e���R�[���̏�񂪊i�[����� */
	semaphoid_t		sid;		/**< �l������Z�}�t�H�̂h�c */
} PAR_takeSEMA;

/**
 * @brief			takeSEMA���Ăяo���A�_�v�^�֐�
 * @param[in] param	�V�X�e���R�[���Ăяo���̈������i�[���ꂽ�\���̂ւ̃|�C���^
 * @return			�V�X�e���R�[���̐��ۏ��
 * @author			Kazuya Fukuhara
 * @date			2010/01/07 14:08:58	�쐬
 */
static svcresultid_t SVC_takeSEMA(PAR_takeSEMA *param) {
	return takeSEMA(param->sid);
}

/**
 * @struct		PAR_giveSEMA
 * @typedef		PAR_giveSEMA
 * @brief		SVC_giveSEMA�̃p�����[�^���i�[����\����
 * @author		Kazuya Fukuhara
 * @date		2010/01/07 14:08:58	�쐬
 */
typedef struct PAR_giveSEMA {
	PAR_syscall		syscall;	/**< �V�X�e���R�[���̏�񂪊i�[����� */
	semaphoid_t		sid;		/**< ��������Z�}�t�H�̂h�c */
} PAR_giveSEMA;

/**
 * @brief			giveSEMA���Ăяo���A�_�v�^�֐�
 * @param[in] param	�V�X�e���R�[���Ăяo���̈������i�[���ꂽ�\���̂ւ̃|�C���^
 * @return			�V�X�e���R�[���̐��ۏ��
 * @author			Kazuya Fukuhara
 * @date			2010/01/07 14:08:58	�쐬
 */
static svcresultid_t SVC_giveSEMA(PAR_giveSEMA *param) {
	return giveSEMA(param->sid);
}

/**
 * @struct		PAR_tasSEMA
 * @typedef		PAR_tasSEMA
 * @brief		SVC_tasSEMA�̃p�����[�^���i�[����\����
 * @author		Kazuya Fukuhara
 * @date		2010/01/07 14:08:58	�쐬
 */
typedef struct PAR_tasSEMA {
	PAR_syscall		syscall;	/**< �V�X�e���R�[���̏�񂪊i�[����� */
	semaphoid_t		sid;		/**< �l�������݂�Z�}�t�H�̂h�c */
} PAR_tasSEMA;

/**
 * @brief			tasSEMA���Ăяo���A�_�v�^�֐�
 * @param[in] param	�V�X�e���R�[���Ăяo���̈������i�[���ꂽ�\���̂ւ̃|�C���^
 * @return			�V�X�e���R�[���̐��ۏ��
 * @author			Kazuya Fukuhara
 * @date			2010/01/07 14:08:58	�쐬
 */
static svcresultid_t SVC_tasSEMA(PAR_tasSEMA *param) {
	return tasSEMA(param->sid);
}

/**
 * @struct		PAR_recvMSG
 * @typedef		PAR_recvMSG
 * @brief		SVC_recvMSG�̃p�����[�^���i�[����\����
 * @author		Kazuya Fukuhara
 * @date		2010/01/07 14:08:58	�쐬
 */
typedef struct PAR_recvMSG {
	PAR_syscall		syscall;	/**< �V�X�e���R�[���̏�񂪊i�[����� */
	taskid_t		from;		/**< ���M���̃^�X�N�̂h�c���i�[����� */
	ptr_t			data;		/**< ��M�������b�Z�[�W�̖{�����i�[����� */
} PAR_recvMSG;

/**
 * @brief			recvMSG���Ăяo���A�_�v�^�֐�
 * @param[in] param	�V�X�e���R�[���Ăяo���̈������i�[���ꂽ�\���̂ւ̃|�C���^
 * @return			�V�X�e���R�[���̐��ۏ��
 * @author			Kazuya Fukuhara
 * @date			2010/01/07 14:08:58	�쐬
 */
static svcresultid_t SVC_recvMSG(PAR_recvMSG *param) {
	return recvMSG(&param->from, &param->data);
}

/**
 * @struct		PAR_sendMSG
 * @typedef		PAR_sendMSG
 * @brief		SVC_sendMSG�̃p�����[�^���i�[����\����
 * @author		Kazuya Fukuhara
 * @date		2010/01/07 14:08:58	�쐬
 */
typedef struct PAR_sendMSG {
	PAR_syscall		syscall;	/**< �V�X�e���R�[���̏�񂪊i�[����� */
	taskid_t		to;			/**< ���M��̃^�X�N�̂h�c */
	ptr_t			data;		/**< ���M���郁�b�Z�[�W�̖{�� */
} PAR_sendMSG;

/**
 * @brief			sendMSG���Ăяo���A�_�v�^�֐�
 * @param[in] param	�V�X�e���R�[���Ăяo���̈������i�[���ꂽ�\���̂ւ̃|�C���^
 * @return			�V�X�e���R�[���̐��ۏ��
 * @author			Kazuya Fukuhara
 * @date			2010/01/07 14:08:58	�쐬
 */
static svcresultid_t SVC_sendMSG(PAR_sendMSG *param) {
	return sendMSG(param->to, param->data);
}

/**
 * @struct		PAR_waitMSG
 * @typedef		PAR_waitMSG
 * @brief		SVC_waitMSG�̃p�����[�^���i�[����\����
 * @author		Kazuya Fukuhara
 * @date		2010/01/07 14:08:58	�쐬
 */
typedef struct PAR_waitMSG {
	PAR_syscall		syscall;	/**< �V�X�e���R�[���̏�񂪊i�[����� */
} PAR_waitMSG;

/**
 * @brief			waitMSG���Ăяo���A�_�v�^�֐�
 * @param[in] param	�V�X�e���R�[���Ăяo���̈������i�[���ꂽ�\���̂ւ̃|�C���^
 * @return			�V�X�e���R�[���̐��ۏ��
 * @author			Kazuya Fukuhara
 * @date			2010/01/07 14:08:58	�쐬
 */
static svcresultid_t SVC_waitMSG(PAR_waitMSG *param) {
	(void)param;
	return waitMSG();
}

/**
 * @struct		PAR_hookInterrupt 
 * @typedef		PAR_hookInterrupt 
 * @brief		SVC_hookInterrupt�̃p�����[�^���i�[����\����
 * @author		Kazuya Fukuhara
 * @date		2010/09/11 11:01:20	�쐬
 */
typedef struct PAR_hookInterrupt  {
	PAR_syscall	syscall;		/**< �V�X�e���R�[���̏�񂪊i�[����� */
	extintid_t	interrupt_id;	/**< �O�����荞�ݔԍ� */
	taskid_t	task_id;		/**< ���荞�݂ŋN������^�X�N�̔ԍ� */
#if (SCHEDULER_TYPE == SCHEDULING_PRIORITY_BASE_TIME_SHARING) 
	priolity_t	priolity;		/**< �N������^�X�N�ɐݒ肷��D�揇�� */
#endif
} PAR_hookInterrupt ;

/**
 * @brief			�O�������݂ɂ��A�w��̃^�X�N�����s����
 * @param[in] param	�V�X�e���R�[���Ăяo���̈������i�[���ꂽ�\���̂ւ̃|�C���^
 * @return			�V�X�e���R�[���̐��ۏ��
 * @author			Kazuya Fukuhara
 * @date			2010/09/11 11:01:20	�쐬
 */
static svcresultid_t SVC_hookInterrupt(PAR_hookInterrupt *param) {
	/* ���荞�ݔԍ��̗L���͈͂̓V�X�e�����ƂɈႤ�̂őÓ��������� */
	if (is_hookable_interrupt_id(param->interrupt_id) == FALSE) {
		return ERR27;
	}

	if ((1<=param->task_id)&&(param->task_id<=TASK_NUM-2)) {
		set_int_hook_tid(param->interrupt_id, param->task_id
#if (SCHEDULER_TYPE == SCHEDULING_PRIORITY_BASE_TIME_SHARING) 
			,param->priolity
#endif
		);
		EnableExtInterrupt(param->interrupt_id);
	} else {
		set_int_hook_tid(param->interrupt_id, (taskid_t)-1
#if (SCHEDULER_TYPE == SCHEDULING_PRIORITY_BASE_TIME_SHARING) 
			,0xFE
#endif
		);
		DisableExtInterrupt(param->interrupt_id);
	}

	return SUCCESS;
}

#if (SCHEDULER_TYPE == SCHEDULING_PRIORITY_BASE_TIME_SHARING) 
/**
 * @struct		PAR_setPriolity
 * @typedef		PAR_setPriolity
 * @brief		SVC_setPriolity�̃p�����[�^���i�[����\����
 * @author		Kazuya Fukuhara
 * @date		2010/09/10 10:57:13	�쐬
 * @note		�X�P�W���[�����O�A���S���Y�����D��x�t���^�C���V�F�A�����O�̏ꍇ�Ɏg�p�\
 */
typedef struct PAR_setPriolity {
	PAR_syscall		syscall;	/**< �V�X�e���R�[���̏�񂪊i�[����� */
	priolity_t		priolity;	/**< ���^�X�N�ɐݒ肷��D��x */
} PAR_setPriolity;

/**
 * @brief			���^�X�N�̗D��x��ύX����
 * @param[in] param	�V�X�e���R�[���Ăяo���̈������i�[���ꂽ�\���̂ւ̃|�C���^
 * @return			�V�X�e���R�[���̐��ۏ��
 * @author			Kazuya Fukuhara
 * @date			2010/09/10 10:57:13	�쐬
 * @note			�X�P�W���[�����O�A���S���Y�����D��x�t���^�C���V�F�A�����O�̏ꍇ�Ɏg�p�\
 */
static svcresultid_t SVC_setPriolity(PAR_setPriolity *param) {
	if ((&INITTASK == currentTCB) || (&DIAGTASK == currentTCB)) {
		return ERR1;
	} else {
		removeTaskFromReadyQueue(currentTCB);
		currentTCB->task_priolity = param->priolity;
		addTaskToReadyQueue(currentTCB);
		rescheduling = TRUE;
		return SUCCESS;
	}
}

/**
 * @struct		PAR_getPriolity
 * @typedef		PAR_getPriolity
 * @brief		SVC_getPriolity�̃p�����[�^���i�[����\����
 * @author		Kazuya Fukuhara
 * @date		2010/01/07 14:08:58	�쐬
 */
typedef struct PAR_getPriolity {
	PAR_syscall		syscall;	/**< �V�X�e���R�[���̏�񂪊i�[����� */
	priolity_t		priolity;	/**< ���^�X�N�̗D��x */
} PAR_getPriolity;

/**
 * @brief			���^�X�N�̗D��x���擾����
 * @param[in] param	�V�X�e���R�[���Ăяo���̈������i�[���ꂽ�\���̂ւ̃|�C���^
 * @return			�V�X�e���R�[���̐��ۏ��
 * @author			Kazuya Fukuhara
 * @date			2010/09/10 10:57:13	�쐬
 * @note			�X�P�W���[�����O�A���S���Y�����D��x�t���^�C���V�F�A�����O�̏ꍇ�Ɏg�p�\
 */
static svcresultid_t SVC_getPriolity(PAR_getPriolity *param) {
	param->priolity = currentTCB->task_priolity;
	return SUCCESS;
}
#endif

/**
 * @brief	�V�X�e���R�[���Ăяo������
 * @note	�@��ˑ��R�[�h���܂܂��
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	�쐬
 * @date	2010/08/15 16:11:02	�@��ˑ������𕪗�
 * @date	2010/09/10 10:57:13	�D��x�t���^�C���V�F�A�����O�X�P�W���[�����O�p�̃V�X�e���R�[���Ăяo����ǉ�
 */
void syscall_entry(void) {

	context_t* context;
	PAR_syscall *par;

	/* �Ăяo�����̃R���e�L�X�g����V�X�e���R�[���̈����𓾂� */
	context = GetContext(currentTCB);
	par = (PAR_syscall*)GetArgPtr(context);

	/* �V�X�e���R�[����ID�ɉ��������� */
	switch (par->id) {
		case SVCID_startTASK	: par->result = SVC_startTASK((PAR_startTASK*)par); break;
		case SVCID_exitTASK		: /* par->result = */ SVC_exitTASK((PAR_exitTASK*)par); break;
		case SVCID_pauseTASK	: par->result = SVC_pauseTASK((PAR_pauseTASK*)par); break;
		case SVCID_resumeTASK	: par->result = SVC_resumeTASK((PAR_resumeTASK*)par); break;
		case SVCID_restartTASK	: /* par->result = */ SVC_restartTASK((PAR_restartTASK*)par); break;

		case SVCID_takeSEMA	 	: par->result = SVC_takeSEMA((PAR_takeSEMA*)par); break;
		case SVCID_giveSEMA	 	: par->result = SVC_giveSEMA((PAR_giveSEMA*)par); break;
		case SVCID_tasSEMA	 	: par->result = SVC_tasSEMA((PAR_tasSEMA*)par); break;

		case SVCID_sendMSG	 	: par->result = SVC_sendMSG((PAR_sendMSG*)par); break;
		case SVCID_recvMSG	 	: par->result = SVC_recvMSG((PAR_recvMSG*)par); break;
		case SVCID_waitMSG	 	: par->result = SVC_waitMSG((PAR_waitMSG*)par); break;

		case SVCID_getTID		: par->result = SVC_getTID((PAR_getTID*)par); break;

		case SVCID_hookInterrupt: par->result = SVC_hookInterrupt((PAR_hookInterrupt*)par); break;

#if (SCHEDULER_TYPE == SCHEDULING_PRIORITY_BASE_TIME_SHARING) 
		case SVCID_setPriolity	: par->result = SVC_setPriolity((PAR_setPriolity*)par); break;
		case SVCID_getPriolity	: par->result = SVC_getPriolity((PAR_getPriolity*)par); break;
#endif

		default					: par->result = ERR30; break;
	}

	/* �X�P�W���[�����O���s���A���Ɏ��s����^�X�N��I������ */
	scheduler();
}

/**
 * �ȍ~�̓��[�U�[�Ɍ��J����API
 */

/**
 * @brief		�w�肵���^�X�N�h�c�̃^�X�N���N������
 * @param[in]	taskId �N������^�X�N�̃^�X�N�h�c
 * @param[in]	param �N������^�X�N�ɗ^�������
 * @param[in]	priolity �N������^�X�N�ɗ^�������(�D��x�t���^�C���V�F�A�����O�X�P�W���[�����O���ɗL��)
 * @return		�V�X�e���R�[���̐��ۏ��
 * @author		Kazuya Fukuhara
 * @date		2010/01/07 14:08:58	�쐬
 * @date		2010/08/15 16:11:02	�@��ˑ������𕪗�
 * @date		2010/09/10 10:57:13	�D��x�t���^�C���V�F�A�����O�X�P�W���[�����O�Ŏg�p������� priolity ��ǉ�
 */
extern svcresultid_t API_startTASK(taskid_t taskId, ptr_t param
#if (SCHEDULER_TYPE == SCHEDULING_PRIORITY_BASE_TIME_SHARING) 
, priolity_t priolity
#endif
) {
	PAR_startTASK par;
	par.syscall.id = SVCID_startTASK;
	par.taskId = taskId;
	par.param = param;
#if (SCHEDULER_TYPE == SCHEDULING_PRIORITY_BASE_TIME_SHARING) 
	par.priolity = priolity;
#endif
	syscall( (ptr_t)&par);
	return par.syscall.result;
}

/**
 * @brief			���ݎ��s���̃^�X�N���I������B
 * @return			�V�X�e���R�[���̐��ۏ��
 * @note			�^�X�N�̃��Z�b�g�������s����B
 * @author			Kazuya Fukuhara
 * @date			2010/01/07 14:08:58	�쐬
 */
svcresultid_t API_exitTASK(void) {
	PAR_exitTASK par;
	par.syscall.id = SVCID_exitTASK;
	syscall( (ptr_t)&par );
	return par.syscall.result;
}

/**
 * @brief			���ݎ��s���̃^�X�N���x����Ԃɂ���
 * @param[in] count	�x����Ԃɂ��鎞�ԁitick�P�ʁj
 * @return			�V�X�e���R�[���̐��ۏ��
 * @note            count��0�ɐݒ肷��ƁAresumeTASK�V�X�e���R�[���ōĊJ������܂Ŗ����ɋx������B
 * @note            �����ԋx��(count > 255)�ɂ͊��荞�݂ȂǕʂ̗v�f���g�����ƁB
 * @author          Kazuya Fukuhara
 * @date            2010/01/07 14:08:58	�쐬
 */
svcresultid_t API_pauseTASK(tick_t count) {
	PAR_pauseTASK par;
	par.syscall.id = SVCID_pauseTASK;
	par.count = count;
	syscall( (ptr_t)&par );
	return par.syscall.result;
}

/**
 * @brief				�w�肵���x����Ԃ̃^�X�N���ĊJ���ɂ���
 * @param[in] taskId	�ĊJ����^�X�N�̃^�X�N�h�c
 * @return				�V�X�e���R�[���̐��ۏ��
 * @author			    Kazuya Fukuhara
 * @date			    2010/01/07 14:08:58	�쐬
 */
svcresultid_t API_resumeTASK(taskid_t taskId) {
	PAR_resumeTASK par;
	par.syscall.id = SVCID_resumeTASK;
	par.taskId = taskId;
	syscall( (ptr_t)&par );
	return par.syscall.result;
}

/**
 * @brief			���^�X�N�����X�^�[�g����
 * @param[in] count	�ĊJ����܂ł̎��ԁitick�P�ʁj
 * @return			�V�X�e���R�[���̐��ۏ��
 * @note            count��0�ɐݒ肷��ƁAresumeTASK�V�X�e���R�[���ōĊJ������܂Ŗ����ɋx������B
 * @note            �����ԋx��(count > 255)�ɂ͊��荞�݂ȂǕʂ̗v�f���g�����ƁB
 * @author          Kazuya Fukuhara
 * @date            2010/01/07 14:08:58	�쐬
 */
svcresultid_t API_restartTASK(tick_t count) {
	PAR_restartTASK par;
	par.syscall.id = SVCID_restartTASK;
	par.count = count;
	syscall( (ptr_t)&par );
	return par.syscall.result;
}

/**
 * @brief				���^�X�N�̃^�X�N�h�c���擾����
 * @param[out] pTaskID	���^�X�N�̃^�X�N�h�c���i�[�����̈�
 * @return				�V�X�e���R�[���̐��ۏ��
 * @author				Kazuya Fukuhara
 * @date				2010/01/07 14:08:58	�쐬
 */
svcresultid_t API_getTID(taskid_t *pTaskID) {
	PAR_getTID par;
	par.syscall.id = SVCID_getTID;
	syscall( (ptr_t)&par );
	*pTaskID = par.taskId;
	return par.syscall.result;
}

/**
 * @brief				takeSEMA���Ăяo���A�_�v�^�֐�
 * @param[in] sid		�l������Z�}�t�H�̂h�c
 * @return				�V�X�e���R�[���̐��ۏ��
 * @author				Kazuya Fukuhara
 * @date				2010/01/07 14:08:58	�쐬
 */
svcresultid_t API_takeSEMA(semaphoid_t sid) {
	PAR_takeSEMA par;
	par.syscall.id = SVCID_takeSEMA;
	par.sid = sid;
	syscall( (ptr_t)&par );
	return par.syscall.result;
}

/**
 * @brief				giveSEMA���Ăяo���A�_�v�^�֐�
 * @param[in] sid		��������Z�}�t�H�̂h�c
 * @return				�V�X�e���R�[���̐��ۏ��
 * @author				Kazuya Fukuhara
 * @date				2010/01/07 14:08:58	�쐬
 */
svcresultid_t API_giveSEMA(semaphoid_t sid) {
	PAR_giveSEMA par;
	par.syscall.id = SVCID_giveSEMA;
	par.sid = sid;
	syscall( (ptr_t)&par );
	return par.syscall.result;
}

/**
 * @brief				tasSEMA���Ăяo���A�_�v�^�֐�
 * @param[in] sid		�l�������݂�Z�}�t�H�̂h�c
 * @return				�V�X�e���R�[���̐��ۏ��
 * @author				Kazuya Fukuhara
 * @date				2010/01/07 14:08:58	�쐬
 */
svcresultid_t API_tasSEMA(semaphoid_t sid) {
	PAR_tasSEMA par;
	par.syscall.id = SVCID_tasSEMA;
	par.sid = sid;
	syscall( (ptr_t)&par );
	return par.syscall.result;
}

/**
 * @brief				recvMSG���Ăяo���A�_�v�^�֐�
 * @param[out] from		���b�Z�[�W�̑��M�^�X�NID���i�[����̈�̃|�C���^
 * @param[out] data		���b�Z�[�W���i�[����̈�̃|�C���^
 * @return				�V�X�e���R�[���̐��ۏ��
 * @author				Kazuya Fukuhara
 * @date				2010/01/07 14:08:58	�쐬
 */
svcresultid_t API_recvMSG(taskid_t* from, ptr_t* data) {
	PAR_recvMSG par;
	par.syscall.id = SVCID_recvMSG;
	syscall( (ptr_t)&par );
	*from = par.from;
	*data = par.data;
	return par.syscall.result;
}

/**
 * @brief				sendMSG���Ăяo���A�_�v�^�֐�
 * @param[in] to		���M��̃^�X�N�̂h�c
 * @param[in] data		���M���郁�b�Z�[�W�̖{��
 * @return				�V�X�e���R�[���̐��ۏ��
 * @author				Kazuya Fukuhara
 * @date				2010/01/07 14:08:58	�쐬
 */
svcresultid_t API_sendMSG(taskid_t to, ptr_t data) {
	PAR_sendMSG par;
	par.syscall.id = SVCID_sendMSG;
	par.to = to;
	par.data = data;
	syscall( (ptr_t)&par );
	return par.syscall.result;
}

/**
 * @brief	waitMSG���Ăяo���A�_�v�^�֐�
 * @return	�V�X�e���R�[���̐��ۏ��
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	�쐬
 */
svcresultid_t API_waitMSG(void) {
	PAR_waitMSG par;
	par.syscall.id = SVCID_waitMSG;
	syscall( (ptr_t)&par );
	return par.syscall.result;
}


/**
 * @brief					hookInterrupt���Ăяo���A�_�v�^�֐�
 * @param[in] interrupt_id	�t�b�N����O�����荞�ݔԍ�
 * @param[in] task_id		���荞�݂ŋN������^�X�N�̔ԍ�
 * @param[in] priolity		�N������^�X�N�ɗ^�������(�D��x�t���^�C���V�F�A�����O�X�P�W���[�����O���ɗL��)
 * @return					�V�X�e���R�[���̐��ۏ��
 * @author					Kazuya Fukuhara
 * @date					2010/09/11 11:01:20	�쐬
 */
svcresultid_t API_hookInterrupt(extintid_t interrupt_id, taskid_t task_id
#if (SCHEDULER_TYPE == SCHEDULING_PRIORITY_BASE_TIME_SHARING) 
, priolity_t priolity
#endif
) {
	PAR_hookInterrupt par;
	par.syscall.id		= SVCID_hookInterrupt;
	par.interrupt_id	= interrupt_id;
	par.task_id			= task_id;
#if (SCHEDULER_TYPE == SCHEDULING_PRIORITY_BASE_TIME_SHARING) 
	par.priolity        = priolity;
#endif
	syscall( (ptr_t)&par );
	return par.syscall.result;
}


#if (SCHEDULER_TYPE == SCHEDULING_PRIORITY_BASE_TIME_SHARING) 
/**
 * @brief	setPriplity ���Ăяo���A�_�v�^�֐�
 * @param[in] priolity	�ύX��̗D�揇��
 * @return	�V�X�e���R�[���̐��ۏ��
 * @author	Kazuya Fukuhara
 * @date	2010/09/10 10:57:13	�쐬
 * @note	�X�P�W���[�����O�A���S���Y�����D��x�t���^�C���V�F�A�����O�̏ꍇ�Ɏg�p�\
 */
svcresultid_t API_setPriolity(priolity_t priolity) {
	PAR_setPriolity par;
	par.syscall.id = SVCID_setPriolity;
	par.priolity = priolity;
	syscall( (ptr_t)&par );
	return par.syscall.result;
}


/**
 * @brief	getPriplity ���Ăяo���A�_�v�^�֐�
 * @param[out] priolity	�D�揇�ʂ̊i�[��
 * @return	�V�X�e���R�[���̐��ۏ��
 * @author	Kazuya Fukuhara
 * @date	2010/09/10 10:57:13	�쐬
 * @note	�X�P�W���[�����O�A���S���Y�����D��x�t���^�C���V�F�A�����O�̏ꍇ�Ɏg�p�\
 */
svcresultid_t API_getPriolity(priolity_t *priolity) {
	PAR_setPriolity par;
	par.syscall.id = SVCID_getPriolity;
	syscall( (ptr_t)&par );
	*priolity = par.priolity;
	return par.syscall.result;
}
#endif
