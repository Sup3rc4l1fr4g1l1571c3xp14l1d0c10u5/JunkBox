/*
 * gpio.h
 *
 *  Created on: 2010/09/16
 *      Author: whelp
 */

#ifndef GPIO_H_
#define GPIO_H_

#include "../../../src/type.h"

typedef enum gpioDirection_e {
	gpioDirection_Input = 0,
	gpioDirection_Output
} gpioDirection_t;

extern void GPIO_Initialize (void);
extern void GPIO_SetDir (uint32_t portNum, uint32_t bitPos, gpioDirection_t dir);
extern void GPIO_SetValue (uint32_t portNum, uint32_t bitPos, uint32_t bitVal);

#endif /* GPIO_H_ */
