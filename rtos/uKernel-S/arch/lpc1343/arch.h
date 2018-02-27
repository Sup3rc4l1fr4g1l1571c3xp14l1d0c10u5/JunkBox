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
 * @brief	LPC1343アーキテクチャ固有の設定を記述する
 * @author	Kazuya Fukuhara
 * @date	2010/11/28 07:51:53	作成
 */

#ifndef __ARCH_LPC1343_H__
#define __ARCH_LPC1343_H__

/**
 * @def		ARCH_TASK_STACK_OFFSET
 * @brief	タスクのスタックのサイズのオフセット値	
 * @author	Kazuya Fukuhara
 * @date	2010/11/28 07:51:53	作成
 * 
 * LPC1343環境ではオフセットを噛ませる必要がないので0
 */
#define ARCH_TASK_STACK_OFFSET 0

/**
 * @def		TASKPROC
 * @brief	タスクルーチンの呼び出し規約
 * @author	Kazuya Fukuhara
 * @date	2010/11/28 07:51:53	作成
 * 
 * LPCExpresso環境では呼び出し規約は不要
 */
#define TASKPROC /* nothing */

#endif
