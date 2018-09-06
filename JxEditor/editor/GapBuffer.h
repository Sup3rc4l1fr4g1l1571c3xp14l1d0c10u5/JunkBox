#pragma once

#include "../pico/pico.utf16.h"
#include "../pico/pico.types.h"

#define ELEM_TYPE char16_t
#define GLOW_SIZE 32

typedef struct __gap_buffer_t {
	size32_t	gap_start;
	size32_t	gap_length;
	size32_t	buffer_capacity;
	ELEM_TYPE	*buffer;
} gap_buffer_t;

typedef enum 
{
	GapBufferResult_Success = 0,
	GapBufferResult_OutOfMemory = 1,	// メモリ不足
	GapBufferResult_OutOfRange = 2,	// インデクス位置が不正
	GapBufferResult_ArgumentNull = 3,	// NULL を有効な引数として受け付けない関数に NULL を渡した
} GapBufferResultCode;

extern const struct __tagGapBufferAPI {
	GapBufferResultCode (*initialize)(gap_buffer_t *self);
	void(*finalize)(gap_buffer_t *self);
	void(*clear)(gap_buffer_t *self);
	GapBufferResultCode (*length)(const gap_buffer_t *self, size32_t* result);
	GapBufferResultCode (*insert)(gap_buffer_t *self, size32_t index, ELEM_TYPE value);
	GapBufferResultCode (*insert_many)(gap_buffer_t *self, size32_t index, ELEM_TYPE *value, size32_t len);
	GapBufferResultCode (*take)(const gap_buffer_t *self, size32_t index, ELEM_TYPE *value);
	GapBufferResultCode (*set)(gap_buffer_t *self, size32_t index, ELEM_TYPE value);
	GapBufferResultCode (*remove)(gap_buffer_t *self, size32_t index);
	GapBufferResultCode (*concat)(gap_buffer_t *self, const gap_buffer_t *that);
	GapBufferResultCode (*CopyTo)(gap_buffer_t *dest, size32_t dest_index, const gap_buffer_t *src, size32_t src_index, size32_t len);
	GapBufferResultCode (*RemoveRange)(gap_buffer_t *self, size32_t index, size32_t len);

} GapBuffer;
