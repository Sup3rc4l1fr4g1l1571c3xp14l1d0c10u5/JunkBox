#pragma once

#include "../pico/pico.types.h"
#include "font.h"

//240x216(NTSC) -> 30x27
//320x240(QVGA) -> 40x30
#define WIDTH_X		(640/FONT_WIDTH)	//(22) // ‰¡•ûŒü•¶š”
#define WIDTH_Y		(480/FONT_HEIGHT)	//(17) // c•ûŒü•¶š”

#define VWIDTH_X	(WIDTH_X * 2) // ”¼Šp•¶š‚ğ•‚P‘SŠp•¶š‚ğ•‚Q‚Æ‚µ‚½Û‚ÌVRAM‚Ì‰¡•ûŒü•¶š•”

typedef struct {
	char16_t	ch;
	uint8_t		color;
	uint8_t		bgcolor;
} text_vram_pixel_t;

typedef union {
	uint32_t	bgra;
	struct {
		uint8_t b;
		uint8_t g;
		uint8_t r;
		uint8_t a;
	} s;
} color_t;

typedef struct {
	int X, Y;
} screen_pos_t;

extern const struct TextVRAM {
	void				(*ClearScreen)(void);
	void				(*SetPaletteColor)(unsigned char index, unsigned char b, unsigned char r, unsigned char g);
	void				(*Initialize)(void);
	text_vram_pixel_t *	(*GetCursorPtr)(void);
	void				(*GetCursorPosition)(screen_pos_t *pos);
	void				(*SetCursorPosition)(const screen_pos_t *pos);
	text_vram_pixel_t *	(*GetVramPtr)(void);
	const color_t *		(*GetPalettePtr)(void);
	uint8_t				(*GetBackgroundColor)(void);
	void				(*SetBackgroundColor)(uint8_t);
	void				(*FillBackgroundColor)(uint8_t x, uint8_t y, uint16_t length, uint8_t palette);
	uint8_t				(*GetTextColor)(void);
	void				(*SetTextColor)(uint8_t c);
	void				(*FillTextColor)(uint8_t x, uint8_t y, uint16_t length, uint8_t palette);
	void				(*Scroll)(void);
	void				(*putch)(char16_t n);
	void				(*puts)(const char16_t *s);
	void				(*putdigit)(uint32_t n);
	void				(*putdigit2)(uint32_t n, uint8_t e);
	void				(*CalcCursorPosition)(uint16_t *head, uint16_t *cur, screen_pos_t *p, size32_t width);
} TextVRAM;
