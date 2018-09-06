//	�g���C��ڎ��Ɏg�p����P���Ȏ��������v���O����

#include "../pico/pico.memory.h"
#include "../pico/pico.utf16.h"
#include "lookup.h"

// �g���C�m�[�h
typedef struct _TrieNode {
	struct _TrieNode *next;  // �Z��m�[�h
	struct _TrieNode *child; // �q�m�[�h
	char16_t c;
	size32_t val;
} TrieNode;

#define TRIE_LEVEL 3
static int nodenum = 0;

//
// �g���C�̃m�[�h��nodepp�ɍ쐬����
//
static void addnode(TrieNode **nodepp, const char16_t *s, size32_t val)
{
	if (!nodepp || !*s) {
		// �o�^�悪�Ȃ��ꍇ�A�������͏I�[�������B�̏ꍇ
		return;
	}

	TrieNode *p = (*nodepp);
	if (p == NULL) {
		// �m�[�h�����o�^�̏ꍇ�͐V�����쐬����
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
		// �m�[�h���o�^�ς݂̏ꍇ�ŃG�b�W����v����ꍇ�͒H��
		addnode(&(p->child), s + 1, val);
		return;
	}
	else {
		// �m�[�h���o�^�ς݂̏ꍇ�ŃG�b�W����v���Ȃ��ꍇ�̓����N���X�g�̎�������B
		addnode(&(p->next), s, val);
		return;
	}
}

static bool_t getval(const TrieNode *nodep, const char16_t *s, size32_t *ret) {
	for (;;) {
		if (*s == nodep->c) {
			char16_t c = *(s + 1);
			// ���͕����̎��̈ꕶ�����Z�p���[�^�łȂ��Ȃ炳��ɂ���1�����ǂ�
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

// ��������g���C�؂����
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

// �g���C�؂���T�[�`
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
