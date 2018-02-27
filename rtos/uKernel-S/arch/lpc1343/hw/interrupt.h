/*
 * interrupt.h
 *
 *  Created on: 2010/09/16
 *      Author: whelp
 */

#ifndef INTERRUPT_H_
#define INTERRUPT_H_

#define WEAK __attribute__ ((weak))
#define ALIAS(f) __attribute__ ((weak, alias (#f)))

//*****************************************************************************

//*****************************************************************************
//
// Forward declaration of the default handlers. These are aliased.
// When the application defines a handler (with the same name), this will
// automatically take precedence over these weak definitions
//
//*****************************************************************************
extern void ResetISR(void);
extern void NMI_Handler(void);
extern void HardFault_Handler(void);
extern void MemManage_Handler(void);
extern void BusFault_Handler(void);
extern void UsageFault_Handler(void);
extern void SVCall_Handler(void);
extern void DebugMon_Handler(void);
extern void PendSV_Handler(void);
extern void SysTick_Handler(void);
extern void IntDefaultHandler(void);

//*****************************************************************************
//
// Forward declaration of the specific IRQ handlers. These are aliased
// to the IntDefaultHandler, which is a 'forever' loop. When the application
// defines a handler (with the same name), this will automatically take
// precedence over these weak definitions
//
//*****************************************************************************

extern void I2C_IRQHandler(void);
extern void TIMER16_0_IRQHandler(void);
extern void TIMER16_1_IRQHandler(void);
extern void TIMER32_0_IRQHandler(void);
extern void TIMER32_1_IRQHandler(void);
extern void SSP_IRQHandler(void);
extern void UART_IRQHandler(void);
extern void USB_IRQHandler(void);
extern void USB_FIQHandler(void);
extern void ADC_IRQHandler(void);
extern void WDT_IRQHandler(void);
extern void BOD_IRQHandler(void);
extern void FMC_IRQHandler(void);
extern void PIOINT3_IRQHandler(void);
extern void PIOINT2_IRQHandler(void);
extern void PIOINT1_IRQHandler(void);
extern void PIOINT0_IRQHandler(void);
extern void WAKEUP_IRQHandler(void);

#endif /* INTERRUPT_H_ */
