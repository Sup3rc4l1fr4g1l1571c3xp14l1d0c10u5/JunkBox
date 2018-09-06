//
//	2001/02/12 18:10:50
//	$Date: 2006-12-18 13:49:34 -0800 (Mon, 18 Dec 2006) $
//	$Revision: 1752 $
//
#ifndef _ASEARCH_H_
#define _ASEARCH_H_

#include "../pico/pico.utf16.h"

#define MAXCHAR (1UL << (sizeof(char16_t)*8))

typedef struct {
	sint32_t mismatch;
	uint32_t epsilon;
	uint32_t acceptpat;
	uint32_t shiftpat[MAXCHAR];	// ‚P•¶Žš‚É‘Î‚µ‚Ä1ƒrƒbƒgŠ„‚è“–‚Ä‚é‚Ì‚Åunicode1.0”ÍˆÍ‚¾‚Æ256KbyteÁ”ï
} asearch_context_t;

enum
{
	PAT_GROUP_START = 0x0003,
	PAT_GROUP_END = 0x0004,
	PAT_WILDCARD = 0x0020,
};

#define MAXMISMATCH 3

extern void asearch_makepat(asearch_context_t *ctx, const char16_t *pattern, sint32_t ambig);
extern uint32_t asearch_match(asearch_context_t *ctx, const char16_t *text);

#endif
