#line 28 "C:\\workspace\\ソースコード入れ\\uKernel-S\\src\\message.c"
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
 * @brief   タスク間メッセージ通信
 * @author  Kazuya Fukuhara
 * @date    2010/01/07 14:08:58	作成
 */


#line 28 "C:\\workspace\\ソースコード入れ\\uKernel-S\\src\\message.h"
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
 * @brief   タスク間メッセージ通信
 * @author  Kazuya Fukuhara
 * @date    2010/01/07 14:08:58	作成
 */





#line 28 "C:\\workspace\\ソースコード入れ\\uKernel-S\\src\\kernel.h"
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
 * @brief   カーネルコア
 * @author  Kazuya Fukuhara
 * @date    2010/01/07 14:08:58	作成
 * @date	2010/08/15 16:11:02	機種依存部分を分離
 * @date	2010/09/09 12:48:22	タイムシェアリングスケジューリングを追加
 * @date	2010/09/10 10:57:13	優先度付きタイムシェアリングスケジューリングを追加
 */





#line 28 "C:\\workspace\\ソースコード入れ\\uKernel-S\\src\\arch.h"
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
 * @brief   アーキテクチャ選択
 * @author  Kazuya Fukuhara
 * @date	2010/08/15 16:11:02	作成
 * @date	2010/09/01 21:36:49	WIN32アーキテクチャを追加
 *
 * 新しいアーキテクチャを追加した場合、このファイルにアーキテクチャ固有の
 * ヘッダファイルへのincludeを追加すること。
 */





#line 28 "C:\\workspace\\ソースコード入れ\\uKernel-S\\src\\config.h"
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
 * @brief   カーネル設定
 * @note    カーネル本体の変更可能な設定部分はすべてここに記述する
 * @author  Kazuya Fukuhara
 * @date    2010/01/07 14:08:58	作成
 * @date	2010/08/15 16:11:02	機種依存部分を分離
 * @date	2010/09/01 21:36:49	WIN32アーキテクチャを追加
 * @date	2010/09/04 19:46:15	M16Cマイコン搭載T-Birdボードアーキテクチャを追加
 * @date	2010/09/09 12:48:22	タイムシェアリングスケジューリング追加
 * @date	2010/09/10 10:57:13	優先度付きタイムシェアリングスケジューリングを追加
 */




/* アーキテクチャ */






/**
 * @def		ARCH_DUMMY
 * @brief	ダミーターゲット
 * @author  Kazuya Fukuhara
 * @date	2010/08/15 16:11:02	作成
 */







/**
 * @def		ARCH_AVR
 * @brief	AVRマイコンを対象とする
 * @author  Kazuya Fukuhara
 * @date	2010/08/15 16:11:02	作成
 */







/**
 * @def		ARCH_WIN32
 * @brief	WIN32アプリケーションを対象とする
 * @author  Kazuya Fukuhara
 * @date	2010/09/01 21:36:49	作成
 */







/**
 * @def		ARCH_M16C_TBIRD
 * @brief	M16Cマイコン搭載T-Birdボードを対象とする
 * @author  Kazuya Fukuhara
 * @date	2010/09/04 19:46:15	作成
 */







/**
 * @def		ARCH_LPC1343
 * @brief	LPC1343(LPCXpresso1343)を対象とする
 * @author  Kazuya Fukuhara
 * @date	2010/09/04 19:46:15	作成
 */







/**
 * @def		ARCH_RX63N
 * @brief	RX63N(GR-SAKURA)を対象とする
 * @author  Kazuya Fukuhara
 * @date	2010/09/04 19:46:15	作成
 */















/**
 * @def		TARGET_ARCH
 * @brief	対象とするアーキテクチャの指定
 * @author  Kazuya Fukuhara
 * @date	2010/08/15 16:11:02	作成
 *
 * 対応アーキテクチャは以下の通り
 * - ARCH_AVR        : Atmel AVR (ATMega328P)
 * - ARCH_WIN32      : Windows上のアプリ (シミュレーション)
 * - ARCH_M16C_TBIRD : 教育用マイコンボードT-Bird(M16C62A)
 * - ARCH_LPC1343    : LPCXpresso1343 (CortexM3 LPC1343 評価ボード)
 * - ARCH_RX63N      : RX63N (GR-SAKURAボード)
 *
 */









/**
 * @def     TASK_NUM
 * @brief   タスク総数
 * @author  Kazuya Fukuhara
 * @note    コンパイル時にタスク数を決定する。
 * @note    タスク数を増やす場合はここで増やす。
 * @date    2010/01/07 14:08:58	作成
 */









/**
 * @def     TASK_STACK_SIZE
 * @brief   タスクスタックのサイズ
 * @note    一つのタスクに割り当てられる実行時スタックのサイズ
 * @author  Kazuya Fukuhara
 * @date    2010/01/07 14:08:58	作成
 * @date	2010/09/01 21:36:49	WIN32アーキテクチャの追加にともない、オフセットを指定可能に変更
 */









/**
 * @def     KERNEL_STACK_SIZE
 * @brief   カーネルスタックのサイズ
 * @note    カーネルに割り当てられる実行時スタックのサイズ
 * @author  Kazuya Fukuhara
 * @date    2010/01/07 14:08:58	作成
 * @date	2010/09/01 21:36:49	WIN32アーキテクチャの追加にともない、オフセットを指定可能に変更
 */







/**
 * @def     SEMAPHO_NUM
 * @brief   カーネル全体で使えるセマフォの総数
 * @author  Kazuya Fukuhara
 * @date    2010/01/07 14:08:58	作成
 */







/**
 * @def     MESSAGE_NUM
 * @brief   カーネル全体で使えるメッセージの総数
 * @author  Kazuya Fukuhara
 * @date    2010/01/07 14:08:58	作成
 */










/**
 * @def     SCHEDULING_PRIORITY_BASE
 * @brief   固定優先度スケジューリング
 * @author  Kazuya Fukuhara
 * @date	2010/09/09 12:48:22	作成
 *
 * 優先度の高いタスクが実行権を得ている場合、そのタスクが待ち状態になるか、
 * より高い優先度を持ったタスクが実行可能状態にならない限り、実行権を放棄しない。
 */











/**
 * @def     SCHEDULING_TIME_SHARING
 * @brief   優先度なしタイムシェアリングスケジューリング
 * @author  Kazuya Fukuhara
 * @date	2010/09/09 12:48:22	作成
 *
 * 実行権を得たタスクには一定のCPU時間が割り当てられる。
 * 割り当てられた時間を使い切るとカーネルが強制的に次のタスクに実行権を与える。
 * 実行権を奪われたタスクは他のタスクの実行が終わった後に、再び実行権が割り当てられる。
 */













/**
 * @def     SCHEDULING_PRIORITY_BASE_TIME_SHARING
 * @brief   優先度付きタイムシェアリングスケジューリング（ラウンドロビン）
 * @author  Kazuya Fukuhara
 * @date	2010/09/10 10:57:13	作成
 *
 * タスクの優先度ごとにグループ化される。
 * 実行権を得たグループのタスクには順番に一定のCPU時間が割り当てられる。
 * 割り当てられた時間を使い切るとカーネルが強制的に同一優先度内の次のタスクに実行権を与える。
 * 実行権を奪われたタスクは同一優先度の他のタスクの実行が終わった後に、再び実行権が割り当てられる。
 * より優先度の高いグループに属するタスクが実行可能状態になった場合、実行権が移動する。
 */







/**
 * @def     SCHEDULER_TYPE
 * @brief   スケジューラを選択
 * @author  Kazuya Fukuhara
 * @date	2010/09/09 12:48:22	作成
 */

















#line 46 "C:\\workspace\\ソースコード入れ\\uKernel-S\\src\\arch.h"



#line 28 "C:\\workspace\\ソースコード入れ\\uKernel-S\\arch\\stub\\arch.h"
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
 * @brief   アーキテクチャ固有の設定
 * @author  
 * @date	20xx/xx/xx xx:xx:xx	作成
 *
 * 移植対象機種に応じた記述を行うこと。
 */










/**
 * @def		ARCH_TASK_STACK_OFFSET
 * @brief	タスクのスタックのサイズのオフセット値	
 * @note	環境に応じて設定が必要
 * @author	作成者名
 * @date	20xx/xx/xx xx:xx:xx	作成
 */








/**
 * @def		TASKPROC
 * @brief	タスクルーチンの呼び出し規約
 * @note	環境に応じて設定が必要
 * @author	作成者名
 * @date	20xx/xx/xx xx:xx:xx	作成
 */



#line 49 "C:\\workspace\\ソースコード入れ\\uKernel-S\\src\\arch.h"

#line 28 "C:\\workspace\\ソースコード入れ\\uKernel-S\\arch\\stub\\hw.h"
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





#line 28 "C:\\workspace\\ソースコード入れ\\uKernel-S\\src\\syscall.h"
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
 * @brief	システムコール
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	作成
 * @date	2010/08/15 16:11:02	機種依存部分を分離
 * @date	2010/09/10 10:57:13	優先度付きタイムシェアリングスケジューリング用のシステムコールを追加
 * @date	2010/09/11 11:01:20	外部割り込みフック用のシステムコールを追加
 */





#line 28 "C:\\workspace\\ソースコード入れ\\uKernel-S\\src\\type.h"
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










/**
 * @typedef	uint8_t
 * @brief	8ビット符号なし変数型
 * @note	可能な限り unsigned char ではなく、こちらを使うこと。
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58
 */
typedef unsigned char uint8_t ; 







/**
 * @typedef	sint8_t
 * @brief	8ビット符号あり変数型
 * @note	可能な限り signed char ではなく、こちらを使うこと。
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58
 */
typedef   signed char sint8_t ; 








/**
 * @typedef	uint16_t
 * @brief	16ビット符号なし変数型
 * @note	可能な限り unsigned short ではなく、こちらを使うこと。
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58
 */
typedef unsigned short uint16_t ; 








/**
 * @typedef sint16_t
 * @brief 16ビット符号あり変数型
 * @note 可能な限り signed short ではなく、こちらを使うこと。
 * @author Kazuya Fukuhara
 * @date 2010/01/07 14:08:58
 */
typedef   signed short sint16_t ; 







/**
 * @typedef uint32_t
 * @brief 32ビット符号なし変数型
 * @note 可能な限り unsigned long ではなく、こちらを使うこと。
 * @author Kazuya Fukuhara
 * @date 2010/01/07 14:08:58
 */
typedef unsigned long uint32_t ; 







/**
 * @typedef sint32_t
 * @brief 32ビット符号あり変数型
 * @note 可能な限り signed long ではなく、こちらを使うこと。
 * @author Kazuya Fukuhara
 * @date 2010/01/07 14:08:58
 */
typedef   signed long sint32_t ; 







/**
 * @typedef ptr_t
 * @brief ポインタ型
 * @note 可能な限り void* ではなく、こちらを使うこと。
 * @author Kazuya Fukuhara
 * @date 2010/01/07 14:08:58
 */
typedef          void * ptr_t ; 







/**
 * @typedef uint_t
 * @brief 無符号整数型
 * @note 可能な限り unsigned int ではなく、こちらを使うこと。
 * @author Kazuya Fukuhara
 * @date 2010/01/07 14:08:58
 */
typedef  unsigned int uint_t ; 







/**
 * @typedef sint_t
 * @brief 符号整数型
 * @note 可能な限り signed int ではなく、こちらを使うこと。
 * @author Kazuya Fukuhara
 * @date 2010/01/07 14:08:58
 */
typedef    signed int sint_t ; 







/**
 * @typedef bool_t
 * @brief 真偽値
 * @note 可能な限り 1 や 0 ではなく、こちらを使うこと。
 * @author Kazuya Fukuhara
 * @date 2010/01/07 14:08:58
 */
typedef enum { 
	FALSE = 0 , 
	TRUE  = ! 0
} bool_t ; 







/**
 * @def     countof
 * @brief   静的配列の要素数を計算
 * @author  Kazuya Fukuhara
 * @date    2010/01/07 14:08:58
 */








/**
 * @typedef  taskid_t
 * @brief    タスクＩＤ型の宣言
 * @author   Kazuya Fukuhara
 * @date     2010/01/07 14:08:58
 */
typedef uint8_t taskid_t ; 






/**
 * @typedef semaphoid_t
 * @brief   セマフォＩＤの型
 * @author  Kazuya Fukuhara
 * @date    2010/01/07 14:08:58
 */
typedef uint8_t semaphoid_t ; 






/**
 * @typedef priolity_t
 * @brief   優先度の型
 * @author  Kazuya Fukuhara
 * @date    2010/01/07 14:08:58
 */
typedef uint8_t priolity_t ; 






/**
 * @typedef tick_t
 * @brief   pauseやrestartで時間を指定する際に用いる型
 * @author  Kazuya Fukuhara
 * @date    2010/01/07 14:08:58
 */
typedef uint8_t tick_t ; 






/**
 * @typedef extintid_t
 * @brief   外部割り込みの番号を示す型
 * @author  Kazuya Fukuhara
 * @date    2010/01/07 14:08:58
 */
typedef uint8_t extintid_t ; 







/**
 * @def     NULL
 * @brief   ヌルポインタの宣言
 * @author  Kazuya Fukuhara
 * @date    2010/01/07 14:08:58
 */









/**
 * @def     global
 * @brief   グローバル宣言であることを示すコーディング規約用識別子
 * @author  Kazuya Fukuhara
 * @date    2011/09/27 12:22:19
 */





#line 51 "C:\\workspace\\ソースコード入れ\\uKernel-S\\src\\syscall.h"
typedef enum svcresultid_t { 
	SUCCESS	= 0 ,	/**< 正常終了 */
	ERR1	= 1 ,	/**< タスクＩＤが不正  */
	ERR2	= 2 ,	/**< DORMANT状態でないタスクを起動した */
	ERR3	= 3 ,	/**< [予約] */
	ERR4	= 4 ,	/**< [予約] */
	ERR5	= 5 ,	/**< wait 状態でないタスクを resume させようとした */
	ERR6	= 6 ,	/**< Pause指定時間が不正(0) */
	ERR7	= 7 ,	/**< [予約] */
	ERR8	= 8 ,	/**< [予約] */
	ERR9	= 9 ,	/**< [予約] */
	ERR10	= 10 ,	/**< セマフォＩＤが不正  */
	ERR11	= 11 ,	/**< 指定したセマフォが獲得できない */
	ERR12	= 12 ,	/**< 指定したセマフォを開放できない */
	ERR13	= 13 ,	/**< [予約] */
	ERR14	= 14 ,	/**< [予約] */
	ERR15	= 15 ,	/**< [予約] */
	ERR16	= 16 ,	/**< [予約] */
	ERR17	= 17 ,	/**< [予約] */
	ERR18	= 18 ,	/**< [予約] */
	ERR19	= 19 ,	/**< [予約] */
	ERR20	= 20 ,	/**< 自タスクを reset しようとした */
	ERR21	= 21 ,	/**< [予約] */
	ERR22	= 22 ,	/**< [予約] */
	ERR23	= 23 ,	/**< [予約] */
	ERR24	= 24 ,	/**< [予約] */
	ERR25	= 25 ,	/**< [予約] */
	ERR26	= 26 ,	/**< [予約] */
	ERR27	= 27 ,	/**< 不正な割込み番号  */
	ERR28	= 28 ,	/**< [予約] */
	ERR29	= 29 ,	/**< [予約] */
	ERR30	= 30 ,	/**< 不正なシステムコール呼び出し */
	ERR31	= 31 ,	/**< INIT/DIAGからは呼び出せないシステムコールを呼び出した */
	ERR40	= 40 ,	/**< メール送信失敗 */
	ERR41	= 41 ,	/**< メールボックスは空 */
	ERR42	= 42 ,	/**< メール取得失敗 */
} svcresultid_t ; 







/**
 * @brief			システムコール呼び出し処理
 * @note			機種依存コードが含まれる
 * @author			Kazuya Fukuhara
 * @date			2010/01/07 14:08:58	作成
 * @date			2010/08/15 16:11:02	機種依存部分を分離
 */
extern void syscall_entry ( void ) ; 











/**
 * @brief		指定したタスクＩＤのタスクを起動する
 * @param[in]	taskId 起動するタスクのタスクＩＤ
 * @param[in]	param 起動するタスクに与える引数
 * @param[in]	priolity 起動するタスクに与える引数(優先度付きタイムシェアリングスケジューリング時に有効)
 * @return		システムコールの成否情報
 * @author		Kazuya Fukuhara
 * @date		2010/01/07 14:08:58	作成
 * @date		2010/08/15 16:11:02	機種依存部分を分離
 * @date		2010/09/10 10:57:13	優先度付きタイムシェアリングスケジューリングで使用する引数 priolity を追加
 */
extern svcresultid_t API_startTASK ( taskid_t taskId , ptr_t param




) ; 







/**
 * @brief		現在実行中のタスクを終了する。
 * @return		システムコールの成否情報
 * @note		タスクのリセット処理も行われる。
 * @author		Kazuya Fukuhara
 * @date		2010/01/07 14:08:58	作成
 */
extern svcresultid_t API_exitTASK ( void ) ; 









/**
 * @brief			現在実行中のタスクを休眠状態にする
 * @param[in] count	休眠状態にする時間（tick単位）
 * @return			システムコールの成否情報
 * @note            countを0に設定すると、resumeTASKシステムコールで再開させるまで無限に休眠する。
 * @note            長期間休眠(count > 255)には割り込みなど別の要素を使うこと。
 * @author          Kazuya Fukuhara
 * @date            2010/01/07 14:08:58	作成
 */
extern svcresultid_t API_pauseTASK ( tick_t count ) ; 







/**
 * @brief				指定した休眠状態のタスクを再開中にする
 * @param[in] taskId	再開するタスクのタスクＩＤ
 * @return				システムコールの成否情報
 * @author				Kazuya Fukuhara
 * @date				2010/01/07 14:08:58	作成
 */
extern svcresultid_t API_resumeTASK ( taskid_t taskId ) ; 









/**
 * @brief			自タスクをリスタートする
 * @param[in] count	再開するまでの時間（tick単位）
 * @return			システムコールの成否情報
 * @note            countを0に設定すると、resumeTASKシステムコールで再開させるまで無限に休眠する。
 * @note            長期間休眠(count > 255)には割り込みなど別の要素を使うこと。
 * @author          Kazuya Fukuhara
 * @date            2010/01/07 14:08:58	作成
 */
extern svcresultid_t API_restartTASK ( tick_t count ) ; 







/**
 * @brief				自タスクのタスクＩＤを取得する
 * @param[out] pTaskID	自タスクのタスクＩＤが格納される領域
 * @return				システムコールの成否情報
 * @author				Kazuya Fukuhara
 * @date				2010/01/07 14:08:58	作成
 */
extern svcresultid_t API_getTID ( taskid_t * pTaskID ) ; 







/**
 * @brief			takeSEMAを呼び出すアダプタ関数
 * @param[in] sid	獲得するセマフォのＩＤ
 * @return			システムコールの成否情報
 * @author			Kazuya Fukuhara
 * @date			2010/01/07 14:08:58	作成
 */
extern svcresultid_t API_takeSEMA ( semaphoid_t sid ) ; 







/**
 * @brief			giveSEMAを呼び出すアダプタ関数
 * @param[in] sid	放棄するセマフォのＩＤ
 * @return			システムコールの成否情報
 * @author			Kazuya Fukuhara
 * @date			2010/01/07 14:08:58	作成
 */
extern svcresultid_t API_giveSEMA ( semaphoid_t sid ) ; 







/**
 * @brief			tasSEMAを呼び出すアダプタ関数
 * @param[in] sid	獲得を試みるセマフォのＩＤ
 * @return			システムコールの成否情報
 * @author			Kazuya Fukuhara
 * @date			2010/01/07 14:08:58	作成
 */
extern svcresultid_t API_tasSEMA ( semaphoid_t sid ) ; 








/**
 * @brief			recvMSGを呼び出すアダプタ関数
 * @param[out] from	メッセージの送信タスクIDを格納する領域のポインタ
 * @param[out] data	メッセージを格納する領域のポインタ
 * @return			システムコールの成否情報
 * @author			Kazuya Fukuhara
 * @date			2010/01/07 14:08:58	作成
 */
extern svcresultid_t API_recvMSG ( taskid_t * from , ptr_t * data ) ; 








/**
 * @brief			sendMSGを呼び出すアダプタ関数
 * @param[in] to	送信先のタスクのＩＤ
 * @param[in] data	送信するメッセージの本文
 * @return			システムコールの成否情報
 * @author			Kazuya Fukuhara
 * @date			2010/01/07 14:08:58	作成
 */
extern svcresultid_t API_sendMSG ( taskid_t to , ptr_t data ) ; 






/**
 * @brief		waitMSGを呼び出すアダプタ関数
 * @return		システムコールの成否情報
 * @author		Kazuya Fukuhara
 * @date		2010/01/07 14:08:58	作成
 */
extern svcresultid_t API_waitMSG ( void ) ; 









/**
 * @brief					hookInterruptを呼び出すアダプタ関数
 * @param[in] interrupt_id	フックする外部割り込み番号
 * @param[in] task_id		割り込みで起動するタスクの番号
 * @param[in] priolity		起動するタスクに与える引数(優先度付きタイムシェアリングスケジューリング時に有効)
 * @return					システムコールの成否情報
 * @author					Kazuya Fukuhara
 * @date					2010/09/11 11:01:20	作成
 */
extern svcresultid_t API_hookInterrupt ( extintid_t interrupt_id , taskid_t task_id




) ; 

























#line 43 "C:\\workspace\\ソースコード入れ\\uKernel-S\\arch\\stub\\hw.h"

#line 28 "C:\\workspace\\ソースコード入れ\\uKernel-S\\arch\\stub\\context.h"
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














/**
 * @def		SET_KERNEL_STACKPOINTER
 * @brief	カーネルスタックを現在のスタックポインタに設定する
 * @author	
 * @date	20xx/xx/xx xx:xx:xx	作成
 *
 * 多くの場合、インラインアセンブラを用いて記述する
 */










/**
 * @def			SAVE_CONTEXT
 * @brief		現在のコンテキストを待避
 * @attention	コンテキストはスタックに待避され、スタックポインタは currentTCB->stack_pointer に格納される。そのため、currentTCBに適切な値が設定済みであることが求められる。
 * @author		
 * @date		20xx/xx/xx xx:xx:xx	作成
 *
 * 多くの場合、インラインアセンブラを用いて記述する
 */










/**
 * @def			RESTORE_CONTEXT
 * @brief		コンテキストを復帰する
 * @attention	復帰に使うスタックポインタは currentTCB->stack_pointer から読み出される。そのため、currentTCBに適切な値が設定済みであることが求められる。
 * @author		
 * @date		20xx/xx/xx xx:xx:xx	作成
 *
 * 多くの場合、インラインアセンブラを用いて記述する
 */









/**
 * @def		RETURN_FROM_INTERRUPT
 * @brief	割り込み処理から脱出する
 * @author  
 * @date	20xx/xx/xx xx:xx:xx	作成
 *
 * 多くの場合、インラインアセンブラを用いて記述する
 */










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
	int dummy ; /**< 各アーキテクチャに従って実装を行ってください */
} context_t ; 








/**
 * @def			SetReturnAddressToContext
 * @brief		コンテキストの戻り先アドレスを設定する
 * @param[in]	context 対象コンテキスト
 * @param[in]	address 設定する戻り先アドレス
 * @author		
 * @date		20xx/xx/xx xx:xx:xx	作成
 */









/**
 * @def			GetContext
 * @brief		タスクコントロールブロックから実行コンテキストを取得
 * @param[in]	tcb 対象タスクコントロールブロック
 * @return		実行コンテキストを示すポインタ値
 * @author	
 * @date		20xx/xx/xx xx:xx:xx	作成
 */









/**
 * @def			GetArgWord
 * @brief		コンテキストからポインタ引数を取り出す
 * @param[in]	context 対象コンテキスト
 * @return		引数として渡されたポインタ値
 * @author		
 * @date		20xx/xx/xx xx:xx:xx	作成
 */









/**
 * @def			SetTaskArg
 * @brief		コンテキストにタスク開始時の引数を設定
 * @param[in]	context 対象コンテキスト
 * @param[in]	arg     設定する引数
 * @author		
 * @date		20xx/xx/xx xx:xx:xx	作成
 */



#line 50 "C:\\workspace\\ソースコード入れ\\uKernel-S\\arch\\stub\\hw.h"
extern void initHardware ( void ) ; 






/**
 * @brief			コンテキストの初期化
 * @param[in] tid	タスク番号
 * @author  
 * @date	20xx/xx/xx xx:xx:xx	作成
 */
extern void resetContext ( taskid_t tid ) ; 








/**
 * @brief				システムコール呼び出し
 * @param[in] id 		呼び出すシステムコールのID
 * @param[in] param1	システムコールの引数１
 * @param[in] param2	システムコールの引数２
 * @author  
 * @date	20xx/xx/xx xx:xx:xx	作成
 */
extern  svcresultid_t syscall ( ptr_t param ) ; 






/**
 * @def		disableInterrupt
 * @brief	割り込みを禁止する
 * @author  
 * @date	20xx/xx/xx xx:xx:xx	作成
 */







/**
 * @def		enableInterrupt
 * @brief	割り込みを許可する
 * @author  
 * @date	20xx/xx/xx xx:xx:xx	作成
 */







/**
 * @def		GetExtIntId
 * @brief	発生した外部割り込み番号の取得
 * @author	
 * @date	20xx/xx/xx xx:xx:xx	作成
 */








/**
 * @enum	interrupt_id_t
 * @typedef	enum interrupt_id_t interrupt_id_t
 * @brief	割り込み番号
 * @author	
 * @date	20xx/xx/xx xx:xx:xx	作成
 */
typedef enum interrupt_id_t { 
	/* ここに割り込み要因と対応する要素を列挙する */
	INT_MAX	/**< 特に問題がない場合、この要素を外部割込みの個数に使える */
} interrupt_id_t ; 






/**
 * @def		EXTINT_NUM
 * @brief	外部割り込みの個数
 * @author	
 * @date	20xx/xx/xx xx:xx:xx	作成
 */









/**
 * @brief			フック可能な外部割り込み番号か判定
 * @param[in] id	外部割り込み番号
 * @retval TRUE		フック可能な外部割り込み番号
 * @retval FALSE	フック不可能な外部割り込み番号
 * @author	
 * @date			20xx/xx/xx xx:xx:xx	作成
 */
extern bool_t is_hookable_interrupt_id ( extintid_t int_id ) ; 









/**
 * @def		EnableExtInterrupt
 * @brief	特定の外部割り込み番号の割り込みを有効化する
 * @author	
 * @date	20xx/xx/xx xx:xx:xx	作成
 * 
 * M16Cのようにレジスタなどで割り込み要因ごとに有効無効を設定できる場合は、そちらを用いるコードを作成。
 * そうでない場合は、別途テーブルなどを作成して有効無効を設定するようにすること。
 */










/**
 * @def		DisableExtInterrupt
 * @brief	特定の外部割り込み番号の割り込みを無効化する
 * @author	
 * @date	20xx/xx/xx xx:xx:xx	作成
 * 
 * M16Cのようにレジスタなどで割り込み要因ごとに有効無効を設定できる場合は、そちらを用いるコードを作成。
 * そうでない場合は、別途テーブルなどを作成して有効無効を設定するようにすること。
 */



#line 50 "C:\\workspace\\ソースコード入れ\\uKernel-S\\src\\arch.h"





























#line 44 "C:\\workspace\\ソースコード入れ\\uKernel-S\\src\\kernel.h"









































/**
 * @typedef	enum stateid_t stateid_t;
 * @enum	stateid_t
 * @brief	タスクの状態を示す定数
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	作成
 * @par		タスクの状態の遷移図:
 * @dot
 *   digraph state {
 *       node [ shape=box, fontname="ＳＨ Ｇ30-M", fontsize=10];
 *       edge [ fontname="ＳＨ Ｇ30-M", fontsize=10];
 *
 *       NON_EXISTS   [ label="NON_EXISTS" URL="\ref NON_EXISTS"];
 *       DORMANT      [ label="DORMANT" URL="\ref DORMANT"];
 *       READY        [ label="READY" URL="\ref READY"];
 *       PAUSE        [ label="PAUSE" URL="\ref PAUSE"];
 *       WAIT_MSG     [ label="WAIT_MSG" URL="\ref WAIT_MSG"];
 *       WAIT_SEMA    [ label="WAIT_SEMA" URL="\ref WAIT_SEMA"];
 *       WAIT_RESTART [ label="WAIT_RESTART" URL="\ref WAIT_RESTART"];
 *
 *       NON_EXISTS   -> DORMANT      [ arrowhead="open", style="solid", label="reset" ];
 *       DORMANT      -> READY        [ arrowhead="open", style="solid", label="start" ];
 *       READY        -> PAUSE        [ arrowhead="open", style="solid", label="pause" ];
 *       READY        -> DORMANT      [ arrowhead="open", style="solid", label="exit" ];
 *       PAUSE        -> READY        [ arrowhead="open", style="solid", label="resume" ];
 *       READY        -> WAIT_RESTART [ arrowhead="open", style="solid", label="restart" ];
 *       WAIT_RESTART -> READY        [ arrowhead="open", style="solid", label="" ];
 *       READY        -> WAIT_SEMA    [ arrowhead="open", style="solid", label="takeSema" ];
 *       WAIT_SEMA    -> READY        [ arrowhead="open", style="solid", label="giveSema" ];
 *       READY        -> WAIT_MSG     [ arrowhead="open", style="solid", label="waitMsg" ];
 *       WAIT_MSG     -> READY        [ arrowhead="open", style="solid", label="sendMsg" ];
 *
 *       { rank = same; NON_EXISTS }
 *       { rank = same; DORMANT }
 *       { rank = same; READY }
 *       { rank = same; PAUSE, WAIT_MSG, WAIT_SEMA, WAIT_RESTART }
 *   }
 * @enddot
 */
typedef enum stateid_t { 
	NON_EXISTS ,		/**< 未初期化状態  */
	DORMANT ,		/**< 休止状態：未起動、実行可能 */ 
	READY , 			/**< 実行状態：起動済み、実行可能 */
	PAUSE , 			/**< 待ち状態：起動済み、実行不可能 */
	WAIT_MSG ,		/**< メッセージ待ち状態：起動済み、実行不可能 */
	WAIT_SEMA ,		/**< セマフォ待ち状態：起動済み、実行不可能 */
	WAIT_RESTART ,	/**< リスタート待ち状態：起動済み、実行不可能 */
} stateid_t ; 









/**
 * @typedef	struct tcb_t tcb_t;
 * @struct	tcb_t
 * @brief	タスクコントロールブロック
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	作成
 * @date	2010/09/09 12:48:22	タイムシェアリングスケジューリングを追加
 * @date	2010/09/10 10:57:13	優先度付きタイムシェアリングスケジューリングを追加
 */
typedef struct tcb_t { 
	uint8_t *			stack_pointer ;	/**< スタックポインタ */
	stateid_t			status ;			/**< タスクの状態 */
	ptr_t				parameter ;		/**< タスクの起動時引数 */
	uint8_t				pause_count ;	/**< 待ち時間(0x00は永遠にお休み) */
	struct tcb_t *		next_task ;		/**< 各種行列でのリンクリスト用 */
	struct message_t *	message ;		/**< 受信したメッセージのキュー */


















} tcb_t ; 








/**
 * @def		TCBtoTID
 * @brief	タスクコントロールブロックのアドレスからタスクＩＤを得る
 * @author	Kazuya Fukuhara
 * @warning	ルネサス M3T-NC30WA などは可換が可能な場合でも除算をビットシフトに置換しない。
 *          その結果、除算関数が呼ばれ、スタックが溢れることもあるため、必要ならばビットシフトが可能な形に書き換えてもよい。
 * @date	2010/01/07 14:08:58	作成
 */






/**
 * @brief 	タスクコントロールブロック配列
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	作成
 */
extern tcb_t	tasks [ ( 3 + 2 ) ] ; 





/**
 * @brief	タスクの実行時スタック領域
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	作成
 */
extern uint8_t	task_stack [ ( 3 + 2 ) ] [ ( 1024 + 0 ) ] ; 







/**
 * @typedef	struct readyqueue_t readyqueue_t;
 * @struct	readyqueue_t
 * @brief	ready状態のタスクを並べるキュー型
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	作成
 */
typedef struct readyqueue_t { 
	tcb_t *	next_task ;		/**< リンクリスト用 */
} readyqueue_t ; 





/**
 * @brief	ready状態のタスクを並べるキュー
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	作成
 */
extern readyqueue_t readyqueue ; 










/**
 * @brief	指定したtidがユーザータスクを示しているか判定する。
 * @author	Kazuya Fukuhara
 * @param   tid 判定するタスクＩＤ
 * @retval	TRUE  ユーザータスクである
 * @retval	FALSE ユーザータスクではない
 * @date	2010/12/03 20:31:26	作成
 *
 * ユーザータスクとは、INIT/DIAGを除いたタスクのこと。
 */
extern bool_t isUserTask ( taskid_t tid ) ; 






/**
 * @brief			タスクをreadyqueueに挿入する
 * @param[in] tcb	挿入するタスク
 * @author			Kazuya Fukuhara
 * @date			2010/01/07 14:08:58	作成
 */
extern void addTaskToReadyQueue ( tcb_t * tcb ) ; 






/**
 * @brief			タスクをreadyqueueから取り除く
 * @param[in] tcb	取り除くタスク
 * @author			Kazuya Fukuhara
 * @date			2010/01/07 14:08:58	作成
 */
extern bool_t removeTaskFromReadyQueue ( tcb_t * tcb ) ; 





/**
 * @brief	待ち時間が有限のwait状態のタスクを並べるキュー
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	作成
 */
typedef struct { 
	tcb_t *	next_task ;		/**< リンクリスト用 */
} waitqueue_t ; 





/**
 * @brief	wait状態のタスクを並べるキュー
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	作成
 */
extern waitqueue_t waitqueue ; 











/**
 * @brief			tcbで示されるタスクを待ち行列 WaitQueue に追加する
 * @param[in] tcb	追加するタスク
 * @param[in] time	待ち時間
 * @retval FALSE	タスクは WaitQueue 中に存在しなかった。
 * @retval TRUE		タスクは WaitQueue 中に存在したので取り除いた。
 * @note			tcbの妥当性は検査しないので注意
 * @note			time に 0 を渡した場合は、待ち行列には追加されない
 * @author			Kazuya Fukuhara
 * @date			2010/01/07 14:08:58	作成
 */
extern void addTaskToWaitQueue ( tcb_t * tcb , uint8_t time ) ; 









/**
 * @brief			tcbで示されるタスクが待ち行列 WaitQueue 中にあれば取り除く。
 * @param[in] tcb	取り除くタスク
 * @retval FALSE	タスクは WaitQueue 中に存在しなかった。
 * @retval TRUE		タスクは WaitQueue 中に存在したので取り除いた。
 * @note			tcbの妥当性は検査しないので注意
 * @author			Kazuya Fukuhara
 * @date			2010/01/07 14:08:58	作成
 */
extern bool_t removeTaskFromWaitQueue ( tcb_t * tcb ) ; 






/**
 * @def		INITTASK
 * @brief	初期化タスク
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	作成
 */







/**
 * @def		DIAGTASK
 * @brief	Idleタスク
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	作成
 */






/**
 * @brief	現在実行中のタスクのタスクコントロールブロック
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	作成
 */
extern tcb_t *	currentTCB ; 





/**
 * @brief	スケジューリング要求フラグ
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	作成
 */
extern bool_t	rescheduling ; 






/**
 * @typedef	taskproc_t
 * @brief	タスク開始アドレスを示す型
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	作成
 */
typedef void ( * taskproc_t ) ( ptr_t arg ) ; 






/**
 * @brief	タスクの開始アドレスが格納されている配列
 * @note	ユーザー側で定義しなければならない。
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	作成
 */
extern const taskproc_t TaskTable [ ] ; 





/**
 * @brief	カーネルを起動
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	作成
 */
extern void startKERNEL ( void ) ; 







/**
 * @brief			タスクコントロールブロックからタスクIDを得る
 * @param[in] tcb	IDを得たいタスクのタスクコントロールブロック
 * @return			タスクID;
 * @author			Kazuya Fukuhara
 * @date			2010/01/07 14:08:58	作成
 */
extern taskid_t getTaskID ( tcb_t * tcb ) ; 








/**
 * @brief				指定したタスクコントロールブロックを初期状態にリセットする
 * @param[in] taskid	リセット対象のタスクのタスクID
 * @note				指定したタスクのスタックポインタや状態が全てリセットされ、タスクは初期状態となる。
 * @author				Kazuya Fukuhara
 * @date    			2010/01/07 14:08:58	作成
 * @date				2010/08/15 16:11:02	機種依存部分を分離
 */
extern void resetTCB ( taskid_t taskid ) ; 





/**
 * @brief   待ち状態のタスクの待ち時間を更新する
 * @author	Kazuya Fukuhara
 * @date    2010/01/07 14:08:58	作成
 */
extern void updateTick ( void ) ; 






/**
 * @brief   スケジューラ関数
 * @note    スケジューリングを行い、currentTCBに実行すべきタスクのタスクコントロールブロックのポインタを設定する。
 * @author	Kazuya Fukuhara
 * @date    2010/01/07 14:08:58	作成
 */
extern void scheduler ( void ) ; 






/**
 * @brief   カーネルのスタートアップコード
 * @author	Kazuya Fukuhara
 * @date    2010/01/07 14:08:58	作成
 * @date	2010/08/15 16:11:02	機種依存部分を分離
 */
extern void startKernel ( void ) ; 








/**
 * @brief					外部割り込みフック表に情報をを設定する
 * @param[in] interrupt_id	フックする外部割り込み番号
 * @param[in] task_id		割り込みで起動するタスクの番号
 * @param[in] priolity		起動するタスクに与える引数(優先度付きタイムシェアリングスケジューリング時に有効)
 * @author					Kazuya Fukuhara
 * @date					2010/09/11 11:01:20	作成
 */
void set_int_hook_tid ( extintid_t interrupt_id , taskid_t task_id




) ; 





/**
 * @brief	外部割り込みに応じてタスクを起動
 * @author	Kazuya Fukuhara
 * @date	2010/09/11 11:01:20	作成
 */
extern void external_interrupt_handler ( void ) ; 






/**
 * @brief   指定領域をゼロクリア
 * @author  Kazuya Fukuhara
 * @date    2010/01/07 14:08:58	作成
 */
extern void ZeroMemory ( ptr_t buf , uint_t szbuf ) ; 


#line 41 "C:\\workspace\\ソースコード入れ\\uKernel-S\\src\\message.h"









/**
 * @typedef struct message_t message_t;
 * @struct  message_t
 * @brief   タスク間通信で用いるメッセージ構造体
 * @note    INITタスクとDIAGタスクではセマフォ使用を禁止する
 * @author  Kazuya Fukuhara
 * @date    2010/01/07 14:08:58	作成
 */
typedef struct message_t { 
	struct message_t *	next ;	/**< 次のメッセージへのリンク */
	taskid_t			from ;	/**< メッセージ送信元のタスクID */
	ptr_t				data ;	/**< メッセージ本文 */
} message_t ; 





/**
 * @brief	メッセージキューを初期化する
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	作成
 */
extern void initMessageQueue ( void ) ; 










/**
 * @brief			タスクのメッセージキューから受信したメッセージを取り出す
 * @param[out] from	送信元のタスクのＩＤ
 * @param[out] data	送信されたメッセージの本文
 * @retval SUCCESS	受信成功
 * @retval ERR31	エラー：INITTASKとDIAGTASKがrecvMSGを呼び出した
 * @retval ERR41	エラー：自分宛てのメッセージはなかった（正常終了）
 * @author  		Kazuya Fukuhara
 * @date			2010/01/07 14:08:58	作成
 */
extern svcresultid_t recvMSG ( taskid_t * from , ptr_t * data ) ; 












/**
 * @brief			指定したタスクのメッセージキューにメッセージを送信する
 * @param[in] to	送信先のタスクのＩＤ
 * @param[in] data	送信するメッセージの本文
 * @retval SUCCESS	受信成功
 * @retval ERR1		エラー：不正なタスクIDがtoに指定されている
 * @retval ERR31	エラー：INITTASKとDIAGTASKがsendMSGを呼び出した
 * @retval ERR40	エラー：メール送信失敗（dormant状態のタスクへのメッセージ送信）
 * @retval ERR41	エラー：自分宛てのメッセージはなかった（正常終了）
 * @author  		Kazuya Fukuhara
 * @date			2010/01/07 14:08:58	作成
 */
extern svcresultid_t sendMSG ( taskid_t to , ptr_t data ) ; 






/**
 * @brief			メッセージを受信するまで待ち状態となる
 * @author  		Kazuya Fukuhara
 * @retval SUCCESS	成功
 * @date			2010/01/07 14:08:58	作成
 */
extern svcresultid_t waitMSG ( void ) ; 






/**
 * @brief			指定したタスクのメッセージをすべて開放
 * @param[in] tcb	タスクのタスクポインタ
 * @author  		Kazuya Fukuhara
 * @date			2010/01/07 14:08:58	作成
 */
extern void unlinkMSG ( tcb_t * tcb ) ; 


#line 38 "C:\\workspace\\ソースコード入れ\\uKernel-S\\src\\message.c"






/**
 * @brief	メッセージ領域
 * @author  Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	作成
 */
static message_t	message_table [ ( 16 ) ] ; 





/**
 * @brief	空きメッセージ領域を管理するリンクリスト
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	作成
 */
static message_t *	free_message_list ; 





/**
 * @brief	メッセージキューを初期化する
 * @author	Kazuya Fukuhara
 * @date	2010/01/07 14:08:58	作成
 */
void initMessageQueue ( void ) { 
	int i ; 
	free_message_list = & message_table [ 0 ] ; 
	for ( i = 1 ; i < ( 16 ) ; i ++ ) { 
		message_table [ i - 1 ] . next = & message_table [ i ] ; 
	} 
	message_table [ ( 16 ) - 1 ] . next = ( ( ptr_t ) 0 ) ; 
} 










/**
 * @brief			タスクのメッセージキューから受信したメッセージを取り出す
 * @param[out] from	送信元のタスクのＩＤ
 * @param[out] data	送信されたメッセージの本文
 * @retval SUCCESS	受信成功
 * @retval ERR31	エラー：INITTASKとDIAGTASKがrecvMSGを呼び出した
 * @retval ERR41	エラー：自分宛てのメッセージはなかった（正常終了）
 * @author  		Kazuya Fukuhara
 * @date			2010/01/07 14:08:58	作成
 */
svcresultid_t recvMSG ( taskid_t * from , ptr_t * data ) { 
	message_t * p ; 
	if ( ( & ( tasks [ 0 ] ) == currentTCB ) || ( & ( tasks [ ( 3 + 2 ) - 1 ] ) == currentTCB ) ) { 
		/* INITTASKとDIAGTASKがrecvMSGを呼び出した */
		return ERR31 ; 
	} 

	/* 自分宛てのメッセージが存在しているか判定 */
	if ( currentTCB -> message == ( ( ptr_t ) 0 ) ) { 
		/* 自分宛てのメッセージはなかったので正常終了 */
		return ERR41 ; 
	} else { 
		/* 自分宛てのメッセージがあったので、受信処理 */
		p = currentTCB -> message ; 
		currentTCB -> message = p -> next ; 
		* from = p -> from ; 
		* data = p -> data ; 

		/* 使い終わったメッセージ領域を開放する */
		p -> from = 0 ; 
		p -> next = free_message_list ; 
		free_message_list = p ; 

		return SUCCESS ; 
	} 
} 












/**
 * @brief			指定したタスクのメッセージキューにメッセージを送信する
 * @param[in] to	送信先のタスクのＩＤ
 * @param[in] data	送信するメッセージの本文
 * @retval SUCCESS	受信成功
 * @retval ERR1		エラー：不正なタスクIDがtoに指定されている
 * @retval ERR31	エラー：INITTASKとDIAGTASKがsendMSGを呼び出した
 * @retval ERR40	エラー：メール送信失敗（dormant状態のタスクへのメッセージ送信）
 * @retval ERR41	エラー：自分宛てのメッセージはなかった（正常終了）
 * @author  		Kazuya Fukuhara
 * @date			2010/01/07 14:08:58	作成
 */
svcresultid_t sendMSG ( taskid_t to , ptr_t data ) { 
	message_t * msg ; 
	message_t * p ; 

	if ( to >= ( 3 + 2 ) ) { 
		/* 不正なタスクIDがtoに指定されている */
		return ERR1 ; 
	} else if ( ( to == 0 ) || ( to == ( 3 + 2 ) - 1 ) ) { 
		/* INITTASKとDIAGTASK宛てのメッセージ送信は禁止 */
		return ERR31 ; 
	} else if ( ( & ( tasks [ 0 ] ) == currentTCB ) || ( & ( tasks [ ( 3 + 2 ) - 1 ] ) == currentTCB ) ) { 
		/* INITTASKとDIAGTASKがsendMSGを呼び出した */
		return ERR31 ; 
	} 

	/* dormant状態のタスクへのメッセージ送信をチェック */
	if ( tasks [ to ] . status == DORMANT ) { 
		return ERR40 ; 
	} 

	/* メッセージスロットから空きスロットを取得 */
	msg = free_message_list ; 
	if ( msg == ( ( ptr_t ) 0 ) ) { 
		return ERR40 ; 
	} else { 
		free_message_list = free_message_list -> next ; 
		msg -> next = ( ( ptr_t ) 0 ) ; 
	} 

	/* 送信先のメッセージリストの一番後ろにメッセージを追加 */
	p = tasks [ to ] . message ; 
	if ( p != ( ( ptr_t ) 0 ) ) { 
		while ( p -> next != ( ( ptr_t ) 0 ) ) { 
			p = p -> next ; 
		} 
		p -> next = msg ; 
	} else { 
		tasks [ to ] . message = msg ; 
	} 
	
	/* メッセージ情報を構築して書き込む */
	msg -> next = ( ( ptr_t ) 0 ) ; 
	msg -> from = ( ( taskid_t ) ( ( currentTCB ) - & tasks [ 0 ] ) ) ; 
	msg -> data = data ; 

	/* もしも、送信先がメッセージ待ち状態ならばタスクを起こす */
	if ( tasks [ to ] . status == WAIT_MSG ) { 
		tasks [ to ] . status = READY ;									/* タスクをready状態に設定する */
		removeTaskFromReadyQueue ( & tasks [ to ] ) ;						/* 実行中タスクキューからタスクを取り除く */
		addTaskToReadyQueue ( & tasks [ to ] ) ;							/* タスクを実行中タスクのキューに追加する */
		rescheduling = ( & tasks [ to ] < currentTCB ) ? TRUE : FALSE ;	/* スケジューリングが必要なら実行する */
	} 
	
	return SUCCESS ; 
} 






/**
 * @brief			メッセージを受信するまで待ち状態となる
 * @author  		Kazuya Fukuhara
 * @retval SUCCESS	成功
 * @date			2010/01/07 14:08:58	作成
 */
svcresultid_t waitMSG ( void ) { 
	if ( currentTCB -> message == ( ( ptr_t ) 0 ) ) {			/* メッセージを受信していない場合は待ち状態に入る */
		currentTCB -> status = WAIT_MSG ;			/* タスクの状態をメッセージ待ち状態に変化させる */
		removeTaskFromReadyQueue ( currentTCB ) ;	/* 実行中タスクキューからタスクを取り除く */
		rescheduling = TRUE ; 
	} 
	return SUCCESS ; 
} 






/**
 * @brief			指定したタスクのメッセージをすべて開放
 * @param[in] tcb	タスクのタスクポインタ
 * @author  		Kazuya Fukuhara
 * @date			2010/01/07 14:08:58	作成
 */
void unlinkMSG ( tcb_t * tcb ) { 
	/* タスクのメッセージをすべて開放 */
	if ( ( tcb != ( ( ptr_t ) 0 ) ) && ( tcb -> message != ( ( ptr_t ) 0 ) ) ) { 
		message_t * p = tcb -> message ; 
		while ( p -> next ) { 
			p = p -> next ; 
		} 
		p -> next = free_message_list ; 
		free_message_list = tcb -> message ; 
		tcb -> message = ( ( ptr_t ) 0 ) ; 
	} 
} 

