// Bitapアルゴリズムで行うあいまい検索

#include "asearch.h"
#include "../pico/pico.memory.h"


#define INITPAT (0x01 << (sizeof(char16_t)*8-2))

/**
 * @brief 検索パタンと曖昧度の指定
 * @param pat	検索パタン
 * @param ambig	検索の曖昧度
 * 検索処理の最初に呼んで曖昧検索のための状態遷移機械を生成する。 
 * 実際の検索はasearch_match()を使用する。
 * 検索パタンには文字クラスを使用可能。(e.g. "k[aiueo]")
 * 検索パタン中の空白文字(0x20)はワイルドカードとなる。
*  (0文字以上のあらゆる文字の並びにマッチする。正規表現の".*"と同様。)
 * 曖昧度ambigの値が0のときは完全マッチングが行なわれ、値が1のときは曖昧度1の曖昧マッチングが行なわれる。
 */
void asearch_makepat(asearch_context_t *ctx, const char16_t *pattern, sint32_t ambig)
{
	uint32_t mask = INITPAT;

	ctx->mismatch = ambig;
	ctx->epsilon = 0;

	Memory.Fill.Uint32(ctx->shiftpat, 0UL, MAXCHAR);

	for(;*pattern; pattern++){
		if(*pattern == PAT_WILDCARD){ // ワイルドカード文字
			ctx->epsilon |= mask;
		}
		else if (*pattern == PAT_GROUP_START){	// グループ指定（正規表現上の[ABC...]みたいなもの）
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
