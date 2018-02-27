/*
 *   Redistribution and use in source and binary forms, with or without
 *   modification, are permitted provided that the following conditions are met:
 *
 *   * Redistributions of source code must retain the above copyright
 *     notice, this list of conditions and the following disclaimer.
 *
 *   * Redistributions in binary form must reproduce the above copyright
 *     notice, this list of conditions and the following disclaimer in
 *     the documentation and/or other materials provided with the
 *     distribution.
 *
 *   * Neither the name of the copyright holders nor the names of
 *     contributors may be used to endorse or promote products derived
 *     from this software without specific prior written permission.
 *
 *   THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 *   AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 *   IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 *   ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE
 *   LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
 *   CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
 *   SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 *   INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
 *   CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 *   ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
 *   POSSIBILITY OF SUCH DAMAGE.
 */

/**
 * @file
 * @brief	カーネルで用いられる基本的な型を定義
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58
 */

#ifndef __type_h__
#define __type_h__

/**
 * @typedef	uint8_t
 * @brief	8ビット符号なし変数型
 * @note	可能な限り unsigned char ではなく、こちらを使うこと。
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58
 */
typedef unsigned char uint8_t;

/**
 * @typedef	sint8_t
 * @brief	8ビット符号あり変数型
 * @note	可能な限り signed char ではなく、こちらを使うこと。
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58
 */
typedef   signed char sint8_t;

#if (TARGET_ARCH != ARCH_AVR)
/**
 * @typedef	uint16_t
 * @brief	16ビット符号なし変数型
 * @note	可能な限り unsigned short ではなく、こちらを使うこと。
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58
 */
typedef unsigned short uint16_t;
#endif

/**
 * @typedef sint16_t
 * @brief 16ビット符号あり変数型
 * @note 可能な限り signed short ではなく、こちらを使うこと。
 * @author Kazuya Fukuhara
 * @date 2010/01/07 14:08:58
 */
typedef   signed short sint16_t;

/**
 * @typedef uint32_t
 * @brief 32ビット符号なし変数型
 * @note 可能な限り unsigned long ではなく、こちらを使うこと。
 * @author Kazuya Fukuhara
 * @date 2010/01/07 14:08:58
 */
typedef unsigned long uint32_t;

/**
 * @typedef sint32_t
 * @brief 32ビット符号あり変数型
 * @note 可能な限り signed long ではなく、こちらを使うこと。
 * @author Kazuya Fukuhara
 * @date 2010/01/07 14:08:58
 */
typedef   signed long sint32_t;

/**
 * @typedef ptr_t
 * @brief ポインタ型
 * @note 可能な限り void* ではなく、こちらを使うこと。
 * @author Kazuya Fukuhara
 * @date 2010/01/07 14:08:58
 */
typedef          void* ptr_t;

/**
 * @typedef uint_t
 * @brief 無符号整数型
 * @note 可能な限り unsigned int ではなく、こちらを使うこと。
 * @author Kazuya Fukuhara
 * @date 2010/01/07 14:08:58
 */
typedef  unsigned int uint_t;

/**
 * @typedef sint_t
 * @brief 符号整数型
 * @note 可能な限り signed int ではなく、こちらを使うこと。
 * @author Kazuya Fukuhara
 * @date 2010/01/07 14:08:58
 */
typedef    signed int sint_t;

/**
 * @typedef bool_t
 * @brief 真偽値
 * @note 可能な限り 1 や 0 ではなく、こちらを使うこと。
 * @author Kazuya Fukuhara
 * @date 2010/01/07 14:08:58
 */
typedef enum {
	FALSE = 0,
	TRUE  = !0
} bool_t;

#ifndef countof
/**
 * @def     countof
 * @brief   静的配列の要素数を計算
 * @author  Kazuya Fukuhara
 * @date    2010/01/07 14:08:58
 */
#define countof(x) (sizeof(x)/sizeof((x)[0]))
#endif

/**
 * @typedef  taskid_t
 * @brief    タスクＩＤ型の宣言
 * @author   Kazuya Fukuhara
 * @date     2010/01/07 14:08:58
 */
typedef uint8_t taskid_t;

/**
 * @typedef semaphoid_t
 * @brief   セマフォＩＤの型
 * @author  Kazuya Fukuhara
 * @date    2010/01/07 14:08:58
 */
typedef uint8_t semaphoid_t;

/**
 * @typedef priolity_t
 * @brief   優先度の型
 * @author  Kazuya Fukuhara
 * @date    2010/01/07 14:08:58
 */
typedef uint8_t priolity_t;

/**
 * @typedef tick_t
 * @brief   pauseやrestartで時間を指定する際に用いる型
 * @author  Kazuya Fukuhara
 * @date    2010/01/07 14:08:58
 */
typedef uint8_t tick_t;

/**
 * @typedef extintid_t
 * @brief   外部割り込みの番号を示す型
 * @author  Kazuya Fukuhara
 * @date    2010/01/07 14:08:58
 */
typedef uint8_t extintid_t;

#ifndef NULL
/**
 * @def     NULL
 * @brief   ヌルポインタの宣言
 * @author  Kazuya Fukuhara
 * @date    2010/01/07 14:08:58
 */
#define NULL ((ptr_t)0)
#endif

#ifndef global 
/**
 * @def     global
 * @brief   グローバル宣言であることを示すコーディング規約用識別子
 * @author  Kazuya Fukuhara
 * @date    2011/09/27 12:22:19
 */
#define global 
#endif


#endif
