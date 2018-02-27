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
 * @brief	M16C T-Bird���ł̃R���e�L�X�g�ƃR���e�L�X�g���� 
 * @author	Kazuya Fukuhara
 * @date	2010/09/04 19:46:15	�쐬
 */

#ifndef __CONTEXT__
#define __CONTEXT__

#include "./arch.h"
#include "../../src/type.h"


#ifdef UK_DEPRECATED
/**
 * @def		NAKED
 * @brief	�֐��̃v�����[�O�ƃG�s���[�O�����Ȃ��悤�ɂ���֐��C���q
 * @author	Kazuya Fukuhara
 * @date	2010/09/04 19:46:15	�쐬
 * @warning	�p�~�\��
 */
#define NAKED

/**
 * @def		NAKED_ISR
 * @brief	���荞�ݏ����֐����v�����[�O�ƃG�s���[�O�����Ȃ��悤�ɂ���֐��C���q
 * @author	Kazuya Fukuhara
 * @date	2010/09/04 19:46:15	�쐬
 * @warning	�p�~�\��
 */
#define NAKED_ISR
#endif

/**
 * @typedef	union flags_t flags_t
 * @union	flags_t
 * @brief	�t���O���W�X�^
 * @author	Kazuya Fukuhara
 * @date	2010/09/04 19:46:15	�쐬
 */
typedef union flags_t {
	struct {
		uint8_t C:1;		/**< 0x01 : �L�����[�t���O */
		uint8_t D:1;		/**< 0x02 : �f�o�b�O�t���O */
		uint8_t Z:1;		/**< 0x04 : �[���t���O */
		uint8_t S:1;		/**< 0x08 : �T�C���t���O */
		uint8_t B:1;		/**< 0x10 : ���W�X�^�o���N�w��t���O */
		uint8_t O:1;		/**< 0x20 : �I�[�o�t���[�t���O */
		uint8_t I:1;		/**< 0x40 : ���荞�݋��t���O */
		uint8_t U:1;		/**< 0x80 : �X�^�b�N�|�C���^�w��t���O */
	} Bits;
	uint8_t Value;
} flags_t;

/**
 * @typedef	struct context_t
 * @struct	context_t
 * @brief	�R���e�L�X�g
 * @note	R���W�X�^����A���W�X�^����z��ɏ���������\��
 * @author	Kazuya Fukuhara
 * @date	2010/09/04 19:46:15	�쐬
 */
typedef struct context_t {
	uint16_t	R0;
	uint16_t	R1;
	uint16_t	R2;
	uint16_t	R3;
	uint16_t	A0;
	uint16_t	A1;
	uint16_t	SB;
	uint16_t	FB;
	uint8_t		PC_L;		/**< �o�b�̍ŉ��ʃo�C�g */
	uint8_t		PC_M;		/**< �o�b�̒��ʃo�C�g */
	flags_t		FLG;		/**< �t���O���W�X�^ */
	uint8_t		PC_H;		/**< �o�b�̍ŏ�ʃo�C�g */
	uint8_t		HOOK[3];	/**< [EX] SVC_exit�ւ̃A�h���X (return�΍�) */
} context_t;

/**
 * @def		SET_KERNEL_STACKPOINTER
 * @brief	�J�[�l���X�^�b�N�����݂̃X�^�b�N�|�C���^�ɐݒ肷��
 * @author	Kazuya Fukuhara
 * @date	2010/09/04 19:46:15	�쐬
 */
#define SET_KERNEL_STACKPOINTER()		\
asm(									\
	".GLB	_kernel_stack_ptr	\r\n"	\
	"ldc _kernel_stack_ptr, ISP	\r\n"	\
)

/**
 * @def			SAVE_CONTEXT
 * @brief		���݂̃R���e�L�X�g��Ҕ�
 * @attention	�R���e�L�X�g�̓X�^�b�N�ɑҔ�����A�X�^�b�N�|�C���^�� currentTCB->stack_pointer �Ɋi�[�����B���̂��߁AcurrentTCB�ɓK�؂Ȓl���ݒ�ς݂ł��邱�Ƃ����߂���B
 * @author		Kazuya Fukuhara
 * @date		2010/09/04 19:46:15	�쐬
 */
#define SAVE_CONTEXT()									\
asm (													\
	/* �ėp���W�X�^ R0�`R3 */							\
	/* �A�h���X���W�X�^ A0�`A1�ASB, FB ��Ҕ� */		\
	/* �X�^�b�N�|�C���^ ISP , USP �͑ޔ����Ȃ��Ă悢 */	\
	"pushm	R0, R1, R2, R3, A0, A1, SB, FB		\r\n"	\
														\
	/* a0 = &(currentTCB->stack_pointer) */				\
	/* stack_pointer�̓I�t�Z�b�g��0�Ȃ̂� */			\
	/* �擪�Ԓn�����̂܂܎g���� */						\
	".GLB	_currentTCB							\r\n"	\
	"mov.w	_currentTCB,	a0					\r\n"	\
														\
	/* ISP��TCB.stackpointer�ɕۑ����� */				\
	"stc		ISP,	[a0]					\r\n"	\
);

/**
 * @def			RESTORE_CONTEXT
 * @brief		�R���e�L�X�g�𕜋A����
 * @attention	���A�Ɏg���X�^�b�N�|�C���^�� currentTCB->stack_pointer ����ǂݏo�����B���̂��߁AcurrentTCB�ɓK�؂Ȓl���ݒ�ς݂ł��邱�Ƃ����߂���B
 * @author		Kazuya Fukuhara
 * @date		2010/09/04 19:46:15	�쐬
 */
#define RESTORE_CONTEXT()							\
asm(												\
	/* ISP = currentTCB->stack_pointer */			\
	/* a0 = currentTCB */							\
	"mov.w	_currentTCB,	a0				\r\n"	\
	/* stack_pointer�̃I�t�Z�b�g��0�Ȃ̂� */		\
	"ldc		[A0], ISP					\r\n"	\
	"popm	R0, R1, R2, R3, A0, A1, SB, FB	\r\n"	\
);

/**
 * @def		RETURN_FROM_INTERRUPT
 * @brief	���荞�ݏ�������E�o����
 * @author	Kazuya Fukuhara
 * @date	2010/09/04 19:46:15	�쐬
 */
#define RETURN_FROM_INTERRUPT() asm( "reit" )

/**
 * @def   		GetContext
 * @brief		�^�X�N�R���g���[���u���b�N������s�R���e�L�X�g���擾
 * @param[in]	tcb �Ώۃ^�X�N�R���g���[���u���b�N
 * @return		���s�R���e�L�X�g�������|�C���^�l
 * @author		Kazuya Fukuhara
 * @date		2010/09/04 19:46:15	�쐬
 */
#define GetContext(tcb) ((context_t*)((tcb)->stack_pointer))

/**
 * @def			GetArgWord
 * @brief		�R���e�L�X�g����|�C���^���������o��
 * @param[in]	context �ΏۃR���e�L�X�g
 * @return		�����Ƃ��ēn���ꂽ�|�C���^�l
 * @author		Kazuya Fukuhara
 * @date		2010/09/04 19:46:15	�쐬
 */
#define GetArgPtr(context) ((context)->R0)

/**
 * @def			SetTaskArg
 * @brief		�R���e�L�X�g�Ƀ^�X�N�J�n���̈�����ݒ�
 * @param[in]	context �ΏۃR���e�L�X�g
 * @param[in]	arg     �ݒ肷�����
 * @author		Kazuya Fukuhara
 * @date		2010/09/04 19:46:15	�쐬
 */
#define SetTaskArg(context, arg) { (context)->R0 = (uint16_t)(arg); }

#endif
