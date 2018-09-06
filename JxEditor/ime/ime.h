#pragma once

#include "../pico/pico.utf16.h"
#include "TinyPobox.h"

typedef struct {
	const uint8_t    *tree;
	const size32_t    tree_len;
	const uint8_t    *accept;
	const size32_t    accept_len;
	const char16_t   *edges;
	const size32_t    edges_len;
	const char16_t  **values;
	const size32_t    values_len;
} LoudsData;

#define IME_BUF_MAX 64

typedef struct {
	const LoudsData *data;
	char16_t output[IME_BUF_MAX];
	char16_t current[IME_BUF_MAX];
	int currentlen;

	int pobox_search_next;
	char16_t pobox_output[IME_BUF_MAX];
} IME_Context;

extern void IME_Reset(IME_Context *work);
extern bool_t IME_Update(IME_Context *work, char16_t ch);
extern bool_t IME_Initialize(void);
