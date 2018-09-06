//	トライを目次に使用する単純な辞書検索プログラム

#include "../pico/pico.memory.h"
#include "../pico/pico.utf16.h"
#include "lookup.h"

// トライノード
typedef struct _TrieNode {
	struct _TrieNode *next;  // 兄弟ノード
	struct _TrieNode *child; // 子ノード
	char16_t c;
	size32_t val;
} TrieNode;

#define TRIE_LEVEL 3
static int nodenum = 0;

//
// トライのノードをnodeppに作成する
//
static void addnode(TrieNode **nodepp, const char16_t *s, size32_t val)
{
	if (!nodepp || !*s) {
		// 登録先がない場合、もしくは終端文字到達の場合
		return;
	}

	TrieNode *p = (*nodepp);
	if (p == NULL) {
		// ノードが未登録の場合は新しく作成する
		p = (TrieNode*)Memory.Allocate(sizeof(TrieNode));
		nodenum++;
		p->c = *s;
		p->child = p->next = NULL;
		p->val = val;
		(*nodepp) = p;

		addnode(&(p->child), s + 1, val);
		return;
	}
	else if (p->c == *s) {
		// ノードが登録済みの場合でエッジが一致する場合は辿る
		addnode(&(p->child), s + 1, val);
		return;
	}
	else {
		// ノードが登録済みの場合でエッジが一致しない場合はリンクリストの次を見る。
		addnode(&(p->next), s, val);
		return;
	}
}

static bool_t getval(const TrieNode *nodep, const char16_t *s, size32_t *ret) {
	for (;;) {
		if (*s == nodep->c) {
			char16_t c = *(s + 1);
			// 入力文字の次の一文字がセパレータでないならさらにもう1文字読む
			if ((c != L'\0' && c != L'\t' && c != L'\n' && c != L'\r') && nodep->child != NULL) {
				nodep = nodep->child;
				s = s + 1;
				continue;
			}
			else {
				*ret = nodep->val;
				return true;
			}
		}
		else if (nodep->next) {
			nodep = nodep->next;
			s = s;
			continue;
		}
		else {
			*ret = 0;
			return false;
		}
	}
}

// 辞書からトライ木を作る
bool_t lookup_new(Lookup *l, const char16_t *dictfile)
{
	char16_t buf[LOOPUP_MAX_LINE_SIZE], prev[LOOPUP_MAX_LINE_SIZE];

	ptr_t f = File.Open(dictfile, FileMode_Open, FileAccess_Read);
	if (f == NULL) {
		return false;
	}

	l->dict = f;
	l->root = NULL;

	Memory.Fill.Uint16(prev, 0, LOOPUP_MAX_LINE_SIZE);

	size32_t pos = 0;
	while (!File.EoF(f)) {
		if (Utf16.ReadLine(buf, LOOPUP_MAX_LINE_SIZE, f) == 0)
		{
			continue;
		}
		char16_t *p = Utf16.FindAny(buf, L"\t\r\n");
		*p = L'\0';
		buf[TRIE_LEVEL] = L'\0';

		if (Utf16.NCompare(buf, prev, TRIE_LEVEL) != 0) {
			addnode(&(l->root), buf, pos);
			Utf16.NCopy(prev, buf, TRIE_LEVEL);
		}
		File.Position(f, &pos);
	}
	return true;
}

// トライ木からサーチ
int lookup_search(Lookup *loopup, const char16_t *pat) {
	if (!loopup || !pat || !(*pat)) return 0;
	Utf16.Copy(loopup->pat, pat);

	size32_t pos;
	if (getval(loopup->root, pat, &pos) == false) {
		return 0;
	}
	File.Seek(loopup->dict, pos, SeekOrigin_Begin);
	return pos;
}

bool_t lookup_get_next_line(const Lookup *l, char16_t *buf, size32_t bufSize) {
	while (!File.EoF(l->dict)) {
		if (Utf16.ReadLine(buf, bufSize, l->dict) == 0)
		{
			continue;
		}
		int cmp = Utf16.NCompare(buf, l->pat, Utf16.Length(l->pat));
		if (cmp == 0) return true;
		if (cmp < 0) continue;
		if (cmp > 0) break;
	}
	return false;
}

int lookup_destroy(Lookup *l) {
	if (l) {
		if (l->dict) {
			File.Close(l->dict);
			l->dict = NULL;
		}
	}
	return 1;
}
