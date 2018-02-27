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
	SYSTEM.MSTPCRA.BIT.MSTPA15 = 0; //モジュールストップ状態の解除 CMT0
	
	CMT.CMSTR0.BIT.STR0 = 0; // CMT0カウンタ停止
	CMT0.CMCR.BIT.CMIE = 0; // コンペアマッチ割り込み停止

	CMT0.CMCR.BIT.CKS = 0; // φ/8 = 12,500,000
	CMT0.CMCNT = 0; // カウントクリア
	CMT0.CMCOR = 12500 * ms; // 周期設定

	ICU.IER[0x03].BIT.IEN4 = 1; // 割り込み要求許可
	ICU.IPR[0x04].BIT.IPR = 10; // 割り込み優先レベル

	CMT0.CMCR.BIT.CMIE = 1; // コンペアマッチ割り込み許可
	CMT.CMSTR0.BIT.STR0 = 1; // CMTカウンタ開始
}

void SYSTICK_Reset(void) {
	CMT0.CMCNT = 0;
}

void CPU_Initialize() {
	// 特にすることない
}

