// Bitap�A���S���Y���ōs�������܂�����

#include "asearch.h"
#include "../pico/pico.memory.h"


#define INITPAT (0x01 << (sizeof(char16_t)*8-2))

/**
 * @brief �����p�^���ƞB���x�̎w��
 * @param pat	�����p�^��
 * @param ambig	�����̞B���x
 * ���������̍ŏ��ɌĂ�ŞB�������̂��߂̏�ԑJ�ڋ@�B�𐶐�����B 
 * ���ۂ̌�����asearch_match()���g�p����B
 * �����p�^���ɂ͕����N���X���g�p�\�B(e.g. "k[aiueo]")
 * �����p�^�����̋󔒕���(0x20)�̓��C���h�J�[�h�ƂȂ�B
*  (0�����ȏ�̂����镶���̕��тɃ}�b�`����B���K�\����".*"�Ɠ��l�B)
 * �B���xambig�̒l��0�̂Ƃ��͊��S�}�b�`���O���s�Ȃ��A�l��1�̂Ƃ��͞B���x1�̞B���}�b�`���O���s�Ȃ���B
 */
void asearch_makepat(asearch_context_t *ctx, const char16_t *pattern, sint32_t ambig)
{
	uint32_t mask = INITPAT;

	ctx->mismatch = ambig;
	ctx->epsilon = 0;

	Memory.Fill.Uint32(ctx->shiftpat, 0UL, MAXCHAR);

	for(;*pattern; pattern++){
		if(*pattern == PAT_WILDCARD){ // ���C���h�J�[�h����
			ctx->epsilon |= mask;
		}
		else if (*pattern == PAT_GROUP_START){	// �O���[�v�w��i���K�\�����[ABC...]�݂����Ȃ��́j
			for(pattern++;*pattern != PAT_GROUP_END; pattern++){
				ctx->shiftpat[*pattern] |= mask;
			}
			mask >>= 1;
		}
		else {
			ctx->shiftpat[*pattern] |= mask;
			mask >>= 1;
		}
	}
	ctx->acceptpat = mask;
}

uint32_t asearch_match(asearch_context_t *ctx, const char16_t *text)
{
	register uint32_t i0 = INITPAT;
#if MAXMISMATCH > 0
	register uint32_t i1=0;
#endif
#if MAXMISMATCH > 1
	register uint32_t i2=0;
#endif
#if MAXMISMATCH > 2
	register uint32_t i3=0;
#endif
	register uint32_t mask;
	register uint32_t e = ctx->epsilon;

	for(;*text;text++){
		mask = ctx->shiftpat[*text];
#if MAXMISMATCH > 2
		i3 = (i3 & e) | ((i3 & mask) >> 1) | (i2 >> 1) | i2;
#endif
#if MAXMISMATCH > 1
		i2 = (i2 & e) | ((i2 & mask) >> 1) | (i1 >> 1) | i1;
#endif
#if MAXMISMATCH > 0
		i1 = (i1 & e) | ((i1 & mask) >> 1) | (i0 >> 1) | i0;
#endif
		i0 = (i0 & e) | ((i0 & mask) >> 1);
#if MAXMISMATCH > 0
		i1 |= (i0 >> 1);
#endif
#if MAXMISMATCH > 1
		i2 |= (i1 >> 1);
#endif
#if MAXMISMATCH > 2
		i3 |= (i2 >> 1);
#endif
	}
	switch(ctx->mismatch){
		case 0: return (i0 & ctx->acceptpat);
		case 1: return (i1 & ctx->acceptpat);
		case 2: return (i2 & ctx->acceptpat);
		case 3: return (i3 & ctx->acceptpat);
		default: return 0U;
	}
}
