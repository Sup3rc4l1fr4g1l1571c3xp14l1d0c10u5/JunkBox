// Pobox�x�[�X�̂��Ȋ����ϊ�

#include "TinyPobox.h"
#include "lookup.h"
#include "asearch.h"
#include "../pico/pico.utf16.h"
#include "../pico/pico.memory.h"

#define FIND_PATTERN_MAX 31

// �����G���g��
typedef struct {
	char16_t *pat;
	char16_t *word;
} DicEntry;

typedef struct {
	const POBoxDict *poboxdict;
	// POBoxDictType = POBOXDICT_TEXT�̎��Ɏg���w�K�����̂��߂̃G���g��
	DicEntry	*entry;	// �����f�[�^
	size32_t		nentries;		// �����̃G���g����
	size32_t		maxentries;		// �����T�C�Y�B����Ȃ��Ȃ��malloc()	�ő��₷
	// POBoxDictType = POBOXDICT_LOOKUP�̎��Ɏg��Trie�����̂��߂̃G���g��
	Lookup		lookup;
} Dict;

#define MAXDICTIONARIES 3
static Dict dictionaries[MAXDICTIONARIES];
static int ndictionaries = 0;	// ������

static Dict *FindDictionary(const POBoxDict *pbdict)
{
	for (int i = 0; i < ndictionaries; i++) {
		if (dictionaries[i].poboxdict == pbdict)
		{
			return &dictionaries[i];
		}
	}
	return NULL;
}

//
// �o�^�P�ꐔ�̎擾
//
bool_t pobox_entries(const POBoxDict *pbdict, size32_t *ret)
{
	if (pbdict->type != POBOXDICT_TEXT)
	{
		return false;
	}
	Dict *dict = FindDictionary(pbdict);
	if (dict)
	{
		*ret = dict->nentries;
		return true;
	} else {
		*ret = 0;
		return false;
	}
}

//
// n�Ԗڂ̃G���g���ɒP���o�^
//
bool_t pobox_regent(const POBoxDict *pbdict, size32_t nth, char16_t *word, char16_t *pat)
{
	if (pbdict->type != POBOXDICT_TEXT)
	{
		// TEXT�`���ȊO�ɂ͒ǉ��ł��Ȃ�
		return false;
	}

	Dict *dict = FindDictionary(pbdict);
	if (dict == NULL) {
		// �����͓o�^����Ă��Ȃ�
		return false;
	}

	if (nth >= dict->maxentries) {
		// �����̈悪����Ȃ��Ȃ�ƃG���g�����g������
		int newsize = (nth + 15) & 0xFFFFFFF0UL;

		DicEntry *d = (DicEntry*)Memory.Resize(dict->entry, newsize * sizeof(DicEntry));
		if (d == NULL) {
			return false;
		}
		Memory.Fill.Uint8((uint8_t*)&d[nth], 0, (newsize - nth) * sizeof(DicEntry));
		dict->entry = d;
		dict->maxentries = newsize;
	}

	// �V�����G���g����o�^�i�㏑���j
	if (dict->entry[nth].word != NULL)
	{
		Memory.Free(dict->entry[nth].word);
	}
	dict->entry[nth].word = Utf16.Dupulicate(word);
	if (dict->entry[nth].pat != NULL)
	{
		Memory.Free(dict->entry[nth].pat);
	}
	dict->entry[nth].pat = Utf16.Dupulicate(pat);

	if (nth >= dict->nentries) {
		dict->nentries = nth + 1;
	}

	return true;
}

//
// n�Ԗڂ̃G���g���P����擾
//
bool_t pobox_getent(POBoxDict *pbdict, size32_t nth, char16_t *word, char16_t *pat)
{

	if (pbdict->type != POBOXDICT_TEXT)
	{
		return false;
	}
	Dict *dict = FindDictionary(pbdict);
	if (dict == NULL) {
		return false;
	}

	if (nth >= dict->nentries) {
		return false;
	}

	if (dict->entry[nth].word) {
		Utf16.Copy((char16_t*)word, (char16_t*)(dict->entry[nth].word));
	}
	if (dict->entry[nth].pat) {
		Utf16.Copy((char16_t*)pat, (char16_t*)(dict->entry[nth].pat));
	}
	return true;
}

//
// n�Ԗڂ̓o�^�P��̍폜
//
bool_t pobox_delent(POBoxDict *pbdict, size32_t nth)
{
	Dict *dict;

	if (pbdict->type != POBOXDICT_TEXT) {
		return false;
	}
	dict = FindDictionary(pbdict);
	if (dict == NULL) {
		return false;
	}

	if (nth >= dict->nentries) {
		return false;
	}

	if (dict->entry[nth].word) {
		Memory.Free(dict->entry[nth].word);
	}
	if (dict->entry[nth].pat) {
		Memory.Free(dict->entry[nth].pat);
	}
	for (size32_t i = nth; i < dict->nentries - 1; i++) {
		dict->entry[i] = dict->entry[i + 1];
	}
	dict->nentries--;

	return true;
}

//
// �g�p���鎫���̎w��
//
bool_t pobox_usedict(const POBoxDict *pbdict)
{
	int i;
	int nwords = 0;

	if (pbdict == NULL) {
		return false;
	}
	for (i = 0; i < ndictionaries; i++) {
		if (dictionaries[i].poboxdict == pbdict) {
			break;
		}
	}
	if (ndictionaries >= MAXDICTIONARIES - 1) {
		return false;
	}

	if (pbdict->type == POBOXDICT_TEXT) {
		if (i >= ndictionaries) {
			Dict *dict = &(dictionaries[i]);
			dict->entry = NULL;
			dict->nentries = 0;
			dict->maxentries = 0;
			dict->poboxdict = pbdict;
			ptr_t f = File.Open(pbdict->name, FileMode_Open, FileAccess_Read);
			if (f == NULL) {
				return false;
			}
			ndictionaries++;
			nwords = 0;

			while (!File.EoF(f)) {
				char16_t buf[FIND_PATTERN_MAX+1];
				if (Utf16.ReadLine(buf, sizeof(buf) / sizeof(buf[0]), f) == 0)
				{
					continue;
				}
				if (buf[0] == L'#') continue;
				char16_t *p = buf;
				char16_t *w = buf;
				while (*w != L'\0')
				{
					if (*w == L'\t') { break; }
					w++;
				}

				if (*w == L'\t') {
					*w++ = 0x0000;
					pobox_regent(pbdict, nwords, w, p);
					nwords++;
				}
			}
			File.Close(f);
		}
	}
	else if (pbdict->type == POBOXDICT_LOOKUP) {
		if (i >= ndictionaries) {
			Dict *dict = &(dictionaries[i]);
			dict->poboxdict = pbdict;
			if (lookup_new(&(dict->lookup), pbdict->name) == false) { return false; }
			ndictionaries++;
		}
	}
	else {
		return false;
	}
	return true;
}

//
// �����Z�[�u
//
bool_t pobox_save(const POBoxDict *pbdict)
{
	int i;
	char16_t *w, *p;
	Dict *dict;

	for (i = 0; i < ndictionaries; i++) {
		dict = &(dictionaries[i]);
		if (dict->poboxdict == pbdict) {
			if (pbdict->type == POBOXDICT_TEXT) {
				ptr_t f = File.Open(pbdict->name, FileMode_CreateNew, FileAccess_Write);
				if (f == NULL) { return false; };
				for (size32_t j = 0; j < dict->nentries; j++) {
					w = dict->entry[j].word;
					p = dict->entry[j].pat;
					if (w && *w && p && *p) {
						File.Write(f, (const uint8_t*)p, Utf16.Length(p) * sizeof(char16_t));
						File.Write(f, (const uint8_t*)L"\t", sizeof(char16_t));
						File.Write(f, (const uint8_t*)w, Utf16.Length(w) * sizeof(char16_t));
						File.Write(f, (const uint8_t*)L"\r\n", 2*sizeof(char16_t));
					}
				}
				File.Close(f);
				return true;
			}

		}
	}
	return false;
}

//
// �g�p�\�Ȏ������X�g�̎擾
//
size32_t pobox_getdictlist(const POBoxDict *dictlist[], size32_t limit)
{
	size32_t i;
	for (i = 0; i < limit && i < MAXDICTIONARIES; i++) {
		dictlist[i] = dictionaries[i].poboxdict;
	}
	return i;
}

////
//// �����̑�����ݒ�
////
//POBOX_INT pobox_setattr(POBoxDict *dict, POBoxDict *newdict)
//{
//	(void)dict;
//	(void)newdict;
//	return -1;
//}

//
// �����ɒP���o�^
//
bool_t pobox_regword(const POBoxDict *pbdict, char16_t *word, char16_t *pat)
{
	size32_t i;
	DicEntry de;

	if (pbdict->type != POBOXDICT_TEXT)
	{
		return false;
	}
	Dict *dict = FindDictionary(pbdict);
	if (dict == NULL) { return false; }

	if (!(word && *word && pat && *pat)) { return false; }

	// �P��w�K
	for (i = 0; i < dict->nentries; i++) {
		if (Utf16.Compare(dict->entry[i].word, word) == 0 && Utf16.Compare(dict->entry[i].pat, pat) == 0)
		{
			break;
		}
	}
	if (i == dict->nentries) { // �V���ɓo�^
		pobox_regent(pbdict, i, word, pat);
	}
	de = dict->entry[i];
	for (size32_t j = i; j > 0; j--) {
		dict->entry[j] = dict->entry[j-1];
	}
	dict->entry[0] = de;

	return true;
}

//
// ��������P����폜
//
bool_t pobox_delword(const POBoxDict *pbdict, char16_t *word, char16_t *pat)
{
	size32_t i, j;
	Dict *dict;

	if (pbdict->type != POBOXDICT_TEXT)
	{
		return false;
	}
	dict = FindDictionary(pbdict);
	if (dict == NULL) {
		return false;
	}

	for (i = j = 0; i < dict->nentries; i++) {
		if (Utf16.Compare(dict->entry[i].word, word) && Utf16.Compare(dict->entry[i].pat, pat)) {
			if (i != j) {
				dict->entry[j] = dict->entry[i];
			}
			j++;
		}
		else {
			if (i != j) {
				if (dict->entry[i].word) {
					Memory.Free(dict->entry[i].word);
					dict->entry[i].word = NULL;
				}
				if (dict->entry[i].pat) {
					Memory.Free(dict->entry[i].pat);
					dict->entry[i].pat = NULL;
				}
			}
		}
	}
	dict->nentries = j;
	return true;
}

/////////////////////////////////////////////////////////////////
//
//		����API
//
/////////////////////////////////////////////////////////////////

//#define MAXCANDS 40
//static char16_t *cands[MAXCANDS];
//static char16_t *candspat[MAXCANDS];
//static int ncands = 0;
//static int exact = 0;
//static int approximate = 0;

static asearch_context_t* ctx;

//void pobox_searchmode(pobox_searchmode_t mode)
//{
//	exact = ((mode & POBOX_EXACT) ? 1 : 0);
//	approximate = ((mode | POBOX_APPROXIMATE) ? 1 : 0);
//}

//static int addcand(DicEntry *de) {
//	int i;
//	for (i = 0; i < ncands; i++) {
//		if (Utf16.Compare(cands[i], de->word) == 0)
//			return 0;
//	}
//	cands[ncands] = Utf16.Dupulicate(de->word);
//	candspat[ncands] = Utf16.Dupulicate(de->pat);
//	ncands++;
//	return (ncands >= MAXCANDS ? -1 : 0);
//}

//
// �P�ꌟ��
//
// words (char[wordBufLengthInWord])[wordsLength]�ŗ̈���w��
// 
size32_t pobox_search(const char16_t *pat, char16_t **words, size32_t wordsLength, size32_t wordBufLengthInWord, size32_t skip)
{
	int m;
	int dicno;

	char16_t searchpat[FIND_PATTERN_MAX*2 + 1];	// �B�������p�̃p�^�[�����\�z����ۂɂ͓�{�̗̈���g��

	Dict *dict;

	if (wordBufLengthInWord > 0) {
		for (size32_t i = 0; i < wordsLength; i++)
		{
			words[i][0] = 0x0000;
 		}
	}

	if (pat == NULL || Utf16.Length(pat) > FIND_PATTERN_MAX)
	{
		return 0;
	}

	size32_t ncands = 0;

	char16_t **w = words;
	// �ŏ��͊e�����ɂ��Ĉȉ��̏����Ō���
	// �����̃��C���h�J�[�h�F�L��
	// ���ʐړ��ꂠ���܂������F����
	// �u�Ƃ�����v����������ꍇ�Ɂu�Ƃ�����*�v�����Ō��������̂Łu�Ƃ����傤�v�A�u�Ƃ����傭�v�A�u�Ƃ������v�̓q�b�g���邪�A�u�Ƃ����v��u�Ƃ����イ�v�Ȃǂ̓q�b�g���Ȃ�
	for (dicno = 0; dicno < ndictionaries && ncands < wordsLength + skip; dicno++) {
		dict = &(dictionaries[dicno]);
		switch (dict->poboxdict->type) {
			case POBOXDICT_TEXT: {
				// asearch�̂��߂̌����p�^���쐬
				size32_t n = Utf16.Length(pat);
				Utf16.Copy(searchpat, pat);
				searchpat[n] = L' ';
				searchpat[n + 1] = 0x0000;

				// �P�ꌟ��
				asearch_makepat(ctx, searchpat, 0);
				for (size32_t i = 0; i < dict->nentries; i++) {
					if (asearch_match(ctx, dict->entry[i].pat)) {
						if (skip <= ncands) {
							Utf16.NCopy(*w, dict->entry[i].word, wordBufLengthInWord);
							(*w)[wordBufLengthInWord - 1] = L'\0';
							w++;
						}
						ncands++;
					}
				}
				break;
			}
			case POBOXDICT_LOOKUP: {
				Utf16.Copy(searchpat, pat);
				if (lookup_search(&dict->lookup, searchpat)) {
					while (ncands < wordsLength + skip) {
						char16_t line[LOOPUP_MAX_LINE_SIZE];
						if (lookup_get_next_line(&dict->lookup, line, LOOPUP_MAX_LINE_SIZE) == false) { break; }

						if (skip <= ncands) {
							char16_t *s = Utf16.Find(line, L'\t');
							if (*s != '\0')
							{
								char16_t *word = ++s;
								s = Utf16.FindAny(line, L"\t\n\r");
								*s = L'\0';

								Utf16.NCopy(*w, word, wordBufLengthInWord);
								(*w)[wordBufLengthInWord - 1] = L'\0';
								w++;

							}
						}
						ncands++;
					}
				}
				break;
			}
		}
	}

	// �B������
	for (dicno = 0; dicno < ndictionaries && ncands < wordsLength + skip; dicno++) {
		//printf("dictno = %d\n",dicno);
		dict = &(dictionaries[dicno]);
		if (dict->poboxdict->type == POBOXDICT_TEXT) {
			for (m = 1; ncands < wordsLength + skip && m < 2; m++) {
				asearch_makepat(ctx, searchpat, m);
				for (size32_t i = 0; i < dict->nentries && ncands < wordsLength + skip; i++) {
					if (asearch_match(ctx, dict->entry[i].pat)) {
						if (skip <= ncands) {
							Utf16.NCopy(*w, dict->entry[i].word, wordBufLengthInWord);
							(*w)[wordBufLengthInWord - 1] = L'\0';
							w++;
						}
						ncands++;
					}
				}
			}
			// ��₪�݂���Ȃ��ꍇ�̓p�^����ς��ĞB���������s�Ȃ�
			if (ncands == 0) {
				char16_t *p;
				const char16_t *s;
				for (p = searchpat, s = pat; *s; s++) { // 'dsg' => 'd s g ', etc.
					*p++ = *s;
					*p++ = L' ';
				}
				*p = L'\0';
				asearch_makepat(ctx, searchpat, 0);
				for (size32_t i = 0; i < dict->nentries && ncands < wordsLength + skip; i++) {
					if (asearch_match(ctx, dict->entry[i].pat)) {
						if (skip <= ncands) {
							Utf16.NCopy(*w, dict->entry[i].word, wordBufLengthInWord);
							(*w)[wordBufLengthInWord - 1] = L'\0';
							w++;
						}
						ncands++;
					}
				}
			}
		}
	}

	//if (ncands < wordBufLengthInWord + skip) {
	//	Utf16.NCopy(*w, pat, wordBufLengthInWord);
	//	(*w)[wordBufLengthInWord - 1] = L'\0';
	//	w++;
	//	ncands++;
	//}

	return w - words;
}

/////////////////////////////////////////////////////////////////
//
//		������/�I��API
//
/////////////////////////////////////////////////////////////////

//
// ������
//
bool_t pobox_init(void)
{
	ctx = Memory.Allocate(sizeof(asearch_context_t));
	return ctx != NULL;
}

//
// �I��
//
void pobox_finish(void)
{
	Memory.Free(ctx);
	ctx = NULL;
}
