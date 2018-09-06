#pragma once

#if defined(_WIN32)
# include "./win32/arch.h"
#elif defined(__arm__)
# if defined(STM32F429_439xx)
#  include "./arm/STM32F429Discovery/arch.h"
# endif
#endif
