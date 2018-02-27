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

#ifndef __config_h__
#define __config_h__

/* アーキテクチャ */

/**
 * @def		ARCH_DUMMY
 * @brief	ダミーターゲット
 * @author  Kazuya Fukuhara
 * @date	2010/08/15 16:11:02	作成
 */
#define ARCH_DUMMY  0x00

/**
 * @def		ARCH_AVR
 * @brief	AVRマイコンを対象とする
 * @author  Kazuya Fukuhara
 * @date	2010/08/15 16:11:02	作成
 */
#define ARCH_AVR    0x01

/**
 * @def		ARCH_WIN32
 * @brief	WIN32アプリケーションを対象とする
 * @author  Kazuya Fukuhara
 * @date	2010/09/01 21:36:49	作成
 */
#define ARCH_WIN32  0x02

/**
 * @def		ARCH_M16C_TBIRD
 * @brief	M16Cマイコン搭載T-Birdボードを対象とする
 * @author  Kazuya Fukuhara
 * @date	2010/09/04 19:46:15	作成
 */
#define ARCH_M16C_TBIRD  0x03

/**
 * @def		ARCH_LPC1343
 * @brief	LPC1343(LPCXpresso1343)を対象とする
 * @author  Kazuya Fukuhara
 * @date	2010/09/04 19:46:15	作成
 */
#define ARCH_LPC1343  0x04

/**
 * @def		ARCH_RX63N
 * @brief	RX63N(GR-SAKURA)を対象とする
 * @author  Kazuya Fukuhara
 * @date	2010/09/04 19:46:15	作成
 */
#define ARCH_RX63N  0x05

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
#define TARGET_ARCH ARCH_WIN32

/**
 * @def     TASK_NUM
 * @brief   タスク総数
 * @author  Kazuya Fukuhara
 * @note    コンパイル時にタスク数を決定する。
 * @note    タスク数を増やす場合はここで増やす。
 * @date    2010/01/07 14:08:58	作成
 */
#define TASK_NUM (3+2)

/**
 * @def     TASK_STACK_SIZE
 * @brief   タスクスタックのサイズ
 * @note    一つのタスクに割り当てられる実行時スタックのサイズ
 * @author  Kazuya Fukuhara
 * @date    2010/01/07 14:08:58	作成
 * @date	2010/09/01 21:36:49	WIN32アーキテクチャの追加にともない、オフセットを指定可能に変更
 */
#define TASK_STACK_SIZE (1024+ARCH_TASK_STACK_OFFSET)

/**
 * @def     KERNEL_STACK_SIZE
 * @brief   カーネルスタックのサイズ
 * @note    カーネルに割り当てられる実行時スタックのサイズ
 * @author  Kazuya Fukuhara
 * @date    2010/01/07 14:08:58	作成
 * @date	2010/09/01 21:36:49	WIN32アーキテクチャの追加にともない、オフセットを指定可能に変更
 */
#define KERNEL_STACK_SIZE	(64+ARCH_TASK_STACK_OFFSET)

/**
 * @def     SEMAPHO_NUM
 * @brief   カーネル全体で使えるセマフォの総数
 * @author  Kazuya Fukuhara
 * @date    2010/01/07 14:08:58	作成
 */
#define SEMAPHO_NUM (64)

/**
 * @def     MESSAGE_NUM
 * @brief   カーネル全体で使えるメッセージの総数
 * @author  Kazuya Fukuhara
 * @date    2010/01/07 14:08:58	作成
 */
#define MESSAGE_NUM (16)

/**
 * @def     SCHEDULING_PRIORITY_BASE
 * @brief   固定優先度スケジューリング
 * @author  Kazuya Fukuhara
 * @date	2010/09/09 12:48:22	作成
 *
 * 優先度の高いタスクが実行権を得ている場合、そのタスクが待ち状態になるか、
 * より高い優先度を持ったタスクが実行可能状態にならない限り、実行権を放棄しない。
 */
#define SCHEDULING_PRIORITY_BASE 0

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
#define SCHEDULING_TIME_SHARING 1

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
#define SCHEDULING_PRIORITY_BASE_TIME_SHARING 2

/**
 * @def     SCHEDULER_TYPE
 * @brief   スケジューラを選択
 * @author  Kazuya Fukuhara
 * @date	2010/09/09 12:48:22	作成
 */
#define SCHEDULER_TYPE SCHEDULING_PRIORITY_BASE

#if (SCHEDULER_TYPE == SCHEDULING_TIME_SHARING) || (SCHEDULER_TYPE == SCHEDULING_PRIORITY_BASE_TIME_SHARING) 
/**
 * @def		TIME_SHARING_THRESHOLD
 * @brief	タスク切り替えを発生させるCPU時間の閾値
 * @author	Kazuya Fukuhara
 * @date	2010/09/09 12:48:22	作成
 * @date	2010/09/10 10:57:13	優先度付きタイムシェアリングスケジューリングでの利用を追加
 *
 * タイムシェアリングスケジューリング/優先度付きタイムシェアリングスケジューリングで、
 * 強制的にスケジューリングを発生させる時間の閾値
 */
#define TIME_SHARING_THRESHOLD 10
#endif

#endif
