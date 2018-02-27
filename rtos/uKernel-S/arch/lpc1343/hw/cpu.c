/*
 * cpu.c
 *
 *  Created on: 2010/09/16
 *      Author: whelp
 */

#include "../../../src/type.h"
#include "gpio.h"
#include "cpu.h"

typedef uint32_t* preg32_t;

/* System Control Block */
#define SCB_BASE_ADDRESS	(*(preg32_t)(0x40048000))	// System control block base address
#define SCB_MEMREMAP		(*(preg32_t)(0x40048000))	// System memory remap
#define SCB_PRESETCTRL		(*(preg32_t)(0x40048004))	// Peripheral reset control
#define SCB_PLLCTRL			(*(preg32_t)(0x40048008))	// System PLL control
#define SCB_PLLSTAT			(*(preg32_t)(0x4004800C))	// System PLL status
#define SCB_USBPLLCTRL		(*(preg32_t)(0x40048010))	// USB PLL control
#define SCB_USBPLLSTAT		(*(preg32_t)(0x40048014))	// USB PLL status
#define SCB_SYSOSCCTRL		(*(preg32_t)(0x40048020))	// System oscillator control
#define SCB_WDTOSCCTRL		(*(preg32_t)(0x40048024))	// Watchdog oscillator control
#define SCB_IRCCTRL			(*(preg32_t)(0x40048028))	// IRC control
#define SCB_RESETSTAT		(*(preg32_t)(0x40048030))	// System reset status register
#define SCB_PLLCLKSEL		(*(preg32_t)(0x40048040))	// System PLL clock source select
#define SCB_PLLCLKUEN		(*(preg32_t)(0x40048044))	// System PLL clock source update enable
#define SCB_USBPLLCLKSEL	(*(preg32_t)(0x40048048))	// USB PLL clock source select
#define SCB_USBPLLCLKUEN	(*(preg32_t)(0x4004804C))	// USB PLL clock source update enable
#define SCB_MAINCLKSEL		(*(preg32_t)(0x40048070))	// Main clock source select
#define SCB_MAINCLKUEN		(*(preg32_t)(0x40048074))	// Main clock source update enable
#define SCB_SYSAHBCLKDIV	(*(preg32_t)(0x40048078))	// System AHB clock divider
#define SCB_SYSAHBCLKCTRL	(*(preg32_t)(0x40048080))	// System AHB clock control
#define SCB_SSP0CLKDIV		(*(preg32_t)(0x40048094))	// SSP0 clock divider
#define SCB_UARTCLKDIV		(*(preg32_t)(0x40048098))	// UART clock divider
#define SCB_SYSTICKCLKDIV	(*(preg32_t)(0x400480B0))	// System tick clock divider
#define SCB_USBCLKSEL		(*(preg32_t)(0x400480C0))	// USB clock source select
#define SCB_USBCLKUEN		(*(preg32_t)(0x400480C4))	// USB clock source update enable
#define SCB_USBCLKDIV		(*(preg32_t)(0x400480C8))	// USB clock divider
#define SCB_WDTCLKSEL		(*(preg32_t)(0x400480D0))	// Watchdog clock source select
#define SCB_WDTCLKUEN		(*(preg32_t)(0x400480D4))	// Watchdog clock source update enable
#define SCB_WDTCLKDIV		(*(preg32_t)(0x400480D8))	// Watchdog clock divider
#define SCB_CLKOUTCLKSEL	(*(preg32_t)(0x400480E0))	// CLKOUT clock source select
#define SCB_CLKOUTCLKUEN	(*(preg32_t)(0x400480E4))	// CLKOUT clock source update enable
#define SCB_CLKOUTCLKDIV	(*(preg32_t)(0x400480E8))	// CLKOUT clock divider
#define SCB_PIOPORCAP0		(*(preg32_t)(0x40048100))	// POR captured PIO status 0
#define SCB_PIOPORCAP1		(*(preg32_t)(0x40048104))	// POR captured PIO status 1
#define SCB_BODCTRL			(*(preg32_t)(0x40048150))	// Brown-out detector control
#define SCB_SYSTICKCCAL		(*(preg32_t)(0x40048158))	// System tick counter calibration
#define SCB_STARTAPRP0		(*(preg32_t)(0x40048200))	// Start logic edge control register 0; bottom 32 interrupts
#define SCB_STARTERP0		(*(preg32_t)(0x40048204))	// Start logic signal enable register 0; bottom 32 interrupts
#define SCB_STARTRSRP0CLR	(*(preg32_t)(0x40048208))	// Start logic reset register 0; bottom 32 interrupts
#define SCB_STARTSRP0		(*(preg32_t)(0x4004820C))	// Start logic status register 0; bottom 32 interrupts
#define SCB_STARTAPRP1		(*(preg32_t)(0x40048210))	// Start logic edge control register 1; top 8 interrupts
#define SCB_STARTERP1		(*(preg32_t)(0x40048214))	// Start logic signal enable register 1; top 8 interrupts
#define SCB_STARTRSRP1CLR	(*(preg32_t)(0x40048218))	// Start logic reset register 1; top 8 interrupts
#define SCB_STARTSRP1		(*(preg32_t)(0x4004821C))	// Start logic status register 1; top 8 interrupts
#define SCB_PDSLEEPCFG		(*(preg32_t)(0x40048230))	// Power-down states in Deep-sleep mode
#define SCB_PDAWAKECFG		(*(preg32_t)(0x40048234))	// Power-down states after wake-up from Deep-sleep mode
#define SCB_PDRUNCFG		(*(preg32_t)(0x40048238))	// Power-down configuration register
#define SCB_DEVICEID		(*(preg32_t)(0x400483F4))	// Device ID
#define SCB_MMFAR			(*(preg32_t)(0xE000ED34))	// Memory Manage Address Register (MMAR)
#define SCB_BFAR			(*(preg32_t)(0xE000ED38))	// Bus Fault Manage Address Register (BFAR)


/*  PDRUNCFG (Power-down configuration register)
    The bits in the PDRUNCFG register control the power to the various analog blocks. This
    register can be written to at any time while the chip is running, and a write will take effect
    immediately with the exception of the power-down signal to the IRC.  Setting a 1 powers-down
    a peripheral and 0 enables it. */

#define SCB_PDRUNCFG_IRCOUT			((uint32_t) 0x00000001)	// IRC oscillator output power-down
#define SCB_PDRUNCFG_IRCOUT_MASK                  ((uint32_t) 0x00000001)
#define SCB_PDRUNCFG_IRC                          ((uint32_t) 0x00000002)	// IRC oscillator power-down
#define SCB_PDRUNCFG_IRC_MASK                     ((uint32_t) 0x00000002)
#define SCB_PDRUNCFG_FLASH                        ((uint32_t) 0x00000004)	// Flash power-down
#define SCB_PDRUNCFG_FLASH_MASK                   ((uint32_t) 0x00000004)
#define SCB_PDRUNCFG_BOD                          ((uint32_t) 0x00000008)	// Brown-out detector power-down
#define SCB_PDRUNCFG_BOD_MASK                     ((uint32_t) 0x00000008)
#define SCB_PDRUNCFG_ADC                          ((uint32_t) 0x00000010)	// ADC power-down
#define SCB_PDRUNCFG_ADC_MASK                     ((uint32_t) 0x00000010)
#define SCB_PDRUNCFG_SYSOSC                       ((uint32_t) 0x00000020)	// System oscillator power-down
#define SCB_PDRUNCFG_SYSOSC_MASK                  ((uint32_t) 0x00000020)
#define SCB_PDRUNCFG_WDTOSC                       ((uint32_t) 0x00000040)	// Watchdog oscillator power-down
#define SCB_PDRUNCFG_WDTOSC_MASK                  ((uint32_t) 0x00000040)
#define SCB_PDRUNCFG_SYSPLL                       ((uint32_t) 0x00000080)	// System PLL power-down
#define SCB_PDRUNCFG_SYSPLL_MASK                  ((uint32_t) 0x00000080)
#define SCB_PDRUNCFG_USBPLL                       ((uint32_t) 0x00000100)	// USB PLL power-down
#define SCB_PDRUNCFG_USBPLL_MASK                  ((uint32_t) 0x00000100)
#define SCB_PDRUNCFG_USBPAD                       ((uint32_t) 0x00000400)	// USB PHY power-down
#define SCB_PDRUNCFG_USBPAD_MASK                  ((uint32_t) 0x00000400)

/*  SYSOSCCTRL (System oscillator control register)
    This register configures the frequency range for the system oscillator. */

#define SCB_SYSOSCCTRL_BYPASS_DISABLED            ((uint32_t) 0x00000000)	// Oscillator is not bypassed.
#define SCB_SYSOSCCTRL_BYPASS_ENABLED             ((uint32_t) 0x00000001)	// Bypass enabled
#define SCB_SYSOSCCTRL_BYPASS_MASK                ((uint32_t) 0x00000001)
#define SCB_SYSOSCCTRL_FREQRANGE_1TO20MHZ         ((uint32_t) 0x00000000)	// 1-20 MHz frequency range
#define SCB_SYSOSCCTRL_FREQRANGE_15TO25MHZ        ((uint32_t) 0x00000002)	// 15-25 MHz frequency range
#define SCB_SYSOSCCTRL_FREQRANGE_MASK             ((uint32_t) 0x00000002)

/*  SYSPLLCLKSEL (System PLL clock source select register)
    This register selects the clock source for the system PLL. The SYSPLLCLKUEN register
    must be toggled from LOW to HIGH for the update to take effect.
    Remark: The system oscillator must be selected if the system PLL is used to generate a
    48 MHz clock to the USB block.
*/

#define SCB_CLKSEL_SOURCE_INTERNALOSC             ((uint32_t) 0x00000000)
#define SCB_CLKSEL_SOURCE_MAINOSC                 ((uint32_t) 0x00000001)
#define SCB_CLKSEL_SOURCE_RTCOSC                  ((uint32_t) 0x00000002)
#define SCB_CLKSEL_SOURCE_MASK                    ((uint32_t) 0x00000002)

/*  SYSPLLUEN (System PLL clock source update enable register)
    This register updates the clock source of the system PLL with the new input clock after the
    SYSPLLCLKSEL register has been written to. In order for the update to take effect, first
    write a zero to the SYSPLLUEN register and then write a one to SYSPLLUEN. */

#define SCB_PLLCLKUEN_DISABLE                     ((uint32_t) 0x00000000)
#define SCB_PLLCLKUEN_UPDATE                      ((uint32_t) 0x00000001)
#define SCB_PLLCLKUEN_MASK                        ((uint32_t) 0x00000001)

/*  SYSPLLCTRL (System PLL control register)
    This register connects and enables the system PLL and configures the PLL multiplier and
    divider values. The PLL accepts an input frequency from 10 MHz to 25 MHz from various
    clock sources. The input frequency is multiplied up to a high frequency, then divided down
    to provide the actual clock used by the CPU, peripherals, and optionally the USB
    subsystem. Note that the USB subsystem has its own dedicated PLL. The PLL can
    produce a clock up to the maximum allowed for the CPU, which is 72 MHz. */

#define SCB_PLLCTRL_MULT_1                        ((uint32_t) 0x00000000)
#define SCB_PLLCTRL_MULT_2                        ((uint32_t) 0x00000001)
#define SCB_PLLCTRL_MULT_3                        ((uint32_t) 0x00000002)
#define SCB_PLLCTRL_MULT_4                        ((uint32_t) 0x00000003)
#define SCB_PLLCTRL_MULT_5                        ((uint32_t) 0x00000004)
#define SCB_PLLCTRL_MULT_6                        ((uint32_t) 0x00000005)
#define SCB_PLLCTRL_MULT_7                        ((uint32_t) 0x00000006)
#define SCB_PLLCTRL_MULT_8                        ((uint32_t) 0x00000007)
#define SCB_PLLCTRL_MULT_9                        ((uint32_t) 0x00000008)
#define SCB_PLLCTRL_MULT_10                       ((uint32_t) 0x00000009)
#define SCB_PLLCTRL_MULT_11                       ((uint32_t) 0x0000000A)
#define SCB_PLLCTRL_MULT_12                       ((uint32_t) 0x0000000B)
#define SCB_PLLCTRL_MULT_13                       ((uint32_t) 0x0000000C)
#define SCB_PLLCTRL_MULT_14                       ((uint32_t) 0x0000000D)
#define SCB_PLLCTRL_MULT_15                       ((uint32_t) 0x0000000E)
#define SCB_PLLCTRL_MULT_16                       ((uint32_t) 0x0000000F)
#define SCB_PLLCTRL_MULT_17                       ((uint32_t) 0x00000010)
#define SCB_PLLCTRL_MULT_18                       ((uint32_t) 0x00000011)
#define SCB_PLLCTRL_MULT_19                       ((uint32_t) 0x00000012)
#define SCB_PLLCTRL_MULT_20                       ((uint32_t) 0x00000013)
#define SCB_PLLCTRL_MULT_21                       ((uint32_t) 0x00000014)
#define SCB_PLLCTRL_MULT_22                       ((uint32_t) 0x00000015)
#define SCB_PLLCTRL_MULT_23                       ((uint32_t) 0x00000016)
#define SCB_PLLCTRL_MULT_24                       ((uint32_t) 0x00000017)
#define SCB_PLLCTRL_MULT_25                       ((uint32_t) 0x00000018)
#define SCB_PLLCTRL_MULT_26                       ((uint32_t) 0x00000019)
#define SCB_PLLCTRL_MULT_27                       ((uint32_t) 0x0000001A)
#define SCB_PLLCTRL_MULT_28                       ((uint32_t) 0x0000001B)
#define SCB_PLLCTRL_MULT_29                       ((uint32_t) 0x0000001C)
#define SCB_PLLCTRL_MULT_30                       ((uint32_t) 0x0000001D)
#define SCB_PLLCTRL_MULT_31                       ((uint32_t) 0x0000001E)
#define SCB_PLLCTRL_MULT_32                       ((uint32_t) 0x0000001F)
#define SCB_PLLCTRL_MULT_MASK                     ((uint32_t) 0x0000001F)
#define SCB_PLLCTRL_DIV_2                         ((uint32_t) 0x00000000)
#define SCB_PLLCTRL_DIV_4                         ((uint32_t) 0x00000020)
#define SCB_PLLCTRL_DIV_8                         ((uint32_t) 0x00000040)
#define SCB_PLLCTRL_DIV_16                        ((uint32_t) 0x00000060)
#define SCB_PLLCTRL_DIV_BIT                       (5)
#define SCB_PLLCTRL_DIV_MASK                      ((uint32_t) 0x00000060)
#define SCB_PLLCTRL_DIRECT_MASK                   ((uint32_t) 0x00000080)	// Direct CCO clock output control
#define SCB_PLLCTRL_BYPASS_MASK                   ((uint32_t) 0x00000100)	// Input clock bypass control
#define SCB_PLLCTRL_MASK                          ((uint32_t) 0x000001FF)

/*  SYSPLLSTAT (System PLL status register)
    This register is a Read-only register and supplies the PLL lock status */

#define SCB_PLLSTAT_LOCK                          ((uint32_t) 0x00000001)	// 0 = PLL not locked, 1 = PLL locked
#define SCB_PLLSTAT_LOCK_MASK                     ((uint32_t) 0x00000001)

/*  MAINCLKUEN (Main clock source update enable register)
    This register updates the clock source of the main clock with the new input clock after the
    MAINCLKSEL register has been written to. In order for the update to take effect, first write
    a zero to the MAINUEN register and then write a one to MAINCLKUEN. */

#define SCB_MAINCLKUEN_DISABLE                    ((uint32_t) 0x00000000)
#define SCB_MAINCLKUEN_UPDATE                     ((uint32_t) 0x00000001)
#define SCB_MAINCLKUEN_MASK                       ((uint32_t) 0x00000001)

/*  PDSLEEPCFG (Deep-sleep mode configuration register)
    The bits in this register can be programmed to indicate the state the chip must enter when
    the Deep-sleep mode is asserted by the ARM. The value of the PDSLEEPCFG register
    will be automatically loaded into the PDRUNCFG register when the Sleep mode is
    entered. */

#define SCB_PDSLEEPCFG_IRCOUT_PD                  ((uint32_t) 0x00000001)
#define SCB_PDSLEEPCFG_IRCOUT_PD_MASK             ((uint32_t) 0x00000001)
#define SCB_PDSLEEPCFG_IRC_PD                     ((uint32_t) 0x00000002)
#define SCB_PDSLEEPCFG_IRC_PD_MASK                ((uint32_t) 0x00000002)
#define SCB_PDSLEEPCFG_FLASH_PD                   ((uint32_t) 0x00000004)
#define SCB_PDSLEEPCFG_FLASH_PD_MASK              ((uint32_t) 0x00000004)
#define SCB_PDSLEEPCFG_BOD_PD                     ((uint32_t) 0x00000008)
#define SCB_PDSLEEPCFG_BOD_PD_MASK                ((uint32_t) 0x00000008)
#define SCB_PDSLEEPCFG_ADC_PD                     ((uint32_t) 0x00000010)
#define SCB_PDSLEEPCFG_ADC_PD_MASK                ((uint32_t) 0x00000010)
#define SCB_PDSLEEPCFG_SYSOSC_PD                  ((uint32_t) 0x00000020)
#define SCB_PDSLEEPCFG_SYSOSC_PD_MASK             ((uint32_t) 0x00000020)
#define SCB_PDSLEEPCFG_WDTOSC_PD                  ((uint32_t) 0x00000040)
#define SCB_PDSLEEPCFG_WDTOSC_PD_MASK             ((uint32_t) 0x00000040)
#define SCB_PDSLEEPCFG_SYSPLL_PD                  ((uint32_t) 0x00000080)
#define SCB_PDSLEEPCFG_SYSPLL_PD_MASK             ((uint32_t) 0x00000080)
#define SCB_PDSLEEPCFG_USBPLL_PD                  ((uint32_t) 0x00000100)
#define SCB_PDSLEEPCFG_USBPLL_PD_MASK             ((uint32_t) 0x00000100)
#define SCB_PDSLEEPCFG_USBPAD_PD                  ((uint32_t) 0x00000400)
#define SCB_PDSLEEPCFG_USBPAD_PD_MASK             ((uint32_t) 0x00000400)

/*  MAINCLKSEL (Main clock source select register)
    This register selects the main system clock which can be either the output from the
    system PLL or the IRC, system, or Watchdog oscillators directly. The main system clock
    clocks the core, the peripherals, and optionally the USB block.
    The MAINCLKUEN register must be toggled from LOW to HIGH for the update to take effect.*/

#define SCB_MAINCLKSEL_SOURCE_INTERNALOSC         ((uint32_t) 0x00000000)	// Use IRC oscillator for main clock source
#define SCB_MAINCLKSEL_SOURCE_INPUTCLOCK          ((uint32_t) 0x00000001)	// Use Input clock to system PLL for main clock source
#define SCB_MAINCLKSEL_SOURCE_WDTOSC              ((uint32_t) 0x00000002)	// Use watchdog oscillator for main clock source
#define SCB_MAINCLKSEL_SOURCE_SYSPLLCLKOUT        ((uint32_t) 0x00000003)	// Use system PLL clock out for main clock source
#define SCB_MAINCLKSEL_MASK                       ((uint32_t) 0x00000003)

/*  SYSAHBCLKDIV (System AHB clock divider register)
    This register divides the main clock to provide the system clock to the core, memories,
    and the peripherals. The system clock can be shut down completely by setting the DIV
    bits to 0x0. */

#define SCB_SYSAHBCLKDIV_DISABLE                  ((uint32_t) 0x00000000)	// 0 will shut the system clock down completely
#define SCB_SYSAHBCLKDIV_DIV1                     ((uint32_t) 0x00000001)	// 1, 2 or 4 are the most common values
#define SCB_SYSAHBCLKDIV_DIV2                     ((uint32_t) 0x00000002)
#define SCB_SYSAHBCLKDIV_DIV4                     ((uint32_t) 0x00000004)
#define SCB_SYSAHBCLKDIV_MASK                     ((uint32_t) 0x000000FF)	// AHB clock divider can be from 0 to 255

/*  AHBCLKCTRL (System AHB clock control register)
    The AHBCLKCTRL register enables the clocks to individual system and peripheral blocks.
    The system clock (sys_ahb_clk[0], bit 0 in the AHBCLKCTRL register) provides the clock
    for the AHB to APB bridge, the AHB matrix, the ARM Cortex-M3, the Syscon block, and
    the PMU. This clock cannot be disabled. */

#define SCB_SYSAHBCLKCTRL_SYS                     ((uint32_t) 0x00000001)	// Enables clock for AHB and APB bridges, FCLK, HCLK, SysCon and PMU
#define SCB_SYSAHBCLKCTRL_SYS_MASK                ((uint32_t) 0x00000001)
#define SCB_SYSAHBCLKCTRL_ROM                     ((uint32_t) 0x00000002)	// Enables clock for ROM
#define SCB_SYSAHBCLKCTRL_ROM_MASK                ((uint32_t) 0x00000002)
#define SCB_SYSAHBCLKCTRL_RAM                     ((uint32_t) 0x00000004)	// Enables clock for SRAM
#define SCB_SYSAHBCLKCTRL_RAM_MASK                ((uint32_t) 0x00000004)
#define SCB_SYSAHBCLKCTRL_FLASH1                  ((uint32_t) 0x00000008)	// Enables clock for flash1
#define SCB_SYSAHBCLKCTRL_FLASH1_MASK             ((uint32_t) 0x00000008)
#define SCB_SYSAHBCLKCTRL_FLASH2                  ((uint32_t) 0x00000010)	// Enables clock for flash2
#define SCB_SYSAHBCLKCTRL_FLASH2_MASK             ((uint32_t) 0x00000010)
#define SCB_SYSAHBCLKCTRL_I2C                     ((uint32_t) 0x00000020)	// Enables clock for I2C
#define SCB_SYSAHBCLKCTRL_I2C_MASK                ((uint32_t) 0x00000020)
#define SCB_SYSAHBCLKCTRL_GPIO                    ((uint32_t) 0x00000040)	// Enables clock for GPIO
#define SCB_SYSAHBCLKCTRL_GPIO_MASK               ((uint32_t) 0x00000040)
#define SCB_SYSAHBCLKCTRL_CT16B0                  ((uint32_t) 0x00000080)	// Enables clock for 16-bit counter/timer 0
#define SCB_SYSAHBCLKCTRL_CT16B0_MASK             ((uint32_t) 0x00000080)
#define SCB_SYSAHBCLKCTRL_CT16B1                  ((uint32_t) 0x00000100)	// Enables clock for 16-bit counter/timer 1
#define SCB_SYSAHBCLKCTRL_CT16B1_MASK             ((uint32_t) 0x00000100)
#define SCB_SYSAHBCLKCTRL_CT32B0                  ((uint32_t) 0x00000200)	// Enables clock for 32-bit counter/timer 0
#define SCB_SYSAHBCLKCTRL_CT32B0_MASK             ((uint32_t) 0x00000200)
#define SCB_SYSAHBCLKCTRL_CT32B1                  ((uint32_t) 0x00000400)	// Enables clock for 32-bit counter/timer 1
#define SCB_SYSAHBCLKCTRL_CT32B1_MASK             ((uint32_t) 0x00000400)
#define SCB_SYSAHBCLKCTRL_SSP0                    ((uint32_t) 0x00000800)	// Enables clock for SSP0
#define SCB_SYSAHBCLKCTRL_SSP0_MASK               ((uint32_t) 0x00000800)
#define SCB_SYSAHBCLKCTRL_UART                    ((uint32_t) 0x00001000)	// Enables clock for UART.  UART pins must be configured
#define SCB_SYSAHBCLKCTRL_UART_MASK               ((uint32_t) 0x00001000)	// in the IOCON block before the UART clock can be enabled.
#define SCB_SYSAHBCLKCTRL_ADC                     ((uint32_t) 0x00002000)	// Enables clock for ADC
#define SCB_SYSAHBCLKCTRL_ADC_MASK                ((uint32_t) 0x00002000)
#define SCB_SYSAHBCLKCTRL_USB_REG                 ((uint32_t) 0x00004000)	// Enables clock for USB_REG
#define SCB_SYSAHBCLKCTRL_USB_REG_MASK            ((uint32_t) 0x00004000)
#define SCB_SYSAHBCLKCTRL_WDT                     ((uint32_t) 0x00008000)	// Enables clock for watchdog timer
#define SCB_SYSAHBCLKCTRL_WDT_MASK                ((uint32_t) 0x00008000)
#define SCB_SYSAHBCLKCTRL_IOCON                   ((uint32_t) 0x00010000)	// Enables clock for IO configuration block
#define SCB_SYSAHBCLKCTRL_IOCON_MASK              ((uint32_t) 0x00010000)
#define SCB_SYSAHBCLKCTRL_ALL_MASK                ((uint32_t) 0x0001FFFF)

/*!
 * @brief Indicates the value for the PLL multiplier
 */
typedef enum {
	CPU_MULTIPLIER_1 = 0,
	CPU_MULTIPLIER_2,
	CPU_MULTIPLIER_3,
	CPU_MULTIPLIER_4,
	CPU_MULTIPLIER_5,
	CPU_MULTIPLIER_6
} cpuMultiplier_t;

static void CPU_Setup_PLL (cpuMultiplier_t multiplier) {
	uint32_t i;

	/* Power up system oscillator */
	SCB_PDRUNCFG &= ~(SCB_PDRUNCFG_SYSOSC_MASK);

	// Setup the crystal input (bypass disabled, 1-20MHz crystal)
	SCB_SYSOSCCTRL = (SCB_SYSOSCCTRL_BYPASS_DISABLED | SCB_SYSOSCCTRL_FREQRANGE_1TO20MHZ);

	for (i = 0; i < 200; i++) {
		__asm volatile ("NOP");
	}

	// Configure PLL
	SCB_PLLCLKSEL = SCB_CLKSEL_SOURCE_MAINOSC;	// Select external crystal as PLL clock source
	SCB_PLLCLKUEN = SCB_PLLCLKUEN_UPDATE;		// Update clock source
	SCB_PLLCLKUEN = SCB_PLLCLKUEN_DISABLE;		// Toggle update register once
	SCB_PLLCLKUEN = SCB_PLLCLKUEN_UPDATE;		// Update clock source again

	// Wait until the clock is updated
	while (!(SCB_PLLCLKUEN & SCB_PLLCLKUEN_UPDATE));

	// Set clock speed
	switch (multiplier) {
		case CPU_MULTIPLIER_2:
			SCB_PLLCTRL = (SCB_PLLCTRL_MULT_2 | (1 << SCB_PLLCTRL_DIV_BIT));
			break;
		case CPU_MULTIPLIER_3:
			SCB_PLLCTRL = (SCB_PLLCTRL_MULT_3 | (1 << SCB_PLLCTRL_DIV_BIT));
			break;
		case CPU_MULTIPLIER_4:
			SCB_PLLCTRL = (SCB_PLLCTRL_MULT_4 | (1 << SCB_PLLCTRL_DIV_BIT));
			break;
		case CPU_MULTIPLIER_5:
			SCB_PLLCTRL = (SCB_PLLCTRL_MULT_5 | (1 << SCB_PLLCTRL_DIV_BIT));
			break;
		case CPU_MULTIPLIER_6:
			SCB_PLLCTRL = (SCB_PLLCTRL_MULT_6 | (1 << SCB_PLLCTRL_DIV_BIT));
			break;
		case CPU_MULTIPLIER_1:
			default:
		SCB_PLLCTRL = (SCB_PLLCTRL_MULT_1 | (1 << SCB_PLLCTRL_DIV_BIT));
			break;
	}

	// Enable system PLL
	SCB_PDRUNCFG &= ~(SCB_PDRUNCFG_SYSPLL_MASK);

	// Wait for PLL to lock
	while (!(SCB_PLLSTAT & SCB_PLLSTAT_LOCK));

	// Setup main clock (use PLL output)
	SCB_MAINCLKSEL = SCB_MAINCLKSEL_SOURCE_SYSPLLCLKOUT;
	SCB_MAINCLKUEN = SCB_MAINCLKUEN_UPDATE;		// Update clock source
	SCB_MAINCLKUEN = SCB_MAINCLKUEN_DISABLE;	// Toggle update register once
	SCB_MAINCLKUEN = SCB_MAINCLKUEN_UPDATE;

	// Wait until the clock is updated
	while (!(SCB_MAINCLKUEN & SCB_MAINCLKUEN_UPDATE));

	// Disable USB clock by default (enabled in USB code)
	SCB_PDRUNCFG |= (SCB_PDSLEEPCFG_USBPAD_PD);	// Power-down USB PHY
	SCB_PDRUNCFG |= (SCB_PDSLEEPCFG_USBPLL_PD);	// Power-down USB PLL

	// Set system AHB clock
	// クロックの分周比を1/1に設定
	SCB_SYSAHBCLKDIV = SCB_SYSAHBCLKDIV_DIV1;

	// Enabled IOCON clock for I/O related peripherals
	// 周辺ＩＯのクロックを設定
	SCB_SYSAHBCLKCTRL |= SCB_SYSAHBCLKCTRL_IOCON;
}

void CPU_Initialize(void) {
	/* Initializes GPIO */
	GPIO_Initialize();

	/* Setup PLL (etc.) */
	CPU_Setup_PLL(CPU_MULTIPLIER_6);
}


/* TODO: Implement context switch. */

