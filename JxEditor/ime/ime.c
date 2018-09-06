#include "ime.h"
#include "TinyPobox.h"

static bool_t GetBit(const uint8_t *bv, const size32_t pos) {
	const size32_t index  = pos / 8U;
	const size32_t offset = 7-(pos % 8U);
	return (bv[index] >> offset) & 0x01U;
}

static size32_t Select0(const uint8_t *bv, const size32_t len, size32_t n) {
	// ビット数テーブル使って最適化したものは後回しにする
	for (size32_t i = 0U; i < len; i++) {
		if (!GetBit(bv, i)) {
			n--;
			if (n == 0U) {
				return i+1U;
			}
		}
	}
	return 0U;
}

static size32_t Rank1(const uint8_t *bv, size32_t k) {
	size32_t cnt = 0U;
	for (size32_t i = 0; i <= k; i++)
	{
		if (GetBit(bv, i))
		{
			cnt++;
		}
	}
	return cnt;
}

static bool_t GetChildIndexeFromParentNode(const LoudsData *data, int *result, int node, int subindex)
{
	int i = Select0(data->tree, data->tree_len*8, node) - 1;

	if (i >= 0)
	{
		int j = i + 1 + subindex;
		if (GetBit(data->tree, j)) {
			*result = j;
			return true;
		}
		else
		{
			return false;
		}
	}
	return false;

}

static int GetNodeFromIndex(const LoudsData *data, int index)
{
//	if (!BitVector_GetBit(data->tree, index))
//	{
//		return -1;
//	}
	return Rank1(data->tree, index);
}

size32_t TryGetValue(const LoudsData *data, const char16_t **result, const char16_t *key, const size32_t keylen, const char16_t** misspos) {
	int node = 1;
	int matchNode = -1;
	int matchLength = 0;
	const char16_t *value = NULL;
	size32_t i;
	for (i = 0; i < keylen; i++)
	{
		char16_t ch = key[i];
		int childIndexFirst;
		if (GetChildIndexeFromParentNode(data, &childIndexFirst, node, 0)) {
			int childNodeFirst = GetNodeFromIndex(data, childIndexFirst);

			int subindex = 0;
			int childIndex = 0;
			while (GetChildIndexeFromParentNode(data, &childIndex, node, subindex)) {
				int idx = childIndex - childIndexFirst + childNodeFirst;
				if (data->edges[idx] == ch) {
					// found
					node = idx;
					if (GetBit(data->accept, idx)) {
						matchNode = node;
						matchLength = i + 1;
					}
					goto Found;
				}
				subindex++;
			}
		}
		// not found
		break;
	Found:
		;
	}
	if (matchLength > 0)
	{
		value = data->values[matchNode];
	}
	else
	{
		value = NULL;
	}
	if (misspos != NULL) { *misspos = &key[i]; }

	*result = value;
	return matchLength;
}

void IME_Reset(IME_Context *work) {
	work->output[0] = 0x0000;
	work->current[0] = 0x0000;
	work->currentlen = 0;
}

bool_t IME_Update(IME_Context *work, char16_t ch) {
	if (work->currentlen + 1 >= IME_BUF_MAX) {
		return false;
	}
	work->current[work->currentlen++] = ch;
	work->current[work->currentlen] = 0x0000;

	char16_t *dst = work->output;
	char16_t *last = &work->current[work->currentlen];
	for (char16_t* src = work->current; *src != 0x0000;)
	{
		const char16_t *found, *misspos = NULL;
		size32_t len = TryGetValue(work->data, &found, src, last-src, &misspos);

		// utf16上 で 'ん'に対する特例的な変換ルール
		if (found == NULL && len == 0)
		{
			if (*src == 0x006EU)	// L'n'
			{
				if (*misspos == 0x0000) {
					while (src < misspos) {
						*dst++ = *src++;
					}
				}
				else {
					*dst++ = 0x3093;	// L'ん';
					src++;
				}
			}
			else {
				*dst++ = *src++;
			}
		}
		else {
			if (work->output < dst && *(dst - 1) == 0x006EU)	// L'n'
			{
				*(dst - 1) = 0x3093;	// L'ん';
			}
			for (const char16_t *p = found; p != NULL && *p != 0x0000 && dst - work->output < IME_BUF_MAX-1; p++) {
				*dst++ = *p;
			}
			src += len;
		}
	}
	*dst = 0x0000;
	work->currentlen = dst - work->output;
	for (int i=0; i<=work->currentlen; i++) {	
		work->current[i] = work->output[i];
	}

	return true;
}

static const POBoxDict lookupdic = {
	L"./staticdic2",
	//POBOXDICT_TEXT,
	POBOXDICT_LOOKUP,
	true// read-only
};

bool_t IME_Initialize(void)
{
	if (pobox_init() == false) { return false; }
	if (pobox_usedict(&lookupdic) == false) { return false; }
	//pobox_searchmode(POBOX_APPROXIMATE);
	return true;
}