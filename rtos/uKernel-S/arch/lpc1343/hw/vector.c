/*
 * vector.c
 *
 *  Created on: 2010/09/16
 *      Author: whelp
 */

#include "interrupt.h"

//*****************************************************************************
//
// External declaration for the pointer to the stack top from the Linker Script
//
//*****************************************************************************
extern void _vStackTop(void);

//*****************************************************************************
//
// The vector table.  Note that the proper constructs must be placed on this to
// ensure that it ends up at physical address 0x0000.0000.
//
//*****************************************************************************
extern void (* const g_pfnVectors[])(void);
__attribute__ ((section(".isr_vector")))
void (* const g_pfnVectors[])(void) = {
// Core Level - CM3
	&_vStackTop, // The initial stack pointer
	ResetISR, // The reset handler
	NMI_Handler, // The NMI handler
	HardFault_Handler, // The hard fault handler
	MemManage_Handler, // The MPU fault handler
	BusFault_Handler, // The bus fault handler
	UsageFault_Handler, // The usage fault handler
	0, // Reserved
	0, // Reserved
	0, // Reserved
	0, // Reserved
	SVCall_Handler, // SVCall handler
	DebugMon_Handler, // Debug monitor handler
	0, // Reserved
	PendSV_Handler, // The PendSV handler
	SysTick_Handler, // The SysTick handler


	// Wakeup sources (40 ea.) for the I/O pins:
	//   PIO0 (0:11)
	//   PIO1 (0:11)
	//   PIO2 (0:11)
	//   PIO3 (0:3)
	WAKEUP_IRQHandler, // PIO0_0  Wakeup
	WAKEUP_IRQHandler, // PIO0_1  Wakeup
	WAKEUP_IRQHandler, // PIO0_2  Wakeup
	WAKEUP_IRQHandler, // PIO0_3  Wakeup
	WAKEUP_IRQHandler, // PIO0_4  Wakeup
	WAKEUP_IRQHandler, // PIO0_5  Wakeup
	WAKEUP_IRQHandler, // PIO0_6  Wakeup
	WAKEUP_IRQHandler, // PIO0_7  Wakeup
	WAKEUP_IRQHandler, // PIO0_8  Wakeup
	WAKEUP_IRQHandler, // PIO0_9  Wakeup
	WAKEUP_IRQHandler, // PIO0_10 Wakeup
	WAKEUP_IRQHandler, // PIO0_11 Wakeup

	WAKEUP_IRQHandler, // PIO1_0  Wakeup
	WAKEUP_IRQHandler, // PIO1_1  Wakeup
	WAKEUP_IRQHandler, // PIO1_2  Wakeup
	WAKEUP_IRQHandler, // PIO1_3  Wakeup
	WAKEUP_IRQHandler, // PIO1_4  Wakeup
	WAKEUP_IRQHandler, // PIO1_5  Wakeup
	WAKEUP_IRQHandler, // PIO1_6  Wakeup
	WAKEUP_IRQHandler, // PIO1_7  Wakeup
	WAKEUP_IRQHandler, // PIO1_8  Wakeup
	WAKEUP_IRQHandler, // PIO1_9  Wakeup
	WAKEUP_IRQHandler, // PIO1_10 Wakeup
	WAKEUP_IRQHandler, // PIO1_11 Wakeup

	WAKEUP_IRQHandler, // PIO2_0  Wakeup
	WAKEUP_IRQHandler, // PIO2_1  Wakeup
	WAKEUP_IRQHandler, // PIO2_2  Wakeup
	WAKEUP_IRQHandler, // PIO2_3  Wakeup
	WAKEUP_IRQHandler, // PIO2_4  Wakeup
	WAKEUP_IRQHandler, // PIO2_5  Wakeup
	WAKEUP_IRQHandler, // PIO2_6  Wakeup
	WAKEUP_IRQHandler, // PIO2_7  Wakeup
	WAKEUP_IRQHandler, // PIO2_8  Wakeup
	WAKEUP_IRQHandler, // PIO2_9  Wakeup
	WAKEUP_IRQHandler, // PIO2_10 Wakeup
	WAKEUP_IRQHandler, // PIO2_11 Wakeup

	WAKEUP_IRQHandler, // PIO3_0  Wakeup
	WAKEUP_IRQHandler, // PIO3_1  Wakeup
	WAKEUP_IRQHandler, // PIO3_2  Wakeup
	WAKEUP_IRQHandler, // PIO3_3  Wakeup

	I2C_IRQHandler, // I2C0
	TIMER16_0_IRQHandler, // CT16B0 (16-bit Timer 0)
	TIMER16_1_IRQHandler, // CT16B1 (16-bit Timer 1)
	TIMER32_0_IRQHandler, // CT32B0 (32-bit Timer 0)
	TIMER32_1_IRQHandler, // CT32B1 (32-bit Timer 1)
	SSP_IRQHandler, // SSP0
	UART_IRQHandler, // UART0

	USB_IRQHandler, // USB IRQ
	USB_FIQHandler, // USB FIQ

	ADC_IRQHandler, // ADC   (A/D Converter)
	WDT_IRQHandler, // WDT   (Watchdog Timer)
	BOD_IRQHandler, // BOD   (Brownout Detect)
	FMC_IRQHandler, // Flash (IP2111 Flash Memory Controller)
	PIOINT3_IRQHandler, // PIO INT3
	PIOINT2_IRQHandler, // PIO INT2
	PIOINT1_IRQHandler, // PIO INT1
	PIOINT0_IRQHandler, // PIO INT0

};
