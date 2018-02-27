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
 * @brief	�V�X�e���R�[��
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	�쐬
 * @date	2010/08/15 16:11:02	�@��ˑ������𕪗�
 * @date	2010/09/10 10:57:13	�D��x�t���^�C���V�F�A�����O�X�P�W���[�����O�p�̃V�X�e���R�[����ǉ�
 * @date	2010/09/11 11:01:20	�O�����荞�݃t�b�N�p�̃V�X�e���R�[����ǉ�
 */

#ifndef __syscall_h__
#define __syscall_h__

#include "type.h"

/**
 * @enum	svcresultid_t
 * @brief	�V�X�e���R�[���̌Ăяo������
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	�쐬
 */
typedef enum svcresultid_t {
	SUCCESS	= 0,	/**< ����I�� */
	ERR1	= 1,	/**< �^�X�N�h�c���s��  */
	ERR2	= 2,	/**< DORMANT��ԂłȂ��^�X�N���N������ */
	ERR3	= 3,	/**< [�\��] */
	ERR4	= 4,	/**< [�\��] */
	ERR5	= 5,	/**< wait ��ԂłȂ��^�X�N�� resume �����悤�Ƃ��� */
	ERR6	= 6,	/**< Pause�w�莞�Ԃ��s��(0) */
	ERR7	= 7,	/**< [�\��] */
	ERR8	= 8,	/**< [�\��] */
	ERR9	= 9,	/**< [�\��] */
	ERR10	= 10,	/**< �Z�}�t�H�h�c���s��  */
	ERR11	= 11,	/**< �w�肵���Z�}�t�H���l���ł��Ȃ� */
	ERR12	= 12,	/**< �w�肵���Z�}�t�H���J���ł��Ȃ� */
	ERR13	= 13,	/**< [�\��] */
	ERR14	= 14,	/**< [�\��] */
	ERR15	= 15,	/**< [�\��] */
	ERR16	= 16,	/**< [�\��] */
	ERR17	= 17,	/**< [�\��] */
	ERR18	= 18,	/**< [�\��] */
	ERR19	= 19,	/**< [�\��] */
	ERR20	= 20,	/**< ���^�X�N�� reset ���悤�Ƃ��� */
	ERR21	= 21,	/**< [�\��] */
	ERR22	= 22,	/**< [�\��] */
	ERR23	= 23,	/**< [�\��] */
	ERR24	= 24,	/**< [�\��] */
	ERR25	= 25,	/**< [�\��] */
	ERR26	= 26,	/**< [�\��] */
	ERR27	= 27,	/**< �s���Ȋ����ݔԍ�  */
	ERR28	= 28,	/**< [�\��] */
	ERR29	= 29,	/**< [�\��] */
	ERR30	= 30,	/**< �s���ȃV�X�e���R�[���Ăяo�� */
	ERR31	= 31,	/**< INIT/DIAG����͌Ăяo���Ȃ��V�X�e���R�[�����Ăяo���� */
	ERR40	= 40,	/**< ���[�����M���s */
	ERR41	= 41,	/**< ���[���{�b�N�X�͋� */
	ERR42	= 42,	/**< ���[���擾���s */
} svcresultid_t;

/**
 * @brief			�V�X�e���R�[���Ăяo������
 * @note			�@��ˑ��R�[�h���܂܂��
 * @author			Kazuya Fukuhara
 * @date			2010/01/07 14:08:58	�쐬
 * @date			2010/08/15 16:11:02	�@��ˑ������𕪗�
 */
extern void syscall_entry(void);

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
);

/**
 * @brief		���ݎ��s���̃^�X�N���I������B
 * @return		�V�X�e���R�[���̐��ۏ��
 * @note		�^�X�N�̃��Z�b�g�������s����B
 * @author		Kazuya Fukuhara
 * @date		2010/01/07 14:08:58	�쐬
 */
extern svcresultid_t API_exitTASK(void);

/**
 * @brief			���ݎ��s���̃^�X�N���x����Ԃɂ���
 * @param[in] count	�x����Ԃɂ��鎞�ԁitick�P�ʁj
 * @return			�V�X�e���R�[���̐��ۏ��
 * @note            count��0�ɐݒ肷��ƁAresumeTASK�V�X�e���R�[���ōĊJ������܂Ŗ����ɋx������B
 * @note            �����ԋx��(count > 255)�ɂ͊��荞�݂ȂǕʂ̗v�f���g�����ƁB
 * @author          Kazuya Fukuhara
 * @date            2010/01/07 14:08:58	�쐬
 */
extern svcresultid_t API_pauseTASK(tick_t count);

/**
 * @brief				�w�肵���x����Ԃ̃^�X�N���ĊJ���ɂ���
 * @param[in] taskId	�ĊJ����^�X�N�̃^�X�N�h�c
 * @return				�V�X�e���R�[���̐��ۏ��
 * @author				Kazuya Fukuhara
 * @date				2010/01/07 14:08:58	�쐬
 */
extern svcresultid_t API_resumeTASK(taskid_t taskId);

/**
 * @brief			���^�X�N�����X�^�[�g����
 * @param[in] count	�ĊJ����܂ł̎��ԁitick�P�ʁj
 * @return			�V�X�e���R�[���̐��ۏ��
 * @note            count��0�ɐݒ肷��ƁAresumeTASK�V�X�e���R�[���ōĊJ������܂Ŗ����ɋx������B
 * @note            �����ԋx��(count > 255)�ɂ͊��荞�݂ȂǕʂ̗v�f���g�����ƁB
 * @author          Kazuya Fukuhara
 * @date            2010/01/07 14:08:58	�쐬
 */
extern svcresultid_t API_restartTASK(tick_t count);

/**
 * @brief				���^�X�N�̃^�X�N�h�c���擾����
 * @param[out] pTaskID	���^�X�N�̃^�X�N�h�c���i�[�����̈�
 * @return				�V�X�e���R�[���̐��ۏ��
 * @author				Kazuya Fukuhara
 * @date				2010/01/07 14:08:58	�쐬
 */
extern svcresultid_t API_getTID(taskid_t *pTaskID);

/**
 * @brief			takeSEMA���Ăяo���A�_�v�^�֐�
 * @param[in] sid	�l������Z�}�t�H�̂h�c
 * @return			�V�X�e���R�[���̐��ۏ��
 * @author			Kazuya Fukuhara
 * @date			2010/01/07 14:08:58	�쐬
 */
extern svcresultid_t API_takeSEMA(semaphoid_t sid);

/**
 * @brief			giveSEMA���Ăяo���A�_�v�^�֐�
 * @param[in] sid	��������Z�}�t�H�̂h�c
 * @return			�V�X�e���R�[���̐��ۏ��
 * @author			Kazuya Fukuhara
 * @date			2010/01/07 14:08:58	�쐬
 */
extern svcresultid_t API_giveSEMA(semaphoid_t sid);

/**
 * @brief			tasSEMA���Ăяo���A�_�v�^�֐�
 * @param[in] sid	�l�������݂�Z�}�t�H�̂h�c
 * @return			�V�X�e���R�[���̐��ۏ��
 * @author			Kazuya Fukuhara
 * @date			2010/01/07 14:08:58	�쐬
 */
extern svcresultid_t API_tasSEMA(semaphoid_t sid);

/**
 * @brief			recvMSG���Ăяo���A�_�v�^�֐�
 * @param[out] from	���b�Z�[�W�̑��M�^�X�NID���i�[����̈�̃|�C���^
 * @param[out] data	���b�Z�[�W���i�[����̈�̃|�C���^
 * @return			�V�X�e���R�[���̐��ۏ��
 * @author			Kazuya Fukuhara
 * @date			2010/01/07 14:08:58	�쐬
 */
extern svcresultid_t API_recvMSG(taskid_t* from, ptr_t* data);

/**
 * @brief			sendMSG���Ăяo���A�_�v�^�֐�
 * @param[in] to	���M��̃^�X�N�̂h�c
 * @param[in] data	���M���郁�b�Z�[�W�̖{��
 * @return			�V�X�e���R�[���̐��ۏ��
 * @author			Kazuya Fukuhara
 * @date			2010/01/07 14:08:58	�쐬
 */
extern svcresultid_t API_sendMSG(taskid_t to, ptr_t data);

/**
 * @brief		waitMSG���Ăяo���A�_�v�^�֐�
 * @return		�V�X�e���R�[���̐��ۏ��
 * @author		Kazuya Fukuhara
 * @date		2010/01/07 14:08:58	�쐬
 */
extern svcresultid_t API_waitMSG(void);

/**
 * @brief					hookInterrupt���Ăяo���A�_�v�^�֐�
 * @param[in] interrupt_id	�t�b�N����O�����荞�ݔԍ�
 * @param[in] task_id		���荞�݂ŋN������^�X�N�̔ԍ�
 * @param[in] priolity		�N������^�X�N�ɗ^�������(�D��x�t���^�C���V�F�A�����O�X�P�W���[�����O���ɗL��)
 * @return					�V�X�e���R�[���̐��ۏ��
 * @author					Kazuya Fukuhara
 * @date					2010/09/11 11:01:20	�쐬
 */
extern svcresultid_t API_hookInterrupt(extintid_t interrupt_id, taskid_t task_id
#if (SCHEDULER_TYPE == SCHEDULING_PRIORITY_BASE_TIME_SHARING) 
, priolity_t priolity
#endif
);

#if (SCHEDULER_TYPE == SCHEDULING_PRIORITY_BASE_TIME_SHARING) 
/**
 * @brief	setPriplity ���Ăяo���A�_�v�^�֐�
 * @param[in] priolity	�ύX��̗D�揇��
 * @return	�V�X�e���R�[���̐��ۏ��
 * @author	Kazuya Fukuhara
 * @date	2010/09/10 10:57:13	�쐬
 * @note	�X�P�W���[�����O�A���S���Y�����D��x�t���^�C���V�F�A�����O�̏ꍇ�Ɏg�p�\
 */
extern svcresultid_t API_setPriolity(priolity_t priolity);

/**
 * @brief	getPriplity ���Ăяo���A�_�v�^�֐�
 * @param[out] priolity	�D�揇�ʂ̊i�[��
 * @return	�V�X�e���R�[���̐��ۏ��
 * @author	Kazuya Fukuhara
 * @date	2010/09/10 10:57:13	�쐬
 * @note	�X�P�W���[�����O�A���S���Y�����D��x�t���^�C���V�F�A�����O�̏ꍇ�Ɏg�p�\
 */
extern svcresultid_t API_getPriolity(priolity_t *priolity);
#endif

#endif
