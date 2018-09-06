#include "TextBuffer.h"
#include "../pico/pico.types.h"
#include "../pico/pico.assert.h"
#include "../pico/pico.memory.h"
#include "../arch/arch.h"
#include "./GapBuffer.h"

#define TBUFSIZE (256-5)
#define TBUFMAXSIZE (TBUFSIZE * 100)
/**
* @brief 行バッファ
*/
typedef struct __tag_text_buffer_t {
	struct __tag_text_buffer_t *prev;//前方へのリンク。NULLの場合先頭または空き
	struct __tag_text_buffer_t *next;//後方へのリンク。NULLの場合最後
	gap_buffer_t buffer;
} text_buffer_t;

//テキストバッファ領域
static text_buffer_t *Buffers;

//全バッファ内に格納されている文字数
static size32_t TotalLength;

//
// テキストバッファ操作用のカーソル
//

#define CURSOR_MAX 32
// カーソル
static struct {
	bool_t			use;
	text_buffer_t *	Buffer;		/* 行 */
	size32_t		Index;		/* 列 */
	const char		*file;
	int				line;
} Cursors[CURSOR_MAX];

#define CUR(x) Cursors[x]

static bool_t is_valid_cursor(text_buffer_cursor_t x) {
	return (0 <= (x) && (x) < CURSOR_MAX) && Cursors[(x)].use == true;
}

// デバッグ・開発時に使用するカーソルの検証
static void TextBufferCursor_CheckValid(text_buffer_cursor_t cursor) {
    size32_t len;
	Diagnotics.Assert(0 <= cursor);
	Diagnotics.Assert(cursor < CURSOR_MAX);
	Diagnotics.Assert(CUR(cursor).use);
	Diagnotics.Assert(CUR(cursor).Buffer != NULL);
    Diagnotics.Assert(GapBuffer.length(&CUR(cursor).Buffer->buffer, &len) == GapBufferResult_Success);
	Diagnotics.Assert(CUR(cursor).Index <= len);
}

static text_buffer_cursor_t TextBufferCursor_Allocate(void)
{
	for (int i = 0; i < CURSOR_MAX; i++)
	{
		if (CUR(i).use == false)
		{
			CUR(i).use = true;
			CUR(i).Buffer = Buffers;
			CUR(i).Index = 0;
			return i;
		};
	}
	return INVALID_TEXTBUFFER_CURSOR;
}

static void TextBufferCursor_Copy(text_buffer_cursor_t dest, text_buffer_cursor_t src)
{
	TextBufferCursor_CheckValid(src);
	Diagnotics.Assert(is_valid_cursor(src));
	Diagnotics.Assert(is_valid_cursor(dest));
	if (is_valid_cursor(dest) && is_valid_cursor(src)) {
		Cursors[dest] = Cursors[src];
	}
}

static text_buffer_cursor_t TextBufferCursor_Duplicate(text_buffer_cursor_t c)
{
	TextBufferCursor_CheckValid(c);
	for (int i = 0; i < CURSOR_MAX; i++)
	{
		if (CUR(i).use == false)
		{
			CUR(i).use = true;
			CUR(i).Buffer = CUR(c).Buffer;
			CUR(i).Index = CUR(c).Index;
			return i;
		};
	}
	return INVALID_TEXTBUFFER_CURSOR;
}

static void TextBufferCursor_Dispose(text_buffer_cursor_t cursor)
{
	if (0 <= cursor && cursor < CURSOR_MAX && Cursors[cursor].use == true)
	{
		Cursors[cursor].use = false;
		Cursors[cursor].Buffer = NULL;
		Cursors[cursor].Index = 0xFFFF;
	};
}

const struct __tagTextBufferCursorApi TextBufferCursor = {
	TextBufferCursor_Allocate,
	TextBufferCursor_Copy,
	TextBufferCursor_Duplicate,
	TextBufferCursor_Dispose,
	TextBufferCursor_CheckValid,
};

//
// テキストバッファ
//

static text_buffer_t *TextBuffer_Allocate()
{
	text_buffer_t *nb = (text_buffer_t*)Memory.Allocate(sizeof(text_buffer_t));
	if (nb == NULL)
	{
		return NULL;
	}
	if (GapBuffer.initialize(&nb->buffer) != GapBufferResult_Success)
	{
		Memory.Free(nb);
		return NULL;
	}
	nb->prev = NULL;
	nb->next = NULL;

	return nb;
}

static void TextBuffer_Dispose(text_buffer_t *buf)
{
	GapBuffer.finalize(&buf->buffer);
	buf->prev = buf->next = NULL;
	Memory.Free(buf);
}

void TextBuffer_Initialize(void) {

	Buffers = TextBuffer_Allocate();

	//バッファ使用量を初期化
	TotalLength = 0;
}
static void TextBuffer_RemoveLine(text_buffer_t *buf);

void TextBuffer_DeleteCharacatorOnCursor(text_buffer_cursor_t cursor) {
    size32_t len;
	if (GapBuffer.length(&CUR(cursor).Buffer->buffer, &len) == GapBufferResult_Success && len == CUR(cursor).Index)
	{
		// 末尾文字を削除＝後ろの行があれば削除
		if (CUR(cursor).Buffer->next != NULL) {
			text_buffer_t *next = CUR(cursor).Buffer->next;
			GapBuffer.concat(&CUR(cursor).Buffer->buffer, &next->buffer);
			TextBuffer_RemoveLine(next);
			TotalLength--;
		}
	}
	else
	{
		// カーソルの位置の文字を削除
		if (GapBuffer.remove(&CUR(cursor).Buffer->buffer, CUR(cursor).Index) == false)
		{
			return;
		}
		TotalLength--;
	}

}

size32_t TextBuffer_GetTotalLength(void) {
	return TotalLength;
}

size32_t TextBuffer_GetCapacity(void) {
	return TBUFMAXSIZE;
}

static void TextBuffer_RemoveLine(text_buffer_t *buf)
{
	if (buf->prev == NULL && buf->next == NULL)
	{
		GapBuffer.clear(&buf->buffer);
		return;
	}

	// カーソル更新

	// １行目以外が存在する場合に限って１行目バッファを更新する
	if (Buffers == buf && buf->next != NULL)
	{
		Buffers = buf->next;
	}
	// 削除した行内にあるカーソルは移動させる
	//   次の行がある場合：次の行の行頭に移動する
	//   次の行がなく前の行がある場合：前の行の行末に移動する。
	//   どちらもない場合：最初の行に対する行削除なので最初の行に設定
	for (int i = 0; i < CURSOR_MAX; i++) {
		if (CUR(i).use == false) { continue; }
		if (CUR(i).Buffer != buf) { continue; }

		if (buf->next != NULL) {
			CUR(i).Buffer = buf->next;
			CUR(i).Index = 0;
		}
		else if (buf->prev != NULL) {
            size32_t len;
			CUR(i).Buffer = buf->prev;
			CUR(i).Index = GapBuffer.length(&buf->prev->buffer, &len) == GapBufferResult_Success;
		}
		else {
			// 先頭行の場合は
			CUR(i).Buffer = Buffers;
			CUR(i).Index = 0;
		}
	}

	if (buf->next != NULL)
	{
		buf->next->prev = buf->prev;
	}
	if (buf->prev != NULL)
	{
		buf->prev->next = buf->next;
	}
	TextBuffer_Dispose(buf);
}

void TextBuffer_DeleteArea(text_buffer_cursor_t s, size32_t n) {
	text_buffer_cursor_t start = TextBufferCursor_Duplicate(s);

	// １文字づつ消す
	while (n > 0) {
		TextBuffer_DeleteCharacatorOnCursor(start);
		n--;
	}
	TextBufferCursor_Dispose(start);
}

/**
* @brief カーソル位置にある文字を取得する
* @param cursor カーソル位置
* @retval カーソルが妥当な位置の場合はカーソル位置の文字を返す。妥当ではない位置か終端の場合はヌル文字を返す。
*/
char16_t TextBuffer_TakeCharacatorOnCursor(text_buffer_cursor_t cursor) {
	TextBufferCursor_CheckValid(cursor);
	size32_t sz;
    if (GapBuffer.length(&CUR(cursor).Buffer->buffer, &sz) == GapBufferResult_Success && sz == CUR(cursor).Index) {
		if (CUR(cursor).Buffer->next == NULL) {
			return 0x0000; // 終端なのでヌル文字を返す
		}
		else {
			return L'\n';	// 改行文字を返す
		}
	}

	char16_t ch;
	Diagnotics.Assert(GapBuffer.take(&CUR(cursor).Buffer->buffer, CUR(cursor).Index, &ch) == GapBufferResult_Success);

	return ch;
}

/**
* @brief カーソル位置に文字を挿入する
* @param cursor カーソル位置
*/
int TextBuffer_InsertCharacatorOnCursor(text_buffer_cursor_t cursor, char16_t ch) {

	TextBufferCursor_CheckValid(cursor);
	if (ch == L'\n')
	{
		// 改行は特別扱い

		// 新しい行バッファを確保
		text_buffer_t *nb = TextBuffer_Allocate();
		if (nb == NULL)
		{
			return -1;
		}

		// 現在カーソルがあるバッファの後ろに連結
		text_buffer_t *buf = CUR(cursor).Buffer;
		nb->prev = buf;
		nb->next = buf->next;
		if (buf->next != NULL) {
			buf->next->prev = nb;
		}
		buf->next = nb;

		// カーソル位置から行末までを新しい行バッファに移動させる
		size32_t len = CUR(cursor).Index;
        size32_t sz;
        Diagnotics.Assert(GapBuffer.length(&buf->buffer, &sz) == GapBufferResult_Success);
		size32_t size = sz - len;
		GapBuffer.CopyTo(&nb->buffer, 0, &buf->buffer, len, size);
		GapBuffer.RemoveRange(&buf->buffer, len, size);

		// 移動させた範囲にあるカーソルを新しい位置に変更する
		for (size32_t i = 0; i < CURSOR_MAX; i++)
		{
			if (CUR(i).use == false) { continue; }
			if (i == cursor) { continue; }
			if (CUR(i).Buffer == buf && CUR(i).Index >= len)
			{
				CUR(i).Buffer = nb;
				CUR(i).Index -= len;
			}
		}

		// 行を増やす＝改行挿入なので文字は増えていないが仮想的に1文字使っていることにする
		TotalLength++;
		return 0;
	}
	else
	{
		int ret = GapBuffer.insert(&CUR(cursor).Buffer->buffer, CUR(cursor).Index, ch);
		if (ret == GapBufferResult_Success) {
			TotalLength++;
		}
		return ret;
	}
}

int TextBuffer_OverwriteCharacatorOnCursor(text_buffer_cursor_t cursor, char16_t ch) {
	TextBufferCursor_CheckValid(cursor);
    size32_t sz;
    Diagnotics.Assert(GapBuffer.length(&CUR(cursor).Buffer->buffer, &sz) == GapBufferResult_Success);

	if (sz == CUR(cursor).Index || ch == L'\n')
	{
		return TextBuffer_InsertCharacatorOnCursor(cursor, ch);
	}
	else
	{
		TotalLength++;
		return GapBuffer.set(&CUR(cursor).Buffer->buffer, CUR(cursor).Index, ch);
	}
}

// テキストバッファカーソルを１文字前に移動させる
// falseはcursorが元々先頭であったことを示す。
bool_t TextBuffer_CursorBackward(text_buffer_cursor_t cursor) {
	TextBufferCursor_CheckValid(cursor);

	if (CUR(cursor).Index > 0) {
		// カーソル位置がバッファ先頭以外の場合
		CUR(cursor).Index--;

		TextBufferCursor_CheckValid(cursor);
		return true;
	}
	else {
		// カーソル位置がバッファ先頭の場合は前の行に動かす
		if (CUR(cursor).Buffer->prev != NULL)
		{
            size32_t sz;
            Diagnotics.Assert(GapBuffer.length(&CUR(cursor).Buffer->buffer, &sz) == GapBufferResult_Success);
			CUR(cursor).Buffer = CUR(cursor).Buffer->prev;
			CUR(cursor).Index = sz;
			TextBufferCursor_CheckValid(cursor);
			return true;
		}
		else
		{
			return false;
		}
	}

}

// テキストバッファカーソルを１文字後ろに移動させる
// falseはcursorが元々終端であったことを示す。
bool_t TextBuffer_CursorForward(text_buffer_cursor_t cursor) {
	TextBufferCursor_CheckValid(cursor);
    size32_t sz;
    Diagnotics.Assert(GapBuffer.length(&CUR(cursor).Buffer->buffer, &sz) == GapBufferResult_Success);

	if (CUR(cursor).Index < sz) {
		// カーソル位置がバッファ先頭以外の場合
		CUR(cursor).Index++;

		TextBufferCursor_CheckValid(cursor);
		return true;
	}
	else {
		// カーソル位置がバッファ末尾の場合は次の行に動かす
		if (CUR(cursor).Buffer->next != NULL)
		{
			CUR(cursor).Buffer = CUR(cursor).Buffer->next;
			CUR(cursor).Index = 0;
			TextBufferCursor_CheckValid(cursor);
			return true;
		}
		else
		{
			return false;

		}
	}
}

// カーソルを文字列中の位置関係で比較する
// c1がc2より前にある場合は -1 、c2がc1より前にある場合は1、同じ位置にある場合は0を返す
int TextBuffer_CompareCursor(text_buffer_cursor_t c1, text_buffer_cursor_t c2) {
	TextBufferCursor_CheckValid(c1);
	TextBufferCursor_CheckValid(c2);

	if (CUR(c1).Buffer == CUR(c2).Buffer) {
		if (CUR(c1).Index < CUR(c2).Index) { return -1; }
		if (CUR(c1).Index == CUR(c2).Index) { return 0; }
		if (CUR(c1).Index > CUR(c2).Index) { return  1; }
	}

	for (text_buffer_t * p = Buffers; p != NULL; p = p->next) {
		if (p == CUR(c1).Buffer) { return -1; }
		if (p == CUR(c2).Buffer) { return  1; }
	}

	Diagnotics.Halt(); // 不正なカーソルが渡された
	return 0;	// 到達しないはず
}

// カーソルが同一位置を示しているか判定
// TextBuffer_CompareCursorより高速
bool_t TextBuffer_EqualCursor(text_buffer_cursor_t c1, text_buffer_cursor_t c2) {
	TextBufferCursor_CheckValid(c1);
	TextBufferCursor_CheckValid(c2);
	if (CUR(c1).Buffer == CUR(c2).Buffer) {
		if (CUR(c1).Index == CUR(c2).Index) { return true; }
	}
	return false;
}

// テキスト全体の先頭かどうか判定
bool_t TextBuffer_StartOfString(text_buffer_cursor_t c) {
	TextBufferCursor_CheckValid(c);
	return (CUR(c).Buffer->prev == NULL && CUR(c).Index == 0);
}

bool_t TextBuffer_EndOfString(text_buffer_cursor_t c) {
	TextBufferCursor_CheckValid(c);
    size32_t sz;
    Diagnotics.Assert(GapBuffer.length(&CUR(c).Buffer->buffer, &sz) == GapBufferResult_Success);
	if (sz == CUR(c).Index && CUR(c).Buffer->next == NULL) { return true; }
	return false;
}

// カーソルを行頭に移動
void TextBuffer_MoveToBeginningOfLine(text_buffer_cursor_t cur) {
	TextBufferCursor_CheckValid(cur);
	CUR(cur).Index = 0;
}

void TextBuffer_MoveToBeginningOfDocument(text_buffer_cursor_t cur) {
	TextBufferCursor_CheckValid(cur);
	CUR(cur).Buffer = Buffers;
	CUR(cur).Index = 0;
}

// 区間[start, end)の文字数をカウントする。endは含まれないので注意
size32_t TextBuffer_strlen(text_buffer_cursor_t start, text_buffer_cursor_t end)
{
	TextBufferCursor_CheckValid(start);
	TextBufferCursor_CheckValid(end);
	int n = 0;
	text_buffer_cursor_t c = TextBufferCursor_Duplicate(start);
	for (;;) {
		if (TextBuffer_EqualCursor(c, end)) {
			break;
		}
		TextBuffer_CursorForward(c);
		n++;
	}
	TextBufferCursor_Dispose(c);
	return n;
}
