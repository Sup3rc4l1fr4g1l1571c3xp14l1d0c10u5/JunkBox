#pragma once

#include "../pico/pico.types.h"

#define FONT_DATA_BYTE_SIZE	29
#define FONT_WIDTH			14
#define FONT_HEIGHT			14

#if !defined(char16_t)
typedef unsigned short char16_t;
#endif

typedef struct {
	unsigned short id;
} fontid_t;

typedef uint8_t		FontBmp_T[29];
typedef FontBmp_T	FontData_T[23268];

extern const FontData_T FontData;
extern fontid_t	Uni2Id(char16_t ch);
extern char16_t	Id2Uni(fontid_t id);
extern uint8_t	UniWidth(char16_t ch);
extern uint8_t	UniWidth1(char16_t ch);
