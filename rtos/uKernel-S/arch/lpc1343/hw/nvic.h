/*
 * nvic.h
 *
 *  Created on: 2010/09/16
 *      Author: whelp
 */

#ifndef NVIC_H_
#define NVIC_H_

/* ネスト型ベクタ割り込みコントローラ - Nested Vectored Interrupt Controller */

#define NVIC_BASE_ADDRESS	(0xE000E100UL)

typedef uint32_t nvic_iser_t[8];
typedef uint32_t nvic_icer_t[8];
typedef uint32_t nvic_ispr_t[8];
typedef uint32_t nvic_icpr_t[8];
typedef uint32_t nvic_iabr_t[8];
typedef uint32_t nvic_ipr_t[8];

/*!< Offset: 0x000  Interrupt Set Enable Register           */
#define NVIC_ISER (*(volatile nvic_iser_t*)(NVIC_BASE_ADDRESS + 0x0000UL))
/*!< Offset: 0x080  Interrupt Clear Enable Register         */
#define NVIC_ICER (*(volatile nvic_icer_t*)(NVIC_BASE_ADDRESS + 0x0080UL))
/*!< Offset: 0x100  Interrupt Set Pending Register          */
#define NVIC_ISPR (*(volatile nvic_ispr_t*)(NVIC_BASE_ADDRESS + 0x0100UL))
/*!< Offset: 0x180  Interrupt Clear Pending Register        */
#define NVIC_ICPR (*(volatile nvic_icpr_t*)(NVIC_BASE_ADDRESS + 0x0180UL))
/*!< Offset: 0x200  Interrupt Active bit Register           */
#define NVIC_IABR (*(volatile nvic_iabr_t*)(NVIC_BASE_ADDRESS + 0x0200UL))
/*!< Offset: 0x300  Interrupt Priority Register (8Bit wide) */
#define NVIC_IPR  (*(volatile nvic_ipr_t *)(NVIC_BASE_ADDRESS + 0x0300UL))
/*!< Offset: 0xE00  Software Trigger Interrupt Register     */
#define NVIC_STIR	(*(volatile uint32_t  *)(NVIC_BASE_ADDRESS + 0x0E00UL))

typedef enum IRQn
{
/******  Cortex-M3 Processor Exceptions Numbers ***************************************************/
	NonMaskableInt_IRQn           = -14,      /*!< 2 Non Maskable Interrupt                         */
	MemoryManagement_IRQn         = -12,      /*!< 4 Cortex-M3 Memory Management Interrupt          */
	BusFault_IRQn                 = -11,      /*!< 5 Cortex-M3 Bus Fault Interrupt                  */
	UsageFault_IRQn               = -10,      /*!< 6 Cortex-M3 Usage Fault Interrupt                */
	SVCall_IRQn                   = -5,       /*!< 11 Cortex-M3 SV Call Interrupt                   */
	DebugMonitor_IRQn             = -4,       /*!< 12 Cortex-M3 Debug Monitor Interrupt             */
	PendSV_IRQn                   = -2,       /*!< 14 Cortex-M3 Pend SV Interrupt                   */
	SysTick_IRQn                  = -1,       /*!< 15 Cortex-M3 System Tick Interrupt               */

/******  LPC13xx Specific Interrupt Numbers *******************************************************/
	WAKEUP0_IRQn                  = 0,        /*!< All I/O pins can be used as wakeup source.       */
	WAKEUP1_IRQn                  = 1,        /*!< There are 40 pins in total for LPC17xx           */
	WAKEUP2_IRQn                  = 2,
	WAKEUP3_IRQn                  = 3,
	WAKEUP4_IRQn                  = 4,
	WAKEUP5_IRQn                  = 5,
	WAKEUP6_IRQn                  = 6,
	WAKEUP7_IRQn                  = 7,
	WAKEUP8_IRQn                  = 8,
	WAKEUP9_IRQn                  = 9,
	WAKEUP10_IRQn                 = 10,
	WAKEUP11_IRQn                 = 11,
	WAKEUP12_IRQn                 = 12,
	WAKEUP13_IRQn                 = 13,
	WAKEUP14_IRQn                 = 14,
	WAKEUP15_IRQn                 = 15,
	WAKEUP16_IRQn                 = 16,
	WAKEUP17_IRQn                 = 17,
	WAKEUP18_IRQn                 = 18,
	WAKEUP19_IRQn                 = 19,
	WAKEUP20_IRQn                 = 20,
	WAKEUP21_IRQn                 = 21,
	WAKEUP22_IRQn                 = 22,
	WAKEUP23_IRQn                 = 23,
	WAKEUP24_IRQn                 = 24,
	WAKEUP25_IRQn                 = 25,
	WAKEUP26_IRQn                 = 26,
	WAKEUP27_IRQn                 = 27,
	WAKEUP28_IRQn                 = 28,
	WAKEUP29_IRQn                 = 29,
	WAKEUP30_IRQn                 = 30,
	WAKEUP31_IRQn                 = 31,
	WAKEUP32_IRQn                 = 32,
	WAKEUP33_IRQn                 = 33,
	WAKEUP34_IRQn                 = 34,
	WAKEUP35_IRQn                 = 35,
	WAKEUP36_IRQn                 = 36,
	WAKEUP37_IRQn                 = 37,
	WAKEUP38_IRQn                 = 38,
	WAKEUP39_IRQn                 = 39,
	I2C_IRQn                      = 40,       /*!< I2C Interrupt                                    */
	TIMER_16_0_IRQn               = 41,       /*!< 16-bit Timer0 Interrupt                          */
	TIMER_16_1_IRQn               = 42,       /*!< 16-bit Timer1 Interrupt                          */
	TIMER_32_0_IRQn               = 43,       /*!< 32-bit Timer0 Interrupt                          */
	TIMER_32_1_IRQn               = 44,       /*!< 32-bit Timer1 Interrupt                          */
	SSP_IRQn                      = 45,       /*!< SSP Interrupt                                    */
	UART_IRQn                     = 46,       /*!< UART Interrupt                                   */
	USB_IRQn                      = 47,       /*!< USB Regular Interrupt                            */
	USB_FIQn                      = 48,       /*!< USB Fast Interrupt                               */
	ADC_IRQn                      = 49,       /*!< A/D Converter Interrupt                          */
	WDT_IRQn                      = 50,       /*!< Watchdog timer Interrupt                         */
	BOD_IRQn                      = 51,       /*!< Brown Out Detect(BOD) Interrupt                  */
	EINT3_IRQn                    = 53,       /*!< External Interrupt 3 Interrupt                   */
	EINT2_IRQn                    = 54,       /*!< External Interrupt 2 Interrupt                   */
	EINT1_IRQn                    = 55,       /*!< External Interrupt 1 Interrupt                   */
	EINT0_IRQn                    = 56,       /*!< External Interrupt 0 Interrupt                   */
} IRQn_t;

/*!< 割り込み許可を設定 */
static inline void NVIC_EnableIRQ(IRQn_t IRQn) {
  NVIC_ISER[((uint32_t)(IRQn) >> 5)] = (1 << ((uint32_t)(IRQn) & 0x1F));
}

/*!< 割り込み禁止を設定 */
static inline void NVIC_DisableIRQ(IRQn_t IRQn) {
  NVIC_ICER[((uint32_t)(IRQn) >> 5)] = (1 << ((uint32_t)(IRQn) & 0x1F));
}

#endif /* NVIC_H_ */
