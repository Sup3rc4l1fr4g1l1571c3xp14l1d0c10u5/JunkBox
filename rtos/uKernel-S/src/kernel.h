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
 */

#ifndef __kernel_h__
#define __kernel_h__

#include "./arch.h"
#include "./type.h"
#include "./config.h"

/**
 * @typedef	enum stateid_t stateid_t;
 * @enum	stateid_t
 * @brief	�^�X�N�̏�Ԃ������萔
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	�쐬
 * @par		�^�X�N�̏�Ԃ̑J�ڐ}:
 * @dot
 *   digraph state {
 *       node [ shape=box, fontname="�r�g �f30-M", fontsize=10];
 *       edge [ fontname="�r�g �f30-M", fontsize=10];
 *
 *       NON_EXISTS   [ label="NON_EXISTS" URL="\ref NON_EXISTS"];
 *       DORMANT      [ label="DORMANT" URL="\ref DORMANT"];
 *       READY        [ label="READY" URL="\ref READY"];
 *       PAUSE        [ label="PAUSE" URL="\ref PAUSE"];
 *       WAIT_MSG     [ label="WAIT_MSG" URL="\ref WAIT_MSG"];
 *       WAIT_SEMA    [ label="WAIT_SEMA" URL="\ref WAIT_SEMA"];
 *       WAIT_RESTART [ label="WAIT_RESTART" URL="\ref WAIT_RESTART"];
 *
 *       NON_EXISTS   -> DORMANT      [ arrowhead="open", style="solid", label="reset" ];
 *       DORMANT      -> READY        [ arrowhead="open", style="solid", label="start" ];
 *       READY        -> PAUSE        [ arrowhead="open", style="solid", label="pause" ];
 *       READY        -> DORMANT      [ arrowhead="open", style="solid", label="exit" ];
 *       PAUSE        -> READY        [ arrowhead="open", style="solid", label="resume" ];
 *       READY        -> WAIT_RESTART [ arrowhead="open", style="solid", label="restart" ];
 *       WAIT_RESTART -> READY        [ arrowhead="open", style="solid", label="" ];
 *       READY        -> WAIT_SEMA    [ arrowhead="open", style="solid", label="takeSema" ];
 *       WAIT_SEMA    -> READY        [ arrowhead="open", style="solid", label="giveSema" ];
 *       READY        -> WAIT_MSG     [ arrowhead="open", style="solid", label="waitMsg" ];
 *       WAIT_MSG     -> READY        [ arrowhead="open", style="solid", label="sendMsg" ];
 *
 *       { rank = same; NON_EXISTS }
 *       { rank = same; DORMANT }
 *       { rank = same; READY }
 *       { rank = same; PAUSE, WAIT_MSG, WAIT_SEMA, WAIT_RESTART }
 *   }
 * @enddot
 */
typedef enum stateid_t { 
	NON_EXISTS,		/**< �����������  */
	DORMANT,		/**< �x�~��ԁF���N���A���s�\ */ 
	READY, 			/**< ���s��ԁF�N���ς݁A���s�\ */
	PAUSE, 			/**< �҂���ԁF�N���ς݁A���s�s�\ */
	WAIT_MSG,		/**< ���b�Z�[�W�҂���ԁF�N���ς݁A���s�s�\ */
	WAIT_SEMA,		/**< �Z�}�t�H�҂���ԁF�N���ς݁A���s�s�\ */
	WAIT_RESTART,	/**< ���X�^�[�g�҂���ԁF�N���ς݁A���s�s�\ */
} stateid_t;

/**
 * @typedef	struct tcb_t tcb_t;
 * @struct	tcb_t
 * @brief	�^�X�N�R���g���[���u���b�N
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	�쐬
 * @date	2010/09/09 12:48:22	�^�C���V�F�A�����O�X�P�W���[�����O��ǉ�
 * @date	2010/09/10 10:57:13	�D��x�t���^�C���V�F�A�����O�X�P�W���[�����O��ǉ�
 */
typedef struct tcb_t {
	uint8_t*			stack_pointer;	/**< �X�^�b�N�|�C���^ */
	stateid_t			status;			/**< �^�X�N�̏�� */
	ptr_t				parameter;		/**< �^�X�N�̋N�������� */
	uint8_t				pause_count;	/**< �҂�����(0x00�͉i���ɂ��x��) */
	struct tcb_t*		next_task;		/**< �e��s��ł̃����N���X�g�p */
	struct message_t*	message;		/**< ��M�������b�Z�[�W�̃L���[ */
#if (SCHEDULER_TYPE == SCHEDULING_TIME_SHARING) || \
    (SCHEDULER_TYPE == SCHEDULING_PRIORITY_BASE_TIME_SHARING) 
	/**
	 * �^�C���V�F�A�����O�X�P�W���[�����O/�D��x�t���^�C���V�F�A�����O�X�P�W���[�����O
	 * �ɂ����āA�^�X�N�������CPU���Ԃ𐔂���B
	 * scheduler() �Ŏ��s�����ړ������ۂ� 臒l TIME_SHARING_THRESHOLD �ɏ���������A
	 * �J�[�l���^�C�}���荞�݂̃^�C�~���O�� -1 �����B
	 * 0�ɂȂ����ꍇ�́A�X�P�W���[�����O���s����^�C�~���O�ŋ����I�Ɏ��̃^�X�N�Ɏ��s�����ړ�����B
	 */
	uint8_t				time_sharing_tick;
#endif
#if (SCHEDULER_TYPE == SCHEDULING_PRIORITY_BASE_TIME_SHARING) 
	/**
	 * �D��x�t���^�C���V�F�A�����O�X�P�W���[�����O�ɂ�����^�X�N�̗D��x���i�[����
	 */
	uint8_t				task_priolity;
#endif
} tcb_t;

/**
 * @def		TCBtoTID
 * @brief	�^�X�N�R���g���[���u���b�N�̃A�h���X����^�X�N�h�c�𓾂�
 * @author	Kazuya Fukuhara
 * @warning	���l�T�X M3T-NC30WA �Ȃǂ͉����\�ȏꍇ�ł����Z���r�b�g�V�t�g�ɒu�����Ȃ��B
 *          ���̌��ʁA���Z�֐����Ă΂�A�X�^�b�N�����邱�Ƃ����邽�߁A�K�v�Ȃ�΃r�b�g�V�t�g���\�Ȍ`�ɏ��������Ă��悢�B
 * @date	2010/01/07 14:08:58	�쐬
 */
#define TCBtoTID(x) ((taskid_t)((x)-&tasks[0]))

/**
 * @brief 	�^�X�N�R���g���[���u���b�N�z��
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	�쐬
 */
extern tcb_t	tasks[TASK_NUM];

/**
 * @brief	�^�X�N�̎��s���X�^�b�N�̈�
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	�쐬
 */
extern uint8_t	task_stack[TASK_NUM][TASK_STACK_SIZE];

/**
 * @typedef	struct readyqueue_t readyqueue_t;
 * @struct	readyqueue_t
 * @brief	ready��Ԃ̃^�X�N����ׂ�L���[�^
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	�쐬
 */
typedef struct readyqueue_t {
	tcb_t*	next_task;		/**< �����N���X�g�p */
} readyqueue_t;

/**
 * @brief	ready��Ԃ̃^�X�N����ׂ�L���[
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	�쐬
 */
extern readyqueue_t readyqueue;

/**
 * @brief	�w�肵��tid�����[�U�[�^�X�N�������Ă��邩���肷��B
 * @author	Kazuya Fukuhara
 * @param   tid ���肷��^�X�N�h�c
 * @retval	TRUE  ���[�U�[�^�X�N�ł���
 * @retval	FALSE ���[�U�[�^�X�N�ł͂Ȃ�
 * @date	2010/12/03 20:31:26	�쐬
 *
 * ���[�U�[�^�X�N�Ƃ́AINIT/DIAG���������^�X�N�̂��ƁB
 */
extern bool_t isUserTask(taskid_t tid);

/**
 * @brief			�^�X�N��readyqueue�ɑ}������
 * @param[in] tcb	�}������^�X�N
 * @author			Kazuya Fukuhara
 * @date			2010/01/07 14:08:58	�쐬
 */
extern void addTaskToReadyQueue(tcb_t *tcb);

/**
 * @brief			�^�X�N��readyqueue�����菜��
 * @param[in] tcb	��菜���^�X�N
 * @author			Kazuya Fukuhara
 * @date			2010/01/07 14:08:58	�쐬
 */
extern bool_t removeTaskFromReadyQueue(tcb_t *tcb);

/**
 * @brief	�҂����Ԃ��L����wait��Ԃ̃^�X�N����ׂ�L���[
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	�쐬
 */
typedef struct {
	tcb_t*	next_task;		/**< �����N���X�g�p */
} waitqueue_t;

/**
 * @brief	wait��Ԃ̃^�X�N����ׂ�L���[
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	�쐬
 */
extern waitqueue_t waitqueue;

/**
 * @brief			tcb�Ŏ������^�X�N��҂��s�� WaitQueue �ɒǉ�����
 * @param[in] tcb	�ǉ�����^�X�N
 * @param[in] time	�҂�����
 * @retval FALSE	�^�X�N�� WaitQueue ���ɑ��݂��Ȃ������B
 * @retval TRUE		�^�X�N�� WaitQueue ���ɑ��݂����̂Ŏ�菜�����B
 * @note			tcb�̑Ó����͌������Ȃ��̂Œ���
 * @note			time �� 0 ��n�����ꍇ�́A�҂��s��ɂ͒ǉ�����Ȃ�
 * @author			Kazuya Fukuhara
 * @date			2010/01/07 14:08:58	�쐬
 */
extern void addTaskToWaitQueue(tcb_t* tcb, uint8_t time) ;

/**
 * @brief			tcb�Ŏ������^�X�N���҂��s�� WaitQueue ���ɂ���Ύ�菜���B
 * @param[in] tcb	��菜���^�X�N
 * @retval FALSE	�^�X�N�� WaitQueue ���ɑ��݂��Ȃ������B
 * @retval TRUE		�^�X�N�� WaitQueue ���ɑ��݂����̂Ŏ�菜�����B
 * @note			tcb�̑Ó����͌������Ȃ��̂Œ���
 * @author			Kazuya Fukuhara
 * @date			2010/01/07 14:08:58	�쐬
 */
extern bool_t removeTaskFromWaitQueue(tcb_t* tcb);

/**
 * @def		INITTASK
 * @brief	�������^�X�N
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	�쐬
 */
#define INITTASK (tasks[0])

/**
 * @def		DIAGTASK
 * @brief	Idle�^�X�N
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	�쐬
 */
#define DIAGTASK (tasks[TASK_NUM-1])

/**
 * @brief	���ݎ��s���̃^�X�N�̃^�X�N�R���g���[���u���b�N
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	�쐬
 */
extern tcb_t*	currentTCB;

/**
 * @brief	�X�P�W���[�����O�v���t���O
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	�쐬
 */
extern bool_t	rescheduling;

/**
 * @typedef	taskproc_t
 * @brief	�^�X�N�J�n�A�h���X�������^
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	�쐬
 */
typedef void (TASKPROC *taskproc_t)(ptr_t arg);

/**
 * @brief	�^�X�N�̊J�n�A�h���X���i�[����Ă���z��
 * @note	���[�U�[���Œ�`���Ȃ���΂Ȃ�Ȃ��B
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	�쐬
 */
extern const taskproc_t TaskTable[];

/**
 * @brief	�J�[�l�����N��
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	�쐬
 */
extern void startKERNEL(void);

/**
 * @brief			�^�X�N�R���g���[���u���b�N����^�X�NID�𓾂�
 * @param[in] tcb	ID�𓾂����^�X�N�̃^�X�N�R���g���[���u���b�N
 * @return			�^�X�NID;
 * @author			Kazuya Fukuhara
 * @date			2010/01/07 14:08:58	�쐬
 */
extern taskid_t getTaskID(tcb_t* tcb);

/**
 * @brief				�w�肵���^�X�N�R���g���[���u���b�N��������ԂɃ��Z�b�g����
 * @param[in] taskid	���Z�b�g�Ώۂ̃^�X�N�̃^�X�NID
 * @note				�w�肵���^�X�N�̃X�^�b�N�|�C���^���Ԃ��S�ă��Z�b�g����A�^�X�N�͏�����ԂƂȂ�B
 * @author				Kazuya Fukuhara
 * @date    			2010/01/07 14:08:58	�쐬
 * @date				2010/08/15 16:11:02	�@��ˑ������𕪗�
 */
extern void resetTCB(taskid_t taskid);

/**
 * @brief   �҂���Ԃ̃^�X�N�̑҂����Ԃ��X�V����
 * @author	Kazuya Fukuhara
 * @date    2010/01/07 14:08:58	�쐬
 */
extern void updateTick(void);

/**
 * @brief   �X�P�W���[���֐�
 * @note    �X�P�W���[�����O���s���AcurrentTCB�Ɏ��s���ׂ��^�X�N�̃^�X�N�R���g���[���u���b�N�̃|�C���^��ݒ肷��B
 * @author	Kazuya Fukuhara
 * @date    2010/01/07 14:08:58	�쐬
 */
extern void scheduler(void);

/**
 * @brief   �J�[�l���̃X�^�[�g�A�b�v�R�[�h
 * @author	Kazuya Fukuhara
 * @date    2010/01/07 14:08:58	�쐬
 * @date	2010/08/15 16:11:02	�@��ˑ������𕪗�
 */
extern void startKernel(void);

/**
 * @brief					�O�����荞�݃t�b�N�\�ɏ�����ݒ肷��
 * @param[in] interrupt_id	�t�b�N����O�����荞�ݔԍ�
 * @param[in] task_id		���荞�݂ŋN������^�X�N�̔ԍ�
 * @param[in] priolity		�N������^�X�N�ɗ^�������(�D��x�t���^�C���V�F�A�����O�X�P�W���[�����O���ɗL��)
 * @author					Kazuya Fukuhara
 * @date					2010/09/11 11:01:20	�쐬
 */
void set_int_hook_tid(extintid_t interrupt_id, taskid_t task_id
#if (SCHEDULER_TYPE == SCHEDULING_PRIORITY_BASE_TIME_SHARING) 
, priolity_t priolity
#endif
);

/**
 * @brief	�O�����荞�݂ɉ����ă^�X�N���N��
 * @author	Kazuya Fukuhara
 * @date	2010/09/11 11:01:20	�쐬
 */
extern void external_interrupt_handler(void);


/**
 * @brief   �w��̈���[���N���A
 * @author  Kazuya Fukuhara
 * @date    2010/01/07 14:08:58	�쐬
 */
extern void ZeroMemory(ptr_t buf, uint_t szbuf);

#endif
