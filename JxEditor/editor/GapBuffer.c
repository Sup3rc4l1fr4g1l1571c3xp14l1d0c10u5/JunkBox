#include "../pico/pico.types.h"
#include "../pico/pico.memory.h"
#include "../pico/pico.assert.h"
#include "./GapBuffer.h"

#define ELEM_TYPE char16_t
#define GLOW_SIZE 32

#define ALIGNED_CAPACITY(x) (((x) + GLOW_SIZE - 1) / GLOW_SIZE * GLOW_SIZE)

/**
 * @brief �M���b�v�o�b�t�@������������
 * @param  gap_buffer_t* self          �������Ώۂ̃M���b�v�o�b�t�@
 * @retval GapBufferResult_OutOfMemory ���������s�i�\���ȃ��������m�ۂł��Ȃ������j
 * @retval GapBufferResult_Success     ����������
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
 * @brief �M���b�v�o�b�t�@���������
 * @param  gap_buffer_t* self ����Ώۂ̃M���b�v�o�b�t�@
 * @note   self��NULL���n���ꂽ�ꍇ�A�������Ȃ�
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
 * @brief �M���b�v�o�b�t�@�����Z�b�g����
 * @param  gap_buffer_t* self ���Z�b�g�Ώۂ̃M���b�v�o�b�t�@
 * @note   self��NULL���n���ꂽ�ꍇ�A�������Ȃ�
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
 * @brief �M���b�v�o�b�t�@�̃f�[�^�����擾����
 * @param  gap_buffer_t* self �f�[�^���擾�Ώۂ̃M���b�v�o�b�t�@
 * @param  size32_t *result   �f�[�^�����i�[�����ϐ��̎Q��
 * @retval GapBufferResult_ArgumentNull ������NULL���n���ꂽ
 * @retval GapBufferResult_Success     �擾����
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
 * @brief �M���b�v�o�b�t�@�̃T�C�Y���w��T�C�Y�܂Ŋg������
 * @param  gap_buffer_t* self �Ώۂ̃M���b�v�o�b�t�@
 * @param  size32_t size      �T�C�Y
 * @retval GapBufferResult_ArgumentNull ������NULL���n���ꂽ
 * @retval GapBufferResult_Success      ����
 * @retval GapBufferResult_OutOfMemory  �������s��
 */
static bool_t RequestCapacity(gap_buffer_t *self, size32_t size)
{
    if (self == NULL) {
        return GapBufferResult_ArgumentNull;
    }

    if (self->buffer_capacity >= size)
	{
		// �g���̕K�v�Ȃ�
		return GapBufferResult_Success;
	}

    // �V�����̈���m��
    const size32_t newCapacity = ALIGNED_CAPACITY(size);
    ELEM_TYPE * const newBuf = Memory.Allocate(newCapacity * sizeof(ELEM_TYPE));

    // �o�b�t�@�̃��C�A�E�g
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
    //  +-------------------------------- head_start(0�Œ�)

    const size32_t head_len = self->gap_start;
    const size32_t tail_len = self->buffer_capacity - (self->gap_start + self->gap_length);

    // �g���O�o�b�t�@�̃��C�A�E�g
    //const size32_t old_buffer_capaity = self->buffer_capacity;
    const size32_t old_gap_start = self->gap_start;
    const size32_t old_gap_len = self->gap_length;
    const size32_t old_tail_start = old_gap_start + old_gap_len;

    // �g����o�b�t�@�̃��C�A�E�g
    const size32_t new_buffer_capaity = newCapacity;
    const size32_t new_gap_start = self->gap_start;
    const size32_t new_tail_start = new_buffer_capaity - tail_len;
    const size32_t new_gap_len = new_tail_start - new_gap_start;

    // �M���b�v�̑O��V�����̈�̐擪�փR�s�[
    for (size32_t  i = 0; i < head_len; i++) {
        newBuf[i] = self->buffer[i];
    }

    // �M���b�v�̌���V�����̈�̖����փR�s�[
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
 * @brief �M���b�v�o�b�t�@�Ɉꕶ���}������
 * @param  gap_buffer_t* self �}���Ώۂ̃M���b�v�o�b�t�@
 * @param  size32_t index     �}���ʒu
 * @param  ELEM_TYPE value    �}������
 * @retval GapBufferResult_ArgumentNull ������NULL���n���ꂽ
 * @retval GapBufferResult_OutOfMemory  ���������s��
 * @retval GapBufferResult_OutOfRange   �}���ʒu���s��
 * @retval GapBufferResult_Success      �}������
 */
static GapBufferResultCode insert(gap_buffer_t *self, size32_t index, ELEM_TYPE value)
{
    if (self == NULL) {
        return GapBufferResult_ArgumentNull;
    }

	if (index > self->buffer_capacity - self->gap_length) {
		// �ʒu���s��
		return GapBufferResult_OutOfRange;
	}

	if (self->gap_length == 0) {
		// �󂫂��Ȃ��̂Ńo�b�t�@���g������
	    GapBufferResultCode ret = RequestCapacity(self, self->buffer_capacity + GLOW_SIZE);
	    if (ret != GapBufferResult_Success)
	    {
		    return ret;
	    }
	}

	if (index <= self->gap_start) {
		// �}���ʒu���M���b�v�����O�̏ꍇ
		// ��F[1,2,3,4,-,-,7,8]�̈ʒu2��A������
		// 1. �}���ʒu����M���b�v�̎n�_�܂ł��M���b�v�����Ɉړ������A�M���b�v�̎n�_���X�V����
		//    [1,2,3,4,-,-,7,8] -> [1,2,-,-,3,4,7,8]
		// 2. �}���ʒu�ɕ�����}�����A�M���b�v�̎n�_��+1�A�M���b�v�̃T�C�Y��-1����
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
		// �}���ʒu���M���b�v�̎n�_�����̏ꍇ
		// �M���b�v�I�[����}���ʒu�܂ł���O�Ɉړ�������
		// ��F[1,2,-,-,-,6,7,8]�̈ʒu4��A������
		// 1. �M���b�v�I�[����}���ʒu�܂ł��M���b�v�J�n�ʒu�ړ������ăM���b�v�̎n�_���X�V
		//    [1,2,-,-,-,6,7,8] -> [1,2,6,7,-,-,-,8]
		// 2. �}���ʒu�ɕ�����}�����A�M���b�v�̎n�_��+1�A�M���b�v�̃T�C�Y��-1����
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
 * @brief �M���b�v�o�b�t�@�ɕ��������}������
 * @param  gap_buffer_t* self �}���Ώۂ̃M���b�v�o�b�t�@
 * @param  size32_t index     �}���ʒu
 * @param  const ELEM_TYPE *value    �}��������
 * @param  size32_t len    �}��������
 * @retval GapBufferResult_ArgumentNull ������NULL���n���ꂽ
 * @retval GapBufferResult_OutOfMemory  ���������s��
 * @retval GapBufferResult_OutOfRange   �}���ʒu���s��
 * @retval GapBufferResult_Success      �}������
 */
static GapBufferResultCode insert_many(gap_buffer_t *self, size32_t index, const ELEM_TYPE *value, size32_t len)
{
    if (self == NULL || value == NULL) {
        return GapBufferResult_ArgumentNull;
    }

    if (index > self->buffer_capacity - self->gap_length) {
		// �ʒu���s��
		return GapBufferResult_OutOfRange;
	}

	if (self->gap_length < len) {
		// �󂫂��Ȃ��̂Ńo�b�t�@���g������
		// �󂫂��Ȃ��̂Ńo�b�t�@���g������
	    GapBufferResultCode ret = RequestCapacity(self, self->buffer_capacity + len);
	    if (ret != GapBufferResult_Success)
	    {
		    return ret;
	    }
	}
	for (size32_t i = 0; i<len; i++) {
		// �G���[�͖߂�Ȃ��͂�
		Diagnotics.Assert(insert(self, index + i, value[i]) == GapBufferResult_Success);
	}

	return GapBufferResult_Success;
}

/**
 * @brief �M���b�v�o�b�t�@����P�����擾����
 * @param  gap_buffer_t* self �擾�Ώۂ̃M���b�v�o�b�t�@
 * @param  size32_t index     �擾�ʒu
 * @param  ELEM_TYPE *value    �擾�����̊i�[��
 * @retval GapBufferResult_ArgumentNull ������NULL���n���ꂽ
 * @retval GapBufferResult_OutOfMemory  ���������s��
 * @retval GapBufferResult_OutOfRange   �擾�ʒu���s��
 * @retval GapBufferResult_Success      �擾����
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
 * @brief �M���b�v�o�b�t�@���P��������������
 * @param  gap_buffer_t* self ���������Ώۂ̃M���b�v�o�b�t�@
 * @param  size32_t index     ���������ʒu
 * @param  ELEM_TYPE value    ������������
 * @retval GapBufferResult_ArgumentNull ������NULL���n���ꂽ
 * @retval GapBufferResult_OutOfRange   ���������ʒu���s��
 * @retval GapBufferResult_Success      ������������
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
 * @brief �M���b�v�o�b�t�@����ꕶ���폜����
 * @param  gap_buffer_t* self �폜�Ώۂ̃M���b�v�o�b�t�@
 * @param  size32_t index     �폜�ʒu
 * @retval GapBufferResult_ArgumentNull ������NULL���n���ꂽ
 * @retval GapBufferResult_OutOfRange   �폜�ʒu���s��
 * @retval GapBufferResult_Success      �폜����
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
		// �O�ɋl�߂�
		for (size32_t i = index; i < self->gap_start; i++)
		{
			self->buffer[i] = self->buffer[i + 1];
		}
		self->gap_start--;
		self->gap_length++;
	}
	else if (index >= self->gap_start)
	{
		// ���ɋl�߂�
		for (size32_t i = index; i > self->gap_start; i--)
		{
			self->buffer[i + self->gap_length] = self->buffer[i + self->gap_length - 1];
		}
		self->gap_length++;
	}
	return GapBufferResult_Success;
}

/**
 * @brief ����̃M���b�v�o�b�t�@�ɑ����̃M���b�v�o�b�t�@��A������
 * @param  gap_buffer_t* self �A����̃M���b�v�o�b�t�@
 * @param  gap_buffer_t* that �A�����̃M���b�v�o�b�t�@
 * @retval GapBufferResult_ArgumentNull ������NULL���n���ꂽ
 * @retval GapBufferResult_Success      �擾����
 * @retval GapBufferResult_OutOfMemory  �������s��
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
		// �󂫂��Ȃ��̂Ńo�b�t�@���g������
	    ret = RequestCapacity(self, self->buffer_capacity + len);
	    if (ret != GapBufferResult_Success)
	    {
		    return ret;
	    }
	}
	size32_t index;
	Diagnotics.Assert(length(self, &index) == GapBufferResult_Success);
	for (size32_t i = 0; i<len; i++) {
		// �G���[�͖߂�Ȃ��͂�
		ELEM_TYPE ch;
		Diagnotics.Assert(take(that, i, &ch) == GapBufferResult_Success);
		Diagnotics.Assert(insert(self, index + i, ch) == GapBufferResult_Success);
	}

	return GapBufferResult_Success;
}

/**
 * @brief ����̃M���b�v�o�b�t�@�ɑ����̃M���b�v�o�b�t�@�̈ꕔ���R�s�[����i�㏑���ł͖����}���j
 * @param  gap_buffer_t* dest  �ǉ���̃M���b�v�o�b�t�@
 * @param  size32_t dest_index �ǉ���̈ʒu
 * @param  gap_buffer_t* src   �ǉ����̃M���b�v�o�b�t�@
 * @param  size32_t src_index  �ǉ����̈ʒu
 * @param  size32_t len        �R�s�[����v�f��
 * @retval GapBufferResult_ArgumentNull ������NULL���n���ꂽ
 * @retval GapBufferResult_Success      �擾����
 * @retval GapBufferResult_OutOfMemory  �������s��
 * @retval GapBufferResult_OutOfRange   ���������ʒu���s��
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
 * @brief �M���b�v�o�b�t�@��̎w��͈͂��폜����
 * @param  gap_buffer_t* self  �폜��̃M���b�v�o�b�t�@
 * @param  size32_t index      �폜�J�n�ʒu
 * @param  size32_t len        �폜�v�f��
 * @retval GapBufferResult_ArgumentNull ������NULL���n���ꂽ
 * @retval GapBufferResult_Success      �폜����
 * @retval GapBufferResult_OutOfRange   �폜�ʒu/�͈͂��s��
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
 * @brief API�\����
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
