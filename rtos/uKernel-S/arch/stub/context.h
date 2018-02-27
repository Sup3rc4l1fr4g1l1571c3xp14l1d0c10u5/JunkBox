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
 * @brief	コンテキストとコンテキスト操作
 * @author  
 * @date	20xx/xx/xx xx:xx:xx	作成
 *
 * 移植対象機種に応じた記述を行うこと。
 */

#ifndef __CONTEXT_H__
#define __CONTEXT_H__

#include "./arch.h"
#include "../../src/type.h"

/**
 * @def		SET_KERNEL_STACKPOINTER
 * @brief	カーネルスタックを現在のスタックポインタに設定する
 * @author	
 * @date	20xx/xx/xx xx:xx:xx	作成
 *
 * 多くの場合、インラインアセンブラを用いて記述する
 */
#define SET_KERNEL_STACKPOINTER()

/**
 * @def			SAVE_CONTEXT
 * @brief		現在のコンテキストを待避
 * @attention	コンテキストはスタックに待避され、スタックポインタは currentTCB->stack_pointer に格納される。そのため、currentTCBに適切な値が設定済みであることが求められる。
 * @author		
 * @date		20xx/xx/xx xx:xx:xx	作成
 *
 * 多くの場合、インラインアセンブラを用いて記述する
 */
#define SAVE_CONTEXT()

/**
 * @def			RESTORE_CONTEXT
 * @brief		コンテキストを復帰する
 * @attention	復帰に使うスタックポインタは currentTCB->stack_pointer から読み出される。そのため、currentTCBに適切な値が設定済みであることが求められる。
 * @author		
 * @date		20xx/xx/xx xx:xx:xx	作成
 *
 * 多くの場合、インラインアセンブラを用いて記述する
 */
#define RESTORE_CONTEXT()

/**
 * @def		RETURN_FROM_INTERRUPT
 * @brief	割り込み処理から脱出する
 * @author  
 * @date	20xx/xx/xx xx:xx:xx	作成
 *
 * 多くの場合、インラインアセンブラを用いて記述する
 */
#define RETURN_FROM_INTERRUPT()

/**
 * @typedef	struct context_t context_t;
 * @struct	context_t
 * @brief	スタックに保存される実行コンテキスト（スタックフレーム）
 * @author	
 * @date	20xx/xx/xx xx:xx:xx	作成
 *
 * 多くの場合は、汎用レジスタ、ステータスレジスタ、戻り先アドレスが格納される
 */
typedef struct context_t {
	int dummy; /**< 各アーキテクチャに従って実装を行ってください */
} context_t;

/**
 * @def			SetReturnAddressToContext
 * @brief		コンテキストの戻り先アドレスを設定する
 * @param[in]	context 対象コンテキスト
 * @param[in]	address 設定する戻り先アドレス
 * @author		
 * @date		20xx/xx/xx xx:xx:xx	作成
 */
#define SetReturnAddressToContext(context, address)

/**
 * @def			GetContext
 * @brief		タスクコントロールブロックから実行コンテキストを取得
 * @param[in]	tcb 対象タスクコントロールブロック
 * @return		実行コンテキストを示すポインタ値
 * @author	
 * @date		20xx/xx/xx xx:xx:xx	作成
 */
#define GetContext(tcb) NULL

/**
 * @def			GetArgWord
 * @brief		コンテキストからポインタ引数を取り出す
 * @param[in]	context 対象コンテキスト
 * @return		引数として渡されたポインタ値
 * @author		
 * @date		20xx/xx/xx xx:xx:xx	作成
 */
#define GetArgPtr(context) NULL

/**
 * @def			SetTaskArg
 * @brief		コンテキストにタスク開始時の引数を設定
 * @param[in]	context 対象コンテキスト
 * @param[in]	arg     設定する引数
 * @author		
 * @date		20xx/xx/xx xx:xx:xx	作成
 */
#define SetTaskArg(context, arg) 

#endif

