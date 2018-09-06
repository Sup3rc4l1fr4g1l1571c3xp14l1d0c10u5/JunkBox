#include "tm_stm32f4_delay.h"
#include "./arch.h"

void WaitMS(uint32_t n) {
	Delayms(n);
}

void DisableInterrupt(void)
{
	NVIC_DisableIRQ();
}

void EnableInterrupt(void)
{
	NVIC_EnableIRQ();
}

#define USE_DYNTAMIC_AREA
