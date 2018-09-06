#include "../pico/pico.types.h"
#include "../pico/pico.memory.h"
#include "../pico/pico.assert.h"
#include "./GapBuffer.h"

#define ELEM_TYPE char16_t
#define GLOW_SIZE 32

#define ALIGNED_CAPACITY(x) (((x) + GLOW_SIZE - 1) / GLOW_SIZE * GLOW_SIZE)

/**
 * @brief ギャップバッファを初期化する
 * @param  gap_buffer_t* self          初期化対象のギャップバッファ
 * @retval GapBufferResult_OutOfMemory 初期化失敗（十分なメモリが確保できなかった）
 * @retval GapBufferResult_Success     初期化成功
 */
static GapBufferResultCode initialize(gap_buffer_t *self)
{
    if (self == NULL) {
        return GapBufferResult_ArgumentNull;
    }
	self->gap_start = 0;
	self->gap_length = GLOW_SIZE;
	self->buffer_capacity = GLOW_SIZE;
	self->buffer = (ELEM_TYPE *)Memory.Allocate(GLOW_SIZE * sizeof(ELEM_TYPE));
	if (self->buffer == NULL)
	{
		return GapBufferResult_OutOfMemory;
	}
	return GapBufferResult_Success;
}

/**
 * @brief ギャップバッファを解放する
 * @param  gap_buffer_t* self 解放対象のギャップバッファ
 * @note   selfにNULLが渡された場合、何もしない
 */
static void finalize(gap_buffer_t *self)
{
    if (self == NULL) {
        return;
    }
	Memory.Free(self->buffer);
	self->buffer = NULL;
	Memory.Fill.Uint8((uint8_t*)self, 0, sizeof(gap_buffer_t));
}

/**
 * @brief ギャップバッファをリセットする
 * @param  gap_buffer_t* self リセット対象のギャップバッファ
 * @note   selfにNULLが渡された場合、何もしない
 */
static void clear(gap_buffer_t *self)
{
    if (self == NULL) {
        return;
    }
	self->gap_start = 0;
	self->gap_length = GLOW_SIZE;
	self->buffer_capacity = GLOW_SIZE;
	Memory.Free(self->buffer);
	self->buffer = (ELEM_TYPE *)Memory.Allocate(GLOW_SIZE * sizeof(ELEM_TYPE));
}

/**
 * @brief ギャップバッファのデータ長を取得する
 * @param  gap_buffer_t* self データ長取得対象のギャップバッファ
 * @param  size32_t *result   データ長が格納される変数の参照
 * @retval GapBufferResult_ArgumentNull 引数にNULLが渡された
 * @retval GapBufferResult_Success     取得成功
 */
static GapBufferResultCode length(const gap_buffer_t *self, size32_t *result)
{
    if (self == NULL || result == NULL) {
        return GapBufferResult_ArgumentNull;
    }
	*result = self->buffer_capacity - self->gap_length;
    return GapBufferResult_Success;
}

/**
 * @brief ギャップバッファのサイズを指定サイズまで拡張する
 * @param  gap_buffer_t* self 対象のギャップバッファ
 * @param  size32_t size      サイズ
 * @retval GapBufferResult_ArgumentNull 引数にNULLが渡された
 * @retval GapBufferResult_Success      成功
 * @retval GapBufferResult_OutOfMemory  メモリ不足
 */
static bool_t RequestCapacity(gap_buffer_t *self, size32_t size)
{
    if (self == NULL) {
        return GapBufferResult_ArgumentNull;
    }

    if (self->buffer_capacity >= size)
	{
		// 拡張の必要なし
		return GapBufferResult_Success;
	}

    // 新しい領域を確保
    const size32_t newCapacity = ALIGNED_CAPACITY(size);
    ELEM_TYPE * const newBuf = Memory.Allocate(newCapacity * sizeof(ELEM_TYPE));

    // バッファのレイアウト
    //                +------------------ buffer_capacity   
    //  |<------------+----------->|
    // [a,b,c,d,-,-,-,-,-,-,x,y,z],E
    //  A       A           A      A
    //  |<--+-->|<----+---->|      |
    //  |   |   |     |     |<--+->|   
    //  |   |   |     |     |   |    
    //  |   |   |     |     |   +-------- tail_len
    //  |   |   |     |     +------------ tail_start
    //  |   |   |     +------------------ gap_len
    //  |   |   +------------------------ gap_start(=head_len)
    //  |   +---------------------------- head_len
    //  +-------------------------------- head_start(0固定)

    const size32_t head_len = self->gap_start;
    const size32_t tail_len = self->buffer_capacity - (self->gap_start + self->gap_length);

    // 拡張前バッファのレイアウト
    //const size32_t old_buffer_capaity = self->buffer_capacity;
    const size32_t old_gap_start = self->gap_start;
    const size32_t old_gap_len = self->gap_length;
    const size32_t old_tail_start = old_gap_start + old_gap_len;

    // 拡張後バッファのレイアウト
    const size32_t new_buffer_capaity = newCapacity;
    const size32_t new_gap_start = self->gap_start;
    const size32_t new_tail_start = new_buffer_capaity - tail_len;
    const size32_t new_gap_len = new_tail_start - new_gap_start;

    // ギャップの前を新しい領域の先頭へコピー
    for (size32_t  i = 0; i < head_len; i++) {
        newBuf[i] = self->buffer[i];
    }

    // ギャップの後ろを新しい領域の末尾へコピー
    for (size32_t  i = 0; i < tail_len; i++) {
        newBuf[new_tail_start + i] = self->buffer[old_tail_start + i];
    }
    self->gap_start = new_gap_start;
    self->gap_length = new_gap_len;
    self->buffer_capacity = new_buffer_capaity;
    self->buffer = newBuf;

	return GapBufferResult_Success;
}

/**
 * @brief ギャップバッファに一文字挿入する
 * @param  gap_buffer_t* self 挿入対象のギャップバッファ
 * @param  size32_t index     挿入位置
 * @param  ELEM_TYPE value    挿入文字
 * @retval GapBufferResult_ArgumentNull 引数にNULLが渡された
 * @retval GapBufferResult_OutOfMemory  メモリが不足
 * @retval GapBufferResult_OutOfRange   挿入位置が不正
 * @retval GapBufferResult_Success      挿入成功
 */
static GapBufferResultCode insert(gap_buffer_t *self, size32_t index, ELEM_TYPE value)
{
    if (self == NULL) {
        return GapBufferResult_ArgumentNull;
    }

	if (index > self->buffer_capacity - self->gap_length) {
		// 位置が不正
		return GapBufferResult_OutOfRange;
	}

	if (self->gap_length == 0) {
		// 空きがないのでバッファを拡張する
	    GapBufferResultCode ret = RequestCapacity(self, self->buffer_capacity + GLOW_SIZE);
	    if (ret != GapBufferResult_Success)
	    {
		    return ret;
	    }
	}

	if (index <= self->gap_start) {
		// 挿入位置がギャップよりも前の場合
		// 例：[1,2,3,4,-,-,7,8]の位置2にAを入れる
		// 1. 挿入位置からギャップの始点までをギャップ末尾に移動させ、ギャップの始点を更新する
		//    [1,2,3,4,-,-,7,8] -> [1,2,-,-,3,4,7,8]
		// 2. 挿入位置に文字を挿入し、ギャップの始点を+1、ギャップのサイズを-1する
		//    [1,2,-,-,3,4,7,8] -> [1,2,A,-,3,4,7,8]

		size32_t src = self->gap_start - 1;
		size32_t dst = self->gap_start + self->gap_length - 1;
		size32_t sz = self->gap_start - index;
		for (size32_t i = 0; i < sz; i++)
		{
			self->buffer[dst] = self->buffer[src];
			dst--;
			src--;
		}
		self->gap_start -= sz;
	}
	else if (index > self->gap_start)
	{
		// 挿入位置がギャップの始点より後ろの場合
		// ギャップ終端から挿入位置までを手前に移動させる
		// 例：[1,2,-,-,-,6,7,8]の位置4にAを入れる
		// 1. ギャップ終端から挿入位置までをギャップ開始位置移動させてギャップの始点を更新
		//    [1,2,-,-,-,6,7,8] -> [1,2,6,7,-,-,-,8]
		// 2. 挿入位置に文字を挿入し、ギャップの始点を+1、ギャップのサイズを-1する
		//    [1,2,6,7,-,-,-,8] -> [1,2,6,7,A,-,-,8]

		size32_t src = self->gap_start + self->gap_length;
		size32_t dst = self->gap_start;
		size32_t sz = index - self->gap_start;
		for (size32_t i = 0; i < sz; i++)
		{
			self->buffer[dst] = self->buffer[src];
			dst++;
			src++;
		}
		self->gap_start += sz;
	}
	self->gap_start++;
	self->gap_length--;
	self->buffer[index] = value;
	return GapBufferResult_Success;
}

/**
 * @brief ギャップバッファに複数文字挿入する
 * @param  gap_buffer_t* self 挿入対象のギャップバッファ
 * @param  size32_t index     挿入位置
 * @param  const ELEM_TYPE *value    挿入文字列
 * @param  size32_t len    挿入文字列長
 * @retval GapBufferResult_ArgumentNull 引数にNULLが渡された
 * @retval GapBufferResult_OutOfMemory  メモリが不足
 * @retval GapBufferResult_OutOfRange   挿入位置が不正
 * @retval GapBufferResult_Success      挿入成功
 */
static GapBufferResultCode insert_many(gap_buffer_t *self, size32_t index, const ELEM_TYPE *value, size32_t len)
{
    if (self == NULL || value == NULL) {
        return GapBufferResult_ArgumentNull;
    }

    if (index > self->buffer_capacity - self->gap_length) {
		// 位置が不正
		return GapBufferResult_OutOfRange;
	}

	if (self->gap_length < len) {
		// 空きがないのでバッファを拡張する
		// 空きがないのでバッファを拡張する
	    GapBufferResultCode ret = RequestCapacity(self, self->buffer_capacity + len);
	    if (ret != GapBufferResult_Success)
	    {
		    return ret;
	    }
	}
	for (size32_t i = 0; i<len; i++) {
		// エラーは戻らないはず
		Diagnotics.Assert(insert(self, index + i, value[i]) == GapBufferResult_Success);
	}

	return GapBufferResult_Success;
}

/**
 * @brief ギャップバッファから１文字取得する
 * @param  gap_buffer_t* self 取得対象のギャップバッファ
 * @param  size32_t index     取得位置
 * @param  ELEM_TYPE *value    取得文字の格納先
 * @retval GapBufferResult_ArgumentNull 引数にNULLが渡された
 * @retval GapBufferResult_OutOfMemory  メモリが不足
 * @retval GapBufferResult_OutOfRange   取得位置が不正
 * @retval GapBufferResult_Success      取得成功
 */
static GapBufferResultCode take(const gap_buffer_t *self, size32_t index, ELEM_TYPE *value)
{
    if (self == NULL || value == NULL) {
        return GapBufferResult_ArgumentNull;
    }

    if (index < 0 || index >= self->buffer_capacity - self->gap_length)
	{
		return GapBufferResult_OutOfRange;
	}
	else
	{
		if (index < self->gap_start)
		{
			*value = self->buffer[index];
		}
		else
		{
			*value = self->buffer[index + self->gap_length];
		}
		return GapBufferResult_Success;
	}
}

/**
 * @brief ギャップバッファを１文字書き換える
 * @param  gap_buffer_t* self 書き換え対象のギャップバッファ
 * @param  size32_t index     書き換え位置
 * @param  ELEM_TYPE value    書き換え文字
 * @retval GapBufferResult_ArgumentNull 引数にNULLが渡された
 * @retval GapBufferResult_OutOfRange   書き換え位置が不正
 * @retval GapBufferResult_Success      書き換え成功
 */
static GapBufferResultCode  set(gap_buffer_t *self, size32_t index, ELEM_TYPE value)
{
    if (self == NULL) {
        return GapBufferResult_ArgumentNull;
    }
	if (index < 0 || index >= self->buffer_capacity - self->gap_length)
	{
		return GapBufferResult_OutOfRange;
	} else {
		if (index < self->gap_start)
		{
			self->buffer[index] = value ;
		}
		else
		{
			self->buffer[index + self->gap_length] = value ;
		}
		return GapBufferResult_Success;
	}
}

/**
 * @brief ギャップバッファから一文字削除する
 * @param  gap_buffer_t* self 削除対象のギャップバッファ
 * @param  size32_t index     削除位置
 * @retval GapBufferResult_ArgumentNull 引数にNULLが渡された
 * @retval GapBufferResult_OutOfRange   削除位置が不正
 * @retval GapBufferResult_Success      削除成功
 */
static GapBufferResultCode  remove(gap_buffer_t *self, size32_t index)
{
    if (self == NULL) {
        return GapBufferResult_ArgumentNull;
    }
	if (index < 0 || index >= self->buffer_capacity - self->gap_length)
	{
		return GapBufferResult_OutOfRange;
	}
	else if (index < self->gap_start)
	{
		// 前に詰める
		for (size32_t i = index; i < self->gap_start; i++)
		{
			self->buffer[i] = self->buffer[i + 1];
		}
		self->gap_start--;
		self->gap_length++;
	}
	else if (index >= self->gap_start)
	{
		// 後ろに詰める
		for (size32_t i = index; i > self->gap_start; i--)
		{
			self->buffer[i + self->gap_length] = self->buffer[i + self->gap_length - 1];
		}
		self->gap_length++;
	}
	return GapBufferResult_Success;
}

/**
 * @brief 一方のギャップバッファに他方のギャップバッファを連結する
 * @param  gap_buffer_t* self 連結先のギャップバッファ
 * @param  gap_buffer_t* that 連結元のギャップバッファ
 * @retval GapBufferResult_ArgumentNull 引数にNULLが渡された
 * @retval GapBufferResult_Success      取得成功
 * @retval GapBufferResult_OutOfMemory  メモリ不足
 */
static GapBufferResultCode concat(gap_buffer_t *self, const gap_buffer_t *that)
{
    if (self == NULL || that == NULL) {
        return GapBufferResult_ArgumentNull;
    }

    size32_t len;
	GapBufferResultCode ret = length(that, &len);
    if (ret != GapBufferResult_Success) {
        return ret;
    }

	if (self->gap_length < len) {
		// 空きがないのでバッファを拡張する
	    ret = RequestCapacity(self, self->buffer_capacity + len);
	    if (ret != GapBufferResult_Success)
	    {
		    return ret;
	    }
	}
	size32_t index;
	Diagnotics.Assert(length(self, &index) == GapBufferResult_Success);
	for (size32_t i = 0; i<len; i++) {
		// エラーは戻らないはず
		ELEM_TYPE ch;
		Diagnotics.Assert(take(that, i, &ch) == GapBufferResult_Success);
		Diagnotics.Assert(insert(self, index + i, ch) == GapBufferResult_Success);
	}

	return GapBufferResult_Success;
}

/**
 * @brief 一方のギャップバッファに他方のギャップバッファの一部をコピーする（上書きでは無く挿入）
 * @param  gap_buffer_t* dest  追加先のギャップバッファ
 * @param  size32_t dest_index 追加先の位置
 * @param  gap_buffer_t* src   追加元のギャップバッファ
 * @param  size32_t src_index  追加元の位置
 * @param  size32_t len        コピーする要素数
 * @retval GapBufferResult_ArgumentNull 引数にNULLが渡された
 * @retval GapBufferResult_Success      取得成功
 * @retval GapBufferResult_OutOfMemory  メモリ不足
 * @retval GapBufferResult_OutOfRange   書き換え位置が不正
 */
static GapBufferResultCode CopyTo(gap_buffer_t *dest, size32_t dest_index, const gap_buffer_t *src, size32_t src_index, size32_t len)
{
    if (src == NULL || dest == NULL) {
        return GapBufferResult_ArgumentNull;
    }
	size32_t slen, dlen;
    Diagnotics.Assert(length(src, &slen) == GapBufferResult_Success);
    Diagnotics.Assert(length(dest, &dlen) == GapBufferResult_Success);
	if (slen <= src_index || slen < src_index + len || dlen < dest_index)
	{
		return GapBufferResult_OutOfRange;
	}
	GapBufferResultCode ret = RequestCapacity(dest, dlen + len);
	if (ret != GapBufferResult_Success)
	{
		return ret;
	}
	for (size32_t i = 0; i < len; i++)
	{
		ELEM_TYPE c;
		Diagnotics.Assert(take(src, src_index + i, &c) == GapBufferResult_Success);
		Diagnotics.Assert(insert(dest, dest_index + i, c) == GapBufferResult_Success);
	}
	return GapBufferResult_Success;
}

/**
 * @brief ギャップバッファ上の指定範囲を削除する
 * @param  gap_buffer_t* self  削除先のギャップバッファ
 * @param  size32_t index      削除開始位置
 * @param  size32_t len        削除要素数
 * @retval GapBufferResult_ArgumentNull 引数にNULLが渡された
 * @retval GapBufferResult_Success      削除成功
 * @retval GapBufferResult_OutOfRange   削除位置/範囲が不正
 */
static GapBufferResultCode RemoveRange(gap_buffer_t *self, size32_t index, size32_t len)
{
    if (self == NULL) {
        return GapBufferResult_ArgumentNull;
    }
	size32_t slen;
    Diagnotics.Assert(length(self, &slen) == GapBufferResult_Success);
	if (slen <= index || slen < index + len)
	{
		return GapBufferResult_OutOfRange;
	}
	for (size32_t i = 0; i < len; i++)
	{
		Diagnotics.Assert(GapBuffer.remove(self, index) == GapBufferResult_Success);
	}
	return GapBufferResult_Success;
}

/**
 * @brief API構造体
 */
const struct __tagGapBufferAPI GapBuffer = {
	initialize,
	finalize,
	clear,
	length,
	insert,
	insert_many,
	take,
	set,
	remove,
	concat,
	CopyTo,
	RemoveRange
} ;
