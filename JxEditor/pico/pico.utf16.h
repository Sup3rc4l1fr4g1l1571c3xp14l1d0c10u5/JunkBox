#pragma once

#include "pico.types.h"

typedef uint16_t char16_t;

extern const struct PicoUtf16Api
{
	char16_t*		(*Dupulicate)(const char16_t *str);
	void			(*Copy)(char16_t *dst, const char16_t *src);
	void			(*NCopy)(char16_t *dst, const char16_t *src, size32_t length);
	size32_t		(*Length)(const char16_t *src);
	int				(*Compare)(const char16_t *s1, const char16_t *s2);
	int				(*NCompare)(const char16_t *s1, const char16_t *s2, size32_t length);
	char16_t*		(*Find)(char16_t *str, char16_t ch);
	char16_t*		(*FindAny)(char16_t *str, const char16_t *subs);

	size32_t		(*ReadLine)(char16_t *buf, size32_t sizeInWord, ptr_t fp);
} Utf16;


