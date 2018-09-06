#pragma once

/**
 * 簡易的なブロック管理型のテキストバッファ
 */

#include "../pico/pico.types.h"

/**
 * @brief テキストバッファ
 */
typedef struct __tag_text_buffer_t text_buffer_t;

/**
 * カーソル
 */
typedef uint32_t text_buffer_cursor_t;

#define INVALID_TEXTBUFFER_CURSOR ((text_buffer_cursor_t)~(text_buffer_cursor_t)0)

extern const struct __tagTextBufferCursorApi {
	text_buffer_cursor_t (*Allocate)(void);
	void (*Copy)(text_buffer_cursor_t dest, text_buffer_cursor_t src);
	text_buffer_cursor_t(*Duplicate)(text_buffer_cursor_t);
	void(*Dispose)(text_buffer_cursor_t cursor);
	void(*CheckValid)(text_buffer_cursor_t cursor);
} TextBufferCursor;


extern void	TextBuffer_Initialize(void);
extern size32_t TextBuffer_GetTotalLength(void);
extern size32_t TextBuffer_GetCapacity(void);

extern void TextBuffer_DeleteArea(text_buffer_cursor_t start, size32_t n);
extern char16_t TextBuffer_TakeCharacatorOnCursor(text_buffer_cursor_t cursor);
extern int TextBuffer_InsertCharacatorOnCursor(text_buffer_cursor_t cursor, char16_t ch);
extern int TextBuffer_OverwriteCharacatorOnCursor(text_buffer_cursor_t cursor, char16_t ch);
extern void TextBuffer_DeleteCharacatorOnCursor(text_buffer_cursor_t cursor);

extern bool_t TextBuffer_CursorBackward(text_buffer_cursor_t cursor);
extern bool_t TextBuffer_CursorForward(text_buffer_cursor_t cursor);

extern int TextBuffer_CompareCursor(text_buffer_cursor_t c1, text_buffer_cursor_t c2);
extern bool_t TextBuffer_EqualCursor(text_buffer_cursor_t c1, text_buffer_cursor_t c2);

extern bool_t TextBuffer_StartOfString(text_buffer_cursor_t c);
extern bool_t TextBuffer_EndOfString(text_buffer_cursor_t c);

extern void TextBuffer_MoveToBeginningOfLine(text_buffer_cursor_t cur);
extern void TextBuffer_MoveToBeginningOfDocument(text_buffer_cursor_t cur);
extern size32_t TextBuffer_strlen(text_buffer_cursor_t start, text_buffer_cursor_t end);
