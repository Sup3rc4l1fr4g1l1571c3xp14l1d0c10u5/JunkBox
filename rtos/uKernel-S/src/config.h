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
 * @brief   �J�[�l���ݒ�
 * @note    �J�[�l���{�̂̕ύX�\�Ȑݒ蕔���͂��ׂĂ����ɋL�q����
 * @author  Kazuya Fukuhara
 * @date    2010/01/07 14:08:58	�쐬
 * @date	2010/08/15 16:11:02	�@��ˑ������𕪗�
 * @date	2010/09/01 21:36:49	WIN32�A�[�L�e�N�`����ǉ�
 * @date	2010/09/04 19:46:15	M16C�}�C�R������T-Bird�{�[�h�A�[�L�e�N�`����ǉ�
 * @date	2010/09/09 12:48:22	�^�C���V�F�A�����O�X�P�W���[�����O�ǉ�
 * @date	2010/09/10 10:57:13	�D��x�t���^�C���V�F�A�����O�X�P�W���[�����O��ǉ�
 */

#ifndef __config_h__
#define __config_h__

/* �A�[�L�e�N�`�� */

/**
 * @def		ARCH_DUMMY
 * @brief	�_�~�[�^�[�Q�b�g
 * @author  Kazuya Fukuhara
 * @date	2010/08/15 16:11:02	�쐬
 */
#define ARCH_DUMMY  0x00

/**
 * @def		ARCH_AVR
 * @brief	AVR�}�C�R����ΏۂƂ���
 * @author  Kazuya Fukuhara
 * @date	2010/08/15 16:11:02	�쐬
 */
#define ARCH_AVR    0x01

/**
 * @def		ARCH_WIN32
 * @brief	WIN32�A�v���P�[�V������ΏۂƂ���
 * @author  Kazuya Fukuhara
 * @date	2010/09/01 21:36:49	�쐬
 */
#define ARCH_WIN32  0x02

/**
 * @def		ARCH_M16C_TBIRD
 * @brief	M16C�}�C�R������T-Bird�{�[�h��ΏۂƂ���
 * @author  Kazuya Fukuhara
 * @date	2010/09/04 19:46:15	�쐬
 */
#define ARCH_M16C_TBIRD  0x03

/**
 * @def		ARCH_LPC1343
 * @brief	LPC1343(LPCXpresso1343)��ΏۂƂ���
 * @author  Kazuya Fukuhara
 * @date	2010/09/04 19:46:15	�쐬
 */
#define ARCH_LPC1343  0x04

/**
 * @def		ARCH_RX63N
 * @brief	RX63N(GR-SAKURA)��ΏۂƂ���
 * @author  Kazuya Fukuhara
 * @date	2010/09/04 19:46:15	�쐬
 */
#define ARCH_RX63N  0x05

/**
 * @def		TARGET_ARCH
 * @brief	�ΏۂƂ���A�[�L�e�N�`���̎w��
 * @author  Kazuya Fukuhara
 * @date	2010/08/15 16:11:02	�쐬
 *
 * �Ή��A�[�L�e�N�`���͈ȉ��̒ʂ�
 * - ARCH_AVR        : Atmel AVR (ATMega328P)
 * - ARCH_WIN32      : Windows��̃A�v�� (�V�~�����[�V����)
 * - ARCH_M16C_TBIRD : ����p�}�C�R���{�[�hT-Bird(M16C62A)
 * - ARCH_LPC1343    : LPCXpresso1343 (CortexM3 LPC1343 �]���{�[�h)
 * - ARCH_RX63N      : RX63N (GR-SAKURA�{�[�h)
 *
 */
#define TARGET_ARCH ARCH_WIN32

/**
 * @def     TASK_NUM
 * @brief   �^�X�N����
 * @author  Kazuya Fukuhara
 * @note    �R���p�C�����Ƀ^�X�N�������肷��B
 * @note    �^�X�N���𑝂₷�ꍇ�͂����ő��₷�B
 * @date    2010/01/07 14:08:58	�쐬
 */
#define TASK_NUM (3+2)

/**
 * @def     TASK_STACK_SIZE
 * @brief   �^�X�N�X�^�b�N�̃T�C�Y
 * @note    ��̃^�X�N�Ɋ��蓖�Ă�����s���X�^�b�N�̃T�C�Y
 * @author  Kazuya Fukuhara
 * @date    2010/01/07 14:08:58	�쐬
 * @date	2010/09/01 21:36:49	WIN32�A�[�L�e�N�`���̒ǉ��ɂƂ��Ȃ��A�I�t�Z�b�g���w��\�ɕύX
 */
#define TASK_STACK_SIZE (1024+ARCH_TASK_STACK_OFFSET)

/**
 * @def     KERNEL_STACK_SIZE
 * @brief   �J�[�l���X�^�b�N�̃T�C�Y
 * @note    �J�[�l���Ɋ��蓖�Ă�����s���X�^�b�N�̃T�C�Y
 * @author  Kazuya Fukuhara
 * @date    2010/01/07 14:08:58	�쐬
 * @date	2010/09/01 21:36:49	WIN32�A�[�L�e�N�`���̒ǉ��ɂƂ��Ȃ��A�I�t�Z�b�g���w��\�ɕύX
 */
#define KERNEL_STACK_SIZE	(64+ARCH_TASK_STACK_OFFSET)

/**
 * @def     SEMAPHO_NUM
 * @brief   �J�[�l���S�̂Ŏg����Z�}�t�H�̑���
 * @author  Kazuya Fukuhara
 * @date    2010/01/07 14:08:58	�쐬
 */
#define SEMAPHO_NUM (64)

/**
 * @def     MESSAGE_NUM
 * @brief   �J�[�l���S�̂Ŏg���郁�b�Z�[�W�̑���
 * @author  Kazuya Fukuhara
 * @date    2010/01/07 14:08:58	�쐬
 */
#define MESSAGE_NUM (16)

/**
 * @def     SCHEDULING_PRIORITY_BASE
 * @brief   �Œ�D��x�X�P�W���[�����O
 * @author  Kazuya Fukuhara
 * @date	2010/09/09 12:48:22	�쐬
 *
 * �D��x�̍����^�X�N�����s���𓾂Ă���ꍇ�A���̃^�X�N���҂���ԂɂȂ邩�A
 * ��荂���D��x���������^�X�N�����s�\��ԂɂȂ�Ȃ�����A���s����������Ȃ��B
 */
#define SCHEDULING_PRIORITY_BASE 0

/**
 * @def     SCHEDULING_TIME_SHARING
 * @brief   �D��x�Ȃ��^�C���V�F�A�����O�X�P�W���[�����O
 * @author  Kazuya Fukuhara
 * @date	2010/09/09 12:48:22	�쐬
 *
 * ���s���𓾂��^�X�N�ɂ͈���CPU���Ԃ����蓖�Ă���B
 * ���蓖�Ă�ꂽ���Ԃ��g���؂�ƃJ�[�l���������I�Ɏ��̃^�X�N�Ɏ��s����^����B
 * ���s����D��ꂽ�^�X�N�͑��̃^�X�N�̎��s���I�������ɁA�Ăю��s�������蓖�Ă���B
 */
#define SCHEDULING_TIME_SHARING 1

/**
 * @def     SCHEDULING_PRIORITY_BASE_TIME_SHARING
 * @brief   �D��x�t���^�C���V�F�A�����O�X�P�W���[�����O�i���E���h���r���j
 * @author  Kazuya Fukuhara
 * @date	2010/09/10 10:57:13	�쐬
 *
 * �^�X�N�̗D��x���ƂɃO���[�v�������B
 * ���s���𓾂��O���[�v�̃^�X�N�ɂ͏��ԂɈ���CPU���Ԃ����蓖�Ă���B
 * ���蓖�Ă�ꂽ���Ԃ��g���؂�ƃJ�[�l���������I�ɓ���D��x���̎��̃^�X�N�Ɏ��s����^����B
 * ���s����D��ꂽ�^�X�N�͓���D��x�̑��̃^�X�N�̎��s���I�������ɁA�Ăю��s�������蓖�Ă���B
 * ���D��x�̍����O���[�v�ɑ�����^�X�N�����s�\��ԂɂȂ����ꍇ�A���s�����ړ�����B
 */
#define SCHEDULING_PRIORITY_BASE_TIME_SHARING 2

/**
 * @def     SCHEDULER_TYPE
 * @brief   �X�P�W���[����I��
 * @author  Kazuya Fukuhara
 * @date	2010/09/09 12:48:22	�쐬
 */
#define SCHEDULER_TYPE SCHEDULING_PRIORITY_BASE

#if (SCHEDULER_TYPE == SCHEDULING_TIME_SHARING) || (SCHEDULER_TYPE == SCHEDULING_PRIORITY_BASE_TIME_SHARING) 
/**
 * @def		TIME_SHARING_THRESHOLD
 * @brief	�^�X�N�؂�ւ��𔭐�������CPU���Ԃ�臒l
 * @author	Kazuya Fukuhara
 * @date	2010/09/09 12:48:22	�쐬
 * @date	2010/09/10 10:57:13	�D��x�t���^�C���V�F�A�����O�X�P�W���[�����O�ł̗��p��ǉ�
 *
 * �^�C���V�F�A�����O�X�P�W���[�����O/�D��x�t���^�C���V�F�A�����O�X�P�W���[�����O�ŁA
 * �����I�ɃX�P�W���[�����O�𔭐������鎞�Ԃ�臒l
 */
#define TIME_SHARING_THRESHOLD 10
#endif

#endif
