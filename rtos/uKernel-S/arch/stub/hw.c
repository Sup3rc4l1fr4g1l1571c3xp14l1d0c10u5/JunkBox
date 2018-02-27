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
 * @brief	�n�[�h�E�F�A�ˑ��̃R�[�h
 * @author  
 * @date	20xx/xx/xx xx:xx:xx	�쐬
 *
 * �ڐA�Ώۋ@��ɉ������L�q���s�����ƁB
 */

#include "../../src/arch.h"
#include "../../src/kernel.h"
#include "../../src/syscall.h"

/**
 * @brief	�J�[�l���X�^�b�N�̈�
 * @author  
 * @date	20xx/xx/xx xx:xx:xx	�쐬
 */
uint8_t kernel_stack[KERNEL_STACK_SIZE];

/**
 * @brief	�n�[�h�E�F�A�S�ʂ̏���������
 * @author  
 * @date	20xx/xx/xx xx:xx:xx	�쐬
 * 
 * �J�[�l���^�C�}��\�t�g�E�F�A���荞�݂�L��������Ȃǂ̏��������s���B
 */
void initHardware(void) {
}

/**
 * @brief			�R���e�L�X�g�̏�����
 * @param[in] tid	�^�X�N�ԍ�
 * @author  
 * @date	20xx/xx/xx xx:xx:xx	�쐬
 * 
 * �w�肳�ꂽ�^�X�N�̃R���e�L�X�g�̏��������s���B��̓I�ɂ͈ȉ��̏������s��
 * @li �X�^�b�N�|�C���^�̏����l��ݒ�
 * @li �R���e�L�X�g�𔲂����ۂɎ����I��exit���Ă΂��悤�ɐݒ�
 * @li �R���e�L�X�g�̏����l�ݒ�
 * @li �J�n�A�h���X�l�̐ݒ�
 */
void resetContext(taskid_t tid) {
}

/**
 * @brief					�V�X�e���R�[���Ăяo��
 * @param[in,out] param		�Ăяo���V�X�e���R�[���̂h�c������̊i�[���ꂽ�\���̂ւ̃|�C���^
 * @author  
 * @date	20xx/xx/xx xx:xx:xx	�쐬
 * 
 * �\�t�g�E�F�A���荞�݁A�������͑����̏�����p���āA�V�X�e���R�[�����Ăяo���B
 */
svcresultid_t syscall(ptr_t param) {
}

/**
 * @brief	�\�t�g�E�F�A���荞�݂̕ߑ�
 * @author  
 * @date	20xx/xx/xx xx:xx:xx	�쐬
 * 
 * �Ăяo���V�X�e���R�[����ID�A�����ȂǂɃA�N�Z�X�ł���悤�ɃR���e�L�X�g��Ҕ��B
 * ���̌�A�J�[�l���X�^�b�N�֐؂�ւ��Ă��� syscall_entry() ���Ăяo���B
 */
void software_int(void) {
}

/**
 * @brief	�X�^�[�g�A�b�v���[�`��
 * @author  
 * @date	20xx/xx/xx xx:xx:xx	�쐬
 * 
 * �J�[�l���X�^�b�N�ւ̐؂�ւ����s���A�K�v�ȏ������s���Ă���AstartKernel()���Ăяo���B
 */
void main(void) {
}

/**
 * @brief			�t�b�N�\�ȊO�����荞�ݔԍ�������
 * @param[in] id	�O�����荞�ݔԍ�
 * @retval TRUE		�t�b�N�\�ȊO�����荞�ݔԍ�
 * @retval FALSE	�t�b�N�s�\�ȊO�����荞�ݔԍ�
 * @author	
 * @date			20xx/xx/xx xx:xx:xx	�쐬
 */
bool_t is_hookable_interrupt_id(extintid_t int_id) {
	/* �J�[�l���^�C�}��A�V�X�e���R�[���Ăяo���Ɏg���\�t�g�E�F�A���荞�݂Ȃǂ̃t�b�N��h���悤�ɂ��� */
}

