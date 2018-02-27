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
 * @brief   Win32アーキテクチャ固有の設定
 * @author  Kazuya Fukuhara
 * @date	2010/09/01 21:36:49	作成
 */

#ifndef __ARCH_WIN32_H__
#define __ARCH_WIN32_H__

#include <windows.h>
#include <assert.h>

#ifdef FALSE
# undef FALSE
#endif
#ifdef TRUE
# undef TRUE
#endif
#ifdef ZeroMemory
# undef ZeroMemory
#endif

/**
 * @def		ARCH_TASK_STACK_OFFSET
 * @brief	タスクのスタックのサイズのオフセット値
 * @author	Kazuya Fukuhara
 * @date	2010/09/01 21:36:49	作成
 * 
 * WIN32のデバッグモードではスタックフレーム破壊を検出するために通常よりも多くのスタックを確保するコードが生成される。そのため最低でも8Kほど下駄を履かせる
 */
#define ARCH_TASK_STACK_OFFSET 8192

/**
 * @def		TASKPROC
 * @brief	タスクルーチンの呼び出し規約
 * @author	Kazuya Fukuhara
 * @date	2010/09/01 21:36:49	作成
 *
 * Win32環境で引数や戻り値などを容易に扱うためにstdcall呼び出し規約を指定
 */
#define TASKPROC __stdcall 

#endif

