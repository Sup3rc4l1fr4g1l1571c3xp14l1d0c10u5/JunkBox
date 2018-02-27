/*
 * cpu.h
 *
 *  Created on: 2010/09/16
 *      Author: whelp
 */

#ifndef CPU_H_
#define CPU_H_

static inline void __enable_irq(void)	{ __asm volatile ("cpsie i"); }
static inline void __disable_irq(void)	{ __asm volatile ("cpsid i"); }

extern void CPU_Initialize(void);

#endif /* CPU_H_ */
