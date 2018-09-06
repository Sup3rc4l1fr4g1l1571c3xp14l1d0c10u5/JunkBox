
#include "../pico/pico.types.h"
#include "../pico/pico.assert.h"
#include "../pico/pico.memory.h"
#include "../pico/pico.file.h"
#include "../pico/pico.dictionary.h"

#include "TextVRAM.h"
#include "Keyboard.h"
#include "keyinput.h"
#include "TextBuffer.h"
#include "Font.h"

#define EXPERIMENTAL

#ifdef EXPERIMENTAL
#include "../ime/roma2kana.h"	// experimental
#include "../ime/ime.h"	// experimental
#include "../ime/TinyPobox.h"	// experimental
#endif

#include "Config.h"
#include "../arch/arch.h"

#define COLOR_NORMALTEXT 7 //�ʏ�e�L�X�g�F
#define COLOR_NORMALTEXT_BG 0	 //�ʏ�e�L�X�g�w�i
#define COLOR_ERRORTEXT 4 //�G���[���b�Z�[�W�e�L�X�g�F
#define COLOR_AREASELECTTEXT 4 //�͈͑I���e�L�X�g�F
#define COLOR_BOTTOMLINE 5 //��ʍŉ��s�̃e�L�X�g�F
#define COLOR_CURSOR 6 //�J�[�\���F
#define COLOR_BOTTOMLINE_BG 8 //��ʍŉ��s�w�i
#define FILEBUFSIZE 256 //�t�@�C���A�N�Z�X�p�o�b�t�@�T�C�Y
#define FILE_NAME_BUF_LEN (8 + 1 + 3 + 1) // �t�@�C�����o�b�t�@�T�C�Y
#define MAXFILENUM 50 //���p�\�t�@�C���ő吔

#define ERR_FILETOOBIG -1
#define ERR_CANTFILEOPEN -2
#define ERR_CANTWRITEFILE -3

/**
* ���݂̃J�[�\���ʒu�ɑΉ�����e�L�X�g�ʒu
* [0] �͌��݈ʒu�������l
* [1] ��save_cursor�őޔ����ꂽ�l
*/
static text_buffer_cursor_t Cursor[2] = { INVALID_TEXTBUFFER_CURSOR , INVALID_TEXTBUFFER_CURSOR };

/**
* ���ݕ\�����̉�ʂ̍���ɑΉ�����e�L�X�g�ʒu
* [0] �͌��݈ʒu�������l
* [1] ��save_cursor�őޔ����ꂽ�l
*/
static text_buffer_cursor_t DisplayLeftTop[2] = { INVALID_TEXTBUFFER_CURSOR , INVALID_TEXTBUFFER_CURSOR };

/**
* ��ʏ�ł̃J�[�\���̈ʒu
* [0] �͌��݈ʒu�������l
* [1] ��save_cursor�őޔ����ꂽ�l
*/
static screen_pos_t CursorScreenPos[2];

static int cx2; //�㉺�ړ����̉��J�[�\��X���W

// �͈͑I�����̃J�[�\���̃X�^�[�g�ʒu
static text_buffer_cursor_t SelectStart = INVALID_TEXTBUFFER_CURSOR;
// �͈͑I�����̃J�[�\���̃X�^�[�g���W�i��ʏ�j
static screen_pos_t SelectStartCursorScreenPos;

static bool_t edited; //�ۑ���ɕύX���ꂽ����\���t���O

static char16_t filebuf[FILEBUFSIZE]; //�t�@�C���A�N�Z�X�p�o�b�t�@
static char16_t CurrentFileName[FILE_NAME_BUF_LEN]; //�ҏW���̃t�@�C����
static char16_t filenames[MAXFILENUM][FILE_NAME_BUF_LEN];

static screen_pos_t calc_screen_pos_from_line_head(text_buffer_cursor_t head, text_buffer_cursor_t cur);

/**
 * ��ʏ�̃J�[�\���ʒu�ƍĎZ�o�����ʒu��񂪈�v���Ă��邩�����i�f�o�b�O�p�j
 */
void ValidateDisplayCursorPos(void) {
	screen_pos_t p = calc_screen_pos_from_line_head(DisplayLeftTop[0], Cursor[0]);
	Diagnotics.Assert((p.X == CursorScreenPos[0].X) && (p.Y == CursorScreenPos[0].Y));
}

// ��ʏ�̈ʒu�Ɛ܂�Ԃ����l���ɓ���Ȃ���J�[�\���Ɖ�ʏ�̈ʒu��A�������ĂЂƂi�߂�
static bool_t iterate_cursor_with_screen_pos(screen_pos_t *p, text_buffer_cursor_t cur)
{
	// �P������i�ړ���j��ǂ�
	char16_t c1 = TextBuffer_TakeCharacatorOnCursor(cur);
	if (c1 == 0x0000) {
		// 1�����悪�����Ȃ̂őł��؂�
		return false;
	}
	int w1 = UniWidth1(c1);

	// �Q�������ǂށi��q�̐܂�Ԃ�����̂��߁j
	TextBuffer_CursorForward(cur);
	char16_t c2 = TextBuffer_TakeCharacatorOnCursor(cur);
	int w2 = UniWidth1(c2);

	// �^�u�E�܂�Ԃ����l�����ĕ����̈ʒu������

	if (c1 == L'\n') {
		// �P�����ڂ����s�����̏ꍇ�͎��s�Ɉړ�
		p->X = 0;
		p->Y++;
	} else if (c1 == L'\t') {
		// �^�u�����̏ꍇ
		int tabSize = 4 - (p->X % 4);
		// �^�u�̈ʒu�ɉ����ď����ύX
		if (p->X + tabSize >= EDITOR_SCREEN_WIDTH) {
			// �^�u�ōs���܂Ŗ��܂�
			p->X = 0;
			p->Y++;
		}
		else {
			// �s���܂ł͖��܂�Ȃ��̂Ń^�u������
			p->X += tabSize;
			// ���̕��������߂ă`�F�b�N���Đ܂�Ԃ����ǂ����l����
			if (p->X + w2 > EDITOR_SCREEN_WIDTH) {
				p->X = 0;
				p->Y++;
			}
		}
	} else if (p->X + w1 + w2 > EDITOR_SCREEN_WIDTH) {
		// �ꕶ���ځA�񕶎��ڗ������󂯓����ƍs���𒴂���ꍇ
		p->X = 0;
		p->Y++;
	} else {
		p->X += w1;
	}
	return true;
}

// head�̃X�N���[�����W��(0,0)�Ƃ����ꍇ��cur�̃X�N���[�����W�ʒu���Z�o
static screen_pos_t calc_screen_pos_from_line_head(text_buffer_cursor_t head, text_buffer_cursor_t cur) {
	TextBufferCursor.CheckValid(head);
	TextBufferCursor.CheckValid(cur);

	screen_pos_t p = { 0,0 };
	text_buffer_cursor_t h = TextBufferCursor.Duplicate(head);
	while  (TextBuffer_EqualCursor(h, cur) == false) {
		if (iterate_cursor_with_screen_pos(&p, h) == false) {
			break;
		}
	}
	TextBufferCursor.Dispose(h);
	return p;
}

// ��ʌ��_����Ɍ��݂̍s�̉�ʏ�ł̍ō��ʒu�ɑΉ�����ʒu�ɃJ�[�\�����ړ�
static bool_t move_cursor_to_display_line_head(screen_pos_t *pos, text_buffer_cursor_t cur) {
	TextBufferCursor.CheckValid(cur);
	screen_pos_t p = { 0,0 };
	text_buffer_cursor_t c = TextBufferCursor.Duplicate(DisplayLeftTop[0]);
	text_buffer_cursor_t cprev = TextBufferCursor.Allocate();
	// �^�u��܂�Ԃ��Ȃǂ����邽�߁A�P���ɋt���ɒH�邱�Ƃ��ł��Ȃ��ꍇ�������ďꍇ��������Ɩʓ|������
	//��ʌ��_�ɑΉ�����o�b�t�@�J�[�\���͏�Ɍv�Z�ς݂Ȃ̂ł�������̋��������߂�
	bool_t result;
	for (;;) {
		screen_pos_t pprev = p;
		TextBufferCursor.Copy(cprev, c);
		if (p.Y == pos->Y && p.X == 0)
		{
			*pos = p;
			TextBufferCursor.Copy(cur, c);
			result = true;
			break;
		}
		else if (iterate_cursor_with_screen_pos(&p, c) == false)
		{
			*pos = pprev;
			TextBufferCursor.Copy(cur, cprev);
			result = false;
			break;
		}
		else if (p.Y == pos->Y)
		{
			Diagnotics.Assert(p.X == 0);
			*pos = p;
			TextBufferCursor.Copy(cur, c);
			result = true;
			break;
		}
	}
	TextBufferCursor.Dispose(c);
	TextBufferCursor.Dispose(cprev);
	return result;
}

// ��ʏ�Ō��ݍs�̖����܂ňړ�
static bool_t move_cursor_to_display_line_tail(screen_pos_t *pos, text_buffer_cursor_t cur) {
	TextBufferCursor.CheckValid(cur);

	screen_pos_t p = *pos;
	text_buffer_cursor_t c = TextBufferCursor.Duplicate(cur);
	text_buffer_cursor_t cprev = TextBufferCursor.Allocate();
	bool_t result;
	for (;;) {
		screen_pos_t pprev = p;
		TextBufferCursor.Copy(cprev, c);
		if (iterate_cursor_with_screen_pos(&p, c) == false)
		{
			*pos = p;
			TextBufferCursor.Copy(cur, c);
			result = false;
			break;
		}
		else if (p.Y == pos->Y + 1)
		{
			*pos = pprev;
			TextBufferCursor.Copy(cur, cprev);
			result = true;
			break;
		}
	}
	TextBufferCursor.Dispose(c);
	TextBufferCursor.Dispose(cprev);
	return result;
}

// ��ʏ�Ō��݂̕����̎�O�܂ňړ�
static bool_t move_cursor_to_prev(screen_pos_t *pos, text_buffer_cursor_t cur) {
	TextBufferCursor.CheckValid(cur);
	screen_pos_t p = { 0,0 };
	screen_pos_t pprev = p;
	text_buffer_cursor_t c = TextBufferCursor.Duplicate(DisplayLeftTop[0]);
	text_buffer_cursor_t cprev = TextBufferCursor.Duplicate(DisplayLeftTop[0]);
	while (!(p.Y == pos->Y && p.X== pos->X)) {
		pprev = p;
		TextBufferCursor.Copy(cprev, c);
		Diagnotics.Assert(iterate_cursor_with_screen_pos(&p, c));
	}
	*pos = pprev;
	TextBufferCursor.Copy(cur, cprev);
	TextBufferCursor.Dispose(c);
	TextBufferCursor.Dispose(cprev);
	return true;
}

// ��ʏ��target.y �̍s��target.x�𒴂��Ȃ��ʒu(rp)�̃J�[�\��c��DisplayLeftTop�����_�Ƃ��ċ��߂�
// �s���Œ�~�����ꍇ�� false 
static bool_t move_cursor_to_display_pos(screen_pos_t target, screen_pos_t *rp, text_buffer_cursor_t c)
{
	screen_pos_t p = { 0,0 };
	screen_pos_t pp = p;
	TextBufferCursor.Copy(c, DisplayLeftTop[0]);
	text_buffer_cursor_t pc = TextBufferCursor.Duplicate(DisplayLeftTop[0]);
	bool_t ret;
	for (;;)
	{
		if (p.Y == target.Y && p.X >= target.X)
		{
			*rp = p;
			ret = true;
			break;
		}
		if (p.Y > target.Y)
		{
			*rp = pp;
			TextBufferCursor.Copy(c, pc);
			ret = true;
			break;
		}
		pp = p;
		TextBufferCursor.Copy(pc, c);
		if (iterate_cursor_with_screen_pos(&p, c) == false)
		{
			// �������ɓ��B
			*rp = pp;
			TextBufferCursor.Copy(c, pc);
			ret = false;
			break;
		}
	}
	TextBufferCursor.Dispose(pc);
	return ret;
}


static bool_t OverwriteCharactor(char16_t ch) {
	int i = TextBuffer_OverwriteCharacatorOnCursor(Cursor[0], ch);//�e�L�X�g�o�b�t�@�ɂP�����㏑��
	if (i != 0) {
		return false;
	}
	CursorScreenPos[0] = calc_screen_pos_from_line_head(DisplayLeftTop[0], Cursor[0]);
	ValidateDisplayCursorPos();
	return true;
}

static bool_t InsertCharactor(char16_t ch) {
	int i = TextBuffer_InsertCharacatorOnCursor(Cursor[0], ch);//�e�L�X�g�o�b�t�@�ɂP�����}��
	if (i != 0) {
		return false;
	}
	CursorScreenPos[0] = calc_screen_pos_from_line_head(DisplayLeftTop[0], Cursor[0]);
	ValidateDisplayCursorPos();
	return true;
}

// ��ʂ��P�s���ɃX�N���[������
static bool_t screen_scrolldown() {
	text_buffer_cursor_t c = TextBufferCursor.Duplicate(DisplayLeftTop[0]);
	screen_pos_t p = { 0,0 };
	for (;;)
	{
		if (p.Y == 1)
		{
			TextBufferCursor.Copy(DisplayLeftTop[0], c);
			CursorScreenPos[0].Y -= 1;
			TextBufferCursor.Dispose(c);
			return true;
		}
		if (iterate_cursor_with_screen_pos(&p, c) == false)
		{
			// ���̍s���Ȃ��̂łǂ����悤���Ȃ�
			TextBufferCursor.Dispose(c);
			return false;
		}

	}
}

static bool_t screen_scrollup()
{
	// �J�[�\���̉�ʂx���W���ŏ��Ȃ̂ō�����W�̍X�V���K�v
	text_buffer_cursor_t lt = TextBufferCursor.Duplicate(DisplayLeftTop[0]);
	if (TextBuffer_CursorBackward(lt)) {
		// ���ォ��P�����߂遁��ʏ�ɂ�����O�̍s�̖����Ƀo�b�t�@��̃J�[�\�����ړ�

		// �s���ʒu���Z�o
		text_buffer_cursor_t c = TextBufferCursor.Duplicate(lt);
		TextBuffer_MoveToBeginningOfLine(c);

		// �s���ʒu��(0,0)�_�Ƃ��ăJ�[�\��lt�̉�ʏ�ʒu���v�Z
		screen_pos_t lp = calc_screen_pos_from_line_head(c, lt);

		// lt�̉�ʈʒu�𒴂��Ȃ��ʒu�̍s�����T�[�`
		screen_pos_t p = { 0,0 };
		while (p.Y != lp.Y)
		{
			iterate_cursor_with_screen_pos(&p, c);
		}

		// ���������Ƃ��낪�V��������ʒu
		TextBufferCursor.Copy(DisplayLeftTop[0], c);
		CursorScreenPos[0].Y += 1;
		ValidateDisplayCursorPos();
		TextBufferCursor.Dispose(c);
		TextBufferCursor.Dispose(lt);
		return true;
	}
	else
	{
		// DisplayLeftTop�̓o�b�t�@�擪�Ȃ̂Ŗ߂�Ȃ�
		TextBufferCursor.Dispose(lt);
		return false;
	}
}

//�J�[�\����1�O�Ɉړ�
//�o�́F���L�ϐ����ړ���̒l�ɕύX
//Cursor[0] �o�b�t�@��̃J�[�\���ʒu
//CursorScreenPos[0] ��ʏ�̃J�[�\���ʒu
//cx2 cx�Ɠ���
//DisplayLeftTop[0] ��ʍ���̃o�b�t�@��̈ʒu
void cursor_left_(void) {

	if (CursorScreenPos[0].X == 0 && CursorScreenPos[0].Y == 0)
	{
		if (screen_scrollup() == false)
		{
			// ��ɃX�N���[���ł��Ȃ����擪�Ȃ̂Ŗ߂�Ȃ�
			return;
		}
	}

	move_cursor_to_prev(&CursorScreenPos[0], Cursor[0]);
	cx2 = CursorScreenPos[0].X;
}

void cursor_left(void) {
	ValidateDisplayCursorPos();
	cursor_left_();
	ValidateDisplayCursorPos();
}

//�J�[�\�����E�Ɉړ�
void cursor_right(void) {
	ValidateDisplayCursorPos();
	if (iterate_cursor_with_screen_pos(&CursorScreenPos[0], Cursor[0])) {
		if (CursorScreenPos[0].Y == EDITOR_SCREEN_HEIGHT) {
			// �J�[�\����i�߂邱�Ƃɐ���������J�[�\���ʒu����ʍŉ��i����͂ݏo���̂�
			// ��ʂ���s���ɃX�N���[������
			screen_scrolldown();
		}
	}	
	cx2 = CursorScreenPos[0].X;
	ValidateDisplayCursorPos();
}

//�J�[�\����1��Ɉړ�
void cursor_up_(void) {

	if (CursorScreenPos[0].Y == 0) {
		if (screen_scrollup() == false)
		{
			// ��ʂ���ɃX�N���[���ł��Ȃ����߂�Ȃ��̂ŏI��
			return;
		}
	}
	ValidateDisplayCursorPos();

	// ����̕␳���I����
	// ����ʒu�����ʏ�łP�s��̈ʒu���T�[�`����
	screen_pos_t target = { cx2, CursorScreenPos[0].Y - 1 };
	screen_pos_t p = { 0,0 };
	text_buffer_cursor_t c = TextBufferCursor.Allocate();

	move_cursor_to_display_pos(target, &p, c);

	TextBufferCursor.Copy(Cursor[0], c);
	CursorScreenPos[0] = p;
	TextBufferCursor.Dispose(c);

	ValidateDisplayCursorPos();
}


void cursor_up(void) {
	ValidateDisplayCursorPos();
	cursor_up_();
	ValidateDisplayCursorPos();
}


//�J�[�\����1���Ɉړ�
//�o�́F���L�ϐ����ړ���̒l�ɕύX
//cursorbp,cursorix �o�b�t�@��̃J�[�\���ʒu
//cx,cy ��ʏ�̃J�[�\���ʒu
//cx2 �ړ��O��cx�Ɠ���
//disptopbp,disptopix ��ʍ���̃o�b�t�@��̈ʒu
void cursor_down_(void) {
	// ����ʒu����T�[�`����
#if 1
	screen_pos_t target = { cx2, CursorScreenPos[0].Y + 1 };
	screen_pos_t p = { 0,0 };
	text_buffer_cursor_t c = TextBufferCursor.Allocate();

	if (move_cursor_to_display_pos(target, &p, c) == false)
	{
		if (p.Y != target.Y )
		{
			// ���̍s�Ɉړ��ł��Ă��Ȃ����ŉ��i�̍s�ŉ���������
			TextBufferCursor.Dispose(c);
			return;
		}
	}

	TextBufferCursor.Copy(Cursor[0], c);
	CursorScreenPos[0] = p;
	TextBufferCursor.Dispose(c);

#else


	text_buffer_cursor_t lt = TextBufferCursor.Duplicate(DisplayLeftTop[0]);
	screen_pos_t csp = CursorScreenPos[0];
	screen_pos_t p = { 0,0 };
	screen_pos_t target = { cx2, csp.Y + 1 };
	text_buffer_cursor_t c = TextBufferCursor.Duplicate(lt);
	screen_pos_t pp = p;
	text_buffer_cursor_t pc = TextBufferCursor.Duplicate(c);
	for (;;)
	{
		if (p.Y == target.Y && p.X == target.X)
		{
			break;
		}
		if (p.Y == target.Y && p.X > target.X)
		{
			p = pp;
			TextBufferCursor.Copy(c, pc);
			break;
		}
		if (p.Y > target.Y)
		{
			p = pp;
			TextBufferCursor.Copy(c, pc);
			break;
		}
		pp = p;
		TextBufferCursor.Copy(pc, c);
		if (iterate_cursor_with_screen_pos(&p, c) == false)
		{
			// ���̍s���Ȃ��̂ňړ��ł��Ȃ�
			TextBufferCursor.Dispose(lt);
			TextBufferCursor.Dispose(c);
			TextBufferCursor.Dispose(pc);
			return;
		}
	}

	TextBufferCursor.Copy(Cursor[0], c);
	CursorScreenPos[0] = p;
	TextBufferCursor.Copy(DisplayLeftTop[0], lt);

	TextBufferCursor.Dispose(lt);
	TextBufferCursor.Dispose(c);
	TextBufferCursor.Dispose(pc);

#endif

	// ��ʂ���s���ɃX�N���[������
	if (CursorScreenPos[0].Y == EDITOR_SCREEN_HEIGHT)
	{
		if (screen_scrolldown() == false)
		{
			// �ړ��ł��Ȃ����X�N���[���ł��Ȃ��̂ŏI��
			return;
		}
	}

	ValidateDisplayCursorPos();


}

void cursor_down(void) {
	ValidateDisplayCursorPos();
	cursor_down_();
	ValidateDisplayCursorPos();
}


// �J�[�\�����s���Ɉړ�����
void cursor_home_(void) {
	move_cursor_to_display_line_head(&CursorScreenPos[0], Cursor[0]);
	cx2 = 0;
}

void cursor_home(void) {
	ValidateDisplayCursorPos();
	cursor_home_();
	ValidateDisplayCursorPos();
}



// �J�[�\�����s���Ɉړ�����
// ���ړ��Ȃ̂ōŌ�ɍs�������ړ���̂w���W��ێ�����ϐ�cx2���X�V����
void cursor_end_(void) {
	move_cursor_to_display_line_tail(&CursorScreenPos[0], Cursor[0]);
	cx2 = CursorScreenPos[0].X;
}

void cursor_end(void) {
	ValidateDisplayCursorPos();
	cursor_end_();
	ValidateDisplayCursorPos();
}

void cursor_pageup_(void) {
	//PageUp�L�[
	//�ŏ�s���ŉ��s�ɂȂ�܂ŃX�N���[��
	//�o�́F���L�ϐ����ړ���̒l�ɕύX
	//cursorbp,cursorix �o�b�t�@��̃J�[�\���ʒu
	//cx,cx2
	//cy
	//disptopbp,disptopix ��ʍ���̃o�b�t�@��̈ʒu

	const int cy_old = CursorScreenPos[0].Y;
	while (CursorScreenPos[0].Y > 0) {
		cursor_up(); // cy==0�ɂȂ�܂ŃJ�[�\������Ɉړ�
	}

	int i;
	text_buffer_cursor_t prev = TextBufferCursor.Allocate();
	for (i = 0; i < EDITOR_SCREEN_HEIGHT - 1; i++) {
		//��ʍs��-1�s���J�[�\������Ɉړ�
		TextBufferCursor.Copy(prev, DisplayLeftTop[0]);
		cursor_up();
		if (TextBuffer_EqualCursor(prev, DisplayLeftTop[0])) {
			break; //�ŏ�s�ňړ��ł��Ȃ������ꍇ������
		}
	}
	TextBufferCursor.Dispose(prev);
	//����Y���W�܂ŃJ�[�\�������Ɉړ��A1�s�������Ȃ������ꍇ�͍ŏ�s�ɗ��܂�
	if (i > 0) {
		while (CursorScreenPos[0].Y < cy_old) {
			cursor_down();
		}
	}
}

void cursor_pageup(void) {
	ValidateDisplayCursorPos();
	cursor_pageup_();
	ValidateDisplayCursorPos();
}

void cursor_pagedown_(void) {
	//PageDown�L�[
	//�ŉ��s���ŏ�s�ɂȂ�܂ŃX�N���[��
	//�o�́F���L�ϐ����ړ���̒l�ɕύX
	//cursorbp,cursorix �o�b�t�@��̃J�[�\���ʒu
	//cx,cx2
	//cy
	//disptopbp,disptopix ��ʍ���̃o�b�t�@��̈ʒu


	const int cy_old = CursorScreenPos[0].Y;
	while (CursorScreenPos[0].Y < EDITOR_SCREEN_HEIGHT - 1) {
		// cy==EDITWIDTH-1�ɂȂ�܂ŃJ�[�\�������Ɉړ�
		int y = CursorScreenPos[0].Y;
		cursor_down();
		if (y == CursorScreenPos[0].Y) {
			break;// �o�b�t�@�ŉ��s�ňړ��ł��Ȃ������ꍇ������
		}
	}

	int i;
	text_buffer_cursor_t prev = TextBufferCursor.Allocate();
	for (i = 0; i < EDITOR_SCREEN_HEIGHT - 1; i++) {
		//��ʍs��-1�s���J�[�\�������Ɉړ�
		TextBufferCursor.Copy(prev, DisplayLeftTop[0]);
		cursor_down();
		if (TextBuffer_EqualCursor(prev, DisplayLeftTop[0])) {
			break; //�ŉ��s�ňړ��ł��Ȃ������ꍇ������
		}
	}
	TextBufferCursor.Dispose(prev);

	//���[���炳��Ɉړ������s�����A�J�[�\������Ɉړ��A1�s�������Ȃ������ꍇ�͍ŉ��s�ɗ��܂�
	if (i > 0) {
		while (CursorScreenPos[0].Y > cy_old) {
			cursor_up();
		}
	}
}

void cursor_pagedown(void) {
	ValidateDisplayCursorPos();
	cursor_pagedown_();
	ValidateDisplayCursorPos();
}

void cursor_top(void) {
	//�J�[�\�����e�L�X�g�o�b�t�@�̐擪�Ɉړ�
	TextBuffer_MoveToBeginningOfDocument(Cursor[0]);

	//�͈͑I�����[�h����
	TextBufferCursor.Dispose(SelectStart);
	SelectStart = INVALID_TEXTBUFFER_CURSOR;

	// ��ʂ̍���ʒu�����Z�b�g
	TextBufferCursor.Copy(DisplayLeftTop[0], Cursor[0]);

	// ��ʏ�J�[�\���ʒu�����Z�b�g
	CursorScreenPos[0].X = 0;
	CursorScreenPos[0].Y = 0;

	// �Ō�̉��ړ��ʒu�����Z�b�g
	cx2 = 0;
}

static int countarea(void) {
	//�e�L�X�g�o�b�t�@�̎w��͈͂̕��������J�E���g
	//�͈͂�(cursorbp,cursorix)��(SelectStart.Buffer,SelectStart.Index)�Ŏw��
	//��둤�̈�O�̕����܂ł��J�E���g

	//�I��͈͂̊J�n�ʒu�ƏI���̑O��𔻒f���ĊJ�n�ʒu�ƏI���ʒu��ݒ�
	if (CursorScreenPos[0].Y < SelectStartCursorScreenPos.Y ||
		(CursorScreenPos[0].Y == SelectStartCursorScreenPos.Y && CursorScreenPos[0].X < SelectStartCursorScreenPos.X)) {
		return TextBuffer_strlen(Cursor[0], SelectStart);
	}
	else {
		return TextBuffer_strlen(SelectStart, Cursor[0]);
	}
}

// �e�L�X�g�o�b�t�@�̎w��͈͂��폜
static void deletearea(void) {
	//�͈͂�(cursorbp,cursorix)��(SelectStart.Buffer,SelectStart.Index)�Ŏw��
	//��둤�̈�O�̕����܂ł��폜
	//�폜��̃J�[�\���ʒu�͑I��͈͂̐擪�ɂ��A�͈͑I�����[�h��������

	int n = countarea(); //�I��͈͂̕������J�E���g

	//�͈͑I���̊J�n�ʒu�ƏI���ʒu�̑O��𔻒f���ăJ�[�\�����J�n�ʒu�ɐݒ�
	if (CursorScreenPos[0].Y > SelectStartCursorScreenPos.Y || (CursorScreenPos[0].Y == SelectStartCursorScreenPos.Y && CursorScreenPos[0].X > SelectStartCursorScreenPos.X)) {
		TextBufferCursor.Copy(Cursor[0], SelectStart);
		CursorScreenPos[0] = SelectStartCursorScreenPos;
	}
	cx2 = CursorScreenPos[0].X;

	//�͈͑I�����[�h����
	TextBufferCursor.Dispose(SelectStart);
	SelectStart = INVALID_TEXTBUFFER_CURSOR;

	// �n�_����n�����폜
	TextBuffer_DeleteArea(Cursor[0], n);
	CursorScreenPos[0] = calc_screen_pos_from_line_head(DisplayLeftTop[0], Cursor[0]);
}

//��ʂ̍ĕ`��
static void redraw(void) {
	uint8_t cl = COLOR_NORMALTEXT;

	DisableInterrupt();

	text_buffer_cursor_t select_start, select_end;
	if (SelectStart == INVALID_TEXTBUFFER_CURSOR) {
		//�͈͑I�����[�h�łȂ��ꍇ
		select_start = INVALID_TEXTBUFFER_CURSOR;
		select_end = INVALID_TEXTBUFFER_CURSOR;
	}
	else {
		//�͈͑I�����[�h�̏ꍇ�A�J�n�ʒu�ƏI���̑O�㔻�f����
		//bp1 ���J�n�ʒu�Abp2 ���I���ʒu�ɐݒ�
		if (CursorScreenPos[0].Y < SelectStartCursorScreenPos.Y ||
			(CursorScreenPos[0].Y == SelectStartCursorScreenPos.Y && CursorScreenPos[0].X < SelectStartCursorScreenPos.X)) {
			select_start = TextBufferCursor.Duplicate(Cursor[0]);
			select_end = TextBufferCursor.Duplicate(SelectStart);
		}
		else {
			select_start = TextBufferCursor.Duplicate(SelectStart);
			select_end = TextBufferCursor.Duplicate(Cursor[0]);
		}
	}
	TextVRAM.SetTextColor(COLOR_NORMALTEXT);
	TextVRAM.SetBackgroundColor(COLOR_NORMALTEXT_BG);

	// �e�L�X�gVRAM�ւ̏�������
	// �K�v�Ȃ犄�荞�݋֎~�Ƃ��g������
	TextVRAM.ClearScreen();
	text_vram_pixel_t* vp = TextVRAM.GetVramPtr();
	text_buffer_cursor_t bp = TextBufferCursor.Duplicate(DisplayLeftTop[0]);
	screen_pos_t sp = { 0,0 };
	while (sp.Y < EDITOR_SCREEN_HEIGHT) {
		// �I��͈͂̎n�_/�I�_�ɓ��B���Ă���F�ݒ�ύX
		if (SelectStart != INVALID_TEXTBUFFER_CURSOR) {
			if (TextBuffer_EqualCursor(bp, select_start)) cl = COLOR_AREASELECTTEXT;
			if (TextBuffer_EqualCursor(bp, select_end)) cl = COLOR_NORMALTEXT;
		}
		char16_t ch = TextBuffer_TakeCharacatorOnCursor(bp);
		vp[sp.Y * VWIDTH_X + sp.X].ch = ch;
		vp[sp.Y * VWIDTH_X + sp.X].color = cl;
		//vp[sp.Y * VWIDTH_X + sp.X].bgcolor = bc;
		if (iterate_cursor_with_screen_pos(&sp, bp) == false)
		{
			break;
		}
	}
	TextBufferCursor.Dispose(bp);
	TextBufferCursor.Dispose(select_start);
	TextBufferCursor.Dispose(select_end);

	EnableInterrupt();

}


//
//�N���b�v�{�[�h
//
static struct {
	// �o�b�t�@
	char16_t buffer[EDITOR_SCREEN_WIDTH * EDITOR_SCREEN_HEIGHT];
	// �i�[����Ă��镶����
	size32_t	length;
} Clipboard;

// �I��͈͂��N���b�v�{�[�h�ɃR�s�[
void Clipboard_CopyTo(void) {
	text_buffer_cursor_t bp1 = TextBufferCursor.Allocate();
	text_buffer_cursor_t bp2 = TextBufferCursor.Allocate();

	//�͈͑I�����[�h�̏ꍇ�A�J�n�ʒu�ƏI���̑O�㔻�f����
	//bp1,ix1���J�n�ʒu�Abp2,ix2���I���ʒu�ɐݒ�
	if (CursorScreenPos[0].Y < SelectStartCursorScreenPos.Y ||
		(CursorScreenPos[0].Y == SelectStartCursorScreenPos.Y && CursorScreenPos[0].X < SelectStartCursorScreenPos.X)) {
		TextBufferCursor.Copy(bp1, Cursor[0]);
		TextBufferCursor.Copy(bp2, SelectStart);
	}
	else {
		TextBufferCursor.Copy(bp1, SelectStart);
		TextBufferCursor.Copy(bp2, Cursor[0]);
	}

	char16_t *pd = Clipboard.buffer;
	while (TextBuffer_EqualCursor(bp1, bp2) == false) {
		*pd++ = TextBuffer_TakeCharacatorOnCursor(bp1);
		TextBuffer_CursorForward(bp1);
	}
	Clipboard.length = pd - Clipboard.buffer;
	TextBufferCursor.Dispose(bp1);
	TextBufferCursor.Dispose(bp2);
}

void Clipboard_PasteFrom(void) {
	// �N���b�v�{�[�h����\��t��

	char16_t *p = Clipboard.buffer;
	for (int n = Clipboard.length; n > 0; n--) {
		if (InsertCharactor(*p++) == false) {
			break;
		}
		cursor_right();//��ʏ�A�o�b�t�@��̃J�[�\���ʒu��1���Ɉړ�
	}
}

//�͈͑I�����[�h�J�n���̃J�[�\���J�n�ʒu�O���[�o���ϐ��ݒ�
void set_areamode() {
	TextBufferCursor.Dispose(SelectStart);
	SelectStart = TextBufferCursor.Duplicate(Cursor[0]);
	SelectStartCursorScreenPos = CursorScreenPos[0];
}

//�J�[�\���֘A�O���[�o���ϐ����ꎞ���
void save_cursor(void) {
	TextBufferCursor.Copy(Cursor[1], Cursor[0]);
	TextBufferCursor.Copy(DisplayLeftTop[1], DisplayLeftTop[0]);
	CursorScreenPos[1] = CursorScreenPos[0];
}

//�J�[�\���֘A�O���[�o���ϐ����ꎞ���ꏊ����߂�
void restore_cursor(void) {
	TextBufferCursor.Copy(Cursor[0], Cursor[1]);
	TextBufferCursor.Copy(DisplayLeftTop[0], DisplayLeftTop[1]);
	CursorScreenPos[0] = CursorScreenPos[1];
}

void inittextbuf(void) {
	// �e�L�X�g�o�b�t�@�̏�����
	TextBuffer_Initialize();

	// �J�[�\�������Z�b�g
	TextBufferCursor.Dispose(Cursor[0]);
	TextBufferCursor.Dispose(Cursor[1]);
	TextBufferCursor.Dispose(DisplayLeftTop[0]);
	TextBufferCursor.Dispose(DisplayLeftTop[1]);
	TextBufferCursor.Dispose(SelectStart);

	Cursor[0] = TextBufferCursor.Allocate();
	Cursor[1] = TextBufferCursor.Allocate();
	DisplayLeftTop[0] = TextBufferCursor.Allocate();
	DisplayLeftTop[1] = TextBufferCursor.Allocate();
	SelectStart = INVALID_TEXTBUFFER_CURSOR;

	// �J�[�\���ʒu��擪��
	cursor_top();

	//�ҏW�ς݃t���O�N���A
	edited = false;
}

int savetextfile(char16_t *filename) {
	// �e�L�X�g�o�b�t�@���e�L�X�g�t�@�C���ɏ�������
	// �������ݐ�����0�A���s�ŃG���[�R�[�h�i�����j��Ԃ�

	ptr_t fp = File.Open(filename, FileMode_CreateNew, FileAccess_Write);
	if (fp == NULL) {
		return ERR_CANTFILEOPEN;
	}

	text_buffer_cursor_t bp = TextBufferCursor.Allocate();

	int er = 0;//�G���[�R�[�h
	do {
		char16_t *pd = filebuf;
		int n = 0;
		while (n < FILEBUFSIZE - 1) {
			char16_t ch = TextBuffer_TakeCharacatorOnCursor(bp);
			if (ch == 0x0000)
			{
				break;
			}
			if (ch == L'\n')
			{
				// ���s������\n->\r\n�ɕύX
				*pd++ = L'\r';
			}
			*pd++ = ch;
			TextBuffer_CursorForward(bp);
			n++;
		}
		if (n > 0) {
			int i = File.Write(fp, (uint8_t*)filebuf, sizeof(char16_t) * n) / sizeof(char16_t);
			if (i != n) {
				er = ERR_CANTWRITEFILE;
			}
		}
	} while (TextBuffer_EndOfString(bp) == false);

	File.Close(fp);
	TextBufferCursor.Dispose(bp);
	return er;
}

// �e�L�X�g�t�@�C�����e�L�X�g�o�b�t�@�ɓǂݍ���
// �ǂݍ��ݐ�����0�A���s�ŃG���[�R�[�h�i�����j��Ԃ�
int LoadTextFile(char16_t *filename) {
#if defined(_MSC_VER)
	ptr_t fp = File.Open(filename, FileMode_Open, FileAccess_Read);
	if (fp == NULL) {
		return ERR_CANTFILEOPEN;
	}

	inittextbuf();

	text_buffer_cursor_t bp = TextBufferCursor.Allocate();

	int n;
	int er = 0;
	do {
		n = File.Read(fp, (uint8_t*)filebuf, sizeof(char16_t) * FILEBUFSIZE) / sizeof(char16_t);
		char16_t *ps = filebuf;
		for (int i = 0; i < n; i++) {
			if (*ps == L'\r') {
				//���s�R�[�h\r\n��\r��ǂݔ�΂�
				ps++;
			}
			else {
				if (TextBuffer_InsertCharacatorOnCursor(bp, *ps++) != 0) {
					er = ERR_FILETOOBIG;
					break;
				}
				TextBuffer_CursorForward(bp);

			}
		}
	} while (n == FILEBUFSIZE && er == 0);
	File.Close(fp);
	TextBufferCursor.Dispose(bp);
	if (er) {
		inittextbuf();//�G���[�����̏ꍇ�o�b�t�@�N���A
	}
	return er;
#else
	return ERR_CANTFILEOPEN;
#endif
}

//

void save_as(void) {
	// ���݂̃e�L�X�g�o�b�t�@�̓��e���t�@�C������t����SD�J�[�h�ɕۑ�
	// �t�@�C�����̓O���[�o���ϐ�currentfile[]
	// �t�@�C�����̓L�[�{�[�h����ύX�\
	// ���������ꍇcurrentfile���X�V

	int er;
	TextVRAM.ClearScreen();
	{
		screen_pos_t vcp = { 0,0 };
		TextVRAM.SetCursorPosition(&vcp);
		TextVRAM.SetTextColor(COLOR_NORMALTEXT);
	}
	TextVRAM.puts(L"Save To SD Card\n");

	//currentfile����tempfile�ɃR�s�[
	char16_t tempfile[FILE_NAME_BUF_LEN];
	Memory.Copy(tempfile, CurrentFileName, sizeof(CurrentFileName));

	while (1) {
		// �t�@�C�������͂̃v�����v�g�\��
		TextVRAM.puts(L"File Name + [Enter] / [ESC]\n");
		if (KeyInput.gets(tempfile, sizeof(tempfile) / sizeof(tempfile[0])) == false) {
			return; //ESC�L�[�������ꂽ
		}

		if (tempfile[0] == 0) continue; //NULL������̏ꍇ
		TextVRAM.puts(L"Writing...\n");
		er = savetextfile(tempfile); //�t�@�C���ۑ��Aer:�G���[�R�[�h
		if (er == 0) {
			TextVRAM.puts(L"OK");
			//tempfile����currentfile�ɃR�s�[���ďI��
			Memory.Copy(CurrentFileName, tempfile, sizeof(tempfile));
			edited = false; //�ҏW�ς݃t���O�N���A
			WaitMS(1000);//1�b�҂�
			return;
		}
		TextVRAM.SetTextColor(COLOR_ERRORTEXT);
		if (er == ERR_CANTFILEOPEN) {
			TextVRAM.puts(L"Bad File Name or File Error\n");
		}
		else {
			TextVRAM.puts(L"Cannot Write\n");
		}
		TextVRAM.SetTextColor(COLOR_NORMALTEXT);
		TextVRAM.puts(L"Retry:[Enter] / Quit:[ESC]\n");
		while (1) {
			if (KeyInput.getch() && Keyboard_GetCurrentVKeyCode())
			{
				uint8_t vk = Keyboard_GetCurrentVKeyCode();
				if (vk == VKEY_RETURN || vk == VKEY_SEPARATOR) {
					break;
				}
				if (vk == VKEY_ESCAPE) {
					return;
				}
			}
		}
	}
}

// SD�J�[�h������t�@�C����I�����ēǂݍ���
int selectfile(void) {
#if defined(_MSC_VER)
	// currenfile[]�Ƀt�@�C�������L��
	// �߂�l�@0�F�ǂݍ��݂��s�����@-1�F�ǂݍ��݂Ȃ�
	int i, er;

	EnumerateFilesContext_t ctx;

	//�t�@�C���̈ꗗ��SD�J�[�h����ǂݏo��
	TextVRAM.ClearScreen();
	if (edited) {
		//�ŏI�ۑ���ɕҏW�ς݂̏ꍇ�A�ۑ��̊m�F
		TextVRAM.SetTextColor(COLOR_NORMALTEXT);
		TextVRAM.puts(L"Save Editing File?\n");
		TextVRAM.puts(L"Save:[Enter] / Not Save:[ESC]\n");
		while (1) {
			KeyInput.getch();
			uint8_t vk = Keyboard_GetCurrentVKeyCode();
			if (vk == VKEY_RETURN || vk == VKEY_SEPARATOR) {
				save_as();
				break;
			}
			else if (vk == VKEY_ESCAPE) {
				break;
			}
		}
	}
	while (Directory.EnumerateFiles.Create(&ctx, L"") == false) {
		TextVRAM.SetTextColor(COLOR_ERRORTEXT);
		TextVRAM.puts(L"No File Found\n");
		TextVRAM.puts(L"Retry:[Enter] / Quit:[ESC]\n");
		while (1) {
			KeyInput.getch();
			uint8_t vk = Keyboard_GetCurrentVKeyCode();
			if (vk == VKEY_RETURN || vk == VKEY_SEPARATOR) {
				break;
			}
			else if (vk == VKEY_ESCAPE) {
				return -1;
			}
		}
	}


	uint16_t filenum = 0;
	while (Directory.EnumerateFiles.MoveNext(&ctx) && (filenum < MAXFILENUM)) {
		//filenames[]�Ƀt�@�C�����̈ꗗ��ǂݍ���
		const char16_t *filename = Directory.EnumerateFiles.GetCurrent(&ctx);
		size32_t sz = sizeof(filenames[filenum]) / sizeof(filenames[filenum][0]);
		Utf16.NCopy(filenames[filenum], filename, sz);
		filenames[filenum][sz - 1] = 0;
		filenum++;
	}

	Directory.EnumerateFiles.Dispose(&ctx);

	//�t�@�C���ꗗ����ʂɕ\��
	TextVRAM.ClearScreen();
	{
		screen_pos_t vcp = { 0,0 };
		TextVRAM.SetCursorPosition(&vcp);
		TextVRAM.SetTextColor(4);
	}
	TextVRAM.puts(L"Select File + [Enter] / [ESC]\n");
	for (i = 0; i < filenum; i++) {
		int x = (i & 1) * 15U + 1U;
		int y = i / 2U + 1U;
		{
			screen_pos_t vcp = { x, y };
			TextVRAM.SetCursorPosition(&vcp);
			TextVRAM.SetTextColor(COLOR_NORMALTEXT);
		}
		TextVRAM.puts(filenames[i]);
	}

	//�t�@�C���̑I��
	i = 0;
	{
		screen_pos_t vcp = { 0, 1 };
		TextVRAM.SetCursorPosition(&vcp);
		TextVRAM.SetTextColor(5);
	}
	TextVRAM.putch('>'); // Right Arrow
	{
		screen_pos_t vcp;
		TextVRAM.GetCursorPosition(&vcp);
		vcp.X--;
		TextVRAM.SetCursorPosition(&vcp);
	}
	while (1) {
		KeyInput.getch();
		uint8_t vk = Keyboard_GetCurrentVKeyCode();
		if (vk == 0) { continue; }
		TextVRAM.putch(' ');
		{
			screen_pos_t vcp = { 0,EDITOR_SCREEN_HEIGHT - 1 };
			TextVRAM.SetCursorPosition(&vcp);
			TextVRAM.SetTextColor(COLOR_NORMALTEXT);
		}
		for (uint8_t x = 0; x < EDITOR_SCREEN_WIDTH - 1; x++) TextVRAM.putch(' '); //�ŉ��s�̃X�e�[�^�X�\��������
		switch (vk) {
		case VKEY_UP:
		case VKEY_NUMPAD8:
			//����L�[
			if (i >= 2) i -= 2;
			break;
		case VKEY_DOWN:
		case VKEY_NUMPAD2:
			//�����L�[
			if (i + 2 < filenum) i += 2;
			break;
		case VKEY_LEFT:
		case VKEY_NUMPAD4:
			//�����L�[
			if (i > 0) i--;
			break;
		case VKEY_RIGHT:
		case VKEY_NUMPAD6:
			//�E���L�[
			if (i + 1 < filenum) i++;
			break;
		case VKEY_RETURN: //Enter�L�[
		case VKEY_SEPARATOR: //�e���L�[��Enter
						   //�t�@�C��������B�ǂݍ���ŏI��
			er = LoadTextFile(filenames[i]); //�e�L�X�g�o�b�t�@�Ƀt�@�C���ǂݍ���
			if (er == 0) {
				//currenfile[]�ϐ��Ƀt�@�C�������R�s�[���ďI��
				Memory.Copy(CurrentFileName, filenames[i], sizeof(filenames[i]));
				return 0;
			}
			else {
				// �G���[����
				screen_pos_t vcp = { 0,EDITOR_SCREEN_HEIGHT - 1 };
				TextVRAM.SetCursorPosition(&vcp);
				TextVRAM.SetTextColor(COLOR_ERRORTEXT);
				if (er == ERR_CANTFILEOPEN) TextVRAM.puts(L"Cannot Open File");
				else if (er == ERR_FILETOOBIG) TextVRAM.puts(L"File Too Big");
			}
			break;
		case VKEY_ESCAPE:
			//ESC�L�[�A�t�@�C���ǂݍ��݂����I��
			return -1;
		}
		{
			screen_pos_t vcp = { (i & 1) * 15, i / 2 + 1 };
			TextVRAM.SetCursorPosition(&vcp);
			TextVRAM.SetTextColor(5);
		}

		TextVRAM.putch('>'); // Right Arrow
		{
			screen_pos_t vcp;
			TextVRAM.GetCursorPosition(&vcp);
			vcp.X--;
			TextVRAM.SetCursorPosition(&vcp);
		}
	}
#else
	return -1;
#endif
	}
void newtext(void) {
	// �V�K�e�L�X�g�쐬
	if (edited) {
		//�ŏI�ۑ���ɕҏW�ς݂̏ꍇ�A�ۑ��̊m�F
		TextVRAM.ClearScreen();
		TextVRAM.SetTextColor(COLOR_NORMALTEXT);
		TextVRAM.puts(L"Save Editing File?\n");
		TextVRAM.puts(L"Save:[Enter] / Not Save:[ESC]\n");
		while (1) {
			KeyInput.getch();
			uint8_t vk = Keyboard_GetCurrentVKeyCode();
			if (vk == VKEY_RETURN || vk == VKEY_SEPARATOR) {
				save_as();
				break;
			}
			else if (vk == VKEY_ESCAPE) {
				break;
			}
		}
	}
	inittextbuf(); //�e�L�X�g�o�b�t�@������
	CurrentFileName[0] = 0; //��ƒ��t�@�C�����N���A
}
void displaybottomline(void) {
	//�G�f�B�^�[��ʍŉ��s�̕\��
	{
		screen_pos_t vcp = { 0, EDITOR_BOTTOM_STATUSLINE };
		TextVRAM.SetCursorPosition(&vcp);
		TextVRAM.SetTextColor(COLOR_BOTTOMLINE);
		TextVRAM.SetBackgroundColor(COLOR_BOTTOMLINE_BG);
	}
	TextVRAM.puts(L"F1:LOAD F2:SAVE   F4:NEW ");
	TextVRAM.putdigit2(TextBuffer_GetCapacity() - TextBuffer_GetTotalLength(), 5);
	TextVRAM.FillBackgroundColor(0, EDITOR_BOTTOM_STATUSLINE, EDITOR_SCREEN_WIDTH, COLOR_BOTTOMLINE_BG);
}

void normal_code_process(char16_t k) {
	// �ʏ핶�����͏���
	// k:���͂��ꂽ�����R�[�h

	edited = true; //�ҏW�ς݃t���O��ݒ�

	if (KeyInput.GetInsertMode() || k == '\n' || SelectStart != INVALID_TEXTBUFFER_CURSOR) { // �}�����[�h�̏ꍇ
		// �I��͈͂��폜
		if (SelectStart != INVALID_TEXTBUFFER_CURSOR) {
			deletearea();
		}
		if (InsertCharactor(k)) {
			cursor_right();//��ʏ�A�o�b�t�@��̃J�[�\���ʒu��1���Ɉړ�
		}
	}
	else { //�㏑�����[�h
		if (OverwriteCharactor(k)) {
			cursor_right();//��ʏ�A�o�b�t�@��̃J�[�\���ʒu��1���Ɉړ�
		}
	}
}

void control_code_process(uint8_t k, uint8_t sh) {
	// ���䕶�����͏���
	// k:���䕶���̉��z�L�[�R�[�h
	// sh:�V�t�g�֘A�L�[���

	save_cursor(); //�J�[�\���֘A�ϐ��ޔ��i�J�[�\���ړ��ł��Ȃ������ꍇ�߂����߁j

	switch (k) {
	case VKEY_LEFT:
	case VKEY_NUMPAD4:
		//�V�t�g�L�[�������Ă��Ȃ���Δ͈͑I�����[�h�����iNumLock�{�V�t�g�{�e���L�[�ł������j
		if ((sh & CHK_SHIFT) == 0 || (k == VKEY_NUMPAD4) && (sh & CHK_NUMLK)) {
			TextBufferCursor.Dispose(SelectStart);
			SelectStart = INVALID_TEXTBUFFER_CURSOR;
		}
		else if (SelectStart != INVALID_TEXTBUFFER_CURSOR) {
			set_areamode(); //�͈͑I�����[�h�łȂ���Δ͈͑I�����[�h�J�n
		}
		if (sh & CHK_CTRL) {
			//CTRL�{������Home
			cursor_home();
			break;
		}
		cursor_left();
		//if (SelectStart.Buffer != NULL && (DisplayLeftTop[0].Buffer != DisplayLeftTop[1].Buffer || DisplayLeftTop[0].Index != DisplayLeftTop[1].Index)) {
		if (SelectStart != INVALID_TEXTBUFFER_CURSOR && !TextBuffer_EqualCursor(DisplayLeftTop[0], DisplayLeftTop[1])) {
			//�͈͑I�����[�h�ŉ�ʃX�N���[�����������ꍇ
			if (SelectStartCursorScreenPos.Y < EDITOR_SCREEN_HEIGHT - 1) {
				SelectStartCursorScreenPos.Y++; //�͈̓X�^�[�g�ʒu���X�N���[��
			}
			else {
				restore_cursor(); //�J�[�\���ʒu��߂��i��ʔ͈͊O�͈̔͑I���֎~�j
			}
		}
		break;
	case VKEY_RIGHT:
	case VKEY_NUMPAD6:
		//�V�t�g�L�[�������Ă��Ȃ���Δ͈͑I�����[�h�����iNumLock�{�V�t�g�{�e���L�[�ł������j
		if ((sh & CHK_SHIFT) == 0 || (k == VKEY_NUMPAD6) && (sh & CHK_NUMLK)) {
			TextBufferCursor.Dispose(SelectStart);
			SelectStart = INVALID_TEXTBUFFER_CURSOR;
		}
		else if (SelectStart == INVALID_TEXTBUFFER_CURSOR) {
			set_areamode(); //�͈͑I�����[�h�łȂ���Δ͈͑I�����[�h�J�n
		}
		if (sh & CHK_CTRL) {
			//CTRL�{�E����End
			cursor_end();
			break;
		}
		cursor_right();
		if (SelectStart != INVALID_TEXTBUFFER_CURSOR && (TextBuffer_EqualCursor(DisplayLeftTop[0], DisplayLeftTop[1]) == false)) {
			//�͈͑I�����[�h�ŉ�ʃX�N���[�����������ꍇ
			if (SelectStartCursorScreenPos.Y > 0) SelectStartCursorScreenPos.Y--; //�͈̓X�^�[�g�ʒu���X�N���[��
			else restore_cursor(); //�J�[�\���ʒu��߂��i��ʔ͈͊O�͈̔͑I���֎~�j
		}
		break;
	case VKEY_UP:
	case VKEY_NUMPAD8:
		//�V�t�g�L�[�������Ă��Ȃ���Δ͈͑I�����[�h�����iNumLock�{�V�t�g�{�e���L�[�ł������j
		if ((sh & CHK_SHIFT) == 0 || (k == VKEY_NUMPAD8) && (sh & CHK_NUMLK)) {
			TextBufferCursor.Dispose(SelectStart);
			SelectStart = INVALID_TEXTBUFFER_CURSOR;
		}
		else if (SelectStart == INVALID_TEXTBUFFER_CURSOR) {
			set_areamode(); //�͈͑I�����[�h�łȂ���Δ͈͑I�����[�h�J�n
		}
		cursor_up();
		if (SelectStart != INVALID_TEXTBUFFER_CURSOR && (TextBuffer_EqualCursor(DisplayLeftTop[0], DisplayLeftTop[1]) == false)) {
			//�͈͑I�����[�h�ŉ�ʃX�N���[�����������ꍇ
			if (SelectStartCursorScreenPos.Y < EDITOR_SCREEN_HEIGHT - 1) SelectStartCursorScreenPos.Y++; //�͈̓X�^�[�g�ʒu���X�N���[��
			else restore_cursor(); //�J�[�\���ʒu��߂��i��ʔ͈͊O�͈̔͑I���֎~�j
		}
		break;
	case VKEY_DOWN:
	case VKEY_NUMPAD2:
		//�V�t�g�L�[�������Ă��Ȃ���Δ͈͑I�����[�h�����iNumLock�{�V�t�g�{�e���L�[�ł������j
		if ((sh & CHK_SHIFT) == 0 || (k == VKEY_NUMPAD2) && (sh & CHK_NUMLK)) {
			TextBufferCursor.Dispose(SelectStart);
			SelectStart = INVALID_TEXTBUFFER_CURSOR;
		}
		else if (SelectStart == INVALID_TEXTBUFFER_CURSOR) {
			set_areamode(); //�͈͑I�����[�h�łȂ���Δ͈͑I�����[�h�J�n
		}
		cursor_down();
		if (SelectStart != INVALID_TEXTBUFFER_CURSOR && (TextBuffer_EqualCursor(DisplayLeftTop[0], DisplayLeftTop[1]) == false)) {
			//�͈͑I�����[�h�ŉ�ʃX�N���[�����������ꍇ
			if (SelectStartCursorScreenPos.Y > 0) SelectStartCursorScreenPos.Y--; //�͈̓X�^�[�g�ʒu���X�N���[��
			else restore_cursor(); //�J�[�\���ʒu��߂��i��ʔ͈͊O�͈̔͑I���֎~�j
		}
		break;
	case VKEY_HOME:
	case VKEY_NUMPAD7:
		//�V�t�g�L�[�������Ă��Ȃ���Δ͈͑I�����[�h�����iNumLock�{�V�t�g�{�e���L�[�ł������j
		if ((sh & CHK_SHIFT) == 0 || (k == VKEY_NUMPAD7) && (sh & CHK_NUMLK)) {
			TextBufferCursor.Dispose(SelectStart);
			SelectStart = INVALID_TEXTBUFFER_CURSOR;
		}
		else if (SelectStart == INVALID_TEXTBUFFER_CURSOR) {
			set_areamode(); //�͈͑I�����[�h�łȂ���Δ͈͑I�����[�h�J�n
		}
		cursor_home();
		break;
	case VKEY_END:
	case VKEY_NUMPAD1:
		//�V�t�g�L�[�������Ă��Ȃ���Δ͈͑I�����[�h�����iNumLock�{�V�t�g�{�e���L�[�ł������j
		if ((sh & CHK_SHIFT) == 0 || (k == VKEY_NUMPAD1) && (sh & CHK_NUMLK)) {
			TextBufferCursor.Dispose(SelectStart);
			SelectStart = INVALID_TEXTBUFFER_CURSOR;
		}
		else if (SelectStart == INVALID_TEXTBUFFER_CURSOR) {
			set_areamode(); //�͈͑I�����[�h�łȂ���Δ͈͑I�����[�h�J�n
		}
		cursor_end();
		break;
	case VKEY_PRIOR: // PageUp�L�[
	case VKEY_NUMPAD9:
		//�V�t�g�{PageUp�͖����iNumLock�{�V�t�g�{�u9�v�����j
		if ((sh & CHK_SHIFT) && ((k != VKEY_NUMPAD9) || ((sh & CHK_NUMLK) == 0))) {
			break;
		}
		//�͈͑I�����[�h����
		TextBufferCursor.Dispose(SelectStart);
		SelectStart = INVALID_TEXTBUFFER_CURSOR;
		cursor_pageup();
		break;
	case VKEY_NEXT: // PageDown�L�[
	case VKEY_NUMPAD3:
		//�V�t�g�{PageDown�͖����iNumLock�{�V�t�g�{�u3�v�����j
		if ((sh & CHK_SHIFT) && ((k != VKEY_NUMPAD3) || ((sh & CHK_NUMLK) == 0))) {
			break;
		}
		//�͈͑I�����[�h����
		TextBufferCursor.Dispose(SelectStart);
		SelectStart = INVALID_TEXTBUFFER_CURSOR;
		cursor_pagedown();
		break;
	case VKEY_DELETE: //Delete�L�[
	case VKEY_DECIMAL: //�e���L�[�́u.�v
		edited = true; //�ҏW�ς݃t���O
		if (SelectStart != INVALID_TEXTBUFFER_CURSOR) {
			deletearea();//�I��͈͂��폜
		}
		else {
			TextBuffer_DeleteCharacatorOnCursor(Cursor[0]);
			CursorScreenPos[0] = calc_screen_pos_from_line_head(DisplayLeftTop[0], Cursor[0]);
		}
		break;
	case VKEY_BACK: //BackSpace�L�[
		edited = true; //�ҏW�ς݃t���O
		if (SelectStart != INVALID_TEXTBUFFER_CURSOR) {
			deletearea();//�I��͈͂��폜
			break;
		}
		if (TextBuffer_StartOfString(Cursor[0])) {
			break; //�o�b�t�@�擪�ł͖���
		}
		cursor_left();
		TextBuffer_DeleteCharacatorOnCursor(Cursor[0]);
		CursorScreenPos[0] = calc_screen_pos_from_line_head(DisplayLeftTop[0], Cursor[0]);
		break;
	case VKEY_INSERT:
	case VKEY_NUMPAD0:
		KeyInput.SetInsertMode(!KeyInput.GetInsertMode()); //�}�����[�h�A�㏑�����[�h��؂�ւ�
		break;
	case 'C':
		//CTRL+C�A�N���b�v�{�[�h�ɃR�s�[
		if (SelectStart != INVALID_TEXTBUFFER_CURSOR && (sh & CHK_CTRL)) {
			Clipboard_CopyTo();
		}
		break;
	case 'X':
		//CTRL+X�A�N���b�v�{�[�h�ɐ؂���
		if (SelectStart != INVALID_TEXTBUFFER_CURSOR && (sh & CHK_CTRL)) {
			Clipboard_CopyTo();
			deletearea(); //�I��͈͂̍폜
			edited = true; //�ҏW�ς݃t���O
		}
		break;
	case 'V':
		//CTRL+V�A�N���b�v�{�[�h����\��t��
		if ((sh & CHK_CTRL) == 0) break;
		if (Clipboard.length == 0) break;
		edited = true; //�ҏW�ς݃t���O
		if (SelectStart != INVALID_TEXTBUFFER_CURSOR) {
			//�͈͑I�����Ă��鎞�͍폜���Ă���\��t��
			if (TextBuffer_GetTotalLength() - countarea() + Clipboard.length <= TextBuffer_GetCapacity()) { //�o�b�t�@�󂫗e�ʃ`�F�b�N
				deletearea();//�I��͈͂��폜
				Clipboard_PasteFrom();//�N���b�v�{�[�h�\��t��
			}
		}
		else {
			if (TextBuffer_GetTotalLength() + Clipboard.length <= TextBuffer_GetCapacity()) { //�o�b�t�@�󂫗e�ʃ`�F�b�N
				Clipboard_PasteFrom();//�N���b�v�{�[�h�\��t��
			}
		}
		break;
	case 'S':
		//CTRL+S�ASD�J�[�h�ɕۑ�
		if ((sh & CHK_CTRL) == 0) break;
	case VKEY_F2: //F2�L�[
		save_as(); //�t�@�C������t���ĕۑ�
		break;
	case 'O':
		//CTRL+O�A�t�@�C���ǂݍ���
		if ((sh & CHK_CTRL) == 0) break;
	case VKEY_F1: //F1�L�[
				//F1�L�[�A�t�@�C���ǂݍ���
		selectfile();	//�t�@�C����I�����ēǂݍ���
		break;
	case 'N':
		//CTRL+N�A�V�K�쐬
		if ((sh & CHK_CTRL) == 0) break;
	case VKEY_F4: //F4�L�[
		newtext(); //�V�K�쐬
		break;
	}
}

#ifdef EXPERIMENTAL
static IME_Context context = { &roma2kana,{ 0 },{ 0 }, 0 };
static int ime = 0;

bool_t hook_ime(uint8_t ascii, uint8_t vkey, CTRLKEY_FLAG ctrlkey)
{
	if ((vkey == L' ') && (ctrlkey & CHK_CTRL)) {
		// �g�O��
		ime = (ime == 0) ? 1 : 0;
		IME_Reset(&context);
		return true;
	}
ReEval:
	switch (ime) {
	case 0:
		return false;
	case 1:
		if (vkey == L' ')
		{
			char16_t *o = context.pobox_output;
			context.pobox_search_next = 0;
			pobox_search(context.current, &o, 1, IME_BUF_MAX, context.pobox_search_next);
			ime = 2;
			goto ReEval;
		}
		if (vkey == VKEY_BACK)
		{
			if (context.currentlen > 0)
			{
				context.currentlen--;
				context.current[context.currentlen] = 0x0000;
				return true;
			}
			return false;
		}
		if (vkey == VKEY_ESCAPE)
		{
			context.currentlen = 0;
			context.current[0] = 0x0000;
			return true;
		}
		if (vkey == VKEY_RETURN || vkey == VKEY_SEPARATOR) {
			if (context.currentlen == 0)
			{
				return false;
			}
			// �ϊ��o�b�t�@�̓��e��}��

			char16_t *p = context.current;
			for (int n = context.currentlen; n > 0; n--) {
				if (InsertCharactor(*p) == false) {
					break;
				}
				cursor_right();//��ʏ�A�o�b�t�@��̃J�[�\���ʒu��1���Ɉړ�
				p++;
			}

			IME_Reset(&context);
			return true;
		}
		if (vkey == VKEY_F7)
		{
			// �J�^�J�i��
			char16_t *p = context.current;
			for (int n = context.currentlen; n > 0; n--) {
				*p = (0x3040 <= *p && *p <= 0x309F) ? *p + (0x30A0 - 0x3040) : *p;
				p++;
			}
			return true;
		}
		if (vkey == VKEY_F6)
		{
			// �Ђ炪�ȉ�
			char16_t *p = context.current;
			for (int n = context.currentlen; n > 0; n--) {
				*p = (0x30A0 <= *p && *p <= 0x30FF) ? *p -= (0x30A0 - 0x3040) : *p;
				p++;
			}
			return true;
		}

		if (ascii != 0x0000U)
		{
			IME_Update(&context, ascii);
			return true;
		}

		if (context.currentlen > 0)
		{
			// ���͒��͈���Ԃ�
			return true;
		}
		return false;
	case 2:
		if (vkey == L' ')
		{
			char16_t *o = context.pobox_output;
			context.pobox_search_next++;
			if (pobox_search(context.current, &o, 1, IME_BUF_MAX, context.pobox_search_next) == 0)
			{
				context.pobox_search_next = 0;
				pobox_search(context.current, &o, 1, IME_BUF_MAX, context.pobox_search_next);
			}
			return true;
		}
		if (vkey == VKEY_ESCAPE)
		{
			ime = 1;
			return true;
		}
		if (vkey == VKEY_RETURN || vkey == VKEY_SEPARATOR) {
			// �ϊ��o�b�t�@�̓��e��}��
			if (context.pobox_output[0] == L'\0')
			{
				TextVRAM.puts(context.current);
			}
			else
			{
				TextVRAM.puts(context.pobox_output);
			}

			for (char16_t *p = context.pobox_output[0] != L'\0' ? context.pobox_output : context.current; *p != L'\0'; p++) {
				if (InsertCharactor(*p) == false) {
					break;
				}
				cursor_right();//��ʏ�A�o�b�t�@��̃J�[�\���ʒu��1���Ɉړ�
			}
			ime = 1;
			IME_Reset(&context);
			return true;
		}
		if (vkey == VKEY_BACK)
		{
			// �ϊ��o�b�t�@�̓��e��}��
			if (context.pobox_output[0] == L'\0')
			{
				TextVRAM.puts(context.current);
			}
			else
			{
				TextVRAM.puts(context.pobox_output);
			}

			for (char16_t *p = context.pobox_output[0] != L'\0' ? context.pobox_output : context.current; *p != L'\0' && *(p + 1) != L'\0'; p++) {
				if (InsertCharactor(*p) == false) {
					break;
				}
				cursor_right();//��ʏ�A�o�b�t�@��̃J�[�\���ʒu��1���Ɉړ�
			}
			ime = 1;
			IME_Reset(&context);
			return true;
		}

		return true;

	default:
		return false;
	}
}
#endif

void texteditor(void) {
	Memory.Initialize();

	// �r�f�I�������N���A
	TextVRAM.Initialize();

	{
		screen_pos_t vcp = { 0,0 };
		TextVRAM.SetCursorPosition(&vcp);
		TextVRAM.SetTextColor(COLOR_NORMALTEXT);
		TextVRAM.SetBackgroundColor(COLOR_NORMALTEXT_BG);
	}
	// �L�[�{�[�h������	
	Keyboard_Initialize();

	// IME������
	IME_Initialize();


	inittextbuf(); //�e�L�X�g�o�b�t�@������
	TextVRAM.SetPaletteColor(COLOR_BOTTOMLINE_BG, 0x00, 0x20, 0x80);

	CurrentFileName[0] = 0; //��ƒ��t�@�C�����N���A

	KeyInput.SetInsertMode(true); //0:�㏑���A1:�}��
	Clipboard.length = 0; //�N���b�v�{�[�h�N���A


	text_buffer_cursor_t c = TextBufferCursor.Allocate();
	uint8_t ch[] = { 0x54, 0x00, 0x69, 0x00, 0x6e, 0x00, 0x79, 0x00, 0x54, 0x00, 0x65, 0x00, 0x78, 0x00, 0x74, 0x00, 0x45, 0x00, 0x64, 0x00, 0x69, 0x00, 0x74, 0x00, 0x6f, 0x00, 0x72, 0x00, 0xe5, 0x65, 0x2C, 0x67, 0x9e, 0x8a, 0xfe, 0x5b, 0xdc, 0x5f, 0x48, 0x72, 0x00, 0x00 };
	for (char16_t *p = (char16_t *)ch; *p != 0x0000; ++p) {
		TextBuffer_InsertCharacatorOnCursor(c, *p);
		TextBuffer_CursorForward(c);
	}

	while (1) {
		redraw();//��ʍĕ`��
		displaybottomline(); //��ʍŉ��s�Ƀt�@���N�V�����L�[�@�\�\��

		screen_pos_t vcp = { CursorScreenPos[0].X, CursorScreenPos[0].Y };
		TextVRAM.SetCursorPosition(&vcp);

#ifdef EXPERIMENTAL
		switch (ime)
		{
		case 0:
			break;
		case 1:
			TextVRAM.SetTextColor(COLOR_BOTTOMLINE);
			TextVRAM.SetBackgroundColor(COLOR_NORMALTEXT_BG);
			TextVRAM.puts(context.current);
			break;
		case 2:
			TextVRAM.SetTextColor(COLOR_BOTTOMLINE);
			TextVRAM.SetBackgroundColor(COLOR_NORMALTEXT_BG);
			if (context.pobox_output[0] == '\0')
			{
				TextVRAM.puts(context.current);
			}
			else
			{
				TextVRAM.puts(context.pobox_output);
			}
			break;
		default:
			break;

		}

		//		TextVRAM.SetCursorPosition(&vcp);
#endif

		TextVRAM.SetTextColor(COLOR_NORMALTEXT);
		TextVRAM.SetBackgroundColor(COLOR_NORMALTEXT_BG);



		while (1) {
			//�L�[���͑҂����[�v
			WaitMS(1000 / 60);  //60����1�b�E�F�C�g
			if (Keyboard_ReadKey() && Keyboard_GetCurrentVKeyCode()) {
				break;  // ���z�L�[���擾�ł����烋�[�v���甲����
			}
			//if (SelectStart.Buffer == NULL) {
			//	GCTextBufferOne(); //1�o�C�g�K�x�[�W�R���N�V�����i�͈͑I�����͂��Ȃ��j
			//}
		}

		uint8_t k1 = Keyboard_GetCurrentAsciiCode();
		uint8_t k2 = Keyboard_GetCurrentVKeyCode();
		CTRLKEY_FLAG sh = Keyboard_GetCurrentCtrlKeys();             //sh:�V�t�g�֘A�L�[���

#ifdef EXPERIMENTAL
		if (hook_ime(k1, k2, sh) == false) {

			//Enter�����͒P���ɉ��s��������͂Ƃ���
			if (k2 == VKEY_RETURN || k2 == VKEY_SEPARATOR) {
				k1 = '\n';
			}
			if (k2 == VKEY_TAB) {
				k1 = '\t';
			}
			if (k1) {
				//�ʏ핶�������͂��ꂽ�ꍇ
				normal_code_process(k1);
			}
			else {
				//���䕶�������͂��ꂽ�ꍇ
				control_code_process(k2, sh);
			}
		}
#else

		//Enter�����͒P���ɉ��s��������͂Ƃ���
		if (k2 == VKEY_RETURN || k2 == VKEY_SEPARATOR) {
			k1 = '\n';
		}
		if (k1) {
			//�ʏ핶�������͂��ꂽ�ꍇ
			normal_code_process(k1);
		}
		else {
			//���䕶�������͂��ꂽ�ꍇ
			control_code_process(k2, sh);
		}
#endif

		if (SelectStart != INVALID_TEXTBUFFER_CURSOR &&
			CursorScreenPos[0].X == SelectStartCursorScreenPos.X &&
			CursorScreenPos[0].Y == SelectStartCursorScreenPos.Y) {
			//�I��͈͂̊J�n�ƏI�����d�Ȃ�����͈͑I�����[�h����
			TextBufferCursor.Dispose(SelectStart);
			SelectStart = INVALID_TEXTBUFFER_CURSOR;
		}
	}
}


