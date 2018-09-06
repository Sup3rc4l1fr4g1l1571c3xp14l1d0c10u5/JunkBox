/*
 * テキストVRAM
 */

#include "TextVRAM.h"
#include "../pico/pico.memory.h"

/**
 * @brief カラーパレット
 */
static color_t ColorPalette[256];

/**
 * @brief 画素
 */
static text_vram_pixel_t Pixels[VWIDTH_X*WIDTH_Y];

/**
* @brief 現在設定されている背景色
 */
static uint8_t BackgroundColor;

/**
 * @brief Pixels上のカーソル位置
 */
static text_vram_pixel_t *CursorPtr;

/**
 * @brief 現在設定されている文字色
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

// 画面クリア
static void TextVRAM_ClearScreen(void)
{
	Memory.Fill.Uint8((uint8_t*)Pixels, 0, sizeof(Pixels));
	CursorPtr = Pixels;
}

static void TextVRAM_SetPaletteColor(uint8_t index, uint8_t b, uint8_t r, uint8_t g)
{
	// カラーパレット設定
	// n:パレット番号、r,g,b:0～255
	color_t color;
	color.bgra = (b << 16U) | (g << 8U) | (r << 0U);
	ColorPalette[index] = color;
}

static void TextVRAM_SetBackgroundColor(uint8_t c)
{
	BackgroundColor = c;
}

// カラーコンポジット出力初期化
static void TextVRAM_Initialize(void)
{
	int i;
	TextVRAM_ClearScreen();

	//カラー番号0～7のパレット初期化
	for (i = 0; i<8; i++) {
		TextVRAM_SetPaletteColor(
			(uint8_t)i, 
			(uint8_t)(255U * (i & 1U)),
			(uint8_t)(255U * ((i >> 1U) & 1U)),
			(uint8_t)(255U * (i >> 2U))
		);
	}
	for (; i<256; i++) {
		//8以上は全て白に初期化
		TextVRAM_SetPaletteColor(
			(uint8_t)i, 
			255U, 
			255U, 
			255U
		);
	}
	TextVRAM_SetBackgroundColor(0); //バックグランドカラーは黒
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
	//カーソル位置にテキストコードnを1文字表示し、カーソルを1文字進める
	//画面最終文字表示してもスクロールせず、次の文字表示時にスクロールする

	int sz = UniWidth1(n);

	if (CursorPtr<Pixels || CursorPtr>Pixels + VWIDTH_X*WIDTH_Y) {
		// VRAM外への描画
		return;
	}
	if (CursorPtr + sz - 1 >= Pixels + VWIDTH_X*WIDTH_Y) {
		// 画面末尾での描画
		TextVRAM_Scroll();
		CursorPtr = Pixels + VWIDTH_X*(WIDTH_Y - 1);
	}
	if (n == '\n') {
		//改行
		CursorPtr += VWIDTH_X - ((CursorPtr - Pixels) % VWIDTH_X);
	}
	else {
		// 残り空きセル数を取得
		int rest = VWIDTH_X - ((CursorPtr - Pixels) % VWIDTH_X);
		if (rest < sz) {
			// 文字を挿入すると画面端をはみ出す場合、その位置に空白入れて改行したことにする
			CursorPtr->ch = 0;
			// 文字色と背景色はそのままにしておくほうがいいのかな
			//CursorPtr->color = TextColor;
			//CursorPtr->bgcolor = BackgroundColor;
			CursorPtr += VWIDTH_X - ((CursorPtr - Pixels) % VWIDTH_X);
			TextVRAM_putch(n);
		}
		else {
			// 文字を挿入する
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
	//カーソル位置に文字列sを表示
	while (*s) {
		TextVRAM_putch(*s++);
	}
}

static void TextVRAM_putdigit(uint32_t n) {
	//カーソル位置に符号なし整数nを10進数表示
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
	//カーソル位置に符号なし整数nをe桁の10進数表示（前の空き桁部分はスペースで埋める）
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

// VRAM上の(x,y)にカーソルがあり、幅width のテキストボックスに 範囲[head..cur)の文字列を入力した後のカーソル位置を算出
void TextVRAM_CalcCursorPosition(uint16_t *head, uint16_t *cur, screen_pos_t *p, size32_t width) {
	for (;;) {
		if (head == cur) {
			return;
		}
		// １文字先（移動先）を読む
		char16_t c1 = *head++;
		if (c1 == 0x0000) {
			// 1文字先が文末なので打ち切り
			return;
		}
		uint8_t w1 = UniWidth1(c1);

		// ２文字先を読む（折り返し判定のため）
		char16_t c2 = *head;
		uint8_t w2 = UniWidth1(c2);

		// 全角文字の回り込みを考慮して改行が必要か判定
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
