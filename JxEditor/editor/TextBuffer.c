#include "TextBuffer.h"
#include "../pico/pico.types.h"
#include "../pico/pico.assert.h"
#include "../pico/pico.memory.h"
#include "../arch/arch.h"
#include "./GapBuffer.h"

#define TBUFSIZE (256-5)
#define TBUFMAXSIZE (TBUFSIZE * 100)
/**
* @brief �s�o�b�t�@
*/
typedef struct __tag_text_buffer_t {
	struct __tag_text_buffer_t *prev;//�O���ւ̃����N�BNULL�̏ꍇ�擪�܂��͋�
	struct __tag_text_buffer_t *next;//����ւ̃����N�BNULL�̏ꍇ�Ō�
	gap_buffer_t buffer;
} text_buffer_t;

//�e�L�X�g�o�b�t�@�̈�
static text_buffer_t *Buffers;

//�S�o�b�t�@���Ɋi�[����Ă��镶����
static size32_t TotalLength;

//
// �e�L�X�g�o�b�t�@����p�̃J�[�\��
//

#define CURSOR_MAX 32
// �J�[�\��
static struct {
	bool_t			use;
	text_buffer_t *	Buffer;		/* �s */
	size32_t		Index;		/* �� */
	const char		*file;
	int				line;
} Cursors[CURSOR_MAX];

#define CUR(x) Cursors[x]

static bool_t is_valid_cursor(text_buffer_cursor_t x) {
	return (0 <= (x) && (x) < CURSOR_MAX) && Cursors[(x)].use == true;
}

// �f�o�b�O�E�J�����Ɏg�p����J�[�\���̌���
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
// �e�L�X�g�o�b�t�@
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

	//�o�b�t�@�g�p�ʂ�������
	TotalLength = 0;
}
static void TextBuffer_RemoveLine(text_buffer_t *buf);

void TextBuffer_DeleteCharacatorOnCursor(text_buffer_cursor_t cursor) {
    size32_t len;
	if (GapBuffer.length(&CUR(cursor).Buffer->buffer, &len) == GapBufferResult_Success && len == CUR(cursor).Index)
	{
		// �����������폜�����̍s������΍폜
		if (CUR(cursor).Buffer->next != NULL) {
			text_buffer_t *next = CUR(cursor).Buffer->next;
			GapBuffer.concat(&CUR(cursor).Buffer->buffer, &next->buffer);
			TextBuffer_RemoveLine(next);
			TotalLength--;
		}
	}
	else
	{
		// �J�[�\���̈ʒu�̕������폜
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

	// �J�[�\���X�V

	// �P�s�ڈȊO�����݂���ꍇ�Ɍ����ĂP�s�ڃo�b�t�@���X�V����
	if (Buffers == buf && buf->next != NULL)
	{
		Buffers = buf->next;
	}
	// �폜�����s���ɂ���J�[�\���͈ړ�������
	//   ���̍s������ꍇ�F���̍s�̍s���Ɉړ�����
	//   ���̍s���Ȃ��O�̍s������ꍇ�F�O�̍s�̍s���Ɉړ�����B
	//   �ǂ�����Ȃ��ꍇ�F�ŏ��̍s�ɑ΂���s�폜�Ȃ̂ōŏ��̍s�ɐݒ�
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
			// �擪�s�̏ꍇ��
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

	// �P�����Â���
	while (n > 0) {
		TextBuffer_DeleteCharacatorOnCursor(start);
		n--;
	}
	TextBufferCursor_Dispose(start);
}

/**
* @brief �J�[�\���ʒu�ɂ��镶�����擾����
* @param cursor �J�[�\���ʒu
* @retval �J�[�\�����Ó��Ȉʒu�̏ꍇ�̓J�[�\���ʒu�̕�����Ԃ��B�Ó��ł͂Ȃ��ʒu���I�[�̏ꍇ�̓k��������Ԃ��B
*/
char16_t TextBuffer_TakeCharacatorOnCursor(text_buffer_cursor_t cursor) {
	TextBufferCursor_CheckValid(cursor);
	size32_t sz;
    if (GapBuffer.length(&CUR(cursor).Buffer->buffer, &sz) == GapBufferResult_Success && sz == CUR(cursor).Index) {
		if (CUR(cursor).Buffer->next == NULL) {
			return 0x0000; // �I�[�Ȃ̂Ńk��������Ԃ�
		}
		else {
			return L'\n';	// ���s������Ԃ�
		}
	}

	char16_t ch;
	Diagnotics.Assert(GapBuffer.take(&CUR(cursor).Buffer->buffer, CUR(cursor).Index, &ch) == GapBufferResult_Success);

	return ch;
}

/**
* @brief �J�[�\���ʒu�ɕ�����}������
* @param cursor �J�[�\���ʒu
*/
int TextBuffer_InsertCharacatorOnCursor(text_buffer_cursor_t cursor, char16_t ch) {

	TextBufferCursor_CheckValid(cursor);
	if (ch == L'\n')
	{
		// ���s�͓��ʈ���

		// �V�����s�o�b�t�@���m��
		text_buffer_t *nb = TextBuffer_Allocate();
		if (nb == NULL)
		{
			return -1;
		}

		// ���݃J�[�\��������o�b�t�@�̌��ɘA��
		text_buffer_t *buf = CUR(cursor).Buffer;
		nb->prev = buf;
		nb->next = buf->next;
		if (buf->next != NULL) {
			buf->next->prev = nb;
		}
		buf->next = nb;

		// �J�[�\���ʒu����s���܂ł�V�����s�o�b�t�@�Ɉړ�������
		size32_t len = CUR(cursor).Index;
        size32_t sz;
        Diagnotics.Assert(GapBuffer.length(&buf->buffer, &sz) == GapBufferResult_Success);
		size32_t size = sz - len;
		GapBuffer.CopyTo(&nb->buffer, 0, &buf->buffer, len, size);
		GapBuffer.RemoveRange(&buf->buffer, len, size);

		// �ړ��������͈͂ɂ���J�[�\����V�����ʒu�ɕύX����
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

		// �s�𑝂₷�����s�}���Ȃ̂ŕ����͑����Ă��Ȃ������z�I��1�����g���Ă��邱�Ƃɂ���
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

// �e�L�X�g�o�b�t�@�J�[�\�����P�����O�Ɉړ�������
// false��cursor�����X�擪�ł��������Ƃ������B
bool_t TextBuffer_CursorBackward(text_buffer_cursor_t cursor) {
	TextBufferCursor_CheckValid(cursor);

	if (CUR(cursor).Index > 0) {
		// �J�[�\���ʒu���o�b�t�@�擪�ȊO�̏ꍇ
		CUR(cursor).Index--;

		TextBufferCursor_CheckValid(cursor);
		return true;
	}
	else {
		// �J�[�\���ʒu���o�b�t�@�擪�̏ꍇ�͑O�̍s�ɓ�����
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

// �e�L�X�g�o�b�t�@�J�[�\�����P�������Ɉړ�������
// false��cursor�����X�I�[�ł��������Ƃ������B
bool_t TextBuffer_CursorForward(text_buffer_cursor_t cursor) {
	TextBufferCursor_CheckValid(cursor);
    size32_t sz;
    Diagnotics.Assert(GapBuffer.length(&CUR(cursor).Buffer->buffer, &sz) == GapBufferResult_Success);

	if (CUR(cursor).Index < sz) {
		// �J�[�\���ʒu���o�b�t�@�擪�ȊO�̏ꍇ
		CUR(cursor).Index++;

		TextBufferCursor_CheckValid(cursor);
		return true;
	}
	else {
		// �J�[�\���ʒu���o�b�t�@�����̏ꍇ�͎��̍s�ɓ�����
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

// �J�[�\���𕶎��񒆂̈ʒu�֌W�Ŕ�r����
// c1��c2���O�ɂ���ꍇ�� -1 �Ac2��c1���O�ɂ���ꍇ��1�A�����ʒu�ɂ���ꍇ��0��Ԃ�
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

	Diagnotics.Halt(); // �s���ȃJ�[�\�����n���ꂽ
	return 0;	// ���B���Ȃ��͂�
}

// �J�[�\��������ʒu�������Ă��邩����
// TextBuffer_CompareCursor��荂��
bool_t TextBuffer_EqualCursor(text_buffer_cursor_t c1, text_buffer_cursor_t c2) {
	TextBufferCursor_CheckValid(c1);
	TextBufferCursor_CheckValid(c2);
	if (CUR(c1).Buffer == CUR(c2).Buffer) {
		if (CUR(c1).Index == CUR(c2).Index) { return true; }
	}
	return false;
}

// �e�L�X�g�S�̂̐擪���ǂ�������
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

// �J�[�\�����s���Ɉړ�
void TextBuffer_MoveToBeginningOfLine(text_buffer_cursor_t cur) {
	TextBufferCursor_CheckValid(cur);
	CUR(cur).Index = 0;
}

void TextBuffer_MoveToBeginningOfDocument(text_buffer_cursor_t cur) {
	TextBufferCursor_CheckValid(cur);
	CUR(cur).Buffer = Buffers;
	CUR(cur).Index = 0;
}

// ���[start, end)�̕��������J�E���g����Bend�͊܂܂�Ȃ��̂Œ���
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
