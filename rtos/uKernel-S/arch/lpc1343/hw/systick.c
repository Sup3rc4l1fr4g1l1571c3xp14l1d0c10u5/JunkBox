/*
 * systick.c
 *
 *  Created on: 2010/09/17
 *      Author: whelp
 */

#include "systick.h"

typedef uint32_t* preg32_t;

// systickはシステムクロックと同期したカウンタ？

#define SYSTICK_BASE_ADDRESS                      (0xE000E000)

#define SYSTICK_STCTRL                            (*(preg32_t)(0xE000E010))    // System tick control
#define SYSTICK_STRELOAD                          (*(preg32_t)(0xE000E014))    // System timer reload
#define SYSTICK_STCURR                            (*(preg32_t)(0xE000E018))    // System timer current
#define SYSTICK_STCALIB                           (*(preg32_t)(0xE000E01C))    // System timer calibration

/*  STCTRL (System Timer Control and status register) */
#define SYSTICK_STCTRL_ENABLE                     (0x00000001)    // System tick counter enable
#define SYSTICK_STCTRL_TICKINT                    (0x00000002)    // System tick interrupt enable
#define SYSTICK_STCTRL_CLKSOURCE                  (0x00000004)    // NOTE: This isn't documented but is based on NXP examples
#define SYSTICK_STCTRL_COUNTFLAG                  (0x00010000)    // System tick counter flag

#define SYSTICK_STRELOAD_MASK                     (0x00FFFFFF)

#define CFG_CPU_CCLK                (72000000)

static uint32_t SYSTICK_Config(uint32_t ticks) {
	// 間隔が最大値以上ならなかったことに。
	if (ticks > SYSTICK_STRELOAD_MASK) {
		return (1);
	}

	// リロード値レジスタを設定
	SYSTICK_STRELOAD  = (ticks & SYSTICK_STRELOAD_MASK) - 1;

	// カウンタ値レジスタを設定
	SYSTICK_STCURR = 0;

	// タイマカウントと割り込みを有効化
	SYSTICK_STCTRL = SYSTICK_STCTRL_CLKSOURCE | SYSTICK_STCTRL_TICKINT | SYSTICK_STCTRL_ENABLE;

	return (0);
}

// 	systickの間隔を delayMs [ms] に設定
void SYSTICK_Initialize(uint32_t delayMs) {
	SYSTICK_Config ((CFG_CPU_CCLK / 1000) * delayMs);
}

