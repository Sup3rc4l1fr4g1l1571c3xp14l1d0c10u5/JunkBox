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
 * @brief	ハードウェア依存のコード
 * @author  
 * @date	20xx/xx/xx xx:xx:xx	作成
 *
 * 移植対象機種に応じた記述を行うこと。
 */

#include "../../src/arch.h"
#include "../../src/kernel.h"
#include "../../src/syscall.h"

/**
 * @brief	カーネルスタック領域
 * @author  
 * @date	20xx/xx/xx xx:xx:xx	作成
 */
uint8_t kernel_stack[KERNEL_STACK_SIZE];

/**
 * @brief	ハードウェア全般の初期化処理
 * @author  
 * @date	20xx/xx/xx xx:xx:xx	作成
 * 
 * カーネルタイマやソフトウェア割り込みを有効化するなどの初期化を行う。
 */
void initHardware(void) {
}

/**
 * @brief			コンテキストの初期化
 * @param[in] tid	タスク番号
 * @author  
 * @date	20xx/xx/xx xx:xx:xx	作成
 * 
 * 指定されたタスクのコンテキストの初期化を行う。具体的には以下の処理を行う
 * @li スタックポインタの初期値を設定
 * @li コンテキストを抜けた際に自動的にexitが呼ばれるように設定
 * @li コンテキストの初期値設定
 * @li 開始アドレス値の設定
 */
void resetContext(taskid_t tid) {
}

/**
 * @brief					システムコール呼び出し
 * @param[in,out] param		呼び出すシステムコールのＩＤや引数の格納された構造体へのポインタ
 * @author  
 * @date	20xx/xx/xx xx:xx:xx	作成
 * 
 * ソフトウェア割り込み、もしくは相当の処理を用いて、システムコールを呼び出す。
 */
svcresultid_t syscall(ptr_t param) {
}

/**
 * @brief	ソフトウェア割り込みの捕捉
 * @author  
 * @date	20xx/xx/xx xx:xx:xx	作成
 * 
 * 呼び出すシステムコールのID、引数などにアクセスできるようにコンテキストを待避。
 * その後、カーネルスタックへ切り替えてから syscall_entry() を呼び出す。
 */
void software_int(void) {
}

/**
 * @brief	スタートアップルーチン
 * @author  
 * @date	20xx/xx/xx xx:xx:xx	作成
 * 
 * カーネルスタックへの切り替えを行い、必要な処理を行ってから、startKernel()を呼び出す。
 */
void main(void) {
}

/**
 * @brief			フック可能な外部割り込み番号か判定
 * @param[in] id	外部割り込み番号
 * @retval TRUE		フック可能な外部割り込み番号
 * @retval FALSE	フック不可能な外部割り込み番号
 * @author	
 * @date			20xx/xx/xx xx:xx:xx	作成
 */
bool_t is_hookable_interrupt_id(extintid_t int_id) {
	/* カーネルタイマや、システムコール呼び出しに使うソフトウェア割り込みなどのフックを防ぐようにする */
}

