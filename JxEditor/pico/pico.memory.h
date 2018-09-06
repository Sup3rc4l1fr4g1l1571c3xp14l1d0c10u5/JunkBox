#pragma once

#include "pico.types.h"

extern const struct PicoMemoryApi
{
	void	(*Initialize)(void);

	ptr_t	(*Allocate)(size32_t sz);
	void	(*Free)(ptr_t ptr);
	ptr_t	(*Resize)(ptr_t ptr, size32_t sz);

	void	(*Copy)(ptr_t dest, const ptr_t src, size32_t sz);
	struct {
		void (*Uint8)(uint8_t *dest, uint8_t value, size32_t len);
		void (*Uint16)(uint16_t *dest, uint16_t value, size32_t len);
		void (*Uint32)(uint32_t *dest, uint32_t value, size32_t len);
	} Fill;
} Memory;
