#pragma once

typedef unsigned char bool_t;
#ifndef __cplusplus
#define false (0)
#define true (1)
#endif

typedef   signed char  sint8_t;
typedef   signed short sint16_t;
typedef   signed int   sint32_t;
#ifndef __stdint_h
typedef unsigned char  uint8_t;
typedef unsigned short uint16_t;
typedef unsigned int   uint32_t;
#endif

typedef unsigned short char16_t;

typedef unsigned long size32_t;

typedef void* ptr_t;

#ifndef NULL
# define NULL ((ptr_t)0)
#endif
