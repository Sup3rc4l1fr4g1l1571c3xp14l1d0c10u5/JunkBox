/*
 * gpio.c
 *
 *  Created on: 2010/09/16
 *      Author: whelp
 */

#include "../../../src/type.h"
#include "nvic.h"
#include "gpio.h"

typedef uint32_t* preg32_t;

/* System AHB clock control register */
#define SCB_SYSAHBCLKCTRL		(*(preg32_t)0x40048080UL)
#define SCB_SYSAHBCLKCTRL_GPIO	( (uint32_t)0x00000040UL)	// Enables clock for GPIO

#define GPIO_GPIO0DIR                             (*(preg32_t)(0x50008000UL))    // Data direction register
#define GPIO_GPIO1DIR                             (*(preg32_t)(0x50018000UL))    // Data direction register
#define GPIO_GPIO2DIR                             (*(preg32_t)(0x50028000UL))    // Data direction register
#define GPIO_GPIO3DIR                             (*(preg32_t)(0x50038000UL))    // Data direction register

#define GPIO_GPIO0DATA                            (*(preg32_t)(0x50003FFCUL))    // Port data register
#define GPIO_GPIO1DATA                            (*(preg32_t)(0x50013FFCUL))    // Port data register
#define GPIO_GPIO2DATA                            (*(preg32_t)(0x50023FFCUL))    // Port data register
#define GPIO_GPIO3DATA                            (*(preg32_t)(0x50033FFCUL))    // Port data register

#define GPIO_IO_P0                                ((uint32_t) 0x00000001UL)
#define GPIO_IO_P1                                ((uint32_t) 0x00000002UL)
#define GPIO_IO_P2                                ((uint32_t) 0x00000004UL)
#define GPIO_IO_P3                                ((uint32_t) 0x00000008UL)
#define GPIO_IO_P4                                ((uint32_t) 0x00000010UL)
#define GPIO_IO_P5                                ((uint32_t) 0x00000020UL)
#define GPIO_IO_P6                                ((uint32_t) 0x00000040UL)
#define GPIO_IO_P7                                ((uint32_t) 0x00000080UL)
#define GPIO_IO_P8                                ((uint32_t) 0x00000100UL)
#define GPIO_IO_P9                                ((uint32_t) 0x00000200UL)
#define GPIO_IO_P10                               ((uint32_t) 0x00000400UL)
#define GPIO_IO_P11                               ((uint32_t) 0x00000800UL)
#define GPIO_IO_ALL                               ((uint32_t) 0x00000FFFUL)

/* Enable AHB clock to the GPIO domain. */
void GPIO_Enable_AHB_Clock(void) {
	SCB_SYSAHBCLKCTRL |= (SCB_SYSAHBCLKCTRL_GPIO);
}

/* Enable AHB clock to the GPIO domain. */
void GPIO_Disable_AHB_Clock(void) {
	SCB_SYSAHBCLKCTRL &= ~(SCB_SYSAHBCLKCTRL_GPIO);
}

void GPIO_Initialize (void) {

	/* Set up NVIC when I/O pins are configured as external interrupts. */
	NVIC_EnableIRQ(EINT0_IRQn);
	NVIC_EnableIRQ(EINT1_IRQn);
	NVIC_EnableIRQ(EINT2_IRQn);
	NVIC_EnableIRQ(EINT3_IRQn);

	/* Set all GPIO pins to input by default */
	GPIO_GPIO0DIR &= ~(GPIO_IO_ALL);
	GPIO_GPIO1DIR &= ~(GPIO_IO_ALL);
	GPIO_GPIO2DIR &= ~(GPIO_IO_ALL);
	GPIO_GPIO3DIR &= ~(GPIO_IO_ALL);

	return;
}

/**************************************************************************/
/*!
    @brief Sets the direction (input/output) for a specific port pin

    @param[in]  portNum
                The port number (0..3)
    @param[in]  bitPos
                The bit position (0..11)
    @param[in]  dir
                The pin direction (gpioDirection_Input or
                gpioDirection_Output)
*/
/**************************************************************************/
void GPIO_SetDir (uint32_t portNum, uint32_t bitPos, gpioDirection_t dir) {
  switch (portNum) {
    case 0:
      if (gpioDirection_Output == dir) {
        GPIO_GPIO0DIR |= (1 << bitPos);
      } else {
        GPIO_GPIO0DIR &= ~(1 << bitPos);
      }
      break;
    case 1:
      if (gpioDirection_Output == dir) {
        GPIO_GPIO1DIR |= (1 << bitPos);
      } else {
        GPIO_GPIO1DIR &= ~(1 << bitPos);
      }
      break;
    case 2:
      if (gpioDirection_Output == dir) {
        GPIO_GPIO2DIR |= (1 << bitPos);
      } else {
        GPIO_GPIO2DIR &= ~(1 << bitPos);
      }
      break;
    case 3:
      if (gpioDirection_Output == dir) {
        GPIO_GPIO3DIR |= (1 << bitPos);
      } else {
        GPIO_GPIO3DIR &= ~(1 << bitPos);
      }
      break;
  }
}

/**************************************************************************/
/*!
    @brief Sets the value for a specific port pin (only relevant when a
           pin is configured as output).

    @param[in]  portNum
                The port number (0..3)
    @param[in]  bitPos
                The bit position (0..31)
    @param[in]  bitValue
                The value to set for the specified bit (0..1).  0 will set
                the pin low and 1 will set the pin high.
*/
/**************************************************************************/
void GPIO_SetValue (uint32_t portNum, uint32_t bitPos, uint32_t bitVal)
{
  switch (portNum)
  {
    case 0:
      if (1 == bitVal)
      {
        GPIO_GPIO0DATA |= (1 << bitPos);
      }
      else
      {
        GPIO_GPIO0DATA &= ~(1 << bitPos);
      }
      break;
    case 1:
      if (1 == bitVal)
      {
        GPIO_GPIO1DATA |= (1 << bitPos);
      }
      else
      {
        GPIO_GPIO1DATA &= ~(1 << bitPos);
      }
      break;
    case 2:
      if (1 == bitVal)
      {
        GPIO_GPIO2DATA |= (1 << bitPos);
      }
      else
      {
        GPIO_GPIO2DATA &= ~(1 << bitPos);
      }
      break;
    case 3:
      if (1 == bitVal)
      {
        GPIO_GPIO3DATA |= (1 << bitPos);
      }
      else
      {
        GPIO_GPIO3DATA &= ~(1 << bitPos);
      }
      break;
    default:
      break;
  }
}
