/***********************************************************************/
/*                                                                     */
/*  FILE        :Main.c or Main.cpp                                    */
/*  DATE        :Tue, Oct 31, 2006                                     */
/*  DESCRIPTION :Main Program                                          */
/*  CPU TYPE    :                                                      */
/*                                                                     */
/*  NOTE:THIS IS A TYPICAL EXAMPLE.                                    */
/*                                                                     */
/***********************************************************************/
#include "typedefine.h"
#include "iodefine.h"

void SYSTICK_Initialize(int ms) {
	SYSTEM.MSTPCRA.BIT.MSTPA15 = 0; //���W���[���X�g�b�v��Ԃ̉��� CMT0
	
	CMT.CMSTR0.BIT.STR0 = 0; // CMT0�J�E���^��~
	CMT0.CMCR.BIT.CMIE = 0; // �R���y�A�}�b�`���荞�ݒ�~

	CMT0.CMCR.BIT.CKS = 0; // ��/8 = 12,500,000
	CMT0.CMCNT = 0; // �J�E���g�N���A
	CMT0.CMCOR = 12500 * ms; // �����ݒ�

	ICU.IER[0x03].BIT.IEN4 = 1; // ���荞�ݗv������
	ICU.IPR[0x04].BIT.IPR = 10; // ���荞�ݗD�惌�x��

	CMT0.CMCR.BIT.CMIE = 1; // �R���y�A�}�b�`���荞�݋���
	CMT.CMSTR0.BIT.STR0 = 1; // CMT�J�E���^�J�n
}

void SYSTICK_Reset(void) {
	CMT0.CMCNT = 0;
}

void CPU_Initialize() {
	// ���ɂ��邱�ƂȂ�
}

