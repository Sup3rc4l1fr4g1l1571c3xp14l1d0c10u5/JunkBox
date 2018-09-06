#pragma once

#include "../../../pico/pico.types.h"

extern void WaitMS(uint32_t n);
extern void DisableInterrupt(void);
extern void EnableInterrupt(void);

#define USE_DYNTAMIC_AREA

