//
//	$Date: 2001/07/23 15:01:58 $
//	$Revision: 1.2 $
//

#ifndef _LOOKUP_H_
#define _LOOKUP_H_

#include "ime.h"
#include "../pico/pico.file.h"

#define LOOPUP_MAX_LINE_SIZE 64

typedef struct _TrieNode TrieNode;

typedef struct {
	ptr_t		*dict;
	TrieNode	*root;
	char16_t	pat[LOOPUP_MAX_LINE_SIZE];
} Lookup;

bool_t lookup_new(Lookup *l, const char16_t *dictfile);
int lookup_search(Lookup *loopup, const char16_t *pat);
bool_t lookup_get_next_line(const Lookup *l, char16_t *buf, size32_t bufSize);
int lookup_destroy(Lookup *l);

#endif
