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
 * @brief	Win32���ł̃R���e�L�X�g�ƃR���e�L�X�g����
 * @author	Kazuya Fukuhara
 * @date	2010/09/01 21:36:49	�쐬
 */

#ifndef __CONTEXT_H__
#define __CONTEXT_H__

#include "./arch.h"
#include "../../src/type.h"

/**
 * @def		NAKED
 * @brief	�֐��̃v�����[�O�ƃG�s���[�O�����Ȃ��悤�ɂ���֐��C���q
 * @author	Kazuya Fukuhara
 * @date	2010/09/01 21:36:49	�쐬
 */
#define NAKED __declspec(naked)

/**
 * @def		SET_KERNEL_STACKPOINTER
 * @brief	�J�[�l���X�^�b�N�����݂̃X�^�b�N�|�C���^�ɐݒ肷��
 * @author	Kazuya Fukuhara
 * @date	2010/09/01 21:36:49	�쐬
 */
#define SET_KERNEL_STACKPOINTER() \
	__asm{	mov esp, DWORD PTR [kernel_stack_ptr]	}\
	__asm{	mov ebp, esp							}

/**
 * @def			SAVE_CONTEXT
 * @brief		���݂̃R���e�L�X�g��Ҕ�
 * @attention	�R���e�L�X�g�̓X�^�b�N�ɑҔ�����A�X�^�b�N�|�C���^�� currentTCB->stack_pointer �Ɋi�[�����B���̂��߁AcurrentTCB�ɓK�؂Ȓl���ݒ�ς݂ł��邱�Ƃ����߂���B
 * @author		Kazuya Fukuhara
 * @date		2010/09/01 21:36:49	�쐬
 * @note        ���������_�����W�X�^�� MMX ���W�X�^���̑ޔ��͍s���Ă��Ȃ��B
 */
#define SAVE_CONTEXT() {					\
	__asm{	push esi						}\
	__asm{	push edi						}\
	__asm{	push ebp						}\
	__asm{	push edx						}\
	__asm{	push ecx						}\
	__asm{	push ebx						}\
	__asm{	push eax						}\
	__asm{	pushfd							}\
	__asm{	mov	eax, [currentTCB]			}\
	__asm{	mov DWORD PTR [eax+0x00], esp	}\
}

/**
 * @def			RESTORE_CONTEXT
 * @brief		�R���e�L�X�g�𕜋A����
 * @attention	���A�Ɏg���X�^�b�N�|�C���^�� currentTCB->stack_pointer ����ǂݏo�����B���̂��߁AcurrentTCB�ɓK�؂Ȓl���ݒ�ς݂ł��邱�Ƃ����߂���B
 * @author		Kazuya Fukuhara
 * @date		2010/09/01 21:36:49	�쐬
 * @note        ���������_�����W�X�^�� MMX ���W�X�^���̕��A�͍s���Ă��Ȃ��B
 */
#define RESTORE_CONTEXT() {					\
	__asm{	mov	eax, [currentTCB]			}\
	__asm{	mov esp, DWORD PTR [eax+0x00]	}\
	__asm{	popfd							}\
	__asm{	pop eax							}\
	__asm{	pop ebx							}\
	__asm{	pop ecx							}\
	__asm{	pop edx							}\
	__asm{	pop ebp							}\
	__asm{	pop edi							}\
	__asm{	pop esi							}\
}

/**
 * @def		RETURN_FROM_INTERRUPT
 * @brief	���荞�ݏ�������E�o����
 * @author	Kazuya Fukuhara
 * @date	2010/09/01 21:36:49	�쐬
 */
#define RETURN_FROM_INTERRUPT() {	\
	__asm { push eax }				\
	enableInterrupt();				\
	__asm { pop eax }				\
	__asm { ret };					\
}

/**
 * @brief	__stacl
 * @brief	�J�[�l���X�^�b�N�ւ̃|�C���^
 * @author	Kazuya Fukuhara
 * @date	2010/09/01 21:36:49	�쐬
 */
const extern void* kernel_stack_ptr;

/**
 * @typedef	register_t
 * @brief	32bit���W�X�^�������^
 * @author	Kazuya Fukuhara
 * @date	2010/09/01 21:36:49	�쐬
 */
typedef unsigned long register_t;

/**
 * @typedef	struct context_t context_t;
 * @struct	context_t
 * @brief	���s�R���e�L�X�g
 * @author	Kazuya Fukuhara
 * @date	2010/09/01 21:36:49	�쐬
 */
typedef struct context_t {
	register_t flag;
	register_t eax;
	register_t ebx;
	register_t ecx;
	register_t edx;
	register_t ebp;
	register_t edi;
	register_t esi;
	register_t ret;
} context_t;

/**
 * @typedef	struct context_init_t context_init_t;
 * @struct	context_init_t
 * @brief	�^�X�N�̏����ݒ莞�Ɏg�p����R���e�L�X�g
 * @author	Kazuya Fukuhara
 * @date	2010/09/01 21:36:49	�쐬
 * @par		�R���e�L�X�g�ڍ�
 * @htmlonly
<table border="1">
<tr><td><code>+0x00</code></td><td><code>flag</code></td><td rowspan="9"><code>context_t</code></td></tr>
<tr><td><code>+0x04</code></td><td><code>eax</code></td></tr>
<tr><td><code>+0x08</code></td><td><code>ebx</code></td></tr>
<tr><td><code>+0x0C</code></td><td><code>ecx</code></td></tr>
<tr><td><code>+0x10</code></td><td><code>edx</code></td></tr>
<tr><td><code>+0x14</code></td><td><code>ebp</code></td></tr>
<tr><td><code>+0x18</code></td><td><code>edi</code></td></tr>
<tr><td><code>+0x1C</code></td><td><code>esi</code></td></tr>
<tr><td><code>+0x20</code></td><td><code>ret</code></td></tr>
<tr><td><code>+0x24</code></td><td><code>exit_proc</code></td><td><code>�^�X�N�I�����ɌĂяo�����A�h���X</code></td></tr>
<tr><td><code>+0x28</code></td><td><code>arg</code></td><td><code>�^�X�N�J�n���̈���</code></td></tr>
</table>
 * @endhtmlonly
 */
typedef struct context_init_t {
	context_t context;
	register_t exit_proc;
	register_t arg;
} context_init_t;

/**
 * @def			SetReturnAddressToContext
 * @brief		�R���e�L�X�g�̖߂��A�h���X��ݒ肷��
 * @param[in]	context �ΏۃR���e�L�X�g
 * @param[in]	address �ݒ肷��߂��A�h���X
 * @author		Kazuya Fukuhara
 * @date		2010/09/01 21:36:49	�쐬
 */
#define SetReturnAddressToContext(context, address) {	\
	(context)->ret = (register_t)(address);				\
}

/**
 * @def   		GetContext
 * @brief		�^�X�N�R���g���[���u���b�N������s�R���e�L�X�g���擾
 * @param[in]	tcb �Ώۃ^�X�N�R���g���[���u���b�N
 * @return		���s�R���e�L�X�g�������|�C���^�l
 * @author		Kazuya Fukuhara
 * @date		2010/09/01 21:36:49	�쐬
 */
#define GetContext(tcb) ((context_t*)((tcb)->stack_pointer))

/**
 * @def			GetArgWord
 * @brief		�R���e�L�X�g����|�C���^���������o��
 * @param[in]	context �ΏۃR���e�L�X�g
 * @return		�����Ƃ��ēn���ꂽ�|�C���^�l
 * @author		Kazuya Fukuhara
 * @date		2010/09/01 21:36:49	�쐬
 */
#define GetArgPtr(context) (ptr_t)(((uint32_t*)(((uint8_t*)(context))+sizeof(context_t)))[0])

/**
 * @def			SetTaskArg
 * @brief		�R���e�L�X�g�Ƀ^�X�N�J�n���̈�����ݒ�
 * @param[in]	context �ΏۃR���e�L�X�g
 * @param[in]	arg     �ݒ肷�����
 * @author		Kazuya Fukuhara
 * @date		2010/09/01 21:36:49	�쐬
 */
#define SetTaskArg(context, argument) {\
	context_init_t* context_init = (context_init_t*)(context);\
	context_init->arg = (register_t)(argument);\
}

#endif
