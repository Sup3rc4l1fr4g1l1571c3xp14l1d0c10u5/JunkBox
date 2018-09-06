#pragma once

#include "../pico/pico.utf16.h"

#define MAXWORDLEN 200

//typedef int POBOX_INT;

typedef enum {
	POBOXDICT_TEXT,		// テキスト型式(非圧縮/交換型式)
	POBOXDICT_LOOKUP,	// トライ型式
} POBoxDictType;

typedef struct {
	const char16_t *name;
	const POBoxDictType type;
	const bool_t readonly;
} POBoxDict;

// 検索モードの指定
//typedef enum
//{
//	POBOX_EXACT = 1,
//	POBOX_APPROXIMATE = 2
//} pobox_searchmode_t;

extern bool_t pobox_init(void);
extern bool_t pobox_usedict(const POBoxDict *pbdict);
extern bool_t pobox_save(const POBoxDict *pbdict);
extern bool_t pobox_regword(const POBoxDict *pbdict, char16_t *word, char16_t *pat);
extern bool_t pobox_delword(const POBoxDict *pbdict, char16_t *word, char16_t *pat);
extern size32_t pobox_getdictlist(const POBoxDict *dictlist[], size32_t limit);
extern bool_t pobox_regent(const POBoxDict *pbdict, size32_t nth, char16_t *word, char16_t *pat);
//extern void pobox_searchmode(pobox_searchmode_t mode);
extern size32_t pobox_search(const char16_t *pat, char16_t **words, size32_t wordsLength, size32_t wordBufLengthInWord, size32_t skip);
