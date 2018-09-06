/*
 * �e�L�X�gVRAM
 */

#include "TextVRAM.h"
#include "../pico/pico.memory.h"

/**
 * @brief �J���[�p���b�g
 */
static color_t ColorPalette[256];

/**
 * @brief ��f
 */
static text_vram_pixel_t Pixels[VWIDTH_X*WIDTH_Y];

/**
* @brief ���ݐݒ肳��Ă���w�i�F
 */
static uint8_t BackgroundColor;

/**
 * @brief Pixels��̃J�[�\���ʒu
 */
static text_vram_pixel_t *CursorPtr;

/**
 * @brief ���ݐݒ肳��Ă��镶���F
 */
static uint8_t TextColor;

static void					TextVRAM_ClearScreen(void);
static void					TextVRAM_SetPaletteColor(uint8_t n, uint8_t b, uint8_t r, uint8_t g);
static void					TextVRAM_SetBackgroundColor(uint8_t c);
static void					TextVRAM_Initialize(void);
static text_vram_pixel_t *	TextVRAM_GetCursorPtr(void);
static void					TextVRAM_GetCursorPosition(screen_pos_t *pos);
static void					TextVRAM_SetCursorPosition(const screen_pos_t *pos);
static text_vram_pixel_t *	TextVRAM_GetVramPtr(void);
static const color_t *		TextVRAM_GetPalettePtr(void);
static uint8_t				TextVRAM_GetBackgroundColor(void);
static uint8_t				TextVRAM_GetTextColor(void);
static void					TextVRAM_SetTextColor(uint8_t c);
static void					TextVRAM_Scroll(void);
static void					TextVRAM_putch(char16_t n);
static void					TextVRAM_puts(const char16_t *s);
static void					TextVRAM_putdigit(uint32_t n);
static void					TextVRAM_putdigit2(uint32_t n, uint8_t e);
static void					TextVRAM_FillTextColor(uint8_t sx, uint8_t sy, uint16_t length, uint8_t palette);
static void					TextVRAM_FillBackgroundColor(uint8_t sx, uint8_t sy, uint16_t length, uint8_t palette);
static void					TextVRAM_CalcCursorPosition(uint16_t *head, uint16_t *cur, screen_pos_t *p, size32_t width);

// ��ʃN���A
static void TextVRAM_ClearScreen(void)
{
	Memory.Fill.Uint8((uint8_t*)Pixels, 0, sizeof(Pixels));
	CursorPtr = Pixels;
}

static void TextVRAM_SetPaletteColor(uint8_t index, uint8_t b, uint8_t r, uint8_t g)
{
	// �J���[�p���b�g�ݒ�
	// n:�p���b�g�ԍ��Ar,g,b:0�`255
	color_t color;
	color.bgra = (b << 16U) | (g << 8U) | (r << 0U);
	ColorPalette[index] = color;
}

static void TextVRAM_SetBackgroundColor(uint8_t c)
{
	BackgroundColor = c;
}

// �J���[�R���|�W�b�g�o�͏�����
static void TextVRAM_Initialize(void)
{
	int i;
	TextVRAM_ClearScreen();

	//�J���[�ԍ�0�`7�̃p���b�g������
	for (i = 0; i<8; i++) {
		TextVRAM_SetPaletteColor(
			(uint8_t)i, 
			(uint8_t)(255U * (i & 1U)),
			(uint8_t)(255U * ((i >> 1U) & 1U)),
			(uint8_t)(255U * (i >> 2U))
		);
	}
	for (; i<256; i++) {
		//8�ȏ�͑S�Ĕ��ɏ�����
		TextVRAM_SetPaletteColor(
			(uint8_t)i, 
			255U, 
			255U, 
			255U
		);
	}
	TextVRAM_SetBackgroundColor(0); //�o�b�N�O�����h�J���[�͍�
	TextVRAM_SetTextColor(7);
}


static text_vram_pixel_t *TextVRAM_GetCursorPtr(void) {
	return (text_vram_pixel_t *)CursorPtr;
}

static void TextVRAM_GetCursorPosition(screen_pos_t *pos) {
	int offset = CursorPtr - Pixels;
	pos->X = offset % VWIDTH_X;
	pos->Y = offset / VWIDTH_X;
}

static void TextVRAM_SetCursorPosition(const screen_pos_t *pos) {
	if (pos->X < 0 || pos->X >= VWIDTH_X || pos->Y < 0 || pos->Y >= WIDTH_Y) {
		return;
	}

	CursorPtr = &Pixels[pos->Y  * VWIDTH_X + pos->X];
}

static text_vram_pixel_t *TextVRAM_GetVramPtr(void) {
	return (text_vram_pixel_t *)Pixels;
}

static const color_t *TextVRAM_GetPalettePtr(void) {
	return (const color_t *)ColorPalette;
}

static uint8_t TextVRAM_GetBackgroundColor(void) {
	return BackgroundColor;
}

static uint8_t TextVRAM_GetTextColor(void) {
	return TextColor;
}

static void TextVRAM_SetTextColor(uint8_t c) {
	TextColor = c;
}

static void	TextVRAM_FillTextColor(uint8_t x, uint8_t y, uint16_t length, uint8_t palette)
{
	uint16_t n = y  * VWIDTH_X + x;
	while (length-- > 0) {
		Pixels[n++].color = palette;
	}
}

static void	TextVRAM_FillBackgroundColor(uint8_t x, uint8_t y, uint16_t length, uint8_t palette)
{
	uint16_t n = y  * VWIDTH_X + x;
	while (length-- > 0) {
		Pixels[n++].bgcolor = palette;
	}
}

static void TextVRAM_Scroll(void) {
	text_vram_pixel_t *pv1 = &Pixels[0];
	text_vram_pixel_t *pv2 = &Pixels[VWIDTH_X];
	for (int y = 1; y < WIDTH_Y; y++) {
		for (int x = 0; x < VWIDTH_X; x++) {
			*pv1++ = *pv2++;
		}
	}
	Memory.Fill.Uint8((uint8_t*)pv1, 0, VWIDTH_X * sizeof(text_vram_pixel_t));
}

static void TextVRAM_putch(char16_t n) {
	//�J�[�\���ʒu�Ƀe�L�X�g�R�[�hn��1�����\�����A�J�[�\����1�����i�߂�
	//��ʍŏI�����\�����Ă��X�N���[�������A���̕����\�����ɃX�N���[������

	int sz = UniWidth1(n);

	if (CursorPtr<Pixels || CursorPtr>Pixels + VWIDTH_X*WIDTH_Y) {
		// VRAM�O�ւ̕`��
		return;
	}
	if (CursorPtr + sz - 1 >= Pixels + VWIDTH_X*WIDTH_Y) {
		// ��ʖ����ł̕`��
		TextVRAM_Scroll();
		CursorPtr = Pixels + VWIDTH_X*(WIDTH_Y - 1);
	}
	if (n == '\n') {
		//���s
		CursorPtr += VWIDTH_X - ((CursorPtr - Pixels) % VWIDTH_X);
	}
	else {
		// �c��󂫃Z�������擾
		int rest = VWIDTH_X - ((CursorPtr - Pixels) % VWIDTH_X);
		if (rest < sz) {
			// ������}������Ɖ�ʒ[���͂ݏo���ꍇ�A���̈ʒu�ɋ󔒓���ĉ��s�������Ƃɂ���
			CursorPtr->ch = 0;
			// �����F�Ɣw�i�F�͂��̂܂܂ɂ��Ă����ق��������̂���
			//CursorPtr->color = TextColor;
			//CursorPtr->bgcolor = BackgroundColor;
			CursorPtr += VWIDTH_X - ((CursorPtr - Pixels) % VWIDTH_X);
			TextVRAM_putch(n);
		}
		else {
			// ������}������
			CursorPtr->ch = n;
			CursorPtr->color = TextColor;
			CursorPtr->bgcolor = BackgroundColor;
			CursorPtr++;
			sz--;
			while (sz > 0) {
				CursorPtr->ch = 0;
				CursorPtr->color = 0;
				CursorPtr++;
				sz--;
			}
		}
	}
}

static void TextVRAM_puts(const char16_t *s) {
	//�J�[�\���ʒu�ɕ�����s��\��
	while (*s) {
		TextVRAM_putch(*s++);
	}
}

static void TextVRAM_putdigit(uint32_t n) {
	//�J�[�\���ʒu�ɕ����Ȃ�����n��10�i���\��
	uint32_t n1 = n / 10U;
	uint32_t d = 1U;
	while (n1 >= d) {
		d *= 10U;
	}
	while (d != 0U) {
		TextVRAM_putch((char16_t)(L'0' + (n / d)));
		n %= d;
		d /= 10U;
	}
}

static void TextVRAM_putdigit2(uint32_t n, uint8_t e) {
	//�J�[�\���ʒu�ɕ����Ȃ�����n��e����10�i���\���i�O�̋󂫌������̓X�y�[�X�Ŗ��߂�j
	if (e == 0) {
		return;
	}
	uint32_t n1 = n / 10U;
	uint32_t d = 1U;
	e--;
	while (e > 0 && n1 >= d) {
		d *= 10U;
		e--;
	}
	if (e == 0 && n1 > d) {
		n %= d * 10U;
	}
	for (; e > 0; e--) {
		TextVRAM_putch(' ');
	}
	while (d != 0) {
		TextVRAM_putch((char16_t)(L'0' + (n / d)));
		n %= d;
		d /= 10U;
	}
}

// VRAM���(x,y)�ɃJ�[�\��������A��width �̃e�L�X�g�{�b�N�X�� �͈�[head..cur)�̕��������͂�����̃J�[�\���ʒu���Z�o
void TextVRAM_CalcCursorPosition(uint16_t *head, uint16_t *cur, screen_pos_t *p, size32_t width) {
	for (;;) {
		if (head == cur) {
			return;
		}
		// �P������i�ړ���j��ǂ�
		char16_t c1 = *head++;
		if (c1 == 0x0000) {
			// 1�����悪�����Ȃ̂őł��؂�
			return;
		}
		uint8_t w1 = UniWidth1(c1);

		// �Q�������ǂށi�܂�Ԃ�����̂��߁j
		char16_t c2 = *head;
		uint8_t w2 = UniWidth1(c2);

		// �S�p�����̉�荞�݂��l�����ĉ��s���K�v������
		if (c1 == L'\n') {
			p->X = 0;
			p->Y++;
		}
		else if (p->X + w1 + w2 > width) {
			p->X = 0;
			p->Y++;
		}
		else {
			p->X += w1;
		}
	}
}

const struct TextVRAM TextVRAM = {
	TextVRAM_ClearScreen,
	TextVRAM_SetPaletteColor,
	TextVRAM_Initialize,
	TextVRAM_GetCursorPtr,
	TextVRAM_GetCursorPosition,
	TextVRAM_SetCursorPosition,
	TextVRAM_GetVramPtr,
	TextVRAM_GetPalettePtr,
	TextVRAM_GetBackgroundColor,
	TextVRAM_SetBackgroundColor,
	TextVRAM_FillBackgroundColor,
	TextVRAM_GetTextColor,
	TextVRAM_SetTextColor,
	TextVRAM_FillTextColor,
	TextVRAM_Scroll,
	TextVRAM_putch,
	TextVRAM_puts,
	TextVRAM_putdigit,
	TextVRAM_putdigit2,
	TextVRAM_CalcCursorPosition,
};
